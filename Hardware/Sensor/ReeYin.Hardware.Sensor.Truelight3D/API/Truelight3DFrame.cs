namespace ReeYin.Hardware.Sensor.Truelight3D.API
{
    public sealed class Truelight3DFrame
    {
        public byte[] PixelData { get; set; } = [];

        public int Width { get; set; }

        public int Height { get; set; }

        public int Channel { get; set; }

        public Truelight3DPixelFormat Format { get; set; } = Truelight3DPixelFormat.RGB;

        public float Clarity { get; set; }
    }
}
