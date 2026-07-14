using ACS.SPiiPlusNET;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    public bool ConfigurePegOutput(AcsPegOutputConfig config, out string message)
    {
        if (!ValidatePegOutput(config, requireEnabled: false, out message))
        {
            return false;
        }

        if (!IsConnected)
        {
            message = "ACS is not connected; cannot configure PEG output.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        try
        {
            var axis = ToAcsAxis(config.Axis);
            _api.AssignPegNT(axis, config.EngineToEncoderBitCode, config.GeneralOutputBitCode);
            _api.AssignPegOutputsNT(axis, config.OutputIndex, config.OutputBitCode);
            message = $"PEG output configured for axis {config.Axis}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"configure PEG output for axis {config.Axis} failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool StartIncrementalPeg(AcsPegIncrementalRequest request, out string message)
    {
        if (request == null)
        {
            message = "incremental PEG request is null.";
            return false;
        }

        if (!ValidatePegOutput(request.Output, requireEnabled: true, out message)
            || !ValidateFinite(request.FirstPoint, nameof(request.FirstPoint), out message)
            || !ValidateFinite(request.LastPoint, nameof(request.LastPoint), out message)
            || !ValidatePositive(request.Interval, nameof(request.Interval), out message))
        {
            return false;
        }

        if (request.FirstPoint > request.LastPoint)
        {
            message = "incremental PEG FirstPoint must be less than or equal to LastPoint.";
            return false;
        }

        if (!IsConnected)
        {
            message = "ACS is not connected; cannot start incremental PEG.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        var axis = ToAcsAxis(request.Output.Axis);
        try
        {
            if (!ConfigurePegOutput(request.Output, out message))
            {
                return false;
            }

            // Incremental PEG is the ACS hardware path for one-dimensional equal-spacing pulses.
            _api.PegIncNTV2(
                request.Output.Flags,
                axis,
                request.Output.PulseWidth,
                request.FirstPoint,
                request.Interval,
                request.LastPoint,
                -1,
                0,
                -1,
                0,
                0,
                0,
                0,
                0);

            if (request.WaitReadyBeforeStart)
            {
                _api.WaitPegReadyNT(axis, request.Output.Timeout);
            }

            if (request.StartImmediately)
            {
                _api.StartPegNT(axis);
            }

            message = $"incremental PEG started for axis {request.Output.Axis}.";
            return true;
        }
        catch (Exception ex)
        {
            TryExecute(() => _api.StopPegNT(axis), $"stop PEG axis {request.Output.Axis}");
            message = $"start incremental PEG for axis {request.Output.Axis} failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool StartRandomPeg(AcsPegRandomRequest request, out string message)
    {
        if (request == null)
        {
            message = "random PEG request is null.";
            return false;
        }

        if (!ValidatePegOutput(request.Output, requireEnabled: true, out message)
            || !ValidateArrayName(request.PointArrayName, nameof(request.PointArrayName), out message)
            || !ValidateArrayName(request.StateArrayName, nameof(request.StateArrayName), out message))
        {
            return false;
        }

        var points = request.Points?.ToArray() ?? Array.Empty<double>();
        if (points.Length == 0)
        {
            message = "random PEG requires at least one point.";
            return false;
        }

        if (points.Any(value => !IsFinite(value)))
        {
            message = "random PEG points must be finite.";
            return false;
        }

        var states = request.States?.ToArray() ?? Array.Empty<int>();
        if (states.Length == 0)
        {
            states = Enumerable.Repeat(1, points.Length).ToArray();
        }

        if (states.Length != points.Length)
        {
            message = "random PEG state count must match point count.";
            return false;
        }

        if (!IsConnected)
        {
            message = "ACS is not connected; cannot start random PEG.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        var axis = ToAcsAxis(request.Output.Axis);
        var firstIndex = Math.Max(0, request.FirstIndex);
        var lastIndex = request.LastIndex ?? firstIndex + points.Length - 1;
        if (lastIndex < firstIndex || lastIndex - firstIndex + 1 != points.Length)
        {
            message = "random PEG first/last indexes must match point count.";
            return false;
        }

        try
        {
            if (!ConfigurePegOutput(request.Output, out message))
            {
                return false;
            }

            // Random PEG consumes controller arrays; point and state indexes must stay aligned.
            _api.WriteVariable(points, request.PointArrayName, ProgramBuffer.ACSC_NONE, firstIndex, lastIndex, -1, -1);
            _api.WriteVariable(states, request.StateArrayName, ProgramBuffer.ACSC_NONE, firstIndex, lastIndex, -1, -1);
            _api.PegRandomNTV2(
                request.Output.Flags,
                axis,
                request.Output.PulseWidth,
                request.Mode,
                firstIndex,
                lastIndex,
                request.PointArrayName,
                request.StateArrayName,
                -1,
                0,
                -1,
                0,
                0,
                0,
                0);

            if (request.WaitReadyBeforeStart)
            {
                _api.WaitPegReadyNT(axis, request.Output.Timeout);
            }

            if (request.StartImmediately)
            {
                _api.StartPegNT(axis);
            }

            message = $"random PEG started for axis {request.Output.Axis} with {points.Length} points.";
            return true;
        }
        catch (Exception ex)
        {
            TryExecute(() => _api.StopPegNT(axis), $"stop PEG axis {request.Output.Axis}");
            message = $"start random PEG for axis {request.Output.Axis} arrays {request.PointArrayName}/{request.StateArrayName} failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool StartEquidistantPeg2D(AcsPeg2DPathRequest request, out string message)
    {
        if (request == null)
        {
            message = "2D PEG request is null.";
            return false;
        }

        if (!ValidatePegOutput(request.Output, requireEnabled: true, out message)
            || !ValidateArrayName(request.PointArrayName, nameof(request.PointArrayName), out message)
            || !ValidateArrayName(request.StateArrayName, nameof(request.StateArrayName), out message))
        {
            return false;
        }

        if (IsFinite(request.PulseWidth) && request.PulseWidth > 0d)
        {
            request.Output.PulseWidth = request.PulseWidth;
        }

        // 2D equal spacing is generated in path length, then emitted through random PEG on one monotonic axis.
        if (!AcsPegPathSampler.TrySamplePath(request, out var pathPoints, out message))
        {
            message = $"sample 2D PEG path failed: {message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        if (!AcsPegPathSampler.TryProjectReferencePositions(pathPoints, request.ReferenceAxis, out var positions, out message))
        {
            message = $"project 2D PEG path failed: {message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        var states = Enumerable.Repeat(request.StateValue, positions.Length).ToArray();
        var randomRequest = new AcsPegRandomRequest
        {
            Output = request.Output,
            Points = positions,
            States = states,
            PointArrayName = request.PointArrayName,
            StateArrayName = request.StateArrayName,
            StartImmediately = request.StartImmediately,
            WaitReadyBeforeStart = true
        };

        return StartRandomPeg(randomRequest, out message);
    }

    public bool StopPeg(En_AxisNum axisId, out string message)
    {
        if (!IsConnected)
        {
            message = "ACS is not connected; cannot stop PEG.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        try
        {
            _api.StopPegNT(ToAcsAxis(axisId));
            message = $"PEG stopped for axis {axisId}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"stop PEG for axis {axisId} failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool WaitPegReady(En_AxisNum axisId, int timeout, out string message)
    {
        if (!IsConnected)
        {
            message = "ACS is not connected; cannot wait PEG ready.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        try
        {
            _api.WaitPegReadyNT(ToAcsAxis(axisId), Math.Max(1000, timeout));
            message = $"PEG ready for axis {axisId}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"wait PEG ready for axis {axisId} failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public override bool ControlPosComparison(bool On_Off, PosComparisonOutputParam param)
    {
        if (param == null)
        {
            Console.WriteLine("ACS position comparison failed: parameter is null.");
            return false;
        }

        var axisId = ToAxisNumFromOneBased(param.compare_X);
        var output = CreatePegOutputFromPositionComparison(param, axisId);

        if (!On_Off)
        {
            return StopPeg(output.Axis, out _);
        }

        // Compatibility path for the legacy Googol-style position comparison model.
        var compareData = GetPositionCompareData(param).ToList();
        if (compareData.Count > 0)
        {
            var randomRequest = new AcsPegRandomRequest
            {
                Output = output,
                Points = compareData.Select(data => (double)data.PosX).ToList(),
                States = compareData.Select(data => ToPegState(data)).ToList(),
                PointArrayName = Options.DefaultPegPointArrayName,
                StateArrayName = Options.DefaultPegStateArrayName,
                StartImmediately = true,
                WaitReadyBeforeStart = true
            };

            return StartRandomPeg(randomRequest, out _);
        }

        if (param.compareDimension == 1 && param.syncPos > 0)
        {
            var incrementalRequest = new AcsPegIncrementalRequest
            {
                Output = output,
                FirstPoint = 0,
                Interval = param.syncPos,
                LastPoint = param.syncPos,
                StartImmediately = true,
                WaitReadyBeforeStart = true
            };

            return StartIncrementalPeg(incrementalRequest, out _);
        }

        if (param.compareDimension == 2)
        {
            Console.WriteLine("ACS 2D equidistant position comparison requires StartEquidistantPeg2D with path start/end/arc/polyline context.");
            return false;
        }

        Console.WriteLine($"ACS position comparison dimension {param.compareDimension} is not supported.");
        return false;
    }

    private AcsPegOutputConfig CreatePegOutputFromPositionComparison(PosComparisonOutputParam param, En_AxisNum axisId)
    {
        EnsureOptions();

        var outputIndex = Math.Max(0, param.psoIndex - 1);
        var configured = Options.PegOutputs?
            .FirstOrDefault(output => output.OutputIndex == outputIndex)
            ?? Options.PegOutputs?.FirstOrDefault(output => output.Axis == axisId);

        var output = configured != null
            ? ClonePegOutput(configured)
            : new AcsPegOutputConfig { Axis = axisId, IsEnabled = true };

        output.Axis = configured?.Axis ?? axisId;
        output.OutputIndex = outputIndex;
        output.PulseWidth = Math.Max(1d, param.comparePulseWidth);
        return output;
    }

    private static AcsPegOutputConfig ClonePegOutput(AcsPegOutputConfig source)
    {
        return new AcsPegOutputConfig
        {
            Axis = source.Axis,
            EngineToEncoderBitCode = source.EngineToEncoderBitCode,
            GeneralOutputBitCode = source.GeneralOutputBitCode,
            OutputIndex = source.OutputIndex,
            OutputBitCode = source.OutputBitCode,
            PulseWidth = source.PulseWidth,
            Timeout = source.Timeout,
            Flags = source.Flags,
            IsEnabled = source.IsEnabled
        };
    }

    private static IEnumerable<PosCompareData> GetPositionCompareData(PosComparisonOutputParam param)
    {
        if (param.PosCompareDatas != null && param.PosCompareDatas.Count > 0)
        {
            return param.PosCompareDatas;
        }

        if (param.posCompareDatas != null && param.posCompareDatas.Count > 0)
        {
            return param.posCompareDatas;
        }

        return Enumerable.Empty<PosCompareData>();
    }

    private static int ToPegState(PosCompareData data)
    {
        if (data.Hso != 0)
        {
            return data.Hso;
        }

        return data.Gpo != 0 ? data.Gpo : 1;
    }

    private static En_AxisNum ToAxisNumFromOneBased(short axisNo)
    {
        var zeroBased = Math.Max(0, axisNo - 1);
        return Enum.IsDefined(typeof(En_AxisNum), zeroBased)
            ? (En_AxisNum)zeroBased
            : En_AxisNum.X;
    }

    private static bool ValidatePegOutput(AcsPegOutputConfig config, bool requireEnabled, out string message)
    {
        if (config == null)
        {
            message = "PEG output config is null.";
            return false;
        }

        if (requireEnabled && !config.IsEnabled)
        {
            message = $"PEG output for axis {config.Axis} is disabled.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        if (!ValidatePositive(config.PulseWidth, nameof(config.PulseWidth), out message))
        {
            return false;
        }

        if (config.Timeout < 1000)
        {
            config.Timeout = 1000;
        }

        message = string.Empty;
        return true;
    }

    private static bool ValidateArrayName(string value, string name, out string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            message = $"{name} must not be empty.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool ValidateFinite(double value, string name, out string message)
    {
        if (!IsFinite(value))
        {
            message = $"{name} must be finite.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool ValidatePositive(double value, string name, out string message)
    {
        if (!IsFinite(value) || value <= 0d)
        {
            message = $"{name} must be greater than zero.";
            return false;
        }

        message = string.Empty;
        return true;
    }
}
