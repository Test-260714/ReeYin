using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

namespace ReeYin_V.Config.Models
{
    public sealed class ThemeOption
    {
        public ThemeOption(string name, string filePath)
        {
            Name = name;
            FilePath = filePath;
        }

        public string Name { get; }

        public string FilePath { get; }

        public string DisplayName => $"{Name} - {Path.GetFileName(FilePath)}";
    }

    public sealed class StyleThemeDocument
    {
        public string Name { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public XDocument Document { get; set; } = new XDocument();

        public ObservableCollection<StyleResourceItem> Colors { get; } = new ObservableCollection<StyleResourceItem>();

        public ObservableCollection<StyleResourceItem> FontSizes { get; } = new ObservableCollection<StyleResourceItem>();
    }
}
