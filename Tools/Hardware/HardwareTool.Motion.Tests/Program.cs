using HardwareTool.Motion.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.Models;
using System.Collections.ObjectModel;
using System.Reflection;

namespace HardwareTool.Motion.Tests;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("Target map uses coordinate position order when physical axis numbers are not sequential", TargetMapUsesCoordinatePositionOrder),
        };

        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"FAIL {test.Name}");
                Console.WriteLine(ex.Message);
            }
        }

        return failed == 0 ? 0 : 1;
    }

    private static void TargetMapUsesCoordinatePositionOrder()
    {
        var model = new MotionModel
        {
            ControlCard = CreateControlCard()
        };

        var coordinate = new CoordinatePos
        {
            TargetPos = [12.34d, 56.78d]
        };

        var success = InvokeTryBuildTargetPositionMap(model, coordinate, out var targetPositions, out var errorMessage);

        AssertTrue(success, $"Expected target map to be built, but got: {errorMessage}");
        AssertEqual(12.34d, targetPositions[En_AxisNum.X], "X target position");
        AssertEqual(56.78d, targetPositions[En_AxisNum.Y], "Y target position");
    }

    private static TestControlCard CreateControlCard()
    {
        return new TestControlCard
        {
            Config = new ControlCardConfig
            {
                AllAxis = new ObservableCollection<SingleAxisParam>
                {
                    new() { AxisNum = En_AxisNum.X, AxisNo = 3 },
                    new() { AxisNum = En_AxisNum.Y, AxisNo = 1 },
                }
            }
        };
    }

    private static bool InvokeTryBuildTargetPositionMap(
        MotionModel model,
        CoordinatePos coordinate,
        out Dictionary<En_AxisNum, double> targetPositions,
        out string errorMessage)
    {
        var method = typeof(MotionModel).GetMethod(
            "TryBuildTargetPositionMap",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(CoordinatePos),
                typeof(Dictionary<En_AxisNum, double>).MakeByRefType(),
                typeof(string).MakeByRefType()
            ],
            modifiers: null);

        if (method == null)
        {
            throw new InvalidOperationException("TryBuildTargetPositionMap overload was not found.");
        }

        object?[] args = [coordinate, null, null];
        var success = (bool)method.Invoke(model, args)!;
        targetPositions = (Dictionary<En_AxisNum, double>)args[1]!;
        errorMessage = (string)args[2]!;
        return success;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual(double expected, double actual, string name)
    {
        if (Math.Abs(expected - actual) > 0.000001d)
        {
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}.");
        }
    }

    private sealed class TestControlCard : ControlCardBase
    {
        protected override bool DoInit() => true;

        protected override void DoConfigure()
        {
        }

        protected override void DoStop(En_AxisNum? axisType, AxisStopMode stopMode)
        {
        }

        protected override void DoClose()
        {
        }

        protected override bool DoGetAxisEnable(En_AxisNum axisType) => true;

        protected override bool DoSetAxisEnable(En_AxisNum axisType, bool v) => true;

        protected override bool DoGetAxisStopped(En_AxisNum axisType) => true;

        protected override bool DoMoveAxis(En_AxisNum axisType, double um) => true;

        protected override bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection) => true;

        protected override bool DoGoHome(out string message)
        {
            message = string.Empty;
            return true;
        }
    }
}
