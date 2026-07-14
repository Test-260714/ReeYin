using System;
using System.Runtime.InteropServices;
using static GTN.mc;
using static GTN.mc_la;

namespace GTN
{ 
	/*-----------------------------------------------------------*/
	/*原gts.cs：基本功能指令                                      */
	/*-----------------------------------------------------------*/
    public class mc
    {
        /*-----------------------------------------------------------*/
        /* Channel of Command                                        */
        /*-----------------------------------------------------------*/
        public const short CHANNEL_HOST = 0;
        public const short CHANNEL_UART = 1;
        public const short CHANNEL_SIM = 2;
        public const short CHANNEL_ETHER = 3;
        public const short CHANNEL_RS232 = 4;
        public const short CHANNEL_PCIE = 5;
        public const short CHANNEL_RINGNET = 6;
        /*-----------------------------------------------------------*/
        /* Error Code                                                */
        /*-----------------------------------------------------------*/

        public const short CMD_SUCCESS = 0;

        public const short CMD_ERROR_READ_LEN = -2;                 /* read data length error */
        public const short CMD_ERROR_READ_CHECKSUM = -3;            /* checksum error */

        public const short CMD_ERROR_WRITE_BLOCK = -4;              /* write pci fail */
        public const short CMD_ERROR_READ_BLOCK = -5;               /* read pci fail */

        public const short CMD_ERROR_OPEN = -6;                     /* error to open device */
        public const short CMD_ERROR_CLOSE = -6;                    /* error to close device */
        public const short CMD_ERROR_DSP_BUSY = -7;                 /* DSP busy */

        public const short CMD_LOCK_ERROR = -8;                     /* multi-thread lock error */
        public const short CMD_DMA_ERROR = -9;                      /* dma transmission error */    
        public const short CMD_COMM_ERROR = -10;                    /* pcie comm error */
        public const short CMD_LOAD_RINGNET_DLL_ERROR = -11;        /* load ringnet dll error */
        public const short CMD_RINGNET_STIME_ERROR = -12;           /* load ringnet dll error */

        public const short CMD_RINGNET_ENC0_ERROR = -13;            /* core1 encoder initial error */
		public const short CMD_RINGNET_ENC1_ERROR = -14;            /* core2 encoder initial error */

        public const short CMD_LOAD_RINGNET_ERROR = -17;             /* ringnet API load error */
        public const short CMD_MCVERSION_MATCH_ERROR = -15;          /* dll error,need to update dll */
		public const short CMD_LOCK_NULL = -20;                      /* multithreading open handle error */
        public const short CMD_MCVERSION_MATCH_WARNING = 15;         /* dll error,without some functions */
        public const short CMD_DSPVERSION_MATCH_WARNING = 16;        /* dsp is too old */

		public const short CMD_FILE_MATCH_WARNING = 17;              /* config file format or data error */

        public const short CMD_ERROR_EXECUTE = 1;
        public const short CMD_ERROR_VERSION_NOT_MATCH = 3;
        public const short CMD_ERROR_PARAMETER= 7;
        public const short CMD_ERROR_UNKNOWN = 8;                    /* unspported command */

        public const short MC_NONE  = -1;

        public const short MC_LIMIT_POSITIVE = 0;
        public const short MC_LIMIT_NEGATIVE = 1;
        public const short MC_ALARM  = 2;
        public const short MC_HOME  = 3;
        public const short MC_GPI = 4;
        public const short MC_ARRIVE = 5;
        public const short MC_EGPI0 = 6;
//      public const short MC_EGPI1 = 7;
//      public const short MC_EGPI2 = 8;
        public const short MC_MPG = 9;

        public const short MC_ENABLE = 10;
        public const short MC_CLEAR = 11;
        public const short MC_GPO  = 12;
//      public const short MC_EGPO0 = 13;
//      public const short MC_EGPO1 = 14;
        public const short MC_AU_ADC = 17;
        public const short MC_HSO = 18;
        public const short MC_AU_DAC = 19;

        public const short MC_DAC = 20;
        public const short MC_STEP  = 21;
        public const short MC_PULSE = 22;
        public const short MC_ENCODER = 23;
        public const short MC_ADC = 24;

        public const short MC_AU_ENCODER = 26;

        public const short MC_ABS_ENCODER = 29;

        public const short MC_AXIS = 30;
        public const short MC_PROFILE = 31;
        public const short MC_CONTROL = 32;

        public const short MC_TRIGGER = 40;

        public const short MC_AU_TRIGGER = 44;

        public const short MC_TERMINAL = 50;
        public const short MC_MPG_ENCODER = 53;
        public const short MC_EXT_MODULE = 60;
        public const short MC_EXT_DI = 61;
        public const short MC_EXT_DO = 62;
        public const short MC_EXT_AI = 63;
        public const short MC_EXT_AO = 64;

        public const short MC_SCAN_CRD  = 70;
        public const short MC_LASER	= 71;
        public const short MC_LASER_AO = 72;

        public const short MC_POS_COMPARE = 80;

        public const short MC_WATCH_VAR = 200;
        public const short MC_WATCH_EVENT = 201;
        public struct TVersion
        {
            public short year;
            public short month;
            public short day;
            public short version;
            public short user;
            public short reserve1;
            public short reserve2;
            public short chip;
         } 
        public const short CORE_MODE_TIMER = 0;
        public const short CORE_MODE_SYNCH = 1;
        public const short CORE_MODE_EXTERNAL =	2;

        public const short CORE_TASK_DEFAULT = 0;
        public const short CORE_TASK_DLM = 1;

        public const short SKIP_MODULE_SCAN	= 0x001;
        public const short SKIP_MODULE_POS_COMPARE = 0x002;
        public const short SKIP_MODULE_CRD = 0x004;

        public const short SKIP_MODULE_PLC = 0x010;
        public const short SKIP_MODULE_DLM = 0x020;

        public const short SKIP_MODULE_AXIS_CALCULATE = 0x100;

        public const short SKIP_MODULE_WATCH = 0x800;

        public enum ETimeElapse
        {
	        TIME_ELAPSE_PROFILE = 1000,

	        TIME_ELAPSE_HOST_COMMAND_EXECUTE = 1220,
	        TIME_ELAPSE_ETHER_COMMAND_EXECUTE,

	        TIME_ELAPSE_PROFILE_CALCULATE = 6000,

	        TIME_ELAPSE_SCAN = 18000,

	        TIME_ELAPSE_AXIS_CHECK = 20000,
	        TIME_ELAPSE_AXIS_CALCULATE,

	        TIME_ELAPSE_ENCODER = 30000,

	        TIME_ELAPSE_DI = 31000,

	        TIME_ELAPSE_DO = 32000,

	        TIME_ELAPSE_AI = 33000,

	        TIME_ELAPSE_AO = 34000,

	        TIME_ELAPSE_TRIGGER = 38000,

	        TIME_ELAPSE_CONTROL = 40000,

	        TIME_ELAPSE_WATCH = 52000,

	        TIME_ELAPSE_TERMINAL = 53000,
            TIME_ELAPSE_TERMINALDET = 54000,
       } 
       
        public struct TPid
        {
	            public double kp;
	            public double ki;
	            public double kd;
	            public double kvff;
	            public double kaff;
	            public Int32   IntegralLimit;
	            public Int32   derivativeLimit;
	            public short  limit;
        }

        /*-----------------------------------------------------------*/
        /* Basic function                                            */
        /*-----------------------------------------------------------*/
        [DllImport("gxn.dll")]
        public static extern short GTN_GetUuid(short core, out byte pCode, short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_OpenCard(short channel, string pPrm, string pFileName);
        [DllImport("gxn.dll")]
        public static extern short GTN_NetInit(short mode, string pFileName, short overTime, out long pStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDeviceShareMax(short core, short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_Open(short channel,short param);
        [DllImport("gxn.dll")]
        public static extern short GTN_OpenRingNet(short channel,short param,string pFile,short index,Int32 count);
        [DllImport("gxn.dll")]
        public static extern short GTN_Close();
        [DllImport("gxn.dll")]
        public static extern short GTN_GetChannel(out short pChannel);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetVersion(short core,out System.IntPtr pVersion);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetVersionEx(short core,short type,out TVersion pVersion);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVersion(short core,short type,ref TVersion pVersion);
        [DllImport("gxn.dll")]
        public static extern short GTN_Reset(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetClock(short core, out UInt32 pClock, out UInt32 pLoop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetClockHighPrecision(short core,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearTime(short core,ETimeElapse item);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTime(short core, ETimeElapse item, out UInt32 pTime, out UInt32 pTimeMax, out UInt32 pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCoreMode(short core,short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCoreMode(short core,out short pMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCoreShare(short core,short type,short index,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCoreShare(short core,short type,out short pIndex,out short pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCoreTask(short core,short task);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCoreTask(short core,out short pTask);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetResMax(short core,short type,out short pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetResCount(short core,short type,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetResCount(short core,short type,out short pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetSts(short core,short axis,out Int32 pSts,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClrSts(short core,short axis,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_AxisOn(short core,short axis);
        [DllImport("gxn.dll")]
        public static extern short GTN_AxisOff(short core,short axis);
        [DllImport("gxn.dll")]
        public static extern short GTN_MultiAxisOn(short core,UInt32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_MultiAxisOff(short core, UInt32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisOnDelayTime(short core,ushort delayTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisOnDelayTime(short core,out ushort pDelayTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_Stop(short core, Int32 mask, Int32 option);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPrfPos(short core, short profile, Int32 prfPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_SynchAxisPos(short core, Int32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_ZeroPos(short core,short axis,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLimitStatus(short core,short axis,out short pLimitPositive,out short pLimitNegative);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetSoftLimitMode(short core,short axis,short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetSoftLimitMode(short core,short axis,out short pMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetSoftLimit(short core, short axis, Int32 positive, Int32 negative);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetSoftLimit(short core, short axis, out Int32 pPositive, out Int32 pNegative);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisBand(short core, short axis, Int32 band, Int32 time);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisBand(short core, short axis, out Int32 pBand, out Int32 pTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPrfPos(short core,short profile,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPrfVel(short core,short profile,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPrfAcc(short core,short profile,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPrfMode(short core,short profile,out Int32 pValue, short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisPrfPos(short core,short axis,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisPrfPosCompensate(short core,short axis,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisPrfVel(short core,short axis,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisPrfAcc(short core,short axis,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisEncPos(short core,short axis,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisEncVel(short core,short axis,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisEncAcc(short core,short axis,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisError(short core,short axis,out double pValue, short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetControlFilter(short core,short control,short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetControlFilter(short core,short control,out short pIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetControlSuperimposed(short core,short control,short superimposedType,short superimposedIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetControlSuperimposed(short core,short control,out short pSuperimposedType,out short pSuperimposedIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPid(short core,short control,short index,ref TPid pPid);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPid(short core,short control,short index,out TPid pPid);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetKvffFilter(short core,short control,short index,short kvffFilterExp,double accMax);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetKvffFilter(short core,short control,short index,out short pKvffFilterExp,out double pAccMax);
        [DllImport("gxn.dll")]
        public static extern short GTN_Delay(short core,ushort ms);
        [DllImport("gxn.dll")]
        public static extern short GTN_DelayHighPrecision(short core,ushort profile);

        public const short STEP_DIR  = 0;
        public const short STEP_PULSE = 1;
        public const short STEP_ORTHOGONAL = 2;
        
        [DllImport("gxn.dll")]
        public static extern short GTN_LoadConfig(short core,string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_AlarmOn(short core,short axis);
        [DllImport("gxn.dll")]
        public static extern short GTN_AlarmOff(short core,short axis);
        [DllImport("gxn.dll")]
        public static extern short GTN_LmtsOn(short core,short axis,short limitType);
        [DllImport("gxn.dll")]
        public static extern short GTN_LmtsOff(short core,short axis,short limitType);
        [DllImport("gxn.dll")]
        public static extern short GTN_StepDir(short core,short step);
        [DllImport("gxn.dll")]
        public static extern short GTN_StepPulse(short core,short step);
        [DllImport("gxn.dll")]
        public static extern short GTN_StepOrthogonal(short core,short step);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetMtrBias(short core,short dac,short bias);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMtrBias(short core,short dac,out short pBias);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetMtrLmt(short core,short dac,short limit);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMtrLmt(short core,short dac,out short pLimit);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetSense(short core,short dataType,short dataIndex,short value);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetSense(short core,short dataType,short dataIndex,out short pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_EncOn(short core,short encoder);
        [DllImport("gxn.dll")]
        public static extern short GTN_EncOff(short core,short encoder);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosErr(short core, short control, Int32 error);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosErr(short core, short control, out Int32 pError);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetStopDec(short core,short profile,double decSmoothStop,double decAbruptStop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetStopDec(short core,short profile,out double pDecSmoothStop,out double pDecAbruptStop);
        [DllImport("gxn.dll")]
        public static extern short GTN_CtrlMode(short core,short axis,short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetStopIo(short core,short axis,short stopType,short inputType,short inputIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAdcFilterPrm(short core,short adc,double k);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAdcFilterPrm(short core,short adc,out double pk);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisPrfVelFilter(short core,short axis,short filterNumExp);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisPrfVelFilter(short core,short axis,out short pFilterNumExp);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisEncVelFilter(short core,short axis,short filterNumExp);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisEncVelFilter(short core,short axis,out short pFilterNumExp);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetProfileScale(short core, short i, Int32 alpha, Int32 beta);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetProfileScale(short core, short i, out Int32 pAlhpa, out Int32 pBeta);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEncoderScale(short core, short i, Int32 alpha, Int32 beta);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEncoderScale(short core, short i, out Int32 pAlhpa, out Int32 pBeta);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAuEncoderScale(short core, short i, Int32 alpha, Int32 beta);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuEncoderScale(short core, short i, out Int32 pAlhpa, out Int32 pBeta);
        
        /*-----------------------------------------------------------*/
        /* Capture and Triggr                                        */
        /*-----------------------------------------------------------*/
        public const short CAPTURE_HOME = 1;
        public const short CAPTURE_INDEX = 2;
        public const short CAPTURE_PROBE = 3;
        public const short CAPTURE_HSIO0 = 6;
        public const short CAPTURE_HSIO1 = 7;

        public struct TTrigger
        {
            public short encoder;
            public short probeType;
            public short probeIndex;
            public short sense;
            public Int32 offset;
            public UInt32 loop;
            public short windowOnly;
            public Int32 firstPosition;
            public Int32 lastPosition;
        }
        public struct TTriggerEx
        {
            public short latchType;
            public short latchIndex;
            public short probeType;
            public short probeIndex;
            public short sense;
            public Int32 offset;
            public UInt32 loop;
            public short windowOnly;
            public Int32 firstPosition;
            public Int32 lastPosition;
        }
        public struct TTriggerAlign
        {
            public short encoder;
            public short probeType;
            public short probeIndex;
            public short sense;
            public Int32 offset;
            public UInt32 loop;
            public short windowOnly;
            public short pad2;
            public Int32 firstPosition;
            public Int32 lastPosition;
        }

        public struct TTriggerStatus
        {
            public short execute;
            public short done;
            public Int32 position;
        }

        public struct TTriggerStatusEx
        {
            public short execute;
            public short done;
            public Int32 position;
            public UInt32 clock;
            public UInt32 loopCount;
        }
       
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAuTrigger(short core, short i, ref TTriggerEx pTrigger);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuTrigger(short core, short i, out TTriggerEx pTrigger);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearAuTriggerStatus(short core, short i);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuTriggerStatus(short core, short i, out TTriggerStatusEx pTriggerStatusEx, short count);
        
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTrigger(short core,short i,ref TTrigger pTrigger);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTrigger(short core,short i,out TTrigger pTrigger);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTriggerEx(short core, short i, ref TTriggerEx pTrigger);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTriggerEx(short core, short i,out TTriggerEx pTrigger);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTriggerStatus(short core,short i,out TTriggerStatus pTriggerStatus,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTriggerStatusEx(short core,short i,out TTriggerStatusEx pTriggerStatusEx,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearTriggerStatus(short core,short i);
        [DllImport("gxn.dll")]
        public static extern short GTN_DisableTrigger(short core, short i);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCaptureMode(short core,short encoder,short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCaptureMode(short core,short encoder,out short pMode,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCaptureStatus(short core,short encoder,out short pStatus,out Int32 pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCaptureSense(short core,short encoder,short mode,short sense);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearCaptureStatus(short core,short encoder);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCaptureRepeat(short core,short encoder,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCaptureRepeatStatus(short core,short encoder,out short pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCaptureRepeatPos(short core, short encoder, out Int32 pValue, short startNum, short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCaptureRepeatFifoMode(short core, short encoder, short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCaptureRepeatFifoMode(short core, short encoder, out short pMode);
        
        public struct TLatchValueInfo
        {
	        public short fifoFull;
	        public short pad1_1;
	        public short pad1_2;
	        public short pad1_3;
	        public double pad2_1;
	        public double pad2_2;
        };

        public struct TTriggerPrm
        {
	        public short latchType;
	        public short latchIndex;
	        public short probeType;
	        public short probeIndex;
	        public short sense;
	        public short loopType;
	        public Int32  offset;
	        public UInt32 loop;
	        public short windowOnly;
	        public short pad1;
	        public Int32 firstPosition;
	        public Int32 lastPosition;
	        public short fifoMode;
	        public short pad2_1;
	        public short pad2_2;
	        public short pad2_3;
	        public double pad3;
        };

        [DllImport("gxn.dll")]
        public static extern short  GTN_SetTriggerPrm(short core,short i,ref TTriggerPrm pTriggerPrm);
        [DllImport("gxn.dll")]
        public static extern short  GTN_GetTriggerPrm(short core,short i,out TTriggerPrm pTriggerPrm);
        [DllImport("gxn.dll")]
        public static extern short  GTN_GetTriggerLatchValue(short core,short i,Int32 count,out Int32 pValue,out Int32 pCount,ref TLatchValueInfo pInfo);

        public const short TRIGGER_DELTA_MODE_DEFAULT = 0;
        public const short TRIGGER_DELTA_CHECKPOINT_MODE_AUTO = 0;
        public const short TRIGGER_DELTA_CHECKPOINT_MODE_MANUAL = 1;
        public struct TCheckpoint
        {
	        public short mode;
            public Int32 offset;
	        public short fifoIndex;
            public UInt32 crossCount;
	        public short fifoDataCount;
	        public short dataReady;
            public Int32 data;
	        public short dataIndex;
        } 
        public struct TTriggerDeltaPrm
        {
	        public short mode;
	        public short dir;
	        public short triggerIndex0;
            public short triggerIndex1;
        } 

        public struct TTriggerDeltaInfo
        {
	        public short enable;
	        public short checkpointCount;
	        public short fifoDataCount;
	        public short lostCount;
        }
        
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearTriggerDelta(short core,short index,short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_AddTriggerDeltaCheckpoint(short core, short index, short mode, Int32 offset, short fifo, ref short pIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_ReadTriggerDeltaCheckpointData(short core, short index, short checkpointIndex, out Int32 pBuf, short count, out short pReadCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_WriteTriggerDeltaCheckpointData(short core, short index, short checkpointIndex, ref Int32 pBuf, short count, ref short pWriteCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTriggerDeltaPrm(short core,short index,ref TTriggerDeltaPrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTriggerDeltaPrm(short core,short index,out TTriggerDeltaPrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTriggerDeltaCheckpoint(short core,short index,short checkpointIndex,out TCheckpoint pCheckpoint);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTriggerDeltaInfo(short core,short index,out TTriggerDeltaInfo pTriggerDelta);
        [DllImport("gxn.dll")]
        public static extern short GTN_TriggerDeltaOn(short core,short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_TriggerDeltaOff(short core,short index);
        
        [DllImport("gxn.dll")]
        public static extern short GTN_LinkCaptureOffset(short core, short encoder, short source);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCaptureOffset(short core, short encoder, ref Int32 pOffset, short count, Int32 loop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCaptureOffset(short core, short encoder, out Int32 pOffset, out short pCount, out Int32 pLoop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCaptureOffsetStatus(short core, short encoder, out short pCount, out Int32 pLoop, out Int32 pCapturePos);

        /*-----------------------------------------------------------*/
        /* Basic Motion                                              */
        /*-----------------------------------------------------------*/

        public struct TTrapPrm
        {
	        public double acc;
	        public double dec;
	        public double velStart;
	        public short  smoothTime;
        } 
       
        [DllImport("gxn.dll")]
        public static extern short GTN_Update(short core, Int32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPos(short core, short profile, Int32 pos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPos(short core, short profile, out Int32 pPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVel(short core,short profile,double vel);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetVel(short core,short profile,out double pVel);
        [DllImport("gxn.dll")]
        public static extern short GTN_PrfTrap(short core,short profile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTrapPrm(short core,short profile,ref TTrapPrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTrapPrm(short core,short profile,out TTrapPrm pPrm);
        public struct TJogPrm
        {
	        public double acc;
	        public double dec;
	        public double smooth;
        }
        [DllImport("gxn.dll")]
        public static extern short GTN_PrfJog(short core,short profile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetJogPrm(short core,short profile,ref TJogPrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetJogPrm(short core,short profile,out TJogPrm pPrm);

        public const short PT_MODE_STATIC = 0;
        public const short PT_MODE_DYNAMIC = 1;

        public const short PT_SEGMENT_NORMAL = 0;
        public const short PT_SEGMENT_EVEN = 1;
        public const short PT_SEGMENT_STOP = 2;

        public struct TPtInfo 
        {
	        public double prfPos;
            public Int32 loop;
	        public short mode;
	        public short fifoUse;
	        public short fifoPlace;
	        public short segmentNumber;
            public UInt32 segmentReceive1;
            public UInt32 segmentReceive2;
            public UInt32 segmentExecute1;
            public UInt32 segmentExecute2;
            public UInt32 bufferReceive1;
            public UInt32 bufferReceive2;
            public UInt32 bufferExecute1;
            public UInt32 bufferExecute2;
        } 
        //[DllImport("gxn.dll")]
        //public static extern short GTN_PosCurrFeedForward(short core,short profile,double pos,Int32 time,short torque,short type,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_PrfPt(short core,short profile,short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPtLoop(short core, short profile, Int32 loop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPtLoop(short core, short profile, out Int32 pLoop);
        [DllImport("gxn.dll")]
        public static extern short GTN_PtSpace(short core,short profile,out short pSpace,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_PtSpaceEx(short core,short profile,out short pSpace,out short pListSpace,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_PtData(short core,short profile,double pos,Int32 time,short type,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_PtClear(short core,short profile,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_PtStart(short core, Int32 mask, Int32 option);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPtMemory(short core,short profile,short memory);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPtMemory(short core,short profile,out short pMemory);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPtPrecisionMode(short core,short profile,short precisionMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPtPrecisionMode(short core,short profile,out short pPrecisionMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPtInfo(short core,short profile,out TPtInfo pPtInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPtLink(short core,short profile,short fifo,short list);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPtLink(short core,short profile,short fifo,out short pList);
        [DllImport("gxn.dll")]
        public static extern short GTN_PtDoBit(short core,short profile,short doType,short index,short value,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_PtAo(short core,short profile,short aoType,short index,double value,short fifo);

        public struct TPvtTableMovePrm
        {	
	        public short tableId;
            public Int32 distance;					
	        public double vm;						
	        public double am;						
	        public double jm;						
	        public double time;					
        } 
        [DllImport("gxn.dll")]
        public static extern short GTN_PrfPvt(short core,short profile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPvtLoop(short core, short profile, Int32 loop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPvtLoop(short core, short profile, out Int32 pLoopCount, out Int32 pLoop);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtStatus(short core,short profile,out short pTableId,out double pTime,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtStart(short core, Int32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableSelect(short core,short profile,short tableId);
        [DllImport("gxn.dll")]

        public static extern short GTN_PvtTable(short core, short tableId, Int32 count, ref double pTime, ref double pPos, ref double pVel);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableEx(short core, short tableId, Int32 count, ref double pTime, ref double pPos, ref double pVelBegin, ref double pVelEnd);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableComplete(short core, short tableId, Int32 count, ref double pTime, ref double pPos, ref double pA, ref double pB, ref double pC, double velBegin, double velEnd);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTablePercent(short core, short tableId, Int32 count, ref double pTime, ref double pPos, ref double pPercent, double velBegin);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtPercentCalculate(short core, Int32 n, ref double pTime, ref double pPos, ref double pPercent, double velBegin, ref double pVel);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableContinuous(short core, short tableId, Int32 count, ref double pPos, ref double pVel, ref double pPercent, ref double pVelMax, ref double pAcc, ref double pDec, double timeBegin);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtContinuousCalculate(short core, Int32 n, ref double pPos, ref double pVel, ref double pPercent, ref double pVelMax, ref double pAcc, ref double pDec, ref double pTime);
       
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableMove(short core, short tableId, Int32 distance, double vm, double am, double jm, ref double pTime);
       [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableMove2(short core, short tableId, Int32 distance, double vm, double am, double jm, ref double pTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableMovePercent(short core, short tableId, Int32 distance, double vm, double acc,double pa1,double pa2,double dec,double pd1,double pd2, ref double pVel,ref double pAcc,ref double pDec,ref double pTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableMovePercentEx(short core, short tableId, Int32 distance, double vm,double acc,double pa1,double pa2,double ma,double dec,double pd1,double pd2,double md,ref double pVel,ref double pAcc,ref double pDec,ref double pTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableMoveTogether(short core,short tableCount,ref TPvtTableMovePrm pPvtTableMovePrm);

        public const short GEAR_MASTER_ENCODER = 1;
        public const short GEAR_MASTER_PROFILE = 2;
        public const short GEAR_MASTER_AXIS = 3;
        public const short GEAR_MASTER_AU_ENCODER  = 4;
        public const short GEAR_MASTER_MPG_ENCODER = 5;
        public const short GEAR_MASTER_ENCODER_OTHER = 101;
        public const short GEAR_MASTER_AXIS_OTHER = 103;
        public const short GEAR_MASTER_AU_ENCODER_OTHER = 104;
        public const short GEAR_MASTER_MPG_ENCODER_OTHER = 105;
        public const short GEAR_EVENT_START = 1;
        public const short GEAR_EVENT_PASS = 2;
        public const short GEAR_EVENT_AREA = 5;
       
        [DllImport("gxn.dll")]
        public static extern short GTN_PrfGear(short core,short profile,short dir);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetGearMaster(short core,short profile,short masterIndex,short masterType,short masterItem);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetGearMaster(short core,short profile,out short  pMasterIndex,out short  pMasterType,out short  pMasterItem);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetGearRatio(short core, short profile, Int32 masterEven, Int32 slaveEven, Int32 masterSlope);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetGearRatio(short core, short profile, out Int32 pMasterEven, out Int32 pSlaveEven, out Int32 pMasterSlope);
        [DllImport("gxn.dll")]
        public static extern short GTN_GearStart(short core, Int32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetGearEvent(short core, short profile, short gearevent, Int32 startPara0, Int32 startPara1);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetGearEvent(short core, short profile, out short pEvent, out Int32 pStartPara0, out Int32 pStartPara1);
        
        public const short FOLLOW_SWITCH_SEGMENT = 1;
        public const short FOLLOW_SWITCH_TABLE = 2;

        public const short FOLLOW_MASTER_ENCODER = 1;
        public const short FOLLOW_MASTER_PROFILE = 2;
        public const short FOLLOW_MASTER_AXIS = 3;
        public const short FOLLOW_MASTER_AU_ENCODER = 4;

        public const short FOLLOW_MASTER_ENCODER_OTHER = 101;
        public const short FOLLOW_MASTER_AXIS_OTHER = 103;

        public const short FOLLOW_EVENT_START = 1;
        public const short FOLLOW_EVENT_PASS = 2;

        public const short FOLLOW_SEGMENT_NORMAL = 0;
        public const short FOLLOW_SEGMENT_EVEN = 1;
        public const short FOLLOW_SEGMENT_STOP = 2;
        public const short FOLLOW_SEGMENT_CONTINUE = 3;
       
        [DllImport("gxn.dll")]
        public static extern short GTN_PrfFollow(short core,short profile,short dir);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowMaster(short core,short profile,short masterIndex,short masterType,short masterItem);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowMaster(short core,short profile,out short  pMasterIndex,out short  pMasterType,out short  pMasterItem);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowLoop(short core, short profile, Int32 loop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowLoop(short core, short profile, out Int32 pLoop);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowEvent(short core, short profile, short followevent, short masterDir, Int32 pos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowEvent(short core, short profile, out short pEvent, out short pMasterDir, out Int32 pPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowSpace(short core,short profile,out short  pSpace,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowData(short core, short profile, Int32 masterSegment, double slaveSegment, short type, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowClear(short core,short profile,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowStart(short core, Int32 mask, Int32 option);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowSwitch(short core,Int32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowMemory(short core,short profile,short memory);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowMemory(short core,short profile,out short  pMemory);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowStatus(short core,short profile,out short  pFifoNum,out short  pSwitchStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowPhasing(short core,short profile,short profilePhasing);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowPhasing(short core,short profile,out short  pProfilePhasing);
        [DllImport("gxn.dll")]
        public static extern short GTN_PrfFollowEx(short core,short profile,short dir);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowMasterEx(short core,short profile,short masterIndex,short masterType,short masterItem);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowMasterEx(short core,short profile,out short  pMasterIndex,out short  pMasterType,out short  pMasterItem);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowLoopEx(short core, short profile, Int32 loop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowLoopEx(short core, short profile, out Int32 pLoop);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowEventEx(short core, short profile, short followevent, short masterDir, Int32 pos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowEventEx(short core, short profile, out short pEvent, out short pMasterDir, out Int32 pPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowSpaceEx(short core,short profile,out short  pSpace,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowDataPercentEx(short core,short profile,double masterSegment,double slaveSegment,short type,short percent,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowClearEx(short core,short profile,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowStartEx(short core, Int32 mask, Int32 option);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowSwitchEx(short core, Int32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowMemoryEx(short core,short profile,short memory);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowMemoryEx(short core,short profile,out short  pMemory);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowStatusEx(short core,short profile,out short  pFifoNum,out short  pSwitchStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowPhasingEx(short core,short profile,short profilePhasing);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowPhasingEx(short core,short profile,out short  pProfilePhasing);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowSwitchNowEx(short core,short profile,short method,short buffer,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowDataPercent2Ex(short core,short profile,double masterSegment,double slaveSegment,double velBeginRatio,double velEndRatio,short percent,out short  pPercent1,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFollowDataPercent2Ex(short core,double masterPos,double v1,double v2,double p,double p1,out double pSlavePos);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowDoBitEx(short core,short profile,short doType,short index,short value,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowDelayEx(short core,short profile,UInt32 delayTime,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_FollowDiBitEx(short core,short profile,short diType,short index,short value,UInt32 time,short fifo);

        public struct TMoveAbsolutePrm
        {
	        public Int32 pos;
	        public double vel;
	        public double acc;
	        public double dec;
	        public short percent;
        } 
       
        [DllImport("gxn.dll")]
        public static extern short GTN_MoveAbsolute(short core,short profile,ref TMoveAbsolutePrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMoveAbsolute(short core,short profile,out TMoveAbsolutePrm pPrm);


        public struct  TTransformOrthogonal
        {
	        public short source;
	        public short enable;
	        public short x;
	        public short y;
	        public double theta;
        } 
       
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTransformOrthogonal(short core,short index,ref TTransformOrthogonal pOrthogonal);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTransformOrthogonal(short core,short index,out TTransformOrthogonal pOrthogonal);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTransformOrthogonalPosition(short core,short index,out double pPositionX,out double pPositionY);
		
        /*-----------------------------------------------------------*/
        /* Comp                                                      */
        /*-----------------------------------------------------------*/  
		public struct TCoordTransformPrm
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public double[] translateion;	                      //工件坐标系相对原始坐标系的平移
			public double theta;	                              //工件坐标系相对原始坐标系的旋转
		}

		public struct TCoordSyncCompPrm
		{
			public short enable;	                              //是否使能坐标系转换功能
			public short refType;	                              //参考类型，可以设置为MC_CRD、MC_GROUP、MC_PROFILE等
			public short crd;	                                  //如果参考类型为MC_CRD，写入坐标系号
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] refIndex;	                           //参考轴号，指定坐标系中的某两个轴，或者profile的两个轴
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public double[] offset;	                               //补偿轴X2、Y2零点相对参考坐标系X1、Y1零点的偏移
			public TCoordTransformPrm refTrans;	                   //参考工件坐标系相对原坐标系的平移和旋转
			public TCoordTransformPrm syncTrans;	               //同步工件坐标系相对原坐标系的平移和旋转
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public short[] reserve1;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public double[] reserve2;
		}

		public struct TPrfComp
		{
			public short type;	                    //补偿类型，例如同步补偿等
			public short index;	                    //一级索引，例如同步补偿支持最多允许四套坐标系，该索引表示第几套同步补偿
			public short subIndex;	                //二级索引，例如第一套同步补偿的第一个轴
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public short[] reserve1;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public double[] reserve2;
		}
		
		[DllImport("gxn.dll")]
		public static extern short GTN_PrfComp(short core,short profile,ref TPrfComp pComp);
		[DllImport("gxn.dll")]
		public static extern short GTN_PrfCompEnable(short core,short profile,short enable,short enableType);
		[DllImport("gxn.dll")]
		public static extern short GTN_BufPrfCompEnable(short core,short crd,short fifo,short profile,short enable,short enableType);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetCoordSyncCompPrm(short core,short index,ref TCoordSyncCompPrm pPrm);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetCoordSyncCompPrm(short core,short index,out TCoordSyncCompPrm pPrm);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetCoordSyncCompValue(short core,short index,double x,double y,out double pCompX,out double pCompY);
				
        /*-----------------------------------------------------------*/
        /* Home                                                      */
        /*-----------------------------------------------------------*/
        public const short HOME_STAGE_IDLE = 0;
        public const short HOME_STAGE_START	= 1;

        public const short HOME_STAGE_SEARCH_LIMIT = 10;
        public const short HOME_STAGE_SEARCH_LIMIT_STOP	= 11;

        public const short HOME_STAGE_SEARCH_LIMIT_ESCAPE = 13;

        public const short HOME_STAGE_SEARCH_LIMIT_RETURN = 15;
        public const short HOME_STAGE_SEARCH_LIMIT_RETURN_STOP = 16;

        public const short HOME_STAGE_SEARCH_HOME = 20;

        public const short HOME_STAGE_SEARCH_HOME_RETURN = 25;

        public const short HOME_STAGE_SEARCH_INDEX = 30;

        public const short HOME_STAGE_SEARCH_GPI = 40;

        public const short HOME_STAGE_SEARCH_GPI_RETURN = 45;

        public const short HOME_STAGE_GO_HOME = 80;

        public const short HOME_STAGE_END = 100;

        public const short HOME_ERROR_NONE = 0;
        public const short HOME_ERROR_NOT_TRAP_MODE = 1;
        public const short HOME_ERROR_DISABLE = 2;
        public const short HOME_ERROR_ALARM = 3;
        public const short HOME_ERROR_STOP = 4;
        public const short HOME_ERROR_STAGE = 5;
        public const short HOME_ERROR_HOME_MODE	= 6;
        public const short HOME_ERROR_SET_CAPTURE_HOME = 7;
        public const short HOME_ERROR_NO_HOME = 8;
        public const short HOME_ERROR_SET_CAPTURE_INDEX	= 9;
        public const short HOME_ERROR_NO_INDEX = 10;

        public const short HOME_MODE_LIMIT = 10;
        public const short HOME_MODE_LIMIT_HOME	= 11;
        public const short HOME_MODE_LIMIT_INDEX = 12;
        public const short HOME_MODE_LIMIT_HOME_INDEX = 13;

        public const short HOME_MODE_HOME = 20;

        public const short HOME_MODE_HOME_INDEX	= 22;

        public const short HOME_MODE_INDEX = 30;

        public struct THomePrm
        {
	        public short mode;						// 回零模式
	        public short moveDir;					// 设置启动搜索时的运动方向
	        public short indexDir;					// 设置Index搜索方向
	        public short edge;						// 设置捕获沿
	        public short triggerIndex;				// 用于设置触发器
	        public short pad10;					    // 保留1
            public short pad11;					    // 保留1
            public short pad12;					    // 保留1
	        public double velHigh;					// Home搜索速度
	        public double velLow;					// Index搜索速度
	        public double acc;
	        public double dec;
	        public short smoothTime;
	        public short pad20;					    // 保留2
            public short pad21;					    // 保留2
            public short pad22;					    // 保留2
	        public Int32 homeOffset;				// 原点偏移
	        public Int32 searchHomeDistance;		// Home最大搜索距离，为0表示不限制
	        public Int32 searchIndexDistance;		// Index最大搜索距离，为0表示不限制
	        public Int32 escapeStep;				// 脱离步长
	        public Int32 pad31;					    // 保留3
            public Int32 pad32;					    // 保留3
        } 

        public struct THomeStatus
        {
	        public short run;
	        public short stage;
	        public short error;
	        public short pad1;
	        public Int32 capturePos;
	        public Int32 targetPos;
        } 
       
        [DllImport("gxn.dll")]
        public static extern short GTN_GoHome(short core,short axis,ref THomePrm pHomePrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetHomePrm(short core,short axis,out THomePrm pHomePrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetHomeStatus(short core,short axis,out THomeStatus pHomeStatus);

        [DllImport("gxn.dll")]
        public static extern short GTN_HandwheelInit(short core, short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetHandwheelStopDec(short core, short slave, double decSmoothStop, double decAbruptStop);
        [DllImport("gxn.dll")]
        public static extern short GTN_StartHandwheel(short core, short slave, short master, short masterEven, short slaveEven, short intervalTime, double acc, double dec, double vel, short stopWaitTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_EndHandwheel(short core, short slave);

        /*-----------------------------------------------------------*/
        /* PLC                                                       */
        /*-----------------------------------------------------------*/
        public const short PLC_THREAD_MAX =	32;
        public const short PLC_PAGE_MAX	= 32;
        public const short PLC_LOCAL_VAR_MAX = 1024;
        public const short PLC_ACCESS_VAR_COUNT_MAX = 8;

        public const short PLC_TIMER_TT	= 0;
        public const short PLC_TIMER_TF	= 1;
        public const short PLC_TIMER_TTF = 2;

        public const short PLC_COUNTER_EQ = 0;
        public const short PLC_COUNTER_LE = 1;
        public const short PLC_COUNTER_GE = 2;

        public const short PLC_COUNTER_EDGE_UP = 0;
        public const short PLC_COUNTER_EDGE_DOWN = 1;
        public const short PLC_COUNTER_EDGE_UP_DOWN = 2;

        public const short PLC_FLANK_UP	= 0;
        public const short PLC_FLANK_DOWN = 1;
        public const short PLC_FLANK_UP_DOWN = 2;

        public enum EPlcBind
        {
	        PLC_BIND_NONE,
	        PLC_BIND_DI,
	        PLC_BIND_DO,
	        PLC_BIND_TIMER,
	        PLC_BIND_COUNTER,
	        PLC_BIND_FLANK,
	        PLC_BIND_SRFF,
        } 

        public struct TVarInfo
        {
	        public short id;
	        public short dataType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public String name;
        } 

        public struct TBindDi
        {
	        public short diType;
	        public short index;
	        public short reverse;
        } 

        public struct TBindDo
        {
	        public short doType;
	        public short index;
	        public short reverse;
        } 

        public struct TBindTimer
        {
	        public short timerType;
	        public Int32 delay;
	        public short inputVarId;
        } 

        public struct TBindCounter
        {
	        public short counterType;
	        public short edge;
	        public Int32 init;
	        public Int32 target;
	        public Int32 begin;
	        public Int32 end;
	        public short dir;
	        public Int32 unit;
	        public short inputVarId;
	        public short resetVarId;
        } 

        public struct TBindFlank
        {
	        public short flankType;
	        public short inputVarId;
        } 

        public struct TBindSrff
        {
	        public short setVarId;
	        public short resetVarId;
        } 

        public struct TCompileInfo
        {
	       public string pFileName;
	       public short  pLineNo;
	       public string pMessage;
        } 

        public struct TThreadSts
        {
	        public short  run;
	        public short  error;
	        public double result;
	        public short  line;
        } 
        
        [DllImport("gxn.dll")]
        public static extern short GTN_Compile(string pFileName,ref TCompileInfo pWrongInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_Download(short core,string pFileName);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFunId(string pFunName,out short  pFunId);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetVarId(string pFunName,string pVarName,out TVarInfo pVarInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_Bind(short core,short thread,short funId,short page);
        [DllImport("gxn.dll")]
        public static extern short GTN_RunThread(short core,short thread);
        [DllImport("gxn.dll")]
        public static extern short GTN_RunThreadPeriod(short core,short thread,short ms,short priority);
        [DllImport("gxn.dll")]
        public static extern short GTN_StopThread(short core,short thread);
        [DllImport("gxn.dll")]
        public static extern short GTN_PauseThread(short core,short thread);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetThreadSts(short core,short thread,out TThreadSts pThreadSts);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetThreadTime(short core,short thread,out short  pPeriod,out double pExecuteTime,out double pExecuteTimeMax);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVarValue(short core,short page,ref TVarInfo pVarInfo,ref double pValue,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetVarValue(short core, short page, ref TVarInfo pVarInfo, out double pValue, short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_UnbindVar(short core,short thread);
        [DllImport("gxn.dll")]
        public static extern short GTN_BindDi(short core,short thread,ref TVarInfo pVarInfo,ref TBindDi pBindDi);
        [DllImport("gxn.dll")]
        public static extern short GTN_BindDo(short core,short thread,ref TVarInfo pVarInfo,ref TBindDo pBindDo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BindTimer(short core,short thread,ref TVarInfo pVarInfo,ref TBindTimer pBindTimer);
        [DllImport("gxn.dll")]
        public static extern short GTN_BindCounter(short core,short thread,ref TVarInfo pVarInfo,ref TBindCounter pBindCounter);
        [DllImport("gxn.dll")]
        public static extern short GTN_BindFlank(short core,short thread,ref TVarInfo pVarInfo,ref TBindFlank pBindFlank);
        [DllImport("gxn.dll")]
        public static extern short GTN_BindSrff(short core,short thread,ref TVarInfo pVarInfo,ref TBindSrff pBindSrff);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindDi(short core,out TVarInfo pVarInfo,out TBindDi pBindDi);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindDo(short core,out TVarInfo pVarInfo,out TBindDo pBindDo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindTimer(short core,out TVarInfo pVarInfo,out TBindTimer pBindTimer,out Int32  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindCounter(short core,out TVarInfo pVarInfo,out TBindCounter pBindCounter,out Int32  pUnitCount,out Int32  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindFlank(short core,out TVarInfo pVarInfo,out TBindFlank pBindFlank);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindSrff(short core,out TVarInfo pVarInfo,out TBindSrff pBindSrff);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindDiCount(short core,short thread,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindDoCount(short core,short thread,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindTimerCount(short core,short thread,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindCounterCount(short core,short thread,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindFlankCount(short core,short thread,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindSrffCount(short core,short thread,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindDiInfo(short core,short thread,short index,out short  pVar,out TBindDi pBindDi);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindDoInfo(short core,short thread,short index,out short  pVar,out TBindDo pBindDo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindTimerInfo(short core,short thread,short index,out short  pVar,out TBindTimer pBindTimer);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindCounterInfo(short core,short thread,short index,out short  pVar,out TBindCounter pBindCounter);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindFlankInfo(short core,short thread,short index,out short  pVar,out TBindFlank pBindFlank);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBindSrffInfo(short core,short thread,short index,out short  pVar,out TBindSrff pBindSrff);

        public struct TThreadStatus
        {
            public short link;
            public UInt32 address;
            public short size;
            public UInt32 page;
            public short delay;
            public short priority;
            public short ptr;
            public short status;
            public short error;
            public short result1;
            public short result2;
            public short result3;
            public short result4;
            public short resultType;
            public short breakpoint;
            public short period;
            public short count;
            public short function;
        } 

        [DllImport("gxn.dll")]
        public static extern short GTN_StepThread(short core,short thread);
        /*-----------------------------------------------------------*/
        /* Interpolation                                             */
        /*-----------------------------------------------------------*/

        public const short INTERPOLATION_AXIS_MAX = 8;

        public const short CRD_OPERATION_DATA_EXT_MAX = 2;


        public const short CRD_BUFFER_MODE_DYNAMIC_DEFAULT = 0;
        public const short CRD_BUFFER_MODE_DYNAMIC_KEEP = 1;
        public const short CRD_BUFFER_MODE_STATIC_INPUT = 11;
        public const short CRD_BUFFER_MODE_STATIC_READY = 12;
        public const short CRD_BUFFER_MODE_STATIC_START  = 1;

        public const short INTERPOLATION_CIRCLE_PLAT_XY = 0;
        public const short INTERPOLATION_CIRCLE_PLAT_YZ = 1;
        public const short INTERPOLATION_CIRCLE_PLAT_ZX = 2;

        public const short INTERPOLATION_HELIX_CIRCLE_XY_LINE_Z = 0;
        public const short INTERPOLATION_HELIX_CIRCLE_YZ_LINE_X = 1;
        public const short INTERPOLATION_HELIX_CIRCLE_ZX_LINE_Y = 2;
        public const short INTERPOLATION_CIRCLE_DIR_CW = 0;
        public const short INTERPOLATION_CIRCLE_DIR_CCW = 1;

        public struct TCrdPrm
        {
	        public short dimension;                              // 坐标系维数
	        public short profile1;                               // 关联profile和坐标轴
            public short profile2;                               // 关联profile和坐标轴
            public short profile3;                               // 关联profile和坐标轴
            public short profile4;                               // 关联profile和坐标轴
            public short profile5;                               // 关联profile和坐标轴
            public short profile6;                               // 关联profile和坐标轴
            public short profile7;                               // 关联profile和坐标轴
            public short profile8;                               // 关联profile和坐标轴
	        public double synVelMax;                             // 最大合成速度
	        public double synAccMax;                             // 最大合成加速度
	        public short evenTime;                               // 最小匀速时间
	        public short setOriginFlag;                          // 设置原点坐标值标志,0:默认当前规划位置为原点位置;1:用户指定原点位置
	        public Int32 originPos1;                             // 用户指定的原点位置
            public Int32 originPos2;                             // 用户指定的原点位置
            public Int32 originPos3;                             // 用户指定的原点位置
            public Int32 originPos4;                             // 用户指定的原点位置
            public Int32 originPos5;                             // 用户指定的原点位置
            public Int32 originPos6;                             // 用户指定的原点位置
            public Int32 originPos7;                             // 用户指定的原点位置
            public Int32 originPos8;                             // 用户指定的原点位置
        }

        public struct TCrdBufOperation
        {
	        public short flag;                                   // 标志该插补段是否含有IO和延时
	        public ushort delay;                                 // 延时时间
	        public short doType;                                 // 缓存区IO的类型,0:不输出IO
            public ushort doMask;                                // 缓存区IO的输出控制掩码
            public ushort doValue;                               // 缓存区IO的输出值
            public ushort dataExt1;                              // 辅助操作扩展数据
            public ushort dataExt2;                              // 辅助操作扩展数据
        }
         
        public struct TCrdData
        {
	        public short motionType;                             // 运动类型,0:直线插补,1:圆弧插补
	        public short circlePlat;                             // 圆弧插补的平面
	        public Int32 pos1;                                   // 当前段各轴终点位置
            public Int32 pos2;                                   // 当前段各轴终点位置
            public Int32 pos3;                                   // 当前段各轴终点位置
            public Int32 pos4;                                   // 当前段各轴终点位置
            public Int32 pos5;                                   // 当前段各轴终点位置
            public Int32 pos6;                                   // 当前段各轴终点位置
            public Int32 pos7;                                   // 当前段各轴终点位置
            public Int32 pos8;                                   // 当前段各轴终点位置
	        public double radius;                                // 圆弧插补的半径
	        public short circleDir;                              // 圆弧旋转方向,0:顺时针;1:逆时针
	        public double center1;                               // 圆弧插补的圆心坐标
            public double center2;                               // 圆弧插补的圆心坐标
            public double center3;                               // 圆弧插补的圆心坐标
            public double vel;                                   // 当前段合成目标速度
	        public double acc;                                   // 当前段合成加速度
	        public short velEndZero;                             // 标志当前段的终点速度是否强制为0,0:不强制为0;1:强制为0
	        public TCrdBufOperation operation;                   // 缓存区延时和IO结构体

	        public double cos1;                                  // 当前段各轴对应的余弦值
            public double cos2;                                  // 当前段各轴对应的余弦值
            public double cos3;                                  // 当前段各轴对应的余弦值
            public double cos4;                                  // 当前段各轴对应的余弦值
            public double cos5;                                  // 当前段各轴对应的余弦值
            public double cos6;                                  // 当前段各轴对应的余弦值
            public double cos7;                                  // 当前段各轴对应的余弦值
            public double cos8;                                  // 当前段各轴对应的余弦值
	        public double velEnd;                                // 当前段合成终点速度
	        public double velEndAdjust;                          // 调整终点速度时用到的变量(前瞻模块)
	        public double r;                                     // 当前段合成位移量
        }

        public struct  TCrdTime
        {
	        public double time;
	        public Int32 segmentUsed;
	        public Int32 segmentHead;
	        public Int32 segmentTail;
        }

        public struct TBufFollowMaster
        {
            public short crdAxis;
            public short masterIndex;
            public short masterType;
        }

        public struct TBufFollowEventCross
        {
            public Int32 masterPos;
            public Int32 pad;
        }

        public struct TBufFollowEventTrigger
        {
            public short triggerIndex;
            public Int32 triggerOffset;
            public Int32 pad;
        }

        public struct TCrdFollowPrm 
        {
            public double velRatioMax;
            public double accRatioMax;
            public Int32 masterLead;
            public Int32 masterEven;
            public Int32 slaveEven;
            public short dir;
            public short smoothPercent;
            public short synchAlign;
        }

        public struct TCrdFollowStatus
        {
            public short stage;
            public double slavePos;
            public double slaveVel;
            public Int32 masterFrameWidth;
            public Int32 masterFrameIndex;
            public UInt32 loopCount;
        }
      
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdPrm(short core, short crd, ref TCrdPrm pCrdPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdPrm(short core,short crd,out TCrdPrm pCrdPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdSpace(short core,short crd,out Int32  pSpace,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdData(short core, short crd, IntPtr pCrdData, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_Bezier(short core, short crd, ref TBezierPrm pBezier, double synVel, double synAcc, double velEnd, long segNum, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXY(short core,short crd,Int32 x,Int32 y,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYOverride2(short core,short crd,Int32 x,Int32 y,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYWN(short core,short crd,Int32 x,Int32 y,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYOverride2WN(short core,short crd,Int32 x,Int32 y,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_LnXYG0(short core,short crd,Int32 x,Int32 y,double synVel,double synAcc,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYG0Override2(short core,short crd,Int32 x,Int32 y,double synVel,double synAcc,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYG0WN(short core,short crd,Int32 x,Int32 y,double synVel,double synAcc,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYG0Override2WN(short core,short crd,Int32 x,Int32 y,double synVel,double synAcc,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_LnXYZ(short core,short crd,Int32 x,Int32 y,Int32 z,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZOverride2(short core,short crd,Int32 x,Int32 y,Int32 z,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZWN(short core,short crd,Int32 x,Int32 y,Int32 z,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZOverride2WN(short core,short crd,Int32 x,Int32 y,Int32 z,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_LnXYZG0(short core,short crd,Int32 x,Int32 y,Int32 z,double synVel,double synAcc,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZG0Override2(short core,short crd,Int32 x,Int32 y,Int32 z,double synVel,double synAcc,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZG0WN(short core,short crd,Int32 x,Int32 y,Int32 z,double synVel,double synAcc,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZG0Override2WN(short core,short crd,Int32 x,Int32 y,Int32 z,double synVel,double synAcc,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_LnXYZA(short core,short crd,Int32 x,Int32 y,Int32 z,Int32 a,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZAOverride2(short core,short crd,Int32 x,Int32 y,Int32 z,Int32 a,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZAWN(short core,short crd,Int32 x,Int32 y,Int32 z,Int32 a,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZAOverride2WN(short core,short crd,Int32 x,Int32 y,Int32 z,Int32 a,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_LnXYZAG0(short core,short crd,Int32 x,Int32 y,Int32 z,Int32 a,double synVel,double synAcc,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZAG0Override2(short core,short crd,Int32 x,Int32 y,Int32 z,Int32 a,double synVel,double synAcc,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZAG0WN(short core,short crd,Int32 x,Int32 y,Int32 z,Int32 a,double synVel,double synAcc,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZAG0Override2WN(short core,short crd,Int32 x,Int32 y,Int32 z,Int32 a,double synVel,double synAcc,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_LnXYZACUVW(short core,short crd,ref Int32  pPos,short posMask,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZACUVWOverride2(short core,short crd,ref Int32  pPos,short posMask,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZACUVWWN(short core,short crd,ref Int32  pPos,short posMask,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_LnXYZACUVWOverride2WN(short core,short crd,ref Int32  pPos,short posMask,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_ArcXYR(short core,short crd,Int32 x,Int32 y,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYROverride2(short core,short crd,Int32 x,Int32 y,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYRWN(short core,short crd,Int32 x,Int32 y,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYROverride2WN(short core,short crd,Int32 x,Int32 y,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_ArcXYC(short core,short crd,Int32 x,Int32 y,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYCOverride2(short core,short crd,Int32 x,Int32 y,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYCWN(short core,short crd,Int32 x,Int32 y,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYCOverride2WN(short core,short crd,Int32 x,Int32 y,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_ArcYZR(short core,short crd,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcYZROverride2(short core,short crd,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcYZRWN(short core,short crd,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcYZROverride2WN(short core,short crd,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_ArcYZC(short core,short crd,Int32 y,Int32 z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcYZCOverride2(short core,short crd,Int32 y,Int32 z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcYZCWN(short core,short crd,Int32 y,Int32 z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcYZCOverride2WN(short core,short crd,Int32 y,Int32 z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_ArcZXR(short core,short crd,Int32 z,Int32 x,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcZXROverride2(short core,short crd,Int32 z,Int32 x,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcZXRWN(short core,short crd,Int32 z,Int32 x,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcZXROverride2WN(short core,short crd,Int32 z,Int32 x,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_ArcZXC(short core,short crd,Int32 z,Int32 x,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcZXCOverride2(short core,short crd,Int32 z,Int32 x,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcZXCWN(short core,short crd,Int32 z,Int32 x,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcZXCOverride2WN(short core,short crd,Int32 z,Int32 x,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_HelixXYRZ(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixXYRZOverride2(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixXYRZWN(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixXYRZOverride2WN(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_HelixXYCZ(short core,short crd,Int32 x,Int32 y,Int32 z,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixXYCZOverride2(short core,short crd,Int32 x,Int32 y,Int32 z,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixXYCZWN(short core,short crd,Int32 x,Int32 y,Int32 z,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixXYCZOverride2WN(short core,short crd,Int32 x,Int32 y,Int32 z,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_HelixYZRX(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixYZRXOverride2(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixYZRXWN(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixYZRXOverride2WN(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_HelixYZCX(short core,short crd,Int32 x,Int32 y,Int32 z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixYZCXOverride2(short core,short crd,Int32 x,Int32 y,Int32 z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixYZCXWN(short core,short crd,Int32 x,Int32 y,Int32 z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixYZCXOverride2WN(short core,short crd,Int32 x,Int32 y,Int32 z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_HelixZXRY(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixZXRYOverride2(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixZXRYWN(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixZXRYOverride2WN(short core,short crd,Int32 x,Int32 y,Int32 z,double radius,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_HelixZXCY(short core,short crd,Int32 x,Int32 y,Int32 z,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixZXCYOverride2(short core,short crd,Int32 x,Int32 y,Int32 z,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,double velEnd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixZXCYWN(short core,short crd,Int32 x,Int32 y,Int32 z,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_HelixZXCYOverride2WN(short core,short crd,Int32 x,Int32 y,Int32 z,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,double velEnd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]

        public static extern short GTN_BufIO(short core,short crd,short doType,ushort doMask,ushort doValue,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufDelay(short core,short crd,ushort delayTime,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufDA(short core,short crd,short chn,short daValue,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufIOWN(short core, short crd, Int16 doType, Int16 doMask, Int16 doValue, Int32 segNum, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufDelayWN(short core,short crd,Int16 delayTime,Int32 segNum,short fifo=0);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufDAWN(short core,short crd,short chn,short daValue,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLmtsOn(short core, short crd, short axis, short limitType, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLmtsOff(short core,short crd,short axis,short limitType,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufSetStopIo(short core,short crd,short axis,short stopType,short inputType,short inputIndex,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufMove(short core,short crd,short moveAxis,Int32 pos,double vel,double acc,short modal,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufGear(short core,short crd,short gearAxis,Int32 pos,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufGearPercent(short core,short crd,short gearAxis,Int32 pos,short accPercent,short decPercent,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufStopMotion(short core,short crd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufSetVarValue(short core,short crd,short pageId,ref TVarInfo pVarInfo,double value,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufJumpNextSeg(short core,short crd,short axis,short limitType,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufSynchPrfPos(short core,short crd,short encoder,short profile,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufVirtualToActual(short core,short crd,short fifo);
        
        
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdStart(short core,short mask,short option);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdStartStep(short core,short mask,short option);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdStepMode(short core,short mask,short option);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetOverride(short core,short crd,double synVelRatio);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetOverride2(short core,short crd,double synVelRatio);
        [DllImport("gxn.dll")]
        public static extern short GTN_InitLookAhead(short core,short crd,short fifo,double T,double accMax,short n,ref TCrdData pLookAheadBuf);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLookAheadSpace(short core,short crd,out Int32  pSpace,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLookAheadSegCount(short core,short crd,out Int32  pSegCount,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdClear(short core,short crd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdStatus(short core,short crd,out short  pRun,out Int32  pSegment,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetUserSegNum(short core,short crd,Int32 segNum,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetUserSegNum(short core,short crd,out Int32  pSegment,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetUserSegNumWN(short core,short crd,out Int32  pSegment,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetRemainderSegNum(short core,short crd,out Int32  pSegment,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdStopDec(short core,short crd,double decSmoothStop,double decAbruptStop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdStopDec(short core,short crd,out double pDecSmoothStop,out double pDecAbruptStop);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdLmtStopMode(short core,short crd,short lmtStopMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdLmtStopMode(short core,short crd,out short  pLmtStopMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetUserTargetVel(short core,short crd,out double pTargetVel);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetSegTargetPos(short core,short crd,out Int32  pTargetPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdPos(short core,short crd,out double pPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdVel(short core,short crd,out double pSynVel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserOn(short core,short crd,short fifo,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserOff(short core,short crd,short fifo,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserPrfCmd(short core,short crd,double laserPower,short fifo,short channel);
        
        
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserFollowMode(short core, short crd, short source, short fifo, short channel, double startPower);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserFollowRatio(short core, short crd, double ratio, double minPower, double maxPower, short fifo, short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserFollowOff(short core, short crd, short fifo, short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserFollowSpline(short core, short crd, short tableId, double minPower, double maxPower, short fifo, short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserFollowTable(short core, short crd, short tableId, double minPower, double maxPower, short fifo, short channel);

        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowMaster(short core, short crd, ref TBufFollowMaster pBufFollowMaster, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowEventCross(short core, short crd, ref TBufFollowEventCross pEventCross, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowEventTrigger(short core, short crd, ref TBufFollowEventTrigger pEventTrigger, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowStart(short core, short crd, Int32 masterSegment, Int32 slaveSegment, Int32 masterFrameWidth, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowNext(short core, short crd, Int32 width, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowReturn(short core, short crd, double vel, double acc, short smoothPercent, short fifo);


        [DllImport("gxn.dll")]
        public static extern short GTN_SetG0Mode(short core, short crd, short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetG0Mode(short core,short crd,out short  pMode);
        [DllImport("gxn.dll")]

        public static extern short GTN_SetCrdMapBase(short core,short crd,short Mapbase);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdMapBase(short core,short crd,out short  pBase);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdBufferMode(short core,short crd,  short bufferMode,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdBufferMode(short core,short crd,out short  pBufferMode,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdSegmentTime(short core,short crd,Int32 segmentIndex,out double pSegmentTime,out Int32  pSegmentNumber,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdTime(short core,short crd,out TCrdTime pTime,short fifo);

        /*-----------------------------------------------------------*/
        /* Compensate                                                */
        /*-----------------------------------------------------------*/

        [DllImport("gxn.dll")]
        public static extern short GTN_SetBacklash(short core,short axis,Int32 value,double changeValue,Int32 dir);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBacklash(short core,short axis,out Int32  pValue,out double pChangeValue,out Int32  pDir);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetLeadScrewComp(short core,short axis,short n,Int32 startPos,Int32 lenPos,ref Int32  pPositive,ref Int32  pNegative);
        [DllImport("gxn.dll")]
        public static extern short GTN_EnableLeadScrewComp(short core,short axis,short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetLeadScrewCrossComp(short core,short axis,short n,Int32 startPos,Int32 lenPos,ref Int32  pPositive,ref Int32  pNegative,short link);
        [DllImport("gxn.dll")]
        public static extern short GTN_EnableLeadScrewCrossComp(short core,short axis,short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCompensate(short core,short axis,out double pPitchError,out double pCrossError,out double pBacklashError,ref double pEncPos,ref double pPrfPos);
   
        public struct TLeadScrewPrm
        {
	        public short n;
	        public Int32 startPos;
	        public Int32 lenPos;
	        public Int32  pCompPos;
	        public Int32  pCompNeg;
        } 
 
        [DllImport("gxn.dll")]
        public static extern short GTN_SetLeadScrewTable(short core,short axis,ref TLeadScrewPrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_EnableLeadScrewTable(short core,short axis,Int32 error);
        [DllImport("gxn.dll")]
        public static extern short GTN_DisableLeadScrewTable(short core,short axis);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLeadScrewTablePrfPosCount(short core,Int32 encPos,out TLeadScrewPrm pPrm,out short  pCountPositive,out short  pCountNegative);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLeadScrewTablePrfPosPositive(short core,Int32 encPos,out TLeadScrewPrm pPrm,short index,out Int32  pPrfPosPositive);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLeadScrewTablePrfPosNegative(short core,Int32 encPos,out TLeadScrewPrm pPrm,short index,out Int32  pPrfPosNegative);


        public struct TCompensate2DTable
        {
	        public short count1;                                // 行数和列数，最小值为2
            public short count2;                                // 行数和列数，最小值为2
	        public Int32 posBegin1;                             // 起点位置
            public Int32 posBegin2;                             // 起点位置
	        public Int32 step1;                                 // 步长
            public Int32 step2;                                 // 步长
        } 

        public struct TCompensate2D
        {
	        public short enable;                                // 2D补偿使能标志
	        public short tableIndex;                            // 所使用的2D补偿表
	        public short axisType1;                             // 查表轴类型
            public short axisType2;                             // 查表轴类型
	        public short axisIndex1;                            // 查表轴索引
            public short axisIndex2;                            // 查表轴索引
        } 
       
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCompensate2DTable(short core,short tableIndex,ref TCompensate2DTable pTable,ref Int32  pData,short extend);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCompensate2DTable(short core,short tableIndex,out TCompensate2DTable pTable,out short  pExtend);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCompensate2D(short core,short axis,ref TCompensate2D pComp2d);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCompensate2D(short core,short axis,out TCompensate2D pComp2d);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCompensate2DValue(short core,short axis,out double pValue);

        /*-----------------------------------------------------------*/
        /* IO and Encoder                                            */
        /*-----------------------------------------------------------*/

        [DllImport("gxn.dll")]
        public static extern short GTN_SetDo(short core,short doType,Int32 value);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDoEx(short core, short doType, ref Int32 pValue, short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDoBit(short core,short doType,short doIndex,short value);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDo(short core,short doType,out Int32  pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDoEx(short core, short doType, out Int32 pValue, short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDoBitReverse(short core,short doType,short doIndex,short value,short reverseTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearAlarm(short core, short axis, short count, ushort delayTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDi(short core,short diType,out Int32  pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDiReverseCount(short core,short diType,short diIndex,out UInt32 pReverseCount,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDiReverseCount(short core,short diType,short diIndex,ref UInt32 pReverseCount,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDiRaw(short core,short diType,out Int32  pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDiEx(short core,short diType,out Int32  pValue,short count);
        public struct TExtModuleType
        {
            public short type;
            public short input;
            public short output;
        }

        public struct TExtModuleStatus
        {
            public short active;
            public short checkError;
            public short linkError;
            public short packageErrorCount;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtModuleType(short core, short station, short module, out TExtModuleType pModuleType);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtModuleCount(short core, short station, out short pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_WriteAo(short core,Int32 aoType,short aoIndex,ref double pValue,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDac(short core,short dac,ref short  pValue,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDac(short core,short dac,out short  pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAuDac(short core,short dac,ref short pValue,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuDac(short core, short dac, out short pValue, short count, out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAdc(short core,short adc,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAdcValue(short core,short adc,out short  pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuAdc(short core,short adc,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuAdcValue(short core,short adc,out short pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEncLineNum(short core,short encoder,out Int32 pLineNum,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEncType(short core, short encoder, out short pType, short count, out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEncPos(short core,short encoder,Int32 encPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEncPos(short core,short encoder,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEncPosPre(short core,short encoder,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEncVel(short core,short encoder,out double pValue,short count,out UInt32 pClock);
      
        
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPlsPos(short core,short encoder,Int32 encPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPlsPos(short core,short pulse,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPlsVel(short core,short pulse,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]

        public static extern short GTN_SetAuEncPos(short core,short encoder,Int32 encPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuEncPos(short core,short encoder,out double pValue,short count,out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuEncVel(short core,short encoder,out double pValue,short count,out UInt32 pClock);
      
        
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAbsEncPos(short core,short encoder,out Int32  pValue,short mode,short param);

        [DllImport("gxn.dll")]
        public static extern short GTN_InitFPGAEncoder(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFPGAEncoder(short core, short enable, Int32 intervalTime, short reverseMask);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetFPGAEncoder(short core, out Int32 pDataChn1, out Int32 pDataChn2, out Int32 pDataChn3, out Int32 pDataChn4, out Int32 pCount, out Int32 pOverFlowFlag);

        /*-----------------------------------------------------------*/
        /* ExtModule                                                 */
        /*-----------------------------------------------------------*/
        [DllImport("gxn.dll")]
        public static extern short GTN_ExtModuleInit(short core,short method);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtModuleReverse(short core, short station, short module, short inputCount,ref int pInputReverse, short outputCount, ref int pOutputReverse);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetILinkManuMode(short core, short station);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetILinkManuId(short core, short station, short autoId, out short manuId);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetILinkAutoId(short core, short station, short manuId, out short autoId);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtDoBit(short core,short doIndex,short value);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtDoBit(short core, short doIndex, out short pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtDo(short core,short doIndex,Int32 value,Int32 mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtDo(short core,short doIndex,out Int32  pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtDiBit(short core,short diIndex,out short pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtDi(short core,short diIndex,out Int32 pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtAoValue(short core,short index,out short pValue,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtAiValue(short core,short index,out short pValue,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtAo(short core,short index,ref double pValue,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtAo(short core,short index,out double pValue,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtAi(short core,short index,out double pValue,short count);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetILinkDo(short core,short station,short module,ushort data,ushort mask);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetILinkDi(short core,short station,short module,ushort data);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetILinkAo(short core,short station,short module,short channel,short data);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetILinkAi(short core,short station,short module,short channel,out short data);

        /*-----------------------------------------------------------*/
        /* Pos Compare                                               */
        /*-----------------------------------------------------------*/
        public const short POS_COMPARE_MODE_FIFO = 0;
        public const short POS_COMPARE_MODE_LINEAR = 1;
        public const short POS_COMPARE_MODE_EQUIDISTANT = 2;

        public const short POS_COMPARE_OUTPUT_PULSE = 0;
        public const short POS_COMPARE_OUTPUT_LEVEL = 1;

        public const short POS_COMPARE_SOURCE_ENCODER = 0;
        public const short POS_COMPARE_SOURCE_PULSE = 1;


        public struct TPosCompareMode
        {
	        public short mode;                                  //0：FIFO 模式，1：liner 模式，2：PSO 立即模式，3：PSO 等待到位触发模式
            public short dimension;                             //1：1D，2：2D
            public short sourceMode;                            //0：编码器； 1：脉冲计数器
            public short sourceX;                               //X 轴比较源[1,12]
            public short sourceY;                               //Y 轴比较源[1,12] 
            public short outputMode;                            //输出模式：0:脉冲 1:电平 2：电平自动翻转    
            public short outputCounter;                         //保留
            public ushort outputPulseWidth;                     //输出脉冲宽度,单位为 1us，电平模式该参数无效
            public ushort errorBand;                            //二维位置比较输出误差带
        } 

        public struct TPosCompareLinear
        {
	        public UInt32 count;                                //位置比较输出位置个数
            public ushort hso;                                  //位置比较输出hso通道的输出数值，按位表示
                                                                //脉冲模式：0 表示无输出，1 表示输出脉冲
            public ushort gpo;                                  //通用GPO通道的输出数值，按位表示
                                                                //脉冲模式：0 表示无输出，1 表示输出脉冲

            public Int32 startPos;                              //线性比较输出的起点位置
            public Int32 interval;                              //位置比较输出位置间隔
        } 

        public struct TPosCompareLinear2D
        {
	        public UInt32 count;
            public ushort hso;
            public ushort gpo;

	        public Int32 startPosX;
	        public Int32 startPosY;
	        public Int32 intervalX;
	        public Int32 intervalY;
        } 


        public struct TPosCompareData
        {
	        public Int32 pos;                                   //位置比较输出的位置
            public ushort hso;                                  //位置比较输出hso通道的输出数值,按位表示HSObit0-bit9对应
                                                                //HSO0-HSO9，bit15:表示逻辑位。在激光功能和位置比较输出
                                                                //功能复用时生效；脉冲模式：0表示无输出，1表示输出脉冲；
                                                                //电平模式：0表示无输出，1表示有输出。
            public ushort gpo;                                  //通用GPO通道的输出数值，按位表示GPO，bit0-bit15分别对应
                                                                //GPO0-GPO15。脉冲模式：0表示无输出，1表示输出脉冲；电平
                                                                //模式：0表示拉低，1表示拉高。
	        public UInt32 segmentNumber;                        //设置数据段号
        } 

        public struct TPosCompareData2D
        {
	        public Int32 posX;                                  //X轴位置比较输出的位置
            public Int32 posY;                                  //Y轴位置比较输出的位置
            public ushort hso;                                  //位置比较输出hso通道的输出数值,按位表示HSObit0-bit9对应
                                                                //HSO0-HSO9，bit15:表示逻辑位。在激光功能和位置比较输出
                                                                //功能复用时生效；脉冲模式：0表示无输出，1表示输出脉冲；
                                                                //电平模式：0表示无输出，1表示有输出。
            public ushort gpo;                                  //通用GPO通道的输出数值，按位表示GPO，bit0-bit15分别对应
                                                                //GPO0-GPO15。脉冲模式：0表示无输出，1表示输出脉冲；电
                                                                //平模式：0表示拉低，1表示拉高。
	        public UInt32 segmentNumber;                        //设置数据段号
        } 

        public struct TPosCompareStatus
        {
            public ushort mode;                                 // 0:FIFO模式；1：Linear模式
            public ushort run;                                  //1:开始，0：停止
            public ushort space;                                //FIFO剩余空间（最大为1000）
            public UInt32 pulseCount;                           //输出脉冲个数
            public ushort hso;                                  //位置比较输出hso通道的输出数值
            public ushort gpo;                                  //通用GPO通道的输出数值
            public UInt32 segmentNumber;                        //执行的段号
        } 

        public struct TPosCompareInfo
        {
            public ushort config;                               //保留
            public ushort fifoEmpty;                            //fifo跑空标志，1：表示曾经跑空，清
                                                                //空FPGA后才能清0,其他位保留
            public ushort head;                                 //保留
            public ushort tail;                                 //保留
            public UInt32 commandReceive;                       //接收指令数量
            public UInt32 commandSend;                          //发送指令数量
            public Int32 posX;                                  // 在FIFO模式下，表示用户触发的x轴位置；在PSO模式下，表示用
                                                                //户的开关PSO位置，其他模式下无意义。
	        public Int32 posY;                                  // 在FIFO模式下，表示用户触发的y轴位置；在PSO模式下，表示用
                                                                //户的开关PSO位置，其他模式下无意义。
        }
        
        public struct TPosComparePsoPrm
        {
            public UInt32 count;                                //保留
            public Int16 hso;                                   //保留
            public UInt16 gpo;                                  //保留
            public Int32 startPosX;                             //保留
            public Int32 startPosY;                             //保留
            public Int32 syncPos;                               //同步位置增量
            public Int32 time;                                  //保留
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 20)]
            public short[] reserve;                             //保留
        }


        public struct TPosCompareContinueMode
        {
            public short mode;
            public Int16 count;
            public short highLevel;
            public short lowLevel;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public short[] reserve;
        }
		
		public struct THsoPulsePrm
		{
			public short mode;                                  // 0：不输出，1：按默认设置输出，2：按照当前指令配置信息输出
			public short timeScale;                             // 时间精度：0：1us，1:0.1us
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] pad1;
			public double pulseWidth;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public double[] pad2;
		};
		
		public struct TPosComparePulse
		{
			public short outputMode;
			public short level;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] reserve1;
			public double highLevelTime;
			public double lowLevelTime;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public double[] reserve2;
		};
		
		public struct  TPosComparePulseStatus
		{
			public Int32 count;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] reserve1;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public double[] reserve2;
		};


        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalPermitEx(short core, short station, short dataType, ref short permit, short index, short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalPermitEx(short core, short station, short dataType, out short pPermit, short index, short count);
       
		[DllImport("gxn.dll")]
		public static extern short GTN_PosComparePulse(short core,short index,short outputMode,short level,UInt16 outputPulseWidth);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetPosComparePulseCount(short core,short index,Int32 count);
		[DllImport("gxn.dll")]
		public static extern short GTN_PosComparePulseEx(short core,short index,ref TPosComparePulse pPosComparePulse);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetPosComparePulseStatus(short core,short index,out TPosComparePulseStatus pPosComparePulseStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareStart(short core,short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareStop(short core,short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareClear(short core,short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareStatus(short core,short index,out TPosCompareStatus pStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareData(short core,short index,ref TPosCompareData pData);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareData(short core, short index, System.IntPtr pData);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareData2D(short core,short index,ref TPosCompareData2D pData);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareData2D(short core, short index, System.IntPtr pData);
        [DllImport("gxn.dll")]
        public static extern short GTN_Buf2DCompareData(short crd, short fifo, short chn, ref TPosCompareData2D data, short compareFifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosCompareMode(short core,short index,ref TPosCompareMode pMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosCompareMode(short core,short index,out TPosCompareMode pMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosCompareContinueMode(short core,short index,ref TPosCompareContinueMode pMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosCompareContinueMode(short core,short index,out TPosCompareContinueMode pMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosCompareLinear(short core,short index,ref TPosCompareLinear pLinear);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosCompareLinear(short core,short index,out TPosCompareLinear pLinear);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosCompareLinear2D(short core,short index,ref TPosCompareLinear2D pLinear);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosCompareLinear2D(short core,short index,out TPosCompareLinear2D pLinear);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareInfo(short core,short index,out TPosCompareInfo pInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareHsOn(short core,short index,short link,Int16 threshold);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareHsOff(short core,short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareSpace(short core,short index,out Int16 pSpace);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosComparePsoPrm(short core,short index,ref TPosComparePsoPrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosComparePsoPrm(short core,short index,out TPosComparePsoPrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosCompareStartLevel(short core,short index,short type,short startLevel);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareDataMass(short core, short index, ref TPosCompareData pData, out long pSendCount, long count);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCompareDataMass(short core, short index, System.IntPtr pData, out long pSendCount, long count);

        [DllImport("gxn.dll")]
		public static extern short GTN_SetHsoPulsePrm(short core,short station,short hsoIndex,ref THsoPulsePrm pPem,short hsoCount);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetHsoPulsePrm(short core,short station,short hsoIndex,out THsoPulsePrm pPem,short hsoCount);

        public const short COMPARE_SEND_DATA_MAX = 30;
        public const short COMPARE_DATA_MAX = 4096;
        public const Int32 COMPARE_STEP_MAX = 0x1fff;
        public const Int32 COMPARE_MAX_NUM = 0x3fffffff;

        /*-----------------------------------------------------------*/
        /* Laser and Scan                                            */
        /*-----------------------------------------------------------*/
        public const short FIFO_MODE_STATIC = 0;
        public const short FIFO_MODE_DYNAMIC = 1;

        public const short SCAN_STATUS_WAIT = 0;
        public const short SCAN_STATUS_RUN = 1;
        public const short SCAN_STATUS_DONE = 2;

        public struct TScanInit
        {
	        public int lookAheadNum;             //前瞻段数
	        public double time;                  //时间常数
	        public double radiusRatio;           //曲率限制调节参数
        }

        public struct TScanInfo
        {
	        public UInt32 segmentNumber;
            public ushort commandNumber;
            public ushort prfVel;
            public ushort fifoEmpty;
            public ushort head;
            public ushort tail;
	        public UInt32 commandReceive;
	        public UInt32 commandSend;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 6)]
	        public UInt32[] reserve;

        }

        public struct TScanPosSuperposeParameter
        {
            public short enable;
            public short superposeSrc;
            public short superposeAxisX;
            public short superposeAxisY;
            public double xCoefficient;
            public double yCoefficient;
			public double xVelCoefficient;
			public double yVelCoefficient;
        }

        public struct TLaserInfo
        {
            public ushort hso;
            public ushort powerMode;
            public ushort power;
            public ushort powerMax;
            public ushort powerMin;
            public ushort frequency;
            public ushort pulseWidth;
        } 
        public struct TLaserPowerPrm
        {
            public short n;
            public double startVel;
            public double power;
        }

        public struct TLaserPowerTable
        {
            public short n;
            public double startVel;
            public double velStep;
            public double power;
        }
        //typedef struct
        //{
        //    short corrX[65][65];
        //    short corrY[65][65];
        //}TScanCorrectionTableData;

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanInit(short core, ref TScanInit pScanInit, double jumpAcc, double markAcc, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanCrdDataEnd(short core, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanLaserLink(short core, short link, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetScanLaserLink(short core, out short pLink, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanMode(short core, short mode, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetScanMode(short core, out short pMode, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearScanStatus(short core, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanGetCrdPos(short core, out short pPos, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanJump(short core, short x, short y, double vel, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanJumpPoint(short core, short x, short y, double vel, Int32 motionDelayTime, Int32 laserDelayTime, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanTimeJump(short core, short x, short y, ushort time, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanTimeJumpPoint(short core, short x, short y, ushort time, Int32 motionDelayTime, Int32 laserDelayTime, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanMark(short core, short x, short y, double vel, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanTimeMark(short core, short x, short y, ushort time, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufLaserPrfCmd(short core, double laserPower, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufIO(short core, ushort doType, ushort doMask, ushort doValue, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufDelay(short core, Int32 time, short crd);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufDA(short core, ushort chn, short value, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufAO(short core,Int16 chn,double voltage,short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufLaserDelay(short core, short laserOnDelay, short laserOffDelay, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufLaserOutFrq(short core, double outFrq, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufSetPulseWidth(short core, ushort width, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufLaserOn(short core, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufLaserOff(short core, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanBufStop(short core, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLaserIntervalOnList(short core, Int32 time, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanDelayTime(short core, ushort maxJumpDelay, ushort markDelay, ushort multiMarkDelayConst, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanDelayMode(short core, short multiMarkDelayMode, ushort multiMarkLaserOffDelay, ushort minJumpDelay, ushort jumpDelayLengthLimit, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanStop(short core, short stopType, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanCrdSpace(short core, out short pSpace, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanCrdStart(short core, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanCrdClear(short core, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanCrdStatus(short core, out short pRun, out short pStatus, short scan);
        
        
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanPosSuperposeParameter(short core,short crd,TScanPosSuperposeParameter param);
        //[DllImport("gxn.dll")]
        //public static extern short GTN_ScanSetCorrectionTable(short core,short crd,ref TScanCorrectionTableData pParam);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanCorrectionOn(short core,short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanCorrectionOff(short core,short scan);
        //[DllImport("gxn.dll")]
        //public static extern short GTN_ScanGenerateCorrectionTable(short core,double paraX,double paraY,short rangeX,short rangeY,ref TScanCorrectionTableData pParam);

        [DllImport("gxn.dll")]
        public static extern short GTN_LaserOn(short core,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_LaserOff(short core,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_LaserOnStatus(short core,out ushort pValue,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_LaserPowerMode(short core,short laserPowerMode,double maxValue,double minValue,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_LaserPrfCmd(short core,double power,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_LaserOutFrq(short core,double outFrq,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPulseWidth(short core,ushort width,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetLevelDelay(short core,UInt32 highLevelDelay,UInt32 lowLevelDelay,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_LaserInfo(short core,out TLaserInfo pLaserInfo,short channel);

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanInfo(short core,ref TScanInfo pScanInfo,short crd);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanHsOn(short core,short crd,short link,Int16 threshold);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanHsOff(short core,short crd);
        

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLaserOn(short core,short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLaserOff(short core,short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLaserOnStatus(short core,out Int16 pValue,short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanSetLaserMode(short core,Int16 laserMode, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLaserPowerMode(short core,short laserPowerMode,double maxValue,double minValue, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLaserPrfCmd(short core,double power, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLaserOutFrq(short core,double outFrq, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanSetPulseWidth(short core,Int16 width, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanSetLevelDelay(short core, Int16 highLevelDelay, Int16 lowLevelDelay, short scan);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLaserInfo(short core,ref TLaserInfo pLaserInfo, short scan);
        /*----------------*/
        /*振镜相关指令    */
        /*----------------*/

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanInitPro(short core, short scanCrd, ref TScanLookAheadParameterPro pPrm, ref TListInfo pListInfo);

        [DllImport("gxn.dll")]
        public static extern short GTN_GetScanCrdPosPro(short core, short scanCrd, out double pPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetScanStatusPro(short core, short scanCrd, out TScanStatusPro pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLinearPro(short core, short scanCrd, short motionMode, ref TScanLinearMotionPro pPrm, ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanCircularPro(short core, short scanCrd, short motionMode, ref TScanCircularMotionPro pPrm, ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanDelayPrmPro(short core, short scanCrd, ref TScanDelayParameterPro pPrm, ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanMotionDelayPro(short core, short scanCrd, double delayTime, ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetScanExecuteTimePro(short core, short scanCrd, out double pExecuteTime);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearScanExecuteTimePro(short core, short scanCrd);

        [DllImport("gxn.dll")]
        public static extern short GTN_GetScanLaserLinkPro(short core, short scanCrd, out short pLaserChannel);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanLaserLinkPro(short core, short scanCrd, short laserChannel, ref TListInfo pListInfo);//振镜激光绑定,laserChannel为0则是解绑
        [DllImport("gxn.dll")]
        public static extern short GTN_GetScanLaserInfoPro(short core, short scanCrd, out TLaserInfoPro pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanLaserEnablePro(short core, short scanCrd, short laserEnable, ref TListInfo pListInfo);//激光开关光
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanLaserPrmPro(short core, short scanCrd, ref TLaserParameterPro pLaserPrm, ref TListInfo pListInfo);//激光模式和固定参数设置

        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanLaserPowerPro(short core, short scanCrd, double power, ref TListInfo pListInfo);//激光能量设置
        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanLaserDelayPro(short core, short scanCrd, double laserOnDelay, double laserOffDelay, ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanWobbleMotionCircularEnablePro(short core, short scanCrd, short enable, ref TScanWobbleMotionCircular pWobbleMotionCircular, ref TListInfo pListInfo);

        public const short SCAN_MOTION_MODE_NONE = 0;
        public const short SCAN_MOTION_MODE_JUMP = 1;
        public const short SCAN_MOTION_MODE_JUMP_POINT = 2;
        public const short SCAN_MOTION_MODE_JUMP_TIME = 3;
        public const short SCAN_MOTION_MODE_JUMP_TIME_POINT = 4;
        public const short SCAN_MOTION_MODE_MARK = 5;
        public const short SCAN_MOTION_MODE_MARK_TIME = 6;

        public const ushort SCAN_LASER_MODE_DUTY_RATIO = 0;//占空比模式
        public const ushort SCAN_LASER_MODE_FREQUENCY = 1;//频率模式
        public const ushort SCAN_LASER_MODE_ANALOG = 2;//模拟量模式

        /*占空比模式固定参数*/
        public struct TLaserDutyRatioModeParameterPro
        {
            public double minDutyRatio;
            public double maxDutyRatio;
            public double frequency;
        }

        /*频率模式固定参数*/
        public struct TLaserFrequencyModeParameterPro
        {
            public double minFrequency;
            public double maxFrequency;
            public double pulseWidth;
        }

        /*模拟量模式固定参数*/
        public struct TlaserAnalogModeParameterPro
        {
            public double minVoltage;
            public double maxVoltage;
        }
        /*并口模式固定参数*/
        public struct TlasetParallelModeParameterPro
        {

            public double minParallel;
            public double maxParallel;
            public int powerFollowEnable;                                // 打开或者关闭激光能量跟随,0: 关闭激光能量跟随,1: 打开激光能量跟随
            public int powerSet;                                         // 如果关闭激光能量跟随时,激光的输出能量值
        }

        /*激光各个模式固定参数*/
        public struct TLaserParameterUnionPro
        {
            public TLaserDutyRatioModeParameterPro dutyRatioModePrm;
            public TLaserFrequencyModeParameterPro frequencyModePrm;
            public TlaserAnalogModeParameterPro analogModePrm;
            public TlasetParallelModeParameterPro parallelModePrm;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public double[] data;
        }


        /*激光信息参数*/
        public struct TLaserInfoPro
        {

            public UInt16 laserOn;   //激光开关状态
            public UInt16 laserMode; //PWM输出模式，宏定义
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Int16[] pad;             //对齐
            public double power;             //激光能量
            public TLaserParameterUnionPro laserPrm;
        }

        public struct TLaserParameterPro
        {

            public UInt16 laserMode; //PWM输出模式，定义宏
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] pad;             //对齐
            public TLaserParameterUnionPro laserPrm;
        }


        /*振镜前瞻参数*/
        public struct TScanLookAheadParameterPro
        {
            public short lookAheadNum;   //前瞻段数
            public short highSpeedMode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public short[] pad;         //对齐
            public double time;          //时间常数
            public double radiusRatio;   //曲率限制调节参数
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public double[] reserve;    //保留
        }



        /*振镜状态*/
        public struct TScanStatusPro
        {
            public short run;            //振镜运动标志
            public short space;
            public ushort fifoEmpty;     //跑空标志
            public short pad1;           //对齐
            public UInt32 segmentNumber;  //执行段号
            public UInt32 commandReceive; //接收到的指令数
            public UInt32 commandSend;    //已发送的指令数
            public Int32 pad2;            //对齐
            public double prfVel;        //合成规划速度

        }


        public struct TScanCircularMotionPro
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] endPos;
            public double radius;//半径正负区分优弧劣弧
            public short dir;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] pad;//对齐
            public TScanMotionPrmUnionPro motionPrm;
        }


        public struct TScanDelayParameterPro
        {
            public short multiMarkDelayMode;
            public ushort jumpDelayLengthLimit;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public short[] pad;//对齐
            public double multiMarkLaserOffDelay;
            public double multiMarkDelayConst;
            public double markDelay;
            public double minJumpDelay;
            public double maxJumpDelay;
        }


        public struct TScanLinearMotionPro
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] pos;
            public double reserve;//保留
            public TScanMotionPrmUnionPro motionPrm;
        }

        public struct TScanMotionPrmUnionPro
        {
            public TVelModePro velMode;
            public TTimeModePro timeMode;
            public TVelPointModePro velPointMode;
            public TTimePointModePro timePointMode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public double[] data;
        }

        public struct TVelModePro
        {
            public double acc;
            public double dec;
            public double vel;
        }

        public struct TTimeModePro
        {
            public double acc;
            public double dec;
            public ulong time;
            public long pad;//对齐
        }

        public struct TVelPointModePro
        {
            public double acc;
            public double dec;
            public double vel;
            public ulong motionDelayTime;
            public ulong laserDelayTime;
        }

        public struct TTimePointModePro
        {
            public double acc;
            public double dec;
            public ulong time;
            public ulong motionDelayTime;
            public ulong laserDelayTime;
            public long pad;//对齐
        }
        public struct TScanWobbleMotionCircular
        {

            public double radius;
            public short dir;
            public short velMode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public short[] reserve;    //保留
            public double velValue;
    }
    /*-----------------------------------------------------------*/
    /* 无限视野                                                  */
    /*-----------------------------------------------------------*/
    public struct TScanInfiniteView
        {
            public short superposeSource;
            public short tableAxisX;
            public short tableAxisY;
            public short reserve1;
            public double scanScale;
            public double tableScale;
            //public double tableScaleY;
            public double tableAccMax;
            public double tableVelMax;
            public double tableMotionRatio;
            public double reserve2;
            public double reserve3;
            public double reserve4;
            public double reserve5;
        }



        public struct TScanDisableInfiniteView
        {
            public double reserve1;
            public double reserve2;
            public double reserve3;
            public double reserve4;
        }


        [DllImport("gxn.dll")]
        public static extern short GTN_ScanEnableInfiniteView(short core, short scanCrd, ref TScanInfiniteView pScanInfiniteView, string pFileName, ref TListInfo pListInfo);

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanDisableInfiniteView(short core, short scanCrd, ref TScanDisableInfiniteView pScanInfiniteView, ref TListInfo pListInfo);


        [DllImport("gxn.dll")]
        public static extern short GTN_ScanLoadInfiniteViewFile(short core, short scanCrd, string pFileName);


        [DllImport("gxn.dll")]
        public static extern short GTN_ScanSendInfiniteViewData(short core, short scanCrd, double time, out double pTime, out double pMotionTime);


        /*-----------------------------------------------------------*/
        /* 无限视野                                                  */
        /*-----------------------------------------------------------*/
        public struct TScanIvInitParameter
        {

            public short list;                      // 无限视野使用的指令流号
            public short superposeSource;           // 振镜位置叠加源，编码器/脉冲计数器

            public short superposeReserveX;  // 振镜位置叠加方向取反  1：取反  0：正常      
            public short superposeReserveY;  // 振镜位置叠加方向取反  1：取反  0：正常     

            public short tableAxisX;                // 振镜X轴叠加轴号
            public short tableAxisY;                // 振镜Y轴叠加轴号

            public short reserve4;// 保留参数，需要设置为0
            public short reserve5;// 保留参数，需要设置为0

            public double scanScale;                // 振镜物理当量，单位：bit/mm
            public double tableAxisXScale;          // 工作台X轴物理当量，当量和Y轴应保持一致，单位：pulse/mm
            public double tableAxisYScale;          // 工作台Y轴物理当量，当量和X轴应保持一致，单位：pulse/mm
            public double scanViewRange;            // 振镜视野加工范围，单位：mm，比如设置4mm的话，振镜视野就是正负2mm
            public double tableAccMax;              // 叠加轴运动最大合成加速度
            public double tableVelMax;              // 叠加轴运动最大合成速度
            public double tableMotionRatio;         // 叠加轴运动系数，给定100

            public double reserve0;              // 保留参数，需要设置为0
            public double reserve1;              // 保留参数，需要设置为0
            public double reserve2;              // 保留参数，需要设置为0
            public double reserve3;              // 保留参数，需要设置为0
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvInit(short core, short scanCrd, ref TScanIvInitParameter pScanIvInitPrm);

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvClear(short core, short scanCrd);

        public struct TScanIvCaculateStatus
        {

            public short process;
            public short errorCode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public short[] pad;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvCaculate(short core, short scanCrd, string pFileName, ref TScanIvCaculateStatus pScanIvCaculateStatus);

        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvSend(short core, short scanCrd, string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvStart(short core, short scanCrd);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvReset(short core, short scanCrd);
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvStop(short core, short scanCrd, short stopMode);

        public struct TScanIvInfo
        {

            public short run;                // 无限视野运动状态
            public short scanStatus;         // 无限视野振镜运动状态
            public short tableStatus;        // 无限视野工作台运动状态
            public short fifoEmpty;          // 无限视野缓冲区跑空标志，bit0：振镜跑空标志，bit1：工作台跑空标志
            public short stopInfo;           // 无限视野停止信息
            public short reserve1;           // 保留参数
            public Int32 scanCmdCount;        // 无限视野底层数据存储剩余空间
            public Int32 tableCmdCount;       // 无限视野底层数据存储剩余空间
            public Int32 segNum;              // 无限视野运动段号
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Int32[] reserve2;         // 保留参数
        }
        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvInfo(short core, short scanCrd, out TScanIvInfo pScanIvInfo);



        public struct TScanIvLaserPowerFollowPrm
        {
            public short laserMode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] pad;
            public double laserPrePrm;
            public double ratio;
            public double powerMaxLimit;
            public double powerMin;

        }



        [DllImport("gxn.dll")]
        public static extern short GTN_ScanIvLaserFollowMode(short core, short scanCrd, ref TScanIvLaserPowerFollowPrm pScanIvLaserPowerFollowPrm);

        /*-----------------------------------------------------------*/
        /* DLM                                                       */
        /*-----------------------------------------------------------*/
        public const short DLM_FUNCTION_EVENT = 0;
        public const short DLM_FUNCTION_TIMER = 1;
        public const short DLM_FUNCTION_BACKGROUND = 2;
        public const short DLM_FUNCTION_COMMAND	= 3;

        public const short DLM_FUNCTION_PROCEDURE = 7;

        public const short DLM_FUNCTION_PROFILE_EVENT = 8;
        public const short DLM_FUNCTION_PROFILE	= 9;
        public const short DLM_FUNCTION_PROFILE_SUPERIMPOSED = 10;
        public const short DLM_FUNCTION_PROFILE_FILTER = 11;

        public const short DLM_FUNCTION_SERVO_EVENT = 16;
        public const short DLM_FUNCTION_SERVO = 17;
        public const short DLM_FUNCTION_SERVO_SUPERIMPOSED = 18;
        public const short DLM_FUNCTION_SERVO_FILTER = 19;

        public const short DLM_LOAD_MODE_NONE = 0;
        public const short DLM_LOAD_MODE_COMMAND = 1;
        public const short DLM_LOAD_MODE_BOOT = 2;
        public const short DLM_LOAD_MODE_RUN = 3;

        public struct TDlmStatus
        {
	        public Int32 version;
	        public Int32 date;
	        public short enable;
	        public Int32 function;
        } 

        public struct TDlmFunction
        {
	        public short function;
	        public short enable;
	        public Int32 value;
        } 
       
        [DllImport("gxn.dll")]
        public static extern short GTN_LoadDlm(short core,Int32 vender,Int32 module,string fileName,out short  pId);
        [DllImport("gxn.dll")]
        public static extern short GTN_ProgramDlm(short core,short id,short loadMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDlmLoadMode(short core,short id,out short  pLoadMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_RunDlm(short core,short id);
        [DllImport("gxn.dll")]
        public static extern short GTN_StopDlm(short core,short id);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDlmStatus(short core,short id,out TDlmStatus pStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDlmFunction(short core,short id,ref TDlmFunction pFunction);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDlmFunction(short core,short id,out TDlmFunction pFunction);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandInit(short core,short code,Int32 index);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandAdd16(short core,short value);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandAdd32(short core,Int32 value);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandAddFloat(short core,float value);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandAddDouble(short core,double value);
        [DllImport("gxn.dll")]
        public static extern short GTN_SendDlmCommand(short core,short id,out short  pReturnValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandGet16(short core,out short  pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandGet32(short core,out Int32  pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandGetFloat(short core,ref float pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_DlmCommandGetDouble(short core,out double pValue);

        /*-----------------------------------------------------------*/
        /* Event-Task                                                */
        /*-----------------------------------------------------------*/
        public const short TASK_SET_DO_BIT = 0x1101;
        public const short TASK_SET_DAC	= 0x1120;

        public const short TASK_STOP = 0x1303;

        public const short TASK_UPDATE_POS = 0x2002;
        public const short TASK_UPDATE_VEL = 0x2004;

        public const short TASK_PT_START = 0x2306;
        public const short TASK_PVT_START = 0x2346;
        public const short TASK_MOVE_ABSOLUTE = 0x2500;

        public const short TASK_GEAR_START = 0x3005;

        public const short TASK_FOLLOW_START = 0x310A;
        public const short TASK_FOLLOW_SWITCH = 0x310B;

        public const short TASK_CRD_START = 0x4004;
        public const short TASK_SCAN_START = 0x4102;

        public const short TASK_SET_DO_BIT_MODE_NONE = 0;
        public const short TASK_SET_DO_BIT_MODE_TIME = 10;
        public const short TASK_SET_DO_BIT_MODE_DISTANCE = 20;



        public struct   TTaskSetDoBit
        {
	        public short doType;
	        public short doIndex;
	        public short doValue;
	        public short mode;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 8)]
	        public Int32[] parameter;
        } 

        public struct  TTaskSetDac
        {
	        public short dac;
	        public short value;
        } 

        public struct  TTaskStop
        {
	        public Int32 mask;
	        public Int32 option;
        } 

        public struct  TTaskFifoOperation
        {
	        public short type;
	        public short index;
	        public short operation;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 20)]
	        public short[] data;
        } 

        public struct  TTaskUpdatePos
        {
	        public short profile;
	        public Int32 pos;
        }

        public struct TTaskUpdateDistance
        {
            public short profile;
            public short triggerIndex;
            public Int32 distance;
        } 
        public struct  TTaskUpdateVel
        {
	        public short profile;
	        public double vel;
        } 

        public struct  TTaskPtStart
        {
	        public Int32 mask;
	        public Int32 option;
        } 

        public struct  TTaskPvtStart
        {
	        public Int32 mask;
        } 

        public struct  TTaskGearStart
        {
	        public Int32 mask;
        } 

        public struct TTaskFollowStart
        {
	        public Int32 mask;
	        public Int32 option;
        } 

        public struct TTaskFollowSwitch
        {
	      public Int32 mask;
        } 

        public struct  TTaskMoveAbsolute
        {
	        public short profile;
	        public Int32 pos;
	        public double vel;
	        public double acc;
	        public double dec;
	        public short percent;
        } 

        public struct  TTaskCrdStart
        {
	        public short mask;
	        public short option;
        } 

        public struct  TTaskScanStart
        {
	        public short core;
	        public short index;
	        public short count;
        } 

        public struct  TEvent
        {
	        public UInt32 loop;
	        public TWatchVar var;
            public ushort condition;
	        public double value;
        } 
       
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearEvent(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearTask(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearEventTaskLink(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_AddTask(short core,short taskType, IntPtr pTaskData, out short pTaskIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_AddEvent(short core,ref TEvent pEvent,out short  pEventIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_AddEventTaskLink(short core,short eventIndex,short taskIndex,out short  pLinkIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEventCount(short core,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEvent(short core,short eventIndex,out TEvent pEvent);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEventLoop(short core,short eventIndex,out UInt32 pEventLoop);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTaskCount(short core,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEventTaskLinkCount(short core,out short  pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEventTaskLink(short core,short linkIndex,out short  pEventIndex,out short  pTaskIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_EventOn(short core,short eventIndex,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_EventOff(short core,short eventIndex,short count);
       
        /*--------- -------------------------------------------------*/
        /* Gantry                                                    */
        /*-----------------------------------------------------------*/
        public const short GANTRY_MODE_NONE = -1;
        public const short GANTRY_MODE_OPEN_LOOP_GANTRY = 1;
        public const short GANTRY_MODE_DECOUPLE_POSITION_LOOP = 2;

        [DllImport("gxn.dll")]
        public static extern short GTN_SetGantryMode(short core, short group, short master, short slave, short mode, Int32 syncErrorLimit);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetGantryMode(short core, short group,out short pMaster,out short pSlave,out short pMode, out Int32 pSyncErrorLimit);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetGantryPid(short core, short group, ref TPid pGantryPid, ref TPid pYawPid);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetGantryPid(short core, short group, out TPid pGantryPid,out TPid pYawPid);
        [DllImport("gxn.dll")]
        public static extern short GTN_GantryAxisOn(short core, short group);
        [DllImport("gxn.dll")]
        public static extern short GTN_GantryAxisOff(short core, short group);


        public struct TSecondOrderFilterPara
        {
            public short active;

            public double a0;
            public double a1;
            public double a2;

            public double b1;
            public double b2;
        }

        public const short TYPE_CONTROL_OUTPUT_FILTER = 1;
        public const short TYPE_YAW_CONTROL_OUTPUT_FILTER = 2;
        public const short FILTER_COUNT_MAX = 2;

        [DllImport("gxn.dll")]
        public static extern short GTN_SetSecondOrderFilterPara(short core, short control, short type, short index, ref TSecondOrderFilterPara pFilterPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetSecondOrderFilterPara(short core, short control, short type, short index, out TSecondOrderFilterPara pFilterPrm);

        public struct OCA_CTRL_GEN
        {
            public short s16Gain;
            public short s16Offset;
            public double dFrq;
            public Int32 u32RunPeriod;
            public short u16SigType;
        }
        
        public struct TExcitationPara
        {
            public short gain;
            public short offset;
            public short frq;		// hz
            public double runTime;	// s
        }

        public struct TExcitationTrpPara
        {
            public double vel;
            public double acc;
            public double dec;
            public double runTime;	// s
        }


        public const short NO_EXCIT = 0xff;
        public const short OPEN_LOOP_EXCIT = 1;
        public const short SIGNAL_TYPE_STEP = 3;
        public const short SIGNAL_TYPE_SIN = 0x10;
        public const short SIGNAL_TYPE_TRAP = 0x11;
        public const short SIGNAL_TYPE_NOTHING = 0xFF;
       
        [DllImport("gxn.dll")]
        public static extern short GTN_SetGenerator(short core,ref OCA_CTRL_GEN pGenStr);
       [DllImport("gxn.dll")]
        public static extern short GTN_StepResponse(short core,short control,short gain,double time);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExctLoopMode(short core,short control,short exciteLoopMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExctLoopMode(short core,short control,out short pExciteLoopMode);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetExcitation(short core,short axis,short objectValue,short type,short pParameter);

        /*-----------------------------------------------------------*/
        /* DMA                                                       */
        /*-----------------------------------------------------------*/
      
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdHsOn(short core, short crd, short fifo, short link, Int16 threshold, short lookAheadInMc);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdHsOff(short core,short crd,short fifo);

        /*-----------------------------------------------------------*/
        /* Others                                                    */
        /*-----------------------------------------------------------*/
        [DllImport("gxn.dll")]
        public static extern short GTN_SetRetainValue(short core, UInt32 address, short count, ref short pData);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetRetainValue(short core, UInt32 address, short count, out short pData);


        [DllImport("gxn.dll")]
        public static extern short GTN_SetGPIOConfig(short core, short effectiveLevel, short direction);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPrfTorque(short core, short axis, short prfTorque);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAtlTorque(short core, short axis, out short atlTorque);
        [DllImport("gxn.dll")]
        public static extern short GTN_PosCurrFeedForward(short core, short profile, double pos, Int32 gtime, short torque, short gtype, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetMotionMode(short core, short axis, short motionMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMotionMode(short core, short axis, out short motionMode);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetProfileTime(short core, Int32 profileTime, Int32 delay, Int32 stepCoef);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetProfileTime(short core, out Int32 pProfileTime, out Int32 pDelay, out Int32 pStepCoef);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCoreTime(short core, Int32 profileTime, Int32 delay, Int32 stepCoef);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCoreTime(short core, out Int32 pProfileTime, out Int32 pDelay, out Int32 pStepCoef);

        public struct TControlInfo
        {
            public double refPos;
            public double refPosFilter;
            public double refPosFilter2;
            public double cntPos;
            public double cntPosFilter;

            public double error;
            public double refVel;
            public double refAcc;

            public short value;
            public short valueFilter;

            public short offset;
        }
        
        public struct TCommandCount
        {
	        public UInt32 notify;
	        public UInt32 receive;
	        public UInt32 execute;
	        public UInt32 retry;
	        public UInt32 receiveError;
            public UInt32 echo;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_ClearCommandCount(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCommandCount(short core,out TCommandCount pCommandCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetServoTime(short core, Int32 servoTime, Int32 delay, Int32 stepCoef);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetServoTime(short core, out Int32 pServoTime, out Int32 pDelay, out Int32 pStepCoef);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetControlInfo(short core, short control, out TControlInfo pControlInfo);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetLongVar(short core, short index, Int32 value);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLongVar(short core, short index, out Int32 pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDoubleVar(short core, short index, double value);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDoubleVar(short core, short index, out double pValue);
        
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBufWaitDiStatus(short core,short crd,out short pDiType,out Int16 pDiIndex,out Int16 pLevel, out Int16 pContinueTime,out Int32 pOverTime,out short pFlagMode,out Int32 pSegNum,out short pEnable,out Int32 pCount,short fifo);
        [DllImport("gxn.dll")]
         public static extern short GTN_GetBufWaitLongVarStatus(short core,short crd,out short pIndex,out Int32 pValue,out short pFlagMode,out Int32 pSegNum,out short pEnable,out short pStatus,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearBufWaitStatus(short core,short crd,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufWaitDi(short core, short crd, short diType, Int16 diIndex, Int16 level, short continueTime, Int32 overTime, short flagMode, Int32 segNum, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufWaitLongVar(short core, short crd, short index, Int32 value, Int32 overTime, short flagMode, Int32 segNum, short fifo);
        
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisMotionSmooth(short core,short axis,double time,double k);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisMotionSmooth(short core,short axis,out double pTime,out double pK);
        
        [DllImport("gxn.dll")]
        public static extern short GTN_BufDoBit(short core,short crd,Int16 doType,Int16 index,short value,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufDoBitDelay(short core, short crd, Int16 doType, Int16 index, short value, Int32 delayTime, short fifo);
        
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdJerk(short core,short crd,double jerkMax);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdJerk(short core,short crd,out double pJerkMax);
      
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdJerkTime(short core,short crd,double jerkTime,double coef);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdJerkTime(short core,short crd,out double pJerkTime,out double pCoef);

        public struct TCrdSmooth
        {
            public short percent;
            public short accStartPercent;
            public short decEndPercent;
            public double reserve;
        }
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdSmooth(short core, short crd, ref TCrdSmooth pCrdSmooth);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdSmooth(short core, short crd, out TCrdSmooth pCrdSmooth);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdSmoothTime(short core, short crd, short smoothType, ref double pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdSmoothTime(short core, short crd, out short pSmoothType, out double pPrm);

        //////////////////////////////////////////////////////////////////////////
        //Standard Home
        //////////////////////////////////////////////////////////////////////////
        
        public const short STANDARD_HOME_STAGE_IDLE = 0;                    //未启动回原点
        public const short STANDARD_HOME_STAGE_START = 1;                   //启动回原点
        public const short STANDARD_HOME_STAGE_SEARCH_HOME = 20;            //正在搜索Home
        public const short STANDARD_HOME_STAGE_SEARCH_INDEX   = 30;         //正在搜索Index
        public const short STANDARD_HOME_STAGE_GO_HOME = 80;                //正在运动到原点
        public const short STANDARD_HOME_STAGE_END = 100;                   //回原点结束
        public const short STANDARD_HOME_STAGE_START_CHECK = -1;            //启动回原点前自检
        public const short STANDARD_HOME_STAGE_CHECKING = -2;               //自检中

        public const short STANDARD_HOME_ERROR_NONE = 0;                    //未发生错误
        public const short STANDARD_HOME_ERROR_DISABLE = 10;                //执行回原点的轴未使能
        public const short STANDARD_HOME_ERROR_ALARM = 20;                  //执行回原点的轴报警
        public const short STANDARD_HOME_ERROR_STOP = 30;                   //未完成回原点，被停止运动
        public const short STANDARD_HOME_ERROR_ON_LIMIT = 40;               //触发了限位无法继续
        public const short STANDARD_HOME_ERROR_NO_HOME = 50;                //未找到Home
        public const short STANDARD_HOME_ERROR_NO_INDEX = 60;               //未找到Index
        public const short STANDARD_HOME_ERROR_NO_LIMIT = 70;               //未找到限位
        public const short STANDARD_HOME_ERROR_ENCODER_DIR_SCALE = -1;      //规划器与编码器方向方向相反或者当量不一致
        
        public struct TStandardHomePrm
        {
            public short mode;		                              // 回原点模式取值范围1~36
            public double highSpeed;                              // 搜索Home的速度，单位pulse/ms
            public double lowSpeed;	                              // 搜索Index的速度，单位pulse/ms
            public double acc;		                              // 回零加速度，单位pulse/ms^2
            public Int32 offset;                                  // 回零偏移量，单位pulse
            public short check;                                   // 是否启用自检功能，1-启用，其它值-不启用
            public short autoZeroPos;                             // 回零完毕是否自动清零，1-自动清零，其它值-不清零
            public Int32 motorStopDelay;                          // 电机到位延时，单位：控制周期
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short pad1;	                                  // 保留（不需要设置）
        }
        
        public struct TStandardHomeStatus
        {
	        public short run;           // 是正在进行回原点，0—已停止运动，1-正在回原点
	        public short stage;         // 回原点运动的阶段
	        public short error;         // 回原点过程的发生的错误
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] pad1;        // 保留（无具体含义）
            public Int32 capturePos;    // 捕获到Home或Index时刻的编码器位置
	        public Int32 targetPos;     // 需要运动到的目标位置（原点位置或者原点位置+偏移量），在搜索Limit时或者搜索Home或Index时，设置的搜索距离为0，那么该值显示为805306368
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_ExecuteStandardHome(short core, short axis, ref TStandardHomePrm pHomePrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetStandardHomePrm(short core, short axis, out TStandardHomePrm pHomePrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetStandardHomeStatus(short core, short axis, out TStandardHomeStatus pHomeStatus);

        public struct TLaserOnOffCount
        {
            public UInt32 onCount;
            public UInt32 offCount;
            public UInt32 onCountInFpga;
            public UInt32 offCountInFpga;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public UInt32[] pad;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_GetLaserOnOffCount(short core, short channel, out TLaserOnOffCount pLaserCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearLaserOnOffCount(short core, short channel);

        [DllImport("gxn.dll")]
        public static extern short GTN_BufPosComparePsoPrm(short core, short crd, short index, ref TPosComparePsoPrm pPrm, short fifo);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisInputShaping(short core,short axis,short enable,short count,double k);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetGantrySynchErrorCompensate2DTable(short core,short tableIndex,ref TCompensate2DTable pTable,ref Int32 pData,short extend);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetGantrySynchErrorCompensate2D(short core,short group,ref TCompensate2D pComp2d);

        public struct TLaserFollowPrm
        {
            public short laserFollowMode;
            public double maxFrq;
            public double minFrq;
            public double maxDuty;
            public double minDuty;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public short[] pad1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public double[] pad2;
        };
        [DllImport("gxn.dll")]
        public static extern short GTN_SetLaserFollowMode(short core, ref TLaserFollowPrm pPrm, short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetLaserFollowTable(short core,short tableId,Int32 n,ref double pVel,ref double pPower,short channel);
        
        [DllImport("gxn.dll")]
        public static extern short GTN_LaserFollowOff(short core,short crd,short fifo,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetLaserOnOffSmooth(short core,short crd,short fifo,short channel,short mask);

        public struct TBufWaitDiStatusEx
        {
	        public short type;
	        public short enable;
	        public short flagMode;
	        public short diType;
	        public short diIndex;
	        public short diValue;
	        public ushort continueTime;
	        public ushort trigDelay;
	        public UInt32 overTime;
	        public UInt32 counter;
	        public Int32 longVar;
	        public Int32 segNum;
	        public short stop;
	        public short overTimeStop;
	        public short pad11;
	        public short pad12;
        };
        [DllImport("gxn.dll")]
        public static extern short GTN_GetBufWaitDiStatusEx(short core,short crd,short fifo,out TBufWaitDiStatusEx pStatus);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosCompareFifoMode(short core, short index, short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosCompareFifoMode(short core,short index,out short pMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosCompareLatchValue(short core,short index,Int32 count,out Int32 pDataX,out Int32 pDataY,out Int32 pCount,out TLatchValueInfo pInfo);
        
        public struct TTaskMoveEscape
        {
	        public UInt32 profileMask;
	        public Int32 offset;
	        public double vel;
	        public double acc;
        };

        public struct TEventStatus
        {
	        public short eventHit;
	        public short pad11;
	        public short pad12;
	        public short pad13;
	        public Int32 pad21;
	        public Int32 pad22;
	        public double pad31;
	        public double pad32;
        };

        public struct TaskStatus
        {
	        public short start;
	        public short execute;
	        public short pad11;
	        public short pad12;
	        public Int32 pad21;
	        public Int32 pad22;
	        public double pad31;
	        public double pad32;
        };
                
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEventStatus(short core,short index,out TEventStatus pStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTaskStatus(short core,short index,out TaskStatus pStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTriggerMoveEscape(short core,short trigger,ref TTaskMoveEscape pPrm);

        [DllImport("gxn.dll")]
        public static extern short GTN_BufPosCompareStart(short core, short crd, short fifo, short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufPosCompareStop(short core, short crd, short fifo, short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufPosCompareStartEx(short core, short crd, short fifo, short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdBufCommandDelay(short core, short crd, short fifo, short enable);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufPosCompareStopEx(short core, short crd, short fifo, short index);
       
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdHsPrm(short core,short crd,short fifo,out short pEnable,out short pLink,out UInt16 pThreshold,out short pLookAheadInMc);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetPrfPosEx(short core,short profile,double pos);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEncPosEx(short core,short encoder,double pos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDiBit(short core,short diType,short diIndex,out short pValue);

        public struct TMpgInfo
        {
            public double pos;
            public double vel;
            public double reserve1;
            public double reserve2;
            public Int32 di;
            public Int32 reserve1_1;
            public Int32 reserve1_2;
            public Int32 reserve1_3;
        };

        [DllImport("gxn.dll")]
        public static extern short GTN_ReadMpgInfo(short core,short mpg,out TMpgInfo pMpgInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_WriteMpgPos(short core,short mpg,ref double pPos,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_ReadAuEncPos(short core,short encoder,out double pPos,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_WriteAuEncPos(short core,short encoder,ref double pPos,short count);
  
        [DllImport("gxn.dll")]
        public static extern short GTN_DeleteEvent(short core,short eventIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_DeleteTask(short core,short taskIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_DeleteEventTaskLink(short core,short linkIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEvent(short core,ref TEvent pEvent,short eventIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTask(short core,short taskType,IntPtr pTaskData,short taskIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEventTaskLink(short core,short eventIndex,short taskIndex,short linkIndex);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearMcStatus(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetMcInfo(short core,Int32 info,Int32 index,UInt32 data);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMcInfo(short core,Int32 info,Int32 index,out UInt32 cpData);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetRNMasterInfo(short core,out UInt16 pPhyId,out UInt16 pType,out UInt16 pInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetRNMasterInfo(short core,UInt16 phyId,UInt16 type,UInt16 info);
        [DllImport("gxn.dll")]
        public static extern short GTN_RN_MltPcPduRd(short core,string pData, Byte des_id,UInt16 byte_start_offset,UInt16 byte_num);
        [DllImport("gxn.dll")]
        public static extern short GTN_RN_MltPcPduRdUpdate(short core,Byte des_id);
        [DllImport("gxn.dll")]
        public static extern short GTN_RN_MltPcPduWr(short core,String pData, Byte des_id, UInt16 byte_start_offset,UInt16 byte_num);
        [DllImport("gxn.dll")]
        public static extern short GTN_RN_MltPcPduWrUpdate(short core,Byte des_id);

        public struct TLaserStatus
        {
            public short run;
            public short mode;
            public double power;
            public double frequency;
            public double pulseWidth;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 9)]
            public double[] pad1;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 8)]
            public short[] pad2;
        } ;
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLaserStatus(short core,short channel,out TLaserStatus pStatus);

        /*-----------------------------------------------------------*/
        /* 新架构                                                    */
        /*-----------------------------------------------------------*/

		public struct TListInfo
		{
			public short list;	
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
			public short[] reserve1;		
			public short modal;	
			public Int32 segNum;	
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 3)]
			public short[] reserve2;	
	        [MarshalAs(UnmanagedType.ByValArray,SizeConst = 4)]
			public short[] reserve3;	
		}

        public struct  TCommandListStatus
        {
	        public short execute;
	        public short empty;
	        public short stopInfo;
	        public short reserve1;
	        public short motionDone;
	        public short commandType;
	        public short command;
	        public short direction;
	        public Int32 executeSegNum;
	        public Int32 remainderSegCount;
	        [MarshalAs(UnmanagedType.ByValArray,SizeConst = 6)]
	        public Int32[] reserve2;
        };
        
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCommandListSpace(short core,short list,out Int32 pSpace);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCommandListStatus(short core,short list,out TCommandListStatus pStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearCommandListData(short core,short list,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_CommandListDataEnd(short core,short list);
        [DllImport("gxn.dll")]
        public static extern short GTN_StartCommandList(short core,short list,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_StartMultiCommandList(short core,UInt32 mask,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_StopCommandList(short core,short index,short stopMode,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_StopMultiCommandList(short core,UInt32 mask,short stopMode,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearCommandListStatus(short core, short list);

        public const short WAIT_TIMEOUT_MODE_INFINITY = 0;
        public const short WAIT_TIMEOUT_MODE_SKIP = 1;
        public const short WAIT_TIMEOUT_MODE_STOP = 2;
        
        public struct TWatchCondition
        {
	        public TWatchVar var;
	        public UInt16 condition;
	        public double value;
        } ;

        public struct TVarCalculate
        {
	        public UInt16 operation;
	        public UInt16 varType;
	        public UInt16 result;
	        public UInt16 lhs;
	        public UInt16 rhs;
        } ;

        public struct TVarCondition
        {
	        public short varIndex;
	        [MarshalAs(UnmanagedType.ByValArray,SizeConst = 3)]
	        public short[] reserve;
	        public TWatchCondition watchCondition;
        } ;

        public struct TWaitTimeout
        {
	        public Int32 time;				// 超时时间
	        public short mode;		        // 超时后的行为，0：无限等待，1：跳过当前等待操作继续执行指令流，2：停止指令流
	        public short reserve1;
	        [MarshalAs(UnmanagedType.ByValArray,SizeConst = 4)]
	        public double[] reserve2;
        } ;
        
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVarBoolCondition(short core,short varIndex,ref TWatchCondition pWatchCondition);
        [DllImport("gxn.dll")]
        public static extern short GTN_LoadVarCalculate(short core,ref TVarCalculate pVarCalculate,short count,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_MoveTrap(short core,short profile,double pos,double vel,ref TTrapPrm pPrm,ref TListInfo pList);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVarCondition(short core,ref TVarCondition pVarCondition,short count,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_WaitForCondition(short core,ref TWatchCondition pWatchCondition,short conditionCount,short operation,ref TWaitTimeout pTimeout,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetWaitForCondition(short core,short list,out TWatchCondition pWatchCondition,out short pConditionCount,out short pOperation,out TWaitTimeout pTimeout,out short pConditionResult,out short pConditionDone);
 
        public static short DIGITAL_OUTPUT_MODE_NORMAL = 0;
        public static short DIGITAL_OUTPUT_MODE_REVERSE_TIME = 10;

        public struct TDoReverseParameter
        {
	        public double time;
	        [MarshalAs(UnmanagedType.ByValArray,SizeConst = 9)]
	        public double[] reserve;
        };

        public struct TDigitalOutputModeStruct
        {
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 10)]
            public double[] data;
        };

        //联合体的定义，但是此处加了屏蔽的代码，软件运行起来会崩溃，因此暂时不加,暂时不影响使用
        [StructLayout(LayoutKind.Explicit)]
        public struct TDigitalOutputMode
        {
            [FieldOffset(0)]
            public TDoReverseParameter doReverse;
            //[FieldOffset(0)]
            //[MarshalAs(UnmanagedType.ByValArray,SizeConst = 10)]
            //public double[] data;
        };

        public struct TDigitalOutputBit
        {
            public short mode;              //Do输出模式
            public short doType;
            public short doIndex;
            public short doValue;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 4)]
            public short[] reverse1;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
            public Int32[] reserve;
            public TDigitalOutputMode prm;
        };

        public struct TDelay
        {
            public double delayTime;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 4)]
            public short[] reserve1;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
            public Int32[] reserve2;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
            public double[] reserve3;
        };
        [DllImport("gxn.dll")]
        public static extern short GTN_WriteDigitalOutputBit(short core, ref TDigitalOutputBit pDoBit,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDelay(short core, ref TDelay pDelay, ref TListInfo pListInfo);

        public struct TProfileScale
        {
            public short[]  count;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 3)]
            public short[]  reverse1;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 4)]
            public double[] alpha;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 4)]
            public double[] beta;
        };

        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisScale(short core, short profile, ref TProfileScale pScale,ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisScale(short core, short profile,out TProfileScale pScale);

        [DllImport("gxn.dll")]
        public static extern short GTN_GetTrapSts(short core, short profile, out short prfsts);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearTrapSts(short core, short profile);
        
		[DllImport("gxn.dll")]
		public static extern short GTN_SetMcMode(short core,short mode,short value);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetMcMode(short core,short mode,out short pValue);

        /*-----------------------------------------------------------*/
        /* New Watch  Code                                           */
        /*-----------------------------------------------------------*/
        public const short WATCH_MODE_STATIC = 0;
        public const short WATCH_MODE_LOOP = 1;
        public const short WATCH_MODE_DYNAMIC = 2;

        public const short WATCH_MODE_STATIC_BACKGROUND = 10;
        public const short WATCH_MODE_LOOP_BACKGROUND = 11;
        public const short WATCH_MODE_DYNAMIC_BACKGROUND = 12;

        public const short WATCH_EVENT_RUN = 1;
        public const short WATCH_EVENT_START = 10;
        public const short WATCH_EVENT_STOP = 20;
        public const short WATCH_EVENT_OFF = 30;

        public const short WATCH_CONDITION_EQ = 1;
        public const short WATCH_CONDITION_NE = 2;
        public const short WATCH_CONDITION_GE = 3;
        public const short WATCH_CONDITION_LE = 4;

        public const short WATCH_CONDITION_CHANGE_TO = 11;
        public const short WATCH_CONDITION_CHANGE = 12;
        public const short WATCH_CONDITION_UP = 13;
        public const short WATCH_CONDITION_DOWN = 14;

        public const short WATCH_CONDITION_REMAIN_AT = 21;
        public const short WATCH_CONDITION_REMAIN = 22;

        public const long WATCH_VAR_CLOCK = 1200;
        public const long WATCH_VAR_PRF_LOOP = 1201;

        public const long WATCH_VAR_COMMAND_CODE = 1220;
        public const long WATCH_VAR_COMMAND_DATA = 1221;
        public const long WATCH_VAR_COMMAND_COUNT = 1222;
        public const long WATCH_VAR_COMMAND_READ_FLAG = 1223;

        public const long WATCH_VAR_PRF_POS = 6000;
        public const long WATCH_VAR_PRF_VEL = 6001;
        public const long WATCH_VAR_PRF_ACC = 6002;

        public const long WATCH_VAR_PRF_RUN = 6200;

        public const long WATCH_VAR_CRD_PRF_POS = 8000;
        public const long WATCH_VAR_CRD_PRF_VEL = 8001;
        public const long WATCH_VAR_CRD_PRF_ACC = 8002;

        public const long WATCH_VAR_CRD_RUN = 8200;

        public const long WATCH_VAR_CRD_SEGMENT_NUMBER = 8202;
        public const long WATCH_VAR_CRD_SEGMENT_NUMBER_USER = 8203;
        public const long WATCH_VAR_CRD_COMMAND_RECEIVE = 8204;
        public const long WATCH_VAR_CRD_COMMAND_EXECUTE = 8205;

        public const long WATCH_VAR_CRD_FOLLOW_SLAVE_POS = 8600;
        public const long WATCH_VAR_CRD_FOLLOW_SLAVE_VEL = 8601;

        public const long WATCH_VAR_CRD_FOLLOW_STAGE = 8610;

        public const long WATCH_VAR_SCAN_PRF_POS = 18000;
        public const long WATCH_VAR_SCAN_PRF_VEL = 18001;
        public const long WATCH_VAR_SCAN_PRF_ACC = 18002;

        public const long WATCH_VAR_SCAN_PRF_POS_X = 18010;
        public const long WATCH_VAR_SCAN_PRF_POS_Y = 18020;

        public const long WATCH_VAR_SCAN_RUN = 18200;

        public const long WATCH_VAR_SCAN_SEGMENT_NUMBER = 18202;


        public const long WATCH_VAR_LASER_HSIO = 18600;
        public const long WATCH_VAR_LASER_POWER = 18601;

        public const long WATCH_VAR_AXIS_PRF_POS = 20000;
        public const long WATCH_VAR_AXIS_PRF_VEL = 20001;
        public const long WATCH_VAR_AXIS_PRF_ACC = 20002;
        public const long WATCH_VAR_AXIS_ENC_POS = 20003;

        public const long WATCH_VAR_AXIS_PRF_VEL_FILTER = 20011;

        public const long WATCH_VAR_ENC_POS = 30000;

        public const long WATCH_VAR_ENC_VEL = 30001;

        public const long WATCH_VAR_GPI = 31000;

        public const long WATCH_VAR_GPO = 32000;

        public const long WATCH_VAR_AI = 33000;

        public const long WATCH_VAR_AO = 34000;

        public const long WATCH_VAR_AUTO_FOCUS_OUT = 34006;

        public const long WATCH_VAR_TRIGGER_EXECUTE = 38000;
        public const long WATCH_VAR_TRIGGER_STATUS = 38001;
        public const long WATCH_VAR_TRIGGER_POSITION = 38002;
        public const long WATCH_VAR_TRIGGER_COUNT = 38010;

        public const long WATCH_VAR_POS_LOOP_ERROR = 40000;

        public const long WATCH_VAR_CONTROL_REF_VEL = 41000;

        public const long WATCH_VAR_WATCH_TIME = 52001;

        public const long WATCH_VAR_INT32 = 52020;
        public const long WATCH_VAR_INT64 = 52021;
        public const long WATCH_VAR_FLOAT = 52022;
        public const long WATCH_VAR_DOUBLE = 52023;
        public const long WATCH_VAR_BOOL = 52024;


        public struct TWatchVar
        {
            public ushort type;
            public ushort index;
            public ushort id;
        }

        public struct TWatchEvent
        {
            public ushort type;
            public ushort loop;
            public ushort watchCount;
            public TWatchVar var;
            public ushort condition;
            public double value;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_ClearWatch(short core, short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_AddWatchVar(short core, ref TWatchVar pVar);
        [DllImport("gxn.dll")]
        public static extern short GTN_AddWatchEvent(short core, ref TWatchEvent pEvent);
        [DllImport("gxn.dll")]
        public static extern short GTN_WatchOn(short core, short interval, short mode, ushort count);
        [DllImport("gxn.dll")]
        public static extern short GTN_WatchOff(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_PrintWatch(short core, string pFileName, long start, uint printCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMcVar(short core, out TWatchVar pVar, out double pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetWatchGroup(short core, short group);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetWatchGroup(short core, out short pGroup);
        [DllImport("gxn.dll")]
        public static extern short GTN_LoadWatchConfig(short core, string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SaveWatchConfig(short core, short group, out char pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_ReadWatch(short core, short varIndex, out double pBuffer, uint bufferSize, out uint pReadCount);

        public struct TWatchInfo
        {
            public short enable;                        // 采集使能
            public short run;                           // 采集状态
            public uint time;                   // 采集次数
            public uint head;                   // 头指针
            public uint threshold;          // 最多容纳采集次数

            public short interval;                      // 采集间隔
            public short mode;                          // 采集模式
            public ushort countBeforeEvent; // 事件触发之前的采集数量
            public ushort countAfterEvent;      // 事件触发以后的采集数量
            public ushort varCount;         // 采集变量数量
            public ushort eventCount;           // 采集事件数量
        }

        public struct TWatchVarInfo
        {
            public uint pAddress;
            public ushort length;
            public short fraction;
            public ushort format;
            public ushort hex;
            public ushort sign;
            public ushort bit;
        }

        public struct TWatchFormat
        {
            public short width;
            public short precision;
            public char seperator;
            public short hex;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_GetWatchInfo(short core, out TWatchInfo pInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetWatchVar(short core, short index, out TWatchVar pVar, out TWatchVarInfo pVarInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetWatchEvent(short core, short index, out TWatchEvent pEvent);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetWatchFormat(short core, ref TWatchFormat pFormat);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetWatchFormat(short core, out TWatchFormat pFormat);


        [DllImport("gxn.dll")]
        public static extern short GTN_LoadReadHsConfig(short core,string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_ReadHsOn(short core, short enable, short mode, double interval);

        public struct TAxisArrivePrm
        {
            public short mode;
            public short pad0;
            public Int32 band;		//控制器判断到位误差带
            public Int32 time;		//控制器判断到位时间
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
            public Int32[] pad1;
        };
        [DllImport("gxn.dll")]
        public static extern short GTN_GetStsEx(short core, short axis, out Int32 pSts, short count, out UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisArriveMode(short core, short axis, ref TAxisArrivePrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisArriveMode(short core, short axis, out TAxisArrivePrm pPrm);

        
        public struct TCrdStatusEx
        {
	        public short runEmpty;
	        public Int32 segUseCount;
	        public Int32 segReceiveCount;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
	        public short[] pad1;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
	        public Int32[] pad2;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
	        public double[] pad3;
        };

        [DllImport("gxn.dll")]
        public static extern short GTN_CrdStatusEx(short core, short crd, out TCrdStatusEx pCrdStatus, short fifo);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetCrdMPGMode(short core, short crd, short enable, short master, Int32 masterEven, Int32 slaveEven, short filterTime, short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCrdMPGMode(short core, short crd, out short pEnable, out short pMaster, out Int32 pMasterEven, out Int32 pSlaveEven, out short pFilterTime, out short pMode, out short pFifoEnd);
        
        public struct TMoveContinuousAbsolutePrm 
        {
	        public double pos;
	        public double vel;
	        public double velEnd;
	        public double acc;
	        public double dec;
	        public short  direction;
	        public short overrideSelect;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 2)]
	        public short[] reserve;
        } ;

        [DllImport("gxn.dll")]
        public static extern short GTN_MoveContinuousAbsolute(short core, short profile, ref TMoveContinuousAbsolutePrm pPrm, ref TListInfo pListInfo, short group);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetReadHs(short core, short enable, short mode, short interval);
        [DllImport("gxn.dll")]
        public static extern short GTN_ReadHsReadBuffer(short core, out short pData, Int32 count);

        [DllImport("gxn.dll")]
        public static extern short GTN_RN_GeneralCommand(short core, short stationId, Int32 cmdId, Int32 prmSizeCount, IntPtr pPrm, Int32 resultSizeCount, IntPtr pResult);
        /*-----------------------------------------------------------*/
        /* Encrypt                                           */
        /*-----------------------------------------------------------*/
        
        public struct TEncryptOperatePrm
        {	
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 32)]
	        public Byte[] keyValue;		// 原始秘钥
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 32)]
	        public Byte[] randomIn;
	        public short block;			// 第几个block,32bytes一个block,保留
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 32)]
	        public Byte[] data;			// 读取到的或者写下去的新的数据
	        public short sts;
        };
        [DllImport("gxn.dll")]
        public static extern short GTN_WriteUserSlotDataEncrypt(short core, short slotIndex, ref TEncryptOperatePrm pWritePrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_ReadUserSlotDataEncrypt(short core, short slotIndex, out TEncryptOperatePrm pReadPrm);

        [DllImport("gxn.dll")]
        public static extern short GTN_EncryptionFunConfig(short core, short slotIndex, ref Byte pOldKey, ref Byte pNewKey, ref Byte pUserData, out short pSts, out short pConfigured);

        
        public struct TTriggerProfilePrm 
        {
	        public short mode;	    //运动类型,0-点位，6-PVT，其它类型暂时不支持
	        public short enable;    //是否使能，0-不使能，1-使能
	        public short trigger;	//trigger索引，取值从1开始
	        public short pad1;		//保留
	        public Int32 distance;	//触发时偏移量，可正可负，单位：脉冲
	        public Int32 posLimit;  //重新规划后，触发位置+偏移量不能超过该值
	        public double vel;      //目标速度，重新规划的目标速度，单位：脉冲/ms
	        public double acc;      //需要提速时的加速度，单位：脉冲/ms
	        public double dec;		//运动到触发偏移量的减速度，单位：脉冲/ms
	        public double percent;	//减速段S型曲线时间百分比，例如60表示60%
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 4)]
	        public double[] reserve;
        };

        public struct TTriggerProfileStatus
        {
	        public short mode;
	        public short enable;
	        public short execute;			//是否执行中,0-未执行，1-执行
	        public short status;			//执行过程中的状态，正常为0，异常则返回错误码
	        public Int32 endPos;			//终点位置（捕获+偏移量）
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 7)]
	        public Int32[] reserve;
        };

        [DllImport("gxn.dll")]
        public static extern short GTN_SetTriggerProfilePrm(short core, short profile, ref TTriggerProfilePrm pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTriggerProfileStatus(short core, short profile, out TTriggerProfileStatus pSts);

        [DllImport("gxn.dll")]
        public static extern short GTN_LmtsOnEx(short core, short axis, short limitType, short limitMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_LmtsOffEx(short core, short axis, short limitType, short limitMode);

        [DllImport("gxn.dll")]
        public static extern short GTN_PrintLogInfo(short core, string pFileName, Int32 start, Int32 count);
        [DllImport("gxn.dll")]
        public static extern short GTN_PrintMcStsInfo(short core, string pFileName, short type, short index, short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_PrintCommandInfo(short core, string pFileName, Int32 start, Int32 count);
        
        /*-----------------------------------------------------------*/
        /* ILC                                                       */
        /*-----------------------------------------------------------*/
        public struct TIlcResult
        {				  
	        public double ErrorMax;
	        public double ErrorAvg;
	        public double ErrorRms;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 9)]
	        public double[] pad1;
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 10)]
	        public short[]  pad2;
        };

        [DllImport("gxn.dll")]
        public static extern short GTN_InitIlc(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_StartIlc(short core, short crd);
        [DllImport("gxn.dll")]
        public static extern short GTN_StopIlc(short core, short crd, ref TIlcResult pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_SaveIlcFile(short core, string pIlcFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_LoadIlcFile(short core, short crd, string filePath);
        
        /*-----------------------------------------------------------*/
        /* MovePos                                                   */
        /*-----------------------------------------------------------*/
        public struct  TMovePosPercent
        {
	        public short value;
        };

        public struct TMovePosSmoothTime 
        {
	        public short value;
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct TMovePosUnion
        {
            [FieldOffset(0)]
	        public TMovePosPercent percent;
            [FieldOffset(1)]
	        public TMovePosSmoothTime smoothTime;
            //[FieldOffset(2)]
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            //public double[] value;
        };

        public struct TMovePosParameter 
        {
            public double pos;
            public double vel;
            public double acc;
            public double dec;
            public short direction;
            public short overrideSelect;
            public short pad;
            public short mode;
            public TMovePosUnion data;
        };

        public struct TMovePosTwoSegmentParameter 
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public double[] pos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public double[] vel;
            public double acc;
            public double dec;
            public short direction;
            public short overrideSelect;
            public short pad;
            public short mode;
            public TMovePosUnion data;
        };

        public struct  TMultiMovePosParameter
        {
            public short profile;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] pad1;
            public double pos;
            public double vel;
            public double acc;
            public double dec;
            public short direction;
            public short overrideSelect;
            public short pad2;
            public short mode;
            public TMovePosUnion data;
        };

        public struct TMultiMovePosTwoSegmentParameter 
        {
            public short profile;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] pad1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public double[] pos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public double[] vel;
            public double acc;
            public double dec;
            public short direction;
            public short overrideSelect;
            public short pad2;
            public short mode;
            public TMovePosUnion data;
        };
        [DllImport("gxn.dll")]
        public static extern short GTN_MovePos(short core, short profile,ref TMovePosParameter pMovePos,ref TListInfo pListInfo, short group = 0);
        [DllImport("gxn.dll")]
        public static extern short GTN_MovePosTwoSegment(short core, short profile, ref TMovePosTwoSegmentParameter pMovePos, ref TListInfo pListInfo, short group);

        public struct TMotionTimeRestrictDirect
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public short[] condition;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public double[] time;
        }

        public struct TMotionTimeRestrictAttentionProfile 
        {
            public short condition;	
            public short attentionProfileCount;		
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public short[] attentionProfile;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public short[] reserve1;
            public double settlingTime; 
        };

        //[StructLayout(LayoutKind.Explicit)]
        //public struct TMotionTimeRestrictUnion
        //{
        //    [FieldOffset(0)]
        //    TMotionTimeRestrictDirect direct;		// 保留
        //    [FieldOffset(11)]
        //    TMotionTimeRestrictAttentionProfile attentionProfile; // 参考轴信息
        //    //[FieldOffset(0)]
        //    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        //    //public double[] data;
        //};

        public struct TMotionTimeRestrict
        {
            public short profileCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public short[] profile;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public short[] pad1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public double[] profilePos;
            public short mode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] pad2;
            //public TMotionTimeRestrictUnion parameter;
        };
        [DllImport("gxn.dll")]
        public static extern short GTN_SetMotionTimeRestrict(short core, short restrictIndex, ref TMotionTimeRestrict pRestrict, ref TListInfo pListInfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetWaitForCondition(short core, short list, out TWatchCondition pWatchCondition, out short pConditionCount, short pOperation, out TWaitTimeout pTimeout, out short pConditionResult);
        /*-----------------------------------------------------------*/
        /* 力控                                                      */
        /*-----------------------------------------------------------*/	
		public const short LOOP_MODE_POSITION = 0;		// 位置闭环
		public const short LOOP_MODE_PRESS =1;		    // 压力闭环
		public const short LOOP_MODE_NONE = 2;

		public const short PRESS_PROFILE_NONE = 0;	    // 处于压力闭环模式，不进行规划
		public const short PRESS_PROFILE_TARGET = 1;	// 单组目标压力模式
		public const short PRESS_PROFILE_ARRAY = 2;	    // 多组目标压力模式
		public const short PRESS_PROFILE_PERCENT = 3;
		public const short PRESS_PROFILE_TABLE = 4;

		public const short LIMIT_TYPE_PRESS	= 7;
		public const short LIMIT_TYPE_TORQUE = 8;
		public const short MC_PRESS	= 500;
		public const short MC_TORQUE = 501;

		public const short PRESS_GREATER_THAN_LIMIT = 0;
		public const short PRESS_LESS_THAN_LIMIT = 1; 

		public const short PRESS_DIR_NONE = 0;
		public const short PRESS_DIR_CROSS_POSITIVE = 1;
		public const short PRESS_DIR_CROSS_NEGATIVE = 2;
		public const short PRESS_DIR_CROSS_BOTH = 3;
		public const short PRESS_DIR_GE = 4;
		public const short PRESS_DIR_LE = 5;

		public struct TStopOffset
		{
			public Int32 distance;		// 回退距离
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public short[] reverse;
            public double vel;			// 回速度
			public double acc;			// 回退加速度
		};

		public struct TLoopMode
		{
			public short loopMode;				// 环路模式
			public short pressProfileMode;		// 压力闭环模式下有效
		};

		public struct TPressPrm
		{
			public short active;			// 功能使能
            public short reverse1;
			public Int32 scale;				// 当量，物理量1牛（N）转换到脉冲的当量。
			public short linkAxis;			// 压力规划器与那个轴相关联。
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] reverse2;
		};

		public struct TPressTargetPrm
		{
			public double acc;				// 力矩加速度
			public double dec;				// 力矩加速度
			public double pressStart;		// 起跳力矩
			public short  smoothTime;		// 平滑时间
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] reverse2;
		};

		public struct TPressArrayData
		{
			public double pressTarget;		// 目标压力
			public Int32 pressTime;			// 爬升时间 ms单位
			public Int32 holdTime;			// 保持时间
		};

		public struct TPressArray
		{
			public short count;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] reverse1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public TPressArrayData[] buffer;
			public short exit;				// 规划完成后是否切换到位置闭环模式
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public short[] reverse2;
		};

		public struct TPressPid
		{
			public double kp;
			public double ki;
			public double kd;
			public double kvff;
			public double kaff;
			public Int32 integralLimit;
			public Int32 derivativeLimit;
			public short limitMax;					// 控制器输出最大值
			public short limitMin;					// 控制器输出最小值
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public short[] reverse1;
		};

		// 环路模式
        [DllImport("gxn.dll")]
		public static extern short GTN_SetLoopMode(short core,short axis,ref TLoopMode pMode); // 设置环路模式，压力闭环还是位置闭环，
        [DllImport("gxn.dll")]
		public static extern short GTN_GetLoopMode(short core,short axis,out TLoopMode pMode); // 读取环路模式，压力闭环还是位置闭环，
		// 设置、读取力控功能相关参数
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressPrm(short core,short pressAxis,ref TPressPrm pPressPrm);
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressPrm(short core,short pressAxis,out TPressPrm pPressPrm);
		// 单组目标压力
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressTarget(short core,short pressProfile,double value,ref TPressTargetPrm pPressTargetPrm);
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressTarget(short core,short pressProfile,out double pValue,out TPressTargetPrm pPressTargetPrm);
		// 多组目标压力
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressArray(short core,short pressProfile,ref TPressArray pPressArray);
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressArray(short core,short pressProfile,out TPressArray pPressArray);
		// 压力环PID
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressPid(short core,short pressControl,ref TPressPid pPid);
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressPid(short core,short pressControl,out TPressPid pPid);
		// 读取规划压力
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPrfPress(short core,short pressProfile,out double pValue,out double pValueFilter);
		// 读取反馈压力
        [DllImport("gxn.dll")]
		public static extern short GTN_GetAtlPress(short core,short pressProfile,out double pValue);
		// 获取压力状态
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressStatus(short core,short pressAxis,out Int32 pStatus);

		// 设置规划压力
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPrfPress(short core,short axis,double prfPressValue);
		// 设置压力反馈源
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressFeedbackType(short core,short pressAxis,out short pFbType,out short pFbIndex);
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressFeedbackType(short core,short pressAxis,short fbType,short fbIndex);

		public struct TPressLimit
		{
			public short limit1;
			public short limit2;
			public short time;
			public short triggerCondition;          // 压力急停触发条件，PRESS_GREATER_THAN_LIMIT,PRESS_LESS_THAN_LIMIT,
		};

		public struct TTorqueLimit
		{
			public short limit1;					// 超过Limit1经过time个周期停止
			public short limit2;					// 超过Limit2马上停止
			public short time;
            public short reverse;
		};
		// 设置力矩超限停止参数		// 保护功能
        [DllImport("gxn.dll")]
		public static extern short GTN_SetLimit(short core,short axis,short type,System.IntPtr pPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetLimit(short core, short axis, short type, System.IntPtr pPrm);

		// 保护功能触发后回退参数
        [DllImport("gxn.dll")]
		public static extern short GTN_GetStopOffset(short core,short axis,out TStopOffset pStopOffset);
        [DllImport("gxn.dll")]
		public static extern short GTN_SetStopOffset(short core,short axis,ref TStopOffset pStopOffset);

		public struct TPressAutoSwitchPrm
		{
			public short limit1;				
			public short limit2;				
			public short time;
			public short triggerCondition;     
			public short loopMode;				
			public short pressProfileMode;		
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public short[] reverse;
		};
		// 设置力/位自动切换参数
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressAutoSwitchPrm(short core,short pressAxis,ref TPressAutoSwitchPrm pPrm);
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressAutoSwitchPrm(short core,short pressAxis,out TPressAutoSwitchPrm pPrm);
        [DllImport("gxn.dll")]
		public static extern short GTN_PressAutoSwitchEnable(short core,short pressAxis,short enable);

		// 设置压力闭环模式下的工作空间
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressRange(short core,short pressAxis,short centerSynch,Int32 range);
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressRange(short core,short pressAxis,out Int32 pCenterPos,out Int32 pRange);

		// 设置压力闭环模式下位置穿越保护
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressCross(short core,short pressAxis,short dir,Int32 pos);
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressCross(short core,short pressAxis,out short pDir,out Int32 pPos);

		// 设置压力捕获参数
        [DllImport("gxn.dll")]
		public static extern short GTN_SetPressTrigger(short core,short pressAxis,short pressThread,short triggerCondition);
		// 读取压力捕获参数
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressTrigger(short core,short pressAxis,out short pPressThread,out short pTriggerCondition);
		// 打开压力捕获功能
        [DllImport("gxn.dll")]
		public static extern short GTN_PressTriggerOn(short core,short pressAxis);
		// 关闭压力捕获功能
        [DllImport("gxn.dll")]
		public static extern short GTN_PressTriggerOff(short core,short pressAxis);
		// 读取压力捕获位置
        [DllImport("gxn.dll")]
		public static extern short GTN_GetPressTriggeredPos(short core,short pressAxis,out short pEnable,out double pCntPos,out short pTriggeredPress);
		
		/************************************************************************/
		/* 旋转轴功能                                                           */
		/************************************************************************/
		public const short ROTARY_DIRECTION_SELECT_MODE_DEFAULT = 0;
		public const short ROTARY_DIRECTION_SELECT_MODE_SMART = 1;

		public struct TRotaryConfig
		{
			public short rotary;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public short[] pad;
			public double start;
			public double length;
			public double reserve;
			public double pulse;
		};

		public struct TLineAbsolutePrm
		{
			public short fifo;
			public short overrideNum;
			public short g0Mode;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public short[] reserve1;
			public Int32 segNum;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public Int32[] reserve2;
			public double vel;
			public double acc;
			public double velEnd;
			public double velLimit;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public double[] reserve3;
		};
		[DllImport("gxn.dll")]
		public static extern short GTN_GetAxisPrfPosRotary(short core,short axis,out double pTheta,out double pRound,short count);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetProfileRotaryConfig(short core,short profile,out TRotaryConfig pConfig);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetPrfPosRotary(short core,short profile,out double pTheta,out double pRound,short count);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetCrdScale(short core,short crd,short dimension,double alpha,double beta);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetCrdScale(short core,short crd,short dimension,out double pAlpha,out double pBeta);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetCrdRotaryConfig(short core,short crd,short dimension,out TRotaryConfig pConfig);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetCrdPosRotary(short core,short crd,short dimension,out double pTheta,out double pRound,short count);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetEncoderRotaryConfig(short core,short encoder,out TRotaryConfig pConfig);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetEncPosRotary(short core,short encoder,out double pTheta,out double pRound,short count);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetAxisRotaryConfig(short core,short axis,ref TRotaryConfig pConfig);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetAxisRotaryConfig(short core,short axis,out TRotaryConfig pConfig);
		[DllImport("gxn.dll")]
		public static extern short GTN_ZeroAxisRotaryRound(short core,short axis,short count);
		[DllImport("gxn.dll")]
		public static extern short GTN_LineAbsoluteEx(short core,short crd,ref double pPos,ref short pDir,ref TLineAbsolutePrm pPrm);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetAxisRotaryDirectionSelectMode(short core,short axis,short mode);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetAxisRotaryDirectionSelectMode(short core,short axis,out short pMode);

		public struct TBufMoveAbsPrm 
		{
			public double pos;
			public double vel;
			public double acc;
			public double velMax;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public double[] reserve1;
			public short smoothTime;
			public short enableRatio;
			public short modal;
			public short fifo;
			public short dir;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public short[] reserve2;
			public Int32 segNum;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public Int32[] reserve3;
		};
		[DllImport("gxn.dll")]
		public static extern short GTN_BufMoveAbsoluteEx(short core,short crd,short moveAxis,ref TBufMoveAbsPrm pPrm);

        /*-----------------------------------------------------------*/
        /* 绝对值辅助编码器相关                                      */
        /*-----------------------------------------------------------*/
		[DllImport("gxn.dll")]
        public static extern short GTN_SetAuAbsEncMultiTurnRange(short core,short encoder,double range);
		[DllImport("gxn.dll")]
        public static extern short GTN_ReadAuAbsEncPos(short core,short encoder,ref double pPos);

		[DllImport("gxn.dll")]
        public static extern short GTN_SetCrdUserDataEndVelLa(short core,short crd, short crdUserDataType,double endVel);

        /*-----------------------------------------------------------*/
        /* PVT模式缓冲区至今操作                                      */
        /*-----------------------------------------------------------*/
        public const short PVT_OPERATION_TYPE_LASER_ON = 2;
        public const short PVT_OPERATION_TYPE_BUF_DA = 4;
        public const short PVT_OPERATION_TYPE_LASER_CMD = 5;
        public const short PVT_OPERATION_TYPE_LASER_FOLLOW_RATIO = 6;
        public const short PVT_OPERATION_TYPE_LASER_FOLLOW_MODE	= 25;
        public const short PVT_OPERATION_TYPE_LASER_FOLLOW_OFF = 26;
        public const short PVT_OPERATION_TYPE_BUF_DO_BIT = 94;

        public struct TPVTDoBit
        {
            public short doType;
            public short index;
            public short value;
        };

        public struct TPVTDA
        {
            public short chn;
            public short daValue;
        };

        public struct TPVTLaserSwitch
        {
            public short chn;
            public short enable;
        };

        public struct TPVTLaserPrfCmd
        {
            public short chn;
            public double power;
        };

        public struct TPVTLaserFollowRatio
        {
            public short chn;
            public double ratio;
            public double minPower;
            public double maxPower;
        };

        public struct TPVTLaserFollowOff
        {
            public short chn;
        };

        public struct TPVTLaserFollowMode
        {
            public short chn;
            public short source;
            public double startPower;
        };

        [DllImport("gxn.dll")]
        public static extern short GTN_PvtTableUserData(short core, short tableId, short userDataType, double time, System.IntPtr pData);




        //---------------------------------------------------Ecat指令-----------------------------------------------------------------------------
        public struct TSlaveInfo
        {
            public Int32 slave_cnt;
            public Int32 slave_type;
            public Int32 motion_cnt;
            public Int32 io_nmap;
            public Int32 io_length;
            public UInt32 Vid;
            public UInt32 Pid;
            public Int32 io_type;
        }
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatRawData(short core, ushort offset, ushort nByteSize, ref byte pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatRawData(short core, ushort offset, ushort nByteSize, out byte pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatHomingPrm(short core, short axis, short method, double speed1, double speed2, double acc, Int32 offset, ushort probeFunction);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatHomingPrmEx(short core, short axis, short method, double speed1, double speed2, double acc, Int32 offset, ushort probeFunction);
        [DllImport("gxn.dll")]
        public static extern short GTN_StartEcatHoming(short core, short axis);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetHomingMode(short core, short axis, short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatHomingPrmEx(short core, short axis, IntPtr psMethod, IntPtr pdSpeed1, IntPtr pdSpeed2, IntPtr pdAcc, IntPtr plOffset, IntPtr pusProbeFunction);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatHomingStatus(short core, short axis, out ushort homingStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_StopEcatHoming(short core, short axis);
        [DllImport("gxn.dll")]
        public static extern short GTN_EcatSDODownload(short core, ushort slave, ushort index, byte subindex, ref byte data, UInt32 data_size, out UInt32 abort_code);
        [DllImport("gxn.dll")]
        public static extern short GTN_EcatSDOUpload(short core, ushort slave, ushort index, byte subindex, out byte target, UInt32 target_size, out UInt32 result_size, out UInt32 abort_code);
        [DllImport("gts.dll")]
        public static extern short GTN_SetTouchProbeFunction(short core, short axis, short ProbePrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTouchProbeFunctionEx(short core, short axis, short Probe1Enable, short Probe1TriggerType, short Probe1TriggerLevel, short Probe2Enable, short Probe2TriggerType, short Probe2TriggerLevel);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTouchProbeStatus(short core, short axis, out ushort probeStatus, out Int32 probe1PosValue, out Int32 probe1NegValue, out Int32 probe2PosValue, out Int32 probe2NegValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatGpioConfig(short core, short effectiveLevel, short direction);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatAxisOnThreshold(short core, short axis, ushort threshold);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisOnThreshold(short core, short axis, out ushort threshold);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatErrorCode(short core, short axis, out ushort pErrorCode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDcError(short core, out short pError);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatAxisMode(short core, short axis, short mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisMode(short core, short axis, out ushort drvMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatAxisPV(short core, short axis, Int32 velocity);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatAxisPT(short core, short axis, short torque);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatEncPos(short core, short axis, out Int32 pEncPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAuxEncPos(short core, short axis, out Int32 pAuxEncPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatEncVel(short core, short axis, out Int32 pVelocity);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisAtlCurrent(short core, short axis, out short pCur);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisAtlTorque(short core, short axis, out short pTorque);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisAV(short core, short axis, out Int32 pVel);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisACT(short core, short axis, out short pCur, out short pTorque);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatMCType(short core, out short pType);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosScale(short core, short axis, ushort posScale);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosScale(short core, short axis, out ushort posScale);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisAI(short core, short axis, short channel, out short pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisDI(short core, short axis, out UInt32 pDi);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatAxisDO(short core, short axis, UInt32 DoValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisDO(short core, short axis, out UInt32 pDo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatAxisDOBit(short core, short axis, short bitOffset, byte DoBitValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisDOBit(short core, short axis, short bitOffset, out byte pDoBitValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatSlaves(short core, out short pSlaveMotionCnt, out short pSlaveIOCnt);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatSlaveInfo(short core, short slave, out TSlaveInfo pSlaveinfo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisPE(short core, short axis, out Int32 pPosErr);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatAxisTorqueOffset(short core, short axis, short torqueOffset);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisTorqueOffset(short core, short axis, out short pTorqueOffset);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatPdoLength(short core, out short pPdoLen);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAuxEncoderCapture(short core, short encoder, short CapPrm);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAuxEncoderCaptureEx(short core, short encoder, short CapUpEnable, short CapUpIO, short CapDnEnable, short CapDnIO);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAuxEncoderCaptureStatus(short core, short encoder, out ushort CapSts, out Int32 PosValue, out Int32 NegValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_EcatIOReadInput(short core, ushort slave, ushort offset, ushort nSize, out byte pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_EcatIOWriteOutput(short core, ushort slave, ushort offset, ushort nSize, ref byte pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_EcatIOBitWriteOutput(short core, ushort slaveno, ushort offset, short Index, byte value);
        [DllImport("gxn.dll")]
        public static extern short GTN_EcatIOSynch(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatAxisPdoData(short core, short axis, ushort objectWord, out byte pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_RelateEcatSlaveToMcGpoBit(short core, short gpoIndex, ushort slave, short slaveType, short bitOffset, short pdoOffset);
        [DllImport("gxn.dll")]
        public static extern short GTN_RelateEcatSlaveToMcGpiBit(short core, short gpi, short ecatIndex, short ecatType, short bitoffset, short pdoOffset);
        [DllImport("gxn.dll")]
        public static extern short GTN_RelateEcatSlaveToMcAuEncoderEx(short core, short auenc, short ecatIndex, short ecatType, short pdoOffset, short pdoByteLength);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMcEcatAxisNum(short core, out short pNum);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetEcatAxisLmtHomeIndex(short core, short axis, short index_LimitN, short index_LimitP, short index_Home);

        public struct TEcatErrInfo
        {
            public short dcError;
            public short workingCountErrorCnt;
            public short ecatCommStatus;
            public short workingCount;
            public short offlineFlag;
            public short dump;
            public short dump1;
            public short dump2;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_GetEcatDcErrorEx(short core, out TEcatErrInfo pEcatErrorInfo);
      

    }


    /*-----------------------------------------------------------*/
    //原config.cs：配置功能，包括主卡和模块                       */
    /*-----------------------------------------------------------*/
    public class mc_cfg
    {
        /*-----------------------------------------------------------*/
        /* config of controller                                      */
        /*-----------------------------------------------------------*/
        public const short RES_LIMIT = 8;
        public const short RES_ALARM = 8;
        public const short RES_HOME = 8;
        public const short RES_GPI = 16;
        public const short RES_ARRIVE = 8;
        public const short RES_MPG = 7;

        public const short RES_ENABLE = 8;
        public const short RES_CLEAR = 8;
        public const short RES_GPO = 16;

        public const short RES_DAC = 12;
        public const short RES_STEP = 8;
        public const short RES_PULSE = 8;
        public const short RES_ENCODER = 11;
        public const short RES_LASER = 2;

        public const short AXIS_MAX = 8;
        public const short PROFILE_MAX = 8;
        public const short CONTROL_MAX = 8;

        public const short PRF_MAP_MAX = 2;
        public const short ENC_MAP_MAX = 2;

        public struct TDiConfig
        {
            public short active;
            public short reverse;
            public short filterTime;
        }

        public struct TCountConfig
        {
            public short active;
            public short reverse;
            public short filterType;

            public short captureSource;
            public short captureHomeSense;
            public short captureIndexSense;
        }

        public struct TDoConfig
        {
            public short active;
            public short axis;
            public short axisItem;
            public short reverse;
        }

        public struct TStepConfig
        {
            public short active;
            public short axis;
            public short mode;
            public short parameter;
            public short reverse;
        }

        public struct TDacConfig
        {
            public short active;
            public short control;
            public short reverse;
            public short bias;
            public short limit;
        }

        public struct TAdcConfig
        {
            public short active;
            public short reverse;
            public double a;
            public double b;
            public short filterMode;
        }

        public struct TControlConfig
        {
            public short active;
            public short axis;
            public short encoder1;
            public short encoder2;
            public Int32 errorLimit;
            public short filterType1;
            public short filterType2;
            public short filterType3;
            public short encoderSmooth;
            public short controlSmooth;
        }

        public struct TControlConfigEx
        {
            public short refType;
            public short refIndex;

            public short feedbackType;
            public short feedbackIndex;

            public Int32 errorLimit;
            public short feedbackSmooth;
            public short controlSmooth;
        }

        public struct TProfileConfig
        {
            public short active;
            public double decSmoothStop;
            public double decAbruptStop;
        }

        public struct TAxisConfig
        {
            public short active;
            public short alarmType;
            public short alarmIndex;
            public short limitPositiveType;
            public short limitPositiveIndex;
            public short limitNegativeType;
            public short limitNegativeIndex;
            public short smoothStopType;
            public short smoothStopIndex;
            public short abruptStopType;
            public short abruptStopIndex;
            public Int32 prfMap;
            public Int32 encMap;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = PRF_MAP_MAX)]
            public short[] prfMapAlpha;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = PRF_MAP_MAX)]
            public short[] prfMapBeta;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ENC_MAP_MAX)]
            public short[] encMapAlpha;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ENC_MAP_MAX)]
            public short[] encMapBeta;
        }

        public struct TMcConfig
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = PROFILE_MAX)]
            public TProfileConfig[] profile;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = AXIS_MAX)]
            public TAxisConfig[] axis;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = CONTROL_MAX)]
            public TControlConfig[] control;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_DAC)]
            public TDacConfig[] dac1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_STEP)]
            public TStepConfig[] step;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_ENCODER)]
            public TCountConfig[] encoder;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_PULSE)]
            public TCountConfig[] pulse1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_ENABLE)]
            public TDoConfig[] enable;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_CLEAR)]
            public TDoConfig[] clear;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_GPO)]
            public TDoConfig[] gpo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_LIMIT)]
            public TDiConfig[] limitPositive;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_LIMIT)]
            public TDiConfig[] limitNegative;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_ALARM)]
            public TDiConfig[] alarm;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_HOME)]
            public TDiConfig[] home;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_GPI)]
            public TDiConfig[] gpi;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_ARRIVE)]
            public TDiConfig[] arrive;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = RES_MPG)]
            public TDiConfig[] mpg;
        }
       
        [DllImport("gxn.dll")]
        public static extern short GTN_SaveConfig(short core, out string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDiConfig(short core, short diType, short diIndex, ref TDiConfig pDi);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDiConfig(short core, short diType, short diIndex, out TDiConfig pDi);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDoConfig(short core, short doType, short doIndex, ref TDoConfig pDo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDoConfig(short core, short doType, short doIndex, out TDoConfig pDo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetStepConfig(short core, short step, ref TStepConfig pStep);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetStepConfig(short core, short step, out TStepConfig pStep);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetDacConfig(short core, short dac, ref TDacConfig pDac);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetDacConfig(short core, short dac, out TDacConfig pDac);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAdcConfig(short core, short adc, ref TAdcConfig pAdc);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAdcConfig(short core, short adc, out TAdcConfig pAdc);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCountConfig(short core, short countType, short countIndex, ref TCountConfig pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetCountConfig(short core, short countType, short countIndex, out TCountConfig pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetControlConfig(short core, short control, ref TControlConfig pControl);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetControlConfig(short core, short control, out TControlConfig pControl);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetControlConfigEx(short core, short control, ref TControlConfigEx pControl);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetControlConfigEx(short core, short control, out TControlConfigEx pControl);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetProfileConfig(short core, short profile, ref TProfileConfig pProfile);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetProfileConfig(short core, short profile, out TProfileConfig pProfile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisConfig(short core, short axis, ref TAxisConfig pAxis);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetAxisConfig(short core, short axis, out TAxisConfig pAxis);
        [DllImport("gxn.dll")]
        public static extern short GTN_ProfileScale(short core, short axis, short alpha, short beta);
        [DllImport("gxn.dll")]
        public static extern short GTN_EncScale(short core, short axis, short alpha, short beta);
   
		/*-----------------------------------------------------------*/
        /* Config of Ext-Module                                      */
        /*-----------------------------------------------------------*/
        public struct TExtModuleStatus
        {
            public short active;
            public short checkError;
            public short linkError;
            public short packageErrorCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            public short[] pad;
        }

        public struct TExtModuleType
        {
            public short type;
            public short input;
            public short output;
        }

        public struct TExtIoMap
        {
            public short station;
            public short module;
            public short index;
        }
		
		[DllImport("gxn.dll")]
		public static extern short GTN_LoadExtModuleConfig(short core, string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SaveExtModuleConfig(short core, string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_ExtModuleOn(short core, short station);
        [DllImport("gxn.dll")]
        public static extern short GTN_ExtModuleOff(short core, short station);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtModuleStatus(short core, short station, out TExtModuleStatus pStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtModuleId(short core, short station, short count, ref short pId);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtModuleId(short core, short station, short count, out short pId);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtModuleReverse(short core, short station, short module, short inputCount, ref short pInputReverse, short outputCount, ref short pOutputReverse);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtModuleReverse(short core, short station, short module, short inputCount, out short pInputReverse, short outputCount, out short pOutputReverse);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtModuleCount(short core, short station, out short pCount);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtModuleType(short core, short station, short module, out TExtModuleType pModuleType);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtIoMap(short core, short type, short index, ref TExtIoMap pMap);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtIoMap(short core, short type, short index, out TExtIoMap pMap);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearExtIoMap(short core, short type);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtAoRange(short core, short index, double max, double min);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtAoRange(short core, short index, out double pMax, out double pMin);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetExtAiRange(short core, short index, double max, double min);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetExtAiRange(short core, short index, out double pMax, out double pMin);

        /*-----------------------------------------------------------*/
        /* Config of Laser and Scan                                  */
        /*-----------------------------------------------------------*/
        public struct TScanCommandMotion
        {
            public Int32 segmentNumber;
            public short x;
            public short y;
            public Int32 deltaX;
            public Int32 deltaY;
            public Int32 vel;
            public Int32 acc;
        }
        public struct TScanCommandMotionDelay
        {
            public Int32 delay;
        }

        public struct TScanCommandDo
        {
            public short doType;
            public short doMask;
            public short doValue;
        }

        public struct TScanCommandDoDelay
        {
            public Int32 delay;
        }

        public struct TScanCommandLaser
        {
            public short mask;
            public short value;
        }

        public struct TScanCommandLaserDelay
        {
            public Int32 laserOnDelay;
            public Int32 laserOffDelay;
        }

        public struct TScanCommandLaserPower
        {
            public Int32 power;
        }

        public struct TScanCommandLaserFrequency
        {
            public Int32 frequency;
        }

        public struct TScanCommandLaserPulseWidth
        {
            public Int32 pulseWidth;
        }

        public struct TScanCommandDa
        {
            public short daIndex;
            public short daValue;
        }

        public struct TScanMap
        {
            public short module;
            public short fifo;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_SetScanMap(short core, short index, ref TScanMap pMap);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetScanMap(short core, short index, out TScanMap pMap);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearScanMap(short core);
        [DllImport("gxn.dll")]
        public static extern short GTN_UpdateScanMap(short core);

        /*-----------------------------------------------------------*/
        /* Config of Position Compare                                */
        /*-----------------------------------------------------------*/
        public struct TPosCompareMap
        {
            public short module;
            public short fifo;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_SetPosCompareMap(short core, short index, ref TPosCompareMap pMap);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetPosCompareMap(short core, short index, out TPosCompareMap pMap);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearPosCompareMap(short core);
    }
	
	/*-----------------------------------------------------------*/
	//原ringnet.cs：和等环网网络相关功能的指令                   */
	/*-----------------------------------------------------------*/
	/*-----------------------------------------------------------*/
	/* Ringnet                                                   */
	/*-----------------------------------------------------------*/
	public class mc_ringnet
    {
    	public const short RTN_SUCCESS = 0;
        public const short RTN_MALLOC_FAIL = -100;                 /* malloc memory fail */
        public const short RTN_FREE_FAIL = -101;                   /* free memory or delete the object fail */
        public const short RTN_NULL_POINT = -102;                  /* the param point input is null */ 
        public const short RTN_ERR_ORDER = -103;                   /* call the function order is wrong, some msg isn't validable */
        public const short RTN_PCI_NULL = -104;                    /* the pci address is empty, can't access the pci device */
        public const short RTN_PARAM_OVERFLOW = -105;              /* the param input is too larget */
        public const short RTN_LINK_FAIL  = -106;                  /* the two ports both link fail */ 
        public const short RTN_IMPOSSIBLE_ERR = -107;              /* it means the system or same function work wrong */
        public const short RTN_TOPOLOGY_CONFLICT = -108;           /* the id conflict */
        public const short RTN_TOPOLOGY_ABNORMAL = -109;           /* scan the net abnormal */
        public const short RTN_STATION_ALONE = -110;               /* the device no id, it means the device id is 0xF0 */
        public const short RTN_WAIT_OBJECT_OVERTIME = -111;        /* multi thread wait for object overtime */
        public const short RTN_ACCESS_OVERFLOW = -112;             /* data[i];  i is larger than the define */
        public const short RTN_NO_STATION = -113;                  /* the station accessed not existent */
        public const short RTN_OBJECT_UNCREATED = -114;            /* the object not created yet */
        public const short RTN_PARAM_ERR = -115;                   /* the param input is wrong */
        public const short RTN_PDU_CFG_ERR = -116;                 /*Pdu DMA Cfg Err */
        public const short RTN_PCI_FPGA_ERR = -117;                /*PCI op err or FPGA op err */
        public const short RTN_CHECK_RW_ERR = -118;      	       /*data write to reg, then rd out, and check err */
        public const short RTN_REMOTE_UNEABLE = -119;              /*the device which will be ctrl by net can't be ctrl by net function */ 

        public const short RTN_NET_REQ_DATA_NUM_ZERO = -120;       /* mail op or pdu op req data num can't be 0 */
        public const short RTN_WAIT_NET_OBJECT_OVERTIME = -121;    /* net op multi thread wait for object overtime */
        public const short RTN_WAIT_NET_RESP_OVERTIME = -122;      /* Can't wait for resp */
        public const short RTN_WAIT_NET_RESP_ERR = -123;           /* wait mailbox op err */
        public const short RTN_INITIAL_ERR = -124;                 /* initial the device err */
        public const short RTN_PC_NO_READY = -125;                 /* find the station'pc isn't work */ 
        public const short RTN_STATION_NO_EXIST = -126; 
        public const short RTN_MASTER_FUNCTION = -127;             /* this funciton only used by master */

        public const short RTN_NOT_ALL_RETURN = -128;              /* the GT_RN_PtProcessData funciton fail return */
        public const short RTN_NUM_NOT_EQUAL = -129;               /* the station number of RingNet do not equal  the station number of CFG */

        public const short RTN_CHECK_STATION_ONLINE_NUM_ERR = -130;/* Check no slave */
        public const short RTN_FILE_ERR_OPEN = -131;               /* open file error */
        public const short RTN_FILE_ERR_FORMAT = -132;             /* parse file error */
        public const short RTN_FILE_ERR_MISSMATCH = -133;          /* file info is not match with the actual ones */
        public const short RTN_DMALIST_ERR_MISSMATCH = -134;       /* can't find the slave */

        public const short RTN_REQUSET_MAIL_BUS_OVERTIME = -150;   /* Requset Mail Bus Err */
        public const short RTN_INSTRCTION_ERR = -151;              /* instrctions err */
        public const short RTN_MAIL_RESP_REQ_ERR = -152;           /* RN_MailRespReq  err */
        public const short RTN_CTRL_SRC_ERR = -153;                /* the controlled source  is error */
        public const short RTN_PACKET_ERR = -154;                  /* packet is error */
        public const short RTN_STATION_ID_ERR = -155;              /* the device id is not in the right rang */
        public const short RTN_WAIT_NET_PDU_RESP_OVERTIME = -156;  /* net pdu op wait overtime */
        public const short RTN_ETHCAT_ENC_POS_ERR = -157;

        public const short RTN_IDLINK_PACKET_ERR = -200;           /* ilink master  decode err! packet_length is not match */
        public const short RTN_IDLINK_PACKET_END_ERR = -201;       /* the ending of ilink packet is not 0xFF */
        public const short RTN_IDLINK_TYPER_ERR = -202;            /* the type of ilink module is error */
        public const short RTN_IDLINK_LOST_CNT = -203;             /* the ilink module has lost connection */
        public const short RTN_IDLINK_CTRL_SRC_ERR = -204;         /* the controlled source of ilink module is error */
        public const short RTN_IDLINK_UPDATA_ERR = -205;           /* the ilink module updata error */
        public const short RTN_IDLINK_NUM_ERR = -206;              /* the ilink num larger the IDLINK_MAX_NUM(30) */
        public const short RTN_IDLINK_NUM_ZERO = -207;             /* the ilink num is zero */

        public const short RTN_NO_PACKET = 301;                    /* no valid packet */
        public const short RTN_RX_ERR_PDU_PACKET = -302;           /* ERR PDU PACKET */
        public const short RTN_STATE_MECHINE_ERR = -303; 
        public const short RTN_PCI_DSP_UN_FINISH = 304;
        public const short RTN_SEND_ALL_FAIL = -305;
        public const short RTN_STATION_CLOSE = 310;
        public const short RTN_STATION_RESP_FAIL = 311;		

        public const short RTN_UPDATA_MODAL_ERR = -330;            /* update the modal in normal way fail */

        public const short RTN_NO_MAIL_DATA = 340;                 /* There is no mail data */
        public const short RTN_NO_PDU_DATA = 341;                  /* There is no pdu data */


        public const short RTN_FILE_PARAM_NUM_ERR = -500;
        public const short RTN_FILE_PARAM_LEN_ERR = -501;
        public const short RTN_FILE_MALLOC_FAIL = -502;
        public const short RTN_FILE_FREE_FAIL = -503;
        public const short RTN_FILE_PARAM_ERR = -504;
        public const short RTN_FILE_NOT_EXSITS = 505;
        public const short RTN_FILE_CREATE_FAIL = 510;
        public const short RTN_FILE_DELETE_FAIL = 511;
        public const short RTN_FIFE_CRC_CHECK_ERR = -512;
        public const short RTN_FIFE_FUNCTION_ID_RETURN_ERR = -600;
        public const short RTN_DLL_WINCE = -800;
        public const short RTN_DLL_WIN32 = -801;
        public const short RTN_XML_STATION_ERR = -900;                  //dma config file confilit with slave type
     
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetEncPos(short encoder,out double pValue, short count, ref UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetAxisError(short axis, out double pValue, short count, ref UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetPrfMode(short axis, out Int32 pValue, short count, ref UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetAuEncPos(short encoder, out double pValue, short count, ref UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetCaptureStatus(short encoder, out short pStatus, out Int32 pValue, short count, ref UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetSts(short axis, out Int32 pSts, short count, ref UInt32 pClock);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetPowerSts(out Int32 pValue);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetEcatAxisACTArray(short axis, ref short pCur, out short pTorque, short count);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_PtSpaceArray(short profile, out short pSpace, short fifo, short count);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetDoEx(short doType, out Int32 pValue);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetDiEx(short diType, out Int32 pValue);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetDo(short doType, out Int32 pValue);
        [DllImport("gxn.dll")]
        public static extern short GT_RN_GetDi(short diType, out Int32 pValue);
        
        [DllImport("gxn.dll")]
        public static extern short GTN_LoadRingNetConfig(short core,string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SaveRingNetConfig(short core,string pFile);

        public const short TERMINAL_LOAD_MODE_NONE = 0;
        public const short TERMINAL_LOAD_MODE_BOOT = 2;
 
        public struct TTerminalStatus
        {
	        public UInt16 type;
	        public short id;
	        public Int32 status;
	        public UInt32 synchCount;
	        public UInt32 ringNetType;
	        public UInt32 portStatus;
	        public UInt32 sportDropCount;
	        public UInt32 reserve1;
            public UInt32 reserve2;
            public UInt32 reserve3;
            public UInt32 reserve4;
            public UInt32 reserve5;
            public UInt32 reserve6;
            public UInt32 reserve7;
        }

        [DllImport("gxn.dll")]
        public static extern short GTN_TerminalInit(short core,short detect);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalVersion(short core,short index,out GTN.mc.TVersion pTerminalVersion);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalPermit(short core,short index,short dataType,UInt16 permit);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalPermitEx(short core,short station,short dataType,ref short permit,short index,short count);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalPermitEx(short core,short station,short dataType,out short pPermit,short index,short count);

        [DllImport("gxn.dll")]
        public static extern short GTN_FindStation(short core,short station,UInt32 time);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalPermit(short core,short index,short dataType,out UInt16 pPermit);
        [DllImport("gxn.dll")]
        public static extern short GTN_ProgramTerminalConfig(short core,short loadMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalConfigLoadMode(short core,out short pLoadMode);

        [DllImport("gxn.dll")]
        public static extern short GTN_ReadPhysicalMap();
        [DllImport("gxn.dll")]
        public static extern short ConvertPhysical(short core,short dataType,short terminal,short index);

        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalSafeMode(short core,short index,short safeMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalSafeMode(short core,short index,ref short pSafeMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearTerminalSafeMode(short core,short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalStatus(short core,short index,out TTerminalStatus pTerminalStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalType(short core,short count,out UInt16 pType,ref short pTypeConnect);
		
		/*-----------------------------------------------------------*/
		/* 读取SPORT包数据                                           */
		/*-----------------------------------------------------------*/
		public const short SPORT_COUNT = 256;

		public struct TTerminalData
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = SPORT_COUNT)]
			public UInt32[] terminalTxBuf;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = SPORT_COUNT)]
			public UInt32[] terminalRxBuf;
		};
		[DllImport("gxn.dll")]
		public static extern short GTN_ReadTerminalData(short core,out TTerminalData pTerminalData);

        /*-----------------------------------------------------------*/
        /* Config of module                                          */
        /*-----------------------------------------------------------*/
        public const short TERMINAL_OPERATION_NONE = 0;
        public const short TERMINAL_OPERATION_SKIP = 1;
        public const short TERMINAL_OPERATION_CLEAR = 2;
        public const short TERMINAL_OPERATION_RESET_MODULE = 3;

        public const short TERMINAL_OPERATION_PROGRAM = 11;
        
        public struct TRingNetCrcStatus
        {
            public UInt32 portACrcOkCnt;
            public UInt16 portACrcErrorCnt;
            public UInt32 portBCrcOkCnt;
            public UInt16 portBCrcErrorCnt;
            public UInt32 reserve;             //目前用于读取FLASH总数据长度
        }

        public struct TTerminalError
        {
            public UInt16 errorCountReceive;
            public UInt16 errorCountPackageDown;
            public UInt16 errorCountPackageUp;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
            public UInt16[] reserve;
        }
        public struct TTerminalMap
        {
            public short moduleDataType;
            public short moduleDataIndex;
            public short dataIndex;
            public short dataCount;
        }

       
        [DllImport("gxn.dll")]
        public static extern short GTN_LoadTerminalConfig(short core, string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_SaveTerminalConfig(short core, string pFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_TerminalOn(short core, short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_TerminalSynch(short core, short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetRingNetCrcStatus(short core, short index, out TRingNetCrcStatus pRingNetCrcStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalError(short core, short index, out TTerminalError pTerminalError);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalType(short core, short count, ref short pType);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalLinkStatus(short core, short count, out short ringNetType, out short pLinkStatus);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalMap(short core, short dataType, short moduleIndex, ref TTerminalMap pMap);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalMap(short core, short dataType, short moduleIndex, out TTerminalMap pMap);
        [DllImport("gxn.dll")]
        public static extern short GTN_ClearTerminalMap(short core, short dataType);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalMode(short core, short station, UInt16 mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalMode(short core, short station, out UInt16 pMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalTest(short core, short station, short index, UInt16 value);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalTest(short core, short station, short index, out UInt16 pValue);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetTerminalOperation(short core, short operation);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetTerminalOperation(short core, out short pOperation);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetMailbox(short core, short station, UInt16 byteAddress, ref UInt16 pData, UInt16 wordCount, UInt16 dataMode, UInt16 desId, UInt16 type);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMailbox(short core, short station, UInt16 byteAddress, out UInt16 pData, UInt16 wordCount, UInt16 dataMode, UInt16 desId, UInt16 type);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetUuid(short core, string pCode, short count);
        
        /*-----------------------------------------------------------*/
		/* 网络恢复指令                                              */
		/*-----------------------------------------------------------*/
		[DllImport("gxn.dll")]
		public static extern short GTN_RN_Recover(short cardIndex);

		/*-----------------------------------------------------------*/
		/* GSHD最大最小力矩设置                                      */
		/*-----------------------------------------------------------*/
		public struct TTorqueLimit
		{
			public ushort torqueMax;
			public ushort torquePostive;
			public ushort torqueNegitive;
			public short reserve1;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public double[] reserve2;
		};

		[DllImport("gxn.dll")]
		public static extern short GTN_RN_SetTorqueLimit(short core,short axis,ref TTorqueLimit pTorqueLimit);
		[DllImport("gxn.dll")]
        public static extern short GTN_RN_GetTorqueLimit(short core,short axis,out TTorqueLimit pTorqueLimit);
		
		/*-----------------------------------------------------------*/
		/* GSHD闭环参数设置                                          */
		/*-----------------------------------------------------------*/
		public struct TServoPosLoopPidMode0
		{
			public double value;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public double[] reverse;
		};

		public struct TServoPosLoopPidUnion
		{
			public TServoPosLoopPidMode0 servoPosLoopPidMode0;
		};

		public struct TServoPosLoopPid  
		{
			public short mode;
			public TServoPosLoopPidUnion servoPosLoopPidPrm;
		};

		[DllImport("gxn.dll")]
		public static extern short GTN_SetServoPosLoopPid(short core,short axis,ref TServoPosLoopPid pServoPosLoopPid);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetServoPosLoopPid(short core,short axis,out TServoPosLoopPid pServoPosLoopPid);

		/*-----------------------------------------------------------*/
		/* 安全模式设置                                              */
		/*-----------------------------------------------------------*/
		[DllImport("gxn.dll")]
		public static extern short GTN_RN_SetStationSafeModeControl(short cardIndex,short stationPhyId,short enable,short clearMode);
		[DllImport("gxn.dll")]
		public static extern short GTN_RN_ClearStationSafeModeStatus(short cardIndex,short stationPhyId);
		[DllImport("gxn.dll")]
		public static extern short GTN_RN_SetStationSafeModeOut(short cardIndex,short stationPhyId,short type,short index,ref short pEnable,ref double pValue,short count);
		[DllImport("gxn.dll")]
		public static extern short GTN_RN_IlinkSetSafeModeControl(short cardIndex,short stationPhyId,short modulePhyId,short enable,short clearMode);
		[DllImport("gxn.dll")]
		public static extern short GTN_RN_IlinkClearSafeModeStatus(short cardIndex,short stationPhyId,short modulePhyId);
		[DllImport("gxn.dll")]
		public static extern short GTN_RN_IlinkSetSafeModeOut(short cardIndex,short stationPhyId,short modulePhyId,short type,short index,ref short pEnable,ref double pValue,short count);


        //-------------------------------------------------------------------------------------------------------
        //获取轴模块LED显示模式
        // mode:灯板数码管显示模式： 
        //0：工作模式(上电初始状态显示零，轴使能后显示小数点，具体切换方式待定)；  
        //1：站号模式(显示站号，短暂显示拨码变动)；  
        //2：报警模式(预留)；
        //radix:灯板数码管显示进制： 0：二进制；  1：八进制；  2：十进制；  3：十六进制；
        //-------------------------------------------------------------------------------------------------------
        [DllImport("gxn.dll")]
        public static extern short GTN_RN_ReadLedDispalyMode(short cardIndex, short stationPhyId, out byte pMode, out byte pRadix);
        //-------------------------------------------------------------------------------------------------------
        //设置轴模块LED显示模式
        // mode:灯板数码管显示模式： 
        //0：工作模式(上电初始状态显示零，轴使能后显示小数点，具体切换方式待定)；  
        //1：站号模式(显示站号，短暂显示拨码变动)；  
        //2：报警模式(预留)；
        //radix:灯板数码管显示进制： 0：二进制；  1：八进制；  2：十进制；  3：十六进制；
        //-------------------------------------------------------------------------------------------------------
        [DllImport("gxn.dll")]
        public static extern short GTN_RN_WriteLedDisplayMode(short cardIndex, short stationPhyId, byte mode, byte radix);
    }

	/*-----------------------------------------------------------*/
	//原LookAheadEx.cs：和前瞻相关功能的指令                      */
	/*-----------------------------------------------------------*/
	public class mc_la
	{
		public const short LA_AXIS_NUM = 8;
		public const short LA_WORK_AXIS_NUM = 6;

        //轴的参数信息（各轴最大速度，各轴最大加速度，各轴最大速度变化量）是否限制速度模式
        public const short AXIS_LIMIT_NONE = 0;     //轴无限制
        public const short AXIS_LIMIT_MAX_VEL = 1;  //轴最大速度限制
        public const short AXIS_LIMIT_MAX_ACC = 2;  //轴最大加速度限制
        public const short AXIS_LIMIT_MAX_DV = 4;   //轴最大速度跳变量限制
		public const short KIN_MSG_BUFFER_SIZE = 32;

        //速度规划模式
        public enum EVelMode
        {
            T_CURVE = 0,
            S_CURVE,
            S_CURVE_NEW,                    //根据加加速度、最大加速度进行S曲线速度前瞻，2015.11.16
            S_CURVE_SMOOTH,

            VEL_MODE_MAX = 0x10000,         //确保长度为4Byte
        };

		//工件坐标系下轨迹是否限制速度模式
		public enum EWorkLimitMode
		{
			WORK_LIMIT_INVALID = 0,		    //不限制
			WORK_LIMIT_VALID = 1,			//限制生效
			
			WORK_LIMIT_MODE_MAX=0x10000,    //确保长度为4Byte
		};
		
		//设置的速度定义规则
		public enum EVelSettingDef
		{
			NORMAL_DEF_VEL = 0,			    //输入为轴坐标系所有轴的合成速度
			NUM_DEF_VEL = 1,				//以NUM系统的规则定义
			CUT_DEF_VEL = 2,				//速度为切削速度
		
			VEL_SETTING_DEF_MAX=0x10000,    //确保长度为4Byte
		};
		
		//设置的加速度定义规则
		public enum EAccSettingDef
		{
			NORMAL_DEF_ACC = 0,             //输入即输出
			LONG_AXIS_ACC = 1,              //长轴最大速度
		
			VEL_SETTING_DEF_MAX=0x10000,    //确保长度为4Byte
		};
		
		//机床类型
		public enum EMachineMode
		{
			NORMAL_THREE_AXIS = 0,		    //标准三轴机床模式
			MULTI_AXES = 1,					//多轴联动模式
			FIVE_AXIS = 2,					//五轴机床模式，轴坐标系为主，工件坐标系为辅
			FIVE_AXIS_WORK = 3,				//五轴机床模式，工件坐标系为主，轴坐标系为辅
			ROBOT = 4,
		
			VEL_SETTING_DEF_MAX=0x10000,    //确保长度为4Byte
		};
		//前瞻参数结构体
		public struct TLookAheadParameter
		{
			public int lookAheadNum;					          //前瞻段数
			public double time;						              //时间常数
			public double radiusRatio;					          //曲率限制调节参数
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public double[] vMax;			                      //各轴的最大速度
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public double[] aMax;			                      //各轴的最大加速度
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public double[] DVMax;			                      //各轴的最大速度变化量（在时间常数内）
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public double[] scale;			                      //各轴的脉冲当量
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public short[] axisRelation;	                      //输入坐标和内部坐标的对应关系
            [MarshalAs(UnmanagedType.ByValArray,SizeConst = 128)]
			public char[] machineCfgFileName;		              //机床配置文件名
		};

        /*-----------------------------------------------------------*/
        /* 贝塞尔曲线插补指令                                        */
        /*-----------------------------------------------------------*/
        public const int BEZIER_CONTROL_POINT_COUNT_MAX = 4;
        public const int INTERPOLATION_AXIS_MAX = 8;
        public struct TBezierPrm
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = INTERPOLATION_AXIS_MAX)]
            public double []endPoint;                         // 终点坐标
            public short overrideSelect;                      // 速度倍率选择，0：第1组倍率；1：第2组倍率
            public short plane;                               // 平面选择，0：XY；1：YZ；2：ZX
            public short controlPointCount;                   // 中间控制点个数,目前只能是2个中间点（3阶bezier）
            public short mode;                                // 保留，必须为0
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = BEZIER_CONTROL_POINT_COUNT_MAX* INTERPOLATION_AXIS_MAX)]
            public double[,] controlPoint;                    // 中间控制点位置，最多4个点
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public double[] reserve2;                        // 保留，必须为0
        };
     
        /*-----------------------------------------------------------*/
        /* 椭圆插补指令                                              */
        /*-----------------------------------------------------------*/

        public const short ELLIPSE_AUX_POINT_COUNT = 5;
        public const short ELLIPSE_MODE_AUX_POINT = 0;
        public const short ELLIPSE_MODE_STANDARD = 1;
        public const short ELLIPSE_MODE_AUX_POINT_2D = 0;
        public const short ELLIPSE_MODE_STANDARD_2D = 1;
       

        public struct TEllipseParameter
        {

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = INTERPOLATION_AXIS_MAX)]
            public double[] endPoint;                         // 终点坐标
            public short plane;                               // 椭圆平面选择，0：XY；1：YZ；2：ZX
            public short dir;                                 // 椭圆方向，0：顺时针；1：逆时针
            public short overrideSelect;                      // 速度倍率选择，0：第1组倍率；1：第2组倍率
            public short mode;                                // 椭圆模式，目前只支持参数0(辅助点模式)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ELLIPSE_AUX_POINT_COUNT* INTERPOLATION_AXIS_MAX)]
            public double[,] pos;                                 // 椭圆上辅助点坐标
           
        };

        //-------------------------------------------------------
        //功能说明：椭圆插补描述参数,模式：ELLIPSE_MODE_AU_POINT_2D
        //plane--------------椭圆平面选择，INTERPOLATION_CIRCLE_PLAT_XY(0)：XY；INTERPOLATION_CIRCLE_PLAT_YZ(1)：YZ；INTERPOLATION_CIRCLE_PLAT_ZX(2)：ZX
        //dir----------------椭圆方向，0：顺时针；1：逆时针
        //overrideSelect-----速度倍率选择，0：第1组倍率；1：第2组倍率
        //pad----------------占位变量，不需要传入
        //endPoint1----------终点坐标1,意义根据plane来定，如果palne为XY平面，则endPoint1、pos1为X坐标，endPoint2、pos2为Y坐标
        //endPoint2----------终点坐标2
        //pos1---------------椭圆上辅助点坐标1
        //pos2---------------椭圆上辅助点坐标2
        //-------------------------------------------------------
        public struct TEllipseAuxPoint2D
        {
            public short plane;
            public short dir;
            public short overrideSelect;
            public short pad;
            public double endPoint_X;
            public double endPoint_Y;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ELLIPSE_AUX_POINT_COUNT)]
            public double []pos_X;    // 椭圆上辅助点坐标
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ELLIPSE_AUX_POINT_COUNT)]
            public double []pos_Y;
        };

        //-------------------------------------------------------
        //功能说明：椭圆插补描述参数,模式：ELLIPSE_MODE_STANDARD_2D
        //plane--------------椭圆平面选择，INTERPOLATION_CIRCLE_PLAT_XY(0)：XY；INTERPOLATION_CIRCLE_PLAT_YZ(1)：YZ；INTERPOLATION_CIRCLE_PLAT_ZX(2)：ZX
        //dir----------------椭圆方向，0：顺时针；1：逆时针
        //overrideSelect-----速度倍率选择，0：第1组倍率；1：第2组倍率
        //pad----------------占位变量，不需要传入
        //endPoint1----------终点坐标1,意义根据plane来定，如果palne为XY平面，则endPoint1为X坐标，endPoint2为Y坐标
        //endPoint2----------终点坐标2
        //centerPoint1-------椭圆圆心坐标1，意义根据plane来定，如果palne为XY平面，则centerPoint1为X坐标，centerPoint2为Y坐标
        //centerPoint2-------椭圆圆心坐标2
        //theta--------------椭圆旋转角度，单位：度
        //a------------------椭圆长轴
        //b------------------椭圆短轴，短轴必须比长轴短
        //-------------------------------------------------------
        public struct TEllipseStandard2D
        {
            public short plane;
            public short dir;
            public short overrideSelect;
            public short pad;
            public double endPoint1;
            public double endPoint2;
            public double centerPoint1;
            public double centerPoint2;
            public double theta;
            public double a;
            public double b;
        };
        [DllImport("gxn.dll")]
        public static extern short GTN_Ellipse(short core, short crd, ref TEllipseParameter pEllipse, double synVel, double synAcc, double velEnd, long segNum, short fifo = 0);
        [DllImport("gxn.dll")]
        public static extern short GTN_EllipseEx(short core, short crd, ref TEllipseParameter pEllipse, double synVel, double synAcc, double velEnd, long segNum, short fifo = 0);
        [DllImport("gxn.dll")]
        public static extern short GTN_EllipsePro(short core, short crd, short mode, IntPtr pData, double synVel, double synAcc, double velEnd, long segNum, short fifo = 0);
        [DllImport("gxn.dll")]
        public static extern short GTN_EllipseProEx(short core, short crd, short mode, IntPtr pData, double synVel, double synAcc, double velEnd, long segNum, short fifo = 0);

        //////////////////////////////////////
        public struct RC_KIN_CONFIG
		{
			public short RobotType;
			public short reserved1;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
			public short[] KinParUse;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
			public double[] KinPar;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
			public short[] KinLimitUse;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
			public double[] KinLimitMin;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
			public double[] KinLimitMax;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
			public double[] KinLimitMinShift;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
			public double[] KinLimitMaxShift1;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public short[] AxisUse;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public char[] AxisPosSignSwitch;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public double[] AxisPosOffset;
		
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
			public short[] CartUnitUse;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
			public char[] CartPosKCSSignSwitch;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public short[] reserved2;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
			public double[] CartPosKCSOffset;
		};
		
		public struct RC_ERROR_INTERFACE
		{
			public char Error;
			public short ErrorID;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 129)]
			public char[] Message;
		};
		
		public struct RC_MSG_BUFFER_ELEMENT
		{
			public short ErrorID;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 129)]
			public char[] Message;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public char[] LogTime;
			public Int32 InternalID;
		};
		
		public struct RC_MSG_BUFFER 
		{
			public short LastMsgIndex;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public RC_MSG_BUFFER_ELEMENT[] MsgElement;
			public Int32 LastMsgID;
		};

        //正逆解方向
        public enum ETransDir
        {
            FORWARD_TRANS = 0,          //正解
            INVERSE_TRANS,              //逆解

            TRANS_DIR_MAX = 0x10000,	// 确保长度为4Byte
        };

		//旋转轴范围设置
		public struct TRotationAxisRange
		{
			public int primaryAxisRangeOn;              //第一旋转轴限定范围是否生效，0：不生效，1：生效
			public int slaveAxisRangeOn;                //第二旋转轴限定范围是否生效，0：不生效，1：生效
			public double maxPrimaryAngle;              //第一旋转轴最大值
			public double minPrimaryAngle;              //第一旋转轴最小值
			public double maxSlaveAngle;                //第二旋转轴最大值
			public double minSlaveAgnle;                //第二旋转轴最小值
		};
		
		//选解参数
		public enum EGroupSelect
		{
			Continuous=0,
			Group_1,
			Group_2,
		};
	
		public enum OptimizeState
        {
            OPT_OFF=0,
            OPT_ON=1
        };

		public enum OptimizeMethod
        {
            NO_OPT = 0,
            OPT_BLENDING, 
            OPT_CIRCLEFITTING,
            OPT_CUBICSPLINE,
            OPT_BSPLINE,
        };
		
		public enum ErrorID
        {
            INIT_ERROR = 1,		                    //没有进行参数初始化
			PASSWORD_ERROR,		                    //密码错误，请在固高运动控制平台上运行
			INDATA_ERROR,		                    //输入数据错误（检查圆弧数据是否正确）
			PRE_PROCESS_ERROR,	
			TOOL_RADIUS_COMPENSATE_ERROR_INOUT,		//刀具半径补偿错误：进入/结束刀补处不能是圆弧
			TOOL_RADIUS_COMPENSATE_ERROR_NOCROSS,	//刀具半径补偿错误：数据不合理，无法计算交点
			USERDATA_ERROR,
		};
		
		//轨迹优化参数结构体
		public struct TOptimizeParamUser 
		{
			public OptimizeState usePathOptimize;	 //是否使用路径优化：OPT_OFF:不使用	OPT_ON:使用
		
			public float tolerance;				     //公差(suggest: rough:0.1, pre-finish:0.05, finish:0.01)
		
			public OptimizeMethod optimizeMethod;	//选择曲线优化方式
		
			public OptimizeState keepLargeArc;		//是否保留大圆弧：OPT_OFF：不保留， OPT_ON：保留
		
			public float blendingMinError;			//blending的最小设定误差
		
			public float blendingMaxAngle;			//blending的最大角度限制（即当线段向量角度大于该角度时，不做blending，单位：度）
		
		};
		
		public struct TErrorInfo 
		{
			public ErrorID errorID;		//错误号(INIT_ERROR:未初始化参数；PRE_PROCESS_ERROR:预处理模块错误；
			public int errorRowNum;		//错误行号
		};
		
		public struct TPreStartPos 
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = LA_AXIS_NUM)]
		    public double[] Pos;
		};
		
		public enum EMachineType
		{
			RW_C_ON_B=0,//B 为第一旋转轴，C 为第二旋转轴
			RW_B_ON_A,	//A 为第一旋转轴，B 为第二旋转轴
			RW_A_ON_B,	//B 为第一旋转轴，A 为第二旋转轴
			RW_C_ON_A,	//A 为第一旋转轴，C 为第二旋转轴
			DT_B_ON_A,	//A 为第一旋转轴，B 为第二旋转轴
			DT_A_ON_B,	//B 为第一旋转轴，A 为第二旋转轴
			DT_A_ON_C,	//C 为第一旋转轴，A 为第二旋转轴
			DT_B_ON_C,	//C 为第一旋转轴，B 为第二旋转轴
			T_A_W_B,	//A 为第一旋转轴，B 为第二旋转轴
			T_B_W_A,	//B 为第一旋转轴，A 为第二旋转轴
			T_A_W_C,	//A 为第一旋转轴，C 为第二旋转轴
			T_B_W_C,	//B 为第一旋转轴，C 为第二旋转轴
		};
		
		public struct TMachCfgInfo
		{
			public EMachineType machineType;                        //机床类型
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] reserve1;                                //保留参数
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public double[] primaryAxisPoint;                       //第一旋转轴中心在MCS的坐标
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public double[] slaveAxisPoint;                         //第二旋转轴中心在MCS的坐标
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public double[] toolLocationPoint;                      //刀具坐标系中心在MCS的坐标
			public short dirMode;                                   //方向描述模式
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] reserve2;                                //保留参数
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public short[] dir;                                     //各轴方向
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5*3)]
			public double[] axisVector;                             //各轴轴线方向
		};

		public struct TLaserFollowDuoTablePrm
		{
			public short frqTableId;
			public short dutyTableId;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public double[] pad1;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public short[] pad2;
		}
        [DllImport("gxn.dll")]
        public static extern short GTN_SetupLookAheadCrd(short core,short crd,EMachineMode machineMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisFollowModeLa(short core, short crd, ref Int32 pFollowMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVelDefineModeLa(short core,short crd,EVelSettingDef velDefMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAccDefineModeLa(short core, short crd, EAccSettingDef accDefMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisLimitModeLa(short core, short crd, ref Int32 pAxisLimitMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetWorkLimitModeLa(short core,short crd,EWorkLimitMode workLimitMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisVelValidModeLa(short core, short crd, Int32 velValidAxis);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVelModeLa(short core, short crd, EVelMode velMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVelSmoothModeLa(short core, short crd, short smoothMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetVelSmoothMode(short core, short crd, Int32 smoothMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_PrintLACmdLa(short core, short crd, Int32 printFlag, Int32 clearFile);
        [DllImport("gxn.dll")]
        public static extern short GTN_UpdateMachineBuildingFileLa(short core, short crd, int update);
        [DllImport("gxn.dll")]
        public static extern short GTN_InitialMachineBuildingEx(short core, short crd, string pMachineCfgFileName, ref double machineCoordCenter, ref double workCoordCenter, double toolLength);
        [DllImport("gxn.dll")]
        public static extern short GTN_InitialMachineBuildingPara(short core, short crd, ref TMachCfgInfo pMachCfgInfo, ref double machineCoordCenter,ref double workCoordCenter, double toolLength);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdRTCPOn(short core, short crd, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdRTCPOff(short core, short crd, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetNonlinearErrorControl(short core, short crd, Int32 enable, double nonlinearError);
        [DllImport("gxn.dll")]
        public static extern short GTN_EnableDiscreateArc(short core, short crd, short enable, double arcError);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetAxisVelValidCompModeLa(short core, short crd, Int32 enable, ref Int32 pCompAxis);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowAxisProcessModeLa(short core, short crd, Int32 AxisFollowMode);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetCmdVelLimitLa(short core, short crd, Int32 enable, Int32 n1, Int32 n2, Int32 mode);
        [DllImport("gxn.dll")]
        public static extern short GTN_InitLookAheadEx(short core, short crd, ref TLookAheadParameter pLookAheadPara, short fifo, short motionMode, ref TPreStartPos pPreStartPos);
        [DllImport("gxn.dll")]
        public static extern short GTN_InitLookAheadPara(short core, short crd, Int32 lookAheadNum, double time, double radiusRatio, double scale, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetFollowAxisParaLa(short core, short crd, ref Int32 pAxisLimitMode, ref double pVmax, ref double pAmax, ref double pDVmax);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetCompToolLength(short core,short crd, double compToolLength);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetCompWorkCoordOffset(short core,short crd, ref double pCompWorkOffset);

        [DllImport("gxn.dll")]
        public static extern short GTN_GetLookAheadSegCountEx(short core, short crd, out int pSegCount, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_GetMotionTimeEx(short core, short crd, out double pTime, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_SetUserSegNumEx(short core, short crd, int segNum, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_CrdDataEx(short core, short crd, System.IntPtr pCrdData, short fifo);  //调用时传入 IntPtr.Zero GTN_CrdDataEx(1, System.IntPtr.Zero, 0);      
        
        [DllImport("gxn.dll")]
        public static extern short  GTN_BezierEx(short core, short crd, ref TBezierPrm pBezier, double synVel, double synAcc, double velEnd, long segNum, short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYEx(short core,short crd,double x,double y,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYG0Ex(short core,short crd,double x,double y,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYZEx(short core,short crd,double x,double y,double z,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYZG0Ex(short core,short crd,double x,double y,double z,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYZAEx(short core,short crd,double x,double y,double z,double a,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYZAG0Ex(short core,short crd,double x,double y,double z,double a,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYZACEx(short core,short crd,ref double pPos,short posMask,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYZACG0Ex(short core,short crd,ref double pPos,short posMask,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYZACUVWEx(short core,short crd,ref double pPos,short posMask,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_LnXYZACUVWG0Ex(short core,short crd,ref double pPos,short posMask,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_ArcXYREx(short core,short crd,double x,double y,double radius,short circleDir,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_ArcYZREx(short core,short crd,double y,double z,double radius,short circleDir,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_ArcZXREx(short core,short crd,double z,double x,double radius,short circleDir,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_ArcXYCEx(short core,short crd,double x,double y,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_ArcYZCEx(short core,short crd,double y,double z,double yCenter,double zCenter,short circleDir,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_ArcZXCEx(short core,short crd,double z,double x,double zCenter,double xCenter,short circleDir,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_ArcXYZEx(short core,short crd,double x,double y,double z,double interX,double interY,double interZ,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYRACEx(short core, short crd, double x, double y, double a, double c, double radius, short circleDir, double synVel, double synAcc, long segNum, short override2, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYCACEx(short core, short crd, double x, double y, double a, double c, double xCenter, double yCenter, short circleDir, double synVel, double synAcc, long segNum, short override2, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_ArcXYZACEx(short core, short crd, double x, double y, double z, double a, double c, double interX, double interY, double interZ, double interA, double interC, double synVel, double synAcc, long segNum, short override2, short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_HelixXYRZEx(short core,short crd,double x,double y,double z,double radius,short circleDir,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_HelixXYCZEx(short core,short crd,double x,double y,double z,double xCenter,double yCenter,short circleDir,double synVel,double synAcc,int segNum,short override2,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_BufDelayEx(short core,short crd,ushort delayTime,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_BufGearEx(short core,short crd,short gearAxis,double deltaPos,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_BufMoveEx(short core,short crd,short moveAxis,double pos,double vel,double acc,short modal,short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_BufIOEx(short core,short crd,ushort doType,ushort doMask,ushort doValue,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufEnableDoBitPulseEx(short core, short crd, short doType, short doIndex, ushort highLevelTime, ushort lowLevelTime, int pulseNum, short firstLevel, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufDisableDoBitPulseEx(short core, short crd, short doType, short doIndex, short fifo);
        [DllImport("gxn.dll")]
        public static extern short  GTN_BufDAEx(short core,short crd,short chn,short daValue,short fifo);

        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowMasterEx(short core, short crd, ref GTN.mc.TBufFollowMaster pBufFollowMaster, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowEventCrossEx(short core, short crd, ref GTN.mc.TBufFollowEventCross pEventCross, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowEventTriggerEx(short core, short crd, ref GTN.mc.TBufFollowEventTrigger pEventTrigger, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowStartEx(short core, short crd, Int32 masterSegment, Int32 slaveSegment, Int32 masterFrameWidth, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowNextEx(short core, short crd, Int32 width, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufFollowReturnEx(short core, short crd, double vel, double acc, short smoothPercent, short fifo);

        [DllImport("gxn.dll")]
        public static extern short GTN_BufSmartCutterOnEx(short core, short crd, short index, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufSmartCutterOffEx(short core, short crd, short index, short fifo);

        [DllImport("gxn.dll")]
        public static extern short  GTN_BufLaserOnEx(short core,short crd,short fifo,short channel);
        [DllImport("gxn.dll")]
        public static extern short  GTN_BufLaserOffEx(short core,short crd,short fifo,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserFollowModeEx(short core, short crd, short source, short fifo, short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserFollowTableEx(short core, short crd, short tableId, double minPower, double maxPower, short fifo, short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufLaserFollowOffEx(short core, short crd, short fifo, short channel);
        [DllImport("gxn.dll")]
        public static extern short  GTN_BufLaserPrfCmdEx(short core,short crd,double laserPower,short fifo,short channel);
        [DllImport("gxn.dll")]
        public static extern short  GTN_BufLaserFollowRatioEx(short core,short crd,double ratio,double minPower,double maxPower,short fifo,short channel);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufWaitDiEx(short core, short crd, short diType, UInt16 diIndex, UInt16 level, short continueTime, Int32 overTime, short flagMode, Int32 segNum, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufWaitLongVarEx(short core, short crd, short index, Int32 value, Int32 overTime, short flagMode, Int32 segNum, short fifo);

        [DllImport("gxn.dll")]
        public static extern short GTN_BufDoBitEx(short core,short crd, Int32 doType,UInt16 index,short value,short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufDoBitDelayEx(short core,short crd,UInt16  doType,UInt16 index,short value,Int32 delayTime,short fifo);

        [DllImport("gxn.dll")]
        public static extern short GTN_BufPosComparePsoPrmEx(short core, short crd, short index, ref GTN.mc.TPosComparePsoPrm pPrm, short fifo);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufPosCompareStartEx(short core, short crd, short fifo, short index);
        [DllImport("gxn.dll")]
        public static extern short GTN_BufPosCompareStopEx(short core, short crd, short fifo, short index);
		
		[DllImport("gxn.dll")]
		public static extern short GTN_BufMoveJogEx(short core,short crd,short moveAxis,double vel,double acc,short modal,short fifo);
		[DllImport("gxn.dll")]
		public static extern short GTN_BufStopEx(short core,short crd,Int32 mask,Int32 option,short fifo);

		[DllImport("gxn.dll")]
		public static extern short GTN_BufLaserFollowDuoTableEx(short core,short crd,ref TLaserFollowDuoTablePrm pPrm,short fifo,short channel);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetRadiusRatioTableLa(short core,short crd,short count,ref double pRadius,ref double pRatio);
		/*-----------------------------------------------------------*/
        /* Comp                                                      */
        /*-----------------------------------------------------------*/ 
        [DllImport("gxn.dll")]
        public static extern short GTN_BufPrfCompEnableEx(short core, short crd, short fifo, short profile, short enable, short enableType);

		public struct TAxisPressPid
		{
			public double kp;
			public double ki;
			public double kd;
			public double integralLimit;	//积分极限
			public double derivativeLimit;	//微分极限
			public double limit;			//调节限制（力或电压）
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		    public double[] pad1;
		};

		public const short PRESS_COMPENSATE_MODE_LINEAR = 0;                          // 线性补偿
		public const short PRESS_COMPENSATE_MODE_TABLE = 1;                           // 查表补偿
		public const short PRESS_COMPENSATE_MODE_REGION_LEARN = 2;                    // 区域内自学习补偿

		public struct TAxisPressCompensate
		{
			public short enable;		          //是否使能，0-关闭，1-使能
			public short type;			          //输入类型，电压或网络模块数据
			public short dimension;	              //补偿输入的维度，最大3维
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		    public short[] index;		          //输入的索引,DAC从1开始，ECAT IO模块站号从0开始
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		    public short[] subIndex;              //输入的子索引，,DAC该参数无效，ECAT IO模块的IOMAP从0开始
			public short mode;			          //模式，线性还是查表
			public short revolveAxis;             //用来计算旋转角度的轴号
			public short regionAxisIndex;         //补偿功能区间有效的参考轴
			public short relatedMasterIndex;      //随动主轴的索引
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		    public short[] pad1;
			public double target;			       //目标力或电压
			public double thredshold;		       //thredshold，什么时候开始补偿
			public double deadZone;		           //死区力或电压，死区内不补偿
			public double factor;			       //力和位移的转化系数
			public double revolveOffset;	       //初始旋转的脉冲数，默认合成方向和ingdex[0]的方向重合
			public double revolveScale;	           //旋转轴的一圈脉冲数
			public TAxisPressPid pid;
			public Int32 compPosMaxP;              //输出补偿位置区间[N,P]的端点P
			public Int32 compPosMaxN;              //输出补偿位置区间[N,P]的端点N
			public double k;	                   //补偿量滤波系数 0-1 数值越大滤波越强
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		    public double[] pad2;
			public Int32 activeRegionP;            //补偿功能有效的规划位置区间[N,P]的端点P
			public Int32 activeRegionN;            //补偿功能有效的规划位置区间[N,P]的端点N
			public Int32 activeRegionInterval;     //补偿功能有效的规划位置区间内的自学习间隔，当mode为自学习PRESS_COMPENSATE_MODE_REGION_LEARN时有效
			public Int32 relatedMasterEven;        //随动主轴的比例
			public Int32 relatedSlaveEven;         //随动从轴的比例
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		    public Int32[] pad3;
		};

		[DllImport("gxn.dll")]
		public static extern short GTN_SetAxisPressCompensate(short core,short axis,ref TAxisPressCompensate pPressComp);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetAxisPressCompensate(short core,short axis,out TAxisPressCompensate pPressComp);
		[DllImport("gxn.dll")]
        public static extern short GTN_SetAxisPressCompensateTable(short core, short axis, short index, Int32 count, ref double pPressData, ref double pPosData);
		[DllImport("gxn.dll")]
		public static extern short GTN_SelectAxisPressCompensateTable(short core,short axis,short index);

		public struct TAxisPressCompensateFixFactor
		{
			public short type;			       //输入类型，电压或网络模块数据
			public short dimension;	           //补偿输入的维度，最大3维
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		    public short[] index;		       //输入的索引,DAC从1开始，ECAT IO模块站号从0开始
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		    public short[] subIndex;           //输入的子索引，,DAC该参数无效，ECAT IO模块的IOMAP从0开始

			public double targetMax;	       //标定的力最大值，达到或超过该值时标定结束
			public double targetMin;	       //标定的力最小值，达到或小于该值时标定结束
			public double factor;		       //力和位移的转化系数，启动时传入0,获取状态时得到标定值。

			public Int32 fixRegionP;           //标定规划位置区间[N,P]的端点P，启动位置的相对位置
            public Int32 fixRegionN;           //标定规划位置区间[N,P]的端点N，启动位置的相对位置
            public Int32 fixRegionInterval;    //标定规划位置区间内的标定间隔

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public Int32[] tmp;
		};
		[DllImport("gxn.dll")]
		public static extern short GTN_StartAxisPressCompensateFixFactor(short core,short axis,ref TAxisPressCompensateFixFactor pPressComp);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetAxisPressCompensateFixFactorStatus(short core,short axis,out short pFixFactorSts,out TAxisPressCompensateFixFactor pPressComp);

		public struct TAxisPressCompensateFixPid
		{
			public short type;			 //输入类型，电压或网络模块数据
			public short dimension;      //补偿输入的维度，最大3维
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		    public short[] index;		 //输入的索引,DAC从1开始，ECAT IO模块站号从0开始
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		    public short[] subIndex;     //输入的子索引，,DAC该参数无效，ECAT IO模块的IOMAP从0开始

			public double target;		 //目标力或电压
			public double factor;		 //力和位移的转化系数
			public TAxisPressPid pid;

            public Int32 fixPosLimitP;    //标定位置区间[N,P]的端点P
            public Int32 fixPosLimitN;    //标定补偿位置区间[N,P]的端点N
			public double thredshold;    //thredshold，什么时候开始补偿
			public double deadZone;	     //死区力或电压，死区内不补偿

			//自整定内部参数，调试使用，后续用户接口没有该部分
			public double Knp;			 //压力环全局增益
			public double K1;			 //全局增益递增系数1
			public double K2;			 //全局增益递增系数2
			public double K3;			 //全局增益递增系数3
			public double Krise;		 //上升系数
			public double Kpeak;		 //峰值系数
			public double Tset;		     //目标响应时间
			public double Td;			 //保守响应时间	

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		    public double[] tmp;
		};

		[DllImport("gxn.dll")]
		public static extern short GTN_StartAxisPressCompensateFixPid(short core,short axis,ref TAxisPressCompensateFixPid pPressComp);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetAxisPressCompensateFixPidStatus(short core,short axis,out short pFixPidSts,out TAxisPressCompensateFixPid pPressComp);

		[DllImport("gxn.dll")]
        public static extern short GTN_AxisPressCompensateEnable(short core, short axis, short enable, double deadZone, double factor, ref GTN.mc.TListInfo pListInfo);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetAxisPressCompensateTarget(short core,short axis,double target,double thredshold,ref GTN.mc.TListInfo pListInfo);

		[DllImport("gxn.dll")]
		public static extern short GTN_BufAxisPressCompensatePrmEx(short core,short crd,short axis,double target,double thredshold,short fifo);
		[DllImport("gxn.dll")]
		public static extern short GTN_BufAxisPressCompensateEx(short core,short crd,short axis,short enable,double deadZone,double factor,short fifo);

		[DllImport("gxn.dll")]
		public static extern short GTN_SetAdcBias(short core,short adc,short bias);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetAdcBias(short core,short adc,out short pBias);
		[DllImport("gxn.dll")]
		public static extern short GTN_SetAuAdcBias(short core,short auAdc,short bias);
		[DllImport("gxn.dll")]
		public static extern short GTN_GetAuAdcBias(short core,short auAdc,out short pBias);

    }
}