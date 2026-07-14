using Newtonsoft.Json;
using Prism.Mvvm;
using ReeYin_V.Core.Cache;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace ReeYin_V.Core.Services.Language
{
    [ExposedService(Lifetime.Singleton, 4,typeof(ILanguageManager))]
    public class LanguageManager : ILanguageManager
    {
        #region Fields

        ResourceDictionary resourceDictionary = null;

        public LanguageManagerParam Param { get; set; }
        #endregion

        #region Properties
        private ICacheManager CacheManager { get; }

        private ResourceDictionary Resource { get; set; }
        /// <summary>
        /// 资源字典
        /// </summary>
        private Dictionary<string, string> LanguageResources { get; set; }

        private string LanguagePackDirectory { get; set; }


        #endregion

        #region Constructor
        public LanguageManager(ICacheManager cacheManager)
        {
            CacheManager = cacheManager;
            // 初始化语言包目录路径
            InitializeLanguagePackDirectory();
            resourceDictionary = Application.Current.Resources.MergedDictionaries[0];
            if (CacheManager.Get(CacheKey.Language, out LanguageManagerParam param))
            {
                Param = param;
                if (!Enum.TryParse<LanguageType>(Param.curLanguage.LanguageCode, out var LanguageType))
                {
                    Set(Param.curLanguage);
                }
                else
                {
                    Set(new LanguageItem
                    {
                        IsActive = true,
                        LanguageCode = LanguageType.ToString(),
                    });//设置默认语言为简体中文
                }
            }
            else
            {
                if (param == null)
                {
                    Param = new LanguageManagerParam();
                    Param.LanguageItems = new ObservableCollection<LanguageItem>()
                    {
                        new LanguageItem
                        {
                            DisplayName = "简体中文",
                            LanguageCode = "CN",
                            Icon = "\ue6e2",
                            Description = "简体中文语言包，适用于中国大陆地区",
                            IsActive = false,
                            Version = "1.0.0",
                            CompletionRate = 100.0,
                            SampleTexts = new LanguageSampleTexts
                            {
                                Confirm = "确认",
                                Cancel = "取消",
                                Save = "保存",
                                Delete = "删除",
                                Settings = "设置"
                            }
                        },
                        new LanguageItem
                        {
                            DisplayName = "English",
                            LanguageCode = "EN",
                            Icon = "\ue6e3",
                            Description = "English language pack for international users",
                            IsActive = false,
                            Version = "1.0.0",
                            CompletionRate = 100.0,
                            SampleTexts = new LanguageSampleTexts
                            {
                                Confirm = "Confirm",
                                Cancel = "Cancel",
                                Save = "Save",
                                Delete = "Delete",
                                Settings = "Settings"
                            }
                        },
                    };
                }

                Set(new LanguageItem
                {
                    IsActive = true,
                    LanguageCode = LanguageType.CN.ToString(),
                });//设置默认语言为简体中文
            }
        }
        #endregion


        /// <summary>
        /// 初始化语言包目录
        /// </summary>
        private void InitializeLanguagePackDirectory()
        {
            // 获取应用程序基础目录
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // 语言包目录：应用程序目录下的Languages文件夹
            LanguagePackDirectory = Path.Combine(baseDirectory, "Languages");

            // 如果目录不存在，尝试从Core项目目录加载（开发环境）
            if (!Directory.Exists(LanguagePackDirectory))
            {
                // 尝试从项目源码目录加载
                string projectPath = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\..\ReeYin-V.Core\Languages"));
                if (Directory.Exists(projectPath))
                {
                    LanguagePackDirectory = projectPath;
                }
            }
        }

        /// <summary>
        /// 索引器
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [JsonIgnore]
        public string this[string key]
        {
            get
            {
                // 优先从内存字典中查找
                if (LanguageResources != null && LanguageResources.ContainsKey(key))
                {
                    return LanguageResources[key];
                }

                // 兼容旧的ResourceDictionary方式
                if (Resource != null && Resource.Contains(key))
                {
                    return Resource[key].ToString();
                }

                // 未找到资源，返回键名本身
                return $"[{key}]";
            }
        }

        public void Set(LanguageItem language)
        {
            Assert.NotNull(language);
            language.IsActive = true;
            try
            {
                // 构建XML语言包文件路径
                string FilePath = "";

                if (language.FilePath != null && language.FilePath != "")
                {
                    FilePath = language.FilePath;
                }


                if (File.Exists(FilePath))
                {
                    // 从XML文件加载语言资源
                    LoadFromXmlFile(FilePath);
                }
                else
                {
                    // 如果XML文件不存在，尝试加载旧的XAML文件（向后兼容）
                    if (Enum.TryParse<LanguageType>(language.LanguageCode, out var LanguageType))
                    {
                        LoadFromXamlFile(LanguageType);
                    }
                }

                //PrismProvider.EventAggregator.GetEvent<SwitchLanguageEvent>().Publish();
                Param.curLanguage = language;
                CacheManager.Set(CacheKey.Language, Param);
            }

            catch (Exception ex)
            {
                Console.WriteLine($"加载语言包失败: {ex.Message}");
                Set(new LanguageItem
                {
                    IsActive = true,
                    LanguageCode = LanguageType.CN.ToString(),
                });//设置默认语言为简体中文
            }
        }

        /// <summary>
        /// 从XML文件加载语言包
        /// </summary>
        /// <param name="xmlFilePath">XML文件路径</param>
        public void LoadFromXmlFile(string xmlFilePath)
        {
            // 使用LanguagePackLoader加载XML语言包
            LanguageResources = LoadFromXml(xmlFilePath);

            // 创建新的ResourceDictionary并填充资源
            Resource = new ResourceDictionary();
            foreach (var kvp in LanguageResources)
            {
                Resource[kvp.Key] = kvp.Value;
            }

            // 更新WPF应用程序资源字典（保持兼容性）
            UpdateApplicationResources();

            Console.WriteLine($"成功从XML加载语言包: {xmlFilePath}，共 {LanguageResources.Count} 项资源");
        }

        /// <summary>
        /// 从XAML文件加载语言包（向后兼容）
        /// </summary>
        /// <param name="language">语言类型</param>
        public void LoadFromXamlFile(LanguageType language)
        {
            Assert.NotNull(language);

            // 获取XAML文件路径（旧方式）
            if (Application.Current.Resources.MergedDictionaries.Count > 0)
            {
                string path = resourceDictionary.Source.AbsolutePath;
                string uri = path.Remove(path.LastIndexOf("/"));
                string target = $"{uri}/{language}.xaml";

                Resource = (ResourceDictionary)Application.LoadComponent(new Uri(target, UriKind.RelativeOrAbsolute));

                // 同步到内存字典
                LanguageResources = new Dictionary<string, string>();
                foreach (var key in Resource.Keys)
                {
                    if (Resource[key] is string value)
                    {
                        LanguageResources[key.ToString()] = value;
                    }
                    else
                    {

                    }
                }

                UpdateApplicationResources();
                Console.WriteLine($"成功从XAML加载语言包: {target}");
            }
        }

        /// <summary>
        /// 更新WPF应用程序资源字典
        /// </summary>
        public void UpdateApplicationResources()
        {
            if (Application.Current != null && Resource != null)
            {
                // 移除旧的语言资源字典
                if (Application.Current.Resources.MergedDictionaries.Count > 0)
                {
                    Application.Current.Resources.MergedDictionaries.RemoveAt(0);
                }

                // 插入新的语言资源字典
                Application.Current.Resources.MergedDictionaries.Insert(0, Resource);
            }
        }

        /// <summary>
        /// 根据资源键查找字符串资源
        /// </summary>
        /// <param name="resourceKey">资源键</param>
        /// <returns>对应的字符串值，若未找到则返回 null</returns>
        public string GetStringResource(string resourceKey)
        {
            string value = this[resourceKey];

            if (!value.StartsWith("[") || !value.EndsWith("]"))
            {
                Console.WriteLine($"找到资源: {value}");
                return value;
            }
            else
            {
                Console.WriteLine($"未找到资源键: {resourceKey}");
                return this["NoTranslation"];
            }
        }

        /// <summary>
        /// 扫描指定目录中的语言包
        /// </summary>
        /// <param name="directory">要扫描的目录路径</param>
        /// <returns>找到的语言包元数据列表</returns>
        public List<LanguageItem> ScanLanguagePacks(string directory)
        {
            var languagePacks = new List<LanguageItem>();

            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"目录不存在: {directory}");
                return languagePacks;
            }

            try
            {
                // 查找所有XML文件
                string[] xmlFiles = Directory.GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly);

                foreach (string xmlFile in xmlFiles)
                {
                    try
                    {
                        // 尝试读取语言包元数据
                        var metadata = GetMetadata(xmlFile);

                        // 验证是否为有效的语言包
                        if (!string.IsNullOrEmpty(metadata.LanguageCode))
                        {
                            metadata.FilePath = xmlFile;
                            languagePacks.Add(metadata);
                            Console.WriteLine($"发现语言包: {metadata.DisplayName} ({metadata.LanguageCode}) - {xmlFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"读取语言包失败 {xmlFile}: {ex.Message}");
                    }
                }

                Console.WriteLine($"扫描完成，共发现 {languagePacks.Count} 个语言包");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描目录失败: {ex.Message}");
            }

            return languagePacks;
        }

        /// <summary>
        /// 从指定文件加载语言包
        /// </summary>
        /// <param name="filePath">语言包文件路径</param>
        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"语言包文件不存在: {filePath}");
            }

            try
            {
                // 验证文件扩展名
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".xml")
                {
                    LoadFromXmlFile(filePath);
                }
                else if (extension == ".xaml")
                {
                    // 暂不支持直接从XAML文件路径加载
                    throw new NotSupportedException("不支持直接从XAML文件加载，请使用XML格式的语言包");
                }
                else
                {
                    throw new NotSupportedException($"不支持的语言包格式: {extension}");
                }

                Console.WriteLine($"成功从文件加载语言包: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载语言包失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 设置自定义语言包目录
        /// </summary>
        /// <param name="directory">语言包目录路径</param>
        public void SetLanguagePackDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"目录不存在: {directory}");
            }

            LanguagePackDirectory = directory;
            Console.WriteLine($"语言包目录已设置为: {directory}");
        }

        /// <summary>
        /// 从XML文件加载语言包
        /// </summary>
        /// <param name="filePath">XML文件路径</param>
        /// <returns>语言资源字典</returns>
        public static Dictionary<string, string> LoadFromXml(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"语言包文件不存在: {filePath}");
            }

            var resources = new Dictionary<string, string>();

            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement root = doc.Root;

                if (root == null || root.Name != "LanguagePack")
                {
                    throw new InvalidOperationException("无效的语言包格式：根节点必须是LanguagePack");
                }

                // 读取Strings节点下的所有String元素
                XElement stringsElement = root.Element("Strings");
                if (stringsElement != null)
                {
                    foreach (XElement stringElement in stringsElement.Elements("String"))
                    {
                        string key = stringElement.Attribute("Key")?.Value;
                        string value = stringElement.Value;

                        if (!string.IsNullOrEmpty(key))
                        {
                            resources[key] = value ?? string.Empty;
                        }
                    }
                }

                return resources;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载语言包失败: {filePath}", ex);
            }
        }

        /// <summary>
        /// 获取语言包元数据
        /// </summary>
        /// <param name="filePath">XML文件路径</param>
        /// <returns>元数据信息</returns>
        public static LanguageItem GetMetadata(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"语言包文件不存在: {filePath}");
            }

            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement root = doc.Root;
                XElement metadata = root?.Element("Metadata");

                if (metadata == null)
                {
                    return new LanguageItem();
                }

                return new LanguageItem
                {
                    DisplayName = metadata.Element("Name")?.Value ?? string.Empty,
                    LanguageCode = metadata.Element("Code")?.Value ?? string.Empty,
                    Version = metadata.Element("Version")?.Value ?? "1.0"
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"读取语言包元数据失败: {filePath}", ex);
            }
        }
    }

    /// <summary>
    /// 参数
    /// </summary>
    [Serializable]
    public class LanguageManagerParam
    {
        #region Fields
        public LanguageItem curLanguage { get; set; } = new LanguageItem();

        public ObservableCollection<LanguageItem> LanguageItems { get; set; }
        #endregion

        #region Constructor
        public LanguageManagerParam()
        {

        }
        #endregion

        #region Methods
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (LanguageItems == null)
            {
                LanguageItems = new ObservableCollection<LanguageItem>()
                {
                    new LanguageItem
                    {
                        DisplayName = "简体中文",
                        LanguageCode = "CN",
                        Icon = "\ue6e2",
                        Description = "简体中文语言包，适用于中国大陆地区",
                        IsActive = false,
                        Version = "1.0.0",
                        CompletionRate = 100.0,
                        SampleTexts = new LanguageSampleTexts
                        {
                            Confirm = "确认",
                            Cancel = "取消",
                            Save = "保存",
                            Delete = "删除",
                            Settings = "设置"
                        }
                    },
                    new LanguageItem
                    {
                        DisplayName = "English",
                        LanguageCode = "EN",
                        Icon = "\ue6e3",
                        Description = "English language pack for international users",
                        IsActive = false,
                        Version = "1.0.0",
                        CompletionRate = 100.0,
                        SampleTexts = new LanguageSampleTexts
                        {
                            Confirm = "Confirm",
                            Cancel = "Cancel",
                            Save = "Save",
                            Delete = "Delete",
                            Settings = "Settings"
                        }
                    },
                };
            }

        }
        #endregion
    }

    #region 语言配置项模型
    /// <summary>
    /// 语言配置项
    /// </summary>
    public class LanguageItem : BindableBase
    {
        private string _displayName;
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        private string _languageCode;
        /// <summary>
        /// 语言代码 (如: zh-CN, en-US)
        /// </summary>
        public string LanguageCode
        {
            get => _languageCode;
            set => SetProperty(ref _languageCode, value);
        }

        private string _icon;
        /// <summary>
        /// 图标
        /// </summary>
        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        private string _description;
        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _filePath;
        /// <summary>
        /// 路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        private bool _isActive;
        /// <summary>
        /// 是否为当前激活的语言
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private string _version;
        /// <summary>
        /// 语言包版本
        /// </summary>
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private DateTime _lastUpdated;
        /// <summary>
        /// 最后更新日期
        /// </summary>
        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        private double _completionRate;
        /// <summary>
        /// 翻译完成度 (0-100)
        /// </summary>
        public double CompletionRate
        {
            get => _completionRate;
            set => SetProperty(ref _completionRate, value);
        }

        private LanguageSampleTexts _sampleTexts;
        /// <summary>
        /// 示例文本
        /// </summary>
        public LanguageSampleTexts SampleTexts
        {
            get => _sampleTexts;
            set => SetProperty(ref _sampleTexts, value);
        }
    }

    /// <summary>
    /// 语言示例文本
    /// </summary>
    public class LanguageSampleTexts : BindableBase
    {
        private string _confirm;
        public string Confirm
        {
            get => _confirm;
            set => SetProperty(ref _confirm, value);
        }

        private string _cancel;
        public string Cancel
        {
            get => _cancel;
            set => SetProperty(ref _cancel, value);
        }

        private string _save;
        public string Save
        {
            get => _save;
            set => SetProperty(ref _save, value);
        }

        private string _delete;
        public string Delete
        {
            get => _delete;
            set => SetProperty(ref _delete, value);
        }

        private string _settings;
        public string Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }
    }
    #endregion
}
