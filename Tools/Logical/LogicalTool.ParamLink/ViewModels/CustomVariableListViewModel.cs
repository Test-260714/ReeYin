using LogicalTool.ParamLink.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace LogicalTool.ParamLink.ViewModels
{
    [Serializable]
    public class CustomVariableListViewModel : DialogViewModelBase, IViewModuleParam
    {
        #region Properties

        [JsonIgnore]
        private TransmitParam _sltCustomNodeInput;
        public TransmitParam SltCustomNodeInput
        {
            get { return _sltCustomNodeInput; }
            set { _sltCustomNodeInput = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _sltGlobalNodeInput;
        public TransmitParam SltGlobalNodeInput
        {
            get { return _sltGlobalNodeInput; }
            set { _sltGlobalNodeInput = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private CustomVariableListModel _modelParam = new CustomVariableListModel();
        public CustomVariableListModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #region 配方相关属性

        [JsonIgnore]
        private ObservableCollection<Recipe> _recipes = new ObservableCollection<Recipe>();
        /// <summary>
        /// 配方列表
        /// </summary>
        public ObservableCollection<Recipe> Recipes
        {
            get { return _recipes; }
            set { _recipes = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Recipe _sltRecipe;
        /// <summary>
        /// 选中的配方
        /// </summary>
        public Recipe SltRecipe
        {
            get { return _sltRecipe; }
            set { _sltRecipe = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TransmitParam _sltRecipeParam;
        /// <summary>
        /// 选中的配方参数
        /// </summary>
        public TransmitParam SltRecipeParam
        {
            get { return _sltRecipeParam; }
            set { _sltRecipeParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #endregion

        #region Constructor

        public CustomVariableListViewModel()
        {
        }

        #endregion

        #region Methods

        public override void InitParam()
        {
            LoadRecipes();
        }

        /// <summary>
        /// 加载配方列表
        /// </summary>
        private void LoadRecipes()
        {
            try
            {
                var recipePath = GetRecipePath();
                if (File.Exists(recipePath))
                {
                    var json = File.ReadAllText(recipePath);
                    var recipes = JsonConvert.DeserializeObject<ObservableCollection<Recipe>>(json);
                    if (recipes != null)
                    {
                        Recipes = recipes;
                        if (Recipes.Count > 0)
                            SltRecipe = Recipes[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 保存配方列表
        /// </summary>
        private void SaveRecipes()
        {
            try
            {
                var recipePath = GetRecipePath();
                var dir = Path.GetDirectoryName(recipePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonConvert.SerializeObject(Recipes, Formatting.Indented);
                File.WriteAllText(recipePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配方失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取配方文件路径
        /// </summary>
        private string GetRecipePath()
        {
            return "";
            //var projectPath = PrismProvider.ProjectManager.SltCurSolutionItem?.ProjectPath ?? "";
            //if (string.IsNullOrEmpty(projectPath))
            //    projectPath = AppDomain.CurrentDomain.BaseDirectory;
            //return Path.Combine(projectPath, "Recipes", "recipes.json");
        }

        /// <summary>
        /// 添加新配方
        /// </summary>
        private void AddRecipe()
        {
            var recipe = new Recipe
            {
                Name = $"配方_{Recipes.Count + 1}",
                CreateTime = DateTime.Now
            };
            Recipes.Add(recipe);
            SltRecipe = recipe;
        }

        /// <summary>
        /// 复制配方
        /// </summary>
        private void CopyRecipe()
        {
            if (SltRecipe == null) return;
            var clone = SltRecipe.Clone();
            Recipes.Add(clone);
            SltRecipe = clone;
        }

        /// <summary>
        /// 删除配方
        /// </summary>
        private void DeleteRecipe()
        {
            if (SltRecipe == null) return;
            var index = Recipes.IndexOf(SltRecipe);
            Recipes.Remove(SltRecipe);
            if (Recipes.Count > 0)
                SltRecipe = Recipes[Math.Min(index, Recipes.Count - 1)];
            else
                SltRecipe = null;
        }

        /// <summary>
        /// 添加配方参数
        /// </summary>
        private void AddRecipeParam()
        {
            if (SltRecipe == null) return;
            SltRecipe.Params.Add(new TransmitParam
            {
                Name = "新参数",
                Type = DataType.String,
                Value = ""
            });
        }

        /// <summary>
        /// 删除配方参数
        /// </summary>
        private void DeleteRecipeParam()
        {
            if (SltRecipe == null || SltRecipeParam == null) return;
            SltRecipe.Params.Remove(SltRecipeParam);
            SltRecipeParam = null;
        }

        /// <summary>
        /// 应用配方到自定义全局变量
        /// </summary>
        private void ApplyRecipe()
        {
            if (SltRecipe == null) return;

            foreach (var param in SltRecipe.Params)
            {
                var existing = ModelParam.CustomGlobalParams.FirstOrDefault(p => p.Name == param.Name);
                if (existing != null)
                {
                    existing.Value = param.Value;
                    existing.Type = param.Type;
                }
                else
                {
                    ModelParam.CustomGlobalParams.Add(new TransmitParam
                    {
                        Name = param.Name,
                        Type = param.Type,
                        Value = param.Value,
                        Describe = param.Describe,
                        Resourece = ResoureceType.Global
                    });
                }
            }
        }

        /// <summary>
        /// 导出配方
        /// </summary>
        private void ExportRecipe()
        {
            if (SltRecipe == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "JSON文件|*.json",
                FileName = SltRecipe.Name
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(SltRecipe, Formatting.Indented);
                    File.WriteAllText(dialog.FileName, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"导出配方失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 导入配方
        /// </summary>
        private void ImportRecipe()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON文件|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var recipe = JsonConvert.DeserializeObject<Recipe>(json);
                    if (recipe != null)
                    {
                        recipe.Id = Guid.NewGuid();
                        recipe.CreateTime = DateTime.Now;
                        Recipes.Add(recipe);
                        SltRecipe = recipe;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"导入配方失败：{ex.Message}");
                }
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>((obj) =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    ModelParam.CustomGlobalParams.Add(new TransmitParam
                    {
                        Name = "",
                        Resourece = ResoureceType.Global,
                        Type = DataType.None,
                        Value = "",
                        Describe = "",
                    });
                    break;
                case "Delete":
                    if (SltCustomNodeInput != null)
                        ModelParam.CustomGlobalParams.Remove(SltCustomNodeInput);
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            InitParam();
        });

        public DelegateCommand TypeChanged => new DelegateCommand(() =>
        {
            if (SltCustomNodeInput == null) return;

            SltCustomNodeInput.Value = SltCustomNodeInput.Type switch
            {
                DataType.Int      => (object)0,
                DataType.Double   => (object)0.0,
                DataType.Bool     => (object)false,
                DataType.String   => (object)string.Empty,
                DataType.Datetime => (object)DateTime.Now,
                DataType.Enum     => (object)0,
                DataType.List     => (object)new System.Collections.Generic.List<object>(),
                DataType.Dict     => (object)new System.Collections.Generic.Dictionary<string, object>(),
                DataType.Array    => (object)Array.Empty<object>(),
                DataType.HObject  => (object)new HalconDotNet.HObject(),
                _                 => null,   // Object / Mat / None
            };
        });

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "DoubleClickCustomNode":
                    if (SltCustomNodeInput != null)
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", SltCustomNodeInput },
                        });
                    }
                    break;
                case "DoubleClickGlobalNode":
                    if (SltGlobalNodeInput != null)
                    {
                        CloseDialog(ButtonResult.OK, new DialogParameters()
                        {
                            { "Param", SltGlobalNodeInput },
                        });
                    }
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "确认":
                    PrismProvider.ProjectManager.SltCurSolutionItem.CustomGlobalParams = ModelParam.CustomGlobalParams;
                    CloseDialog(ButtonResult.OK, new DialogParameters()
                    {
                        { "Param", ModelParam },
                    });
                    break;
            }
        });

        public DelegateCommand<string> RecipeCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "Add":
                    AddRecipe();
                    break;
                case "Copy":
                    CopyRecipe();
                    break;
                case "Delete":
                    DeleteRecipe();
                    break;
                case "AddParam":
                    AddRecipeParam();
                    break;
                case "DeleteParam":
                    DeleteRecipeParam();
                    break;
                case "Apply":
                    ApplyRecipe();
                    break;
                case "Save":
                    if (SltRecipe != null)
                        SltRecipe.UpdateTime = DateTime.Now;
                    SaveRecipes();
                    break;
                case "Export":
                    ExportRecipe();
                    break;
                case "Import":
                    ImportRecipe();
                    break;
            }
        });

        #endregion
    }
}
