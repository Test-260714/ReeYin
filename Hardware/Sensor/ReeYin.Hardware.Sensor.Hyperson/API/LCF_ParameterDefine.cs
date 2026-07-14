using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.Hyperson.API
{
    public class LCF_ParameterDefine
    {
        /**,
        * describe: 传感器曝光时间,单位us
        * type: int
        * value:
        */
        public const string PARAM_EXPOSURE_TIME = "exposure_time";

        /**,
        * describe: 传感器曝光模式。正常曝光模式： CMOS曝光时间等于用户设定的曝光时间；
        *           动态曝光模式： 曝光时间等于分频后编码器脉冲间隔,用户电机运行速度越慢，曝光时间越长。
        *           用户可以通过电机速度调节曝光时间,该模式只有在编码器触发和外部触发模式下有效
        * type: int
        * value: 取值参考LCF_ExposureMode_t类型定义
        */
        public const string PARAM_EXPOSURE_MODE = "exposure_mode";

        /**,
        * describe: 设置照明强度
        * type: float
        * value: 照明强度(0.0~100.0)
        */
        public const string PARAM_LIGHT_INTENSITY = "light_intensity";

        /**,
         * describe:    扫描数据缓存的位置选择。
         * 有两种缓存方式:1、扫描数据先缓存在控制器，等扫描完成后再通过LCF_ExportCacheData导出控制器数据
         *              2、扫描数据缓存在PC端，扫描完成后再通过LCF_ExportCacheData导出数据;使用这种方式需要先通过PARAM_SDK_CACHE_SIZE参数设置SDK缓存大小
         *              默认扫描数据缓存在控制器中
         * type: int
         * value: 参考 LCF_DataCacheType_t
         */
        public const string PARAM_CACHE_SELECT = "cache_select";


        /**,
         * describe: SDK Cache大小,单位为轮廓个数;例：设置100000,则SDK最大可以缓存10万条轮廓;
         *            设置Cache大小后通过参数PARAM_CACHE_SELECT选择数据缓存到PC端
         *            注意:需要保证PC端有足够的内存，否则设置可能会失败
         * type: int
         * value:缓存的轮廓个数
         */
        public const string PARAM_SDK_CACHE_SIZE = "sdk_cache_size";

        /**,
         * describe: 使用SDK Cache导出数据时，等待控制器发送FIFO为空，保证控制器当前发送FIFO所有数据已经全部发送出来
         *           在高帧率下以太网带宽可能会被占满导致实时发送的数据堆积在控制器发送FIFO中,使能该功能后在使用SDK Cache导出数据时，会等待直到控制发送FIFO所有数据已经全部发送出来
         *           默认该功能为关闭状态
         * type: int
         * value: 0x00:关闭该功能  0x01:使能该功能
         */
        public const string PARAM_EXP_WAIT_FIFO_EMPTY = "exp_wait_fifo_empty";


        /**,
        * describe: 设置轮廓数量回调阈值，只有控制器内部轮廓数量达到该阈值才会通过回调函数通知用户
        *           在高帧率下用户通过调高该值可以减少回调的频率、降低以太网带宽负载。默认值为1，即回调的频率等于传感器当帧率
        * type: int
        * value: 1~128
        */
        public const string PARAM_COUNTOUR_LINE_THRESHOLD = "countour_line_threshold";

        /**,
        * describe: 设置Cache缓存轮廓的阈值。在Cache轮廓数量达到用户设定的阈值后，通过回调函数通知用户。设置为0则关闭该功能，默认值为0
        *           在达到阈值后用户需要自己调用LCF_ExportCacheData接口从控制器导出数据，导出结束后调用LCF_ClearCacheData清空Cache数据
        * type: int
        * value: 0~100000
        */
        public const string PARAM_CACHE_COUNTOUR_THRESHOLD = "cache_countour_threshold";

        /**,
        * describe: 置帧率控制模式，只在内部触发模式下有效
        * type: int
        * value: 1:使能  0:关闭
        */
        public const string PARAM_FRAME_RATE_CONTROL = "frame_rate_control";

        /**,
        * describe:  设置传感器帧率，只有在使能帧率控制下才有效
        * type: int
        * value: 根据当前分辨率设置帧率，可通过PARAM_MAX_FRAME_RATE 参数获取当前分辨率下支持的最大帧率
        */
        public const string PARAM_FRAME_RATE = "frame_rate";

        /**,
         * describe: X轴的点数
         * type: int
         * value: 返回当前设置下X轴点数
         */
        public const string PARAM_X_COUNT = "x_count";

        /**,
         * describe: 光斑信号长度
         * type: int
         * value: 返回当前设置下光斑信号长度
         */
        public const string PARAM_SIGNAL_LEN = "signal_len";

        /**,
        * describe: 设置时间域滑动中值滤波器深度
        * type: int
        * value: 1~33
        */
        public const string PARAM_T_MEDIAN_FILTER = "t_median_filter";

        /**,
        * describe: 清除滑动中值滤波窗口计数值
        * type: int
        * value: 固定填0
        */
        public const string PARAM_CLEAR_T_MEDIAN_FILTER_CNT = "clear_t_median_filter_cnt";

        /**,
        * describe: 设置时间域滑动平均滤波器深度
        * type: int
        * value: 1~64
        */
        public const string PARAM_T_MOVING_AVG_FILTER = "t_moving_avg_filter";

        /**,
        * describe: 清除滑动平均滤波窗口计数值
        * type: int
        * value: 固定填0
        */
        public const string PARAM_CLEAR_T_AVG_FILTER_CNT = "clear_t_avg_filter_cnt";
        /**,
        * describe: 设置空间域滑动中值滤波器深度
        * type: int
        * value: 1~33
        */
        public const string PARAM_S_MEDIAN_FILTER = "s_median_filter";

        /**,
        * describe: 设置空间域滑动平均滤波器深度
        * type: int
        * value: 1~64
        */
        public const string PARAM_S_MOVING_AVG_FILTER = "s_moving_avg_filter";

        /**,
        * describe: 空间域滑动平均滤波器Z方向阈值，高度差高于该阈值的点不纳入平均
        * type: int
        * value: 高度差阈值，单位um
        */
        public const string PARAM_S_MOVING_AVG_Z_THRE = "s_moving_avg_filter_z_thre";

        /**,
        * describe: 设置编码器分频模式，分为固定分频和动态分频。
        *           固定分频(默认方式):按照固定的分频值对外部编码器脉冲进行分频,扫描后的图像Y方向的间距为固定值;
        *           动态分频:用户可以根据编码器脉冲个数设定几个不同的分频值，扫描后的图像Y方向的间距动态变化。例:在编码器脉冲个数0(起始位置)使用一个分频值；在编码器脉冲个数到达1000时使用另外一个分频值
        *                  通过LCF_SetEncoderDyncDivValue接口可以设置不同区间的分频值
        * type: int
        * value: 参考 LCF_EncoderDivisionMode_t
        */
        public const string PARAM_ENCODER_DIV_MODE = "encoder_div_mode";

        /**,
        * describe: 设置编码器分频系数
        * type: int
        * value: 1~65535
        */
        public const string PARAM_ENCODER_DIV = "encoder_div";

        /**,
        * describe: 设置无信号阈值，信号幅值小于该值时认为该测量点无信号
        * type: int
        * value: 1~255
        */
        public const string PARAM_INVALID_SIGNAL_THRESHOLD = "invalid_signal_threshold";

        /**,
        * describe: 设置信号计算阈值，例:设置为0.5则只取光斑信号50%进行计算
        * type: float
        * value: 0~1
        */
        public const string PARAM_CALCULATE_THRESHOLD = "calculate_threshold";

        /**,
        * describe: 设置传感器模拟增益
        * type:  int
        * value: 取值参考LCF_CameraGain_t类型定义
        */
        public const string PARAM_GAIN = "gain";

        /**,
        * describe: 设置HDR多帧合成模式，例:该参数设置为LCF_HDRMode_4，则控制器每一帧计算结果会用4帧不同光强数据进行平均合成一帧
        * type:  int
        * value: 取值参考LCF_HDRMode_t类型定义
        */
        public const string PARAM_HDR = "hdr";

        /**,
         * describe:HDR多曝光模式，支持每帧数据不同的曝光时间，通过LCF_SetHDRExposureGroup接口可设置每帧的曝光时间；每帧的最小曝光时间通过参数PARAM_HDR_MINI_EXP_TIME获取
         * type:  int
         * value: 0x01:使能  0x00:关闭
         */
        public const string PARAM_HDR_MULTI_EXP = "hdr_multi_exp";

        /**,
         * describe: HDR多曝光模式下，当前量程支持的最小曝光时间；量程越小、或开启binning模式时，该数值越小
         * type:  int
         * value:
         */
        public const string PARAM_HDR_MINI_EXP_TIME = "hdr_mini_exp_time";

        /**,
         * describe CMOS HDR模式，增加动态范围。
         * type:  int
         * value: 取值参考LCF_HDR_CMOS_t类型定义
         */
        public const string PARAM_HDR_CMOS = "hdr_cmos";

        /**,
        * describe HDR模式下限制信号饱和区域的滤除范围，饱和区域周围对应长度的区域会被滤除。信号饱和区域越大，该数值应该越大;设置为0则只要信号饱和,该帧就会被丢弃。
        * type:  int
        * value: 0~255
        */
        public const string PARAM_HDR_SAT_REMOVE_LEN = "hdr_sat_remove_len";

        /**,
        * describe: 设置传感器触发方式
        * type:  int
        * value: 取值参考LCF_TriggerMode_t类型定义
        */
        public const string PARAM_TRIGGER_MODE = "trigger_mode";

        /**,
        * describe: 设置binning模式
        * type:  int
        * value: 取值参考LCF_BinningMode_t类型定义
        */
        public const string PARAM_BINNING_MODE = "binning_mode";

        /**,
        * describe: 设置传感器测量模式
        * type:  int
        * value: 取值参考LCF_MeasureModet_t类型定义
        */
        public const string PARAM_MEASURE_MODE = "measure_mode";

        /**,
        * describe: 设置HDR模式下，输出灰度图是否将过曝和无信号的信号滤除
        * type:  int
        * value: 1:使能  0:关闭
        */
        public const string PARAM_REMOVE_INVALID_SIGNAL = "remove_invalid_signal";

        /**,
        * describe: 设置控制IP地址
        * type:  string
        * value: 例: "192.168.1.100"
        */
        public const string PARAM_IP_ADDR = "ip_addr";

        /**,
         * describe: 获取控制器当前的IP地址
         * type:  string
         * value: 返回控制器当前的IP地址
         */
        public const string PARAM_CUR_IP_ADDR = "cur_ip_addr";

        /**,
        * describe: 设置控制端口号
        * type:  int
        * value: 1~65535
        */
        public const string PARAM_PORT_NUMBER = "port_number";

        /**,
        * describe: 当前分辨率下支持的最大帧率
        * type:  int
        * value:
        */
        public const string PARAM_MAX_FRAME_RATE = "max_frame_rate";

        /**,
        * describe:获取编码器是否漏触发
        * type:  int
        * value: 1:编码器漏触发，用户需要调低电机运行速率或通过PARAM_ENCODER_DIV参数调高编码器分频值    0:触发正常
        */
        public const string PARAM_IS_TRIGGER_PASS = "is_trigger_pass";

        /**,
		 * describe:获取编码器漏触发个数
		 * type:  int
		 * value:
		 */
        public const string PARAM_TRIGGER_PASS_COUNT = "trigger_pass_count";

        /**,
        * describe: 设置传感器数据输出，默认为使能状态;使能后启动传感器测量，测量数据会通过回调函数通知用户
        * type:  int
        * value: 1:使能  0:关闭
        */
        public const string PARAM_STREAM_OUT = "stream_out";

        /**,
        * describe: 获取控制器固件版本号
        * type:  string
        * value: 返回固件版本号
        */
        public const string PARAM_CONTROLER_FW_VERSION = "controler_fw_version";

        /**,
        * describe: 获取SDK版本号
        * type:  string
        * value: 返回SDK版本号
        */
        public const string PARAM_SDK_VERSION = "sdk_version";

        /**,
        * describe: 获取传感器固件版本号
        * type:  string
        * value: 返回传感器固件版本号
        */
        public const string PARAM_SENSOR_FW_VERSION = "sensor_fw_version";

        /**,
        * describe: 获取传感器序列号
        * type:  string
        * value: 返回传感器序列号
        */
        public const string PARAM_SENSOR_SN = "sensor_SN";

        /**,
        * describe: 获取控制器序列号
        * type:  string
        * value: 返回控制器序列号
        */
        public const string PARAM_CONTROLER_SN = "controler_SN";

        /**,
        * describe: 获取相机序列号
        * type:  string
        * value: 返回相机序列号
        */
        public const string PARAM_CAMERA_SN = "camera_SN";

        /**,
        * describe: 获取相机固件版本
        * type:  string
        * value: 返回相机固件版本
        */
        public const string PARAM_CAMERA_FW_VERSION = "camera_fw_version";

        /**,
        * describe: 获取当前Cache缓存的轮廓个数
        * type:  int
        * value: 返回轮廓个数
        */
        public const string PARAM_CACHE_CONTOUR_COUNT = "cache_contour_count";

        /**,
        * describe: 获取控制器Cache缓存容量，该值决定控制器能缓存的最大轮廓数量
        * type:  int
        * value: 返回轮廓个数
        */
        public const string PARAM_CACHE_CAPACITY = "cache_capacity";

        /**,
         * describe: 获取X轴点间隔，单位um
         * type:  float
         * value: 返回X轴点间隔
         */
        public const string PARAM_X_INTERVAL = "x_interval";

        /**,
        * describe: 编码器输入模式
        * type:  int
        * value: 取值参考LCF_EncoderInputMode_t类型定义
        */
        public const string PARAM_ENCODER_INPUT_MODE = "encoder_input_mode";

        /**,
        * describe: 心跳包超时时间，单位秒
        * type:  int
        * value: 心跳包超时时间
        */
        public const string PARAM_HEART_BEAT_TIMEOUT = "heart_beat_timeout";

        /**,
        * describe: 使能输出质心数据
        * type:  int
        * value: 1:使能   0:关闭
        */
        public const string PARAM_BARYCENTER_DATA = "barycenter_data";

        /**,
        * describe: 对空洞进行插值，最大的空洞长度(点数)，超过该长度的空洞不进行插值
        * type:  int
        * value: 空洞的长度(点数)
        */
        public const string PARAM_MAX_INTERPOLATION_POINT = "max_interpolation_point";

        /**,
        * describe:对空洞进行插值，最大Z方向间隙(um)，超过该间隙的空洞则不进行插值
        * type: int
        * value: 最大间隙(um)
        */
        public const string PARAM_MAX_INTERPOLATION_THICKNESS = "max_interpolation_thickness";

        /**,
         * describe: 信号数量限制，该参数只有在近端测量模式或远端测量模式或多波峰模式下有效，如果某一扫描点的信号数量不等于该值，则该点会被填充为空洞点。
         *           在测量玻璃上表面或玻璃下表面时，有时因为信号比较弱导致SDK只检测到一个信号无法分辨该信号为上表面信号或下表面信号。
         *           可以通过设置该参数保证检测信号的真确性。
         * type:  int
         * value: 信号数量限制，设置为0则不做任何限制
         */
        public const string PARAM_SIGNAL_NUMBER_LIMIT = "signal_number_limit";

        /**,
         * describe: 每个信号最小计算点数阈值，小于该点数的信号认为是无效信号，该点会被填充为空洞值
         * type:  int
         * value: 点数
         */
        public const string PARAM_CAL_POINTS_THRESHOLD = "cal_points_threshold";

        /**,
         * describe: 2维平面中值滤波，该滤波为2维滤波，只对LCF_ExportCacheData接口导出的测量结果有效
         * type:  int
         * value: 滤波尺寸,设置为1则关闭滤波，其他数值则使用 value x value的结构元尺寸对深度图进行滤波
         */
        public const string PARAM_MEDIAN_FILTER_2D = "median_filter_2d";

        /**,
         * describe: 2维平面平均滤波，该滤波为2维滤波，只对LCF_ExportCacheData接口导出的测量结果有效
         * type:  int
         * value: 滤波尺寸,设置为1则关闭滤波，其他数值则使用 value x value的结构元尺寸对深度图进行滤波
         */
        public const string PARAM_AVG_FILTER_2D = "avg_filter_2d";

        /**,
         * describe:  设置空间域滑动平均滤波器基于卷积核中心点的距离标准差。距离中心点越近权重越大，该值设置的越大越趋于平均。该滤波为2维滤波，只对LCF_ExportCacheData接口导出的测量结果有效
         * type: float
         * value:
         */
        public const string PARAM_AVG_FILTER_SIGMA_S_2D = "avg_filter_sigma_s_2d";

        /**,
         * describe: 设置空间域滑动平均滤波器基于卷积核中心点的差值标准差,和中心点的差值越小权重越大，该值设置的越大越趋于平均。该滤波为2维滤波，只对LCF_ExportCacheData接口导出的测量结果有效
         * type: float
         * value:
         */
        public const string PARAM_AVG_FILTER_SIGMA_DIFF_2D = "avg_filter_sigma_diff_2d";

        /**,
        * describe: 2维平面开运算，可以将一些孤立的信号点、毛刺滤除，该滤波为2维滤波，只对LCF_ExportCacheData接口导出的测量结果有效
        * type:  int
        * value: 滤波尺寸,设置为1则关闭滤波，其他数值则使用 value * value的结构元的尺寸对深度图进行滤波
        */
        public const string PARAM_MORPH_OPEN_2D = "morph_open_2d";

        /**,
         * describe: 2维平面开运算Z方向阈值，和临近点差值大于该值认为是离群点将被滤除，只对LCF_ExportCacheData接口导出的测量结果有效
         * type:  int
         * value: Z方向阈值，单位um
         */
        public const string PARAM_MORPH_OPEN_Z_THRE_2D = "morph_open_z_thre_2d";

        /**,
		 * describe: 2维平面闭运算标准差，如果空洞点附近标准差大于该阈值，则不进行填充，
		 * type:  int
		 * value:
		 */
        public const string PARAM_MORPH_CLOSE_SIGMA_2D = "morph_close_sigma_2d";

        /**,
        * describe: 2维平面闭运算，可以填充无信号的空洞点，该滤波为2维滤波，只对LCF_ExportCacheData接口导出的测量结果有效
        * type:  int
        * value: 滤波尺寸,设置为1则关闭滤波，其他数值则使用 value * value 的结构元的尺寸对深度图进行滤波
        */
        public const string PARAM_MORPH_CLOSE_2D = "morph_close_2d";

        /**,
         * describe: 灰度数据FFT2宽度
         * type:  int
         * value: 宽度
         */
        public const string PARAM_INTENSITY_FFT2_W = "intensity_fft2_w";

        /**,
         * describe: 灰度数据FFT2高度
         * type:  int
         * value: 宽度
         */
        public const string PARAM_INTENSITY_FFT2_H = "intensity_fft2_h";

        /**,
         * describe: 深度数据FFT2宽度
         * type:  int
         * value: 宽度
         */
        public const string PARAM_HEIGHT_FFT2_W = "height_fft2_w";

        /**,
         * describe: 高度数据FFT2高度
         * type:  int
         * value: 宽度
         */
        public const string PARAM_HEIGHT_FFT2_H = "height_fft2_h";

        /**,
        * describe: 设置/读取输出端口（OUT0~OUT1）状态，一个Bit对应一个输出引脚状态，例:Bit0对应OUT0、Bit1对应OUT1
        * type:  int
        * value:  输出端口状态
        */
        public const string PARAM_OUTPUT_PIN_STATE = "output_pin_state";

        /**,
        * describe: 读取输入端口（IN0~IN1）状态，一个Bit对应一个输出引脚状态，例:Bit0对应IN0、Bit1对应IN1
        * type:  int
        * value:  输入端口状态
        */
        public const string PARAM_INPUT_PIN_STATE = "input_pin_state";

        /**,
        * describe: 折射率标定文件
        * type:  string
        * value: 文件路径名,设置为""则清空控制器保存的折射率标定文件
        */
        public const string PARAM_REFRACTIVE_INDEX_FILE = "refractive_index_file";

        /**,
        * describe:  多层距离、远端模式使能折射率校正，用户需要先使用标准厚度的玻璃对折射率进行标定或加载已经标定好的折射率标定文件。默认不使能折射率校正；
        * type:  int
        * value: 1 使能   0 关闭
        */
        public const string PARAM_REFRACTIVE_CAL = "refractive_cal";

        /**,
        * describe:  外部触发模式配置
        * type:   int
        * value:  取值参考LCF_ExternalTriggerMode_t类型定义
        */
        public const string PARAM_EXTERNAL_TRIGGER_MODE = "external_trigger_mode";

        /**,
        * describe: 当前分辨率下Z轴量程，单位um
        * type: float
        * value: 返回Z轴量程
        */
        public const string PARAM_Z_RANGE = "z_range";

        /**,
        * describe: X轴降采样,减少X轴点数，扩大X轴点间隔。用户可以通过X轴降采样降低数据量
        * type: int
        * value: 取值参考LCF_X_Subsample_t类型定义
        */
        public const string PARAM_X_SUBSAMPLE = "x_subsample";

        /**,
        * describe: X轴量程
        * type: int
        * value: 取值参考LCF_X_Range_t类型定义
        */
        public const string PARAM_X_RANGE = "x_range";

        /**,
         * describe: X轴量程缩小方向，在缩小X轴量程时可以选择裁剪掉X方向尾部、头部或两端
         * type: int
         * value: 取值参考LCF_X_Reduce_dir_t类型定义
         */
        public const string PARAM_X_REDUCE_DIR = "x_reduce_dir";

        /**,
        * describe: Z轴降采样,减低Z轴精度，减低资源占用率
        * type: int
        * value: 取值参考LCF_Z_Subsample_t类型定义
        */
        public const string PARAM_Z_SUBSAMPLE = "z_subsample";

        /**,
        * describe: 对灰度图进行锐化增强
        * type: int 0x01 使能锐化    0x00 关闭锐化
        * value:
        */
        public const string PARAM_INTENSITY_SHARPEN = "intensity_sharpen";

        /**,
        * describe: 外部IO同步输出设置。使能后在传感器执行一次测量后，sync out 引脚会翻转一次
        * type: int 0x01 使能    0x00 关闭
        * value:
        */
        public const string PARAM_TRIGGER_SYNC_OUT = "trigger_sync_out";

        /**,
        * describe: 信号平滑长度，默认为不进行平滑(长度为1)
        * type: int 1~11
        * value:
        */
        public const string PARAM_SIGNAL_SMOOTH_LEN = "signal_smooth_len";

        /**,
        * describe: 灰度图重采样，默认使能
        * type: int 0x01 使能    0x00 关闭
        * value:
        */
        public const string PARAM_INTENSITY_RESAMPLE = "intensity_resample";

        /**,
        * describe: IO 输入消抖延迟时间，单位ms。若没有外部硬件消抖则该延迟时间应该设置得比较大,设置为0则关闭消抖功能。默认值为50
        * type: int
        * value:
        */
        public const string PARAM_IO_DEBOUNCE_TIME = "io_debounce_time";

        /**,
        * describe:
        * 第一层和第二层之间的最小距离(um)。
        * 在多距离模式下，如果第一层点缺失了，返回的测量结果中第二层的点就会被检测为第一层点。可以通过设置该参数对多层距离进行分类。设置为0则关闭缺失层检测功能，默认值0。典型值30-100um
        * type: int
        * value:
        */
        public const string PARAM_MISSING_FIRST_LAYER_MIN_DIST = "missing_first_layer_min_dist";

        /**,
        * describe:
        * 第一层距离的最小点数。
        * 在多距离模式下，如果第一层点缺失了，返回的测量结果中第二层的点就会被检测为第一层点。可以通过设置该参数对多层距离进行分类。设置为0则关闭缺失层检测功能，默认值0。典型值10-40,该参数可以减少对第一层噪声值的误判
        * type: int
        * value:
        */
        public const string PARAM_MISSING_FIRST_LAYER_MIN_LEN = "missing_first_layer_min_len";

        /**,
        * describe:
        * 多距离模式下选择灰度图对应哪一层距离的灰度图，设置为-1则灰度图对应信号强度最强的层，默认值为-1
        * type: int
        * value:
        */
        public const string PARAM_INTENSITY_SELECT = "intensity_select";

        /**,
        * describe: 集群滤波最小长度，小于指定长度的集群轮廓将会被删除。设置为0则不启用该功能
        *
        * type: int
        * value:
        */
        public const string PARAM_CLUSTER_FILTER_MIN_LEN = "cluster_filter_min_len";

        /**,
        * describe:集群中两点之间的最大Z距离，单位um,。设置为0则不启用该功能
        *
        * type: int
        * value:
        */
        public const string PARAM_CLUSTER_FILTER_DISTANCE_Z = "cluster_filter_distance_z";

        /**,
        * describe:设置信号检测的灵敏度，高灵敏度容易检测到合并再一起的两个波峰。
        *
        * type: int
        * value:  取值参考LCF_SignalDetectSensitivity_t类型定义
        */
        public const string PARAM_SIGNAL_DETECT_SENSITIVITY = "signal_detect_sensitivity";

        /**,
        * describe:自定义信号检测灵敏度，在PARAM_SIGNAL_DETECT_SENSITIVITY参数设置为LCF_CustomizeSensitivity后可以灵敏度等级
        *
        * type: int
        * value: 0~10个等级
        */
        public const string PARAM_SIGNAL_DETECT_CUSTOM_SENSITIVITY = "signal_detect_custom_sensitivity";

        /**,
        * describe:光源超功率模式，提高亮度
        *
        * type: int int 0x01 使能    0x00 关闭
        * value:
        */
        public const string PARAM_LIGHT_OVERPOWER = "light_overpower";

        /**,
         * describe:去除工件边缘由于点扩散函数导致出现翘起的伪边沿
         * type: int  边沿滤除最大长度,该值小于等于1时，则关闭该滤波功能，默认值为0
         * value:
         */
        public const string PARAM_TRIM_EDGES_FILTER = "trim_edges_filter";

        /**,
         * describe:HDR模式下选择灰度图类型,可以选择使用多帧平均灰度/灰度最大值的一帧/灰度值最小的一帧作为最终灰度图
         * type: int
         * value: 取值参考LCF_HDR_Intensity_t类型定义
         */
        public const string PARAM_HDR_INTENSITY_SELECT = "hdr_intensity_select";

        /**,
         * describe: X方向左边无效点个数
         * type: int
         * value: 返回X方向左边无效点个数
         */
        public const string PARAM_X_INVALID_LEFT = "x_invalid_left";

        /**,
         * describe: X方向右边无效点个数
         * type: int
         * value: 返回X方向右边无效点个数
         */
        public const string PARAM_X_INVALID_RIGHT = "x_invalid_right";

        /**,
         * describe: 灰度补偿
         * type: int 0x01 使能    0x00 关闭
         * value:
         */
        public const string PARAM_INTENSITY_CAL = "intensity_cal";

        /**,
        * describe: 光源使用寿命延长
        * type: int 0x01 使能    0x00 关闭
        * value:
        */
        public const string PARAM_ENERGY_SAVING_MODE = "energy_saving_mode";


        /**,
         * describe: SDK支持的最大检测层数
         * type: int
         * value: 返回最大检测层数
         */
        public const string PARAM_SDK_MAX_DETECT_LAYER = "sdk_max_detect_layer";


        /**,
         * describe: 控制器支持的最大检测层数
         * type: int
         * value: 返回最大检测层数
         */
        public const string PARAM_CONTROLER_MAX_DETECT_LAYER = "controler_max_detect_layer";


        /**,
         * describe:设置自定义的控制器User ID。LCF_ScanDeviceList_Detail返回的控制器详细信息中包含了User ID
         * type:   该参数需要使用LCF_SetControllerParam接口进行设置，该参数不受SDK连接状态影响，在SDK未连接控制器前依旧可以设置
         * value:  输入自定义的User ID字符串，例: "User_0"
         */
        public const string PARAM_CTRL_SET_USER_ID = "ctrl_set_user_id";


        /**,
         * describe:重启控制器
         * type:   该参数需要使用LCF_SetControllerParam接口进行设置，该参数不受SDK连接状态影响，在SDK未连接控制器前依旧可以设置
         * value:  忽略
         */
        public const string PARAM_CTRL_REBOOT = "ctrl_reboot";


        /**,
         * describe:清零LCF_DeviceStatisticInfo_t统计信息中相机CMOS历史最大帧率
         *
         * type: int
         * value: 忽略
         */
        public const string PARAM_CLEAR_MAX_CMOS_FRAME_RATE = "clear_max_cmos_frame_rate";


        /**,
         * describe:导出传感器配置文件，需要指定导出的路径和文件名
         * type:   string
         * value: 输入导出的文件名
         */
        public const string PARAM_EXPORT_SENSOR_SETTING = "export_sensor_setting";


        /* describe:获取用户Flash当前的数据长度
        *  type:   int
        * value: 返回Flash数据长度
        */
        public const string PARAM_USER_FLASH_DATA_LEN = "user_flash_data_len";


        /* describe:获取用户Flash容量
        *  type:    int
        * value:   返回Flash容量
        */
        public const string PARAM_USER_FLASH_CAPACITY = "user_flash_capacity";
    }
}
