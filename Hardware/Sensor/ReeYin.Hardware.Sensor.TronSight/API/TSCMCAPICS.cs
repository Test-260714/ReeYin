
using System;
using System.Runtime.InteropServices;
using InstanceHandle = System.UInt64;
using uint16_t = System.UInt16;
using uint8_t = System.Byte;


namespace ReeYin.Hardware.Sensor.TronSight
{//begin of namespace tscmccs
    public struct ConstDef {
public static int CMOS_PIXEL_NUM = 1024; ///<CMOS传感器像素数�?
public static int MAX_SENSOR_CHANNEL = 16; ///<传感头通道�?
public static int MAX_DIGITAL_CHANNEL = 4; ///<数字输出通道
public static int MAX_DIGITAL_LIMIT_CHANNEL = 3; ///<数字上下限源通道�?
public static int MAX_ANANLOG_CHANNEL = 2; ///<模拟输出通道�?
public static int OBJECT_NAME_LEN = 20; ///<折射率表物体名称长度
public static int MAX_DATA_TYPES = 16; ///<最大数据输出类型数
public static int MAX_SINGLE_DATA_CNT = 64; ///<读取单次测量数据长度
public static int MAX_INDICATE_SIZE = 16; ///<最大标签数�?
public static int MAX_ENCODER_CHANNEL = 2; ///<最大编码器数量
public static int MAX_SERIAL_NUMBER_LEN = 19; ///<序列号最大长�?
public static int CALIB_TABLE_SIZE = 512; ///<标定表长�?

};
/**
* @brief 设备类型
*/
public enum DEVICE_TYPE {
	CONTROLLER,		///<控制�?
	SENSOR,///<传感�?
};
/**
* @brief 通讯连接端口
*/
public enum CONNECTION_TYPE {
	USB,///<USB
	SERIAL,///<串口
	ETHERNET,///<以太�?
};
/**
* @brief 传感头模�?
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
	CR4000 = 0x34,///<光谱CR4000版本探头
	C2000 = 0x35,///<光谱C2000版本探头
	C2600 = 0x36,///<光谱C2600版本探头
	C100 = 0x37,///<光谱C100版本探头
	C7000L = 0x38,///<光谱C7000L版本探头
	C7000S = 0x39,///<光谱C7000S版本探头
	CR5000S = 0x3A,///<光谱CR5000S版本探头
	C100B = 0x3B,///<光谱C100B版本探头
	C100C = 0x3C,///<光谱C100C版本探头
	C700 = 0x3D,///<光谱C700版本探头
	C8000 = 0x3E,///<光谱C8000版本探头
};

/**
* @brief 控制器版�?
*/
public enum CONTROLLER_VERSION {
	NONE = 0x00,///<未知控制器类�?
	TC1  = 0x01,///<TS-TC1单通道光谱共焦控制�?
	TC2  = 0x02,///<TS-TC2双通道光谱共焦控制�?
	TC4  = 0x04,///<TS-TC2双通道光谱共焦控制�?
    TC8  = 0x08,///<TS-TC8八双通道光谱共焦控制�?
    TC16 = 0x10,///<TS-TC16十六双通道光谱共焦控制�?
    };
/**
* @brief 功能状�?
*/
public enum STATE {
	OFF,		///<关闭
	ON,		///<开�?
};
/**
* @brief 原始图像数据�?
*/
public enum FRAME_DATA_SRC {
	ORIGIN,		///<原始图像
	CALIB,		///<校准图像
	SHARPNESS		///<锐度图像
};
/**
* @brief 滤波器窗口尺�?
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

// ===================== 新增API - 测量模式 =====================
/**
* @brief 测量模式（新增）
* @details 对应官方手册5.1.6节，共有三种测量模式
*/
public enum MEASUREMODE {
	CONFOCAL_DISTANCE,                  ///<光谱测距模式
	INTERF_THICKNESS_SINGLE_LAYER,      ///<干涉单层测厚模式
	INTERF_THICKNESS_MULTI_LAYER,       ///<干涉多层测厚模式
	CONFOCAL_THICKNESS_SINGLE_LAYER,    ///<光谱单层测厚模式
	CONFOCAL_THICKNESS_MULTI_LAYER,     ///<光谱多层测厚模式
};

// ===================== 新增API - 中值滤波窗口宽�?=====================
/**
* @brief 中值滤波窗口宽度（新增�?
* @details 对应官方TSCMCBase.h中的TS_MEDIAN_FILTER_WIDTH
*/
public enum MEDIAN_FILTER_WIDTH {
	_1,     ///<无滤�?
	_3,     ///<窗口宽度�?
	_5,     ///<窗口宽度�?
	_9,     ///<窗口宽度�?
	_15,    ///<窗口宽度�?5
	_31,    ///<窗口宽度�?1
	_63,    ///<窗口宽度�?3
};

// ===================== 新增API - 图像滤波宽度 =====================
/**
* @brief 图像滤波宽度（新增）
* @details 对应官方TSCMCBase.h中的TS_IMAGE_FILTER_WIDTH，用于原始图像滤�?
*/
public enum IMAGE_FILTER_WIDTH {
	_1,     ///<滤波宽度�?
	_2,     ///<滤波宽度�?
	_3,     ///<滤波宽度�?
	_5,     ///<滤波宽度�?
	_7,     ///<滤波宽度�?
	_11,    ///<滤波宽度�?1
	_15,    ///<滤波宽度�?5
};

// ===================== 新增API - 峰排序方�?=====================
/**
* @brief 峰排序方式（新增�?
*/
public enum PEAK_SORT_MODE {
	LEFT_AND_RIGHT, ///<左右排序
	HEIGHT,         ///<高度排序
};
/**
* @brief 编码器滤波尺�?
*/
public enum ENCODER_FILTER_WIDTH {
	NONE,	///<无滤�?
	_4,		///<窗口尺寸4
	_16,		///<窗口尺寸16
};
/**
* @brief 编码器输出形�?
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
	CH1 = 1,///<通道1
	CH2 = 2,///<通道2
};
/**
* @brief 测量数据超限警告来源类型
*/
public enum WARNING_SOURCE {
	DIST1,///<距离1
	DIST2,///<距离2
	THICKNESS,///<厚度2
};

/**
* @brief 用于进行数据运算的数据来�?
*/
public enum MATH_DATA_SRC {
	DIST1,///<距离1
	DIST2,///<距离2
};

/**
* @brief MATH数据运算通道
*/
public enum MATH_CHANNEL {
	_1,///<MATH1
	_2,///<MATH2
	_3,///<MATH3
	_4,///<MATH4
	_5,///<MATH5
	_6,///<MATH6
	_7,///<MATH7
	_8,///<MATH8
};

/**
* @brief MATH通道符号
*/
public enum MATHSIGN {
	POS,  ///<�?
	NEG,  ///<�?
	ZERO, ///<�?
};

/**
* @brief 模拟通道�?
*/
public enum ANALOG_CHANNEL {
	CH1 = 1,		///<模拟输出端口1
	CH2 = 2,        ///<模拟输出端口2
	CH3 = 3,      ///<模拟输出端口3
	CH4 = 4,      ///<模拟输出端口4
	CH5 = 5,      ///<模拟输出端口5
	CH6 = 6,      ///<模拟输出端口6
	CH7 = 7,      ///<模拟输出端口7
	CH8 = 8,      ///<模拟输出端口8
	CH9 = 9,      ///<模拟输出端口9
	CH10 = 10,    ///<模拟输出端口10
	CH11 = 11,    ///<模拟输出端口11
	CH12 = 12,    ///<模拟输出端口12
	CH13 = 13,    ///<模拟输出端口13
	CH14 = 14,    ///<模拟输出端口14
	CH15 = 15,    ///<模拟输出端口15
	CH16 = 16,    ///<模拟输出端口16
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
* @brief 传感头采样间�?
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
	_50US,      //<采样间隔50us
	_55_5US,    //<采样间隔55.5us
	_62_5US,    //<采样间隔62.5us
	_66_5US,    //<采样间隔66.5us
	_80US,      //<采样间隔80us
	_90_5US,    //<采样间隔90.5us
	_110US,     //<采样间隔110us
	_142_5US,   //<采样间隔142.5us
	_166_5US,   //<采样间隔166.5us
	_400US, //<采样间隔400us
	_4MS,       //<采样间隔4ms
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
* @brief 数字通道输出极�?
*/
public enum DIGITAL_OUTPUT_LEVEL {
	LOW,	///< 低电�?
	HIGH,	///< 高电�?
};
/**
* @brief 编码器输入通道
*/
public enum ENCODER_CHANNEL {
	CH1 = 1,		///<编码器通道1
	CH2 = 2,		///<编码器通道2
};
/**
* @brief 编码器输入模�?
*/
public enum ENCODER_INPUT_MODE {
	A,		///< 单路脉冲
	AB,		///< 双路脉冲
};
/**
* @brief 外部触发/编码器通道触发�?
*/
public enum TRIG_SOURCE {
	LEVEL,///< 单路脉冲，以上升沿为一个位移单位（360°）为一个位移单�?
	ENCODER_AB,		///<AB，AB和ABZ模式�?0°为一个位移单�?
	ENCODER_ABZ,		///< ABZ，AB和ABZ模式�?0°为一个位移单�?
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
	ON,///<开�?
};
/**
* @brief 触发同步模式配置
*/
public enum SYNC_MODE {
	SYNC,	///<同步触发，两个探头均以编码器ch1的触发信号触�?
	ASYNC	///<异步触发，ch1和ch2上的探头分别以编码器ch1和ch2的触发信号触�?
};
/**
* @brief 触发方式选择
*/
public enum TRIG_METHOD {
	NONE,///<无触发，传感头根据设置的采样间隔进行采样并输�?
	ENCODER,///<编码器触发采�?
	SYNCIN///<同步触发
};
/**
* @brief 峰选择模式
*/
public enum PEAK_SELECTION_MODE {
	NUMBER = 0,///<编号模式
	WINDOW = 1,///<窗模�?
	MAX    = 2,///<最大值模�?
	LAST   = 3,///<最后一个峰模式
};
/**
* @brief 数据输出来源类型
*/
public enum DIGITAL_INPUT_SRC {
	DIST1 = 0,///<距离1
	DIST2 = 1,///<距离2
	THICKNESS = 2,///<厚度
	MATH1 = 0,///<通道数据加减计算�?
	MATH2 = 1,///<通道数据加减计算�?
	MATH3 = 2,///<通道数据加减计算�?
	MATH4 = 3,///<通道数据加减计算�?
	MATH5 = 4,///<通道数据加减计算�?
	MATH6 = 5,///<通道数据加减计算�?
	MATH7 = 6,///<通道数据加减计算�?
	MATH8 = 7,///<通道数据加减计算�?
	MULTI_MATH1 = 8, ///<多光点运算�?
	MULTI_MATH2 = 9, ///<多光点运算�?
	MULTI_MATH3 = 10,    ///<多光点运算�?
	MULTI_MATH4 = 11,    ///<多光点运算�?
	ANY_MATH1 = 12,  ///任意测量运算�?
	ANY_MATH2 = 13,  ///任意测量运算�?
	};
/**
* @brief 模拟输入通道
*/
public enum ANALOG_INPUT_CHANNEL {
	CONTROLLER,///<控制�?
	CH1,	///<通道1
	CH2,	///<通道2
	CH3,	///<通道3
	CH4,    ///<通道4
	CH5,    ///<通道6
	CH6,    ///<通道6
	CH7,    ///<通道7
	CH8,    ///<通道8
	CH9,    ///<通道9
	CH10,   ///<通道10
	CH11,   ///<通道11
	CH12,   ///<通道12
	CH13,   ///<通道13
	CH14,   ///<通道14
	CH15,   ///<通道15
	CH16,   ///<通道16
};

/**
* @brief 模拟通道数据�?
*/
public enum ANALOG_SOURCE {
	DIST1 = 0,	///<距离1
	DIST2 = 1,	///<距离2
	THICKNESS = 2,	///<厚度
	MATH1 = 0,	///<MATH1
	MATH2 = 1,  ///<MATH2
	MATH3 = 2, ///<MATH3
	MATH4 = 3, ///<MATH4
	MATH5 = 4, ///<MATH5
	MATH6 = 5, ///<MATH6
	MATH7 = 6, ///<MATH7
	MATH8 = 7, ///<MATH8
	MULTI_MATH1 = 8,   ///<多光点运算�?
	MULTI_MATH2 = 9,   ///<多光点运算�?
	MULTI_MATH3 = 10,  ///<多光点运算�?
	MULTI_MATH4 = 11,  ///<多光点运算�?
	ANY_MATH1 = 12,    ///任意测量运算�?
	ANY_MATH2 = 13,    ///任意测量运算�?
	};

/**
* @brief 数据输出来源通道
*/
public enum DIGITAL_INPUT_CHANNEL {
	CONTROLLER,		///<控制�?
	CH1,	///<通道1
	CH2,	///<通道2
	CH3,	///<通道2
	CH4,	///<通道2
};/**
* @brief 数据输出条件
*/
public enum DIGITAL_OUTPUT_COND {
	OVER_LIMIT	    = 0,///<超上�?
	UNDER_LIMIT	    = 1,///<超下�?
	OVER_UNDER_LIMIT = 2,///<超上限或超下�?
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
	PEAK1_HEIGHT,///<�?高度
	PEAK2_HEIGHT,///<�?高度
	INTENSITY,///<光强
	EXPTIME,	///<曝光时间
	THICKNESS,///<厚度
};
/**
* @brief 控制器输出数据选择
*/
public enum CONTROLLER_OUTPUT_DATA {
	TIMESTAMP = 1,	///<时间�?
	ENCODER1,		///<编码�?读数
	ENCODER2,		///<编码�?读数
	MATH1,///<探头距离数据加减后输�?
	MATH2,///<探头距离数据加减后输出，�?个计算�?
	MULTI_MATH1,///<四探头测量值进行运算，�?个计算�?
	MATH3,///<探头距离数据加减后输出，�?个计算�?
	MATH4,///<探头距离数据加减后输出，�?个计算�?
	MATH5,///<探头距离数据加减后输出，�?个计算�?
	MATH6,///<探头距离数据加减后输出，�?个计算�?
	MATH7,///<探头距离数据加减后输出，�?个计算�?
	MATH8,///<探头距离数据加减后输出，�?个计算�?
	MULTI_MATH2,///<四探头测量值进行运算，�?个计算�?
	MULTI_MATH3,///<四探头测量值进行运算，�?个计算�?
	MULTI_MATH4,///<四探头测量值进行运算，�?个计算�?
	ANY_MATH1,///<任意测试数据计算�?
	ANY_MATH2,///<任意测试数据计算�?
	};
	/**
* @brief 控制器输出数据选择
*/
public enum MULTI_MATH_CHANNEL
{
	_1,///<通道1
	_2,///<通道2
	_3,///<通道3
	_4,///<通道4
};
	
	/**
* @brief 多光点计算数据源
*/
public enum MULTI_MATH_DATA_SRC
{
	DISTANCE1,///<距离1
	DISTANCE2,///<距离2
	THICKNESS,///<厚度
};

	/**
* @brief 多光点计算方�?
*/
public enum MULTI_MATH_CALC_MODE
{
	MEAN,///<平均�?
	MEDIAN,///<中�?
	MAX,///<最大�?
	MIN,///<最小�?
};

	/**
* @brief ANY_MATH数据运算通道
*/
public enum ANY_MATH_CHANNEL
{
	_1,///<ANY_MATH1
	_2,///<ANY_MATH2
};

/**
* @brief ANY_MATH计算方式
*/
public enum ANY_MATH_MODE
{
	FORMULA,///<公式计算
	AVERAGE,///<平均值计�?
	MEDIAN,///<中值计�?
	PEAK_PEAK,///<峰峰值计�?
	MAX,///<最大值计�?
	MIN,///<最小值计�?
};

/**
* @brief AnyMath计算方式AX+BY+C系数
*/
public struct ANY_MATH_FORMULA
{
	public double A;
	public double B;
	public double C;
};
	/**
* @brief 数据类型与通道信息
*/
public struct DATA_INPUT_SETTING
{
	public int channel;///<数据通道，包含控制器通道及探头通道
	public DIGITAL_INPUT_SRC type;///<数据类型
};

public struct AnyMathSetting
{
	public ANY_MATH_MODE mode;///<任意测量计算后输出的模式
	public ANY_MATH_FORMULA formula;///如果模式为公式，则需配置该系�?
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
	public DATA_INPUT_SETTING[] anyMathSrc;///<如果模式为公式，则使用前两项做公式计算的X和Y，如果为其它统计模式，则将数据中配置的所有数据类型参与到运算中，当channel�?1，表明该数据源无�?
};

	/**
	* @brief 错误代码
*/
	public enum ERRCODE {
	OK = 0,		///<成功
	CMD_FAILED = -1,///<发送命令失�?
	TIMEOUT = -2,///<接收命令超时
	WAITING_FOR_NEXT_FRAME = -3,///<等待下一�?
	MESSAGE_INCOMPLETE = -4,///<返回信息不全
	RETN_CMD_UNMATCH = -5,///<返回命令不匹�?
	RETN_CMD_ERROR = -6,///<返回命令出错 
	ALREADY_OPEN = -7,///<通道已经打开
	INVALID_PARAMETER = -8,///<输入参数无效
	BAUDRATE_TOO_LOW = -9,///<波特率过�?
	NO_DATA_IN_BUFFER = -10,///<缓冲区中无数�?
	DEVICE_NOT_SUPPORTED = -11, ///<当前命令设备不支�?
	DEVICE_NOT_CONNECTED = -12,///<设备未连�?
	IS_ACQUIRE_DATA = -13,///<正在采集数据
	FILE_NOT_EXIST = -14,///<文件不存�?
	FILE_NOT_OPEN = -15,///<文件打开失败
	SENSOR_INFO_NOT_FOUND = -16,///<未找到相应传感头信息
	FIRMWARE_NOT_SUPPORTED = -17,///<固件版本不支�?
	CONTROLLER_TYPE_UNCOMPATIBLE = -18,///<控制器类型不支持
	CALIBRATION_TABLE_CHANNEL_UNCOMPATIBLE = -19,///<不是对应通道的标定表
	LOWER_GREATER_THAN_UPPER = -20,///<参数中设置的下限大于上限或起点大于终�?
	OUT_OF_VALID_RANGE = -21,///<输入参数不在有效范围�?
	CALIBRATION_TABLE_USED_BY_OTHER_CHANNEL = -22,///<标定表正在被其它通道使用
	SENSOR_CHANNAL_NOT_ENABLED = -23,///<探头通道未使�?
	UNKNOWN = -999,///<未知错误
};

/**
* @brief SYNC有效电平
*/
public enum SYNC_VALID_LEVEL {
	LOW,		///<低电平或下降沿触�?
	HIGH,		///<高电平或上升沿触�?
};
/**
* @brief SYNC用于计数触发时的滤波宽度，滤除小于设定时间长度的正脉冲或负脉�?
*/
public enum SYNC_FILTER_WIDTH {
	_0_1_US,///<滤波宽度�?.1us
	_0_4_US,///<滤波宽度�?.4us
	_1_6_US,///<滤波宽度�?.6us
	_6_4_US,///<滤波宽度�?.4us
	_25_6_US,///<滤波宽度�?5.6us
	_102_4_US,///<滤波宽度�?02.4us
	_409_6_US,///<滤波宽度�?09.6us
	_1638_4US,///<滤波宽度�?638.4us
};

/**
* @brief 通道使能状态独热码
*/
public enum CHANNEL_ENABLE_MODE {
	CH1 = (1 << 0),///<通道1使能
	CH2 = (1 << 1),///<通道2使能
	CH3 = (1 << 2),///<通道3使能
	CH4 = (1 << 3),///<通道4使能
	CH5 = (1 << 4),///<通道5使能
	CH6 = (1 << 5),///<通道6使能
	CH7 = (1 << 6),///<通道7使能
	CH8 = (1 << 7),///<通道8使能
	CH9 = (1 << 8),///<通道9使能
	CH10 = (1 << 9),///<通道10使能
	CH11 = (1 << 10),///<通道11使能
	CH12 = (1 << 11),///<通道12使能
	CH13 = (1 << 12),///<通道13使能
	CH14 = (1 << 13),///<通道14使能
	CH15 = (1 << 14),///<通道15使能
	CH16 = (1 << 15),///<通道16使能
};
/**
* @brief 串口波特�?
*/
public enum BAUDRATE {
	_19200,
	_38400,
	_57600,
	_115200,
	_9600,
};/**
* @brief 网络地址结构体，大端字节�?
*/
public struct IPAddr {
	public uint8_t c1;///<IP地址�?�?
	public uint8_t c2;///<IP地址�?�?
	public uint8_t c3;///<IP地址�?�?
	public uint8_t c4;///<IP地址�?�?
};

/**
* @brief 控制器以太网通信参数结构�?
*/
public struct EthernetConfiguration {
	public IPAddr ip;///<IP地址
	public IPAddr subnet_mask;///<子网掩码
	public IPAddr gateway;///<网关
	public uint8_t host_addr_last_char;///<本机地址最后一�?
	public uint16_t host_port;///<与不同控制器通信采用不同端口，防止相互占�?
};
	
/**
* @brief 峰检测参�?
*/
public struct PeakDetection {
	public int threshold;///<峰阈�?
	public int sharpness;///<峰锐�?
	public int minimum_spacing;///<峰间�?
};



/**
* @brief 编码器配置参�?
*/
public struct EncoderSetting {
	public ENCODER_FILTER_WIDTH filter_width;		///<编码器滤波窗�?
	public ENCODER_INPUT_MODE input_mode;///<输入模式
	public ENCODER_OUTPUT_MODE output_mode;///<编码器输出格�?
	public bool z_phase;		///<编码器Z相输�?
};

/**
* @brief 外部触发参数配置
*/
public struct TriggerSetting {
	public ENCODER_CHANNEL channel;		///<触发�?
	public TRIG_MODE mode;	///<触发模式
	public TRIG_DIRECTION direction;	///<触发方向
	public TRIG_TRACK_MODE track_mode;	///<追踪模式
	public int downsample_factor;///<采样间隔点数
};

	/**
* @brief 多光点计算参数结构体
*/
public struct MultiMathSetting
{
	public MULTI_MATH_DATA_SRC src;///<数据�?
	public MULTI_MATH_CALC_MODE mode;///<计算方式
};

/**
* @brief 通道模拟量配置结构体
* @details 仅支持固件版�?.2.0以前控制器模拟配置，在固件版�?.2.0以后，采用新型号模拟输出芯片，该结构体不再使�?
*/
public struct ChannelAnalogOutput {
	public ANALOG_SOURCE source;		///<模拟输出�?
	public ANALOG_INPUT_CHANNEL input_channel;		///<输出通道
	public STATE output_en;	///<输出使能
	public ANALOG_OUTPUT_MODE output_mode;///<输出模式
	public double distance_start;///<距离起点
	public double distance_end;///<距离终点
	public double cv_start;	///<电压（流）起�?
	public double cv_end;		///<电压（流）终�?
};


/**
* @brief 通道模拟量配�?
* @details 支持固件版本2.2.0以后控制器模拟配置，2.2.0以前版本见TS_ChannelAnalogOutput
*/
public struct AnalogOutputSetting {
	public STATE output_en;///<模拟输出使能，使能为关时无模拟量输出，使能为开时才有模拟量输出
	public ANALOG_INPUT_CHANNEL input_channel;///<数据输入通道
	public ANALOG_SOURCE source;///<映射为模拟量的数据来�?
	public ANALOG_OUT_RANGE range;///<模拟量输出范�?
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
	public DIGITAL_OUTPUT_LEVEL output_level;///<输出状�?
};
	


/**
* @brief 峰值结构体，每个探头对应一个TS_Peak值，最多两个峰
*/
public struct PeakSelection {
	public PEAK_SELECTION_MODE mode;///<峰选择模式
	public int peak1_idx;///<�?编号
	public int peak1_window_start;///<�?起始像素
	public int peak1_window_end;///<�?终止像素
	public int peak2_idx;///<�?编号
	public int peak2_window_start;///<�?起始像素
	public int peak2_window_end;///<�?终止像素
};

/**
* @brief 曝光时间控制结构�?
*/
public struct ExposureConfig {
	public STATE auto_control;///<曝光自动控制
	public uint16_t exposure_time;///<手动曝光时间,单位为us,限制范围�?.4-5000us,自动曝光时，该参数无�?
};

// ===================== 新增结构�?- 自动曝光时间设置 =====================
/**
* @brief 自动曝光时间设置结构体（新增�?
*/
public struct AutoExposureTimeSetting {
	public uint16_t min_exposure_time;  ///<自动曝光最小时�?单位为us
	public uint16_t max_exposure_time;  ///<自动曝光最大时�?单位为us
};
	
/**
* @brief 编码器同步参�?
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
* @brief 双通道探头测量MATH数据计算方法配置，固定选择探头1、探�?距离数据进行运算
*/
public struct ChannelSetting {
	public MATH_DATA_SRC src;///<数据�?
	public MATHSIGN sign;///<通道符号
};

/**
* @brief 多通道探头测量MATH数据计算方法配置，不同探头之前可两两配对
*/
public struct MathSetting {
	public int sensor;///<探头通道
	public MATH_DATA_SRC src;///<数据�?
	public MATHSIGN sign;///<通道符号
};
/**
* @brief 暗校准本底�?
*/
public struct DarkRefCurve {
	public short[] data;///<数据
};

/**
* @brief 暗校准系�?
*/
public struct DarkCoeffCurve {
	public uint16_t[] data;///<数据
};

/**
* @brief 折射率系�?
*/
public struct RefractiveCoeff {
	public double c486;///<486nm波长下折射率
	public double c587;///<587nm波长下折射率
	public double c656;///<656nm波长下折射率
};

/**
* @brief 折射率校准表
*/
public struct RefractiveTable {
	public string object_name;		///<物体名称
	public RefractiveCoeff refractive_data;///<折射率表
};

/**
* @brief 暗校准表
*/
public struct DarkReferenceTable {
	public DarkRefCurve refr;///<本底�?
	public DarkCoeffCurve coeff;		///<系数
};

/**
* @brief 设备序列�?
*/
public struct SerialNumber {
	public string serial;///<序列号字符数据，长度固定
};

/**
* @brief 通道使能状�?
*/
public struct ChannelEnable {
	public int channelCnt;		///<最大通道数，表示状态有效位，例如，当channelCnt�?时，说明channelState仅低两位有效
	public short channelState;	///<通道状态，通道对应bit位为1时，说明通道被使能，可以进行数据采集，例如，�?channelState&TS_CHANNEL_ENABLE_MODE_CH1)==1时，说明通道被使能，如果�?，则说明该通道未使�?
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
	public DataCfg cfg;///<用于记录当前数据对应的通道及类�?
	public double data;///<测量数据
};

/**
* @brief 固件版本
*/
public struct VersionDetail {
	public uint8_t reserve;///<保留�?
	public uint8_t major;///<主版�?
	public uint8_t minor;///<小版�?
	public uint8_t patch;///<修订版本
};

/**
* @brief 探头
*/
public struct MeasureRangeNode {
	public double start;///<距离起点
	public double end;///<距离终点
	public double rev;///<保留位，暂时无具体含�?
};
	

public class TSCMCAPICS {
private InstanceHandle m_impl;

	private const string TscmcapicDll = "TSCMCAPIC.dll";
	private const string TscmcapinetDll = "TSCMCAPINET.dll";
	private const string NativeDirectoryName = "TronSight";

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
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetContorllerChannelEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetContorllerChannelEnable(InstanceHandle handle,int controller,ChannelEnable channelEnable);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetContorllerChannelEnable", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetContorllerChannelEnable(InstanceHandle handle,int controller,ref ChannelEnable channelEnable);
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
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConnectionDeviceInfo", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConnectionDeviceInfo(InstanceHandle handle,ref int controller_idx);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConnectionOn", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConnectionOn(InstanceHandle handle,int controller);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConnectionOff", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConnectionOff(InstanceHandle handle,int controller);
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
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigZeroOffset", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigZeroOffset(InstanceHandle handle,int controller,int sensor,double zero_offset);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigZeroOffset", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigZeroOffset(InstanceHandle handle,int controller,int sensor,ref double zero_offset);
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
public static extern ERRCODE SetConfigMultiMathSetting(InstanceHandle handle, int controller, MULTI_MATH_CHANNEL channel, MultiMathSetting multi_para);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigMultiMathSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetAnyMathSetting(InstanceHandle handle, int controller, ANY_MATH_CHANNEL any_math_channel, AnyMathSetting ams);
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetAnyMathSetting", CallingConvention = CallingConvention.StdCall)]
public static extern int MaxMathChannels(InstanceHandle handle);

// ===================== 新增API - 测量模式�?种：测距、单层测厚、多层测厚）=====================
/// <summary>
/// 设置测量模式（新增）
/// </summary>
/// <param name="handle">设备实例对象</param>
/// <param name="controller">控制器编�?/param>
/// <param name="measure_mode">测量模式：光谱测�?干涉单层测厚/干涉多层测厚/光谱单层测厚/光谱多层测厚</param>
/// <returns>错误�?/returns>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigMeasurementMode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigMeasurementMode(InstanceHandle handle, int controller, MEASUREMODE measure_mode);

/// <summary>
/// 读取测量模式（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigMeasurementMode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigMeasurementMode(InstanceHandle handle, int controller, ref MEASUREMODE measure_mode);

// ===================== 新增API - 外部触发配置（触发采样模式、采样使能电平、单脉冲采样个数、滤波宽度）=====================
/// <summary>
/// 设置外部触发相关参数（新增）
/// </summary>
/// <param name="handle">设备实例对象</param>
/// <param name="controller">控制器编�?/param>
/// <param name="external_trigger">外部触发参数结构体，包含触发方式和同步触发配�?/param>
/// <returns>错误�?/returns>
/// <remarks>
/// ExternalTrigger结构体包含：
/// - trig_method: 触发方式（无触发/编码器触�?同步触发�?
/// - sync_setting: 同步触发配置，包含：
///   - state: SYNC使能
///   - input_mode: SYNC输入模式（边沿触�?电平触发�? 对应"触发采样模式"
///   - valid_level: SYNC有效电平（低电平/高电平）- 对应"采样使能电平"
///   - sample_per_trigger: 单次脉冲采样点数 - 对应"单脉冲采样个�?
///   - filter_width: 滤波宽度 - 对应"滤波宽度"
/// </remarks>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigExternalTrigger", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigExternalTrigger(InstanceHandle handle, int controller, ExternalTrigger external_trigger);

/// <summary>
/// 获取外部触发相关参数（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigExternalTrigger", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigExternalTrigger(InstanceHandle handle, int controller, ref ExternalTrigger external_trigger);

// ===================== 新增API - 图像滤波宽度（原始图像滤波）=====================
/// <summary>
/// 设置图像滤波窗口宽度（新增）
/// </summary>
/// <param name="handle">设备实例对象</param>
/// <param name="controller">控制器编�?/param>
/// <param name="sensor">探头编号</param>
/// <param name="autoSelect">自动选择状态（ON/OFF�?/param>
/// <param name="width">图像滤波宽度</param>
/// <returns>错误�?/returns>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetSensorImageFilterWidth", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetSensorImageFilterWidth(InstanceHandle handle, int controller, int sensor, STATE autoSelect, IMAGE_FILTER_WIDTH width);

/// <summary>
/// 读取图像滤波窗口宽度（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetSensorImageFilterWidth", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetSensorImageFilterWidth(InstanceHandle handle, int controller, int sensor, ref STATE autoSelect, ref IMAGE_FILTER_WIDTH width);

/// <summary>
/// 获取当前使用的滤波宽度（新增�?
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetCurrentImageFilterWidthInUse", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetCurrentImageFilterWidthInUse(InstanceHandle handle, int controller, int sensor, ref IMAGE_FILTER_WIDTH width);

// ===================== 新增API - 峰排序方�?=====================
/// <summary>
/// 设置峰排序类型（新增�?
/// </summary>
/// <param name="handle">设备实例对象</param>
/// <param name="controller">控制器编�?/param>
/// <param name="sensor">探头通道�?/param>
/// <param name="peak_sort_mode">峰排序方式（左右排序/高度排序�?/param>
/// <returns>错误�?/returns>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigPeakSortMode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigPeakSortMode(InstanceHandle handle, int controller, int sensor, PEAK_SORT_MODE peak_sort_mode);

/// <summary>
/// 读取峰排序类型（新增�?
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigPeakSortMode", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigPeakSortMode(InstanceHandle handle, int controller, int sensor, ref PEAK_SORT_MODE peak_sort_mode);

// ===================== 新增API - 时间戳复�?=====================
/// <summary>
/// 时间戳复位（新增�?
/// </summary>
/// <param name="handle">设备实例对象</param>
/// <param name="controller">控制器编�?/param>
/// <returns>错误�?/returns>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ResetTimeStamp", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE ResetTimeStamp(InstanceHandle handle, int controller);

// ===================== 新增API - 位置清零 =====================
/// <summary>
/// 设置位置清零（新增）
/// </summary>
/// <param name="handle">设备实例对象</param>
/// <param name="controller">控制器编�?/param>
/// <param name="sensor">探头编号</param>
/// <returns>错误�?/returns>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_ResetZero", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE ResetZero(InstanceHandle handle, int controller, int sensor);

// ===================== 新增API - 零参考点量程指示范围 =====================
/// <summary>
/// 设置零参考点量程指示范围（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigZeroPointIndicateScale", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigZeroPointIndicateScale(InstanceHandle handle, int controller, int sensor, uint8_t indicate_scale);

/// <summary>
/// 获取零参考点量程指示范围（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigZeroPointIndicateScale", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigZeroPointIndicateScale(InstanceHandle handle, int controller, int sensor, ref uint8_t indicate_scale);

// ===================== 新增API - 峰位置获�?=====================
/// <summary>
/// 获取有效检测峰所在的像素位置（新增）
/// </summary>
/// <param name="handle">设备实例对象</param>
/// <param name="controller">控制器编�?/param>
/// <param name="sensor">探头编号</param>
/// <param name="peakPosBuf">存储峰位置的数组</param>
/// <param name="npixel">有效峰的个数</param>
/// <param name="maxLength">打算读取的峰数目，最大为10</param>
/// <returns>错误�?/returns>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetExposurePeakPosition", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetExposurePeakPosition(InstanceHandle handle, int controller, int sensor, ref int peakPosBuf, ref int npixel, int maxLength);

// ===================== 新增API - 最大有效峰数目 =====================
/// <summary>
/// 读取最大有效峰数目（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetMaxValidPeakNum", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetMaxValidPeakNum(InstanceHandle handle, int controller, int sensor, ref uint8_t maxValidPeakNum);

/// <summary>
/// 设置最大有效峰数目（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetMaxValidPeakNum", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetMaxValidPeakNum(InstanceHandle handle, int controller, int sensor, uint8_t maxValidPeakNum);

// ===================== 新增API - 环境温度 =====================
/// <summary>
/// 获取环境温度（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetEnvironmentTemperature", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetEnvironmentTemperature(InstanceHandle handle, int controller, ref double tmp);

// ===================== 新增API - 自动曝光时间设置 =====================
/// <summary>
/// 设置自动曝光时间参数（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetConfigAutoExposureTimeSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetConfigAutoExposureTimeSetting(InstanceHandle handle, int controller, int sensor, AutoExposureTimeSetting aets);

/// <summary>
/// 获取自动曝光时间参数（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetConfigAutoExposureTimeSetting", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetConfigAutoExposureTimeSetting(InstanceHandle handle, int controller, int sensor, ref AutoExposureTimeSetting aets);

// ===================== 新增API - 交替曝光 =====================
/// <summary>
/// 设置交替曝光组使能（新增�?
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetAlternateExposureGroup", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetAlternateExposureGroup(InstanceHandle handle, int controller, uint16_t group1, uint16_t group2);

/// <summary>
/// 读取交替曝光组使能（新增�?
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetAlternateExposureGroup", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetAlternateExposureGroup(InstanceHandle handle, int controller, ref uint16_t group1, ref uint16_t group2);

/// <summary>
/// 设置交替曝光开关状态（新增�?
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetAlternateExposureState", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetAlternateExposureState(InstanceHandle handle, int controller, STATE state);

/// <summary>
/// 读取交替曝光开关状态（新增�?
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetAlternateExposureState", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetAlternateExposureState(InstanceHandle handle, int controller, ref STATE state);

// ===================== 新增API - 输出数据选择 =====================
/// <summary>
/// 读取所有通道的数据输出选择（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_GetOutputDataSelection", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE GetOutputDataSelection(InstanceHandle handle, int controller, CONNECTION_TYPE connection_port, ref int data_channels, ref int data_types, ref int ntypes, int max_length);

/// <summary>
/// 设置所有通道的数据输出选择（新增）
/// </summary>
[DllImport("TSCMCAPIC.dll", EntryPoint = "TSCMCAPI_SetOutputDataSelection", CallingConvention = CallingConvention.StdCall)]
public static extern ERRCODE SetOutputDataSelection(InstanceHandle handle, int controller, CONNECTION_TYPE connection_port, ref int data_channels, ref int data_types, int max_length);

 };//end of class TSCMCAPICS

}//end of namespace tscmccs
