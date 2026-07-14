using System;

namespace ReeYin_V.UI.UserControls.TrajectoryDesigner.Models
{
    [Serializable]
    public enum TrajectoryShapeCategory
    {
        Point,
        Line,
        Region
    }

    public static class TrajectoryShapeCategoryResolver
    {
        public static TrajectoryShapeCategory Resolve(TrajectoryShapeKind kind)
        {
            return kind switch
            {
                TrajectoryShapeKind.Point => TrajectoryShapeCategory.Point,
                TrajectoryShapeKind.Circle or TrajectoryShapeKind.Rectangle => TrajectoryShapeCategory.Region,
                _ => TrajectoryShapeCategory.Line
            };
        }

        public static string ToDisplayText(TrajectoryShapeCategory category)
        {
            return category switch
            {
                TrajectoryShapeCategory.Point => "点",
                TrajectoryShapeCategory.Region => "区域",
                _ => "线段"
            };
        }
    }
}
