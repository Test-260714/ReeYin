using ReeYin_V.Core.Services.Project;
using System;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    private bool TryGetCurrentInterpolationPoint(En_AxisNum[] axisIds, out double[] point)
    {
        point = Array.Empty<double>();
        if (axisIds == null || axisIds.Length == 0)
        {
            Console.WriteLine("ACS XSEG interpolation axes are missing.");
            return false;
        }

        point = new double[axisIds.Length];
        for (var i = 0; i < axisIds.Length; i++)
        {
            point[i] = _api.GetFPosition(ToAcsAxis(axisIds[i]));
            if (!IsFinite(point[i]))
            {
                Console.WriteLine($"ACS XSEG start position for axis {axisIds[i]} is invalid.");
                return false;
            }
        }

        return true;
    }
}
