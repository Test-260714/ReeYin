using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Services.Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ReeYin_V.Core.Helper.ImageOP
{
    /// <summary>
    /// 处理节点参数边界上可能包含 HALCON 对象的复杂对象图。
    /// </summary>
    /// <remarks>
    /// NodeViewModel 负责决定参数什么时候在节点之间流转；
    /// 本类只负责 TransmitParam、Result、Dictionary、List 内部 HALCON 对象的复制和释放策略。
    /// </remarks>
    public static class HalconTransmitParamOwnership
    {
        /// <summary>
        /// 克隆节点间传递的参数字典，为下游节点创建独立的参数对象。
        /// </summary>
        public static Dictionary<string, object> CloneTransmitParams(Dictionary<string, object> source)
        {
            if (source == null)
            {
                return new Dictionary<string, object>();
            }

            var cloned = new Dictionary<string, object>(source.Count, source.Comparer);
            foreach (KeyValuePair<string, object> kv in source)
            {
                cloned[kv.Key] = CloneTransmitParamValue(kv.Value);
            }

            return cloned;
        }

        /// <summary>
        /// 克隆单个 TransmitParam 或普通边界值，供节点输出向下游传播时使用。
        /// </summary>
        public static object CloneTransmitParamValue(object value)
        {
            return value is TransmitParam param
                ? CloneTransmitParam(param)
                : CloneBoundaryValue(value);
        }

        /// <summary>
        /// 为参数读取场景创建隔离副本，避免调用方直接拿到全局参数或上游缓存中的共享对象。
        /// </summary>
        public static object CloneValueForParameterIsolation(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is HImage hImage)
            {
                return HalconImageOwnership.CopyBorrowedOrNull(hImage);
            }

            if (value is HObject hObject)
            {
                return HalconImageOwnership.CopyBorrowedObjectOrNull(hObject);
            }

            return value.DeepClone();
        }

        /// <summary>
        /// 递归释放当前调用方持有的参数对象图，通常用于替换节点输入缓存前的清理。
        /// </summary>
        public static void DisposeOwnedTransmitValue(object value)
        {
            DisposeOwnedTransmitValue(value, new HashSet<object>(ObjectReferenceEqualityComparer.Instance));
        }

        /// <summary>
        /// 克隆 TransmitParam 的元信息，并单独处理 Value 内可能存在的 HALCON 对象。
        /// </summary>
        private static TransmitParam CloneTransmitParam(TransmitParam source)
        {
            if (source == null)
            {
                return null;
            }

            return new TransmitParam
            {
                IsLink = source.IsLink,
                LinkGuid = source.LinkGuid,
                Serial = source.Serial,
                ParentNode = source.ParentNode,
                Guid = source.Guid,
                Resourece = source.Resourece,
                Name = source.Name,
                ParamName = source.ParamName,
                Type = source.Type,
                Value = CloneBoundaryValue(source.Value),
                Describe = source.Describe,
                IsGlobal = source.IsGlobal,
                ResourcePath = source.ResourcePath
            };
        }

        private static object CloneBoundaryValue(object value)
        {
            // HALCON 对象不能依赖普通序列化深拷贝，必须显式 CopyObj/CopyImage 后交给下游节点持有。
            if (value is HImage hImage)
            {
                return HalconImageOwnership.CopyBorrowedOrNull(hImage);
            }

            if (value is HObject hObject)
            {
                return HalconImageOwnership.CopyBorrowedObjectOrNull(hObject);
            }

            if (value is Result result)
            {
                return CloneResult(result);
            }

            // 深度学习结果会把图像、区域、附加数据塞进字典或列表，这里逐层保护 HALCON 对象。
            if (value is Dictionary<string, object> stringObjectDictionary)
            {
                return CloneStringObjectDictionary(stringObjectDictionary);
            }

            if (value is IEnumerable<Result> resultList)
            {
                return resultList
                    .Where(item => item != null)
                    .Select(CloneResult)
                    .ToList();
            }

            if (value is IEnumerable<Dictionary<string, object>> dictionaryList)
            {
                return dictionaryList
                    .Where(item => item != null)
                    .Select(CloneStringObjectDictionary)
                    .ToList();
            }

            if (value is IEnumerable<HImage> imageList)
            {
                return imageList
                    .Select(HalconImageOwnership.CopyBorrowedOrNull)
                    .Where(item => item != null)
                    .ToList();
            }

            return value;
        }

        private static Dictionary<string, object> CloneStringObjectDictionary(Dictionary<string, object> source)
        {
            if (source == null)
            {
                return null;
            }

            var cloned = new Dictionary<string, object>(source.Count, source.Comparer);
            foreach (KeyValuePair<string, object> kv in source)
            {
                cloned[kv.Key] = CloneBoundaryValue(kv.Value);
            }

            return cloned;
        }

        private static Result CloneResult(Result source)
        {
            if (source == null)
            {
                return null;
            }

            return new Result
            {
                Cx = source.Cx,
                Cy = source.Cy,
                Width = source.Width,
                Height = source.Height,
                Angle = source.Angle,
                Confidence = source.Confidence,
                ClassId = source.ClassId,
                ClassName = source.ClassName,
                Kpt = source.Kpt,
                // Seg 可能是分割区域或轮廓对象，保持 HObject 类型，不强转为 HImage。
                Seg = HalconImageOwnership.CopyBorrowedObjectOrNull(source.Seg) ?? new HObject(),
                ModelType = source.ModelType,
                Others = CloneStringObjectDictionary(source.Others)
                    ?? new Dictionary<string, object>()
            };
        }

        private static void DisposeOwnedTransmitValue(object value, HashSet<object> visited)
        {
            try
            {
                if (value == null || value is string)
                {
                    return;
                }

                Type valueType = value.GetType();
                if (!valueType.IsValueType && !visited.Add(value))
                {
                    // 参数对象图可能出现重复引用，visited 用于避免重复释放同一个 HALCON 句柄。
                    return;
                }

                switch (value)
                {
                    case TransmitParam param:
                        DisposeOwnedTransmitValue(param.Value, visited);
                        param.Value = null;
                        break;

                    case HImage image:
                        HalconImageOwnership.DisposeOwned(image);
                        break;

                    case HObject hObject:
                        HalconImageOwnership.DisposeOwned(hObject);
                        break;

                    case Result result:
                        DisposeOwnedTransmitValue(result.Seg, visited);
                        result.Seg = null;
                        DisposeOwnedTransmitValue(result.Others, visited);
                        result.Others = null;
                        break;

                    case IDictionary dictionary:
                        foreach (object item in dictionary.Values)
                        {
                            DisposeOwnedTransmitValue(item, visited);
                        }
                        break;

                    case IEnumerable enumerable:
                        foreach (object item in enumerable)
                        {
                            DisposeOwnedTransmitValue(item, visited);
                        }
                        break;
                }
            }
            catch
            {
                // 参数清理不能反向打断节点运行；具体异常在上层流程日志中定位。
            }
        }

        /// <summary>
        /// 按对象引用比较，用于识别对象图中的重复引用，避免递归释放时误判为值相等。
        /// </summary>
        private sealed class ObjectReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ObjectReferenceEqualityComparer Instance = new ObjectReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
