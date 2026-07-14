using Newtonsoft.Json;
using ReeYin_V.Core.Helper;
using System;
using System.IO;

namespace Nodify.FlowApp
{
    public static class LegacyProjectImporter
    {
        public static bool TryLoad(string filePath, out AppViewModel? appViewModel, out string message)
        {
            appViewModel = null;
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                message = "旧工程文件路径不能为空";
                return false;
            }

            if (!File.Exists(filePath))
            {
                message = "旧工程文件不存在";
                return false;
            }

            try
            {
                appViewModel = JsonHelper.JsonDisObjectSerialize<AppViewModel>(
                    filePath,
                    out _,
                    TypeNameHandling.Auto);

                if (appViewModel == null)
                {
                    message = "旧工程文件内容无效，无法导入";
                    return false;
                }

                appViewModel.GraphViewModel ??= new NodifyEditorViewModel();
                appViewModel.guid = appViewModel.guid == Guid.Empty ? Guid.NewGuid() : appViewModel.guid;
                return true;
            }
            catch (Exception ex)
            {
                message = $"导入旧工程失败: {ex.Message}";
                return false;
            }
        }
    }
}
