using Prism.Ioc;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ReeYin_V.Core.Extension
{
    public static class IDialogRegistrationExtensions
    {
        /// <summary>
        /// 注册一个对话框并添加到Nodify页面的菜单栏
        /// Icon不知道，设置为null
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        /// <param name="containerRegistry"></param>
        /// <param name="name"></param>
        /// <param name="Info"></param>
        public static void RegisterDialogAndMenu<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TView>(this IContainerRegistry containerRegistry, string name = null
            ,MenuInfo Info = null)
        {
            PrismProvider.NodifyMenuManager.AddMenu(Info);
            containerRegistry.RegisterForNavigation<TView>(name);
        }

        /// <summary>
        /// 注册一个对话框并添加到Nodify页面的菜单栏
        /// Icon不知道，设置为null
        /// </summary>
        /// <typeparam name="TView"></typeparam>
        /// <param name="containerRegistry"></param>
        /// <param name="name"></param>
        /// <param name="Info"></param>
        public static void RegisterDialogAndDynamic<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TView>(this IContainerRegistry containerRegistry, string name = null
            , DynamicView Info = null)
        {
            PrismProvider.DynamicViewManager.AddDynamic(Info);
            containerRegistry.RegisterForNavigation<TView>(name);
        }



    }
}
