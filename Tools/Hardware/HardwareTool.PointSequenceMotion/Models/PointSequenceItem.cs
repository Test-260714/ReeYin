using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;

namespace HardwareTool.PointSequenceMotion.Models
{
    [Serializable]
    public class PointSequenceItem : BindableBase
    {
        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set { _description = value; RaisePropertyChanged(); }
        }

        private CoordinatePos? _linkedPosition;
        public CoordinatePos? LinkedPosition
        {
            get => _linkedPosition;
            set
            {
                _linkedPosition = value;
                ApplyLinkedPosition(value);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PositionDescription));
            }
        }

        private double _x;
        public double X
        {
            get => _x;
            set { _x = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(PositionDescription)); }
        }

        private double _y;
        public double Y
        {
            get => _y;
            set { _y = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(PositionDescription)); }
        }

        private double _z;
        public double Z
        {
            get => _z;
            set { _z = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(PositionDescription)); }
        }

        private double _z1;
        public double Z1
        {
            get => _z1;
            set { _z1 = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(PositionDescription)); }
        }

        private double _z2;
        public double Z2
        {
            get => _z2;
            set { _z2 = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(PositionDescription)); }
        }

        [JsonIgnore]
        public string PositionDescription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(LinkedPosition?.Describe))
                {
                    return LinkedPosition.Describe;
                }

                return $"{X:F3}/{Y:F3}/{Z:F3}/{Z1:F3}/{Z2:F3}";
            }
        }

        public double[] ToTargetArray()
        {
            return new[] { X, Y, Z, Z1, Z2 };
        }

        public Dictionary<En_AxisNum, double> ToTargetDictionary(
            bool useX,
            bool useY,
            bool useZ,
            bool useZ1,
            bool useZ2)
        {
            var targets = new Dictionary<En_AxisNum, double>();

            if (useX) targets[En_AxisNum.X] = X;
            if (useY) targets[En_AxisNum.Y] = Y;
            if (useZ) targets[En_AxisNum.Z] = Z;
            if (useZ1) targets[En_AxisNum.Z1] = Z1;
            if (useZ2) targets[En_AxisNum.Z2] = Z2;

            return targets;
        }

        private void ApplyLinkedPosition(CoordinatePos? position)
        {
            if (position?.TargetPos == null)
            {
                return;
            }

            if (position.TargetPos.Count > 0) X = position.TargetPos[0];
            if (position.TargetPos.Count > 1) Y = position.TargetPos[1];
            if (position.TargetPos.Count > 2) Z = position.TargetPos[2];
            if (position.TargetPos.Count > 3) Z1 = position.TargetPos[3];
            if (position.TargetPos.Count > 4) Z2 = position.TargetPos[4];
        }
    }
}
