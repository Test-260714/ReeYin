using System;
using System.Runtime.InteropServices;

namespace ReeYin.Hardware.Sensor.Truelight3D.API
{
    public sealed class Truelight3DApi : ITruelight3DApi
    {
        private readonly object _syncRoot = new();
        private IntPtr _sdkHandle = IntPtr.Zero;
        private string _lastStatusSummary = "TrueLight3D API 未初始化。";

        public bool IsInitialized { get; private set; }

        public bool IsConnected { get; private set; }

        public uint ImageWidth { get; private set; }

        public uint ImageHeight { get; private set; }

        public Truelight3DApiResult Initialize()
        {
            lock (_syncRoot)
            {
                try
                {
                    if (IsInitialized && _sdkHandle != IntPtr.Zero)
                    {
                        return BuildSuccess(Truelight3DStatus.STATUS_ALREADY_INITIALIZED, "AMSDK 已初始化。");
                    }

                    Truelight3DStatus status = Truelight3DNativeMethods.Initialize();
                    if (status != Truelight3DStatus.STATUS_OK && status != Truelight3DStatus.STATUS_ALREADY_INITIALIZED)
                    {
                        return BuildFailure(status, $"AMSDK 初始化失败：{status.ToDisplayString()}。");
                    }

                    _sdkHandle = Truelight3DNativeMethods.Instance();
                    if (_sdkHandle == IntPtr.Zero)
                    {
                        return BuildFailure(Truelight3DStatus.STATUS_NOT_INITIALIZED, "AMSDK::instance 返回空指针。");
                    }

                    IsInitialized = true;
                    return BuildSuccess(status, "AMSDK 初始化完成。");
                }
                catch (Exception ex)
                {
                    return BuildException("初始化", ex);
                }
            }
        }

        public Truelight3DApiResult Connect()
        {
            lock (_syncRoot)
            {
                Truelight3DApiResult initResult = EnsureInitializedCore();
                if (!initResult.Success)
                {
                    return initResult;
                }

                try
                {
                    Truelight3DStatus status = Truelight3DNativeMethods.Connect(_sdkHandle);
                    if (status != Truelight3DStatus.STATUS_OK)
                    {
                        return BuildFailure(status, $"TrueLight3D 连接失败：{status.ToDisplayString()}。");
                    }

                    IsConnected = true;
                    Truelight3DApiResult refreshResult = RefreshImageSizeCore();
                    if (!refreshResult.Success)
                    {
                        ResetConnectionState();
                        TryDisconnectSilently();
                        return BuildFailure(refreshResult.Status, $"TrueLight3D 已连接但初始化图像尺寸失败，已自动断开：{refreshResult.Message}");
                    }

                    return BuildSuccess(status, $"TrueLight3D 连接成功，图像尺寸：{ImageWidth} x {ImageHeight}。");
                }
                catch (Exception ex)
                {
                    return BuildException("连接设备", ex);
                }
            }
        }

        public Truelight3DApiResult Disconnect()
        {
            lock (_syncRoot)
            {
                if (_sdkHandle == IntPtr.Zero)
                {
                    IsConnected = false;
                    ImageWidth = 0;
                    ImageHeight = 0;
                    return BuildSuccess(Truelight3DStatus.STATUS_OK, "AMSDK 未创建实例，视为已断开。");
                }

                try
                {
                    Truelight3DStatus status = Truelight3DNativeMethods.Disconnect(_sdkHandle);
                    if (status != Truelight3DStatus.STATUS_OK && status != Truelight3DStatus.STATUS_NOT_CONNECTED)
                    {
                        return BuildFailure(status, $"TrueLight3D 断开失败：{status.ToDisplayString()}。");
                    }

                    IsConnected = false;
                    ImageWidth = 0;
                    ImageHeight = 0;
                    return BuildSuccess(status, "TrueLight3D 已断开连接。");
                }
                catch (Exception ex)
                {
                    return BuildException("断开设备", ex);
                }
            }
        }

        public Truelight3DApiResult Shutdown()
        {
            lock (_syncRoot)
            {
                try
                {
                    if (_sdkHandle != IntPtr.Zero || IsInitialized)
                    {
                        if (IsConnected && _sdkHandle != IntPtr.Zero)
                        {
                            Truelight3DNativeMethods.Disconnect(_sdkHandle);
                        }

                        Truelight3DNativeMethods.Destroy();
                    }

                    _sdkHandle = IntPtr.Zero;
                    IsInitialized = false;
                    IsConnected = false;
                    ImageWidth = 0;
                    ImageHeight = 0;
                    _lastStatusSummary = "TrueLight3D API 已关闭。";
                    return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "AMSDK 已关闭。");
                }
                catch (Exception ex)
                {
                    return BuildException("关闭 SDK", ex);
                }
            }
        }

        public Truelight3DApiResult<Truelight3DFrame> ReadImage(uint timeoutMs = 500)
        {
            lock (_syncRoot)
            {
                Truelight3DApiResult readyResult = EnsureConnectedWithImageSizeCore();
                if (!readyResult.Success)
                {
                    return Truelight3DApiResult<Truelight3DFrame>.FailureResult(readyResult.Status, readyResult.Message);
                }

                int byteLength = checked((int)(ImageWidth * ImageHeight * 3));
                IntPtr buffer = IntPtr.Zero;

                try
                {
                    buffer = Marshal.AllocHGlobal(byteLength);
                    Truelight3DNativeImage image = new()
                    {
                        Data = buffer,
                        Width = (int)ImageWidth,
                        Height = (int)ImageHeight,
                        Channel = 3,
                        Format = Truelight3DPixelFormat.RGB,
                    };

                    Truelight3DStatus status = Truelight3DNativeMethods.ReadImage(_sdkHandle, ref image, timeoutMs);
                    if (status != Truelight3DStatus.STATUS_OK)
                    {
                        return Truelight3DApiResult<Truelight3DFrame>.FailureResult(status, $"读取实时图像失败：{status.ToDisplayString()}。");
                    }

                    int actualByteLength = GetImageByteLength(image.Width, image.Height, image.Channel, byteLength);
                    byte[] pixels = new byte[actualByteLength];
                    Marshal.Copy(buffer, pixels, 0, pixels.Length);

                    Truelight3DFrame frame = new()
                    {
                        PixelData = pixels,
                        Width = image.Width,
                        Height = image.Height,
                        Channel = image.Channel,
                        Format = image.Format,
                    };

                    _lastStatusSummary = $"已读取实时图像：{frame.Width} x {frame.Height}。";
                    return Truelight3DApiResult<Truelight3DFrame>.SuccessResult(frame, status, _lastStatusSummary);
                }
                catch (Exception ex)
                {
                    return BuildException<Truelight3DFrame>("读取实时图像", ex);
                }
                finally
                {
                    if (buffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
            }
        }

        public Truelight3DApiResult ConfigureScan(Truelight3DScanConfiguration configuration)
        {
            lock (_syncRoot)
            {
                Truelight3DApiResult readyResult = EnsureConnectedWithImageSizeCore();
                if (!readyResult.Success)
                {
                    return readyResult;
                }

                try
                {
                    Truelight3DApiResult validationResult = ValidateConfiguration(configuration);
                    if (!validationResult.Success)
                    {
                        return validationResult;
                    }

                    if (configuration.ScanType == Truelight3DScanType.Confocal &&
                        !Truelight3DNativeMethods.IsConfocalSupported(_sdkHandle))
                    {
                        return BuildFailure(Truelight3DStatus.STATUS_NOT_SUPPORTED, "当前设备不支持共焦扫描。");
                    }

                    Truelight3DApiResult result = ExecuteStatus(Truelight3DNativeMethods.SetScanType(_sdkHandle, configuration.ScanType), "设置扫描类型");
                    if (!result.Success) return result;

                    result = ExecuteStatus(Truelight3DNativeMethods.SetObjectiveLensMagnification(_sdkHandle, configuration.ObjectiveMagnification), "设置物镜倍率");
                    if (!result.Success) return result;

                    result = ExecuteStatus(Truelight3DNativeMethods.SetExposureTime(_sdkHandle, configuration.ExposureTimeUs), "设置曝光时间");
                    if (!result.Success) return result;

                    result = ExecuteStatus(Truelight3DNativeMethods.SetAcquisitionParameter(_sdkHandle, configuration.WindowSize, configuration.ZFilter), "设置采集参数");
                    if (!result.Success) return result;

                    result = configuration.UseScanRange
                        ? ExecuteStatus(Truelight3DNativeMethods.SetScanRange(_sdkHandle, configuration.ScanRangeMm), "设置扫描范围")
                        : ExecuteStatus(Truelight3DNativeMethods.SetScanPosition(_sdkHandle, configuration.ScanStartMm, configuration.ScanEndMm), "设置扫描位置");
                    if (!result.Success) return result;

                    float requestedRangeUm = configuration.UseScanRange
                        ? configuration.ScanRangeMm
                        : Math.Abs(configuration.ScanEndMm - configuration.ScanStartMm) * 1000f;
                    result = ValidateScanRangeAgainstDeviceLimits(requestedRangeUm, configuration.UseScanRange ? "扫描范围" : "起止位置差值");
                    if (!result.Success) return result;

                    result = ExecuteStatus(Truelight3DNativeMethods.SetScanStep(_sdkHandle, configuration.ScanStepMm), "设置扫描步距");
                    if (!result.Success)
                    {
                        return result.Status == Truelight3DStatus.STATUS_BAD_PARAMETER
                            ? BuildFailure(result.Status, $"设置扫描步距失败：错误参数。当前下发步距为 {configuration.ScanStepMm:0.###} um。")
                            : result;
                    }

                    result = ExecuteStatus(Truelight3DNativeMethods.SetLightRgb(_sdkHandle, configuration.LightRed, configuration.LightGreen, configuration.LightBlue), "设置 RGB 光源");
                    if (!result.Success) return result;

                    if (configuration.CircleLightValue.HasValue)
                    {
                        if (!Truelight3DNativeMethods.IsCircleLightSupported(_sdkHandle))
                        {
                            return BuildFailure(Truelight3DStatus.STATUS_NOT_SUPPORTED, "当前设备不支持环形光。");
                        }

                        result = ExecuteStatus(Truelight3DNativeMethods.SetCircleLight(_sdkHandle, configuration.CircleLightValue.Value), "设置环形光");
                        if (!result.Success) return result;
                    }

                    if (configuration.ZSpeedMmPerSec.HasValue)
                    {
                        result = ExecuteStatus(Truelight3DNativeMethods.SetZSpeed(_sdkHandle, configuration.ZSpeedMmPerSec.Value), "设置 Z 轴速度");
                        if (!result.Success) return result;
                    }

                    return BuildSuccess(Truelight3DStatus.STATUS_OK, "扫描参数已下发。");
                }
                catch (Exception ex)
                {
                    return BuildException("配置扫描参数", ex);
                }
            }
        }

        public Truelight3DApiResult StartScan()
        {
            lock (_syncRoot)
            {
                Truelight3DApiResult readyResult = EnsureConnectedCore();
                if (!readyResult.Success)
                {
                    return readyResult;
                }

                try
                {
                    Truelight3DStatus status = Truelight3DNativeMethods.StartScan(_sdkHandle);
                    return BuildStartScanResult(status);
                }
                catch (Exception ex)
                {
                    return BuildException("开始扫描", ex);
                }
            }
        }

        public Truelight3DApiResult StopScan()
        {
            lock (_syncRoot)
            {
                return ExecuteConnectedStatus(() => Truelight3DNativeMethods.StopScan(_sdkHandle), "停止扫描");
            }
        }

        public Truelight3DApiResult<Truelight3DScanResult> ReadScanResult(bool includePointCloud = false)
        {
            lock (_syncRoot)
            {
                Truelight3DApiResult readyResult = EnsureConnectedWithImageSizeCore();
                if (!readyResult.Success)
                {
                    return Truelight3DApiResult<Truelight3DScanResult>.FailureResult(readyResult.Status, readyResult.Message);
                }

                int pixelCount = checked((int)(ImageWidth * ImageHeight));
                IntPtr depthBuffer = IntPtr.Zero;
                IntPtr textureBuffer = IntPtr.Zero;
                IntPtr pointBuffer = IntPtr.Zero;

                try
                {
                    depthBuffer = Marshal.AllocHGlobal(sizeof(float) * pixelCount);
                    Truelight3DNativeDepthMap depthMap = new()
                    {
                        Data = depthBuffer,
                        Width = (int)ImageWidth,
                        Height = (int)ImageHeight,
                    };

                    Truelight3DStatus depthStatus = Truelight3DNativeMethods.GetDepthMap(_sdkHandle, ref depthMap);
                    if (depthStatus != Truelight3DStatus.STATUS_OK)
                    {
                        return Truelight3DApiResult<Truelight3DScanResult>.FailureResult(depthStatus, $"读取深度图失败：{depthStatus.ToDisplayString()}。");
                    }

                    int textureBytes = pixelCount * 3;
                    textureBuffer = Marshal.AllocHGlobal(textureBytes);
                    Truelight3DNativeImage texture = new()
                    {
                        Data = textureBuffer,
                        Width = (int)ImageWidth,
                        Height = (int)ImageHeight,
                        Channel = 3,
                        Format = Truelight3DPixelFormat.RGB,
                    };

                    Truelight3DStatus textureStatus = Truelight3DNativeMethods.GetTexture(_sdkHandle, ref texture);
                    if (textureStatus != Truelight3DStatus.STATUS_OK)
                    {
                        return Truelight3DApiResult<Truelight3DScanResult>.FailureResult(textureStatus, $"读取纹理图失败：{textureStatus.ToDisplayString()}。");
                    }

                    float[] depthData = new float[pixelCount];
                    Marshal.Copy(depthBuffer, depthData, 0, depthData.Length);

                    int actualTextureBytes = GetImageByteLength(texture.Width, texture.Height, texture.Channel, textureBytes);
                    byte[] textureData = new byte[actualTextureBytes];
                    Marshal.Copy(textureBuffer, textureData, 0, textureData.Length);

                    Truelight3DScanResult result = new()
                    {
                        Width = texture.Width,
                        Height = texture.Height,
                        DepthData = depthData,
                        TextureData = textureData,
                        TextureChannel = texture.Channel,
                        TextureFormat = texture.Format,
                        XScale = depthMap.XScale,
                        YScale = depthMap.YScale,
                    };

                    if (includePointCloud)
                    {
                        int pointSize = Marshal.SizeOf<Truelight3DNativePointXYZ>();
                        pointBuffer = Marshal.AllocHGlobal(pointSize * pixelCount);
                        Truelight3DNativePointCloudXYZ cloud = new()
                        {
                            Width = ImageWidth,
                            Height = ImageHeight,
                            IsDense = false,
                            Points = pointBuffer,
                        };

                        Truelight3DStatus pointStatus = Truelight3DNativeMethods.GetPointCloud(_sdkHandle, ref cloud);
                        if (pointStatus == Truelight3DStatus.STATUS_OK)
                        {
                            float[] rawPoints = new float[pixelCount * 4];
                            Marshal.Copy(pointBuffer, rawPoints, 0, rawPoints.Length);
                            Truelight3DPoint[] points = new Truelight3DPoint[pixelCount];
                            for (int i = 0; i < pixelCount; i++)
                            {
                                int offset = i * 4;
                                points[i] = new Truelight3DPoint(rawPoints[offset], rawPoints[offset + 1], rawPoints[offset + 2]);
                            }

                            result.PointCloud = new Truelight3DPointCloud
                            {
                                Width = (int)cloud.Width,
                                Height = (int)cloud.Height,
                                IsDense = cloud.IsDense,
                                Points = points,
                            };
                        }
                    }

                    _lastStatusSummary = $"已读取扫描结果：{result.Width} x {result.Height}。";
                    return Truelight3DApiResult<Truelight3DScanResult>.SuccessResult(result, Truelight3DStatus.STATUS_OK, _lastStatusSummary);
                }
                catch (Exception ex)
                {
                    return BuildException<Truelight3DScanResult>("读取扫描结果", ex);
                }
                finally
                {
                    if (pointBuffer != IntPtr.Zero) Marshal.FreeHGlobal(pointBuffer);
                    if (textureBuffer != IntPtr.Zero) Marshal.FreeHGlobal(textureBuffer);
                    if (depthBuffer != IntPtr.Zero) Marshal.FreeHGlobal(depthBuffer);
                }
            }
        }

        public Truelight3DApiResult SetObjectiveMagnification(Truelight3DObjectiveMagnification magnification)
        {
            lock (_syncRoot)
            {
                return ExecuteConnectedStatus(() => Truelight3DNativeMethods.SetObjectiveLensMagnification(_sdkHandle, magnification), "Set objective magnification");
            }
        }

        public Truelight3DApiResult SetExposureTime(uint exposureTimeUs)
        {
            lock (_syncRoot)
            {
                return ExecuteConnectedStatus(() => Truelight3DNativeMethods.SetExposureTime(_sdkHandle, exposureTimeUs), "Set exposure time");
            }
        }

        public Truelight3DApiResult SetLightRgb(byte red, byte green, byte blue)
        {
            lock (_syncRoot)
            {
                return ExecuteConnectedStatus(() => Truelight3DNativeMethods.SetLightRgb(_sdkHandle, red, green, blue), "设置 RGB 光源");
            }
        }

        public Truelight3DApiResult SetCircleLight(uint value)
        {
            lock (_syncRoot)
            {
                Truelight3DApiResult readyResult = EnsureConnectedCore();
                if (!readyResult.Success)
                {
                    return readyResult;
                }

                try
                {
                    if (!Truelight3DNativeMethods.IsCircleLightSupported(_sdkHandle))
                    {
                        return BuildFailure(Truelight3DStatus.STATUS_NOT_SUPPORTED, "当前设备不支持环形光。");
                    }

                    return ExecuteStatus(Truelight3DNativeMethods.SetCircleLight(_sdkHandle, value), "设置环形光");
                }
                catch (Exception ex)
                {
                    return BuildException("设置环形光", ex);
                }
            }
        }

        public Truelight3DApiResult MoveZ(Truelight3DMotionDirection direction)
        {
            lock (_syncRoot)
            {
                return ExecuteZStatus(() => Truelight3DNativeMethods.MoveZ(_sdkHandle, direction), "Z axis jog");
            }
        }

        public Truelight3DApiResult MoveZRelative(float positionMm)
        {
            lock (_syncRoot)
            {
                return ExecuteZStatus(() => Truelight3DNativeMethods.MoveZRelative(_sdkHandle, positionMm), "Z 轴相对运动");
            }
        }

        public Truelight3DApiResult MoveZAbsolute(float positionMm)
        {
            lock (_syncRoot)
            {
                return ExecuteZStatus(() => Truelight3DNativeMethods.MoveZAbsolute(_sdkHandle, positionMm), "Z 轴绝对运动");
            }
        }

        public Truelight3DApiResult MoveZHome(bool isWaiting = false)
        {
            lock (_syncRoot)
            {
                return ExecuteZStatus(() => Truelight3DNativeMethods.MoveZHome(_sdkHandle, isWaiting), "Z 轴回零");
            }
        }

        public Truelight3DApiResult StopZ()
        {
            lock (_syncRoot)
            {
                return ExecuteZStatus(() => Truelight3DNativeMethods.StopZ(_sdkHandle), "停止 Z 轴");
            }
        }

        public Truelight3DApiResult<float> GetZPosition()
        {
            lock (_syncRoot)
            {
                Truelight3DApiResult readyResult = EnsureConnectedCore();
                if (!readyResult.Success)
                {
                    return Truelight3DApiResult<float>.FailureResult(readyResult.Status, readyResult.Message);
                }

                try
                {
                    Truelight3DStatus status = Truelight3DNativeMethods.GetZPosition(_sdkHandle, out float positionMm);
                    if (status != Truelight3DStatus.STATUS_OK)
                    {
                        return Truelight3DApiResult<float>.FailureResult(status, $"获取 Z 轴位置失败：{status.ToDisplayString()}。");
                    }

                    string message = $"当前 Z 轴位置：{positionMm:F3} mm。";
                    _lastStatusSummary = message;
                    return Truelight3DApiResult<float>.SuccessResult(positionMm, status, message);
                }
                catch (Exception ex)
                {
                    return BuildException<float>("获取 Z 轴位置", ex);
                }
            }
        }

        public Truelight3DApiResult<float> GetZSpeed()
        {
            lock (_syncRoot)
            {
                Truelight3DApiResult readyResult = EnsureConnectedCore();
                if (!readyResult.Success)
                {
                    return Truelight3DApiResult<float>.FailureResult(readyResult.Status, readyResult.Message);
                }

                try
                {
                    Truelight3DStatus status = Truelight3DNativeMethods.GetZSpeed(_sdkHandle, out float speedMmPerSec);
                    if (status != Truelight3DStatus.STATUS_OK)
                    {
                        return Truelight3DApiResult<float>.FailureResult(status, $"获取 Z 轴速度失败：{status.ToDisplayString()}。");
                    }

                    string message = $"当前 Z 轴速度：{speedMmPerSec:F3} mm/s。";
                    _lastStatusSummary = message;
                    return Truelight3DApiResult<float>.SuccessResult(speedMmPerSec, status, message);
                }
                catch (Exception ex)
                {
                    return BuildException<float>("获取 Z 轴速度", ex);
                }
            }
        }

        public Truelight3DApiResult SetParameter(string key, object? value)
        {
            return key switch
            {
                "ObjectiveMagnification" when value is Truelight3DObjectiveMagnification magnification => SetObjectiveMagnification(magnification),
                "ExposureTimeUs" when TryConvertToUInt(value, out uint exposureTimeUs) => SetExposureTime(exposureTimeUs),
                "CircleLight" when TryConvertToUInt(value, out uint circleLight) => SetCircleLight(circleLight),
                "MoveZPositive" => MoveZ(Truelight3DMotionDirection.Positive),
                "MoveZNegative" => MoveZ(Truelight3DMotionDirection.Negative),
                "MoveZRelative" when TryConvertToFloat(value, out float relativeDistance) => MoveZRelative(relativeDistance),
                "MoveZAbsolute" when TryConvertToFloat(value, out float absolutePosition) => MoveZAbsolute(absolutePosition),
                "MoveZHome" => MoveZHome(value is bool boolValue && boolValue),
                "StopZ" => StopZ(),
                "ZSpeed" when TryConvertToFloat(value, out float speed) => ExecuteConnectedStatus(() => Truelight3DNativeMethods.SetZSpeed(_sdkHandle, speed), "设置 Z 轴速度"),
                _ => BuildFailure(Truelight3DStatus.STATUS_NOT_IMPLEMENTATION, $"当前未实现参数写入：{key}。"),
            };
        }

        public string GetStatusSummary()
        {
            lock (_syncRoot)
            {
                return _lastStatusSummary;
            }
        }

        private Truelight3DApiResult EnsureInitializedCore()
        {
            return IsInitialized && _sdkHandle != IntPtr.Zero
                ? Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "AMSDK 已初始化。")
                : Initialize();
        }

        private Truelight3DApiResult EnsureConnectedCore()
        {
            Truelight3DApiResult initResult = EnsureInitializedCore();
            if (!initResult.Success)
            {
                return initResult;
            }

            if (IsConnected)
            {
                return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "TrueLight3D 已连接。");
            }

            if (_sdkHandle == IntPtr.Zero)
            {
                return Truelight3DApiResult.FailureResult(Truelight3DStatus.STATUS_NOT_CONNECTED, "TrueLight3D 尚未连接。");
            }

            try
            {
                if (Truelight3DNativeMethods.IsConnected(_sdkHandle))
                {
                    IsConnected = true;
                    return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "TrueLight3D 已连接。");
                }

                return Truelight3DApiResult.FailureResult(Truelight3DStatus.STATUS_NOT_CONNECTED, "TrueLight3D 尚未连接。");
            }
            catch (Exception ex)
            {
                return BuildException("检查连接状态", ex);
            }
        }

        private Truelight3DApiResult EnsureConnectedWithImageSizeCore()
        {
            Truelight3DApiResult connectResult = EnsureConnectedCore();
            if (!connectResult.Success)
            {
                return connectResult;
            }

            return ImageWidth > 0 && ImageHeight > 0
                ? Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "图像尺寸已就绪。")
                : RefreshImageSizeCore();
        }

        private Truelight3DApiResult RefreshImageSizeCore()
        {
            if (_sdkHandle == IntPtr.Zero)
            {
                return BuildFailure(Truelight3DStatus.STATUS_NOT_INITIALIZED, "AMSDK 实例未创建。");
            }

            try
            {
                Truelight3DStatus widthStatus = Truelight3DNativeMethods.GetWidth(_sdkHandle, out uint width);
                if (widthStatus != Truelight3DStatus.STATUS_OK)
                {
                    return BuildFailure(widthStatus, $"获取图像宽度失败：{widthStatus.ToDisplayString()}。");
                }

                Truelight3DStatus heightStatus = Truelight3DNativeMethods.GetHeight(_sdkHandle, out uint height);
                if (heightStatus != Truelight3DStatus.STATUS_OK)
                {
                    return BuildFailure(heightStatus, $"获取图像高度失败：{heightStatus.ToDisplayString()}。");
                }

                ImageWidth = width;
                ImageHeight = height;
                _lastStatusSummary = $"图像尺寸已更新：{ImageWidth} x {ImageHeight}。";
                return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, _lastStatusSummary);
            }
            catch (Exception ex)
            {
                return BuildException("获取图像尺寸", ex);
            }
        }

        private Truelight3DApiResult ValidateConfiguration(Truelight3DScanConfiguration configuration)
        {
            if (configuration.ExposureTimeUs == 0)
            {
                return BuildFailure(Truelight3DStatus.STATUS_BAD_PARAMETER, "曝光时间必须大于 0。");
            }

            if (configuration.WindowSize == 0 || configuration.WindowSize % 2 == 0)
            {
                return BuildFailure(Truelight3DStatus.STATUS_BAD_PARAMETER, "滤波窗口必须为大于 0 的奇数。");
            }

            if (configuration.ZFilter < 0f || configuration.ZFilter > 1f)
            {
                return BuildFailure(Truelight3DStatus.STATUS_BAD_PARAMETER, "Z 轴滤波系数必须在 0 到 1 之间。");
            }

            if (configuration.UseScanRange)
            {
                if (configuration.ScanRangeMm <= 0f)
                {
                    return BuildFailure(Truelight3DStatus.STATUS_BAD_PARAMETER, "扫描范围必须大于 0。");
                }
            }
            else if (Math.Abs(configuration.ScanEndMm - configuration.ScanStartMm) < 0.000001f)
            {
                return BuildFailure(Truelight3DStatus.STATUS_BAD_PARAMETER, "扫描起点和终点不能相同。");
            }

            if (configuration.ScanStepMm <= 0f)
            {
                return BuildFailure(Truelight3DStatus.STATUS_BAD_PARAMETER, "扫描步距必须大于 0。");
            }

            float minScanStepUm = configuration.ScanType == Truelight3DScanType.Confocal ? 0.01f : 0.1f;
            if (configuration.ScanStepMm < minScanStepUm)
            {
                string scanTypeName = configuration.ScanType == Truelight3DScanType.Confocal ? "Confocal" : "Variation";
                return BuildFailure(Truelight3DStatus.STATUS_BAD_PARAMETER, $"Scan step too small: current {configuration.ScanStepMm:0.###} um, {scanTypeName} minimum is {minScanStepUm:0.##} um.");
            }


            if (configuration.ZSpeedMmPerSec.HasValue && configuration.ZSpeedMmPerSec.Value <= 0f)
            {
                return BuildFailure(Truelight3DStatus.STATUS_BAD_PARAMETER, "Z 轴速度必须大于 0。");
            }

            return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "扫描参数校验通过。");
        }
        private Truelight3DApiResult ValidateScanRangeAgainstDeviceLimits(float requestedRangeUm, string sourceName)
        {
            if (_sdkHandle == IntPtr.Zero)
            {
                return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "SDK 未就绪，跳过扫描范围约束校验。");
            }

            try
            {
                Truelight3DStatus minStatus = Truelight3DNativeMethods.GetScanRangeMin(_sdkHandle, out float minRange);
                Truelight3DStatus maxStatus = Truelight3DNativeMethods.GetScanRangeMax(_sdkHandle, out float maxRange);
                if (minStatus != Truelight3DStatus.STATUS_OK || maxStatus != Truelight3DStatus.STATUS_OK)
                {
                    return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "获取设备扫描范围约束失败，跳过范围预校验。");
                }

                if (requestedRangeUm < minRange || requestedRangeUm > maxRange)
                {
                    return BuildFailure(
                        Truelight3DStatus.STATUS_BAD_PARAMETER,
                        $"{sourceName}对应的有效扫描范围为 {requestedRangeUm:0.###} um，超出设备允许范围 [{minRange:0.###}, {maxRange:0.###}] um。");
                }

                return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "扫描范围预校验通过。");
            }
            catch
            {
                return Truelight3DApiResult.SuccessResult(Truelight3DStatus.STATUS_OK, "扫描范围预校验异常，已降级为跳过校验。");
            }
        }
        private Truelight3DApiResult ExecuteConnectedStatus(Func<Truelight3DStatus> action, string actionName)
        {
            Truelight3DApiResult readyResult = EnsureConnectedCore();
            if (!readyResult.Success)
            {
                return readyResult;
            }

            try
            {
                return ExecuteStatus(action(), actionName);
            }
            catch (Exception ex)
            {
                return BuildException(actionName, ex);
            }
        }

        private Truelight3DApiResult ExecuteZStatus(Func<Truelight3DStatus> action, string actionName)
        {
            Truelight3DApiResult readyResult = EnsureConnectedCore();
            if (!readyResult.Success)
            {
                return readyResult;
            }

            try
            {
                if (!Truelight3DNativeMethods.IsZMotorSupported(_sdkHandle))
                {
                    return BuildFailure(Truelight3DStatus.STATUS_NOT_SUPPORTED, "当前设备不支持 Z 轴运动。");
                }

                return ExecuteStatus(action(), actionName);
            }
            catch (Exception ex)
            {
                return BuildException(actionName, ex);
            }
        }

        private Truelight3DApiResult ExecuteStatus(Truelight3DStatus status, string actionName)
        {
            return status == Truelight3DStatus.STATUS_OK
                ? BuildSuccess(status, $"{actionName}成功。")
                : BuildFailure(status, $"{actionName}失败：{status.ToDisplayString()}。");
        }

        private Truelight3DApiResult BuildStartScanResult(Truelight3DStatus status)
        {
            if (status == Truelight3DStatus.STATUS_OK)
            {
                return BuildSuccess(status, "开始扫描成功。");
            }

            if (status == Truelight3DStatus.STATUS_OUT_OF_RANGE)
            {
                return BuildFailure(status, BuildScanRangeErrorMessage());
            }

            if (status == Truelight3DStatus.STATUS_OUT_OF_LAYER_MAX)
            {
                return BuildFailure(status, BuildLayerLimitErrorMessage());
            }

            return BuildFailure(status, $"开始扫描失败：{status.ToDisplayString()}。");
        }

        private string BuildScanRangeErrorMessage()
        {
            if (_sdkHandle == IntPtr.Zero)
            {
                return "开始扫描失败：扫描范围超出设备允许范围。";
            }

            try
            {
                Truelight3DStatus minStatus = Truelight3DNativeMethods.GetScanRangeMin(_sdkHandle, out float minRange);
                Truelight3DStatus maxStatus = Truelight3DNativeMethods.GetScanRangeMax(_sdkHandle, out float maxRange);
                if (minStatus == Truelight3DStatus.STATUS_OK && maxStatus == Truelight3DStatus.STATUS_OK)
                {
                    return $"开始扫描失败：扫描范围超出设备允许范围，当前设备支持范围约为 {minRange:F3} 到 {maxRange:F3}。";
                }
            }
            catch
            {
            }

            return "开始扫描失败：扫描范围超出设备允许范围。";
        }

        private string BuildLayerLimitErrorMessage()
        {
            if (_sdkHandle == IntPtr.Zero)
            {
                return "开始扫描失败：扫描层数超过设备允许上限。";
            }

            try
            {
                Truelight3DStatus status = Truelight3DNativeMethods.GetLayerMaxNumber(_sdkHandle, out int maxLayers);
                if (status == Truelight3DStatus.STATUS_OK)
                {
                    return $"开始扫描失败：扫描层数超过设备允许上限，当前设备最大层数为 {maxLayers}。";
                }
            }
            catch
            {
            }

            return "开始扫描失败：扫描层数超过设备允许上限。";
        }

        private Truelight3DApiResult BuildSuccess(Truelight3DStatus status, string message)
        {
            _lastStatusSummary = message;
            return Truelight3DApiResult.SuccessResult(status, message);
        }

        private Truelight3DApiResult BuildFailure(Truelight3DStatus status, string message)
        {
            _lastStatusSummary = message;
            return Truelight3DApiResult.FailureResult(status, message);
        }

        private Truelight3DApiResult BuildException(string actionName, Exception ex)
        {
            string message = BuildExceptionMessage(actionName, ex);
            _lastStatusSummary = message;
            return Truelight3DApiResult.FailureResult(Truelight3DStatus.STATUS_ERROR, message);
        }

        private Truelight3DApiResult<T> BuildException<T>(string actionName, Exception ex)
        {
            string message = BuildExceptionMessage(actionName, ex);
            _lastStatusSummary = message;
            return Truelight3DApiResult<T>.FailureResult(Truelight3DStatus.STATUS_ERROR, message);
        }

        private static string BuildExceptionMessage(string actionName, Exception ex)
        {
            return ex switch
            {
                DllNotFoundException => $"{actionName}失败：AMSDK.dll 或其依赖库缺失。请确认已提供 AMSDK.dll 以及 PylonBase_v7_3.dll、GenApi_MD_VC141_v3_1_Basler_pylon.dll、opencv_world4110.dll、tbb12.dll、torch_cpu.dll、MSVCP140.dll、VCRUNTIME140.dll 等依赖，并放在主程序同级目录。",
                EntryPointNotFoundException => $"{actionName}失败：AMSDK 导出符号与当前对接签名不一致，需要重新核对厂商版本。",
                BadImageFormatException => $"{actionName}失败：AMSDK 位数与当前进程不匹配，请使用 64 位运行环境。",
                _ => $"{actionName}失败：{ex.Message}",
            };
        }

        private static bool TryConvertToUInt(object? value, out uint result)
        {
            switch (value)
            {
                case byte byteValue:
                    result = byteValue;
                    return true;
                case ushort ushortValue:
                    result = ushortValue;
                    return true;
                case uint uintValue:
                    result = uintValue;
                    return true;
                case int intValue when intValue >= 0:
                    result = (uint)intValue;
                    return true;
                case long longValue when longValue >= 0 && longValue <= uint.MaxValue:
                    result = (uint)longValue;
                    return true;
                case string stringValue when uint.TryParse(stringValue, out uint parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private static bool TryConvertToFloat(object? value, out float result)
        {
            switch (value)
            {
                case float floatValue:
                    result = floatValue;
                    return true;
                case double doubleValue:
                    result = (float)doubleValue;
                    return true;
                case decimal decimalValue:
                    result = (float)decimalValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case string stringValue when float.TryParse(stringValue, out float parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0f;
                    return false;
            }
        }

        private static int GetImageByteLength(int width, int height, int channel, int fallbackLength)
        {
            if (width <= 0 || height <= 0)
            {
                return Math.Max(0, fallbackLength);
            }

            int safeChannel = Math.Clamp(channel, 1, 4);
            int actualLength = checked(width * height * safeChannel);
            return Math.Min(actualLength, fallbackLength);
        }

        private void ResetConnectionState()
        {
            IsConnected = false;
            ImageWidth = 0;
            ImageHeight = 0;
        }

        private void TryDisconnectSilently()
        {
            if (_sdkHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                Truelight3DNativeMethods.Disconnect(_sdkHandle);
            }
            catch
            {
            }
        }
    }
}
