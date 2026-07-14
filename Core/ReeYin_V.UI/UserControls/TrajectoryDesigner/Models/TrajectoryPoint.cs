using System;

namespace ReeYin_V.UI.UserControls.TrajectoryDesigner.Models
{
    [Serializable]
    public sealed class TrajectoryPoint
    {
        public double X { get; set; }

        public double Y { get; set; }

        public TrajectoryPoint Clone()
        {
            return new TrajectoryPoint
            {
                X = X,
                Y = Y
            };
        }
    }
}
