using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PrecitecClass
{
    /// <summary>
    ///  edit 2025.10.17 
    /// </summary>
    public class PrecitecControllerClassSync
    {
        public enum LoopDataMode
        {
            GetNextSamples,
            GetLastSample
        }

        private int _channelcount;

        public int ChannelCount
        {
            get
            {
                int c1 = TCHRLibFunctionWrapper.GetDeviceChannelCount(_conHandle.ConnectionHandle);
                return c1;
            }
            private set { _channelcount = value; }
        }

        private float[] _calibrationTable;

        public float[] CalibrationTable
        {
            get
            {
                return _calibrationTable;
            }
            private set
            {
                _calibrationTable = value;
            }
        }
        //是否开启扩展量程
        public bool IsExtendMode { get; private set; } = false;

        //当前表号
        public int TableIndex { get; private set; } = 0;

        public Action<List<DataSamples>> DataSampling;
        public Action<ConnectStatus> del_Status;
        private TCHRLibConnectionWrapper _conHandle;

        private ConnectStatus _connectStatus = ConnectStatus.Idle;

        public ConnectStatus Status
        {
            get { return _connectStatus; }
            set
            {
                _connectStatus = value;
                if (_connectStatus == ConnectStatus.Disconnected)
                {
                    del_Status?.Invoke(_connectStatus);
                }
            }
        }

        public enum ConnectStatus
        {
            Connected,
            Disconnected,
            Idle,
        }

        private TriggerStatus triggerStatus = TriggerStatus.CTN;

        public TriggerStatus Triggerstatus
        {
            get { return triggerStatus; }
            set { triggerStatus = value; }
        }

        public enum TriggerStatus
        {
            CTN,
            TRE,
            TRW,
            TRG,
            Unknown,
        }

        private bool isRunning = false;

        private LoopDataMode _loopDataMode = LoopDataMode.GetNextSamples;

        public LoopDataMode loopDataMode
        {
            get { return _loopDataMode; }
            set
            {
                _loopDataMode = value;
            }
        }

        public int DeviceType => TCHRLibFunctionWrapper.GetDeviceType(_conHandle.ConnectionHandle);


        public PrecitecControllerClassSync()
        {
            _conHandle = new TCHRLibConnectionWrapper();
        }

        public class DataSamples
        {
            public ushort id { get; set; }
            public double[] data { get; set; }
            public DataSamples Clone()
            {
                return new DataSamples { id = this.id, data = (double[])this.data.Clone() };
            }
        }

        private PrecitecControllerClassSync ShareConnection()
        {
            try
            {
                PrecitecControllerClassSync _p = new PrecitecControllerClassSync();
                _p._conHandle.OpenSharedConnection(_conHandle.ConnectionHandle, true);

                return _p;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        //0.raw  1.confocol 2.fft(interferometric)
        public void GetWaveLengthTable(out float[] wavelengthtable)
        {
            wavelengthtable = null;
            int tabletype = TCHRLibFunctionWrapper.Table_WaveLength;
            TCHRLibCmdWrapper.TDownloadTableCmd wave = new TCHRLibCmdWrapper.TDownloadTableCmd(tabletype);
            var res1 = TCHRLibCmdWrapper.ExecCommand(_conHandle.ConnectionHandle, wave, out TCHRLibCmdWrapper.IBaseRsp iRsp);

            if (TCHRLibFunctionWrapper.ResultSuccess(res1))
            {
                TCHRLibCmdWrapper.TDownloadTableRsp ow = (TCHRLibCmdWrapper.TDownloadTableRsp)iRsp;
                byte[] b = ow.TableData;
                float[] f = new float[b.Length / 4];

                for (int i = 0; i < f.Length; i++)
                {
                    f[i] = BitConverter.ToSingle(b, i * 4);
                }
                wavelengthtable = f;
            }
        }


        public (int, int, int, int, int, int) GetCalibrationTable(out float[] table_bin_all, out UInt16 range)
        {
            UInt16[] tabledata = null;
            int type = TCHRLibFunctionWrapper.Table_Confocal_Calibration;
            TCHRLibCmdWrapper.TDownloadTableCmd otab = new TCHRLibCmdWrapper.TDownloadTableCmd(type, TableIndex);
            var nes = TCHRLibCmdWrapper.ExecCommand(_conHandle.ConnectionHandle, otab, out TCHRLibCmdWrapper.IBaseRsp iRsp);

            TCHRLibCmdWrapper.TDownloadTableRsp tabrsp = (TCHRLibCmdWrapper.TDownloadTableRsp)iRsp;
            byte[] tabdata = tabrsp.TableData;

            UInt16 startpixel_shorten = 0;
            UInt16 endpixel_shorten = 0;
            int startpixel_valid = 0;
            int endpixel_valid = 0;
            int startpixel_all = 0;
            int endpixel_all = 999;
            range = 0;
            table_bin_all = null;

            int length = 8;
            byte[] b = new byte[length];
            Array.Copy(tabdata, tabdata.Length - 24, b, 0, b.Length);

            tabledata = new UInt16[b.Length / 2];

            for (int i = 0; i < b.Length / 2; i++)
            {
                tabledata[i] = BitConverter.ToUInt16(b, i * 2);
            }
            startpixel_shorten = tabledata[0];
            endpixel_shorten = tabledata[1];
            range = tabledata[2];
            UInt16 res16 = tabledata[3];

            int length2 = 16;
            byte[] b2 = new byte[length2];
            Array.Copy(tabdata, tabdata.Length - 16, b2, 0, b2.Length);
            UInt32[] tabledata2 = new UInt32[b2.Length / 4];
            for (int i = 0; i < b2.Length / 4; i++)
            {
                tabledata2[i] = BitConverter.ToUInt32(b2, i * 4);
            }
            UInt32 Calib_date = tabledata2[0];
            UInt32 TableMagicNumber = tabledata2[1];
            UInt32 reducedRangeMicrometer = tabledata2[2];
            UInt32 ProbeSN = tabledata2[3];

            int length3 = 4000;
            byte[] b3 = tabdata.Take(length3).ToArray();
            //找到实际不是0的作为其实与结束
            float[] tabledata3 = new float[b3.Length / 4];
            for (int i = 0; i < b3.Length / 4; i++)
            {
                tabledata3[i] = BitConverter.ToSingle(b3, i * 4);
            }
            startpixel_valid = Array.FindIndex(tabledata3, p => p != 0);
            endpixel_valid = Array.FindLastIndex(tabledata3, p => p != 0);
            //表长1000，后面24为是descriptor

            table_bin_all = tabledata3.Skip(startpixel_all).Take(endpixel_all - startpixel_all + 1).ToArray();
            tabledata3 = Array.ConvertAll(tabledata3, x => double.IsNaN(x) ? 0 : x);
            CalibrationTable = tabledata3;

            return (startpixel_shorten, endpixel_shorten, startpixel_valid, endpixel_valid, startpixel_all, endpixel_all);

        }
        public void GetSpectrum(int type, out short[] data, out double[] layerdata)
        {
            //type 0 raw  1 confocol  2 fft 这个方法只能下载这几个
            data = null; layerdata = null;
            if (_connectStatus == ConnectStatus.Disconnected)
                return;
            try
            {
                TCHRLibCmdWrapper.TDownloadSpectrumCmd ospec = new TCHRLibCmdWrapper.TDownloadSpectrumCmd(type);
                TCHRLibCmdWrapper.IBaseRsp rsp;
                var res1 = TCHRLibCmdWrapper.ExecCommand(_conHandle.ConnectionHandle, ospec, out rsp);
                TCHRLibCmdWrapper.TDownloadSpectrumRsp oRSpec = (TCHRLibCmdWrapper.TDownloadSpectrumRsp)(rsp);
                data = oRSpec?.SpecData;//光谱数据 

                var res = getLastSample();

                //只给单点用
                layerdata = res.Where(v => (v.id - 256) % 8 == 0).Select(item => item.data[0]).ToArray();
            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message);
            }
        }



        public void GetSpectrumImage(out byte[] data, out int width, out int height)
        {
            data = null; width = 0; height = 0;
            if (DeviceType != 2) return;

            try
            {
                string cra = ExecStringCommand("CRA?");
                if(cra=="")
                {
                    return;
                }
                MatchCollection matches = Regex.Matches(cra, "\\d+");

                List<int> numbers = new List<int>();
                foreach (Match match in matches)
                {
                    if (int.TryParse(match.Value, out int number))
                    {
                        numbers.Add(number);
                    }
                }
                width = ChannelCount;
                height = numbers[1] - numbers[0];
                TCHRLibCmdWrapper.TDownloadSpectrumImageCmd specImage = null;
                if (width == 192)
                {
                    width = width * 8;
                    height = height * 8;
                    specImage = new TCHRLibCmdWrapper.TDownloadSpectrumImageCmd(0, height - 174 * 6, width * 8, height);
                }
                else
                {
                    specImage = new TCHRLibCmdWrapper.TDownloadSpectrumImageCmd(0, 0, width, height);
                }



                var res = TCHRLibCmdWrapper.ExecCommand(_conHandle.ConnectionHandle, specImage, out TCHRLibCmdWrapper.IBaseRsp rsp);
                if (TCHRLibFunctionWrapper.ResultSuccess(res))
                {
                    TCHRLibCmdWrapper.TDownloadSpectrumImageRsp rspImage = (TCHRLibCmdWrapper.TDownloadSpectrumImageRsp)rsp;
                    data = rspImage.SpecImageData;
                }
                else
                {
                    Console.WriteLine("error in downloading spectrum image");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public int Connect(string IP, int deviceType)
        {
            try
            {
                _conHandle.OpenConnection(IP, deviceType, true, 1024 * 1024 * 1024);
                TCHRLibFunctionWrapper.SetLibConfigFlags(2);
                Status = ConnectStatus.Connected;
                _loopDataMode = LoopDataMode.GetLastSample;
                triggerStatus = TriggerStatus.CTN;

                //CalibrationTable
                if (deviceType != 2)
                {
                    GetCalibrationTable(out _, out _); //为了获取一下CalibrationTable
                }

                //为了获取通道数
                ExecStringCommand("SODX 16640");
                _conHandle.GetLastSample(out _);
                _channelcount = TCHRLibFunctionWrapper.GetDeviceChannelCount(_conHandle.ConnectionHandle);

                string rsp = ExecStringCommand("SEN?");
                MatchCollection matches = Regex.Matches(rsp, "\\d+");
                if (matches.Count == 1)
                {
                    int tableindex = int.Parse(matches[0].Value);
                    TableIndex = tableindex;
                }
                if (matches.Count == 2)
                {
                    int tableindex = int.Parse(matches[0].Value);
                    int enablescale = int.Parse(matches[1].Value);
                    TableIndex = tableindex;
                    if (enablescale == 0)
                    {
                        IsExtendMode = false;
                    }
                    else
                    {
                        IsExtendMode = true;
                    }
                }


                return 0;
            }
            catch (Exception ex)
            {
                Status = ConnectStatus.Disconnected;
                triggerStatus = TriggerStatus.Unknown;
                return -1;
            }
        }
        /// <summary>
        /// 连接传感器
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="deviceType"></param>
        /// <param name="timeout">等待时间seconds</param>
        /// <returns></returns>
        public int Connect(string IP, int deviceType, int timeout)
        {
            int res = 0;
            Action action = new Action(() =>
            {
                res = Connect(IP, deviceType);
            });

            IAsyncResult result = action.BeginInvoke(null, null);
            timeout = timeout * 1000;
            if (result.AsyncWaitHandle.WaitOne(timeout) && res == 0)
            {
                action.EndInvoke(result);
                return 0;
            }
            else
            {
                return -1;
            }
        }

        public void CloseConnection()
        {
            stopMeasure();
            _connectStatus = ConnectStatus.Disconnected;
            _conHandle.CloseConnection();
        }

        public string ExecStringCommand(string command)
        {
            try
            {
                command = command.ToUpper();
                string rsp = _conHandle.ExecStringCommand(command);

                if (command == "TRE" || command == "CTN" || command == "TRG" || command == "TRW")
                {
                    switch (command)
                    {
                        case "TRE":
                            triggerStatus = TriggerStatus.TRE;
                            _loopDataMode = LoopDataMode.GetNextSamples;
                            break;
                        case "TRW":
                            triggerStatus = TriggerStatus.TRW;
                            _loopDataMode = LoopDataMode.GetNextSamples;
                            break;
                        case "TRG":
                            triggerStatus = TriggerStatus.TRG;
                            _loopDataMode = LoopDataMode.GetNextSamples;
                            break;
                        case "CTN":
                            triggerStatus = TriggerStatus.CTN;
                            _loopDataMode = LoopDataMode.GetLastSample;
                            break;
                        default:
                            triggerStatus = TriggerStatus.CTN;
                            _loopDataMode = LoopDataMode.GetLastSample;
                            break;
                    }
                }
                // Match m1 = Regex.Match(command, @"^SEN\s+\d\s+\d$");
                Match m1 = Regex.Match(command, @"^SEN\s+\d+");
                if (m1.Success)
                {
                    MatchCollection matches = Regex.Matches(rsp, "\\d+");
                    if (matches.Count == 1)
                    {
                        int tableindex = int.Parse(matches[0].Value);
                        TableIndex = tableindex;
                    }
                    else if (matches.Count == 2)
                    {
                        int tableindex = int.Parse(matches[0].Value);
                        int enablescale = int.Parse(matches[1].Value);
                        TableIndex = tableindex;
                        if (enablescale == 0)
                        {
                            IsExtendMode = false;
                        }
                        else
                        {
                            IsExtendMode = true;
                        }
                    }
                }
                return rsp;
            }
            catch (TCHRLibConnectionException ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        private Task measureTask;

        public void startMeasure()
        {
            if (measureTask != null && !measureTask.IsCompleted)
                return;
            _conHandle.FlushConnectionBuffer();
            isRunning = true;
            ExecStringCommand("STA");
            measureTask = Task.Run(new Action(() => RunningThread()));
        }

        public void stopMeasure()
        {
            if (!isRunning)
            {
                return;
            }
            isRunning = false;
            if (_connectStatus == ConnectStatus.Disconnected)
                return;
            string rsp = ExecStringCommand("STO");
            if (rsp == "") //网线断了
                return;
            measureTask.Wait();
        }

        //获取最新的一次采样
        public List<DataSamples> getLastSample()
        {
            try
            {
                stopMeasure();

                ExecStringCommand("STA");
                int res = _conHandle.GetLastSample(out TCHRLibConnData _oData);
                if (res < 0)
                {
                    try
                    {
                        _conHandle.ExecStringCommand("SHZ?");  //随便发个指令，报错就是断开，不报错就没断开，可以继续下一次获取数据
                    }
                    catch
                    {
                        //"断开连接了" 判断断开连接首先要连上，设置输出信号，设置setlibflags(2),确认好后如果抛异常就是断连了
                        //目前有,三种情况导致：1.没有设置输出项目，2.没有设置SetLibFlags(2) 3.连接断开;
                        Status = ConnectStatus.Disconnected;
                        return null;
                    }
                }

                List<DataSamples> dataSamples = new List<DataSamples>();
                for (int j = 0; j < _oData.SignalInfos.Count; j++)
                {
                    dataSamples.Add(new DataSamples
                    {
                        id = _oData.SignalInfos[j].SignalID,
                        data = _oData.GetData(0, j).Select(value => double.IsNaN(value) ? 0 : value).ToArray(),
                    });
                }
                List<DataSamples> _list = DeepCopy(dataSamples);
                startMeasure();
                return _list;
            }
            catch (Exception e)
            {
                return null;
            }

        }

        //获取指定量的数据，会阻塞原来的实时数据获取，如果需要实时获取，则要重新调用startmeasure
        public async Task<List<List<DataSamples>>> getNextSamples(long samplecount)
        {
            stopMeasure();
            ExecStringCommand("STA");
            _conHandle.FlushConnectionBuffer();
            List<DataSamples> dataSamplestemp = new List<DataSamples>();
            List<List<DataSamples>> listsamples = new List<List<DataSamples>>();
            await Task.Run(() =>
            {
                long count = 0;
                while (count < samplecount)
                {
                    int res = _conHandle.GetNextSamples(samplecount, out TCHRLibConnData _oData);
                    if (res < 0)
                    {
                        try
                        {
                            _conHandle.ExecStringCommand("SHZ?");  //随便发个指令，报错就是断开，不报错就没断开，可以继续下一次获取数据
                        }
                        catch
                        {
                            //"断开连接了" 判断断开连接首先要连上，设置输出信号，设置setlibflags(2),确认好后如果抛异常就是断连了
                            //目前有, 三种情况导致：1.没有设置输出项目，2.没有设置SetLibFlags(2) 3.连接断开;
                            Status = ConnectStatus.Disconnected;
                        }
                    }
                    if (_oData.SampleCount == 0)
                        continue;

                    dataSamplestemp.Clear();
                    for (int i = 0; i < _oData.SampleCount; i++)
                    {
                        for (int j = 0; j < _oData.SignalInfos.Count; j++)
                        {
                            dataSamplestemp.Add(new DataSamples
                            {
                                id = _oData.SignalInfos[j].SignalID,
                                data = _oData.GetData(i, j).Select(value => double.IsNaN(value) ? 0 : value).ToArray()
                            });
                        }
                        listsamples.Add(dataSamplestemp);
                    }
                    count += _oData.SampleCount;
                    Thread.Sleep(1);
                }
            });

            startMeasure();
            return listsamples;
        }

        private List<DataSamples> DeepCopy(List<DataSamples> listtoCopy)
        {
            var newlist = new List<DataSamples>(listtoCopy.Count);
            foreach (var item in listtoCopy)
            {
                newlist.Add(item.Clone());
            }
            return newlist;
        }


        void RunningThread()
        {
            List<DataSamples> dataSamples = new List<DataSamples>();
            while (isRunning)
            {
                try
                {
                   int res = 0;
                    TCHRLibConnData _oData = new TCHRLibConnData();

                    if (_loopDataMode == LoopDataMode.GetNextSamples)
                        res = _conHandle.GetNextSamples(5000, out _oData);
                    else if (_loopDataMode == LoopDataMode.GetLastSample)
                        res = _conHandle.GetLastSample(out _oData);

                    if (res < 0)
                    {
                        try
                        {
                            _conHandle.ExecStringCommand("SHZ?");  //随便发个指令，报错就是断开，不报错就没断开，可以继续下一次获取数据
                        }
                        catch
                        {
                            //"断开连接了" 判断断开连接首先要连上，设置输出信号，设置setlibflags(2),确认好后如果抛异常就是断连了
                            Status = ConnectStatus.Disconnected;
                            isRunning = false;
                        }
                        continue; //目前有,三种情况导致：1.没有设置输出项目，2.没有设置SetLibFlags(2) 3.连接断开;
                    }

                    if (_oData.SampleCount == 0) continue;

                    for (int i = 0; i < _oData.SampleCount; i++)
                    {
                        dataSamples.Clear();
                        for (int j = 0; j < _oData.SignalInfos.Count; j++)
                        {
                            dataSamples.Add(new DataSamples
                            {
                                id = _oData.SignalInfos[j].SignalID,
                                data = _oData.GetData(i, j).Select(value => double.IsNaN(value) ? 0 : value).ToArray()
                            });
                        }
                        List<DataSamples> _list = DeepCopy(dataSamples);
                        DataSampling?.Invoke(_list);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Thread.Sleep(1);
                    continue;
                }
                Thread.Sleep(1);
            }
        }
    }
}
