using HalconDotNet;
using MathNet.Numerics.Distributions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OpenCvSharp;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Services.CustomProject;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;


namespace Custom.EVEMFDJC.Models
{
    using DeepLearningHandle = System.IntPtr;

    public partial class EVEMFDJC0_Algorithm : ICustomAlgo
    {
        public enum DeviceType
        {
            DEVICE_CPU = 0,
            DEVICE_GPU = 1
        }

        public enum ModelType
        {
            MODEL_CLASSIFICATION = 0,
            MODEL_DETECTION_BBOX = 1,
            MODEL_DETECTION_SEG = 2,
            MODEL_DETECTION_OBB = 3,
            MODEL_DETECTION_KPT = 4,
            MODEL_SEGMENTATION = 5,
            MODEL_ANOMALY_DETECTION = 6
        }

        public enum RotationAngle
        {
            ANGLE_0,
            ANGLE_90,
            ANGLE_180,
            ANGLE_270
        }

        public enum FlowList
        {
            一次扫描,
            两次扫描,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NativeModelConfig
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string ModelPath;
            public int BatchSize;
            public DeviceType DeviceType;
            public ModelType ModelType;

            public float ConfidenceThreshold;
            public float IoUThreshold;
            public float SegmentationThreshold;
            public float KeypointThreshold;
        }

        internal class SealingNailsSDK
        {
            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern DeepLearningHandle CreateModel(ref NativeModelConfig config);

            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int DestroyModel(DeepLearningHandle handle);

            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int InitRuntime(DeepLearningHandle handle, ref NativeModelConfig config);


            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public extern static int Pipeline(DeepLearningHandle handle,
                                              IntPtr inImageData, int inIw, int inIh, int inIc, int inItype,
                                              IntPtr inDepthData, int inDw, int inDh, int inDc, int inDtype,
                                              out IntPtr objInfo, out int objectNum);

            [DllImport("ALGO.DeepLearningNative.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int CleanUpResult(DeepLearningHandle handle, ref IntPtr objInfo);
        }

        /// <summary>
        /// 用来存一下临时的位置数据
        /// </summary>
        public List<double> tempx = new List<double>();
        
        private DeepLearningHandle _deepLearningHandle;
        private NativeModelConfig _config;

        private double _expendPixels;
        private List<ImageData> _imageData;
        private MFDJC0_MeasureParam _measureParam;

        private Dictionary<int, string> _categories;
        private int _segCategoryNum;
        private static List<FeatureAlgorithm> _algorithmList;
        private static List<Defect> _defectList;
        private Locator _locator = new Locator();

        private HObject _hoNailCenterModel;
        private bool _nailCenterIsTrue = false;

        private HObject _hoOrbitMask;
        private HObject _hoNailWarpBaseMask;
        private HObject _hoValidMask;
        private HObject _hoIrregularMask;
        private HTuple _hvNailCenterModelID;

        private HTuple _hvHeightImageGlobalMinValue;
        private HTuple _hvHeightImageGlobalMaxValue;


        private MFDJC0_MeasureResult _measureResult;

        private bool _disposed = false;

        public EVEMFDJC0_Algorithm(MFDJC0_MeasureParam param)
        {
            _imageData = new List<ImageData>();
            _measureParam = param;

            _algorithmList = new List<FeatureAlgorithm>();
            _defectList = new List<Defect>();

            try
            {
                LoadModelConfig(param);
                _deepLearningHandle = SealingNailsSDK.CreateModel(ref _config);
                int state = SealingNailsSDK.InitRuntime(_deepLearningHandle, ref _config);

                InitVariable();

                ParseModelConfigJson(param.ModelConfigPath);
                ParseFeatureConfigJson(param.FeatureConfigPath);

                //HOperatorSet.ReadShapeModel(param.TemplateModelPath, out _hvNailCenterModelID);
                HOperatorSet.ReadNccModel(param.TemplateModelPath, out _hvNailCenterModelID);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

        }


        public void Dispose()
        {
            if (!_disposed)
            {
                if (_imageData != null)
                {
                    foreach (var data in _imageData)
                    {
                        data?.Dispose();
                    }
                    _imageData.Clear();
                }

                _hoValidMask.Dispose();
                _hoIrregularMask.Dispose();
                _hoNailCenterModel.Dispose();
                _hoOrbitMask.Dispose();
                _hoNailWarpBaseMask.Dispose();

            }
            _nailCenterIsTrue = false;

            _disposed = true;

            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            //GC.Collect();
        }


        public void DestroyModel()
        {
            int state = SealingNailsSDK.DestroyModel(_deepLearningHandle);
        }


        ~EVEMFDJC0_Algorithm()
        {
            int state = SealingNailsSDK.DestroyModel(_deepLearningHandle);

            _hvNailCenterModelID.Dispose();
            _hvHeightImageGlobalMinValue.Dispose();
            _hvHeightImageGlobalMaxValue.Dispose();

            Dispose();
        }


        private void LoadModelConfig(MFDJC0_MeasureParam config)
        {
            _config.ModelPath = config.ModelPath;
            _config.BatchSize = config.BatchSize;
            _config.DeviceType = (DeviceType)config.DeviceType;
            _config.ModelType = (ModelType)config.ModelType;
            _config.ConfidenceThreshold = (float)config.ConfidenceThreshold;
            _config.IoUThreshold = (float)config.IoUThreshold;
            _config.SegmentationThreshold = (float)config.SegmentationThreshold;
            _config.KeypointThreshold = 0.5f;
        }


        /// <summary>
        /// 初始化
        /// </summary>
        public int InitVariable()
        {
            _disposed = false;

            _hoValidMask = new HObject();
            _hoIrregularMask = new HObject();
            _hoNailCenterModel = new HObject();
            _hoOrbitMask = new HObject();
            _hoNailWarpBaseMask = new HObject();

            HOperatorSet.GenEmptyObj(out _hoValidMask);
            HOperatorSet.GenEmptyObj(out _hoIrregularMask);
            HOperatorSet.GenEmptyObj(out _hoNailCenterModel);
            HOperatorSet.GenEmptyObj(out _hoOrbitMask);
            HOperatorSet.GenEmptyObj(out _hoNailWarpBaseMask);

            _expendPixels = 0;
            _nailCenterIsTrue = false;

            _hvNailCenterModelID = new HTuple();
            _hvHeightImageGlobalMinValue = new HTuple();
            _hvHeightImageGlobalMaxValue = new HTuple();

            _measureResult = new MFDJC0_MeasureResult();

            return 0;
        }


        /// <summary>
        /// 解析ModelConfig.json
        /// </summary>
        private int ParseModelConfigJson(string path)
        {
            _categories = new Dictionary<int, string>();

            string json = File.ReadAllText(path);
            BsonDocument document = BsonDocument.Parse(json);

            var segCategories = document["param"]["categories"].AsBsonArray.Select(a => new Category
            {
                Id = a["id"].AsInt32,
                Name = a["name"].AsString
            }).ToList();

            for (int i = 0; i < segCategories.Count; i++)
            {
                Category category = segCategories[i];
                if (_categories.ContainsKey(category.Id))
                {
                    throw new InvalidOperationException($"Category {category.Id} is found in the segCategories.");
                }
                else
                {
                    _categories[category.Id] = category.Name;
                }
            }

            _segCategoryNum = segCategories.Count;

            // 添加翘钉类别
            int id = segCategories.Count;
            _categories[id] = "翘钉";

            // 添加轨迹偏移逻辑
            id += 1;
            _categories[id] = "轨迹偏移";


            return 0;
        }


        /// <summary>
        /// 解析FeatureConfig.json
        /// </summary>
        private int ParseFeatureConfigJson(string path)
        {
            string json = File.ReadAllText(path);
            BsonDocument document = BsonDocument.Parse(json);

            _algorithmList.Clear();
            _defectList.Clear();
            _locator = ParseLocatorConfig(document);

            _algorithmList = document["algorithm_list"].AsBsonArray.Select(a => new FeatureAlgorithm
            {
                Id = a["id"].AsInt32,
                Name = a["name"].AsString,
                Parameters = ExtractParameters(a["parameters"].AsBsonArray)
            }).ToList();

            _defectList = document["defect_list"].AsBsonArray.Select(d => new Defect
            {
                Id = d["id"].AsInt32,
                Name = d["name"].AsString,
                AlgName = d["alg_name"].AsString,
                AlgParam = ExtractParameters(d["alg_param"].AsBsonArray)
            }).ToList();

            return 0;
        }


        /// <summary>
        /// 更新_algorithmList与_defectList
        /// </summary>
        public static int UpdateParseFeatureConfig(FeatureConfig featureConfig)
        {
            _algorithmList.Clear();
            _defectList.Clear();

            foreach (var algorith in featureConfig.AlgorithmList)
            {
                FeatureAlgorithm al = new FeatureAlgorithm();
                al.Id = algorith.Id;
                al.Name = algorith.Name;
                al.Parameters = new BsonDocument();
                foreach (var param in algorith.Parameters)
                {
                    if (param.GetType().GetProperty("Name") != null && param.GetType().GetProperty("Value") != null)
                    {
                        string paramName = param.Name;
                        BsonValue paramValue = Convert.ToDouble(param.Value ?? 0);
                        al.Parameters[paramName] = paramValue;
                    }
                }

                _algorithmList.Add(al);
            }

            foreach (var defect in featureConfig.DefectList)
            {
                Defect df = new Defect();
                df.Id = defect.Id;
                df.Name = defect.Name;
                df.AlgName = defect.AlgName;
                df.AlgParam = new BsonDocument();
                foreach (var param in defect.AlgParam)
                {
                    if (param.GetType().GetProperty("Name") != null && param.GetType().GetProperty("Value") != null)
                    {
                        string paramName = param.Name;
                        BsonValue paramValue = Convert.ToDouble(param.Value ?? 0);
                        df.AlgParam[paramName] = paramValue;
                    }

                    _defectList.Add(df);
                }
            }

            return 0;
        }


        private Locator ParseLocatorConfig(BsonDocument document)
        {
            Locator locator = CreateLegacyLocatorConfig();
            if (!document.TryGetValue("locator", out BsonValue locatorValue) || !locatorValue.IsBsonDocument)
                return locator;

            BsonDocument locatorDocument = locatorValue.AsBsonDocument;
            if (TryGetString(locatorDocument, "name", out string name))
                locator.Name = name;

            if (TryGetString(locatorDocument, "alg_name", out string algName))
                locator.AlgName = algName;

            if (locatorDocument.TryGetValue("alg_param", out BsonValue algParamValue) && algParamValue.IsBsonArray)
                locator.AlgParam = ExtractParameters(algParamValue.AsBsonArray);

            return locator;
        }

        private Locator CreateLegacyLocatorConfig()
        {
            return new Locator
            {
                AlgName = _measureParam.IsWithMilling
                    ? "GetNailCenterAndOrbitMask"
                    : "GetNailCenterAndOrbitMaskV2",
                AlgParam = new BsonDocument()
            };
        }

        private static bool TryGetString(BsonDocument document, string key, out string value)
        {
            value = string.Empty;
            if (!document.TryGetValue(key, out BsonValue bsonValue) || !bsonValue.IsString)
                return false;

            value = bsonValue.AsString;
            return !string.IsNullOrWhiteSpace(value);
        }

        private static BsonDocument ExtractParameters(BsonArray rawParams)
        {
            var result = new BsonDocument();
            foreach (var param in rawParams)
            {
                if (param.IsBsonDocument)
                {
                    var paramDoc = param.AsBsonDocument;
                    if (paramDoc.Contains("name") && paramDoc.Contains("value"))
                    {
                        string paramName = paramDoc["name"].AsString;
                        BsonValue paramValue = paramDoc["value"];
                        result[paramName] = paramValue;
                    }
                }
            }
            return result;
        }


        public enum ImageType
        {
            Gray,    // 灰度图
            Depth,   // 深度图
            RGB,     // 三通道RGB图
            BGR      // 三通道BGR图
        }


        /// <summary>
        /// 将List<float[]>数组转换为halcon图片对象
        /// </summary>
        /// <param name="data">输入的List<float[]>数组</param>
        /// <param name="imageType">输入图片数据类型</param>
        /// <param name="hoObject">输出的halcon图片对象</param>
        /// <returns>状态标志</returns>
        public int ConvertListToHObject(List<float[]> data, ImageType imageType, out HObject hoObject)
        {
            int height = data.Count;
            if (height == 0)
            {
                HOperatorSet.GenEmptyObj(out hoObject);
                return -1;
            }

            int width = data[0].Length;
            GCHandle handle = default;

            try
            {
                if (imageType == ImageType.Gray)
                {
                    byte[] imageData = data.SelectMany(row => row.Select(value => (byte)Math.Clamp(value, 0, 255))).ToArray();
                    handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                    HOperatorSet.GenImage1(out hoObject, "byte", width, height, handle.AddrOfPinnedObject());
                }
                else if (imageType == ImageType.Depth)
                {
                    float[] imageData = data.SelectMany(row => row).ToArray();
                    handle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                    HOperatorSet.GenImage1(out hoObject, "real", width, height, handle.AddrOfPinnedObject());
                }
                else
                {
                    HOperatorSet.GenEmptyObj(out hoObject);
                    return -1;
                }

                return 0;
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }


        /// <summary>
        /// 将List<float[]>数组转换为OpenCvSharp Mat图片对象
        /// </summary>
        /// <param name="data">输入的List<float[]>数组</param>
        /// <param name="imageType">输入图片数据类型</param>
        /// <param name="cvImage">输出的opencv图片对象</param>
        /// <returns>状态标志</returns>
        public int ConvertListToMat(List<float[]> data, ImageType imageType, out Mat cvImage)
        {
            int height = data.Count;
            int width;
            if (height > 0)
            {
                width = data[0].Length;

                if (imageType == ImageType.Gray)
                {
                    byte[] imageData = data.SelectMany(row => row.Select(value => (byte)Math.Max(0, Math.Min(255, value)))).ToArray();
                    cvImage = new Mat(height, width, MatType.CV_8UC1);
                    cvImage.SetArray(imageData);
                }
                else if (imageType == ImageType.Depth)
                {
                    float[] imageData = data.SelectMany(row => row).ToArray();
                    cvImage = new Mat(height, width, MatType.CV_32FC1);
                    cvImage.SetArray(imageData);
                }
                else
                {
                    cvImage = new Mat();
                    return -1;
                }
                return 0;
            }
            else
            {
                cvImage = new Mat();
                return -1;
            }
        }


        /// <summary>
        /// halcon HObject类型图片转OpenCVSharp Mat类型
        /// </summary>
        public Mat HobjectToMat(HObject hoImage, ImageType imageType)
        {
            Mat dst = new Mat();
            try
            {
                HOperatorSet.CountChannels(hoImage, out HTuple hvChannels);

                if (hvChannels.Length == 0)
                {
                    return dst;
                }
                if (hvChannels[0].I == 1)
                {
                    IntPtr intPtr = IntPtr.Zero;
                    HOperatorSet.GetImagePointer1(hoImage, out HTuple hvPointer, out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    intPtr = hvPointer;
                    if (imageType == ImageType.Gray)
                    {
                        dst = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, intPtr);
                    }
                    else
                    {
                        dst = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_32FC1, intPtr);
                    }
                }
                else if (hvChannels[0].I == 3)
                {
                    IntPtr ptrRed = IntPtr.Zero;
                    IntPtr ptrGreen = IntPtr.Zero;
                    IntPtr ptrBlue = IntPtr.Zero;

                    HOperatorSet.GetImagePointer3(hoImage, out HTuple hvPtrRed, out HTuple hvPtrGreen, out HTuple hvPtrBlue,
                                                  out HTuple hvType, out HTuple hvWidth, out HTuple hvHeight);
                    ptrRed = hvPtrRed;
                    ptrGreen = hvPtrGreen;
                    ptrBlue = hvPtrBlue;

                    //分别生成3张图片
                    Mat matRed = new Mat();
                    Mat matGreen = new Mat();
                    Mat matBlue = new Mat();

                    if (imageType == ImageType.Gray)
                    {
                        matRed = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrRed);
                        matGreen = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrGreen);
                        matBlue = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_8UC1, ptrBlue);
                    }
                    else
                    {
                        matRed = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_32FC1, ptrRed);
                        matGreen = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_32FC1, ptrGreen);
                        matBlue = Mat.FromPixelData(hvHeight, hvWidth, MatType.CV_32FC1, ptrBlue);
                    }

                    //合成
                    Mat[] multi = new Mat[] { matBlue, matGreen, matRed };
                    Cv2.Merge(multi, dst);

                    //释放
                    matBlue.Dispose();
                    matGreen.Dispose();
                    matRed.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return dst.Clone();
        }


        /// <summary>
        /// 将 OpenCvSharp 的 Mat 图像转换为 Halcon 的 HObject
        /// </summary>
        public static HObject MatToHObject(Mat mat)
        {
            HObject hoImage = new HObject();
            HOperatorSet.GenEmptyObj(out hoImage);

            if (mat.Empty())
                return hoImage;

            int width = mat.Width;
            int height = mat.Height;

            try
            {
                if (mat.Channels() == 1)
                {
                    if (mat.Type() == MatType.CV_8UC1)
                    {
                        byte[] data = new byte[width * height];
                        Marshal.Copy(mat.Data, data, 0, data.Length);
                        HOperatorSet.GenImage1(out hoImage, "byte", width, height, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
                    }
                    else if (mat.Type() == MatType.CV_32FC1)
                    {
                        float[] data = new float[width * height];
                        Marshal.Copy(mat.Data, data, 0, data.Length);
                        HOperatorSet.GenImage1(out hoImage, "real", width, height, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
                    }
                }
                else if (mat.Channels() == 3 && mat.Type() == MatType.CV_8UC3)
                {
                    // 按 BGR 拆分
                    Mat[] channels = Cv2.Split(mat);
                    byte[] red = new byte[width * height];
                    byte[] green = new byte[width * height];
                    byte[] blue = new byte[width * height];

                    Marshal.Copy(channels[2].Data, red, 0, red.Length);   // R
                    Marshal.Copy(channels[1].Data, green, 0, green.Length); // G
                    Marshal.Copy(channels[0].Data, blue, 0, blue.Length);  // B

                    HOperatorSet.GenImage3(out hoImage, "byte", width, height,
                                           Marshal.UnsafeAddrOfPinnedArrayElement(red, 0),
                                           Marshal.UnsafeAddrOfPinnedArrayElement(green, 0),
                                           Marshal.UnsafeAddrOfPinnedArrayElement(blue, 0));

                    foreach (var c in channels)
                        c.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return hoImage;

        }



        private void ReplaceHobject(ref HObject target, ref HObject source)
        {
            target.Dispose();
            target = source;
            HOperatorSet.GenEmptyObj(out source);
        }

        private static void ProcessByteMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            byte[] buffer = new byte[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                int offset = i * cols;
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[offset + j];
                }
                data.Add(row);
            }
        }

        private static void ProcessFloatMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            float[] buffer = new float[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                Array.Copy(buffer, i * cols, row, 0, cols);
                data.Add(row);
            }
        }

        private static void ProcessIntMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            int[] buffer = new int[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转为 float 存入结果
                }
                data.Add(row);
            }
        }

        private static void ProcessShortMat(Mat mat, int rows, int cols, List<float[]> data)
        {
            short[] buffer = new short[rows * cols];
            Marshal.Copy(mat.Data, buffer, 0, buffer.Length);

            for (int i = 0; i < rows; i++)
            {
                float[] row = new float[cols];
                for (int j = 0; j < cols; j++)
                {
                    row[j] = buffer[i * cols + j];  // 转 float 存储
                }
                data.Add(row);
            }
        }


        /// <summary>
        /// OpenCVSharp Mat转List<float[]>
        /// </summary>
        public List<float[]> ConvertMatToList(Mat mat)
        {
            List<float[]> data = new List<float[]>();

            if (mat.Empty())
                return data;

            if (!mat.IsContinuous())
                mat = mat.Clone();

            int channels = mat.Channels();
            if (channels != 1)
                throw new InvalidOperationException("Only single-channel matrices are supported");

            int rows = mat.Rows;
            int cols = mat.Cols;
            MatType type = mat.Type();

            try
            {
                if (type == MatType.CV_8UC1)
                {
                    ProcessByteMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32FC1)
                {
                    ProcessFloatMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_32SC1)
                {
                    ProcessIntMat(mat, rows, cols, data);
                }
                else if (type == MatType.CV_16SC1)
                {
                    ProcessShortMat(mat, rows, cols, data);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported matrix type: {type}");
                }
            }
            finally
            {
                if (!mat.IsContinuous())
                    if (mat.Data != IntPtr.Zero)
                        mat.Dispose();
            }

            return data;
        }


        /// <summary>
        /// 创建halcon模板
        /// </summary>
        /// <param name="centerX">模板区域的中心X坐标</param>
        /// <param name="centerY">模板区域的中心Y坐标</param>
        /// <param name="radius">模板区域半径</param>
        /// <returns>状态标志</returns>
        public int CreateHalconTemplateOld(double centerX, double centerY, double radius)
        {
            HObject hoCircle, hoCircleXld;
            HObject hoEmptyImage;

            int width = _measureResult.GrayImage.Width;
            int height = _measureResult.GrayImage.Height;
            HOperatorSet.GenImageConst(out hoEmptyImage, "byte", width, height);

            HOperatorSet.GenCircle(out hoCircle, centerY, centerX, radius);
            HOperatorSet.GenContourRegionXld(hoCircle, out hoCircleXld, "border");
            HOperatorSet.CreateScaledShapeModelXld(hoCircleXld, "auto", -0.39, 0.79, "auto", 0.5, 1.5, "auto",
                                                   "auto", "ignore_local_polarity", 5, out _hvNailCenterModelID);

            HOperatorSet.WriteShapeModel(_hvNailCenterModelID, _measureParam.TemplateModelPath);

            hoCircle.Dispose();
            hoCircleXld.Dispose();
            hoEmptyImage.Dispose();

            return 0;
        }

        /// <summary>
        /// 创建halcon模板
        /// </summary>
        /// <param name="centerX">模板区域的中心X坐标</param>
        /// <param name="centerY">模板区域的中心Y坐标</param>
        /// <param name="radius">模板区域半径</param>
        /// <returns>状态标志</returns>
        public int CreateHalconTemplate(double centerX, double centerY, double radius)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject? hoTmp = null;

                HObject? hoPaintHeightImage = null;
                HObject? hoCircle = null;
                HObject? hoTmpHeightImage = MatToHObject(_measureResult.HeightImage);

                try
                {
                    //HOperatorSet.PaintRegion(_measureResult.HoIrregularMask, hoTmpHeightImage, out hoPaintHeightImage, _measureResult.MinDepth, "fill");
                    HOperatorSet.ReduceDomain(hoTmpHeightImage, _measureResult.HoValidMask, out hoPaintHeightImage);
                    HOperatorSet.ExpandDomainGray(hoPaintHeightImage, out hoTmp, radius);
                    ReplaceHobject(ref hoPaintHeightImage, ref hoTmp);

                    HOperatorSet.ScaleImageMax(hoPaintHeightImage, out hoTmp);
                    ReplaceHobject(ref hoPaintHeightImage, ref hoTmp);
                    HOperatorSet.MeanImage(hoPaintHeightImage, out hoTmp, 25, 25);
                    ReplaceHobject(ref hoPaintHeightImage, ref hoTmp);

                    HOperatorSet.GenCircle(out hoCircle, centerY, centerX, radius);
                    HOperatorSet.ReduceDomain(hoPaintHeightImage, hoCircle, out hoTmp);
                    ReplaceHobject(ref hoPaintHeightImage, ref hoTmp);

                    HOperatorSet.ClearNccModel(_hvNailCenterModelID);
                    HOperatorSet.CreateNccModel(hoPaintHeightImage, "auto", -0.39, 0.79, "auto", "use_polarity", out _hvNailCenterModelID);
                    HOperatorSet.WriteNccModel(_hvNailCenterModelID, _measureParam.TemplateModelPath);

                    return 0;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"{DateTime.Now}:CreateHalconTemplate报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                    Console.WriteLine(ex.StackTrace);

                    return -1;
                }
                finally
                {
                    hoTmp?.Dispose();
                    hoPaintHeightImage?.Dispose();
                    hoCircle?.Dispose();
                    hoTmpHeightImage?.Dispose();
                }

                
            }
                
        }



        /// <summary>
        /// 获取高度图有效值区域
        /// </summary>
        public int GetDepthValidMask(HObject hoHeightImage)
        {
            using (var dh = new HDevDisposeHelper())
            {
                int state = -1;

                HObject hoValidMask;
                HObject hoRectangle;
                HObject hoIrregularRegion;
                HObject hoIrregularRegion0;
                HObject hoIrregularRegion1;
                HObject hoIrregularRegion2;
                HObject hoIrregularMask;

                HOperatorSet.GenEmptyObj(out hoValidMask);
                HOperatorSet.GenEmptyObj(out hoRectangle);
                HOperatorSet.GenEmptyObj(out hoIrregularRegion);
                HOperatorSet.GenEmptyObj(out hoIrregularRegion0);
                HOperatorSet.GenEmptyObj(out hoIrregularRegion1);
                HOperatorSet.GenEmptyObj(out hoIrregularRegion2);
                HOperatorSet.GenEmptyObj(out hoIrregularMask);

                HTuple hvWidth = new HTuple();
                HTuple hvHeight = new HTuple();
                HTuple hvRange = new HTuple();
                HTuple hvHeightImageGlobalMinValue = new HTuple();
                HTuple hvHeightImageGlobalMaxValue = new HTuple();

                try
                {
                    HOperatorSet.GetImageSize(hoHeightImage, out hvWidth, out hvHeight);
                    HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, hvHeight, hvWidth);

                    HOperatorSet.Threshold(hoHeightImage, out hoValidMask, _measureParam.MinDepth, _measureParam.MaxDepth);

                    HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion0, 8888880, 8888880);
                    HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion0, out hoIrregularRegion);
                    HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion1, -2147483648, -2147483648);
                    HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion1, out hoIrregularRegion);
                    HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion2, 0, 0);
                    HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion2, out hoIrregularRegion);
                    HOperatorSet.Union1(hoIrregularRegion, out hoIrregularMask);
                    HOperatorSet.Difference(hoRectangle, hoIrregularMask, out hoRectangle);
                    HOperatorSet.Intersection(hoValidMask, hoRectangle, out hoValidMask);

                    HOperatorSet.MinMaxGray(hoValidMask, hoHeightImage, 0, out hvHeightImageGlobalMinValue, out hvHeightImageGlobalMaxValue, out hvRange);

                    _hoValidMask.Dispose();
                    _hoIrregularMask.Dispose();
                    _hoValidMask = hoValidMask.Clone();
                    _hoIrregularMask = hoIrregularMask.Clone();

                    _hvHeightImageGlobalMinValue.Dispose();
                    _hvHeightImageGlobalMaxValue.Dispose();
                    _hvHeightImageGlobalMinValue = hvHeightImageGlobalMinValue.Clone();
                    _hvHeightImageGlobalMaxValue = hvHeightImageGlobalMaxValue.Clone();

                    _measureResult.MinDepth = _hvHeightImageGlobalMinValue.D;
                    _measureResult.MaxDepth = _hvHeightImageGlobalMaxValue.D;

                    _measureResult.HoValidMask.Dispose();
                    _measureResult.HoIrregularMask.Dispose();
                    _measureResult.HoValidMask = _hoValidMask.Clone();
                    _measureResult.HoIrregularMask = _hoIrregularMask.Clone();

                    state = 0;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"{DateTime.Now}:GetDepthValidMask()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    hvWidth.Dispose();
                    hvHeight.Dispose();
                    hvRange.Dispose();
                    hvHeightImageGlobalMinValue.Dispose();
                    hvHeightImageGlobalMaxValue.Dispose();

                    hoValidMask.Dispose();
                    hoRectangle.Dispose();
                    hoIrregularRegion.Dispose();
                    hoIrregularRegion0.Dispose();
                    hoIrregularRegion1.Dispose();
                    hoIrregularRegion2.Dispose();
                    hoIrregularMask.Dispose();
                }

                return state;
            }
        }

        /// <summary>
        /// 获取高度图有效值区域
        /// </summary>
        public HObject GetLocalDepthValidMask(HObject hoHeightImage)
        {
            using (var dh = new HDevDisposeHelper())
            {
                HObject hoValidMask;
                HObject hoRectangle;
                HObject hoIrregularRegion;
                HObject hoIrregularRegion0;
                HObject hoIrregularRegion1;
                HObject hoIrregularRegion2;
                HObject hoIrregularMask;
                HObject hoResult;

                HOperatorSet.GenEmptyObj(out hoValidMask);
                HOperatorSet.GenEmptyObj(out hoRectangle);
                HOperatorSet.GenEmptyObj(out hoIrregularRegion);
                HOperatorSet.GenEmptyObj(out hoIrregularRegion0);
                HOperatorSet.GenEmptyObj(out hoIrregularRegion1);
                HOperatorSet.GenEmptyObj(out hoIrregularRegion2);
                HOperatorSet.GenEmptyObj(out hoIrregularMask);
                HOperatorSet.GenEmptyObj(out hoResult);

                HTuple hvWidth = new HTuple();
                HTuple hvHeight = new HTuple();

                try
                {
                    HOperatorSet.GetImageSize(hoHeightImage, out hvWidth, out hvHeight);
                    HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, hvHeight, hvWidth);

                    HOperatorSet.Threshold(hoHeightImage, out hoValidMask, _measureParam.MinDepth, _measureParam.MaxDepth);

                    HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion0, 8888880, 8888880);
                    HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion0, out hoIrregularRegion);
                    HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion1, -2147483648, -2147483648);
                    HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion1, out hoIrregularRegion);
                    HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion2, 0, 0);
                    HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion2, out hoIrregularRegion);
                    HOperatorSet.Union1(hoIrregularRegion, out hoIrregularMask);
                    HOperatorSet.Difference(hoRectangle, hoIrregularMask, out hoRectangle);
                    HOperatorSet.Intersection(hoValidMask, hoRectangle, out hoValidMask);

                    hoResult.Dispose();
                    hoResult = hoValidMask.Clone();
                }
                catch (Exception ex)
                {
                    Logs.LogError($"{DateTime.Now}:GetLocalDepthValidMask()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    hvWidth.Dispose();
                    hvHeight.Dispose();

                    hoValidMask.Dispose();
                    hoRectangle.Dispose();
                    hoIrregularRegion.Dispose();
                    hoIrregularRegion0.Dispose();
                    hoIrregularRegion1.Dispose();
                    hoIrregularRegion2.Dispose();
                    hoIrregularMask.Dispose();
                }

                return hoResult;
            }
        }

        /// <summary>
        /// 拼接图片
        /// </summary>
        public int ConcateImages(out HObject hoTileGrayImage, out HObject hoTileHeightImage)
        {
            using (var dh = new HDevDisposeHelper())
            {
                int state = -1;
                int imageNum = _imageData.Count;

                HOperatorSet.GenEmptyObj(out hoTileGrayImage);
                HOperatorSet.GenEmptyObj(out hoTileHeightImage);

                HObject hoGrayImages;
                HObject hoHeightImages;
                HObject hoTileGrayImageTmp;
                HObject hoTileHeightImageTmp;
                HObject hoZoomGrayImage;
                HObject hoZoomHeightImage;

                HObject hoImageRegions;
                HObject hoOneImageRegion;
                HObject hoCoveredRegion;
                HObject hoCanvasRegion;
                HObject hoNonImageRegion;
                HObject hoPaintHeightImage;

                HOperatorSet.GenEmptyObj(out hoGrayImages);
                HOperatorSet.GenEmptyObj(out hoHeightImages);
                HOperatorSet.GenEmptyObj(out hoTileGrayImageTmp);
                HOperatorSet.GenEmptyObj(out hoTileHeightImageTmp);
                HOperatorSet.GenEmptyObj(out hoZoomGrayImage);
                HOperatorSet.GenEmptyObj(out hoZoomHeightImage);

                HOperatorSet.GenEmptyObj(out hoImageRegions);
                HOperatorSet.GenEmptyObj(out hoOneImageRegion);
                HOperatorSet.GenEmptyObj(out hoCoveredRegion);
                HOperatorSet.GenEmptyObj(out hoCanvasRegion);
                HOperatorSet.GenEmptyObj(out hoNonImageRegion);
                HOperatorSet.GenEmptyObj(out hoPaintHeightImage);

                HTuple hvOffsetRows = new HTuple();
                HTuple hvOffsetCols = new HTuple();
                HTuple hvTmpRow1 = new HTuple();
                HTuple hvTmpCol1 = new HTuple();
                HTuple hvTmpRow2 = new HTuple();
                HTuple hvTmpCol2 = new HTuple();
                HTuple hvConcatW = new HTuple(0);
                HTuple hvConcatH = new HTuple(0);
                HTuple scaleX = new HTuple(1);
                HTuple scaleY = new HTuple(1);

                try
                {
                    if (imageNum <= 0)
                    {
                        Logs.LogError($"{DateTime.Now}:ConcateImages()报错信息:_imageData为空");
                        return state;
                    }

                    for (int i = 0; i < imageNum; i++)
                    {
                        HOperatorSet.ConcatObj(hoGrayImages, _imageData[i].hoGrayImage, out hoGrayImages);
                        HOperatorSet.ConcatObj(hoHeightImages, _imageData[i].hoHeightImage, out hoHeightImages);

                        HOperatorSet.TupleConcat(hvOffsetCols, _imageData[i].OffsetX, out hvOffsetCols);
                        HOperatorSet.TupleConcat(hvOffsetRows, _imageData[i].OffsetY, out hvOffsetRows);

                        HOperatorSet.TupleConcat(hvTmpCol1, -1, out hvTmpCol1);
                        HOperatorSet.TupleConcat(hvTmpRow1, -1, out hvTmpRow1);
                        HOperatorSet.TupleConcat(hvTmpCol2, -1, out hvTmpCol2);
                        HOperatorSet.TupleConcat(hvTmpRow2, -1, out hvTmpRow2);

                        HTuple hvTmpW = new HTuple();
                        HTuple hvTmpH = new HTuple();

                        try
                        {
                            HOperatorSet.GetImageSize(_imageData[i].hoGrayImage, out hvTmpW, out hvTmpH);

                            HTuple hvW = _imageData[i].OffsetX + hvTmpW;
                            HTuple hvH = _imageData[i].OffsetY + hvTmpH;
                            if (hvW > hvConcatW)
                                hvConcatW = hvW;
                            if (hvH > hvConcatH)
                                hvConcatH = hvH;
                        }
                        finally
                        {
                            hvTmpW.Dispose();
                            hvTmpH.Dispose();
                        }
                    }

                    HOperatorSet.TileImagesOffset(hoGrayImages, out hoTileGrayImageTmp, hvOffsetRows, hvOffsetCols,
                                                  hvTmpRow1, hvTmpCol1, hvTmpRow2, hvTmpCol2, hvConcatW, hvConcatH);
                    HOperatorSet.TileImagesOffset(hoHeightImages, out hoTileHeightImageTmp, hvOffsetRows, hvOffsetCols,
                                                  hvTmpRow1, hvTmpCol1, hvTmpRow2, hvTmpCol2, hvConcatW, hvConcatH);

                    for (int i = 0; i < imageNum; i++)
                    {
                        HOperatorSet.GetImageSize(_imageData[i].hoHeightImage, out HTuple hvW, out HTuple hvH);
                        HOperatorSet.GenRectangle1(out hoOneImageRegion, _imageData[i].OffsetY, _imageData[i].OffsetX,
                                                   _imageData[i].OffsetY + hvH - 1, _imageData[i].OffsetX + hvW - 1);
                        HOperatorSet.ConcatObj(hoImageRegions, hoOneImageRegion, out hoImageRegions);

                        hvW.Dispose();
                        hvH.Dispose();
                    }

                    HOperatorSet.Union1(hoImageRegions, out hoCoveredRegion);
                    HOperatorSet.GenRectangle1(out hoCanvasRegion, 0, 0, hvConcatH - 1, hvConcatW - 1);
                    HOperatorSet.Difference(hoCanvasRegion, hoCoveredRegion, out hoNonImageRegion);
                    HOperatorSet.PaintRegion(hoNonImageRegion, hoTileHeightImageTmp, out hoPaintHeightImage, 8888880, "fill");
                    hoTileHeightImageTmp.Dispose();
                    hoTileHeightImageTmp = hoPaintHeightImage.Clone();

                    if (_imageData[0].hvIntervalX > _imageData[0].hvIntervalY)
                    {
                        scaleX = _imageData[0].hvIntervalX / _imageData[0].hvIntervalY;
                        scaleY = 1;
                    }
                    else
                    {
                        scaleX = 1;
                        scaleY = _imageData[0].hvIntervalY / _imageData[0].hvIntervalX;
                    }

                    HOperatorSet.ZoomImageFactor(hoTileGrayImageTmp, out hoZoomGrayImage, scaleX, scaleY, "constant");
                    HOperatorSet.ZoomImageFactor(hoTileHeightImageTmp, out hoZoomHeightImage, scaleX, scaleY, "nearest_neighbor");

                    hoTileGrayImage.Dispose();
                    hoTileHeightImage.Dispose();
                    hoTileGrayImage = hoZoomGrayImage.Clone();
                    hoTileHeightImage = hoZoomHeightImage.Clone();

                    if (GetDepthValidMask(hoTileHeightImage) != 0)
                        return state;

                    state = 0;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"{DateTime.Now}:ConcateImages()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    hvOffsetRows.Dispose();
                    hvOffsetCols.Dispose();
                    hvTmpRow1.Dispose();
                    hvTmpCol1.Dispose();
                    hvTmpRow2.Dispose();
                    hvTmpCol2.Dispose();
                    hvConcatW.Dispose();
                    hvConcatH.Dispose();
                    scaleX.Dispose();
                    scaleY.Dispose();

                    hoGrayImages.Dispose();
                    hoHeightImages.Dispose();
                    hoTileGrayImageTmp.Dispose();
                    hoTileHeightImageTmp.Dispose();
                    hoZoomGrayImage.Dispose();
                    hoZoomHeightImage.Dispose();

                    hoImageRegions.Dispose();
                    hoOneImageRegion.Dispose();
                    hoCoveredRegion.Dispose();
                    hoCanvasRegion.Dispose();
                    hoNonImageRegion.Dispose();
                    hoPaintHeightImage.Dispose();
                }

                return state;
            }
        }


        /// <summary>
        /// 标注的类别
        /// </summary>
        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// 特征算法库
        /// </summary>
        public class FeatureAlgorithm
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public BsonDocument Parameters { get; set; }
        }

        public class Locator
        {
            public string Name { get; set; } = "密封钉定位";
            public string AlgName { get; set; } = "GetNailCenterAndOrbitMaskV2";
            public BsonDocument AlgParam { get; set; } = new BsonDocument();
        }

        /// <summary>
        /// 检出的缺陷类别与对应特征算法
        /// </summary>
        public class Defect
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string AlgName { get; set; }
            public BsonDocument AlgParam { get; set; }
        }



        /// <summary>
        /// 翘钉检测
        /// </summary>
        public WarpResult GetWarpFeatureWrapper(HObject hoTileGrayImage, HObject hoTileHeightImage, HTuple hvCx, HTuple hvCy, HTuple hvOrbitParam)
        {
            WarpResult result = new WarpResult();

            try
            {
                var defect = _defectList.FirstOrDefault(d => d.Name == "翘钉");
                if (defect == null)
                {
                    Console.WriteLine("ERROR:请在FeatureConfig.json配置文件的'defect_list'中配置翘钉检测算法。");
                    result.IsOk = false;
                    return result;
                }

                var algorithm = _algorithmList.FirstOrDefault(a => a.Name == defect.AlgName);
                if (algorithm == null)
                {
                    Console.WriteLine("ERROR:请在FeatureConfig.json配置文件的'algorithm_list'中申明翘钉检测算法。");
                    result.IsOk = false;
                    return result;
                }

                // 合并参数（缺陷参数覆盖算法默认参数）
                var mergedParams = algorithm.Parameters.DeepClone().AsBsonDocument;
                foreach (var param in defect.AlgParam)
                {
                    mergedParams[param.Name] = param.Value;
                }

                // 转换为字典方便反射调用
                var paramDict = mergedParams.ToDictionary(p => p.Name, p => BsonTypeMapper.MapToDotNetValue(p.Value));

                // 反射调用对应方法
                MethodInfo? method = typeof(FeatureAlgorithms).GetMethod(defect.AlgName);
                if (method == null)
                {
                    Console.WriteLine($"GetWarpFeatureWrapper()未找到方法：{defect.AlgName}");
                    result.IsOk = false;
                    return result;
                }

                ParameterInfo[] parameters = method.GetParameters();
                List<object> validArgs = new List<object>();

                foreach (var param in parameters)
                {
                    if (paramDict.TryGetValue(param.Name, out var value))
                    {
                        try
                        {
                            object convertedValue = Convert.ChangeType(value, param.ParameterType);
                            validArgs.Add(convertedValue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"参数 {param.Name} 转换失败: {ex.Message}");
                            validArgs.Add(param.DefaultValue ?? Activator.CreateInstance(param.ParameterType));
                        }
                    }
                    else if (param.Name == "hoTileGrayImage")
                    {
                        validArgs.Add(hoTileGrayImage);
                    }
                    else if (param.Name == "hoTileHeightImage")
                    {
                        validArgs.Add(hoTileHeightImage);
                    }
                    else if (param.Name == "hvCx")
                    {
                        validArgs.Add(hvCx);
                    }
                    else if (param.Name == "hvCy")
                    {
                        validArgs.Add(hvCy);
                    }
                    else if (param.Name == "hvOrbitParam")
                    {
                        validArgs.Add(hvOrbitParam);
                    }
                    else if (param.Name == "hoValidMask")
                    {
                        validArgs.Add(_hoValidMask);
                    }
                    else if (param.Name == "hoNailWarpBaseMask")
                    {
                        validArgs.Add(_hoNailWarpBaseMask);
                    }
                    else if (param.Name == "measureParam")
                    {
                        validArgs.Add(_measureParam);
                    }
                    else
                    {
                        Console.WriteLine($"缺少必要参数：{param.Name}");
                        validArgs.Add(param.DefaultValue ?? Activator.CreateInstance(param.ParameterType));
                    }
                }

                object invokeResult = method.Invoke(null, validArgs.ToArray());
                if (invokeResult is WarpResult warpResult)
                {
                    result = warpResult;
                }
                else
                {
                    Console.WriteLine($"GetWarpFeatureWrapper()返回结果类型错误：{defect.AlgName}");
                    result.IsOk = false;
                }
            }
            catch (TargetInvocationException ex)
            {
                Exception innerEx = ex.InnerException ?? ex;
                Logs.LogError($"{DateTime.Now}:GetWarpFeatureWrapper()报错信息:{innerEx.Message},调用堆栈:{innerEx.StackTrace}");
                Console.WriteLine(innerEx.StackTrace);
                result.IsOk = false;
            }
            catch (Exception ex)
            {
                Logs.LogError($"{DateTime.Now}:GetWarpFeatureWrapper()报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                Console.WriteLine(ex.StackTrace);
                result.IsOk = false;
            }

            return result;
        }

        /// <summary>
        /// 轨迹偏移
        /// </summary>
        public OrbitResult GetOrbitFeatureWrapper(HTuple hvCx, HTuple hvCy, HTuple hvOrbitParam)
        {
            OrbitResult result = new OrbitResult();

            var defect = _defectList.FirstOrDefault(d => d.Name == "轨迹偏移");
            if (defect == null)
            {
                Console.WriteLine("ERROR:请在FeatureConfig.json配置文件的'defect_list'中配置轨迹偏移测量算法。");
                return result;
            }

            var algorithm = _algorithmList.FirstOrDefault(a => a.Name == defect.AlgName);
            if (algorithm == null)
            {
                Console.WriteLine("ERROR:请在FeatureConfig.json配置文件的'algorithm_list'中申明轨迹偏移测量算法。");
                return result;
            }

            // 合并参数（缺陷参数覆盖算法默认参数）
            var mergedParams = algorithm.Parameters.DeepClone().AsBsonDocument;
            foreach (var param in defect.AlgParam)
            {
                mergedParams[param.Name] = param.Value;
            }

            // 转换为字典方便反射调用
            var paramDict = mergedParams.ToDictionary(p => p.Name, p => BsonTypeMapper.MapToDotNetValue(p.Value));

            // 反射调用对应方法
            MethodInfo? method = typeof(FeatureAlgorithms).GetMethod(defect.AlgName);
            if (method == null)
            {
                Console.WriteLine($"GetOrbitFeatureWrapper()未找到方法：{defect.AlgName}");
                return result;
            }

            ParameterInfo[] parameters = method.GetParameters();
            List<object> validArgs = new List<object>();

            foreach (var param in parameters)
            {
                if (paramDict.TryGetValue(param.Name, out var value))
                {
                    try
                    {
                        object convertedValue = Convert.ChangeType(value, param.ParameterType);
                        validArgs.Add(convertedValue);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"参数 {param.Name} 转换失败: {ex.Message}");
                        validArgs.Add(param.DefaultValue ?? Activator.CreateInstance(param.ParameterType));
                    }
                }
                else if (param.Name == "hvCx")
                {
                    validArgs.Add(hvCx);
                }
                else if (param.Name == "hvCy")
                {
                    validArgs.Add(hvCy);
                }
                else if (param.Name == "hvOrbitParam")
                {
                    validArgs.Add(hvOrbitParam);
                }
                else if (param.Name == "measureParam")
                {
                    validArgs.Add(_measureParam);
                }
                else if (param.Name == "nailCenterIsTrue")
                {
                    validArgs.Add(_nailCenterIsTrue);
                }
                else
                {
                    Console.WriteLine($"缺少必要参数：{param.Name}");
                    validArgs.Add(param.DefaultValue ?? Activator.CreateInstance(param.ParameterType));
                }
            }

            result = (OrbitResult)method.Invoke(null, validArgs.ToArray());

            return result;
        }


        /// <summary>
        /// 缺陷特征计算
        /// </summary>
        public DefectResult GetDefectFeatureWrapper(string defectName, ImageData imageData, int bboxId)
        {
            DefectResult result = new DefectResult();

            var defect = _defectList.FirstOrDefault(d => d.Name == defectName);
            if (defect == null)
            {
                Console.WriteLine($"请在FeatureConfig.json配置文件的'defect_list'中配置{defectName}算法。");
                return result;
            }

            var algorithm = _algorithmList.FirstOrDefault(a => a.Name == defect.AlgName);
            if (algorithm == null)
            {
                Console.WriteLine($"请在FeatureConfig.json配置文件的'algorithm_list'中申明{defectName}算法。");
                return result;
            }

            // 合并参数（缺陷参数覆盖算法默认参数）
            var mergedParams = algorithm.Parameters.DeepClone().AsBsonDocument;
            foreach (var param in defect.AlgParam)
            {
                mergedParams[param.Name] = param.Value;
            }

            // 转换为字典方便反射调用
            var paramDict = mergedParams.ToDictionary(p => p.Name, p => BsonTypeMapper.MapToDotNetValue(p.Value));

            // 反射调用对应方法
            MethodInfo? method = typeof(FeatureAlgorithms).GetMethod(defect.AlgName);
            if (method == null)
            {
                Console.WriteLine($"GetWarpFeatureWrapper()未找到方法：{defect.AlgName}");
                return result;
            }

            ParameterInfo[] parameters = method.GetParameters();
            List<object> validArgs = new List<object>();

            foreach (var param in parameters)
            {
                if (paramDict.TryGetValue(param.Name, out var value))
                {
                    try
                    {
                        object convertedValue = Convert.ChangeType(value, param.ParameterType);
                        validArgs.Add(convertedValue);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"参数 {param.Name} 转换失败: {ex.Message}");
                        validArgs.Add(param.DefaultValue ?? Activator.CreateInstance(param.ParameterType));
                    }
                }
                else if (param.Name == "bboxId")
                {
                    validArgs.Add(bboxId);
                }
                else if (param.Name == "imageData")
                {
                    validArgs.Add(imageData);
                }
                else if (param.Name == "hoOrbitMask")
                {
                    validArgs.Add(_hoOrbitMask);
                }
                else if (param.Name == "hoNailBaseMask")
                {
                    validArgs.Add(_hoNailWarpBaseMask);
                }
                else if (param.Name == "hoValidMask")
                {
                    validArgs.Add(_hoValidMask);
                }
                else if (param.Name == "measureParam")
                {
                    validArgs.Add(_measureParam);
                }
                else
                {
                    Console.WriteLine($"缺少必要参数：{param.Name}");
                    validArgs.Add(param.DefaultValue ?? Activator.CreateInstance(param.ParameterType));
                }
            }

            result = (DefectResult)method.Invoke(null, validArgs.ToArray());

            return result;
        }


        /// <summary>
        /// 使用halcon进行模型输出结果的特征计算
        /// </summary>
        public int GetFeature()
        {
            using (var dh = new HDevDisposeHelper())
            {
                int state = -1;

                HObject hoTileGrayImage, hoTileHeightImage;
                HOperatorSet.GenEmptyObj(out hoTileGrayImage);
                HOperatorSet.GenEmptyObj(out hoTileHeightImage);

                Mat cvGrayImage = new Mat();
                Mat cvHeightImage = new Mat();
                bool grayImageAssigned = false;
                bool heightImageAssigned = false;

                try
                {
                    _measureResult.Warp = new WarpResult();
                    _measureResult.Orbit = new OrbitResult();
                    _measureResult.Defects.Clear();
                    _measureResult.MinDepth = Single.NegativeInfinity;
                    _measureResult.MaxDepth = Single.NegativeInfinity;

                    if (_measureResult.GrayImage.Data != IntPtr.Zero)
                        _measureResult.GrayImage.Dispose();
                    _measureResult.GrayImage = new Mat();

                    if (_measureResult.HeightImage.Data != IntPtr.Zero)
                        _measureResult.HeightImage.Dispose();
                    _measureResult.HeightImage = new Mat();

                    _measureResult.HoValidMask.Dispose();
                    _measureResult.HoIrregularMask.Dispose();
                    HOperatorSet.GenEmptyObj(out HObject hoEmptyValidMask);
                    HOperatorSet.GenEmptyObj(out HObject hoEmptyIrregularMask);
                    _measureResult.HoValidMask = hoEmptyValidMask;
                    _measureResult.HoIrregularMask = hoEmptyIrregularMask;

                    int imageNum = _imageData.Count;
                    if (imageNum <= 0)
                    {
                        Logs.LogError($"{DateTime.Now}:GetFeature()报错信息:_imageData为空");
                        return state;
                    }

                    double minOffsetX = _imageData.Min(p => p.OffsetX);
                    double minOffsetY = _imageData.Min(p => p.OffsetY);

                    if (minOffsetX < 0)
                    {
                        for (int i = 0; i < imageNum; i++)
                        {
                            _imageData[i].OffsetX -= minOffsetX;
                        }
                    }
                    if (minOffsetY < 0)
                    {
                        for (int i = 0; i < imageNum; i++)
                        {
                            _imageData[i].OffsetY -= minOffsetY;
                        }
                    }

                    double maxH = _imageData.Max(p => p.hvImageHeight.D);
                    double minH = _imageData.Min(p => p.hvImageHeight.D);
                    _expendPixels = _imageData.Max(p => Math.Abs(p.OffsetY)) + Math.Abs(maxH - minH);

                    // 拼接图片
                    int concateState = ConcateImages(out hoTileGrayImage, out hoTileHeightImage);

                    if (hoTileGrayImage.IsInitialized() && hoTileGrayImage.CountObj() > 0)
                    {
                        cvGrayImage = HobjectToMat(hoTileGrayImage, ImageType.Gray);
                        _measureResult.GrayImage.Dispose();
                        _measureResult.GrayImage = cvGrayImage;
                        grayImageAssigned = true;
                    }

                    if (hoTileHeightImage.IsInitialized() && hoTileHeightImage.CountObj() > 0)
                    {
                        cvHeightImage = HobjectToMat(hoTileHeightImage, ImageType.Depth);
                        _measureResult.HeightImage.Dispose();
                        _measureResult.HeightImage = cvHeightImage;
                        heightImageAssigned = true;
                    }

                    if (concateState != 0)
                        return state;
                    // 定位焊钉中心和焊迹的掩码
                    HTuple hvCx, hvCy;
                    HTuple hvOrbitParam;

                    int nailState = GetNailCenterAndOrbitMaskWrapper(hoTileGrayImage, hoTileHeightImage, out hvCx, out hvCy, out hvOrbitParam);

                    if (nailState != 0)
                        return state;

                    // 翘钉检测
                    WarpResult warpResult = GetWarpFeatureWrapper(hoTileGrayImage, hoTileHeightImage, hvCx, hvCy, hvOrbitParam);
                    _measureResult.Warp = warpResult;

                    // 轨迹偏移
                    OrbitResult orbitResult = GetOrbitFeatureWrapper(hvCx, hvCy, hvOrbitParam);
                    _measureResult.Orbit = orbitResult;

                    // 缺陷特征计算
                    int defectCount = 0;

                    for (int i = 0; i < imageNum; i++)
                    {
                        ImageData imageData = _imageData[i];
                        int defectNum = imageData.Boxes.Count;

                        for (int idx = 0; idx < defectNum; idx++)
                        {
                            int defectClassId = imageData.Boxes[idx].ClassId;

                            string defectName = _categories[defectClassId];
                            DefectResult defectResult = GetDefectFeatureWrapper(defectName, imageData, idx);

                            if (!defectResult.IsOk)
                            {
                                defectResult.Categories = _categories;
                                defectResult.InstanceId = defectCount;
                                defectCount += 1;
                                _measureResult.Defects.Add(defectResult);
                            }
                        }
                    }

                    if (!_measureResult.Warp.IsOk)
                    {
                        DefectResult defectResult = new DefectResult();

                        defectResult.ClassId = _segCategoryNum;
                        defectResult.DepthFeature = _measureResult.Warp.Height;
                        defectResult.IsOk = _measureResult.Warp.IsOk;

                        defectResult.Categories = _categories;
                        defectResult.InstanceId = defectCount;
                        defectCount += 1;

                        _measureResult.Defects.Add(defectResult);
                    }

                    if (!_measureResult.Orbit.IsOk)
                    {
                        DefectResult defectResult = new DefectResult();

                        defectResult.ClassId = _segCategoryNum + 1;
                        defectResult.LengthFeature = _measureResult.Orbit.Offset;
                        defectResult.IsOk = _measureResult.Orbit.IsOk;

                        defectResult.Categories = _categories;
                        defectResult.InstanceId = defectCount;
                        defectCount += 1;

                        _measureResult.Defects.Add(defectResult);
                    }

                    state = 0;
                }
                catch (Exception ex)
                {
                    Logs.LogError($"{DateTime.Now}:报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    hoTileGrayImage.Dispose();
                    hoTileHeightImage.Dispose();

                    if (!grayImageAssigned && cvGrayImage.Data != IntPtr.Zero)
                        cvGrayImage.Dispose();
                    if (!heightImageAssigned && cvHeightImage.Data != IntPtr.Zero)
                        cvHeightImage.Dispose();
                }

                return state;
            }
        }


        /// <summary>
        /// 组装测量结果objInfo, objectNum
        /// </summary>
        private int PourObjectInfo(IntPtr objInfo, int objectNum, IntPtr inGrayImageData, int inGw, int inGh, int inGc, int inGtype,
                                   IntPtr inHeightImageData, int inDw, int inDh, int inDc, int inDtype, out ImageData imageData)
        {
            imageData = new ImageData();

            HOperatorSet.GenImage1(out HObject hoGrayImage, "byte", inGw, inGh, inGrayImageData);
            HOperatorSet.GenImage1(out HObject hoHeightImage, "real", inDw, inDh, inHeightImageData);

            // 将深度图Z方向的像素当量统一缩放到0.1μm
            HOperatorSet.ScaleImage(hoHeightImage, out hoHeightImage, _measureParam.IntervalZ * 10, 0);
            _measureParam.MinDepth = _measureParam.MinDepth * _measureParam.IntervalZ * 10;
            _measureParam.MaxDepth = _measureParam.MaxDepth * _measureParam.IntervalZ * 10;
            _measureParam.IntervalZ = _measureParam.IntervalZ / (_measureParam.IntervalZ * 10);

            imageData.hoGrayImage = hoGrayImage;
            imageData.hoHeightImage = hoHeightImage;

            imageData.hvImageWidth = inGw;
            imageData.hvImageHeight = inGh;

            imageData.hoValidMask = GetLocalDepthValidMask(hoHeightImage);

            imageData.hvIntervalX = _measureParam.IntervalX;
            imageData.hvIntervalY = _measureParam.IntervalY;
            imageData.hvIntervalZ = _measureParam.IntervalZ;
            imageData.OffsetX = _measureParam.OffsetX / _measureParam.IntervalX;
            imageData.OffsetY = _measureParam.OffsetY / _measureParam.IntervalY;

            List<Box> boxes = new List<Box>();
            int NativeResultSize = Marshal.SizeOf<NativeResult>();
            for (int i = 0; i < objectNum; i++)
            {
                IntPtr currentBoxPtr = IntPtr.Add(objInfo, i * NativeResultSize);
                NativeResult nativeBox = Marshal.PtrToStructure<NativeResult>(currentBoxPtr);

                Segment seg = new Segment();
                if (nativeBox.Segmentation.Width > 0 && nativeBox.Segmentation.Height > 0)
                {
                    seg.Valid = 1;
                    seg.Width = nativeBox.Segmentation.Width;
                    seg.Height = nativeBox.Segmentation.Height;
                    seg.Thresh = nativeBox.Segmentation.Thresh;
                    seg.AffineMatrix = (float[])nativeBox.Segmentation.AffineMatrix.Clone();
                    seg.Data = new float[nativeBox.Segmentation.Width * nativeBox.Segmentation.Height];
                }
                else
                {
                    seg.Valid = 0;
                    seg.Width = 0;
                    seg.Height = 0;
                    seg.Thresh = 0;
                }

                Box box = new Box();
                box.Left = (float)(nativeBox.Cx - 0.5 * nativeBox.Width);
                box.Top = (float)(nativeBox.Cy - 0.5 * nativeBox.Height);
                box.Right = (float)(nativeBox.Cx + 0.5 * nativeBox.Width);
                box.Bottom = (float)(nativeBox.Cy + 0.5 * nativeBox.Height);
                box.Confidence = nativeBox.Confidence;
                box.ClassId = nativeBox.ClassId;
                box.Seg = seg;

                if (nativeBox.Segmentation.FloatData != IntPtr.Zero && box.Seg.Data.Length > 0)
                {
                    Marshal.Copy(nativeBox.Segmentation.FloatData, box.Seg.Data, 0, box.Seg.Data.Length);
                }

                box.InstanceId = i;

                boxes.Add(box);
            }

            imageData.Boxes = boxes;
            _imageData.Add(imageData);



            return 0;
        }


        /// <summary>
        /// 获取测量结果
        /// </summary>
        public MFDJC0_MeasureResult GetMeasureResult()
        {
            return _measureResult;
        }

        /// <summary>
        /// 获取传感器原始图片
        /// </summary>
        public List<ImageData> GetImageData()
        {
            return _imageData;
        }


        /// <summary>
        /// 测量过程
        /// </summary>
        /// <param name="grayDate">输入的灰度图数据</param>
        /// <param name="heightData">输入深度图数据</param>
        /// <param name="param">测量配置参数</param>
        /// <returns>result</returns>
        public int Process(List<float[]> grayDate, List<float[]> heightData, MFDJC0_MeasureParam param)
        {
            _disposed = false;

            Mat grayImage = new Mat();
            Mat heightImage = new Mat();
            Mat legacyHeightImage = new Mat();

            IntPtr objInfo = IntPtr.Zero;

            try
            {
                _measureParam = param.DeepCopy();

                
                int objectNum = 0;

                IntPtr grayImagePtr, heightImagePtr, legacyHeightImagePtr;

                // 将C#数组格式的图片数据转为OpenCvSharp Mat对象
                List<float[]> legacyHeightData = EVEMFDJCLegacyDepthHelper.CreateLegacyCompatibleDepthData(heightData);
                int statusGrayDate, statusHeightData, statusLegacyHeightData;
                statusGrayDate = ConvertListToMat(grayDate, ImageType.Gray, out grayImage);
                statusHeightData = ConvertListToMat(heightData, ImageType.Depth, out heightImage);
                statusLegacyHeightData = ConvertListToMat(legacyHeightData, ImageType.Depth, out legacyHeightImage);

                if (statusGrayDate == 0 && statusHeightData == 0 && statusLegacyHeightData == 0)
                {
                    Cv2.Flip(grayImage, grayImage, FlipMode.Y);
                    Cv2.Flip(heightImage, heightImage, FlipMode.Y);
                    Cv2.Flip(legacyHeightImage, legacyHeightImage, FlipMode.Y);

                    if (_measureParam.IsFlip)
                    {
                        Cv2.Flip(grayImage, grayImage, FlipMode.X);
                        Cv2.Flip(heightImage, heightImage, FlipMode.X);
                        Cv2.Flip(legacyHeightImage, legacyHeightImage, FlipMode.X);
                    }

                    int inGw = grayImage.Cols;
                    int inGh = grayImage.Rows;
                    int inGc = grayImage.Channels();
                    grayImagePtr = grayImage.Data;

                    int inDw = heightImage.Cols;
                    int inDh = heightImage.Rows;
                    int inDc = heightImage.Channels();
                    heightImagePtr = heightImage.Data;
                    legacyHeightImagePtr = legacyHeightImage.Data;

                    int inGtype = (int)grayImage.Type();
                    int inDtype = (int)heightImage.Type();
                    int legacyDtype = (int)legacyHeightImage.Type();

                    int state = SealingNailsSDK.Pipeline(_deepLearningHandle,
                                                         grayImagePtr, inGw, inGh, inGc, inGtype,
                                                         heightImagePtr, inDw, inDh, inDc, inDtype,
                                                         out objInfo, out objectNum);
                    
                    if(state != 0)
                    {
                        return state;
                    }

                    PourObjectInfo(objInfo, objectNum,
                                       grayImagePtr, inGw, inGh, inGc, inGtype,
                                       legacyHeightImagePtr, inDw, inDh, inDc, legacyDtype,
                                       out ImageData imageData);
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"{DateTime.Now}:报错信息:{ex.Message},调用堆栈:{ex.StackTrace}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                int state = SealingNailsSDK.CleanUpResult(_deepLearningHandle, ref objInfo);

                if (grayImage.Data != IntPtr.Zero)
                    grayImage.Dispose();
                if (heightImage.Data != IntPtr.Zero)
                    heightImage.Dispose();
                if (legacyHeightImage.Data != IntPtr.Zero)
                    legacyHeightImage.Dispose();
            }

            return 0;
        }



        public Mat BuildAffineMatrix(RotationAngle rotation)
        {
            int width = _measureResult?.GrayImage?.Cols ?? 0;
            int height = _measureResult?.GrayImage?.Rows ?? 0;

            return BuildAffineMatrix(rotation, width, height, out _, out _);
        }

        private static Mat BuildAffineMatrix(RotationAngle rotation, int width, int height, out int Wout, out int Hout)
        {
            Mat M = new Mat(2, 3, MatType.CV_64FC1);

            Point2f s0, s1, s2, s3;
            s0 = new Point2f(0,          0);    // 左上
            s1 = new Point2f(width,      0);    // 右上
            s2 = new Point2f(width, height);    // 右下
            s3 = new Point2f(0,     height);    // 左下

            Point2f d0, d1, d2, d3;
            switch (rotation)
            {
                case RotationAngle.ANGLE_90:
                    Wout = height;
                    Hout = width;
                    d0 = new Point2f(height,     0);
                    d1 = new Point2f(height, width);
                    d2 = new Point2f(0,      width);
                    d3 = new Point2f(0,          0);
                    break;
                case RotationAngle.ANGLE_180:
                    Wout = width;
                    Hout = height;
                    d0 = new Point2f(width, height);
                    d1 = new Point2f(0,     height);
                    d2 = new Point2f(0,          0);
                    d3 = new Point2f(width,      0);
                    break;
                case RotationAngle.ANGLE_270:
                    Wout = height;
                    Hout = width;
                    d0 = new Point2f(0,      width);
                    d1 = new Point2f(0,          0);
                    d2 = new Point2f(height,     0);
                    d3 = new Point2f(height, width);
                    break;
                default:
                    Wout = width;
                    Hout = height;
                    d0 = new Point2f(0,          0);
                    d1 = new Point2f(width,      0);
                    d2 = new Point2f(width, height);
                    d3 = new Point2f(0,     height);
                    break;
            }
            

            return Cv2.GetAffineTransform(new[] { s0, s1, s2, s3 }, new[] { d0, d1, d2, d3 }); 
        }


        public void RotationImage(MFDJC0_MeasureResult measureResult, RotationAngle rotation)
        {
            // 根据rotation构建仿射变换矩阵
            int srcW = measureResult.GrayImage.Cols;
            int srcH = measureResult.GrayImage.Rows;
            Mat M = BuildAffineMatrix(rotation, srcW, srcH, out int Wout, out int Hout);

            Cv2.WarpAffine(measureResult.GrayImage, measureResult.GrayImage, M, new Size(Wout, Hout), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));
            Cv2.WarpAffine(measureResult.HeightImage, measureResult.HeightImage, M, new Size(Wout, Hout), InterpolationFlags.Nearest, BorderTypes.Constant, Scalar.All(8888880));

            measureResult.GrayImageOri = measureResult.GrayImage.Clone();

            double m00 = M.At<double>(0, 0);
            double m01 = M.At<double>(0, 1);
            double m02 = M.At<double>(0, 2);
            double m10 = M.At<double>(1, 0);
            double m11 = M.At<double>(1, 1);
            double m12 = M.At<double>(1, 2);

            if (measureResult.Warp.HighestPointCol != Single.NegativeInfinity &&
                measureResult.Warp.HighestPointRow != Single.NegativeInfinity)
            {
                double x = measureResult.Warp.HighestPointCol;
                double y = measureResult.Warp.HighestPointRow;
                measureResult.Warp.HighestPointCol = m00 * x + m01 * y + m02;
                measureResult.Warp.HighestPointRow = m10 * x + m11 * y + m12;
            }

            if (measureResult.Orbit.NailCenterCol != Single.NegativeInfinity &&
                measureResult.Orbit.NailCenterRow != Single.NegativeInfinity)
            {
                double x = measureResult.Orbit.NailCenterCol;
                double y = measureResult.Orbit.NailCenterRow;
                measureResult.Orbit.NailCenterCol = m00 * x + m01 * y + m02;
                measureResult.Orbit.NailCenterRow = m10 * x + m11 * y + m12;
            }

            if (measureResult.Orbit.OrbitCenterCol != Single.NegativeInfinity &&
                measureResult.Orbit.OrbitCenterRow != Single.NegativeInfinity)
            {
                double x = measureResult.Orbit.OrbitCenterCol;
                double y = measureResult.Orbit.OrbitCenterRow;
                measureResult.Orbit.OrbitCenterCol = m00 * x + m01 * y + m02;
                measureResult.Orbit.OrbitCenterRow = m10 * x + m11 * y + m12;
            }

            int warpPolygonCount = measureResult.Warp.Polygons.Count;
            for (int i = 0; i < warpPolygonCount; i++)
            {
                Polygon polygon = measureResult.Warp.Polygons[i];
                foreach (var contour in polygon.Contours)
                {
                    for (int c = 0; c < contour.Length; c++)
                    {
                        double x = contour[c].X;
                        double y = contour[c].Y;
                        double xp = m00 * x + m01 * y + m02;
                        double yp = m10 * x + m11 * y + m12;
                        contour[c].X = (int)Math.Round(xp);
                        contour[c].Y = (int)Math.Round(yp);
                    }
                }
            }

            int defectNum = measureResult.Defects.Count;
            for (int i = 0; i < defectNum; i++)
            {
                DefectResult defect = measureResult.Defects[i];

                if (defect.Left != Single.NegativeInfinity && defect.Top != Single.NegativeInfinity &&
                    defect.Right != Single.NegativeInfinity && defect.Bottom != Single.NegativeInfinity)
                {
                    double tmpLeft = m00 * defect.Left + m01 * defect.Top + m02;
                    double tmpTop = m10 * defect.Left + m11 * defect.Top + m12;
                    double tmpRight = m00 * defect.Right + m01 * defect.Bottom + m02;
                    double tmpBottom = m10 * defect.Right + m11 * defect.Bottom + m12;

                    defect.Left = Math.Min(tmpLeft, tmpRight);
                    defect.Top = Math.Min(tmpTop, tmpBottom);
                    defect.Right = Math.Max(tmpLeft, tmpRight);
                    defect.Bottom = Math.Max(tmpTop, tmpBottom);
                }

                if (defect.CenterColFeature != Single.NegativeInfinity &&
                    defect.CenterRowFeature != Single.NegativeInfinity)
                {
                    double x = defect.CenterColFeature;
                    double y = defect.CenterRowFeature;
                    defect.CenterColFeature = m00 * x + m01 * y + m02;
                    defect.CenterRowFeature = m10 * x + m11 * y + m12;
                }

                int polygonCount = defect.DefectPolygons.Count;
                for (int p = 0; p < polygonCount; p++)
                {
                    Polygon polygon = defect.DefectPolygons[p];
                    foreach (var contour in polygon.Contours)
                    {
                        for (int c = 0; c < contour.Length; c++)
                        {
                            double x = contour[c].X;
                            double y = contour[c].Y;
                            double xp = m00 * x + m01 * y + m02;
                            double yp = m10 * x + m11 * y + m12;
                            contour[c].X = (int)Math.Round(xp);
                            contour[c].Y = (int)Math.Round(yp);
                        }
                    }
                }
            }
        }



        /// <summary>
        /// 绘制结果
        /// </summary>
        public int CvDrawResult(MFDJC0_MeasureResult measureResult, bool showGuides = false)
        {
            // 根据旋转角度构建仿射变换矩阵
            if(_measureParam.Rotation != RotationAngle.ANGLE_0)
            {
                RotationImage(measureResult, _measureParam.Rotation);
            }


            Mat image = measureResult.GrayImage;

            try
            {
                Cv2.CvtColor(image, image, ColorConversionCodes.GRAY2BGR);

                Scalar s;

                double alpha = 0.5;
                Mat blended;

                // 绘制翘钉结果
                if (measureResult.Warp.IsOk)
                {
                    s = new Scalar(0, 255, 0);
                }
                else
                {
                    s = new Scalar(0, 0, 255);
                }
                Mat WarpOverlay = new Mat(image.Size(), MatType.CV_8UC3, new Scalar(0, 0, 255));
                Mat WarpMask = new Mat(image.Size(), MatType.CV_8UC1, new Scalar(0));
                if (measureResult.Warp.HighestPointCol != Single.NegativeInfinity &&
                    measureResult.Warp.HighestPointRow != Single.NegativeInfinity &&
                    measureResult.Warp.Height != Single.NegativeInfinity)
                {
                    for (int i = 0; i < measureResult.Warp.Polygons.Count; i++)
                    {
                        if (measureResult.Warp.Polygons[i].Contours.Length > 0)
                        {
                            Cv2.FillPoly(WarpMask, measureResult.Warp.Polygons[i].Contours, new Scalar(255), LineTypes.AntiAlias);
                        }
                    }
                    if (measureResult.Warp.Polygons.Count > 0)
                    {
                        blended = image.Clone();
                        WarpOverlay.CopyTo(blended, WarpMask);
                        Cv2.AddWeighted(blended, alpha, image, 1 - alpha, 0, image);
                    }
                    Cv2.Circle(image, new OpenCvSharp.Point((int)measureResult.Warp.HighestPointCol, (int)measureResult.Warp.HighestPointRow), 5, s, -1);
                    string text = $"WarpHeight:{measureResult.Warp.Height:F3}";
                    double tmpX = measureResult.Warp.HighestPointCol;
                    double tmpY = measureResult.Warp.HighestPointRow;
                    OpenCvSharp.Point orgR = new OpenCvSharp.Point(tmpX + 20, tmpY + 20);
                    Cv2.PutText(image, text, orgR, HersheyFonts.HersheyDuplex, 1, s, 1);
                }

                // 绘制轨迹
                if (measureResult.Orbit.IsOk)
                {
                    s = new Scalar(0, 255, 0);
                }
                else
                {
                    s = new Scalar(0, 0, 255);
                }
                if (measureResult.Orbit.NailCenterCol != Single.NegativeInfinity &&
                    measureResult.Orbit.NailCenterRow != Single.NegativeInfinity &&
                    measureResult.Orbit.OrbitCenterCol != Single.NegativeInfinity &&
                    measureResult.Orbit.OrbitCenterRow != Single.NegativeInfinity &&
                    measureResult.Orbit.Offset != Single.NegativeInfinity)
                {
                    Cv2.Circle(image, new OpenCvSharp.Point((int)measureResult.Orbit.NailCenterCol, (int)measureResult.Orbit.NailCenterRow), 5, new Scalar(255, 144, 30), -1);
                    Cv2.Circle(image, new OpenCvSharp.Point((int)measureResult.Orbit.OrbitCenterCol, (int)measureResult.Orbit.OrbitCenterRow), 5, new Scalar(209, 206, 0), -1);
                    Cv2.Circle(image, new OpenCvSharp.Point((int)measureResult.Orbit.OrbitCenterCol, (int)measureResult.Orbit.OrbitCenterRow),
                                      (int)measureResult.Orbit.OrbitRadius, new Scalar(209, 206, 0), 8);
                    Cv2.Line(image, new OpenCvSharp.Point((int)measureResult.Orbit.NailCenterCol, (int)measureResult.Orbit.NailCenterRow),
                                    new OpenCvSharp.Point((int)measureResult.Orbit.OrbitCenterCol, (int)measureResult.Orbit.OrbitCenterRow), s, 1, LineTypes.AntiAlias);
                    string text = $"OrbitOffset:{measureResult.Orbit.Offset:F3}";
                    double tmpX = (measureResult.Orbit.NailCenterCol + measureResult.Orbit.OrbitCenterCol) / 2;
                    double tmpY = (measureResult.Orbit.NailCenterRow + measureResult.Orbit.OrbitCenterRow) / 2;
                    OpenCvSharp.Point orgR = new OpenCvSharp.Point(tmpX + 20, tmpY + 20);
                    Cv2.PutText(image, text, orgR, HersheyFonts.HersheyDuplex, 0.5, s, 1);
                }

                // 绘制缺陷
                int defectNum = measureResult.Defects.Count;
                Mat defectOverlay = new Mat(image.Size(), MatType.CV_8UC3, new Scalar(255, 0, 0));
                Mat defectMask = new Mat(image.Size(), MatType.CV_8UC1, new Scalar(0));
                Mat crackOverlay = new Mat(image.Size(), MatType.CV_8UC3, new Scalar(0, 255, 255));
                Mat crackMask = new Mat(image.Size(), MatType.CV_8UC1, new Scalar(0));
                for (int i = 0; i < defectNum; i++)
                {
                    DefectResult defect = measureResult.Defects[i];

                    if (defect.IsOk)
                        continue;

                    if (defect.Left != Single.NegativeInfinity && defect.Top != Single.NegativeInfinity &&
                       defect.Right != Single.NegativeInfinity && defect.Bottom != Single.NegativeInfinity &&
                       defect.InstanceId != -1 && defect.Confidence != Single.NegativeInfinity)
                    {
                        Cv2.Rectangle(image, new OpenCvSharp.Point((int)defect.Left, (int)defect.Top),
                                             new OpenCvSharp.Point((int)defect.Right, (int)defect.Bottom), new Scalar(255, 0, 0), 8);
                        for (int j = 0; j < defect.DefectPolygons.Count; j++)
                        {
                            if (defect.DefectPolygons[j].Contours.Length > 0)
                            {
                                if (_categories[defect.ClassId] != "裂纹")
                                    Cv2.FillPoly(defectMask, defect.DefectPolygons[j].Contours, new Scalar(255), LineTypes.AntiAlias);
                                else
                                    Cv2.FillPoly(crackMask, defect.DefectPolygons[j].Contours, new Scalar(255), LineTypes.AntiAlias);
                            }
                        }

                        string text = $"ID:{defect.InstanceId}";
                        Cv2.PutText(image, text, new OpenCvSharp.Point(defect.Left, defect.Top - 8), HersheyFonts.HersheyDuplex, 3, new Scalar(255, 0, 0), 6);
                    }

                }

                blended = image.Clone();
                defectOverlay.CopyTo(blended, defectMask);
                Cv2.AddWeighted(blended, alpha, image, 1 - alpha, 0, image);

                blended = image.Clone();
                crackOverlay.CopyTo(blended, crackMask);
                Cv2.AddWeighted(blended, alpha, image, 1 - alpha, 0, image);


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);

                image = Mat.Zeros(new OpenCvSharp.Size(128, 128), MatType.CV_8UC3);
            }

            return 0;
        }


        internal static class EVEMFDJCLegacyDepthHelper
        {
            public const float LegacyInvalidDepthValue = 8888880f;

            public static List<float[]> CreateLegacyCompatibleDepthData(List<float[]> source)
            {
                ArgumentNullException.ThrowIfNull(source);

                List<float[]> result = new List<float[]>(source.Count);
                foreach (float[] row in source)
                {
                    ArgumentNullException.ThrowIfNull(row);

                    float[] sanitizedRow = new float[row.Length];
                    for (int i = 0; i < row.Length; i++)
                    {
                        float value = row[i];
                        sanitizedRow[i] = float.IsNaN(value) || float.IsInfinity(value)
                            ? LegacyInvalidDepthValue
                            : value;
                    }

                    result.Add(sanitizedRow);
                }

                return result;
            }
        }


        /// <summary>
        /// 算法配置参数
        /// </summary>
        [Serializable]
        public class MFDJC0_MeasureParam
        {
            //模型参数
            private string _modelConfigPath = "./SealingNailsSDK/models_WidthoutCleanSurface/model.json";
            private string _modelPath = "./SealingNailsSDK/models_WidthoutCleanSurface/model.kmodel";
            private int _batchSize = 1;
            private DeviceType _deviceType = DeviceType.DEVICE_GPU;
            private ModelType _modelType = ModelType.MODEL_DETECTION_SEG;
            private double _confidenceThreshold = 0.5;
            private double _ioUThreshold = 0.5;
            private double _segmentationThreshold = 0.5;

            //传感器参数
            private double _intervalX = 2.9;    //X方向的像素当量(μm)
            private double _intervalY = 5;      //Y方向的像素当量(μm)
            private double _intervalZ = 0.1;    //Y方向的像素当量(μm)
            private double _minDepth = -50000;  //深度图深度值下限
            private double _maxDepth = 50000;   //深度图深度值上限
            private bool _isFlip = false;       //图片是否需要翻转
            private bool _isScanEnd = false;    //扫描是否结束
            private double _offsetX = 0;        //图片拼接X方向偏移量(μm)
            private double _offsetY = 0;        //图片拼接Y方向偏移量(μm)
            private bool _isSaveImage = false;    //是否存储图片
            private string _saveImagePath = "";    //存储图片路径

            //图片绘制参数
            private RotationAngle _rotation = RotationAngle.ANGLE_270; // 绘制图片时的旋转角度

            //缺陷特征参数
            private string _featureConfigPath = "./SealingNailsSDK/models_WidthoutCleanSurface/FeatureConfig.json";
            //private string _templateModelPath = "./SealingNailsSDK/models/NailCenterModel";
            private string _templateModelPath = "./SealingNailsSDK/models_WidthoutCleanSurface/NailCenterNCCModel";

            //密封钉类型
            private bool _isWithMilling = true; //密封钉是否带有清洗面

            //图片是否镜像
            private bool _isMirror = false;

            /// <summary>
            /// 模型参数：密封钉检测模型配置文件路径
            /// </summary>
            public string ModelConfigPath
            {
                get { return _modelConfigPath; }
                set { _modelConfigPath = value; }
            }

            /// <summary>
            /// 模型参数：密封钉检测模型文件路径
            /// </summary>
            public string ModelPath
            {
                get { return _modelPath; }
                set { _modelPath = value; }
            }

            /// <summary>
            /// 模型参数：batchSize
            /// </summary>
            public int BatchSize
            {
                get { return _batchSize; }
                set { _batchSize = value; }
            }

            /// <summary>
            /// 模型参数：设备类型
            /// </summary>
            public DeviceType DeviceType
            {
                get { return _deviceType; }
                set { _deviceType = value; }
            }

            /// <summary>
            /// 模型参数：模型类型
            /// </summary>
            public ModelType ModelType
            {
                get { return _modelType; }
                set { _modelType = value; }
            }

            /// <summary>
            /// 模型参数：置信度阈值
            /// </summary>
            public double ConfidenceThreshold
            {
                get { return _confidenceThreshold; }
                set
                {
                    if (value > 0 && value <= 1)
                    {
                        _confidenceThreshold = value;
                    }
                    else
                    {
                        _confidenceThreshold = 0.5;
                    }
                }
            }

            /// <summary>
            /// 模型参数：IOU阈值
            /// </summary>
            public double IoUThreshold
            {
                get { return _ioUThreshold; }
                set
                {
                    if (value > 0 && value <= 1)
                    {
                        _ioUThreshold = value;
                    }
                    else
                    {
                        _ioUThreshold = 0.5;
                    }
                }
            }

            /// <summary>
            /// 模型参数：分割阈值
            /// </summary>
            public double SegmentationThreshold
            {
                get { return _segmentationThreshold; }
                set
                {
                    if (value > 0 && value <= 1)
                    {
                        _segmentationThreshold = value;
                    }
                    else
                    {
                        _segmentationThreshold = 0.5;
                    }
                }
            }


            /// <summary>
            /// 传感器参数：X方向点间隔
            /// </summary>
            public double IntervalX
            {
                get { return _intervalX; }
                set
                {
                    if (value > 0)
                    {
                        _intervalX = value;
                    }
                    else
                    {
                        _intervalX = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：Y方向点间隔
            /// </summary>
            public double IntervalY
            {
                get { return _intervalY; }
                set
                {
                    if (value > 0)
                    {
                        _intervalY = value;
                    }
                    else
                    {
                        _intervalY = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：Z方向点间隔
            /// </summary>
            public double IntervalZ
            {
                get { return _intervalZ; }
                set
                {
                    if (value > 0)
                    {
                        _intervalZ = value;
                    }
                    else
                    {
                        _intervalZ = 1;
                    }
                }
            }

            /// <summary>
            /// 传感器参数：深度图深度值下限
            /// </summary>
            public double MinDepth
            {
                get { return _minDepth; }
                set { _minDepth = value; }
            }

            /// <summary>
            /// 传感器参数：深度图深度值上限
            /// </summary>
            public double MaxDepth
            {
                get { return _maxDepth; }
                set { _maxDepth = value; }
            }

            /// <summary>
            /// 传感器参数：图片是否需要翻转(需要翻转：ture; 不需要翻转：false)
            /// </summary>
            public bool IsFlip
            {
                get { return _isFlip; }
                set { _isFlip = value; }
            }

            /// <summary>
            /// 传感器参数：扫描是否结束
            /// </summary>
            public bool IsScanEnd
            {
                get { return _isScanEnd; }
                set { _isScanEnd = value; }
            }

            /// <summary>
            /// 传感器参数：图片拼接X方向偏移量(μm)
            /// </summary>
            public double OffsetX
            {
                get { return _offsetX; }
                set { _offsetX = value; }
            }

            /// <summary>
            /// 传感器参数：图片拼接Y方向偏移量(μm)
            /// </summary>
            public double OffsetY
            {
                get { return _offsetY; }
                set { _offsetY = value; }
            }

            /// <summary>
            /// 是否存图
            /// </summary>
            public bool IsSaveImage
            {
                get { return _isSaveImage; }
                set { _isSaveImage = value; }
            }

            /// <summary>
            /// 存图位置
            /// </summary>
            public string SaveImagePath
            {
                get { return _saveImagePath; }
                set { _saveImagePath = value; }
            }

            /// <summary>
            /// 绘制图片时的旋转角度
            /// </summary>
            public RotationAngle Rotation
            {
                get { return _rotation; }
                set { _rotation = value; }
            }

            /// <summary>
            /// 缺陷特征算法参数：缺陷特征算法配置文件路径
            /// </summary>
            public string FeatureConfigPath
            {
                get { return _featureConfigPath; }
                set { _featureConfigPath = value; }
            }

            /// <summary>
            /// 缺陷特征算法参数: 模板匹配模型文件路径
            /// </summary>
            public string TemplateModelPath
            {
                get { return _templateModelPath; }
                set { _templateModelPath = value; }
            }

            /// <summary>
            /// 密封钉类型: 是否带有清洗面
            /// </summary>
            public bool IsWithMilling
            {
                get { return _isWithMilling; }
                set { _isWithMilling = value; }
            }

            /// <summary>
            /// 镜像
            /// </summary>
            public bool IsMirror
            {
                get { return _isMirror; }
                set { _isMirror = value; }
            }
        }


        public class Polygon
        {
            // 轮廓点集
            public OpenCvSharp.Point[][] Contours { get; }

            public Polygon(HObject region, int offsetX = 0, int offsetY = 0)
            {
                if (region != null && region.IsInitialized() && region.CountObj() > 0)
                {
                    // 根据轮廓拟合圆
                    HOperatorSet.GenContourRegionXld(region, out HObject hoRegionContour, "border");

                    // 提取轮廓点集
                    HOperatorSet.GenPolygonsXld(hoRegionContour, out HObject hoRegionPolygon, "ramer", 2);
                    HOperatorSet.GetPolygonXld(hoRegionPolygon, out HTuple hvPolygonRows, out HTuple hvPolygonCols,
                                               out HTuple hvTmpLength, out HTuple hvTmpPhi);

                    List<OpenCvSharp.Point[]> tmpContours = new List<OpenCvSharp.Point[]>();
                    OpenCvSharp.Point[] tmpContour = new OpenCvSharp.Point[hvPolygonRows.Length];
                    for (int i = 0; i < hvPolygonRows.Length; i++)
                    {
                        tmpContour[i] = new OpenCvSharp.Point((int)hvPolygonCols.TupleSelect(i).D + offsetX, (int)hvPolygonRows.TupleSelect(i).D + offsetY);
                    }
                    if (tmpContour.Length > 0)
                    {
                        tmpContours.Add(tmpContour);
                    }
                    Contours = tmpContours.ToArray();

                    hoRegionContour.Dispose();
                    hoRegionPolygon.Dispose();
                }
                else
                {
                    Contours = new OpenCvSharp.Point[][] { };
                }
            }

            public Polygon()
            {
                Contours = new OpenCvSharp.Point[][] { };
            }
        }


        public class Segment
        {
            public int Valid { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public float[] AffineMatrix { get; set; } = new float[6];
            public float Thresh { get; set; }
            public float[] Data { get; set; } = Array.Empty<float>();
        }

        public class Box
        {
            public int InstanceId { get; set; }
            public float Left { get; set; }
            public float Top { get; set; }
            public float Right { get; set; }
            public float Bottom { get; set; }
            public float Confidence { get; set; }
            public int ClassId { get; set; }
            public Segment Seg { get; set; } = new Segment();
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct NativeKpts
        {
            public IntPtr Point;      // 指向 NativePoint 数组
            public int PointNum;
            public IntPtr Skeleton;
            public int ConnectionNum;
            public float Thresh;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeSeg
        {
            public int Width;
            public int Height;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public float[] AffineMatrix;

            public float Thresh;
            public IntPtr IntData;   // 指向 int 数组
            public IntPtr FloatData;

            [MarshalAs(UnmanagedType.I1)]
            public bool IsIntData;   // 标记当前数据类型
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct NativeResult
        {
            public float Cx, Cy, Width, Height, Angle;
            public float Confidence;
            public int ClassId;
            public IntPtr ClassName; // const char*
            public NativeKpts Keypoints;
            public NativeSeg Segmentation;
        }


        /// <summary>
        /// 图片与测量数据
        /// </summary>
        public class ImageData : IDisposable
        {
            private bool _disposed = false;

            /// <summary>
            /// 灰度图
            /// </summary>
            public HObject hoGrayImage { get; set; }

            /// <summary>
            /// 深度图
            /// </summary>
            public HObject hoHeightImage { get; set; }

            /// <summary>
            /// 深度图有效区域
            /// </summary>
            public HObject hoValidMask { get; set; }

            /// <summary>
            /// 图片宽(pixel)
            /// </summary>
            public HTuple hvImageWidth { get; set; }

            /// <summary>
            /// 图片高(pixel)
            /// </summary>
            public HTuple hvImageHeight { get; set; }

            /// <summary>
            /// X方向像素当量
            /// </summary>
            public HTuple hvIntervalX { get; set; }

            /// <summary>
            /// Y方向像素当量
            /// </summary>
            public HTuple hvIntervalY { get; set; }

            /// <summary>
            /// Y方向像素当量
            /// </summary>
            public HTuple hvIntervalZ { get; set; }

            /// <summary>
            /// 图片拼接X方向偏移量(pixel)
            /// </summary>
            public double OffsetX { get; set; }

            /// <summary>
            /// 图片拼接Y方向偏移量(pixel)
            /// </summary>
            public double OffsetY { get; set; }

            /// <summary>
            /// 当前图片的检测结果
            /// </summary>
            public List<Box> Boxes { get; set; }


            public ImageData()
            {
                hoGrayImage = new HObject();
                hoHeightImage = new HObject();
                hoValidMask = new HObject();
                hvIntervalX = new HTuple();
                hvIntervalY = new HTuple();
                hvIntervalZ = new HTuple();

                OffsetX = 0;
                OffsetY = 0;

                Boxes = new List<Box>();
            }


            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                if (disposing)
                {
                    hoGrayImage?.Dispose();
                    hoHeightImage?.Dispose();
                    hvIntervalX?.Dispose();
                    hvIntervalY?.Dispose();
                    hvIntervalZ?.Dispose();
                }

                _disposed = true;
            }

            ~ImageData()
            {
                Dispose(false);
            }
        }


        /// <summary>
        /// 翘钉测量结果
        /// </summary>
        public class WarpResult
        {
            /// <summary>
            /// 是否翘钉
            /// </summary>
            public bool IsOk { get; set; }

            /// <summary>
            /// 翘曲高度(μm)
            /// </summary>
            public double Height { get; set; }

            /// <summary>
            /// 翘曲最高点Y坐标(pixel)
            /// </summary>
            public double HighestPointRow { get; set; }

            /// <summary>
            /// 翘曲最高点X坐标(pixel)
            /// </summary>
            public double HighestPointCol { get; set; }

            /// <summary>
            /// 翘曲高度超出阈值的区域
            /// </summary>
            public List<Polygon> Polygons { get; set; }

            public WarpResult()
            {
                IsOk = true;
                Height = Single.NegativeInfinity;
                HighestPointRow = Single.NegativeInfinity;
                HighestPointCol = Single.NegativeInfinity;

                Polygons = new List<Polygon>();
            }
        }


        /// <summary>
        /// 轨迹偏移测量结果
        /// </summary>
        public class OrbitResult
        {
            /// <summary>
            /// 是否存在轨迹偏移
            /// </summary>
            public bool IsOk { get; set; }

            /// <summary>
            /// 密封钉中心X轴坐标
            /// </summary>
            public double NailCenterCol { get; set; }

            /// <summary>
            /// 密封钉中心Y轴坐标
            /// </summary>
            public double NailCenterRow { get; set; }

            /// <summary>
            /// 密封钉中心空半径
            /// </summary>
            public double NailRadius { get; set; }

            /// <summary>
            /// 焊接轨迹中心X轴坐标
            /// </summary>
            public double OrbitCenterCol { get; set; }

            /// <summary>
            /// 焊接轨迹中心Y轴坐标
            /// </summary>
            public double OrbitCenterRow { get; set; }

            /// <summary>
            /// 焊接轨迹半径
            /// </summary>
            public double OrbitRadius { get; set; }

            /// <summary>
            /// 偏移距离(μm)
            /// </summary>
            public double Offset { get; set; }


            public OrbitResult()
            {
                IsOk = true;
                NailCenterCol = Single.NegativeInfinity;
                NailCenterRow = Single.NegativeInfinity;
                OrbitCenterCol = Single.NegativeInfinity;
                OrbitCenterRow = Single.NegativeInfinity;
                OrbitRadius = Single.NegativeInfinity;
                Offset = Single.NegativeInfinity;
            }
        }


        /// <summary>
        /// 瑕疵检测结果
        /// </summary>
        public class DefectResult
        {
            public bool IsOk { get; set; }

            public int InstanceId { get; set; }

            public double Left { get; set; }

            public double Top { get; set; }

            public double Right { get; set; }

            public double Bottom { get; set; }

            public List<Polygon> DefectPolygons { get; set; }

            public double AreaFeature { get; set; }

            public double LengthFeature { get; set; }

            public double WidthFeature { get; set; }

            public double DepthFeature { get; set; }

            public double CenterRowFeature { get; set; }

            public double CenterColFeature { get; set; }

            public double Diameter { get; set; }

            public double Confidence { get; set; }

            public int ClassId { get; set; }

            public Dictionary<int, string> Categories { get; set; }


            public DefectResult()
            {
                IsOk = true;
                InstanceId = -1;
                Left = Single.NegativeInfinity;
                Top = Single.NegativeInfinity;
                Right = Single.NegativeInfinity;
                Bottom = Single.NegativeInfinity;
                AreaFeature = Single.NegativeInfinity;
                LengthFeature = Single.NegativeInfinity;
                WidthFeature = Single.NegativeInfinity;
                DepthFeature = Single.NegativeInfinity;
                CenterRowFeature = Single.NegativeInfinity;
                CenterColFeature = Single.NegativeInfinity;
                Diameter = Single.NegativeInfinity;
                Confidence = Single.NegativeInfinity;
                ClassId = -1;

                DefectPolygons = new List<Polygon>();
                Categories = new Dictionary<int, string>();

            }
        }


        /// <summary>
        /// 测量结果
        /// </summary>
        public class MFDJC0_MeasureResult
        {
            /// <summary>
            /// 完整的灰度图(标注测量结果)
            /// </summary>
            public Mat GrayImage { get; set; }

            /// <summary>
            /// 完整的高度图
            /// </summary>
            public Mat HeightImage { get; set; }

            /// <summary>
            /// 原始完整的灰度图
            /// </summary>
            public Mat GrayImageOri { get; set; }

            /// <summary>
            /// 翘钉测量结果
            /// </summary>
            public WarpResult Warp { get; set; }

            /// <summary>
            /// 轨迹偏移测量结果
            /// </summary>
            public OrbitResult Orbit { get; set; }

            /// <summary>
            /// 瑕疵检测结果
            /// </summary>
            public List<DefectResult> Defects { get; set; }

            /// <summary>
            /// 高度图有效值下限
            /// </summary>
            public double MinDepth { get; set; }

            /// <summary>
            /// 高度图有效值上限
            /// </summary>
            public double MaxDepth { get; set; }

            /// <summary>
            /// 高度图有效区域掩码
            /// </summary>
            public HObject HoValidMask { get; set; }

            /// <summary>
            /// 高度图无效区域掩码
            /// </summary>
            public HObject HoIrregularMask { get; set; }


            public MFDJC0_MeasureResult()
            {
                GrayImage = new Mat();
                HeightImage = new Mat();
                GrayImageOri = new Mat();
                Warp = new WarpResult();
                Orbit = new OrbitResult();

                Defects = new List<DefectResult>();

                MinDepth = Single.NegativeInfinity;
                MaxDepth = Single.NegativeInfinity;

                HoValidMask = new HObject();
                HoIrregularMask = new HObject();
            }

            ~MFDJC0_MeasureResult()
            {
                GrayImage.Dispose();
                HeightImage.Dispose();
                GrayImageOri.Dispose();

                HoValidMask.Dispose();
                HoIrregularMask.Dispose();
            }
        }

    }
}

