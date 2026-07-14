using HalconDotNet;
using ReeYin_V.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.DeepLearning
{
    public class Point
    {
        public float X;
        public float Y;
        public float Confidence;
    }


    public class Skeleton
    {
        public int StartKptId;
        public int EndKptId;
    }


    public class Keypoints
    {
        public List<Point> Points = new List<Point>();
        public List<Skeleton> Skeletons = new List<Skeleton>();
        public float Thresh;
    }


    //public class Segment
    //{
    //    public int Width;
    //    public int Height;
    //    public float[] AffineMatrix = new float[6];
    //    public float Thresh;
    //    public bool IsIntData;
    //    public int[] IntData = Array.Empty<int>();
    //    public float[] FloatData = Array.Empty<float>();
    //}


    public class Result
    {
        public float Cx, Cy, Width, Height, Angle;
        public float Confidence;
        public int ClassId;
        public string ClassName;
        public Keypoints Kpt = new Keypoints();
        //public Segment Seg = new Segment();
        public HObject Seg = new HObject();

        public eDeepLearningModelType ModelType;

        /// <summary>
        /// 参数名称，参数结果
        /// </summary>
        public Dictionary<string,object> Others = new Dictionary<string, object>();
    }

}
