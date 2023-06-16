using EnterpriseMessengerUI.Models;
using System;

namespace EnterpriseMessengerUI.ViewModels
{
    public class TabsViewModel : PageViewModelBase
    {
        public static TabItemModel[] TabItems
        {
            get => new TabItemModel[] {
                new TabItemModel(string.Empty, null, null, null),
                new TabItemModel("Сообщения", "/Assets/chat.png", "/Assets/chat_pointerover.png", new MessagesTabViewModel()),
                new TabItemModel("Заметки", "/Assets/contract.png", "/Assets/contract_pointerover.png", new NotesTabViewModel()),
                new TabItemModel("Настройки", "/Assets/cogwheel.png", "/Assets/cogwheel_pointerover.png", new SettingsTabViewModel()),
                new TabItemModel(string.Empty, null, null, null)
            };
        }

        public override bool CanNavigateNext
        {
            get => false;
            protected set => throw new NotSupportedException();
        }

        public override bool CanNavigatePrevious
        {
            get => true;
            protected set => throw new NotSupportedException();
        }
    }
}