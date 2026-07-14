using ReeYin_V.Core.Config;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Module
{
    public interface IHardwareModuleManager
    {
        Dictionary<ConfigKey, IHardwareModule> Modules { set; get; }

        ObservableCollection<HardwareConfigItem> ConfigItems { get; set; }

        void Initialize();

        void Shutdown();
        void RefreshStatus();

    }

    public interface IHardwareModule
    {
        InitResult Init();

        void Shutdown();

        void RefreshStatus();
    }

    public interface IStartupResetHardwareModule
    {
        Task<InitResult> ExecuteStartupResetAsync(Action<string> updateMessage);
    }
}
