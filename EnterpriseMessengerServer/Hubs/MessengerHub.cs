using EnterpriseMessengerServer.Models;
using EnterpriseMessengerServer.Models.Helpers;
using LinqKit;
using Lucene.Net.Analysis.Ru;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Data;
using System.Text.Json;

namespace EnterpriseMessengerServer.Hubs
{
    [Authorize]
    public class MessengerHub : Hub<IClient>
    {
        private readonly DBContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IWebHostEnvironment appEnvironment;
        private readonly IConfigurationRoot JSONParams;

        public MessengerHub(DBContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment appEnvironment)
        {
            this.context = context;
            this.userManager = userManager;
            this.appEnvironment = appEnvironment;
            JSONParams = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            Attachments = new();
        }

        public async Task GetMessagesFromChat(string id, int skip, bool isGroupChat)
        {
            ApplicationUser? user = await userManager.FindByNameAsync(Context.UserIdentifier!);

            List<Message> messages = context.Messages.Include(m => m.Attachments).ToList();

            messages = !isGroupChat
                ? messages.Where(m => (m.AuthorId == id && m.ReceiverUserId == user!.Id) || (m.AuthorId == user!.Id && m.ReceiverUserId == id)).ToList()
                : messages.Where(m => m.ReceiverChatId == id).ToList();

            messages = messages.OrderByDescending(m => m.SendDateTime).
                                Skip(skip).Take(Math.Abs(JSONParams.GetValue<int>("DataAmounts:Messages"))).
                                ToList();

            ApplicationUserAndGroupChat? applicationUserAndGroupChat = null;
            if (isGroupChat)
            {
                applicationUserAndGroupChat = context.ApplicationUserAndGroupChat.Where(auagc => auagc.ParticipantId == user!.Id && auagc.GroupChatId == id).First();
            }

            bool HasBeenReadAtLeastOnce = false;
            int i = 0;

            if (isGroupChat && messages.Count > 0)
            {
                applicationUserAndGroupChat!.LastReadMessageId = messages[i].Id;
                HasBeenReadAtLeastOnce = true;
            }

            for (; i < messages.Count; i++)
            {
                if (isGroupChat && messages[i].AuthorId != user!.Id && !messages[i].HasRead)
                {
                    messages[i].HasRead = true;
                    HasBeenReadAtLeastOnce = true;
                }
                else if (messages[i].AuthorId == id && !messages[i].HasRead)
                {
                    messages[i].HasRead = true;
                    HasBeenReadAtLeastOnce = true;
                }
                else
                {
                    break;
                }
            }

            if (HasBeenReadAtLeastOnce)
            {
                context.SaveChanges();
            }

            List<bool> hasAttachments = new();
            hasAttachments.AddRange(messages.Select(message => message.Attachments.Count > 0));
            messages.ForEach(m => m.Attachments.Clear());

            List<object> authors = new();
            foreach (var message in messages)
            {
                ApplicationUser? author = await userManager.FindByIdAsync(message.AuthorId!);
                authors.Add(new { author!.Id, author!.Surname, Name = author!.Name[0], Patronymic = author!.Patronymic[0], author!.HasPhoto });
            }

            await Clients.User(Context.UserIdentifier!).SendMessages(messages, hasAttachments, i > 1 ? --i : 0, isGroupChat ? authors : new List<object>());

            if (HasBeenReadAtLeastOnce)
            {
                if (!isGroupChat)
                {
                    await Clients.User(userManager.FindByIdAsync(id).Result!.UserName!).MessageHasBeenRead(user!.Id);
                }
                else
                {
                    await Clients.GroupExcept(id, Context.ConnectionId).MessageHasBeenRead(id);
                }
            }
        }

        public void CanDownloadFile(string data)
        {
            context.MessageAttachments.Where(ma => ma.Data == data).First().CanDownload = true;
            context.SaveChanges();
        }

        public async Task RemoveUnuploadedAttachment(string data)
        {
            var attachment = context.MessageAttachments.Where(ma => ma.Data == data).First();
            var message = context.Messages.Include(m => m.Attachments).Where(m => m.Id == attachment.MessageId).First();
            message.EditDateTime = DateTime.Now;
            context.MessageAttachments.Remove(attachment);
            context.SaveChanges();

            bool hasAttachments = message.Attachments.Count > 0;
            if (!hasAttachments && message.Text == string.Empty)
            {
                await DeleteMessage(message.Id, DateTime.Now);
            }
            else
            {
                if (message.ReceiverChatId == null)
                {
                    await Clients.User(Context.UserIdentifier!).SendEditedMessage(message.Id, message.Text, message.EditDateTime, hasAttachments);
                    await Clients.User(userManager.FindByIdAsync(message.ReceiverUserId!).Result!.UserName!).SendEditedMessage(message.Id, message.Text, message.EditDateTime, hasAttachments);
                }
                else
                {
                    await Clients.Group(message.ReceiverChatId).SendEditedMessage(message.Id, message.Text, message.EditDateTime, hasAttachments);
                }
            }
        }

        public async Task GetAttachmentsForMessage(long id, bool withId)
        {
            var message = context.Messages.Include(m => m.Attachments).Where(m => m.Id == id).First();
            Attachments = new();
            GetAttachmentsForMessageWorker(message.Attachments, withId);

            if (withId)
            {
                await Clients.User(Context.UserIdentifier!).SendAttachmentsWithIds(Attachments);
            }
            else
            {
                await Clients.User(Context.UserIdentifier!).SendAttachments(Attachments);
            }
        }

        private List<object> Attachments;

        private void GetAttachmentsForMessageWorker(List<MessageAttachment> list, bool withId)
        {
            foreach (var attachment in list)
            {
                switch (attachment.Type)
                {
                    case MessageAttachmentType.File:
                        if (withId)
                        {
                            Attachments.Add(new { attachment.Id, attachment.Type, attachment.Data, attachment.AdditionalData });
                        }
                        else
                        {
                            Attachments.Add(new { attachment.Type, attachment.Data, attachment.AdditionalData, attachment.CanDownload });
                        }

                        break;
                    case MessageAttachmentType.Message:
                        var attachedMessage = context.Messages.Include(m => m.Attachments).Where(m => m.Id == Convert.ToInt64(attachment.Data)).First();
                        var author = userManager.Users.Where(u => u.Id == attachedMessage.AuthorId).First();
                        var data = new
                        {
                            author!.HasPhoto,
                            AuthorId = author!.Id,
                            FullName = $"{author!.Surname} {author!.Name[0]}. {author!.Patronymic[0]}.",
                            SendDateTime = attachedMessage.SendDateTime.ToString("G"),
                            attachedMessage.Text,
                            MessageId = attachedMessage.Id.ToString()
                        };
                        
                        if (withId)
                        {
                            Attachments.Add(new { attachment.Id, attachment.Type, data, AdditionalData = string.Empty });
                        }
                        else
                        {
                            Attachments.Add(new { attachment.Type, data, AdditionalData = string.Empty });

                            if (attachedMessage.Attachments.Count > 0)
                            {
                                GetAttachmentsForMessageWorker(attachedMessage.Attachments, withId);
                            }
                        }

                        break;
                }
            }
        }

        public async Task ReadMessage(long id)
        {
            var message = context.Messages.Where(m => m.Id == id).First();

            if (message.ReceiverChatId != null)
            {
                context.ApplicationUserAndGroupChat.Where(auagc => auagc.ParticipantId == userManager.FindByNameAsync(Context.UserIdentifier!).Result!.Id && auagc.GroupChatId == message.ReceiverChatId).First().LastReadMessageId = message.Id;
            }
            message.HasRead = true;

            context.SaveChanges();

            if (message.ReceiverChatId == null)
            {
                await Clients.User(userManager.FindByIdAsync(message.AuthorId!).Result!.UserName!).MessageHasBeenRead(userManager.FindByNameAsync(Context.UserIdentifier!).Result!.Id);
            }
            else
            {
                await Clients.GroupExcept(message.ReceiverChatId, Context.ConnectionId).MessageHasBeenRead(message.ReceiverChatId);
            }
        }

        public async Task SendMessageToReceivers(string text, string to, DateTime dateTime, bool isGroupChat, List<JsonElement> attachments)
        {
            Message message = new()
            {
                Text = text ?? string.Empty,
                AuthorId = userManager.FindByNameAsync(Context.UserIdentifier!).Result!.Id,
                SendDateTime = dateTime
            };

            if (!isGroupChat)
            {
                message.ReceiverUserId = to;
            }
            else
            {
                message.ReceiverChatId = to;
            }

            context.Messages.Add(message);
            context.SaveChanges();

            ApplicationUser? author = await userManager.FindByNameAsync(Context.UserIdentifier!);

            if (isGroupChat)
            {
                context.ApplicationUserAndGroupChat.Where(auagc => auagc.ParticipantId == author!.Id && auagc.GroupChatId == to).First().LastReadMessageId = message.Id;
                context.SaveChanges();
            }

            bool hasAttachments = attachments.Count > 0;
            if (hasAttachments)
            {
                foreach (var attachment in attachments)
                {
                    context.MessageAttachments.Add(new MessageAttachment
                    {
                        MessageId = message.Id,
                        Type = (MessageAttachmentType)attachment.GetProperty("type").GetInt32(),
                        Data = attachment.GetProperty("data").ToString(),
                        AdditionalData = attachment.GetProperty("additionalData").GetString()
                    });
                }

                context.SaveChanges();
                message.Attachments.Clear();
            }

            if (!isGroupChat)
            {
                await Clients.User(Context.UserIdentifier!).SendMessage(message, hasAttachments, string.Empty);
                await Clients.User(userManager.FindByIdAsync(to).Result!.UserName!).SendMessage(message, hasAttachments, new { author!.HasPhoto, author!.Surname, author!.Name, author!.Patronymic });
            }
            else
            {
                await Clients.Group(to).SendMessage(message, hasAttachments, new { author!.Id, author!.HasPhoto, author!.Surname, author!.Name, author!.Patronymic, ChatName = context.GroupChats.Where(gp => gp.Id == to).First().Name });
            }
        }

        public async Task EditMessage(long id, string text, DateTime dateTime, List<JsonElement> attachments)
        {
            if ((DateTime.Now - dateTime) > TimeSpan.FromMinutes(Math.Abs(JSONParams.GetValue<int>("EditDeleteTimeLimit"))+1))
            {
                return;
            }

            var message = context.Messages.Include(m => m.Attachments).Where(m => m.Id == id).First();
            message.Text = text;
            message.EditDateTime = dateTime;
            context.SaveChanges();

            bool invalidOperation = false;
            bool hasAttachments = attachments.Count > 0;
            if (hasAttachments)
            {
                List<long> attachmentsIds = new();

                foreach (var attachment in attachments)
                {
                    long attachmentId = attachment.GetProperty("id").GetInt64();
                    if (attachmentId == 0)
                    {
                        context.MessageAttachments.Add(new MessageAttachment
                        {
                            MessageId = message.Id,
                            Type = (MessageAttachmentType)attachment.GetProperty("type").GetInt32(),
                            Data = attachment.GetProperty("data").ToString(),
                            AdditionalData = attachment.GetProperty("additionalData").GetString()
                        });
                    }
                    else
                    {
                        attachmentsIds.Add(attachmentId);
                    }
                }

                if (attachmentsIds.Count > 0)
                {
                    for (int i = 0; i < message.Attachments.Count; i++)
                    {
                        if (message.Attachments[i].Id != 0 && !attachmentsIds.Contains(message.Attachments[i].Id))
                        {
                            var attachment = context.MessageAttachments.Where(ma => ma.Id == message.Attachments[i].Id).First();

                            try
                            {
                                if (attachment.Type == MessageAttachmentType.File)
                                {
                                    File.Delete(appEnvironment.ContentRootPath + "/UserFiles/Attachments/" + attachment.Data);
                                }

                                context.MessageAttachments.Remove(attachment);
                            }
                            catch
                            {
                                invalidOperation = true;
                            }
                        }
                    }
                }
                else
                {
                    invalidOperation = DeleteAttachments(id);
                }

                context.SaveChanges();
                message.Attachments.Clear();
            }
            else
            {
                invalidOperation = DeleteAttachments(id);
                context.SaveChanges();
            }

            if (message.ReceiverChatId == null)
            {
                await Clients.User(Context.UserIdentifier!).SendEditedMessage(message.Id, message.Text, message.EditDateTime, hasAttachments);
                await Clients.User(userManager.FindByIdAsync(message.ReceiverUserId!).Result!.UserName!).SendEditedMessage(message.Id, message.Text, message.EditDateTime, hasAttachments);
            }
            else
            {
                await Clients.Group(message.ReceiverChatId).SendEditedMessage(message.Id, message.Text, message.EditDateTime, hasAttachments);
            }

            if (invalidOperation)
            {
                await Clients.User(Context.UserIdentifier!).InvalidOperationAttachmentIsUploadingEdit();
            }
        }

        private bool DeleteAttachments(long messageId)
        {
            var attachments = context.MessageAttachments.Where(ma => ma.MessageId == messageId).ToList();

            bool invalidOperation = false;
            foreach (var attachment in new List<MessageAttachment>(attachments.Where(a => a.Type == MessageAttachmentType.File)))
            {
                try
                {
                    File.Delete(appEnvironment.ContentRootPath + "/UserFiles/Attachments/" + attachment.Data);
                }
                catch
                {
                    invalidOperation = true;
                    attachments.Remove(attachment);
                }
            }

            context.MessageAttachments.RemoveRange(attachments);

            return invalidOperation;
        }

        public async Task DeleteMessage(long id, DateTime dateTime)
        {
            if ((DateTime.Now - dateTime) > TimeSpan.FromMinutes(Math.Abs(JSONParams.GetValue<int>("EditDeleteTimeLimit"))+1))
            {
                return;
            }

            var message = context.Messages.Where(m => m.Id == id).First();

            List<Message> messages = message.ReceiverChatId == null
                ? context.Messages.Where(m => (m.AuthorId == message.AuthorId && m.ReceiverUserId == message.ReceiverUserId) || (m.AuthorId == message.ReceiverUserId && m.ReceiverUserId == message.AuthorId)).ToList()
                : context.GroupChats.Include(gp => gp.Messages).Where(gp => gp.Id == message.ReceiverChatId).First().Messages;

            messages = messages.OrderBy(m => m.SendDateTime).ToList();

            int messageIndex = messages.IndexOf(message);
            Message? previousMessage;
            if (messageIndex > 0)
            {
                previousMessage = messages[messageIndex - 1];
            }
            else
            {
                previousMessage = null;
            }

            if (DeleteAttachments(id))
            {
                await Clients.User(Context.UserIdentifier!).InvalidOperationAttachmentIsUploadingDelete();
            }
            else
            {
                var attachments = new List<MessageAttachment>(context.MessageAttachments.Where(ma => ma.Data == id.ToString()));
                for (int i = 0; i < attachments.Count; i++)
                {
                    var messageWithThisMessage = context.Messages.Include(m => m.Attachments).Where(m => m.Id == attachments[i].MessageId).First();
                    if (messageWithThisMessage.Attachments.Count == 1 && string.IsNullOrWhiteSpace(messageWithThisMessage.Text))
                    {
                        await DeleteMessage(messageWithThisMessage.Id, DateTime.Now);
                    }
                    else
                    {
                        context.MessageAttachments.Remove(attachments[i]);
                        i--;

                        messageWithThisMessage.EditDateTime = DateTime.Now;
                        await Clients.User(Context.UserIdentifier!).SendEditedMessage(messageWithThisMessage.Id, messageWithThisMessage.Text, messageWithThisMessage.EditDateTime, true);
                        await Clients.User(userManager.FindByIdAsync(messageWithThisMessage.ReceiverUserId!).Result!.UserName!).SendEditedMessage(messageWithThisMessage.Id, messageWithThisMessage.Text, messageWithThisMessage.EditDateTime, true);
                    }
                }

                if (message.ReceiverChatId != null)
                {
                    var groupChat = context.GroupChats.Include(gp => gp.Messages).Where(gp => gp.Id == message.ReceiverChatId).First();

                    List<Message> groupChatMessages = new(groupChat.Messages);
                    groupChatMessages = groupChatMessages.OrderBy(m => m.SendDateTime).ToList();

                    long? preLastMessageId = groupChatMessages.Count > 1 ? groupChatMessages[groupChatMessages.Count-2].Id : null;
                    context.ApplicationUserAndGroupChat.Where(auagc => auagc.LastReadMessageId == message.Id).ForEach(auagc => auagc.LastReadMessageId = preLastMessageId);
                }

                context.Messages.Remove(message);
                context.SaveChanges();

                if (message.ReceiverChatId == null)
                {
                    await Clients.User(Context.UserIdentifier!).MessageHasBeenDeleted(message.Id, previousMessage);
                    await Clients.User(userManager.FindByIdAsync(message.ReceiverUserId!).Result!.UserName!).MessageHasBeenDeleted(message.Id, previousMessage);
                }
                else
                {
                    await Clients.Group(message.ReceiverChatId).MessageHasBeenDeleted(message.Id, previousMessage);
                }
            }
        }

        public async Task GetChats()
        {
            var user = userManager.Users.Include(u => u.SentMessages).Include(u => u.ReceivedMessages).Where(u => u.UserName == Context.UserIdentifier).First();

            var userGroupChats = context.GroupChats.Where(gp => context.ApplicationUserAndGroupChat.Where(auagc => auagc.ParticipantId == user.Id).Select(auagc => auagc.GroupChatId).Contains(gp.Id));

            IEnumerable<Message> messages = user.SentMessages.ToList();
            messages = messages.Where(m => m.ReceiverChatId == null || userGroupChats.Select(gp => gp.Id).Contains(m.ReceiverChatId)).Concat(user.ReceivedMessages);

            List<GroupChat> emptyChats = new();
            foreach (var groupChat in context.GroupChats.Include(gp => gp.Messages).Where(gp => userGroupChats.Contains(gp)))
            {
                if (groupChat.Messages.Count > 0)
                {
                    messages = messages.Concat(groupChat.Messages);
                }
                else
                {
                    emptyChats.Add(groupChat);
                }
            }

            var ids = messages.OrderByDescending(m => m.SendDateTime).Select(m => m.ReceiverChatId ?? (m.AuthorId == user.Id ? m.ReceiverUserId : m.AuthorId)).ToList();
            ids = ids.Distinct().ToList();
            ids.Reverse();

            List<Message> lastMessages = new();
            List<string> readStatuses = new();
            foreach (var id in ids)
            {
                var userMessages = user.SentMessages.Concat(user.ReceivedMessages).OrderBy(m => m.SendDateTime).Where(m => m.AuthorId == id || m.ReceiverUserId == id).ToList();

                bool isGroupChat = false;
                if (userMessages.Count == 0)
                {
                    userMessages = context.GroupChats.Include(gp => gp.Messages).Where(gp => gp.Id == id).First().Messages.OrderBy(m => m.SendDateTime).ToList();
                    isGroupChat = true;
                }

                Message lastUserMessage = userMessages.Last();
                lastUserMessage.Text = string.IsNullOrEmpty(lastUserMessage.Text) ? "[Вложения]" : lastUserMessage.Text;
                if (lastUserMessage.AuthorId == user.Id)
                {
                    lastUserMessage.Text = $"Вы: {lastUserMessage.Text}";
                }
                lastMessages.Add(lastUserMessage);

                string userCount;
                if (!isGroupChat)
                {
                    int count = userMessages.Count(m => m.AuthorId == id && m.HasRead == false);
                    userCount = (lastUserMessage.AuthorId == id && !lastUserMessage.HasRead) ? (count > 99 ? "99" : count.ToString()) : (lastUserMessage.HasRead ? string.Empty : " ");
                }
                else
                {
                    long? lastReadMessageId = context.ApplicationUserAndGroupChat.Where(auagc => auagc.GroupChatId == id && auagc.ParticipantId == user.Id).First().LastReadMessageId;
                    int count = lastReadMessageId != null
                        ? userMessages.Count - userMessages.IndexOf(userMessages.Where(um => um.Id == lastReadMessageId.Value).First())
                        : userMessages.Count;
                    userCount = count == 0 ? string.Empty : ((lastUserMessage.AuthorId != user!.Id && !lastUserMessage.HasRead) ? (count > 99 ? "99" : count.ToString()) : (context.ApplicationUserAndGroupChat.Where(auagc => auagc.LastReadMessageId == lastUserMessage.Id).Count() > 1 ? string.Empty : " "));
                }
                readStatuses.Add(userCount);
            }

            List<object> chats = new();
            foreach (var message in lastMessages)
            {
                if (message.ReceiverChatId == null)
                {
                    ApplicationUser? secondParticipant = await userManager.FindByIdAsync(message.AuthorId! == user.Id ? message.ReceiverUserId! : message.AuthorId!);
                    chats.Add(new { secondParticipant!.Id, secondParticipant!.Surname, secondParticipant!.Name, secondParticipant!.Patronymic, secondParticipant!.HasPhoto, secondParticipant!.Position, secondParticipant!.Information, secondParticipant!.HasAccess,
                                    NetworkStatus = MessengerHubHelpers.UsersOnline.TryGetValue(secondParticipant!.Id, out NetworkStatus value) ? value : NetworkStatus.Offline });
                }
                else
                {
                    chats.Add(context.GroupChats.Where(gp => gp.Id == message.ReceiverChatId).Select(gp => new { gp.Id, gp.Name, gp.AuthorId, gp.HasPhoto, NetworkStatus = NetworkStatus.Offline }).First());
                }
            }

            foreach (var chat in emptyChats)
            {
                chats.Insert(0, new { chat.Id, chat.Name, chat.AuthorId, chat.HasPhoto, NetworkStatus = NetworkStatus.Offline });
                lastMessages.Insert(0, new Message() { Id = -1, Text = string.Empty });
                readStatuses.Insert(0, string.Empty);
            }

            await Clients.User(Context.UserIdentifier!).SendChats(chats, lastMessages.Select(lm => new { lm.Id, lm.Text, lm.SendDateTime }), readStatuses);
        }

        public async Task MessagesChatsSearch(string searchQuery, string chatId, bool isGroupChat)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await GetChats();
                return;
            }

            bool isGlobalSearch = string.IsNullOrWhiteSpace(chatId) && isGroupChat == false;

            var user = userManager.Users.Include(u => u.SentMessages).Include(u => u.ReceivedMessages).Where(u => u.UserName == Context.UserIdentifier).First();

            IEnumerable<Message> messagesToSearch;
            if (isGlobalSearch)
            {
                var userGroupChats = context.GroupChats.Where(gp => context.ApplicationUserAndGroupChat.Where(auagc => auagc.ParticipantId == user.Id).Select(auagc => auagc.GroupChatId).Contains(gp.Id));

                messagesToSearch = user.SentMessages.ToList();
                messagesToSearch = messagesToSearch.Where(m => m.ReceiverChatId == null || userGroupChats.Select(gp => gp.Id).Contains(m.ReceiverChatId)).Concat(user.ReceivedMessages);

                foreach (var groupChat in context.GroupChats.Include(gp => gp.Messages).Where(gp => userGroupChats.Contains(gp)))
                {
                    messagesToSearch = messagesToSearch.Concat(groupChat.Messages);
                }
                messagesToSearch = messagesToSearch.Distinct();
            }
            else
            {
                messagesToSearch = !isGroupChat
                    ? context.Messages.Where(m => (m.AuthorId == chatId && m.ReceiverUserId == user!.Id) || (m.AuthorId == user!.Id && m.ReceiverUserId == chatId))
                    : context.Messages.Where(m => m.ReceiverChatId == chatId);
            }

            var indexDirectory = new RAMDirectory();
            var analyzer = new RussianAnalyzer(LuceneVersion.LUCENE_48);
            using (var indexWriter = new IndexWriter(indexDirectory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
            {
                foreach (var message in messagesToSearch)
                {
                    var doc = new Document
                    {
                        new Int64Field("Id", message.Id, Field.Store.YES),
                        new TextField("Text", message.Text, Field.Store.YES),
                    };

                    indexWriter.AddDocument(doc);
                }

                indexWriter.Commit();
            }

            List<object> messages = new();

            var indexSearcher = new IndexSearcher(DirectoryReader.Open(indexDirectory));
            foreach (var scoreDoc in indexSearcher.Search(new QueryParser(LuceneVersion.LUCENE_48, "Text", analyzer).Parse(QueryParserBase.Escape(searchQuery)), int.MaxValue).ScoreDocs)
            {
                var document = indexSearcher.Doc(scoreDoc.Doc);
                var id = long.Parse(document.Get("Id"));

                var message = context.Messages.Where(m => m.Id == id).First();
                if (message.AuthorId == user.Id)
                {
                    message.Text = $"Вы: {message.Text}";
                }
                messages.Add(message);
            }

            messages = messages.OrderBy(m => ((Message)m).SendDateTime).ToList();

            List<object> chats = new();
            foreach (var message in messages.Cast<Message>())
            {
                if (message.ReceiverChatId == null)
                {
                    ApplicationUser? secondParticipant = await userManager.FindByIdAsync(message.AuthorId! == user.Id ? message.ReceiverUserId! : message.AuthorId!);
                    chats.Add(new { secondParticipant!.Id, secondParticipant!.Surname, secondParticipant!.Name, secondParticipant!.Patronymic, secondParticipant!.HasPhoto, secondParticipant!.Position, secondParticipant!.Information, secondParticipant!.HasAccess, NetworkStatus = NetworkStatus.Offline });
                }
                else
                {
                    chats.Add(context.GroupChats.Where(gp => gp.Id == message.ReceiverChatId).Select(gp => new { gp.Id, gp.Name, gp.AuthorId, gp.HasPhoto, NetworkStatus = NetworkStatus.Offline }).First());
                }
            }

            if (isGlobalSearch)
            {
                var predicate = PredicateBuilder.New<ApplicationUser>(true);
                foreach (var value in searchQuery.Split(' '))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        predicate = predicate.And(u => u.Surname.Contains(value) ||
                                                       u.Name.Contains(value) ||
                                                       u.Patronymic.Contains(value) ||
                                                       u.Position.Contains(value));
                    }
                }

                chats.AddRange(userManager.Users.Where(u => u.UserName != Context.UserIdentifier && (u.HasAccess || context.Messages.Any(m => (m.AuthorId == u.Id && m.ReceiverUserId == user.Id) || (m.AuthorId == user.Id && m.ReceiverUserId == u.Id)))).
                                                 Where(predicate).
                                                 OrderBy(u => u.Surname).ThenBy(u => u.Name).ThenBy(u => u.Patronymic).ThenBy(u => u.Position).
                                                 Select(u => new { u.Id, u.Surname, u.Name, u.Patronymic, u.HasPhoto, u.Position, u.Information, u.HasAccess, NetworkStatus = NetworkStatus.Offline }).
                                                 ToList());

                chats.AddRange(context.GroupChats.Where(gp => gp.Name.Contains(searchQuery)).Select(gp => new { gp.Id, gp.Name, gp.AuthorId, gp.HasPhoto, NetworkStatus = NetworkStatus.Offline }).ToList());

                if (searchQuery.Contains("чат") || searchQuery.Contains("груп"))
                {
                    chats.AddRange(context.GroupChats.Select(gp => new { gp.Id, gp.Name, gp.AuthorId, gp.HasPhoto, NetworkStatus = NetworkStatus.Offline }).ToList());
                }

                messages.AddRange(Enumerable.Repeat(new { Id = -1, Text = string.Empty, SendDateTime = string.Empty }, chats.Count - messages.Count));
            }

            await Clients.User(Context.UserIdentifier!).SendChats(chats, messages, Enumerable.Repeat(string.Empty, messages.Count).ToList());
        }

        public async Task GetMessagesSearchLoadCount(long id)
        {
            var user = userManager.Users.Include(u => u.SentMessages).Include(u => u.ReceivedMessages).Where(u => u.UserName == Context.UserIdentifier).First();

            var message = context.Messages.Where(m => m.Id == id).First();
            var secondParticipantId = message.AuthorId == user.Id ? message.ReceiverUserId : message.AuthorId;

            var messages = user.SentMessages.Where(sm => sm.ReceiverUserId == secondParticipantId).ToList();
            messages.AddRange(user.ReceivedMessages.Where(rm => rm.AuthorId == secondParticipantId));
            messages = messages.OrderBy(m => m.SendDateTime).ToList();

            await Clients.User(Context.UserIdentifier!).SendMessagesSearchLoadCount(messages.Count - messages.IndexOf(message));
        }

        public async Task GetUsers(string searchQuery, int skip)
        {
            var predicate = PredicateBuilder.New<ApplicationUser>(true);
            foreach (var value in searchQuery.Split(' '))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    predicate = predicate.And(u => u.Surname.Contains(value) ||
                                                   u.Name.Contains(value) ||
                                                   u.Patronymic.Contains(value) ||
                                                   u.Position.Contains(value));
                }
            }

            await Clients.User(Context.UserIdentifier!).SendUsers(
                    userManager.Users.Where(u => u.UserName != Context.UserIdentifier && u.HasAccess).
                                      Where(predicate).
                                      OrderBy(u => u.Surname).ThenBy(u => u.Name).ThenBy(u => u.Patronymic).ThenBy(u => u.Position).
                                      Select(u => new { u.Id, u.Surname, u.Name, u.Patronymic, u.HasPhoto, u.Position, u.Information }).
                                      Skip(skip).Take(Math.Abs(JSONParams.GetValue<int>("DataAmounts:Users"))).
                                      ToList());
        }

        public async Task GetNewChat(string id)
        {
            ApplicationUser? user = await userManager.FindByIdAsync(id);
            await Clients.User(Context.UserIdentifier!).SendNewChat(new { user!.Id, user!.Surname, user!.Name, user!.Patronymic, user!.HasPhoto, user!.Position, user!.Information, NetworkStatus = MessengerHubHelpers.UsersOnline.TryGetValue(user!.Id, out NetworkStatus value) ? value : NetworkStatus.Offline });
        }

        public async Task CreateGroupChat(string name, string ids)
        {
            ApplicationUser? user = await userManager.FindByNameAsync(Context.UserIdentifier!);

            GroupChat groupChat = new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                AuthorId = user!.Id,
            };

            List<ApplicationUserAndGroupChat> participants = new()
            {
                new ApplicationUserAndGroupChat()
                {
                    GroupChatId = groupChat.Id,
                    ParticipantId = user!.Id
                }
            };

            foreach (string id in ids.Split(';'))
            {
                participants.Add(new ApplicationUserAndGroupChat()
                {
                    GroupChatId = groupChat.Id,
                    ParticipantId = id
                });
            }

            context.GroupChats.Add(groupChat);
            context.ApplicationUserAndGroupChat.AddRange(participants);
            context.SaveChanges();

            groupChat.Author = null;

            foreach (var participant in participants)
            {
                await Clients.User(userManager.FindByIdAsync(participant.ParticipantId).Result!.UserName!).SendNewGroupChat(groupChat);
            }
        }

        public async Task AddToGroupChatNow(string id)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, id);
        }

        public async Task GetGroupChatInformation(string id)
        {
            var groupChat = context.GroupChats.Include(gp => gp.Messages).Where(gp => gp.Id == id).First();
            await Clients.User(Context.UserIdentifier!).SendGroupChatInformation(groupChat.Name, groupChat.HasPhoto, groupChat.Messages.Count == 0);
        }

        public async Task UpdateGroupChatName(string id, string newName)
        {
            var groupChat = context.GroupChats.Where(gp => gp.Id == id).First();
            groupChat.Name = newName;
            context.SaveChanges();

            await Clients.Group(id).SendUpdatedGroupChatName(id, newName);
        }

        public async Task DeleteGroupChatAvatar(string id)
        {
            var groupChat = context.GroupChats.Where(gp => gp.Id == id).First();
            groupChat.HasPhoto = false;
            context.SaveChanges();

            File.Delete(appEnvironment.ContentRootPath + "/UserFiles/GroupChatAvatars/" + groupChat.Id + ".png");

            await Clients.Group(id).SendGroupChatDeletedPhoto(id);
        }

        public async Task DeleteGroupChat(string id)
        {
            var groupChat = context.GroupChats.Where(gp => gp.Id == id).First();

            context.ApplicationUserAndGroupChat.RemoveRange(context.ApplicationUserAndGroupChat.Where(auagc => auagc.GroupChatId == id));
            context.GroupChats.Remove(groupChat);
            context.SaveChanges();

            await Clients.Group(id).GroupChatHasBeenDeleted(id);
        }

        public async Task RemoveFromGroupChatNow(string id)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, id);
        }

        public async Task GetNotGroupChatParticipants(string chatId, string searchQuery, int skip)
        {
            var predicate = PredicateBuilder.New<ApplicationUser>(true);
            foreach (var value in searchQuery.Split(' '))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    predicate = predicate.And(u => u.Surname.Contains(value) ||
                                                   u.Name.Contains(value) ||
                                                   u.Patronymic.Contains(value) ||
                                                   u.Position.Contains(value));
                }
            }

            await Clients.User(Context.UserIdentifier!).SendUsers(
                    userManager.Users.Where(u => u.UserName != Context.UserIdentifier && u.HasAccess && !context.ApplicationUserAndGroupChat.Where(auagc => auagc.GroupChatId == chatId).Select(auagc => auagc.ParticipantId).Contains(u.Id)).
                                      Where(predicate).
                                      OrderBy(u => u.Surname).ThenBy(u => u.Name).ThenBy(u => u.Patronymic).ThenBy(u => u.Position).
                                      Select(u => new { u.Id, u.Surname, u.Name, u.Patronymic, u.HasPhoto, u.Position, u.Information }).
                                      Skip(skip).Take(Math.Abs(JSONParams.GetValue<int>("DataAmounts:Users"))).
                                      ToList());
        }

        public async Task AddParticipantsToGroupChat(string chatId, List<string> receiverIds)
        {
            GroupChat groupChat = context.GroupChats.Where(gp => gp.Id == chatId).First();

            foreach (string receiverId in receiverIds)
            {
                context.ApplicationUserAndGroupChat.Add(new ApplicationUserAndGroupChat()
                {
                    GroupChatId = chatId,
                    ParticipantId = receiverId
                });
                context.SaveChanges();

                await Clients.User(userManager.FindByIdAsync(receiverId).Result!.UserName!).SendNewGroupChat(new GroupChat() { Id = chatId, Name = groupChat.Name, HasPhoto = groupChat.HasPhoto, AuthorId = groupChat.AuthorId });
            }
        }

        public async Task ShowGroupChatParticipants(string chatId)
        {
            GroupChat groupChat = context.GroupChats.Include(gp => gp.Author).Where(gp => gp.Id == chatId).First();

            List<object> participants = new()
            {
                new { groupChat.Author!.Id, groupChat.Author!.Surname, groupChat.Author!.Name, groupChat.Author!.Patronymic, groupChat.Author!.HasPhoto, groupChat.Author!.Position }
            };

            List<ApplicationUser> groupChatParticipants = new();
            foreach (var participantId in context.ApplicationUserAndGroupChat.Where(auagc => auagc.GroupChatId == chatId && auagc.ParticipantId != groupChat.AuthorId).Select(auagc => auagc.ParticipantId).ToList())
            {
                groupChatParticipants.Add(userManager.FindByIdAsync(participantId).Result!);
            }

            participants.AddRange(groupChatParticipants.OrderBy(u => u.Surname).ThenBy(u => u.Name).ThenBy(u => u.Patronymic).ThenBy(u => u.Position).
                                                        Select(u => new { u.Id, u.Surname, u.Name, u.Patronymic, u.HasPhoto, u.Position }));

            await Clients.User(Context.UserIdentifier!).SendUsers(participants);
        }

        public async Task RemoveParticipantsFromGroupChat(string chatId, List<string> receiverIds)
        {
            foreach (string receiverId in receiverIds)
            {
                context.ApplicationUserAndGroupChat.Remove(context.ApplicationUserAndGroupChat.Where(auagc => auagc.GroupChatId == chatId && auagc.ParticipantId == receiverId).First());
                context.SaveChanges();

                await Clients.User(userManager.FindByIdAsync(receiverId).Result!.UserName!).GroupChatHasBeenDeleted(chatId);
            }
        }

        public async Task LeaveGroupChat(string id)
        {
            context.ApplicationUserAndGroupChat.Remove(context.ApplicationUserAndGroupChat.Where(auagc => auagc.GroupChatId == id && auagc.ParticipantId == userManager.FindByNameAsync(Context.UserIdentifier!).Result!.Id).First());
            context.SaveChanges();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, id);
        }

        public async Task UpdateUserInformation(string newInformation)
        {
            var user = userManager.Users.Include(u => u.SentMessages).Include(u => u.ReceivedMessages).Where(u => u.UserName == Context.UserIdentifier).First();
            user.Information = newInformation;
            context.SaveChanges();

            var ids = user.SentMessages.Concat(user.ReceivedMessages).OrderByDescending(m => m.SendDateTime).Select(m => m.AuthorId == user.Id ? m.ReceiverUserId : m.AuthorId).ToList();
            ids = ids.Distinct().ToList();

            if (ids.Any())
            {
                await Clients.Users(userManager.Users.Where(u => ids.Contains(u.Id)).Select(u => u.UserName)!).SendUpdatedUserInformation(user.Id, newInformation);
            }
        }

        public async Task ChangePassword(ChangePassword model)
        {
            ApplicationUser? user = await userManager.FindByNameAsync(Context.UserIdentifier!);
            IdentityResult result = await userManager.ChangePasswordAsync(user!, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await Clients.User(Context.UserIdentifier!).ChangePasswordResults(null);
            }
            else
            {
                await Clients.User(Context.UserIdentifier!).ChangePasswordResults(result.Errors);
            }
        }

        public async Task DeleteUserAvatar()
        {
            var user = userManager.Users.Include(u => u.SentMessages).Include(u => u.ReceivedMessages).Where(u => u.UserName == Context.UserIdentifier).First();
            user.HasPhoto = false;
            context.SaveChanges();

            File.Delete(appEnvironment.ContentRootPath + "/UserFiles/Avatars/" + user.Id + ".png");

            var ids = user.SentMessages.Concat(user.ReceivedMessages).OrderByDescending(m => m.SendDateTime).Select(m => m.AuthorId == user.Id ? m.ReceiverUserId : m.AuthorId).ToList();
            ids = ids.Distinct().ToList();

            if (ids.Any())
            {
                await Clients.Users(userManager.Users.Where(u => ids.Contains(u.Id)).Select(u => u.UserName)!).SendUserDeletedPhoto(user.Id);
            }
        }

        public async Task GetNotes()
        {
            ApplicationUser? user = await userManager.FindByNameAsync(Context.UserIdentifier!);
            var notes = context.Notes.Include(n => n.Owners).Include(n => n.SubPoints).Where(n => n.Owners.Contains(user!)).OrderByDescending(n => n.IsChecked).ToList();
            notes.Reverse();

            List<bool> hasSubPoints = new();
            hasSubPoints.AddRange(notes.Select(note => note.SubPoints.Count > 0));

            List<bool> isAuthor = new();
            isAuthor.AddRange(notes.Select(note => note.Author == user));

            foreach (var note in notes)
            {
                note.SubPoints.Clear();
                note.Owners.Clear();
                note.Author = null;
            }

            await Clients.User(Context.UserIdentifier!).SendNotes(notes, hasSubPoints, isAuthor);
        }

        public async Task GetNote(long id)
        {
            var note = context.Notes.Include(n => n.SubPoints).Where(n => n.Id == id).First();
            note.SubPoints.ForEach(sp => sp.Note = null);

            await Clients.User(Context.UserIdentifier!).SendNote(note);
        }

        public async Task CreateNote(string text, bool isChecked, List<JsonElement> subPoints)
        {
            ApplicationUser? user = await userManager.FindByNameAsync(Context.UserIdentifier!);

            Note note = new()
            {
                Name = text,
                IsChecked = isChecked,
                AuthorId = user!.Id
            };

            note.Owners.Add(user!);

            context.Notes.Add(note);
            context.SaveChanges();

            bool hasSubPoints = subPoints.Count > 0;
            if (hasSubPoints)
            {
                foreach (var subPoint in subPoints)
                {
                    context.NoteSubPoints.Add(new NoteSubPoint
                    {
                        NoteId = note.Id,
                        Text = subPoint.GetProperty("text").ToString(),
                        IsChecked = subPoint.GetProperty("isChecked").GetBoolean(),
                    });
                }

                context.SaveChanges();
                note.SubPoints.Clear();
            }

            note.Owners.Clear();
            note.Author = null;
            await Clients.User(Context.UserIdentifier!).SendNewNote(note, hasSubPoints, true, string.Empty);
        }

        public async Task EditNote(long id, string text, bool isChecked, List<JsonElement> subPoints)
        {
            var note = context.Notes.Include(n => n.SubPoints).Include(n => n.Owners).Where(n => n.Id == id).First();
            note.Name = text;
            note.IsChecked = isChecked;
            context.SaveChanges();

            bool hasSubPoints = subPoints.Count > 0;
            if (hasSubPoints)
            {
                List<long> subPointsIds = new();

                for (int i = 0; i < subPoints.Count; i++)
                {
                    long subPointId = subPoints[i].GetProperty("id").GetInt64();
                    if (subPointId == 0)
                    {
                        context.NoteSubPoints.Add(new NoteSubPoint
                        {
                            NoteId = note.Id,
                            Text = subPoints[i].GetProperty("text").ToString(),
                            IsChecked = subPoints[i].GetProperty("isChecked").GetBoolean(),
                        });
                    }
                    else
                    {
                        var subPoint = context.NoteSubPoints.Where(sp => sp.Id == subPointId).First();
                        subPoint.Text = subPoints[i].GetProperty("text").ToString();
                        subPoint.IsChecked = subPoints[i].GetProperty("isChecked").GetBoolean();

                        subPointsIds.Add(subPointId);
                    }
                }

                if (subPointsIds.Count > 0)
                {
                    for (int i = 0; i < note.SubPoints.Count; i++)
                    {
                        if (note.SubPoints[i].Id != 0)
                        {
                            if (!subPointsIds.Contains(note.SubPoints[i].Id))
                            {
                                context.NoteSubPoints.Remove(context.NoteSubPoints.Where(sp => sp.Id == note.SubPoints[i].Id).First());
                            }
                        }
                    }
                }
                else
                {
                    context.NoteSubPoints.RemoveRange(context.NoteSubPoints.Where(sp => sp.NoteId == id));
                }

                context.SaveChanges();
                note.SubPoints.Clear();
            }
            else
            {
                context.NoteSubPoints.RemoveRange(context.NoteSubPoints.Where(sp => sp.NoteId == id));
                context.SaveChanges();
            }

            List<ApplicationUser> owners = new(note.Owners);
            note.Owners.Clear();
            note.Author = null;

            foreach (var owner in owners)
            {
                await Clients.User(owner.UserName!).SendEditedNote(note, hasSubPoints);
            }
        }

        public async Task DeleteNote(long id)
        {
            var note = context.Notes.Include(n => n.SubPoints).Include(n => n.Owners).Where(n => n.Id == id).First();

            context.NoteSubPoints.RemoveRange(note.SubPoints);
            context.Notes.Remove(note);
            context.SaveChanges();

            foreach (var owner in note.Owners)
            {
                await Clients.User(owner.UserName!).NoteHasBeenDeleted(id);
            }
        }

        public async Task GetNotNoteOwners(long noteId, string searchQuery, int skip)
        {
            Note note = context.Notes.Include(n => n.Owners).Where(n => n.Id == noteId).First();

            var predicate = PredicateBuilder.New<ApplicationUser>(true);
            foreach (var value in searchQuery.Split(' '))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    predicate = predicate.And(u => u.Surname.Contains(value) ||
                                                   u.Name.Contains(value) ||
                                                   u.Patronymic.Contains(value) ||
                                                   u.Position.Contains(value));
                }
            }

            await Clients.User(Context.UserIdentifier!).SendUsers(
                    userManager.Users.Where(u => u.UserName != Context.UserIdentifier && u.HasAccess && !note.Owners.Contains(u)).
                                      Where(predicate).
                                      OrderBy(u => u.Surname).ThenBy(u => u.Name).ThenBy(u => u.Patronymic).ThenBy(u => u.Position).
                                      Select(u => new { u.Id, u.Surname, u.Name, u.Patronymic, u.HasPhoto, u.Position, u.Information }).
                                      Skip(skip).Take(Math.Abs(JSONParams.GetValue<int>("DataAmounts:Users"))).
                                      ToList());
        }

        public async Task ShareNote(long noteId, List<string> receiverIds)
        {
            Note note = context.Notes.Include(n => n.Owners).Include(n => n.SubPoints).Where(n => n.Id == noteId).First();

            foreach (string receiverId in receiverIds)
            {
                ApplicationUser? receiver = await userManager.FindByIdAsync(receiverId);
                note.Owners.Add(receiver!);
                context.SaveChanges();

                bool hasSubPoints = note.SubPoints.Count > 0;
                var authorInfo = new { note.Author!.Id, note.Author!.HasPhoto, note.Author!.Surname, note.Author!.Name, note.Author!.Patronymic };
                Note noteToSend = new() { Id = noteId, Name = note.Name, IsChecked = note.IsChecked };

                await Clients.User(receiver!.UserName!).SendNewNote(noteToSend, hasSubPoints, false, authorInfo);
            }
        }

        public async Task ShowNoteOwners(long noteId)
        {
            Note note = context.Notes.Include(n => n.Owners).Where(n => n.Id == noteId).First();

            List<object> owners = new()
            {
                new { note.Author!.Id, note.Author!.Surname, note.Author!.Name, note.Author!.Patronymic, note.Author!.HasPhoto, note.Author!.Position }
            };

            note.Owners.Remove(note.Author!);
            owners.AddRange(note.Owners.OrderBy(u => u.Surname).ThenBy(u => u.Name).ThenBy(u => u.Patronymic).ThenBy(u => u.Position).
                                        Select(u => new { u.Id, u.Surname, u.Name, u.Patronymic, u.HasPhoto, u.Position }));

            await Clients.User(Context.UserIdentifier!).SendUsers(owners);
        }

        public async Task RemoveOwnersFromNote(long noteId, List<string> receiverIds)
        {
            Note note = context.Notes.Include(n => n.Owners).Where(n => n.Id == noteId).First();

            foreach (string receiverId in receiverIds)
            {
                ApplicationUser? receiver = await userManager.FindByIdAsync(receiverId);
                note.Owners.Remove(receiver!);
                context.SaveChanges();

                await Clients.User(receiver!.UserName!).NoteHasBeenDeleted(noteId);
            }
        }

        public async Task IWannaBe(NetworkStatus networkStatus)
        {
            ApplicationUser? user = await userManager.FindByNameAsync(Context.UserIdentifier!);

            lock (((ICollection)MessengerHubHelpers.UsersOnline).SyncRoot)
            {
                MessengerHubHelpers.UsersOnline[user!.Id] = networkStatus;
            }

            await Clients.AllExcept(Context.ConnectionId).UserNetworkStatusChanged(user!.Id, networkStatus);
        }

        public override async Task OnConnectedAsync()
        {
            ApplicationUser? user = await userManager.FindByNameAsync(Context.UserIdentifier!);

            lock (((ICollection)MessengerHubHelpers.UsersOnline).SyncRoot)
            {
                MessengerHubHelpers.UsersOnline.Add(user!.Id, NetworkStatus.Online);
            }
            await Clients.AllExcept(Context.ConnectionId).UserNetworkStatusChanged(user!.Id, NetworkStatus.Online);

            await Clients.User(Context.UserIdentifier!).SendCurrentUserInformation(
                    new { user!.Id, user!.Surname, user!.Name, user!.Patronymic, user!.Position, user!.HasPhoto, user!.Information },
                    Math.Abs(JSONParams.GetValue<int>("EditDeleteTimeLimit")));

            foreach (var item in context.ApplicationUserAndGroupChat.Where(auagc => auagc.ParticipantId == user!.Id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, item.GroupChatId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ApplicationUser? user = await userManager.FindByNameAsync(Context.UserIdentifier!);

            lock (((ICollection)MessengerHubHelpers.UsersOnline).SyncRoot)
            {
                MessengerHubHelpers.UsersOnline.Remove(user!.Id);
            }

            await Clients.AllExcept(Context.ConnectionId).UserNetworkStatusChanged(user!.Id, NetworkStatus.Offline);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
