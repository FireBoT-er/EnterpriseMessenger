using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System;

namespace EnterpriseMessengerUI.Views
{
    public partial class PseudoNotificationWindow : Window
    {
        public PseudoNotificationWindow()
        {
            InitializeComponent();
            this.WhenAnyValue(x => x.invAvatarI.CurrentImage).Subscribe(_ => ShowImage());
        }

        private void ShowImage()
        {
            ((ImageBrush)avatarI.Background!).Source = (Bitmap)invAvatarI.CurrentImage!;
        }
    }
}
