using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    [Serializable]
    public class IOManagerModel : BindableBase
    {
        #region Fields

        #endregion

        #region Properties
        private ObservableCollection<IOParam> _allInput = new ObservableCollection<IOParam>();
        /// <summary>
        /// 输入信号
        /// </summary>
        public ObservableCollection<IOParam> AllInput
        {
            get { return _allInput; }
            set { _allInput = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<IOParam> _allOutput = new ObservableCollection<IOParam>();
        /// <summary>
        /// 输出信号
        /// </summary>
        public ObservableCollection<IOParam> AllOutput
        {
            get { return _allOutput; }
            set { _allOutput = value; RaisePropertyChanged(); }
        }

        private IOParam _sltIO;
        /// <summary>
        /// 选中的IO
        /// </summary>
        public IOParam SltIO
        {
            get { return _sltIO; }
            set { _sltIO = value; RaisePropertyChanged(); }
        }

        #endregion

        #region Constructor
        public IOManagerModel()
        {

        }
        #endregion

    }

    /// <summary>
    /// IO配置
    /// </summary>
    public class IOParam : BindableBase
    {
        [JsonIgnore]
        private string _name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _describe;
        /// <summary>
        /// 描述
        /// </summary>
        public string Describe
        {
            get { return _describe; }
            set { _describe = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private string _subjection;
        /// <summary>
        /// 隶属
        /// </summary>
        public string Subjection
        {
            get { return _subjection; }
            set { _subjection = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private long _port;
        /// <summary>
        /// 端口
        /// </summary>
        public long Port
        {
            get { return _port; }
            set { _port = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private bool _state;

        public bool State
        {
            get { return _state; }
            set { _state = value; RaisePropertyChanged(); }
        }

    }
}
