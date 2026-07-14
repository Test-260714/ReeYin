using ReeYin_V.Core.Services.Project;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    public static class AxisViewAxisMatcher
    {
        public static IReadOnlyList<En_AxisNum> DisplayAxes { get; } =
            new[] { En_AxisNum.X, En_AxisNum.Y, En_AxisNum.Z, En_AxisNum.Z1, En_AxisNum.Z2 };

        public static bool ContainsAxis(IEnumerable<SingleAxisParam>? axes, En_AxisNum axisType)
        {
            return TryGetAxis(axes, axisType, out _);
        }

        public static short? GetAxisNo(IEnumerable<SingleAxisParam>? axes, En_AxisNum axisType)
        {
            return TryGetAxis(axes, axisType, out var axis) ? axis.AxisNo : null;
        }

        public static bool TryGetAxis(IEnumerable<SingleAxisParam>? axes, En_AxisNum axisType, out SingleAxisParam axis)
        {
            var matchedAxis = axes?.FirstOrDefault(item => item != null && item.AxisNum == axisType);
            if (matchedAxis == null)
            {
                axis = null!;
                return false;
            }

            axis = matchedAxis;
            return true;
        }

        public static int GetDisplayIndex(En_AxisNum axisType)
        {
            for (var index = 0; index < DisplayAxes.Count; index++)
            {
                if (DisplayAxes[index] == axisType)
                {
                    return index;
                }
            }

            return -1;
        }

        public static double[] BuildPositionSnapshot(IEnumerable<SingleAxisParam>? axes)
        {
            var result = new double[DisplayAxes.Count];
            for (var index = 0; index < DisplayAxes.Count; index++)
            {
                if (TryGetAxis(axes, DisplayAxes[index], out var axis))
                {
                    result[index] = axis.CurPos;
                }
            }

            return result;
        }

        public static double[] BuildSpeedSnapshot(IEnumerable<SingleAxisParam>? axes)
        {
            var result = new double[DisplayAxes.Count];
            for (var index = 0; index < DisplayAxes.Count; index++)
            {
                if (TryGetAxis(axes, DisplayAxes[index], out var axis))
                {
                    result[index] = axis.CurSpeed;
                }
            }

            return result;
        }

        public static bool[] BuildEnableSnapshot(IEnumerable<SingleAxisParam>? axes)
        {
            var result = new bool[DisplayAxes.Count];
            for (var index = 0; index < DisplayAxes.Count; index++)
            {
                if (TryGetAxis(axes, DisplayAxes[index], out var axis))
                {
                    result[index] = axis.IsEnable;
                }
            }

            return result;
        }

        public static bool[] BuildConfiguredSnapshot(IEnumerable<SingleAxisParam>? axes)
        {
            var result = new bool[DisplayAxes.Count];
            for (var index = 0; index < DisplayAxes.Count; index++)
            {
                result[index] = ContainsAxis(axes, DisplayAxes[index]);
            }

            return result;
        }
    }
}
