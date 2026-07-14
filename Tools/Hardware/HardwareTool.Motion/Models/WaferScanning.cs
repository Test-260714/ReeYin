using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HardwareTool.Motion.Models
{
    /// <summary>
    /// 晶圆扫描
    /// </summary>
    public class WaferScanning : BindableBase
    {
        [JsonIgnore]
        private bool isUsing;
        [Browsable(false)]
        public bool IsUsing
        {
            get { return isUsing; }
            set { isUsing = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private int _scanningMode = 0;
        /// <summary>
        /// 扫描方式
        /// </summary>
        [Browsable(false)]
        public int ScanningMode
        {
            get { return _scanningMode; }
            set { _scanningMode = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _isAerialShot;
        /// <summary>
        /// 启用飞拍
        /// </summary>
        [Category("位置比较"), DisplayName("启用飞拍")]
        public bool IsAerialShot
        {
            get { return _isAerialShot; }
            set { _isAerialShot = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private List<(Point Start, Point End)> _lines = new List<(Point Start, Point End)>();
        /// <summary>
        /// 线s(线扫时一直开)
        /// </summary>
        [Browsable(false)]
        public List<(Point Start, Point End)> Lines
        {
            get { return _lines; }
            set { _lines = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private List<Point> _points = new List<Point>();
        /// <summary>
        /// 点s(点扫描时，使用位置比较（飞拍）)
        /// </summary>
        [Browsable(false)]
        public List<Point> Points
        {
            get { return _points; }
            set { _points = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _describe;
        /// <summary>
        /// 描述
        /// </summary>
        [Browsable(false)]
        public string Describe
        {
            get { return _describe; }
            set { _describe = value; RaisePropertyChanged(); }
        }

    }
}
