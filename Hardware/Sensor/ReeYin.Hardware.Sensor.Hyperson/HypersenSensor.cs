using Dm;
using Dm.util;
using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Hyperson.API;
using ReeYin.Hardware.Sensor.Hyperson.CustomUI.Defines;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using System.Collections;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ReeYin.Hardware.Sensor.Hyperson
{

    public sealed class LcfManagedResult
    {
        public int XCount;
        public int YCount;
        public int LayerCount;

        public int[] HeightRaw;     // 已深拷贝
        public short[] GrayRaw;     // 已深拷贝
    }

    /// <summary>
    /// 海伯森传感器
    /// </summary>
    public class HypersenSensor : SensorBase
    {
        #region Fields
        // 线程安全缓存结构：存储收到的 LCFResult 列表
        private readonly List<LcfManagedResult> _resultCache = new List<LcfManagedResult>();
        private readonly object _cacheLock = new object(); // 缓存锁

        // 最大缓存大小（用于防止无限内存增长）
        private const int MAX_CACHE_SIZE = 4000;

        public int ControlerHandle;

        FixLengthList<Tuple<int[], short[], LCF_Result_t>> _tempSensorData = new FixLengthList<Tuple<int[], short[], LCF_Result_t>>(100);

        /// <summary>
        /// 配置
        /// </summary>
        public HypersenSensorConfig Config = new HypersenSensorConfig();

        private int _receiveCount = 0;

        /// <summary>
        /// LCF设备配置
        /// </summary>
        public LCF_DeviceSetting_t deviceSetting;

        LCF_SensorHeadFactoryPara_t sensorHeadFactoryPara;

        public float[] intenSity;

        /// <summary>
        /// 接受存放的分辨率
        /// </summary>
        public int Fx = 0;
        public int signal_len = 0;

        /// <summary>
        /// 存放信号
        /// </summary>
        [JsonIgnore]
        public float?[][] Fx_Buf;
        public short[] Signal_Buf;

        private int _hdrindex = 0;

        public bool[] intenSity_able = new bool[8];
        public string[] intenSity_value = new string[8];

        public bool EnTrig_flag = false;


        #region 回调参数
        LCF_UserEventCallbackHandleDelegate eventCallback;

        int g_GetDataMethod = 1;
        #endregion


        #endregion

        #region Properties
        [JsonIgnore]
        private int _getDataMode = 2;
        /// <summary>
        /// 采集数据方式
        /// 1：停止导出
        /// 2：回调导出
        /// </summary>
        public int GetDataMode
        {
            get { return _getDataMode; }
            set { _getDataMode = value; RaisePropertyChanged(); }
        }
        [JsonIgnore]
        private int _hightLayer = 1;
        /// <summary>
        /// 高度数据层数
        /// </summary>
        public int HightLayer
        {
            get { return _hightLayer; }
            set { _hightLayer = value; RaisePropertyChanged(); }
        }


        [JsonIgnore]
        private CMOSConfig _cmosConfig = new CMOSConfig();

        public CMOSConfig CmosConfig
        {
            get { return _cmosConfig; }
            set { _cmosConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private TriggerConfig _triggerConfig = new TriggerConfig();

        public TriggerConfig TriggerConfig
        {
            get { return _triggerConfig; }
            set { _triggerConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ContourConfig _contourConfig = new ContourConfig();

        public ContourConfig ContourConfig
        {
            get { return _contourConfig; }
            set { _contourConfig = value; RaisePropertyChanged(); }
        }

        #region 其他配置
        private bool _isClearCacheData;

        public bool IsClearCacheData
        {
            get { return _isClearCacheData; }
            set { _isClearCacheData = value; RaisePropertyChanged(); }
        }

        private bool _isExportCacheData;

        public bool IsExportCacheData
        {
            get { return _isExportCacheData; }
            set { _isExportCacheData = value; RaisePropertyChanged(); }
        }

        #endregion

        #endregion

        #region Constructor
        public HypersenSensor()
        {

        }
        #endregion

        #region Override
        public override bool Init()
        {
            if (LCFDevice.LCF_IsConnect(ControlerHandle))
            {
                State = HardwareState.Connected;
                return true;
            }

            //注册回调
            eventCallback = new LCF_UserEventCallbackHandleDelegate(UserEventCallbackHandle);
            LCFDevice.LCF_RegisterEventCallback(eventCallback, IntPtr.Zero);

            State = HardwareState.Connecting;

            int tmpCtlHandle;
            var ret = LCFDevice.LCF_ConnectDevice(IP, Port, out tmpCtlHandle);
            ControlerHandle = tmpCtlHandle;
            if (ret == LCF_StatusTypeDef.LCF_Status_Succeed)
            {
                //设置回调阈值
                LCFDevice.LCF_SetIntParameter(ControlerHandle, LCF_ParameterDefine.PARAM_COUNTOUR_LINE_THRESHOLD, 10);
                State = HardwareState.Connected;

                IsConnected = true;
                GetMeasureParam();

                Config.ExposureTime = deviceSetting.exposureTime;
                CmosConfig.ExposureTime = deviceSetting.exposureTime;
                IsConnected = true;
                return true;
            }
            else
            {
                State = HardwareState.NotConnected;
                IsConnected = false;
                return false;
            }
        }

        public override void Close()
        {
            State = HardwareState.Disconnecting;
            if (LCFDevice.LCF_IsConnect(ControlerHandle))
            {
                State = HardwareState.Closed;
                LCFDevice.LCF_CloseDevice(ControlerHandle);
                IsConnected = false;
            }
        }

        public override void StartCollect()
        {
            Logs.LogInfo("开始采集");
            lock (this)
            {
                if (!LCFDevice.LCF_IsConnect(ControlerHandle))
                {
                    State = HardwareState.NotConnected;
                    return;
                }
                if (State == HardwareState.Running) return;
                State = HardwareState.Running;

                if (!LCFDevice.LCF_IsStart(ControlerHandle))
                {
                    var ret = LCFDevice.LCF_StartCapture(ControlerHandle);
                    if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                    {
                        return;
                    }
                }
                _receiveCount = 0;
                LCFDevice.LCF_ClearCacheData(ControlerHandle);
                //清除编码器计数值
                LCFDevice.LCF_ClearEncoderTriggerCount(ControlerHandle);
                //在获取一下数据，确保为空的
                //var temp = ReceiveSensorData();
            }

            Logs.LogInfo("6传感器结束");
        }

        public override void StopCollect()
        {
            lock (this)
            {
                if(GetDataMode == 1)
                {
                    _receiveCount = 0;
                    if (LCFDevice.LCF_IsStart(ControlerHandle))
                    {
                        var ret = LCFDevice.LCF_StopCapture(ControlerHandle);

                        //AnalysisData();

                        if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                        {
                            Logs.LogError("停止采集失败！！！");
                            return;
                        }
                        State = HardwareState.Complete;
                    }
                }
                else if(GetDataMode == 2)
                {
                    _receiveCount = 0;
                    if (LCFDevice.LCF_IsStart(ControlerHandle))
                    {
                        var ret = LCFDevice.LCF_StopCapture(ControlerHandle);

                        //AnalysisData();

                        if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                        {
                            Logs.LogError("停止采集失败！！！");
                            return;
                        }
                        State = HardwareState.Complete;
                    }
                }
            }
        }

        public override List<MeasureData> ReceiveSensorData()
        {
            lock (this)
            {
                Tuple<List<List<float[]>>, List<float[]>> result = null;
                if (GetDataMode == 1)
                {
                    result = AnalysisData();
                }
                else if(GetDataMode == 2)
                {
                    result = ExportCallbackData();
                }

                List<MeasureData> ListMeasureData = new List<MeasureData>();

                if (result == null)
                    return new List<MeasureData>();

                for (int i = 0; i < HightLayer; i++)
                {
                    Logs.LogInfo($"高度数据一共为{result.Item1.Count}个，" +
                        $"{i}层数据为{result.Item1[i].Count}个");
                }

                var heightData1 = result.Item1[0];
                var heightData2 = result.Item1[1];
                var grayData = result.Item2;

                for (int i = 0; i < heightData1.Count; i++)
                {
                    List<float[]> areaData = new();

                    areaData.Add(heightData1[i]);

                    if (HightLayer >= 2)
                        areaData.Add(heightData2[i]);

                    areaData.Add(grayData[i]);

                    MeasureData md = new MeasureData
                    {
                        AreaData = areaData
                    };

                    ListMeasureData.Add(md);
                }
                //for (int i = 0; i < heightData1.Count; i++)
                //{
                //    var height1 = heightData1[i];
                //    float[] height2 = null;
                //    if (HightLayer == 2)
                //    {
                //        height2 = heightData2[i];
                //    }

                //    var gray = grayData[i];

                //    MeasureData md = new MeasureData();
                //    if(HightLayer == 2)
                //        md.AreaData = [height1, height2, gray];
                //    else
                //    {
                //        md.AreaData = [height1,gray];
                //    }
                //    ListMeasureData.Add(md);
                //}

                return ListMeasureData;
            }
        }

        public override bool SaveConfig()
        {
            SetSingleParam("int", LCF_ParameterDefine.PARAM_EXPOSURE_TIME, CmosConfig.ExposureTime);
            SaveToSensor();
            return base.SaveConfig();
        }
        #endregion

        #region CallBack
        /// <summary>
        /// 回调
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="arg"></param>
        /// <param name="userPara"></param>
        public void UserEventCallbackHandle(int handle, ref LCF_EventCallbackArgs_t arg, IntPtr userPara)
        {
            //接收到数据
            if (arg.eventType == LCF_EventTypeDef.LCF_EventType_DataRecv)
            {
                //数据类型
                if (arg.rid == LCF_DataRid_t.LCF_RID_RESULT)                //数据接收事件
                {
                    //记录最新一次的结果，用于界面显示
                    var lcf_result = LCFDevice.getLCFResult(arg.data);

                    // ⭐ 优化点：在回调中立即获取托管结构体，避免在回调外使用 IntPtr
                    LCF_Result_t currentResult = LCFDevice.getLCFResult(arg.data);


                    //获取扫描数据三种方式: 
                    //1、扫描结束后通过LCF_ExportCacheData接口将扫描数据一次性从控制器导出来


                    //2、扫描过程中通过回调函数收集扫描数据
                    //注意: 1、回调函数里面不应该做耗时的操作，应该只做数据拷贝。
                    //      2、在扫描帧率比较高的情况下通过设置PARAM_COUNTOUR_LINE_THRESHOLD参数，调高轮廓回调阈值，减少回调的频率，保证数据能及时缓存不会丢失数据。
                    //      3、该参数仅用于降低回调的频率，不用于一次性导出缓存数据


                    //3、通过PARAM_CACHE_COUNTOUR_THRESHOLD设置Cache缓存轮廓的阈值，达到阈值后通过回调函数通知用户，用户通过LCF_ExportCacheData导出缓存数据，通过LCF_ClearCacheData清空缓存数据后重新开始计数
                    //注意: 1、该参数一般用在用户已知总共扫描轮廓个数的场景，达到设定轮廓数后通知用户


                    //当前处于编码器触发模式且通过回调函数收集扫描数据
                    if (TriggerConfig.TriggerModelIndex == (TriggerModel)1 && g_GetDataMethod == 1)
                    {
                        // ⭐ 优化点：使用 lock 保护缓存，只进行添加操作
                        lock (_cacheLock)
                        {
                            var native = LCFDevice.getLCFResult(arg.data);

                            int width = (int)native.x_count;
                            int yCount = (int)native.y_count;
                            int layerCount = (int)native.layer_number;

                            int pointsPerLayer = width * yCount;
                            int totalPoints = pointsPerLayer * layerCount;

                            // 1️⃣ 深拷贝高度数据
                            int[] heightRaw = new int[totalPoints];
                            Marshal.Copy(native.height, heightRaw, 0, totalPoints);

                            // 2️⃣ 深拷贝灰度数据
                            short[] grayRaw = new short[pointsPerLayer];
                            Marshal.Copy(native.intensity, grayRaw, 0, pointsPerLayer);

                            // 3️⃣ 存入托管结构
                            _resultCache.Add(new LcfManagedResult
                            {
                                XCount = width,
                                YCount = yCount,
                                LayerCount = layerCount,
                                HeightRaw = heightRaw,
                                GrayRaw = grayRaw
                            });
                        }
                    }
                }
                //Cache轮廓数量已经达到阈值且当前处于编码器触发模式
                else if (arg.rid == LCF_DataRid_t.LCF_RID_CACHE_REACH_THRE && TriggerConfig.TriggerModelIndex == (TriggerModel)1 && g_GetDataMethod == 2)
                {
                    //导出Cache轮廓数据，由于达到阈值后电机可能没有马上停止，所以实际导出的轮廓个数可能会大于等于用户设定的阈值
                    //ExportData((UInt32)NUD_outlineNum.Value);
                }
                else if (arg.rid == LCF_DataRid_t.LCF_RID_IO_ASYNC_EVENT)  //控制器IO触发事件
                {
                    if (arg.dataLen == 2)
                    {
                        string str_info = "Please update SDK! ";    //使用IO触发功能请把SDK升级到 1.4.6 版本以上
                        Console.WriteLine(str_info);
                    }
                    else if (arg.dataLen == 3)
                    {
                        //控制器IO触发的引脚
                        LCF_IO_Port_t port = (LCF_IO_Port_t)Marshal.ReadByte(arg.data, 0);

                        //IO触发类型
                        LCF_IO_TriggerMode_t trigger = (LCF_IO_TriggerMode_t)Marshal.ReadByte(arg.data, 2);

                        //该引脚绑定的功能
                        LCF_IO_Func_t func = (LCF_IO_Func_t)Marshal.ReadByte(arg.data, 1);

                        string str_portNumber = "";
                        string str_fun = "";
                        string str_trigger = "";
                        if (port == LCF_IO_Port_t.LCF_IO_IN0)
                        {
                            str_portNumber = "IN0";
                        }
                        else if (port == LCF_IO_Port_t.LCF_IO_IN1)
                        {
                            str_portNumber = "IN1";
                        }

                        if (trigger == LCF_IO_TriggerMode_t.LCF_IO_TriggerRasing)
                        {
                            str_trigger = "IO_TriggerFalling";
                        }
                        else if (trigger == LCF_IO_TriggerMode_t.LCF_IO_TriggerFalling)
                        {
                            str_trigger = "IO_TriggerFalling";
                        }
                        else if (trigger == LCF_IO_TriggerMode_t.LCF_IO_TriggerRasingFalling)
                        {
                            str_trigger = "IO_TriggerRasingFalling";
                        }

                        if (func == LCF_IO_Func_t.LCF_IO_Func_StartSample)
                        {
                            str_fun = "StartSample";
                        }
                        else if (func == LCF_IO_Func_t.LCF_IO_Func_StopSample)
                        {
                            str_fun = "StopSample";
                        }
                        else if (func == LCF_IO_Func_t.LCF_IO_Func_ClearCache)
                        {
                            str_fun = "ClearCache";
                        }
                        else if (func == LCF_IO_Func_t.LCF_IO_Func_AsyncNotice)
                        {
                            str_fun = "AsyncNotice";
                        }

                        string str_info = "IO port trigger event-->" + str_portNumber + ":" + str_trigger + " --> " + str_fun;
                        Console.WriteLine(str_info);
                    }
                }
                else if (arg.rid == LCF_DataRid_t.LCF_RID_API_CALL_EXCEPTION) //API调用异常事件
                {
                    byte[] bytes = System.Text.Encoding.Unicode.GetBytes(Marshal.PtrToStringUni(arg.data));//转成UNICODE编码
                    string str = System.Text.Encoding.UTF8.GetString(bytes);//再转成UTF8
                    Console.WriteLine(str);

                }
            }
            else if (arg.eventType == LCF_EventTypeDef.LCF_EventType_Disconnect)
            {
                Console.WriteLine("设备断开连接\r\n");
            }
        }

        #endregion


        #region Methods
        public void SaveToSensor()
        {
            if (!LCFDevice.LCF_IsConnect(ControlerHandle))
            {
                return;
            }

            LCF_StatusTypeDef ret = LCFDevice.LCF_SaveSetting(ControlerHandle);
        }

        /// <summary>
        /// 设置参数
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Handle"></param>
        /// <param name="ParamName"></param>
        /// <param name="value"></param>
        public bool SetSingleParam(string Type, string ParamName, object value)
        {
            //检查是否连接设备
            if (!LCFDevice.LCF_IsConnect(ControlerHandle))
            {
                Console.WriteLine("请先连接设备\r\n");
                return false;
            }

            switch (Type)
            {
                case "int":
                    {
                        int temp = Convert.ToInt32(value);
                        LCF_StatusTypeDef ret = LCFDevice.LCF_SetIntParameter(ControlerHandle, ParamName, temp);
                        if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                        {
                            Console.WriteLine($"设置int类型数据({ParamName})失败! 错误码:" + ret.ToString());
                            return false;
                        }
                        else
                        {
                            Console.WriteLine($"设置int类型数据({ParamName})成功");
                            return true;
                        }
                    }
                    break;

                case "float":
                    {
                        float temp = Convert.ToSingle(value);
                        LCF_StatusTypeDef ret = LCFDevice.LCF_SetFloatParameter(ControlerHandle, ParamName, temp);
                        if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                        {
                            Console.WriteLine($"设置float类型数据({ParamName})失败! 错误码:" + ret.ToString());
                            return false;
                        }
                        else
                        {
                            Console.WriteLine($"设置float类型数据({ParamName})成功");
                            return true;
                        }
                    }
                    break;

                default:
                    {
                        return false;
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Handle"></param>
        /// <param name="ParamName"></param>
        /// <param name="value"></param>
        public void GetSingleParam(string Type, string ParamName, out object value)
        {
            value = null;
            //检查是否连接设备
            if (!LCFDevice.LCF_IsConnect(ControlerHandle))
            {
                Console.WriteLine("请先连接设备\r\n");
                return;
            }
            switch (Type)
            {
                case "int":
                    {
                        int temp;
                        LCF_StatusTypeDef ret = LCFDevice.LCF_GetIntParameter(ControlerHandle, ParamName, out temp);
                        if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                        {
                            Console.WriteLine($"获取int类型数据({ParamName})失败! 错误码:" + ret.ToString());
                            return;
                        }
                        else
                        {
                            value = temp;
                            Console.WriteLine($"获取int类型数据({ParamName})成功");
                            return;
                        }
                    }
                    break;
                case "float":
                    {
                        float temp;
                        LCF_StatusTypeDef ret = LCFDevice.LCF_GetFloatParameter(ControlerHandle, ParamName, out temp);
                        if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                        {
                            Console.WriteLine($"获取float类型数据({ParamName})失败! 错误码:" + ret.ToString());
                            return;
                        }
                        else
                        {
                            value = temp;
                            Console.WriteLine($"获取float类型数据({ParamName})成功");
                            return;
                        }
                    }
                    break;
                case "string":
                    {
                        string temp;
                        LCF_StatusTypeDef ret = LCFDevice.LCF_GetStringParameter(ControlerHandle, ParamName, out temp);
                        if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                        {
                            Console.WriteLine($"获取strnig类型数据({ParamName})失败! 错误码:" + ret.ToString());
                            return;
                        }
                        else
                        {
                            value = temp;
                            Console.WriteLine($"获取string类型数据({ParamName})成功");
                            return;
                        }
                    }
                    break;
                default:
                    {
                    }
                    break;
            }
        }

        /// <summary>
        /// 执行自定义指令
        /// </summary>
        /// <param name="Handle"></param>
        /// <param name="Custom"></param>
        /// <returns></returns>
        public bool ExecuteCustomCommand(Func<object> Custom)
        {
            try
            {
                //检查是否连接设备
                if (!LCFDevice.LCF_IsConnect(ControlerHandle))
                {
                    Console.WriteLine("请先连接设备\r\n");
                    return false;
                }

                #region 执行自定义指令
                var ret = Custom();
                if (ret == null)
                {
                    Console.WriteLine("传感器返回为空，请检查指令是否正确！");
                    return false;
                }
                else
                {
                    Console.WriteLine("执行自定义指令成功\r\n");
                    return true;
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.StackTrace}");
                return false;
            }
        }

        #region 处理数据
        /// <summary>
        /// 所有数据集
        /// </summary>
        public Queue<Tuple<List<List<float[]>>, List<float[]>>> allDatas;

        /// <summary>
        /// 解析数据（多层高度、灰度）
        /// </summary>
        private Tuple<List<List<float[]>>, List<float[]>> AnalysisData()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now.ToString("HHmmss.ff")}：传感器开始解析数据....");
                // 1）导出缓存
                LCF_Result_t result;
                var ret = LCFDevice.LCF_ExportCacheData(ControlerHandle, out result);
                if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                    return default;

                int width = (int)result.x_count;
                int height = (int)result.y_count;
                int layerCount = (int)result.layer_number;
                Console.WriteLine($"LayerCount为：{layerCount}");
                int pointsPerLayer = width * height;
                int totalPoints = pointsPerLayer * layerCount;

                // ======================
                // 2）读取全部 height 数据
                // ======================
                int[] rawHeights = new int[totalPoints];
                Marshal.Copy(result.height, rawHeights, 0, totalPoints);

                List<List<float[]>> layers = new List<List<float[]>>();

                //for (int l = 0; l < layerCount; l++)
                for (int l = 0; l < HightLayer; l++)//先固定读取两层
                {
                    List<float[]> oneLayerRows = new List<float[]>();

                    for (int y = 0; y < height; y++)
                    {
                        float[] row = new float[width];

                        for (int x = 0; x < width; x++)
                        {
                            int index = l * pointsPerLayer + y * width + x;

                            int raw = rawHeights[index];

                            if (raw == LCFDevice.INVALID_VALUE)
                                row[x] = 888888.0f;
                            else
                                row[x] = raw / 10.0f;
                        }

                        oneLayerRows.Add(row);
                    }

                    layers.Add(oneLayerRows);
                }

                // ======================
                // 3）灰度图（只有一层）
                // ======================
                short[] rawGray = new short[pointsPerLayer];
                Marshal.Copy(result.intensity, rawGray, 0, pointsPerLayer);

                List<float[]> grayList = new List<float[]>();

                for (int y = 0; y < height; y++)
                {
                    float[] row = new float[width];
                    for (int x = 0; x < width; x++)
                    {
                        row[x] = rawGray[y * width + x];
                    }

                    grayList.Add(row);
                }

                // ======================
                // 4）清空缓存
                // ======================
                LCFDevice.LCF_ClearCacheData(ControlerHandle);
                LCFDevice.LCF_ClearEncoderTriggerCount(ControlerHandle);
                Console.WriteLine($"{DateTime.Now.ToString("HHmmss.ff")}：传感器完成解析数据");
                var lastDatas = Tuple.Create(layers, grayList);
                //allDatas.Enqueue(lastDatas);
                return lastDatas;
            }
            catch (Exception ex)
            {
                Console.WriteLine("传感器解析数据失败：" + ex.StackTrace.ToString());
                return null;
            }
           
        }

        /// <summary>
        /// 异步处理和导出回调函数收集到的数据。
        /// </summary>
        public Tuple<List<List<float[]>>, List<float[]>> ExportCallbackData()
        {
            List<LcfManagedResult> resultsToProcess = new();

            lock (_cacheLock)
            {
                if (_resultCache.Count == 0)
                {
                    // 使用 Debug 或 try-catch 保护 Console，避免句柄无效错误
                    System.Diagnostics.Debug.WriteLine("生成失败，请检查采集是否开始\r\n");
                    return null;
                }
                resultsToProcess.AddRange(_resultCache);
                _resultCache.Clear();
            }

            //CheckDuplicateIntensity(resultsToProcess);

            if (resultsToProcess.Count == 0) return null;

            // 初始化最终容器
            // layers[0] 存放第一层所有结果的行，layers[1] 存放第二层，以此类推
            List<List<float[]>> layers = new List<List<float[]>>();
            List<float[]> grayList = new List<float[]>(); // 灰度图（通常只有一层）

            // 假设我们要支持解析的层数（参考 AnalysisData 固定解析 2 层，或者使用 result.layer_number）
            // 这里先初始化 List 结构，防止 Add 时越界
            const int targetLayerCount = 2;
            for (int i = 0; i < targetLayerCount; i++)
            {
                layers.Add(new List<float[]>());
            }

            foreach (var result in resultsToProcess)
            {
                int width = result.XCount;
                int yCount = result.YCount;
                int layerCount = result.LayerCount;

                int pointsPerLayer = width * yCount;

                for (int l = 0; l < targetLayerCount && l < layerCount; l++)
                {
                    for (int y = 0; y < yCount; y++)
                    {
                        float[] row = new float[width];
                        for (int x = 0; x < width; x++)
                        {
                            int index = l * pointsPerLayer + y * width + x;
                            int raw = result.HeightRaw[index];

                            row[x] = raw == LCFDevice.INVALID_VALUE
                                ? 888888.0f
                                : raw / 10.0f;
                        }
                        layers[l].Add(row);
                    }
                }

                // 灰度
                for (int y = 0; y < yCount; y++)
                {
                    float[] row = new float[width];
                    for (int x = 0; x < width; x++)
                    {
                        row[x] = result.GrayRaw[y * width + x];
                    }
                    grayList.Add(row);
                }
            }

            //        SaveFloatColumnsToCsv(
            //grayList,
            //@"C:\Users\REECHI-3D\Desktop\新建文件夹 (2)\test.csv");
            return Tuple.Create(layers, grayList);
        }


        public static void CheckDuplicateIntensity(
        List<LCF_Result_t> results)
        {
            // key: intensity 地址
            // value: 出现该地址的索引列表
            var map = new Dictionary<IntPtr, List<int>>();

            for (int i = 0; i < results.Count; i++)
            {
                IntPtr ptr = results[i].intensity;

                // 可选：忽略空指针
                if (ptr == IntPtr.Zero)
                    continue;

                if (!map.TryGetValue(ptr, out var indexList))
                {
                    indexList = new List<int>();
                    map[ptr] = indexList;
                }

                indexList.Add(i);
            }

            // 输出重复的 intensity
            foreach (var kv in map)
            {
                if (kv.Value.Count > 1)
                {
                    Console.WriteLine(
                        $"Intensity Address: 0x{kv.Key.ToInt64():X}");

                    foreach (int index in kv.Value)
                    {
                        Console.WriteLine($"  -> List Index: {index}");
                    }
                }
            }
        }

        public static void SaveFloatColumnsToCsv(
       List<float[]> columns,
       string filePath,
       string[] columnHeaders = null)
        {
            if (columns == null || columns.Count == 0)
                throw new ArgumentException("columns 不能为空");

            int columnCount = columns.Count;
            int rowCount = columns.Max(c => c?.Length ?? 0);

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            // 写入表头（可选）
            if (columnHeaders != null && columnHeaders.Length == columnCount)
            {
                writer.WriteLine(string.Join(",", columnHeaders));
            }

            // 按行写入
            for (int row = 0; row < rowCount; row++)
            {
                var line = new string[columnCount];

                for (int col = 0; col < columnCount; col++)
                {
                    var column = columns[col];
                    if (column != null && row < column.Length)
                    {
                        line[col] = column[row]
                            .ToString("G", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        line[col] = string.Empty;
                    }
                }

                writer.WriteLine(string.Join(",", line));
            }
        }
        /// <summary>
        /// 保存数据
        /// </summary>
        public void SaveData()
        {
            var (heightData, grayData, result) = _tempSensorData.SafeFirst();

            var heigthFloatData = new float[heightData.Length];

            //除以10等于实际测量数值
            for (int i = 0; i < result.x_count * result.y_count; i++)
            {
                heightData[i] /= 10;
                heigthFloatData[i] = heightData[i] / 10;
            }

            byte[] grayscale_8bit = new byte[result.x_count * result.y_count];
            //映射灰度图数据到颜色表
            for (int i = 0; i < result.x_count * result.y_count; i++)
            {
                if (result.statisticInfo.maxIntensityValue - result.statisticInfo.minIntensityValue == 0)
                {
                    grayscale_8bit[i] = (byte)((grayData[i] - result.statisticInfo.minIntensityValue) * 255 / (result.statisticInfo.maxIntensityValue - result.statisticInfo.minIntensityValue + 1));
                }
                else
                {
                    grayscale_8bit[i] = (byte)((grayData[i] - result.statisticInfo.minIntensityValue) * 255 / (result.statisticInfo.maxIntensityValue - result.statisticInfo.minIntensityValue));
                }
            }

            var grayBitmap = LCF_Util.getInstance().ToGrayBitmap(grayscale_8bit, (int)result.x_count, (int)result.y_count);
            var heigthBitmap = LCF_Util.getInstance().ToBMPmap(heightData, (int)result.x_count, (int)result.y_count, result.statisticInfo.maxHeightValue[0] / 10, result.statisticInfo.minHeightValue[0] / 10);

            ///TODO: 图片缩放
            float x_interval;

            //获取X轴点间隔
            var ret = LCFDevice.LCF_GetFloatParameter(ControlerHandle, LCF_ParameterDefine.PARAM_X_INTERVAL, out x_interval);
            //调整图像高度比例
            double ratioH = (Config.YInterval * Config.EncoderDivision) / x_interval;
            bool flagHeight = LCF_Util.Zoom(heigthBitmap, 1.0, ratioH, out var HeightBmp, LCF_Util.ZoomType.NearestNeighborInterpolation);
            bool flagGray = LCF_Util.Zoom(grayBitmap, 1.0, ratioH, out var GaryBmp, LCF_Util.ZoomType.NearestNeighborInterpolation);

            var writeTime = DateTime.Now;
            if (flagHeight)
            {
                HeightBmp.Save($"{writeTime:yyyyMMddHHmmss}_Height.tiff", ImageFormat.Tiff);
            }
            if (flagGray)
            {
                GaryBmp.Save($"{writeTime:yyyyMMddHHmmss}_Gray.tiff", ImageFormat.Tiff);
            }

            //GenerateCSVData(result, grayscale_8bit, heigthFloatData, writeTime);
        }
        #endregion

        /// <summary>
        /// 获取测量参数
        /// </summary>
        public void GetMeasureParam()
        {
            if (!ExecuteCustomCommand(() =>
            {
                var rtn = LCFDevice.LCF_GetDeviceSetting(ControlerHandle, out deviceSetting);
                if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed) return null;

                return rtn;
            })) return;

            //测量参数
            CmosConfig.XDim = deviceSetting.resolution_x_raw;
            CmosConfig.ZDim = deviceSetting.resolution_y_raw;
            CmosConfig.XPos = deviceSetting.pos_x_raw;
            CmosConfig.ZPos = deviceSetting.pos_y_raw;
            CmosConfig.XInterval = deviceSetting.x_point_interval;
            CmosConfig.NearRange = deviceSetting.measureRange_near;
            CmosConfig.FarRange = deviceSetting.measureRange_far;

            object zAxisRange;
            GetSingleParam("float", LCF_ParameterDefine.PARAM_Z_RANGE, out zAxisRange);
            if (zAxisRange != null)
            {
                CmosConfig.ZAxisRange = (float)zAxisRange;
            }
        }

        /// <summary>
        /// 获取传感器所有设置信息
        /// </summary>
        public void GetSensorParam()
        {
            //连接后自动获取设备数据
            if (!ExecuteCustomCommand(() =>
            {
                var rtn = LCFDevice.LCF_GetDeviceSetting(ControlerHandle, out deviceSetting);
                if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed) return null;

                return rtn;
            })) return;

            ////获取设备信息
            //object controlerFWVersion = "";
            //GetSingleParam("int", LCF_ParameterDefine.PARAM_CONTROLER_FW_VERSION, out controlerFWVersion);
            //object sensorfwVersion = "";
            //GetSingleParam("int", LCF_ParameterDefine.PARAM_SENSOR_FW_VERSION, out sensorfwVersion);
            //object sdkVersion = "";
            //GetSingleParam("int", LCF_ParameterDefine.PARAM_SDK_VERSION, out sdkVersion);
            //object controler_SN = "";
            //GetSingleParam("int", LCF_ParameterDefine.PARAM_SENSOR_SN, out controler_SN);

            //获取当前X轴分辨率(X轴总点数)
            Fx = deviceSetting.resolution_x_raw;
            if (Fx == 0)
            {
                Fx_Buf = new float?[LCFDevice.MAX_DETECT_LAYER_NUMBER][];
                for (int i = 0; i < LCFDevice.MAX_DETECT_LAYER_NUMBER; i++)
                {
                    Fx_Buf[i] = new float?[2048];
                }
            }
            else
            {
                Fx_Buf = new float?[LCFDevice.MAX_DETECT_LAYER_NUMBER][];
                for (int i = 0; i < LCFDevice.MAX_DETECT_LAYER_NUMBER; i++)
                {
                    Fx_Buf[i] = new float?[Fx];
                }
            }

            //获取Y轴分辨率(光斑信号总点数)
            signal_len = deviceSetting.resolution_y_raw;
            Signal_Buf = new short[signal_len];

            //由分辨率控制拖动范围
            ContourConfig.SignalControlMaximum = Fx;
            ContourConfig.NUDSignalControlMaximum = Fx;

            //显示获取的数据
            CmosConfig.ExposureTime = deviceSetting.exposureTime;
            //CmosConfig.Intensity = (int)deviceSetting.lightIntensity * 10;
            CmosConfig.Intensity = (int)deviceSetting.lightIntensity;
            TriggerConfig.IsFrameRateControl = Convert.ToBoolean(deviceSetting.frameRateControl);
            TriggerConfig.InputModelIndex = (InputModel)(deviceSetting.encoderInputMode - 1);
            TriggerConfig.DetectFrameRate = deviceSetting.frameRate;
            TriggerConfig.NUDEncoderDivision = deviceSetting.encoderDivision;
            CmosConfig.HdrModelIndex = (HdrModel)deviceSetting.HDR_Num - 1;
            CmosConfig.XSubsampleIndex = (XSubsample)deviceSetting.x_subsample;
            CmosConfig.XRangeIndex = (XRange)deviceSetting.x_range;
            //导致采图异常点
            CmosConfig.BinningIndex = (Binning)deviceSetting.binningMode;
            CmosConfig.HdrSatRemoveLen = deviceSetting.HDR_SaturationRemoveLen;
            CmosConfig.ZSubsampleIndex = (ZSubsample)deviceSetting.z_subsample;

            //获取测量相关参数
            GetMeasureParam();

            if (deviceSetting.HDR_Num == 1)
            {
                if (!SetSingleParam("int", LCF_ParameterDefine.PARAM_COUNTOUR_LINE_THRESHOLD, 32)) return;
            }
            else
            {
                if (!SetSingleParam("int", LCF_ParameterDefine.PARAM_COUNTOUR_LINE_THRESHOLD, 1)) return;
            }

            if (deviceSetting.cameraGain == 1)
            {
                CmosConfig.GainIndex = Gain.GAIN_1;
            }
            if (deviceSetting.cameraGain == 2)
            {
                CmosConfig.GainIndex = Gain.GAIN_2;
            }
            if (deviceSetting.cameraGain == 4)
            {
                CmosConfig.GainIndex = Gain.GAIN_4;
            }

            //获取HDR设置
            float[] intenSity = new float[(_hdrindex + 1) * 8];
            intenSity = deviceSetting.HDR_LightIntensity;

            for (int j = 0; j < _hdrindex + 1; j++)
            {
                intenSity_value[j] = intenSity[_hdrindex * 8 + j].ToString();
            }
            setIntenSity();

            if (deviceSetting.triggerMode == 0)
            {
                TriggerConfig.TriggerModelIndex = 0;
                EnTrig_flag = false;
                Encoder_enable();
            }
            if (deviceSetting.triggerMode == 1)
            {
                TriggerConfig.TriggerModelIndex = (TriggerModel)1;
                EnTrig_flag = true;
                Encoder_enable();
            }

            if (!ExecuteCustomCommand(() =>
            {
                var rtn = LCFDevice.LCF_ReadSensorHeadFactoryPara(ControlerHandle, out sensorHeadFactoryPara);
                if (rtn != LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    return null;
                }
                //获取传感器量程,设置取值范围
                int Sensor_range = (sensorHeadFactoryPara.measureRangeMax - sensorHeadFactoryPara.measureRangeMin) / 2 * 100;
                CmosConfig.FarRangeMinimum = (int)(Sensor_range / (-1) * 1.1);
                CmosConfig.FarRangeMaximum = (int)(Sensor_range * 1.1);
                CmosConfig.NearRangeMinimum = (int)(Sensor_range / (-1) * 1.1);
                CmosConfig.NearRangeMaximum = (int)(Sensor_range * 1.1);

                return rtn;
            })) return;
        }

        /// <summary>
        /// 八个文本框的使能和文本显示
        /// </summary>
        public void setIntenSity()
        {
            CmosConfig.IsHdrIntensity1 = intenSity_able[0];
            CmosConfig.IsHdrIntensity2 = intenSity_able[1];
            CmosConfig.IsHdrIntensity3 = intenSity_able[2];
            CmosConfig.IsHdrIntensity4 = intenSity_able[3];
            CmosConfig.IsHdrIntensity5 = intenSity_able[4];
            CmosConfig.IsHdrIntensity6 = intenSity_able[5];
            CmosConfig.IsHdrIntensity7 = intenSity_able[6];
            CmosConfig.IsHdrIntensity8 = intenSity_able[7];

            int cHdrIntensity1;
            int cHdrIntensity2;
            int cHdrIntensity3;
            int cHdrIntensity4;
            int cHdrIntensity5;
            int cHdrIntensity6;
            int cHdrIntensity7;
            int cHdrIntensity8;

            int.TryParse(intenSity_value[0], out cHdrIntensity1);
            int.TryParse(intenSity_value[1], out cHdrIntensity2);
            int.TryParse(intenSity_value[2], out cHdrIntensity3);
            int.TryParse(intenSity_value[3], out cHdrIntensity4);
            int.TryParse(intenSity_value[4], out cHdrIntensity5);
            int.TryParse(intenSity_value[5], out cHdrIntensity6);
            int.TryParse(intenSity_value[6], out cHdrIntensity7);
            int.TryParse(intenSity_value[7], out cHdrIntensity8);

            CmosConfig.HdrIntensity1 = cHdrIntensity1;
            CmosConfig.HdrIntensity2 = cHdrIntensity2;
            CmosConfig.HdrIntensity3 = cHdrIntensity3;
            CmosConfig.HdrIntensity4 = cHdrIntensity4;
            CmosConfig.HdrIntensity5 = cHdrIntensity5;
            CmosConfig.HdrIntensity6 = cHdrIntensity6;
            CmosConfig.HdrIntensity7 = cHdrIntensity7;
            CmosConfig.HdrIntensity8 = cHdrIntensity8;
        }

        /// <summary>
        /// 触发方式参数设置使能
        /// </summary>
        public void Encoder_enable()
        {
            if (EnTrig_flag)
            {
                TriggerConfig.IsInputModel = true;
                TriggerConfig.IsNUDEncoderDivision = true;
                IsClearCacheData = true;
                IsExportCacheData = true;
            }
            else if (!EnTrig_flag)
            {
                TriggerConfig.IsInputModel = false;
                TriggerConfig.IsNUDEncoderDivision = false;
            }

            if (EnTrig_flag)
            {
                IsClearCacheData = true;
            }
        }

        public void ConvertDataToImage()
        {
            //#region 数据转换为图片
            ////将数据转换为图像信息
            //Bitmap HeightBmp;
            //Bitmap GaryBmp;
            //Gray_pic.Image = Util.getInstance().ToGrayBitmap(grayscale_8bit, ShareObjects.width, ShareObjects.height);
            //Height_pic.Image = Util.getInstance().ToBMPmap(Height_Buf, (int)(ShareObjects.width), (int)(ShareObjects.height), heightMax, heightMin);

            ////调整图像高度比例
            //double ratioH = (y_interval * encoderDivision) / x_interval;
            //bool flag = Util.Zoom((Bitmap)Height_pic.Image, 1.0, ratioH, out HeightBmp, Util.ZoomType.NearestNeighborInterpolation);
            //if (!flag)
            //{
            //    showInfo("深度图转换出现错误，请重试\r\n");
            //}
            //Height_pic.Image = HeightBmp;
            //flag = Util.Zoom((Bitmap)Gray_pic.Image, 1.0, ratioH, out GaryBmp, Util.ZoomType.NearestNeighborInterpolation);
            //if (!flag)
            //{
            //    showInfo("灰度图转换出现错误，请重试\r\n");
            //}
            //Gray_pic.Image = GaryBmp;

            ////自适应图片大小  
            //Gray_pic.SizeMode = PictureBoxSizeMode.Zoom;
            //Height_pic.SizeMode = PictureBoxSizeMode.Zoom;
            //Size size = Height_pic.Image.Size;
            ////label_Resolution.Text += ShareObjects.width.ToString() + " x " +ShareObjects.height.ToString();
            //label_Resolution.Text += size.Width.ToString() + " x " + size.Height.ToString();
            //showInfo("导出成功\r\n");
            //#endregion
        }

        #endregion

        #region Discard
        private void GenerateCSVData(LCF_Result_t result, byte[] grayData, float[] heigthData, DateTime writeData)
        {
            var maxData = grayData.Max();
            var firstMaxIndex = grayData.FirstIndexOf(m => m == maxData);
            var maxX = Convert.ToUInt16(firstMaxIndex % result.y_count);
            var maxY = Convert.ToUInt16(firstMaxIndex / result.x_count);

            var minData = grayData.Min();
            var firstMinIndex = grayData.FirstIndexOf(m => m == minData);
            var minX = Convert.ToUInt16(firstMinIndex % result.y_count);
            var minY = Convert.ToUInt16(firstMinIndex / result.x_count);

            #region 灰度图
            StringBuilder sbGray = new StringBuilder();
            var headerStr = GenerateHeader(result, true, Tuple.Create(Convert.ToSingle(maxData), maxX, maxY), Tuple.Create(Convert.ToSingle(maxData), maxX, maxY));
            sbGray.AppendLine(headerStr);

            sbGray.Append("Trigger Count,");
            for (int i = 0; i < result.x_count; i++)
            {
                sbGray.Append($"Gray{i},");
            }

            sbGray.Append("\r\n");
            #endregion

            #region 高度图
            StringBuilder sbHeight = new StringBuilder();
            sbHeight.AppendLine(GenerateHeader(result, false, Tuple.Create(Convert.ToSingle(heigthData.Min()), maxX, maxY), Tuple.Create(Convert.ToSingle(heigthData.Max()), maxX, maxY)));
            sbHeight.Append("Trigger Count,");
            for (int i = 0; i < result.x_count; i++)
            {
                sbHeight.Append($"Height{i},");
            }

            sbHeight.Append("\r\n");
            #endregion

            List<string> tempData = new List<string>();
            for (int i = 0; result.y_count > 0; i++)
            {
                sbGray.Append((i * Config.EncoderDivision).ToString());
                for (int j = 0; j < result.x_count; j++)
                {
                    sbGray.Append($",{grayData[i * result.x_count + j]}");
                }
                sbGray.Append("\r\n");

                sbHeight.Append((i * Config.EncoderDivision).ToString());
                for (int j = 0; j < result.x_count; j++)
                {
                    sbHeight.Append($",{heigthData[i * result.x_count + j]}");
                }
                sbHeight.Append("\r\n");
            }


            using (StreamWriter writer = new StreamWriter($"{writeData:yyyyMMddHHmmss}_Height.csv", false))
            {
                writer.WriteAsync(sbHeight.ToString());
            }
            using (StreamWriter writer = new StreamWriter($"{writeData:yyyyMMddHHmmss}_Gray.csv", false))
            {
                writer.WriteAsync(sbGray.ToString());
            }
        }

        private string GenerateHeader(LCF_Result_t result, bool isGray, Tuple<float, ushort, ushort> min, Tuple<float, ushort, ushort> max)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("csv");
            sb.AppendLine($"format,{(isGray ? "gray" : "height")}");
            sb.AppendLine($"author,reechi");
            sb.AppendLine($"y/x scale,{result.y_count / result.x_count}");
            sb.AppendLine($"x point interval,{Config.XInterval}");
            sb.AppendLine($"y line interval,{Config.YInterval}");
            sb.AppendLine($"trigger count interval,{Config.EncoderDivision}");
            sb.AppendLine($"width,{result.x_count}");
            sb.AppendLine($"height,{result.y_count}");
            sb.AppendLine($"min,{min.Item1},min_x,{min.Item2},min_y,{min.Item3},max,{max.Item1},max_x,{max.Item2},max_y,{max.Item3}");
            sb.AppendLine("end_header");

            return sb.ToString();
        }

        /// <summary>
        /// 采集数据
        /// </summary>
        /// <returns></returns>
        private Tuple<List<float[]>, List<float[]>> ExportData()
        {
            unsafe
            {
                //导出控制器缓存数据
                LCF_Result_t result;
                LCF_StatusTypeDef ret = LCFDevice.LCF_ExportCacheData(ControlerHandle, out result);
                if (ret != LCF_StatusTypeDef.LCF_Status_Succeed)
                {
                    return default;
                }

                int[] Height_Buf = new int[result.x_count * result.y_count];
                short[] Gray_Buf = new short[result.x_count * result.y_count];
                //将数据从非托管内存中拷贝出来
                Marshal.Copy(result.height, Height_Buf, 0, (int)(result.x_count * result.y_count));
                Marshal.Copy(result.intensity, Gray_Buf, 0, (int)(result.x_count * result.y_count));

                float[] height_FloatBuf = new float[result.x_count * result.y_count];
                //除以10等于实际测量数值，为了方便显示这里只保留整数部分
                for (int i = 0; i < result.x_count * result.y_count; i++)
                {
                    height_FloatBuf[i] = Height_Buf[i] / 10.0f;
                }

                List<float[]> tempOut = new List<float[]>();
                float minValue = float.MaxValue;
                float maxValue = float.MinValue;
                for (int i = 0; i < result.y_count; i++)
                {
                    List<float> tempInner = new List<float>();
                    for (int j = 0; j < result.x_count; j++)
                    {
                        if (height_FloatBuf[i * result.x_count + j] == LCFDevice.INVALID_VALUE)
                        {
                            //tempInner.Add(float.NaN);
                            tempInner.Add(888888.0f);
                        }
                        else
                        {

                            tempInner.Add(height_FloatBuf[i * result.x_count + j]);
                        }
                    }
                    var tempMax = tempInner.Max();
                    var tempMin = tempInner.Min();
                    if (minValue > tempMin)
                    {
                        minValue = tempMin;
                    }
                    if (maxValue < tempMax)
                    {
                        maxValue = tempMax;
                    }
                    tempOut.Add(tempInner.ToArray());
                }

                List<float[]> tempGrayOut = new List<float[]>();
                for (int i = 0; i < result.y_count; i++)
                {
                    List<float> tempGrayInner = new List<float>();
                    for (int j = 0; j < result.x_count; j++)
                    {
                        tempGrayInner.Add(Gray_Buf[i * result.x_count + j]);
                    }
                    tempGrayOut.Add(tempGrayInner.ToArray());
                }

                ret = LCFDevice.LCF_ClearCacheData(ControlerHandle);
                //清除编码器计数值
                ret = LCFDevice.LCF_ClearEncoderTriggerCount(ControlerHandle);
                //_tempSensorData.AddLast(Tuple.Create(Height_Buf, Gray_Buf, result));
                return Tuple.Create(tempOut, tempGrayOut);
            }
        }
        #endregion
    }

    #region Param

    /// <summary>
    /// 海伯森传感器配置
    /// </summary>
    [Serializable]
    public class HypersenSensorConfig : BindableBase
    {
        [JsonIgnore]
        private string _ip;
        /// <summary>
        /// IP
        /// </summary>
        public string IP
        {
            get { return _ip; }
            set { _ip = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ushort _port;
        /// <summary>
        /// 端口
        /// </summary>
        public ushort Port
        {
            get { return _port; }
            set { _port = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _exposureTime;
        /// <summary>
        /// 曝光时间
        /// </summary>
        public int ExposureTime
        {
            get { return _exposureTime; }
            set { _exposureTime = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private float _xInterval;
        /// <summary>
        /// X脉冲间距
        /// </summary>
        public float XInterval
        {
            get { return _xInterval; }
            set { _xInterval = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private float _yInterval = 5;
        /// <summary>
        /// Y脉冲间距
        /// </summary>
        public float YInterval
        {
            get { return _yInterval; }
            set { _yInterval = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _encoderDivision;
        /// <summary>
        /// 设备分频值
        /// </summary>
        public int EncoderDivision
        {
            get { return _encoderDivision; }
            set { _encoderDivision = value; RaisePropertyChanged(); }
        }
    }

    /// <summary>
    /// CMOS配置参数
    /// </summary>
    public class CMOSConfig : BindableBase
    {
        [JsonIgnore]
        private int _exposureTime;

        public int ExposureTime
        {
            get { return _exposureTime; }
            set { _exposureTime = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _intensity;

        public int Intensity
        {
            get { return _intensity; }
            set { _intensity = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _intensityPercentage = "";

        public string IntensityPercentage
        {
            get { return _intensityPercentage; }
            set { _intensityPercentage = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Gain _gainIndex;

        public Gain GainIndex
        {
            get { return _gainIndex; }
            set { _gainIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private HdrModel _hdrModelIndex;

        public HdrModel HdrModelIndex
        {
            get { return _hdrModelIndex; }
            set { _hdrModelIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrSatRemoveLen;

        public int HdrSatRemoveLen
        {
            get { return _hdrSatRemoveLen; }
            set { _hdrSatRemoveLen = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrSatRemoveLen;

        public bool IsHdrSatRemoveLen
        {
            get { return _isHdrSatRemoveLen; }
            set { _isHdrSatRemoveLen = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrIntensity1;

        public int HdrIntensity1
        {
            get { return _hdrIntensity1; }
            set { _hdrIntensity1 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrIntensity2;

        public int HdrIntensity2
        {
            get { return _hdrIntensity2; }
            set { _hdrIntensity2 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrIntensity3;

        public int HdrIntensity3
        {
            get { return _hdrIntensity3; }
            set { _hdrIntensity3 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrIntensity4;

        public int HdrIntensity4
        {
            get { return _hdrIntensity4; }
            set { _hdrIntensity4 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrIntensity5;

        public int HdrIntensity5
        {
            get { return _hdrIntensity5; }
            set { _hdrIntensity5 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrIntensity6;

        public int HdrIntensity6
        {
            get { return _hdrIntensity6; }
            set { _hdrIntensity6 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrIntensity7;

        public int HdrIntensity7
        {
            get { return _hdrIntensity7; }
            set { _hdrIntensity7 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _hdrIntensity8;
        public int HdrIntensity8
        {
            get { return _hdrIntensity8; }
            set { _hdrIntensity8 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrIntensity1;
        public bool IsHdrIntensity1
        {
            get { return _isHdrIntensity1; }
            set { _isHdrIntensity1 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrIntensity2;
        public bool IsHdrIntensity2
        {
            get { return _isHdrIntensity2; }
            set { _isHdrIntensity2 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrIntensity3;
        public bool IsHdrIntensity3
        {
            get { return _isHdrIntensity3; }
            set { _isHdrIntensity3 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrIntensity4;

        public bool IsHdrIntensity4
        {
            get { return _isHdrIntensity4; }
            set { _isHdrIntensity4 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrIntensity5;

        public bool IsHdrIntensity5
        {
            get { return _isHdrIntensity5; }
            set { _isHdrIntensity5 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrIntensity6;

        public bool IsHdrIntensity6
        {
            get { return _isHdrIntensity6; }
            set { _isHdrIntensity6 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrIntensity7;

        public bool IsHdrIntensity7
        {
            get { return _isHdrIntensity7; }
            set { _isHdrIntensity7 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isHdrIntensity8;

        public bool IsHdrIntensity8
        {
            get { return _isHdrIntensity8; }
            set { _isHdrIntensity8 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isInputeHdrModel;

        public bool IsInputeHdrModel
        {
            get { return _isInputeHdrModel; }
            set { _isInputeHdrModel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private XRange _xRangeIndex;
        public XRange XRangeIndex
        {
            get { return _xRangeIndex; }
            set { _xRangeIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private XSubsample _xSubsampleIndex;
        public XSubsample XSubsampleIndex
        {
            get { return _xSubsampleIndex; }
            set { _xSubsampleIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private float _xInterval;
        public float XInterval
        {
            get { return _xInterval; }
            set { _xInterval = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _xPos;
        public int XPos
        {
            get { return _xPos; }
            set { _xPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _xDim;
        public int XDim
        {
            get { return _xDim; }
            set { _xDim = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _farRange;
        public int FarRange
        {
            get { return _farRange; }
            set { _farRange = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _farRangeMaximum;
        public int FarRangeMaximum
        {
            get { return _farRangeMaximum; }
            set { _farRangeMaximum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _farRangeMinimum;
        public int FarRangeMinimum
        {
            get { return _farRangeMinimum; }
            set { _farRangeMinimum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Binning _binningIndex;
        public Binning BinningIndex
        {
            get { return _binningIndex; }
            set { _binningIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ZSubsample _zSubsampleIndex;
        public ZSubsample ZSubsampleIndex
        {
            get { return _zSubsampleIndex; }
            set { _zSubsampleIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private float _zAxisRange;
        public float ZAxisRange
        {
            get { return _zAxisRange; }
            set { _zAxisRange = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _zPos;
        public int ZPos
        {
            get { return _zPos; }
            set { _zPos = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _zDim;
        public int ZDim
        {
            get { return _zDim; }
            set { _zDim = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _nearRange;
        public int NearRange
        {
            get { return _nearRange; }
            set { _nearRange = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _nearRangeMaximum;
        public int NearRangeMaximum
        {
            get { return _nearRangeMaximum; }
            set { _nearRangeMaximum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _nearRangeMinimum;
        public int NearRangeMinimum
        {
            get { return _nearRangeMinimum; }
            set { _nearRangeMinimum = value; RaisePropertyChanged(); }
        }

    }

    /// <summary>
    /// 触发设定配置参数
    /// </summary>
    public class TriggerConfig : BindableBase
    {
        [JsonIgnore]
        private TriggerModel _triggerModelIndex;
        public TriggerModel TriggerModelIndex
        {
            get { return _triggerModelIndex; }
            set { _triggerModelIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isFrameRateControl;
        public bool IsFrameRateControl
        {
            get { return _isFrameRateControl; }
            set { _isFrameRateControl = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _detectFrameRate;
        public int DetectFrameRate
        {
            get { return _detectFrameRate; }
            set { _detectFrameRate = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isDetectFrameRate;
        public bool IsDetectFrameRate
        {
            get { return _isDetectFrameRate; }
            set { _isDetectFrameRate = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _maxFrameRate = "";
        public string MaxFrameRate
        {
            get { return _maxFrameRate; }
            set { _maxFrameRate = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private AdjustRoiForFpsCo _adjustRoiForFpsIndex;
        public AdjustRoiForFpsCo AdjustRoiForFpsIndex
        {
            get { return _adjustRoiForFpsIndex; }
            set { _adjustRoiForFpsIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private InputModel _inputModelIndex;
        public InputModel InputModelIndex
        {
            get { return _inputModelIndex; }
            set { _inputModelIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isInputModel;
        public bool IsInputModel
        {
            get { return _isInputModel; }
            set { _isInputModel = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _nUDEncoderDivision;
        public int NUDEncoderDivision
        {
            get { return _nUDEncoderDivision; }
            set { _nUDEncoderDivision = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isNUDEncoderDivision;
        public bool IsNUDEncoderDivision
        {
            get { return _isNUDEncoderDivision; }
            set { _isNUDEncoderDivision = value; RaisePropertyChanged(); }
        }

    }

    /// <summary>
    /// 轮廓显示配置参数
    /// </summary>
    public class ContourConfig : BindableBase
    {
        [JsonIgnore]
        private int _signalControl;
        public int SignalControl
        {
            get { return _signalControl; }
            set { _signalControl = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _signalControlMaximum;
        public int SignalControlMaximum
        {
            get { return _signalControlMaximum; }
            set { _signalControlMaximum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _nUDSignalControl;
        public int NUDSignalControl
        {
            get { return _nUDSignalControl; }
            set { _nUDSignalControl = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _nUDSignalControlMaximum;
        public int NUDSignalControlMaximum
        {
            get { return _nUDSignalControlMaximum; }
            set { _nUDSignalControlMaximum = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Layer _layerIndex;
        public Layer LayerIndex
        {
            get { return _layerIndex; }
            set { _layerIndex = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isLayer;
        public bool IsLayer
        {
            get { return _isLayer; }
            set { _isLayer = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _outlinesX;
        public string OutlinesX
        {
            get { return _outlinesX; }
            set { _outlinesX = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _outlinesL1;
        public string OutlinesL1
        {
            get { return _outlinesL1; }
            set { _outlinesL1 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _outlinesL2;
        public string OutlinesL2
        {
            get { return _outlinesL2; }
            set { _outlinesL2 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _outlinesL3;
        public string OutlinesL3
        {
            get { return _outlinesL3; }
            set { _outlinesL3 = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _outlinesL4;
        public string OutlinesL4
        {
            get { return _outlinesL4; }
            set { _outlinesL4 = value; RaisePropertyChanged(); }
        }

    }
    #endregion

    public class FixLengthList<T> : LinkedList<T>
    {
        private int _listLength;
        private object lockkey = new object();

        public int Limit { get { return _listLength; } }

        public FixLengthList(int listLength)
        {
            _listLength = listLength;
        }

        public void UpdateLength(int length)
        {
            lock (lockkey)
            {
                if (this.Count > length)
                {
                    for (int i = 0; i < this.Count - length; i++)
                    {
                        Remove(this.First());
                    }

                }
                _listLength = length;
            }

        }

        public new void AddLast(T value)
        {
            SafeAddLast(value);
        }

        public void SafeAddLast(T item)
        {
            lock (lockkey)
            {
                base.AddLast(item);
                if (Count > _listLength)
                {
                    Remove(this.First());
                }
            }
        }

        public void SafeAddFirst(T item)
        {
            lock (lockkey)
            {
                base.AddFirst(item);
                if (Count > _listLength)
                {
                    Remove(this.Last());
                }
            }
        }

        public List<T> GetListSafe()
        {
            lock (lockkey)
            {
                return this.ToList();
            }
        }

        public List<T> GetSubListSafe(int startIndex)
        {
            lock (lockkey)
            {
                if (Count < startIndex)
                {
                    return null;
                }
                else if (Count == startIndex)
                {
                    return new List<T>();
                }
                else
                {
                    return this.Skip(startIndex).ToList();
                }

            }
        }

        public T Pop()
        {
            lock (lockkey)
            {
                if (base.Count < 1)
                {
                    return default(T);
                }
                else
                {
                    T item = SafeFirst();
                    lock (lockkey)
                    {
                        RemoveFirst();
                    }
                    return item;
                }
            }
        }

        public List<T> Pop(int getCount)
        {
            lock (lockkey)
            {
                if (getCount > Count)
                {
                    getCount = Count;
                }

                var targetElements = this.Take(getCount).ToList();
                targetElements.ToList().ForEach(m => Remove(m));
                return targetElements;
            }
        }

        public T SafeFirst()
        {
            T result = default(T);
            lock (lockkey)
            {
                result = this.First();
            }
            return result;
        }

        public T SafeLast()
        {
            T result = default(T);
            lock (lockkey)
            {
                result = this.Last();
            }
            return result;
        }

        public void SafeClear()
        {
            lock (lockkey)
            {
                base.Clear();
            }
        }

        public new void Clear()
        {
            SafeClear();
        }
    }

    public static class EnumerableExtensions
    {
        #region Dict Extensions
        public static object? GetValueOrNull<TKey, TValue>
        (this IDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : null;
        }

        public static TValue GetValueOrDefaultEx<TKey, TValue>
        (this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static TValue GetValueOrDefaultEx<TKey, TValue>
        (this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TValue> defaultValueProvider, bool cache = true)
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = defaultValueProvider();

                if (cache)
                {
                    dictionary.Add(key, value);
                }
            }
            return value;
        }

        public static TValue GetObjPara<TKey, TValue>
        (this IDictionary<TKey, object> dictionary,
            TKey key, TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out var obj) ? (TValue)obj : defaultValue;
        }

        public static TValue GetObjPara<TValue>
        (this IDictionary<string, object> dictionary,
            string key, TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out var obj) ? (TValue)obj : defaultValue;
        }

        public static bool TryGetObjPara<TKey, TValue>
        (this IDictionary<TKey, object> dictionary,
            TKey key, out TValue value,
            TValue defaultValue)
        {
            var got = dictionary.TryGetValue(key, out var obj);
            value = got ? (TValue)obj : defaultValue;
            return got;
        }

        public static bool TryGetObjPara<TValue>
        (this IDictionary<string, object> dictionary,
            string key, out TValue value,
            TValue defaultValue)
        {
            var got = dictionary.TryGetValue(key, out var obj);
            value = got ? (TValue)obj : defaultValue;
            return got;
        }

        #endregion

        #region Enumerable Ext

        public static void RemoveItemsMatch<T>(this Collection<T> source, Func<T, bool> matcher)
        {
            int checkedCount = 0, all = source.Count;
            while (checkedCount < all)
            {
                var found = source.FirstIndexOf(matcher);
                if (found != -1) source.RemoveAt(found);
                checkedCount++;
            }
        }

        public static void RemoveItemsMatch<T>(this IList source, Func<T, bool> matcher)
        {
            int checkedCount = 0, all = source.Count;
            while (checkedCount < all)
            {
                var found = source.OfType<T>().FirstIndexOf(matcher);
                if (found != -1) source.RemoveAt(found);
                checkedCount++;
            }
        }

        public static int FirstIndexOf<T>(this IEnumerable<T> source, Func<T, bool> matcher)
        {
            int idx = 0;
            foreach (T item in source)
            {
                if (matcher(item)) return idx;
                idx++;
            }

            return -1;
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
        #endregion     
    }


}
