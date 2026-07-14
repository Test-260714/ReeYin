using HandyControl.Controls;
using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using SR7Link;
using SRAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ReeYin.Hardware.Sensor.SSZN
{
    public class SSZNSensor : SensorBase
    {
        #region Fields
        // 官方无效值：32位为-10000*10^5（10纳米单位），16位为-32768。
        private const int InvalidHeight32 = -1000000000;
        private const short InvalidHeight16 = short.MinValue;
        private const float InvalidHeightMm = -10000.0f;
        private const float HeightScale32 = 1.0f / 100000.0f;
        private const int DataWaitTimeoutMs = 3000;
        private const int InfiniteLoopReadCount = 500;

        public SR7ApiBase iSR7APi;

        private SSZNSensroConfig _config = new SSZNSensroConfig();
        public SSZNSensroConfig Config
        {
            get { return _config; }
            set { _config = value; }
        }
        private float heightScale16 = 1.0f;
        private readonly ManualResetEventSlim dataReadyEvent = new ManualResetEventSlim(false);

        //状态标记
        [JsonIgnore]
        private bool isScanning = false;  // 标记是否正在扫描
        [JsonIgnore]
        private bool clickStop = true;
        [JsonIgnore]
        private int m_callbackMode = 0;

        //图像缓存
        [JsonIgnore]
        public int[][] ImgBuff32;   // 32位高度数据
        [JsonIgnore]
        public short[][] ImgBuff16; // 16位高度数据
        [JsonIgnore]
        public byte[][] GrayBuff;    // 灰度数据
        [JsonIgnore]
        public int[][] Encoder;    // 编码器数据

        //统计信息
        [JsonIgnore]
        private int lastTotalPoints = 0; // 记录上次的总点数
        [JsonIgnore]
        public int coutCallbackPoints;//统计总回调行数
        [JsonIgnore]
        public uint profile16Bits = 0;//0:32位 1:16位
        #endregion

        #region Callback
        private ErrConnectCallBack ErrConnectDelegate;
        private SR7IFGetDataCallBack GetDataDelegate;

        // 用于等待回调信号的自动重置事件
        private AutoResetEvent callbackEvent = new AutoResetEvent(false);
        private CancellationTokenSource cancellationTokenSource; // 用于取消任务
        #endregion

        #region Properties

        #endregion

        #region Constructor
        public SSZNSensor()
        {
            
        }
        #endregion

        #region Override
        public override bool Init()
        {
            InitAPI(Config.CallbackMode);

            if (!Connect())
            {
                State = HardwareState.NotConnected;
                IsConnected = false;
                Logs.LogError("SSZN_传感器连接失败");
                return false;
            }
            else
            {

                State = HardwareState.Connected;
                IsConnected = true;
            }
            return true;
        }

        public override void Close()
        {
            if (iSR7APi == null)
            {
                return;
            }

            StopCamera();
            int nRet = iSR7APi.Close();
            State = HardwareState.Closed;
            IsConnected = false;
            //UpdateStatus(false);
            Logs.LogInfo($"Close Camera: {nRet}{(SR7IF_ERROR)nRet}");
        }

        public override void StartCollect()
        {
            lock(this)
            {
                if (iSR7APi == null)
                {
                    Logs.LogWarning("SSZN_StartCollect失败：SDK未初始化");
                    return;
                }
                dataReadyEvent.Reset();
                Update16BitScale();
                State = HardwareState.Running;
                StartScan();
            }
        }

        public override void StopCollect()
        {
            lock (this)
            {
                if (iSR7APi == null)
                {
                    return;
                }
                StopCamera();
                State = HardwareState.Complete;
            }
        }

        public override List<MeasureData> ReceiveSensorData()
        {
            lock (this)
            {
                // 等待至少一批数据到达，或被停止信号唤醒
                WaitForDataReady();
                List<MeasureData> ListMeasureData = new List<MeasureData>();

                var height32 = ImgBuff32 != null && ImgBuff32.Length > 0 ? ImgBuff32[0] : null;
                var height16 = ImgBuff16 != null && ImgBuff16.Length > 0 ? ImgBuff16[0] : null;
                var gray = GrayBuff != null && GrayBuff.Length > 0 ? GrayBuff[0] : null;

                if ((height32 == null && height16 == null) || gray == null)
                {
                    return ListMeasureData;
                }

                if (!TryGetFrameSize(height32?.Length ?? 0, height16?.Length ?? 0, gray.Length, out int width, out int height))
                {
                    return ListMeasureData;
                }

                for (int y = 0; y < height; y++)
                {
                    float[] heightRow = new float[width];
                    float[] grayRow = new float[width];
                    int rowOffset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int index = rowOffset + x;
                        grayRow[x] = gray[index];
                        if (profile16Bits == 1 && height16 != null)
                        {
                            short raw16 = height16[index];
                            if (raw16 == InvalidHeight16)
                            {
                                heightRow[x] = InvalidHeightMm;
                            }
                            else
                            {
                                // 16位高度为无符号数据，按官方规则减32768后乘比例
                                ushort rawU16 = unchecked((ushort)raw16);
                                heightRow[x] = (rawU16 - 32768.0f) * heightScale16;
                            }
                        }
                        else if (height32 != null)
                        {
                            int raw32 = height32[index];
                            heightRow[x] = raw32 == InvalidHeight32 ? InvalidHeightMm : raw32 * HeightScale32;
                        }
                        else
                        {
                            heightRow[x] = InvalidHeightMm;
                        }
                    }

                    MeasureData md = new MeasureData();
                    md.AreaData = [heightRow, grayRow];
                    ListMeasureData.Add(md);
                }

                return ListMeasureData;
            }
        }
        #endregion

        #region CallBack
        public void ErrConnectFunc(int dwDeviceId, int nErrCode)
        {
            Logs.LogInfo($"Into ConnectExceptionCallback  Error Code: {nErrCode}{(SR7IF_ERROR)nErrCode}");
            iSR7APi.CameraAOnline = false;
            iSR7APi.CameraBOnline = false;
            //UpdateStatus(false);
            //1:数据溢出，3:断开连接，4:版本错误
            //非异步模式：-1 表示一般错误（链路失败/设置失败/采集失败等），-1000
            switch (nErrCode)
            {
                case 1:
                    Logs.LogInfo($"Into ConnectExceptionCallback  Error Code: {nErrCode}{"CAMERA_EXCEPTION_1"}");
                    break;
                case 3:
                    Logs.LogInfo($"Into ConnectExceptionCallback  Error Code: {nErrCode}{"CAMERA_EXCEPTION_3"}");
                    break;
                case 4:
                    Logs.LogInfo($"Into ConnectExceptionCallback  Error Code: {nErrCode}{"CAMERA_EXCEPTION_3"}");
                    break;
                case -1:
                    Logs.LogInfo($"Into ConnectExceptionCallback  Error Code: {nErrCode}{"CAMERA_EXCEPTION_N1"}");
                    break;
                default:
                    break;
            }
        }

        public void GetDataCallBack(int nProfileWidth, int nHighlen, int nFlag, int nStatusCode, int ProfileBits)
        {
            profile16Bits = (uint)ProfileBits;
            if (nStatusCode == 0)
            {
                Logs.LogInfo($"Into GetDataFunc W:{nProfileWidth},H:{nHighlen},ErrCode:{nStatusCode}({(SR7IF_ERROR)nStatusCode})");
                //这里拿到相机数据做显示，可以根据业务实现自己的逻辑
                int nTempWidth = nProfileWidth; // 使用回调宽度
                //双相机时，一次回调和异步回调宽度为3200，无限循环回调宽度为6400
                if (iSR7APi.CameraBOnline && m_callbackMode == 2)//双相机
                    nTempWidth /= 2;

                int copyLength = nTempWidth * nHighlen;
                iSR7APi.BatchPoints = nHighlen;
                var width = (uint)nTempWidth;
                var height = (uint)iSR7APi.BatchPoints;
                var xInterval = iSR7APi.GetProfileData_XPitch();
                var yInterval = iSR7APi.GetProfileData_XPitch();

                if (coutCallbackPoints == 0)
                {
                    InitImageBufferMem(iSR7APi.CameraBOnline, nProfileWidth, iSR7APi.BatchPoints);
                    //ShowRGBImage(ImgBuff32[0], pcHead, pictureBoxA);
                }
                int offset = coutCallbackPoints * nTempWidth;
                if (Config.CallbackMode == 2)
                {
                    // 无限循环回调持续触发时，避免写入越界，必要时回到起点覆盖
                    int bufferLength = 0;
                    if (ProfileBits == 0 && ImgBuff32 != null && ImgBuff32.Length > 0 && ImgBuff32[0] != null)
                        bufferLength = ImgBuff32[0].Length;
                    else if (ImgBuff16 != null && ImgBuff16.Length > 0 && ImgBuff16[0] != null)
                        bufferLength = ImgBuff16[0].Length;
                    else if (GrayBuff != null && GrayBuff.Length > 0 && GrayBuff[0] != null)
                        bufferLength = GrayBuff[0].Length;

                    if (bufferLength > 0 && offset + copyLength > bufferLength)
                    {
                        coutCallbackPoints = 0;
                        offset = 0;
                    }
                }
                if (ProfileBits == 0)
                {
                    int[] tempHeightDataA = new int[copyLength];
                    byte[] tempGrayDataA = new byte[copyLength];
                    iSR7APi.GetData(tempHeightDataA, tempGrayDataA, Encoder[0], (int)width, (int)nHighlen, 0);
                    Array.Copy(tempHeightDataA, 0, ImgBuff32[0], offset, copyLength);
                    Array.Copy(tempGrayDataA, 0, GrayBuff[0], offset, copyLength);
                    //if (ImgBuff32 != null && ImgBuff32.Length != 0 && ImgBuff32[0] != null)
                    //{
                    //    ShowRGBImage(ImgBuff32[0], pcHead, pictureBoxA);
                    //}
                }
                else
                {
                    short[] temp16bitDataA = new short[copyLength];
                    byte[] tempGrayDataA = new byte[copyLength];
                    iSR7APi.GetData(temp16bitDataA, tempGrayDataA, Encoder[0], (int)width, (int)nHighlen, 0);
                    Array.Copy(temp16bitDataA, 0, ImgBuff16[0], offset, copyLength);
                    Array.Copy(tempGrayDataA, 0, GrayBuff[0], offset, copyLength);

                    //int[] showtemp16bitDataA = new int[pcHead.width * pcHead.height];
                    //ShowTiff16Data(ImgBuff16[0], showtemp16bitDataA);

                    //if (ImgBuff16 != null && ImgBuff16.Length != 0 && ImgBuff16[0] != null)
                    //{
                    //    ShowRGBImage(showtemp16bitDataA, pcHead, pictureBoxA);
                    //}
                }

                if (iSR7APi.CameraBOnline && ImgBuff32[1] != null)
                {
                    if (ProfileBits == 0)
                    {
                        int[] tempHeightDataB = new int[copyLength];
                        byte[] tempGrayDataB = new byte[copyLength];
                        iSR7APi.GetData(tempHeightDataB, tempGrayDataB, Encoder[1], (int)width, (int)nHighlen, 1);
                        Array.Copy(tempHeightDataB, 0, ImgBuff32[1], offset, copyLength);
                        Array.Copy(tempGrayDataB, 0, GrayBuff[1], offset, copyLength);
                        //ShowRGBImage(ImgBuff32[1], pcHead, pictureBoxB);
                    }
                    else
                    {
                        short[] tempHeightDataB = new short[copyLength];
                        byte[] tempGrayDataB = new byte[copyLength];
                        iSR7APi.GetData(tempHeightDataB, tempGrayDataB, Encoder[1], (int)width, (int)nHighlen, 1);
                        Array.Copy(tempHeightDataB, 0, ImgBuff16[1], offset, copyLength);
                        Array.Copy(tempGrayDataB, 0, GrayBuff[1], offset, copyLength);

                        //int[] showtemp16bitDataB = new int[pcHead.width * pcHead.height];

                        //ShowTiff16Data(ImgBuff16[1], showtemp16bitDataB);

                        //ShowRGBImage(showtemp16bitDataB, pcHead, pictureBoxB);
                    }

                }
                coutCallbackPoints += nHighlen;
                if (Config.CallbackMode != 2)
                {
                    if (Config.CallbackMode == 0)
                    {
                        coutCallbackPoints = 0;
                        callbackEvent.Set();
                        dataReadyEvent.Set();
                        // 一次回调完成，触发Complete状态
                        State = HardwareState.Complete;
                    }
                    else if (Config.CallbackMode == 1 && coutCallbackPoints >= iSR7APi.BatchPoints)
                    {
                        coutCallbackPoints = 0;
                        callbackEvent.Set();
                        dataReadyEvent.Set();
                        // 异步回调完成，触发Complete状态
                        State = HardwareState.Complete;
                    }
                }
                else
                {
                    // 无限循环回调：每批数据归零，下一次从起点覆盖
                    if (iSR7APi != null && iSR7APi.BatchPoints > 0 && coutCallbackPoints >= iSR7APi.BatchPoints)
                    {
                        coutCallbackPoints = 0;
                        // 无限循环回调每次完成一批数据，触发Complete状态
                        State = HardwareState.Complete;
                    }
                    dataReadyEvent.Set();
                }
            }
            else
            {
                Logs.LogInfo($"Data CallBack Exception:{nStatusCode}({(SR7IF_ERROR)nStatusCode})");
                callbackEvent.Set();
                dataReadyEvent.Set();
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 开始扫描
        /// </summary>
        private async void StartScan()
        {
            if (isScanning)
            {
                Logs.LogWarning($"MSG_SCANTHREAD_ISRUNNING");
                return;
            }
            
            // 固定使用软触发模式
            bool isIOTrigger = false;
            // 固定为单次采集模式（无限循环回调模式除外）
            bool isLoopScan = false;
            
            // 无限循环回调模式必须开启循环采集
            if (Config.CallbackMode == 2)
            {
                isLoopScan = true;
                Logs.LogInfo("无限循环回调模式自动开启循环采集");
            }
            
            profile16Bits = 0;
            if (Config.Sync16bit == 1)
                profile16Bits = 1;

            //获取批处理行数
            decimal numValue = Config.RowCollected;
            int height = (int)numValue;
            int ntProfileDataWidth = iSR7APi.GetProfileDataWidth();
            if (iSR7APi.CameraBOnline)
                ntProfileDataWidth /= 2;
            //回调行数
            decimal numValueCallbackPoints = Config.RowCallBack;
            int callbackPoints = (int)numValueCallbackPoints;

            if (Config.CallbackMode == 2)
            {
                StartInfiniteLoopScan(isIOTrigger, height, callbackPoints);
                return;
            }

            //参数本地保存
            //Properties.Settings.Default.batchpoints = height;
            //Properties.Settings.Default.callbackpoints = callbackPoints;
            //Properties.Settings.Default.Save();

            //设置接口参数
            iSR7APi.BatchPoints = height;
            //iSR7APi.CallBackPoints = height;
            //无限循环需要设置回调函数，一次回调和异步回调需要设置扫描总行数
            int callbackNum = callbackPoints;

            //无限循环回调不用设置扫描行数
            if (m_callbackMode != 2)
            {
                //一次回调 异步回调 只需要注册一次
                //无限循环停止后，需要再次注册回调
                //callbackNum = height;
                int setParamRet = iSR7APi.SetParams(0, 1, -1, SR7IF_SETTING_ITEM.BATCH_POINT, height);
                Logs.LogInfo($"set batch point param {setParamRet}");
                if (setParamRet != 0)
                {
                    return;
                }
            }

            //开始扫描
            InitImageBufferMem(iSR7APi.CameraBOnline, ntProfileDataWidth, height);
            coutCallbackPoints = 0;

            //注册数据回调
            //异步回调模式调用stop后，不用重新注册回调
            if (clickStop || m_callbackMode == 2)
            {
                GetDataDelegate = new SR7IFGetDataCallBack(GetDataCallBack);
                int nInitRet = iSR7APi.Init(callbackNum, profile16Bits, -1, GetDataDelegate);
                Logs.LogInfo($"Init Camera {nInitRet} {(SR7IF_ERROR)nInitRet}");
                if (nInitRet != 0)
                {
                    return;
                }
            }
            clickStop = false;
            //显示控件
            //progressBar1.Visible = true;
            //progressBar1.Style = ProgressBarStyle.Marquee;

            //线程内循环触发扫描
            cancellationTokenSource = new CancellationTokenSource();
            int scanTotalCount = 0;
            callbackEvent.Reset();
            await Task.Run(() =>
            {
                isScanning = true;
                try
                {
                    do
                    {
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            break;
                        }
                        int startRet = iSR7APi.Start(isIOTrigger, -1);
                        Logs.LogInfo($"Scan Image Thread,Start Ret:{startRet},Scan Count:{++scanTotalCount}");
                        if (startRet != 0)
                            break;
                        // 等待回调信号
                        callbackEvent.WaitOne();


                    } while (isLoopScan);
                }
                catch (OperationCanceledException)
                {
                    Logs.LogError("Task canceled");
                }
                finally
                {
                    //this.Invoke(new Action(() =>
                    //{
                    //    progressBar1.Visible = false;
                    //}));
                    isScanning = false;
                }
                Logs.LogInfo("Out Scan Image Thread");
            });
        }

        private void StartInfiniteLoopScan(bool isIOTrigger, int totalLines, int refreshLines)
        {
            if (iSR7APi is not SR7ApiInfiniteLoopCallbackImpl infiniteApi)
            {
                Logs.LogError("SSZN_无限循环模式初始化失败：API类型不匹配");
                return;
            }

            if (refreshLines <= 0)
            {
                Logs.LogWarning("SSZN_无限循环模式启动失败：回调行数必须大于0");
                return;
            }

            if (totalLines > 0 && totalLines < 15000)
            {
                Logs.LogWarning("SSZN_无限循环模式启动失败：采集行数需为0或大于等于15000");
                return;
            }

            if (totalLines > 0 && refreshLines > totalLines)
            {
                Logs.LogWarning("SSZN_无限循环模式启动失败：回调行数不能大于采集行数");
                return;
            }

            if (Config.Sync16bit == 1)
            {
                Logs.LogWarning("SSZN_无限循环模式按官方demo固定使用32位高度数据，已忽略16位设置");
            }
            profile16Bits = 0;

            int deviceProfileWidth = iSR7APi.GetProfileDataWidth();
            if (deviceProfileWidth <= 0)
            {
                Logs.LogError("SSZN_无限循环模式启动失败：无法获取轮廓宽度");
                return;
            }

            int singleProfileWidth = deviceProfileWidth;
            if (iSR7APi.CameraBOnline)
            {
                singleProfileWidth /= 2;
            }

            int bufferHeight = totalLines > 0 ? totalLines : refreshLines;
            iSR7APi.BatchPoints = totalLines;
            InitImageBufferMem(iSR7APi.CameraBOnline, singleProfileWidth, bufferHeight);
            coutCallbackPoints = 0;
            clickStop = false;

            cancellationTokenSource = new CancellationTokenSource();
            dataReadyEvent.Reset();

            Task.Run(() =>
            {
                isScanning = true;
                try
                {
                    int startRet = infiniteApi.Start(isIOTrigger, -1);
                    Logs.LogInfo($"SSZN_无限循环模式开始采集 ret:{startRet}");
                    if (startRet != 0)
                    {
                        return;
                    }

                    RunInfiniteLoopPolling(infiniteApi, deviceProfileWidth, singleProfileWidth, totalLines, refreshLines, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Logs.LogInfo("SSZN_无限循环模式采集被取消");
                }
                finally
                {
                    infiniteApi.Stop();
                    isScanning = false;
                    Logs.LogInfo("SSZN_无限循环模式采集线程退出");
                }
            });
        }

        private void RunInfiniteLoopPolling(SR7ApiInfiniteLoopCallbackImpl infiniteApi, int deviceProfileWidth, int singleProfileWidth, int totalLines, int refreshLines, CancellationToken cancellationToken)
        {
            int tempLineCapacity = InfiniteLoopReadCount * 2;
            int[] tempHeightData = new int[tempLineCapacity * deviceProfileWidth];
            byte[] tempGrayData = new byte[tempLineCapacity * deviceProfileWidth];
            uint[] tempEncoder = new uint[tempLineCapacity];
            long[] frameId = new long[tempLineCapacity];
            uint[] frameLoss = new uint[tempLineCapacity];

            int[] refreshHeightData = new int[refreshLines * deviceProfileWidth];
            byte[] refreshGrayData = new byte[refreshLines * deviceProfileWidth];
            uint[] refreshEncoder = new uint[refreshLines];

            int pendingLines = 0;
            int totalWrittenLines = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                int currentBatchLines = infiniteApi.GetBatchRollData(tempHeightData, tempGrayData, tempEncoder, frameId, frameLoss, InfiniteLoopReadCount);
                if (currentBatchLines == 0)
                {
                    continue;
                }

                if (currentBatchLines < 0)
                {
                    HandleInfiniteLoopReadError(infiniteApi, currentBatchLines);
                    break;
                }

                int readStartLine = 0;
                while (readStartLine < currentBatchLines && !cancellationToken.IsCancellationRequested)
                {
                    int remainReadLines = currentBatchLines - readStartLine;
                    int remainRefreshLines = refreshLines - pendingLines;
                    int appendLines = Math.Min(remainReadLines, remainRefreshLines);

                    if (totalLines > 0)
                    {
                        int remainTotalLines = totalLines - totalWrittenLines - pendingLines;
                        if (remainTotalLines <= 0)
                        {
                            State = HardwareState.Complete;
                            return;
                        }

                        appendLines = Math.Min(appendLines, remainTotalLines);
                    }

                    CopyLoopLinesToRefreshBuffer(
                        tempHeightData,
                        tempGrayData,
                        tempEncoder,
                        readStartLine,
                        appendLines,
                        refreshHeightData,
                        refreshGrayData,
                        refreshEncoder,
                        pendingLines,
                        deviceProfileWidth);

                    pendingLines += appendLines;
                    readStartLine += appendLines;

                    bool needFlush = pendingLines >= refreshLines || (totalLines > 0 && totalWrittenLines + pendingLines >= totalLines);
                    if (!needFlush)
                    {
                        continue;
                    }

                    int flushLines = pendingLines;
                    if (totalLines > 0)
                    {
                        flushLines = Math.Min(flushLines, totalLines - totalWrittenLines);
                    }

                    int destinationStartLine = totalLines > 0 ? totalWrittenLines : 0;
                    CopyRefreshBufferToImageBuffers(refreshHeightData, refreshGrayData, refreshEncoder, flushLines, deviceProfileWidth, singleProfileWidth, destinationStartLine);

                    totalWrittenLines += flushLines;
                    coutCallbackPoints = totalWrittenLines;
                    Console.WriteLine($"SSZN 无限循环回调数据量: 本次{flushLines}行, 累计{totalWrittenLines}行");
                    dataReadyEvent.Set();
                    pendingLines = 0;

                    if (totalLines > 0)
                    {
                        if (totalWrittenLines >= totalLines)
                        {
                            State = HardwareState.Complete;
                            return;
                        }
                    }
                }
            }
        }

        private void CopyLoopLinesToRefreshBuffer(int[] sourceHeight, byte[] sourceGray, uint[] sourceEncoder, int sourceStartLine, int lineCount, int[] targetHeight, byte[] targetGray, uint[] targetEncoder, int targetStartLine, int deviceProfileWidth)
        {
            int sourcePointOffset = sourceStartLine * deviceProfileWidth;
            int targetPointOffset = targetStartLine * deviceProfileWidth;
            int copyPointCount = lineCount * deviceProfileWidth;

            Array.Copy(sourceHeight, sourcePointOffset, targetHeight, targetPointOffset, copyPointCount);
            Array.Copy(sourceGray, sourcePointOffset, targetGray, targetPointOffset, copyPointCount);
            Array.Copy(sourceEncoder, sourceStartLine, targetEncoder, targetStartLine, lineCount);
        }

        private void CopyRefreshBufferToImageBuffers(int[] sourceHeight, byte[] sourceGray, uint[] sourceEncoder, int lineCount, int deviceProfileWidth, int singleProfileWidth, int destinationStartLine)
        {
            int destinationPointOffset = destinationStartLine * singleProfileWidth;

            if (!iSR7APi.CameraBOnline)
            {
                Array.Copy(sourceHeight, 0, ImgBuff32[0], destinationPointOffset, lineCount * singleProfileWidth);
                Array.Copy(sourceGray, 0, GrayBuff[0], destinationPointOffset, lineCount * singleProfileWidth);
                for (int i = 0; i < lineCount; i++)
                {
                    Encoder[0][destinationStartLine + i] = unchecked((int)sourceEncoder[i]);
                }
                return;
            }

            for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {
                int sourceOffset = lineIndex * deviceProfileWidth;
                int destinationOffset = destinationPointOffset + lineIndex * singleProfileWidth;
                Array.Copy(sourceHeight, sourceOffset, ImgBuff32[0], destinationOffset, singleProfileWidth);
                Array.Copy(sourceGray, sourceOffset, GrayBuff[0], destinationOffset, singleProfileWidth);
                Array.Copy(sourceHeight, sourceOffset + singleProfileWidth, ImgBuff32[1], destinationOffset, singleProfileWidth);
                Array.Copy(sourceGray, sourceOffset + singleProfileWidth, GrayBuff[1], destinationOffset, singleProfileWidth);

                int encoderValue = unchecked((int)sourceEncoder[lineIndex]);
                Encoder[0][destinationStartLine + lineIndex] = encoderValue;
                Encoder[1][destinationStartLine + lineIndex] = encoderValue;
            }
        }

        private void HandleInfiniteLoopReadError(SR7ApiInfiniteLoopCallbackImpl infiniteApi, int errorCode)
        {
            if (errorCode == (int)SR7IF_ERROR.SR7IF_ERROR_MODE)
            {
                Logs.LogError($"SSZN_无限循环模式读取失败：当前设备不是循环模式 ret:{errorCode}");
                return;
            }

            if (errorCode == (int)SR7IF_ERROR.SR7IF_NORMAL_STOP)
            {
                Logs.LogInfo($"SSZN_无限循环模式正常停止 ret:{errorCode}");
                return;
            }

            if (errorCode == (int)SR7IF_ERROR.SR7IF_ERROR_ROLL_DATA_OVERFLOW)
            {
                if (infiniteApi.GetBatchRollError(out int ethErrCnt, out int userErrCnt) == 0)
                {
                    Logs.LogError($"SSZN_无限循环模式数据溢出：网络原因={ethErrCnt}，用户原因={userErrCnt}");
                    return;
                }
            }

            Logs.LogError($"SSZN_无限循环模式读取失败 ret:{errorCode}({(SR7IF_ERROR)errorCode})");
        }

        private void InitAPI(int nMode)
        {
            switch (nMode)
            {
                //一次回调：SDK一次性将数据全部给到回调函数，最大扫描行数15000
                case 0:
                    iSR7APi = new SR7ApiOneTimeCallbackImpl(); break;
                //异步回调：图像采集和传输分开进行
                case 1:
                    iSR7APi = new SR7ApiSyncCallbackImpl(); break;
                //无循环回调：根据设置回调行数，将数据给到回调函数
                case 2:
                    iSR7APi = new SR7ApiInfiniteLoopCallbackImpl(); break;
                default:
                    throw new ArgumentException("Invalid mode", nameof(nMode));
            }
            m_callbackMode = nMode;
        }

        private void StopCamera()
        {
            clickStop = true;
            cancellationTokenSource?.Cancel();
            iSR7APi.Stop();
            callbackEvent.Set();
            callbackEvent.Reset();
            dataReadyEvent.Set();
            Logs.LogInfo("buttonStopScan_Click end");
        }

        /// <summary>
        /// 连接相机
        /// </summary>
        /// <returns></returns>
        private bool Connect()
        {
            if (isScanning)
            {
                Logs.LogWarning($"MSG_SCANTHREAD_ISRUNNING");
                return false;
            }

            string cameraIp = ResolveCameraIp();
            if (string.IsNullOrWhiteSpace(cameraIp) || cameraIp == "127.0.0.1")
            {
                MessageBox.Show("相机IP错误");
                return false;
            }

            ErrConnectDelegate = new ErrConnectCallBack(ErrConnectFunc);
            int nOpenRet = iSR7APi.Open(0, cameraIp, 2000, ErrConnectDelegate);
            if(nOpenRet != 0)
            {
                Logs.LogInfo($"Connect Camera {nOpenRet} {(SR7IF_ERROR)nOpenRet}");
                return false;
            }
            else
            {
                //UpdateStatus(nOpenRet == 0);
            }
            IsConnected = true;

            //设置相机参数，否则无法取图
            //一次回调和无限回调可设置批处理测量为开启，批处理数据接收为无或循环
            //异步回调必须在Ed软件中设置
            int nReadBastchValue = -1;
            int getParamRet = iSR7APi.GetParams(0, -1, SR7IF_SETTING_ITEM.BATCH_ON_OFF, out nReadBastchValue);
            if (nReadBastchValue != 1)
            {
                int nSetParamRet = iSR7APi.SetParams(0, (int)SAVEPOWEROFF.ESAPO_SAVE, -1, SR7IF_SETTING_ITEM.BATCH_ON_OFF, 1);
                Logs.LogInfo($"Set Batch On Off: {nSetParamRet}");
            }

            // 根据回调模式设置相机硬件参数
            if (Config.CallbackMode == 0)  // 一次回调模式
            {
                // 关闭循环模式和分段存储
                int nSetParamRet = iSR7APi.SetParams(0, (int)SAVEPOWEROFF.ESAPO_SAVE, -1, SR7IF_SETTING_ITEM.CYCLICAL_PATTERN, 0);
                int nSetParamRet1 = iSR7APi.SetParams(0, (int)SAVEPOWEROFF.ESAPO_SAVE, -1, SR7IF_SETTING_ITEM.SEGMENT_BUFER, 0);
                Logs.LogInfo($"一次回调模式：关闭循环模式({nSetParamRet})，关闭分段存储({nSetParamRet1})");
            }
            else if (Config.CallbackMode == 1)  // 异步回调模式
            {
                // 开启分段存储
                int nSetParamRet1 = iSR7APi.SetParams(0, (int)SAVEPOWEROFF.ESAPO_SAVE, -1, SR7IF_SETTING_ITEM.SEGMENT_BUFER, 1);
                Logs.LogInfo($"异步回调模式：开启分段存储({nSetParamRet1})");
            }
            else if (Config.CallbackMode == 2)  // 无限循环回调模式
            {
                // 必须开启硬件循环模式
                int nSetParamRet = iSR7APi.SetParams(0, (int)SAVEPOWEROFF.ESAPO_SAVE, -1, SR7IF_SETTING_ITEM.CYCLICAL_PATTERN, 1);
                Logs.LogInfo($"无限循环回调模式：开启硬件循环模式({nSetParamRet})");
            }
            //GetDataDelegate = new SR7IFGetDataCallBack(GetDataCallBack);
            //int nInitRet = iSR7APi.Init(1000, 0, -1, GetDataDelegate);
            //Log($"Init Camera {nInitRet} {GetSDKErrMsgByCode(nInitRet)}");

            Update16BitScale();
            return true;
        }

        /// <summary>
        /// 搜索相机
        /// </summary>
        /// <returns></returns>
        public List<string> SearchCam()
        {
            if (isScanning)
            {
                Logs.LogInfo("MSG_SCANTHREAD_ISRUNNING");
                return null;
            }
            try
            {
                List<string> cameraIPs = new List<string>();
                if (!iSR7APi.SearchCameraIP(out cameraIPs))
                {
                    MessageBox.Show("MSG_SEARCH_EMPTY");
                    return null;
                }

                Logs.LogInfo($"Search Camera Size: {cameraIPs.Count}");
                return cameraIPs;
            }
            catch (SRAPIException ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public void InitImageBufferMem(bool isExistCameraB, int nWidth, int nHeight)
        {
            int nTotalPoints = nWidth * nHeight;
            if (nTotalPoints <= 0 || nTotalPoints <= lastTotalPoints)
            {
                for (int i = 0; i < 2; i++)
                {

                    for (int j = 0; j < nTotalPoints; j++)
                    {
                        ImgBuff32[i][j] = -1000000000;  // 初始化为 -1000000000
                    }
                for (int j = 0; j < nTotalPoints; j++)
                {
                    ImgBuff16[i][j] = InvalidHeight16;
                    GrayBuff[i][j] = 0;
                }
                    for (int j = 0; j < nHeight; j++)
                    {
                        Encoder[i][j] = 0;
                    }

                }
                return; // 避免重复初始化
            }

            // 如果 nTotalPoints 发生变化，才重新分配内存
            FreeMemory();

            ImgBuff32 = new int[2][];
            ImgBuff16 = new short[2][];
            GrayBuff = new byte[2][];
            Encoder = new int[2][];

            for (int i = 0; i < 2; i++)
            {
                ImgBuff32[i] = new int[nTotalPoints];    // 32位高度数据
                for (int j = 0; j < nTotalPoints; j++)
                {
                    ImgBuff32[i][j] = -1000000000;  // 初始化为 -1000000000
                }
                ImgBuff16[i] = new short[nTotalPoints];  // 16位高度数据
                GrayBuff[i] = new byte[nTotalPoints];     // 灰度数据
                Encoder[i] = new int[nHeight];       // 编码器数据
                for (int j = 0; j < nTotalPoints; j++)
                {
                    ImgBuff16[i][j] = InvalidHeight16;
                }

            }

            lastTotalPoints = nTotalPoints;
        }

        public void FreeMemory()
        {
            if (ImgBuff32 != null)
            {
                ImgBuff32 = null;
                ImgBuff16 = null;
                GrayBuff = null;
                Encoder = null;
                lastTotalPoints = 0;
                GC.Collect();
            }
        }

        private bool TryGetFrameSize(int height32Len, int height16Len, int grayLen, out int width, out int height)
        {
            width = 0;
            height = 0;
            int bufferLen = int.MaxValue;
            if (height32Len > 0)
                bufferLen = Math.Min(bufferLen, height32Len);
            if (height16Len > 0)
                bufferLen = Math.Min(bufferLen, height16Len);
            if (grayLen > 0)
                bufferLen = Math.Min(bufferLen, grayLen);
            if (bufferLen == int.MaxValue)
                return false;

            int profileWidth = 0;
            if (iSR7APi != null)
            {
                profileWidth = iSR7APi.GetProfileDataWidth();
                if (iSR7APi.CameraBOnline && profileWidth > 0)
                    profileWidth /= 2;
            }

            int candidateHeight = Config.RowCollected > 0 ? Config.RowCollected : (iSR7APi?.BatchPoints ?? 0);
            if (profileWidth > 0)
            {
                width = profileWidth;
                if (candidateHeight <= 0)
                    candidateHeight = bufferLen / width;
                height = Math.Min(candidateHeight, bufferLen / width);
            }
            else if (candidateHeight > 0)
            {
                width = bufferLen / candidateHeight;
                height = candidateHeight;
            }

            return width > 0 && height > 0;
        }

        private void Update16BitScale()
        {
            if (iSR7APi == null)
                return;

            // 获取16位高度的毫米比例
            if (iSR7APi.Get16BitScale(out float scale) == 0 && scale > 0)
            {
                heightScale16 = scale;
            }
            else
            {
                heightScale16 = 1.0f;
            }
        }

        private void WaitForDataReady()
        {
            if (dataReadyEvent.IsSet)
                return;

            // 避免回调不来时无限等待
            if (!dataReadyEvent.Wait(DataWaitTimeoutMs))
            {
                Logs.LogWarning("SSZN_等待采集数据超时");
            }
        }

        public bool TryGetParam(SR7IF_SETTING_ITEM item, out int value)
        {
            value = 0;
            if (iSR7APi == null)
            {
                return false;
            }

            if (!IsConnected && !(iSR7APi.CameraAOnline || iSR7APi.CameraBOnline))
            {
                return false;
            }

            int configNum = GetConfigNum();
            int ret = iSR7APi.GetParams(0, configNum, item, out value);
            if (ret != 0)
            {
                Logs.LogWarning($"SSZN_读取参数失败:{item} ret:{ret}");
                return false;
            }
            return true;
        }

        public bool TrySetParam(SR7IF_SETTING_ITEM item, int value)
        {
            if (iSR7APi == null)
            {
                return false;
            }

            if (!IsConnected && !(iSR7APi.CameraAOnline || iSR7APi.CameraBOnline))
            {
                return false;
            }

            int configNum = GetConfigNum();
            int ret = iSR7APi.SetParams(0, (int)Config.SavePowerOff, configNum, item, value);
            if (ret != 0)
            {
                Logs.LogWarning($"SSZN_写入参数失败:{item} ret:{ret}");
                return false;
            }
            return true;
        }

        public void RefreshParamsFromSensor()
        {
            if (iSR7APi == null)
            {
                return;
            }

            // 已移除触发模式UI控件，不再读取此参数
            //if (TryGetParam(SR7IF_SETTING_ITEM.TRIG_MODE, out int trigMode))
            //{
            //    Config.TrigModeParam = (SR7IF_TRIG_MODE)trigMode;
            //}
            if (TryGetParam(SR7IF_SETTING_ITEM.SAMPLED_CYCLE, out int sampledCycle))
            {
                Config.SampledCycle = sampledCycle;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.BATCH_ON_OFF, out int batchOnOff))
            {
                Config.BatchOnOff = (SR7IF_BATCH_ON_OFF)batchOnOff;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.ENCODER_TYPE, out int encoderType))
            {
                Config.EncoderType = (SR7IF_ENCODER_TYPE)encoderType;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.ENCODER_INPUTMODE, out int encoderInputMode))
            {
                Config.EncoderInputMode = encoderInputMode;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.REFINING_POINTS, out int refiningPoints))
            {
                Config.RefiningPoints = refiningPoints;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.BATCH_POINT, out int batchPoint))
            {
                Config.RowCollected = batchPoint;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.CYCLICAL_PATTERN, out int cyclicalPattern))
            {
                Config.CyclicalPattern = (SR7IF_CYCLICAL_PATTERN)cyclicalPattern;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.SEGMENT_BUFER, out int segmentBuffer))
            {
                Config.SegmentBuffer = segmentBuffer != 0;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.BATCH_OUTPUT, out int batchOutput))
            {
                Config.BatchOutput = (SR7IF_BATCH_OUTPUT)batchOutput;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.Z_MEASURING_RANGE, out int zMeasuringRange))
            {
                Config.ZMeasuringRange = (SR7IF_Z_MEASURING_RANGE)zMeasuringRange;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.SENSITIVITY, out int sensitivity))
            {
                Config.Sensitivity = (SR7IF_SENSITIVITY)sensitivity;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.EXP_TIME, out int expTime))
            {
                Config.ExpTime = (SR7IF_EXP_TIME)expTime;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.LIGHT_CONTROL, out int lightControl))
            {
                Config.LightControl = (SR7IF_LIGHT_CONTROL)lightControl;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.LIGHT_MAX, out int lightMax))
            {
                Config.LightMax = lightMax;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.LIGHT_MIN, out int lightMin))
            {
                Config.LightMin = lightMin;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.PEAK_SENSITIVITY, out int peakSensitivity))
            {
                Config.PeakSensitivity = (SR7IF_PEAK_SENSITIVITY)peakSensitivity;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.PEAK_SELECT, out int peakSelect))
            {
                Config.PeakSelect = (SR7IF_PEAK_SELECT)peakSelect;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.X_SAMPLING, out int xSampling))
            {
                Config.XSampling = (SR7IF_X_SAMPLING)xSampling;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.FILTER_X_MEDIAN, out int filterXMedian))
            {
                Config.FilterXMedian = (SR7IF_FILTER_X_MEDIAN)filterXMedian;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.FILTER_X_SMOOTH, out int filterXSmooth))
            {
                Config.FilterXSmooth = (SR7IF_FILTER_X_SMOOTH)filterXSmooth;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.FILTER_Y_MEDIAN, out int filterYMedian))
            {
                Config.FilterYMedian = (SR7IF_FILTER_Y_MEDIAN)filterYMedian;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.FILTER_Y_SMOOTH, out int filterYSmooth))
            {
                Config.FilterYSmooth = (SR7IF_FILTER_Y_SMOOTH)filterYSmooth;
            }
            if (TryGetParam(SR7IF_SETTING_ITEM.CHANGE_3D_25D, out int changeMode))
            {
                Config.Change3D25D = (SR7IF_CHANGE_3D_25D)changeMode;
            }
        }

        public bool EnsureConnectedForParams()
        {
            if (iSR7APi != null && (iSR7APi.CameraAOnline || iSR7APi.CameraBOnline))
            {
                if (!IsConnected)
                {
                    IsConnected = true;
                }
                return true;
            }

            if (IsConnected && iSR7APi != null)
            {
                return true;
            }

            return Init();
        }

        private int GetConfigNum()
        {
            // 固定使用当前配方（-1）
            return -1;
        }

        private string ResolveCameraIp()
        {
            // 优先使用SSZN配置IP，未设置时兼容SensorSetView里的网口配置
            string ip = Config.CameraIP;
            if (string.IsNullOrWhiteSpace(ip) || ip == "127.0.0.1")
            {
                ip = IP;
            }

            if (!string.IsNullOrWhiteSpace(ip))
            {
                Config.CameraIP = ip;
            }

            return ip;
        }

        public void ShowTiff16Data(short[] inputsrcayyar, int[] outarray)
        {
            for (int i = 0; i < inputsrcayyar.Length; i++)
            {
                short data = inputsrcayyar[i];
                if (data == -32768)
                    outarray[i] = InvalidHeight32;
                else
                    outarray[i] = data;
            }
        }

        //private int ShowRGBImage(int[] heightData, SImagePro.SPointCloudHead pcHead, PictureBox pictureBox)
        //{
        //    if ((pcHead.height * pcHead.width) <= 0)
        //    {
        //        Log($"camera w:{pcHead.width},h:{pcHead.height} err!");
        //        return -1;
        //    }
        //    int ret = -1;

        //    //计算图像高度上下限，用于高度转灰度图自动设置上下限，也可以手动设置
        //    double Upper = INVALID_VALUE_MIN;
        //    double Lower = INVALID_VALUE_MAX;
        //    ret = SImagePro.SCV.CalUpperAndLower(heightData, pcHead.height, pcHead.width, ref Upper, ref Lower);

        //    // 创建一个与灰度图像大小相同的三通道矩阵，用于显示伪彩图
        //    byte[] gR = new byte[pcHead.height * pcHead.width];                           //红色
        //    byte[] gG = new byte[pcHead.height * pcHead.width];                           //绿色
        //    byte[] gB = new byte[pcHead.height * pcHead.width];                           //蓝色
        //    //高度转伪彩色图
        //    ret = SImagePro.SCV.PointToRGBData(heightData, pcHead.width, pcHead.height, 0, Upper, Lower, gR, gG, gB);
        //    // 生成 Bitmap 并显示
        //    //pictureBox.Image = null;
        //    pictureBox.Image = srcsharpTools.CreateBitmapFromChannels(gR, gG, gB, (int)pcHead.width, (int)pcHead.height);
        //    return ret;
        //}
        #endregion
    }

    #region Param
    [Serializable]
    public class SSZNSensroConfig :BindableBase
    {
        [JsonIgnore]
        private int _callbackMode;
        /// <summary>
        /// 0:一次回调
        /// 1:异步回调
        /// 2:无限循环回调
        /// </summary>
        public int CallbackMode
        {
            get { return _callbackMode; }
            set { _callbackMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _cameraIP = "192.168.0.10";  // SSZN相机默认IP
        public string CameraIP
        {
            get { return _cameraIP; }
            set { _cameraIP = value; RaisePropertyChanged(); }
        }

        // 已移除触发模式UI控件，固定使用软触发
        //[JsonIgnore]
        //private int _triggerMode;
        ///// <summary>
        ///// 0:软触发
        ///// 1:硬触发
        ///// </summary>
        //public int TriggerMode
        //{
        //    get { return _triggerMode; }
        //    set { _triggerMode = value; RaisePropertyChanged(); }
        //}

        [JsonIgnore]
        private int _rowCollected;
        /// <summary>
        /// 采集行数
        /// </summary>
        public int RowCollected
        {
            get { return _rowCollected; }
            set { _rowCollected = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _rowCallBack;
        /// <summary>
        /// 回调行数
        /// </summary>
        public int RowCallBack
        {
            get { return _rowCallBack; }
            set { _rowCallBack = value; RaisePropertyChanged(); }
        }

        // 已移除循环采集UI控件，固定为单次采集（无限循环回调模式除外）
        //[JsonIgnore]
        //private bool _loopScan;
        ///// <summary>
        ///// 循环取图
        ///// </summary>
        //public bool LoopScan
        //{
        //    get { return _loopScan; }
        //    set { _loopScan = value; RaisePropertyChanged(); }
        //}

        [JsonIgnore]
        private int _sync16bit;
        /// <summary>
        /// 回调行数
        /// </summary>
        public int Sync16bit
        {
            get { return _sync16bit; }
            set { _sync16bit = value; RaisePropertyChanged(); }
        }

        // 已移除配方编号UI控件，固定使用当前配方（-1）
        //[JsonIgnore]
        //private int _recipeNo = -1;
        ///// <summary>
        ///// 配方编号（-1：当前配方）
        ///// </summary>
        //public int RecipeNo
        //{
        //    get { return _recipeNo; }
        //    set { _recipeNo = value; RaisePropertyChanged(); }
        //}

        [JsonIgnore]
        private SAVEPOWEROFF _savePowerOff = SAVEPOWEROFF.ESAPO_SAVE;
        /// <summary>
        /// 断电保存
        /// </summary>
        public SAVEPOWEROFF SavePowerOff
        {
            get { return _savePowerOff; }
            set { _savePowerOff = value; RaisePropertyChanged(); }
        }

        // 已移除触发模式参数UI控件，相机参数仍可通过RefreshParamsFromSensor读取
        //[JsonIgnore]
        //private SR7IF_TRIG_MODE _trigModeParam;
        ///// <summary>
        ///// 触发模式参数
        ///// </summary>
        //public SR7IF_TRIG_MODE TrigModeParam
        //{
        //    get { return _trigModeParam; }
        //    set { _trigModeParam = value; RaisePropertyChanged(); }
        //}

        [JsonIgnore]
        private int _sampledCycle;
        /// <summary>
        /// 采样周期
        /// </summary>
        public int SampledCycle
        {
            get { return _sampledCycle; }
            set { _sampledCycle = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_BATCH_ON_OFF _batchOnOff;
        /// <summary>
        /// 批处理开关
        /// </summary>
        public SR7IF_BATCH_ON_OFF BatchOnOff
        {
            get { return _batchOnOff; }
            set { _batchOnOff = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_ENCODER_TYPE _encoderType;
        /// <summary>
        /// 编码器类型
        /// </summary>
        public SR7IF_ENCODER_TYPE EncoderType
        {
            get { return _encoderType; }
            set { _encoderType = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _encoderInputMode;
        /// <summary>
        /// 编码器输入模式
        /// </summary>
        public int EncoderInputMode
        {
            get { return _encoderInputMode; }
            set { _encoderInputMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _refiningPoints;
        /// <summary>
        /// 细化点数
        /// </summary>
        public int RefiningPoints
        {
            get { return _refiningPoints; }
            set { _refiningPoints = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_CYCLICAL_PATTERN _cyclicalPattern;
        /// <summary>
        /// 循环模式
        /// </summary>
        public SR7IF_CYCLICAL_PATTERN CyclicalPattern
        {
            get { return _cyclicalPattern; }
            set { _cyclicalPattern = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _segmentBuffer;
        /// <summary>
        /// 分段存储
        /// </summary>
        public bool SegmentBuffer
        {
            get { return _segmentBuffer; }
            set { _segmentBuffer = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_BATCH_OUTPUT _batchOutput;
        /// <summary>
        /// 批处理输出
        /// 0: 轮廓+亮度
        /// 1: 轮廓
        /// </summary>
        public SR7IF_BATCH_OUTPUT BatchOutput
        {
            get { return _batchOutput; }
            set { _batchOutput = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_Z_MEASURING_RANGE _zMeasuringRange;
        /// <summary>
        /// Z方向测量范围
        /// </summary>
        public SR7IF_Z_MEASURING_RANGE ZMeasuringRange
        {
            get { return _zMeasuringRange; }
            set { _zMeasuringRange = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_SENSITIVITY _sensitivity;
        /// <summary>
        /// 感光灵敏度
        /// </summary>
        public SR7IF_SENSITIVITY Sensitivity
        {
            get { return _sensitivity; }
            set { _sensitivity = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_EXP_TIME _expTime;
        /// <summary>
        /// 曝光时间
        /// </summary>
        public SR7IF_EXP_TIME ExpTime
        {
            get { return _expTime; }
            set { _expTime = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_LIGHT_CONTROL _lightControl;
        /// <summary>
        /// 光亮控制
        /// </summary>
        public SR7IF_LIGHT_CONTROL LightControl
        {
            get { return _lightControl; }
            set { _lightControl = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _lightMax;
        /// <summary>
        /// 激光亮度上限
        /// </summary>
        public int LightMax
        {
            get { return _lightMax; }
            set { _lightMax = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _lightMin;
        /// <summary>
        /// 激光亮度下限
        /// </summary>
        public int LightMin
        {
            get { return _lightMin; }
            set { _lightMin = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_PEAK_SENSITIVITY _peakSensitivity;
        /// <summary>
        /// 峰值灵敏度
        /// </summary>
        public SR7IF_PEAK_SENSITIVITY PeakSensitivity
        {
            get { return _peakSensitivity; }
            set { _peakSensitivity = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_PEAK_SELECT _peakSelect;
        /// <summary>
        /// 峰值选择
        /// </summary>
        public SR7IF_PEAK_SELECT PeakSelect
        {
            get { return _peakSelect; }
            set { _peakSelect = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_X_SAMPLING _xSampling;
        /// <summary>
        /// X轴压缩设定
        /// </summary>
        public SR7IF_X_SAMPLING XSampling
        {
            get { return _xSampling; }
            set { _xSampling = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_FILTER_X_MEDIAN _filterXMedian;
        /// <summary>
        /// X轴中位数滤波
        /// </summary>
        public SR7IF_FILTER_X_MEDIAN FilterXMedian
        {
            get { return _filterXMedian; }
            set { _filterXMedian = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_FILTER_X_SMOOTH _filterXSmooth;
        /// <summary>
        /// X轴平滑滤波
        /// </summary>
        public SR7IF_FILTER_X_SMOOTH FilterXSmooth
        {
            get { return _filterXSmooth; }
            set { _filterXSmooth = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_FILTER_Y_MEDIAN _filterYMedian;
        /// <summary>
        /// Y轴中位数滤波
        /// </summary>
        public SR7IF_FILTER_Y_MEDIAN FilterYMedian
        {
            get { return _filterYMedian; }
            set { _filterYMedian = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_FILTER_Y_SMOOTH _filterYSmooth;
        /// <summary>
        /// Y轴平滑滤波
        /// </summary>
        public SR7IF_FILTER_Y_SMOOTH FilterYSmooth
        {
            get { return _filterYSmooth; }
            set { _filterYSmooth = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private SR7IF_CHANGE_3D_25D _change3D25D;
        /// <summary>
        /// 3D/2.5D模式切换
        /// </summary>
        public SR7IF_CHANGE_3D_25D Change3D25D
        {
            get { return _change3D25D; }
            set { _change3D25D = value; RaisePropertyChanged(); }
        }
    }
    #endregion
}
