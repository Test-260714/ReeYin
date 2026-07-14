using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ComTool.General.Communacation
{
    public class SerHelper
    {
        public SerHelper()
        {
        }

        public static byte[] Serialize(object obj)
        {
            //BinaryFormatter binaryFormatter = new BinaryFormatter();
            //byte[] result;
            //using (MemoryStream memoryStream = new MemoryStream())
            //{
            //    binaryFormatter.Serialize(memoryStream, obj);
            //    result = memoryStream.ToArray();
            //}
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }

        public static T Deserialize<T>(byte[] buffer)
        {
            //BinaryFormatter binaryFormatter = new BinaryFormatter();
            //T result;
            //using (MemoryStream memoryStream = new MemoryStream(buffer))
            //{
            //    result = (T)((object)binaryFormatter.Deserialize(memoryStream));
            //}
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(buffer));
        }

        public static object Deserialize(byte[] datas, int index)
        {
            //BinaryFormatter binaryFormatter = new BinaryFormatter();
            //MemoryStream memoryStream = new MemoryStream(datas, index, datas.Length - index);
            //object result = binaryFormatter.Deserialize(memoryStream);
            //memoryStream.Dispose();
            return JsonConvert.DeserializeObject<object>(Encoding.UTF8.GetString(datas, index, datas.Length - index));
        }
    }
}
