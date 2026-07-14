using ReeYin_V.Core;
using System.Collections.ObjectModel;

namespace ReeYin.RootManager.Models
{
    [Serializable]
    public class RootManagerModel : BindableBase
    {
    }

    public class ModuleInfo
    {
        public string Header { get; set; }
        public ObservableCollection<MenuInfo> Children { get; set; }
    }
}
