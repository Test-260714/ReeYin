using DryIoc;
using OpenCvSharp.Internal;
using Prism.Dialogs;
using Prism.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using ReeYin_V.Core;
using ReeYin_V.Core.Cache;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using ReeYin_V.Share.Events;
using ReeYin_V.Share.Prism;
using ReeYin_V.Shell.Views;
using ReeYin_V.UI.Style.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ReeYin_V.Shell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static Mutex _mutex;
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            //程序域异常
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logs.LogError((Exception)(e.ExceptionObject));
                MessageBox.Show(e.ExceptionObject.ToString(), "应用程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            //应用程序异常
            Current.DispatcherUnhandledException += (s, e) =>
            {
                Logs.LogError(e.Exception);
                MessageBox.Show(e.Exception.ToString(), "应用程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            //多线程异常
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logs.LogError(e.Exception);
                MessageBox.Show(e.Exception.ToString(), "应用程序异常", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            //加载一些库被Modules中使用
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                var baseDir = AppContext.BaseDirectory;
                var dll = Path.Combine(baseDir, $"{name.Name}.dll");
                if (File.Exists(dll))
                    return context.LoadFromAssemblyPath(dll);

                var moduleDll = Path.Combine(baseDir, "Modules", $"{name.Name}.dll");
                if (File.Exists(moduleDll))
                    return context.LoadFromAssemblyPath(moduleDll);

                return null;
            };

        }
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logs.LogError($"DispatcherUnhandledException{e.Exception}");
            e.Handled = false;
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!EnsureSingleInstance())
            {
                MessageBox.Show(
            "软件已启动，请勿重复打开。",
            "提示",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
                // ⚠️ 关键：直接 Environment.Exit
                Environment.Exit(0);
                Shutdown();
                return;
            }
            base.OnStartup(e);

            #region 控制台
            // 分配控制台
            if (!ConsoleHelper.AllocConsole())
            {
                MessageBox.Show("控制台分配失败，但应用程序将继续运行", "警告",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                // 设置控制台编码为UTF-8，解决中文乱码问题
                ConsoleHelper.SetConsoleOutputCP(65001); // UTF-8代码页
                ConsoleHelper.SetConsoleCP(65001);

                // 设置包含中文的控制台标题
                ConsoleHelper.SetConsoleTitleWithChinese($"Console输出");

                // 重定向标准输出，确保Console.WriteLine能正常工作
                ConsoleHelper.RedirectConsoleOutput();

                // 获取并记录控制台标题
                ConsoleHelper._consoleTitle = $"Console输出";
                ConsoleHelper.LogToConsole($"控制台标题: {ConsoleHelper._consoleTitle}");

                //延时一小会，等待控制台被打开
                Thread.Sleep(100);
                // 查找控制台窗口句柄（使用获取到的实际标题）
                ConsoleHelper._consoleHandle = ConsoleHelper.FindWindow(null, ConsoleHelper._consoleTitle);

                // 默认隐藏控制台
                if (ConsoleHelper._consoleHandle != IntPtr.Zero)
                {
                    ConsoleHelper.ShowWindow(ConsoleHelper._consoleHandle, 0); // 0 = SW_HIDE
                }
            }

            // 测试日志输出（包含中文）
            ConsoleHelper.LogToConsole($"[{DateTime.Now:HH:mm:ss}] 应用程序启动完成，中文测试正常");
            #endregion
            FileHelper.CreateHiddenFolderInProgramData();
            Logs.LogInfo("启动应用程序");
        }

        private bool EnsureSingleInstance()
        {
            bool createdNew;
            _mutex = new Mutex(true, "ReeYin_V_SingleInstance", out createdNew);

            if (!createdNew)
            {
                ActivateExistingInstance();
                return false;
            }

            return true;
        }

        private void ActivateExistingInstance()
        {
            try
            {
                var current = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(current.ProcessName);

                foreach (var p in processes)
                {
                    if (p.Id != current.Id)
                    {
                        IntPtr hWnd = p.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            ShowWindow(hWnd, 9); // SW_RESTORE
                            SetForegroundWindow(hWnd);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.LogError(ex);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 关闭所有硬件
            CloseAllHardware();
            //正常退出删除配方.running，不然正常退出日志仍提示异常退出
            PrismProvider.ProjectManager?.MarkApplicationClosed();

            base.OnExit(e);
            Logs.LogInfo("关闭应用程序");


            //最后 释放控制台资源
            ConsoleHelper.FreeConsole();
        }

        /// <summary>
        /// 关闭所有硬件
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void CloseAllHardware()
        {
            if (PrismProvider.EventAggregator == null)
                return;
            PrismProvider.EventAggregator.GetEvent<CloseAllHardwareEvent>().Publish();
        }

        protected override Window CreateShell()
        {
            return new MainWindow();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IDialogWindow, SingleInstanceDialogWindowView>(nameof(SingleInstanceDialogWindowView));
            containerRegistry.Register<IDialogWindow, DialogWindowView>(nameof(DialogWindowView));
            containerRegistry.Register<IDialogWindow, NonTitleDialogWindowView>(nameof(NonTitleDialogWindowView));

            // 注册扩展的 ModuleManager
            containerRegistry.RegisterSingleton<IModuleManager, CategoryModuleManager>();
            // 注册带条件判断的模块初始化器
            containerRegistry.RegisterSingleton<IModuleInitializer, ConditionalModuleInitializer>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            var temp = moduleCatalog.Modules;
            //最优先加载的模块
            moduleCatalog.AddModule<CoreModule>();
            moduleCatalog.AddModule<ShareModule>();
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new DirectoryModuleCatalog() { ModulePath = ModuleNames.ModulePath };//配置本地模块目录
        }

    }

    public class EventSubscriptionContainerExtension : IContainerExtension
    {
        private readonly IContainerExtension _inner;
        private readonly IEventAggregator _eventAggregator;

        public IScopedProvider CurrentScope => throw new NotImplementedException();

        public EventSubscriptionContainerExtension(IContainerExtension inner, IEventAggregator eventAggregator)
        {
            _inner = inner;
            _eventAggregator = eventAggregator;
        }

        //public void FinalizeExtension() => _inner.FinalizeExtension();

        public object Resolve(Type type) => WrapAndSubscribe(_inner.Resolve(type));

        public object Resolve(Type type, string name) => WrapAndSubscribe(_inner.Resolve(type, name));

        public IContainerRegistry RegisterInstance(Type type, object instance)
        {
            _inner.RegisterInstance(type, instance);
            return this;
        }

        public IContainerRegistry RegisterSingleton(Type from, Type to)
        {
            _inner.RegisterSingleton(from, to);
            return this;
        }

        public IContainerRegistry Register(Type from, Type to)
        {
            _inner.Register(from, to);
            return this;
        }

        //public IContainerProvider Container => _inner.Container;

        private object WrapAndSubscribe(object instance)
        {
            EventSubscriptionHelper.AutoSubscribe(instance, _eventAggregator);
            return instance;
        }

        public object Resolve(Type type, params (Type Type, object Instance)[] parameters)
        {
            throw new NotImplementedException();
        }

        public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters)
        {
            throw new NotImplementedException();
        }

        public IScopedProvider CreateScope()
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterInstance(Type type, object instance, string name)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterSingleton(Type from, Type to, string name)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterSingleton(Type type, Func<object> factoryMethod)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterSingleton(Type type, Func<IContainerProvider, object> factoryMethod)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterManySingleton(Type type, params Type[] serviceTypes)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry Register(Type from, Type to, string name)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry Register(Type type, Func<object> factoryMethod)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry Register(Type type, Func<IContainerProvider, object> factoryMethod)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterMany(Type type, params Type[] serviceTypes)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterScoped(Type from, Type to)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterScoped(Type type, Func<object> factoryMethod)
        {
            throw new NotImplementedException();
        }

        public IContainerRegistry RegisterScoped(Type type, Func<IContainerProvider, object> factoryMethod)
        {
            throw new NotImplementedException();
        }

        public bool IsRegistered(Type type)
        {
            throw new NotImplementedException();
        }

        public bool IsRegistered(Type type, string name)
        {
            throw new NotImplementedException();
        }
    }
}
