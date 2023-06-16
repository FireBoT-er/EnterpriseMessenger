using Avalonia.Threading;
using EnterpriseMessengerUI.Models;
using Microsoft.AspNetCore.SignalR.Client;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace EnterpriseMessengerUI.ViewModels
{
    public class ShowAttachmentsWindowViewModel : ViewModelBase
    {
        public ShowAttachmentsWindowViewModel()
        {
            _Attachments = new();
            AddConnectionHandlers();
        }

        private void AddConnectionHandlers()
        {
            MainWindowViewModel.Connection.On<List<JsonElement>>("SendAttachments", (attachments) =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var item in attachments)
                    {
                        MessageAttachmentType type = (MessageAttachmentType)item.GetProperty("type").GetInt32();

                        switch (type)
                        {
                            case MessageAttachmentType.File:
                                Attachments.Add(new AttachmentModel(
                                    item.GetProperty("additionalData").ToString(),
                                    item.GetProperty("data").ToString(),
                                    item.GetProperty("canDownload").GetBoolean())
                                );
                                break;
                            case MessageAttachmentType.Message:
                                Attachments.Add(new AttachmentModel(
                                    item.GetProperty("data").GetProperty("hasPhoto").GetBoolean()
                                        ? $"{ServerSettings.ServerAddress}/UserFiles/Avatars/{item.GetProperty("data").GetProperty("authorId")}.png"
                                        : "/Assets/user.png",
                                    item.GetProperty("data").GetProperty("fullName").ToString(),
                                    item.GetProperty("data").GetProperty("sendDateTime").ToString(),
                                    item.GetProperty("data").GetProperty("text").ToString(),
                                    type,
                                    item.GetProperty("data").GetProperty("messageId").ToString())
                                );
                                break;
                        }
                    }
                });
            });
        }

        private long _MessageId;

        public long MessageId
        {
            get => _MessageId;
            set => this.RaiseAndSetIfChanged(ref _MessageId, value);
        }

        private ObservableCollection<AttachmentModel> _Attachments;

        public ObservableCollection<AttachmentModel> Attachments
        {
            get => _Attachments;
            set => this.RaiseAndSetIfChanged(ref _Attachments, value);
        }
    }
}
