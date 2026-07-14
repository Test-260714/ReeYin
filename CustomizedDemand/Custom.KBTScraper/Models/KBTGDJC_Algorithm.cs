using Custom.KBTScraper.Models;
using HalconDotNet;
using Microsoft.VisualBasic.Logging;
using MongoDB.Bson;
using OpenCvSharp;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.CustomProject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Shapes;
using static Custom.KBTScraper.Models.KBTGDJC_Algorithm;
using static OpenCvSharp.FileStorage;
using static System.Windows.Forms.AxHost;

namespace Custom.KBTScraper.Models
{

    using DeepLearningHandle = System.IntPtr;

    public class KBTGDJC_Algorithm : ICustomAlgo
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

        internal class ScraperDefectsSDK
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

        private DeepLearningHandle _deepLearningHandle;

        private ImageData _imageData;
        private KBTGDJC_MeasureParam _mParam;
        private NativeModelConfig _config;

        /// <summary>
        /// 算法参数（公开属性，供多个页面共享绑定）
        /// </summary>
        public KBTGDJC_MeasureParam MParam
        {
            get { return _mParam; }
            set { _mParam = value; }
        }

        private Dictionary<int, string> _categories;
        private int _segCategoryNum;

        private HTuple _hvHeightImageGlobalMinValue;
        private HTuple _hvHeightImageGlobalMaxValue;

        private HObject _hoOrbitMask;
        private HObject _hoValidMask;
        private HObject _hoIrregularMask;
        
        private KBTGDJC_MeasureResult _measureResult;

        private bool _disposed = false;

        public KBTGDJC_Algorithm(KBTGDJC_MeasureParam mParam)
        {
            _imageData = new ImageData();
            _mParam = mParam;

            try 
            {
                LoadModelConfig(mParam);
                _deepLearningHandle = ScraperDefectsSDK.CreateModel(ref _config);
                int state = ScraperDefectsSDK.InitRuntime(_deepLearningHandle, ref _config);

                ParseModelConfig(_config.ModelPath.Replace(".kmodel", ".json"));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void DestroyModel()
        {
            int state = ScraperDefectsSDK.DestroyModel(_deepLearningHandle);
        }

        ~KBTGDJC_Algorithm()
        {
            int state = ScraperDefectsSDK.DestroyModel(_deepLearningHandle);

            _hvHeightImageGlobalMinValue.Dispose();
            _hvHeightImageGlobalMaxValue.Dispose();

            Dispose();
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                _imageData?.Dispose();

                //_hoValidMask.Dispose();
                //_hoOrbitMask.Dispose();
                //_hoIrregularMask.Dispose();

            }

            _disposed = true;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void LoadModelConfig(KBTGDJC_MeasureParam config)
        {
            _config.ModelPath = config.ModelPath;
            _config.BatchSize = config.BatchSize;
            _config.DeviceType = (DeviceType)config.DeviceType;
            _config.ModelType = (ModelType)config.ModelType;
            _config.ConfidenceThreshold = (float)config.ConfidenceThreshold;
            _config.IoUThreshold = (float)config.IoUThreshold;
            _config.KeypointThreshold = 0.5f;
            _config.SegmentationThreshold = (float)config.SegmentationThreshold;
        }


        /// <summary>
        /// 解析模型配置文件
        /// </summary>
        /// <param name="modelConfigPath"></param>
        /// <returns></returns>
        private int ParseModelConfig(string modelConfigPath)
        {

            _categories = new Dictionary<int, string>();

            string json = File.ReadAllText(modelConfigPath);
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
                    continue;
                }
                else
                {
                    _categories[category.Id] = category.Name;
                }
            }

            return 0;
        }


        /// <summary>
        /// AI模型推理图像数据
        /// </summary>
        /// <param name="grayData"></param>
        /// <param name="heightData"></param>
        /// <param name="mParam"></param>
        /// <returns></returns>
        public int Process(List<float[]> grayData, List<float[]> heightData, KBTGDJC_MeasureParam mParam)
        {
            try 
            {
                _mParam = mParam;

                IntPtr objInfo = IntPtr.Zero;
                int objectNum = 0;

                Mat grayImage, heightImage;
                IntPtr grayImagePtr, heightImagePtr;

                // 将C#数组格式的图片数据转为OpenCvSharp Mat对象
                int statusGrayData, statusHeightData;
                statusGrayData = Common_Algorithm.ConvertListToMat(grayData, ImageType.Gray, out grayImage);
                statusHeightData = Common_Algorithm.ConvertListToMat(heightData, ImageType.Depth, out heightImage);

                if (statusGrayData == 0 && statusHeightData == 0)
                {
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

                    int state = ScraperDefectsSDK.Pipeline(_deepLearningHandle,
                                                            grayImagePtr, inGw, inGh, inGc, inGtype,
                                                            heightImagePtr, inDw, inDh, inDc, inDtype,
                                                            out objInfo, out objectNum);

                    PourObjectInfo(objInfo, objectNum, grayImagePtr, inGw, inGh, inGc, inGtype,
                                   heightImagePtr, inDw, inDh, inDc, inDtype, out ImageData imageData);

                    state = ScraperDefectsSDK.CleanUpResult(_deepLearningHandle, ref objInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

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
            HOperatorSet.ScaleImage(hoHeightImage, out hoHeightImage, _mParam.IntervalZ * 10, 0);
            _mParam.MinDepth = _mParam.MinDepth * _mParam.IntervalZ * 10;
            _mParam.MaxDepth = _mParam.MaxDepth * _mParam.IntervalZ * 10;
            _mParam.IntervalZ = _mParam.IntervalZ / (_mParam.IntervalZ * 10);

            imageData.hoGrayImage = hoGrayImage;
            imageData.hoHeightImage = hoHeightImage;

            imageData.hoValidMask = Common_Algorithm.GetLocalDepthValidMask(hoHeightImage, _mParam.MinDepth, _mParam.MaxDepth);

            imageData.hvIntervalX = _mParam.IntervalX;
            imageData.hvIntervalY = _mParam.IntervalY;
            imageData.hvIntervalZ = _mParam.IntervalZ;

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
            _imageData = imageData;

            return 0;
        }

        /// <summary>
        /// 获取缺陷结果
        /// </summary>
        /// <returns></returns>
        public KBTGDJC_MeasureResult GetDefectsResult()
        {
            _measureResult = new KBTGDJC_MeasureResult();
            _measureResult.Defects.Clear();

            HTuple scaleX;
            HTuple scaleY;

            if (_imageData.hvIntervalX < _imageData.hvIntervalY)
            {
                scaleX = _imageData.hvIntervalX / _imageData.hvIntervalY;
                scaleY = 1;
            }
            else if (_imageData.hvIntervalX < _imageData.hvIntervalY)
            {
                scaleX = 1;
                scaleY = _imageData.hvIntervalY / _imageData.hvIntervalX;
            }
            else
            {
                scaleX = 1;
                scaleY = 1;
            }

            HOperatorSet.ZoomImageFactor(_imageData.hoGrayImage, out HObject hoZoomGrayImage, scaleX, scaleY, "constant");
            HOperatorSet.ZoomImageFactor(_imageData.hoHeightImage, out HObject hoZoomHeightImage, scaleX, scaleY, "nearest_neighbor");


            try 
            {
                // 缺陷特征计算
                int defectCount = 0;

                int defectNum = _imageData.Boxes.Count;
                                            
                for (int idx = 0; idx < defectNum; idx++)
                {
                    int defectClassId = _imageData.Boxes[idx].ClassId;

                    string defectName = _categories[defectClassId];
                    DefectResult defectResult = GetDefectFeature(defectName, idx, _imageData, _mParam);

                    if (!defectResult.IsOk)
                    {
                        defectResult.ClassId = defectClassId;
                        defectResult.Categories = _categories;
                        defectResult.InstanceId = defectCount;
                        defectCount += 1;
                        _measureResult.Defects.Add(defectResult);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            _measureResult.GrayImage = Common_Algorithm.HobjectToMat(hoZoomGrayImage, ImageType.Gray);
            _measureResult.HeightImage = Common_Algorithm.HobjectToMat(hoZoomHeightImage, ImageType.Depth);

            return _measureResult;
        }


        /// <summary>
        /// sigmoid变换
        /// </summary>
        /// <param name="hoInImage"></param>
        /// <param name="hoOutImage"></param>
        /// <returns></returns>
        public static int Sigmoid(HObject hoInImage, out HObject hoOutImage)
        {

            HOperatorSet.GetImageSize(hoInImage, out HTuple hvCracksWidth, out HTuple hvCracksHeight);
            HOperatorSet.ScaleImage(hoInImage, out hoInImage, -1, 0);
            HOperatorSet.ExpImage(hoInImage, out hoInImage, "e");
            HOperatorSet.ScaleImage(hoInImage, out hoInImage, 1, 1);
            HOperatorSet.GenImageConst(out HObject hoOnes, "real", hvCracksWidth, hvCracksHeight);
            HOperatorSet.ScaleImage(hoOnes, out hoOnes, 1, 1);
            HOperatorSet.DivImage(hoOnes, hoInImage, out hoOutImage, 1, 0);

            hoOnes.Dispose();

            return 0;
        }


        /// <summary>
        /// 获取缺陷特征
        /// </summary>
        /// <param name="bboxId"></param>
        /// <param name="imageData"></param>
        /// <param name="hoOrbitMask"></param>
        /// <param name="measureParam"></param>
        /// <returns></returns>
        public static DefectResult GetDefectFeature(string DefectName, int bboxId, ImageData imageData, KBTGDJC_MeasureParam measureParam)
        {
            DefectResult defectResult = new DefectResult();

            HObject hoHeightImage;
            HObject hoGrayImage;
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
            if (imageData.hvIntervalX < imageData.hvIntervalY)
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

            hoHeightImage = imageData.hoHeightImage.Clone();
            hoValidMask = imageData.hoValidMask.Clone();

            HOperatorSet.ScaleImage(hoHeightImage, out hoHeightImage, hvZp, 0);

            //加速系数
            HTuple hvAccelerationFactor = 1.0f;
            HTuple hvScaleFactorW = 1.0f / hvAccelerationFactor;
            HTuple hvScaleFactorH = 1.0f / hvAccelerationFactor;
            HOperatorSet.ZoomImageFactor(hoHeightImage, out hoHeightImage, hvScaleFactorW, hvScaleFactorH, "nearest_neighbor");
            HOperatorSet.ZoomImageFactor(imageData.hoGrayImage.Clone(), out hoGrayImage, scaleX, scaleY, "constant");

            HOperatorSet.ZoomRegion(hoValidMask, out hoValidMask, hvScaleFactorW, hvScaleFactorH);
            HOperatorSet.Threshold(hoHeightImage, out hoValidMaskZoom, measureParam.MinDepth, measureParam.MaxDepth);
            HOperatorSet.Intersection(hoValidMask, hoValidMaskZoom, out hoValidMask);

            Box bbox = imageData.Boxes[bboxId];

            if (bbox.Seg.Valid == 0)
            {
                defectResult.Left = bbox.Left * scaleX;
                defectResult.Top = bbox.Top * scaleY;
                defectResult.Right = bbox.Right * scaleX;
                defectResult.Bottom = bbox.Bottom * scaleY;
                defectResult.ClassId = bbox.ClassId;
                defectResult.Confidence = bbox.Confidence;
                defectResult.InstanceId = bbox.InstanceId;
                defectResult.IsOk = false;

                return defectResult;
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
            HOperatorSet.Intersection(hoCracks, hoValidMask, out hoCracks);

            //region筛选
            HOperatorSet.Connection(hoCracks, out hoCracks);
            HOperatorSet.SelectShape(hoCracks, out hoCracks, "area", "and", 10, 9999999999999999999);
            HOperatorSet.CountObj(hoCracks, out HTuple hvNum);

            HTuple hvDepthFeature = new HTuple();
            HTuple hvDiameterFeature = new HTuple();
            HTuple hvLengthFeature = new HTuple();
            HTuple hvAreaFeature = new HTuple();
            List<Polygon> polygons = new List<Polygon>();

            for (int i = 0; i < hvNum; i++)
            {
                HOperatorSet.SelectObj(hoCracks, out hoCrack, i + 1);

                HOperatorSet.GenRectangle1(out HObject bboxRegon, bbox.Top, bbox.Left, bbox.Bottom, bbox.Right);
                HOperatorSet.Intersection(bboxRegon, hoCrack, out HObject bboxValidRegion);
                HOperatorSet.AreaCenter(bboxValidRegion, out HTuple bboxArea, out HTuple bboxRow, out HTuple bboxCol);
                HOperatorSet.AreaCenter(hoCrack, out HTuple crackArea, out HTuple crackRow, out HTuple crackCol);

                //计算缺陷区域在检测框内的比例
                double crackInBBoxRatio = (crackArea.D != 0) ? (bboxArea.D / crackArea.D) : 0.0;
                if (crackInBBoxRatio > 0.5)
                {
                    HOperatorSet.ZoomRegion(hoCrack, out hoScaled, scaleX, scaleY);
                    HOperatorSet.FillUp(hoScaled, out hoScaled);

                    //获取缺陷轮廓
                    HObject hoTmpScaled0;
                    HOperatorSet.ZoomRegion(hoScaled, out hoTmpScaled0, hvAccelerationFactor, hvAccelerationFactor);
                    Polygon polygon = new Polygon(hoTmpScaled0, 0, 0);
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
                    HOperatorSet.SelectObjectModel3d(hvConnCloud, "num_points", "and", 10, 9999999999999999999, out hvSeleCloud);
                    HOperatorSet.UnionObjectModel3d(hvSeleCloud, "points_surface", out hvUnionCloud);
                    HOperatorSet.GetObjectModel3dParams(hvUnionCloud, "num_points", out HTuple hvPointNum);
                    if (hvPointNum.I == 0)
                    {
                        continue;
                    }
                    HOperatorSet.SmoothObjectModel3d(hvUnionCloud, "mls", "mls_kNN", 99, out hvSmthCloud);
                    HOperatorSet.AffineTransObjectModel3d(hvSmthCloud, hvPoseMat, out hvAffdCloud);
                    //计算高度
                    HOperatorSet.GetObjectModel3dParams(hvAffdCloud, "point_coord_z", out hvValueZ);
                    HOperatorSet.TupleSort(hvValueZ, out hvSortedZ);
                    HOperatorSet.TupleInverse(hvSortedZ, out hvSortedZ);
                    HOperatorSet.TupleMean(hvSortedZ, out HTuple hvAvgZ);
                    HOperatorSet.TupleMin(hvSortedZ, out HTuple hvMinZ);
                    HOperatorSet.TupleMax(hvSortedZ, out HTuple hvMaxZ);
                    HOperatorSet.TupleLessElem(hvSortedZ, hvAvgZ, out HTuple hvMark0);
                    HOperatorSet.TupleFindFirst(hvMark0, 1, out HTuple hvB0);
                    HOperatorSet.TupleSelectRange(hvSortedZ, 0, hvB0, out HTuple hvTop0);
                    HOperatorSet.TupleMean(hvTop0, out HTuple hvDiv0);
                    HOperatorSet.TupleLessElem(hvTop0, hvDiv0, out HTuple hvMark1);
                    HOperatorSet.TupleFindFirst(hvMark1, 1, out HTuple hvB1);
                    HOperatorSet.TupleSelectRange(hvTop0, 0, hvB1, out HTuple hvTop2);
                    HOperatorSet.TupleMean(hvTop2, out HTuple hvDiv1);
                    HTuple hvTemp = hvMaxZ - hvMinZ;
                    hvDepthFeature = hvDepthFeature.TupleConcat(hvTemp);

                    //计算直径
                    HOperatorSet.RegionFeatures(hoScaled, "outer_radius", out HTuple hvRadius);
                    hvDiameterFeature = hvDiameterFeature.TupleConcat((2 * hvRadius) * hvXp);

                    //计算面积
                    HOperatorSet.RegionFeatures(hoScaled, "area", out HTuple hvArea);
                    hvAreaFeature = hvAreaFeature.TupleConcat(hvArea * hvXp * hvYp);
                }
            }

            defectResult.IsOk = true;

            if (hvDepthFeature.Length > 0)
            {
                // 结果输出, 单位um
                HTuple hvD = hvDepthFeature.TupleMax() * hvAccelerationFactor;
                defectResult.DepthFeature = hvD.D * 1000;
                if (defectResult.DepthFeature > measureParam.MinDefectDepth) { defectResult.IsOk = false; }
            }
            if (hvDiameterFeature.Length > 0)
            {
                // 结果输出, 单位um
                HTuple hvR = hvDiameterFeature.TupleMax() * hvAccelerationFactor;
                defectResult.DiameterFeature = hvR.D;
                if (defectResult.DiameterFeature > measureParam.MinDefectDiameter) { defectResult.IsOk = false;}
            }
            if (hvAreaFeature.Length > 0)
            {
                // 面积结果输出，单位平方mm
                HTuple hvA = hvAreaFeature.TupleSum() * hvAccelerationFactor * hvAccelerationFactor;
                defectResult.AreaFeature = hvA * 0.001 * 0.001;
            }

            defectResult.DefectPolygons = polygons;
            defectResult.Left = bbox.Left * scaleX;
            defectResult.Top = bbox.Top * scaleY;
            defectResult.Right = bbox.Right * scaleX;
            defectResult.Bottom = bbox.Bottom * scaleY;
            defectResult.ClassId = bbox.ClassId;
            defectResult.Confidence = bbox.Confidence;
            defectResult.InstanceId = bbox.InstanceId;

            hoHeightImage.Dispose();
            hoValidMask.Dispose(); hoValidMaskZoom.Dispose();
            hoCracks.Dispose(); hoCrack.Dispose();
            hoCrackZ.Dispose();
            hoScaled.Dispose();
            hoSkeleton.Dispose(); hoContours.Dispose(); hoLines.Dispose(); hoLine.Dispose();

            hvCrackCloud.Dispose(); hvConnCloud.Dispose(); hvSeleCloud.Dispose(); hvUnionCloud.Dispose();
            hvSmthCloud.Dispose(); hvAffdCloud.Dispose(); hvValueZ.Dispose(); hvSortedZ.Dispose();

            return defectResult;
        }

        /// <summary>
        /// 绘制结果
        /// </summary>
        /// <param name="measureResult"></param>
        /// <returns></returns>
        public int CvDrawResult(KBTGDJC_MeasureResult measureResult, double startY, double endY)
        {
            double scale = 0.3;
            Mat image = measureResult.GrayImage;

            Mat scaleImage = new Mat();
            Cv2.Resize(image, scaleImage, new OpenCvSharp.Size(0, 0), scale, scale, InterpolationFlags.Nearest);


            try
            {
                Cv2.CvtColor(scaleImage, scaleImage, ColorConversionCodes.GRAY2BGR);

                // 绘制缺陷
                int defectNum = measureResult.Defects.Count;

                for (int i = 0; i < defectNum; i++)
                {
                    DefectResult defect = measureResult.Defects[i];

                    if (defect.IsOk)
                        continue;

                    if (defect.Left != Single.NegativeInfinity && defect.Top != Single.NegativeInfinity &&
                       defect.Right != Single.NegativeInfinity && defect.Bottom != Single.NegativeInfinity &&
                       defect.InstanceId != -1 && defect.Confidence != Single.NegativeInfinity)
                    {
                        Cv2.Rectangle(scaleImage, new OpenCvSharp.Point((int)defect.Left * scale, (int)defect.Top * scale),
                                new OpenCvSharp.Point((int)defect.Right * scale, (int)defect.Bottom * scale), new Scalar(255, 0, 0), 1);

                        double defectPositon = startY + (0.5 * (defect.Top + defect.Bottom) / image.Height) * (endY - startY);

                        string[] lines =
                        [
                            $"ID:{defect.InstanceId}",
                            $"Name:{defect.Categories[defect.ClassId]}",
                            $"Depth:{Math.Round(defect.DepthFeature, 2)}um",
                            $"Diam:{Math.Round(defect.DiameterFeature, 2)}um",
                            $"Area:{Math.Round(defect.AreaFeature, 2)}mm^2",
                            $"Pos:{Math.Round(defectPositon, 2)}mm"
                        ];

                        int x = (int)defect.Right;
                        int y = (int)defect.Top;

                        foreach (var line in lines)
                        {
                            Cv2.PutText(scaleImage, line, new OpenCvSharp.Point(1, y * scale), HersheyFonts.HersheyDuplex, scale, new Scalar(255, 0, 0), 1);
                            y += 30;
                        }

                        measureResult.Defects[i].Position = defectPositon;
                    }

                }

                string[] OtherInfor =
                [
                    $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}",
                    $"PN:{_mParam.ProductNum}",
                    $"BN:{_mParam.BatchNum}"
                ];

                int oy = 40;
                foreach (var line in OtherInfor)
                {
                    Cv2.PutText(scaleImage, line, new OpenCvSharp.Point(1, oy * scale), HersheyFonts.HersheyDuplex, scale, new Scalar(0, 255, 0), 1);
                    oy += 30;
                }

                measureResult.GrayImage = scaleImage;
            }
            catch (Exception ex)
            {
                scaleImage = Mat.Zeros(new OpenCvSharp.Size(128, 128), MatType.CV_8UC3);
                Console.WriteLine(ex.Message);
            }

            return 0;

        }

        public int InitVariable()
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// 检测算法参数
    /// </summary>
    public class KBTGDJC_MeasureParam : BindableBase
    {
        //模型参数
        private string _modelPath = "./ScraperDefectsSDK/models/dfine_seg.kmodel";
        private int _batchSize = 1;
        private DeviceType _deviceType = DeviceType.DEVICE_GPU;
        private ModelType _modelType = ModelType.MODEL_DETECTION_SEG;
        private double _confidenceThreshold = 0.35;
        private double _ioUThreshold = 0.1;
        private double _segmentationThreshold = 0.5;

        //传感器参数
        private double _intervalX = 2.9;            //X方向的像素当量(μm)
        private double _intervalY = 8;              //Y方向的像素当量(μm)
        private double _intervalZ = 0.1;            //Y方向的像素当量(μm)
        private double _minDepth = -50000;          //深度图深度值下限
        private double _maxDepth = 50000;           //深度图深度值上限
        private bool _isScanEnd = false;            //扫描是否结束
        private bool _isSaveImage = false;          //是否存储图片
        private string _saveImagePath = "";         //存储图片路径

        private double _minDefectDepth = 10;        //最小缺陷深度(μm)
        private double _minDefectDiameter = 10;     //最小缺陷直径(μm)

        private string _productNum = "0";            //产品号
        private string _batchNum = "0";              //批次号

        /// <summary>
        /// 模型参数：刮刀缺陷检测模型文件路径
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 传感器参数：深度图深度值下限
        /// </summary>
        public double MinDepth
        {
            get { return _minDepth; }
            set { _minDepth = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 传感器参数：深度图深度值上限
        /// </summary>
        public double MaxDepth
        {
            get { return _maxDepth; }
            set { _maxDepth = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 传感器参数：扫描是否结束
        /// </summary>
        public bool IsScanEnd
        {
            get { return _isScanEnd; }
            set { _isScanEnd = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 是否存图
        /// </summary>
        public bool IsSaveImage
        {
            get { return _isSaveImage; }
            set { _isSaveImage = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 存图位置
        /// </summary>
        public string SaveImagePath
        {
            get { return _saveImagePath; }
            set { _saveImagePath = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 缺陷深度阈值(μm)
        /// </summary>
        public double MinDefectDepth
        {
            get { return _minDefectDepth; }
            set { _minDefectDepth = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 缺陷直径阈值(μm)
        /// </summary>
        public double MinDefectDiameter
        {
            get { return _minDefectDiameter; }
            set { _minDefectDiameter = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 产品号
        /// </summary>
        public string ProductNum
        {
            get { return _productNum; }
            set { _productNum = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 批次号
        /// </summary>
        public string BatchNum
        {
            get { return _batchNum; }
            set { _batchNum = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// 算法检测结果
    /// </summary>
    public class KBTGDJC_MeasureResult
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


        public KBTGDJC_MeasureResult()
        {
            GrayImage = new Mat();
            HeightImage = new Mat();

            Defects = new List<DefectResult>();

            MinDepth = Single.NegativeInfinity;
            MaxDepth = Single.NegativeInfinity;

            HoValidMask = new HObject();
            HoIrregularMask = new HObject();
        }

        ~KBTGDJC_MeasureResult()
        {
            GrayImage.Dispose();
            HeightImage.Dispose();

            HoValidMask.Dispose();
            HoIrregularMask.Dispose();
        }

    }

    /// <summary>
    /// 轮廓多边形
    /// </summary>
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

    /// <summary>
    /// 实例分割的mask
    /// </summary>
    public class Segment
    {
        public int Valid { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float[] AffineMatrix { get; set; } = new float[6];
        public float Thresh { get; set; }
        public float[] Data { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// 实例分割的bbox
    /// </summary>
    public class Box
    {
        public int DetNumCls { get; set; }
        public int SegNumCls { get; set; }
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

        public double DiameterFeature { get; set; }

        public double DepthFeature { get; set; }

        public double CenterRowFeature { get; set; }

        public double CenterColFeature { get; set; }

        public double Diameter { get; set; }

        public double Confidence { get; set; }

        public int ClassId { get; set; }

        public Dictionary<int, string> Categories { get; set; }

        public double Position { get; set; }


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
            DiameterFeature = Single.NegativeInfinity;
            DepthFeature = Single.NegativeInfinity;
            CenterRowFeature = Single.NegativeInfinity;
            CenterColFeature = Single.NegativeInfinity;
            Diameter = Single.NegativeInfinity;
            Confidence = Single.NegativeInfinity;
            ClassId = -1;

            DefectPolygons = new List<Polygon>();
            Categories = new Dictionary<int, string>();

            Position = 0;
        }
    }

    /// <summary>
    /// 标注的类别
    /// </summary>
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
