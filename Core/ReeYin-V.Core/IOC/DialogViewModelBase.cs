using ImageTool.Halcon;
using Newtonsoft.Json;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.Core.IOC
{
    public class DialogViewModelBase : BindableBase, IDialogAware
    {
        #region Fields
        [JsonIgnore]
        public DialogCloseListener RequestClose { get; }


        #endregion
        public DialogViewModelBase()
        {

        }

        #region Prop
        [JsonIgnore]
        private string _name;
        [JsonIgnore]
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _serial = -999;
        [JsonIgnore]
        public int Serial
        {
            get { return _serial; }
            set { _serial = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _title;
        [JsonIgnore]
        public string Title
        {
            get { return _title; }
            set { _title = value; RaisePropertyChanged(); }
        }
        
        [JsonIgnore]
        private Visibility _visibility;
        [JsonIgnore]
        public Visibility Visibility
        {
            get { return _visibility; }
            set { _visibility = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _icon;
        [JsonIgnore]
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private object _param;
        [JsonIgnore]
        public object Param
        {
            get { return _param; }
            set { _param = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Guid _guid;
        [JsonIgnore]
        public Guid Guid
        {
            get { return _guid; }
            set { _guid = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ModelParamBase _modelParam;

        public ModelParamBase ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion



        /// <summary>
        /// 关闭对话框前，判断是否可以关闭
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual bool CanCloseDialog()
        {

            return true;
        }

        /// <summary>
        /// 关闭时执行的方法
        /// </summary>
        public virtual void OnDialogClosed()
        {

        }

        /// <summary>
        /// 打开时执行的方法
        /// </summary>
        /// <param name="parameters"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            Guid = parameters.GetValue<Guid>("Guid");

            Name = parameters.GetValue<string>("Title");

            Serial = parameters.GetValue<int>("Serial");

            Title = parameters.GetValue<string>("Title");

            Visibility = parameters.GetValue<Visibility>("Visibility");

            Icon = parameters.GetValue<string>("Icon");

            Param = parameters.GetValue<object>("Param");

            if (Icon == null)
            {
                Icon = "\ue640";
            }
            Title = Icon + " " + Title;

            InitParam();
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        public virtual void InitParam()
        {
            if (ModelParam == null)
            {
                return;
            }

            InitializeModelParam(ModelParam);
        }

        protected TModel InitModelParam<TModel>() where TModel : ModelParamBase, new()
        {
            TModel modelParam = ResolveModelParam<TModel>();
            InitializeModelParam(modelParam);
            return modelParam;
        }

        protected virtual TModel ResolveModelParam<TModel>() where TModel : ModelParamBase, new()
        {
            if (Param is TModel existingModelParam)
            {
                return existingModelParam;
            }

            if (TryResolveCachedModelParam(out TModel cachedModelParam))
            {
                Logs.LogInfo(
                    $"打开组件参数恢复：Serial={Serial:D3}, TargetModel={typeof(TModel).FullName}, " +
                    $"ParamType={Param?.GetType().FullName ?? "null"}, Source=NodeParamCaches");
                return cachedModelParam;
            }

            Logs.LogWarning(
                $"打开组件创建新参数：Serial={Serial:D3}, TargetModel={typeof(TModel).FullName}, " +
                $"ParamType={Param?.GetType().FullName ?? "null"}, Reason=未找到可复用缓存或类型不匹配");

            TModel modelParam = new TModel();
            if (Param is IModuleParam moduleParam)
            {
                modelParam.moduleInputParam = moduleParam.moduleInputParam;
                modelParam.moduleOutputParam = moduleParam.moduleOutputParam;
            }

            return modelParam;
        }

        private bool TryResolveCachedModelParam<TModel>(out TModel modelParam) where TModel : ModelParamBase
        {
            modelParam = null;

            if (PrismProvider.ProjectManager == null)
            {
                return false;
            }

            foreach (string cacheKey in GetCandidateNodeParamCacheKeys().Distinct())
            {
                object cachedValue = PrismProvider.ProjectManager.GetNodeParamCacheValue(cacheKey);
                if (cachedValue is TModel cachedModelParam)
                {
                    SyncModuleParamShell(cachedModelParam);
                    Logs.LogInfo(
                        $"命中节点参数缓存：CacheKey={cacheKey}, Serial={Serial:D3}, " +
                        $"ModelType={typeof(TModel).FullName}, CachedType={cachedValue.GetType().FullName}");
                    modelParam = cachedModelParam;
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<string> GetCandidateNodeParamCacheKeys()
        {
            foreach (int serial in GetCandidateSerials().Distinct())
            {
                if (serial < 0)
                {
                    continue;
                }

                yield return serial.ToString("D3");
                yield return serial.ToString();
            }
        }

        private IEnumerable<int> GetCandidateSerials()
        {
            if (Serial >= 0)
            {
                yield return Serial;
            }

            if (Param is IModuleParam moduleParam && moduleParam.Serial >= 0)
            {
                yield return moduleParam.Serial;
            }
        }

        private void SyncModuleParamShell(ModelParamBase modelParam)
        {
            if (modelParam == null || Param is not IModuleParam sourceParam)
            {
                return;
            }

            if (sourceParam.moduleInputParam != null)
            {
                modelParam.moduleInputParam = sourceParam.moduleInputParam;
            }

            if (modelParam.moduleOutputParam == null && sourceParam.moduleOutputParam != null)
            {
                modelParam.moduleOutputParam = sourceParam.moduleOutputParam;
            }
        }

        protected virtual void InitializeModelParam(ModelParamBase modelParam)
        {
            if (modelParam == null)
            {
                return;
            }
            modelParam.Serial = Serial;
            ModelParamCompatibilityDefaults.Normalize(modelParam);
            ModelParam = modelParam;
            modelParam.OnceInit();
            LoadSpecificConfig(modelParam);
            modelParam.InitOutputParamResource(Guid);
            modelParam.TransferParam();
            modelParam.IsDebug = true;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void ReleaseResources(string order)
        {

        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        /// <param name="buttonResult"></param>
        /// <param name="dialogParameters"></param>
        public async virtual void CloseDialog(ButtonResult buttonResult, IDialogParameters dialogParameters = null)
        {

            RequestClose.Invoke(dialogParameters, buttonResult);

        }

        #region Others
        /// <summary>
        /// 加载一些需要特殊控件的方法
        /// </summary>
        public virtual void LoadSpecificConfig(ModelParamBase ModelParam)
        {
            ModelParam.OutputParamResource.Clear();
            if (ModelParam.Serial == -999)
                ModelParam.Serial = Serial;

            ModelParamBase.ModuleName = ModelParam.Serial.ToString("D3");

            if (!PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.ContainsKey(ModelParamBase.ModuleName))
            {
                ModelParam.mWindowH = new VMHWindowControl();
                PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair.Add(ModelParamBase.ModuleName, ModelParam.mWindowH);
            }
            else
            {
                ModelParam.mWindowH = PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair[ModelParamBase.ModuleName] as VMHWindowControl;
            }
        }
        #endregion
    }
}
