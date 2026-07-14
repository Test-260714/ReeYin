/*
TCHRLibFunctionWrapper directly translates basic CHRocodileLib API functions into c# functions. 
The detail description of each function please refer to the ducumentation
*/

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PrecitecClass
{
    public static class TCHRLibFunctionWrapper
    {
        private const string DLL_Filename = "CHRocodile.dll";

        #region Type Definition
        //Lib interface function return type
        public struct Res_t
        {
            private Int32 nRes;
            public static implicit operator Res_t(Int32 i)
            {
                return new Res_t { nRes = i };
            }
            public static implicit operator Int32(Res_t Res)
            {
                return Res.nRes;
            }
        }

        //Connection handle type
        public struct Conn_h
        {
            private UInt32 nHandle;
            public static implicit operator Conn_h(UInt32 i)
            {
                return new Conn_h { nHandle = i };
            }
            public static implicit operator UInt32(Conn_h h)
            {
                return h.nHandle;
            }
        }


        //Command handle type
        public struct Cmd_h
        {
            private UInt32 nHandle;
            public static implicit operator Cmd_h(UInt32 i)
            {
                return new Cmd_h { nHandle = i };
            }
            public static implicit operator UInt32(Cmd_h h)
            {
                return h.nHandle;
            }
        }

        //Response handle type
        public struct Rsp_h
        {
            private UInt32 nHandle;
            public static implicit operator Rsp_h(UInt32 i)
            {
                return new Rsp_h { nHandle = i };
            }
            public static implicit operator UInt32(Rsp_h h)
            {
                return h.nHandle;
            }
        }

        //Plugin handle type
        public struct Plugin_h
        {
            private UInt32 nHandle;
            public static implicit operator Plugin_h(UInt32 i)
            {
                return new Plugin_h { nHandle = i };
            }
            public static implicit operator UInt32(Plugin_h h)
            {
                return h.nHandle;
            }
        }

        #endregion

        #region Constant Definition

        public const UInt32 Invalid_Handle = 0xFFFFFFFF;
        public const Int32 Handle_Unknown_Type = -1;
        public const Int32 Handle_Connection = 0;
        public const Int32 Handle_Command = 1;
        public const Int32 Handle_Response = 2;

        public const Int32 CHR_Unspecified = -1;
        public const Int32 CHR_1_Device = 0;
        public const Int32 CHR_2_Device = 1;
        public const Int32 CHR_Multi_Channel_Device = 2;
        public const Int32 CHR_Compact_Device = 3;

        public const Int32 Connection_Synchronous = 0;
        public const Int32 Connection_Asynchronous = 1;

        public const Int32 Output_Data_Format_Double = 0;
        public const Int32 Output_Data_Format_Raw = 1;

        public const Int16 Data_Type_Unsigned_Char = 0;
        public const Int16 Data_Type_Signed_Char = 1;
        public const Int16 Data_Type_Unsigned_Short = 2;
        public const Int16 Data_Type_Signed_Short = 3;
        public const Int16 Data_Type_Unsigned_Int32 = 4;
        public const Int16 Data_Type_Signed_Int32 = 5;
        public const Int16 Data_Type_Float = 6;
        public const Int16 Data_Type_Double = 255;

        public const UInt32 Rsp_Flag_Query = 0x0001;
        public const UInt32 Rsp_Flag_Error = 0x8000;
        public const UInt32 Rsp_Flag_Warning = 0x4000;
        public const UInt32 Rsp_Flag_Update = 0x2000;

        public const Int32 Rsp_Param_Type_Integer = 0;
        public const Int32 Rsp_Param_Type_Float = 1;
        public const Int32 Rsp_Param_Type_String = 2;
        public const Int32 Rsp_Param_Type_Byte_Array = 4;
        public const Int32 Rsp_Param_Type_Integer_Array = 254;
        public const Int32 Rsp_Param_Type_Float_Array = 255;

        public const UInt32 CmdID_Output_Signals = 0x58444f53;
        public const UInt32 CmdID_Firmware_Version = 0x00524556;
        public const UInt32 CmdID_Measuring_Method = 0x00444d4d;
        public const UInt32 CmdID_Full_Scale = 0x00414353;
        public const UInt32 CmdID_Scan_Rate = 0x005a4853;
        public const UInt32 CmdID_Data_Average = 0x00445641;
        public const UInt32 CmdID_Spectrum_Average = 0x00535641;
        public const UInt32 CmdID_Serial_Data_Average = 0x53445641;
        public const UInt32 CmdID_Refractive_Indices = 0x00495253;
        public const UInt32 CmdID_Abbe_Numbers = 0x00454241;
        public const UInt32 CmdID_Refractive_Index_Tables = 0x00545253;
        public const UInt32 CmdID_Lamp_Intensity = 0x0049414c;
        public const UInt32 CmdID_Optical_Probe = 0x004e4553;
        public const UInt32 CmdID_Confocal_Detection_Threshold = 0x00524854;
        public const UInt32 CmdID_Peak_Separation_Min = 0x004D4350;
        public const UInt32 CmdID_Interferometric_Quality_Threshold = 0x00485451;
        public const UInt32 CmdID_Duty_Cycle = 0x00594344;
        public const UInt32 CmdID_Detection_Window_Active = 0x00414d4c;
        public const UInt32 CmdID_Detection_Window = 0x00445744;
        public const UInt32 CmdID_Number_Of_Peaks = 0x00504f4e;
        public const UInt32 CmdID_Peak_Ordering = 0x00444f50;
        public const UInt32 CmdID_Dark_Reference = 0x004b5244;
        public const UInt32 CmdID_Continuous_Dark_Reference = 0x4b445243;
        public const UInt32 CmdID_Start_Data_Stream = 0x00415453;
        public const UInt32 CmdID_Stop_Data_Stream = 0x004f5453;
        public const UInt32 CmdID_Light_Source_Auto_Adapt = 0x004c4141;
        public const UInt32 CmdID_CCD_Range = 0x00415243;
        public const UInt32 CmdID_Median = 0x5844454d;
        public const UInt32 CmdID_Analog_Output = 0x58414e41;
        public const UInt32 CmdID_Encoder_Counter = 0x53504525;
        public const UInt32 CmdID_Encoder_Counter_Source = 0x53434525;
        public const UInt32 CmdID_Encoder_Preload_Function = 0x46504525;
        public const UInt32 CmdID_Encoder_Trigger_Enabled = 0x45544525;
        public const UInt32 CmdID_Encoder_Trigger_Property = 0x50544525;
        public const UInt32 CmdID_Device_Trigger_Mode = 0x4d525425;
        public const UInt32 CmdID_Download_Spectrum = 0x444c4e44;
        public const UInt32 CmdID_Save_Settings = 0x00555353;
        public const UInt32 CmdID_Download_Upload_Table = 0x4C424154;

        public const Int32 Read_Data_Not_Enough = 0;
        public const Int32 Read_Data_Success = 1;
        public const Int32 Read_Data_Response = 2;
        public const Int32 Read_Data_Buffer_Small = 3;

        public const Int32 Auto_Buffer_Error = -1;
        public const Int32 Auto_Buffer_Saving = 0;
        public const Int32 Auto_Buffer_Finished = 1;
        public const Int32 Auto_Buffer_Received_Response = 2;
        public const Int32 Auto_Buffer_Deactivated = 3;
        public const Int32 Auto_Buffer_UnInit = 4;

        public const Int32 Confocal_Measurement = 0;
        public const Int32 Interferometric_Measurement = 1;
        public const Int32 Encoder_Preload_Config_Once = 0;
        public const Int32 Encoder_Preload_Config_Eachtime = 1;
        public const Int32 Encoder_Preload_Config_Trigger_RisingEdge = 0;
        public const Int32 Encoder_Preload_Config_Trigger_FallingEdge = 2;
        public const Int32 Encoder_Preload_Config_Trigger_OnEdge = 0;
        public const Int32 Encoder_Preload_Config_Trigger_OnLevel = 4;
        public const Int32 Encoder_Preload_Config_Active = 8;
        public const Int32 Encoder_Trigger_Source_A0 = 0;
        public const Int32 Encoder_Trigger_Source_B0 = 1;
        public const Int32 Encoder_Trigger_Source_A1 = 2;
        public const Int32 Encoder_Trigger_Source_B1 = 3;
        public const Int32 Encoder_Trigger_Source_A2 = 4;
        public const Int32 Encoder_Trigger_Source_B2 = 5;
        public const Int32 Encoder_Trigger_Source_A3 = 6;
        public const Int32 Encoder_Trigger_Source_B3 = 7;
        public const Int32 Encoder_Trigger_Source_A4 = 8;
        public const Int32 Encoder_Trigger_Source_B4 = 9;
        public const Int32 Encoder_Trigger_Source_SyncIn = 10;
        public const Int32 Encoder_Trigger_Source_Quadrature = 15;
        public const Int32 Encoder_Trigger_Source_Immediate = 15;
        public const Int32 Device_Trigger_Mode_Free_Run = 0;
        public const Int32 Device_Trigger_Mode_Wait_Trigger = 1;
        public const Int32 Device_Trigger_Mode_Trigger_Each = 2;
        public const Int32 Device_Trigger_Mode_Trigger_Window = 3;
        public const Int32 Spectrum_Raw = 0;
        public const Int32 Spectrum_Confocal = 1;
        public const Int32 Spectrum_FT = 2;
        public const Int32 Spectrum_2D_Image = 3;
        public const Int32 Table_Confocal_Calibration = 1;
        public const Int32 Table_WaveLength = 2;
        public const Int32 Table_Refractive_Index = 3;
        public const Int32 Table_Dark_Correction = 4;
        public const Int32 Table_Confocal_Calibration_Multi_Channel = 5;
        public const Int32 Table_CLS_Mask = 6;
        public const Int32 Table_MPS_Mask = 8;


        public const UInt32 CHRLib_Flag_Rsp_Deactivate_Auto_Buffer = 1;
        public const UInt32 CHRLib_Flag_Auto_Change_Data_Buffer_Size = 2;
        public const UInt32 CHRLib_Flag_Check_Thread_ID = 4;

        public const Int32 Search_Both_Serial_TCPIP_Connection = 0;
        public const Int32 Search_Only_Serial_Connection = 1;
        public const Int32 Search_Only_TCPIP_Connection = 2;


        public const UInt32 OPID_Save_Performance = 0x5356504D;

        #endregion

        #region Struct Definition
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TErrorInfo
        {
            public Conn_h ConHandle;
            public Int32 ErrorCode;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TRspCallbackInfo
        {
            public IntPtr User;
            public IntPtr State;
            public Int32 Ticket;
            public UInt32 SourceConnection;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TResponseInfo
        {
            public UInt32 CmdID;
            public Int32 Ticket;
            public UInt32 Flag;
            public UInt32 ParamCount;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct TSampleSignalInfo
        {
            public UInt16 SignalID;
            public Int16 DataType;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TSampleSignalGeneralInfo
        {
            public UInt32 InfoIndex;
            public Int32 PeakSignalCount;
            public Int32 GlobalSignalCount;
            public Int32 ChannelCount;
        }
        #endregion

        #region Delegate Definition
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SampleDataCallback(IntPtr _pUser, Int32 _nState, Int64 _nSampleCount,
            IntPtr _pSampleBuffer, Int64 _nSizePerSample, TSampleSignalGeneralInfo _sSignalGeneralInfo, IntPtr _psSignalInfo);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ResponseAndUpdateCallback(TRspCallbackInfo _sInfo, Rsp_h _hRsp);
        #endregion

        #region Handle Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetHandleType(UInt32 _Handle);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyHandle(UInt32 _Handle);
        #endregion

        #region Result Checking Functions
        public static bool ResultSuccess(Res_t _nRes)
        {
            return (_nRes >= 0);
        }
        public static bool ResultInformation(Res_t _nRes)
        {
            return ((_nRes >> 30) == 1);
        }
        public static bool ResultWarning(Res_t _nRes)
        {
            return ((_nRes >> 30) == 2);
        }
        public static bool ResultError(Res_t _nRes)
        {
            return ((_nRes >> 30) == 3);
        }
        #endregion

        #region Error Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t LastErrors(Conn_h _hConnection, [In, Out] TErrorInfo[] _aErrorInfos, ref Int64 _pnBufSizeInBytes);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t ClearErrors(Conn_h _hConnection);
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t ErrorCodeToString(Int32 _nErrorCode, [MarshalAs(UnmanagedType.LPStr)] StringBuilder _strErrorString, ref Int64 _pnSize);
        #endregion

        #region Open/Close Connection Functions
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t OpenConnection([MarshalAs(UnmanagedType.LPStr)] string _strConnectionInfo, Int32 _nDeviceType, Int32 _eConnectionMode, Int64 _nDevBufSize, out Conn_h _pHandle);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t OpenSharedConnection(Conn_h _nExistingConnection, Int32 _eConnectionMode, out Conn_h _pHandle);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t CloseConnection(Conn_h _hConnection);
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetDeviceConnectionInfo(Conn_h _hConnection, [MarshalAs(UnmanagedType.LPStr)] StringBuilder _strConnectInfo, ref Int64 _pnSize);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 GetConnectionMode(Conn_h _hConnection);
        #endregion

        #region Device Info. Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 GetDeviceType(Conn_h _hConnection);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 GetDeviceChannelCount(Conn_h _hConnection);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetDeviceOutputSignals(Conn_h _hConnection, Int32[] _pSignalIDbuffer, out Int32 _pSignalCount);
        #endregion

        #region Sample Data Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetNextSamples(Conn_h _hConnection, ref Int64 _pSampleCount, out IntPtr _ppData, out Int64 _pnSizePerSample,
            out TSampleSignalGeneralInfo _pSignalGeneralInfo, out IntPtr _psSignalInfo);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetLastSample(Conn_h _hConnection, out IntPtr _ppData, out Int64 _pnSizePerSample,
            out TSampleSignalGeneralInfo _pSignalGeneralInfo, out IntPtr _psSignalInfo);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t RegisterSampleDataCallback(Conn_h _hConnection, Int64 _nReadSampleCount, Int32 _nReadSampleTimeOut, IntPtr _pUser, SampleDataCallback _pOnReadSample);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetCurrentAvailableSampleCount(Conn_h _hConnection, out Int64 _pnSampleCount);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetSingleOutputSampleSize(Conn_h _hConnection, out Int64 _pnSampleSize);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetConnectionOutputSignalInfos(
            Conn_h _hConnection, out TSampleSignalGeneralInfo _pSignalGeneralInfo, [In, Out] TSampleSignalInfo[] _pSignalInfoBuf, ref Int64 _pnSignalInfoBufSizeInBytes);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t StopConnectionDataStream(Conn_h _hConnection);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t StartConnectionDataStream(Conn_h _hConnection);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t SetOutputDataFormatMode(Conn_h _hConnection, Int32 _eMode);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetOutputDataFormatMode(Conn_h _hConnection);
        #endregion

        #region Auto Buffer Mode Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t ActivateAutoBufferMode(Conn_h _hConnection, IntPtr _pBuffer, Int64 _nSampleCount, ref Int64 _pBufferSize);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetAutoBufferSavedSampleCount(Conn_h _hConnection, out Int64 _pnSampleCount);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t DeactivateAutoBufferMode(Conn_h _hConnection);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 GetAutoBufferStatus(Conn_h _hConnection);
        #endregion

        #region Command Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t ExecCommand(UInt32 _hDest, Cmd_h _hCmd, out Rsp_h _hRsp);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t ExecCommandAsync(UInt32 _hDest, Cmd_h _hCmd, IntPtr _pUser, ResponseAndUpdateCallback _pCB, out Int32 _nTicket);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t RegisterGeneralResponseAndUpdateCallback(Conn_h _hConnection, IntPtr _pUser, ResponseAndUpdateCallback _pGenCBFct);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t NewCommand(UInt32 _nCmdID, Int32 _bQuery, out Cmd_h _hCmd);
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 CmdNameToID([MarshalAs(UnmanagedType.LPStr)] string _strCmdName, Int32 _nLength);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t AddCommandIntArg(Cmd_h _hCmd, Int32 _nArg);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t AddCommandFloatArg(Cmd_h _hCmd, float _nArg);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t AddCommandIntArrayArg(Cmd_h _hCmd, Int32[] _pArg, Int32 _nLength);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t AddCommandFloatArrayArg(Cmd_h _hCmd, float[] _pArg, Int32 _nLength);
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t AddCommandStringArg(Cmd_h _hCmd, [MarshalAs(UnmanagedType.LPStr)] string _pArg, Int32 _nLength);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t AddCommandBlobArg(Cmd_h _hCmd, byte[] _pArg, Int32 _nLength);
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t NewCommandFromString([MarshalAs(UnmanagedType.LPStr)] string _strCommand, out Cmd_h _hCmd);
        #endregion

        #region Response Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetResponseInfo(Rsp_h _hRsp, out TResponseInfo sRspInfo);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetResponseArgType(Rsp_h _hRsp, UInt32 _nIndex, out Int32 _pArgType);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetResponseIntArg(Rsp_h _hRsp, UInt32 _nIndex, out Int32 _pArg);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetResponseFloatArg(Rsp_h _hRsp, UInt32 _nIndex, out float _pArg);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetResponseIntArrayArg(Rsp_h _hRsp, UInt32 _nIndex, out IntPtr _pArg, out Int32 _pLength);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetResponseFloatArrayArg(Rsp_h _hRsp, UInt32 _nIndex, out IntPtr _pArg, out Int32 _pLength);
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetResponseStringArg(Rsp_h _hRsp, UInt32 _nIndex, out IntPtr _pArg, out Int32 _pLength);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t GetResponseBlobArg(Rsp_h _hRsp, UInt32 _nIndex, out IntPtr _pArg, out Int32 _pLength);
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t ResponseToString(Rsp_h _hRsp, [MarshalAs(UnmanagedType.LPStr)] StringBuilder _aResponseStr, ref Int64 _pnSize);
        #endregion

        #region Connection Output Processing Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t ProcessDeviceOutput(Conn_h _hConnection);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t StartAutomaticDeviceOutputProcessing(Conn_h _hConnection);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t StopAutomaticDeviceOutputProcessing(Conn_h _hConnection);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t FlushConnectionBuffer(Conn_h _hConnection);
        #endregion

        #region Lib Configuration Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t SetLibLogFileDirectory([MarshalAs(UnmanagedType.LPTStr)] string _pstrDirectory, Int64 _nMaxFileSize, Int32 _nMaxFileNumber);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t SetLibLogLevel(Int32 _nLevel);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLibConfigFlags(UInt32 _nFlag);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t SetSampleDataBufferSize(Conn_h _hConnection, Int64 _nBufferSize);
        #endregion

        #region Device Auto. Search Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t StartCHRDeviceAutoSearch(Int32 _nConnectionType, Int32 _bSBlockingSearch);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CancelCHRDeviceAutoSearch();
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t IsCHRDeviceAutoSearchFinished();
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t DetectedCHRDeviceInfo([MarshalAs(UnmanagedType.LPStr)] StringBuilder _strDeviceInfos, ref Int64 _pnSize);
        #endregion

        #region Upload Table Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t UploadConfocalCalibrationTableFromFile(Conn_h _hConnection, [MarshalAs(UnmanagedType.LPTStr)] string _strFullFileName, UInt32 _nProbeSerialNumber, UInt32 _nCHRTableIndex);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t UploadWaveLengthTableFromFile(Conn_h _hConnection, [MarshalAs(UnmanagedType.LPTStr)] string _strFullFileName);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t UploadRefractiveIndexTableFromFile(Conn_h _hConnection, [MarshalAs(UnmanagedType.LPTStr)] string _strFullFileName, [MarshalAs(UnmanagedType.LPStr)] string _strTableName, UInt32 _nCHRTableIndex, float _nRefSRI);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t UploadCHRocodileDemoMaskTable(Conn_h _hConnection, [MarshalAs(UnmanagedType.LPTStr)] string _strFullFileName);
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t UploadFirmware(Conn_h _hConnection, [MarshalAs(UnmanagedType.LPTStr)] string _strFullFileName);
        #endregion

        #region Extra Data Processing Functions
        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t SetCHRocodileDemoProfileInterpolation(Conn_h _hConnection, Int32 _nMaxHoleSize);
        #endregion

        #region Plugin
        [DllImport(DLL_Filename, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t AddConnectionPlugIn(Conn_h _hConnection, [MarshalAs(UnmanagedType.LPStr)] string _strPluginName, out Plugin_h _hPlugin);
        #endregion

        #region Perform Lib Operation
        public const Int32 Performance_Snapshot_Mode = 0;
        public const Int32 Performance_Complete_Mode = 1;

        [DllImport(DLL_Filename, CallingConvention = CallingConvention.Cdecl)]
        public static extern Res_t PerformLibOperation(Conn_h _hConnection, Cmd_h _hCmd, out Rsp_h _phRsp);
        #endregion

    }
}