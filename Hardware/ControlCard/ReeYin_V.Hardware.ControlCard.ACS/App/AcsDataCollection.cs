using ACS.SPiiPlusNET;
using ReeYin_V.Core.Services.Project;
using System;

namespace ReeYin_V.Hardware.ControlCard.ACS.App;

public partial class AcsControlCard
{
    public bool StartDataCollection(AcsDataCollectionRequest request, out string message)
    {
        if (!ValidateDataCollectionRequest(request, requireVariables: true, out message))
        {
            return false;
        }

        if (!IsConnected)
        {
            message = "ACS is not connected; cannot start Data Collection.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        try
        {
            _api.DataCollectionExt(
                request.Flags,
                ToAcsAxis(request.Axis),
                request.ArrayName,
                request.SampleCount,
                request.Period,
                request.Variables);

            message = $"Data Collection started on axis {request.Axis}, array {request.ArrayName}.";
            return true;
        }
        catch (Exception ex)
        {
            TryExecute(() => _api.StopCollect(), "stop Data Collection after start failure");
            message = $"start Data Collection on axis {request.Axis}, array {request.ArrayName} failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool StopDataCollection(out string message)
    {
        if (!IsConnected)
        {
            message = "ACS is not connected; cannot stop Data Collection.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        try
        {
            _api.StopCollect();
            message = "Data Collection stopped.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"stop Data Collection failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool WaitDataCollectionEnd(En_AxisNum axisId, int timeout, out string message)
    {
        if (!IsConnected)
        {
            message = "ACS is not connected; cannot wait Data Collection end.";
            Console.WriteLine($"ACS {message}");
            return false;
        }

        try
        {
            _api.WaitCollectEndExt(Math.Max(1000, timeout), ToAcsAxis(axisId));
            message = $"Data Collection ended on axis {axisId}.";
            return true;
        }
        catch (Exception ex)
        {
            TryExecute(() => _api.StopCollect(), "stop Data Collection after wait failure");
            message = $"wait Data Collection end on axis {axisId} failed: {ex.Message}";
            Console.WriteLine($"ACS {message}");
            return false;
        }
    }

    public bool ReadDataCollection(AcsDataCollectionRequest request, out AcsDataCollectionResult result)
    {
        result = CreateDataCollectionResult(request);
        if (!ValidateDataCollectionRequest(request, requireVariables: false, out var message))
        {
            result.Message = message;
            return false;
        }

        if (!IsConnected)
        {
            result.Message = "ACS is not connected; cannot read Data Collection.";
            Console.WriteLine($"ACS {result.Message}");
            return false;
        }

        try
        {
            // Single-variable collections usually read back as vectors.
            result.Values = _api.ReadRealVector(
                request.ArrayName,
                0,
                request.SampleCount - 1,
                ProgramBuffer.ACSC_NONE) ?? Array.Empty<double>();
            result.Success = true;
            result.Message = $"Data Collection vector {request.ArrayName} read {result.Values.Length} samples.";
            return true;
        }
        catch (Exception vectorEx)
        {
            try
            {
                // Multi-variable collections may read back as a matrix; flatten for callers that only need raw values.
                var matrix = _api.ReadRealMatrix(
                    request.ArrayName,
                    0,
                    request.SampleCount - 1,
                    0,
                    0,
                    ProgramBuffer.ACSC_NONE);
                result.MatrixValues = FlattenMatrix(matrix);
                result.Success = true;
                result.Message = $"Data Collection matrix {request.ArrayName} read {result.MatrixValues.Length} values.";
                return true;
            }
            catch (Exception matrixEx)
            {
                result.Message = $"read Data Collection array {request.ArrayName} failed: vector={vectorEx.Message}; matrix={matrixEx.Message}";
                Console.WriteLine($"ACS {result.Message}");
                return false;
            }
        }
    }

    public bool RunDataCollection(AcsDataCollectionRequest request, out AcsDataCollectionResult result)
    {
        result = CreateDataCollectionResult(request);

        if (!StartDataCollection(request, out var message))
        {
            result.Message = message;
            return false;
        }

        if (!WaitDataCollectionEnd(request.Axis, request.Timeout, out message))
        {
            TryExecute(() => _api.StopCollect(), "stop Data Collection after run failure");
            result.Message = message;
            return false;
        }

        return ReadDataCollection(request, out result);
    }

    private static AcsDataCollectionResult CreateDataCollectionResult(AcsDataCollectionRequest? request)
    {
        return new AcsDataCollectionResult
        {
            ArrayName = request?.ArrayName ?? string.Empty,
            SampleCount = request?.SampleCount ?? 0
        };
    }

    private static double[] FlattenMatrix(double[,]? matrix)
    {
        if (matrix == null)
        {
            return Array.Empty<double>();
        }

        // Preserve ACS row/column order while converting to the result model's flat array.
        var values = new double[matrix.Length];
        var index = 0;
        for (var row = 0; row < matrix.GetLength(0); row++)
        {
            for (var column = 0; column < matrix.GetLength(1); column++)
            {
                values[index++] = matrix[row, column];
            }
        }

        return values;
    }

    private static bool ValidateDataCollectionRequest(
        AcsDataCollectionRequest? request,
        bool requireVariables,
        out string message)
    {
        if (request == null)
        {
            message = "Data Collection request is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ArrayName))
        {
            message = "Data Collection array name must not be empty.";
            return false;
        }

        if (request.SampleCount <= 0)
        {
            message = "Data Collection sample count must be greater than zero.";
            return false;
        }

        if (!IsFinite(request.Period) || request.Period <= 0d)
        {
            message = "Data Collection period must be greater than zero.";
            return false;
        }

        if (requireVariables && string.IsNullOrWhiteSpace(request.Variables))
        {
            message = "Data Collection variables must not be empty.";
            return false;
        }

        if (request.Timeout < 1000)
        {
            request.Timeout = 1000;
        }

        message = string.Empty;
        return true;
    }
}
