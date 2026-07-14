using Newtonsoft.Json;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DynamicView;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard
{
    /// <summary>
    /// 控制卡
    /// </summary>
    [Serializable]
    public class ControlCardConfig : BindableBase
    {
        #region Fields
        public bool IsUsed = true;                                  //启用
        public string Com = "";                                     //COM口
        public int BaudRate = 230400;                               //波特率
        public byte DevAddr = 0;                                    //板卡地址

        public bool IsAxisXReserve = false;             //示教X轴是否反向
        public bool IsAxisYReserve = false;             //示教Y轴是否反向
        public bool IsAxisZReserve = false;             //示教Z轴是否反向
        public bool IsAxisRReserve = false;             //示教R轴是否反向


        /// <summary>
        /// 插补坐标系
        /// </summary>
        //public Dictionary<int, List<En_AxisNum>> InterCooSys;
        #endregion

        #region Properties

        public int AxisNum { get; set; } = 4;

        public float Height_Content_Z { get; set; } = 0f;

        public float Height_Z { get; set; } = 0f;

        [JsonIgnore]
        private ObservableCollection<SingleAxisParam> _allAxis = new ObservableCollection<SingleAxisParam>();

        /// <summary>
        /// 所有轴
        /// </summary>
        public ObservableCollection<SingleAxisParam> AllAxis
        {
            get { return _allAxis; }
            set { _allAxis = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ObservableCollection<InterPoCoordinateSystem> _interpolationCoordinateSystems = new ObservableCollection<InterPoCoordinateSystem>();

        [JsonIgnore]
        [NonSerialized]
        private bool _isSynchronizingInterpolationCoordinateSystems;

        /// <summary>
        /// 插补坐标系配置
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<InterPoCoordinateSystem> InterpolationCoordinateSystems
        {
            get { return _interpolationCoordinateSystems; }
            set
            {
                if (_interpolationCoordinateSystems != null)
                {
                    _interpolationCoordinateSystems.CollectionChanged -= OnInterpolationCoordinateSystemsChanged;
                    UnsubscribeInterpolationCoordinateSystems(_interpolationCoordinateSystems);
                }

                _interpolationCoordinateSystems = value ?? new ObservableCollection<InterPoCoordinateSystem>();
                _interpolationCoordinateSystems.CollectionChanged += OnInterpolationCoordinateSystemsChanged;
                SubscribeInterpolationCoordinateSystems(_interpolationCoordinateSystems);
                NormalizeInterpolationCoordinateSystems();
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private InterPoCoordinateSystem _defaultInterpCS;

        /// <summary>
        /// 默认插补坐标系
        /// </summary>
        public InterPoCoordinateSystem DefaultInterpCS
        {
            get { return _defaultInterpCS; }
            set
            {
                _defaultInterpCS = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Constructor
        public ControlCardConfig()
        {
            InterpolationCoordinateSystems = new ObservableCollection<InterPoCoordinateSystem>
            {
                CreateDefaultInterpolationCoordinateSystem(true)
            };
            SyncDefaultInterpolationCoordinateSystem();
        }
        #endregion

        #region Methods
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            InterpolationCoordinateSystems = InterpolationCoordinateSystems ?? new ObservableCollection<InterPoCoordinateSystem>();
            NormalizeInterpolationCoordinateSystems();
            SyncDefaultInterpolationCoordinateSystem();
        }

        public InterPoCoordinateSystem CreateDefaultInterpolationCoordinateSystem(bool isUsing)
        {
            return new InterPoCoordinateSystem
            {
                IsUsing = isUsing,
                AxisCount = 2,
                Axis1 = En_AxisNum.X,
                Axis2 = En_AxisNum.Y,
                Axis3 = En_AxisNum.Z,
                Axis4 = En_AxisNum.R,
                Axis5 = En_AxisNum.X1,
                StartSpeed = 10,
                MaxSpeed = 50,
                AccSpeed = 200,
                EndSpeed = 0,
                PulseEquivalent = 10000
            };
        }

        public void EnsureInterpolationCoordinateSystems()
        {
            if (InterpolationCoordinateSystems == null)
            {
                InterpolationCoordinateSystems = new ObservableCollection<InterPoCoordinateSystem>();
            }

            NormalizeInterpolationCoordinateSystems();
            SyncDefaultInterpolationCoordinateSystem();
        }

        public void SyncDefaultInterpolationCoordinateSystem(InterPoCoordinateSystem selectedSystem = null)
        {
            var sourceSystem = selectedSystem
                ?? GetMatchedInterpolationCoordinateSystem(DefaultInterpCS)
                ?? InterpolationCoordinateSystems?.FirstOrDefault(item => item != null && item.IsUsing)
                ?? InterpolationCoordinateSystems?.FirstOrDefault(item => item != null)
                ?? CreateDefaultInterpolationCoordinateSystem(true);

            DefaultInterpCS = sourceSystem?.Clone();
        }

        public InterPoCoordinateSystem GetMatchedInterpolationCoordinateSystem(InterPoCoordinateSystem sourceSystem)
        {
            if (sourceSystem == null || InterpolationCoordinateSystems == null || InterpolationCoordinateSystems.Count == 0)
            {
                return null;
            }

            return InterpolationCoordinateSystems.FirstOrDefault(item => IsSameInterpolationCoordinateSystem(item, sourceSystem));
        }

        private void OnInterpolationCoordinateSystemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<InterPoCoordinateSystem>())
                {
                    item.PropertyChanged -= OnInterpolationCoordinateSystemPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<InterPoCoordinateSystem>())
                {
                    item.PropertyChanged += OnInterpolationCoordinateSystemPropertyChanged;
                }
            }

            NormalizeInterpolationCoordinateSystems();
            SyncDefaultInterpolationCoordinateSystem();
        }

        private void OnInterpolationCoordinateSystemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isSynchronizingInterpolationCoordinateSystems || sender is not InterPoCoordinateSystem changedSystem)
            {
                return;
            }

            if (e.PropertyName == nameof(InterPoCoordinateSystem.IsUsing) && changedSystem.IsUsing)
            {
                _isSynchronizingInterpolationCoordinateSystems = true;
                try
                {
                    foreach (var system in InterpolationCoordinateSystems.Where(item => item != null && !ReferenceEquals(item, changedSystem)))
                    {
                        system.IsUsing = false;
                    }
                }
                finally
                {
                    _isSynchronizingInterpolationCoordinateSystems = false;
                }
            }

            if (e.PropertyName == nameof(InterPoCoordinateSystem.IsUsing) &&
                !InterpolationCoordinateSystems.Any(item => item != null && item.IsUsing))
            {
                _isSynchronizingInterpolationCoordinateSystems = true;
                try
                {
                    changedSystem.IsUsing = true;
                }
                finally
                {
                    _isSynchronizingInterpolationCoordinateSystems = false;
                }
            }
        }

        private void NormalizeInterpolationCoordinateSystems()
        {
            if (_isSynchronizingInterpolationCoordinateSystems)
            {
                return;
            }

            _isSynchronizingInterpolationCoordinateSystems = true;
            try
            {
                if (InterpolationCoordinateSystems == null)
                {
                    _interpolationCoordinateSystems = new ObservableCollection<InterPoCoordinateSystem>();
                    _interpolationCoordinateSystems.CollectionChanged += OnInterpolationCoordinateSystemsChanged;
                }

                if (InterpolationCoordinateSystems.Count == 0)
                {
                    InterpolationCoordinateSystems.Add(CreateDefaultInterpolationCoordinateSystem(true));
                    return;
                }

                var firstEnabledSystem = InterpolationCoordinateSystems.FirstOrDefault(item => item != null && item.IsUsing)
                    ?? InterpolationCoordinateSystems.FirstOrDefault();

                foreach (var system in InterpolationCoordinateSystems.Where(item => item != null))
                {
                    system.IsUsing = ReferenceEquals(system, firstEnabledSystem);
                }
            }
            finally
            {
                _isSynchronizingInterpolationCoordinateSystems = false;
            }
        }

        private static bool IsSameInterpolationCoordinateSystem(InterPoCoordinateSystem left, InterPoCoordinateSystem right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.AxisCount == right.AxisCount &&
                   left.Axis1 == right.Axis1 &&
                   left.Axis2 == right.Axis2 &&
                   left.Axis3 == right.Axis3 &&
                   left.Axis4 == right.Axis4 &&
                   left.Axis5 == right.Axis5 &&
                   Math.Abs(left.StartSpeed - right.StartSpeed) < 0.000001d &&
                   Math.Abs(left.MaxSpeed - right.MaxSpeed) < 0.000001d &&
                   Math.Abs(left.AccSpeed - right.AccSpeed) < 0.000001d &&
                   Math.Abs(left.EndSpeed - right.EndSpeed) < 0.000001d &&
                   Math.Abs(left.PulseEquivalent - right.PulseEquivalent) < 0.000001d;
        }

        private void SubscribeInterpolationCoordinateSystems(IEnumerable<InterPoCoordinateSystem> coordinateSystems)
        {
            foreach (var coordinateSystem in coordinateSystems.Where(item => item != null))
            {
                coordinateSystem.PropertyChanged -= OnInterpolationCoordinateSystemPropertyChanged;
                coordinateSystem.PropertyChanged += OnInterpolationCoordinateSystemPropertyChanged;
            }
        }

        private void UnsubscribeInterpolationCoordinateSystems(IEnumerable<InterPoCoordinateSystem> coordinateSystems)
        {
            foreach (var coordinateSystem in coordinateSystems.Where(item => item != null))
            {
                coordinateSystem.PropertyChanged -= OnInterpolationCoordinateSystemPropertyChanged;
            }
        }
        #endregion

    }
}
