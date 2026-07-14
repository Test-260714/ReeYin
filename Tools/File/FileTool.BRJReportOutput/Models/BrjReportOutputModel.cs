using FileTool.BRJReportOutput.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace FileTool.BRJReportOutput.Models
{
    [Serializable]
    public class BrjReportOutputModel : ModelParamBase
    {
        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = null!;

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                {
                    return true;
                }

                if (!base.OnceInit())
                {
                    return false;
                }

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun = () => ExecuteModule().Result;
                }

                BrjReportStorage.EnsureCreated();

                IsOnceInit = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            Output = new ExecuteModuleOutput
            {
                RunStatus = NodeStatus.Success,
            };

            return Task.FromResult(Output);
        }
    }
}
