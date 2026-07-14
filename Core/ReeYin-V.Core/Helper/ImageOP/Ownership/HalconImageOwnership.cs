using HalconDotNet;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Helper.ImageOP
{
    /// <summary>
    /// 统一处理流程节点边界上的 HALCON 对象复制、校验和释放。
    /// </summary>
    /// <remarks>
    /// 本类不全局追踪对象所有权，也不会主动接管对象生命周期。
    /// 调用方必须明确对象是“借用”还是“自有”；方法名带 Owned 的接口可能释放入参，
    /// 不要传入 InputParams、NodesOutputCache、全局参数或 UI 缓存中的共享对象。
    /// </remarks>
    public static class HalconImageOwnership
    {
        /// <summary>
        /// 安全判断 HALCON 包装对象是否初始化，避免 IsInitialized 抛出的异常继续向外扩散。
        /// </summary>
        public static bool IsInitializedSafe(HObject value)
        {
            try
            {
                return value != null && value.IsInitialized();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全读取图像尺寸；比 IsInitializedSafe 更适合判断对象是否真的是可用图像。
        /// </summary>
        public static bool TryGetImageSize(HObject value, out int width, out int height, out string error)
        {
            width = 0;
            height = 0;
            error = string.Empty;

            if (!IsInitializedSafe(value))
            {
                error = "image is not initialized";
                return false;
            }

            try
            {
                HOperatorSet.GetImageSize(value, out HTuple widthTuple, out HTuple heightTuple);
                if (widthTuple.Length == 0 || heightTuple.Length == 0)
                {
                    error = "image has no size";
                    return false;
                }

                width = widthTuple.I;
                height = heightTuple.I;
                if (width <= 0 || height <= 0)
                {
                    error = $"invalid image size: {width}x{height}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryGetImageSize(HObject value, out double width, out double height, out string error)
        {
            width = 0.0;
            height = 0.0;

            if (!TryGetImageSize(value, out int intWidth, out int intHeight, out error))
                return false;

            width = intWidth;
            height = intHeight;
            return true;
        }

        /// <summary>
        /// 将借用的 HImage 拷贝成当前调用方自有的 HImage，源图像绝不释放。
        /// </summary>
        public static bool TryCopyBorrowed(HImage borrowed, out HImage ownedCopy)
        {
            ownedCopy = null;
            try
            {
                if (!IsInitializedSafe(borrowed))
                    return false;

                ownedCopy = borrowed.CopyImage();
                if (IsInitializedSafe(ownedCopy))
                    return true;

                DisposeOwned(ownedCopy);
                ownedCopy = null;
                return false;
            }
            catch
            {
                DisposeOwned(ownedCopy);
                ownedCopy = null;
                return false;
            }
        }

        /// <summary>
        /// 将借用的图像型 HObject 拷贝成自有 HImage，源对象绝不释放。
        /// </summary>
        /// <remarks>
        /// 只用于图像对象；Region、XLD 或需要保持 HObject 类型的对象应使用 TryCopyBorrowedObject。
        /// </remarks>
        public static bool TryCopyBorrowed(HObject borrowed, out HImage ownedCopy)
        {
            ownedCopy = null;

            if (borrowed is HImage borrowedImage)
                return TryCopyBorrowed(borrowedImage, out ownedCopy);

            return TryCopyBorrowed(borrowed, 1, out ownedCopy);
        }

        /// <summary>
        /// 从借用的 HALCON 对象元组中选取一个图像对象，并拷贝成自有 HImage。
        /// </summary>
        public static bool TryCopyBorrowed(HObject borrowed, int index, out HImage ownedCopy)
        {
            ownedCopy = null;
            HObject selectedObject = null;
            HImage selectedImage = null;

            try
            {
                if (!IsInitializedSafe(borrowed) || index <= 0)
                    return false;

                // HALCON 对象下标从 1 开始，先 SelectObj 再 CopyImage，避免下游持有共享对象句柄。
                HOperatorSet.SelectObj(borrowed, out selectedObject, index);
                selectedImage = new HImage(selectedObject);
                return TryCopyBorrowed(selectedImage, out ownedCopy);
            }
            catch
            {
                DisposeOwned(ownedCopy);
                ownedCopy = null;
                return false;
            }
            finally
            {
                DisposeOwned(selectedImage);
                DisposeOwned(selectedObject);
            }
        }

        /// <summary>
        /// 将借用的 HALCON 对象拷贝成自有 HObject，源对象绝不释放。
        /// </summary>
        /// <remarks>
        /// 这是 Region、XLD、对象元组场景的拷贝入口，会保持 HObject 类型不强转成 HImage。
        /// </remarks>
        public static bool TryCopyBorrowedObject(HObject borrowed, out HObject ownedCopy)
        {
            ownedCopy = null;
            try
            {
                if (!IsInitializedSafe(borrowed))
                    return false;

                HOperatorSet.CopyObj(borrowed, out ownedCopy, 1, -1);
                if (IsInitializedSafe(ownedCopy))
                    return true;

                DisposeOwned(ownedCopy);
                ownedCopy = null;
                return false;
            }
            catch
            {
                DisposeOwned(ownedCopy);
                ownedCopy = null;
                return false;
            }
        }

        /// <summary>
        /// 将当前调用方自有的图像型 HObject 拷贝成 HImage，并释放传入的原对象。
        /// </summary>
        /// <remarks>
        /// 只允许传入当前调用方刚创建的对象，典型来源是 HALCON 算子 out 参数。
        /// 不要传入 InputParams、NodesOutputCache、全局参数或 UI 缓存中的借用对象。
        /// </remarks>
        public static bool TryCopyOwnedAndDispose(HObject owned, out HImage ownedCopy)
        {
            try
            {
                return TryCopyBorrowed(owned, out ownedCopy);
            }
            finally
            {
                DisposeOwned(owned);
            }
        }

        /// <summary>
        /// 将当前调用方自有的 HALCON 对象拷贝成新的 HObject，并释放传入的原对象。
        /// </summary>
        /// <remarks>
        /// 只允许传入当前调用方刚创建的对象，典型来源是 HALCON 算子 out 参数。
        /// 不要传入 InputParams、NodesOutputCache、全局参数或 UI 缓存中的借用对象。
        /// </remarks>
        public static bool TryCopyOwnedObjectAndDispose(HObject owned, out HObject ownedCopy)
        {
            try
            {
                return TryCopyBorrowedObject(owned, out ownedCopy);
            }
            finally
            {
                DisposeOwned(owned);
            }
        }

        /// <summary>
        /// 将借用的 HImage 拷贝成自有 HImage，失败时返回 null。
        /// </summary>
        public static HImage CopyBorrowedOrNull(HImage borrowed)
        {
            return TryCopyBorrowed(borrowed, out HImage ownedCopy)
                ? ownedCopy
                : null;
        }

        /// <summary>
        /// 将借用的图像型 HObject 拷贝成自有 HImage，失败时返回 null。
        /// </summary>
        public static HImage CopyBorrowedOrNull(HObject borrowed)
        {
            return TryCopyBorrowed(borrowed, out HImage ownedCopy)
                ? ownedCopy
                : null;
        }

        /// <summary>
        /// 将借用的 HALCON 对象拷贝成自有 HObject，失败时返回 null。
        /// </summary>
        public static HObject CopyBorrowedObjectOrNull(HObject borrowed)
        {
            return TryCopyBorrowedObject(borrowed, out HObject ownedCopy)
                ? ownedCopy
                : null;
        }

        /// <summary>
        /// 兼容旧命名：将自有图像型 HObject 拷贝成 HImage，并释放入参。
        /// </summary>
        /// <remarks>
        /// 新代码优先使用语义更明确的 CopyOwnedImageObjectAndDisposeOrNull。
        /// </remarks>
        public static HImage CopyOwnedObjectOrNull(HObject owned)
        {
            return TryCopyOwnedAndDispose(owned, out HImage ownedCopy)
                ? ownedCopy
                : null;
        }

        /// <summary>
        /// 将自有的图像型 HObject 拷贝成 HImage，并释放传入对象。
        /// </summary>
        public static HImage CopyOwnedImageObjectAndDisposeOrNull(HObject owned)
        {
            return CopyOwnedObjectOrNull(owned);
        }

        /// <summary>
        /// 将自有的 HALCON 对象拷贝成新的 HObject，并释放传入对象。
        /// </summary>
        public static HObject CopyOwnedObjectAndDisposeOrNull(HObject owned)
        {
            return TryCopyOwnedObjectAndDispose(owned, out HObject ownedCopy)
                ? ownedCopy
                : null;
        }

        /// <summary>
        /// 当同一张自有 HImage 还需要交给其他持有方时，创建新的自有副本。
        /// </summary>
        public static HImage CopyOwnedOrNull(HImage owned)
        {
            return CopyBorrowedOrNull(owned);
        }

        /// <summary>
        /// 为 OutputParams 或下游节点发布创建稳定图像副本，避免节点间共享同一个 HALCON 句柄。
        /// </summary>
        public static HImage CopyForOutput(HImage source)
        {
            return CopyBorrowedOrNull(source);
        }

        /// <summary>
        /// 替换自有 HImage 字段，并释放旧对象，常用于模型内部缓存图像更新。
        /// </summary>
        public static void ReplaceOwned(ref HImage target, HImage nextOwned)
        {
            HImage old = target;
            target = nextOwned;

            if (!ReferenceEquals(old, nextOwned))
                DisposeOwned(old);
        }

        /// <summary>
        /// 释放自有 HImage 字段，并将引用重置为 null。
        /// </summary>
        public static void DisposeOwned(ref HImage owned)
        {
            HImage current = owned;
            owned = null;
            DisposeOwned(current);
        }

        /// <summary>
        /// 释放自有 HALCON 对象，并把释放异常交给调用方分类处理。
        /// </summary>
        public static bool TryDisposeOwned(HObject owned, out Exception exception)
        {
            exception = null;

            try
            {
                owned?.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        /// <summary>
        /// 尽力释放自有 HALCON 对象；如果调用方需要区分异常类型，请使用 TryDisposeOwned。
        /// </summary>
        public static void DisposeOwned(HObject owned)
        {
            TryDisposeOwned(owned, out _);
        }

        /// <summary>
        /// 判断是否为 HALCON 常见的“对象已被删除”清理异常，通常对应错误码 #4051。
        /// </summary>
        public static bool IsDeletedObjectError(Exception ex)
        {
            string message = ex?.Message ?? string.Empty;
            return message.Contains("#4051", StringComparison.Ordinal)
                || message.Contains("object has been deleted already", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 批量释放自有 HImage 列表。
        /// </summary>
        public static void DisposeOwnedList(IEnumerable<HImage> ownedImages)
        {
            if (ownedImages == null)  return;

            foreach (HImage image in ownedImages) DisposeOwned(image);
        }
    }
}
