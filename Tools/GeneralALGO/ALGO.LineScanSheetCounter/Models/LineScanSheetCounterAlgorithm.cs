using HalconDotNet;
using ReeYin_V.Core.Helper.ImageOP;
using ReeYin_V.Logger;

namespace ALGO.LineScanSheetCounter.Models;

/// <summary>
/// 线扫片材计数核心算法。
/// 处理方向固定为图像 Y 方向：通过 MeasurePos 查找片材上下边缘，再按行范围裁剪单张片材。
/// </summary>
public sealed class LineScanSheetCounterAlgorithm
{
    #region 字段：运行时图像与测量状态

    private HObject _sourceConcatImage = new();
    private HTuple _concatWidth = new();
    private HTuple _concatHeight = new();
    private LineScanSheetCounterResult _counterResult = new();
    private bool _shouldKeepRemainImage = true;
    private static readonly object ProcessSyncRoot = new();
    private static readonly object HalconTilingSyncRoot = new();
    private static long _processLogSequence;

    #endregion

    #region 公共入口：处理单帧线扫图像

    /// <summary>
    /// 处理当前帧图像。上一帧残留图像会叠加到当前帧上方，继续查找完整片材。
    /// </summary>
    /// <param name="remainImage">上一帧未闭合的残留图像。</param>
    /// <param name="inputImage">当前输入线扫图像。</param>
    /// <param name="parameters">测量参数。</param>
    /// <returns>本帧计数、裁剪图像、残留图像和预览信息。</returns>
    public LineScanSheetCounterResult Process(HImage? remainImage, HImage inputImage, LineScanSheetCounterParams parameters)
    {
        return Process(remainImage, inputImage, null, null, parameters);
    }

    public LineScanSheetCounterResult Process(
        HImage? remainImage,
        HImage inputImage,
        HImage? remainMaskImage,
        HImage? maskImage,
        LineScanSheetCounterParams parameters,
        bool createPreviewImages = true)
    {
        lock (ProcessSyncRoot)
        {
            return ProcessCore(remainImage, inputImage, remainMaskImage, maskImage, parameters, createPreviewImages);
        }
    }

    private LineScanSheetCounterResult ProcessCore(
        HImage? remainImage,
        HImage inputImage,
        HImage? remainMaskImage,
        HImage? maskImage,
        LineScanSheetCounterParams parameters,
        bool createPreviewImages)
    {
        ArgumentNullException.ThrowIfNull(inputImage);
        ArgumentNullException.ThrowIfNull(parameters);

        _counterResult = new LineScanSheetCounterResult();
        bool resultReturned = false;

        // 输入图像不做 X 方向预裁剪，片材裁剪只按 Y 方向行范围执行。
        // The caller already owns cloned input images; keep this layer read-only to avoid another full-frame copy.
        long processLogId = System.Threading.Interlocked.Increment(ref _processLogSequence);
        LogProcessImageStates(processLogId, "entry", remainImage, inputImage, remainMaskImage, maskImage);

        HObject inputObject = inputImage;
        HObject? maskObject = TryIsInitializedForProcess(maskImage, processLogId, "maskImage")
            ? maskImage
            : null;
        HObject remainObject;
        bool ownsRemainObject = false;
        if (TryIsInitializedForProcess(remainImage, processLogId, "remainImage"))
        {
            remainObject = remainImage!;
        }
        else
        {
            HOperatorSet.GenEmptyObj(out remainObject);
            ownsRemainObject = true;
        }

        HObject remainMaskObject;
        bool ownsRemainMaskObject = false;
        if (TryIsInitializedForProcess(remainMaskImage, processLogId, "remainMaskImage"))
        {
            remainMaskObject = remainMaskImage!;
        }
        else
        {
            HOperatorSet.GenEmptyObj(out remainMaskObject);
            ownsRemainMaskObject = true;
        }

        HObject? measureConcatImage = null;
        try
        {
            LogProcessImageStates(processLogId, "after-input-resolve", remainObject, inputObject, remainMaskObject, maskObject);

            if (maskObject != null)
            {
                ValidateSameSize(inputObject, maskObject, nameof(maskImage));
            }

            NormalizeRemainStreamsForCurrentFrame(
                ref remainObject,
                ref ownsRemainObject,
                ref remainMaskObject,
                ref ownsRemainMaskObject,
                inputObject,
                maskObject != null);
            ConcateImage(remainObject, inputObject);
            if (!TryGetImageSize(_sourceConcatImage, out int concatWidth, out int concatHeight))
            {
                throw new InvalidOperationException("Concat image is invalid.");
            }

            _concatWidth = concatWidth;
            _concatHeight = concatHeight;

            HObject measureImage = _sourceConcatImage;
            if (maskObject != null)
            {
                HObject? rawMaskConcatImage = null;
                try
                {
                    rawMaskConcatImage = ConcatenateImage(remainMaskObject, maskObject, out _, out _);
                    ValidateSameSize(_sourceConcatImage, rawMaskConcatImage, "MaskImage concat");
                    measureConcatImage = BinarizeMaskImage(rawMaskConcatImage);
                    ValidateSameSize(_sourceConcatImage, measureConcatImage, "MaskImage binarized concat");
                    measureImage = measureConcatImage;
                }
                finally
                {
                    SafeDispose(rawMaskConcatImage, "rawMaskConcatImage", processLogId);
                }
            }

            double zoomHeight = LineMeasureProcess(parameters, measureImage, _sourceConcatImage, createPreviewImages);
            SortLines(parameters, zoomHeight);
            PourResult(parameters, measureConcatImage, createPreviewImages, out LineScanSheetCounterResult result);
            resultReturned = true;
            return result;
        }
        finally
        {
            if (!resultReturned)
            {
                _counterResult.Dispose();
            }

            LogProcessImageStates(processLogId, "cleanup-before-dispose", remainObject, inputObject, remainMaskObject, maskObject);
            SafeDispose(measureConcatImage, "measureConcatImage", processLogId);
            SafeDispose(_sourceConcatImage, "_sourceConcatImage", processLogId);
            _sourceConcatImage = new();
            if (ownsRemainMaskObject)
            {
                SafeDispose(remainMaskObject, "remainMaskObject", processLogId);
            }

            if (ownsRemainObject)
            {
                SafeDispose(remainObject, "remainObject", processLogId);
            }
        }
    }

    #endregion

    #region 图像拼接：残留图像叠加到当前帧上方

    /// <summary>
    /// 将上一帧残留图像和当前输入图像按行方向纵向拼接。
    /// </summary>
    private void ConcateImage(HObject remainImage, HObject image)
    {
        SafeDispose(_sourceConcatImage);
        _sourceConcatImage = TileImagesVertically(remainImage, image, out _, out _);
    }

    private static HObject ConcatenateImage(HObject remainImage, HObject image, out HTuple width, out HTuple height)
    {
        return TileImagesVertically(remainImage, image, out width, out height);
    }

    private static HObject TileImagesVertically(HObject remainImage, HObject image, out HTuple width, out HTuple height)
    {
        LogTileImagesVerticallyCheckpoint("enter before GetImageSize", remainImage, image);
        try
        {
            lock (HalconTilingSyncRoot)
            {
                if (!TryGetImageSize(image, out int inputWidth, out int inputHeight))
                {
                    throw new InvalidOperationException("Input image is invalid before tiling.");
                }

                width = inputWidth;
                height = inputHeight;
                HTuple remainH = TryGetImageSize(remainImage, out _, out int remainHeight)
                    ? new HTuple(remainHeight)
                    : new HTuple();

                return TileImagesVerticallyCore(remainImage, image, width, height, remainH);
            }
        }
        catch (Exception ex)
        {
            LogTileImagesVerticallyError("managed failure", ex, remainImage, image);
            throw;
        }
    }

    private static HObject TileImagesVerticallyCore(HObject remainImage, HObject image, HTuple width, HTuple height, HTuple remainH)
    {
        int remainNum = remainH.Length > 0 ? 1 : 0;
        HTuple offsetR = new();
        offsetR[0] = 0;
        HOperatorSet.TupleGenConst(remainNum + 1, 0, out HTuple offsetC);
        HOperatorSet.TupleGenConst(remainNum + 1, -1, out HTuple tileRC);

        if (remainH.Length > 0)
        {
            using HObjectTupleScope objectsToTile = BuildObjectTuple(remainImage, image);
            offsetR[1] = remainH;
            LogTileImagesVerticallyCheckpoint(
                "before TileImagesOffset",
                remainImage,
                image,
                $"branch=with-remain, remainHeight={FormatTupleForLog(remainH)}, inputWidth={FormatTupleForLog(width)}, inputHeight={FormatTupleForLog(height)}, outputHeight={FormatTupleForLog(height + remainH)}");
            HOperatorSet.TileImagesOffset(
                objectsToTile.ObjectTuple,
                out HObject tiledImage,
                offsetR,
                offsetC,
                tileRC,
                tileRC,
                tileRC,
                tileRC,
                width,
                height + remainH);
            LogTileImagesVerticallyCheckpoint(
                "after TileImagesOffset",
                remainImage,
                image,
                $"branch=with-remain, output={DescribeObjectStateForLog(tiledImage)}");
            return tiledImage;
        }

        using HObjectTupleScope singleObjectToTile = BuildObjectTuple(image);
        LogTileImagesVerticallyCheckpoint(
            "before TileImagesOffset",
            remainImage,
            image,
            $"branch=input-only, inputWidth={FormatTupleForLog(width)}, inputHeight={FormatTupleForLog(height)}, outputHeight={FormatTupleForLog(height)}");
        HOperatorSet.TileImagesOffset(
            singleObjectToTile.ObjectTuple,
            out HObject singleTiledImage,
            offsetR,
            offsetC,
            tileRC,
            tileRC,
            tileRC,
            tileRC,
            width,
            height);
        LogTileImagesVerticallyCheckpoint(
            "after TileImagesOffset",
            remainImage,
            image,
            $"branch=input-only, output={DescribeObjectStateForLog(singleTiledImage)}");
        return singleTiledImage;
    }

    private static void LogTileImagesVerticallyCheckpoint(
        string stage,
        HObject? remainImage,
        HObject? image,
        string? detail = null)
    {
        try
        {
            string suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $", {detail}";
            Logs.LogTrace(
                $"[LineScanSheetCounter] TileImagesVertically {stage}: thread={Environment.CurrentManagedThreadId}, remain={DescribeObjectStateForLog(remainImage)}, input={DescribeObjectStateForLog(image)}{suffix}");
        }
        catch
        {
            // Diagnostics must never affect HALCON processing.
        }
    }

    private static void LogTileImagesVerticallyError(
        string stage,
        Exception exception,
        HObject? remainImage,
        HObject? image)
    {
        try
        {
            Logs.LogError(
                $"[LineScanSheetCounter] TileImagesVertically {stage}: thread={Environment.CurrentManagedThreadId}, remain={DescribeObjectStateForLog(remainImage)}, input={DescribeObjectStateForLog(image)}, error={exception}");
        }
        catch
        {
            // Diagnostics must never mask the original HALCON failure.
        }
    }

    private static string DescribeObjectStateForLog(HObject? image)
    {
        if (image == null)
        {
            return "null";
        }

        try
        {
            return image.IsInitialized() ? "initialized" : "uninitialized";
        }
        catch (Exception ex)
        {
            return $"init-check-failed:{ex.GetType().Name}:{ex.Message}";
        }
    }

    private static string FormatTupleForLog(HTuple tuple)
    {
        try
        {
            return tuple.Length == 0 ? "empty" : tuple.ToString();
        }
        catch (Exception ex)
        {
            return $"tuple-format-failed:{ex.GetType().Name}:{ex.Message}";
        }
    }

    private static HObjectTupleScope BuildObjectTuple(params HObject[] objects)
    {
        HOperatorSet.GenEmptyObj(out HObject objectTuple);
        List<HObject> objectTuples = new() { objectTuple };
        try
        {
            foreach (HObject obj in objects)
            {
                HOperatorSet.ConcatObj(objectTuple, obj, out HObject nextTuple);
                objectTuples.Add(nextTuple);
                objectTuple = nextTuple;
            }

            return new HObjectTupleScope(objectTuple, objectTuples);
        }
        catch
        {
            foreach (HObject tuple in objectTuples)
            {
                SafeDispose(tuple);
            }

            throw;
        }
    }

    private sealed class HObjectTupleScope : IDisposable
    {
        private readonly List<HObject> _objectTuples;

        public HObjectTupleScope(HObject objectTuple, List<HObject> objectTuples)
        {
            ObjectTuple = objectTuple;
            _objectTuples = objectTuples;
        }

        public HObject ObjectTuple { get; }

        public void Dispose()
        {
            foreach (HObject tuple in _objectTuples)
            {
                SafeDispose(tuple);
            }
        }
    }

    private static void ValidateSameSize(HObject expected, HObject actual, string name)
    {
        if (!TryGetImageSize(expected, out int expectedWidth, out int expectedHeight))
        {
            throw new InvalidOperationException($"{name} expected image is invalid.");
        }

        if (!TryGetImageSize(actual, out int actualWidth, out int actualHeight))
        {
            throw new InvalidOperationException($"{name} actual image is invalid.");
        }

        if (expectedWidth != actualWidth || expectedHeight != actualHeight)
        {
            throw new InvalidOperationException($"{name} size must match InputImage.");
        }
    }

    private static void NormalizeRemainStreamsForCurrentFrame(
        ref HObject remainObject,
        ref bool ownsRemainObject,
        ref HObject remainMaskObject,
        ref bool ownsRemainMaskObject,
        HObject inputImage,
        bool hasMaskInput)
    {
        if (!TryGetImageSize(inputImage, out int inputWidth, out _))
        {
            return;
        }

        bool hasRemain = TryGetImageSize(remainObject, out int remainWidth, out int remainHeight);
        bool hasRemainMask = TryGetImageSize(remainMaskObject, out int remainMaskWidth, out int remainMaskHeight);

        if (!hasRemain)
        {
            ReplaceWithEmptyObject(ref remainObject, ref ownsRemainObject);
        }

        if (!hasRemainMask)
        {
            ReplaceWithEmptyObject(ref remainMaskObject, ref ownsRemainMaskObject);
        }

        if (hasRemain && remainWidth != inputWidth)
        {
            ReplaceWithEmptyObject(ref remainObject, ref ownsRemainObject);
            hasRemain = false;
        }

        if (hasRemainMask && remainMaskWidth != inputWidth)
        {
            ReplaceWithEmptyObject(ref remainMaskObject, ref ownsRemainMaskObject);
            hasRemainMask = false;
        }

        if (!hasMaskInput)
        {
            if (hasRemainMask)
            {
                ReplaceWithEmptyObject(ref remainMaskObject, ref ownsRemainMaskObject);
            }

            return;
        }

        bool remainStateUnsynced =
            hasRemain != hasRemainMask ||
            (hasRemain && hasRemainMask && (remainWidth != remainMaskWidth || remainHeight != remainMaskHeight));
        if (remainStateUnsynced)
        {
            ReplaceWithEmptyObject(ref remainObject, ref ownsRemainObject);
            ReplaceWithEmptyObject(ref remainMaskObject, ref ownsRemainMaskObject);
        }
    }

    private static bool TryGetImageSize(HObject image, out int width, out int height)
    {
        return HalconImageOwnership.TryGetImageSize(image, out width, out height, out _);
    }

    private static void ReplaceWithEmptyObject(ref HObject image, ref bool ownsImage)
    {
        if (ownsImage)
        {
            SafeDispose(image);
        }

        HOperatorSet.GenEmptyObj(out HObject emptyObject);
        image = emptyObject;
        ownsImage = true;
    }

    private static HObject BinarizeMaskImage(HObject maskImage)
    {
        if (!TryGetImageSize(maskImage, out int imageWidth, out int imageHeight))
        {
            throw new InvalidOperationException("Mask image is invalid.");
        }

        HTuple width = imageWidth;
        HTuple height = imageHeight;
        HOperatorSet.GetDomain(maskImage, out HObject domain);
        HObject validRegion = new();
        try
        {
            HOperatorSet.MinMaxGray(domain, maskImage, 0, out _, out HTuple maxGray, out _);
            if (maxGray.Length > 0 && maxGray.D > 0)
            {
                HOperatorSet.Threshold(maskImage, out validRegion, 1.0, maxGray.D);
            }
            else
            {
                HOperatorSet.GenEmptyRegion(out validRegion);
            }

            HOperatorSet.RegionToBin(validRegion, out HObject binMaskImage, 255, 0, width, height);
            try
            {
                HOperatorSet.FullDomain(binMaskImage, out HObject fullDomainMaskImage);
                return fullDomainMaskImage;
            }
            finally
            {
                SafeDispose(binMaskImage);
            }
        }
        finally
        {
            SafeDispose(validRegion);
            SafeDispose(domain);
        }
    }

    private static void SafeDispose(HObject? image, string? label = null, long? processLogId = null)
    {
        if (image == null)
        {
            return;
        }

        try
        {
            image.Dispose();
        }
        catch (HOperatorException ex) when (IsDeletedObjectCleanup(ex))
        {
            LogDeletedObjectCleanup(label, processLogId, image, ex);
        }
    }

    private static void LogProcessImageStates(
        long processLogId,
        string stage,
        HObject? remainImage,
        HObject? inputImage,
        HObject? remainMaskImage,
        HObject? maskImage)
    {
        try
        {
            Logs.LogTrace(
                $"[LineScanSheetCounter] Process images {stage}: ProcessId={processLogId}, Thread={Environment.CurrentManagedThreadId}, " +
                $"remainImage={DescribeObjectStateForLog(remainImage)}, inputImage={DescribeObjectStateForLog(inputImage)}, " +
                $"remainMaskImage={DescribeObjectStateForLog(remainMaskImage)}, maskImage={DescribeObjectStateForLog(maskImage)}");
        }
        catch
        {
            // Diagnostics must never affect HALCON processing.
        }
    }

    private static bool TryIsInitializedForProcess(HObject? image, long processLogId, string label)
    {
        if (image == null)
        {
            return false;
        }

        try
        {
            return image.IsInitialized();
        }
        catch (Exception ex)
        {
            try
            {
                Logs.LogWarning(
                    $"[LineScanSheetCounter] 图像初始化状态读取失败: ProcessId={processLogId}, Label={label}, Error={ex}");
            }
            catch
            {
                // Diagnostics must never affect HALCON processing.
            }

            return false;
        }
    }

    private static void LogDeletedObjectCleanup(string? label, long? processLogId, HObject image, HOperatorException exception)
    {
        try
        {
            Logs.LogWarning(
                $"[LineScanSheetCounter] 忽略已删除对象释放异常: ProcessId={processLogId?.ToString() ?? "n/a"}, " +
                $"Label={label ?? "unlabelled"}, State={DescribeObjectStateForLog(image)}, Error={exception.Message}");
        }
        catch
        {
            // Diagnostics must never mask the original HALCON cleanup path.
        }
    }

    private static bool IsDeletedObjectCleanup(HOperatorException ex)
    {
        return HalconImageOwnership.IsDeletedObjectError(ex);
    }

    #endregion

    #region 边缘测量：使用 MeasurePos 查找 Y 方向边缘

    /// <summary>
    /// 缩放拼接图后创建纵向卡尺，检测片材上下边缘所在行。
    /// </summary>
    private double LineMeasureProcess(
        LineScanSheetCounterParams parameters,
        HObject measureImage,
        HObject previewSourceImage,
        bool createPreviewImage)
    {
        _counterResult = new LineScanSheetCounterResult();
        _shouldKeepRemainImage = true;
        double scale = Math.Clamp(parameters.ScaleFactor, 0.05, 1.0);

        HObject? zoomPreviewImage = null;
        HOperatorSet.ZoomImageFactor(measureImage, out HObject zoomMeasureImage, scale, scale, "nearest_neighbor");
        try
        {
            if (!TryGetImageSize(zoomMeasureImage, out int zoomImageWidth, out int zoomImageHeight))
            {
                throw new InvalidOperationException("Zoom concat image is invalid.");
            }

            HTuple zoomWidth = zoomImageWidth;
            HTuple zoomHeight = zoomImageHeight;

            double measureCenterColumn = parameters.MeasureCenterColumn > 0
                ? Math.Clamp(parameters.MeasureCenterColumn * scale, 0.0, Math.Max(0.0, zoomWidth.D - 1.0))
                : zoomWidth.D * 0.5;
            double measureRoiWidth = parameters.MeasureRoiWidth > 0
                ? Math.Clamp(parameters.MeasureRoiWidth * scale, 1.0, Math.Max(1.0, zoomWidth.D * 0.5))
                : Math.Max(zoomWidth.D / 20.0, 1.0);

            _counterResult.PreviewMeasureCenterColumn = measureCenterColumn;
            _counterResult.PreviewMeasureRoiWidth = measureRoiWidth;
            if (createPreviewImage)
            {
                if (ReferenceEquals(measureImage, previewSourceImage))
                {
                    _counterResult.PreviewImage = CopyImageObject(zoomMeasureImage);
                }
                else
                {
                    HOperatorSet.ZoomImageFactor(previewSourceImage, out zoomPreviewImage, scale, scale, "nearest_neighbor");
                    _counterResult.PreviewImage = CopyImageObject(zoomPreviewImage);
                }
            }

            LineMeasureParam measureParam = new()
            {
                ImageHW = [zoomHeight.I, zoomWidth.I],
                MeasureRgnCt = [(int)(zoomHeight.D / 2), (int)Math.Round(measureCenterColumn)],
                MeasureRgnHW = [Math.Max(zoomHeight.I - 1, 1), Math.Max((int)Math.Round(measureRoiWidth), 1)],
                MeasureRgnPhi = -1.57,
                SmoothSigma = Math.Max(1, (int)Math.Round(parameters.SmoothSigma)),
                EdgeThreshold = Math.Max(1, (int)Math.Round(parameters.EdgeThreshold)),
                Select = "all",
                Transition = "all"
            };

            if (EdgeAutoDetection(zoomMeasureImage, measureParam, out HTuple allRowEdges, out HTuple amplitude))
            {
                _counterResult.EdgeRows = TupleToDoubleArray(allRowEdges);
                _counterResult.EdgeAmplitudes = TupleToDoubleArray(amplitude);
            }

            return zoomHeight.D;
        }
        finally
        {
            SafeDispose(zoomPreviewImage);
            SafeDispose(zoomMeasureImage);
        }
    }

    /// <summary>
    /// 调用 HALCON MeasurePos 算子获取边缘行坐标和幅值。
    /// </summary>
    private static bool EdgeAutoDetection(HObject image, LineMeasureParam measureParam, out HTuple rowEdge, out HTuple amplitude)
    {
        HTuple measureCt = new(measureParam.MeasureRgnCt);
        HTuple measureHW = new(measureParam.MeasureRgnHW);

        HTuple measureLen1 = measureHW[0];
        HTuple measureLen2 = measureHW[1];

        HOperatorSet.GenMeasureRectangle2(
            measureCt[0],
            measureCt[1],
            measureParam.MeasureRgnPhi,
            measureLen1,
            measureLen2,
            measureParam.ImageHW[1],
            measureParam.ImageHW[0],
            "nearest_neighbor",
            out HTuple measureHandle);

        try
        {
            HOperatorSet.MeasurePos(
                image,
                measureHandle,
                measureParam.SmoothSigma,
                measureParam.EdgeThreshold,
                measureParam.Transition,
                measureParam.Select,
                out rowEdge,
                out _,
                out amplitude,
                out _);

            return rowEdge.TupleLength() > 0;
        }
        finally
        {
            HOperatorSet.CloseMeasure(measureHandle);
        }
    }

    #endregion

    #region 边缘排序：剔除不完整边缘并计算残留起始行

    /// <summary>
    /// 整理边缘序列，只保留可组成完整片材的边缘对，并计算下一帧残留裁剪行。
    /// </summary>
    private void SortLines(LineScanSheetCounterParams parameters, double zoomHeight)
    {
        HTuple rowEdges = new(_counterResult.EdgeRows);
        HTuple edgeAmplitude = new(_counterResult.EdgeAmplitudes);

        if (rowEdges.TupleLength() <= 1)
        {
            if (rowEdges.TupleLength() == 1 && edgeAmplitude.TupleLength() == 1 && edgeAmplitude[0].D > 0)
            {
                _shouldKeepRemainImage = true;
                _counterResult.CropRow = parameters.CropRatio * rowEdges[0].D;
            }
            else
            {
                _shouldKeepRemainImage = false;
                _counterResult.CropRow = 0;
            }

            return;
        }

        // 如果第一条边是片材下边缘，说明前面没有完整片材起点，需要剔除。
        double firstAmp = edgeAmplitude[0].D;
        double upLastRow = 0;
        if (firstAmp < 0)
        {
            if (rowEdges.TupleLength() == 2)
            {
                upLastRow = rowEdges[0];
            }
            HOperatorSet.TupleRemove(edgeAmplitude, 0, out edgeAmplitude);
            HOperatorSet.TupleRemove(rowEdges, 0, out rowEdges);

        }
        else
        {
            upLastRow = rowEdges[rowEdges.TupleLength() - 2].D;
        }

        HTuple tmpRowEdges = rowEdges.Clone();
        double lastRow = tmpRowEdges[tmpRowEdges.TupleLength() - 1].D;
        double lastAmp = edgeAmplitude[edgeAmplitude.TupleLength() - 1].D;

        if (lastAmp > 0)
        {
            // 最后一条是上边缘，表示末尾片材尚未闭合，需要留到下一帧。
            _shouldKeepRemainImage = true;
            _counterResult.CropRow = parameters.CropRatio * lastRow + (1 - parameters.CropRatio) * upLastRow;
            HOperatorSet.TupleRemove(tmpRowEdges, tmpRowEdges.TupleLength() - 1, out tmpRowEdges);
        }
        else
        {
            _shouldKeepRemainImage = false;
            _counterResult.CropRow = (1 - parameters.CropRatio) * lastRow + parameters.CropRatio * zoomHeight;
        }

        _counterResult.EdgeRows = TupleToDoubleArray(tmpRowEdges);
        _counterResult.EdgeAmplitudes = TupleToDoubleArray(edgeAmplitude);
    }

    #endregion

    #region 结果输出：按行裁剪单张片材和残留图像

    /// <summary>
    /// 根据边缘对裁剪完整片材，剩余未闭合区域作为下一帧残留图像。
    /// </summary>
    private void PourResult(
        LineScanSheetCounterParams parameters,
        HObject? measureConcatImage,
        bool createPreviewImages,
        out LineScanSheetCounterResult counterResult)
    {
        double scale = Math.Clamp(parameters.ScaleFactor, 0.05, 1.0);
        if (createPreviewImages)
        {
            _counterResult.LastConcatImage = CopyImageObject(_sourceConcatImage);
        }

        if (_shouldKeepRemainImage)
        {
            HOperatorSet.CropRectangle1(
                _sourceConcatImage,
                out HObject sourceRemainImage,
                _counterResult.CropRow / scale,
                0,
                _concatHeight - 1,
                _concatWidth - 1);
            _counterResult.RemainImage = AdoptImageObject(sourceRemainImage);
            _counterResult.SourceRemainImage = CopyImageObject(_counterResult.RemainImage);

            if (measureConcatImage != null)
            {
                HOperatorSet.CropRectangle1(
                    measureConcatImage,
                    out HObject maskRemainImage,
                    _counterResult.CropRow / scale,
                    0,
                    _concatHeight - 1,
                    _concatWidth - 1);
                _counterResult.RemainMaskImage = AdoptImageObject(maskRemainImage);
            }
            else
            {
                _counterResult.RemainMaskImage = null;
            }
        }
        else
        {
            _counterResult.RemainImage = null;
            _counterResult.SourceRemainImage = null;
            _counterResult.RemainMaskImage = null;
        }

        int rowEdgesLen = _counterResult.EdgeRows.Length;
        for (int ci = 0; ci < rowEdgesLen - 1; ci += 2)
        {
            double stRow = ci == 0
                ? _counterResult.EdgeRows[ci] * (1 - parameters.CropRatio)
                : _counterResult.EdgeRows[ci] * (1 - parameters.CropRatio) + _counterResult.EdgeRows[ci - 1] * parameters.CropRatio;
            double edRow = ci == rowEdgesLen - 2
                ? _counterResult.CropRow
                : _counterResult.EdgeRows[ci + 1] * (1 - parameters.CropRatio) + _counterResult.EdgeRows[ci + 2] * parameters.CropRatio;

            HOperatorSet.CropRectangle1(
                _sourceConcatImage,
                out HObject targetImage,
                stRow / scale,
                0,
                edRow / scale,
                _concatWidth - 1);
            _counterResult.TargetImages.Add(AdoptImageObject(targetImage));
            _counterResult.CropRanges.Add(new CropRange(stRow / scale, edRow / scale));
        }

        counterResult = _counterResult;
    }

    private static HImage CopyImageObject(HObject image)
    {
        HImage? copiedImage = HalconImageOwnership.CopyBorrowedOrNull(image);
        if (copiedImage == null)
        {
            throw new InvalidOperationException("Failed to copy HALCON image.");
        }

        return copiedImage;
    }

    private static HImage AdoptImageObject(HObject image)
    {
        HImage? copiedImage = HalconImageOwnership.CopyOwnedObjectOrNull(image);
        if (copiedImage == null)
        {
            throw new InvalidOperationException("Failed to copy owned HALCON image.");
        }

        return copiedImage;
    }

    #endregion

    #region 工具方法与内部参数

    /// <summary>
    /// 将 HALCON 元组转换为托管数组，便于界面显示和测试断言。
    /// </summary>
    private static double[] TupleToDoubleArray(HTuple tuple)
    {
        int length = tuple.TupleLength();
        double[] values = new double[length];
        for (int i = 0; i < length; i++)
        {
            values[i] = tuple[i].D;
        }

        return values;
    }

    /// <summary>
    /// MeasurePos 卡尺参数。
    /// </summary>
    private sealed class LineMeasureParam
    {
        public int[] ImageHW { get; set; } = [0, 0];

        public int[] MeasureRgnCt { get; set; } = [0, 0];

        public int[] MeasureRgnHW { get; set; } = [0, 0];

        public double MeasureRgnPhi { get; set; } = -1.57;

        public int SmoothSigma { get; set; } = 30;

        public int EdgeThreshold { get; set; } = 40;

        public string Select { get; set; } = "all";

        public string Transition { get; set; } = "all";
    }

    #endregion
}

/// <summary>
/// 线扫片材计数算法参数。
/// 所有裁剪均按 Y 方向行范围执行，不包含 X 方向列裁剪参数。
/// </summary>
public sealed class LineScanSheetCounterParams
{
    #region MeasurePos 测量参数

    /// <summary>
    /// 算法内部缩放比例。
    /// </summary>
    public double ScaleFactor { get; set; } = 0.3;

    /// <summary>
    /// 边缘检测平滑系数。
    /// </summary>
    public double SmoothSigma { get; set; } = 30.0;

    /// <summary>
    /// 边缘检测阈值。
    /// </summary>
    public double EdgeThreshold { get; set; } = 40.0;

    /// <summary>
    /// 卡尺中心列坐标，按原图坐标填写；小于等于 0 时自动取图像中心。
    /// </summary>
    public double MeasureCenterColumn { get; set; }

    /// <summary>
    /// 卡尺 ROI 宽度，按原图坐标填写；小于等于 0 时自动取图像宽度的默认比例。
    /// </summary>
    public double MeasureRoiWidth { get; set; }

    /// <summary>
    /// 裁剪比例。
    /// </summary>
    public double CropRatio { get; set; } = 0.1;

    /// <summary>
    /// 下一帧最多保留的原始图像行数，0 表示不启用高度保护。
    /// </summary>
    public int MaxRemainHeight { get; set; } = 6000;

    #endregion
}

/// <summary>
/// 线扫片材计数结果。
/// 包含本次新增片材、下一帧残留图像和预览绘制所需的边缘信息。
/// </summary>
public sealed class LineScanSheetCounterResult : IDisposable
{
    #region 计数与图像结果

    /// <summary>
    /// 本次新增数量，等于本次裁剪出的完整片材图像数量。
    /// </summary>
    public int IncrementCount => TargetImages.Count;

    /// <summary>
    /// 未闭合片材区域，下一帧会叠加到输入图像上方继续处理。
    /// </summary>
    public HImage? RemainImage { get; set; }

    /// <summary>
    /// 与检测结果同步的原始图像残留，用于下一帧继续拼接并作为最终裁剪源。
    /// </summary>
    public HImage? SourceRemainImage { get; set; }

    /// <summary>
    /// 与原始残留图像并行维护的 mask 残留，用于下一帧检测。
    /// </summary>
    public HImage? RemainMaskImage { get; set; }

    /// <summary>
    /// 本次用于检测的拼接图像，供预览和保存使用。
    /// </summary>
    public HImage? LastConcatImage { get; set; }

    /// <summary>
    /// 与 MeasurePos 坐标一致的缩放原始拼接图，用于界面显示和结果线叠加。
    /// </summary>
    public HImage? PreviewImage { get; set; }

    /// <summary>
    /// 本次裁剪出的完整单张片材图像集合。
    /// </summary>
    public List<HImage> TargetImages { get; } = [];

    #endregion

    #region 所有权转移

    public HImage? DetachRemainImage()
    {
        HImage? image = RemainImage;
        RemainImage = null;
        return image;
    }

    public HImage? DetachSourceRemainImage()
    {
        HImage? image = SourceRemainImage;
        SourceRemainImage = null;
        return image;
    }

    public HImage? DetachRemainMaskImage()
    {
        HImage? image = RemainMaskImage;
        RemainMaskImage = null;
        return image;
    }

    public HImage? DetachLastConcatImage()
    {
        HImage? image = LastConcatImage;
        LastConcatImage = null;
        return image;
    }

    public List<HImage> DetachTargetImages()
    {
        List<HImage> images = new(TargetImages);
        TargetImages.Clear();
        return images;
    }

    #endregion

    #region 预览辅助数据

    /// <summary>
    /// 每张片材的实际裁剪行范围。
    /// </summary>
    public List<CropRange> CropRanges { get; } = [];

    /// <summary>
    /// MeasurePos 检测到的边缘行坐标。
    /// </summary>
    public double[] EdgeRows { get; set; } = [];

    /// <summary>
    /// 预览图坐标系下的边缘行坐标。
    /// </summary>
    public double[] PreviewEdgeRows => EdgeRows;

    /// <summary>
    /// MeasurePos 检测到的边缘幅值。
    /// </summary>
    public double[] EdgeAmplitudes { get; set; } = [];

    /// <summary>
    /// 残留图像的起始裁剪行。
    /// </summary>
    public double CropRow { get; set; }

    /// <summary>
    /// 预览图坐标系下的卡尺中心列。
    /// </summary>
    public double PreviewMeasureCenterColumn { get; set; }

    /// <summary>
    /// 预览图坐标系下的卡尺半宽。
    /// </summary>
    public double PreviewMeasureRoiWidth { get; set; }

    public bool IsRemainTrimmed { get; set; }

    public int RemainOriginalHeight { get; set; }

    public int RemainRetainedHeight { get; set; }

    #endregion

    #region 资源释放

    /// <summary>
    /// 释放结果中持有的 HALCON 图像对象。
    /// </summary>
    public void Dispose()
    {
        RemainImage?.Dispose();
        SourceRemainImage?.Dispose();
        RemainMaskImage?.Dispose();
        LastConcatImage?.Dispose();
        PreviewImage?.Dispose();

        foreach (HImage image in TargetImages)
        {
            image.Dispose();
        }

        TargetImages.Clear();
    }

    #endregion
}

/// <summary>
/// 单张片材的行裁剪范围。
/// </summary>
public sealed record CropRange(double StartRow, double EndRow);
