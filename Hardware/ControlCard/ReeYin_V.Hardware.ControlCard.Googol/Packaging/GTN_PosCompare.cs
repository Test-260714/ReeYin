using Dm.util;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using static GTN.mc;

namespace GoogolMotion
{
    public partial class GoogolGTMotion
    {
        #region Fields
        // 创建取消FIFO1令牌
        private CancellationTokenSource cancellationFIFO1 = new CancellationTokenSource();

        private short fifo_alive = 0;

        private Thread MonitorFIFO1 = null, MonitorFIFO2 = null;

        private Task _task;
        private object _lock = new object();  // 用于确保任务创建时线程安全
        /// <summary>
        /// 需要临时插入数据，使用此变量
        /// </summary>
        public Queue<PosCompareData> InsertPosCompareDatas = new Queue<PosCompareData>();

        /// <summary>
        /// 是否开启位置比较DMA
        /// </summary>
        private int PosCompareDMA;
        #endregion

        #region Methods
        /// <summary>
        /// 开始位置比较输出
        /// (启用后运动至指定位置后就会触发指定信号输出，推测压入的数据在触发后就会被消耗掉，所以不会重新触发，
        /// 新压入的数据应该要在规划运动的路径前)
        /// </summary>
        /// <param name="param">初始化参数</param>
        /// <param name="posCompareDatas">位置参数</param>
        /// <returns></returns>
        public bool StartPosComparisonOutput(PosComparisonOutputParam param, short core = 1)
        {
            try
            {
                //开启位置比较DMA，压点用的到
                SetPosComparisonDMA(true, 1);
                short rtn;

                //先关闭位置比较功能，避免压入数据异常/*压数据时必须在位置比较输出关闭的时候*/
                rtn = GTN.mc.GTN_PosCompareStop(core, param.psoIndex);
                if (rtn != 0)
                {
                    Console.WriteLine($"GTN_PosCompareStop()_停止位置比较失败，返回值为{rtn}！");
                    return false;
                }

                //初始化位置比较模式参数
                TPosCompareMode tPosCompareMode = new TPosCompareMode()
                {
                    mode = param.compareMode,                                                   //位置比较输出模式：0 fifo，1 linear,2 PSO立即模式，3：PSO等待到位模式
                    dimension = param.compareDimension,                                         //位置比较维数
                    sourceMode = param.sourceMode,                                              //位置比较源，0：编码器；1：脉冲计数器
                    sourceX = param.compare_X,                                                  //X轴比较源轴号
                    sourceY = param.compare_Y,                                                  //Y轴比较源轴号
                    outputMode = param.compareOutputMode,                                       //输出类型0:脉冲 1:电平，2:电平自动翻转
                    outputPulseWidth = param.comparePulseWidth,                                 //输出脉冲宽度, 单位为 1us，电平模式该参数无效
                    outputCounter = 1,                                                          //保留，需要大于0
                    errorBand = param.compareErrBand,                                           //二维位置比较输出误差带，单位pulse
                };

                rtn = GTN_SetPosCompareMode(core, param.psoIndex, ref tPosCompareMode);         //设置位置比较输出模式
                if (rtn != 0)
                {
                    Console.WriteLine($"GTN_SetPosCompareMode()_设置比较模式失败，返回值为{rtn}！");
                    return false;
                }

                //设置位置比较输出数据并启动位置比较输出
                if (param.compareMode == 0 && param.compareDimension == 1)                              //一维位置比较，FIFO模式
                {
                    TPosCompareData tPosData = new TPosCompareData();
                    // 确定需要处理的数据量（最多1000条）
                    int itemsToProcess = Math.Min(param.posCompareDatas.Count, 1000);

                    #region 测试
                    //for (i = 0; i < 1001; i++)
                    //{
                    //    tPosData.pos = 0 + i;

                    //    tPosData.gpo = 0xff;// 通用GPO通道的输出数值，按位表示GPO，bit0 - bit15分别对应GPO0 - GPO15。
                    //                        //脉冲模式：0表示无输出，1表示输出脉冲；电平模式：0表示拉低，1表示拉高
                    //    tPosData.hso = 0xffff;//位置比较输出hso通道的输出数值,按位表示HSObit0-bit9对应HSO0 - HSO9。bit15: 表示逻辑位,在激光功能和位置比较输出功能复用时生效；
                    //                          //脉冲模式：0表示无输出，1表示输出脉冲；电平模式：0表示无输出，1表示有输出。
                    //    tPosData.segmentNumber = (uint)(i + 1);
                    //    rtn = GTN.mc.GTN_PosCompareData(core, param.psoIndex, ref tPosData);
                    //    if (rtn != 0)
                    //    {
                    //        Console.WriteLine($"GTN_PosCompareData()_位置{tPosData.pos}的数据压入异常，返回值为{rtn}！");
                    //        return false;
                    //    }
                    //}
                    #endregion
                }
                //二维位置比较，FIFO模式
                else if (param.compareMode == 0 && param.compareDimension == 2)
                {
                    GTN.mc.TPosCompareData2D tPosData2D = new GTN.mc.TPosCompareData2D();
                    short CompareSpace;

                    do
                    {
                        rtn = GTN.mc.GTN_PosCompareSpace(core, param.psoIndex, out CompareSpace);
                        if (rtn != 0)
                        {
                            Console.WriteLine($"GTN_PosCompareSpace()_获取位置比较剩余空间异常，返回值为{rtn}！");
                        }
                    } while (0 == CompareSpace);

                    #region 测试
                    //int j = 0;
                    //for (j = 0; j < 100; j++)
                    //{
                    //    do
                    //    {
                    //        rtn = GTN.mc.GTN_PosCompareSpace(core, param.psoIndex, out CompareSpace);
                    //        //commandHandler("GTN_PosCompareSpace", Z);
                    //    } while (0 == CompareSpace);
                    //    tPosData2D.posX = 100 + 100 * j;
                    //    tPosData2D.posY = 100 + 100 * j;
                    //    tPosData2D.segmentNumber = (uint)(j + 1);
                    //    tPosData2D.gpo = 0xff;//同一维FIFO模式
                    //    tPosData2D.hso = 0x1;

                    //    rtn = GTN.mc.GTN_PosCompareData2D(core, param.psoIndex, ref tPosData2D);
                    //    //commandHandler("GTN_PosCompareData2D", Z);
                    //}
                    //for (int p = 0; p < InsertPosCompareDatas.Count; p++)
                    //{
                    //    do
                    //    {
                    //        rtn = GTN.mc.GTN_PosCompareSpace(core, param.psoIndex, out CompareSpace);
                    //        //commandHandler("GTN_PosCompareSpace", Z);
                    //    } while (0 == CompareSpace);

                    //    var temp = InsertPosCompareDatas.Dequeue();
                    //    var tPosData = new TPosCompareData2D();
                    //    if (temp != null)
                    //    {
                    //        tPosData = new TPosCompareData2D
                    //        {
                    //            posY = temp.PosX,
                    //            posX = temp.PosY,
                    //            hso = 0xf,
                    //            gpo = 0xff,//同一维FIFO模式
                    //            segmentNumber = (uint)(p + 1),
                    //        };
                    //        rtn = GTN.mc.GTN_PosCompareData2D(core, param.psoIndex, ref tPosData);
                    //    }
                    //}

                    lock (_lock)
                    {
                        // 如果任务已经存在，则不再重复创建
                        if (_task == null || _task.IsCompleted)
                        {
                            _task = Task.Run(() =>
                            {
                                int j = 0;  // 初始化计数器
                                while (true)
                                {
                                    // 确保队列不为空
                                    if (InsertPosCompareDatas.Count == 0)
                                    {
                                        //Console.WriteLine("数据队列为空，等待新的数据...");
                                        //Thread.Sleep(10);  // 防止 CPU 高占用，适当等待
                                        continue;  // 如果队列为空，继续循环
                                    }

                                    short CompareSpace;
                                    // 循环直到获取到非零的 CompareSpace
                                    var R = GTN.mc.GTN_PosCompareSpace(core, param.psoIndex, out CompareSpace);
                                    // 输出剩余空间
                                    Console.WriteLine($"剩余空间为 {CompareSpace}");
                                    while (R != 0 || CompareSpace == 0)
                                    {
                                        Console.WriteLine("等待位置比较空间...");
                                        Task.Delay(1).Wait();
                                    }

                                    // 从队列中取出数据
                                    var temp = InsertPosCompareDatas.Dequeue();
                                    var tPosData = new TPosCompareData2D();
                                    j++;
                                    if (temp != null)
                                    {
                                        // 准备要插入的数据
                                        tPosData = new TPosCompareData2D
                                        {
                                            posY = temp.PosX,
                                            posX = temp.PosY,
                                            hso = 0xf,
                                            gpo = 0xff,  // 同一维FIFO模式
                                            segmentNumber = (uint)(j + 1),
                                        };

                                        // 插入数据
                                        int rtn = GTN.mc.GTN_PosCompareData2D(core, param.psoIndex, ref tPosData);
                                        if (rtn != 0)
                                        {
                                            Console.WriteLine($"数据插入失败，返回码: {rtn}");
                                        }
                                    }

                                }
                            });
                        }
                        else
                        {
                            Console.WriteLine("任务已在运行中，跳过创建新任务");
                        }
                    }

                    if (MonitorFIFO1 == null)
                    {
                        if (cancellationFIFO1 == null) cancellationFIFO1 = new CancellationTokenSource();
                        CancellationToken token = cancellationFIFO1.Token;
                        //开启状态
                        MonitorFIFO1 = new Thread(() => MonitorStatusWork(param, token));
                        MonitorFIFO1.Start();
                    }
                    #endregion

                    fifo_alive = 1;
                }
                //一维位置比较，Linear模式
                else if (param.compareMode == 1 && param.compareDimension == 1)
                {
                    TPosCompareLinear tPosCompareLinear = new TPosCompareLinear()
                    {
                        startPos = 0,                                                                   //线性比较输出的起点位置
                        interval = 1000,                                                                //位置比较输出位置间隔
                        count = 10,                                                                     //位置比较输出个数
                        hso = 8,                                                                        //位置比较输出hso通道的输出数值，按位表示
                                                                                                        //脉冲模式：0 表示无输出，1 表示输出脉冲
                        gpo = 0x1,                                                                      //通用GPO通道的输出数值，按位表示
                                                                                                        //脉冲模式：0 表示无输出，1 表示输出脉冲
                    };

                    rtn = GTN_PosCompareClear(core, param.psoIndex);//先清除一下数据
                    if (rtn != 0)
                    {
                        Console.WriteLine($"GTN_PosCompareClear()_清除数据源异常，返回值为{rtn}！");
                        return false;
                    }

                    rtn = GTN_SetPosCompareLinear(core, param.psoIndex, ref tPosCompareLinear);
                    if (rtn != 0)
                    {
                        Console.WriteLine($"GTN_SetPosCompareLinear()_设置一维线性比较输出参数异常，返回值为{rtn}！");
                        return false;
                    }
                }
                //PSO立即模式
                else if (param.compareMode == 2)
                {
                    rtn = GTN.mc.GTN_PosCompareClear(core, param.psoIndex);//先清除一下数据
                    if (rtn != 0)
                    {
                        Console.WriteLine($"GTN_PosCompareClear()_清除数据源异常，返回值为{rtn}！");
                        return false;
                    }

                    GTN.mc.TPosComparePsoPrm tPosComparePsoPrm = new GTN.mc.TPosComparePsoPrm()
                    {
                        count = 1,                                                                      //保留，使用时设置大于0的数
                        syncPos = param.syncPos                                                         //输出间距，X、Y轴的合成间距。单位：Pulse
                    };

                    rtn = GTN.mc.GTN_SetPosComparePsoPrm(core, param.psoIndex, ref tPosComparePsoPrm);
                    if (rtn != 0)
                    {
                        Console.WriteLine($"GTN_SetPosComparePsoPrm()_设置位置同步比较输出异常，返回值为{rtn}！");
                        return false;
                    }

                    //if (MonitorFIFO1 == null)
                    //{
                    //    CancellationToken token = cancellationFIFO1.Token;
                    //    //开启状态
                    //    MonitorFIFO1 = new Thread(() => MonitorStatusWork(param, token));
                    //    MonitorFIFO1.Start();
                    //}

                }
                //PSO等待到位模式
                else if (param.compareMode == 3)
                {
                    rtn = GTN.mc.GTN_PosCompareClear(core, param.psoIndex);//先清除一下数据
                    if (rtn != 0)
                    {
                        Console.WriteLine($"GTN_PosCompareClear()_清除数据源异常，返回值为{rtn}！");
                        return false;
                    }

                    GTN.mc.TPosComparePsoPrm tPosComparePsoPrm = new GTN.mc.TPosComparePsoPrm()
                    {
                        count = 1,                                                                      //保留，使用时设置大于0的数
                        syncPos = param.syncPos                                                         //输出间距，X、Y轴的合成间距。单位：Pulse
                    };

                    rtn = GTN.mc.GTN_SetPosComparePsoPrm(core, param.psoIndex, ref tPosComparePsoPrm);
                    if (rtn != 0)
                    {
                        Console.WriteLine($"GTN_SetPosComparePsoPrm()_设置位置同步比较输出异常，返回值为{rtn}！");
                        return false;
                    }
                }

                rtn = GTN_PosCompareStart(core, param.psoIndex);
                if (rtn != 0)
                {
                    Console.WriteLine($"GTN_PosCompareStart()_设置位置同步比较开始异常，返回值为{rtn}！");
                    return false;
                }

                rtn = GTN_SetPosCompareFifoMode(core, param.psoIndex, 1);//设置存储实际位置比较点的缓冲区模式（大小 2048）。
                short pMode = 0;
                rtn = GTN_GetPosCompareFifoMode(core, param.psoIndex, out pMode);//设置存储实际位置比较点的缓冲区模式（大小 2048）。
                if (rtn != 0)
                {
                    Console.WriteLine($"GTN_SetPosCompareFifoMode()_启用实际数据，返回值为{rtn}！");
                    return false;
                }

                #region 设置一些高速IO同时输出
                //if (!SetTerminalPermit(4))
                //{
                //    Console.WriteLine("设置GPO输出失败！！！");
                //}
                //if (!SetTerminalPermit(3))
                //{
                //    Console.WriteLine("设置GPO输出失败！！！");
                //}
                //if (!SetTerminalPermit(5))
                //{
                //    Console.WriteLine("设置GPO输出失败！！！");
                //}
                //if (!SetTerminalPermit(1))
                //{
                //    Console.WriteLine("设置GPO输出失败！！！");
                //}
                //if (!SetTerminalPermit(2))
                //{
                //    Console.WriteLine("设置GPO输出失败！！！");
                //}
                //if (!SetTerminalPermit(0))
                //{
                //    Console.WriteLine("设置GPO输出失败！！！");
                //}
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 停止位置比较功能
        /// </summary>
        /// <returns></returns>
        public bool StopPosComparisonOutput(PosComparisonOutputParam param, short core = 1)
        {
            try
            {
                int[] ActualX = new int[2000], ActualY = new int[2000];
                //GetActualComparePos(param.psoIndex, ref ActualX, ref ActualY);
                //Console.WriteLine($"实际位置比较数量为{ActualX.Length}");
                //foreach (var item in ActualX)
                //{
                //    if (item != 0)
                //        Console.WriteLine($"实际位置为：{item}");
                //}
                GetActualComparePos(param.psoIndex, ref ActualX, ref ActualY);
                Console.WriteLine($"实际位置比较数量为{ActualX.Length}");
                for (int i = 0; i < ActualX.Length; i++)
                {
                    if (ActualX[i] != 0)
                        Console.WriteLine($"实际位置为：X{ActualX[i]}Y:{ActualY[i]}");
                }



                var rtn = GTN_PosCompareStop(core, param.psoIndex);
                if (rtn != 0)
                {
                    Console.WriteLine($"GTN_PosCompareStop()_停止位置比较失败，返回值为{rtn}！");
                    return false;
                }

                //位置比较输出状态
                GTN.mc.TPosCompareStatus pStatus = new GTN.mc.TPosCompareStatus();
                rtn = GTN.mc.GTN_PosCompareStatus(core, param.psoIndex, out pStatus);

                Console.WriteLine($"位置比较脉冲输出次数：{pStatus.pulseCount},剩余空间：{pStatus.space}");

                if (cancellationFIFO1 != null)
                {
                    Console.WriteLine("发送停止信号给 MonitorFIFO1...");
                    cancellationFIFO1.Cancel(); // 发送取消信号
                                                // 注意：这里只是发送信号，MonitorStatusWork 内部需要响应并退出
                }

                if (MonitorFIFO1 != null && MonitorFIFO1.IsAlive)
                {
                    try
                    {
                        // 可选：等待线程自然结束，避免强制终止
                        // 设置一个合理的超时时间，以防线程挂起
                        bool finished = MonitorFIFO1.Join(TimeSpan.FromSeconds(5)); // 例如等待最多5秒
                        if (!finished)
                        {
                            Console.WriteLine("警告：MonitorFIFO1 线程未能在规定时间内正常退出，可能需要强制终止或排查原因。");
                            // 不推荐使用 MonitorFIFO1.Interrupt() 或 MonitorFIFO1.Abort() (已废弃)
                            // 更好的方式是在 MonitorStatusWork 中确保循环能快速响应 cancellation token。
                        }
                        else
                        {
                            Console.WriteLine("MonitorFIFO1 线程已正常退出。");
                        }
                    }
                    catch (ThreadStateException ex)
                    {
                        Console.WriteLine($"等待 MonitorFIFO1 线程退出时出错: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("MonitorFIFO1 线程未启动或已结束。");
                }



                // 释放 CTS 资源
                cancellationFIFO1?.Dispose();
                cancellationFIFO1 = null;
                MonitorFIFO1 = null; // 清空引用
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 设置位置比较时输出指定IO
        /// PS：已经启用位置比较后有效
        /// </summary>
        /// <param name="index">起始硬件通道号</param>
        /// <param name="permit"></param>
        /// <param name="ModuleNum">模块号(从1开始)</param>
        /// <param name="dataType">输出类型(MC_GPO（12）: 通用数字量输出，对应硬件的 DO/MC_HSO（18）: 高速IO输出，对应硬件的HSO)</param>
        /// <param name="core">核号（一般就是核1，因为核2是EC不支持）</param>
        /// <returns></returns>
        public bool SetTerminalPermit(short index, short permit = 0x02, short ModuleNum = 1, short dataType = GTN.mc.MC_GPO, short core = 1)
        {
            try
            {
                short rtn;
                rtn = GTN.mc.GTN_SetTerminalPermitEx(core,              //核号（一般就是核1，因为核2是EC不支持）
                ModuleNum,                                              //模块号(从1开始)
                dataType,                                               //输出类型(MC_GPO（12）: 通用数字量输出，对应硬件的 DO/MC_HSO（18）: 高速IO输出，对应硬件的HSO)
                ref permit,                                             //设置软件控制权限：
                                                                        //Bit0: 通用 IO 指令输出（当 dataType 为 MC_HSO 时，该值无效）
                                                                        //Bit1: 第一路位置比较输出
                                                                        //Bit2: 第二路位置比较输出
                                                                        //Bit3: 使能激光开关光输出（当 dataType 为 MC_GPO 时，该值无效）
                                                                        //Bit4: 使能 PWM 信号输出（当 dataType 为 MC_GPO 时，该值无效）
                index,                                                  //起始硬件通道号
                1);                                                     //硬件数量

                return rtn == 0 ? true : false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 设置位置比较DMA功能开关
        /// </summary>
        /// <param name="On_Off"></param>
        /// <param name="psoIndex">位置比较索引</param>
        /// <param name="threshold"></param>
        /// <param name="core"></param>
        /// <returns></returns>
        public bool SetPosComparisonDMA(bool On_Off, short psoIndex, short threshold = 200, short core = 1)
        {
            try
            {
                short rtn = 0;
                //开启
                if (On_Off)
                {
                    rtn = GTN_PosCompareHsOff(core, psoIndex);
                    if (rtn != 0)
                    {
                        Logs.LogWarning($"GTN_PosCompareHsOff()_设置位置比较功能关闭失败，返回值为{rtn}！");
                        return false;
                    }

                    rtn = GTN_PosCompareHsOn(core, psoIndex, 1, threshold);
                    if (rtn != 0)
                    {
                        Logs.LogWarning($"GTN_PosCompareHsOff()_设置位置比较功能开启失败，返回值为{rtn}！");
                        return false;
                    }

                    PosCompareDMA = 1;
                }
                else //关闭
                {
                    rtn = GTN_PosCompareHsOff(core, psoIndex);
                    if (rtn != 0)
                    {
                        Logs.LogWarning($"GTN_PosCompareHsOff()_设置位置比较功能关闭失败，返回值为{rtn}！");
                        return false;
                    }

                    PosCompareDMA = 0;
                }

                return rtn == 0 ? true : false;
            }
            catch (Exception ex)
            {
                Logs.LogError($"{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 获取存储实际位置比较点数据。
        /// </summary>
        /// <returns></returns>
        public bool GetActualComparePos(short posCompareIndex, ref int[] ActualX, ref int[] ActualY, short core = 1)
        {
            try
            {
                short rtn = 0;
                int actCount;
                TLatchValueInfo latchInfo;
                rtn = GTN_GetPosCompareLatchValue(core, posCompareIndex, 200, out ActualX[0], out ActualY[0], out actCount, out latchInfo);
                if (rtn != 0)
                {

                    Console.WriteLine($"GTN_GetPosCompareLatchValue()_获取存储实际位置比较点数据失败，返回值为{rtn}！");
                    return false;
                }
                Console.WriteLine($"latchInfo{latchInfo.pad1_1},{latchInfo.pad1_2},{latchInfo.pad1_3},{latchInfo.pad2_1},{latchInfo.pad2_2},{latchInfo.fifoFull},");
                if (actCount == 0)
                {
                    Console.WriteLine($"缓存区已无数据！");
                }

                if (latchInfo.fifoFull != 0)
                {
                    Console.WriteLine($"缓冲区已经满了，重置！");
                    rtn = GTN_SetPosCompareFifoMode(core, posCompareIndex, 1);//设置存储实际位置比较点的缓冲区模式（大小 2048）。
                    if (rtn != 0)
                    {
                        Console.WriteLine($"GTN_SetPosCompareFifoMode()设置存储实际位置比较点的缓冲区模式失败，返回值为{rtn}！");
                        return false;
                    }
                }

                return rtn == 0 ? true : false;
            }
            catch (Exception ex)
            {
                Logs.LogError($"{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 监控FIFO1的工作，压入新的数据
        /// </summary>
        /// <param name="param"></param>
        /// <param name="core"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public bool MonitorFIFO1Work(PosComparisonOutputParam param, CancellationToken cancellationToken, short core = 1)
        {
            short rtn;
            while (true)
            {
                // 检查是否需要取消
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("工作线程：收到停止信号，准备退出");
                    #region 处理需要释放的资源等

                    #endregion
                    return false;
                }

                if (param.compareDimension == 1)
                {
                    #region 获取位置比较状态
                    TPosCompareStatus pStatus = new TPosCompareStatus();
                    rtn = GTN_PosCompareStatus(core, param.psoIndex, out pStatus);
                    if (rtn != 0)
                    {
                        Console.WriteLine($"GTN_PosCompareStatus()_获取位置比较状态失败，返回值为{rtn}！");
                    }
                    #endregion

                    TPosCompareData tPosData = new TPosCompareData();
                    #region 边走边压入数据，优先将未压入完的数据压入
                    if (pStatus.space < 1000 && param.posCompareDatas.Count > 0)
                    {
                        var temp = param.posCompareDatas.Dequeue();

                        tPosData = new TPosCompareData
                        {
                            pos = temp.PosX,
                            hso = temp.Hso,
                            gpo = temp.Gpo,
                            segmentNumber = temp.SegmentNumber,
                        };

                        rtn = GTN_PosCompareData(core, param.psoIndex, ref tPosData);
                        if (rtn != 0)
                        {
                            Console.WriteLine($"GTN_PosCompareData()_位置{temp.PosX}的数据压入异常，返回值为{rtn}！");
                        }
                    }

                    //压入新追加的参数
                    if (pStatus.space < 1000 && param.posCompareDatas.Count == 0 && InsertPosCompareDatas.Count > 0)
                    {
                        var temp = InsertPosCompareDatas.Dequeue();

                        tPosData = new TPosCompareData
                        {
                            pos = temp.PosX,
                            hso = temp.Hso,
                            gpo = temp.Gpo,
                            segmentNumber = temp.SegmentNumber,
                        };

                        rtn = GTN_PosCompareData(core, param.psoIndex, ref tPosData);
                        if (rtn != 0)
                        {
                            Console.WriteLine($"GTN_PosCompareData()_位置{temp.PosX}的数据压入异常，返回值为{rtn}！");
                        }
                    }

                    #endregion
                }
                else if (param.compareDimension == 2)
                {
                    GTN.mc.TPosCompareData2D tPosData2D = new GTN.mc.TPosCompareData2D();
                    short CompareSpace;

                    do
                    {
                        rtn = GTN.mc.GTN_PosCompareSpace(core, param.psoIndex, out CompareSpace);
                        if (rtn != 0)
                        {
                            Console.WriteLine($"GTN_PosCompareSpace()_获取位置比较剩余空间异常，返回值为{rtn}！");
                        }
                    } while (0 == CompareSpace);


                    #region 测试
                    //for (int i = 0; i < 1000; i++)
                    //{
                    //    do
                    //    {
                    //        rtn = GTN.mc.GTN_PosCompareSpace(core, param.psoIndex, out CompareSpace);
                    //        //commandHandler("GTN_PosCompareSpace", Z);
                    //    } while (0 == CompareSpace);
                    //    tPosData2D.posX = 100 + 100 * i;
                    //    tPosData2D.posY = 100 + 100 * i;
                    //    tPosData2D.segmentNumber = (uint)(i + 1);
                    //    tPosData2D.gpo = 0xff;//同一维FIFO模式
                    //    tPosData2D.hso = 0x1;

                    //    rtn = GTN.mc.GTN_PosCompareData2D(core, param.psoIndex, ref tPosData2D);
                    //    //commandHandler("GTN_PosCompareData2D", Z);
                    //}
                    #endregion

                }
            }
        }

        public List<(int[], int[])> ActualCompaPos = new List<(int[], int[])>();
        /// <summary>
        /// 监听状态，脉冲输出次数，
        /// </summary>
        /// <param name="param"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="core"></param>
        /// <returns></returns>
        public bool MonitorStatusWork(PosComparisonOutputParam param, CancellationToken cancellationToken, short core = 1)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested) // 使用 ! 可以让逻辑更清晰
                {
                    //int[] ActualX = new int[2000], ActualY = new int[2000];
                    //GetActualComparePos(param.psoIndex, ref ActualX, ref ActualY);
                    //Console.WriteLine($"实际位置比较数量为{ActualX.Length}");
                    //for (int i = 0; i < ActualX.Length; i++)
                    //{
                    //    if (ActualX[i] != 0)
                    //        Console.WriteLine($"实际位置为：X{ActualX[i]/10000}Y:{ActualY[i] / 10000}");
                    //}

                    //ActualCompaPos.Add((ActualX, ActualY));
                    // 使用 cancellationToken 版本的 Delay，以便能被取消

                    GTN.mc.TPosCompareInfo pInfo = new GTN.mc.TPosCompareInfo();


                    var rtn = GTN.mc.GTN_PosCompareInfo(core, 1, out pInfo);

                    Console.WriteLine($"pInfo.fifoEmpty:{pInfo.fifoEmpty},pInfo.commandReceive:{pInfo.commandReceive},pInfo.commandSend:{pInfo.commandSend},pInfo.posX:{pInfo.posX},pInfo.posY:{pInfo.posY}");
                    try
                    {
                        Task.Delay(1, cancellationToken).Wait(); // Wait(CancellationToken) 会响应取消
                    }
                    catch (OperationCanceledException)
                    {
                        // 当延迟期间令牌被取消时，会抛出此异常
                        Console.WriteLine("MonitorStatusWork()_工作线程：延迟期间收到停止信号，准备退出");
                        break; // 跳出循环
                    }
                }

                Console.WriteLine("MonitorStatusWork()_工作线程：收到停止信号，准备退出");
                #region 处理需要释放的资源等
                // 在这里释放资源...
                #endregion
                return true; // 成功退出
            }
            catch (OperationCanceledException)
            {
                // 确认是被取消导致的退出
                Console.WriteLine("MonitorStatusWork()_工作线程：被取消异常导致退出");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MonitorStatusWork()_工作线程：发生未处理异常 {ex.Message}");
                return false; // 异常退出
            }
        }
        #endregion


    }
}
