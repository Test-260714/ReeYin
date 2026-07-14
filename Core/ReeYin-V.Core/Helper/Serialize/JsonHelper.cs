using HalconDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ReeYin_V.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace ReeYin_V.Core.Helper
{
    /// <summary>
    /// Json文件的序列化和反序列化帮助类
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// 加载某个JSON文件并返回指定的类型实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static T Load<T>(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return default(T);
            }

            string content = File.ReadAllText(fileName);
            return LoadDeserialize<T>(content);
        }

        public static T LoadDeserialize<T>(string content)
        {
            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None, // 关闭引用保留
                //ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // 忽略可能的循环引用
                TypeNameHandling = TypeNameHandling.All,
                ContractResolver = JsonCompatibilityContractResolver.Instance,

            };
            return JsonConvert.DeserializeObject<T>(content, settings);
        }

        public static T Deserialize<T>(string content)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ContractResolver = JsonCompatibilityContractResolver.Instance,

            };
            return JsonConvert.DeserializeObject<T>(content, settings);
        }

        /// <summary>
        /// 将指定的类型实例保存为JSON文件
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="fileName"></param>
        /// <param name="indented"></param>
        public static void Save(object obj, string fileName, bool indented = false)
        {
            string content = SerializeIgnoreImg(obj, indented);
            File.WriteAllText(fileName, content);
        }

        public static string SerializeIgnoreImg(object obj, bool indented = false)
        {
            var settings = new JsonSerializerSettings
            {
                // 关闭引用保留，这是解决此问题的关键
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,                     // 忽略默认值
                //PreserveReferencesHandling = PreserveReferencesHandling.All,
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                Converters = { new HalconSkipConverter() } // 注册自定义转换器
            };

            return JsonConvert.SerializeObject(obj, settings);

        }

        public static string Serialize(object obj, bool indented = false)
        {
            var settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,                     // 忽略默认值
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                Formatting = indented ? Formatting.Indented : Formatting.None
            };

            return JsonConvert.SerializeObject(obj, settings);

        }

        public static T DeepClone<T>(this T obj) where T : class
        {
            string json = Serialize(obj);
            return Deserialize<T>(json);
        }

        ///// <summary>
        ///// 将泛型对象转成json格式并存入文件
        ///// </summary>
        ///// <typeparam name="T">对象类型</typeparam>
        ///// <param name="obj">要序列化的对象</param>
        ///// <param name="path">保存路径</param>
        ///// <param name="typeNameHandling">类型名称处理方式</param>
        //public static void JsonObjectSerialize<T>(T obj, string path, TypeNameHandling typeNameHandling = TypeNameHandling.None)
        //{
        //    // 验证参数
        //    if (string.IsNullOrEmpty(path))
        //        throw new ArgumentNullException(nameof(path), "文件路径不能为空");

        //    // 确保目录存在
        //    DirectoryInfo directoryInfo = Directory.GetParent(path);
        //    if (directoryInfo != null && !Directory.Exists(directoryInfo.FullName))
        //    {
        //        Directory.CreateDirectory(directoryInfo.FullName);
        //    }

        //    // 配置JSON序列化设置
        //    var settings = new JsonSerializerSettings
        //    {
        //        PreserveReferencesHandling = PreserveReferencesHandling.All,
        //        Formatting = Formatting.Indented,
        //        TypeNameHandling = typeNameHandling,
        //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore // 建议添加循环引用处理
        //    };

        //    // 序列化并写入文件
        //    string jsonString = JsonConvert.SerializeObject(obj, typeof(T), settings);
        //    File.WriteAllText(path, jsonString);
        //}

        /// <summary>
        /// 将对象转成json格式并存入文件
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="path">路径</param>
        public static void JsonObjectSerialize(object obj, string path, TypeNameHandling typeNameHandling = TypeNameHandling.None)
        {
            DirectoryInfo directoryInfo = Directory.GetParent(path);
            if (!Directory.Exists(directoryInfo.FullName)) Directory.CreateDirectory(directoryInfo.FullName);

            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                Formatting = Formatting.Indented,
                //ReferenceLoopHandling = ReferenceLoopHandling.Serialize,

                TypeNameHandling = typeNameHandling,
                Converters = { new HalconSkipConverter() } // 注册自定义转换器
            };
            //JsonSerializerSettings settings = new JsonSerializerSettings();
            //settings.Formatting = Formatting.Indented;
            //settings.TypeNameHandling = typeNameHandling;
            string jsonString = JsonConvert.SerializeObject(obj, settings);
            // 将 JSON 字符串写入文件
            File.WriteAllText(path, jsonString);
        }

        /// <summary>
        /// 将json文件转成指定对象
        /// </summary>
        /// <typeparam name="T">指定对象</typeparam>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public static T JsonDisObjectSerialize<T>(string path, out string JsonStr, TypeNameHandling typeNameHandling = TypeNameHandling.None)
        {
            if (File.Exists(path))
            {
                string jsonString = File.ReadAllText(path);
                // 反序列化时的配置（必须与序列化时一致）
                var deserializeSettings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.All,
                    Formatting = Formatting.Indented,
                    //ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    TypeNameHandling = typeNameHandling,
                    ContractResolver = JsonCompatibilityContractResolver.Instance,
                    Error = (sender, args) =>
                    {
                        // 找到出错的属性
                        var member = args.ErrorContext.Member;
                        string jsonPath = args.ErrorContext.Path;
                        Exception error = args.ErrorContext.Error;

                        //if (member == "Data")
                        //{
                        //    // 出错属性强制使用新对象
                        //    ((JsonObjectContract)args.ErrorContext.OriginalObject).Properties
                        //        .GetClosestMatchProperty("Data")
                        //        .ValueProvider.SetValue(args.ErrorContext.OriginalObject, new SubConfig());
                        //}
                        bool shouldRethrow = JsonDeserializationCompatibility.ShouldRethrow(jsonPath, member, error);
                        string logMessage =
                            $"Json反序列化{(shouldRethrow ? "关键失败" : "兼容跳过")}：" +
                            $"File={path}, TargetType={typeof(T).FullName}, JsonPath={jsonPath}, " +
                            $"Member={member}, ErrorType={error?.GetType().FullName}, Error={error?.Message}";

                        Console.WriteLine(logMessage);
                        if (shouldRethrow)
                        {
                            Logs.LogError(logMessage);
                        }
                        else
                        {
                            Logs.LogWarning(logMessage);
                        }

                        args.ErrorContext.Handled = !shouldRethrow;
                    }
                };
                T person = JsonConvert.DeserializeObject<T>(jsonString, deserializeSettings);
                JsonStr = jsonString;
                return person;
            }
            JsonStr = "";
            return default;
        }

        /// <summary>
        /// 将list集合转成json格式并写入文件
        /// </summary>
        /// <param name="obj">list集合</param>
        /// <param name="path">路径</param>
        public static void ListToJsonFile(object obj, string path)
        {
            DirectoryInfo directoryInfo = Directory.GetParent(path);
            if (!Directory.Exists(directoryInfo.FullName)) Directory.CreateDirectory(directoryInfo.FullName);
            // 将 List 集合转换为 JSON 字符串
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);

            // 输出 JSON 以验证内容
            Console.WriteLine(json);

            // 将 JSON 字符串写入文件
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// 将json文件转成list集合
        /// </summary>
        /// <typeparam name="T">list集合里面的对象</typeparam>
        /// <param name="path">路径</param>
        /// <returns></returns>
        public static List<T> JsonFileToList<T>(string path)
        {
            if (!File.Exists(path))
            {
                return default;
            }
            // 读取文件内容
            string jsonData = File.ReadAllText(path);

            // 将 JSON 字符串转换为 List<T>
            List<T> people = JsonConvert.DeserializeObject<List<T>>(jsonData);
            return people;
        }

        /// <summary>
        /// 将对象转成JSON字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string JsonSerializeObject(object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (Exception)
            {

                return " ";
            }

        }

        /// <summary>
        /// 将json字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        public static T DisObjectSerialize<T>(string str)
        {
            return JsonConvert.DeserializeObject<T>(str);
        }
    }

    /// <summary>
    /// 自定义JSON转换器：跳过HImage类型的序列化
    /// </summary>
    public class HImageSkipConverter : JsonConverter
    {
        /// <summary>
        /// 确定当前转换器是否可以处理指定类型
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            // 只处理HImage类型（包括派生类型）
            return typeof(HImage).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// 序列化时调用：返回null表示跳过该对象
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // 对HImage对象写入null（或不写入任何内容）
            writer.WriteNull();
        }

        /// <summary>
        /// 反序列化时调用（当前场景无需实现，返回null即可）
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // 反序列化时返回null（或根据需求处理）
            return null;
        }
    }

    /// <summary>
    /// 自定义JSON转换器：跳过所有HalconDotNet命名空间下的类型的序列化
    /// </summary>
    public class HalconSkipConverter : JsonConverter
    {
        /// <summary>
        /// 确定当前转换器是否可以处理指定类型
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            // 检查类型是否属于 HalconDotNet 命名空间
            // IsAssignableFrom 也可以用于基类检查，但这里主要用 FullName/NS 检查更直接
            // 注意：objectType.Namespace 可能为 null（例如匿名类型或泛型参数）
            return objectType?.Namespace?.StartsWith("HalconDotNet", StringComparison.Ordinal) == true;
        }

        /// <summary>
        /// 序列化时调用：返回null表示跳过该对象
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // 对匹配的类型对象写入null（或不写入任何内容）
            writer.WriteNull();
        }

        /// <summary>
        /// 反序列化时调用（当前场景无需实现，返回null即可）
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // 反序列化时返回null（或根据需求处理）
            return null;
        }
    }


}
