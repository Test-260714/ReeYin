using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    #region 位置比较相关参数
    /// <summary>
    /// 位置比较参数
    /// </summary>
    public class PosComparisonOutputParam : BindableBase
    {
        [Category("位置比较"), DisplayName("索引[1,8]")]
        public short psoIndex { set; get; } = 1;                                                              //位置比较索引[1,8]
        [Category("位置比较"), DisplayName("输出模式(0:FIFO/1:Liner/2:PSO立即模式/3:PSO等待到位模式)")]
        public short compareMode { set; get; } = 2;                                                           //位置比较输出模式:0:FIFO/1:Liner/2:PSO立即模式/3:PSO等待到位模式
        [Category("位置比较"), DisplayName("输出维数(1：一维位置比较/2：二维位置比较)")]
        public short compareDimension { set; get; } = 2;                                                      //位置比较输出维数：1：一维位置比较/2：二维位置比较
        [Category("位置比较"), DisplayName("输出轴号X")]
        public short compare_X { set; get; } = 1;                                                             //位置比较输出轴号X
        [Category("位置比较"), DisplayName("输出轴号Y")]
        public short compare_Y { set; get; } = 2;                                                             //位置比较输出轴号Y
        [Category("位置比较"), DisplayName("输出脉冲宽度，单位us")]
        public ushort comparePulseWidth { set; get; } = 1;                                                    //位置比较输出脉冲宽度，单位us
        [Category("位置比较"), DisplayName("输出信号模式(0：脉冲/1：电平/2:电平自动翻转)")]
        public short compareOutputMode { set; get; } = 0;                                                     //输出信号模式，0：脉冲/1：电平/2:电平自动翻转
        [Category("位置比较"), DisplayName("比较源(0：编码器/1：脉冲计数器)")]
        public short sourceMode { set; get; } = 1;                                                            //比较源：0：编码器/1：脉冲计数器

        //两个轴由于跟随特性差异，无法同时达到理论点进行位置比较，而是在设定的误差带范围内就触发比较，此时如果需要知道实际的比较点位置，那么需要通过获取实际位置比较点功能来实现
        [Category("位置比较"), DisplayName("误差带设置（二维位置比较输出误差带，单位Pulse）")]
        public ushort compareErrBand { set; get; } = 0;                                                       //误差带设置（二维位置比较输出误差带，单位Pulse）
        [Category("位置比较"), DisplayName("等间距触发间隔，XY轴合成间隔（Pulse）")]
        public int syncPos { set; get; } = 5;                                                                 //等间距触发间隔，XY轴合成间隔（Pulse）

        [JsonIgnore]
        public Queue<PosCompareData> posCompareDatas = new Queue<PosCompareData>();                      //位置比较数据们

        private PosCompareData _sltPosCompareData = new PosCompareData();
        /// <summary>
        /// 选中的位置比较数据
        /// </summary>
        public PosCompareData SltPosCompareData
        {
            get { return _sltPosCompareData; }
            set { _sltPosCompareData = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<PosCompareData> _posCompareDatas = new ObservableCollection<PosCompareData>();
        /// <summary>
        /// 位置比较所有数据
        /// </summary>
        public ObservableCollection<PosCompareData> PosCompareDatas
        {
            get { return _posCompareDatas; }
            set { _posCompareDatas = value; RaisePropertyChanged(); }
        }

    }

    /// <summary>
    /// 位置数据
    /// </summary>
    public class PosCompareData : BindableBase
    {
        private Int32 posX;                                                                  //X轴位置比较输出的位置（是一维时，只需要posX）
        public Int32 PosX
        {
            get { return posX; }
            set { posX = value; RaisePropertyChanged(); }
        }
        private Int32 posY;                                                                  //Y轴位置比较输出的位置
        public Int32 PosY
        {
            get { return posY; }
            set { posY = value; RaisePropertyChanged(); }
        }

        private ushort hso;                                                                  //位置比较输出hso通道的输出数值,按位表示HSObit0-bit9对应
                                                                                            //HSO0-HSO9，bit15:表示逻辑位。在激光功能和位置比较输出
                                                                                            //功能复用时生效；脉冲模式：0表示无输出，1表示输出脉冲；
                                                                                            //电平模式：0表示无输出，1表示有输出。
        public ushort Hso
        {
            get { return hso; }
            set { hso = value; RaisePropertyChanged(); }
        }
        private ushort gpo;                                                                  //通用GPO通道的输出数值，按位表示GPO，bit0-bit15分别对应
                                                                                            //GPO0-GPO15。脉冲模式：0表示无输出，1表示输出脉冲；电
                                                                                            //平模式：0表示拉低，1表示拉高。
        public ushort Gpo
        {
            get { return gpo; }
            set { gpo = value; RaisePropertyChanged(); }
        }

        private UInt32 segmentNumber;                                                        //设置数据段号
        public UInt32 SegmentNumber
        {
            get { return segmentNumber; }
            set { segmentNumber = value; RaisePropertyChanged(); }
        }
    }
    #endregion

}
