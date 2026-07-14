using Newtonsoft.Json;
using ReeYin_V.Core.Enums;
using ReeYin_V.NodifyManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Nodify.FlowApp
{
    /// <summary>
    /// 操作菜单
    /// </summary>
    [Serializable]
    public class OperationsMenuViewModel : ObservableObject
    {
        #region Fields
        [JsonIgnore]
        private bool _isVisible;

        [JsonIgnore]
        private Point _location;

        public event Action? Closed;

        private readonly CalculatorViewModel _calculator;
        #endregion

        #region Properties  
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                SetProperty(ref _isVisible, value);
                if (!value)
                {
                    Closed?.Invoke();
                }
            }
        }

        public Point Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public NodifyObservableCollection<OperationInfoViewModel> AvailableOperations { get; }
        #endregion

        #region Commands  
        [JsonIgnore]
        public ICommand CreateOperationCommand { get; }
        #endregion

        #region Constructor
        public OperationsMenuViewModel(CalculatorViewModel calculator)
        {
            _calculator = calculator;
            List<OperationInfoViewModel> operations = new List<OperationInfoViewModel>
            {
                new OperationInfoViewModel
                {
                    Type = OperationType.Graph,
                    Title = "Operation Graph",
                    Icon = "\ueaf2",
                    CurStatus = NodeStatus.None,
                },
                new OperationInfoViewModel
                {
                    Type = OperationType.Graph,
                    Title = "X",
                    Icon = "\ueaf2",
                    CurStatus = NodeStatus.None,
                },
                new OperationInfoViewModel
                {
                    Type = OperationType.Calculator,
                    Title = "Calculator",
                    Icon = "\ueaf2",
                    CurStatus = NodeStatus.None,
                },
                new OperationInfoViewModel
                {
                    Type = OperationType.Expression,
                    Title = "Custom",
                    Icon = "\ueaf2",
                    CurStatus = NodeStatus.None,
                }
            };
            operations.AddRange(OperationFactory.GetOperationsInfo(typeof(OperationsContainer)));

            //模块集合
            AvailableOperations = new NodifyObservableCollection<OperationInfoViewModel>(operations);
            CreateOperationCommand = new ReeYin_V.NodifyManager.DelegateCommand<OperationInfoViewModel>(CreateOperation);
        }
        #endregion

        #region Methods
        public void OpenAt(Point targetLocation)
        {
            Close();
            Location = targetLocation;
            IsVisible = true;
        }

        public void Close()
        {
            IsVisible = false;
        }

        private void CreateOperation(OperationInfoViewModel operationInfo)
        {
            OperationViewModel op = OperationFactory.GetOperation(operationInfo);
            op.Location = Location;

            _calculator.Operations.Add(op);

            var pending = _calculator.PendingConnection;
            if (pending.IsVisible)
            {
                var connector = pending.Source.IsInput ? op.Output : op.Input.FirstOrDefault();
                if (connector != null && _calculator.CanCreateConnection(pending.Source, connector))
                {
                    _calculator.CreateConnection(pending.Source, connector);
                }
            }
            Close();
        }

        #endregion
    }
}
