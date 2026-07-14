using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR7Link
{
    #region public enum

    /// <summary>
    /// 接口函数返回值 / Interface function return value
    /// </summary>
    public enum SR7IF_ERROR
    {
        SR7IF_ERROR_NOT_FOUND = (-999),           // Item is not found.
        SR7IF_ERROR_COMMAND = (-998),             // Command not recognized.
        SR7IF_ERROR_PARAMETER = (-997),           // Parameter is invalid.
        SR7IF_ERROR_UNIMPLEMENTED = (-996),       // Feature not implemented.
        SR7IF_ERROR_HANDLE = (-995),              // Handle is invalid.
        SR7IF_ERROR_MEMORY = (-994),              // Out of memory.
        SR7IF_ERROR_TIMEOUT = (-993),             // Action timed out.
        SR7IF_ERROR_DATABUFFER = (-992),          // Buffer not large enough for data.
        SR7IF_ERROR_STREAM = (-991),              // Error in stream.
        SR7IF_ERROR_CLOSED = (-990),              // Resource is no longer avaiable.
        SR7IF_ERROR_VERSION = (-989),             // Invalid version number.
        SR7IF_ERROR_ABORT = (-988),               // Operation aborted.
        SR7IF_ERROR_ALREADY_EXISTS = (-987),      // Conflicts with existing item.
        SR7IF_ERROR_FRAME_LOSS = (-986),          // Loss of frame.
        SR7IF_ERROR_ROLL_DATA_OVERFLOW = (-985),  // Continue mode Data overflow.
        SR7IF_ERROR_ROLL_BUSY = (-984),           // Read Busy.
        SR7IF_ERROR_MODE = (-983),                // Err mode.
        SR7IF_ERROR_CAMERA_NOT_ONLINE = (-982),   // Camera not online.
        SR7IF_ERROR = (-1),                       // General error.
        SR7IF_OK = (0),                           // Operation successful.
        SR7IF_NORMAL_STOP = (-100)                //A normal stop caused by external IO or other causes

    }
    // SetSetting GetSetting
    public enum SAVEPOWEROFF
    {
        ESAPO_NOSAVE = 1, // 1 掉电不保存 / No save after power off
        ESAPO_SAVE   // 2 掉电保存 / Save after power off
    };

    public enum SR7IF_SETTING_ITEM
    {
        TRIG_MODE = 1,    // 触发模式 / Trigger mode
        SAMPLED_CYCLE,    // 采样周期 / Sampling cycle
        BATCH_ON_OFF,     // 批处理开关 / Batch processing switch
        ENCODER_TYPE,     // 编码器类型 / Encoder type
        ENCODER_INPUTMODE,// 编码器输入模式 / Encoder input mode

        REFINING_POINTS,  // 细化点数 / Refining points
        BATCH_POINT,      // 批处理点数 / Batch processing points

        CYCLICAL_PATTERN, // 循环模式 0 关闭 1 打开 / Cyclical pattern (0: Off, 1: On)
        SEGMENT_BUFER, // 分段存储 0 关闭 1 打开 / Segmented buffer (0: Off, 1: On)
        BATCH_OUTPUT,     // 批处理输出 / Batch processing output
        Z_MEASURING_RANGE,// Z 方向测量范围 / Z-axis measuring range

        SENSITIVITY,      // 感光灵敏度 / Sensitivity
        EXP_TIME,         // 曝光时间 / Exposure time
        LIGHT_CONTROL,    // 光亮控制 / Light control
        LIGHT_MAX,        // 激光亮度上限 / Laser brightness upper limit
        LIGHT_MIN,        // 激光亮度下限 / Laser brightness lower limit
        PEAK_SENSITIVITY, // 峰值灵敏度 / Peak sensitivity
        PEAK_SELECT,      // 峰值选择 / Peak selection

        X_SAMPLING,       // X 轴压缩设定 / X-axis compression setting

        FILTER_X_MEDIAN,  // X 轴中位数滤波 / X-axis median filter
        FILTER_X_SMOOTH,  // X 轴平滑滤波 / X-axis smooth filter
        FILTER_Y_MEDIAN,  // Y 轴(时间轴)中位数滤波 / Y-axis (time axis) median filter
        FILTER_Y_SMOOTH,  // Y 轴平滑滤波（平均滤波） / Y-axis smooth filter (mean filter)

        CHANGE_3D_25D,    // 3D/2.5D 切换 2.5 模式下 X 轴压缩设定为自动变更默认值 / 3D/2.5D switch (In 2.5D mode, X-axis compression setting changes to default automatically)

        X_PIXEL,          // X 数据宽度(单位像素) / X data width (unit: pixels)
        X_PITCH           // X Resolution / X-axis resolution
    };

    // 触发模式 / Trigger mode
    public enum SR7IF_TRIG_MODE
    {
        TM_CONTINUE = 0, // Continuous trigger
        EXT_TRIGGER,     // External trigger
        ENCODER,         // Encoder trigger
    };

    // 采集周期 / Sampling cycle
    // 100 200 400 600 1000 1500 2000 2500

    // 批处理开关 / Batch processing switch
    public enum SR7IF_BATCH_ON_OFF
    {
        OFF = 0, // Off
        ON       // On
    };

    // 编码器类型 / Encoder type
    public enum SR7IF_ENCODER_TYPE
    {
        E_1_1 = 0, // 0：1 相 1 递增 / 1 phase, 1 increment
        E_2_1,     // 1：2 相 1 递增 / 2 phase, 1 increment
        E_2_2,     // 2：2 相 2 递增 / 2 phase, 2 increments
        E_2_4      // 3：2 相 4 递增 / 2 phase, 4 increments
    };

    // 细化点数 1 --
    // 批处理点数 1 --

    // 循环模式 0 关闭 1 打开 / Cyclical pattern (0: Off, 1: On)
    public enum SR7IF_CYCLICAL_PATTERN
    {
        CLOSE = 0, // Off
        OPEN       // On
    };

    // 批处理输出 / Batch processing output
    public enum SR7IF_BATCH_OUTPUT
    {
        PROFILE_AND_INTENSITY = 0, // Profile + intensity
        PROFILE = 1                // Profile only
    };

    // Z 方向测量范围 注：只支持 SR8020/SR8060 / Z-axis measuring range (Note: Only supports SR8020/SR8060)
    public enum SR7IF_Z_MEASURING_RANGE
    {
        Z840 = 0,
        Z768,
        Z512,
        Z384,
        Z256,
        Z192,
        Z128,
        Z96,
        Z64,
        Z48,
        Z32
    };

    // 感光灵敏度 / Sensitivity
    public enum SR7IF_SENSITIVITY
    {
        HIGH = 0,      // High
        HIGH_RANGE_1,  // High range 1
        HIGH_RANGE_2,  // High range 2
        HIGH_RANGE_3,  // High range 3
        HIGH_RANGE_4,  // High range 4
        CUSTOMIZATION  // Custom
    };

    // 曝光时间 / Exposure time
    public enum SR7IF_EXP_TIME
    {
        T10US = 0, // 10µs
        T15US,     // 15µs
        T30US,     // 30µs
        T60US,     // 60µs
        T120US,    // 120µs
        T240US,    // 240µs
        T480US,    // 480µs
        T960US,    // 960µs
        T1920US,   // 1920µs
        T2400US,   // 2400µs
        T4900US,   // 4900µs
        T9800US    // 9800µs
    };

    // 光亮控制 / Light control
    public enum SR7IF_LIGHT_CONTROL
    {
        AUTO = 0, // Auto
        MAN       // Manual
    };

    // 激光亮度上限 0-99 / Laser brightness upper limit 0-99
    // 激光亮度下限 0-99 / Laser brightness lower limit 0-99

    // 峰值灵敏度 / Peak sensitivity
    public enum SR7IF_PEAK_SENSITIVITY
    {
        N_1 = 1,
        N_2,
        N_3,
        N_4,
        N_5
    };

    // 峰值选择 / Peak selection
    public enum SR7IF_PEAK_SELECT
    {
        PS_STANDARD = 0, // Standard
        PS_SRNEAR,       // SR Near
        PS_SRFAR,        // SR Far
        PS_BE_NULL,      // Be Null
        PS_CONTINUE,     // Continue
        PS_GLUE          // Glue
    };

    // X 轴压缩设定 注：2.5D 模式下不能设置 / X-axis compression setting (Note: Not configurable in 2.5D mode)
    public enum SR7IF_X_SAMPLING
    {
        XS_OFF = 1,  // Off
        XS_X2 = 2,   // X2
        XS_X4 = 4,   // X4
        XS_X8 = 8,   // X8
        XS_X16 = 16  // X16
    };

    // X 轴中位数滤波 / X-axis median filter
    public enum SR7IF_FILTER_X_MEDIAN
    {
        XM_OFF = 1,
        XM_N3 = 3,
        XM_N5 = 5,
        XM_N7 = 7,
        XM_N9 = 9
    };

    // Y 轴中位数滤波 / Y-axis median filter
    public enum SR7IF_FILTER_Y_MEDIAN
    {
        YM_OFF = 1,
        YM_N3 = 3,
        YM_N5 = 5,
        YM_N7 = 7,
        YM_N9 = 9
    };

    // X 轴平滑滤波 / X-axis smooth filter
    public enum SR7IF_FILTER_X_SMOOTH
    {
        N1 = 1,
        N2 = 2,
        N4 = 4,
        N8 = 8,
        N16 = 16,
        N32 = 32,
        N64 = 64
    };

    // Y 轴平滑滤波 / Y-axis smooth filter
    public enum SR7IF_FILTER_Y_SMOOTH
    {
        YS_N1 = 1,
        YS_N2 = 2,
        YS_N4 = 4,
        YS_N8 = 8,
        YS_N16 = 16,
        YS_N32 = 32,
        YS_N64 = 64,
        YS_N128 = 128,
        YS_N256 = 256
    };

    // 3D/2.5D 切换 / 3D/2.5D switch
    public enum SR7IF_CHANGE_3D_25D
    {
        T3D = 0,  // 3D mode
        T25D = 1  // 2.5D mode
    };

    #endregion
 
}
