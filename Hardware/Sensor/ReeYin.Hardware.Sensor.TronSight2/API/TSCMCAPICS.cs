
using System;
using System.Runtime.InteropServices;
using InstanceHandle = System.UInt64;
using uint16_t = System.UInt16;
using uint8_t = System.Byte;
namespace ReeYin.Hardware.Sensor.TronSight2.API
{//begin of namespace tscmccs
    public struct ConstDef {
public const int CMOS_PIXEL_NUM = 1024; ///<CMOS传感器像素数据
public const int FFT_FRAME_PIXEL_NUM = 512; ///<干涉测厚模式下，FFT干涉图像长度
public const int IRCS_FFT_FRAME_PIXEL_NUM = 1024; ///<红外干涉干涉测厚模式下，FFT干涉图像长度
public const int MAX_FFT_PIXEL_INDEX = 511; ///<最大FFT序列位置索引，比FFT像素数小1
public const int MAX_IRCS_FFT_PIXEL_INDEX = 1023; ///<红外最大FFT序列位置索引，比FFT像素数小1
public const int MAX_SENSOR_CHANNEL = 4; ///<传感头通道数
public const int MAX_DIGITAL_CHANNEL = 4; ///<数字输出通道
public const int MAX_DIGITAL_LIMIT_CHANNEL = 3; ///<数字上下限源通道数
public const int MAX_ANANLOG_CHANNEL = 2; ///<模拟输出通道数
public const int OBJECT_NAME_LEN = 20; ///<折射率表物体名称长度
public const int MAX_DATA_TYPES = 20; ///<最大数据输出类型数
public const int MAX_SINGLE_DATA_CNT = 64; ///<读取单次测量数据长度
public const int MAX_INDICATE_SIZE = 16; ///<最大标签数量
public const int MAX_REFRACTIVE_TABLE_NUM = 32; ///<折射率表数量
public const int MAX_ENCODER_CHANNEL = 2; ///<最大编码器数量
public const int MAX_SERIAL_NUMBER_LEN = 19; ///<序列号最大长度
public const int CALIB_TABLE_SIZE = 512; ///<标定表长度
public const int MAX_PEAK_NUM = 10; ///<最大可识别峰数目
public const int INTERF_WEIGHT_NEAREST_PIXEL = 4; ///<邻近像素数
public const double INVALID_MEASURE_DATA = -2147.483648; ///<无效数据值
public const int DEFAULT_LOCAL_ETHERNET_LISTEN_PORT = 8001; ///<默认PC端监听端口
public const int MAX_OUTPUT_PEAK_NUM = 6; ///<最大输出峰数目
public const int THICKNESS_LAYER_NUMBER = 5; ///<厚度个数
public const int EXTRA_PEAK_NUM =4;///<单层之外的峰个数
public const int MAX_ENCODER_DATA_TYPES = 3; ///<最大编码器类型数
public const int MAX_MATH_DATA_TYPES = 8; ///<最大双探头运算数据类型数
public const int MAX_MULTI_MATH_DATA_TYPES = 4; ///<最大四光点运算类型数
public const int MAX_ANY_MATH_DATA_TYPES = 16; ///<最大任意测量值运算类型数

	};
/**
* @brief 设备类型
*/
public enum DEVICE_TYPE {
	CONTROLLER,///<控制器
	SENSOR,///<传感头
};
/**
* @brief 通讯连接端口
*/
public enum CONNECTION_TYPE {
	USB,///<USB
	SERIAL,///<串口，上位机通信中未使用
	ETHERNET,///<以太网
};
/**
* @brief 传感头模型
*/
public enum SENSOR_MODEL {
	C400   = 0x11,	///<光谱C400探头
	C1200  = 0x12,	///<光谱C1200探头
	C4000N = 0x13,	///<光谱C4000N探头
	C4000F = 0x14,	///<光谱C4000F探头
	C6000  = 0x15,	///<光谱C6000探头
	C7000  = 0x16,	///<光谱C7000探头
	CR1500 = 0x17,	///<光谱CR1500探头
	C3000  = 0x18,	///<光谱C3000探头
	C10000 = 0x19,	///<光谱C10000探头
	CR1500N = 0x1A,	///<光谱CR1500探头
	C2400   = 0x1B, ///<光谱C2400探头
	C400V2  = 0x1C,///<光谱C400V2版本探头
	C1200V2 = 0x1D,///<光谱C1200V2版本探头
	C3000V2 = 0x1E,///<光谱C3000V2版本探头
	C600 = 0x1F,///<光谱C600版本探头
	C16000 = 0x31,///<光谱C16000版本探头
	C50000 = 0x32,///<光谱C50000版本探头
	C20000 = 0x33,///<光谱C20000版本探头
	CR4000 = 0x34,
	C2000 = 0x35,
	C2600S = 0x36,
	C100 = 0x37,
};

/**
* @brief 干涉测厚传感器探头型号
*/
public enum SENSOR_MODEL_IT {
	_50 = 0x11,
};
/**
* @brief 控制器版本
*/
public enum CONTROLLER_VERSION {
	NONE = 0x00,///<未知控制器类型
	TC1  = 0x01,///<TS-TC1单通道光谱共焦控制器
	TC2  = 0x02,///<TS-TC2双通道光谱共焦控制器
	TC4  = 0x04,///<TS-TC2双通道光谱共焦控制器
};
/**
* @brief 功能状态
*/
public enum STATE {
	OFF,		///<关闭
	ON,		///<开启
};
/**
* @brief 原始图像数据源
*/
public enum FRAME_DATA_SRC {
	ORIGIN,		///<原始图像
	CALIB,		///<校准图像
	SHARPNESS		///<锐度图像
};
/**
* @brief 滤波器窗口尺寸
*/
public enum FILTER_WINDOW_WIDTH {
	_4,		///<窗口尺寸4
	_16,		///<窗口尺寸16
	_64,		///<窗口尺寸64
	_256,		///<窗口尺寸256
	_1024,	///<窗口尺寸1024
	_4096,	///<窗口尺寸4096
	_1,		///<窗口尺寸1
	_2,		///<窗口尺寸2
};
/**
* @brief 编码器滤波尺寸
*/
public enum ENCODER_FILTER_WIDTH {
	NONE,	///<无滤波
	_4,		///<窗口尺寸4
	_16,		///<窗口尺寸16
};
/**
* @brief 编码器输出形式
*/
public enum ENCODER_OUTPUT_MODE {
	X1,		///<单路脉冲
	X2,		///<双路脉冲
	X4,		///<四路脉冲
};
/**
* @brief 测量数据超限警告来源通道
*/
public enum WARNING_INPUT_CHANNEL {
	CONTROLLER = 0,//控制器
	CH1 = 1,///<通道1
	CH2 = 2,///<通道2
	CH3 = 3,///<通道1
	CH4 = 4,///<通道2
};
/**
* @brief 测量数据超限警告来源类型
*/
public enum WARNING_SOURCE {
	DIST1,///<距离1
	DIST2,///<距离2
	THICKNESS,///<厚度2
	DIST3,///<距离3
	DIST4,///<距离4
	DIST5,///<距离5
	DIST6,///<距离6
	THICKNESS2,///<厚度2
	THICKNESS3,///<厚度3
	THICKNESS4,///<厚度4
	THICKNESS5,///<厚度5

	MATH1 = 0,///<通道数据加减计算值1
	MATH2 = 1,///<通道数据加减计算值2
	MULTI_SENSOR_MATH1 = 2,///<多光点运算值1
};

/**
* @brief 用于进行数据运算的数据来源
*/
public enum MATH_DATA_SRC {
	DIST1,///<距离1
	DIST2,///<距离2
	DIST3,///<距离3
	DIST4,///<距离4
	DIST5,///<距离5
	DIST6,///<距离6
};
/**
* @brief MATH数据运算通道
*/
public enum MATH_CHANNEL {
	_1,///<MATH1
	_2,///<MATH2
};
/**
* @brief MATH通道符号
*/
public enum MATHSIGN {
	POS,  ///<正
	NEG,  ///<负
	ZERO, ///<零
};

/**
* @brief 模拟通道号
*/
public enum ANALOG_CHANNEL {
	CH1 = 1,		///<模拟输出端口1
	CH2 = 2,		///<模拟输出端口2
};
/**
* @brief 模拟通道电压/电流输出
*/
public enum ANALOG_OUTPUT_MODE {
	VOLTAGE,	///<模拟通道电压输出 -10~10 V
	CURRENT	///<模拟通道电流输出 4~20 mA
};
/**
* @brief AD芯片输出模式
*/
public enum ANALOG_OUT_RANGE {
	V_0TO5,		///< 0-5 V 
	V_0TO10,		///< 0-10 V 
	V_NEG5TO5,		///< +/- 5 V 
	V_NEG10TO10,		///< +/- 10 V 
	MA_4TO20,		///< 4-20mA 
};/**
* @brief 传感头采样间隔
*/
public enum SAMPLING_INTERVAL {
	_250US,		///<采样间隔250us
	_500US,		///<采样间隔500us
	_1MS,		///<采样间隔1ms
	_2MS,		///<采样间隔2ms
	_5MS,		///<采样间隔5ms
	_10MS,		///<采样间隔10ms
	_100US,		///<采样间隔100us
	_125US,		///<采样间隔125us
	_160US,		///<采样间隔160us
	_200US,     ///<采样间隔200us
	_50US,      ///<采样间隔50us
	_62_5US = 12,       ///<采样间隔62.5us
	_80US = 14,     ///<采样间隔80us
	_40US = 21,     ///<采样间隔40us
	_31_25US = 22,      ///<采样间隔31.25us
	};
/**
* @brief 数字输出端口.
*/
public enum DIGITAL_CHANNEL {
	CH1 = 1,		///<数字输出DO1
	CH2 = 2,		///<数字输出DO2
	CH3 = 3,		///<数字输出DO3
	CH4 = 4,		///<数字输出DO4
};	
/**
* @brief 数字通道输出极性
*/
public enum DIGITAL_OUTPUT_LEVEL {
	LOW,	///< 低电平，若用户使用数字输出接口为NPN型，则该枚举量对应为断开状态
	HIGH,	///< 高电平，若用户使用数字输出接口为NPN型，则该枚举量对应为连接状态
};
/**
* @brief 编码器输入通道
*/
public enum ENCODER_CHANNEL {
	CH1 = 1,		///<编码器通道1
	CH2 = 2,		///<编码器通道2
};
/**
* @brief 编码器输入模式
*/
public enum ENCODER_INPUT_MODE {
	A,		///< 单路脉冲
	AB,		///< 双路脉冲
};
/**
* @brief 外部触发/编码器通道触发源
*/
public enum TRIG_SOURCE {
	LEVEL,///< 单路脉冲，以上升沿为一个位移单位（360°）为一个位移单位
	ENCODER_AB,		///<AB，AB和ABZ模式以90°为一个位移单位
	ENCODER_ABZ,		///< ABZ，AB和ABZ模式以90°为一个位移单位
};
/**
* @brief 外部触发/编码器通道触发方向
*/
public enum TRIG_DIRECTION {
	POS,///<正向
	NEG,///<反向
	BOTH///<双向
};
/**
* @brief 外部触发/编码器通道触发方式
*/
public enum TRIG_MODE {
	COUNTER,///<计数触发
	POSITION,///<位置触发
};
/**
* @brief 外部触发/编码器通道换向模式
*/
public enum TRIG_TRACK_MODE {
	OFF,///<关闭
	ON,///<开启
};
/**
* @brief 触发同步模式配置
*/
public enum SYNC_MODE {
	SYNC,	///<同步触发，两个探头均以编码器ch1的触发信号触发
	ASYNC	///<异步触发，ch1和ch2上的探头分别以编码器ch1和ch2的触发信号触发
};
/**
* @brief 触发方式选择
*/
public enum TRIG_METHOD {
	NONE,///<无触发，传感头根据设置的采样间隔进行采样并输出
	ENCODER,///<编码器触发采样
	SYNCIN///<同步触发
};
/**
* @brief 峰选择模式
*/
public enum PEAK_SELECTION_MODE {
	NUMBER = 0,///<编号模式
	WINDOW = 1,///<窗模式
	MAX    = 2,///<最大值模式
	LAST   = 3,///<最后一个峰模式
};
/**
* @brief 数据输出来源类型
*/
public enum DATA_INPUT_SRC {
	DIST1 = 0,///<距离1
	DIST2 = 1,///<距离2
	THICKNESS = 2,///<厚度
	DIST3 = 3,	///<距离3
	DIST4 = 4,	///<距离4
	DIST5 = 5,	///<距离5
	DIST6 = 6,	///<距离6
	THICKNESS2 = 7,	///<厚度
	THICKNESS3 = 8,	///<厚度
	THICKNESS4 = 9,	///<厚度
	THICKNESS5 = 10,	///<厚度

	MATH1 = 0,///<通道数据加减计算值1
	MATH2 = 1,///<通道数据加减计算值2
	MULTI_SENSOR_MATH1 = 2,///<多光点运算值1
};/**
* @brief 数据输出来源类型
*/
public enum DIGITAL_INPUT_SRC {
	DIST1 = 0,///<距离1
	DIST2 = 1,///<距离2
	THICKNESS = 2,///<厚度1
	DIST3 = 3,	///<距离3
	DIST4 = 4,	///<距离4
	DIST5 = 5,	///<距离5
	DIST6 = 6,	///<距离6
	THICKNESS2 = 7,	///<厚度2
	THICKNESS3 = 8,	///<厚度3
	THICKNESS4 = 9,	///<厚度4
	THICKNESS5 = 10,	///<厚度5

	MATH1 = 0,///<通道数据加减计算值1
	MATH2 = 1,///<通道数据加减计算值2
	MULTI_SENSOR_MATH1 = 2,///<多光点运算值1
};
/**
* @brief 模拟输入通道
*/
public enum ANALOG_INPUT_CHANNEL {
	CONTROLLER,///<控制器
	CH1,	///<通道1
	CH2,	///<通道2
	CH3,	///<通道3
	CH4,	///<通道4
};
/**
* @brief 模拟通道数据源
*/
public enum ANALOG_SOURCE {
	DIST1 = 0,	///<距离1
	DIST2 = 1,	///<距离2
	THICKNESS = 2,	///<厚度
	DIST3 = 3,	///<距离3
	DIST4 = 4,	///<距离4
	DIST5 = 5,	///<距离5
	DIST6 = 6,	///<距离6
	THICKNESS2 = 7,	///<厚度
	THICKNESS3 = 8,	///<厚度
	THICKNESS4 = 9,	///<厚度
	THICKNESS5 = 10,	///<厚度

	MATH1 = 0,	///<MATH1
	MATH2 = 1,	///<MATH2
	MULTI_SENSOR_MATH1 = 2,///<多光点运算值1
};

/**
* @brief 数据输出来源通道
*/
public enum DIGITAL_INPUT_CHANNEL {
	CONTROLLER,		///<控制器
	CH1,	///<通道1
	CH2,	///<通道2
	CH3,	///<通道2
	CH4,	///<通道2
};/**
* @brief 数据输出条件
*/
public enum DIGITAL_OUTPUT_COND {
	OVER_LIMIT	    = 0,///<超上限
	UNDER_LIMIT	    = 1,///<超下限
	OVER_UNDER_LIMIT = 2,///<超上限或超下限
	WARNING = 3,///<出现警告
};
/**
* @brief SYNC输入模式
*/
public enum SYNC_INPUT_MODE {
	EDGE,		///<边沿触发
	LEVEL,		///<电平触发
};
/**
* @brief 输出数据选择
*/
public enum SENSOR_OUTPUT_DATA {
	DIST1 = 1,///<距离1
	DIST2,	///<距离2
	PEAK1_HEIGHT,///<峰1高度
	PEAK2_HEIGHT,///<峰2高度
	INTENSITY,///<光强
	EXPTIME,	///<曝光时间
	THICKNESS,///<厚度
	DIST3,///<距离3
	DIST4,///<距离4
	DIST5,///<距离5
	DIST6,///<距离6
	THICKNESS2,///<厚度2
	THICKNESS3,///<厚度3
	THICKNESS4,///<厚度4
	THICKNESS5,///<厚度5
	PEAK3_HEIGHT,///<峰3高度
	PEAK4_HEIGHT,///<峰4高度
	PEAK5_HEIGHT,///<峰5高度
	PEAK6_HEIGHT,///<峰6高度
};
/**
* @brief 控制器输出数据选择
*/
public enum CONTROLLER_OUTPUT_DATA {
	TIMESTAMP = 1,	///<时间戳
	ENCODER1,		///<编码器1读数
	ENCODER2,		///<编码器2读数
	MATH1,///<探头距离数据加减后输出 
	MATH2,///<探头距离数据加减后输出，第2个计算值
	MULTI_SENSOR_MATH1,///<多探头取平均或中值、最大值、最小值
};

/**
* @brief 错误代码
*/
public enum ERRCODE {
	OK = 0,		///<成功
	CMD_FAILED = -1,///<发送命令失败
	TIMEOUT = -2,///<接收命令超时
	WAITING_FOR_NEXT_FRAME = -3,///<等待下一帧
	MESSAGE_INCOMPLETE = -4,///<返回信息不全
	RETN_CMD_UNMATCH = -5,///<返回命令不匹配
	RETN_CMD_ERROR = -6,///<返回命令出错 
	ALREADY_OPEN = -7,///<通道已经打开
	INVALID_PARAMETER = -8,///<输入参数无效
	BAUDRATE_TOO_LOW = -9,///<波特率过小
	NO_DATA_IN_BUFFER = -10,///<缓冲区中无数据
	DEVICE_NOT_SUPPORTED = -11, ///<当前命令设备不支持
	DEVICE_NOT_CONNECTED = -12,///<设备未连接
	IS_ACQUIRE_DATA = -13,///<正在采集数据
	FILE_NOT_EXIST = -14,///<文件不存在
	FILE_NOT_OPEN = -15,///<文件打开失败
	SENSOR_INFO_NOT_FOUND = -16,///<未找到相应传感头信息
	FIRMWARE_NOT_SUPPORTED = -17,///<固件版本不支持
	CONTROLLER_TYPE_UNCOMPATIBLE = -18,///<控制器类型不支持
	CALIBRATION_TABLE_CHANNEL_UNCOMPATIBLE = -19,///<不是对应通道的标定表
	LOWER_GREATER_THAN_UPPER = -20,///<参数中设置的下限大于上限或起点大于终点
	OUT_OF_VALID_RANGE = -21,///<输入参数不在有效范围内
	CALIBRATION_TABLE_USED_BY_OTHER_CHANNEL = -22,///<标定表正在被其它通道使用
	SENSOR_CHANNAL_NOT_ENABLED = -23,///<探头通道未使能
	SENSOR_CHANNAL_USED_BY_MATH = -24,///<探头通道正用于MATH计算
	SENSOR_CHANNAL_SHOULD_NOT_EQUAL = -25,///<用于MATH计算的探头通道不应相同
	FUNCTION_DEPRECATED = -26,///<函数已不再使用
	UNKNOWN = -999,///<未知错误
};

/**
* @brief SYNC有效电平
*/
public enum SYNC_VALID_LEVEL {
	LOW,		///<低电平或下降沿触发
	HIGH,		///<高电平或上升沿触发
};
/**
* @brief SYNC用于计数触发时的滤波宽度，滤除小于设定时间长度的正脉冲或负脉冲
*/
public enum SYNC_FILTER_WIDTH {
	_0_1_US,///<滤波宽度为0.1us
	_0_4_US,///<滤波宽度为0.4us
	_1_6_US,///<滤波宽度为1.6us
	_6_4_US,///<滤波宽度为6.4us
	_25_6_US,///<滤波宽度为25.6us
	_102_4_US,///<滤波宽度为102.4us
	_409_6_US,///<滤波宽度为409.6us
	_1638_4US,///<滤波宽度为1638.4us
};

/**
* @brief 通道使能状态独热码
*/
public enum CHANNEL_ENABLE_MODE {
	CH1 = (1 << 0),///<通道1使能
	CH2 = (1 << 1),///<通道2使能
	CH3 = (1 << 2),///<通道3使能
	CH4 = (1 << 3),///<通道4使能
};
/**
* @brief 串口波特率.
*/
public enum BAUDRATE {
	_19200,
	_38400,
	_57600,
	_115200,
	_9600,
};

/**
* @brief 多光点计算数据源
*/
public enum MULTI_MATH_DATA_SRC {
	DISTANCE1,///<距离1
	DISTANCE2,///<距离2
	THICKNESS,///<厚度
};
/**
* @brief 多光点计算方式
*/
public enum MULTI_MATH_CALC_MODE {
	MEAN,///<平均值
	MEDIAN,///<中值
	MAX,///<最大值
	MIN,///<最小值
};/**
* @brief 测量模式
*/
public enum MEASUREMODE {
	CONFOCAL_DISTANCE,///<光谱测距模式
	INTERF_THICKNESS_SINGLE_LAYER,	///<干涉单层测厚模式
	INTERF_THICKNESS_MULTI_LAYER,///<干涉多层测厚模式
	CONFOCAL_THICKNESS_SINGLE_LAYER,///<光谱单层测厚模式
	CONFOCAL_THICKNESS_MULTI_LAYER,///<光谱多层测厚模式
};
/**
* @brief 滑动平均窗口宽度
*/
public enum FFT_FRAME_MOVE_AVG_WIDTH {
	_1,///<1
	_2,///<2
	_4,///<4
	_8,///<8
	_16,///<16
};
    /**
* @brief 光源触发控制选项
*/
    public enum TRIG_LIGHT_SRC_MODE
    {
        TRIGGER_UP,///<触发时点亮
        ALWAYS_UP,///<常亮
    };
    /**
* @brief 图像滤波窗口宽度
*/
    public enum IMAGE_FILTER_WIDTH
    {
        _1,
        _2,
        _3,
        _5,
        _7,
        _11,
    };

    /**
* @brief 模拟无效值输出模式
*/
    public enum ANALOG_INVALID_MODE
    {
        ENDPOINT,//端点模式
        MARGIN,//余量模式
    };

    /**
* @brief 模拟无效输出值
*/
    public enum ANALOG_INVALID_OUTPUT
    {
        MIN,//无效值时输出最小值
        MAX,//无效值时输出最大值
    };

    /**
    * @brief 中值滤波窗口宽度
*/
    public enum MEDIAN_FILTER_WIDTH
    {
        _1,///<无滤波
        _3,///<窗口宽度为3
        _5,///<窗口宽度为5
        _9,///<窗口宽度为9
        _15,///<窗口宽度为15
        _31,///<窗口宽度为31
        _63,///<窗口宽度为63
    };
    /**
* @brief SYNC当前状态
*/
    public enum SYNC_PORT_STATE
    {
        CONDUCT,///<导通
        BREAK,///<断开
    };

	/**
* @brief 数据输出时间戳的类型
*/
	public enum TIME_TYPE
	{
		HARDWARE,///<硬件时间
		REAL,///<实际时间
	};

	/**
* @brief 日志等级
*/
	public enum LOG_LEVEL
	{
		DEBUG,
		INFO,
		WARN,
		ERR,
		CRITICAL,
		OFF,
	};

	/**
* @brief CMOS增益挡位
*/
	public enum CMOS_GAIN
	{
		_1,///<档位1
		_2,///<档位2
		_3,///<档位3
		_4,///<档位4
	};

	/**
* @brief 网络地址结构体，大端字节序
*/
	public struct IPAddr {
	public uint8_t c1;///<IP地址第1位
	public uint8_t c2;///<IP地址第2位
	public uint8_t c3;///<IP地址第3位
	public uint8_t c4;///<IP地址第4位
};

/**
* @brief 控制器以太网通信参数结构体
*/
public struct EthernetConfiguration {
	public IPAddr ip;///<IP地址
	public IPAddr subnet_mask;///<子网掩码
	public IPAddr gateway;///<网关
	public uint8_t host_addr_last_char;///<本机地址最后一位
	public uint16_t host_port;///<与不同控制器通信采用不同端口，防止相互占用
};
	
/**
* @brief 峰检测参数
*/
public struct PeakDetection {
	public int threshold;///<峰阈值
	public int sharpness;///<峰锐度
	public int minimum_spacing;///<峰间隔
};



/**
* @brief 编码器配置参数
*/
public struct EncoderSetting {
	public ENCODER_FILTER_WIDTH filter_width;		///<编码器滤波窗宽
	public ENCODER_INPUT_MODE input_mode;///<输入模式
	public ENCODER_OUTPUT_MODE output_mode;///<编码器输出格式
	public bool z_phase;		///<编码器Z相输出
};

/**
* @brief 外部触发参数配置
*/
public struct TriggerSetting {
	public ENCODER_CHANNEL channel;		///<触发源
	public TRIG_MODE mode;	///<触发模式
	public TRIG_DIRECTION direction;	///<触发方向
	public TRIG_TRACK_MODE track_mode;	///<追踪模式
	public int downsample_factor;///<采样间隔点数
};

    public struct AnalogInvalidSetting
    {
        public ANALOG_INVALID_MODE mode;
        public ANALOG_INVALID_OUTPUT output;
    };

    /**
* @brief MATH修正系数
*/
    public struct MathCorrection
    {
        public double slope;//斜率
        public double offset;//偏置
    };


    /**
    * @brief 通道模拟量配置结构体
    * @details 仅支持固件版本2.2.0以前控制器模拟配置，在固件版本2.2.0以后，采用新型号模拟输出芯片，该结构体不再使用
*/
    public struct ChannelAnalogOutput {
	public ANALOG_SOURCE source;		///<模拟输出源
	public ANALOG_INPUT_CHANNEL input_channel;		///<输出通道
	public STATE output_en;	///<输出使能
	public ANALOG_OUTPUT_MODE output_mode;///<输出模式
	public double distance_start;///<距离起点
	public double distance_end;///<距离终点
	public double cv_start;	///<电压（流）起点
	public double cv_end;		///<电压（流）终点
};


/**
* @brief 通道模拟量配置
* @details 支持固件版本2.2.0以后控制器模拟配置，2.2.0以前版本见TS_ChannelAnalogOutput
*/
public struct AnalogOutputSetting {
	public STATE output_en;///<模拟输出使能，使能为关时无模拟量输出，使能为开时才有模拟量输出
	public ANALOG_INPUT_CHANNEL input_channel;///<数据输入通道
	public ANALOG_SOURCE source;///<映射为模拟量的数据来源
	public ANALOG_OUT_RANGE range;///<模拟量输出范围
	public double distance_start;///<映射距离起点
	public double distance_end;///<映射距离终点
};


/**
* @brief 通道数字量配置结构体
*/
public struct ChannelDigitalOutput {
	public STATE output_en;	///<输出使能
	public DIGITAL_INPUT_CHANNEL input_channel;		///<输入通道
	public DIGITAL_INPUT_SRC input_source;///<输入数据类型
	public DIGITAL_OUTPUT_COND output_cond;///<输出条件
	public DIGITAL_OUTPUT_LEVEL output_level;///<输出状态 
};

    /**
* @brief 峰值结构体，每个探头对应一个TS_Peak值，最多两个峰
*/
    public struct PeakIndexWindow
    {
        public int index;///<编号
        public int window_start;///<起始像素
        public int window_end;///<终止像素
    };

/**
* @brief 峰值结构体，每个探头对应一个TS_Peak值，最多两个峰
*/
public struct PeakSelection {
	public PEAK_SELECTION_MODE mode;///<峰选择模式
	public int peak1_idx;///<峰1编号
	public int peak1_window_start;///<峰1起始像素
	public int peak1_window_end;///<峰1终止像素
	public int peak2_idx;///<峰2编号
	public int peak2_window_start;///<峰2起始像素
	public int peak2_window_end;///<峰2终止像素
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.EXTRA_PEAK_NUM, ArraySubType = UnmanagedType.Struct)]
    public PeakIndexWindow[]  extra_peaks;
    };

/**
* @brief 峰选择窗口，窗口用于选择特定区间的峰，窗口起点应小于窗口终点
*/
public struct PeakSelectionWindow {
	public int start;///<窗口起点
	public int end;///<窗口终点
};


/**
* @brief 曝光时间控制结构体
*/
public struct ExposureConfig {
	public STATE auto_control;///<曝光自动控制
	public uint16_t exposure_time;///<手动曝光时间,单位为us,限制范围为5.4-5000us,自动曝光时，该参数无效
};
	
/**
* @brief 自动曝光时间上下限，当曝光方式为自动曝光时，曝光时间将限制在下限与上限之间，防止过长或过短
*/
public struct AutoExposureTimeSetting {
	public uint16_t lower;///<曝光自动控制时间下限
	public uint16_t upper;///<曝光自动控制时间上限
};


/**
* @brief 编码器同步参数
*/
public struct SyncSetting {
	public STATE state;		///<SYNC使能
	public SYNC_INPUT_MODE input_mode;	///<SYNC输入模式
	public SYNC_VALID_LEVEL valid_level;///<SYNC有效电平 
	public uint16_t sample_per_trigger;	///<单次脉冲采样点数
	public SYNC_FILTER_WIDTH filter_width;///<滤波宽度
};

/**
* @brief 外部触发参数
*/
public struct ExternalTrigger {
	public TRIG_METHOD trig_method;		///<触发方式
	public SyncSetting sync_setting;	///<同步触发配置
};


/**
* @brief 双通道探头测量MATH数据计算方法配置，固定选择探头1、探头2距离数据进行运算
*/
public struct ChannelSetting {
	public MATH_DATA_SRC src;///<数据源
	public MATHSIGN sign;///<通道符号
};



/**
* @brief 多光点计算参数结构体
*/
public struct MultiMathSetting {
	public MULTI_MATH_DATA_SRC src;///<数据源
	public MULTI_MATH_CALC_MODE mode;///<计算方式
};



/**
* @brief 多通道探头测量MATH数据计算方法配置，不同探头之前可两两配对
*/
public struct MathSetting {
	public int sensor;///<探头通道
	public MATH_DATA_SRC src;///<数据源
	public MATHSIGN sign;///<通道符号
};
/**
* @brief 暗校准本底值
*/
public struct DarkRefCurve
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.CMOS_PIXEL_NUM, ArraySubType = UnmanagedType.Struct)]
	public short[] data;///<数据
};

/**
* @brief 暗校准系数
*/
public struct DarkCoeffCurve
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.CMOS_PIXEL_NUM, ArraySubType = UnmanagedType.Struct)]
	public uint16_t[] data;///<数据
};

/**
* @brief 折射率系数
*/
public struct RefractiveCoeff {
	public double c486;///<486nm波长下折射率
	public double c587;///<587nm波长下折射率
	public double c656;///<656nm波长下折射率
};

/**
* @brief 折射率校准表
*/
public struct RefractiveTable
{
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = ConstDef.OBJECT_NAME_LEN)]
	public string object_name;		///<物体名称
	public RefractiveCoeff refractive_data;///<折射率表
};

/**
* @brief 暗校准表
*/
public struct DarkReferenceTable {
	public DarkRefCurve refr;///<本底值
	public DarkCoeffCurve coeff;		///<系数
};

/**
* @brief 设备序列号
*/
public struct SerialNumber {

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = ConstDef.MAX_SERIAL_NUMBER_LEN)]
	public string serial;///<序列号字符数据，长度固定
};

/**
* @brief 通道使能状态
*/
public struct ChannelEnable {
	public int channelCnt;		///<最大通道数，表示状态有效位，例如，当channelCnt为2时，说明channelState仅低两位有效
	public short channelState;	///<通道状态，通道对应bit位为1时，说明通道被使能，可以进行数据采集，例如，当(channelState&TS_CHANNEL_ENABLE_MODE_CH1)不为0时，说明通道被使能，如果为0，则说明该通道未使能
};


/**
* @brief 数据类型与通道信息
*/
public struct DataCfg {
	public int channel;///<数据通道，包含控制器通道及探头通道
	public int type;///<数据类型，若数据通道为控制器，则与TS_CONTROLLER_OUTPUT_DATA对应，若数据通道为探头通道，则与TS_SENSOR_OUTPUT_DATA对应
};


/**
* @brief 数据节点
*/
public struct DataNode {
	public DataCfg cfg;///<用于记录当前数据对应的通道及类型
	public double data;///<测量数据
};

/**
* @brief 固件版本
*/
public struct VersionDetail {
	public uint8_t reserve;///<保留位
	public uint8_t major;///<主版本
	public uint8_t minor;///<小版本
	public uint8_t patch;///<修订版本
};

/**
* @brief 探头
*/
public struct MeasureRangeNode {
	public double start;///<距离起点
	public double end;///<距离终点
	public double rev;///<保留位，暂时无具体含义
};

/*
* @brief 探头数据
*/
public struct SensorData
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.MAX_OUTPUT_PEAK_NUM, ArraySubType = UnmanagedType.Struct)]
	public double[] distance;///<距离
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.THICKNESS_LAYER_NUMBER, ArraySubType = UnmanagedType.Struct)]
	public double[] thickness;///<厚度	
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.MAX_OUTPUT_PEAK_NUM, ArraySubType = UnmanagedType.Struct)]
	public double[] peakHeight;///<峰高
	public double intensity;///<被测物光强
	public double exptime;///<曝光时间
};

/*
* @brief 控制器通道数据
*/
public struct ControllerData
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.MAX_ENCODER_CHANNEL, ArraySubType = UnmanagedType.Struct)]
	public double[] encoder;///<编码器测量数据
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.MAX_MATH_DATA_TYPES, ArraySubType = UnmanagedType.Struct)]
	public double[] math;///<双探头运算测量值
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.MAX_MULTI_MATH_DATA_TYPES, ArraySubType = UnmanagedType.Struct)]
	public double[] multiMath;///<四光点运算测量值
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = ConstDef.MAX_ANY_MATH_DATA_TYPES, ArraySubType = UnmanagedType.Struct)]
	public double[] anyMath;///<任意测量值运算
};

/**
* @brief 数字输出手动控制配置参数
*/
public struct DigitalManualSetting
{
	public STATE manualSwitch;///<输出固定值开关
	public char level;///<手动控制输出的电平
};


	public class TSCMCAPICS {
private InstanceHandle m_impl;

	private const string TscmcapicDll = "TSCMCAPIC.dll";
	private const string TscmcapinetDll = "TSCMCAPINET.dll";
	private const string NativeDirectoryName = "TronSight2";

	static TSCMCAPICS()
	{
		NativeLibrary.SetDllImportResolver(typeof(TSCMCAPICS).Assembly, ResolveNativeLibrary);
	}

	private static IntPtr ResolveNativeLibrary(
		string libraryName,
		System.Reflection.Assembly assembly,
		DllImportSearchPath? searchPath)
	{
		if (!string.Equals(libraryName, TscmcapicDll, StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(libraryName, TscmcapinetDll, StringComparison.OrdinalIgnoreCase))
		{
			return IntPtr.Zero;
		}

		var moduleDirectory = System.IO.Path.GetDirectoryName(assembly.Location);
		if (string.IsNullOrEmpty(moduleDirectory))
		{
			moduleDirectory = AppContext.BaseDirectory;
		}

		var nativePath = System.IO.Path.Combine(moduleDirectory, "Native", NativeDirectoryName, libraryName);
		return NativeLibrary.Load(
			nativePath,
			assembly,
			DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.SafeDirectories);
	}

//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
[System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.StdCall)]
public delegate void DllDataNodecallBack([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] DataNode[] node, int bufSize, int dataGroupCnt, int dataGroupSize);
[System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention.StdCall)]
public delegate void DllDatacallBack([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] double[] node, int bufSize, int dataGroupCnt, int dataGroupSize);
TSCMCAPICS(){}
~TSCMCAPICS(){}
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_CreateInstance", CallingConvention = CallingConvention.StdCall)]
public static extern InstanceHandle CreateInstance();
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ReleaseInstance", CallingConvention = CallingConvention.StdCall)]
public static extern bool ReleaseInstance(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetFirmWareVersion", CallingConvention = CallingConvention.StdCall)]
public static extern VersionDetail GetFirmWareVersion(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_HeartBeatState", CallingConvention = CallingConvention.StdCall)]
public static extern bool HeartBeatState(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_isRunning", CallingConvention = CallingConvention.StdCall)]
public static extern bool isRunning(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_isConnected", CallingConvention = CallingConvention.StdCall)]
public static extern bool isConnected(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_isAcquireData", CallingConvention = CallingConvention.StdCall)]
public static extern bool isAcquireData(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_getAllWarning", CallingConvention = CallingConvention.StdCall)]
public static extern int getAllWarning(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_getWarning", CallingConvention = CallingConvention.StdCall)]
public static extern bool getWarning(InstanceHandle handle,WARNING_INPUT_CHANNEL channel,WARNING_SOURCE source);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetUSBPort", CallingConvention = CallingConvention.StdCall)]
public static extern void SetUSBPort(InstanceHandle handle,int PortCOM);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetUSBPort", CallingConvention = CallingConvention.StdCall)]
public static extern int GetUSBPort(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetUSBDeviceName", CallingConvention = CallingConvention.StdCall)]
public static extern void SetUSBDeviceName(InstanceHandle handle,string deviceName);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetUdpPort", CallingConvention = CallingConvention.StdCall)]
public static extern void SetUdpPort(InstanceHandle handle,int portNo);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetUdpPort", CallingConvention = CallingConvention.StdCall)]
public static extern int GetUdpPort(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetDestUdpEndPoint", CallingConvention = CallingConvention.StdCall)]
public static extern bool SetDestUdpEndPoint(InstanceHandle handle,string address,int port);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConnectionType", CallingConvention = CallingConvention.StdCall)]
public static extern CONNECTION_TYPE GetConnectionType(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConnectionType", CallingConvention = CallingConvention.StdCall)]
public static extern bool SetConnectionType(InstanceHandle handle,CONNECTION_TYPE ctype);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentControllerVersion", CallingConvention = CallingConvention.StdCall)]
public static extern CONTROLLER_VERSION GetCurrentControllerVersion(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_OpenConnectionPort", CallingConvention = CallingConvention.StdCall)]
public static extern bool OpenConnectionPort(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_CloseConnectionPort", CallingConvention = CallingConvention.StdCall)]
public static extern bool CloseConnectionPort(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_OpenConnectionUSBPort", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE OpenConnectionUSBPort(InstanceHandle handle,int portNo);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_OpenConnectionUSBDeviceName", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE OpenConnectionUSBDeviceName(InstanceHandle handle,string deviceName);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_OpenConnectionEthernet", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE OpenConnectionEthernet(InstanceHandle handle,IPAddr deviceAddr,int localPort);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SaveControllerConfig", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SaveControllerConfig(InstanceHandle handle,string filename);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ReadControllerConfig", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE ReadControllerConfig(InstanceHandle handle,string filename);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_BindLocalAddress", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE BindLocalAddress(InstanceHandle handle,IPAddr localAddr);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetFFTDataFrame", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetFFTDataFrame(InstanceHandle handle,int controller,int sensor,ref double data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetInterferenceThickCorrectionFactor", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetInterferenceThickCorrectionFactor(InstanceHandle handle,int controller,int sensor,double factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetInterferenceThickCorrectionFactor", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetInterferenceThickCorrectionFactor(InstanceHandle handle,int controller,int sensor,ref double factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetFFTFrameMoveAverage", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetFFTFrameMoveAverage(InstanceHandle handle,int controller,FFT_FRAME_MOVE_AVG_WIDTH width);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetFFTFrameMoveAverage", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetFFTFrameMoveAverage(InstanceHandle handle,int controller,ref FFT_FRAME_MOVE_AVG_WIDTH width);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSensorSerialNumber", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetSensorSerialNumber(InstanceHandle handle,int controller,int sensor,ref SerialNumber serial_number);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_DownloadDarkReference", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE DownloadDarkReference(InstanceHandle handle,int controller,int sensor,ref DarkReferenceTable dark_ref_table);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetRefractiveTableLabel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetRefractiveTableLabel(InstanceHandle handle,int controller,ref int labels,ref int n_labels,int max_length);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_DeleteRefractiveTableLabel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE DeleteRefractiveTableLabel(InstanceHandle handle,int controller,ref int labels,int max_length);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_UploadRefractiveTable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE UploadRefractiveTable(InstanceHandle handle,int controller,int label,RefractiveTable refractive_table);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_DownloadRefractiveTable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE DownloadRefractiveTable(InstanceHandle handle,int controller,int label,ref RefractiveTable refractive_table);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_DarkCalibration", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE DarkCalibration(InstanceHandle handle,int controller,int sensor,ref DarkReferenceTable dark_ref_table);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetCurrentCalibrationTableLabel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetCurrentCalibrationTableLabel(InstanceHandle handle,int controller,int sensor,int label);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentCalibrationTableLabel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetCurrentCalibrationTableLabel(InstanceHandle handle,int controller,int sensor,ref int label);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetCurrentRefractiveTableLabel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetCurrentRefractiveTableLabel(InstanceHandle handle,int controller,int sensor,int label);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentRefractiveTableLabel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetCurrentRefractiveTableLabel(InstanceHandle handle,int controller,int sensor,ref int label);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetMeasureRangeThreshold", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetMeasureRangeThreshold(InstanceHandle handle,int controller,int sensor,ref MeasureRangeNode data);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigRangeEdgePixel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigRangeEdgePixel(InstanceHandle handle,int controller,int sensor,ref int range_start_pixel,ref int range_end_pixel);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentSensorName", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetCurrentSensorName(InstanceHandle handle,int controller,int sensor,ref SerialNumber name);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetContorllerChannelEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetContorllerChannelEnable(InstanceHandle handle,int controller,ChannelEnable channelEnable);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetContorllerChannelEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetContorllerChannelEnable(InstanceHandle handle,int controller,ref ChannelEnable channelEnable);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetControllerChannelEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetControllerChannelEnable(InstanceHandle handle, int controller, ChannelEnable channelEnable);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetControllerChannelEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetControllerChannelEnable(InstanceHandle handle, int controller, ChannelEnable channelEnable);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetWarningHoldPoints", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetWarningHoldPoints(InstanceHandle handle,int controller,int points);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetWarningHoldPoints", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetWarningHoldPoints(InstanceHandle handle,int controller,ref int points);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetThickCorrectionFactor", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetThickCorrectionFactor(InstanceHandle handle,int controller,int sensor,double factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetThickCorrectionFactor", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetThickCorrectionFactor(InstanceHandle handle,int controller,int sensor,ref double factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigOutputSignals", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigOutputSignals(InstanceHandle handle,int controller,int sensor,CONNECTION_TYPE connection_port,ref int data_index,ref int ntypes,int max_length);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigOutputSignals", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigOutputSignals(InstanceHandle handle,int controller,int sensor,CONNECTION_TYPE connection_port,ref int data_index,int max_length);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_RingBufferDataSize", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE RingBufferDataSize(InstanceHandle handle,ref int size);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ClearRingBuffer", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE ClearRingBuffer(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_TransferDataNode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE TransferDataNode(InstanceHandle handle,ref DataNode data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_TransferAllDataNode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE TransferAllDataNode(InstanceHandle handle,ref DataNode data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetLatestDataNode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetLatestDataNode(InstanceHandle handle,ref DataNode data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_TransferData", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE TransferData(InstanceHandle handle,ref double data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_TransferAllData", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE TransferAllData(InstanceHandle handle,ref double data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetLatestData", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetLatestData(InstanceHandle handle,ref double data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ResizeRingBuffer", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE ResizeRingBuffer(InstanceHandle handle,int size);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConnectionOn", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConnectionOn(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConnectionOff", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConnectionOff(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConnectionDeviceInfo", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConnectionDeviceInfo(InstanceHandle handle,ref int controller_idx);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigSensorModel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigSensorModel(InstanceHandle handle,int controller,int sensor,ref SENSOR_MODEL sensor_model);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigEthernet", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigEthernet(InstanceHandle handle,int controller,EthernetConfiguration ethernet_configuration);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigEthernet", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigEthernet(InstanceHandle handle,int controller,ref EthernetConfiguration ethernet_configuration);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigBaudRate", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigBaudRate(InstanceHandle handle,int controller,BAUDRATE baudrate);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigBaudRate", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigBaudRate(InstanceHandle handle,int controller,ref BAUDRATE baudrate);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigLightSource", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigLightSource(InstanceHandle handle,int controller,int sensor,STATE led_switch);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigLightSource", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigLightSource(InstanceHandle handle,int controller,int sensor,ref STATE led_switch);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigLightIntensity", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigLightIntensity(InstanceHandle handle,int controller,int sensor,double intensity);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigLightIntensity", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigLightIntensity(InstanceHandle handle,int controller,int sensor,ref double intensity);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigMeasurementMode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigMeasurementMode(InstanceHandle handle,int controller,MEASUREMODE measure_mode);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigMeasurementMode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigMeasurementMode(InstanceHandle handle,int controller,ref MEASUREMODE measure_mode);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigUpperlimit", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigUpperlimit(InstanceHandle handle,int controller,DIGITAL_INPUT_CHANNEL channel,DIGITAL_INPUT_SRC src,double upper_limit,double hysteresis);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigLowerlimit", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigLowerlimit(InstanceHandle handle,int controller,DIGITAL_INPUT_CHANNEL channel,DIGITAL_INPUT_SRC src,double lower_limit,double hysteresis);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigUpperlimit", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigUpperlimit(InstanceHandle handle,int controller,DIGITAL_INPUT_CHANNEL channel,DIGITAL_INPUT_SRC src,ref double upper_limit,ref double hysteresis);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigLowerlimit", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigLowerlimit(InstanceHandle handle,int controller,DIGITAL_INPUT_CHANNEL channel,DIGITAL_INPUT_SRC src,ref double lower_limit,ref double hysteresis);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ResetTriggerCounter", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE ResetTriggerCounter(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ResetTimeStamp", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE ResetTimeStamp(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigPeakDetection", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigPeakDetection(InstanceHandle handle,int controller,int sensor,PeakDetection peak_detection);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigPeakDetection", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigPeakDetection(InstanceHandle handle,int controller,int sensor,ref PeakDetection peak_detection);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigPeakSelection", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigPeakSelection(InstanceHandle handle,int controller,int sensor,PeakSelection peak_selection);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigPeakSelection", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigPeakSelection(InstanceHandle handle,int controller,int sensor,ref PeakSelection peak_selection);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigMapping", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigMapping(InstanceHandle handle,int controller,int sensor,double mapping_factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigMapping", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigMapping(InstanceHandle handle,int controller,int sensor,ref double mapping_factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigZeroSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigZeroSetting(InstanceHandle handle,int controller,int sensor,bool zero_setting);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigZeroSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigZeroSetting(InstanceHandle handle,int controller,int sensor,ref bool zero_setting);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ResetZero", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE ResetZero(InstanceHandle handle,int controller,int sensor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigZeroOffset", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigZeroOffset(InstanceHandle handle,int controller,int sensor,double zero_offset);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigZeroOffset", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigZeroOffset(InstanceHandle handle,int controller,int sensor,ref double zero_offset);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigZeroPointIndicateScale", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigZeroPointIndicateScale(InstanceHandle handle,int controller,int sensor,uint8_t indicate_scale);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigZeroPointIndicateScale", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigZeroPointIndicateScale(InstanceHandle handle,int controller,int sensor,ref uint8_t indicate_scale);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigMoveAvarage", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigMoveAvarage(InstanceHandle handle,int controller,FILTER_WINDOW_WIDTH window_width);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigMoveAvarage", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigMoveAvarage(InstanceHandle handle,int controller,ref FILTER_WINDOW_WIDTH window_width);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigMath", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigMath(InstanceHandle handle,int controller,ChannelSetting chst1,ChannelSetting chst2);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigMath", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigMath(InstanceHandle handle,int controller,ref ChannelSetting chst1,ref ChannelSetting chst2);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigMathSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigMathSetting(InstanceHandle handle,int controller,MATH_CHANNEL ch,MathSetting mst1,MathSetting mst2);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigMathSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigMathSetting(InstanceHandle handle,int controller,MATH_CHANNEL ch,ref MathSetting mst1,ref MathSetting mst2);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigAnalogOutput", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigAnalogOutput(InstanceHandle handle,int controller,ANALOG_CHANNEL analog_channel,ChannelAnalogOutput analog_output);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigAnalogOutput", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigAnalogOutput(InstanceHandle handle,int controller,ANALOG_CHANNEL analog_channel,ref ChannelAnalogOutput analog_output);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetAnalogOutputSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetAnalogOutputSetting(InstanceHandle handle,int controller,ANALOG_CHANNEL analog_channel,AnalogOutputSetting aos);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetAnalogOutputSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetAnalogOutputSetting(InstanceHandle handle,int controller,ANALOG_CHANNEL analog_channel,ref AnalogOutputSetting aos);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigSamplingInterval", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigSamplingInterval(InstanceHandle handle,int controller,SAMPLING_INTERVAL sampling_interval);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigSamplingInterval", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigSamplingInterval(InstanceHandle handle,int controller,ref SAMPLING_INTERVAL sampling_interval);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigControllerSettings", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigControllerSettings(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigControllerSettings", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigControllerSettings(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigDigitalOutput", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigDigitalOutput(InstanceHandle handle,int controller,DIGITAL_CHANNEL digital_channel,ChannelDigitalOutput digital_output);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigDigitalOutput", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigDigitalOutput(InstanceHandle handle,int controller,DIGITAL_CHANNEL digital_channel,ref ChannelDigitalOutput digital_output);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigExposure", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigExposure(InstanceHandle handle,int controller,int sensor,ExposureConfig exposure_config);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigExposure", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigExposure(InstanceHandle handle,int controller,int sensor,ref ExposureConfig exposure_config);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigAutoExposureTimeSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigAutoExposureTimeSetting(InstanceHandle handle,int controller,int sensor,AutoExposureTimeSetting aets);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigAutoExposureTimeSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigAutoExposureTimeSetting(InstanceHandle handle,int controller,int sensor,ref AutoExposureTimeSetting aets);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigAutoExposureTarget", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigAutoExposureTarget(InstanceHandle handle,int controller,int sensor,uint16_t target);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigAutoExposureTarget", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigAutoExposureTarget(InstanceHandle handle,int controller,int sensor,ref uint16_t target);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigThinFilmMeasureMode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigThinFilmMeasureMode(InstanceHandle handle,int controller,int sensor,STATE state);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigThinFilmMeasureMode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigThinFilmMeasureMode(InstanceHandle handle,int controller,int sensor,ref STATE state);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigFrameDataSource", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigFrameDataSource(InstanceHandle handle,int controller,FRAME_DATA_SRC data_src);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigFrameDataSource", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigFrameDataSource(InstanceHandle handle,int controller,ref FRAME_DATA_SRC data_src);

[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigExternalTrigger", CallingConvention = CallingConvention.StdCall)]
 public static extern ERRCODE SetConfigExternalTrigger(InstanceHandle handle, int controller, ExternalTrigger external_trigger);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigExternalTrigger", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigExternalTrigger(InstanceHandle handle, int controller,ref ExternalTrigger external_trigger);

[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigEncoderResolution", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigEncoderResolution(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,double resolution);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigEncoderResolution", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigEncoderResolution(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,ref double resolution);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigEncoderSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigEncoderSetting(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,EncoderSetting encoder_setting);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigEncoderSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigEncoderSetting(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,ref EncoderSetting encoder_setting);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigTriggerSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigTriggerSetting(InstanceHandle handle,int controller,TriggerSetting trigger_setting);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigTriggerSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigTriggerSetting(InstanceHandle handle,int controller,ref TriggerSetting trigger_setting);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigEncoderCounterEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigEncoderCounterEnable(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,STATE counter_enable);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigEncoderCounterEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigEncoderCounterEnable(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,ref STATE counter_enable);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigEncoderPosition", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigEncoderPosition(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,double position);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigEncoderPosition", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigEncoderPosition(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,ref double position);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigZPhasePosition", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigZPhasePosition(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,ref double position);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigZPhasePosition", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigZPhasePosition(InstanceHandle handle,int controller,ENCODER_CHANNEL encoder_channel,double position);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSingleDataNode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetSingleDataNode(InstanceHandle handle,int controller,ref DataNode data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSingleData", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetSingleData(InstanceHandle handle,int controller,ref double data,ref int nread,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetDataOutputOn", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetDataOutputOn(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetDataOutputOff", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetDataOutputOff(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetDataFrameSingle", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetDataFrameSingle(InstanceHandle handle,int controller,int sensor,ref double data,ref int pixelSize,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetExposureSatureWarning", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetExposureSatureWarning(InstanceHandle handle,int controller,int sensor,ref int warning);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetExposurePeakHeight", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetExposurePeakHeight(InstanceHandle handle,int controller,int sensor,ref int peak1_height,ref int peak2_height);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetExposurePeakPosition", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetExposurePeakPosition(InstanceHandle handle,int controller,int sensor,ref int peakPosBuf,ref int npixel,int maxLength);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConnectionModbusSlaveAddr", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConnectionModbusSlaveAddr(InstanceHandle handle,int controller,uint8_t addr);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConnectionModbusSlaveAddr", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConnectionModbusSlaveAddr(InstanceHandle handle,int controller,ref uint8_t addr);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_RestoreFactorySetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE RestoreFactorySetting(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetControllerTemperature", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetControllerTemperature(InstanceHandle handle,int controller,ref double tmp);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetLEDTemperature", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetLEDTemperature(InstanceHandle handle,int controller,int sensor,ref double tmp);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetFFTScaleFromRefractiveIndex", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetFFTScaleFromRefractiveIndex(InstanceHandle handle,int controller,int sensor,ref double scale);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetDataSubSamplingFactor", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetDataSubSamplingFactor(InstanceHandle handle,int controller,uint16_t factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetDataSubSamplingFactor", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetDataSubSamplingFactor(InstanceHandle handle,int controller,ref uint16_t factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigRS485ResistanceEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigRS485ResistanceEnable(InstanceHandle handle,int controller,STATE state);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigRS485ResistanceEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigRS485ResistanceEnable(InstanceHandle handle,int controller,ref STATE state);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_MaxSensorChannels", CallingConvention = CallingConvention.StdCall)]
public static extern int MaxSensorChannels(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_MaxMathChannels", CallingConvention = CallingConvention.StdCall)]
public static extern int MaxMathChannels(InstanceHandle handle);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetThicknessCorrectionByLayer", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetThicknessCorrectionByLayer(InstanceHandle handle,int controller,int sensor,int layer,ref double factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetThicknessCorrectionByLayer", CallingConvention = CallingConvention.StdCall)]

//原备份
//[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetThicknessRefractionTagByLayer", CallingConvention = CallingConvention.StdCall)]
//public static extern ERRCODE GetThicknessRefractionTagByLayer(InstanceHandle handle, int controller, int sensor, int layer, ref int tag);
//[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetThicknessRefractionTagByLayer", CallingConvention = CallingConvention.StdCall)]
//public static extern ERRCODE SetThicknessRefractionTagByLayer(InstanceHandle handle, int controller, int sensor, int layer, int tag);
//[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SaveRefractToFlash", CallingConvention = CallingConvention.StdCall)]
//public static extern ERRCODE SaveRefractToFlash(InstanceHandle handle, int controller);
//[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetInterfThicknessCorrectionByLayer", CallingConvention = CallingConvention.StdCall)]
//public static extern ERRCODE GetInterfThicknessCorrectionByLayer(InstanceHandle handle, int controller, int sensor, int layer, ref double factor);
//[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetInterfThicknessCorrectionByLayer", CallingConvention = CallingConvention.StdCall)]

public static extern ERRCODE SetThicknessCorrectionByLayer(InstanceHandle handle,int controller,int sensor,int layer,double factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetRefractiveTableLabelByLayer", CallingConvention = CallingConvention.StdCall)]//修改折射表（sdk和api名字不一样）：！！！开始
public static extern ERRCODE GetThicknessRefractionTagByLayer(InstanceHandle handle, int controller, int sensor, int layer, ref int tag);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetRefractiveTableLabelByLayer", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetThicknessRefractionTagByLayer(InstanceHandle handle, int controller, int sensor, int layer, int tag);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SaveRefractiveTableToFlash", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SaveRefractToFlash(InstanceHandle handle, int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetInterferenceThicknessCorrectionByLayer", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetInterfThicknessCorrectionByLayer(InstanceHandle handle, int controller, int sensor, int layer, ref double factor);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetInterferenceThicknessCorrectionByLayer", CallingConvention = CallingConvention.StdCall)]//!!!!!终止
public static extern ERRCODE SetInterfThicknessCorrectionByLayer(InstanceHandle handle,int controller,int sensor,int layer,double factor);

[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetDigitalLimitByChannel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetDigitalLimitByChannel(InstanceHandle handle,int controller,DIGITAL_CHANNEL channel,double up,double upHys,double down,double downHys);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetDigitalLimitByChannel", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetDigitalLimitByChannel(InstanceHandle handle,int controller,DIGITAL_CHANNEL channel,ref double up,ref double upHys,ref double down,ref double downHys);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetPeakSelectionMode", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetPeakSelectionMode(InstanceHandle handle, int controller, int sensor, PEAK_SELECTION_MODE mode);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetPeakSelectionMode", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetPeakSelectionMode(InstanceHandle handle, int controller, int sensor, ref PEAK_SELECTION_MODE mode);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetPeakSelectionIndex", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetPeakSelectionIndex(InstanceHandle handle, int controller, int sensor, int[] peakIndexes, int maxPeakNum);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetPeakSelectionIndex", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetPeakSelectionIndex(InstanceHandle handle, int controller, int sensor, ref int[] peakIndexes, int maxPeakNum, ref int peakNum);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetPeakSelectionWindow", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetPeakSelectionWindow(InstanceHandle handle, int controller, int sensor, PeakSelectionWindow[] peakWindows, int maxPeakNum);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetPeakSelectionWindow", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetPeakSelectionWindow(InstanceHandle handle, int controller, int sensor, PeakSelectionWindow[] peakWindows, int maxPeakNum, ref int peakNum);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetTriggerLightSrcMode", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetTriggerLightSrcMode(InstanceHandle handle, int controller, TRIG_LIGHT_SRC_MODE mode);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetTriggerLightSrcMode", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetTriggerLightSrcMode(InstanceHandle handle, int controller, ref TRIG_LIGHT_SRC_MODE mode);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetSensorThicknessCalSrc", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetSensorThicknessCalSrc(InstanceHandle handle, int controller, int sensor, int layer, MATH_DATA_SRC minuend, MATH_DATA_SRC subtractor);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSensorThicknessCalSrc", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetSensorThicknessCalSrc(InstanceHandle handle, int controller, int sensor, int layer, ref MATH_DATA_SRC minuend,ref MATH_DATA_SRC subtractor);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigRangeEdgePixel", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetConfigRangeEdgePixel(InstanceHandle handle, int controller, int sensor, int range_start_pixel, int range_end_pixel);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetSensorTempCompFactor", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetSensorTempCompFactor(InstanceHandle handle, int controller, int sensor, double factor);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSensorTempCompFactor", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetSensorTempCompFactor(InstanceHandle handle, int controller, int sensor, ref double factor);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetSensorCompBasicTemp", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetSensorCompBasicTemp(InstanceHandle handle, int controller, int sensor, double temperature);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSensorCompBasicTemp", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetSensorCompBasicTemp(InstanceHandle handle, int controller, int sensor, ref double temperature);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetTriggerLightSrcMode", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetSensorThicknessCalSrc(InstanceHandle handle, int controller, int layer, MATH_DATA_SRC minuend, MATH_DATA_SRC subtractor);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSensorThicknessCalSrc", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetSensorThicknessCalSrc(InstanceHandle handle, int controller, int layer, ref MATH_DATA_SRC minuend, ref MATH_DATA_SRC subtractor);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetSensorImageFilterWidth", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetSensorImageFilterWidth(InstanceHandle handle, int controller, int sensor, STATE isAuto, IMAGE_FILTER_WIDTH width);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSensorImageFilterWidth", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetSensorImageFilterWidth(InstanceHandle handle, int controller, int sensor, ref STATE isAuto, ref IMAGE_FILTER_WIDTH width);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentImageFilterWidthInUse", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetCurrentImageFilterWidthInUse(InstanceHandle handle, int controller, int sensor, ref IMAGE_FILTER_WIDTH width);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetAnalogInvalidSetting", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetAnalogInvalidSetting(InstanceHandle handle, int controller, ANALOG_CHANNEL ch, AnalogInvalidSetting setting);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetAnalogInvalidSetting", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetAnalogInvalidSetting(InstanceHandle handle, int controller, ANALOG_CHANNEL ch, ref AnalogInvalidSetting setting);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetMedianFilterWidth", CallingConvention = CallingConvention.StdCall)]

        public static extern ERRCODE SetMedianFilterWidth(InstanceHandle handle, int controller, MEDIAN_FILTER_WIDTH width);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetMedianFilterWidth", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetMedianFilterWidth(InstanceHandle handle, int controller, ref MEDIAN_FILTER_WIDTH width);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetMathCorrection", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetMathCorrection(InstanceHandle handle, int controller, MATH_CHANNEL ch, MathCorrection corr);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetMathCorrection", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetMathCorrection(InstanceHandle handle, int controller, MATH_CHANNEL ch, ref MathCorrection corr);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetSyncResetZeroSetting", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE SetSyncResetZeroSetting(InstanceHandle handle, int controller, STATE zeroSwitchOn, int channelSelected);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSyncResetZeroSetting", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetSyncResetZeroSetting(InstanceHandle handle, int controller, ref STATE zeroSwitchOn, ref int channelSelected);
        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSyncPortState", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE GetSyncPortState(InstanceHandle handle, int controller, ref SYNC_PORT_STATE state);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_InstallDataNodeCallBackFunc", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE InstallDataNodeCallBackFunc(InstanceHandle handle, [MarshalAs(UnmanagedType.FunctionPtr)] DllDataNodecallBack func);

        [DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_RemoveDataNodeCallBackFunc", CallingConvention = CallingConvention.StdCall)]
        public static extern ERRCODE RemoveDataNodeCallBackFunc(InstanceHandle handle);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_InstallDataCallBackFunc", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE InstallDataCallBackFunc(InstanceHandle handle, [MarshalAs(UnmanagedType.FunctionPtr)] DllDatacallBack func);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_RemoveDataCallBackFunc", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE RemoveDataCallBackFunc(InstanceHandle handle);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigLightRestrain", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetConfigLightRestrain(InstanceHandle handle, int controller, STATE state);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigMultiMathSetting", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetConfigMultiMathSetting(InstanceHandle handle, int controller, MultiMathSetting multi_para);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigMultiMathSetting", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetConfigMultiMathSetting(InstanceHandle handle, int controller, ref MultiMathSetting multi_para);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetOutputTimeType", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetOutputTimeType(InstanceHandle handle, TIME_TYPE type);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetLogLevel", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetLogLevel(InstanceHandle handle, LOG_LEVEL lv);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetCallBackDataGroupCount", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetCallBackDataGroupCount(InstanceHandle handle, int groupCount);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentSensorDataValue", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetCurrentSensorDataValue(InstanceHandle handle, int controller, int sensor, ref double distance, ref double thickness, ref double peak_height, ref double intensity, ref double exptime);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentControllerDataValue", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetCurrentControllerDataValue(InstanceHandle handle, int controller, ref double encoder, ref double math, ref double multi_math, ref double any_math);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentSensorData", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetCurrentSensorData(InstanceHandle handle, int controller, int sensor, ref SensorData data);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentControllerData", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetCurrentControllerData(InstanceHandle handle, int controller, ref ControllerData data);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetOnePointMathCalibration", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetOnePointMathCalibration(InstanceHandle handle, int controller, MATH_CHANNEL channel, double reference);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetLaserPowerSwitchState", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetLaserPowerSwitchState(InstanceHandle handle, int controller, int sensor, STATE state);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetLaserPowerSwitchState", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetLaserPowerSwitchState(InstanceHandle handle, int controller, int sensor, ref STATE state);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetRealSLDTemperature", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetRealSLDTemperature(InstanceHandle handle, int controller, int sensor, ref double tmp);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetCMOSGain", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetCMOSGain(InstanceHandle handle, int controller, CMOS_GAIN gain);
		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCMOSGain", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetCMOSGain(InstanceHandle handle, int controller, ref CMOS_GAIN gain);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSpectrometerTemperature", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetSpectrometerTemperature(InstanceHandle handle, int controller, ref double tmp);

		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetDigitalOutputManualSetting", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE SetDigitalOutputManualSetting(InstanceHandle handle, int controller, DigitalManualSetting dms);
		[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetDigitalOutputManualSetting", CallingConvention = CallingConvention.StdCall)]
		public static extern ERRCODE GetDigitalOutputManualSetting(InstanceHandle handle, int controller, ref DigitalManualSetting dms);
	};//end of class TSCMCAPICS
}//end of namespace tscmccs
