using HalconDotNet;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OpenCvSharp;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Services.CustomProject;
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


namespace Custom.MFDJC
{
    using DeepLearningHandle = System.IntPtr;

    public class MFDJC0_Algorithm : ICustomAlgo
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

        private List<ImageData> _imageData;
        private MFDJC0_MeasureParam _measureParam;

        private Dictionary<int, string> _categories;
        private int _segCategoryNum;
        private static List<FeatureAlgorithm> _algorithmList;
        private static List<Defect> _defectList;

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

        public MFDJC0_Algorithm(MFDJC0_MeasureParam param)
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

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }


        public void DestroyModel()
        {
            int state = SealingNailsSDK.DestroyModel(_deepLearningHandle);
        }


        ~MFDJC0_Algorithm()
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
            HObject hoPaintHeightImage;
            HObject hoCircle;
            HObject hoTmpHeightImage = MatToHObject(_measureResult.HeightImage);

            HOperatorSet.PaintRegion(_measureResult.HoIrregularMask, hoTmpHeightImage, out hoPaintHeightImage, _measureResult.MinDepth, "fill");

            HOperatorSet.ScaleImageMax(hoPaintHeightImage, out hoPaintHeightImage);
            HOperatorSet.MeanImage(hoPaintHeightImage, out hoPaintHeightImage, 5, 5);

            HOperatorSet.GenCircle(out hoCircle, centerY, centerX, radius);

            HOperatorSet.ReduceDomain(hoPaintHeightImage, hoCircle, out hoPaintHeightImage);

            HOperatorSet.CreateNccModel(hoPaintHeightImage, "auto", -0.39, 0.79, "auto", "use_polarity", out _hvNailCenterModelID);

            HOperatorSet.WriteNccModel(_hvNailCenterModelID, _measureParam.TemplateModelPath);

            return 0;
        }



        /// <summary>
        /// 获取高度图有效值区域
        /// </summary>
        public int GetDepthValidMask(HObject hoHeightImage)
        {
            HObject hoRectangle, hoIrregularRegion;
            HObject hoIrregularRegion0, hoIrregularRegion1, hoIrregularRegion2;

            HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvWidth, out HTuple hvHeight);
            HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, hvHeight, hvWidth);

            HOperatorSet.Threshold(hoHeightImage, out _hoValidMask, _measureParam.MinDepth, _measureParam.MaxDepth);

            HOperatorSet.GenEmptyObj(out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion0, 8888880, 8888880);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion0, out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion1, -2147483648, -2147483648);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion1, out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion2, 0, 0);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion2, out hoIrregularRegion);
            HOperatorSet.Union1(hoIrregularRegion, out _hoIrregularMask);
            HOperatorSet.Difference(hoRectangle, _hoIrregularMask, out hoRectangle);
            HOperatorSet.Intersection(_hoValidMask, hoRectangle, out _hoValidMask);

            HOperatorSet.MinMaxGray(_hoValidMask, hoHeightImage, 0, out _hvHeightImageGlobalMinValue, out _hvHeightImageGlobalMaxValue, out HTuple range);

            _measureResult.MinDepth = _hvHeightImageGlobalMinValue.D;
            _measureResult.MaxDepth = _hvHeightImageGlobalMaxValue.D;

            _measureResult.HoValidMask.Dispose();
            _measureResult.HoIrregularMask.Dispose();
            _measureResult.HoValidMask = _hoValidMask;
            _measureResult.HoIrregularMask = _hoIrregularMask;

            hoRectangle.Dispose();
            hoIrregularRegion.Dispose();
            hoIrregularRegion0.Dispose();
            hoIrregularRegion1.Dispose();
            hoIrregularRegion2.Dispose();

            return 0;
        }

        /// <summary>
        /// 获取高度图有效值区域
        /// </summary>
        public HObject GetLocalDepthValidMask(HObject hoHeightImage)
        {
            HObject hoValidMask = new HObject();

            HObject hoRectangle, hoIrregularRegion;
            HObject hoIrregularRegion0, hoIrregularRegion1, hoIrregularRegion2, hoIrregularMask;

            HOperatorSet.GetImageSize(hoHeightImage, out HTuple hvWidth, out HTuple hvHeight);
            HOperatorSet.GenRectangle1(out hoRectangle, 0, 0, hvHeight, hvWidth);

            HOperatorSet.Threshold(hoHeightImage, out hoValidMask, _measureParam.MinDepth, _measureParam.MaxDepth);

            HOperatorSet.GenEmptyObj(out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion0, 8888880, 8888880);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion0, out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion1, -2147483648, -2147483648);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion1, out hoIrregularRegion);
            HOperatorSet.Threshold(hoHeightImage, out hoIrregularRegion2, 0, 0);
            HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion2, out hoIrregularRegion);
            HOperatorSet.Union1(hoIrregularRegion, out hoIrregularMask);
            HOperatorSet.Difference(hoRectangle, hoIrregularMask, out hoRectangle);
            HOperatorSet.Intersection(hoValidMask, hoRectangle, out hoValidMask);

            hoRectangle.Dispose();
            hoIrregularRegion.Dispose();
            hoIrregularRegion0.Dispose();
            hoIrregularRegion1.Dispose();
            hoIrregularRegion2.Dispose();
            hoIrregularMask.Dispose();

            return hoValidMask;
        }


        /// <summary>
        /// 拼接图片
        /// </summary>
        public int ConcateImages(out HObject hoTileGrayImage, out HObject hoTileHeightImage)
        {
            int imageNum = _imageData.Count;

            HObject hoGrayImages, hoHeightImages;
            HOperatorSet.GenEmptyObj(out hoGrayImages);
            HOperatorSet.GenEmptyObj(out hoHeightImages);
            HTuple hvOffsetRows = new HTuple();
            HTuple hvOffsetCols = new HTuple();
            HTuple hvTmpRow1 = new HTuple();
            HTuple hvTmpCol1 = new HTuple();
            HTuple hvTmpRow2 = new HTuple();
            HTuple hvTmpCol2 = new HTuple();
            HTuple hvConcatW = new HTuple(0);
            HTuple hvConcatH = new HTuple(0);
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

                HOperatorSet.GetImageSize(_imageData[i].hoGrayImage, out HTuple hvTmpW, out HTuple hvTmpH);

                HTuple hvW = _imageData[i].OffsetX + hvTmpW;
                HTuple hvH = _imageData[i].OffsetY + hvTmpH;
                if (hvW > hvConcatW)
                    hvConcatW = hvW;
                if (hvH > hvConcatH)
                    hvConcatH = hvH;
            }
            HOperatorSet.TileImagesOffset(hoGrayImages, out hoTileGrayImage, hvOffsetRows, hvOffsetCols,
                                          hvTmpRow1, hvTmpCol1, hvTmpRow2, hvTmpCol2, hvConcatW, hvConcatH);
            HOperatorSet.TileImagesOffset(hoHeightImages, out hoTileHeightImage, hvOffsetRows, hvOffsetCols,
                                          hvTmpRow1, hvTmpCol1, hvTmpRow2, hvTmpCol2, hvConcatW, hvConcatH);

            HTuple scaleX, scaleY;
            if (imageNum > 0)
            {
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
            }
            else
            {
                scaleX = 1;
                scaleY = 1;
            }
            HOperatorSet.ZoomImageFactor(hoTileGrayImage, out hoTileGrayImage, scaleX, scaleY, "constant");
            HOperatorSet.ZoomImageFactor(hoTileHeightImage, out hoTileHeightImage, scaleX, scaleY, "nearest_neighbor");
            //HOperatorSet.ZoomRegion(_hoValidMask, out _hoValidMask, scaleX, scaleY);

            GetDepthValidMask(hoTileHeightImage);

            hoGrayImages.Dispose();
            hoHeightImages.Dispose();

            return 0;
        }


        /// <summary>
        /// 定位密封钉中心(old)
        /// </summary>
        public int GetNailCenterAndOrbitMaskOld(HObject hoImage, HObject hoHeightImage, out HTuple hvNailCx, out HTuple hvNailCy, out HTuple hvOrbitParam)
        {
            HObject hoFindRegion;
            HObject hoImageReduced;

            HTuple hvAngle, hvScale, hvScore;

            HOperatorSet.GetImageSize(hoImage, out HTuple hvTmpW, out HTuple hvTmpH);
            HOperatorSet.GenCircle(out hoFindRegion, 0.5 * hvTmpH, 0.5 * hvTmpW, 560.07);
            HOperatorSet.ReduceDomain(hoImage, hoFindRegion, out hoImageReduced);

            HOperatorSet.FindScaledShapeModel(hoImageReduced, _hvNailCenterModelID, -0.39, 0.78, 0.3, 3, 0.9, 1, 0.5,
                                              "least_squares", 0, 0.9, out hvNailCy, out hvNailCx, out hvAngle, out hvScale, out hvScore);

            HObject hoModel;
            HObject hoModelRegon;
            HOperatorSet.GetShapeModelContours(out hoModel, _hvNailCenterModelID, 1);
            HOperatorSet.GenRegionContourXld(hoModel, out hoModelRegon, "filled");
            HOperatorSet.RegionFeatures(hoModelRegon, "outer_radius", out HTuple radius);

            if (hvScore.Length > 0)
            {
                HOperatorSet.GenCircle(out _hoNailCenterModel, hvNailCy, hvNailCx, hvScale * radius);

                _nailCenterIsTrue = true;
            }
            else
            {
                hvNailCx = 0.5 * hvTmpW;
                hvNailCy = 0.5 * hvTmpH;
                HOperatorSet.GenCircle(out _hoNailCenterModel, hvNailCy, hvNailCx, radius);

                _nailCenterIsTrue = false;
            }

            // 计算焊接轨迹的外轮廓
            HObject hoImageMean;
            HObject hoContours;

            HTuple hvMetrologyHandle;
            HTuple hvIndex;
            HTuple hvContourRow, hvContourCol;

            HOperatorSet.CreateMetrologyModel(out hvMetrologyHandle);
            HOperatorSet.AddMetrologyObjectCircleMeasure(hvMetrologyHandle, hvNailCy, hvNailCx, 1500, 638, 5, 28, 30,
                                                         new HTuple(), new HTuple(), out hvIndex);
            HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, 0, "measure_transition", "negative");
            HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "min_score", 0.01);
            HOperatorSet.SetMetrologyObjectParam(hvMetrologyHandle, "all", "measure_select", "last");

            HOperatorSet.MeanImage(hoImage, out hoImageMean, 33, 33);

            HOperatorSet.ApplyMetrologyModel(hoImageMean, hvMetrologyHandle);
            HOperatorSet.GetMetrologyObjectMeasures(out hoContours, hvMetrologyHandle, "all", "all", out hvContourRow, out hvContourCol);
            HOperatorSet.GetMetrologyObjectResult(hvMetrologyHandle, "all", "all", "result_type", "all_param", out hvOrbitParam);

            // 焊迹掩码
            if (hvOrbitParam.Length > 0)
            {
                HTuple hvCirRow, hvCirCol, hvCirRad;

                hvCirRow = hvOrbitParam[0];
                hvCirCol = hvOrbitParam[1];
                hvCirRad = hvOrbitParam[2];

                HObject hoCircleInter, hoCircleOuter;
                HObject hoRect, hoRing;
                HOperatorSet.GenCircle(out hoCircleInter, hvCirRow, hvCirCol, hvCirRad * 0.656);
                HOperatorSet.GenCircle(out hoCircleOuter, hvCirRow, hvCirCol, hvCirRad);

                HOperatorSet.GenRectangle1(out hoRect, hvCirRow + (hvCirRad * 0.656), hvCirCol - (hvCirRad * 0.846),
                                                       hvCirRow + hvCirRad, hvCirCol + (hvCirRad * 0.846));

                HOperatorSet.Difference(hoCircleOuter, hoCircleInter, out hoRing);
                HOperatorSet.Union2(hoRing, hoRect, out _hoOrbitMask);

                hoCircleInter.Dispose();
                hoCircleOuter.Dispose();
                hoRect.Dispose();
                hoRing.Dispose();
            }
            else
            {
                HOperatorSet.GenRectangle1(out _hoOrbitMask, 0, 0, hvTmpH, hvTmpW);
            }

            HTuple scaleX, scaleY;
            int imageNum = _imageData.Count;
            if (imageNum > 0)
            {
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
            }
            else
            {
                scaleX = 1;
                scaleY = 1;
            }

            HOperatorSet.Intersection(_hoValidMask, _hoOrbitMask, out _hoOrbitMask);

            HOperatorSet.ZoomRegion(_hoOrbitMask, out _hoOrbitMask, 1 / scaleX, 1 / scaleY);



            hoFindRegion.Dispose();
            hoImageReduced.Dispose();
            hoModel.Dispose();
            hoModelRegon.Dispose();
            hoImageMean.Dispose();
            hoContours.Dispose();

            return 0;
        }


        /// <summary>
        /// 定位密封钉中心
        /// </summary>
        public int GetNailCenterAndOrbitMask(HObject hoImage, HObject hoHeightImage, out HTuple hvNailCx, out HTuple hvNailCy, out HTuple hvOrbitOuterParam)
        {
            HTuple hvIndex;

            HTuple hvModelAngle, hvModelScore, hvModelRadius;

            HObject hoCoarseNailCircleRegion = new HObject();
            HObject hoTemplateRegion;

            HOperatorSet.GetImageSize(hoImage, out HTuple hvImageOriW, out HTuple hvImageOriH);
            HOperatorSet.GetNccModelRegion(out hoTemplateRegion, _hvNailCenterModelID);
            HOperatorSet.RegionFeatures(hoTemplateRegion, "outer_radius", out hvModelRadius);

            // 密封钉中心区域粗定位
            // 加速系数
            HTuple hvAccelerationFactor = 4.0f;
            HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
            HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
            HTuple hvCoarseHandle;
            HObject hoImageResize = new HObject();
            HObject hoImageResizeMean = new HObject();
            HOperatorSet.ZoomImageFactor(hoImage, out hoImageResize, hvScaleFactorW, hvScaleFactorH, "constant");
            HOperatorSet.GetImageSize(hoImageResize, out HTuple hvResizeWidth, out HTuple hvResizeHeight);

            HTuple hvResizeImageCenterRow = hvResizeHeight / 2.0f;
            HTuple hvResizeImageCenterCol = hvResizeWidth / 2.0f;
            HTuple hvTmpRadius = (hvResizeImageCenterRow + hvResizeImageCenterCol) / 2.0f;

            HOperatorSet.CreateMetrologyModel(out hvCoarseHandle);
            HOperatorSet.AddMetrologyObjectCircleMeasure(hvCoarseHandle, hvResizeImageCenterRow, hvResizeImageCenterCol,
                                                         hvTmpRadius * 1.2, hvTmpRadius * 0.35, 5, 25, 30, new HTuple(), new HTuple(), out hvIndex);
            HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, 0, "measure_transition", "positive");
            HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, "all", "min_score", 0.01);
            HOperatorSet.SetMetrologyObjectParam(hvCoarseHandle, "all", "measure_select", "last");
            HOperatorSet.MeanImage(hoImageResize, out hoImageResizeMean, 11, 11);
            HOperatorSet.ApplyMetrologyModel(hoImageResizeMean, hvCoarseHandle);
            HOperatorSet.GetMetrologyObjectResult(hvCoarseHandle, "all", "all", "result_type", "all_param", out HTuple hvNailCircleParam);

            // 焊迹掩码
            HTuple hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius;
            if (hvNailCircleParam.Length > 0)
            {
                hvCoarseCenterRow = (hvNailCircleParam.TupleSelect(0)) * hvAccelerationFactor;
                hvCoarseCenterCol = (hvNailCircleParam.TupleSelect(1)) * hvAccelerationFactor;
                hvCoarseRadius = (hvNailCircleParam.TupleSelect(2)) * hvAccelerationFactor;

                // 粗定位密封钉最外圈圆
                HOperatorSet.GenCircle(out hoCoarseNailCircleRegion, hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius);
            }
            else
            {
                hvCoarseCenterRow = hvImageOriH / 2.0f;
                hvCoarseCenterCol = hvImageOriW / 2.0f;
                //hvCoarseRadius = 560.07;
                hvCoarseRadius = Math.Min(hvImageOriH, hvImageOriW) * 0.5;
            }

            // 精定位密封钉中心
            HObject hoROI_0, hoImageReduced, hoInvalidROI_0;
            HObject hoImageReducedMean;
            HOperatorSet.GenCircle(out hoROI_0, hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius * 0.25);
            HOperatorSet.ReduceDomain(hoHeightImage, hoROI_0, out hoImageReduced);
            HOperatorSet.Intersection(hoROI_0, _hoIrregularMask, out hoInvalidROI_0);
            HOperatorSet.PaintRegion(hoInvalidROI_0, hoImageReduced, out hoImageReduced, _hvHeightImageGlobalMinValue, "fill");
            HOperatorSet.ScaleImageMax(hoImageReduced, out hoImageReduced);
            // NCC模板匹配
            HOperatorSet.MeanImage(hoImageReduced, out hoImageReducedMean, 5, 5);
            HOperatorSet.FindNccModel(hoImageReducedMean, _hvNailCenterModelID, -0.39, 0.79, 0.65, 1, 0.5, "true", 0,
                                        out hvNailCy, out hvNailCx, out hvModelAngle, out hvModelScore);

            if (hvModelScore.Length > 0)
            {
                HOperatorSet.GenCircle(out _hoNailCenterModel, hvNailCy, hvNailCx, hvModelRadius);
                _nailCenterIsTrue = true;
            }
            else
            {
                hvNailCx = hvCoarseCenterCol;
                hvNailCy = hvCoarseCenterRow;
                HOperatorSet.GenCircle(out _hoNailCenterModel, hvNailCy, hvNailCx, hvModelRadius);
                _nailCenterIsTrue = true;
            }

            // 定位焊缝外圈圆
            HTuple hvOrbitOuterHandle;
            HTuple hvPointRow, hvPointCol;
            //HObject hoImageMean;
            HObject hoContours;
            HObject hoOrbitOuterContour = new HObject();
            HOperatorSet.CreateMetrologyModel(out hvOrbitOuterHandle);
            HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitOuterHandle, hvNailCy / hvAccelerationFactor, hvNailCx / hvAccelerationFactor, (hvCoarseRadius / hvAccelerationFactor) * 0.55, (hvCoarseRadius / hvAccelerationFactor) * 0.35,
                                                            5, 28, 30, new HTuple(), new HTuple(), out hvIndex);
            HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, 0, "measure_transition", "negative");
            HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, "all", "min_score", 0.01);
            HOperatorSet.SetMetrologyObjectParam(hvOrbitOuterHandle, "all", "measure_select", "last");
            //HOperatorSet.MeanImage(hoImage, out hoImageMean, 33, 33);
            HOperatorSet.ApplyMetrologyModel(hoImageResizeMean, hvOrbitOuterHandle);
            // 直接获取拟合结果
            //HOperatorSet.GetMetrologyObjectResult(hvOrbitOuterHandle, "all", "all", "result_type", "all_param", out hvOrbitOuterParam);
            //HOperatorSet.GetMetrologyObjectResultContour(out hoOrbitOuterContour, hvOrbitOuterHandle, "all", "all", 1.5);
            // 通过点集拟合结果
            HOperatorSet.GetMetrologyObjectMeasures(out hoContours, hvOrbitOuterHandle, "all", "all", out hvPointRow, out hvPointCol);
            HTuple TmpCircleRow = new HTuple();
            HTuple TmpCircleColumn = new HTuple();
            HTuple TmpCircleRadius = new HTuple();
            if (hvPointRow.Length > 2)
            {
                HOperatorSet.GenContourPolygonXld(out hoContours, hvPointRow, hvPointCol);
                try
                {
                    HOperatorSet.FitCircleContourXld(hoContours, "geotukey", -1, 0, 0, 3, 2, out TmpCircleRow, out TmpCircleColumn,
                                                     out TmpCircleRadius, out _, out _, out _);
                }
                catch (Exception ex)
                {
                }
            }

            hvOrbitOuterParam = new HTuple();
            if (TmpCircleRow.Length > 0)
            {
                HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleRow * hvAccelerationFactor, out hvOrbitOuterParam);
                HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleColumn * hvAccelerationFactor, out hvOrbitOuterParam);
                HOperatorSet.TupleConcat(hvOrbitOuterParam, TmpCircleRadius * hvAccelerationFactor, out hvOrbitOuterParam);

                HOperatorSet.GenCircleContourXld(out hoOrbitOuterContour, TmpCircleRow, TmpCircleColumn, TmpCircleRadius,
                                                 (new HTuple(0)).TupleRad(), (new HTuple(360)).TupleRad(), "positive", 1);
            }

            // 焊迹掩码
            HTuple hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad;
            HTuple hvOrbitInterCircleRow, hvOrbitInterCircleCol, hvOrbitInterCircleRad;
            if (hvOrbitOuterParam.Length > 0)
            {
                HTuple hvOrbitInterHandle;

                HTuple hvOrbitInterParam;
                HTuple hvCoverCircleParam;

                HObject hoOrbitOuterContourRegion;
                HObject hoOrbitOuterContourImage;

                hvOrbitOuterCircleRow = TmpCircleRow;
                hvOrbitOuterCircleCol = TmpCircleColumn;
                hvOrbitOuterCircleRad = TmpCircleRadius;

                // 定位焊缝内圈圆
                HOperatorSet.GenRegionContourXld(hoOrbitOuterContour, out hoOrbitOuterContourRegion, "filled");
                HOperatorSet.ReduceDomain(hoImageResizeMean, hoOrbitOuterContourRegion, out hoOrbitOuterContourImage);
                HOperatorSet.CreateMetrologyModel(out hvOrbitInterHandle);
                HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitInterHandle, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad * 0.5, hvOrbitOuterCircleRad * 0.45,
                                                                5, 28, 30, new HTuple(), new HTuple(), out HTuple hvIndex0);
                HOperatorSet.AddMetrologyObjectCircleMeasure(hvOrbitInterHandle, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad * 0.5, hvOrbitOuterCircleRad * 0.45,
                                                                5, 28, 30, new HTuple(), new HTuple(), out HTuple hvIndex1);
                HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex0, "measure_transition", "negative");
                HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex1, "measure_transition", "positive");
                HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, "all", "min_score", 0.01);
                HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex0, "measure_select", "last");
                HOperatorSet.SetMetrologyObjectParam(hvOrbitInterHandle, hvIndex1, "measure_select", "first");

                HOperatorSet.ApplyMetrologyModel(hoOrbitOuterContourImage, hvOrbitInterHandle);

                HOperatorSet.GetMetrologyObjectResult(hvOrbitInterHandle, hvIndex0, "all", "result_type", "all_param", out hvOrbitInterParam);
                HOperatorSet.GetMetrologyObjectResult(hvOrbitInterHandle, hvIndex1, "all", "result_type", "all_param", out hvCoverCircleParam);

                if (hvOrbitInterParam.TupleLength() > 0)
                {
                    hvOrbitInterCircleRow = hvOrbitInterParam[0];
                    hvOrbitInterCircleCol = hvOrbitInterParam[1];
                    hvOrbitInterCircleRad = hvOrbitInterParam[2];
                }
                else
                {
                    hvOrbitInterCircleRow = hvOrbitOuterCircleRow;
                    hvOrbitInterCircleCol = hvOrbitOuterCircleCol;
                    hvOrbitInterCircleRad = hvOrbitOuterCircleRad;
                }


                HTuple TmpOrbitW = hvOrbitOuterCircleRad - hvOrbitInterCircleRad;

                HObject hoCircleInter, hoCircleOuter;
                HObject hoRect0, hoRect1, hoRing;
                HOperatorSet.GenCircle(out hoCircleInter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad - TmpOrbitW);
                HOperatorSet.GenCircle(out hoCircleOuter, hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, hvOrbitOuterCircleRad);

                HOperatorSet.GenRectangle1(out hoRect0, hvOrbitOuterCircleRow + (hvOrbitOuterCircleRad - TmpOrbitW), hvOrbitOuterCircleCol - hvOrbitOuterCircleRad,
                                                        hvOrbitOuterCircleRow + hvOrbitOuterCircleRad, hvOrbitOuterCircleCol + hvOrbitOuterCircleRad);

                HOperatorSet.GenRectangle1(out hoRect1, hvOrbitOuterCircleRow - hvOrbitOuterCircleRad, hvOrbitOuterCircleCol - hvOrbitOuterCircleRad,
                                                        hvOrbitOuterCircleRow - (hvOrbitOuterCircleRad - TmpOrbitW), hvOrbitOuterCircleCol + hvOrbitOuterCircleRad);

                HOperatorSet.Difference(hoCircleOuter, hoCircleInter, out hoRing);
                HOperatorSet.Union2(hoRing, hoRect0, out _hoOrbitMask);
                HOperatorSet.Union2(_hoOrbitMask, hoRect1, out _hoOrbitMask);
                HOperatorSet.ZoomRegion(_hoOrbitMask, out _hoOrbitMask, hvAccelerationFactor, hvAccelerationFactor);



                hoOrbitOuterContourRegion.Dispose();
                hoOrbitOuterContourImage.Dispose();
                hoCircleInter.Dispose();
                hoCircleOuter.Dispose();
                hoRect0.Dispose();
                hoRect1.Dispose();
                hoRing.Dispose();

            }
            else
            {
                HOperatorSet.GenRectangle1(out _hoOrbitMask, 0, 0, hvImageOriH, hvImageOriW);

                hvOrbitOuterCircleRow = 0.5 * hvImageOriH;
                hvOrbitOuterCircleCol = 0.5 * hvImageOriW;
                HOperatorSet.TupleMin2(hvOrbitOuterCircleRow, hvOrbitOuterCircleCol, out hvOrbitOuterCircleRad);
            }


            // 翘钉基准面掩码
            if (hvNailCircleParam.Length > 0 && hvOrbitOuterParam.Length > 0)
            {
                HObject hoNailCircle, hoCircleOuter;
                HOperatorSet.GenCircle(out hoNailCircle, hvCoarseCenterRow, hvCoarseCenterCol, hvCoarseRadius);
                HOperatorSet.GenCircle(out hoCircleOuter, hvOrbitOuterCircleRow * hvAccelerationFactor, hvOrbitOuterCircleCol * hvAccelerationFactor, hvOrbitOuterCircleRad * hvAccelerationFactor);

                HOperatorSet.Difference(hoNailCircle, hoCircleOuter, out _hoNailWarpBaseMask);
                HOperatorSet.Intersection(_hoValidMask, _hoNailWarpBaseMask, out _hoNailWarpBaseMask);

                hoNailCircle.Dispose(); hoCircleOuter.Dispose();
            }
            else
            {
                HOperatorSet.GenCircle(out _hoNailWarpBaseMask, hvOrbitOuterCircleRow * hvAccelerationFactor, hvOrbitOuterCircleCol * hvAccelerationFactor, hvOrbitOuterCircleRad * hvAccelerationFactor * 1.55);
                HOperatorSet.Difference(_hoValidMask, _hoNailWarpBaseMask, out _hoNailWarpBaseMask);
            }
            HOperatorSet.Difference(_hoNailWarpBaseMask, _hoOrbitMask, out _hoNailWarpBaseMask);


            HTuple scaleX, scaleY;
            int imageNum = _imageData.Count;
            if (imageNum > 0)
            {
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
            }
            else
            {
                scaleX = 1;
                scaleY = 1;
            }

            HOperatorSet.Intersection(_hoValidMask, _hoOrbitMask, out _hoOrbitMask);

            HOperatorSet.ZoomRegion(_hoOrbitMask, out _hoOrbitMask, 1 / scaleX, 1 / scaleY);


            hoROI_0.Dispose();
            hoImageReduced.Dispose();
            hoInvalidROI_0.Dispose();
            hoImageReducedMean.Dispose();

            //hoImageMean.Dispose();
            hoContours.Dispose();
            hoOrbitOuterContour.Dispose();

            hoCoarseNailCircleRegion.Dispose();
            hoImageResize.Dispose();
            hoImageResizeMean.Dispose();

            hoTemplateRegion.Dispose();


            return 0;
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


        public static class FeatureAlgorithms
        {

            public static int Sigmoid(HObject hoInImage, out HObject hoOutImage)
            {
                HObject hoOnes;

                HOperatorSet.GetImageSize(hoInImage, out HTuple hvCracksWidth, out HTuple hvCracksHeight);
                HOperatorSet.ScaleImage(hoInImage, out hoInImage, -1, 0);
                HOperatorSet.ExpImage(hoInImage, out hoInImage, "e");
                HOperatorSet.ScaleImage(hoInImage, out hoInImage, 1, 1);
                HOperatorSet.GenImageConst(out hoOnes, "real", hvCracksWidth, hvCracksHeight);
                HOperatorSet.ScaleImage(hoOnes, out hoOnes, 1, 1);
                HOperatorSet.DivImage(hoOnes, hoInImage, out hoOutImage, 1, 0);

                hoOnes.Dispose();

                return 0;
            }


            public static WarpResult GetWarpFeatureOld(HObject hoTileGrayImage, HObject hoTileHeightImage, HTuple hvCx, HTuple hvCy,
                                                    HTuple hvOrbitParam, HObject hoValidMask, MFDJC0_MeasureParam measureParam, double height_select = 0)
            {
                WarpResult result = new WarpResult();

                HObject hoDeepX, hoDeepY, hoDeepZ;
                HObject hoDeepXYZ, hoPlaneXYZ;
                HObject hoBaseRegion, hoNailRegion;
                HObject hoNailXYZ;
                HObject hoPeek;

                // 设置分辨率（毫米)
                HTuple scaleX, scaleY;
                if (measureParam.IntervalX > measureParam.IntervalY)
                {
                    scaleX = measureParam.IntervalX / measureParam.IntervalY;
                    scaleY = 1;
                }
                else
                {
                    scaleX = 1;
                    scaleY = measureParam.IntervalY / measureParam.IntervalX;
                }
                HTuple hvXp = new HTuple(((measureParam.IntervalX * 1.0) / scaleX) / 1000);
                HTuple hvYp = new HTuple(((measureParam.IntervalY * 1.0) / scaleY) / 1000);
                HTuple hvZp = new HTuple((measureParam.IntervalZ * 1.0) / 1000);

                //HOperatorSet.ZoomRegion(hoValidMask, out hoValidMask, scaleX, scaleY);

                HOperatorSet.GetImageSize(hoTileHeightImage, out HTuple hvImageW, out HTuple hvImageH);
                // 生成三通道TIFF
                HTuple hvRowD, hvColD;
                HOperatorSet.GetRegionPoints(hoTileHeightImage, out hvRowD, out hvColD);
                HTuple hvValueX = hvColD * hvXp;
                HTuple hvValueY = hvRowD * hvYp;
                HOperatorSet.GenImageConst(out hoDeepX, "real", hvImageW, hvImageH);
                HOperatorSet.GenImageConst(out hoDeepY, "real", hvImageW, hvImageH);
                HOperatorSet.SetGrayval(hoDeepX, hvRowD, hvColD, hvValueX);
                HOperatorSet.SetGrayval(hoDeepY, hvRowD, hvColD, hvValueY);
                HOperatorSet.ConvertImageType(hoTileHeightImage, out hoDeepZ, "real");
                HOperatorSet.ScaleImage(hoDeepZ, out hoDeepZ, hvZp, 0);
                HOperatorSet.Compose3(hoDeepX, hoDeepY, hoDeepZ, out hoDeepXYZ);

                HTuple hvCirRow = hvOrbitParam[0];
                HTuple hvCirCol = hvOrbitParam[1];
                HTuple hvCirRad = hvOrbitParam[2];
                // 定位密封钉区域
                HOperatorSet.GenCircle(out hoNailRegion, hvCirRow, hvCirCol, hvCirRad * 0.656);

                // 定位基准面
                HOperatorSet.GenCircle(out hoBaseRegion, hvCirRow, hvCirCol, hvCirRad * 1.55);
                HOperatorSet.Difference(hoValidMask, hoBaseRegion, out hoBaseRegion);

                HOperatorSet.AreaCenter(hoBaseRegion, out HTuple hvTmpArea, out HTuple hvTmpRow, out HTuple hvTmpCol);
                if (hvTmpArea.D == 0)
                {
                    Console.WriteLine("ERROR:failed to extract the region of base plane");

                    result.IsOk = false;

                    return result;
                }

                HTuple hvPland3d, hvSampP3d;
                HTuple hvPose, hvNormal;
                HTuple hvNail3d, hvNailRpT;
                HTuple hvMatRpT;
                HTuple hvZValue;
                HOperatorSet.ReduceDomain(hoDeepXYZ, hoBaseRegion, out hoPlaneXYZ);
                HOperatorSet.Decompose3(hoPlaneXYZ, out hoDeepX, out hoDeepY, out hoDeepZ);
                HOperatorSet.XyzToObjectModel3d(hoDeepX, hoDeepY, hoDeepZ, out hvPland3d);
                HOperatorSet.SampleObjectModel3d(hvPland3d, "fast", 0.05, new HTuple(), new HTuple(), out hvSampP3d);
                HOperatorSet.FitPrimitivesObjectModel3d(hvSampP3d, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                   (new HTuple("plane")).TupleConcat("least_squares"), out hvPland3d);
                HOperatorSet.GetObjectModel3dParams(hvPland3d, "primitive_parameter_pose", out hvPose);
                HOperatorSet.PoseInvert(hvPose, out hvPose);
                HOperatorSet.GetObjectModel3dParams(hvPland3d, "primitive_parameter", out hvNormal);
                if ((int)(new HTuple(((hvNormal.TupleSelect(2))).TupleLess(0))) != 0)
                {
                    HTuple hvFlip;
                    HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                    HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                }

                // 翘钉高度测量
                HOperatorSet.Intersection(hoValidMask, hoNailRegion, out hoNailRegion);
                HOperatorSet.AreaCenter(hoNailRegion, out hvTmpArea, out hvTmpRow, out hvTmpCol);
                if (hvTmpArea.D == 0)
                {
                    Console.WriteLine("ERROR:failed to extract the region of anchor!");

                    result.IsOk = false;

                    return result;
                }
                HOperatorSet.ReduceDomain(hoDeepXYZ, hoNailRegion, out hoNailXYZ);
                HOperatorSet.Decompose3(hoNailXYZ, out hoDeepX, out hoDeepY, out hoDeepZ);
                HOperatorSet.XyzToObjectModel3d(hoDeepX, hoDeepY, hoDeepZ, out hvNail3d);

                HOperatorSet.PoseToHomMat3d(hvPose, out hvMatRpT);
                HOperatorSet.AffineTransObjectModel3d(hvNail3d, hvMatRpT, out hvNailRpT);

                HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_z", out hvZValue);

                HTuple hvHeight = hvZValue.TupleMax();
                HOperatorSet.TupleFind(hvZValue, hvHeight, out HTuple hvIndex);
                HOperatorSet.GetObjectModel3dParams(hvNail3d, "point_coord_z", out HTuple hvRValue);
                HTuple hvDeepVal = hvRValue.TupleSelect(hvIndex);

                HOperatorSet.Threshold(hoDeepZ, out hoPeek, hvDeepVal - 0.0001, hvDeepVal + 0.0001);
                HOperatorSet.GetRegionPoints(hoPeek, out HTuple hvRows, out HTuple hvColumns);

                HTuple hvRow = hvRows.TupleSelect(0);
                HTuple hvColumn = hvColumns.TupleSelect(0);

                // 结果输出, 单位mm
                if (hvHeight.D < height_select)
                    result.IsOk = true;
                else
                    result.IsOk = false;
                result.Height = hvHeight.D;
                result.HighestPointRow = hvRow.D;
                result.HighestPointCol = hvColumn.D;


                hoDeepX.Dispose(); hoDeepY.Dispose(); hoDeepZ.Dispose();
                hoDeepXYZ.Dispose(); hoPlaneXYZ.Dispose();
                hoBaseRegion.Dispose(); hoNailRegion.Dispose();
                hoNailXYZ.Dispose();
                hoPeek.Dispose();


                return result;
            }


            public static WarpResult GetWarpFeature(HObject hoTileGrayImage, HObject hoTileHeightImage, HTuple hvCx, HTuple hvCy,
                                                    HTuple hvOrbitParam, HObject hoValidMask, HObject hoNailWarpBaseMask,
                                                    MFDJC0_MeasureParam measureParam, double height_select = 0)
            {
                WarpResult result = new WarpResult();

                HObject hoDeepX, hoDeepY, hoDeepZ;
                HObject hoDeepXYZ, hoPlaneXYZ;
                HObject hoNailRegion;
                HObject hoNailXYZ;
                HObject hoPeek = new HObject();

                // 加速系数
                HTuple hvAccelerationFactor = 4.0f;
                HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
                HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
                HOperatorSet.ZoomImageFactor(hoTileGrayImage, out hoTileGrayImage, hvScaleFactorW, hvScaleFactorH, "bilinear");
                HOperatorSet.ZoomImageFactor(hoTileHeightImage, out hoTileHeightImage, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
                HOperatorSet.ZoomRegion(hoNailWarpBaseMask, out hoNailWarpBaseMask, hvScaleFactorW, hvScaleFactorH);

                HObject hoIrregularRegion;
                HObject hoIrregularRegion0, hoIrregularRegion1, hoIrregularRegion2, hoIrregularMask;
                HObject hoValidMaskZoom;

                HOperatorSet.Threshold(hoTileHeightImage, out hoValidMaskZoom, measureParam.MinDepth, measureParam.MaxDepth);

                HOperatorSet.GenEmptyObj(out hoIrregularRegion);
                HOperatorSet.Threshold(hoTileHeightImage, out hoIrregularRegion0, 8888880, 8888880);
                HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion0, out hoIrregularRegion);
                HOperatorSet.Threshold(hoTileHeightImage, out hoIrregularRegion1, -2147483648, -2147483648);
                HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion1, out hoIrregularRegion);
                HOperatorSet.Threshold(hoTileHeightImage, out hoIrregularRegion2, 0, 0);
                HOperatorSet.ConcatObj(hoIrregularRegion, hoIrregularRegion2, out hoIrregularRegion);
                HOperatorSet.Union1(hoIrregularRegion, out hoIrregularMask);
                HOperatorSet.Difference(hoValidMaskZoom, hoIrregularMask, out hoValidMaskZoom);

                HOperatorSet.Intersection(hoNailWarpBaseMask, hoValidMaskZoom, out hoNailWarpBaseMask);
                hoIrregularRegion.Dispose();
                hoIrregularRegion0.Dispose(); hoIrregularRegion1.Dispose(); hoIrregularRegion2.Dispose(); hoIrregularMask.Dispose();


                // 设置分辨率（毫米)
                HTuple scaleX, scaleY;
                if (measureParam.IntervalX > measureParam.IntervalY)
                {
                    scaleX = measureParam.IntervalX / measureParam.IntervalY;
                    scaleY = 1;
                }
                else
                {
                    scaleX = 1;
                    scaleY = measureParam.IntervalY / measureParam.IntervalX;
                }
                HTuple hvXp = new HTuple(((measureParam.IntervalX * hvAccelerationFactor * 1.0) / scaleX) / 1000);
                HTuple hvYp = new HTuple(((measureParam.IntervalY * hvAccelerationFactor * 1.0) / scaleY) / 1000);
                HTuple hvZp = new HTuple((measureParam.IntervalZ * 1.0) / 1000);

                HOperatorSet.GetImageSize(hoTileHeightImage, out HTuple hvImageW, out HTuple hvImageH);
                // 生成三通道TIFF
                HTuple hvRowD, hvColD;
                HOperatorSet.GetRegionPoints(hoTileHeightImage, out hvRowD, out hvColD);
                HTuple hvValueX = hvColD * hvXp;
                HTuple hvValueY = hvRowD * hvYp;
                HOperatorSet.GenImageConst(out hoDeepX, "real", hvImageW, hvImageH);
                HOperatorSet.GenImageConst(out hoDeepY, "real", hvImageW, hvImageH);
                HOperatorSet.SetGrayval(hoDeepX, hvRowD, hvColD, hvValueX);
                HOperatorSet.SetGrayval(hoDeepY, hvRowD, hvColD, hvValueY);
                HOperatorSet.ConvertImageType(hoTileHeightImage, out hoDeepZ, "real");
                HOperatorSet.ScaleImage(hoDeepZ, out hoDeepZ, hvZp, 0);
                HOperatorSet.Compose3(hoDeepX, hoDeepY, hoDeepZ, out hoDeepXYZ);

                HTuple hvCirRow = hvOrbitParam[0] * hvScaleFactorW;
                HTuple hvCirCol = hvOrbitParam[1] * hvScaleFactorH;
                HTuple hvCirRad = hvOrbitParam[2] / hvAccelerationFactor;
                // 定位密封钉区域
                HOperatorSet.GenCircle(out hoNailRegion, hvCirRow, hvCirCol, hvCirRad);

                HOperatorSet.AreaCenter(hoNailWarpBaseMask, out HTuple hvTmpArea, out HTuple hvTmpRow, out HTuple hvTmpCol);
                if (hvTmpArea.D == 0)
                {
                    Console.WriteLine("ERROR:failed to extract the region of base plane");

                    result.IsOk = false;

                    return result;
                }

                HTuple hvPland3d, hvSampP3d;
                HTuple hvPose, hvNormal;
                HTuple hvNail3d, hvNailRpT;
                HTuple hvMatRpT;
                HTuple hvZValue;
                HOperatorSet.ReduceDomain(hoDeepXYZ, hoNailWarpBaseMask, out hoPlaneXYZ);
                HOperatorSet.Decompose3(hoPlaneXYZ, out hoDeepX, out hoDeepY, out hoDeepZ);
                HOperatorSet.XyzToObjectModel3d(hoDeepX, hoDeepY, hoDeepZ, out hvPland3d);
                HOperatorSet.SampleObjectModel3d(hvPland3d, "fast", 0.05, new HTuple(), new HTuple(), out hvSampP3d);
                HOperatorSet.FitPrimitivesObjectModel3d(hvSampP3d, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                   (new HTuple("plane")).TupleConcat("least_squares"), out hvPland3d);
                HOperatorSet.GetObjectModel3dParams(hvPland3d, "primitive_parameter_pose", out hvPose);
                HOperatorSet.PoseInvert(hvPose, out hvPose);
                HOperatorSet.GetObjectModel3dParams(hvPland3d, "primitive_parameter", out hvNormal);
                if ((int)(new HTuple(((hvNormal.TupleSelect(2))).TupleLess(0))) != 0)
                {
                    HTuple hvFlip;
                    HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                    HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                }

                // 翘钉高度测量
                HOperatorSet.Intersection(hoValidMaskZoom, hoNailRegion, out hoNailRegion);
                HOperatorSet.AreaCenter(hoNailRegion, out hvTmpArea, out hvTmpRow, out hvTmpCol);
                if (hvTmpArea.D == 0)
                {
                    Console.WriteLine("ERROR:failed to extract the region of anchor!");

                    result.IsOk = false;

                    return result;
                }
                HOperatorSet.ReduceDomain(hoDeepXYZ, hoNailRegion, out hoNailXYZ);
                HOperatorSet.Decompose3(hoNailXYZ, out hoDeepX, out hoDeepY, out hoDeepZ);
                HOperatorSet.XyzToObjectModel3d(hoDeepX, hoDeepY, hoDeepZ, out hvNail3d);

                HOperatorSet.PoseToHomMat3d(hvPose, out hvMatRpT);
                HOperatorSet.AffineTransObjectModel3d(hvNail3d, hvMatRpT, out hvNailRpT);

                // 原华视方法(方案一)
                //HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_z", out hvZValue);
                //HTuple hvMaxWarpHeightValue = hvZValue.TupleMax();
                //HOperatorSet.TupleFind(hvZValue, hvMaxWarpHeightValue, out HTuple hvIndex);
                //HOperatorSet.GetObjectModel3dParams(hvNail3d, "point_coord_z", out HTuple hvRValue);
                //HTuple hvDeepVal = hvRValue.TupleSelect(hvIndex);
                //HOperatorSet.Threshold(hoDeepZ, out hoPeek, hvDeepVal - 0.0001, hvDeepVal + 0.0001);
                //HOperatorSet.GetRegionPoints(hoPeek, out HTuple hvRows, out HTuple hvColumns);
                //HTuple hvMaxHeightRow = hvRows.TupleSelect(0);
                //HTuple hvMaxHeightColumn = hvColumns.TupleSelect(0);
                // 将3D对象中的xyz点云，转为z向高度图(方案二)
                HOperatorSet.RegionFeatures(hoNailRegion, "row1", out HTuple hvUpperLeftValueRow);
                HOperatorSet.RegionFeatures(hoNailRegion, "column1", out HTuple hvUpperLeftValueCol);
                HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_x", out HTuple hvPointXNoGlueT);
                HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_y", out HTuple hvPointYNoGlueT);
                HOperatorSet.GetObjectModel3dParams(hvNailRpT, "point_coord_z", out HTuple hvPointZNoGlueT);

                HTuple hvCols = ((hvPointXNoGlueT / hvXp)).TupleInt();
                HTuple hvRows = ((hvPointYNoGlueT / hvYp)).TupleInt();

                HOperatorSet.TupleMin(hvRows, out HTuple hvRowsMin);
                HOperatorSet.TupleMin(hvCols, out HTuple hvColsMin);

                HTuple hvOffsetRow = hvUpperLeftValueRow - hvRowsMin;
                HTuple hvOffsetCol = hvUpperLeftValueCol - hvColsMin;

                hvRows = (hvRows + hvOffsetRow).TupleInt();
                hvCols = (hvCols + hvOffsetCol).TupleInt();

                //剔除超出图像区域的点
                int hvPointCount = Math.Min(hvRows.TupleLength(), Math.Min(hvCols.TupleLength(), hvPointZNoGlueT.TupleLength()));
                int hvImageWInt = hvImageW.I;
                int hvImageHInt = hvImageH.I;
                List<int> hvValidRows = new List<int>(hvPointCount);
                List<int> hvValidCols = new List<int>(hvPointCount);
                List<double> hvValidPointZ = new List<double>(hvPointCount);
                for (int i = 0; i < hvPointCount; i++)
                {
                    int row = hvRows[i].I;
                    int col = hvCols[i].I;
                    double pointZ = hvPointZNoGlueT[i].D;
                    if (double.IsNaN(pointZ) || double.IsInfinity(pointZ))
                    {
                        continue;
                    }
                    if (row < 0 || row >= hvImageHInt || col < 0 || col >= hvImageWInt)
                    {
                        continue;
                    }

                    hvValidRows.Add(row);
                    hvValidCols.Add(col);
                    hvValidPointZ.Add(pointZ);
                }
                if (hvValidRows.Count <= 0)
                {
                    Console.WriteLine("ERROR:no valid transformed points for set_grayval");

                    result.IsOk = false;

                    return result;
                }
                hvRows = new HTuple(hvValidRows.ToArray());
                hvCols = new HTuple(hvValidCols.ToArray());
                HTuple hvPointZNoGlueValidT = new HTuple(hvValidPointZ.ToArray());

                HTuple hvWarpMinHeightValue = hvPointZNoGlueValidT.TupleMin();
                HTuple hvGlobalMaxWarpHeightValue = hvPointZNoGlueValidT.TupleMax();

                HTuple hvMaxHeightRows, hvMaxHeightColumns;
                HTuple hvArea, hvMaxHeightRow, hvMaxHeightColumn;
                HObject hoHeightImageAffineTrans, hoMaxHeightRegion;
                List<Polygon> polygons = new List<Polygon>();
                HOperatorSet.GenImageConst(out hoHeightImageAffineTrans, "real", hvImageW, hvImageH);
                HOperatorSet.ScaleImage(hoHeightImageAffineTrans, out hoHeightImageAffineTrans, 0, hvWarpMinHeightValue);
                HOperatorSet.SetGrayval(hoHeightImageAffineTrans, hvRows, hvCols, hvPointZNoGlueValidT);

                HOperatorSet.Threshold(hoHeightImageAffineTrans, out hoMaxHeightRegion, hvGlobalMaxWarpHeightValue - 0.0001, hvGlobalMaxWarpHeightValue + 0.0001);
                HOperatorSet.AreaCenter(hoMaxHeightRegion, out hvArea, out hvMaxHeightRow, out hvMaxHeightColumn);
                if (hvArea <= 0)
                {
                    HOperatorSet.Threshold(hoHeightImageAffineTrans, out hoMaxHeightRegion, hvGlobalMaxWarpHeightValue - 0.01, hvGlobalMaxWarpHeightValue + 0.01);
                    HOperatorSet.AreaCenter(hoMaxHeightRegion, out hvArea, out hvMaxHeightRow, out hvMaxHeightColumn);
                }
                HOperatorSet.GetRegionPoints(hoMaxHeightRegion, out hvMaxHeightRows, out hvMaxHeightColumns);

                //hvMaxHeightRow = hvMaxHeightRow * hvAccelerationFactor;
                //hvMaxHeightColumn = hvMaxHeightColumn * hvAccelerationFactor;
                hvMaxHeightRow = hvMaxHeightRows[(int)Math.Floor(hvMaxHeightRows.TupleLength() * 0.5)] * hvAccelerationFactor;
                hvMaxHeightColumn = hvMaxHeightColumns[(int)Math.Floor(hvMaxHeightColumns.TupleLength() * 0.5)] * hvAccelerationFactor;

                if (height_select < hvGlobalMaxWarpHeightValue)
                {
                    HObject hoWarpRegion, hoWarpRegions;
                    HOperatorSet.Threshold(hoHeightImageAffineTrans, out hoWarpRegion, height_select, hvGlobalMaxWarpHeightValue + 0.01);

                    HOperatorSet.ClosingCircle(hoWarpRegion, out hoWarpRegion, 5.5);
                    HOperatorSet.Connection(hoWarpRegion, out hoWarpRegions);
                    //HOperatorSet.SelectShapeStd(hoWarpRegion, out hoWarpRegion, "max_area", 70);
                    HOperatorSet.SelectShape(hoWarpRegions, out hoWarpRegions, "area", "and", 0, 9999999999999999999);
                    HOperatorSet.CountObj(hoWarpRegions, out HTuple hvRegionsNum);
                    //获取缺陷轮廓
                    if (hvRegionsNum > 0)
                    {
                        HTuple hvLocalMaxWarpHeightValue = 0;
                        for (int i = 0; i < hvRegionsNum; i++)
                        {
                            HOperatorSet.SelectObj(hoWarpRegions, out hoWarpRegion, i + 1);
                            HOperatorSet.AreaCenter(hoWarpRegion, out hvArea, out HTuple hvTmpValue0, out HTuple hvTmpValue1);
                            if (hvArea > 0)
                            {
                                HObject hoWarpRegionHeightImage;
                                HOperatorSet.ReduceDomain(hoHeightImageAffineTrans, hoWarpRegion, out hoWarpRegionHeightImage);
                                HOperatorSet.GrayFeatures(hoWarpRegion, hoWarpRegionHeightImage, "max", out HTuple hvTmpValue);
                                HOperatorSet.Threshold(hoWarpRegionHeightImage, out hoMaxHeightRegion, hvTmpValue - 0.01, hvTmpValue + 0.01);
                                HOperatorSet.AreaCenter(hoMaxHeightRegion, out hvArea, out HTuple hvTmpMaxHeightRow, out HTuple hvTmpMaxHeightColumn);
                                if (hvArea > 0)
                                {
                                    if (hvLocalMaxWarpHeightValue < hvTmpValue)
                                    {
                                        hvLocalMaxWarpHeightValue = hvTmpValue;
                                        hvMaxHeightRow = hvTmpMaxHeightRow * hvAccelerationFactor;
                                        hvMaxHeightColumn = hvTmpMaxHeightColumn * hvAccelerationFactor;
                                    }
                                }

                                HOperatorSet.ZoomRegion(hoWarpRegion, out hoWarpRegion, hvAccelerationFactor, hvAccelerationFactor);

                                Polygon polygon = new Polygon(hoWarpRegion);
                                polygons.Add(polygon);

                                hoWarpRegionHeightImage.Dispose();
                            }
                        }
                    }


                    hoWarpRegion.Dispose(); hoWarpRegions.Dispose();
                }

                // 结果输出, 单位mm
                if (hvGlobalMaxWarpHeightValue.D < height_select)
                    result.IsOk = true;
                else
                    result.IsOk = false;
                result.Height = hvGlobalMaxWarpHeightValue.D;
                result.HighestPointRow = hvMaxHeightRow.D;
                result.HighestPointCol = hvMaxHeightColumn.D;

                result.Polygons = polygons;


                hvMaxHeightRows.Dispose(); hvMaxHeightColumns.Dispose();

                hoValidMaskZoom.Dispose();
                hoHeightImageAffineTrans.Dispose(); hoMaxHeightRegion.Dispose();
                hoDeepX.Dispose(); hoDeepY.Dispose(); hoDeepZ.Dispose();
                hoDeepXYZ.Dispose(); hoPlaneXYZ.Dispose();
                hoNailRegion.Dispose();
                hoNailXYZ.Dispose();
                hoPeek.Dispose();


                return result;
            }


            public static OrbitResult GetOrbitFeature(HTuple hvCx, HTuple hvCy, HTuple hvOrbitParam,
                                                      MFDJC0_MeasureParam measureParam, bool nailCenterIsTrue,
                                                      double offset_select = 0)
            {
                OrbitResult result = new OrbitResult();

                HTuple scaleX, scaleY;
                if (measureParam.IntervalX > measureParam.IntervalY)
                {
                    scaleX = measureParam.IntervalX / measureParam.IntervalY;
                    scaleY = 1;
                }
                else
                {
                    scaleX = 1;
                    scaleY = measureParam.IntervalY / measureParam.IntervalX;
                }
                HTuple hvXp = new HTuple((measureParam.IntervalX * 1.0) / scaleX);
                HTuple hvYp = new HTuple((measureParam.IntervalY * 1.0) / scaleY);

                HTuple hvDistanceRow = (hvOrbitParam[0] - hvCy) * hvXp;
                HTuple hvDistanceCol = (hvOrbitParam[1] - hvCx) * hvYp;
                HTuple hvDistance = (hvDistanceRow * hvDistanceRow + hvDistanceCol * hvDistanceCol).TupleSqrt();

                result.NailCenterCol = hvCx.D;
                result.NailCenterRow = hvCy.D;
                result.OrbitCenterRow = hvOrbitParam[0].D;
                result.OrbitCenterCol = hvOrbitParam[1].D;
                result.OrbitRadius = hvOrbitParam[2].D;

                // 结果输出, 单位mm
                result.Offset = hvDistance.D * 0.001;

                if ((result.Offset < offset_select) && nailCenterIsTrue)
                    result.IsOk = true;
                else
                    result.IsOk = false;

                return result;
            }


            public static DefectResult GetCrackFeature(int bboxId, ImageData imageData, HObject hoOrbitMask,
                                                       MFDJC0_MeasureParam measureParam,
                                                       double area_select = 0, double length_select = 0,
                                                       double width_select = 0, double depth_select = 0)
            {
                DefectResult result = new DefectResult();

                HObject hoHeightImage;
                HObject hoValidMask, hoValidMaskZoom;
                HObject hoCracks, hoCrack;
                HObject hoCrackZ;
                HObject hoScaled;
                HObject hoSkeleton, hoContours, hoLines, hoLine;

                HTuple hvCrackCloud = new HTuple();
                HTuple hvConnCloud = new HTuple();
                HTuple hvSeleCloud = new HTuple();
                HTuple hvUnionCloud = new HTuple();
                HTuple hvSmthCloud = new HTuple();
                HTuple hvAffdCloud = new HTuple();
                HTuple hvValueZ = new HTuple();
                HTuple hvSortedZ = new HTuple();

                HOperatorSet.GenEmptyObj(out hoCrack);
                HOperatorSet.GenEmptyObj(out hoCrackZ);
                HOperatorSet.GenEmptyObj(out hoScaled);
                HOperatorSet.GenEmptyObj(out hoSkeleton);
                HOperatorSet.GenEmptyObj(out hoContours);
                HOperatorSet.GenEmptyObj(out hoLines);
                HOperatorSet.GenEmptyObj(out hoLine);


                HTuple scaleX, scaleY;
                if (imageData.hvIntervalX > imageData.hvIntervalY)
                {
                    scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                    scaleY = 1;
                }
                else
                {
                    scaleX = 1;
                    scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                }
                // 像素当量
                HTuple hvXp = new HTuple((imageData.hvIntervalX * 1.0) / scaleX);
                HTuple hvYp = new HTuple((imageData.hvIntervalY * 1.0) / scaleY);
                HTuple hvZp = new HTuple((imageData.hvIntervalZ * 1.0) / 1000);

                HTuple offsetX = imageData.OffsetX;
                HTuple offsetY = imageData.OffsetY;

                hoHeightImage = imageData.hoHeightImage.Clone();
                hoValidMask = imageData.hoValidMask.Clone();

                HOperatorSet.MoveRegion(hoOrbitMask, out hoOrbitMask, -offsetY, -offsetX);
                HOperatorSet.ScaleImage(hoHeightImage, out hoHeightImage, hvZp, 0);

                // 加速系数
                HTuple hvAccelerationFactor = 1.0f;
                HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
                HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
                HOperatorSet.ZoomImageFactor(hoHeightImage, out hoHeightImage, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
                HOperatorSet.ZoomRegion(hoOrbitMask, out hoOrbitMask, hvScaleFactorW, hvScaleFactorH);
                HOperatorSet.ZoomRegion(hoValidMask, out hoValidMask, hvScaleFactorW, hvScaleFactorH);
                HOperatorSet.Threshold(hoHeightImage, out hoValidMaskZoom, measureParam.MinDepth, measureParam.MaxDepth);
                HOperatorSet.Intersection(hoValidMask, hoValidMaskZoom, out hoValidMask);

                Box bbox = imageData.Boxes[bboxId];

                if (bbox.Seg.Valid == 0)
                {
                    result.Left = (bbox.Left + offsetX) * scaleX;
                    result.Top = (bbox.Top + offsetY) * scaleY;
                    result.Right = (bbox.Right + offsetX) * scaleX;
                    result.Bottom = (bbox.Bottom + offsetY) * scaleY;
                    result.ClassId = bbox.ClassId;
                    result.Confidence = bbox.Confidence;
                    result.InstanceId = bbox.InstanceId;
                    result.IsOk = false;

                    return result;
                }

                HTuple hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                // 修正halcon仿射变换与OpenCV的差异
                hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                HTuple hvthreshold = new HTuple(bbox.Seg.Thresh);
                GCHandle handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                IntPtr pointer = handle.AddrOfPinnedObject();
                HOperatorSet.GenImage1(out hoCracks, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                HOperatorSet.AffineTransImage(hoCracks, out hoCracks, hvaffineMatrix, "bilinear", "true");

                // sigmoid
                // Sigmoid(hoCracks, out hoCracks);

                HOperatorSet.Threshold(hoCracks, out hoCracks, hvthreshold, 255);
                HOperatorSet.ZoomRegion(hoCracks, out hoCracks, hvScaleFactorW, hvScaleFactorH);

                HOperatorSet.Intersection(hoCracks, hoOrbitMask, out hoCracks);
                HOperatorSet.Intersection(hoCracks, hoValidMask, out hoCracks);

                //region筛选
                HOperatorSet.Connection(hoCracks, out hoCracks);
                HOperatorSet.SelectShape(hoCracks, out hoCracks, "area", "and", 99, 9999999999999999999);

                HOperatorSet.CountObj(hoCracks, out HTuple hvNum);

                HTuple hvDepthFeature = new HTuple();
                HTuple hvWidthFeature = new HTuple();
                HTuple hvLengthFeature = new HTuple();
                HTuple hvAreaFeature = new HTuple();
                List<Polygon> polygons = new List<Polygon>();

                for (int i = 0; i < hvNum; i++)
                {
                    HOperatorSet.SelectObj(hoCracks, out hoCrack, i + 1);
                    HOperatorSet.ZoomRegion(hoCrack, out hoScaled, scaleX, scaleY);
                    HOperatorSet.FillUp(hoScaled, out hoScaled);

                    //获取缺陷轮廓
                    HObject hoTmpScaled0;
                    HOperatorSet.ZoomRegion(hoScaled, out hoTmpScaled0, hvAccelerationFactor, hvAccelerationFactor);
                    Polygon polygon = new Polygon(hoTmpScaled0, (int)(offsetX.D * scaleX.D), (int)(offsetY.D * scaleY.D));
                    polygons.Add(polygon);
                    hoTmpScaled0.Dispose();

                    HTuple hvRows, hvCols;
                    HOperatorSet.ReduceDomain(hoHeightImage, hoCrack, out hoCrackZ);

                    HOperatorSet.GetRegionPoints(hoCrackZ, out hvRows, out hvCols);
                    HTuple hvX = hvCols * imageData.hvIntervalX;
                    HTuple hvY = hvRows * imageData.hvIntervalY;
                    HTuple hvZ;

                    HOperatorSet.GetGrayval(hoCrackZ, hvRows, hvCols, out hvZ);
                    HOperatorSet.GenObjectModel3dFromPoints(hvX, hvY, hvZ, out hvCrackCloud);

                    //调整姿态
                    HTuple hvPlane;
                    HTuple hvPose, hvNormal;
                    HTuple hvPoseMat;
                    HOperatorSet.FitPrimitivesObjectModel3d(hvCrackCloud, (new HTuple("primitive_type")).TupleConcat("fitting_algorithm"),
                                                                          (new HTuple("plane")).TupleConcat("least_squares"), out hvPlane);
                    HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter_pose", out hvPose);
                    HOperatorSet.PoseInvert(hvPose, out hvPose);
                    HOperatorSet.GetObjectModel3dParams(hvPlane, "primitive_parameter", out hvNormal);
                    if ((int)(new HTuple(((hvNormal.TupleSelect(2))).TupleLess(0))) != 0)
                    {
                        HTuple hvFlip;
                        HOperatorSet.CreatePose(0, 0, 0, 180, 0, 0, "Rp+T", "gba", "point", out hvFlip);
                        HOperatorSet.PoseCompose(hvFlip, hvPose, out hvPose);
                    }
                    HOperatorSet.PoseToHomMat3d(hvPose, out hvPoseMat);
                    HOperatorSet.ConnectionObjectModel3d(hvCrackCloud, "distance_3d", 2 * hvXp, out hvConnCloud);
                    HOperatorSet.SelectObjectModel3d(hvConnCloud, "num_points", "and", 9, 9999999999999999999, out hvSeleCloud);
                    HOperatorSet.UnionObjectModel3d(hvSeleCloud, "points_surface", out hvUnionCloud);
                    HOperatorSet.GetObjectModel3dParams(hvUnionCloud, "num_points", out HTuple hvPointNum);
                    if (hvPointNum.I == 0)
                    {
                        continue;
                    }
                    HOperatorSet.SmoothObjectModel3d(hvUnionCloud, "mls", "mls_kNN", 199, out hvSmthCloud);
                    HOperatorSet.AffineTransObjectModel3d(hvSmthCloud, hvPoseMat, out hvAffdCloud);
                    //计算高度
                    HOperatorSet.GetObjectModel3dParams(hvAffdCloud, "point_coord_z", out hvValueZ);
                    HOperatorSet.TupleSort(hvValueZ, out hvSortedZ);
                    HOperatorSet.TupleInverse(hvSortedZ, out hvSortedZ);
                    HOperatorSet.TupleMean(hvSortedZ, out HTuple hvAvgZ);
                    HOperatorSet.TupleMin(hvSortedZ, out HTuple hvMinZ);
                    HOperatorSet.TupleLessElem(hvSortedZ, hvAvgZ, out HTuple hvMark0);
                    HOperatorSet.TupleFindFirst(hvMark0, 1, out HTuple hvB0);
                    HOperatorSet.TupleSelectRange(hvSortedZ, 0, hvB0, out HTuple hvTop0);
                    HOperatorSet.TupleMean(hvTop0, out HTuple hvDiv0);
                    HOperatorSet.TupleLessElem(hvTop0, hvDiv0, out HTuple hvMark1);
                    HOperatorSet.TupleFindFirst(hvMark1, 1, out HTuple hvB1);
                    HOperatorSet.TupleSelectRange(hvTop0, 0, hvB1, out HTuple hvTop2);
                    HOperatorSet.TupleMean(hvTop2, out HTuple hvDiv1);
                    HTuple hvTemp = hvDiv1 - hvMinZ;
                    hvDepthFeature = hvDepthFeature.TupleConcat(hvTemp);

                    //计算线宽
                    HOperatorSet.RegionFeatures(hoScaled, "inner_radius", out HTuple hvRadius);
                    hvWidthFeature = hvWidthFeature.TupleConcat((2 * hvRadius) * hvXp);

                    //计算线长
                    HObject hoTmpScaled;
                    HOperatorSet.ClosingCircle(hoScaled, out hoTmpScaled, 50);
                    HOperatorSet.Skeleton(hoTmpScaled, out hoSkeleton);
                    HOperatorSet.GenContoursSkeletonXld(hoSkeleton, out hoContours, 1, "filter");
                    HOperatorSet.UnionAdjacentContoursXld(hoContours, out hoLines, 10, 1, "attr_keep");
                    HOperatorSet.LengthXld(hoLines, out HTuple hvLengthes);
                    HOperatorSet.TupleSortIndex(hvLengthes, out HTuple hvIndex);
                    HOperatorSet.TupleLength(hvIndex, out HTuple hvSize);
                    HOperatorSet.SelectObj(hoLines, out hoLine, hvIndex[hvSize - 1] + 1);
                    HTuple hvLength = hvLengthes.TupleSelect(hvIndex[hvSize - 1]);
                    //如果有多段裂纹，计算裂纹间隙
                    HTuple hvGapDistance;
                    if (i + 2 <= hvNum)
                    {
                        HObject hoCrackNext, hoScaledNext;
                        HOperatorSet.SelectObj(hoCracks, out hoCrackNext, i + 2);
                        HOperatorSet.ZoomRegion(hoCrackNext, out hoScaledNext, scaleX, scaleY);
                        HOperatorSet.DistanceRrMinDil(hoScaled, hoScaledNext, out hvGapDistance);

                        hoCrackNext.Dispose(); hoScaledNext.Dispose();
                    }
                    else
                    {
                        hvGapDistance = 0;
                    }
                    hvLengthFeature = hvLengthFeature.TupleConcat((hvLength + hvGapDistance) * hvXp);
                    hoTmpScaled.Dispose();

                    //计算面积
                    HOperatorSet.RegionFeatures(hoScaled, "area", out HTuple TmpArea);
                    TmpArea = TmpArea * hvXp * hvXp;
                    hvAreaFeature = hvAreaFeature.TupleConcat(TmpArea);


                }

                if (hvDepthFeature.Length > 0 && hvWidthFeature.Length > 0 && hvLengthFeature.Length > 0 && hvAreaFeature.Length > 0)
                {
                    HTuple hvD = hvDepthFeature.TupleMax();
                    HTuple hvW = hvWidthFeature.TupleMax() * hvAccelerationFactor;
                    HTuple hvL = hvLengthFeature.TupleSum() * hvAccelerationFactor;
                    HTuple hvA = hvAreaFeature.TupleSum() * hvAccelerationFactor * hvAccelerationFactor;
                    HTuple hvC = (((bbox.Left + bbox.Right) / 2) + offsetX) * scaleX;
                    HTuple hvR = (((bbox.Top + bbox.Bottom) / 2) + offsetY) * scaleY;

                    // 结果输出, 单位mm
                    //result.DepthFeature = hvD.D * 0.001;
                    result.DepthFeature = hvD.D;
                    result.WidthFeature = hvW.D * 0.001;
                    result.LengthFeature = hvL.D * 0.001;
                    result.AreaFeature = hvA.D * 0.001 * 0.001;
                    result.CenterColFeature = hvC.D;
                    result.CenterRowFeature = hvR.D;

                    result.DefectPolygons = polygons;

                    result.Left = (bbox.Left + offsetX) * scaleX;
                    result.Top = (bbox.Top + offsetY) * scaleY;
                    result.Right = (bbox.Right + offsetX) * scaleX;
                    result.Bottom = (bbox.Bottom + offsetY) * scaleY;
                    result.ClassId = bbox.ClassId;
                    result.Confidence = bbox.Confidence;

                    result.InstanceId = bbox.InstanceId;

                    //if(hvD.D > depth_select && hvW.D > width_select && hvL.D > length_select && hvA.D > area_select)
                    if (result.DepthFeature > depth_select && result.AreaFeature > area_select &&
                        result.LengthFeature > length_select && result.WidthFeature > width_select)
                    {
                        result.IsOk = false;
                    }
                    else
                    {
                        result.IsOk = true;
                    }
                }
                else
                {
                    result.DefectPolygons = polygons;

                    result.Left = (bbox.Left + offsetX) * scaleX;
                    result.Top = (bbox.Top + offsetY) * scaleY;
                    result.Right = (bbox.Right + offsetX) * scaleX;
                    result.Bottom = (bbox.Bottom + offsetY) * scaleY;
                    result.ClassId = bbox.ClassId;
                    result.Confidence = bbox.Confidence;

                    result.InstanceId = bbox.InstanceId;

                    result.IsOk = true;
                }


                hoHeightImage.Dispose();
                hoValidMask.Dispose(); hoValidMaskZoom.Dispose();
                hoCracks.Dispose(); hoCrack.Dispose();
                hoCrackZ.Dispose();
                hoScaled.Dispose();
                hoSkeleton.Dispose(); hoContours.Dispose(); hoLines.Dispose(); hoLine.Dispose();

                hvCrackCloud.Dispose(); hvConnCloud.Dispose(); hvSeleCloud.Dispose(); hvUnionCloud.Dispose();
                hvSmthCloud.Dispose(); hvAffdCloud.Dispose(); hvValueZ.Dispose(); hvSortedZ.Dispose();

                return result;
            }

            public static DefectResult GetDiameterFeature(int bboxId, ImageData imageData, int image_type = 0, double select = 0,
                                                          double min_gray_value = 0, double max_gray_value = 0,
                                                          double area_lower_limit = 0, double area_upper_limit = 0)
            {
                DefectResult result = new DefectResult();

                HObject hoImage;
                HObject hoDefectMask;
                HObject hoRect, hoReduceImage;
                HObject hoRegion, hoRegions;
                HObject hoTmpRegions, hoTmpRegion;

                HOperatorSet.GenEmptyObj(out hoDefectMask);
                HOperatorSet.GenEmptyObj(out hoRect);
                HOperatorSet.GenEmptyObj(out hoReduceImage);
                HOperatorSet.GenEmptyObj(out hoRegion);
                HOperatorSet.GenEmptyObj(out hoRegions);
                HOperatorSet.GenEmptyObj(out hoTmpRegion);


                HTuple scaleX, scaleY;
                if (imageData.hvIntervalX > imageData.hvIntervalY)
                {
                    scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                    scaleY = 1;
                }
                else
                {
                    scaleX = 1;
                    scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                }
                // 像素当量
                HTuple hvXp = new HTuple((imageData.hvIntervalX * 1.0) / scaleX);
                HTuple hvYp = new HTuple((imageData.hvIntervalY * 1.0) / scaleY);
                HTuple hvZp = new HTuple((imageData.hvIntervalZ * 1.0) / 1000);

                HTuple offsetX = imageData.OffsetX;
                HTuple offsetY = imageData.OffsetY;

                //HOperatorSet.MoveRegion(hoValidMask, out hoValidMask, -offsetY, -offsetX);

                Box bbox = imageData.Boxes[bboxId];

                // 使用灰度图
                if (image_type == 0)
                {
                    hoImage = imageData.hoGrayImage.Clone();
                }
                // 使用深度图
                else if (image_type == 1)
                {
                    hoImage = imageData.hoHeightImage.Clone();
                }
                else
                {
                    Console.WriteLine("ERROR:GetDiameterFeature: image_type error");

                    result.Left = (bbox.Left + offsetX) * scaleX;
                    result.Top = (bbox.Top + offsetY) * scaleY;
                    result.Right = (bbox.Right + offsetX) * scaleX;
                    result.Bottom = (bbox.Bottom + offsetY) * scaleY;
                    result.ClassId = bbox.ClassId;
                    result.Confidence = bbox.Confidence;
                    result.InstanceId = bbox.InstanceId;
                    result.IsOk = false;

                    return result;
                }

                if (bbox.Seg.Valid == 1)
                {
                    HTuple hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                    // 修正halcon仿射变换与OpenCV的差异
                    hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                    hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                    hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                    hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                    hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                    hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                    HTuple hvthreshold = new HTuple(bbox.Seg.Thresh);
                    GCHandle handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                    IntPtr pointer = handle.AddrOfPinnedObject();
                    HOperatorSet.GenImage1(out hoDefectMask, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                    HOperatorSet.AffineTransImage(hoDefectMask, out hoDefectMask, hvaffineMatrix, "bilinear", "true");

                    // sigmoid
                    // Sigmoid(hoDefectMask, out hoDefectMask);

                    HOperatorSet.Threshold(hoDefectMask, out hoDefectMask, hvthreshold, 255);
                }

                HOperatorSet.RegionFeatures(hoDefectMask, "area", out HTuple TmpArea);
                if (TmpArea < 0 || bbox.Seg.Valid == 0)
                {
                    HOperatorSet.GenRectangle1(out hoRect, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                    HOperatorSet.ReduceDomain(hoImage, hoRect, out hoReduceImage);
                    HOperatorSet.Threshold(hoReduceImage, out hoRegion, min_gray_value, max_gray_value);
                    if (image_type == 1)
                    {
                        HOperatorSet.Threshold(hoReduceImage, out hoTmpRegion, 8888880, 8888880);
                        HOperatorSet.Union2(hoRegion, hoTmpRegion, out hoRegion);
                    }
                    HOperatorSet.Connection(hoRegion, out hoRegions);
                    HOperatorSet.SelectShape(hoRegions, out hoRegions, "area", "and", area_lower_limit, area_upper_limit);
                    HOperatorSet.Union1(hoRegions, out hoRegions);
                    HOperatorSet.ClosingCircle(hoRegions, out hoDefectMask, 5.5);
                }

                HOperatorSet.ZoomRegion(hoDefectMask, out hoDefectMask, scaleX, scaleY);
                HOperatorSet.RegionFeatures(hoDefectMask, "inner_radius", out HTuple hvInnerRadius);
                HOperatorSet.RegionFeatures(hoDefectMask, "outer_radius", out HTuple hvOuterRadius);
                HOperatorSet.RegionFeatures(hoDefectMask, "area", out HTuple hvArea);

                if (hvOuterRadius.Length == 0)
                    hvOuterRadius = 0;
                if (hvInnerRadius.Length == 0)
                    hvInnerRadius = 0;
                if (hvArea.Length == 0)
                    hvArea = 0;

                HTuple hvOuterRadiusReal = (hvOuterRadius * 2) * hvXp;
                HTuple hvInnerRadiusReal = (hvInnerRadius * 2) * hvXp;
                HTuple hvAreaReal = hvArea * hvXp * hvYp;

                List<Polygon> polygons = new List<Polygon>();
                HOperatorSet.Connection(hoDefectMask, out hoTmpRegions);
                HOperatorSet.CountObj(hoTmpRegions, out HTuple hvNum);
                for (int i = 0; i < hvNum; i++)
                {
                    HOperatorSet.SelectObj(hoTmpRegions, out hoTmpRegion, i + 1);

                    //获取缺陷轮廓
                    Polygon polygon = new Polygon(hoTmpRegion, (int)(offsetX.D * scaleX.D), (int)(offsetY.D * scaleY.D));
                    polygons.Add(polygon);
                }

                // 结果输出, 单位mm
                if (hvOuterRadiusReal.D > 0)
                {
                    result.LengthFeature = hvOuterRadiusReal.D * 0.001;
                    result.Diameter = hvOuterRadiusReal.D * 0.001;
                }
                if (hvInnerRadiusReal.D > 0)
                {
                    result.WidthFeature = hvInnerRadiusReal.D * 0.001;
                }

                if (hvAreaReal.D > 0)
                    result.AreaFeature = hvAreaReal.D * 0.001 * 0.001;
                result.DefectPolygons = polygons;

                result.Left = (bbox.Left + offsetX) * scaleX;
                result.Top = (bbox.Top + offsetY) * scaleY;
                result.Right = (bbox.Right + offsetX) * scaleX;
                result.Bottom = (bbox.Bottom + offsetY) * scaleY;
                result.ClassId = bbox.ClassId;
                result.Confidence = bbox.Confidence;

                result.InstanceId = bbox.InstanceId;

                if (result.Diameter > select)
                {
                    result.IsOk = false;
                }
                else
                {
                    result.IsOk = true;
                }

                hoImage.Dispose();
                hoDefectMask.Dispose();
                hoRect.Dispose(); hoReduceImage.Dispose();
                hoRegion.Dispose(); hoRegions.Dispose();
                hoTmpRegions.Dispose(); hoTmpRegion.Dispose();


                return result;
            }

            public static DefectResult GetLengthFeature(int bboxId, ImageData imageData, int image_type = 0, double select = 0,
                                                          double min_gray_value = 0, double max_gray_value = 0)
            {
                DefectResult result = new DefectResult();

                HObject hoImage;
                HObject hoDefectMask;
                HObject hoRect, hoReduceImage;
                HObject hoRegion = new HObject();
                HObject hoRegions = new HObject();
                HObject hoTmpRegion;

                HOperatorSet.GenEmptyObj(out hoDefectMask);
                HOperatorSet.GenEmptyObj(out hoTmpRegion);


                HTuple scaleX, scaleY;
                if (imageData.hvIntervalX > imageData.hvIntervalY)
                {
                    scaleX = imageData.hvIntervalX / imageData.hvIntervalY;
                    scaleY = 1;
                }
                else
                {
                    scaleX = 1;
                    scaleY = imageData.hvIntervalY / imageData.hvIntervalX;
                }
                // 像素当量
                HTuple hvXp = new HTuple((imageData.hvIntervalX * 1.0) / scaleX);
                HTuple hvYp = new HTuple((imageData.hvIntervalY * 1.0) / scaleY);
                HTuple hvZp = new HTuple((imageData.hvIntervalZ * 1.0) / 1000);

                HTuple offsetX = imageData.OffsetX;
                HTuple offsetY = imageData.OffsetY;

                Box bbox = imageData.Boxes[bboxId];

                // 使用灰度图
                if (image_type == 0)
                {
                    hoImage = imageData.hoGrayImage.Clone();
                }
                // 使用深度图
                else if (image_type == 1)
                {
                    hoImage = imageData.hoHeightImage.Clone();
                }
                else
                {
                    Console.WriteLine("ERROR:GetDiameterFeature: image_type error");

                    result.Left = (bbox.Left + offsetX) * scaleX;
                    result.Top = (bbox.Top + offsetY) * scaleY;
                    result.Right = (bbox.Right + offsetX) * scaleX;
                    result.Bottom = (bbox.Bottom + offsetY) * scaleY;
                    result.ClassId = bbox.ClassId;
                    result.Confidence = bbox.Confidence;
                    result.InstanceId = bbox.InstanceId;
                    result.IsOk = false;

                    return result;
                }

                if (bbox.Seg.Valid == 1)
                {
                    HTuple hvaffineMatrix = new HTuple(bbox.Seg.AffineMatrix);
                    // 修正halcon仿射变换与OpenCV的差异
                    hvaffineMatrix[0] = bbox.Seg.AffineMatrix[4];
                    hvaffineMatrix[1] = bbox.Seg.AffineMatrix[3];
                    hvaffineMatrix[2] = bbox.Seg.AffineMatrix[5];
                    hvaffineMatrix[3] = bbox.Seg.AffineMatrix[1];
                    hvaffineMatrix[4] = bbox.Seg.AffineMatrix[0];
                    hvaffineMatrix[5] = bbox.Seg.AffineMatrix[2];

                    HTuple hvthreshold = new HTuple(bbox.Seg.Thresh);
                    GCHandle handle = GCHandle.Alloc(bbox.Seg.Data, GCHandleType.Pinned);
                    IntPtr pointer = handle.AddrOfPinnedObject();
                    HOperatorSet.GenImage1(out hoDefectMask, "real", bbox.Seg.Width, bbox.Seg.Height, pointer);
                    HOperatorSet.AffineTransImage(hoDefectMask, out hoDefectMask, hvaffineMatrix, "bilinear", "true");

                    // sigmoid
                    // Sigmoid(hoDefectMask, out hoDefectMask);

                    HOperatorSet.Threshold(hoDefectMask, out hoDefectMask, hvthreshold, 255);
                }
                HOperatorSet.RegionFeatures(hoDefectMask, "area", out HTuple TmpArea);
                if (TmpArea < 0 || bbox.Seg.Valid == 0)
                {
                    //断焊
                    HOperatorSet.GenRectangle1(out hoRect, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                    HOperatorSet.ReduceDomain(hoImage, hoRect, out hoReduceImage);
                    HOperatorSet.Threshold(hoReduceImage, out hoRegion, min_gray_value, max_gray_value);
                    HOperatorSet.ClosingCircle(hoRegion, out hoRegion, 7.5);
                    HOperatorSet.OpeningCircle(hoRegion, out hoDefectMask, 7.5);

                    hoRect.Dispose(); hoReduceImage.Dispose();
                }

                HOperatorSet.Connection(hoDefectMask, out hoDefectMask);
                HOperatorSet.RegionFeatures(hoDefectMask, "area", out HTuple hvAreas);
                HOperatorSet.ZoomRegion(hoDefectMask, out hoDefectMask, scaleX, scaleY);
                HOperatorSet.TupleSortIndex(hvAreas, out HTuple hvAreaIndices);
                HOperatorSet.TupleLength(hvAreaIndices, out HTuple hvAN);

                //第一条焊迹
                HObject hoContours = new HObject();
                HObject hoSkeleton = new HObject();
                HObject hoLines = new HObject();
                HObject hoLine1 = new HObject();
                HObject hoObject1 = new HObject();
                HTuple hvLength = 0;
                if (hvAN.D >= 1)
                {
                    HOperatorSet.SelectObj(hoDefectMask, out hoObject1, hvAreaIndices.TupleSelect(hvAN - 1) + 1);
                    HOperatorSet.Skeleton(hoObject1, out hoSkeleton);
                    HOperatorSet.GenContoursSkeletonXld(hoSkeleton, out hoContours, 1, "filter");
                    HOperatorSet.UnionAdjacentContoursXld(hoContours, out hoLines, 10, 1, "attr_keep");
                    HOperatorSet.LengthXld(hoLines, out HTuple hvLengthes);
                    HOperatorSet.TupleSortIndex(hvLengthes, out HTuple hvLenIndices);
                    HOperatorSet.TupleLength(hvLenIndices, out HTuple hvLN);
                    HOperatorSet.SelectObj(hoLines, out hoLine1, hvLenIndices.TupleSelect(hvLN - 1) + 1);
                    hvLength = hvLengthes.TupleSelect(hvLenIndices.TupleSelect(hvLN - 1));
                }
                // 第二条焊迹
                if (hvAN.D >= 2)
                {
                    HObject hoObject2;
                    HObject hoLine2;
                    HOperatorSet.SelectObj(hoDefectMask, out hoObject2, hvAreaIndices.TupleSelect(hvAN - 2) + 1);
                    HOperatorSet.Skeleton(hoObject2, out hoSkeleton);
                    HOperatorSet.GenContoursSkeletonXld(hoSkeleton, out hoContours, 1, "filter");
                    HOperatorSet.UnionAdjacentContoursXld(hoContours, out hoLines, 10, 1, "attr_keep");
                    HOperatorSet.LengthXld(hoLines, out HTuple hvLengthes);
                    HOperatorSet.TupleSortIndex(hvLengthes, out HTuple hvLenIndices);
                    HOperatorSet.TupleLength(hvLenIndices, out HTuple hvLN);
                    HOperatorSet.SelectObj(hoLines, out hoLine2, hvLenIndices.TupleSelect(hvLN - 1) + 1);
                    HOperatorSet.DistanceRrMinDil(hoObject1, hoObject2, out HTuple hvDistance);
                    hvLength = hvLength + hvLengthes.TupleSelect(hvLenIndices.TupleSelect(hvLN - 1)) + hvDistance;

                    hoObject2.Dispose();
                    hoLine2.Dispose();
                }
                if (hvLength.Length == 0)
                {
                    hvLength = 0;
                }
                if (hvAreas.Length == 0)
                {
                    hvAreas = 0;
                }


                HTuple hvLengthReal = hvLength * hvXp;
                HTuple hvAreaReal = (hvAreas.TupleSum()) * hvXp * hvYp;

                List<Polygon> polygons = new List<Polygon>();
                HOperatorSet.CountObj(hoDefectMask, out HTuple hvNum);
                for (int i = 0; i < hvNum; i++)
                {
                    HOperatorSet.SelectObj(hoDefectMask, out hoTmpRegion, i + 1);
                    //获取缺陷轮廓
                    Polygon polygon = new Polygon(hoTmpRegion, (int)(offsetX.D * scaleX.D), (int)(offsetY.D * scaleY.D));

                    polygons.Add(polygon);
                }

                // 结果输出, 单位mm
                if (hvLengthReal.D > 0)
                    result.LengthFeature = hvLengthReal.D * 0.001;
                if (hvAreaReal.D > 0)
                    result.AreaFeature = hvAreaReal.D * 0.001 * 0.001;
                result.DefectPolygons = polygons;

                result.Left = (bbox.Left + offsetX) * scaleX;
                result.Top = (bbox.Top + offsetY) * scaleY;
                result.Right = (bbox.Right + offsetX) * scaleX;
                result.Bottom = (bbox.Bottom + offsetY) * scaleY;
                result.ClassId = bbox.ClassId;
                result.Confidence = bbox.Confidence;

                result.InstanceId = bbox.InstanceId;

                if (result.LengthFeature > select)
                {
                    result.IsOk = false;
                }
                else
                {
                    result.IsOk = true;
                }



                hoObject1.Dispose();
                hoContours.Dispose();
                hoSkeleton.Dispose();
                hoLines.Dispose();
                hoLine1.Dispose();

                hoImage.Dispose();
                hoDefectMask.Dispose();
                hoRegion.Dispose(); hoRegions.Dispose();
                hoTmpRegion.Dispose();


                return result;
            }
        }


        /// <summary>
        /// 翘钉检测
        /// </summary>
        public WarpResult GetWarpFeatureWrapper(HObject hoTileGrayImage, HObject hoTileHeightImage, HTuple hvCx, HTuple hvCy, HTuple hvOrbitParam)
        {
            WarpResult result = new WarpResult();

            var defect = _defectList.FirstOrDefault(d => d.Name == "翘钉");
            if (defect == null)
            {
                Console.WriteLine("ERROR:请在FeatureConfig.json配置文件的'defect_list'中配置翘钉检测算法。");
                return result;
            }

            var algorithm = _algorithmList.FirstOrDefault(a => a.Name == defect.AlgName);
            if (algorithm == null)
            {
                Console.WriteLine("ERROR:请在FeatureConfig.json配置文件的'algorithm_list'中申明翘钉检测算法。");
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

            result = (WarpResult)method.Invoke(null, validArgs.ToArray());

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
            _measureResult.Warp = new WarpResult();
            _measureResult.Orbit = new OrbitResult();
            _measureResult.Defects.Clear();

            int imageNum = _imageData.Count;

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

            HObject hoTileGrayImage, hoTileHeightImage;
            HOperatorSet.GenEmptyObj(out hoTileGrayImage);
            HOperatorSet.GenEmptyObj(out hoTileHeightImage);
            // 拼接图片
            ConcateImages(out hoTileGrayImage, out hoTileHeightImage);

            // 定位焊钉中心和焊迹的掩码
            HTuple hvCx, hvCy;
            HTuple hvOrbitParam;
            GetNailCenterAndOrbitMask(hoTileGrayImage, hoTileHeightImage, out hvCx, out hvCy, out hvOrbitParam);

            try
            {
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

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            //if (_measureResult.GrayImage.Data != IntPtr.Zero)
            //    _measureResult.GrayImage.Dispose();
            //if (_measureResult.HeightImage.Data != IntPtr.Zero)
            //    _measureResult.HeightImage.Dispose();

            _measureResult.GrayImage = HobjectToMat(hoTileGrayImage, ImageType.Gray);
            _measureResult.HeightImage = HobjectToMat(hoTileHeightImage, ImageType.Depth);

            hoTileGrayImage.Dispose();
            hoTileHeightImage.Dispose();

            return 0;
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

            try
            {
                _measureParam = param.DeepCopy();

                IntPtr objInfo = IntPtr.Zero;
                int objectNum = 0;

                Mat grayImage, heightImage;
                IntPtr grayImagePtr, heightImagePtr;

                // 将C#数组格式的图片数据转为OpenCvSharp Mat对象
                int statusGrayDate, statusHeightData;
                statusGrayDate = ConvertListToMat(grayDate, ImageType.Gray, out grayImage);
                statusHeightData = ConvertListToMat(heightData, ImageType.Depth, out heightImage);

                if (statusGrayDate == 0 && statusHeightData == 0)
                {
                    if (_measureParam.IsFlip)
                    {
                        Cv2.Flip(grayImage, grayImage, FlipMode.X);
                        Cv2.Flip(heightImage, heightImage, FlipMode.X);
                    }

                    int inGw = grayImage.Cols;
                    int inGh = grayImage.Rows;
                    int inGc = grayImage.Channels();
                    grayImagePtr = grayImage.Data;

                    int inDw = heightImage.Cols;
                    int inDh = heightImage.Rows;
                    int inDc = heightImage.Channels();
                    heightImagePtr = heightImage.Data;

                    int inGtype = (int)grayImage.Type();
                    int inDtype = (int)heightImage.Type();

                    int state = SealingNailsSDK.Pipeline(_deepLearningHandle,
                                                         grayImagePtr, inGw, inGh, inGc, inGtype,
                                                         heightImagePtr, inDw, inDh, inDc, inDtype,
                                                         out objInfo, out objectNum);

                    PourObjectInfo(objInfo, objectNum,
                                   grayImagePtr, inGw, inGh, inGc, inGtype,
                                   heightImagePtr, inDw, inDh, inDc, inDtype,
                                   out ImageData imageData);

                    state = SealingNailsSDK.CleanUpResult(_deepLearningHandle, ref objInfo);

                    //if (grayImage.Data != IntPtr.Zero)
                    //    grayImage.Dispose();
                    //if (heightImage.Data != IntPtr.Zero)
                    //    heightImage.Dispose();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }


            return 0;
        }



        /// <summary>
        /// 绘制结果
        /// </summary>
        public int CvDrawResult(MFDJC0_MeasureResult measureResult, bool showGuides = false)
        {
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



        /// <summary>
        /// 算法配置参数
        /// </summary>
        [Serializable]
        public class MFDJC0_MeasureParam
        {
            //模型参数
            private string _modelConfigPath = "./SealingNailsSDK/models/model.json";
            private string _modelPath = "./SealingNailsSDK/models/model.kmodel";
            private int _batchSize = 1;
            private DeviceType _deviceType = DeviceType.DEVICE_GPU;
            private ModelType _modelType = ModelType.MODEL_DETECTION_SEG;
            private double _confidenceThreshold = 0.35;
            private double _ioUThreshold = 0.45;
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


            //缺陷特征参数
            private string _featureConfigPath = "./SealingNailsSDK/ini/FeatureConfig.json";
            //private string _templateModelPath = "./SealingNailsSDK/models/NailCenterModel";
            private string _templateModelPath = "./SealingNailsSDK/models/NailCenterNCCModel";

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
            /// 完整的灰度图
            /// </summary>
            public Mat GrayImage { get; set; }

            /// <summary>
            /// 完整的高度图
            /// </summary>
            public Mat HeightImage { get; set; }

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

                HoValidMask.Dispose();
                HoIrregularMask.Dispose();
            }
        }

    }
}
