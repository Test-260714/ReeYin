using HalconDotNet;
using ImageTool.Halcon.Helper;
using OpenCvSharp.LineDescriptor;
using ReeYin.CSharpScript.Models;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.CSharpScript
{
    public abstract class CSharpScriptBase : ModelParamBase
    {
        #region Properties
        public string CSharpScriptName = "";
        public object _lockobj = new object();
        private static readonly object NodeOutputCacheLock = new object();
        #endregion

        #region Methods
        /// <summary>
        /// 执行模块
        /// </summary>
        /// <returns></returns>
        public abstract bool ExeModule(string CSharpScriptName);

        /// <summary>
        /// 更新Output参数
        /// </summary>
        /// <returns></returns>
        public bool UploadOutput(string OutputName,object value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CSharpScriptName) || string.IsNullOrWhiteSpace(OutputName))
                {
                    Logs.LogError("脚本输出失败：脚本名或输出名为空。");
                    return false;
                }

                var solutionItem = PrismProvider.ProjectManager?.SltCurSolutionItem;
                var outputCache = solutionItem?.NodesOutputCache;
                if (outputCache == null)
                {
                    Logs.LogError("脚本输出失败：输出缓存不存在。");
                    return false;
                }

                lock (NodeOutputCacheLock)
                {
                    if (!outputCache.ContainsKey(CSharpScriptName))
                    {
                        outputCache.Add(CSharpScriptName, new ObservableCollection<TransmitParam>
                        {
                            new TransmitParam
                            {
                                Name = OutputName,
                                Value = value
                            }
                        });
                    }
                    else
                    {
                        var existing = outputCache[CSharpScriptName].FirstOrDefault(p => p.Name == OutputName);

                        if (existing != null)
                        {
                            existing.Value = value;
                        }
                        else
                        {
                            outputCache[CSharpScriptName].Add(new TransmitParam
                            {
                                Name = OutputName,
                                Value = value
                            });
                        }
                    }
                    return true;
                }

            }
            catch (Exception ex)
            {
                Logs.LogError(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 获取上一节点输入的当前值
        /// </summary>
        /// <param name="SltParam"></param>
        /// <returns></returns>
        public object GetCurLastInputValue(string Name)
        {
            try
            {
                lock (_lockobj)
                {
                    var caches = PrismProvider.ProjectManager?.SltCurSolutionItem?.NodeParamCaches;
                    if (caches == null || !caches.TryGetValue(CSharpScriptName, out var cacheValue))
                    {
                        Logs.LogError($"获取当前值失败：未找到脚本节点 {CSharpScriptName} 的缓存。");
                        return null;
                    }

                    var modelParam = cacheValue as CSharpScriptModel;
                    if (modelParam == null)
                    {
                        Logs.LogError($"获取当前值失败：节点 {CSharpScriptName} 缓存不是 CSharpScriptModel。");
                        return null;
                    }

                    object output;
                    var Param = modelParam.InputParams.Where(p => p.Name == Name).FirstOrDefault();
                    if (Param == null) return null;
                    if (Param.Value is HObject)
                    {
                        output = (Param.Value as HObject).Clone();
                    }
                    else
                    {
                        output = Param.Value;
                    }
                    if (output == null)
                    {
                        Logs.LogError($"获取当前值失败");
                        return null;
                    }
                    return output;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取当前值失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// 获取当前全局输入的当前值
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public object GetGlobalInputValue(string Name)
        {
            try
            {
                lock (_lockobj)
                {
                    object output;
                    var globalParams = PrismProvider.ProjectManager?.SltCurSolutionItem?.GlobalParams;
                    if (globalParams == null)
                    {
                        Logs.LogError("获取当前值失败：全局参数集合不存在。");
                        return null;
                    }

                    var Param = globalParams.Where(p => p.Name == Name).FirstOrDefault();
                    if (Param == null) return null;
                    if (Param.Value is HObject)
                    {
                        output = (Param.Value as HObject).Clone();
                    }
                    else
                    {
                        output = Param.Value;
                    }
                    if (output == null)
                    {
                        Logs.LogError($"获取当前值失败");
                        return null;
                    }
                    return output;
                }

            }
            catch (Exception ex)
            {
                Logs.LogError($"获取当前值失败: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 输入自定义全局输入的当前值
        /// </summary>
        /// <param name="SltParam"></param>
        /// <returns></returns>
        public object GetCurCustomInputValue(string Name)
        {
            try
            {
                lock (_lockobj)
                {
                    var customGlobalParams = PrismProvider.ProjectManager?.SltCurSolutionItem?.CustomGlobalParams;
                    if (customGlobalParams == null)
                    {
                        Logs.LogError("获取当前自定义值失败：自定义全局参数集合不存在。");
                        return null;
                    }

                    var Param = customGlobalParams.Where(p => p.Name == Name).FirstOrDefault();
                    if (Param == null)
                    {
                        Logs.LogError($"获取当前自定义值失败");
                        return null;
                    }
                    return Param.Value;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取当前值失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// 设置自定义全局输入的当前值
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public bool SetCurCustomInputValue(string Name, object value)
        {
            try
            {
                lock (_lockobj)
                {
                    var customGlobalParams = PrismProvider.ProjectManager?.SltCurSolutionItem?.CustomGlobalParams;
                    if (customGlobalParams == null)
                    {
                        Logs.LogError("设置当前自定义值失败：自定义全局参数集合不存在。");
                        return false;
                    }

                    var Param = customGlobalParams.Where(p => p.Name == Name).FirstOrDefault();
                    if (Param == null)
                    {
                        Logs.LogError($"{CSharpScriptName}，设置当前自定义值失败！");
                        return false;
                    }

                    Param.Value = value;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"设置当前值失败: {ex.Message}");
                return false;
            }
        }
        #endregion

    }
}
