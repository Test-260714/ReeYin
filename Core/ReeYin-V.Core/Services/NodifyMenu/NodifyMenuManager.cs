using Newtonsoft.Json;
using Prism.Common;
using Prism.Mvvm;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace ReeYin_V.Core
{
    [ExposedService(Lifetime.Singleton, 3, typeof(INodifyMenuManager))]
    public class NodifyMenuManager : INodifyMenuManager
    {
        #region Fields
        /// <summary>
        /// 可用菜单
        /// </summary>
        public List<MenuInfo> AvailableMenus = new List<MenuInfo>();

        /// <summary>
        /// 保存在本地的，和所有菜单进行比较的数据，最终得到可用菜单
        /// </summary>
        public List<MenuInfo> LocalMenus = new List<MenuInfo>();
        #endregion

        #region Properties
        private List<MenuInfo> _allMenus = new List<MenuInfo>();
        /// <summary>
        /// 实际加载到的所有菜单
        /// </summary>
        public List<MenuInfo> AllMenus
        {
            get { return _allMenus; }
            set { _allMenus = value; }
        }

        #endregion

        #region Constructor
        public NodifyMenuManager()
        {
            //从本地读取指定LocalMenus信息
            LocalMenus = JsonHelper.JsonDisObjectSerialize<List<MenuInfo>>(FileHelper.AppHiddenPath + $"\\LocalMenus.json", out string str, TypeNameHandling.Auto) ?? new List<MenuInfo>();

            //AvailableMenus

        }
        #endregion

        #region Methods
        /// <summary>
        /// 检查新加的菜单
        /// </summary>
        public void CheckMenu(MenuInfo menu)
        {
            if(menu.Icon == null || menu.Icon == "")
            {
                menu.Icon = "\ue636"; 
            }

            if(menu.Title == null || menu.Title == "")
            {
                menu.Title = "Unnamed";
            }

            if(menu.Type == null || menu.Type == "")
            {
                menu.Type = "Other";
            }
        }


        public void AddMenu(MenuInfo menu)
        {
            // 检查菜单
            CheckMenu(menu);
            if (!Debugger.IsAttached)
            {
                if (LocalMenus.Where(p => p.Title == menu.Title && p.Type == menu.Type && p.IsUsing == true).ToList().Count > 0)
                {
                    menu.IsUsing = true;
                    AvailableMenus.Add(menu);
                }
            }
            else
            {
                menu.IsUsing = true;
                AvailableMenus.Add(menu);
            }
            // 添加菜单
            AllMenus.Add(menu);

        }

        public void RemoveMenu(MenuInfo menu)
        {
            AllMenus.Remove(menu);
        }

        public void Clear()
        {
            AllMenus.Clear();
        }

        #endregion
    }

    [Serializable]
    public class MenuInfo : BindableBase
    {
        [JsonIgnore]
        private bool _isUsing;
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsUsing
        {
            get => _isUsing;
            set => SetProperty(ref _isUsing, value);
        }

        [JsonIgnore]
        private ModuleType moduleType = ModuleType.General;
        /// <summary>
        /// 模块类型
        /// </summary>
        public ModuleType ModuleType
        {
            get => moduleType;
            set => SetProperty(ref moduleType, value);
        }

        [JsonIgnore]
        private NodeType _nodeType;

        public NodeType NodeType
        {
            get => _nodeType;
            set => SetProperty(ref _nodeType, value);
        }

        [JsonIgnore]
        private int _rootSerial;
        /// <summary>
        /// 根节点序号
        /// </summary>
        public int RootSerial
        {
            get => _rootSerial;
            set => SetProperty(ref _rootSerial, value);
        }

        [JsonIgnore]
        private int _serial;
        public int Serial
        {
            get => _serial;
            set => SetProperty(ref _serial, value);
        }

        [JsonIgnore]
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        [JsonIgnore]
        private string _translateKey="";
        /// <summary>
        /// 翻译的Key
        /// </summary>
        public string TranslateKey
        {
            get => _translateKey;
            set => SetProperty(ref _translateKey, value);
        }

        [JsonIgnore]
        private string _icon;
        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        [JsonIgnore]
        private string _type;
        /// <summary>
        /// 如果是定制模块就从管理那获取
        /// </summary>
        public string Type 
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        [JsonIgnore]
        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        [JsonIgnore]
        private string _customDescription;
        /// <summary>
        /// 自定义描述
        /// </summary>
        public string CustomDescription
        {
            get => _customDescription;
            set => SetProperty(ref _customDescription, value);
        }


        [JsonIgnore]
        // 被标记的类的类型
        private Type _targetType;
        public Type TargetType
        {
            get => _targetType;
            set => SetProperty(ref _targetType, value);
        }
    }

    /// <summary>
    /// 模块类型
    /// </summary>
    public enum ModuleType
    {
        /// <summary>
        /// 通用组件
        /// </summary>
        General,

        /// <summary>
        /// 定制组件（定制组件只能够被）
        /// </summary>
        Custom,

    }

    /// <summary>
    /// 节点类型
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        /// 通用节点
        /// </summary>
        General,

        /// <summary>
        /// 监听节点
        /// </summary>
        Monitor,

        /// <summary>
        /// 开始节点
        /// </summary>
        Start,

        /// <summary>
        /// 结束节点
        /// </summary>
        Finish,

        /// <summary>
        /// 节点组
        /// </summary>
        Graph,

        /// <summary>
        /// 合并
        /// </summary>
        Merge,
    } 
}
