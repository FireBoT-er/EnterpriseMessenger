using EnterpriseMessengerServer.Models;

namespace EnterpriseMessengerServer.Hubs
{
    public interface IClient
    {
        Task SendMessage(Message message, bool hasAttachments, object author);

        Task MessageHasBeenRead(string id);

        Task SendMessages(List<Message> messages, List<bool> hasAttachments, int unreadedMessagesCount, List<object> authors);

        Task SendAttachments(List<object> attachments);

        Task SendAttachmentsWithIds(List<object> attachments);

        Task SendEditedMessage(long id, string text, DateTime? dateTime, bool hasAttachments);

        Task InvalidOperationAttachmentIsUploadingEdit();

        Task InvalidOperationAttachmentIsUploadingDelete();

        Task MessageHasBeenDeleted(long id, Message? previousMessage);

        Task SendUsers(object users);
        
        Task SendNewChat(object chat);

        Task SendNewGroupChat(object groupChat);

        Task SendGroupChatInformation(string name, bool hasPhoto, bool canDelete);

        Task SendUpdatedGroupChatName(string chatId, string name);

        Task SendGroupChatDeletedPhoto(string chatId);

        Task GroupChatHasBeenDeleted(string chatId);

        Task SendChats(object chats, object lastMessages, List<string> readStatuses);

        Task SendMessagesSearchLoadCount(int count);

        Task SendCurrentUserInformation(object user, int editDeleteTimeLimit);

        Task SendUpdatedUserInformation(string userId, string information);

        Task ChangePasswordResults(object? result);

        Task SendUserDeletedPhoto(string userId);

        Task SendNotes(List<Note> notes, List<bool> hasSubPoints, List<bool> isAuthor);

        Task SendNote(Note note);

        Task SendNewNote(Note note, bool hasSubPoints, bool isAuthor, object author);

        Task SendEditedNote(Note note, bool hasSubPoints);

        Task NoteHasBeenDeleted(long id);

        Task UserNetworkStatusChanged(string userId, NetworkStatus networkStatus);
    }
}
