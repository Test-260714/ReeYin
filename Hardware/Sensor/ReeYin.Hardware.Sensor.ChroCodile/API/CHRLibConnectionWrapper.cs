/*
TCHRLibConnectionWrapper contains information/wrapper functions for single connection.
TCHRLibConnData is the wrapper class for CHR data.
Two classes aim to simplify function calls to basic Lib functions
*/


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PrecitecClass
{
    #region Connection exception class
    class TCHRLibConnectionException : Exception
    {
        public TCHRLibConnectionException()
        {
        }
        public TCHRLibConnectionException(string _strMessage)
            : base(_strMessage)
        {
        }
    }
    #endregion


    #region Connection data class
    public class TCHRLibConnData
    {
        public Int64 SizePerSample
        { get; private set; }
        private List<Int32> m_aSignalOffset = new List<Int32>();
        private Int32 m_nGlobalSignalSize;
        private Int32 m_nPeakSignalSize;
        private Int32 m_nGlobalSignalNumber;
        private bool m_bInit = false;
        public Int64 SampleCount;
        public byte[] OrgSampleData = new byte[1024 * 32];
        public bool SignalChanged = false;
        public TCHRLibFunctionWrapper.TSampleSignalGeneralInfo SignalGenInfo = new TCHRLibFunctionWrapper.TSampleSignalGeneralInfo();
        public List<TCHRLibFunctionWrapper.TSampleSignalInfo> SignalInfos = new List<TCHRLibFunctionWrapper.TSampleSignalInfo>();

        //copy data from DLL internal unmanaged buffer
        private void CopySampleData(IntPtr _pData, Int64 _nSampleCount)
        {
            if (_nSampleCount <= 0)
                return;
            if (SizePerSample * _nSampleCount > OrgSampleData.Length)
                OrgSampleData = new byte[SizePerSample * _nSampleCount];
            Marshal.Copy(_pData, OrgSampleData, 0, (Int32)(SizePerSample * (Int32)_nSampleCount));
        }

        //read in data and signal related information from DLL
        public void ProcessNewData(IntPtr _pData, Int64 _nSampleCount, Int64 _nSizePerSample,
            TCHRLibFunctionWrapper.TSampleSignalGeneralInfo _sGenInfo, IntPtr _psSignalInfo)
        {
            SampleCount = _nSampleCount;
            if ((SampleCount == 0) || (_psSignalInfo == IntPtr.Zero))
                return;

            if ((_sGenInfo.InfoIndex != SignalGenInfo.InfoIndex) || (!m_bInit))
            {
                SignalGenInfo = _sGenInfo;
                SignalInfos.Clear();
                m_aSignalOffset.Clear();
                m_nGlobalSignalSize = 0;
                m_nPeakSignalSize = 0;
                m_nGlobalSignalNumber = 0;
                Int32 nOffset = 0;
                for (int i = 0; i < SignalGenInfo.PeakSignalCount + SignalGenInfo.GlobalSignalCount; i++)
                {
                    TCHRLibFunctionWrapper.TSampleSignalInfo sSigInfo = (TCHRLibFunctionWrapper.TSampleSignalInfo)
                        Marshal.PtrToStructure(_psSignalInfo, typeof(TCHRLibFunctionWrapper.TSampleSignalInfo));
                    SignalInfos.Add(sSigInfo);
                    m_aSignalOffset.Add(nOffset);
                    _psSignalInfo += Marshal.SizeOf(sSigInfo);
                    var nSize = GetSizefromDataType(sSigInfo.DataType);
                    nOffset += nSize;
                    if ((SignalGenInfo.ChannelCount == 1) || (IsGlobalSignal(sSigInfo.SignalID)))
                    {
                        m_nGlobalSignalSize += nSize;
                        m_nGlobalSignalNumber++;
                    }
                    else
                        m_nPeakSignalSize += nSize;
                }
                SignalChanged = true;
                m_bInit = true;
            }
            SizePerSample = _nSizePerSample;
            CopySampleData(_pData, _nSampleCount);
        }


        private void InitAutoBufferData(Int64 _nSampleCount, Int32 _nChannelCount,
           TCHRLibFunctionWrapper.TSampleSignalGeneralInfo _sGenInfo, TCHRLibFunctionWrapper.TSampleSignalInfo[] _aSignalInfos)
        {
            SampleCount = _nSampleCount;

            SignalGenInfo = _sGenInfo;
            SignalInfos.Clear();
            m_aSignalOffset.Clear();
            m_nGlobalSignalSize = 0;
            m_nPeakSignalSize = 0;
            m_nGlobalSignalNumber = 0;
            Int32 nOffset = 0;
            for (int i = 0; i < SignalGenInfo.PeakSignalCount + SignalGenInfo.GlobalSignalCount; i++)
            {
                SignalInfos.Add(_aSignalInfos[i]);
                m_aSignalOffset.Add(nOffset);
                var nSize = GetSizefromDataType(_aSignalInfos[i].DataType);
                nOffset += nSize;
                if ((SignalGenInfo.ChannelCount == 1) || (IsGlobalSignal(_aSignalInfos[i].SignalID)))
                {
                    m_nGlobalSignalSize += nSize;
                    m_nGlobalSignalNumber++;
                }
                else
                    m_nPeakSignalSize += nSize;
            }
            SignalChanged = true;
            m_bInit = true;

            SizePerSample = m_nGlobalSignalSize + m_nPeakSignalSize * _nChannelCount;
        }

        //Setup OrgSampleData to be used in auto buffer save
        public void InitAutoBufferDataWithInternalBuffer(Int64 _nSampleCount, Int32 _nChannelCount,
           TCHRLibFunctionWrapper.TSampleSignalGeneralInfo _sGenInfo, TCHRLibFunctionWrapper.TSampleSignalInfo[] _aSignalInfos)
        {
            if ((_nSampleCount == 0) || (_aSignalInfos.Length == 0))
                throw new TCHRLibConnectionException("Cannot init autobuffer sample count is 0 or signal info is emtpy!."); ;

            InitAutoBufferData(_nSampleCount, _nChannelCount, _sGenInfo, _aSignalInfos);
            if (SizePerSample * _nSampleCount > OrgSampleData.Length)
                OrgSampleData = new byte[SizePerSample * _nSampleCount];
        }

        //Reference OrgSampleData to the external buffer used in auto buffer save 
        public void InitAutoBufferDataWithExternalBuffer(byte[] _aData, Int64 _nSampleCount, Int32 _nChannelCount,
           TCHRLibFunctionWrapper.TSampleSignalGeneralInfo _sGenInfo, TCHRLibFunctionWrapper.TSampleSignalInfo[] _aSignalInfos)
        {
            if ((_nSampleCount == 0) || (_aSignalInfos.Length == 0))
                throw new TCHRLibConnectionException("Cannot init autobuffer sample count is 0 or signal info is emtpy!."); ;

            InitAutoBufferData(_nSampleCount, _nChannelCount, _sGenInfo, _aSignalInfos);
            OrgSampleData = _aData;
        }




        //based on signal data type read data from buffer, return double as common data type
        private double ReadDataFromRawData(Int32 _nOffset, Int16 _nDataType)
        {
            double nData = 0;
            switch (_nDataType)
            {
                case TCHRLibFunctionWrapper.Data_Type_Unsigned_Char:
                    nData = OrgSampleData[_nOffset];
                    break;
                case TCHRLibFunctionWrapper.Data_Type_Signed_Char:
                    nData = ((sbyte)(OrgSampleData[_nOffset]));
                    break;
                case TCHRLibFunctionWrapper.Data_Type_Unsigned_Short:
                    unsafe
                    {
                        fixed (byte* pSrc = OrgSampleData)
                        {
                            ushort* ps = (ushort*)(pSrc + _nOffset);
                            nData = *ps;
                        }
                    }
                    break;
                case TCHRLibFunctionWrapper.Data_Type_Signed_Short:
                    unsafe
                    {
                        fixed (byte* pSrc = OrgSampleData)
                        {
                            short* ps = (short*)(pSrc + _nOffset);
                            nData = *ps;
                        }
                    }
                    break;
                case TCHRLibFunctionWrapper.Data_Type_Unsigned_Int32:
                    unsafe
                    {
                        fixed (byte* pSrc = OrgSampleData)
                        {
                            UInt32* ps = (UInt32*)(pSrc + _nOffset);
                            nData = *ps;
                        }
                    }
                    break;
                case TCHRLibFunctionWrapper.Data_Type_Signed_Int32:
                    unsafe
                    {
                        fixed (byte* pSrc = OrgSampleData)
                        {
                            Int32* ps = (Int32*)(pSrc + _nOffset);
                            nData = *ps;
                        }
                    }
                    break;
                case TCHRLibFunctionWrapper.Data_Type_Float:
                    unsafe
                    {
                        fixed (byte* pSrc = OrgSampleData)
                        {
                            float* ps = (float*)(pSrc + _nOffset);
                            nData = *ps;
                        }
                    }
                    break;
                default:
                    unsafe
                    {
                        fixed (byte* pSrc = OrgSampleData)
                        {
                            double* ps = (double*)(pSrc + _nOffset);
                            nData = *ps;
                        }
                    }
                    break;
            }
            return nData;

        }




        //return one channel data of one signal in one sample
        public double GetData(Int64 _nSampleIndex, Int32 _nChannelIndex, Int32 _nSignalIndex)
        {
            if (SignalInfos.Count == 0)
                throw new TCHRLibConnectionException("Cannot process raw data, there is no signal informations!.");
            if ((_nSignalIndex >= SignalInfos.Count) || (_nSignalIndex < 0))
                throw new TCHRLibConnectionException("Unknown signal!");
            if ((_nSampleIndex < 0) || (_nSampleIndex >= SampleCount))
                throw new TCHRLibConnectionException("Invalid sample index!");
            if ((_nChannelIndex < 0) || (_nChannelIndex >= SignalGenInfo.ChannelCount))
                throw new TCHRLibConnectionException("Invalid channel index!");
            Int32 nOffset = (int)(_nSampleIndex * SizePerSample);
            if (_nSignalIndex < m_nGlobalSignalNumber)
                nOffset += m_aSignalOffset[_nSignalIndex];
            else
                nOffset += m_aSignalOffset[_nSignalIndex] + _nChannelIndex * m_nPeakSignalSize;
            return (ReadDataFromRawData(nOffset, SignalInfos[_nSignalIndex].DataType));
        }


        //return data for all the channels of one signal in one sample
        public double[] GetData(Int64 _nSampleIndex, Int32 _nSignalIndex)
        {
            try
            {
                Int32 nOffset = 0;
                if (SignalInfos.Count == 0)
                    throw new TCHRLibConnectionException("Cannot process raw data, there is no signal informations!.");
                if ((_nSignalIndex >= SignalInfos.Count) || (_nSignalIndex < 0))
                    throw new TCHRLibConnectionException("Unknown signal!");
                if ((_nSampleIndex < 0) || (_nSampleIndex >= SampleCount))
                    throw new TCHRLibConnectionException("Invalid sample index!");
                if (SignalGenInfo.ChannelCount <= 0)
                    throw new TCHRLibConnectionException("Invalid channel count !");
                nOffset = (int)(_nSampleIndex * SizePerSample);
                if (_nSignalIndex < m_nGlobalSignalNumber)
                {
                    nOffset += m_aSignalOffset[_nSignalIndex];
                    double[] Data = new double[1];
                    Data[0] = ReadDataFromRawData(nOffset, SignalInfos[_nSignalIndex].DataType);
                    return (Data);
                }
                else
                {
                    double[] Data = new double[SignalGenInfo.ChannelCount];
                    switch (SignalInfos[_nSignalIndex].DataType)
                    {
                        case TCHRLibFunctionWrapper.Data_Type_Unsigned_Char:
                            #region byte
                            unsafe
                            {
                                fixed (byte* pSrc = OrgSampleData)
                                {
                                    fixed (double* pDst = Data)
                                    {
                                        byte* ps = (byte*)(pSrc + nOffset + m_aSignalOffset[_nSignalIndex]);
                                        double* pd = pDst;
                                        ushort uOffset = 0;
                                        for (int idx = 0; idx < SignalGenInfo.ChannelCount; idx++)
                                        {
                                            ps += uOffset;
                                            *pd = *ps;
                                            pd++;
                                            uOffset = (ushort)(m_nPeakSignalSize);
                                        }
                                    }
                                }
                            }
                            break;
                        #endregion
                        case TCHRLibFunctionWrapper.Data_Type_Signed_Char:
                            #region sbyte
                            unsafe
                            {
                                fixed (byte* pSrc = OrgSampleData)
                                {
                                    fixed (double* pDst = Data)
                                    {
                                        sbyte* ps = (sbyte*)(pSrc + nOffset + m_aSignalOffset[_nSignalIndex]);
                                        double* pd = pDst;
                                        ushort uOffset = 0;
                                        for (int idx = 0; idx < SignalGenInfo.ChannelCount; idx++)
                                        {
                                            ps += uOffset;
                                            *pd = *ps;
                                            pd++;
                                            uOffset = (ushort)(m_nPeakSignalSize);
                                        }
                                    }
                                }
                            }
                            break;
                        #endregion
                        case TCHRLibFunctionWrapper.Data_Type_Unsigned_Short:
                            #region UShort
                            unsafe
                            {
                                fixed (byte* pSrc = OrgSampleData)
                                {
                                    fixed (double* pDst = Data)
                                    {
                                        ushort* ps = (ushort*)(pSrc + nOffset + m_aSignalOffset[_nSignalIndex]);
                                        double* pd = pDst;
                                        ushort uOffset = 0;
                                        for (int idx = 0; idx < SignalGenInfo.ChannelCount; idx++)
                                        {
                                            ps += uOffset;
                                            *pd = *ps;
                                            pd++;
                                            uOffset = (ushort)(m_nPeakSignalSize / 2);
                                        }
                                    }
                                }
                            }
                            break;
                        #endregion
                        case TCHRLibFunctionWrapper.Data_Type_Signed_Short:
                            #region Short
                            unsafe
                            {
                                fixed (byte* pSrc = OrgSampleData)
                                {
                                    fixed (double* pDst = Data)
                                    {
                                        short* ps = (short*)(pSrc + nOffset + m_aSignalOffset[_nSignalIndex]);
                                        double* pd = pDst;
                                        ushort uOffset = 0;
                                        for (int idx = 0; idx < SignalGenInfo.ChannelCount; idx++)
                                        {
                                            ps += uOffset;
                                            *pd = *ps;
                                            pd++;
                                            uOffset = (ushort)(m_nPeakSignalSize / 2);
                                        }
                                    }
                                }
                            }
                            break;
                        #endregion
                        case TCHRLibFunctionWrapper.Data_Type_Unsigned_Int32:
                            #region UInt32
                            unsafe
                            {
                                fixed (byte* pSrc = OrgSampleData)
                                {
                                    fixed (double* pDst = Data)
                                    {
                                        UInt32* ps = (UInt32*)(pSrc + nOffset + m_aSignalOffset[_nSignalIndex]);
                                        double* pd = pDst;
                                        ushort uOffset = 0;
                                        for (int idx = 0; idx < SignalGenInfo.ChannelCount; idx++)
                                        {
                                            ps += uOffset;
                                            *pd = *ps;
                                            pd++;
                                            uOffset = (ushort)(m_nPeakSignalSize / 4);
                                        }
                                    }
                                }
                            }
                            break;
                        #endregion
                        case TCHRLibFunctionWrapper.Data_Type_Signed_Int32:
                            #region Int32
                            unsafe
                            {
                                fixed (byte* pSrc = OrgSampleData)
                                {
                                    fixed (double* pDst = Data)
                                    {
                                        Int32* ps = (Int32*)(pSrc + nOffset + m_aSignalOffset[_nSignalIndex]);
                                        double* pd = pDst;
                                        ushort uOffset = 0;
                                        for (int idx = 0; idx < SignalGenInfo.ChannelCount; idx++)
                                        {
                                            ps += uOffset;
                                            *pd = *ps;
                                            pd++;
                                            uOffset = (ushort)(m_nPeakSignalSize / 4);
                                        }
                                    }
                                }
                            }
                            break;
                        #endregion
                        case TCHRLibFunctionWrapper.Data_Type_Float:
                            #region Float
                            unsafe
                            {
                                fixed (byte* pSrc = OrgSampleData)
                                {
                                    fixed (double* pDst = Data)
                                    {
                                        float* ps = (float*)(pSrc + nOffset + m_aSignalOffset[_nSignalIndex]);
                                        double* pd = pDst;
                                        ushort uOffset = 0;
                                        for (int idx = 0; idx < SignalGenInfo.ChannelCount; idx++)
                                        {
                                            ps += uOffset;
                                            *pd = *ps;
                                            pd++;
                                            uOffset = (ushort)(m_nPeakSignalSize / 4);
                                        }
                                    }
                                }
                            }
                            break;
                        #endregion
                        default:
                            #region Double
                            unsafe
                            {
                                fixed (byte* pSrc = OrgSampleData)
                                {
                                    fixed (double* pDst = Data)
                                    {
                                        double* ps = (double*)(pSrc + nOffset + m_aSignalOffset[_nSignalIndex]);
                                        double* pd = pDst;
                                        ushort uOffset = 0;
                                        for (int idx = 0; idx < SignalGenInfo.ChannelCount; idx++)
                                        {
                                            ps += uOffset;
                                            *pd = *ps;
                                            pd++;
                                            uOffset = (ushort)(m_nPeakSignalSize / 8);
                                        }
                                    }
                                }
                            }
                            break;
                            #endregion
                    }
                    return (Data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (null);
            }
        }

        //data size for different data type
        public static Int32 GetSizefromDataType(Int16 _nDataType)
        {
            switch (_nDataType)
            {
                case TCHRLibFunctionWrapper.Data_Type_Unsigned_Char:
                case TCHRLibFunctionWrapper.Data_Type_Signed_Char:
                    return 1;
                case TCHRLibFunctionWrapper.Data_Type_Unsigned_Short:
                case TCHRLibFunctionWrapper.Data_Type_Signed_Short:
                    return 2;
                case TCHRLibFunctionWrapper.Data_Type_Unsigned_Int32:
                case TCHRLibFunctionWrapper.Data_Type_Signed_Int32:
                case TCHRLibFunctionWrapper.Data_Type_Float:
                    return 4;
                default:
                    return 8;
            }
        }
        public static bool IsGlobalSignal(UInt16 _nSigID)
        {
            return ((_nSigID < 64) || (_nSigID & 0x100) == 0);
        }
        public void Reset()
        {
            m_bInit = false;
        }
    }
    #endregion


    public class TCHRLibConnectionWrapper : IDisposable
    {
        #region Delegate definition
        //command callback delegate
        public delegate void ConnResponseAndUpdateCallback(TCHRLibCmdWrapper.IBaseRsp _IRsp);
        //data callback deletegate
        public delegate void ConnSampleDataCallback(Int32 _nStatus, TCHRLibConnData _oData);
        #endregion

        private TCHRLibFunctionWrapper.Conn_h m_hConHandle = TCHRLibFunctionWrapper.Invalid_Handle;

        private TCHRLibFunctionWrapper.ResponseAndUpdateCallback m_oGenCmdCB;
        private TCHRLibFunctionWrapper.ResponseAndUpdateCallback m_oSingleCmdCB;

        private ConnResponseAndUpdateCallback m_oGenConnCmdCB;

        private ConcurrentDictionary<Int32, ConnResponseAndUpdateCallback> m_cqConnSingleResponseAndUpdateCallbacks =
            new ConcurrentDictionary<Int32, ConnResponseAndUpdateCallback>();
        private TCHRLibFunctionWrapper.SampleDataCallback m_oDataSamplesCB;

        private ConnSampleDataCallback m_oConnDataSamplesCB;
        private TCHRLibConnData m_oData;

        private GCHandle m_opinnedArray;


        public TCHRLibFunctionWrapper.Conn_h ConnectionHandle
        {
            get
            {
                return m_hConHandle;
            }
        }

        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            CloseConnection();
            if (m_opinnedArray.IsAllocated)
                m_opinnedArray.Free();
            disposed = true;
        }

        public TCHRLibConnectionWrapper()
        {
            m_oGenCmdCB = new TCHRLibFunctionWrapper.ResponseAndUpdateCallback(CmdGenCbFct);
            m_oSingleCmdCB = new TCHRLibFunctionWrapper.ResponseAndUpdateCallback(CmdSingleCbFct);
            m_oDataSamplesCB = new TCHRLibFunctionWrapper.SampleDataCallback(SampleDataCbFct);
            m_oData = new TCHRLibConnData();
        }

        ~TCHRLibConnectionWrapper()
        {
            Dispose(false);
        }

        #region Open/Close connection functions
        public void OpenConnection(string _strConnectionInfo, Int32 _nDeviceType, bool _bSyncConn, Int64 _nDevBufSize)
        {
            var nRes = TCHRLibFunctionWrapper.OpenConnection(_strConnectionInfo, _nDeviceType,
                _bSyncConn ? TCHRLibFunctionWrapper.Connection_Synchronous : TCHRLibFunctionWrapper.Connection_Asynchronous,
                _nDevBufSize, out m_hConHandle);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in openning connection: " + _strConnectionInfo + ".");
        }

        public void OpenSharedConnection(UInt32 _nExistingConnection, bool _bSyncConn)
        {
            if (TCHRLibFunctionWrapper.OpenSharedConnection(_nExistingConnection,
                _bSyncConn ? TCHRLibFunctionWrapper.Connection_Synchronous : TCHRLibFunctionWrapper.Connection_Asynchronous, out m_hConHandle) < 0)
                throw new TCHRLibConnectionException("Error in openning shared connection with existing connection : " + m_hConHandle.ToString() + ".");
        }

        public void CloseConnection()
        {
            if (ConnectionHandle != TCHRLibFunctionWrapper.Invalid_Handle)
            {
                TCHRLibFunctionWrapper.CloseConnection(ConnectionHandle);
                m_hConHandle = TCHRLibFunctionWrapper.Invalid_Handle;
            }
            m_oData.Reset();
        }
        #endregion


        #region Execute string command
        //command is in string format like "SHZ 2000"
        public void ExecStringCommandAsync(string _strCmd, ConnResponseAndUpdateCallback _pCB, out Int32 _nTicket)
        {
            var nRes = TCHRLibCmdWrapper.ExecStringCommandAsync(ConnectionHandle, _strCmd,
                m_oSingleCmdCB, out _nTicket);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in executing async command: " + _strCmd + ".");
            m_cqConnSingleResponseAndUpdateCallbacks[_nTicket] = _pCB;
        }
        //Both command and response are in string format like "SHZ 2000"
        public string ExecStringCommand(string _strCmd)
        {
            string strRsp;
            var nRes = TCHRLibCmdWrapper.ExecStringCommand(ConnectionHandle, _strCmd, out strRsp);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in executing command: " + _strCmd + ".");
            return strRsp;
        }
        #endregion


        #region Execute command based on command class 

        //"_oLibCmd" can be created with special command class like "TScanRateCmd" or with "TCommand" class
        //returned response is already in the correct response class
        //User can cast it to the corresponding class like "TScanRateRsp"
        //Unknown response is of class type TResponse
        public TCHRLibCmdWrapper.IBaseRsp ExecCommand(TCHRLibCmdWrapper.IBaseCmd _oLibCmd)
        {
            TCHRLibCmdWrapper.IBaseRsp iRsp;
            var nRes = TCHRLibCmdWrapper.ExecCommand(ConnectionHandle, _oLibCmd, out iRsp);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in executing command: " + _oLibCmd.CmdName + ".");
            return (iRsp);
        }

        //"_oLibCmd" can be created with special command class like "TScanRateCmd" or with "TCommand" class
        public void ExecCommandAsync(TCHRLibCmdWrapper.IBaseCmd _oLibCmd, ConnResponseAndUpdateCallback _pCB, out Int32 _nTicket)
        {
            var nRes = TCHRLibFunctionWrapper.ExecCommandAsync(ConnectionHandle, _oLibCmd.CmdHandle,
                IntPtr.Zero, m_oSingleCmdCB, out _nTicket);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in executing async command: " + _oLibCmd.CmdName + ".");
            m_cqConnSingleResponseAndUpdateCallbacks[_nTicket] = _pCB;
        }
        #endregion


        #region Command callback functions
        private void CmdGenCbFct(TCHRLibFunctionWrapper.TRspCallbackInfo _sInfo, TCHRLibFunctionWrapper.Rsp_h _hRsp)
        {
            if (m_oGenConnCmdCB == null)
                return;
            try
            {
                var oRsp = TCHRLibCmdWrapper.GetResponse(_sInfo, _hRsp);
                m_oGenConnCmdCB(oRsp);
            }
            catch
            {

            }
        }


        private void CmdSingleCbFct(TCHRLibFunctionWrapper.TRspCallbackInfo _sInfo, TCHRLibFunctionWrapper.Rsp_h _hRsp)
        {
            try
            {
                var oSingleCmdCb = m_cqConnSingleResponseAndUpdateCallbacks[_sInfo.Ticket];
                if (oSingleCmdCb != null)
                {

                    var oRsp = TCHRLibCmdWrapper.GetResponse(_sInfo, _hRsp);
                    oSingleCmdCb(oRsp);

                }
            }
            catch
            {

            }
        }

        public void RegisterGeneralResponseAndUpdateCallback(ConnResponseAndUpdateCallback _pGenCBFct)
        {
            m_oGenConnCmdCB = _pGenCBFct;
            var nRes = TCHRLibFunctionWrapper.RegisterGeneralResponseAndUpdateCallback(ConnectionHandle, IntPtr.Zero, m_oGenCmdCB);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in registering genernal command callback function.");
        }
        #endregion


        #region Asynchronous mode data processing
        public Int32 ProcessDeviceOutput()
        {
            return (TCHRLibFunctionWrapper.ProcessDeviceOutput(ConnectionHandle));
        }


        public void StartAutomaticDeviceOutputProcessing()
        {
            var nRes = TCHRLibFunctionWrapper.StartAutomaticDeviceOutputProcessing(ConnectionHandle);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in starting auto. device output processing.");
        }

        public void StopAutomaticDeviceOutputProcessing()
        {
            var nRes = TCHRLibFunctionWrapper.StopAutomaticDeviceOutputProcessing(ConnectionHandle);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in stopping auto. device output processing.");
        }
        #endregion


        #region GetNext(Last)Samples 
        public Int32 GetNextSamples(Int64 _nSampleCount, out TCHRLibConnData _oConnData)
        {
            Int64 nSampleSize;
            IntPtr pTemp = IntPtr.Zero;
            IntPtr pSigInfo = IntPtr.Zero;
            TCHRLibFunctionWrapper.TSampleSignalGeneralInfo sGenInfo;
            _oConnData = null;
            Int64 nCount = _nSampleCount;
            int nRes = TCHRLibFunctionWrapper.GetNextSamples(ConnectionHandle, ref nCount, out pTemp,
                out nSampleSize, out sGenInfo, out pSigInfo);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
            {
                //throw new TCHRLibConnectionException("Error in GetNextSample.");
                return -1;
            }
            m_oData.ProcessNewData(pTemp, nCount, nSampleSize, sGenInfo, pSigInfo);
            _oConnData = m_oData;
            return (nRes);
        }

        public Int32 GetLastSample(out TCHRLibConnData _oConnData)
        {
            Int64 nSampleSize;
            IntPtr pTemp, pSigInfo;
            TCHRLibFunctionWrapper.TSampleSignalGeneralInfo sGenInfo;
            _oConnData = null;
            var nRes = TCHRLibFunctionWrapper.GetLastSample(ConnectionHandle, out pTemp, out nSampleSize, out sGenInfo, out pSigInfo);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                //throw new TCHRLibConnectionException("Error in GetLastSample.");
                return -1;
            m_oData.ProcessNewData(pTemp, 1, nSampleSize, sGenInfo, pSigInfo);
            _oConnData = m_oData;
            return (nRes);
        }
        #endregion


        #region Sample data callback function
        private void SampleDataCbFct(IntPtr _pUser, Int32 _nStatus, Int64 _nSampleCount,
            IntPtr _pSampleBuffer, Int64 _nSizePerSample, TCHRLibFunctionWrapper.TSampleSignalGeneralInfo _sGenInfo, IntPtr _psSignalInfo)
        {
            if (m_oConnDataSamplesCB == null)
                return;
            TCHRLibConnData oTemp = null;
            m_oData.ProcessNewData(_pSampleBuffer, _nSampleCount, _nSizePerSample, _sGenInfo, _psSignalInfo);
            oTemp = m_oData;
            m_oConnDataSamplesCB(_nStatus, oTemp);
        }
        public void RegisterSampleDataCallback(Int64 _nReadSampleCount, Int32 _nReadSampleTimeOut, ConnSampleDataCallback _pOnReadSample)
        {
            m_oConnDataSamplesCB = _pOnReadSample;
            var nRes = TCHRLibFunctionWrapper.RegisterSampleDataCallback(ConnectionHandle, _nReadSampleCount,
                _nReadSampleTimeOut, IntPtr.Zero, m_oDataSamplesCB);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in registering sample data callback function.");
        }
        #endregion


        #region Output signal information 
        public TCHRLibFunctionWrapper.TSampleSignalInfo[] GetSignalInfo(out TCHRLibFunctionWrapper.TSampleSignalGeneralInfo _sGenInfo)
        {
            Int64 nBufSize = 0;
            TCHRLibFunctionWrapper.GetConnectionOutputSignalInfos(ConnectionHandle, out _sGenInfo, new TCHRLibFunctionWrapper.TSampleSignalInfo[0], ref nBufSize);
            var aSigInfo = new TCHRLibFunctionWrapper.TSampleSignalInfo[nBufSize / Marshal.SizeOf(typeof(TCHRLibFunctionWrapper.TSampleSignalInfo))];
            if (nBufSize > 0)
            {
                var nRes = TCHRLibFunctionWrapper.GetConnectionOutputSignalInfos(ConnectionHandle, out _sGenInfo,
                      aSigInfo, ref nBufSize);
                if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                    throw new TCHRLibConnectionException("Error in getting connection output signal infos!");
            }
            return (aSigInfo);
        }


        public Int32[] GetDeviceOutputSignalIDs()
        {
            Int32 nSignalNr;
            TCHRLibFunctionWrapper.GetDeviceOutputSignals(ConnectionHandle, new Int32[0], out nSignalNr);
            Int32[] aSignals = new int[nSignalNr];
            if (nSignalNr >= 0)
            {
                var nRes = TCHRLibFunctionWrapper.GetDeviceOutputSignals(ConnectionHandle, aSignals, out nSignalNr);
                if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                    throw new TCHRLibConnectionException("Error in getting device output signal IDs.");
            }
            return (aSignals);
        }
        #endregion


        #region Auto buffer functions
        public Int64 GetAutoBufferMinSize(Int32 _nSampleCOunt)
        {
            Int64 BufSize = 0;
            TCHRLibFunctionWrapper.ActivateAutoBufferMode(ConnectionHandle, IntPtr.Zero, _nSampleCOunt, ref BufSize);
            return (BufSize);
        }

        public Int64 GetAutoBufferSavedSampleCount()
        {
            Int64 nCount;
            var nRes = TCHRLibFunctionWrapper.GetAutoBufferSavedSampleCount(ConnectionHandle, out nCount);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in getting auto buffer saved sample count!");
            return (nCount);
        }

        public void ActivateAutoBufferMode(double[] _aBuffer, Int32 _nSampleCount, ref Int64 _nBufferSize)
        {
            if (m_opinnedArray.IsAllocated)
                m_opinnedArray.Free();
            m_opinnedArray = GCHandle.Alloc(_aBuffer, GCHandleType.Pinned);
            IntPtr unmanagedPointer = m_opinnedArray.AddrOfPinnedObject();
            var nRes = TCHRLibFunctionWrapper.ActivateAutoBufferMode(ConnectionHandle, unmanagedPointer, _nSampleCount, ref _nBufferSize);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
            {
                if (m_opinnedArray.IsAllocated)
                    m_opinnedArray.Free();
                throw new TCHRLibConnectionException("Error in activating auto buffer mode.");
            }

        }


        public void ActivateAutoBufferMode(byte[] _aBuffer, Int32 _nSampleCount, ref Int64 _nBufferSize)
        {
            if (m_opinnedArray.IsAllocated)
                m_opinnedArray.Free();
            m_opinnedArray = GCHandle.Alloc(_aBuffer, GCHandleType.Pinned);
            IntPtr unmanagedPointer = m_opinnedArray.AddrOfPinnedObject();
            var nRes = TCHRLibFunctionWrapper.ActivateAutoBufferMode(ConnectionHandle, unmanagedPointer, _nSampleCount, ref _nBufferSize);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
            {
                if (m_opinnedArray.IsAllocated)
                    m_opinnedArray.Free();
                throw new TCHRLibConnectionException("Error in activating auto buffer mode.");
            }

        }


        public void DeactivateAutoBufferMode()
        {
            var nRes = TCHRLibFunctionWrapper.DeactivateAutoBufferMode(ConnectionHandle);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in deactivating auto buffer mode.");
        }

        public Int32 GetAutoBufferStatus()
        {
            var nRes = TCHRLibFunctionWrapper.GetAutoBufferStatus(ConnectionHandle);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in getting auto buffer status!");
            return (nRes);
        }
        #endregion


        #region Flush connection buffer
        public void FlushConnectionBuffer()
        {
            var nRes = TCHRLibFunctionWrapper.FlushConnectionBuffer(ConnectionHandle);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in flushing connection buffer!");
        }
        #endregion


        #region Plugin
        public void AddPlugin(string _strPluginName, out TCHRLibFunctionWrapper.Plugin_h _hPlugin)
        {
            var nRes = TCHRLibFunctionWrapper.AddConnectionPlugIn(m_hConHandle, _strPluginName, out _hPlugin);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in adding plugin: " + _strPluginName + ".");
        }
        #endregion


        #region Error functions
        public TCHRLibFunctionWrapper.TErrorInfo[] LastConnectionErrors()
        {
            Int64 nSize = 0;
            TCHRLibFunctionWrapper.LastErrors(ConnectionHandle, new TCHRLibFunctionWrapper.TErrorInfo[0], ref nSize);
            var nErrorStructSize = Marshal.SizeOf(typeof(TCHRLibFunctionWrapper.TErrorInfo));
            TCHRLibFunctionWrapper.TErrorInfo[] aErrors =
                new TCHRLibFunctionWrapper.TErrorInfo[nSize / nErrorStructSize];
            if (nSize > 0)
            {
                var nRes = TCHRLibFunctionWrapper.LastErrors(ConnectionHandle, aErrors, ref nSize);
                if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                    throw new TCHRLibConnectionException("Error in getting connection errors!");
            }
            return (aErrors);
        }


        public void CleanConnectionErrors()
        {
            var nRes = TCHRLibFunctionWrapper.ClearErrors(ConnectionHandle);
            if (!TCHRLibFunctionWrapper.ResultSuccess(nRes))
                throw new TCHRLibConnectionException("Error in clean connection errors!");

        }
        #endregion

    }
}
