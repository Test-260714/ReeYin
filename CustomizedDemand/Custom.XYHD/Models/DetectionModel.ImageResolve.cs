using HalconDotNet;
using Custom.XYHD.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Logger;
using ReeYin_V.Share;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Models
{
    public partial class DetectionModel
    {
        private static HImage TryExtractHImage(object input)
        {
            if (input == null)
                return null;

            if (input is HImage hImage && hImage.IsInitialized())
                return hImage;

            if (input is HObject hObject && hObject.IsInitialized())
            {
                try
                {
                    return new HImage(hObject);
                }
                catch
                {
                    return null;
                }
            }

            if (input is Array array && array.Length > 0)
                return TryExtractHImage(array.GetValue(0));

            if (input is IList list && list.Count > 0)
                return TryExtractHImage(list[0]);

            return null;
        }

        private static HImage TryExtractHImageAt(object input, int imageIndex)
        {
            if (input == null || input is string)
                return null;

            if (input is HImage hImage && hImage.IsInitialized())
                return hImage.CopyImage();

            if (input is HObject hObject && hObject.IsInitialized())
                return TryExtractHImageFromHObjectAt(hObject, imageIndex);

            if (input is Array array)
            {
                if (array.Length == 0)
                    return null;

                int safeIndex = Math.Clamp(imageIndex, 0, array.Length - 1);
                return TryExtractHImageAt(array.GetValue(safeIndex), 0);
            }

            if (input is IList list)
            {
                if (list.Count == 0)
                    return null;

                int safeIndex = Math.Clamp(imageIndex, 0, list.Count - 1);
                return TryExtractHImageAt(list[safeIndex], 0);
            }

            if (input is IEnumerable enumerable)
            {
                int safeIndex = Math.Max(0, imageIndex);
                int index = 0;
                object last = null;
                foreach (object item in enumerable)
                {
                    last = item;
                    if (index == safeIndex)
                        return TryExtractHImageAt(item, 0);
                    index++;
                }

                return last == null ? null : TryExtractHImageAt(last, 0);
            }

            return null;
        }

        private static HImage TryExtractHImageFromHObjectAt(HObject hObject, int imageIndex)
        {
            HObject selectedObject = null;
            HImage selectedImage = null;

            try
            {
                HOperatorSet.CountObj(hObject, out HTuple countTuple);
                int count = countTuple.TupleLength() == 0 ? 0 : countTuple[0].I;
                if (count <= 0)
                    return null;

                int selectIndex = Math.Clamp(imageIndex + 1, 1, count);
                HOperatorSet.SelectObj(hObject, out selectedObject, selectIndex);
                if (selectedObject == null || !selectedObject.IsInitialized())
                    return null;

                selectedImage = new HImage(selectedObject);
                return selectedImage != null && selectedImage.IsInitialized()
                    ? selectedImage.CopyImage()
                    : null;
            }
            catch
            {
                return null;
            }
            finally
            {
                selectedImage?.Dispose();
                selectedObject?.Dispose();
            }
        }

        private static void TagResultImageIndex(Result result, int imageIndex)
        {
            if (result == null || imageIndex < 0)
                return;

            result.Others ??= new Dictionary<string, object>();
            if (!result.Others.ContainsKey(DefectPostProcessImageIndexKey))
                result.Others[XYHDSourceImageIndexKey] = imageIndex;
        }

        private static HImage CopyImageSafe(HImage image)
        {
            try
            {
                if (image != null && image.IsInitialized())
                    return image.CopyImage();
            }
            catch
            {
            }

            return null;
        }

        private static void DisposePathResultImages(IEnumerable<PathResult> paths)
        {
            if (paths == null)
                return;

            foreach (PathResult path in paths)
                DisposeImageSafe(path.pathImage);
        }

        private static void DisposeImageSafe(HImage image)
        {
            try
            {
                image?.Dispose();
            }
            catch
            {
            }
        }

        private object TryGetResultSourceInputImages(int resultSourceSerial, out string source)
        {
            source = null;
            if (resultSourceSerial < 0)
                return null;

            foreach (var candidate in EnumerateResultSourceModels(resultSourceSerial))
            {
                object imageValue = TryGetModelInputImageValue(candidate.Model, out string valueSource);
                if (!HasExtractableImage(imageValue))
                    continue;

                source = $"{candidate.Source}.{valueSource}";
                return imageValue;
            }

            return null;
        }

        private IEnumerable<(object Model, string Source)> EnumerateResultSourceModels(int resultSourceSerial)
        {
            var solution = PrismProvider.ProjectManager?.SltCurSolutionItem;
            if (solution == null || resultSourceSerial < 0)
                yield break;

            if (solution.NodeParamCaches != null)
            {
                foreach (string cacheKey in EnumerateSourceCacheKeys(resultSourceSerial))
                {
                    if (solution.NodeParamCaches.TryGetValue(cacheKey, out object cachedModel) && cachedModel != null)
                        yield return (cachedModel, $"NodeParamCaches[{cacheKey}]");
                }

                foreach (var cache in solution.NodeParamCaches)
                {
                    if (cache.Value is ModelParamBase model && model.Serial == resultSourceSerial)
                        yield return (model, $"NodeParamCaches[{cache.Key}]");
                }
            }

            if (solution.NodeCaches is not IEnumerable nodeCaches || solution.NodeCaches is string)
                yield break;

            List<object> nodes = nodeCaches.Cast<object>().Where(item => item != null).ToList();
            object currentNode = nodes.FirstOrDefault(node => GetNodeSerial(node) == Serial);
            if (currentNode == null)
                yield break;

            foreach (object node in EnumerateAncestorNodes(currentNode))
            {
                if (GetNodeSerial(node) != resultSourceSerial)
                    continue;

                object moduleParam = GetNodeModuleParamObject(node);
                if (moduleParam != null)
                    yield return (moduleParam, $"AncestorModel[{resultSourceSerial:D3}]");
            }
        }

        private object TryGetModelInputImageValue(object modelObject, out string source)
        {
            source = null;
            if (modelObject == null)
                return null;

            object fieldValue = TryGetTransmitParamMemberValue(modelObject, "_inputImage", isField: true, out source);
            if (fieldValue != null)
                return fieldValue;

            object propertyValue = TryGetTransmitParamMemberValue(modelObject, "InputImage", isField: false, out source);
            if (propertyValue != null)
                return propertyValue;

            if (modelObject is ModelParamBase model)
            {
                TransmitParam inputImageParam =
                    FindTransmitParam(model.InputParams, "InputImage")
                    ?? FindTransmitParam(model.moduleInputParam?.TransmitParams?.Values?.OfType<TransmitParam>(), "InputImage");

                object value = ResolveModelTransmitParamValue(modelObject, inputImageParam);
                if (value != null)
                {
                    source = "InputParams.InputImage";
                    return value;
                }
            }

            return null;
        }

        private object TryGetTransmitParamMemberValue(object modelObject, string memberName, bool isField, out string source)
        {
            source = null;
            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                object memberValue = isField
                    ? modelObject.GetType().GetField(memberName, flags)?.GetValue(modelObject)
                    : modelObject.GetType().GetProperty(memberName, flags)?.GetValue(modelObject);

                object value = ResolveModelTransmitParamValue(modelObject, memberValue as TransmitParam);
                if (value == null)
                    return null;

                source = $"{memberName}.Value";
                return value;
            }
            catch (Exception ex)
            {
                AddLog($"[Parse] Result source image lookup failed: Model={modelObject.GetType().Name}, Member={memberName}, Error={ex.Message}", "WARN");
                return null;
            }
        }

        private object ResolveModelTransmitParamValue(object modelObject, TransmitParam transmitParam)
        {
            if (transmitParam == null)
                return null;

            if (transmitParam.Value != null)
                return transmitParam.Value;

            if (modelObject is ModelParamBase model)
            {
                try
                {
                    return model.GetTransmitParam(model.InputParams, transmitParam, false);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static TransmitParam FindTransmitParam(IEnumerable<TransmitParam> transmitParams, string expectedName)
        {
            return transmitParams?
                .FirstOrDefault(param => ParamNameMatch(param, expectedName));
        }

        private static bool HasExtractableImage(object imageValue)
        {
            HImage image = null;
            try
            {
                image = TryExtractHImageAt(imageValue, 0);
                return image != null && image.IsInitialized();
            }
            catch
            {
                return false;
            }
            finally
            {
                image?.Dispose();
            }
        }

        private void AttachDisplayTargetMetadata(
            string pathName,
            int serial,
            List<Result> results,
            TransmitParam targetImageInput = null,
            string targetImageInputName = null,
            object preferredTargetImages = null,
            string preferredTargetSource = null)
        {
            if (results == null || results.Count == 0)
                return;

            object targetImages = preferredTargetImages;
            string hitSource = preferredTargetSource;
            if (targetImages == null)
            {
                targetImages = GetSelectedInputValue(
                    targetImageInput,
                    targetImageInputName,
                    HasConfiguredSelection(targetImageInput) ? targetImageInput.Serial : -1,
                    out hitSource,
                    out _);
                hitSource ??= targetImageInputName;
            }
            if (targetImages == null)
            {
                AddLog($"[Parse] 缺陷墙目标图固定输入无值: Path={pathName ?? "-"}, Serial={serial}, Source={hitSource}, 缺陷={results.Count}", "DEBUG");
                return;
            }

            var sizeCache = new Dictionary<int, (int Width, int Height)>();
            int attachedCount = 0;
            int missCount = 0;

            foreach (var result in results.Where(item => item != null))
            {
                int imageIndex = ResolveResultImageIndex(result, 0);
                if (!sizeCache.TryGetValue(imageIndex, out var target))
                {
                    target = CreateDisplayTargetSize(targetImages, imageIndex);
                    sizeCache[imageIndex] = target;
                }

                if (target.Width <= 0 || target.Height <= 0)
                {
                    missCount++;
                    continue;
                }

                result.Others ??= new Dictionary<string, object>();
                result.Others.Remove(DefectPreviewFactory.DisplayTargetBitmapKey);
                result.Others.Remove(Custom.DefectOverview.Services.DefectPreviewFactory.DisplayTargetBitmapKey);
                result.Others[DefectPreviewFactory.DisplayTargetSourceWidthKey] = target.Width;
                result.Others[DefectPreviewFactory.DisplayTargetSourceHeightKey] = target.Height;
                result.Others[DefectPreviewFactory.DisplayCenterXKey] = result.Cx;
                result.Others[DefectPreviewFactory.DisplayCenterYKey] = result.Cy;
                result.Others[DefectPreviewFactory.DisplayPixelWidthKey] = result.Width;
                result.Others[DefectPreviewFactory.DisplayPixelHeightKey] = result.Height;
                attachedCount++;
            }

            if (attachedCount > 0)
            {
                AddLog(
                    $"[Parse] 已绑定缺陷墙目标图: Path={pathName ?? "-"}, Serial={serial}, Source={hitSource ?? "-"}, 缺陷={attachedCount}",
                    "DEBUG");
            }
            else if (missCount > 0)
            {
                AddLog(
                    $"[Parse] 缺陷墙目标图存在但转换失败: Path={pathName ?? "-"}, Serial={serial}, Source={hitSource ?? "-"}, Miss={missCount}",
                    "WARN");
            }
        }

        private static (BitmapSource Bitmap, int Width, int Height) CreateDisplayTargetBitmap(object targetImages, int imageIndex)
        {
            if (targetImages == null)
                return default;

            if (targetImages is IList list && targetImages is not string)
            {
                if (list.Count == 0)
                    return default;

                int safeIndex = Math.Clamp(imageIndex, 0, list.Count - 1);
                return CreateDisplayTargetBitmap(list[safeIndex], 0);
            }

            if (targetImages is Array array)
            {
                if (array.Length == 0)
                    return default;

                int safeIndex = Math.Clamp(imageIndex, 0, array.Length - 1);
                return CreateDisplayTargetBitmap(array.GetValue(safeIndex), 0);
            }

            if (targetImages is HObject hObject && hObject.IsInitialized())
                return CreateDisplayTargetBitmapFromHObject(hObject, imageIndex);

            return default;
        }

        private static (int Width, int Height) CreateDisplayTargetSize(object targetImages, int imageIndex)
        {
            HImage image = null;
            try
            {
                image = TryExtractHImageAt(targetImages, imageIndex);
                if (image == null || !image.IsInitialized())
                    return default;

                image.GetImageSize(out int width, out int height);
                return width > 0 && height > 0 ? (width, height) : default;
            }
            catch
            {
                return default;
            }
            finally
            {
                image?.Dispose();
            }
        }

        private static (BitmapSource Bitmap, int Width, int Height) CreateDisplayTargetBitmapFromHObject(HObject hObject, int imageIndex)
        {
            HObject selectedObject = null;
            HImage selectedImage = null;

            try
            {
                HOperatorSet.CountObj(hObject, out HTuple countTuple);
                int count = countTuple.TupleLength() == 0 ? 0 : countTuple[0].I;
                if (count <= 0)
                    return default;

                int selectIndex = Math.Clamp(imageIndex + 1, 1, count);
                HOperatorSet.SelectObj(hObject, out selectedObject, selectIndex);
                if (selectedObject == null || !selectedObject.IsInitialized())
                    return default;

                selectedImage = new HImage(selectedObject);
                if (selectedImage == null || !selectedImage.IsInitialized())
                    return default;

                selectedImage.GetImageSize(out int width, out int height);
                var bitmap = DefectPreviewFactory.CreateBitmapFromHImage(selectedImage);
                return bitmap == null ? default : (bitmap, width, height);
            }
            catch
            {
                return default;
            }
            finally
            {
                selectedImage?.Dispose();
                selectedObject?.Dispose();
            }
        }

        private static int ResolveResultImageIndex(Result result, int fallback)
        {
            if (result?.Others == null)
                return fallback;

            foreach (string key in new[] { DefectPostProcessImageIndexKey, XYHDSourceImageIndexKey, "ImageIndex", "TargetIndex" })
            {
                if (!result.Others.TryGetValue(key, out object rawValue) || rawValue == null)
                    continue;

                try
                {
                    int index = Convert.ToInt32(rawValue);
                    if (index >= 0)
                        return index;
                }
                catch
                {
                }
            }

            return fallback;
        }

        private static int ResolveFirstResultImageIndex(IEnumerable<Result> results, int fallback)
        {
            if (results == null)
                return fallback;

            foreach (Result result in results)
            {
                if (result != null)
                    return ResolveResultImageIndex(result, fallback);
            }

            return fallback;
        }

        private static string ResolvePathName(string name, int serial)
        {
            if (IsLeftPathText(name))
                return "左";

            if (IsRightPathText(name))
                return "右";

            return string.IsNullOrWhiteSpace(name) ? "?" : name;
        }

        private static bool IsLeftPathText(string text)
        {
            return !string.IsNullOrWhiteSpace(text)
                && (text.Contains("左", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("left", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("path1", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("lane1", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsRightPathText(string text)
        {
            return !string.IsNullOrWhiteSpace(text)
                && (text.Contains("右", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("right", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("path2", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("lane2", StringComparison.OrdinalIgnoreCase));
        }

        private HImage TryGetSelectedPathImage(string pathName, out string hitSource)
        {
            if (string.IsNullOrWhiteSpace(pathName))
            {
                hitSource = null;
                return null;
            }

            string selectedName = IsLeftPathText(pathName)
                ? DefaultLeftImageInputName
                : IsRightPathText(pathName)
                    ? DefaultRightImageInputName
                    : null;
            TransmitParam selectedInput = IsLeftPathText(pathName)
                ? LeftInputImage
                : IsRightPathText(pathName)
                    ? RightInputImage
                    : null;
            if (HasConfiguredSelection(selectedInput))
                selectedName = EffectiveName(selectedInput.Name, EffectiveName(selectedInput.ParamName, selectedName));
            hitSource = null;

            try
            {
                object value = GetSelectedInputValue(
                    selectedInput,
                    selectedName,
                    HasConfiguredSelection(selectedInput) ? selectedInput.Serial : -1,
                    out hitSource,
                    out _);
                if (value == null)
                {
                    hitSource ??= selectedName;
                    return null;
                }

                var image = TryExtractHImageAt(value, 0);
                if (image != null && image.IsInitialized())
                {
                    AddLog($"[TryGetPathImage] 使用固定的 {pathName}路图像 (Source={hitSource ?? selectedName}, Size={GetImageSizeStr(image)})", "INFO");
                    return image;
                }

                image?.Dispose();
                AddLog($"[TryGetPathImage] 固定的 {pathName}路图像不是有效图像: Source={hitSource ?? selectedName}, Type={value.GetType().Name}", "WARN");
            }
            catch (Exception ex)
            {
                hitSource ??= selectedName;
                AddLog($"[TryGetPathImage] 获取{pathName}路固定图像异常: {ex.Message}", "WARN");
            }

            return null;
        }

        /// <summary>
        /// 安全获取图像尺寸字符串
        /// </summary>
        private static string GetImageSizeStr(HImage img)
        {
            try
            {
                if (img == null || !img.IsInitialized()) return "null";
                img.GetImageSize(out int w, out int h);
                return $"{w}x{h}";
            }
            catch { return "error"; }
        }
    }
}
