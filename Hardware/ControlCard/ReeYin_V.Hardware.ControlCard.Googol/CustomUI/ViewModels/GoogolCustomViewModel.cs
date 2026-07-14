using GoogolMotion.Models;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using static GTN.mc;
using static GTN.mc_ringnet;

namespace GoogolMotion.ViewModels
{
    public class GoogolCustomViewModel : DialogViewModelBase
    {
        #region Fields
        private IConfigManager ConfigManager { get; }
        private DispatcherTimer _timer;
        private DispatcherTimer _PSOTimer;
        private Task _localTask;

        private short _core = 1;
        /// <summary>
        /// 核2目前不支持位置比较功能，只能用核1
        /// </summary>
        public short core
        {
            get { return _core; }
            set { _core = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 
        /// </summary>
        Thread FIFO, FIFO2;

        private short fifo_alive = 0;

        /// <summary>
        /// GNM模块号
        /// </summary>
        private short station = 0;

        /// <summary>
        /// 硬件通道数量
        /// </summary>
        private short count = 4;

        // 定义映射关系（可在静态构造函数或初始化时创建）
        private static readonly Dictionary<ControlPower, byte> HsoPermitMap = new()
        {
            { ControlPower.激光开关, 0x02 },
            { ControlPower.激光PWM, 0x04 },
            { ControlPower.第一路位置比较, 0x08 },
            { ControlPower.第二路位置比较, 0x10 }
        };

        /// <summary>
        /// DMA功能的开关
        /// </summary>
        private int A;
        #endregion

        #region Properties
        private GoogolCustomModel _modelParam = new GoogolCustomModel();
        /// <summary>
        /// 模块参数
        /// </summary>
        public GoogolCustomModel ModelParam
        {
            get { return _modelParam; }
            set { _modelParam = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public GoogolCustomViewModel(IConfigManager configManager)
        {
            #region LoadConfig
            ConfigManager = configManager;
            ModelParam = ConfigManager.Read<GoogolCustomModel>(ConfigKey.GoogolModel) ?? new GoogolCustomModel();
            #endregion



            InitTimer();
            InitPSOTimer();
            _PSOTimer?.Start();
        }
        #endregion

        #region Methods

        private void InitTimer()
        {
            short rtn;

        }

        private void InitPSOTimer()
        {
            _PSOTimer = new DispatcherTimer();
            _PSOTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            _PSOTimer.Tick += (s, e) =>
            {
                short rtn = 0;
                //位置比较输出状态
                GTN.mc.TPosCompareStatus pStatus = new GTN.mc.TPosCompareStatus();
                GTN.mc.TPosCompareInfo pInfo = new GTN.mc.TPosCompareInfo();
                rtn = GTN.mc.GTN_PosCompareStatus(core, ModelParam.PosComparisonParam.PSOIndex, out pStatus);
                Console.WriteLine($"GTN_PosCompareStatus()_{rtn}");

                rtn = GTN.mc.GTN_PosCompareInfo(core, ModelParam.PosComparisonParam.PSOIndex, out pInfo);
                Console.WriteLine($"GTN_PosCompareInfo()_{rtn}");

                ModelParam.PSOOutputStatus.AddRange
                (
                    [
                        pStatus.run.ToString(),                             //1:开始，0:停止
                        pStatus.space.ToString(),                           //FIFO剩余空间
                        pStatus.pulseCount.ToString(),                      //输出脉冲个数
                        pStatus.mode.ToString(),                            //0:FIFO模式；1:Linear模式
                        pStatus.space.ToString(),                           //FIFO剩余空间
                        //pStatus.hso.ToString(),                           //位置比较输出hso通道的输出数值
                        //pStatus.gpo.ToString(),                           //通用GPO通道的输出数值
                        //pStatus.segmentNumber.ToString(),                 //执行的段号

                        //读取位置比较输出相关信息
                        //pInfo.config.ToString(),                            //保留
                        pInfo.fifoEmpty.ToString(),                         //fifo跑空标志，1:表示曾经跑空，清空FPGA后才能清0,其他位保留
                        //pInfo.head.ToString(),                            //保留
                        //pInfo.tail.ToString(),                            //保留
                        pInfo.commandReceive.ToString(),                    //接收指令数量
                        pInfo.commandSend.ToString(),                       //发送指令数量
                        pInfo.posX.ToString(),                              // 在FIFO模式下，表示用户触发的x轴位置；在PSO模式下，表示用户的开关PSO位置，其他模式下无意义
                        pInfo.posY.ToString(),                              // 在FIFO模式下，表示用户触发的y轴位置；在PSO模式下，表示用户的开关PSO位置，其他模式下无意义

                    ]
                );
            };
        }
        #endregion

        #region Commands
        /// <summary>
        /// 通用指令
        /// </summary>
        public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>((order) =>
        {
            switch (order)
            {
                case "关闭":
                    {
                        //存一下参数
                        ConfigManager.Write(ConfigKey.GoogolModel, ModelParam);

                        _PSOTimer.Stop();
                    }
                    break;

                default:
                    break;

            }
        });


        /// <summary>
        /// 位置比较功能
        /// </summary>
        public DelegateCommand<string> PSOCommand => new DelegateCommand<string>((order) =>
        {
            short rtn = 0;
            switch (order)
            {
                case "设置控制权":
                    {
                        //参数index，需要设置控制权的起始硬件通道。
                        //以402模块为例，有HSO和LASER两个接口,每个接口内部有两路5V差分输出。
                        //HSO接口内部：HSO2+-,HSO3+-
                        //LASER接口内：HSO0+-(LASER+-),HSO1+-(PWM+-)
                        //HSO0、HSO1、HSO2、HSO3（默认HSO0为激光开关，HSO1为PWM，HSO2为第一路位置比较输出，HSO3为第二路位置比较输出）
                        //对于402模块 index值：1-4对应HSO0-HSO3

                        //以403模块为例，有LASER一个接口,接口内部有两路5V差分输出。
                        //LASER接口内：HSO0+-(LASER+-),HSO1+-(PWM+-)
                        //HSO0、HSO1（默认HSO0为激光开关，HSO1为PWM）
                        //对于403模块 index值：1-2对应HSO0-HSO1

                        //参数permit：按位设置硬件输出通道信号输出的类型,
                        //bit0~bit15:按位表示对应信号类型输出， 0：无效， 1：有效。
                        //Bit0：通用GPO
                        //Bit1：第一路位置比较输出	0x02
                        //Bit2：第二路位置比较输出 0x04
                        //Bit3：激光输出	0x08
                        //Bit4： PWM 输出	0x10
                        //Bit5~bit15:对于 403 模块保留
                        //相当于为HSO0-3重新分配上述功能。

                        short[] permit = new short[4];//控制权

                        // 使用时直接查找字典
                        if (HsoPermitMap.TryGetValue(ModelParam.HSO0, out byte value))
                        {
                            permit[0] = value;
                        }
                        // 可选：处理未匹配的情况
                        else
                        {
                            // 例如：permit[0] = 0x00; 或抛出异常
                            Console.WriteLine($"未在字典中匹配到相关值");
                        }
                        // 使用时直接查找字典
                        if (HsoPermitMap.TryGetValue(ModelParam.HSO1, out byte value1))
                        {
                            permit[0] = value1;
                        }
                        // 可选：处理未匹配的情况
                        else
                        {
                            // 例如：permit[0] = 0x00; 或抛出异常
                            Console.WriteLine($"未在字典中匹配到相关值");
                        }
                        // 使用时直接查找字典
                        if (HsoPermitMap.TryGetValue(ModelParam.HSO2, out byte value2))
                        {
                            permit[0] = value2;
                        }
                        // 可选：处理未匹配的情况
                        else
                        {
                            // 例如：permit[0] = 0x00; 或抛出异常
                            Console.WriteLine($"未在字典中匹配到相关值");
                        }
                        // 使用时直接查找字典
                        if (HsoPermitMap.TryGetValue(ModelParam.HSO3, out byte value3))
                        {
                            permit[0] = value3;
                        }
                        // 可选：处理未匹配的情况
                        else
                        {
                            // 例如：permit[0] = 0x00; 或抛出异常
                            Console.WriteLine($"未在字典中匹配到相关值");
                        }

                        rtn = GTN.mc.GTN_SetTerminalPermitEx(core,
                            station,                                                                //模块号
                            GTN.mc.MC_HSO,                                                          //输出类型
                            ref permit[0],                                                          //设置软件控制权限
                            1,                                                                      //起始硬件通道号
                            count);                                                                 //硬件数量
                        Console.WriteLine($"GTN_SetTerminalPermitEx()_Result:{rtn}");
                    }
                    break;

                case "读取控制权":
                    {
                        short[] permit = new short[4];//控制权
                        rtn = GTN.mc.GTN_GetTerminalPermitEx(core, station, GTN.mc.MC_HSO, out permit[0], 1, count);
                        Console.WriteLine($"GTN_GetTerminalPermitEx()_Result:{rtn}");
                        ModelParam.ReadHSOControlPower.Clear();
                        for (int i = 0; i < permit.Length; i++)
                        {
                            ModelParam.ReadHSOControlPower.Add(permit[i].ToString());
                        }

                    }
                    break;

                case "设置GPO控制权":
                    {
                        //Key_PS：可以在已经开始位置比较在操作，
                        short permit = 2;
                        rtn = GTN.mc.GTN_SetTerminalPermitEx(core,
                            station,//模块号
                            GTN.mc.MC_GPO,//输出类型
                            ref permit,//设置软件控制权限
                            1,//起始硬件通道号
                            1);//硬件数量
                        Console.WriteLine($"GTN_SetTerminalPermitEx()_Result:{rtn}");
                    }
                    break;

                case "单次输出":
                    {
                        short permit = 0x2;//第一路位置比较输出功能
                        rtn = GTN.mc.GTN_SetTerminalPermitEx(core, station, GTN.mc.MC_HSO, ref permit, ModelParam.OnceHardChannel, 1);
                        Console.WriteLine($"GTN_SetTerminalPermitEx()_Result:{rtn}");

                        //Key_PS：在位置比较已经开始的情况下，操作会导致无法在进入位置比较
                        rtn = GTN.mc.GTN_PosComparePulse(core,
                            ModelParam.OncePSOIndex,                                                    //位置比较索引，[1,8]
                            0,                                                                          //输出模式，0：脉冲，1：电平
                            0,                                                                          //输出模式为脉冲，则该参数无效，输出脉冲为电平则该参数表示电平高低，0-低电平，1 - 高电平
                            ModelParam.OncePulseWidth);                                                 //输出模式为脉冲有效，表示输出脉冲的宽度，单位us
                        Console.WriteLine($"GTN_PosComparePulse()_Result:{rtn}");
                    }
                    break;

                case "开启位置比较输出":
                    {
                        //Key_PS：


                        //设置位置比较输出模式
                        GTN.mc.TPosCompareMode tPosCompareMode = new GTN.mc.TPosCompareMode()
                        {
                            mode = ModelParam.PosComparisonParam.CompareMode,                                   //位置比较输出模式：0 fifo，1 linear,2 PSO立即
                            dimension = ModelParam.PosComparisonParam.CompareDimension,                         //位置比较维数
                            sourceMode = ModelParam.PosComparisonParam.SourceMode,                              //位置比较源，0：编码器；1：脉冲计数器
                            sourceX = ModelParam.PosComparisonParam.Compare_X,                                  //X轴比较源轴号
                            sourceY = ModelParam.PosComparisonParam.Compare_Y,                                  //Y轴比较源轴号
                            outputMode = ModelParam.PosComparisonParam.CompareOutputMode,                       //输出类型0:脉冲 1:电平，2:电平自动
                            outputPulseWidth = ModelParam.PosComparisonParam.ComparePulseWidth,                 //输出脉冲宽度, 单位为 1us，
                            outputCounter = 1,                                                                  //保留，需要大于0
                            errorBand = ModelParam.PosComparisonParam.CompareErrBand,                           //二维位置比较输出误差带，单位pulse
                        };
                        rtn = GTN.mc.GTN_SetPosCompareMode(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosCompareMode);//设置位置比较输出模式
                        Console.WriteLine($"GTN_SetPosCompareMode()_Result:{rtn}");

                        rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                        Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");
                        //Z = GTN_PosCompareHsOff(core, psoIndex);
                        //commandHandler("GTN_PosCompareHsOff", Z);
                        //Z = GTN_PosCompareHsOn(core, psoIndex, 1, 200);
                        //commandHandler("GTN_PosCompareHsOn", Z);
                        //Z = GTN_SetPosCompareFifoMode(core, psoIndex, 1, 1);//设置存储实际位置比较点的缓冲区模式（大小 2048）。
                        //commandHandler("GTN_SetPosCompareFifoMode", Z);
                        int i = 0;

                        //设置位置比较输出数据并启动位置比较输出
                        if (ModelParam.PosComparisonParam.CompareMode == 0 && ModelParam.PosComparisonParam.CompareDimension == 1)//一维位置比较，FIFO模式
                        {
                            GTN.mc.TPosCompareData tPosData = new GTN.mc.TPosCompareData();
                            rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                            Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");
                            /*压数据时必须在位置比较输出关闭的时候*/
                            for (i = 0; i < 1000; i++)
                            {
                                tPosData.pos = 100 + 100 * i;

                                tPosData.gpo = 0x80;// 通用GPO通道的输出数值，按位表示GPO，bit0 - bit15分别对应GPO0 - GPO15。
                                                    //脉冲模式：0表示无输出，1表示输出脉冲；电平模式：0表示拉低，1表示拉高
                                tPosData.hso = 0xffff;//位置比较输出hso通道的输出数值,按位表示HSObit0-bit9对应HSO0 - HSO9。bit15: 表示逻辑位,在激光功能和位置比较输出功能复用时生效；
                                                      //脉冲模式：0表示无输出，1表示输出脉冲；电平模式：0表示无输出，1表示有输出。
                                tPosData.segmentNumber = (uint)(i + 1);
                                rtn = GTN.mc.GTN_PosCompareData(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData);
                                Console.WriteLine($"GTN_PosCompareData()_Result:{rtn}");
                            }
                            FIFO = new Thread(() =>
                            {
                                while (true)
                                {
                                    GTN.mc.TPosCompareStatus pStatus = new GTN.mc.TPosCompareStatus();
                                    rtn = GTN.mc.GTN_PosCompareStatus(core, ModelParam.PosComparisonParam.PSOIndex, out pStatus);
                                    Console.WriteLine($"GTN_PosCompareStatus()_Result:{rtn}");

                                    ModelParam.PosComparisonParam.Space = pStatus.space;

                                    while (ModelParam.PosComparisonParam.Space > 500)
                                    {
                                        for (i = 1000; i < 1500; i++)
                                        {
                                            tPosData.pos = 100 + 100 * i;

                                            tPosData.gpo = 0x80;// 通用GPO通道的输出数值，按位表示GPO，bit0 - bit15分别对应GPO0 - GPO15。
                                                                //脉冲模式：0表示无输出，1表示输出脉冲；电平模式：0表示拉低，1表示拉高
                                            tPosData.hso = 0xffff;//位置比较输出hso通道的输出数值,按位表示HSObit0-bit9对应HSO0 - HSO9。bit15: 表示逻辑位,在激光功能和位置比较输出功能复用时生效；
                                                                  //脉冲模式：0表示无输出，1表示输出脉冲；电平模式：0表示无输出，1表示有输出。
                                            tPosData.segmentNumber = (uint)(i + 1);

                                            ModelParam.PSOOutputStatus[3] = (i + 30).ToString();
                                            //textBox96.Invoke(new Action(() =>
                                            //{
                                            //    textBox96.Text = (i + 1).ToString();
                                            //}));
                                            rtn = GTN.mc.GTN_PosCompareData(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData);
                                            Console.WriteLine($"GTN_PosCompareData()_Result:{rtn}");
                                        }
                                    }
                                    //short Z1;
                                    //do
                                    //{
                                    //    Z1 = GTN.mc.GTN_PosCompareData(core, psoIndex, ref tPosData);
                                    //    commandHandler("GTN_PosCompareData", Z);
                                    //} while(0 != Z1);   
                                }

                            })
                            { IsBackground = true };
                            FIFO.Start();
                            fifo_alive = 1;
                        }
                        else if (ModelParam.PosComparisonParam.CompareMode == 0 && ModelParam.PosComparisonParam.CompareDimension == 2)//二维位置比较，FIFO模式
                        {
                            GTN.mc.TPosCompareData2D tPosData2D = new GTN.mc.TPosCompareData2D();
                            short CompareSpace;
                            FIFO = new Thread(() =>
                            {
                                while (true)
                                {
                                    for (i = 0; i < 3000; i++)
                                    {

                                        do
                                        {
                                            rtn = GTN.mc.GTN_PosCompareSpace(core, ModelParam.PosComparisonParam.PSOIndex, out CompareSpace);
                                            Console.WriteLine($"GTN_PosCompareSpace()_Result:{rtn}");
                                        } while (0 == CompareSpace);
                                        tPosData2D.posX = 1000 + 1000 * i;
                                        tPosData2D.posY = 1000 + 1000 * i;
                                        tPosData2D.segmentNumber = (uint)(i + 1);
                                        tPosData2D.gpo = 65535;//同一维FIFO模式
                                        tPosData2D.hso = 1;
                                        //textBox96.Invoke(new Action(() =>
                                        //{
                                        //    textBox96.Text = (i + 1).ToString();
                                        //}));

                                        rtn = GTN.mc.GTN_PosCompareData2D(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData2D);
                                        Console.WriteLine($"GTN_PosCompareData2D()_Result:{rtn}");
                                    }
                                    if (A == 1)
                                    {
                                        short Z1;
                                        do
                                        {
                                            Z1 = GTN.mc.GTN_PosCompareData2D(core, ModelParam.PosComparisonParam.PSOIndex, IntPtr.Zero);
                                            Console.WriteLine($"GTN_PosCompareData2D1()_Result:{rtn}");
                                        } while (Z1 != 0);
                                    }
                                }

                            })
                            { IsBackground = true };
                            FIFO.Start();
                            fifo_alive = 1;
                        }
                        else if (ModelParam.PosComparisonParam.CompareMode == 1 && ModelParam.PosComparisonParam.CompareDimension == 1)//一维位置比较，Linear模式
                        {
                            GTN.mc.TPosCompareLinear tPosCompareLinear = new GTN.mc.TPosCompareLinear()
                            {
                                startPos = 0,                                           //线性比较输出的起点位置
                                interval = 1000,                                        //位置比较输出位置间隔
                                count = 10,                                             //位置比较输出个数
                                hso = 8,                                                //位置比较输出hso通道的输出数值，按位表示
                                                                                        //脉冲模式：0 表示无输出，1 表示输出脉冲
                                gpo = 0x1,                                              //通用GPO通道的输出数值，按位表示
                                                                                        //脉冲模式：0 表示无输出，1 表示输出脉冲
                            };
                            rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                            Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");

                            rtn = GTN.mc.GTN_SetPosCompareLinear(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosCompareLinear);
                            Console.WriteLine($"GTN_SetPosCompareLinear()_Result:{rtn}");
                        }
                        else if (ModelParam.PosComparisonParam.CompareMode == 2)//PSO立即模式
                        {
                            GTN.mc.TPosComparePsoPrm tPosComparePsoPrm = new GTN.mc.TPosComparePsoPrm();
                            rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                            Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");

                            tPosComparePsoPrm.count = 1;//保留，使用时设置大于0的数
                            tPosComparePsoPrm.syncPos = ModelParam.PosComparisonParam.SyncPos;//输出间距，X、Y轴的合成间距。单位：Pulse
                            rtn = GTN.mc.GTN_SetPosComparePsoPrm(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosComparePsoPrm);
                            Console.WriteLine($"GTN_SetPosComparePsoPrm()_Result:{rtn}");
                        }
                        else if (ModelParam.PosComparisonParam.CompareMode == 3)//PSO等待到位模式
                        {
                            GTN.mc.TPosComparePsoPrm tPosComparePsoPrm = new GTN.mc.TPosComparePsoPrm();
                            rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                            Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");

                            tPosComparePsoPrm.count = 1;//保留，使用时设置大于0的数
                            tPosComparePsoPrm.syncPos = ModelParam.PosComparisonParam.SyncPos;//输出间距，X、Y轴的合成间距。单位：Pulse
                            rtn = GTN.mc.GTN_SetPosComparePsoPrm(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosComparePsoPrm);
                            Console.WriteLine($"GTN_SetPosComparePsoPrm()_Result:{rtn}");
                        }

                        rtn = GTN.mc.GTN_PosCompareStart(core, ModelParam.PosComparisonParam.PSOIndex);
                        Console.WriteLine($"GTN_PosCompareStart()_Result:{rtn}");
                    }
                    break;
                case "关闭位置比较输出":
                    {

                        if (fifo_alive == 1)
                        {
                            FIFO.Abort();
                            FIFO2.Abort();
                            rtn = GTN.mc.GTN_PosCompareStop(core, ModelParam.PosComparisonParam.PSOIndex);
                            Console.WriteLine($"GTN_PosCompareStop()_Result:{rtn}");
                        }
                        else
                        {
                            rtn = GTN.mc.GTN_PosCompareStop(core, ModelParam.PosComparisonParam.PSOIndex);
                            Console.WriteLine($"GTN_PosCompareStop()_Result:{rtn}");
                        }
                    }
                    break;
                case "清除数据":
                    {

                        rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);
                        Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");
                        //commandHandler("GTN_PosCompareClear", Z);
                        //GTN.mc.TPosCompareData tPosData = new GTN.mc.TPosCompareData();
                        //for (int i = 1000; i < 1500; i++)
                        //{
                        //    tPosData.pos = 100 + 100 * i;

                        //    tPosData.gpo = 0x80;// 通用GPO通道的输出数值，按位表示GPO，bit0 - bit15分别对应GPO0 - GPO15。
                        //                        //脉冲模式：0表示无输出，1表示输出脉冲；电平模式：0表示拉低，1表示拉高
                        //    tPosData.hso = 0xffff;//位置比较输出hso通道的输出数值,按位表示HSObit0-bit9对应HSO0 - HSO9。bit15: 表示逻辑位,在激光功能和位置比较输出功能复用时生效；
                        //                          //脉冲模式：0表示无输出，1表示输出脉冲；电平模式：0表示无输出，1表示有输出。
                        //    tPosData.segmentNumber = (uint)i;
                        //    Z = GTN.mc.GTN_PosCompareData(core, psoIndex, ref tPosData);
                        //    commandHandler("GTN_PosCompareData", Z);
                        //}
                    }
                    break;
                case "关闭DMA":
                    {
                        rtn = GTN.mc.GTN_PosCompareHsOff(core, ModelParam.PosComparisonParam.PSOIndex);
                        Console.WriteLine($"GTN_PosCompareHsOff()_Result:{rtn}");
                        A = 0;
                    }
                    break;
                case "开启DMA":
                    {
                        //位置比较DMA
                        rtn = GTN_PosCompareHsOff(core, ModelParam.PosComparisonParam.PSOIndex);
                        Console.WriteLine($"GTN_PosCompareHsOff()_Result:{rtn}");
                        rtn = GTN_PosCompareHsOn(core, ModelParam.PosComparisonParam.PSOIndex, 1, 200);
                        Console.WriteLine($"GTN_PosCompareHsOn()_Result:{rtn}");
                        A = 1;
                    }
                    break;
                case "测试Y":
                    {
                        //设置位置比较输出模式
                        GTN.mc.TPosCompareMode tPosCompareMode = new GTN.mc.TPosCompareMode()
                        {
                            mode = ModelParam.PosComparisonParam.CompareMode,                                   //位置比较输出模式：0 fifo，1 linear,2 PSO立即
                            dimension = ModelParam.PosComparisonParam.CompareDimension,                         //位置比较维数
                            sourceMode = ModelParam.PosComparisonParam.SourceMode,                              //位置比较源，0：编码器；1：脉冲计数器
                            sourceX = ModelParam.PosComparisonParam.Compare_X,                                  //X轴比较源轴号
                            sourceY = ModelParam.PosComparisonParam.Compare_Y,                                  //Y轴比较源轴号
                            outputMode = ModelParam.PosComparisonParam.CompareOutputMode,                       //输出类型0:脉冲 1:电平，2:电平自动
                            outputPulseWidth = ModelParam.PosComparisonParam.ComparePulseWidth,                 //输出脉冲宽度, 单位为 1us，
                            outputCounter = 1,                                                                  //保留，需要大于0
                            errorBand = ModelParam.PosComparisonParam.CompareErrBand,                           //二维位置比较输出误差带，单位pulse
                        };

                        rtn = GTN.mc.GTN_SetPosCompareMode(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosCompareMode);//设置位置比较输出模式
                        Console.WriteLine($"GTN_SetPosCompareMode()_Result:{rtn}");

                        rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                        Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");

                        rtn = GTN.mc.GTN_PosCompareStart(core, ModelParam.PosComparisonParam.PSOIndex);
                        Console.WriteLine($"GTN_PosCompareStart()_Result:{rtn}");

                        //位置比较DMA
                        //Z = GTN_PosCompareHsOff(core, psoIndex);
                        //commandHandler("GTN_PosCompareHsOff", Z);
                        //Z = GTN_PosCompareHsOn(core, psoIndex, 1, 1);
                        //commandHandler("GTN_PosCompareHsOn", Z);
                        rtn = GTN.mc.GTN_SetPosCompareFifoMode(core, ModelParam.PosComparisonParam.PSOIndex, 1);//设置存储实际位置比较点的缓冲区模式（大小 2048）。
                        Console.WriteLine($"GTN_SetPosCompareFifoMode()_Result:{rtn}");
                        GTN.mc.TPosCompareData2D tPosData2D = new GTN.mc.TPosCompareData2D();


                        for (int i = 0; i < 30; i++)
                        {
                            tPosData2D.posX = 0;
                            tPosData2D.posY = 500 * i;
                            tPosData2D.segmentNumber = (uint)(i + 30);
                            tPosData2D.gpo = 65535;//同一维FIFO模式
                            tPosData2D.hso = 2;
                            ModelParam.PSOOutputStatus[3] = (i + 30).ToString();
                            //textBox96.Invoke(new Action(() =>
                            //{
                            //    textBox96.Text = (i + 30).ToString();
                            //}));

                            rtn = GTN.mc.GTN_PosCompareData2D(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData2D);
                            Console.WriteLine($"GTN_PosCompareData2D()_Result:{rtn}");
                        }
                    }
                    break;
                case "测试X":
                    {
                        //timer_位置比较输出.Enabled = true;
                        //设置位置比较输出模式
                        GTN.mc.TPosCompareMode tPosCompareMode = new GTN.mc.TPosCompareMode()
                        {
                            mode = ModelParam.PosComparisonParam.CompareMode,                                   //位置比较输出模式：0 fifo，1 linear,2 PSO立即模式，3：PSO等待到位模式
                            dimension = ModelParam.PosComparisonParam.CompareDimension,                         //位置比较维数
                            sourceMode = ModelParam.PosComparisonParam.SourceMode,                              //位置比较源，0：编码器；1：脉冲计数器
                            sourceX = ModelParam.PosComparisonParam.Compare_X,                                  //X轴比较源轴号
                            sourceY = ModelParam.PosComparisonParam.Compare_Y,                                  //Y轴比较源轴号
                            outputMode = ModelParam.PosComparisonParam.CompareOutputMode,                       //输出类型0:脉冲 1:电平，2:电平自动翻转
                            outputPulseWidth = ModelParam.PosComparisonParam.ComparePulseWidth,                 //输出脉冲宽度, 单位为 1us，电平模式该参数无效
                            outputCounter = 1,                                                                  //保留，需要大于0
                            errorBand = ModelParam.PosComparisonParam.CompareErrBand,                           // 二维位置比较输出误差带，单位pulse
                        };

                        rtn = GTN.mc.GTN_SetPosCompareMode(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosCompareMode);//设置位置比较输出模式
                        Console.WriteLine($"GTN_SetPosCompareMode()_Result:{rtn}");

                        rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                        Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");

                        rtn = GTN.mc.GTN_PosCompareStart(core, ModelParam.PosComparisonParam.PSOIndex);
                        Console.WriteLine($"GTN_PosCompareStart()_Result:{rtn}");

                        //位置比较DMA
                        rtn = GTN_PosCompareHsOff(core, ModelParam.PosComparisonParam.PSOIndex);
                        Console.WriteLine($"GTN_PosCompareHsOff()_Result:{rtn}");

                        rtn = GTN_PosCompareHsOn(core, ModelParam.PosComparisonParam.PSOIndex, 1, 200);
                        Console.WriteLine($"GTN_PosCompareHsOn()_Result:{rtn}");

                        TPosCompareData[] tPosData = new TPosCompareData[6000];
                        FIFO2 = new Thread(() =>
                        {
                            while (true)
                            {
                                //short CompareSpace;
                                long pSendCount = 0, count = 1000;
                                for (int i = 0; i < 6000; i++)
                                {
                                    //do
                                    //{
                                    //    Z = GTN.mc.GTN_PosCompareSpace(core, psoIndex, out CompareSpace);
                                    //    commandHandler("GTN_PosCompareSpace", Z);
                                    //} while (0 == CompareSpace);
                                    tPosData[0].pos = 100 * i;
                                    tPosData[0].segmentNumber = (uint)(i + 1);
                                    tPosData[0].gpo = 65535;//同一维FIFO模式
                                    tPosData[0].hso = 1;
                                    ModelParam.PSOOutputStatus[3] = (i + 30).ToString();
                                    //textBox96.Invoke(new Action(() =>
                                    //{
                                    //    textBox96.Text = (i + 1).ToString();
                                    //    txt_SendCount.Text = pSendCount.ToString();
                                    //}));

                                    rtn = GTN_PosCompareDataMass(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData[0], out pSendCount, count);
                                    Console.WriteLine($"GTN_PosCompareDataMass()_Result:{rtn}");
                                }
                                short Z1;
                                do
                                {
                                    Z1 = GTN_PosCompareDataMass(core, ModelParam.PosComparisonParam.PSOIndex, IntPtr.Zero, out pSendCount, count);

                                } while (Z1 != 0);
                            }
                        })
                        { IsBackground = true };
                        FIFO2.Start();
                        //fifo_alive = 1;
                    }
                    break;
                case "锁存编码器位置":
                    {
                        int Value_Pcount;
                        int[] Value_X = new int[3000];
                        int[] Value_Y = new int[3000];
                        TLatchValueInfo pValueInfo;
                        rtn = GTN_GetPosCompareLatchValue(core, ModelParam.PosComparisonParam.PSOIndex, 60, out Value_X[0], out Value_Y[0], out Value_Pcount, out pValueInfo);
                        Console.WriteLine($"GTN_GetPosCompareLatchValue()_Result:{rtn}");
                    }
                    break;
                case "绝对位置输出测试":
                    {
                        //设置位置比较输出模式
                        GTN.mc.TPosCompareMode tPosCompareMode = new GTN.mc.TPosCompareMode()
                        {
                            mode = ModelParam.PosComparisonParam.CompareMode,                                   //位置比较输出模式：0 fifo，1 linear,2 PSO立即
                            dimension = ModelParam.PosComparisonParam.CompareDimension,                         //位置比较维数
                            sourceMode = ModelParam.PosComparisonParam.SourceMode,                              //位置比较源，0：编码器；1：脉冲计数器
                            sourceX = ModelParam.PosComparisonParam.Compare_X,                                  //X轴比较源轴号
                            sourceY = ModelParam.PosComparisonParam.Compare_Y,                                  //Y轴比较源轴号
                            outputMode = ModelParam.PosComparisonParam.CompareOutputMode,                       //输出类型0:脉冲 1:电平，2:电平自动
                            outputPulseWidth = ModelParam.PosComparisonParam.ComparePulseWidth,                 //输出脉冲宽度, 单位为 1us，
                            outputCounter = 1,                                                                  //保留，需要大于0
                            errorBand = ModelParam.PosComparisonParam.CompareErrBand,                           //二维位置比较输出误差带，单位pulse
                        };

                        rtn = GTN.mc.GTN_SetPosCompareMode(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosCompareMode);
                        Console.WriteLine($"GTN_SetPosCompareMode()_Result:{rtn}");

                        rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                        Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");

                        GTN.mc.TPosCompareData tPosData = new GTN.mc.TPosCompareData();
                        rtn = GTN.mc.GTN_PosCompareClear(core, ModelParam.PosComparisonParam.PSOIndex);//先清除一下数据
                        Console.WriteLine($"GTN_PosCompareClear()_Result:{rtn}");

                        //1
                        tPosData.pos = 5000;
                        tPosData.gpo = 0;
                        tPosData.hso = 1;
                        rtn = GTN.mc.GTN_PosCompareData(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData);
                        Console.WriteLine($"GTN_PosCompareData()_Result:{rtn}");

                        //2
                        tPosData.pos = 6000;
                        tPosData.gpo = 0;
                        tPosData.hso = 1;
                        rtn = GTN.mc.GTN_PosCompareData(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData);
                        Console.WriteLine($"GTN_PosCompareData()_Result:{rtn}");

                        //3
                        tPosData.pos = 7000;
                        tPosData.gpo = 0;
                        tPosData.hso = 1;
                        rtn = GTN.mc.GTN_PosCompareData(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData);
                        Console.WriteLine($"GTN_PosCompareData()_Result:{rtn}");

                        //4
                        tPosData.pos = 8000;
                        tPosData.gpo = 0;
                        tPosData.hso = 1;
                        rtn = GTN.mc.GTN_PosCompareData(core, ModelParam.PosComparisonParam.PSOIndex, ref tPosData);
                        Console.WriteLine($"GTN_PosCompareData()_Result:{rtn}");

                        rtn = GTN.mc.GTN_PosCompareStart(core, ModelParam.PosComparisonParam.PSOIndex);
                        Console.WriteLine($"GTN_PosCompareStart()_Result:{rtn}");
                    }
                    break;

                default:
                    break;

            }
        });






        #endregion
    }
}
