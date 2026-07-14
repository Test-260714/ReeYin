using HalconDotNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Extension
{
    public static class ObjectExtensions
    {

        // 缓存类型的属性和字段信息，提高性能
        private static readonly Dictionary<Type, MemberInfo[]> _typeMembersCache = new Dictionary<Type, MemberInfo[]>();
        // 缓存已处理的对象，避免循环引用导致的栈溢出
        private static readonly Dictionary<object, object> _processedObjects = new Dictionary<object, object>();

        /// <summary>
        /// 深拷贝对象的扩展方法
        /// </summary>
        public static T DeepCopy<T>(this T source)
        {
            if (EqualityComparer<T>.Default.Equals(source, default))
                return default;

            _processedObjects.Clear();
            return (T)CopyInternal(source);
        }

        private static object CopyInternal(object source)
        {
            if (source == null)
                return null;

            Type type = source.GetType();

            // 值类型和字符串直接返回
            if (type.IsValueType || type == typeof(string))
                return source;

            // 处理枚举
            if (type.IsEnum)
                return source;

            // 处理委托
            if (source is Delegate)
                return null;

            // 检查是否已处理过（避免循环引用）
            if (_processedObjects.TryGetValue(source, out object processed))
                return processed;

            // 处理数组
            if (type.IsArray)
            {
                Array sourceArray = (Array)source;
                Array newArray = Array.CreateInstance(
                    type.GetElementType(),
                    sourceArray.Length);

                _processedObjects[source] = newArray;

                for (int i = 0; i < sourceArray.Length; i++)
                {
                    newArray.SetValue(CopyInternal(sourceArray.GetValue(i)), i);
                }

                return newArray;
            }

            // 处理集合
            if (source is ICollection collection)
            {
                Type collectionType = type;
                Type elementType = GetCollectionElementType(type);

                // 创建新集合实例
                ICollection newCollection = (ICollection)Activator.CreateInstance(
                    collectionType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    null,
                    null);

                _processedObjects[source] = newCollection;

                // 添加元素
                foreach (object item in collection)
                {
                    MethodInfo addMethod = collectionType.GetMethod("Add");
                    addMethod.Invoke(newCollection, new[] { CopyInternal(item) });
                }

                return newCollection;
            }

            // 处理字典
            if (source is IDictionary dictionary)
            {
                Type dictionaryType = type;
                Type keyType = GetDictionaryKeyType(type);
                Type valueType = GetDictionaryValueType(type);

                // 创建新字典实例
                IDictionary newDictionary = (IDictionary)Activator.CreateInstance(
                    dictionaryType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    null,
                    null);

                _processedObjects[source] = newDictionary;

                // 添加键值对
                foreach (DictionaryEntry entry in dictionary)
                {
                    newDictionary[CopyInternal(entry.Key)] = CopyInternal(entry.Value);
                }

                return newDictionary;
            }

            // 处理自定义对象
            object newObject = Activator.CreateInstance(
                type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                null);

            _processedObjects[source] = newObject;

            // 获取所有属性和字段
            MemberInfo[] members = GetTypeMembers(type);

            foreach (MemberInfo member in members)
            {
                if (member is PropertyInfo property)
                {
                    if (property.CanRead && property.CanWrite)
                    {
                        object value = property.GetValue(source);
                        property.SetValue(newObject, CopyInternal(value));
                    }
                }
                else if (member is FieldInfo field)
                {
                    object value = field.GetValue(source);
                    field.SetValue(newObject, CopyInternal(value));
                }
            }

            return newObject;
        }

        private static MemberInfo[] GetTypeMembers(Type type)
        {
            if (!_typeMembersCache.TryGetValue(type, out MemberInfo[] members))
            {
                members = type.GetMembers(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m is PropertyInfo || m is FieldInfo)
                    .ToArray();

                _typeMembersCache[type] = members;
            }

            return members;
        }

        private static Type GetCollectionElementType(Type collectionType)
        {
            if (collectionType.IsGenericType &&
                collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return collectionType.GetGenericArguments()[0];
            }

            foreach (Type interfaceType in collectionType.GetInterfaces())
            {
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }

            return typeof(object);
        }

        private static Type GetDictionaryKeyType(Type dictionaryType)
        {
            if (dictionaryType.IsGenericType &&
                dictionaryType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return dictionaryType.GetGenericArguments()[0];
            }

            foreach (Type interfaceType in dictionaryType.GetInterfaces())
            {
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }

            return typeof(object);
        }

        private static Type GetDictionaryValueType(Type dictionaryType)
        {
            if (dictionaryType.IsGenericType &&
                dictionaryType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return dictionaryType.GetGenericArguments()[1];
            }

            foreach (Type interfaceType in dictionaryType.GetInterfaces())
            {
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return interfaceType.GetGenericArguments()[1];
                }
            }

            return typeof(object);
        }
    }
}
