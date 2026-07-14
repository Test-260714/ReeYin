using ReeYin_V.Core.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Language
{
    public interface ILanguageManager
    {
        LanguageManagerParam Param { get; set; }

        string this[string key] { get; }

        /// <summary>
        /// 设置语言
        /// </summary>
        /// <param name="type"></param>
        void Set(LanguageItem Language);

        /// <summary>
        /// 从Xml文件中加载
        /// </summary>
        /// <param name="xmlFilePath"></param>
        void LoadFromXmlFile(string xmlFilePath);

        /// <summary>
        /// 查找指定资源的翻译
        /// </summary>
        /// <param name="resourceKey"></param>
        /// <returns></returns>
        string GetStringResource(string resourceKey);

        void LoadFromFile(string filePath);

        List<LanguageItem> ScanLanguagePacks(string directory);
    }
}
