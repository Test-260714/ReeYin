using Prism.Events;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ReeYin_V.UserManager.Converters
{
    public class RoleIdToRoleNameConverter : IValueConverter
    {

        Dictionary<int, string> RoleMap { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                int id => RoleMap.TryGetValue(id, out var n) ? n : id.ToString(),
                string s => s,
                _ => DependencyProperty.UnsetValue
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rolename = value as string;
            int roleid = RoleMap.FirstOrDefault(x => x.Value == rolename).Key;
            return roleid;
        }

        public RoleIdToRoleNameConverter()
        {
            RoleMap = PrismProvider.User.AllRole.ToDictionary(x => x.RoleId, x => x.RoleName);
            PrismProvider.EventAggregator.GetEvent<RolesChangeEvent>().Subscribe(RefreshRoleMap, ThreadOption.UIThread,false);
        }

        public void RefreshRoleMap()
        {
            RoleMap = PrismProvider.User.AllRole.ToDictionary(x => x.RoleId, x => x.RoleName);
        }
        

        ~RoleIdToRoleNameConverter()
        {
            PrismProvider.EventAggregator.GetEvent<RolesChangeEvent>().Unsubscribe(RefreshRoleMap);
        }
    }
}
