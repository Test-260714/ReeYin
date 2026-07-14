using OpenCvSharp;
using Prism.Events;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Repository;
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
    public class MenuIdToMenuNameConverter : IValueConverter
    {
        Dictionary<int, string> MenuMap { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                int id => MenuMap.TryGetValue(id, out var n) ? n : id.ToString(),
                string s => s,
                _ => DependencyProperty.UnsetValue
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rolename = value as string;
            int roleid = MenuMap.FirstOrDefault(x => x.Value == rolename).Key;
            return roleid;
        }

        public MenuIdToMenuNameConverter()
        {
            MenuMap = PrismProvider.User.AllMenu.ToDictionary(x => x.MenuId, x => x.Event);
        }

    }
}
