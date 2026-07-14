using ALGO.ImagePerProcessing.Models;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Dialogs;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using DataType = ReeYin_V.Core.Services.Project.DataType;

namespace ALGO.ImagePerProcessing.ViewModels
{
    [Serializable]
    public class ImagePerProcessingViewModel : DialogViewModelBase, IViewModuleParam
    {
        private string _sltOutputParamName;

        public string SltOutputParamName
        {
            get => _sltOutputParamName;
            set => SetProperty(ref _sltOutputParamName, value);
        }

        public new ImagePerProcessingModel ModelParam
        {
            get => base.ModelParam as ImagePerProcessingModel;
            set
            {
                base.ModelParam = value;
                RaisePropertyChanged();
            }
        }

        private TransmitParam _currentOutputParam;

        public TransmitParam CurrentOutputParam
        {
            get => _currentOutputParam;
            set => SetProperty(ref _currentOutputParam, value);
        }

        public Array CreateROITypes { get; set; } = Enum.GetValues(typeof(eCreateRoiType));

        public override void InitParam()
        {
            ModelParam = InitModelParam<ImagePerProcessingModel>();
        }

        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>(async order =>
        {
            string[] commandLine = order?.Split('_') ?? Array.Empty<string>();
            if (commandLine.Length == 2)
            {
                if (commandLine[0] != "add")
                {
                    return;
                }

                ModelParam.ModelToolList.Add(new ModelData
                {
                    m_name = (eOperatorType)Enum.Parse(typeof(eOperatorType), commandLine[1])
                });
                ModelParam.SelectedModel = ModelParam.ModelToolList[^1];
                return;
            }

            switch (order)
            {
                case "remove":
                    RemoveSelectedOperator();
                    break;
                case "up":
                    MoveSelectedOperator(-1);
                    break;
                case "down":
                    MoveSelectedOperator(1);
                    break;
                case "取消":
                    CloseDialog(ButtonResult.No);
                    break;
                case "执行":
                    if (ModelParam.LoadKeyParam())
                    {
                        await ModelParam.ExecuteModule();
                    }
                    break;
                case "确认":
                    ConfirmAndClose();
                    break;
            }
        });

        public DelegateCommand LoadCommand => new DelegateCommand(() =>
        {
            if (ModelParam.LoadKeyParam())
            {
                ModelParam.RefreshPreviewDisplay();
            }

            if (Visibility == Visibility.Hidden)
            {
                CloseDialog(ButtonResult.OK, new DialogParameters
                {
                    { "Param", ModelParam },
                });
            }
        });

        public DelegateCommand<object> DataOperateCommand => new DelegateCommand<object>(obj =>
        {
            switch (obj?.ToString())
            {
                case "Add":
                    AddOutputParam();
                    break;
                case "Delete":
                    DeleteOutputParam();
                    break;
            }
        });

        private void RemoveSelectedOperator()
        {
            if (ModelParam.SelectedModel == null)
            {
                return;
            }

            int selectedIndex = ModelParam.ModelToolList.IndexOf(ModelParam.SelectedModel);
            if (selectedIndex < 0)
            {
                return;
            }

            ModelParam.ModelToolList.RemoveAt(selectedIndex);
            if (ModelParam.ModelToolList.Count == 0)
            {
                ModelParam.SelectedModel = new ModelData();
                return;
            }

            int nextIndex = Math.Max(0, selectedIndex - 1);
            ModelParam.SelectedModel = ModelParam.ModelToolList[nextIndex];
        }

        private void MoveSelectedOperator(int direction)
        {
            if (ModelParam.SelectedModel == null)
            {
                return;
            }

            int index = ModelParam.ModelToolList.IndexOf(ModelParam.SelectedModel);
            int targetIndex = index + direction;
            if (index < 0 || targetIndex < 0 || targetIndex >= ModelParam.ModelToolList.Count)
            {
                return;
            }

            ModelParam.ModelToolList.Move(index, targetIndex);
            ModelParam.SelectedModel = ModelParam.ModelToolList[targetIndex];
        }

        private void ConfirmAndClose()
        {
            ModelParam.LoadKeyParam();
            foreach (var outputParam in ModelParam.OutputParams.Where(item => item.IsGlobal))
            {
                if (!PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Any(gp => gp.Guid == outputParam.Guid))
                {
                    PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Add(outputParam);
                }
            }

            ModelParam.moduleOutputParam.TransmitParams = ModelParam.OutputParams.ToDictionary(
                item => item.Guid.ToString(),
                item => (object)item);

            CloseDialog(ButtonResult.OK, new DialogParameters
            {
                { "Param", ModelParam },
            });
        }

        private void AddOutputParam()
        {
            if (string.IsNullOrWhiteSpace(SltOutputParamName))
            {
                return;
            }

            if (ModelParam.OutputParamResource == null
                || !ModelParam.OutputParamResource.TryGetValue(SltOutputParamName, out object resource)
                || resource is not TransmitParam selectedParam)
            {
                return;
            }

            if (ModelParam.OutputParams.Any(item => item.Name == SltOutputParamName))
            {
                MessageBox.Show("已包含重名参数，请重新输入！");
                return;
            }

            object value = null;
            if (selectedParam.Resourece == ResoureceType.None)
            {
                var values = OutputParamCollector.GetDataPointValues(ModelParam);
                values.TryGetValue(selectedParam.Name, out value);
            }
            else if (selectedParam.Resourece == ResoureceType.Inupt)
            {
                value = ModelParam.InputParams.FirstOrDefault(item => item.Name == selectedParam.Name)?.Value;
            }

            ModelParam.OutputParams.Add(new TransmitParam
            {
                LinkGuid = Guid,
                ParamName = selectedParam.Name,
                Serial = selectedParam.Serial == -999 ? ModelParam.Serial : selectedParam.Serial,
                Name = SltOutputParamName,
                Type = DataType._object,
                Value = value,
                ResourcePath = selectedParam.ResourcePath,
                Describe = selectedParam.Describe
            });
        }

        private void DeleteOutputParam()
        {
            if (CurrentOutputParam == null)
            {
                return;
            }

            ModelParam.OutputParams.Remove(CurrentOutputParam);
            PrismProvider.ProjectManager.SltCurSolutionItem.GlobalParams.Remove(CurrentOutputParam);
            CurrentOutputParam = null;
        }
    }

    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }

            return value.ToString().Equals(parameter.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is true && parameter != null)
            {
                return Enum.Parse(targetType, parameter.ToString());
            }

            return Binding.DoNothing;
        }
    }

    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return Visibility.Collapsed;
            }

            string enumDescription = GetEnumDescription((Enum)value);
            return enumDescription == parameter.ToString()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetEnumDescription(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                  .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? enumValue.ToString();
        }
    }
}
