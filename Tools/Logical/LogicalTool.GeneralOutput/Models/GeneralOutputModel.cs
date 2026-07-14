using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicalTool.GeneralOutput.Models
{
    [Serializable]
    public class GeneralOutputModel : BindableBase
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public int Serial { get; set; }

        [JsonIgnore]
        public ModuleParam moduleInputParam { get; set; } = new ModuleParam();

        public ModuleParam moduleOutputParam { get; set; } = new ModuleParam();

        [JsonIgnore]
        public Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private ExecuteModuleOutput _output;
        [JsonIgnore]
        public ExecuteModuleOutput Output
        {
            get { return _output; }
            set { _output = value; RaisePropertyChanged(); }
        }

        public List<(int, NodeStatus)> InputNodeStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task<ExecuteModuleOutput> ExecuteModule()
        {
            throw new NotImplementedException();
        }

        public void DrogInit()
        {
            throw new NotImplementedException();
        }
    }
}
