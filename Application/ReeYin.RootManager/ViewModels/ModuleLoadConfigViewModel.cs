using ReeYin.RootManager.Models;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Models.Database.Tables;
using ReeYin_V.Logger;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin.RootManager.ViewModels
{
    /// <summary>
    /// 模块加载配置管理 ViewModel
    /// </summary>
    public class ModuleLoadConfigViewModel : BindableBase
    {
        private readonly ISqlSugarClient _db;

        #region Properties

        private ObservableCollection<ModuleLoadConfigEditModel> _configs = new();
        public ObservableCollection<ModuleLoadConfigEditModel> Configs
        {
            get => _configs;
            set => SetProperty(ref _configs, value);
        }

        private ModuleLoadConfigEditModel _selectedConfig;
        public ModuleLoadConfigEditModel SelectedConfig
        {
            get => _selectedConfig;
            set => SetProperty(ref _selectedConfig, value);
        }

        private ObservableCollection<RuleTypeItem> _ruleTypes;
        public ObservableCollection<RuleTypeItem> RuleTypes
        {
            get => _ruleTypes;
            set => SetProperty(ref _ruleTypes, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterConfigs();
            }
        }

        private ObservableCollection<ModuleLoadConfigEditModel> _allConfigs = new();

        #endregion

        #region Constructor

        public ModuleLoadConfigViewModel(ISqlSugarClient db)
        {
            _db = db;
            RuleTypes = RuleTypeItem.GetAllItems();
            LoadConfigs();
        }

        #endregion

        #region Methods

        /// <summary>
        /// 加载所有配置
        /// </summary>
        private void LoadConfigs()
        {
            try
            {
                var entities = _db.Queryable<ModuleLoadConfig>()
                    .OrderBy(c => c.Sort)
                    .ToList();

                _allConfigs.Clear();
                Configs.Clear();

                foreach (var entity in entities)
                {
                    var model = ModuleLoadConfigEditModel.FromEntity(entity);
                    _allConfigs.Add(model);
                    Configs.Add(model);
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"加载模块配置失败: {ex.Message}");
                MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 过滤配置
        /// </summary>
        private void FilterConfigs()
        {
            Configs.Clear();
            var filtered = string.IsNullOrEmpty(SearchText)
                ? _allConfigs
                : _allConfigs.Where(c =>
                    (c.ModuleName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.DisplayName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

            foreach (var item in filtered)
            {
                Configs.Add(item);
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void SaveConfig(ModuleLoadConfigEditModel model)
        {
            if (model == null) return;

            try
            {
                var entity = model.ToEntity();

                if (entity.ConfigId == 0)
                {
                    // 新增
                    entity.CreateTime = DateTime.Now;
                    entity.UpdateTime = DateTime.Now;
                    entity.UpdateBy = 1;
                    entity.CreateBy = 1;
                    var id = _db.Insertable(entity).ExecuteReturnIdentity();
                    model.ConfigId = id;
                }
                else
                {
                    // 更新
                    entity.UpdateTime = DateTime.Now;
                    entity.UpdateBy = 1;
                    entity.CreateBy = 1;
                    _db.Updateable(entity).ExecuteCommand();
                }

                // 清除评估器缓存
                DatabaseModuleConditionEvaluator.ClearCache();

                MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logs.LogError($"保存模块配置失败: {ex.Message}");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        private void DeleteConfig(ModuleLoadConfigEditModel model)
        {
            if (model == null) return;

            var result = MessageBox.Show($"确定要删除模块 '{model.ModuleName}' 的配置吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (model.ConfigId > 0)
                {
                    _db.Deleteable<ModuleLoadConfig>().Where(c => c.ConfigId == model.ConfigId).ExecuteCommand();
                }

                _allConfigs.Remove(model);
                Configs.Remove(model);

                // 清除评估器缓存
                DatabaseModuleConditionEvaluator.ClearCache();

                MessageBox.Show("删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logs.LogError($"删除模块配置失败: {ex.Message}");
                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 新增配置
        /// </summary>
        private void AddConfig()
        {
            var newConfig = new ModuleLoadConfigEditModel
            {
                ModuleName = "NewModule",
                DisplayName = "新模块",
                RuleType = ModuleLoadRuleType.Always,
                IsEnabled = true,
                MutualExclusivePriority = 100,
                Sort = _allConfigs.Count
            };

            _allConfigs.Add(newConfig);
            Configs.Add(newConfig);
            SelectedConfig = newConfig;
        }

        /// <summary>
        /// 刷新配置
        /// </summary>
        private void RefreshConfigs()
        {
            LoadConfigs();
            DatabaseModuleConditionEvaluator.ClearCache();
            MessageBox.Show("刷新成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 保存所有配置
        /// </summary>
        private void SaveAllConfigs()
        {
            try
            {
                foreach (var model in _allConfigs)
                {
                    var entity = model.ToEntity();
                    entity.UpdateTime = DateTime.Now;

                    if (entity.ConfigId == 0)
                    {
                        entity.CreateTime = DateTime.Now;
                        var id = _db.Insertable(entity).ExecuteReturnIdentity();
                        model.ConfigId = id;
                    }
                    else
                    {
                        _db.Updateable(entity).ExecuteCommand();
                    }
                }

                DatabaseModuleConditionEvaluator.ClearCache();
                MessageBox.Show("全部保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logs.LogError($"保存模块配置失败: {ex.Message}");
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Commands

        private DelegateCommand? _addCommand;
        public DelegateCommand AddCommand => _addCommand ??= new DelegateCommand(AddConfig);

        private DelegateCommand<ModuleLoadConfigEditModel>? _saveCommand;
        public DelegateCommand<ModuleLoadConfigEditModel> SaveCommand => _saveCommand ??= new DelegateCommand<ModuleLoadConfigEditModel>(SaveConfig);

        private DelegateCommand<ModuleLoadConfigEditModel>? _deleteCommand;
        public DelegateCommand<ModuleLoadConfigEditModel> DeleteCommand => _deleteCommand ??= new DelegateCommand<ModuleLoadConfigEditModel>(DeleteConfig);

        private DelegateCommand? _refreshCommand;
        public DelegateCommand RefreshCommand => _refreshCommand ??= new DelegateCommand(RefreshConfigs);

        private DelegateCommand? _saveAllCommand;
        public DelegateCommand SaveAllCommand => _saveAllCommand ??= new DelegateCommand(SaveAllConfigs);

        #endregion
    }
}
