/*
TCHRLibCmdWrapper provides classes/functions related to CHR command/response.
Command/Response classes fall into two different categories: general and specific.
General classes can be used to construct any arbitary command (TCommand class) 
and read out response from the response handle passed over from CHRocodileLib (TResponse class).
Specific classes are only for one special command, e.g. TOutputSignalsCmd, TOutputSignalsRsp are only for setting/getting output signals. 
General and specific classes all inherit from the same interface.
"ExecCommand"/"ExecCommandAsync" function, which are provided here, uses above defined command/response c# interface.
These classes/functions aim to simplify the procedure of command sending and response processing.
*/

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace PrecitecClass
{
    public static class TCHRLibCmdWrapper
    {
        #region Response interface
        //Response interface
        public interface IBaseRsp
        {
            string ResponseName
            {
                get;
            }
            UInt32 ResponseID
            {
                get;
            }
            Int32 ParamCount
            {
                get;
            }
            Int32 Ticket
            {
                get;
            }
            TCHRLibFunctionWrapper.Conn_h SourceConnection
            {
                get;
            }
            bool QueryRsp
            {
                get;
            }
            Int32 RspState
            {
                get;
            }
            void SetRspState(Int32 _nState);
            string GetWholeResponseAsString();
        };
        #endregion


        #region Command interface
        //command interface
        public interface IBaseCmd
        {
            TCHRLibFunctionWrapper.Cmd_h CmdHandle
            {
                get;
            }
            string CmdName
            {
                get;
            }
        };
        #endregion


        #region General response class 
        //general response class
        public class TResponse : IBaseRsp
        {
            private List<Int32> m_aParamTypeInfo = new List<Int32>();
            private List<Object> m_aParams = new List<Object>();
            private string m_strCmdName;
            private TCHRLibFunctionWrapper.Rsp_h m_hRsp;
            private TCHRLibFunctionWrapper.TRspCallbackInfo m_sCallbackInfo;
            private TCHRLibFunctionWrapper.TResponseInfo m_sRspInfo;
            private StringBuilder m_sCmdResponse;
            public string ResponseName
            {
                get
                {
                    return m_strCmdName;
                }
            }
            public UInt32 ResponseID
            {
                get
                {
                    return (m_sRspInfo.CmdID);
                }
            }
            public TResponse(TCHRLibFunctionWrapper.Conn_h _hConn, Int32 _nTicket, TCHRLibFunctionWrapper.Rsp_h _hRsp)
            {
                m_sCallbackInfo.User = IntPtr.Zero;
                m_sCallbackInfo.State = IntPtr.Zero;
                m_sCallbackInfo.Ticket = _nTicket;
                m_sCallbackInfo.SourceConnection = _hConn;
                m_hRsp = _hRsp;
                ReadResponse(_hRsp);
            }


            public TResponse(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, TCHRLibFunctionWrapper.Rsp_h _hRsp)
            {
                m_sCallbackInfo = _sCbInfo;
                m_hRsp = _hRsp;
                ReadResponse(_hRsp);
            }

            private void ReadResponse(TCHRLibFunctionWrapper.Rsp_h _hRsp)
            {
                TCHRLibFunctionWrapper.GetResponseInfo(_hRsp, out m_sRspInfo);

                byte[] aCmdBytes = BitConverter.GetBytes(m_sRspInfo.CmdID);
                if (aCmdBytes[3] == 0)
                    m_strCmdName = Encoding.ASCII.GetString(aCmdBytes, 0, 3);
                else
                    m_strCmdName = Encoding.ASCII.GetString(aCmdBytes);
                if (RspState == -1)
                    m_sCmdResponse = new StringBuilder("Error in " + m_strCmdName);
                else
                    m_sCmdResponse = new StringBuilder(m_strCmdName);
                if (QueryRsp)
                    m_sCmdResponse.Append("?");
                m_sCmdResponse.Append(" ");
                for (UInt32 i = 0; i < m_sRspInfo.ParamCount; i++)
                {
                    Int32 nType;
                    TCHRLibFunctionWrapper.GetResponseArgType(m_hRsp, i, out nType);
                    m_aParamTypeInfo.Add(nType);
                    switch (nType)
                    {
                        case TCHRLibFunctionWrapper.Rsp_Param_Type_Integer:
                            {
                                Int32 nIntParam;
                                TCHRLibFunctionWrapper.GetResponseIntArg(m_hRsp, i, out nIntParam);
                                m_aParams.Add(nIntParam);
                                m_sCmdResponse.Append(nIntParam.ToString() + " ");
                                break;
                            }
                        case TCHRLibFunctionWrapper.Rsp_Param_Type_Float:
                            {
                                float nFloatParam;
                                TCHRLibFunctionWrapper.GetResponseFloatArg(m_hRsp, i, out nFloatParam);
                                m_aParams.Add(nFloatParam);
                                m_sCmdResponse.Append(nFloatParam.ToString() + " ");
                                break;
                            }
                        case TCHRLibFunctionWrapper.Rsp_Param_Type_Integer_Array:
                            {
                                IntPtr pData;
                                Int32 nLength;
                                TCHRLibFunctionWrapper.GetResponseIntArrayArg(m_hRsp, i, out pData, out nLength);
                                Int32[] aIntArray = new Int32[nLength];
                                if (nLength > 0)
                                    Marshal.Copy(pData, aIntArray, 0, nLength);
                                m_aParams.Add(aIntArray);
                                for (Int32 k = 0; k < nLength; k++)
                                    m_sCmdResponse.Append(aIntArray[k].ToString() + " ");
                                break;
                            }
                        case TCHRLibFunctionWrapper.Rsp_Param_Type_Float_Array:
                            {
                                IntPtr pData;
                                Int32 nLength;
                                TCHRLibFunctionWrapper.GetResponseFloatArrayArg(m_hRsp, i, out pData, out nLength);
                                float[] aFloatArray = new float[nLength];
                                if (nLength > 0)
                                    Marshal.Copy(pData, aFloatArray, 0, nLength);
                                m_aParams.Add(aFloatArray);
                                for (Int32 k = 0; k < nLength; k++)
                                    m_sCmdResponse.Append(aFloatArray[k].ToString() + " ");
                                break;
                            }
                        case TCHRLibFunctionWrapper.Rsp_Param_Type_String:
                            {
                                IntPtr pData;
                                Int32 nLength;
                                TCHRLibFunctionWrapper.GetResponseStringArg(m_hRsp, i, out pData, out nLength);
                                string sParam = "";
                                if (nLength > 0)
                                    sParam = Marshal.PtrToStringAnsi(pData, nLength);
                                m_aParams.Add(sParam);
                                m_sCmdResponse.Append(sParam + " ");
                                break;
                            }
                        case TCHRLibFunctionWrapper.Rsp_Param_Type_Byte_Array:
                            {
                                IntPtr pData;
                                Int32 nLength;
                                TCHRLibFunctionWrapper.GetResponseBlobArg(m_hRsp, i, out pData, out nLength);
                                byte[] sByteArray = new Byte[nLength];
                                if (nLength > 0)
                                    Marshal.Copy(pData, sByteArray, 0, nLength);
                                m_aParams.Add(sByteArray);
                                m_sCmdResponse.Append("blob data ");
                                break;
                            }
                    }
                }
            }

            //functions to get response parameters
            public Object GetParameter(Int32 _nIndex, out Int32 _nParamType)
            {
                _nParamType = TCHRLibFunctionWrapper.Rsp_Param_Type_Integer;
                if (_nIndex >= m_aParams.Count)
                    throw new TRspException("Current parameter with index " + _nIndex + " does not exist!");
                _nParamType = m_aParamTypeInfo[_nIndex];
                return (m_aParams[_nIndex]);
            }
            public Object GetParameter(Int32 _nIndex)
            {
                Int32 nParamType;
                return (GetParameter(_nIndex, out nParamType));
            }
            public Int32 GetIntParameter(Int32 _nIndex)
            {
                Int32 nType;
                var oParam = GetParameter(_nIndex, out nType);
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Integer)
                    return ((Int32)(oParam));
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Float)
                    return ((Int32)((float)(oParam)));
                else
                    throw new TRspException("Current parameter cannot be output as integer.");
            }

            public float GetFloatParameter(Int32 _nIndex)
            {
                Int32 nType;
                var oParam = GetParameter(_nIndex, out nType);
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Integer)
                    return ((float)((Int32)(oParam)));
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Float)
                    return ((float)(oParam));
                else
                    throw new TRspException("Current parameter cannot be output as float.");
            }

            public Int32[] GetIntArrayParameter(Int32 _nIndex)
            {
                Int32 nType;
                var oParam = GetParameter(_nIndex, out nType);
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Integer_Array)
                    return ((Int32[])(oParam));
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Float_Array)
                {
                    float[] aTemp = (float[])(oParam);
                    Int32[] aParam = new Int32[aTemp.Length];
                    for (Int32 i = 0; i < aTemp.Length; i++)
                        aParam[i] = (Int32)(aTemp[i]);
                }
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Integer)
                {
                    Int32[] aParam = new Int32[1];
                    aParam[0] = (Int32)(oParam);
                    return (aParam);
                }
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Float)
                {
                    Int32[] aParam = new Int32[1];
                    aParam[0] = (Int32)((float)(oParam));
                    return (aParam);
                }
                else
                    throw new TRspException("Current parameter cannot be output as integer array.");
            }

            public float[] GetFloatArrayParameter(Int32 _nIndex)
            {
                Int32 nType;
                var oParam = GetParameter(_nIndex, out nType);
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Float_Array)
                    return ((float[])(oParam));
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Integer_Array)
                {
                    Int32[] aTemp = (Int32[])(oParam);
                    float[] aParam = new float[aTemp.Length];
                    for (Int32 i = 0; i < aTemp.Length; i++)
                        aParam[i] = (float)(aTemp[i]);
                }
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Float)
                {
                    float[] aParam = new float[1];
                    aParam[0] = (float)(oParam);
                    return (aParam);
                }
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Integer)
                {
                    float[] aParam = new float[1];
                    aParam[0] = (float)((Int32)(oParam));
                    return (aParam);
                }
                else
                    throw new TRspException("Current parameter cannot be output as float array.");
            }

            public string GetStringParameter(Int32 _nIndex)
            {
                Int32 nType;
                var oParam = GetParameter(_nIndex, out nType);
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_String)
                    return ((string)(oParam));
                else
                    throw new TRspException("Current parameter cannot be output as string.");
            }

            public byte[] GetBlobParameter(Int32 _nIndex)
            {
                Int32 nType;
                var oParam = GetParameter(_nIndex, out nType);
                if (nType == TCHRLibFunctionWrapper.Rsp_Param_Type_Byte_Array)
                    return ((byte[])(oParam));
                else
                    throw new TRspException("Current parameter cannot be output as Blob.");
            }

            public string GetWholeResponseAsString()
            {
                return (m_sCmdResponse.ToString());
            }
            public Int32 ParamCount
            {
                get
                {
                    return (m_aParams.Count);
                }
            }
            public Int32 Ticket
            {
                get
                {
                    return (m_sRspInfo.Ticket);
                }
            }
            public TCHRLibFunctionWrapper.Conn_h SourceConnection
            {
                get
                {
                    return (m_sCallbackInfo.SourceConnection);
                }
            }
            public bool QueryRsp
            {
                get
                {
                    return ((m_sRspInfo.Flag & TCHRLibFunctionWrapper.Rsp_Flag_Query) != 0);
                }
            }
            public Int32 RspState
            {
                get
                {
                    if (m_sCallbackInfo.State != IntPtr.Zero)
                        return (Marshal.ReadInt32(m_sCallbackInfo.State));
                    else
                        return (0);
                }
            }
            public void SetRspState(Int32 _nState)
            {
                if (m_sCallbackInfo.State != IntPtr.Zero)
                    Marshal.WriteInt32(m_sCallbackInfo.State, _nState);
            }
        };
        #endregion


        #region General command class
        //general command class
        public class TCommand : IBaseCmd
        {
            private TCHRLibFunctionWrapper.Cmd_h m_hCmd;
            private UInt32 m_nCmdID;
            //constructor with command name
            public TCommand(string _strCmdName, bool _bQueryCmd)
            {
                string strCmd;
                if (_strCmdName[0] == '$')
                    strCmd = _strCmdName.Substring(1);
                else
                    strCmd = _strCmdName;
                var aTempCmd = Encoding.ASCII.GetBytes(_strCmdName);
                Byte[] aTempCmd2 = new Byte[4];
                aTempCmd.CopyTo(aTempCmd2, 0);
                m_nCmdID = BitConverter.ToUInt32(aTempCmd2, 0);
                TCHRLibFunctionWrapper.NewCommand(m_nCmdID, _bQueryCmd ? 1 : 0, out m_hCmd);
            }
            //constructor with command ID
            public TCommand(UInt32 _nCmdID, bool _bQueryCmd)
            {
                m_nCmdID = _nCmdID;
                TCHRLibFunctionWrapper.NewCommand(m_nCmdID, _bQueryCmd ? 1 : 0, out m_hCmd);
            }
            public TCHRLibFunctionWrapper.Cmd_h CmdHandle
            {
                get
                {
                    return (m_hCmd);
                }
            }

            public string CmdName
            {
                get
                {
                    var aTemp = BitConverter.GetBytes(m_nCmdID);
                    return (Encoding.ASCII.GetString(aTemp));
                }
            }
            //Add parameter to the command
            public void AddParameter(object _oObject)
            {
                Type t = _oObject.GetType();
                Int32 nParamType = TCHRLibFunctionWrapper.Rsp_Param_Type_Integer;
                if (t.Equals(typeof(float)))
                    nParamType = TCHRLibFunctionWrapper.Rsp_Param_Type_Float;
                else if (t.Equals(typeof(Int32[])))
                    nParamType = TCHRLibFunctionWrapper.Rsp_Param_Type_Integer_Array;
                else if (t.Equals(typeof(float[])))
                    nParamType = TCHRLibFunctionWrapper.Rsp_Param_Type_Float_Array;
                else if (t.Equals(typeof(byte[])))
                    nParamType = TCHRLibFunctionWrapper.Rsp_Param_Type_Byte_Array;
                else if (t.Equals(typeof(string)))
                    nParamType = TCHRLibFunctionWrapper.Rsp_Param_Type_String;
                switch (nParamType)
                {
                    case TCHRLibFunctionWrapper.Rsp_Param_Type_Integer:
                        {
                            Int32 nIntParam = Convert.ToInt32(_oObject);
                            TCHRLibFunctionWrapper.AddCommandIntArg(m_hCmd, nIntParam);
                            break;
                        }
                    case TCHRLibFunctionWrapper.Rsp_Param_Type_Float:
                        {
                            float nFloatParam = Convert.ToSingle(_oObject);
                            TCHRLibFunctionWrapper.AddCommandFloatArg(m_hCmd, nFloatParam);
                            break;
                        }
                    case TCHRLibFunctionWrapper.Rsp_Param_Type_Integer_Array:
                        {
                            Int32[] sIntAParam = (Int32[])(_oObject);
                            TCHRLibFunctionWrapper.AddCommandIntArrayArg(m_hCmd, sIntAParam, sIntAParam.Length);
                            break;
                        }
                    case TCHRLibFunctionWrapper.Rsp_Param_Type_Float_Array:
                        {
                            float[] sFloatAParam = (float[])(_oObject);
                            TCHRLibFunctionWrapper.AddCommandFloatArrayArg(m_hCmd, sFloatAParam, sFloatAParam.Length);
                            break;
                        }
                    case TCHRLibFunctionWrapper.Rsp_Param_Type_String:
                        {
                            string sParam = (string)(_oObject);
                            TCHRLibFunctionWrapper.AddCommandStringArg(m_hCmd, sParam, sParam.Length);
                            break;
                        }
                    case TCHRLibFunctionWrapper.Rsp_Param_Type_Byte_Array:
                        {
                            byte[] sByteAParam = (byte[])(_oObject);
                            TCHRLibFunctionWrapper.AddCommandBlobArg(m_hCmd, sByteAParam, sByteAParam.Length);
                            break;
                        }
                    default:
                        return;
                }
            }
        };
        #endregion


        #region Response exception
        public class TRspException : Exception
        {
            public TRspException(string Message) : base(Message)
            {
                Console.WriteLine("Rsp Error: " + Message);
            }
        }
        #endregion


        #region Base command class for different specific commands
        public class TBaseCmd : IBaseCmd
        {
            protected TCommand m_oCmdWriter;
            public TBaseCmd(string _strCmdName, bool _bQueryCmd)
            {
                m_oCmdWriter = new TCommand(_strCmdName, _bQueryCmd);
            }
            public TBaseCmd(UInt32 _nCmdID, bool _bQueryCmd)
            {
                m_oCmdWriter = new TCommand(_nCmdID, _bQueryCmd);
            }
            public TCHRLibFunctionWrapper.Cmd_h CmdHandle
            {
                get
                {
                    return (m_oCmdWriter.CmdHandle);
                }
            }
            public string CmdName
            {
                get
                {
                    return (m_oCmdWriter.CmdName);
                }
            }
        };
        #endregion


        #region Base response class for different specific responses
        public abstract class TBaseRsp : IBaseRsp
        {
            protected UInt32 m_nTargetCmdID;

            protected TResponse m_oRspReader;



            public TBaseRsp(UInt32 _nTargetCmdID, TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, TCHRLibFunctionWrapper.Rsp_h _hRsp)
            {
                m_nTargetCmdID = _nTargetCmdID;
                m_oRspReader = new TResponse(_sCbInfo, _hRsp);
            }
            public TBaseRsp(UInt32 _nTargetCmdID, TResponse _oRspReader)
            {
                m_nTargetCmdID = _nTargetCmdID;
                m_oRspReader = _oRspReader;
            }
            protected void CheckRspValid()
            {
                if (m_oRspReader.ResponseID != m_nTargetCmdID)
                    throw new TRspException("Wrong response type, response ID is not correct.");
            }
            public string ResponseName
            {
                get
                {
                    return (m_oRspReader.ResponseName);
                }
            }
            public UInt32 ResponseID
            {
                get
                {
                    return (m_oRspReader.ResponseID);
                }
            }
            public Int32 ParamCount
            {
                get
                {
                    return (m_oRspReader.ParamCount);
                }
            }
            public Int32 Ticket
            {
                get
                {
                    return (m_oRspReader.Ticket);
                }
            }
            public TCHRLibFunctionWrapper.Conn_h SourceConnection
            {
                get
                {
                    return (m_oRspReader.SourceConnection);
                }
            }
            public bool QueryRsp
            {
                get
                {
                    return (m_oRspReader.QueryRsp);
                }
            }
            public Int32 RspState
            {
                get
                {
                    return (m_oRspReader.RspState);
                }
            }
            public void SetRspState(Int32 _nState)
            {
                m_oRspReader.SetRspState(_nState);
            }
            public string GetWholeResponseAsString()
            {
                return (m_oRspReader.GetWholeResponseAsString());
            }
        }
        #endregion


        //followings are different specific command and response classes

        #region Output Signls Cmd/Rsp class
        public class TOutputSignalsCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Output_Signals;
            public TOutputSignalsCmd(bool _bQuery, Int32[] _aSignals = null) :
            base(CMD_ID, _bQuery)
            {
                if ((!_bQuery) && (_aSignals != null) && (_aSignals.Length > 0))
                    m_oCmdWriter.AddParameter(_aSignals);
            }
        };
        public class TOutputSignalsRsp : TBaseRsp
        {
            public TOutputSignalsRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Output_Signals, _sCbInfo, _hRsp)
            { }
            public TOutputSignalsRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Output_Signals, _oReader)
            { }
            public Int32[] Signals
            {
                get
                {
                    CheckRspValid();
                    if (m_oRspReader.ParamCount > 0)
                    {
                        return (m_oRspReader.GetIntArrayParameter(0));
                    }
                    else
                        return (new Int32[0]);
                }
            }
        };
        #endregion


        #region Firmware version Cmd/Rsp class
        public class TFirmwareVersionCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Firmware_Version;
            public TFirmwareVersionCmd() :
            base(CMD_ID, false)
            {
            }
        };
        public class TFirmwareVersionRsp : TBaseRsp
        {
            public TFirmwareVersionRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Firmware_Version, _sCbInfo, _hRsp)
            { }
            public TFirmwareVersionRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Firmware_Version, _oReader)
            { }
            public string FirmwareVersion
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetStringParameter(0));
                }
            }
        };
        #endregion


        #region Measuring method Cmd/Rsp class
        public class TMeasuringMethodCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Measuring_Method;
            public TMeasuringMethodCmd(bool _bQuery, Int32 _nMeasuringMethod = TCHRLibFunctionWrapper.Confocal_Measurement) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nMeasuringMethod);
            }
        };
        public class TMeasuringMethodRsp : TBaseRsp
        {
            public TMeasuringMethodRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Measuring_Method, _sCbInfo, _hRsp)
            { }
            public TMeasuringMethodRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Measuring_Method, _oReader)
            { }
            public Int32 MeasuringMethod
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
        };
        #endregion


        #region Full scale Cmd/Rsp class
        public class TFullScaleCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Full_Scale;
            public TFullScaleCmd() :
            base(CMD_ID, false)
            {
            }
        };
        public class TFullScaleRsp : TBaseRsp
        {
            public TFullScaleRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Full_Scale, _sCbInfo, _hRsp)
            { }
            public TFullScaleRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Full_Scale, _oReader)
            { }
            public Int32 FullScale
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
        };
        #endregion


        #region Scan rate Cmd/Rsp class
        public class TScanRateCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Scan_Rate;
            public TScanRateCmd(bool _bQuery, float _nScanRate = 4000) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nScanRate);
            }
        };
        public class TScanRateRsp : TBaseRsp
        {
            public TScanRateRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Scan_Rate, _sCbInfo, _hRsp)
            { }
            public TScanRateRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Scan_Rate, _oReader)
            { }
            public float ScanRate
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(0));
                }
            }
        };
        #endregion


        #region Data average Cmd/Rsp class
        public class TDataAverageCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Data_Average;
            public TDataAverageCmd(bool _bQuery, Int32 _nDataAverage = 1) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nDataAverage);
            }
        };
        public class TDataAverageRsp : TBaseRsp
        {
            public TDataAverageRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Data_Average, _sCbInfo, _hRsp)
            { }
            public TDataAverageRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Data_Average, _oReader)
            { }
            public Int32 DataAverage
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
        };
        #endregion


        #region Spectrum average Cmd/Rsp class
        public class TSpectrumAverageCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Spectrum_Average;
            public TSpectrumAverageCmd(bool _bQuery, Int32 _nSpectrumAverage = 1) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nSpectrumAverage);
            }
        };
        public class TSpectrumAverageRsp : TBaseRsp
        {
            public TSpectrumAverageRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Spectrum_Average, _sCbInfo, _hRsp)
            { }
            public TSpectrumAverageRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Spectrum_Average, _oReader)
            { }
            public Int32 SpectrumAverage
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
        };
        #endregion


        #region Refrative indices Cmd/Rsp class
        public class TRefractiveIndicesCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Refractive_Indices;
            public TRefractiveIndicesCmd(bool _bQuery, float[] _aRefractiveIndices = null) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery && (_aRefractiveIndices != null))
                    m_oCmdWriter.AddParameter(_aRefractiveIndices);
            }
        };
        public class TRefractiveIndicesRsp : TBaseRsp
        {
            public TRefractiveIndicesRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Refractive_Indices, _sCbInfo, _hRsp)
            { }
            public TRefractiveIndicesRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Refractive_Indices, _oReader)
            { }
            public float[] RefractiveIndices
            {
                get
                {
                    CheckRspValid();
                    if (m_oRspReader.ParamCount > 0)
                        return (m_oRspReader.GetFloatArrayParameter(0));
                    else
                        return (new float[0]);
                }
            }
        };
        #endregion


        #region Abbe numbers Cmd/Rsp class
        public class TAbbeNumbersCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Abbe_Numbers;
            public TAbbeNumbersCmd(bool _bQuery, float[] _aAbbeNumbers = null) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery && (_aAbbeNumbers != null))
                    m_oCmdWriter.AddParameter(_aAbbeNumbers);
            }
        };
        public class TAbbeNumbersRsp : TBaseRsp
        {
            public TAbbeNumbersRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Abbe_Numbers, _sCbInfo, _hRsp)
            { }
            public TAbbeNumbersRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Abbe_Numbers, _oReader)
            { }
            public float[] AbbeNumbers
            {
                get
                {
                    CheckRspValid();
                    if (m_oRspReader.ParamCount > 0)
                        return (m_oRspReader.GetFloatArrayParameter(0));
                    else
                        return (new float[0]);
                }
            }
        };
        #endregion


        #region Refractive index tables Cmd/Rsp class
        public class TRefractiveIndexTablesCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Refractive_Index_Tables;
            public TRefractiveIndexTablesCmd(bool _bQuery, Int32[] _aTableIndices) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery && (_aTableIndices != null))
                    m_oCmdWriter.AddParameter(_aTableIndices);
            }
        };
        public class TRefractiveIndexTablesRsp : TBaseRsp
        {
            public TRefractiveIndexTablesRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Refractive_Index_Tables, _sCbInfo, _hRsp)
            { }
            public TRefractiveIndexTablesRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Refractive_Index_Tables, _oReader)
            { }
            public Int32[] TableIndices
            {
                get
                {
                    CheckRspValid();
                    if (m_oRspReader.ParamCount > 0)
                        return (m_oRspReader.GetIntArrayParameter(0));
                    else
                        return (new Int32[0]);
                }
            }
        };
        #endregion


        #region Lamp intensity Cmd/Rsp class
        public class TLampIntensityCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Lamp_Intensity;
            public TLampIntensityCmd(bool _bQuery, float _nLampIntensity = 100.0f) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nLampIntensity);
            }
        };
        public class TLampIntensityRsp : TBaseRsp
        {
            public TLampIntensityRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Lamp_Intensity, _sCbInfo, _hRsp)
            { }
            public TLampIntensityRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Lamp_Intensity, _oReader)
            { }
            public float LampIntensity
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(0));
                }
            }
        };
        #endregion


        #region Optical probe Cmd/Rsp class
        public class TOpticalProbeCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Optical_Probe;
            public TOpticalProbeCmd(bool _bQuery, Int32 _nOpticalProbe = 0) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nOpticalProbe);
            }
        };
        public class TOpticalProbeRsp : TBaseRsp
        {
            public TOpticalProbeRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Optical_Probe, _sCbInfo, _hRsp)
            { }
            public TOpticalProbeRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Optical_Probe, _oReader)
            { }
            public Int32 OpticalProbe
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
        };
        #endregion


        #region Confocal detection threshold Cmd/Rsp class
        public class TConfocalDetectionThresholdCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Confocal_Detection_Threshold;
            public TConfocalDetectionThresholdCmd(bool _bQuery, float _nThreshold = 10) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nThreshold);
            }
        };
        public class TConfocalDetectionThresholdRsp : TBaseRsp
        {
            public TConfocalDetectionThresholdRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Confocal_Detection_Threshold, _sCbInfo, _hRsp)
            { }
            public TConfocalDetectionThresholdRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Confocal_Detection_Threshold, _oReader)
            { }
            public float Threshold
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(0));
                }
            }
        };
        #endregion


        #region Interferometric quality threshold Cmd/Rsp class
        public class TInterQualityThresholdCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Interferometric_Quality_Threshold;
            public TInterQualityThresholdCmd(bool _bQuery, float _nThreshold = 10) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nThreshold);
            }
        };
        public class TInterQualityThresholdRsp : TBaseRsp
        {
            public TInterQualityThresholdRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Interferometric_Quality_Threshold, _sCbInfo, _hRsp)
            { }
            public TInterQualityThresholdRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Interferometric_Quality_Threshold, _oReader)
            { }
            public float Threshold
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(0));
                }
            }
        };
        #endregion


        #region Duty cycle Cmd/Rsp class
        public class TDutyCycleCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Duty_Cycle;
            public TDutyCycleCmd(bool _bQuery, float _nDutyCycle = 100) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nDutyCycle);
            }
        };
        public class TDutyCycleRsp : TBaseRsp
        {
            public TDutyCycleRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Duty_Cycle, _sCbInfo, _hRsp)
            { }
            public TDutyCycleRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Duty_Cycle, _oReader)
            { }
            public float DutyCycle
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(0));
                }
            }
        };
        #endregion


        #region Detection window active Cmd/Rsp class
        public class TDetectionWindowActiveCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Detection_Window_Active;
            public TDetectionWindowActiveCmd(bool _bQuery, bool _bActive = false) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_bActive ? 1 : 0);
            }
        };
        public class TDetectionWindowActiveRsp : TBaseRsp
        {
            public TDetectionWindowActiveRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Detection_Window_Active, _sCbInfo, _hRsp)
            { }
            public TDetectionWindowActiveRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Detection_Window_Active, _oReader)
            { }
            public bool Active
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0) != 0);
                }
            }
        };
        #endregion


        #region Detection window Cmd/Rsp class
        public class TDetectionWindowCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Detection_Window;
            public TDetectionWindowCmd(bool _bQuery, float[] _aWindows = null) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery && (_aWindows != null))
                    m_oCmdWriter.AddParameter(_aWindows);
            }
        };
        public class TDetectionWindowRsp : TBaseRsp
        {
            public TDetectionWindowRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Detection_Window, _sCbInfo, _hRsp)
            { }
            public TDetectionWindowRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Detection_Window, _oReader)
            { }
            public float[] Windows
            {
                get
                {
                    CheckRspValid();
                    if (m_oRspReader.ParamCount > 0)
                        return (m_oRspReader.GetFloatArrayParameter(0));
                    else
                        return (new float[0]);
                }
            }
        };
        #endregion


        #region Number of peaks Cmd/Rsp class
        public class TNumberOfPeaksCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Number_Of_Peaks;
            public TNumberOfPeaksCmd(bool _bQuery, Int32 _nNumber = 1) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nNumber);
            }
        };
        public class TNumberOfPeaksRsp : TBaseRsp
        {
            public TNumberOfPeaksRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Number_Of_Peaks, _sCbInfo, _hRsp)
            { }
            public TNumberOfPeaksRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Number_Of_Peaks, _oReader)
            { }
            public Int32 NumberOfPeaks
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
        };
        #endregion


        #region Peak ordering Cmd/Rsp class
        public class TPeakOrderingCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Peak_Ordering;
            public TPeakOrderingCmd(bool _bQuery, Int32 _nPeakOrdering = 0) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_nPeakOrdering);
            }
        };
        public class TPeakOrderingRsp : TBaseRsp
        {
            public TPeakOrderingRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Peak_Ordering, _sCbInfo, _hRsp)
            { }
            public TPeakOrderingRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Peak_Ordering, _oReader)
            { }
            public Int32 PeakOrdering
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
        };
        #endregion


        #region Dark reference Cmd/Rsp class
        public class TDarkReferenceCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Dark_Reference;
            public TDarkReferenceCmd() :
            base(CMD_ID, false)
            {
            }
        };
        public class TDarkReferenceRsp : TBaseRsp
        {
            public TDarkReferenceRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Dark_Reference, _sCbInfo, _hRsp)
            { }
            public TDarkReferenceRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Dark_Reference, _oReader)
            { }
            public float MinFrequency
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(0));
                }
            }
        };
        #endregion


        #region Start data stream Cmd/Rsp class
        public class TStartDataStreamCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Start_Data_Stream;
            public TStartDataStreamCmd() :
            base(CMD_ID, false)
            {
            }
        };
        public class TStartDataStreamRsp : TBaseRsp
        {
            public TStartDataStreamRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Start_Data_Stream, _sCbInfo, _hRsp)
            { }
            public TStartDataStreamRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Start_Data_Stream, _oReader)
            { }
        };
        #endregion


        #region Stop data stream Cmd/Rsp class
        public class TStopDataStreamCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Stop_Data_Stream;
            public TStopDataStreamCmd() :
            base(CMD_ID, false)
            {
            }
        };
        public class TStopDataStreamRsp : TBaseRsp
        {
            public TStopDataStreamRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Stop_Data_Stream, _sCbInfo, _hRsp)
            { }
            public TStopDataStreamRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Stop_Data_Stream, _oReader)
            { }
        };
        #endregion


        #region Light source auto adapt Cmd/Rsp class
        public class TLightSourceAutoAdaptCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Light_Source_Auto_Adapt;
            public TLightSourceAutoAdaptCmd(bool _bQuery, Int32 _bAutoAdapt = 0, float _nLevel = 100) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                {
                    m_oCmdWriter.AddParameter(_bAutoAdapt);
                    m_oCmdWriter.AddParameter(_nLevel);
                }
            }
        };
        public class TLightSourceAutoAdaptRsp : TBaseRsp
        {
            public TLightSourceAutoAdaptRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Light_Source_Auto_Adapt, _sCbInfo, _hRsp)
            { }
            public TLightSourceAutoAdaptRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Light_Source_Auto_Adapt, _oReader)
            { }
            public Int32 AutoAdapt
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public float Level
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(1));
                }
            }
        };
        #endregion


        #region CCD range Cmd/Rsp class
        public class TCCDRangeCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_CCD_Range;
            public TCCDRangeCmd(bool _bQuery, Int32 _StartPixel = 0, Int32 _StopPixel = 1000) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                {
                    m_oCmdWriter.AddParameter(_StartPixel);
                    m_oCmdWriter.AddParameter(_StopPixel);
                }
            }
        };
        public class TCCDRangeRsp : TBaseRsp
        {
            public TCCDRangeRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_CCD_Range, _sCbInfo, _hRsp)
            { }
            public TCCDRangeRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_CCD_Range, _oReader)
            { }
            public Int32 StartPixel
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 StopPixel
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
        };
        #endregion


        #region Median Cmd/Rsp class
        public class TMedianCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Median;
            public TMedianCmd(bool _bQuery, Int32 _nMedianWidth = 1, float _nPercentile = 50) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                {
                    m_oCmdWriter.AddParameter(_nMedianWidth);
                    m_oCmdWriter.AddParameter(_nPercentile);
                }
            }
        };
        public class TMedianRsp : TBaseRsp
        {
            public TMedianRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Median, _sCbInfo, _hRsp)
            { }
            public TMedianRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Median, _oReader)
            { }
            public Int32 MedianWidth
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public float Percentile
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(1));
                }
            }
        };
        #endregion


        #region Ananlog output Cmd/Rsp class
        public class TAnalogOutputCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Analog_Output;
            public TAnalogOutputCmd(bool _bQuery, Int32 _nIndex, Int32 _nSignalID = 256,
                float _nMin = 0, float _nMax = 1000, float _nVolMin = -10, float _nVolMax = 10, float _nVolInvalid = -10) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                {
                    m_oCmdWriter.AddParameter(_nIndex);
                    m_oCmdWriter.AddParameter(_nSignalID);
                    m_oCmdWriter.AddParameter(_nMin);
                    m_oCmdWriter.AddParameter(_nMax);
                    m_oCmdWriter.AddParameter(_nVolMin);
                    m_oCmdWriter.AddParameter(_nVolMax);
                    m_oCmdWriter.AddParameter(_nVolInvalid);
                }
            }
        };
        public class TAnalogOutputRsp : TBaseRsp
        {
            public TAnalogOutputRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Analog_Output, _sCbInfo, _hRsp)
            { }
            public TAnalogOutputRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Analog_Output, _oReader)
            { }
            public Int32 Index
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 SignalID
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
            public float Min
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(2));
                }
            }
            public float Max
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(3));
                }
            }
            public float VolMin
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(4));
                }
            }
            public float VolMax
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(5));
                }
            }
            public float VolInvalid
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(6));
                }
            }
        };
        #endregion


        #region Encoder counter Cmd/Rsp class
        public class TEncoderCounterCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Encoder_Counter;
            public TEncoderCounterCmd(bool _bQuery, Int32 _nAxis, Int32 _nPosition = 0) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                {
                    m_oCmdWriter.AddParameter(_nAxis);
                    m_oCmdWriter.AddParameter(_nPosition);
                }
            }
        };
        public class TEncoderCounterRsp : TBaseRsp
        {
            public TEncoderCounterRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Counter, _sCbInfo, _hRsp)
            { }
            public TEncoderCounterRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Counter, _oReader)
            { }
            public Int32 Axis
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 Position
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
        };
        #endregion


        #region Encoder counter source Cmd/Rsp class
        public class TEncoderCounterSourceCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Encoder_Counter_Source;
            public TEncoderCounterSourceCmd(bool _bQuery, Int32 _nAxis, Int32 _nSource = TCHRLibFunctionWrapper.Encoder_Trigger_Source_Immediate) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                {
                    m_oCmdWriter.AddParameter(_nAxis);
                    m_oCmdWriter.AddParameter(_nSource);
                }
            }
        };
        public class TEncoderCounterSourceRsp : TBaseRsp
        {
            public TEncoderCounterSourceRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Counter_Source, _sCbInfo, _hRsp)
            { }
            public TEncoderCounterSourceRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Counter_Source, _oReader)
            { }
            public Int32 Axis
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 Source
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
        };
        #endregion


        #region Encoder preload function Cmd/Rsp class
        public class TEncoderPreloadFunctionCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Encoder_Preload_Function;
            public TEncoderPreloadFunctionCmd(bool _bQuery, Int32 _nAxis, Int32 _nPreloadValue = 0, Int32 _nEncoderPreloadConfig = 0) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                {
                    m_oCmdWriter.AddParameter(_nAxis);
                    m_oCmdWriter.AddParameter(_nPreloadValue);
                    m_oCmdWriter.AddParameter(_nEncoderPreloadConfig);
                }
            }
        };
        public class TEncoderPreloadFunctionRsp : TBaseRsp
        {
            public TEncoderPreloadFunctionRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Preload_Function, _sCbInfo, _hRsp)
            { }
            public TEncoderPreloadFunctionRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Preload_Function, _oReader)
            { }
            public Int32 Axis
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 PreloadValue
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
            public Int32 EncoderPreloadConfig
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(2));
                }
            }
        };
        #endregion


        #region Encoder trigger enable Cmd/Rsp class
        public class TEncoderTriggerEnabledCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Encoder_Trigger_Enabled;
            public TEncoderTriggerEnabledCmd(bool _bQuery, bool _bEnabled = false) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                    m_oCmdWriter.AddParameter(_bEnabled ? 1 : 0);
            }
        };
        public class TEncoderTriggerEnabledRsp : TBaseRsp
        {
            public TEncoderTriggerEnabledRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Trigger_Enabled, _sCbInfo, _hRsp)
            { }
            public TEncoderTriggerEnabledRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Trigger_Enabled, _oReader)
            { }
            public bool Enabled
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0) != 0);
                }
            }
        };
        #endregion


        #region Encoder trigger property Cmd/Rsp class
        public class TEncoderTriggerPropertyCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Encoder_Trigger_Property;
            public TEncoderTriggerPropertyCmd(bool _bQuery, Int32 _nEncoderAxis = 0, Int32 _nStartPos = 0,
                Int32 _nStopPos = 1000, float _nInterval = 100, bool _nTriggerOnReturnMove = false) :
            base(CMD_ID, _bQuery)
            {
                if (!_bQuery)
                {
                    m_oCmdWriter.AddParameter(_nEncoderAxis);
                    m_oCmdWriter.AddParameter(_nStartPos);
                    m_oCmdWriter.AddParameter(_nStopPos);
                    m_oCmdWriter.AddParameter(_nInterval);
                    m_oCmdWriter.AddParameter(_nTriggerOnReturnMove ? 1 : 0);
                }
            }
        };
        public class TEncoderTriggerPropertyRsp : TBaseRsp
        {
            public TEncoderTriggerPropertyRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Trigger_Property, _sCbInfo, _hRsp)
            { }
            public TEncoderTriggerPropertyRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Encoder_Trigger_Property, _oReader)
            { }
            public Int32 EncoderAxis
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 StartPos
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
            public Int32 StopPos
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(2));
                }
            }
            public float Interval
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetFloatParameter(3));
                }
            }
            public bool TriggerOnReturnMove
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(4) != 0);
                }
            }
        };
        #endregion


        #region Device trigger mode Cmd/Rsp class
        public class TDeviceTriggerModeCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Device_Trigger_Mode;
            public TDeviceTriggerModeCmd(Int32 _TriggerMode) :
            base(CMD_ID, false)
            {
                m_oCmdWriter.AddParameter(_TriggerMode);
            }
        };
        public class TDeviceTriggerModeRsp : TBaseRsp
        {
            public TDeviceTriggerModeRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Device_Trigger_Mode, _sCbInfo, _hRsp)
            { }
            public TDeviceTriggerModeRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Device_Trigger_Mode, _oReader)
            { }
            public Int32 TriggerMode
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
        };
        #endregion


        #region Download spectrum Cmd/Rsp class
        public class TDownloadSpectrumCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Download_Spectrum;
            public TDownloadSpectrumCmd(Int32 _SpecType, Int32 _StartChannel = 0, Int32 _ChannelCount = 1) :
            base(CMD_ID, false)
            {
                m_oCmdWriter.AddParameter(_SpecType);
                m_oCmdWriter.AddParameter(_StartChannel);
                m_oCmdWriter.AddParameter(_ChannelCount);
            }
        };
        public class TDownloadSpectrumRsp : TBaseRsp
        {
            public TDownloadSpectrumRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Download_Spectrum, _sCbInfo, _hRsp)
            { }
            public TDownloadSpectrumRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Download_Spectrum, _oReader)
            { }
            public Int32 SpecType
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 StartChannel
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
            public Int32 ChannelCount
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(2));
                }
            }

            public Int32 ExposureNumber
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(3));
                }
            }

            public float MicrometerPerBin
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(4));
                }
            }


            public Int32 BlockExponent
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(5));
                }
            }

            public short[] SpecData
            {
                get
                {
                    CheckRspValid();
                    var aTemp = m_oRspReader.GetBlobParameter(6);
                    short[] aShortTemp = new short[aTemp.Length / 2];
                    Buffer.BlockCopy(aTemp, 0, aShortTemp, 0, aTemp.Length);
                    return (aShortTemp);
                }
            }
        };
        #endregion

        #region TDownloadSpectrumImageCmd
        public class TDownloadSpectrumImageCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Download_Spectrum;
            public TDownloadSpectrumImageCmd(Int32 _nXPixelPosition, Int32 _nYPixelPosition,
                Int32 _nWidth, Int32 _nHeight)
                : base(CMD_ID, false)
            {
                m_oCmdWriter.AddParameter(3);
                m_oCmdWriter.AddParameter(_nXPixelPosition);
                m_oCmdWriter.AddParameter(_nYPixelPosition);
                m_oCmdWriter.AddParameter(_nWidth);
                m_oCmdWriter.AddParameter(_nHeight);
            }
        };
        #endregion
        #region TDownloadSpectrumImageRsp
        public class TDownloadSpectrumImageRsp : TBaseRsp
        {
            public TDownloadSpectrumImageRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
                base(TCHRLibFunctionWrapper.CmdID_Download_Spectrum, _sCbInfo, _hRsp)
            { }
            public TDownloadSpectrumImageRsp(TResponse _oReader) :
                base(TCHRLibFunctionWrapper.CmdID_Download_Spectrum, _oReader)
            { }
            public Int32 SpecType
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 ExposureNumber
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
            public byte[] SpecImageData
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetBlobParameter(2));
                }
            }
        };
        #endregion


        #region Download table Cmd/Rsp class
        public class TDownloadTableCmd : TBaseCmd
        {
            public const UInt32 CMD_ID = TCHRLibFunctionWrapper.CmdID_Download_Upload_Table;
            public TDownloadTableCmd(Int32 _nTableType, Int32 _nTableIndex = 0, Int32 _nByteOffset = 0, Int32 _nLength = -1) :
            base(CMD_ID, true)
            {
                m_oCmdWriter.AddParameter(_nTableType);
                m_oCmdWriter.AddParameter(_nTableIndex);
                m_oCmdWriter.AddParameter(_nByteOffset);
                m_oCmdWriter.AddParameter(_nLength);
            }
        };
        public class TDownloadTableRsp : TBaseRsp
        {
            public TDownloadTableRsp(TCHRLibFunctionWrapper.TRspCallbackInfo _sCbInfo, UInt32 _hRsp) :
            base(TCHRLibFunctionWrapper.CmdID_Download_Upload_Table, _sCbInfo, _hRsp)
            { }
            public TDownloadTableRsp(TResponse _oReader) :
            base(TCHRLibFunctionWrapper.CmdID_Download_Upload_Table, _oReader)
            { }
            public Int32 TableType
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(0));
                }
            }
            public Int32 TableIndex
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(1));
                }
            }
            public Int32 ByteOffset
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(2));
                }
            }
            public Int32 TableLength
            {
                get
                {
                    CheckRspValid();
                    return (m_oRspReader.GetIntParameter(3));
                }
            }
            public byte[] TableData
            {
                get
                {
                    CheckRspValid();
                    return ((m_oRspReader.GetBlobParameter(4)));
                }
            }
        };
        #endregion

        //convenient functions for sending command and reading response

        //"_oLibCmd" can be created with special command class like "TScanRateCmd" or with "TCommand" class
        //returned "_oLibRsp" is already in the correct response class
        //User can cast it to the corresponding class like "TScanRateRsp"
        //Unknown response is of class type TResponse
        #region Command execution and response reading functions
        public static Int32 ExecCommand(UInt32 _hDest, IBaseCmd _oLibCmd, out IBaseRsp _oLibRsp)
        {
            TCHRLibFunctionWrapper.Rsp_h hRsp;
            Int32 nRes = TCHRLibFunctionWrapper.ExecCommand(_hDest, _oLibCmd.CmdHandle, out hRsp);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                _oLibRsp = null;
            else
            {
                TCHRLibFunctionWrapper.TRspCallbackInfo _sInfo;
                _sInfo.Ticket = 0;
                _sInfo.User = IntPtr.Zero;
                _sInfo.SourceConnection = _hDest;
                _sInfo.State = IntPtr.Zero;
                _oLibRsp = GetResponse(_sInfo, hRsp);
            }
            return (nRes);
        }

        //"_oLibCmd" can be created with special command class like "TScanRateCmd" or with "TCommand" class
        public static Int32 ExecCommandAsync(UInt32 _hDest, IBaseCmd _oLibCmd, TCHRLibFunctionWrapper.ResponseAndUpdateCallback _pCB, out Int32 _nTicket)
        {
            return (TCHRLibFunctionWrapper.ExecCommandAsync(_hDest, _oLibCmd.CmdHandle, IntPtr.Zero, _pCB, out _nTicket));
        }


        //command and response are both in string format like "SHZ 2000"
        public static Int32 ExecStringCommand(UInt32 _hDest, string _strCmd, out string _strRsp)
        {
            _strRsp = "";
            try
            {
                TCHRLibFunctionWrapper.Cmd_h hCmd;
                TCHRLibFunctionWrapper.NewCommandFromString(_strCmd, out hCmd);
                TCHRLibFunctionWrapper.Rsp_h hRsp;
                var nRes = TCHRLibFunctionWrapper.ExecCommand(_hDest, hCmd, out hRsp);
                if (TCHRLibFunctionWrapper.ResultSuccess(nRes))
                {
                    Int64 nLength = 0;
                    TCHRLibFunctionWrapper.ResponseToString(hRsp, null, ref nLength);
                    var strRsp = new StringBuilder((int)nLength);
                    nRes = TCHRLibFunctionWrapper.ResponseToString(hRsp, strRsp, ref nLength);
                    _strRsp = strRsp.ToString();
                }
                return (nRes);
            }
            catch(AccessViolationException ex)
            {
                return -1;
            }
        }

        //command is in string format like "SHZ 2000"
        //returned "_oLibRsp" is already in the correct response class
        //User can cast it to the corresponding class like "TScanRateRsp"
        //Unknown response is of class type TResponse
        public static Int32 ExecStringCommand(UInt32 _hDest, string _strCmd, out IBaseRsp _oLibRsp)
        {
            TCHRLibFunctionWrapper.Cmd_h hCmd;
            TCHRLibFunctionWrapper.NewCommandFromString(_strCmd, out hCmd);
            TCHRLibFunctionWrapper.Rsp_h hRsp;
            var nRes = TCHRLibFunctionWrapper.ExecCommand(_hDest, hCmd, out hRsp);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                _oLibRsp = null;
            else
            {
                TCHRLibFunctionWrapper.TRspCallbackInfo _sInfo;
                _sInfo.Ticket = 0;
                _sInfo.User = IntPtr.Zero;
                _sInfo.SourceConnection = _hDest;
                _sInfo.State = IntPtr.Zero;
                _oLibRsp = GetResponse(_sInfo, hRsp);
            }
            return (nRes);
        }

        //command is in string format like "SHZ 2000"
        public static Int32 ExecStringCommandAsync(UInt32 _hDest, string _strCmd,
            TCHRLibFunctionWrapper.ResponseAndUpdateCallback _pCB, out Int32 _nTicket)
        {
            TCHRLibFunctionWrapper.Cmd_h hCmd;
            TCHRLibFunctionWrapper.NewCommandFromString(_strCmd, out hCmd);
            var nRes = TCHRLibFunctionWrapper.ExecCommandAsync(_hDest, hCmd, IntPtr.Zero, _pCB, out _nTicket);
            return (nRes);
        }


        //create corresponding response object based on response ID
        private static TCHRLibCmdWrapper.IBaseRsp GetSpecificResponse(TCHRLibCmdWrapper.TResponse _oRsp)
        {
            try
            {
                switch (_oRsp.ResponseID)
                {
                    case TCHRLibFunctionWrapper.CmdID_Output_Signals:
                        return (new TCHRLibCmdWrapper.TOutputSignalsRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Firmware_Version:
                        return (new TCHRLibCmdWrapper.TFirmwareVersionRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Measuring_Method:
                        return (new TCHRLibCmdWrapper.TMeasuringMethodRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Full_Scale:
                        return (new TCHRLibCmdWrapper.TFullScaleRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Scan_Rate:
                        return (new TCHRLibCmdWrapper.TScanRateRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Data_Average:
                        return (new TCHRLibCmdWrapper.TDataAverageRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Spectrum_Average:
                        return (new TCHRLibCmdWrapper.TSpectrumAverageRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Refractive_Indices:
                        return (new TCHRLibCmdWrapper.TRefractiveIndicesRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Abbe_Numbers:
                        return (new TCHRLibCmdWrapper.TAbbeNumbersRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Refractive_Index_Tables:
                        return (new TCHRLibCmdWrapper.TRefractiveIndexTablesRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Lamp_Intensity:
                        return (new TCHRLibCmdWrapper.TLampIntensityRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Optical_Probe:
                        return (new TCHRLibCmdWrapper.TOpticalProbeRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Confocal_Detection_Threshold:
                        return (new TCHRLibCmdWrapper.TConfocalDetectionThresholdRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Interferometric_Quality_Threshold:
                        return (new TCHRLibCmdWrapper.TInterQualityThresholdRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Duty_Cycle:
                        return (new TCHRLibCmdWrapper.TDutyCycleRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Detection_Window_Active:
                        return (new TCHRLibCmdWrapper.TDetectionWindowActiveRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Detection_Window:
                        return (new TCHRLibCmdWrapper.TDetectionWindowRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Number_Of_Peaks:
                        return (new TCHRLibCmdWrapper.TNumberOfPeaksRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Peak_Ordering:
                        return (new TCHRLibCmdWrapper.TPeakOrderingRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Dark_Reference:
                        return (new TCHRLibCmdWrapper.TDarkReferenceRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Start_Data_Stream:
                        return (new TCHRLibCmdWrapper.TStartDataStreamRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Stop_Data_Stream:
                        return (new TCHRLibCmdWrapper.TStopDataStreamRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Light_Source_Auto_Adapt:
                        return (new TCHRLibCmdWrapper.TLightSourceAutoAdaptRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_CCD_Range:
                        return (new TCHRLibCmdWrapper.TCCDRangeRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Median:
                        return (new TCHRLibCmdWrapper.TMedianRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Analog_Output:
                        return (new TCHRLibCmdWrapper.TAnalogOutputRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Encoder_Counter:
                        return (new TCHRLibCmdWrapper.TEncoderCounterRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Encoder_Counter_Source:
                        return (new TCHRLibCmdWrapper.TEncoderCounterSourceRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Encoder_Preload_Function:
                        return (new TCHRLibCmdWrapper.TEncoderPreloadFunctionRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Encoder_Trigger_Enabled:
                        return (new TCHRLibCmdWrapper.TEncoderTriggerEnabledRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Encoder_Trigger_Property:
                        return (new TCHRLibCmdWrapper.TEncoderTriggerPropertyRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Device_Trigger_Mode:
                        return (new TCHRLibCmdWrapper.TDeviceTriggerModeRsp(_oRsp));
                    case TCHRLibFunctionWrapper.CmdID_Download_Spectrum:
                        Int32 nSpecType = _oRsp.GetIntParameter(0);
                        if ((nSpecType >= TCHRLibFunctionWrapper.Spectrum_Raw) &&
                            ((nSpecType <= TCHRLibFunctionWrapper.Spectrum_FT)))
                            return (new TCHRLibCmdWrapper.TDownloadSpectrumRsp(_oRsp));
                        else if (nSpecType == TCHRLibFunctionWrapper.Spectrum_2D_Image)
                            return (new TCHRLibCmdWrapper.TDownloadSpectrumImageRsp(_oRsp));
                        else
                            return (_oRsp);
                    case TCHRLibFunctionWrapper.CmdID_Download_Upload_Table:
                        return (new TCHRLibCmdWrapper.TDownloadTableRsp(_oRsp));
                    default:
                        return (_oRsp);
                }
            }
            catch
            {
                return _oRsp;
            }
        }


        public static IBaseRsp GetResponse(TCHRLibFunctionWrapper.TRspCallbackInfo _sInfo, UInt32 _hRsp)
        {
            try
            {
                var oRsp = new TResponse(_sInfo, _hRsp);
                return (GetSpecificResponse(oRsp));
            }
            catch
            {
                return null;
            }
        }
        #endregion
    };
}