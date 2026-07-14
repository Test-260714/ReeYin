using SImagePro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SImagePro
{

    #region SR7Link
    //相机IP
    public struct SR7IF_ETHERNET_CONFIG
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] abyIpAddress;
    }
    #endregion

    #region 函数返回值
    public enum RTNCODE
    {
        RTN_CODE_OK = 0,                     //成功
        RTN_CODE_ERROR = -1,                 //一般性错误
        RTN_CODE_CAMERA_NOT_ONLINE = -999,   //相机未连接.
        RTN_CODE_NULL_PTR = -998,            //参数指针为null.
        RTN_CODE_ERROR_PARAMETER = -997,     //参数错误.
        RTN_CODE_ERROR_MEMORY = -996,        //内存（溢出/定义）错误.
        RTN_CODE_ERROR_TIMEOUT = -995,       //计算超时.
        RTN_CODE_ERROR_ROI = -994,           //roi错误
        RTN_CODE_ERROR_INIT = -993,      //内存申请错误         
    }
    #endregion

    #region SCVTYPE
    //旋转参数
    public enum ROTATE_TYPE
    {
        ONE_VERTICAL_ROTATE = 0x0000,       //顺时针转90°
        TWO_VERTICAL_ROTATE,                //顺时针旋转180°
        THREE_VERTICAL_ROTATE,              //顺时针旋转270°
        LR_MIRROR_ROTATE,                   //左右镜像翻转
        UD_MIRROR_ROTATE,                   //上下镜像翻转
        CENTER_ROTATE,                      //中心对称反转
    }

    //平面点结构
    public struct SPoint2D
    {
        public double x;
        public double y;
        public SPoint2D(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    };

    //像素点结构
    public struct SPoint2DPix
    {
        public int x;
        public int y;
        public SPoint2DPix(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    };

    //空间结构
    public struct SPoint3D
    {
        public double x;
        public double y;
        public double z;
        public SPoint3D(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    };

    //2D直线方程:a*x+b*y+c=0
    public struct SLine2D
    {
        public double a;
        public double b;
        public double c;
        public SLine2D(double a, double b, double c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    };

    //3D直线
    public struct SLine3D
    {
        public float a;
        public float b;
        public float c;
        public SPoint3D pt;
        public SLine3D(float a, float b, float c, SPoint3D pt)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.pt = pt;
        }
    };

    //线段
    public struct SLineSegment2D
    {
        public SPoint2D startPoint;
        public SPoint2D endPoint;
        public SLineSegment2D(SPoint2D startPoint, SPoint2D endPoint)
        {
            this.startPoint = startPoint;
            this.endPoint = endPoint;
        }
    };

    //平面方程:a*x+b*y+c*z+d=0
    public struct SPlane
    {
        public double a;
        public double b;
        public double c;
        public double d;
        public SPlane(double a, double b, double c, double d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    };

    //圆结构
    public struct SCircle2D
    {
        public SPoint2D center; //圆心
        public double radius;   //半径
    };

    //圆环结构
    public struct SRing2D
    {
        public SPoint2D center; //圆心
        public double maxR;   //外径
        public double minR;   //内径
    };

    //椭圆结构
    public struct SEllipse2D
    {
        SPoint2D center; //中心点
        public double width;    //宽度
        public double height;   //高度
        public double angle;    //角度
    };

    //圆弧结构
    public struct SArc2D
    {
        public SCircle2D Circle;  //圆形
        public double startAngle; //圆弧起始角度
        public double arcAngle;   //逆时针方向圆弧角度
    };

    //矩形
    public struct SRect
    {
        public SPoint2D center;  //中心坐标
        public double angle;     //角度
        public double width;     //矩形宽度
        public double height;    //矩形高度
    };

    //无角度矩形
    public struct SURect
    {
        public double leftX;  //左下角X坐标
        public double leftY;  //左下角Y坐标
        public double width;  //矩形宽度
        public double height; //矩形高度
    };

    //球体结构
    public struct SSphere
    {
        public SPoint3D center; //球心
        public double radius;   //半径
    };

    //空间矩形
    public struct SClipBox
    {
        public SPoint3D center; //中心点坐标
        public double lengthX;  //X方向宽度
        public double lengthY;  //Y方向宽度
        public double lengthZ;  //Z方向宽度
        public double angleX;   //绕X轴角度
        public double angleY;   //绕Y轴角度
        public double angleZ;   //绕Z轴角度
        public SClipBox(SPoint3D center, double lengthX, double lengthY, double lengthZ, double angleX, double angleY, double angleZ)
        {
            this.center = center;
            this.lengthX = lengthX;
            this.lengthY = lengthY;
            this.lengthZ = lengthZ;
            this.angleX = angleX;
            this.angleY = angleY;
            this.angleZ = angleZ;
        }
    };

    //3D变换参数
    public struct S3DAngle
    {
        public double x;
        public double y;
        public double z;
        public double angleX;
        public double angleY;
        public double angleZ;
        public double scale;
        public S3DAngle(double x, double y, double z, double angleX, double angleY, double angleZ, double scale)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.angleX = angleX;
            this.angleY = angleY;
            this.angleZ = angleZ;
            this.scale = scale;
        }
    };

    //矩形卡尺
    public struct SRectCaliper
    {
        public double calspacing;  //卡尺间距
        public double calheight;   //高度为矩形高度
        public double calwidth;    //卡尺宽度(高度为矩形高度)
        public SRect rect;        //矩形
    };

    //腰形卡尺
    public struct SWaistCaliper
    {
        public double calspacing;  //卡尺间距
        public double calheight;   //高度为矩形高度
        public double calwidth;    //卡尺宽度(高度为矩形高度)
        public SRect rect;        //矩形
    };

    //圆形卡尺
    public struct SCircleCaliper
    {
        public double calspacing;  //卡尺间距
        public double calheight;   //高度为矩形高度
        public double calwidth;    //卡尺宽度
        public double startAngle;  //圆弧开始角度 单位：度
        public double endAngle;    //圆弧终止角度 单位：度
        public SCircle2D Circle;    //圆形
    };

    //拟合表面缺陷检测参数
    public struct SPlaneFlawsParm
    {
        public double humpHeightCheck;    //凸起高度阈值
        public double humpAreaCheck;      //凸起面积阈值
        public double sunkenDepthCheck;   //凹陷深度阈值
        public double sunkenAreaCheck;    //凹陷面积阈值
        public double blockBorderPhy;     //计算点块边长
    };

    //拟合表面缺陷检测参数
    struct SSurfaceFlawsParm
    {
        public double humpHeightCheck;    //凸起高度阈值
        public double humpAreaCheck;      //凸起面积阈值
        public double sunkenDepthCheck;   //凹陷深度阈值
        public double sunkenAreaCheck;    //凹陷面积阈值
        public double blockBorderPhy;     //计算点块边长
        public double fitBorderPhyX;      //拟合区域块X长度
        public double fitBorderPhyY;      //拟合区域块Y长度
        public double fitExcluding;       //异常值剔除比率
        public int blockSample;           //计算点降采样倍率
        public int fitSample;             //拟合点降采样倍率
        public int fitSurfaceN;           //曲面拟合次数
        public int fitMethod;             //拟合模式 0：曲面， 1：平面
    };

    //规则面（拟合线）缺陷测参数
    struct SRuleFlawsParm
    {
        public double humpHeightCheck;    //凸起高度阈值
        public double humpAreaCheck;      //凸起面积阈值
        public double sunkenDepthCheck;   //凹陷深度阈值
        public double sunkenAreaCheck;    //凹陷面积阈值
        public int fitMethod;             //0: 曲线 1：直线
        public int fitCurveMaxIndex;      //拟合曲线最大指数
        public double outlieDelRate;      //异常值剔除比率
        public double blockLength;        //分段长
        public double deleteRate;         //线拟合剔除比率
        public double minValidRate;       //有效的点最少占比
        public double Samplewidth;        //采样宽度
    };

    //ROI类型
    public enum ROI_TYPE
    {
        RECT,              //矩形
        CIRCLE,            //圆形
        RING,              //圆环
        POLYGON,           //多边形
        ELLIPSE,           //椭圆
        ELLIPSP_RING,      //椭圆环
        CALLIPERS_RECT,    //矩形卡尺
        CALLIPERS_CIRCULAR,//圆形卡尺
    }

    //ROI
    public struct SRoi
    {
        public ROI_TYPE RoiType;      //roi类型
        public SRect RectRoi;         //常规矩形
        public SCircle2D CircleRoi;   //圆
        public SRing2D RingRoi;    //圆环
        public SPoint2D[] PointsRoi;   //任意多边形
        public uint PointsSize;
    };

    //平面度结果
    public struct FlatnessParam
    {
        public SPlane plane;       //平面方程
        public double minDis;      //点到平面最小距离
        public double maxDis;      //点到平面最大距离
        public SPoint3D maxPt;     //到平面最小距离点
        public SPoint3D minPt;     //到平面最小距离点
    };

    //边界
    public struct SBorder
    {
        double minX;
        double maxX;
        double minY;
        double maxY;
    };

    //点云头结构(zInterval初始化为 1e-5)
    public struct SPointCloudHead
    {
        public uint height; //点云行数
        public uint width;  //点云列数
        public double xInterval;    //点云列间距
        public double yInterval;    //点云行间距
        public double zInterval;    //点云z方向分辨率系数

        public SPointCloudHead(uint height, uint width, double xInterval, double yInterval, double zInterval)
        {
            this.height = height;
            this.width = width;
            this.xInterval = xInterval;
            this.yInterval = yInterval;
            this.zInterval = zInterval;
        }
    };

    //TIF文件信息
    struct STifField
    {
        public uint height;    //行数
        public uint width;     //列数
        public uint bitDepth;  //位宽
        public uint channels;  //通道数
        public int type;               //数据类型 1是signed 2是unsigned
    };

    //3D显示点结构
    struct SPoint3DRGB
    {
        public SPoint3D point3D;
        public byte r;
        public byte g;
        public byte b;
    };

    //缺陷检测结果
    public struct FlawsData
    {
        public SPoint2D[] flawsContours;                //缺陷轮廓
        public int contoursNum;                        //缺陷轮廓点数
        public double flawsAreas;                      //缺陷面积
        public double flawsHeight;                     //缺陷最深
        public SRect flawsRects;                       //缺陷矩形框
    };

    public struct CalData
    {
        public SPoint3D[] fitPoints;
        public int pointsNum;
    };

    //空间显示点结构
    public struct SDisplay3DPoint
    {
        public double x;
        public double y;
        public double z;
        public byte r;
        public byte g;
        public byte b;
        public int AdaptiveRGB;

        public SDisplay3DPoint(double x, double y, double z, byte r, byte g, byte b, int AdaptiveRGB)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.r = r;
            this.g = g;
            this.b = b;
            this.AdaptiveRGB = AdaptiveRGB;
        }
    };
    #endregion

    internal class SCV
    {
        #region 常量
        public const double INVALID_VALUE_MIN = -1000000000;
        public const double INVALID_VALUE_MAX = 1000000000;
        #endregion

        #region SR7Link
        ///<summary>
        /// 通信连接------与相机连接
        /// </summary>
        /// <param name="lDeviceId"></param>          设备ID号，范围为0-3
        /// <param name="pEthernetConfig"></param>   （网口）通信设定
        /// <returns></returns>                       0：成功; 小于0：失败
        /// <remarks></remarks>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_EthernetOpen(int lDeviceId, ref SR7IF_ETHERNET_CONFIG pEthernetConfig);
        #endregion

        #region 3D计算相关算法
        /*************************************************
            Function:       PointPlaneDistance(区分正负）
            Description:    点到平面距离
            Input:          @point		3D点
                            @plane		平面方程
            Output:         @distance	输出距离
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PointPlaneDistance(SPoint3D point, SPlane plane, ref double distance);

        /*************************************************
		    Function:       VolumeMeasurement
		    Description:    体积测量
		    Input:          @HeightData		高度图
						    @pcHead			高度头文件
						    @ROI			ROI
						    @plane			平面方程
						    @upper、lower	高度上下限制
		    Output:         @volume			输出体积MM^3
		    Return:         <0: 失败.
						    =0: 成功
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int VolumeMeasurement(int[] HeightData, SPointCloudHead pcHead, SRoi ROI, SPlane plane, double upper, double lower, ref double volume);

        /*************************************************
		    Function:       ThreePointHeightDiff
		    Description:    三点高度差
		    Input:          @sPt:开始点   ePt:结束点   cPt:中间点
		    Output:         @distance		输出三点高度差
		    Return:         <0: 失败.
						    =0: 成功
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ThreePointHeightDiff(SPoint3D sPt, SPoint3D ePt, SPoint3D cPt, ref double distance);

        /*************************************************
		    Function:       FitPlane
		    Description:    平面拟合
		    Input:          @points		用于拟合的点
						    @ptSize		拟合点数量
		    Output:         @plane		输出平面方程
		    Return:         <0: 失败.
						    =0: 成功
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int FitPlane(SPoint3D[] points, int ptSize, ref SPlane plane);

        /*************************************************
            Function:       PlanePlaneAngle
            Description:    平面角度
            Input:          @plane1		输入平面1
                            @plane2		输入平面2
            Output:         @angle		角度
            Return:         <0: 失败.
                                =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PlanePlaneAngle(SPlane plane1, SPlane plane2, ref double angle);

        /*************************************************
		    Function:       PlanePlaneLin
		    Description:    平面交线
		    Input:          @plane1			输入平面1
						    @plane2			输入平面2
		    Output:         @dstLine		交线
		    Return:         <0: 失败.
						    =0: 成功.
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PlanePlaneLine(SPlane plane1, SPlane plane2, ref SLine3D dstLine);

        /*************************************************
		    Function:       Line3DIntersection
		    Description:    空间直线交点
		    Input:          @line1			空间直线1
						    @line2			空间直线2
		    Output:         @dstPoint		交点
		    Return:         <0: 失败.
						    =0: 成功.
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Line3DIntersection(SLine3D line1, SLine3D line2, ref SPoint3D dstPoint);

        /*************************************************
		    Function:       Point3dDistance
		    Description:    3D点距离
		    Input:          @point1			空间点1
						    @point2			空间点2
		    Output:         @distance		交点
		    Return:         <0: 失败.
						    =0: 成功.
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Point3dDistance(SPoint3D point1, SPoint3D point2, ref double distance);

        /*************************************************
		    Function:       FitLine3D
		    Description:    3D点距离
		    Input:          @point1			空间点1
						    @point2			空间点2
		    Output:         @distance		距离
		    Return:         <0: 失败.
						    =0: 成功.
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int FitLine3D(SPoint3D[] points, int pointnum, ref SLine3D line);

        /*************************************************
		    Function:       PointToLine3dDistance
		    Description:    3D点到直线距离
		    Input:          @poin			空间点
						    @line			空间直线
		    Output:         @distance		距离
		    Return:         <0: 失败.
						    =0: 成功.
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PointToLine3dDistance(SPoint3D point, SLine3D line, ref double distance);

        /*************************************************
		    Function:       PlaneIntersection
		    Description:    三平面交点
		    Input:          @planes			平面，lanes[0]为标定块顶面或底面，planes[1]和planes[2]均为侧面
		    Output:         @dstPt			交点
		    Return:         <0: 失败.
						    =0: 成功.
		    Others:         无
	    *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PlaneIntersection(SPlane[] planes, ref SPoint3D dstPt);

        /*************************************************
            Function:       MedianFiltering
            Description:    中值滤波
            Input:          @srcHeightImage		原始点云
                            @pcHead				点云头
                            @hThreshold			高度阈值
                            @wThreshold			窗口大小
                            @ACC				加速率
            Output:			@dstHeightImage		滤波后点云
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int MedianFiltering(int[] srcHeightImage, int[] dstHeightImage, ref SPointCloudHead pcHead, double hThreshold, uint Wthreshold, uint ACC);

        /*************************************************
            Function:       PointCloudFiltering
            Description:    图像滤波（点云滤波）,支持ROI
            Input:          @srcHeightImage	原始点云数据
                            @pcHead			点云头文件
                            @rois			roi(如果ROI为null或者数量为0，那么就对全图进行滤波)
                            @RoiSize		Roi数量
                            @distanceRate	点与点之间的有效距离比，值越大认为两点之间的联系关系越强（范围0~1）， 默认值为0.5
                            @minValidArea	最小有效关联点，如果区域内关联点的个数小于此值，则这个区域会被去掉，默认值为200
            Output:         @dstHeightImage	滤波后云数据
            Return:         <0 失败.
                            =  成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PointCloudFiltering(
                int[] srcHeightImage,
                int[] dstHeightImaget,
                SPointCloudHead pcHead,
                SRoi[] rois,
                int RoiSize,
                float distanceRate,
                int minValidArea);

        /*************************************************
            Function:       HeightFiltering
            Description:    图像滤波（高度滤波），支持ROI
            Input:          @srcHeightImage	原始点云数据
                            @pcHead			点云头文件
                            @rois			roi(如果ROI为null或者数量为0，那么就对全图进行滤波)
                            @RoiSize			Roi数量
                            @maxValidDis	高度阈值，单位：mm
                            @minValidArea	面积阈值，单位：像素
            Output:         @dstHeightImage	滤波后云数据
            Return:         <0 失败.
                            =  成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int HeightFiltering(
                int[] srcHeightImage,
                int[] dstHeightImaget,
                SPointCloudHead pcHead,
                SRoi[] rois,
                int RoiSize,
                double maxValidDis,
                int minValidArea);

        /*************************************************
            Function:       ImageComplements
            Description:    图像补点
            Input:          @heightImage		补点图像图像
                            @pcHead				点云头
                            @mode				模式： 0:只补X ，1:只补Y，2:两个方向都补
                            @complementsNum		补点数量
                            @angle				补点角度，超过该角度以后补水平直线
            Output:			@heightImage		补点图像图像
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ImageComplements(int[] heightImage, SPointCloudHead pcHead, int mode, int complementsNum, double angle);

        /*************************************************
            Function:       CalFlatnessOfRegion
            Description:    按区域拟合，计算平面度
            Input:          @heightImage			原始高度值
                            @RoiMaskImage			ROI区域Mask图
                            @pcHead					点云头
                            @xRoiPixs，yRoiPixs		拟合区域单位分块大小
                            @Plane3D				基准平面（没有就填NULL）
            Output:			@FlatnessParam			返回平面方程，最大最小点的距离
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int CalFlatnessOfRegion(
            int[] heightImage,
            byte[] RoiMaskImage,
            SPointCloudHead pcHead,
            int xRoiPixs,
            int yRoiPixs,
            SPlane[] basePlane,
            ref FlatnessParam FlatnessPlne);
        #endregion

        #region 标定校正相关算法
        /*************************************************
            Function:       GetShakeCalibData
            Description:    获取图像校正数据（校正机构抖动）
            Input:          @heightImage	输入原始图像数据
                            @width			图像宽度
                            @height			图像高度
                            @fStepX			X间距
                            @fStepY			Y间距
                            @plane			基准平面
                            @rectROI		基准校正区域
            Output:         @outCalibData	校正数据申请内存 大小为 (nHeight* 10) 个字节,外部申请内存
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetShakeCalibData(
            int[] heightImage,
            int width,
            int height,
            float fStepX,
            float fStepY,
            SPlane plane,
            SRect rectROI,
            IntPtr outCalibData);

        /*************************************************
            Function:       GenShakeCalibImage
            Description:    校正图像（校正机构抖动）
            Input:          @heightImage	输入原始图像数据
                            @width			图像宽度
                            @height		    图像高度
                            @Data			输入校正数据
            Output:         @outHeightImage	校正后图像数据，pDstData存储大小和pSrcData一致
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GenShakeCalibImage(int[] heightImage, int width, int height, IntPtr Data, int[] outHeightImage);

        /*************************************************
            Function:       GetRouletteCalibData
            Description:    获取圆盘标定数
            Input:          @imagePointXYs[18],         棋盘格的9个点在原始灰度图像像素里的坐标, 格式为{x1,y1,x2,y2,x3,...,x9,y9}
                            @pixelSizeBeforeTransform,  X方向像素大小
                            @pixelSizeAfterTransform,   转换后像素大小
                            @leftRealX,                 左边点X真实坐标
                            @rightRealX,                右边点X真实坐标
                            @upRealY,                   上面点X真实坐标
                            @downRealY,                 下面点Y真实坐标
            Output:         @outCalibData,              输出的标定数据结果
                            @outMessage[256]            输出信息提示
            Return:         <0 失败.
                            =  成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetRouletteCalibData(
            SPoint2D[] imagePointXY,
            double pixelSizeBeforeTransform,
            double pixelSizeAfterTransform,
            double leftRealX,
            double rightRealX,
            double upRealY,
            double downRealY,
            IntPtr outCalibData,
            ref double outMessage);

        /*************************************************
            Function:       GenRouletteCalibImage
            Description:    圆盘标定校正图像
            Input:          @heightImage,       原始深度图
                            @pointsWidth,       点云宽度
                            @int pointsHeight,  点云高度
                            @calibData,         标定数据
                            @int imageWidth,    转换后图像宽度
                            @int imageHeight,   转换后图像高度
                            @centerX,           裁剪中心X
                            @centerY,           裁剪中心Y
                            @ centerZ,          裁剪中心Z
                            @ ratioMMPerPixel,  转换后像素大小
            Output:         @dst,               转换后3D数据
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GenRouletteCalibImage(
            int[] heightImage,
            int pointsWidth,
            int pointsHeight,
            IntPtr calibData,
            int[] dstHeightImage,
            int imageWidth,
            int imageHeight,
            double centerX,
            double centerY,
            double centerZ,
            double ratioMMPerPixel);

        /*************************************************
            Function:       AffineTransMatrix3D
            Description:    根据输入点求矩阵
            Input:          @ptSeneor		原始点
                            @ptPhysical		目标点
                            @ptSize			点数
            Output:         @dstMatrix		输出的变换矩阵
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AffineTransMatrix3D(SPoint3D[] ptSeneor, SPoint3D[] ptPhysical, int nSize, double[] dstMatrix);

        /*************************************************
            Function:       AffineTransPoint3D
            Description:    根据矩阵求变换后的坐标
            Input:          @srcPt		转换前3D点
                            @matrix		仿射变换4*4矩阵
                            @dstPt		转换后3D点
            Output:         @dstPt      转换后的3D点
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AffineTransPoint3D(ref SPoint3D srcPt, double[] matrix, ref SPoint3D dstPt);

        /*************************************************
            Function:       AffineTransImage
            Description:    点云变换
            Input:          @heightImage				变化前高度图
                            @grayImage					变化前亮度图（没有时可以为NULL）
                            @pcHead						点云头
                            @matrix						仿射变换4*4矩阵
                            @newPcHead					变化点云头
                            @centerX 、centerY、centerZ 转换后图像偏移
                            @hMax、hMin					高度上下限
            Output:         @newHeightImage				变化后高度图
                            @newGrayImage				变化后灰度图
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AffineTransImage(
            int[] heightImage,
            byte[] grayImage,
            SPointCloudHead pcHead,
            double[] matrix,
            SPointCloudHead newPcHead,
            double centerX,
            double centerY,
            double centerZ,
            int[] newHeightImage,
            byte[] newGrayImage,
            double hMax = INVALID_VALUE_MAX,
            double hMin = INVALID_VALUE_MIN);

        /*************************************************
            Function:      CorrectZAxisRotation
            Description:    相机绕Z方向旋转校正
            Input:          @heightImage				校正前高度图
                            @grayImage					校正前亮度图（没有时可以为NULL）
                            @pcHead						点云头
                            @angle						旋转角度
                            @centerX 、centerY、centerZ 转换后图像偏移
                            @newPcHead					校正后头文件
            Output:         @newHeightImage				校正后高度图
                            @newGrayImage				校正后灰度图
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int CorrectZAxisRotation(
            int[] heightImage,
            byte[] grayImage,
            ref SPointCloudHead pcHead,
            double angle,
            SPointCloudHead newPcHead,
            double centerX,
            double centerY,
            double centerZ,
            int[] newHeightImage,
            byte[] newGrayImage);

        /*************************************************
            Function:       PoseToMatrix
            Description:    根据角度求矩阵
            Input:          @angleX			绕X轴旋转角度
                            @angleY			绕Y轴旋转角度
                            @angleZ			绕Z轴旋转角度
                            @thetaXofPd		矫正角度
                            @ceberX			X方向偏移值
                            @ceberY			Y方向偏移值
                            @ceberZ			Z方向偏移值
            Output:         @matrix			计算得到的角度矩阵
            Return:         <0: 失败.
                            =0: 成功
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PoseToMatrix(double AngleX, double AngleY, double AngleZ, double thetaXofPd, double CeberX, double CeberY, double CeberZ, double[] matrix);

        /*************************************************
            Function:       GetMatrixFromPlane
            Description:    通过平面方程计算旋转矩阵
            Input:          @plane			平面方程
            Output:         @matrix			计算得到的角度矩阵
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetMatrixFromPlane(ref SPlane plane, double[] matrix);

        /*************************************************
            Function:       CorrectXAxisRotation
            Description:    矫正绕相机绕X旋转的角度
            Input:          @heightData		原始图像
                            @pcHead			点云头
                            @anglex			X的角度
                            @CeberY			Y方向偏移行数
                            @isHeigh		是否偏移高度值
            Output:         @dstheightData	偏移后原图像
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int CorrectXAxisRotation(int[] heightData, SPointCloudHead pcHead, double anglex, int CeberY, bool isHeigh, int[] dstheightData);

        /*************************************************
            Function:       ImageRotate
            Description:    图像旋转
            Input:          @heightData		原始图像
                            @pcHead			点云头
                            @rotateType		旋转方式
            Output:         @dstheightData	偏移后原图像
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ImageRotate(int[] heightData, int[] dstheightData, ref SPointCloudHead pcHead, ROTATE_TYPE rotateType);
        #endregion

        #region 工具类
        /*************************************************
            Function:       ArrayFilter
            Description:    数组数据筛选
            Input:          @data		输入数组
                            @dataSize	数组大小
                            @filterType	极值(0:正常，1:极大值，2:极小值)
                            @valueType	结果计算方式(0:平均值，1:中位数)
                            @delPara	阈值m，大于等于1按个数，小于1按比例
                            @samPara	阈值n，大于等于1按个数，小于1按比例
                            @upper		数值上限
                            @lower		数值下限
            Output:         @outValue	剔除后数值
                            @outData	剔除后数组
                            @outDataSize剔除后数组大小
            Return:         <0: 失败.
                            =0: 成功
            Others:         正常：去除最大m个点，去除最小n个点；
                            极大值：去除最大m个点；
                            极小值：去除最小m个点；
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ArrayFilter(
            double[] data,
            int dataSize,
            int filterType,
            int valueType,
            double delPara,
            double samPara,
            double upper,
            double lower,
            ref double outValue,
            double[] outData,
            ref int outDataSize);

        /*************************************************
            Function:       RoiArea
            Description:    求ROI面积大小，用于提取ROI内点时候申请内存
            Input:          @roi	输入ROI
            Output:
            Return:         <0: 失败.
                            >0: ROI像素面积.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RoiArea(SRoi roiInfo);

        /*************************************************
            Function:       Point3DFilter
            Description:    3D点数组数据筛选
            Input:          @points		输入数组
                            @ptSize		数组大小
                            @filterType	极值(0:正常，1:极大值，2:极小值)
                            @valueType	结果计算方式(0:平均值，1:中位数)
                            @priorityType	优先级（0:X方向，1:Y方向，2:Z方向）
                            @delPara	阈值m，大于等于1按个数，小于1按比例
                            @samPara	阈值n，大于等于1按个数，小于1按比例
                            @upper		数值上限
                            @lower		数值下限
            Output:         @outValue	剔除后数值
                            @outPoints	剔除后数组
                            @outPtSize	剔除后数组大小
            Return:         <0: 失败.
                            =0: 成功
            Others:         正常：去除最大m个点，去除最小n个点；
                            极大值：去除最大m个点；
                            极小值：去除最小m个点；
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Point3DFilter(
            SPoint3D[] points,
            int ptSize,
            int filterType,
            int valueType,
            int priorityType,
            double delPara,
            double samPara,
            ref SPoint3D outValue,
            SPoint3D[] outPoints,
            ref int outPtSize,
            double upper = INVALID_VALUE_MAX,
            double lower = INVALID_VALUE_MIN);

        /*************************************************
            Function:       GetAllPointsFromRoi
            Description:    ROI内点提取
            Input:          @data		高度数据
                            @pcHead		点云头结构
                            @roiInfo	ROI
                            @accelerate	降采样比率，默认填1
            Output:         @roiData	输出ROI内3D点（SPoint3D[]）
                            @dstanum	3D点数组大小
            Return:         <0: 失败.
                            =0: 成功
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetAllPointsFromRoi(int[] heightImage, SPointCloudHead pcHead, SRoi roiInfo, IntPtr roiData, ref int dstanum, int accelerate = 1);

        /*************************************************
            Function:       GetFilteredPointFromROI
            Description:    ROi获取单点高度
            Input:          @heightImage高度数据
                            @pcHead		点云头结构
                            @roiInfo	ROI
                            @accelerate	降采样比率，默认填1
                            @filterType	极值(0:正常，1:极大值，2:极小值)
                            @valueType	结果计算方式(0:平均值，1:中位数)
                            @delPara	阈值m，大于等于1按个数，小于1按比例
                            @samPara	阈值n，大于等于1按个数，小于1按比例
                            @upper		数值上限
                            @lower		数值下限
            Output:         @outValue	剔除后数值
                            @outPoints	剔除后数组
                            @outPtSize	剔除后数组大小
            Return:         <0: 失败.
                            =0: 成功
            Others:         正常：去除最大m个点，去除最小n个点；
                            极大值：去除最大m个点；
                            极小值：去除最小m个点；
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetFilteredPointFromROI(
            int[] heightData,
            SPointCloudHead pcHead,
            SRoi roiInfo,
            int filterType,
            int valueType,
            double delPara,
            double samPara,
            ref SPoint3D outValue,
            IntPtr outPoints,
            ref int outPtSize,
            int accelerate = 1,
            double upper = INVALID_VALUE_MAX,
            double lower = INVALID_VALUE_MIN);

        /*************************************************
            Function:       GetFilteredPointFromROIOnBasePlane
            Description:    ROi获取单点高度（基于平面）
            Input:          @heightImage高度数据
                            @pcHead		点云头结构
                            @roiInfo	ROI
                            @accelerate	降采样比率，默认填1
                            @filterType	极值(0:正常，1:极大值，2:极小值)
                            @valueType	结果计算方式(0:平均值，1:中位数)
                            @delPara	阈值m，大于等于1按个数，小于1按比例
                            @samPara	阈值n，大于等于1按个数，小于1按比例
                            @upper		数值上限
                            @lower		数值下限
                            @plane		平面方程
            Output:         @outValue	剔除后数值
                            @outPoints	剔除后数组
                            @outPtSize	剔除后数组大小
            Return:         <0: 失败.
                            =0: 成功
            Others:         正常：去除最大m个点，去除最小n个点；
                            极大值：去除最大m个点；
                            极小值：去除最小m个点；
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetFilteredPointFromROIOnBasePlane(
            int[] heightImage,
            SPointCloudHead pcHead,
            SRoi roiInfo,
            int filterType,
            int valueType,
            double delPara,
            double samPara,
            SPlane plane,
            ref double outValue,
            IntPtr outPoints,
            ref int outPtSize,
            int accelerate = 1,
            double upper = INVALID_VALUE_MAX,
            double lower = INVALID_VALUE_MIN);

        /*************************************************
            Function:       GetMaskFromRoi
            Description:    获取ROI区域Mask图
            Input:          @Roi 输入ROI
                            @pcHead  点云头
            Output:         @mask 输出Mask图，大小与原始点云一样；
            Return:         <0  失败.
                            =0  成功
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetMaskFromRoi(SRoi Roi, SPointCloudHead pcHead, byte[] mask);

        /*************************************************
            Function:       GetMaskFromPoints
            Description:    根据点坐标生成Mask图
            Input:          @pt 输入点坐标
                            @ptSize 点个数
                            @width  图像宽度
                            @height 图像高度
            Output:         @mask 输出Mask图；
            Return:         <0  失败.
                            =0  成功
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetMaskFromPoints(SPoint2D[] inPt, int ptSize, int width, int height, byte[] mask);

        /*************************************************
            Function:       GenHeightImageFromMask
            Description:    获取加掩膜以后的高度图图
            Input:          @mask Mask图
                            @pcHead  点云头
                            @data 高度数据
            Output:         @data 高度数据
            Return:         <0  失败.
            =0  成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GenHeightImageFromMask(SPointCloudHead pcHead, byte[] mask, int[] data);
		
		/*************************************************
			Function:       GenErasureImage
			Description:    获取擦除后的图像
			Input:          @pcHead			点云头
							@erasureRoi		擦除区域ROI
							@roiSize		ROI数量
			Output:         @heightImage	高度图像，如果没有可以填null
							@grayImage		灰度图像，如果没有可以填null
			Return:         <0  失败.
			=0  成功
			Others:         无
		*************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GenErasureImage(SPointCloudHead pcHead, SRoi[] erasureRoi, int roiSize,int[] heightImage, byte[] grayImage);
        #endregion

        #region 图像转换类
        /*************************************************
            Function:       CalUpperAndLower
            Description:    求图像上下限
            Input:          @points			输入图像
                            @width height	图像长宽
            Output:         @upper lower	高度上下限，原始int高度，单位1/100000 mm
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int CalUpperAndLower(int[] points, uint hight, uint width, ref double upper, ref double lower);

        /*************************************************
           Function:       PointToGrayData
           Description:    高度转灰度
           Input:          @points			输入图像
                           @width height	图像长宽
                           @upper lower	高度上下限，原始int高度，单位1/100000 mm
           Output:         @grayData		灰度图
           Return:         <0: 失败.
                           =0: 成功
           Others:         无
       *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PointToGrayData(int[] points, uint width, uint height, double upper, double lower, byte[] grayData);

        /*************************************************
            Function:		PointToRGBData
            Description:    高度转RGB显示
            Input:          @points			输入图像
                            @width height	模式：0 只通过高度计算RGB，1、2 通过高度分布计算RGB
                            @upper lower	高度上下限，原始int高度，单位1/100000 mm
            Output:         @R、G、B		RBG颜色
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PointToRGBData(int[] points, uint width, uint height, int mode, double upper, double lower, byte[] R, byte[] G, byte[] B);
		
		/*************************************************
			Function:		RGBBlendGray
			Description:    RGB图混合灰度图
			Input:          @grayData		输入灰度图
							@width height	图像长宽
							@R、G、B		RBG图
			Output:         @R、G、B		RBG图
			Return:         <0: 失败.
							=0: 成功
			Others:         无
		*************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RGBBlendGray(byte[] grayData, uint width, uint height, double blendScale, byte[] R, byte[] G, byte[] B);

        /*************************************************
            Function:		GetPointCloudHead
            Description:    读取ECD头文件
            Input:          @file			输入文件名
            Output:         @pcHead			输出头文件
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetPointCloudHead(char[] file, ref SPointCloudHead pcHead);

        /*************************************************
            Function:		ReadEcd
            Description:    读取ECD文件
            Input:          @file			输入文件名
            Output:         @pcHead			输出头文件
                            @heightImage			高度数据
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ReadEcd(char[] file, int[] heightImage, ref SPointCloudHead pcHead);

        /*************************************************
            Function:		WriteEcd
            Description:    写入ECD头文件
            Input:          @file			输入文件名
            Output:         @pcHead			输出头文件
                            @heightImage			高度数据
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int WriteEcd(char[] file, int[] heightImage, SPointCloudHead pcHead);

        /*************************************************
            Function:		WritePly
            Description:    保存PLY
            Input:          @file			输入文件名
                            @points			点云数据
                            @pointSize		3D点数量
            Output:
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int WritePly(char[] file, SPoint3D[] points, int pointSize);

        /*************************************************
            Function:		SavePly
            Description:    保存PLY
            Input:          @file			输入文件名
                            @pcHead			输出头文件
                            @heightImage	高度数据
            Output:
            Return:         <0: 失败.
                            =0: 成功
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SavePly(char[] file, int[] heightImage, SPointCloudHead pcHead);

        /*************************************************
            Function:       SavePcd
            Description:    保存Ply格式
            Input:          @file 文件名
                            @heightImage 高度数据
                            @PointCloudHead 头文件
            Output:
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SavePcd(char[] file, int[] heightImage, SPointCloudHead pcHead);

        /*************************************************
            Function:       Save32Tif
            Description:    保存32位Tiff格式
            Input:          @file           文件名
                            @heightImage    高度数据
                            @PointCloudHead 头文件
            Output:
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Save32Tif(char[] file, int[] heightImage, SPointCloudHead pcHead);

        /*************************************************
            Function:       Save16TifOfScale
            Description:    32位高度数据按比例保存16位Tiff格式
            Input:          @file           文件名
                            @heightImage    高度数据
                            @PointCloudHead 头文件
                            @z_scale        缩放比例
            Output:
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Save16TifOfScale(char[] file, int[] heightImage, SPointCloudHead pcHead, double z_scale);

        /*************************************************
            Function:       Save16TifOfShort
            Description:    16位高度数据保存16位Tiff格式
            Input:          @file           文件名
                            @heightImage    16位高度数据
                            @PointCloudHead 头文件
                            @z_scale        缩放比例
                            @isSigned       是否有符号
            Output:
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Save16TifOfShort(char[] file, short[] heightImage, SPointCloudHead pcHead, double z_scale, int isSigned);

        /*************************************************
            Function:       Save16Tif
            Description:    保存16位Tiff格式
            Input:          @file           文件名
                            @heightImage    高度数据
                            @PointCloudHead 头文件
                            @HeightMax、HeightMin 高度上下限区间（参考对应相机景深范围）
            Output:			@z_sight 返回缩放比例
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Save16Tif(char[] file, int[] heightImage, SPointCloudHead pcHead, double HeightMax, double HeightMin, ref double z_sight);

        /*************************************************
            Function:       SaveBmp
            Description:    保存Bmp图片
            Input:          @file           文件名
                            @imageData      图像数据
                            @width、height  图像长宽
            Output:
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SaveBmp(char[] file, byte[] imageData, int width, int height);

        /*************************************************
            Function:       GetTifField
            Description:    获取tiff图片字段信息
            Input:          @file       文件名
            Output:         @tiffield   字段信息
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetTifField(char[] file, ref STifField tiffield);

        /*************************************************
            Function:       Read32Tif
            Description:    读取32位tiff图片
            Input:          @file 文件名
            Output:         @data 高度信息
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Read32Tif(char[] file, int[] heightImage);

        /*************************************************
            Function:       Read32Tif
            Description:    读取32位tiff图片
            Input:          @file            文件名
                            @zInterval       缩放比率
            Output:         @dataheightImage 高度信息
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
        *************************************************/
        [DllImport("SImagePro.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Read16Tif(char[] file, double zInterval, int[] heightImage);
        #endregion

        #region 3D显示
        /*************************************************
            Function:       Show3DPoint
            Description:    显示3D点
            Input:          @Points			需要显示的点集 SDisplay3DPoint[]
                            @PointsSize		每块点云数量
                            @PointsNum			点云块数量
                            @streamline		精简点数
                            @width			窗口宽度
                            @height			窗口高度
                            @left			窗口左边距
                            @top			窗口右边距
            Output:         
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
            Author:         FYX
            Date:           2024-05-31
        *************************************************/
        [DllImport("SShow3DImage.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Show3DPoint(
            SDisplay3DPoint[] Points,
            int[] PointsSize,
            int PointsNum,
            int streamline = 1,
            int width = 960,
            int height = 720,
            int left = 50,
            int top = 50);

        /*************************************************
            Function:       Show3DPoint
            Description:    显示3D点
            Input:          @Points			需要显示的点集
                            @PointsSize		每块点云数量
                            @Num			点云块数量
                            @streamline		精简点数
                            @width			窗口宽度
                            @height			窗口高度
                            @left			窗口左边距
                            @top			窗口右边距
            Output:
            Return:         <0: 失败.
                            =0: 成功.
            Others:         无
            Author:         FYX
            Date:           2024-05-31
        *************************************************/
        [DllImport("SShow3DImage.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Show3DImage(
            int[] heightImage,
            int ImageWidth,
            int ImageHeight,
            double ImageXinterval,
            double ImageYinterval,
            int streamline = 1,
            int width = 960,
            int height = 720,
            int left = 50,
            int top = 50);
        #endregion
    }
}

