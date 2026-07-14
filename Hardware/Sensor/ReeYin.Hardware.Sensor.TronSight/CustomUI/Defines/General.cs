using System;

namespace ReeYin.Hardware.Sensor.TronSight.CustomUI.Defines
{
    /// <summary>
    /// 测量模式
    /// </summary>
    public enum MeasureMode
    {
        单层测厚模式,
        多层测厚模式,
        测距模式
    }

    /// <summary>
    /// 触发采样模式（5种组合模式，通过TRIG_METHOD和SyncSetting组合实现）
    /// </summary>
    public enum TriggerSamplingMode
    {
        固定时间间隔采样,
        固定时间间隔采样_SYNC_IN控制数据输出,
        编码器触发采样,
        编码器触发采样_SYNC_IN控制数据输出,
        SYNC_IN边沿触发以固定时间间隔采样N点
    }

    /// <summary>
    /// 采样使能电平（对应API: SYNC_VALID_LEVEL）
    /// </summary>
    public enum SamplingEnableLevel
    {
        低电平_下降沿,
        高电平_上升沿
    }

    /// <summary>
    /// 触发滤波宽度（对应API: SYNC_FILTER_WIDTH）
    /// </summary>
    public enum TriggerFilterWidth
    {
        _0_1us,
        _0_4us,
        _1_6us,
        _6_4us,
        _25_6us,
        _102_4us,
        _409_6us,
        _1638_4us
    }

    /// <summary>
    /// 触发通道
    /// </summary>
    public enum TriggerChannel
    {
        编码器1,
        编码器2
    }

    /// <summary>
    /// 触发模式
    /// </summary>
    public enum TriggerMode
    {
        计数触发,
        位置触发
    }

    /// <summary>
    /// 触发方向
    /// </summary>
    public enum TriggerDirection
    {
        正向,
        反向,
        双向
    }

    /// <summary>
    /// 追踪模式
    /// </summary>
    public enum TrackingMode
    {
        关,
        开
    }

    /// <summary>
    /// 编码器输入模式
    /// </summary>
    public enum EncoderInputMode
    {
        单路,
        双路
    }

    /// <summary>
    /// 编码器解码模式
    /// </summary>
    public enum EncoderDecodeMode
    {
        X1,
        X2,
        X4
    }

    /// <summary>
    /// 标定方式
    /// </summary>
    public enum CalibrationMethod
    {
        单点标定,
        两点标定
    }

    /// <summary>
    /// 厚度标定测量方式（MATH标定）
    /// </summary>
    public enum ThicknessCalibrationMode
    {
        同侧测高,
        对射测厚,
        异侧测宽
    }
}

