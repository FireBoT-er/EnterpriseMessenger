using EnterpriseMessengerUI.ViewModels;

namespace EnterpriseMessengerUI.Models
{
    public class TabItemModel
    {
        public string Header { get; }

        public string? ImagePath { get; }

        public string? ImagePointeroverPath { get; }

        public ViewModelBase? Content { get; }

        public TabItemModel(string header, string? imagePath, string? imagePointeroverPath, ViewModelBase? content)
        {
            Header = header;
            ImagePath = imagePath;
            ImagePointeroverPath = imagePointeroverPath;
            Content = content;
        }
    }
}
