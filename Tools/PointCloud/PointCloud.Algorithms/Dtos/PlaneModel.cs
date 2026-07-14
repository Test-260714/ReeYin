namespace PointCloud.Algorithms.Dtos;

public sealed class PlaneModel
{
    public PlaneModel()
    {
    }

    public PlaneModel(float a, float b, float c, float d, float tiltAngleDegrees = 0)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        TiltAngleDegrees = tiltAngleDegrees;
    }

    public float A { get; set; }

    public float B { get; set; }

    public float C { get; set; }

    public float D { get; set; }

    public float TiltAngleDegrees { get; set; }

    public float[] ToArray()
    {
        return [A, B, C, D];
    }
}
