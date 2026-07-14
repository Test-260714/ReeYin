using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Project
{
    /// <summary>
    /// 通用传递的数据
    /// </summary>
    public class TransmitParam : BindableBase
    {
        private bool isLink = false;
        /// <summary>
        /// 是从连接获取的
        /// </summary>
        public bool IsLink
        {
            get { return isLink; }
            set { isLink = value; }
        }

        private Guid _linkGuid;
        /// <summary>
        /// Link的Guid
        /// </summary>
        public Guid LinkGuid
        {
            get { return _linkGuid; }
            set { _linkGuid = value; RaisePropertyChanged(); }
        }

        private int _serial;
        /// <summary>
        /// 节点序号(唯一)
        /// </summary>
        public int Serial
        {
            get { return _serial; }
            set { _serial = value; RaisePropertyChanged(); }
        }

        private string _parentNode;

        public string ParentNode
        {
            get { return _parentNode; }
            set { _parentNode = value; RaisePropertyChanged(); }
        }

        private Guid _guid = Guid.NewGuid();
        /// <summary>
        /// 唯一标识
        /// </summary>
        public Guid Guid
        {
            get { return _guid; }
            set { _guid = value; RaisePropertyChanged(); }
        }

        private ResoureceType _resourece;
        /// <summary>
        /// 数据来源
        /// </summary>
        public ResoureceType Resourece
        {
            get { return _resourece; }
            set { _resourece = value; }
        }

        private string _name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        private string _paramName;
        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParamName
        {
            get { return _paramName; }
            set { _paramName = value; RaisePropertyChanged(); }
        }

        private DataType _type;
        /// <summary>
        /// 数据类型
        /// </summary>
        public DataType Type
        {
            get { return _type; }
            set { _type = value; RaisePropertyChanged(); }
        }

        private object _value;
        /// <summary>
        /// 值
        /// </summary>
        public object Value
        {
            get { return _value; }
            set { _value = value; RaisePropertyChanged(); }
        }

        private string _describe;
        /// <summary>
        /// 描述
        /// </summary>
        public string Describe
        {
            get { return _describe; }
            set { _describe = value; RaisePropertyChanged(); }
        }

        private bool _isGlobal;
        /// <summary>
        /// 标记为全局
        /// </summary>
        public bool IsGlobal
        {
            get { return _isGlobal; }
            set { _isGlobal = value; RaisePropertyChanged(); }
        }

        private string _resourcePath;
        /// <summary>
        /// 数据源
        /// </summary>
        public string ResourcePath
        {
            get { return _resourcePath; }
            set { _resourcePath = value; }
        }

    }

    /// <summary>
    /// 源类型
    /// </summary>
    public enum ResoureceType
    {
        None,
        Inupt,
        Output,
        Global,
        LastInput,
        CustomGlobal
    }

    /// <summary>
    /// 数据类型
    /// </summary>
    public enum DataType
    {
        None,
        Int,
        String,
        Bool,
        Double,
        Datetime,
        Enum,
        List,
        Dict,
        _object,
        Object,
        Array,
        /// <summary>
        /// Halcon的基础类型
        /// </summary>
        HObject,
        /// <summary>
        /// Opencv的基础类型
        /// </summary>
        Mat,
    }
}
