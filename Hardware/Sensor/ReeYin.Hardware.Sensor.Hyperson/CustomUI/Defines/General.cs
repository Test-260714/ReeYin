using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin.Hardware.Sensor.Hyperson.CustomUI.Defines
{
    public enum Gain
    {
        GAIN_1,
        GAIN_2,
        GAIN_4,
    }

    public enum HdrModel
    {
        HDR_OFF,
        HDR_2,
        HDR_3,
        HDR_4,
        HDR_5,
        HDR_6,
        HDR_7,
        HDR_8,
    }

    public enum XRange
    {
        Full,
        四分之三,
        四分之二,
        四分之一,
        八分之一,
    }

    public enum XSubsample
    {
        OFF,
        二分之一,
        四分之一,
        八分之一,
        十六分之一,
    }

    public enum Binning
    {
        NoBinning,
        XBinning,
        Default,
        XZBinning,
        BinningEx,
    }

    public enum ZSubsample
    {
        OFF,
        二分之一,
    }

    public enum TriggerModel
    {
        连续触发,
        编码器触发,
        外部触发,
    }

    public enum AdjustRoiForFpsCo
    {
        OFF,
        NoBinning,
        X_Binning,
        Z_Binning,
        X_Z_Binning,
    }

    public enum InputModel
    {
        两相一倍频,
        两相两倍频,
        两相四倍频,
    }

    public enum Layer
    {
        One,
        Two,
        Three,
        Four,
        All,
    }





}
