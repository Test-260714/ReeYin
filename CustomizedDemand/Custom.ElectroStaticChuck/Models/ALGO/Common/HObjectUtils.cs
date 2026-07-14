using HalconDotNet;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Common;

public static class HObjectUtils
{
    public static void Replace(ref HObject target, ref HObject? source)
    {
        HObject current = target;
        if (!ReferenceEquals(current, source))
            current?.Dispose();

        target = source ?? new HObject();
        source = null;
    }

    public static void DisposeAll(params HObject?[] objects)
    {
        foreach (HObject? item in objects)
            item?.Dispose();
    }
}
