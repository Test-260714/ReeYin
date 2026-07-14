using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.ACS.App;
using ReeYin_V.Hardware.ControlCard.Models;
using ACS.SPiiPlusNET;
using System.IO;

Run("line sampling keeps equal path spacing", TestLineSampling);
Run("arc sampling keeps equal path spacing", TestArcSampling);
Run("polyline sampling crosses segment boundaries correctly", TestPolylineSampling);
Run("reference projection rejects non-monotonic paths", TestReferenceProjectionRejectsNonMonotonicPath);
Run("ACS options add PEG and data collection defaults", TestOptionsDefaults);
Run("ACS init ensures D-Buffer LCI declarations", TestAcsInitEnsuresDBufferLciDeclarations);
Run("ACS options persist LCI test defaults", TestAcsOptionsPersistLciTestDefaults);
Run("ACS config view model exposes LCI fixed-distance pulse surface", TestAcsConfigViewModelLciFixedDistancePulseSurface);
Run("ACS config view binds LCI fixed-distance pulse surface", TestAcsConfigViewLciFixedDistancePulseSurface);
Run("ACS config view model initializes selected card options", TestAcsConfigViewModelInitializesOptions);
Run("ACS config view model exposes buffer script editor state", TestAcsConfigViewModelBufferEditorState);
Run("ACS config view model exposes D-Buffer editor surface", TestAcsConfigViewModelDBufferEditorSurface);
Run("ACS buffer backend exposes clear and status model", TestAcsBufferBackendExposesClearAndStatusModel);
Run("ACS buffer backend exposes D-Buffer lookup", TestAcsBufferBackendExposesDBufferLookup);
Run("ACS D-Buffer declaration builder canonicalizes required declarations", TestAcsDBufferDeclarationBuilderCanonicalizesRequiredDeclarations);
Run("ACS D-Buffer ensure uses buffer editor flow and verifies result", TestAcsDBufferEnsureUsesBufferEditorFlowAndVerifiesResult);
Run("ACS buffer script dialog exposes program controls", TestAcsBufferScriptDialogExposesProgramControls);
Run("ACS buffer script dialog uses dropdown and D-Buffer button", TestAcsBufferScriptDialogUsesDropdownAndDBufferButton);
Run("ACS config view model exposes connect command", TestAcsConfigViewModelExposesConnectCommand);
Run("ACS config view binds connect command", TestAcsConfigViewBindsConnectCommand);
Run("ACS config view model exposes merged test command surface", TestAcsConfigViewModelMergedCommandSurface);
Run("ACS config view binds fixed monitor panel", TestAcsConfigViewBindsFixedMonitorPanel);
Run("ACS config view binds selected-axis home buffer editor", TestAcsConfigViewBindsSelectedAxisHomeBufferEditor);
Run("ACS config view combines axis controls", TestAcsConfigViewCombinesAxisControls);
Run("ACS config view model exposes selected-axis speed rows", TestAcsConfigViewModelExposesSelectedAxisSpeedRows);
Run("ControlCard speed types expose custom speed setting", TestControlCardSpeedTypesExposeCustomSetting);
Run("ACS config view uses selected-axis speed table", TestAcsConfigViewUsesSelectedAxisSpeedTable);
Run("ACS config view axis tab uses scrollable non-overlapping layout", TestAcsConfigViewAxisTabUsesScrollableNonOverlappingLayout);
Run("ACS config view axis tab uses three-column workspace", TestAcsConfigViewAxisTabUsesThreeColumnWorkspace);
Run("ACS config view axis tab keeps fixed debug panel", TestAcsConfigViewAxisTabKeepsFixedDebugPanel);
Run("ACS config view axis tab keeps ACS-specific settings", TestAcsConfigViewAxisTabKeepsAcsSpecificSettings);
Run("ACS config view removes ACSPL transaction controls", TestAcsConfigViewRemovesAcsplTransactionControls);
Run("ACS options expose XY combined home buffer defaults", TestAcsOptionsExposeXyCombinedHomeBufferDefaults);
Run("ACS XY combined home script matches reference", TestAcsXyCombinedHomeScriptMatchesReference);
Run("ACS GoHome uses XY combined buffer and keeps other axes per-axis", TestAcsGoHomeUsesXyCombinedBuffer);
Run("ACS GoHome ensures D-Buffer LCI declarations after reset", TestAcsGoHomeEnsuresDBufferLciDeclarationsAfterReset);
Run("ACS config view binds XY combined home buffer", TestAcsConfigViewBindsXyCombinedHomeBuffer);
Run("ACS config view removes obsolete PEG home and interpolation entries", TestAcsConfigViewRemovesObsoleteEntries);
Run("ACS relative Move uses ACS motion pipeline directly", TestAcsRelativeMoveUsesAcsMotionPipelineDirectly);
Run("ACS relative Move applies direction to signed distance", TestAcsRelativeMoveAppliesDirectionToSignedDistance);
Run("ACS Jog writes and verifies selected speed profile", TestAcsJogWritesAndVerifiesSelectedSpeedProfile);
Run("ACS options expose interpolation Buffer defaults", TestAcsOptionsExposeInterpolationBufferDefaults);
Run("ACS interpolation uses Buffer scripts instead of SDK interpolation APIs", TestAcsInterpolationUsesBufferScripts);
Run("ACS line interpolation Buffer matches LCI PTP reference script", TestAcsLineInterpolationBufferMatchesLciPtpReferenceScript);
Run("ACS line interpolation Buffer uses configured Work speed profile", TestAcsLineInterpolationBufferUsesConfiguredWorkSpeedProfile);
Run("ACS line interpolation exposes per-move LCI pulse output", TestAcsLineInterpolationExposesPerMoveLciPulseOutput);
Run("ACS line interpolation passes pulse output into Buffer script builders", TestAcsLineInterpolationPassesPulseOutputToBufferScripts);
Run("ACS line interpolation pulse output script matches fixed-distance LCI flow", TestAcsLineInterpolationPulseOutputScriptMatchesFixedDistanceLciFlow);
Run("ACS line interpolation pulse output defaults pulse window to move length", TestAcsLineInterpolationPulseOutputDefaultsPulseWindowToMoveLength);
Run("ACS coordinated motion propagates Buffer timeout", TestAcsCoordinatedMotionPropagatesBufferTimeout);
Run("ACS interpolation rejects invalid explicit Buffer number", TestAcsInterpolationRejectsInvalidExplicitBufferNumber);
Run("Axis view model uses coordinated motion capability before fallback", TestAxisViewModelUsesCoordinatedMotionCapability);
Run("ACS XSEG methods stay ACS-specific", TestAcsXsegMethodsStayAcsSpecific);
Run("ControlCardBase applies relative move direction", TestControlCardBaseAppliesRelativeMoveDirection);
Run("ControlCardBase returns home failure", TestControlCardBaseReturnsHomeFailure);
Run("ControlCardBase move wait times out", TestControlCardBaseMoveWaitTimesOut);
Run("ControlCardBase invokes config change hook", TestControlCardBaseInvokesConfigChangeHook);
Run("ControlCardConfig model supports startup auto reset", TestControlCardConfigModelSupportsStartupAutoReset);
Run("ControlCardConfig view binds startup auto reset", TestControlCardConfigViewBindsStartupAutoReset);
Run("ControlCardConfig model supports AxisView IO Jog settings", TestControlCardConfigModelSupportsAxisViewIoJogSettings);
Run("ControlCardConfig view binds AxisView IO Jog settings", TestControlCardConfigViewBindsAxisViewIoJogSettings);
Run("AxisView IO Jog controller maps input directions", TestAxisViewIoJogControllerMapsInputDirections);
Run("AxisView IO Jog controller honors custom ports and enable switch", TestAxisViewIoJogControllerHonorsCustomPortsAndEnableSwitch);
Run("AxisView IO Jog controller stops on multi-direction input", TestAxisViewIoJogControllerStopsOnMultiDirectionInput);
Run("AxisView integrates IO Jog lifecycle", TestAxisViewIntegratesIoJogLifecycle);
Run("AxisView IO Jog review fixes", TestAxisViewIoJogReviewFixes);
Run("ControlCardBase publishes global reset overlay events", TestControlCardBasePublishesGlobalResetOverlayEvents);
Run("Main view blocks whole screen while control-card reset runs", TestMainViewBlocksWholeScreenWhileControlCardResetRuns);
Run("Initialize view shows startup control-card reset prompt without full-screen overlay", TestInitializeViewShowsStartupControlCardResetPrompt);
Run("ControlCard exposes sync capability interfaces without polluting IControlCard", TestControlCardSyncCapabilityInterfaces);
Run("ControlCard exposes sync request models", TestControlCardSyncRequestModels);
Run("ACS control card implements sync capability interfaces", TestAcsControlCardImplementsSyncCapabilityInterfaces);
Run("ACS sync adapter maps coordinate array pulse", TestAcsSyncAdapterMapsCoordinateArrayPulse);
Run("ACS reuses base control-card helpers", TestAcsReusesBaseControlCardHelpers);
Run("Googol control card implements sync capability interfaces", TestGoogolControlCardImplementsSyncCapabilityInterfaces);
Run("Googol sync adapter keeps FIFO and position compare mapping", TestGoogolSyncAdapterKeepsFifoAndPositionCompareMapping);
Run("CoordinateCache view model derives coordinate values from configured axes", TestCoordinateCacheViewModelDerivesValuesFromConfiguredAxes);
Run("CoordinateCache view model guards selection and caches commands", TestCoordinateCacheViewModelGuardsSelectionAndCachesCommands);
Run("CoordinateCache view model uses coordinated motion capability before fallback", TestCoordinateCacheViewModelUsesCoordinatedMotionCapability);
Run("CoordinateCache resolves only initialized connected control card", TestCoordinateCacheResolvesOnlyInitializedConnectedControlCard);
Run("ACS LCI scripts use configured motion profiles", TestAcsLciScriptsUseConfiguredMotionProfiles);
Run("ACS LCI falls back to Work speed when profile is unset", TestAcsLciFallsBackToWorkSpeedWhenProfileUnset);
Run("ACS config view binds LCI motion profile config", TestAcsConfigViewBindsLciMotionProfileConfig);
Run("AxisView refreshes and displays actual speed", TestAxisViewRefreshesAndDisplaysActualSpeed);
Run("AxisView persists selected speed mode immediately", TestAxisViewPersistsSelectedSpeedModeImmediately);
Run("ACS control card reads feedback speed", TestAcsControlCardReadsFeedbackSpeed);
Run("Googol control card reads encoder speed", TestGoogolControlCardReadsEncoderSpeed);
Run("Googol DoGoHome supports startup auto reset", TestGoogolDoGoHomeSupportsStartupAutoReset);
Run("ControlCardBase sizes speed buffer by physical axis number", TestControlCardBaseSizesSpeedBufferByPhysicalAxisNo);
Run("WaferFlatness point mode uses sync trigger capabilities", TestWaferFlatnessPointModeUsesSyncTriggerCapabilities);
Run("WaferFlatness point mode removes ACS reflection path", TestWaferFlatnessPointModeRemovesAcsReflectionPath);
Run("WaferFlatness synchronized point mode preserves input order", TestWaferFlatnessSynchronizedPointModePreservesInputOrder);
Run("Motion tool keeps ACS dependency out of project references", TestMotionToolKeepsAcsDependencyOut);
Run("Motion model exposes axis-aware target mapping helpers", TestMotionModelAxisAwareTargetMappingHelpers);
Run("Motion arc execution passes complete ACS-compatible parameters", TestMotionArcExecutionPassesCompleteParameters);
Run("Motion execution uses focused movement dispatch helpers", TestMotionExecutionUsesFocusedMovementDispatch);
Run("ACS LCI fixed-distance pulse script matches reference PTP pulse flow", TestAcsLciFixedDistancePulseScriptMatchesReferencePtpFlow);
Run("ACS LCI fixed-distance pulse defaults pulse window to move length", TestAcsLciFixedDistancePulseDefaultsPulseWindowToMoveLength);
Run("ACS LCI fixed-distance pulse runner uses buffer pipeline", TestAcsLciFixedDistancePulseRunnerUsesBufferPipeline);
Run("ACS LCI segment circle script matches reference XSEG flow", TestAcsLciSegmentCircleScriptMatchesReferenceXsegFlow);
Run("ACS LCI segment circle runner uses buffer pipeline", TestAcsLciSegmentCircleRunnerUsesBufferPipeline);
Run("ACS LCI coordinate array pulse script matches reference XSEG flow", TestAcsLciCoordinateArrayPulseScriptMatchesReferenceXsegFlow);
Run("ACS LCI coordinate array pulse runner writes arrays before run", TestAcsLciCoordinateArrayPulseRunnerWritesArraysBeforeRun);
Run("ACS monitor view model exposes status surface", TestAcsMonitorViewModelStatusSurface);
Run("ACS monitor view binds core status fields", TestAcsMonitorViewBindsCoreStatusFields);
Run("ACS monitor read-only text boxes bind one-way", TestAcsMonitorViewReadOnlyTextBoxesBindOneWay);
Run("ACS config view model exposes operation commands", TestAcsConfigViewModelCommandSurface);
Run("ACS config view model exposes hold-to-move command surface", TestAcsConfigViewModelHoldToMoveSurface);
Run("ACS config view model removes transaction command surface", TestAcsConfigViewModelRemovesTransactionCommandSurface);
Run("ACS config Buffer dialog exposes operation controls", TestAcsConfigViewBufferDialogExposesProgramControls);
Run("ACS config view binds hold-to-move buttons", TestAcsConfigViewBindsHoldToMoveButtons);
Run("ACS config view fixes LCI Chinese labels", TestAcsConfigViewFixesLciChineseLabels);
Run("ACS config view read-only text boxes bind one-way", TestAcsConfigViewReadOnlyTextBoxesBindOneWay);
Run("ACS test view files and view model are removed", TestAcsTestViewFilesAndViewModelAreRemoved);
Run("ACS source files use readable Chinese text", TestAcsSourceFilesUseReadableChineseText);
Run("AxisView matches displayed axis state by axis type", TestAxisViewMatchesDisplayedAxisStateByAxisType);
Run("AxisView speed monitor common surface", TestAxisViewSpeedMonitorCommonSurface);
Run("AxisView exposes missing displayed axes as disabled", TestAxisViewExposesMissingDisplayedAxesAsDisabled);
Run("WaferFlatness exposes ACS LCI fixed-distance pulse config", TestWaferFlatnessAcsLciPulseConfigSurface);
Run("WaferFlatness exposes ACS LCI coordinate-array point config", TestWaferFlatnessAcsLciCoordinateArrayConfigSurface);
Run("WaferFlatness uses ACS LCI fixed-distance pulse runner", TestWaferFlatnessUsesAcsLciFixedDistancePulseRunner);
Run("WaferFlatness config view binds ACS LCI pulse parameters", TestWaferFlatnessConfigViewBindsAcsLciPulseParameters);
Run("WaferFlatness config view binds ACS LCI coordinate-array point parameters", TestWaferFlatnessConfigViewBindsAcsLciCoordinateArrayParameters);
Run("WaferFlatness config view model exposes hardware-specific visibility", TestWaferFlatnessConfigViewModelHardwareVisibilitySurface);
Run("WaferFlatness config view uses hardware-specific parameter visibility", TestWaferFlatnessConfigViewUsesHardwareSpecificVisibility);

Console.WriteLine("ACS PEG/DataCollection tests passed.");

void TestLineSampling()
{
    var points = AcsPegPathSampler.SampleLine(new AcsPoint2D(0, 0), new AcsPoint2D(10, 0), 2.5);

    AssertEqual(5, points.Count, "line sample count");
    AssertPoint(points[0], 0, 0, "line point 0");
    AssertPoint(points[1], 2.5, 0, "line point 1");
    AssertPoint(points[2], 5, 0, "line point 2");
    AssertPoint(points[3], 7.5, 0, "line point 3");
    AssertPoint(points[4], 10, 0, "line point 4");
}

void TestArcSampling()
{
    var interval = Math.PI * 10d / 4d;
    var points = AcsPegPathSampler.SampleArcCenter(
        new AcsPoint2D(10, 0),
        new AcsPoint2D(0, 10),
        new AcsPoint2D(0, 0),
        DirOfRotation.逆时针,
        interval);

    AssertEqual(3, points.Count, "arc sample count");
    AssertPoint(points[0], 10, 0, "arc point 0");
    AssertPoint(points[1], 10 / Math.Sqrt(2), 10 / Math.Sqrt(2), "arc point 1", 0.001);
    AssertPoint(points[2], 0, 10, "arc point 2", 0.001);
}

void TestPolylineSampling()
{
    var points = AcsPegPathSampler.SamplePolyline(
        new[]
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(3, 0),
            new AcsPoint2D(3, 4)
        },
        2.5);

    AssertEqual(4, points.Count, "polyline sample count");
    AssertPoint(points[0], 0, 0, "polyline point 0");
    AssertPoint(points[1], 2.5, 0, "polyline point 1");
    AssertPoint(points[2], 3, 2, "polyline point 2");
    AssertPoint(points[3], 3, 4, "polyline point 3");
}

void TestReferenceProjectionRejectsNonMonotonicPath()
{
    var points = new[]
    {
        new AcsPoint2D(0, 0),
        new AcsPoint2D(2, 1),
        new AcsPoint2D(1, 2)
    };

    var ok = AcsPegPathSampler.TryProjectReferencePositions(points, AcsPegReferenceAxis.X, out _, out var message);

    AssertFalse(ok, "non-monotonic projection should fail");
    AssertTrue(message.Contains("monotonic", StringComparison.OrdinalIgnoreCase), "projection message mentions monotonicity");
}

void TestOptionsDefaults()
{
    var options = new AcsControlCardOptions();
    options.EnsurePegOutputs(new[] { En_AxisNum.X, En_AxisNum.Y });

    AssertEqual(2, options.PegOutputs.Count, "peg output count");
    AssertEqual(En_AxisNum.X, options.PegOutputs[0].Axis, "peg output X axis");
    AssertEqual("RY_PEG_POINTS", options.DefaultPegPointArrayName, "default point array");
    AssertEqual("RY_PEG_STATES", options.DefaultPegStateArrayName, "default state array");
    AssertEqual("RY_DC_DATA", options.DefaultDataCollectionArrayName, "default data collection array");
    AssertEqual(1000, options.DefaultDataCollectionSampleCount, "default sample count");
}

void TestAcsOptionsPersistLciTestDefaults()
{
    var options = new AcsControlCardOptions();

    AssertTrue(options.LciFixedDistancePulse != null, "fixed-distance pulse config should be available");
    AssertEqual(10, options.LciFixedDistancePulse.BufferNo, "default fixed pulse buffer");
    AssertEqual(0, options.LciFixedDistancePulse.AxisX, "default fixed pulse X axis");
    AssertEqual(1, options.LciFixedDistancePulse.AxisY, "default fixed pulse Y axis");
    AssertEqual(0.01d, options.LciFixedDistancePulse.PulseWidth, "default fixed pulse width");
    AssertEqual(1d, options.LciFixedDistancePulse.Interval, "default fixed pulse interval");
    AssertEqual(0d, options.LciFixedDistancePulse.StartDistance, "default fixed pulse start distance");
    AssertEqual(0d, options.LciFixedDistancePulse.EndDistance, "default fixed pulse end distance");
    AssertEqual(true, options.LciFixedDistancePulse.RouteConfigOutput, "default fixed pulse output routing");
    AssertEqual(0, options.LciFixedDistancePulse.ConfigOutputIndex, "default fixed pulse config output index");
    AssertEqual(7, options.LciFixedDistancePulse.ConfigOutputCode, "default fixed pulse config output code");
    AssertEqual(60000, options.LciFixedDistancePulse.Timeout, "default fixed pulse timeout");
    AssertEqual(0d, options.LciFixedDistancePulse.Velocity, "default fixed pulse velocity should be unset");
    AssertEqual(0d, options.LciFixedDistancePulse.Acceleration, "default fixed pulse acceleration should be unset");
    AssertEqual(0d, options.LciFixedDistancePulse.Deceleration, "default fixed pulse deceleration should be unset");
    AssertEqual(0d, options.LciFixedDistancePulse.KillDeceleration, "default fixed pulse kill deceleration should be unset");
    AssertEqual(0d, options.LciFixedDistancePulse.Jerk, "default fixed pulse jerk should be unset");
    AssertContains(options.LciFixedDistancePulse.PointsText, "0,0", "default fixed pulse points should include start");
    AssertContains(options.LciFixedDistancePulse.PointsText, "100,100", "default fixed pulse points should include end");

    AssertTrue(options.LciSegmentCircle != null, "segment circle config should be available");
    AssertEqual(10, options.LciSegmentCircle.BufferNo, "default circle buffer");
    AssertEqual(0, options.LciSegmentCircle.AxisX, "default circle X axis");
    AssertEqual(1, options.LciSegmentCircle.AxisY, "default circle Y axis");
    AssertEqual(0d, options.LciSegmentCircle.Velocity, "default circle velocity should be unset");
    AssertEqual(0d, options.LciSegmentCircle.Acceleration, "default circle acceleration should be unset");
    AssertEqual(0d, options.LciSegmentCircle.Deceleration, "default circle deceleration should be unset");
    AssertEqual(0d, options.LciSegmentCircle.KillDeceleration, "default circle kill deceleration should be unset");
    AssertEqual(0d, options.LciSegmentCircle.Jerk, "default circle jerk should be unset");
    AssertEqual(0d, options.LciSegmentCircle.StartX, "default circle XSEG start X");
    AssertEqual(0d, options.LciSegmentCircle.StartY, "default circle XSEG start Y");
    AssertEqual(10d, options.LciSegmentCircle.CenterX, "default circle center X");
    AssertEqual(5d, options.LciSegmentCircle.CenterY, "default circle center Y");
    AssertEqual(5d, options.LciSegmentCircle.Radius, "default circle radius");
    AssertEqual(1, options.LciSegmentCircle.GateActiveState, "default circle gate state");
    AssertEqual(60000, options.LciSegmentCircle.Timeout, "default circle timeout");
}

void TestAxisViewMatchesDisplayedAxisStateByAxisType()
{
    var axes = new[]
    {
        new SingleAxisParam { AxisNum = En_AxisNum.Z1, AxisNo = 4, CurPos = 41.25, IsEnable = true },
        new SingleAxisParam { AxisNum = En_AxisNum.Y, AxisNo = 2, CurPos = 12.5, IsEnable = true },
        new SingleAxisParam { AxisNum = En_AxisNum.X, AxisNo = 1, CurPos = -3.75, IsEnable = false }
    };

    var positions = AxisViewAxisMatcher.BuildPositionSnapshot(axes);
    var statuses = AxisViewAxisMatcher.BuildEnableSnapshot(axes);

    AssertEqual(5, positions.Length, "displayed position count");
    AssertClose(-3.75, positions[0], "X position comes from X axis config");
    AssertClose(12.5, positions[1], "Y position comes from Y axis config");
    AssertClose(0d, positions[2], "missing Z position falls back to zero");
    AssertClose(41.25, positions[3], "Z1 position comes from Z1 axis config");
    AssertClose(0d, positions[4], "missing Z2 position falls back to zero");

    AssertEqual(5, statuses.Length, "displayed status count");
    AssertFalse(statuses[0], "X enable state comes from X axis config");
    AssertTrue(statuses[1], "Y enable state comes from Y axis config");
    AssertFalse(statuses[2], "missing Z is not enabled");
    AssertTrue(statuses[3], "Z1 enable state comes from Z1 axis config");
    AssertFalse(statuses[4], "missing Z2 is not enabled");
}

void TestAxisViewSpeedMonitorCommonSurface()
{
    var interfaceSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCard.cs");
    var baseSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ControlCardBase.cs");
    var singleAxisSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\SingleAxisParam.cs");
    var axisModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisModel.cs");
    var matcherSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisViewAxisMatcher.cs");

    AssertContains(interfaceSource, "bool GetAllSpeedInfos(short core = 2)",
        "IControlCard should expose a unified speed refresh API");
    AssertContains(interfaceSource, "bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)",
        "IControlCard should expose a speed array copy overload");
    AssertContains(baseSource, "public double[] CurSpeed { get; set; }",
        "ControlCardBase should cache current speed by physical axis index");
    AssertContains(baseSource, "protected void EnsureSpeedBuffers(int requiredLength = 0)",
        "ControlCardBase should resize speed buffers consistently");
    AssertContains(baseSource, "public virtual bool GetAllSpeedInfos(short core = 2)",
        "ControlCardBase should provide a default speed refresh implementation");
    AssertContains(singleAxisSource, "public double CurSpeed",
        "SingleAxisParam should store runtime current speed");
    AssertContains(axisModelSource, "public double[] CurSpeedInfos",
        "AxisModel should expose AxisView speed snapshots");
    AssertContains(matcherSource, "BuildSpeedSnapshot",
        "AxisViewAxisMatcher should build speed snapshots by axis type");

    var axes = new[]
    {
        new SingleAxisParam { AxisNum = En_AxisNum.Z1, AxisNo = 4, CurSpeed = 41.25 },
        new SingleAxisParam { AxisNum = En_AxisNum.Y, AxisNo = 2, CurSpeed = -12.5 },
        new SingleAxisParam { AxisNum = En_AxisNum.X, AxisNo = 1, CurSpeed = 3.75 }
    };

    var speeds = AxisViewAxisMatcher.BuildSpeedSnapshot(axes);

    AssertEqual(5, speeds.Length, "displayed speed count");
    AssertClose(3.75, speeds[0], "X speed comes from X axis config");
    AssertClose(-12.5, speeds[1], "Y speed comes from Y axis config");
    AssertClose(0d, speeds[2], "missing Z speed falls back to zero");
    AssertClose(41.25, speeds[3], "Z1 speed comes from Z1 axis config");
    AssertClose(0d, speeds[4], "missing Z2 speed falls back to zero");
}

void TestAxisViewRefreshesAndDisplaysActualSpeed()
{
    var viewModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\AxisView.xaml");

    AssertContains(viewModelSource, "RefreshCurrentSpeedSnapshots();",
        "AxisViewModel constructor should initialize displayed speed snapshots");
    AssertContains(viewModelSource, "if (!RefreshCurrentSpeedSnapshotFromCard())",
        "AxisViewModel timer should refresh speed from the control card and handle failures");
    AssertContains(viewModelSource, "ControlCard.GetAllSpeedInfos()",
        "AxisViewModel should use the unified speed API");
    AssertContains(viewModelSource, "AxisViewAxisMatcher.BuildSpeedSnapshot",
        "AxisViewModel should map speed snapshots by displayed axis type");
    AssertContains(viewModelSource, "获取轴速度数据失败",
        "AxisViewModel should log speed refresh failures without blocking positions");

    AssertContains(xaml, "ModelParam.CurSpeedInfos[0]",
        "AxisView should bind X speed");
    AssertContains(xaml, "ModelParam.CurSpeedInfos[1]",
        "AxisView should bind Y speed");
    AssertContains(xaml, "ModelParam.CurSpeedInfos[2]",
        "AxisView should bind Z speed");
    AssertContains(xaml, "ModelParam.CurSpeedInfos[3]",
        "AxisView should bind Z1 speed");
    AssertContains(xaml, "ModelParam.CurSpeedInfos[4]",
        "AxisView should bind Z2 speed");
    AssertContains(xaml, "mm/s",
        "AxisView should label displayed speed in mm/s");
}

void TestAxisViewPersistsSelectedSpeedModeImmediately()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");
    var switchBody = ReadSourceBetween(source, "case \"切换速度\":", "case \"关闭\":");

    AssertContains(switchBody, "ModelParam.CurSpeedType = newSpeed;",
        "speed switch should update the selected speed mode");
    AssertContains(switchBody, "ConfigManager.Write(ConfigKey.AxisModel, ModelParam);",
        "speed switch should persist AxisModel immediately so restart keeps the selected speed mode");
    AssertContainsBefore(switchBody,
        "ModelParam.CurSpeedType = newSpeed;",
        "ConfigManager.Write(ConfigKey.AxisModel, ModelParam);",
        "AxisView should persist after assigning the new speed mode");
}

void TestAcsControlCardReadsFeedbackSpeed()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\GoHome.cs");

    AssertContains(source, "public override bool GetAllSpeedInfos(short core = 2)",
        "ACS should override the unified speed refresh API");
    AssertContains(source, "public override bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)",
        "ACS should override the speed array copy overload");
    AssertContains(source, "_api.GetFVelocity(acsAxis)",
        "ACS should read actual feedback velocity");
    AssertContains(source, "axisConfig.CurSpeed = speed",
        "ACS should update the configured axis speed");
    AssertContains(source, "CurSpeed[axisIndex] = speed",
        "ACS should update the base speed cache");
    AssertFalse(source.Contains("_api.GetVelocity(acsAxis)", StringComparison.Ordinal),
        "AxisView speed monitoring should not use configured command velocity");
}

void TestGoogolControlCardReadsEncoderSpeed()
{
    var motionSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\Packaging\GoogolGTMotion.cs");
    var cardSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.cs");
    var getEncVelSource = ReadSourceBetween(motionSource,
        "public bool GetEncVel(short axisId, ref double[] encVel, short core = 2)",
        "public bool GetEcatEncPos(short core,short axisId, ref int dEncPos)");

    AssertContains(motionSource, "public bool GetEncVel(short axisId, ref double[] encVel, short core = 2)",
        "Googol motion wrapper should expose encoder velocity reads");
    AssertContains(getEncVelSource, "encVel == null || encVel.Length == 0",
        "Googol encoder velocity should reject missing velocity buffers");
    AssertContains(getEncVelSource, "GTN_GetEncVel(core, axisId, out encVel[0]",
        "Googol motion wrapper should use GTN encoder velocity");
    AssertContains(getEncVelSource, "axisId < 1 || axisId > _axisCount",
        "Googol encoder velocity should reject invalid first axis numbers");
    AssertContains(cardSource, "public override bool GetAllSpeedInfos(short core = 2)",
        "Googol should override the unified speed refresh API");
    AssertContains(cardSource, "public override bool GetAllSpeedInfos(ref double[] allSpeedInfos, short core = 2)",
        "Googol should override the speed array copy overload");
    AssertContains(cardSource, "Motion.GetEncVel(1, ref tmpAllSpeedInfos, core)",
        "Googol should read actual encoder velocity");
    AssertContains(cardSource, "PulseEquivalent",
        "Googol should convert pulse speed to user units");
    AssertContains(cardSource, "tmpAllSpeedInfos[sourceIndex] * 1000d / pulseEquivalent",
        "Googol encoder velocity is reported in pulse/ms and should display AxisView speed in mm/s");
    AssertContains(cardSource, "axisConfig.CurSpeed = speed",
        "Googol should update the configured axis speed");
    AssertContains(cardSource, "CurSpeed[axisIndex] = speed",
        "Googol should update the base speed cache");
}

void TestGoogolDoGoHomeSupportsStartupAutoReset()
{
    var cardSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.cs");
    var resetSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\Reset.cs");
    var doGoHomeBody = ReadMethodBody(cardSource, "protected override bool DoGoHome(out string message)");
    var resetRobotBody = ReadMethodBody(resetSource, "public bool ResetRobot()");

    AssertContains(doGoHomeBody, "ResetRobot()",
        "Googol startup reset should reuse the same reset path that AxisView GoHome invokes");
    AssertFalse(doGoHomeBody.Contains("RunGoogolHomeBatch", StringComparison.Ordinal),
        "Googol DoGoHome should not duplicate or replace the previous reset logic");
    AssertContains(cardSource, "private static bool IsEncoderPlannerOnlyAxis(SingleAxisParam axis)",
        "Googol reset should isolate axes that only need encoder/planner binding");
    AssertContains(cardSource, "axis.AxisNum == En_AxisNum.X || axis.AxisNum == En_AxisNum.Y",
        "Googol reset should treat X/Y as encoder-planner-only axes");

    AssertContains(resetRobotBody, "var homeAxes = Config.AllAxis",
        "Googol reset should keep the original ResetRobot flow and derive physical-home axes there");
    AssertContains(resetRobotBody, ".Where(axis => !IsEncoderPlannerOnlyAxis(axis))",
        "Googol reset should exclude X/Y from physical home operations");
    AssertContainsBefore(resetRobotBody, "var homeAxes = Config.AllAxis", "ResetAxis(axis.AxisNum",
        "Googol reset should filter X/Y before any home command is sent");
    AssertFalse(resetRobotBody.Contains("ZeroPos(En_AxisNum.X", StringComparison.Ordinal),
        "Googol reset should not zero X/Y by clearing a contiguous range from X");
    AssertContains(resetRobotBody, "if (!ZeroPos(axisModel.AxisNum))",
        "Googol reset should zero only axes that actually performed physical home");
    AssertContains(resetRobotBody, "foreach (SingleAxisParam axisModel in Config.AllAxis)",
        "Googol reset should still bind encoder/planner for every configured axis, including X/Y");
    AssertContains(resetRobotBody, "Motion.SetPrf(axisModel.AxisNo)",
        "Googol reset should bind encoder and planner positions");
    AssertFalse(resetRobotBody.Contains("SetSpeedAll(EN_SpeedType.Mid)", StringComparison.Ordinal),
        "Googol reset should not apply post-home speed restoration to X/Y encoder-planner-only axes");
    AssertContains(resetRobotBody, "SetSpeed(axisModel.AxisNum, EN_SpeedType.Mid)",
        "Googol reset should restore speed only for axes that performed physical home");
    AssertContains(resetRobotBody, "MoveAbsoluteAxis(axisModel.AxisNum",
        "Googol reset should move only axes that performed physical home back to offset origin");
}

void TestControlCardBaseSizesSpeedBufferByPhysicalAxisNo()
{
    var card = new TestControlCard();
    card.Config.AllAxis.Add(new SingleAxisParam { AxisNum = En_AxisNum.Z2, AxisNo = 5 });

    card.EnsureSpeedBuffersForTest();

    AssertTrue(card.CurSpeed.Length >= 5,
        "speed buffer should include sparse physical axis numbers");
}

void TestCoordinateCacheViewModelDerivesValuesFromConfiguredAxes()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\CoordinateCacheViewModel.cs");

    AssertFalse(source.Contains("TargetPos = [0.0, 0.0, 0.0, 0.0, 0.0]", StringComparison.Ordinal),
        "new coordinate cache items should not use a hard-coded five-value target list");
    AssertFalse(source.Contains("Config.AllAxis[4]", StringComparison.Ordinal),
        "coordinate cache should not index the first five axes directly");
    AssertContains(source, "GetCoordinateAxes()", "coordinate cache should derive values from current axis configuration");
    AssertContains(source, "NormalizeCoordinatePosition", "coordinate cache should normalize saved items against current axes");
}

void TestCoordinateCacheViewModelGuardsSelectionAndCachesCommands()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\CoordinateCacheViewModel.cs");

    AssertFalse(source.Contains("public DelegateCommand LoadCommand => new DelegateCommand", StringComparison.Ordinal),
        "LoadCommand should be cached instead of creating a command per binding access");
    AssertFalse(source.Contains("public DelegateCommand<string> GeneralCommand => new DelegateCommand<string>", StringComparison.Ordinal),
        "GeneralCommand should be cached instead of creating a command per binding access");
    AssertFalse(source.Contains("ModelParam.AllPosInfo.Remove(ModelParam.SltPosInfo);", StringComparison.Ordinal),
        "delete selected item should guard null selection before removing");
    AssertContains(source, "EnsureSelectedPosition", "commands that operate on the selected position should validate it first");
}

void TestCoordinateCacheViewModelUsesCoordinatedMotionCapability()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\CoordinateCacheViewModel.cs");

    AssertFalse(source.Contains("ReeYin_V.Hardware.ControlCard.ACS", StringComparison.Ordinal),
        "CoordinateCache should not reference the ACS project namespace");
    AssertFalse(source.Contains("AcsControlCard", StringComparison.Ordinal),
        "CoordinateCache should not hard-code the ACS concrete control-card type");

    var moveCommandBody = ReadSourceBetween(source, "case \"移动至此位置\":", "case \"添加新项\":");
    const string planarFailureMarker = "if (!TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray))";
    AssertContains(moveCommandBody, planarFailureMarker,
        "CoordinateCache move command should gate extra-axis moves on planar movement success");
    AssertContains(moveCommandBody, "TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray)",
        "CoordinateCache move command should route planar movement through the helper");
    AssertContainsBefore(moveCommandBody,
        "TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray)",
        "MoveAdditionalAxes(targetPosArray, axes)",
        "CoordinateCache should move extra axes only after planar movement succeeds");

    var planarFailureBlock = ReadBlockStartingAt(moveCommandBody, planarFailureMarker);
    AssertContains(planarFailureBlock, "return;",
        "CoordinateCache should stop the move task when planar movement fails");
    AssertBlockEndsBefore(moveCommandBody, planarFailureMarker, "MoveAdditionalAxes(targetPosArray, axes)",
        "CoordinateCache should move extra axes after the planar failure guard block");
    AssertFalse(moveCommandBody.Contains("MoveAdditionalAxes(position, axes)", StringComparison.Ordinal),
        "CoordinateCache should use the target-position snapshot for extra-axis moves");

    var methodBody = ReadMethodBody(source, "private bool TryMovePlanarAxesToTarget(");
    AssertContains(methodBody, "ICoordinatedMotionCard",
        "CoordinateCache should use neutral coordinated motion capability detection");
    AssertContains(methodBody, "SupportsCoordinatedMotion",
        "CoordinateCache should only use coordinated motion when the card advertises support");
    AssertContains(methodBody, "MoveCoordinated(",
        "CoordinateCache should move coordinated-capable cards through MoveCoordinated");
    AssertContains(methodBody, "new CoordinatedMotionRequest",
        "CoordinateCache should build a coordinated motion request");
    AssertContains(methodBody, "Kind = CoordinatedMotionKind.Line",
        "CoordinateCache coordinated move should request a line move");
    AssertContains(methodBody, "Axes = PlanarMoveAxes.ToList()",
        "CoordinateCache should pass the planar axes to the coordinated motion request");
    AssertContains(methodBody, "TargetPositions = new Dictionary<En_AxisNum, double>(targetPositions)",
        "CoordinateCache should pass target positions to the coordinated motion request");
    AssertContains(methodBody, "WaitForEnd = true",
        "CoordinateCache coordinated move should wait for completion before extra axes move");
    AssertContains(methodBody, "LineParam = lineParam",
        "CoordinateCache should pass the same line interpolation parameters to capable cards");
    AssertContains(methodBody, "CustomInterpolationMoving(",
        "CoordinateCache should keep the legacy custom-interpolation fallback");
    AssertContains(methodBody, "CreateCustomInterpolationParam",
        "CoordinateCache fallback should use the shared custom interpolation parameter helper");
    AssertContains(methodBody, "LineInterpoMoving(lineParam)",
        "CoordinateCache fallback should still execute the line interpolation command");
    AssertContainsBefore(methodBody, "MoveCoordinated(", "CustomInterpolationMoving(",
        "CoordinateCache should try coordinated motion before legacy fallback");
    AssertContainsBefore(methodBody, "SupportsCoordinatedMotion", "MoveCoordinated(",
        "CoordinateCache should check support before calling coordinated motion");

    const string coordinatedFailureMarker = "if (!coordinatedMotionCard.MoveCoordinated(";
    AssertContains(methodBody, coordinatedFailureMarker,
        "CoordinateCache should only return success after MoveCoordinated succeeds");

    var coordinatedFailureBlock = ReadBlockStartingAt(methodBody, coordinatedFailureMarker);
    AssertContains(coordinatedFailureBlock, "return false;",
        "CoordinateCache should fail the planar move when coordinated motion fails");
    AssertBlockEndsBefore(methodBody, coordinatedFailureMarker, "return true;",
        "CoordinateCache should return success only after the coordinated failure block");

    var coordinatedSuccessPath = ReadSourceBetween(methodBody, coordinatedFailureMarker, "CustomInterpolationMoving(");
    AssertContains(coordinatedSuccessPath, "return true;",
        "CoordinateCache should return after a successful coordinated move instead of continuing to fallback");

    var additionalAxisMoveBody = ReadMethodBody(source, "private void MoveAdditionalAxes(");
    AssertContains(additionalAxisMoveBody, "IReadOnlyList<double> targetPositions",
        "CoordinateCache extra-axis moves should receive a target-position snapshot");
    AssertContains(additionalAxisMoveBody, "targetPositions[index]",
        "CoordinateCache extra-axis moves should use the captured target-position snapshot");
    AssertFalse(additionalAxisMoveBody.Contains("position.TargetPos", StringComparison.Ordinal),
        "CoordinateCache extra-axis moves should not read the live selected position after planar motion");
}

void TestCoordinateCacheResolvesOnlyInitializedConnectedControlCard()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\CoordinateCacheViewModel.cs");
    var configViewModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\ControlCardConfigViewModel.cs");
    var resolveBody = ReadMethodBody(source, "private static IControlCard? ResolveControlCard()");

    AssertFalse(resolveBody.Contains("CurSltCard ?? controlCardConfig.CardModels.FirstOrDefault()", StringComparison.Ordinal),
        "CoordinateCache should not blindly use the selected or first configured card");
    AssertContains(resolveBody, "IsUsableControlCard(controlCardConfig.CurSltCard)",
        "CoordinateCache should only use CurSltCard when the selected card is usable");
    AssertContains(resolveBody, "FirstOrDefault(IsUsableControlCard)",
        "CoordinateCache should fall back to another usable card when CurSltCard is disconnected");
    AssertContains(source, "private static bool IsUsableControlCard(ControlCardBase? card)",
        "CoordinateCache should keep the connection-health rule explicit");
    AssertContains(source, "card?.Initialized == true",
        "CoordinateCache should require the resolved card to be initialized");
    AssertContains(source, "card.IsConnected",
        "CoordinateCache should require the resolved card to have a live connection");
    AssertContains(configViewModelSource, "ModelParam.CurSltCard = ModelParam.CardModels[0]",
        "CurSltCard is assigned from persisted CardModels during config page initialization");
    AssertContains(configViewModelSource, "ModelParam.CardModels.Add(module)",
        "Control card instances are added from the DI-created module");
    AssertContains(configViewModelSource, "ModelParam.CurSltCard = module",
        "CurSltCard is assigned to the newly created control card instance");
}

void TestAxisViewExposesMissingDisplayedAxesAsDisabled()
{
    var axes = new[]
    {
        new SingleAxisParam { AxisNum = En_AxisNum.X, AxisNo = 1 },
        new SingleAxisParam { AxisNum = En_AxisNum.Z2, AxisNo = 5 }
    };

    var configured = AxisViewAxisMatcher.BuildConfiguredSnapshot(axes);

    AssertEqual(5, configured.Length, "displayed configured count");
    AssertTrue(configured[0], "X axis should be available");
    AssertFalse(configured[1], "Y axis should be unavailable");
    AssertFalse(configured[2], "Z axis should be unavailable");
    AssertFalse(configured[3], "Z1 axis should be unavailable");
    AssertTrue(configured[4], "Z2 axis should be available");
    AssertEqual((short)5, AxisViewAxisMatcher.GetAxisNo(axes, En_AxisNum.Z2), "Z2 uses configured physical axis number");
    AssertEqual(null, AxisViewAxisMatcher.GetAxisNo(axes, En_AxisNum.Y), "missing Y has no physical axis number");
}


void TestControlCardBaseAppliesRelativeMoveDirection()
{
    var card = CreateBaseTestCard();
    card.AxisStoppedResponses.Enqueue(true);
    card.AxisStoppedResponses.Enqueue(true);

    var ok = card.Move(En_AxisNum.X, MoveDirection.反向, 5d, out var message);

    AssertTrue(ok, $"relative move should succeed: {message}");
    AssertEqual(1, card.RelativeMoveDistances.Count, "relative move command count");
    AssertEqual(-5d, card.RelativeMoveDistances[0], "reverse move should pass a negative relative distance");
}

void TestControlCardBaseReturnsHomeFailure()
{
    var card = CreateBaseTestCard();
    card.HomeResult = false;
    card.HomeMessage = "home failed";

    var ok = card.GoHome(out var message);

    AssertFalse(ok, "GoHome should return the DoGoHome result");
    AssertEqual("home failed", message, "GoHome should preserve the vendor home failure message");
    AssertFalse(card.IsAxisHomed, "failed home should leave IsAxisHomed false");
}

void TestControlCardBaseMoveWaitTimesOut()
{
    var card = CreateBaseTestCard();
    card.MotionTimeoutOverrideMs = 1;
    card.AxisStoppedResponses.Enqueue(true);
    card.AxisStoppedResponses.Enqueue(false);
    card.AxisStoppedResponses.Enqueue(false);

    var ok = card.Move(En_AxisNum.X, MoveDirection.正向, 3d, out var message);

    AssertFalse(ok, "relative move should fail when the axis does not stop before timeout");
    AssertContains(message, "超时", "timeout message should mention timeout");
    AssertEqual(1, card.RelativeMoveDistances.Count, "move should still have been commanded once");
    AssertEqual(3d, card.RelativeMoveDistances[0], "forward move should pass a positive relative distance");
}

void TestControlCardBaseInvokesConfigChangeHook()
{
    var card = new TestControlCard();

    card.Config = new ControlCardConfig();

    AssertEqual(1, card.ConfigChangedCount, "setting Config should invoke OnConfigChanged once");
}

void TestControlCardConfigModelSupportsStartupAutoReset()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardConfigModel.cs");
    var startupAutoResetBody = ReadMethodBody(source, "public async Task<InitResult> ExecuteStartupResetAsync(Action<string> updateMessage)");

    AssertContains(source, "public bool IsAutoResetOnStartup",
        "control-card config model should persist the startup auto-reset option");
    AssertContains(source, "ExecuteStartupResetAsync",
        "control-card config model should expose startup auto-reset execution for InitializeView");
    AssertContains(startupAutoResetBody, "ResolveStartupResetCard",
        "startup auto reset should select an initialized and connected control card");
    AssertContains(startupAutoResetBody, "GoHome(out var message)",
        "startup auto reset should call the selected control card GoHome API");
    AssertContains(startupAutoResetBody, "updateMessage?.Invoke",
        "startup auto reset should update InitializeView while reset is running");
    AssertContains(source, "card?.Initialized == true && card.IsConnected",
        "startup auto reset should avoid disconnected or failed initialization card instances");
    AssertFalse(source.Contains("CurSltCard ?? CardModels.FirstOrDefault()", StringComparison.Ordinal),
        "startup auto reset should not blindly use the selected or first configured card");
}

void TestControlCardConfigViewBindsStartupAutoReset()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\ControlCardConfigView.xaml");

    AssertContains(xaml, "Header=\"其他\"",
        "control-card config view should expose an Other tab");
    AssertContains(xaml, "软件启动自动复位",
        "Other tab should show the startup auto-reset setting");
    AssertContains(xaml, "IsChecked=\"{Binding ModelParam.IsAutoResetOnStartup, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "startup auto-reset checkbox should bind to the persisted model setting");
}

void TestControlCardConfigModelSupportsAxisViewIoJogSettings()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardConfigModel.cs");
    var model = new ControlCardConfigModel();

    AssertContains(source, "public bool IsAxisViewIoJogEnabled",
        "control-card config should persist the AxisView IO Jog enable switch");
    AssertContains(source, "public int AxisViewIoJogUpInputPort",
        "control-card config should persist the up input port");
    AssertContains(source, "public int AxisViewIoJogDownInputPort",
        "control-card config should persist the down input port");
    AssertContains(source, "public int AxisViewIoJogLeftInputPort",
        "control-card config should persist the left input port");
    AssertContains(source, "public int AxisViewIoJogRightInputPort",
        "control-card config should persist the right input port");
    AssertFalse(model.IsAxisViewIoJogEnabled, "AxisView IO Jog should default to disabled");
    AssertEqual(3, model.AxisViewIoJogUpInputPort, "default up input port");
    AssertEqual(4, model.AxisViewIoJogDownInputPort, "default down input port");
    AssertEqual(5, model.AxisViewIoJogLeftInputPort, "default left input port");
    AssertEqual(6, model.AxisViewIoJogRightInputPort, "default right input port");
}

void TestControlCardConfigViewBindsAxisViewIoJogSettings()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\ControlCardConfigView.xaml");

    AssertContains(xaml, "AxisView IO方向控制",
        "Other tab should expose the AxisView IO Jog setting group");
    AssertContains(xaml, "IsChecked=\"{Binding ModelParam.IsAxisViewIoJogEnabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "AxisView IO Jog checkbox should bind to the persisted enable switch");
    AssertContains(xaml, "Value=\"{Binding ModelParam.AxisViewIoJogUpInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "up input port should bind to config");
    AssertContains(xaml, "Value=\"{Binding ModelParam.AxisViewIoJogDownInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "down input port should bind to config");
    AssertContains(xaml, "Value=\"{Binding ModelParam.AxisViewIoJogLeftInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "left input port should bind to config");
    AssertContains(xaml, "Value=\"{Binding ModelParam.AxisViewIoJogRightInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "right input port should bind to config");
    AssertContains(xaml, "仅在运动轴操作面板打开时生效",
        "UI should explain the AxisView-only scope");
}

void TestAxisViewIoJogControllerMapsInputDirections()
{
    var card = CreateBaseTestCard();
    var config = new ControlCardConfigModel { IsAxisViewIoJogEnabled = true };
    var controller = new AxisViewIoJogController(card, config);

    controller.Update(CreateInputs(3), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(1, card.JogCommands.Count, "IO3 should start one Jog command");
    AssertJogCommand(card, 0, En_AxisNum.Y, MoveDirection.正向, EN_SpeedType.High, true,
        "IO3 start should map to Y positive");

    controller.Update(CreateInputs(), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(2, card.JogCommands.Count, "IO3 release should add one stop command");
    AssertJogCommand(card, 1, En_AxisNum.Y, MoveDirection.正向, EN_SpeedType.High, false,
        "IO3 release should stop Y positive");

    controller.Update(CreateInputs(4), EN_SpeedType.Mid, _ => true, (_, _) => true);
    AssertEqual(3, card.JogCommands.Count, "IO4 should add a start command after IO3 release");
    AssertJogCommand(card, 2, En_AxisNum.Y, MoveDirection.反向, EN_SpeedType.Mid, true,
        "IO4 start should map to Y negative");

    controller.Update(CreateInputs(), EN_SpeedType.Mid, _ => true, (_, _) => true);
    AssertEqual(4, card.JogCommands.Count, "IO4 release should add one stop command");
    AssertJogCommand(card, 3, En_AxisNum.Y, MoveDirection.反向, EN_SpeedType.Mid, false,
        "IO4 release should stop Y negative");

    controller.Update(CreateInputs(5), EN_SpeedType.Low, _ => true, (_, _) => true);
    AssertEqual(5, card.JogCommands.Count, "IO5 should add a start command after IO4 release");
    AssertJogCommand(card, 4, En_AxisNum.X, MoveDirection.反向, EN_SpeedType.Low, true,
        "IO5 start should map to X negative");

    controller.Update(CreateInputs(), EN_SpeedType.Low, _ => true, (_, _) => true);
    AssertEqual(6, card.JogCommands.Count, "IO5 release should add one stop command");
    AssertJogCommand(card, 5, En_AxisNum.X, MoveDirection.反向, EN_SpeedType.Low, false,
        "IO5 release should stop X negative");

    controller.Update(CreateInputs(6), EN_SpeedType.Work, _ => true, (_, _) => true);
    AssertEqual(7, card.JogCommands.Count, "IO6 should add a start command after IO5 release");
    AssertJogCommand(card, 6, En_AxisNum.X, MoveDirection.正向, EN_SpeedType.Work, true,
        "IO6 start should map to X positive");

    controller.Update(CreateInputs(), EN_SpeedType.Work, _ => true, (_, _) => true);
    AssertEqual(8, card.JogCommands.Count, "IO6 release should add one stop command");
    AssertJogCommand(card, 7, En_AxisNum.X, MoveDirection.正向, EN_SpeedType.Work, false,
        "IO6 release should stop X positive");
}

void TestAxisViewIoJogControllerHonorsCustomPortsAndEnableSwitch()
{
    var customCard = CreateBaseTestCard();
    var customConfig = new ControlCardConfigModel
    {
        IsAxisViewIoJogEnabled = true,
        AxisViewIoJogUpInputPort = 10,
        AxisViewIoJogDownInputPort = 11,
        AxisViewIoJogLeftInputPort = 12,
        AxisViewIoJogRightInputPort = 13
    };
    var customController = new AxisViewIoJogController(customCard, customConfig);

    customController.Update(CreateInputs(3), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(0, customCard.JogCommands.Count, "default IO3 should not trigger when custom ports are configured");

    customController.Update(CreateInputs(10), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(1, customCard.JogCommands.Count, "custom up port should start Jog");
    AssertJogCommand(customCard, 0, En_AxisNum.Y, MoveDirection.正向, EN_SpeedType.High, true,
        "custom up port should map to Y positive");
    customController.Update(CreateInputs(), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(2, customCard.JogCommands.Count, "custom up release should stop Jog");
    AssertJogCommand(customCard, 1, En_AxisNum.Y, MoveDirection.正向, EN_SpeedType.High, false,
        "custom up release should stop Y positive");

    customController.Update(CreateInputs(11), EN_SpeedType.Mid, _ => true, (_, _) => true);
    AssertEqual(3, customCard.JogCommands.Count, "custom down port should start Jog");
    AssertJogCommand(customCard, 2, En_AxisNum.Y, MoveDirection.反向, EN_SpeedType.Mid, true,
        "custom down port should map to Y negative");
    customController.Update(CreateInputs(), EN_SpeedType.Mid, _ => true, (_, _) => true);
    AssertEqual(4, customCard.JogCommands.Count, "custom down release should stop Jog");
    AssertJogCommand(customCard, 3, En_AxisNum.Y, MoveDirection.反向, EN_SpeedType.Mid, false,
        "custom down release should stop Y negative");

    customController.Update(CreateInputs(12), EN_SpeedType.Low, _ => true, (_, _) => true);
    AssertEqual(5, customCard.JogCommands.Count, "custom left port should start Jog");
    AssertJogCommand(customCard, 4, En_AxisNum.X, MoveDirection.反向, EN_SpeedType.Low, true,
        "custom left port should map to X negative");
    customController.Update(CreateInputs(), EN_SpeedType.Low, _ => true, (_, _) => true);
    AssertEqual(6, customCard.JogCommands.Count, "custom left release should stop Jog");
    AssertJogCommand(customCard, 5, En_AxisNum.X, MoveDirection.反向, EN_SpeedType.Low, false,
        "custom left release should stop X negative");

    customController.Update(CreateInputs(13), EN_SpeedType.Work, _ => true, (_, _) => true);
    AssertEqual(7, customCard.JogCommands.Count, "custom right port should start Jog");
    AssertJogCommand(customCard, 6, En_AxisNum.X, MoveDirection.正向, EN_SpeedType.Work, true,
        "custom right port should map to X positive");

    var disabledCard = CreateBaseTestCard();
    var disabledConfig = new ControlCardConfigModel { IsAxisViewIoJogEnabled = false };
    var disabledController = new AxisViewIoJogController(disabledCard, disabledConfig);

    disabledController.Update(CreateInputs(3), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(0, disabledCard.JogCommands.Count, "disabled IO Jog should ignore configured input ports");
}

void TestAxisViewIoJogControllerStopsOnMultiDirectionInput()
{
    var card = CreateBaseTestCard();
    var config = new ControlCardConfigModel { IsAxisViewIoJogEnabled = true };
    var controller = new AxisViewIoJogController(card, config);

    controller.Update(CreateInputs(3), EN_SpeedType.High, _ => true, (_, _) => true);
    controller.Update(CreateInputs(3, 6), EN_SpeedType.High, _ => true, (_, _) => true);

    AssertEqual(2, card.JogCommands.Count, "multi-direction input should stop the active Jog without starting another");
    AssertJogCommand(card, 1, En_AxisNum.Y, MoveDirection.正向, EN_SpeedType.High, false,
        "multi-direction input should stop the active Jog");

    controller.Update(CreateInputs(3, 6), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(2, card.JogCommands.Count, "holding multiple inputs should not repeat stop commands");
}

void TestAxisViewIntegratesIoJogLifecycle()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");
    var initTimerBody = ReadMethodBody(source, "private void InitTimer()");
    var timerTickBody = ReadBlockStartingAt(initTimerBody, "_timer.Tick += (s, e) =>");
    var closeBody = ReadSourceBetween(source, "case \"关闭\":", "default:");

    AssertContains(source, "private readonly ControlCardConfigModel _controlCardConfig",
        "AxisViewModel should keep the persisted control-card config for IO Jog settings");
    AssertContains(source, "private readonly AxisViewIoJogController _ioJogController",
        "AxisViewModel should own the IO Jog controller");
    AssertContains(source, "new AxisViewIoJogController(ControlCard, _controlCardConfig)",
        "AxisViewModel should create the IO Jog controller for the selected control card");
    AssertContains(timerTickBody, "UpdateAxisViewIoJog()",
        "AxisView timer should process IO Jog each refresh");
    AssertContains(source, "ControlCard.GetAllInput(out var inputStatus)",
        "AxisViewModel should poll input IO through the common control-card API");
    AssertContains(source, "_ioJogController.Update(",
        "AxisViewModel should delegate IO direction state handling to the controller");
    AssertContains(closeBody, "_ioJogController.StopActiveJog();",
        "closing AxisView should stop any active IO Jog");
}

void TestAxisViewIoJogReviewFixes()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");
    var constructorBody = ReadMethodBody(source, "public AxisViewModel(IConfigManager configManager)");
    var initTimerBody = ReadMethodBody(source, "private void InitTimer()");
    var updateBody = ReadMethodBody(source, "private void UpdateAxisViewIoJog()");
    var canStartBody = ReadMethodBody(source, "private bool CanStartOrContinueAxisViewIoJog(");
    var refreshSpeedBody = ReadMethodBody(source, "private bool RefreshCurrentSpeedSnapshotFromCard()");
    var cleanupBody = ReadMethodBody(source, "private void CleanupAxisViewIoJogAndTimer(");
    var movingCommandBody = ReadSourceBetween(source, "public DelegateCommand<object> MovingCommand", "#endregion");
    var controllerSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisViewIoJogController.cs");

    AssertContains(source, "private (En_AxisNum Axis, MoveDirection Direction, EN_SpeedType SpeedType)? _lastAxisViewIoJogRequest",
        "AxisViewModel should remember the IO Jog request that has already passed one-time start checks");
    AssertContains(source, "private bool _isManualJogActive",
        "AxisViewModel should block IO Jog while a manual continuous Jog is active");
    AssertContains(constructorBody, "TryGetValue(ConfigKey.ControlCard",
        "AxisViewModel constructor should safely read the control-card module");
    AssertContains(constructorBody, "FirstOrDefault()",
        "AxisViewModel constructor should avoid indexing an empty control-card list");
    AssertContains(constructorBody, "InvalidOperationException(\"未找到控制卡配置，无法打开轴操作面板。\")",
        "AxisViewModel constructor should fail fast with a clear message when no control card exists");
    AssertContains(initTimerBody, "if (!RefreshCurrentSpeedSnapshotFromCard())",
        "AxisView timer should not process IO Jog when speed refresh fails");
    AssertContains(initTimerBody, "StopAxisViewIoJogAndClearState();",
        "AxisView timer should use the shared IO Jog cleanup path when primary position refresh fails");
    AssertContains(refreshSpeedBody, "return false;",
        "AxisView speed refresh helper should report failure to the timer");
    AssertContains(updateBody, "_localTask == null || _localTask.IsCompleted",
        "AxisView IO Jog should be gated while local movement/reset tasks are active");
    AssertContains(updateBody, "if (_isManualJogActive)",
        "AxisView IO Jog should be blocked while manual Jog is active");
    AssertContains(canStartBody, "NeedLiftZAxesBeforeJog(axis)",
        "AxisView IO Jog should reuse the manual Jog X/Y safety lift rule");
    AssertContains(canStartBody, "TryMoveAllZAxesToSafePosition()",
        "AxisView IO Jog should lift Z axes before a new X/Y continuous Jog starts");
    AssertContains(canStartBody, "IsSameAxisViewIoJogRequest(axis, direction)",
        "AxisView IO Jog should avoid repeating one-time start checks during same-axis continuous polling");
    AssertContains(canStartBody, "ControlCard.ValidateJogLimitCondition(axis, direction, out var message)",
        "AxisView IO Jog should still validate limits on every poll");
    AssertContains(source, "private (En_AxisNum Axis, MoveDirection Direction, string Message)? _lastAxisViewIoJogLimitFailure",
        "AxisView IO Jog limit logging should remember the last emitted failure");
    AssertContains(canStartBody, "LogAxisViewIoJogLimitFailure(axis, direction, message)",
        "AxisView IO Jog should dedupe repeated limit failure logs");
    AssertContains(canStartBody, "ClearAxisViewIoJogLimitFailure()",
        "AxisView IO Jog should clear dedupe state after limit validation succeeds");
    AssertContains(cleanupBody, "ControlCard.Stop(null);",
        "AxisView cleanup should fall back to a card stop when IO Jog stop fails");
    AssertContains(cleanupBody, "_ioJogController.ResetActiveState();",
        "AxisView cleanup should clear controller state after the global stop fallback");
    AssertContains(controllerSource, "public void ResetActiveState()",
        "AxisView IO Jog controller should expose an explicit state reset after external/global stops");
    AssertContains(movingCommandBody, "StopAxisViewIoJogAndClearState();",
        "manual continuous Jog Down should stop any active IO Jog before issuing manual motion");
    AssertContains(movingCommandBody, "_isManualJogActive = true;",
        "manual continuous Jog Down should mark manual Jog active");
    AssertContains(movingCommandBody, "_isManualJogActive = false;",
        "manual continuous Jog Up/Leave should clear manual Jog active");
}
void TestControlCardBasePublishesGlobalResetOverlayEvents()
{
    var eventSource = ReadRepoFile(@"Core\ReeYin-V.Core\Events\Hardware\ControlCardResetOverlayEvent.cs");
    var baseSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ControlCardBase.cs");
    var configSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardConfigModel.cs");

    AssertContains(eventSource, "public class ControlCardResetOverlayEvent : PubSubEvent<ControlCardResetOverlayPayload>",
        "core should expose a reset overlay event that every region can observe");
    AssertContains(eventSource, "public Guid OperationId",
        "reset overlay event should identify each reset operation so stale timeout/end events can be ignored");
    AssertContains(eventSource, "public int TimeoutSeconds",
        "reset overlay event should carry the configured timeout");
    AssertContains(baseSource, "PublishControlCardResetOverlay(true",
        "GoHome should publish a global overlay begin event when reset starts");
    AssertContains(baseSource, "PublishControlCardResetOverlay(false",
        "GoHome should publish a global overlay end event when reset completes, fails, or exits early");
    AssertContains(baseSource, "ResolveResetOverlayTimeoutSeconds",
        "GoHome should resolve the configured reset overlay timeout");
    AssertContains(baseSource, "StartupAutoResetTimeoutSeconds",
        "manual reset should reuse the timeout configured in ControlCardConfigView Other tab");
    AssertContains(configSource, "public int StartupAutoResetTimeoutSeconds",
        "control-card config model should persist reset overlay timeout setting");
    AssertContains(configSource, "_startupAutoResetTimeoutSeconds = 60",
        "reset overlay timeout should default to 60 seconds");
}

void TestMainViewBlocksWholeScreenWhileControlCardResetRuns()
{
    var configXaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\ControlCardConfigView.xaml");
    var mainXaml = ReadRepoFile(@"Application\ReeYin_V.Main\Views\MainView.xaml");
    var mainViewModel = ReadRepoFile(@"Application\ReeYin_V.Main\ViewModels\MainViewModel.cs");

    AssertContains(configXaml, "ModelParam.StartupAutoResetTimeoutSeconds",
        "Other tab should allow configuring startup auto-reset timeout");
    AssertFalse(configXaml.Contains("ModelParam.IsStartupAutoResetRunning", StringComparison.Ordinal),
        "ControlCardConfigView should not own the reset overlay; the shell should cover the whole screen");
    AssertContains(mainViewModel, "ControlCardResetOverlayEvent",
        "main view model should listen for control-card reset overlay events");
    AssertContains(mainViewModel, "IsControlCardResetOverlayVisible",
        "main view model should expose global reset overlay visibility");
    AssertContains(mainViewModel, "ControlCardResetOverlayMessage",
        "main view model should expose global reset overlay message");
    AssertContains(mainViewModel, "HideControlCardResetOverlayOnTimeoutAsync",
        "main view model should hide the global reset overlay on timeout");
    AssertContains(mainViewModel, "Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken)",
        "global reset overlay timeout should use the event timeout seconds");
    AssertContains(mainViewModel, "ShowControlCardResetOverlayWindow",
        "main view model should open a topmost full-screen overlay window for dialogs such as AxisView");
    AssertContains(mainViewModel, "HideControlCardResetOverlayWindow",
        "main view model should close the topmost reset overlay window after reset completes or times out");
    AssertContains(mainViewModel, "Topmost = true",
        "reset overlay window should cover topmost operation windows");
    AssertContains(mainViewModel, "WindowStyle = WindowStyle.None",
        "reset overlay window should not look like a normal dialog");
    AssertContains(mainViewModel, "SystemParameters.VirtualScreenWidth",
        "reset overlay window should cover the whole screen, not just one region");
    AssertContains(mainXaml, "Visibility=\"{Binding IsControlCardResetOverlayVisible, Converter={StaticResource BoolToVisibilityConverter}}\"",
        "main view should bind global reset overlay visibility");
    AssertContains(mainXaml, "Grid.RowSpan=\"2\"",
        "global reset overlay should cover title bar and all content regions");
    AssertContains(mainXaml, "Panel.ZIndex=\"1000\"",
        "global reset overlay should be above normal regions");
    AssertContains(mainXaml, "复位中，不允许操作",
        "global reset overlay should clearly tell users operation is blocked");
    AssertContains(mainXaml, "Text=\"{Binding ControlCardResetOverlayMessage}\"",
        "global reset overlay should show reset status text");
}

void TestInitializeViewShowsStartupControlCardResetPrompt()
{
    var initializeViewModel = ReadRepoFile(@"Application\ReeYin_V.Initialize\ViewModels\InitializeViewModel.cs");
    var controlCardConfigModel = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardConfigModel.cs");
    var startupAutoResetBody = ReadMethodBody(controlCardConfigModel, "public async Task<InitResult> ExecuteStartupResetAsync(Action<string> updateMessage)");

    AssertContains(initializeViewModel, "ControlCardResetOverlayEvent",
        "initialize view model should observe control-card reset events before the main view is loaded");
    AssertContains(initializeViewModel, "ConfigKey.ControlCard",
        "initialize view should resolve the control-card module during startup");
    AssertContains(initializeViewModel, "IStartupResetHardwareModule",
        "initialize view should depend on the shared startup-reset interface instead of the control-card project");
    AssertContains(initializeViewModel, "ExecuteStartupResetAsync",
        "initialize view should trigger control-card startup reset before entering MainView");
    AssertContainsBefore(initializeViewModel, "controlCardStartupReset.ExecuteStartupResetAsync", "plcSet.ExecuteStartupResetAsync",
        "initialize view should trigger control-card startup reset before PLC startup reset");
    AssertContains(initializeViewModel, "Subscribe(OnControlCardResetOverlay, ThreadOption.UIThread)",
        "initialize view model should update its prompt on the UI thread");
    AssertContains(initializeViewModel, "private void OnControlCardResetOverlay(ControlCardResetOverlayPayload payload)",
        "initialize view model should handle reset begin/end events");
    AssertContains(initializeViewModel, "控制卡正在复位",
        "initialize view should only prompt that the control card is resetting");
    AssertContains(initializeViewModel, "Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken)",
        "initialize reset prompt should stop showing the running text after the configured timeout");
    AssertFalse(initializeViewModel.Contains("ShowControlCardResetOverlayWindow", StringComparison.Ordinal),
        "initialize view should not create the full-screen reset overlay window");
    AssertFalse(initializeViewModel.Contains("Topmost = true", StringComparison.Ordinal),
        "initialize view should not open a topmost overlay window");
    AssertFalse(initializeViewModel.Contains("SystemParameters.VirtualScreenWidth", StringComparison.Ordinal),
        "initialize view should not cover the whole screen during startup reset");
    AssertContains(startupAutoResetBody, "await Task.Run(() =>",
        "startup reset should run the blocking GoHome call off the UI thread while InitializeView waits for completion");
}

TestControlCard CreateBaseTestCard()
{
    var card = new TestControlCard();
    card.Config.AllAxis.Add(new SingleAxisParam
    {
        AxisNum = En_AxisNum.X,
        AxisNo = 1,
        IsUsing = true,
        SpeedDict1 = new()
        {
            new SpeedSetting { SpeedType = EN_SpeedType.Work, SpeedDescribe = "work", StartSpeed = 1, MaxSpeed = 10, AccSpeed = 100 }
        }
    });

    AssertTrue(card.Init(), "test card should initialize");
    card.IsAxisHomed = true;
    return card;
}

bool[] CreateInputs(params int[] activePorts)
{
    var inputs = new bool[16];
    foreach (var port in activePorts)
    {
        AssertTrue(port >= 0 && port < inputs.Length,
            $"input port {port} should be between 0 and {inputs.Length - 1}");
        inputs[port] = true;
    }

    return inputs;
}

void AssertJogCommand(
    TestControlCard card,
    int index,
    En_AxisNum expectedAxis,
    MoveDirection expectedDirection,
    EN_SpeedType expectedSpeedType,
    bool expectedIsRunStop,
    string label)
{
    AssertTrue(card.JogCommands.Count > index, $"{label} should produce command at index {index}");

    var command = card.JogCommands[index];
    AssertEqual(expectedAxis, command.Axis, $"{label} axis");
    AssertEqual(expectedDirection, command.Direction, $"{label} direction");
    AssertEqual(expectedSpeedType, command.SpeedType, $"{label} speed type");
    AssertEqual(expectedIsRunStop, command.IsRunStop, $"{label} run/stop flag");
}

void TestWaferFlatnessAcsLciPulseConfigSurface()
{
    var motionSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");
    var configSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\AcsLciFixedDistancePulseConfig.cs");

    AssertContains(motionSource, "public AcsLciFixedDistancePulseConfig AcsLciPulseParam", "Wafer motion model should expose ACS LCI pulse config");
    AssertContains(configSource, "public class AcsLciFixedDistancePulseConfig", "Wafer should define a serializable ACS LCI pulse config model");

    foreach (var property in new[]
    {
        "IsEnabled",
        "BufferNo",
        "AxisX",
        "AxisY",
        "PulseWidth",
        "UseSensorInterval",
        "Interval",
        "StartDistance",
        "EndDistance",
        "RouteConfigOutput",
        "ConfigOutputIndex",
        "ConfigOutputCode",
        "Timeout"
    })
    {
        AssertContains(configSource, $"public ", $"ACS LCI config should contain property surface for {property}");
        AssertContains(configSource, $" {property}", $"ACS LCI config should expose {property}");
    }
}

void TestWaferFlatnessUsesAcsLciFixedDistancePulseRunner()
{
    var motionSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");
    var methodBody = ReadMethodBody(motionSource, "private bool ExecuteAcsLciFixedDistancePulseSegment(");

    AssertContains(motionSource, "ExecuteAcsLciFixedDistancePulseSegment", "Wafer line execution should have a dedicated ACS LCI segment path");
    AssertContains(motionSource, "TryRunLciFixedDistancePulseXseg", "Wafer ACS LCI path should invoke the ACS Buffer runner");
    AssertContains(motionSource, "BuildAcsLciFixedDistancePulseParam", "Wafer ACS LCI path should map wafer line and config into ACS LCI parameters");
    AssertContains(motionSource, "CreateAcsPoint2D", "Wafer ACS LCI path should pass line start/end as ACS points");
    AssertContains(motionSource, "GetLineSegmentLength", "Wafer ACS LCI path should default end distance from the segment length");
    AssertContainsBefore(methodBody, "PublishStartCollectEvent", "TryRunLciFixedDistancePulseXseg", "sensor collection should start before the ACS LCI segment runs");
    AssertContainsBefore(methodBody, "TryRunLciFixedDistancePulseXseg", "PublishStopCollectEvent", "sensor collection should stop after the ACS LCI segment runs");
}

void TestWaferFlatnessAcsLciCoordinateArrayConfigSurface()
{
    var configSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\AcsLciFixedDistancePulseConfig.cs");

    AssertContains(configSource, "CoordinateArrayMultiAxWinSize", "Wafer ACS LCI config should expose coordinate-array trigger window");
    AssertContains(configSource, "CoordinateArrayVelocity", "Wafer ACS LCI config should expose coordinate-array motion velocity");
    AssertContains(configSource, "0.001d", "Coordinate-array trigger window should default from the manual example");
    AssertContains(configSource, "10d", "Coordinate-array velocity should have a safe default");
}

void TestWaferFlatnessPointModeUsesSyncTriggerCapabilities()
{
    var motionSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");

    AssertContains(motionSource, "ResolvePointTriggerCapability(controlCard)", "Wafer point mode should resolve common trigger capabilities");
    AssertContains(motionSource, "ISynchronizedTriggerCard", "Wafer point mode should detect synchronized trigger capability");
    AssertContains(motionSource, "SynchronizedTriggerCapabilities.CoordinateArrayPulse", "Wafer point mode should prefer ACS coordinate-array pulse capability");
    AssertContains(motionSource, "SynchronizedTriggerCapabilities.PositionCompare", "Wafer point mode should support Googol position-compare capability");
    AssertContains(motionSource, "ExecuteSynchronizedPointTrigger", "Wafer point mode should have a common synchronized trigger execution path");
    AssertContains(motionSource, "RunSynchronizedTrigger", "Wafer point mode should invoke the common trigger interface");
    AssertContains(motionSource, "BuildPointTriggerRequest", "Wafer point mode should map wafer points and config into a common trigger request");
    AssertContains(motionSource, "CoordinateArrayMultiAxWinSize", "Wafer point mode should pass coordinate-array trigger window");
    AssertContains(motionSource, "CoordinateArrayVelocity", "Wafer point mode should pass coordinate-array velocity");
    AssertContains(motionSource, "RouteConfigOutput = config.RouteConfigOutput", "Wafer point mode should preserve ACS LCI output routing");
    AssertContains(motionSource, "ConfigOutputIndex = config.ConfigOutputIndex", "Wafer point mode should preserve ACS LCI output index");
    AssertContains(motionSource, "ConfigOutputCode = config.ConfigOutputCode", "Wafer point mode should preserve ACS LCI output code");
}

void TestWaferFlatnessPointModeRemovesAcsReflectionPath()
{
    var motionSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");

    AssertFalse(motionSource.Contains("UseAcsLciCoordinateArrayPulse", StringComparison.Ordinal), "Wafer point mode should not use ACS-only capability checks");
    AssertFalse(motionSource.Contains("TryGetAcsLciCoordinateArrayPulseMethod", StringComparison.Ordinal), "Wafer point mode should not use ACS reflection method lookup");
    AssertFalse(motionSource.Contains("BuildAcsLciCoordinateArrayPulseParam", StringComparison.Ordinal), "Wafer point mode should not build ACS-specific parameter by reflection");
    AssertFalse(motionSource.Contains("SetAcsLciCoordinateArrayPulsePoints", StringComparison.Ordinal), "Wafer point mode should not write ACS coordinate arrays by reflection");
    AssertFalse(motionSource.Contains("IsAcsControlCard", StringComparison.Ordinal), "Wafer point mode should not detect ACS by vendor name");
}

void TestWaferFlatnessSynchronizedPointModePreservesInputOrder()
{
    var motionSource = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Models\SensorMotionControlModel.cs");
    var buildPlanBody = ReadMethodBody(motionSource, "private PointExecutionPlan BuildPointExecutionPlan(");
    var executePointBody = ReadMethodBody(motionSource, "public void ExecutePointMoving(");

    AssertContains(buildPlanBody, "bool preserveInputOrder = false", "point execution plan should allow callers to preserve input order");
    AssertContains(buildPlanBody, "CreateTrajectoryModel.IsOptimalPathEnabled && !shouldInsertCalibration && !preserveInputOrder", "shortest-path sorting should be disabled when input order is preserved");
    AssertContains(executePointBody, "var triggerCapability = ResolvePointTriggerCapability(controlCard);", "point execution should detect trigger capability before planning order");
    AssertContains(executePointBody, "var useSynchronizedPointTrigger = triggerCapability != SynchronizedTriggerCapabilities.None;", "point execution should preserve order for any synchronized trigger card");
    AssertContains(executePointBody, "BuildPointExecutionPlan(Calib, sourceLocusInfos, useSynchronizedPointTrigger)", "synchronized point execution should request input-order preservation");
    AssertContainsBefore(executePointBody, "ResolvePointTriggerCapability(controlCard)", "BuildPointExecutionPlan(Calib, sourceLocusInfos, useSynchronizedPointTrigger)", "capability detection should happen before execution plan creation");
    AssertContainsBefore(executePointBody, "if (useSynchronizedPointTrigger)", "residualOrder.Clear()", "synchronized point mode should branch before ordinary residual queue execution");
}

void TestWaferFlatnessConfigViewBindsAcsLciPulseParameters()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessConfigView.xaml");

    AssertContains(xaml, "Header=\"ACS LCI固定距离脉冲\"", "Wafer config page should add an ACS LCI pulse group");
    foreach (var binding in new[]
    {
        "ModelParam.AcsLciPulseParam.IsEnabled",
        "ModelParam.AcsLciPulseParam.BufferNo",
        "ModelParam.AcsLciPulseParam.AxisX",
        "ModelParam.AcsLciPulseParam.AxisY",
        "ModelParam.AcsLciPulseParam.PulseWidth",
        "ModelParam.AcsLciPulseParam.UseSensorInterval",
        "ModelParam.AcsLciPulseParam.Interval",
        "ModelParam.AcsLciPulseParam.StartDistance",
        "ModelParam.AcsLciPulseParam.EndDistance",
        "ModelParam.AcsLciPulseParam.RouteConfigOutput",
        "ModelParam.AcsLciPulseParam.ConfigOutputIndex",
        "ModelParam.AcsLciPulseParam.ConfigOutputCode",
        "ModelParam.AcsLciPulseParam.Timeout"
    })
    {
        AssertContains(xaml, binding, $"Wafer config page should bind {binding}");
    }
}

void TestWaferFlatnessConfigViewBindsAcsLciCoordinateArrayParameters()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessConfigView.xaml");

    AssertContains(xaml, "ModelParam.AcsLciPulseParam.CoordinateArrayMultiAxWinSize", "Wafer config page should bind ACS coordinate-array trigger window");
    AssertContains(xaml, "ModelParam.AcsLciPulseParam.CoordinateArrayVelocity", "Wafer config page should bind ACS coordinate-array velocity");
    AssertContains(xaml, "坐标数组触发窗口", "Wafer config page should label the coordinate-array window");
    AssertContains(xaml, "坐标数组速度", "Wafer config page should label the coordinate-array velocity");
}

void TestWaferFlatnessConfigViewModelHardwareVisibilitySurface()
{
    var source = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\ViewModels\WaferFlatnessConfigViewModel.cs");

    AssertContains(source, "public bool IsGoogolControlCardVisible", "Wafer config VM should expose Googol parameter visibility");
    AssertContains(source, "public bool IsAcsControlCardVisible", "Wafer config VM should expose ACS parameter visibility");
    AssertContains(source, "RefreshControlCardParameterVisibility();", "Wafer config VM should refresh visibility during initialization");
    AssertContains(source, "RefreshControlCardParameterVisibility", "Wafer config VM should have a dedicated visibility refresh method");
    AssertContains(source, "IsGoogolControlCard", "Wafer config VM should detect Googol card types");
    AssertContains(source, "IsAcsControlCard", "Wafer config VM should detect ACS card types");
    AssertContains(source, ".Googol.", "Googol detection should use the card type name without a direct project reference");
    AssertContains(source, "GoogolControlCard", "Googol detection should recognize Googol control card types");
    AssertContains(source, ".ACS.", "ACS detection should use the card type name without a direct project reference");
    AssertContains(source, "AcsControlCard", "ACS detection should recognize ACS control card types");
}

void TestWaferFlatnessConfigViewUsesHardwareSpecificVisibility()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessConfigView.xaml");

    AssertContains(xaml, "ModelParam.PosComparisonParam.compareMode", "Googol config group should still bind position comparison parameters");
    AssertContains(xaml, "Visibility=\"{Binding IsGoogolControlCardVisible,Converter={StaticResource BoolToVisibilityConverter}}\"", "Googol position comparison config should be shown only for Googol cards");
    AssertContains(xaml, "ModelParam.AcsLciPulseParam.IsEnabled", "ACS config group should still bind ACS LCI parameters");
    AssertContains(xaml, "Visibility=\"{Binding IsAcsControlCardVisible,Converter={StaticResource BoolToVisibilityConverter}}\"", "ACS LCI config should be shown only for ACS cards");
}

void TestAcsConfigViewModelInitializesOptions()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions
    {
        HomeBuffers = new(),
        PegOutputs = new()
    };

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var type = viewModelType!;
    var viewModel = Activator.CreateInstance(type, card);
    var cardProperty = viewModelType!.GetProperty("Card");
    var optionsProperty = viewModelType.GetProperty("Options");

    AssertTrue(ReferenceEquals(card, cardProperty?.GetValue(viewModel)), "view model keeps selected ACS card");
    AssertTrue(ReferenceEquals(card.Options, optionsProperty?.GetValue(viewModel)), "view model exposes selected ACS options");
    AssertEqual(4, card.Options.HomeBuffers.Count, "view model creates default home buffers");
    AssertEqual(4, card.Options.PegOutputs.Count, "view model creates default PEG outputs");
}

void TestAcsConfigViewModelBufferEditorState()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var type = viewModelType!;
    var viewModel = Activator.CreateInstance(type, card);
    AssertEqual(1, viewModelType!.GetProperty("SelectedBufferNo")?.GetValue(viewModel), "default selected buffer");
    AssertEqual(string.Empty, viewModelType.GetProperty("BufferScript")?.GetValue(viewModel), "default buffer script");
    AssertTrue(viewModelType.GetProperty("BufferStatus")?.GetValue(viewModel) is string, "buffer status should be text");

    var commandNames = new[]
    {
        "UploadBufferCommand",
        "DownloadBufferCommand",
        "CompileBufferCommand",
        "RunBufferCommand",
        "StopBufferCommand",
        "ClearBufferCommand",
        "RefreshBufferStatusCommand"
    };

    foreach (var commandName in commandNames)
    {
        AssertTrue(viewModelType.GetProperty(commandName)?.GetValue(viewModel) != null, $"{commandName} should be available");
    }

    AssertTrue(viewModelType.GetProperty("BufferStateText")?.GetValue(viewModel) is string, "BufferStateText should be available");
    AssertTrue(viewModelType.GetProperty("BufferCompiledText")?.GetValue(viewModel) is string, "BufferCompiledText should be available");
    AssertTrue(viewModelType.GetProperty("BufferIsRunning")?.GetValue(viewModel) is bool, "BufferIsRunning should be available");
    AssertTrue(viewModelType.GetProperty("BufferIsCompiled")?.GetValue(viewModel) is bool, "BufferIsCompiled should be available");
}

void TestAcsConfigViewModelDBufferEditorSurface()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    AssertTrue(viewModelType!.GetProperty("OpenDBufferCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand,
        "OpenDBufferCommand should be available for the D-Buffer button");

    var optionsValue = viewModelType.GetProperty("BufferNoOptions")?.GetValue(viewModel);
    AssertTrue(optionsValue is System.Collections.IEnumerable, "BufferNoOptions should be available for the buffer dropdown");

    var options = ((System.Collections.IEnumerable)optionsValue!)
        .Cast<object>()
        .Select(Convert.ToInt32)
        .ToArray();

    AssertEqual(10, options.Length, "buffer dropdown should initially expose program buffers 0..9");
    AssertEqual(0, options[0], "first dropdown buffer");
    AssertEqual(9, options[9], "last dropdown buffer");
}

void TestAcsBufferBackendExposesClearAndStatusModel()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\BufferProgram.cs");

    AssertContains(source, "public bool TryClearProgramBuffer(int bufferNo, out string message)", "ACS should expose clear Buffer operation");
    AssertContains(source, "_api.ClearBuffer(buffer, 1, 100000)", "clear Buffer should clear the controller program text range");
    AssertContains(source, "public bool TryGetProgramBufferStatus(int bufferNo, out AcsProgramBufferStatus status, out string message)", "ACS should expose structured Buffer status");
    AssertContains(source, "public sealed class AcsProgramBufferStatus", "ACS should define a structured Buffer status model");
    AssertContains(source, "IsRunning", "Buffer status should expose running flag");
    AssertContains(source, "IsCompiled", "Buffer status should expose compiled flag");
}

void TestAcsBufferBackendExposesDBufferLookup()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\BufferProgram.cs");
    var methodBody = ReadMethodBody(source, "public bool TryGetDBufferNo(out int bufferNo, out string message)");

    AssertContains(methodBody, "_api.GetDBufferIndex()", "D-Buffer lookup should ask the controller for the D-Buffer index");
    AssertContains(methodBody, "TryToProgramBuffer(bufferNo", "D-Buffer lookup should validate the returned buffer number");
    AssertContains(methodBody, "IsConnected", "D-Buffer lookup should guard disconnected cards");
    AssertContains(methodBody, "catch (Exception ex)", "D-Buffer lookup should report controller errors without throwing");
}

void TestAcsDBufferDeclarationBuilderCanonicalizesRequiredDeclarations()
{
    var method = typeof(AcsControlCard).GetMethod(
        "BuildDBufferLciDeclarationScript",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
        binder: null,
        types: new[] { typeof(string), typeof(bool).MakeByRefType() },
        modifiers: null);

    AssertTrue(method != null, "D-Buffer declaration builder should expose a testable changed flag");

    var args = new object[] { "global LCI lc\r\n! existing D-Buffer content", false };
    var updated = (string)method!.Invoke(null, args)!;

    AssertTrue((bool)args[1], "bare LCI declaration should be rewritten to the required commented declaration");
    AssertContains(updated, "global LCI lc !定义激光变量", "D-Buffer script should contain the required LCI declaration line");
    AssertContains(updated, "global REAL pi = 3.141592653589793", "D-Buffer script should contain the required pi declaration line");
    AssertEqual(1, CountOccurrences(updated, "global LCI lc"), "D-Buffer script should not duplicate the LCI declaration");

    var completeScript = "global LCI lc !定义激光变量\r\nglobal REAL pi = 3.141592653589793";
    args = new object[] { completeScript, false };
    updated = (string)method.Invoke(null, args)!;

    AssertFalse((bool)args[1], "complete required declarations should not trigger a D-Buffer rewrite");
    AssertEqual(completeScript, updated, "complete required declarations should be preserved exactly");

    args = new object[] { "PTP X, 10\r\nSTOP", false };
    updated = (string)method.Invoke(null, args)!;

    var requiredPrefix = $"global LCI lc !定义激光变量{Environment.NewLine}global REAL pi = 3.141592653589793{Environment.NewLine}";
    AssertTrue(updated.StartsWith(requiredPrefix, StringComparison.Ordinal),
        "D-Buffer global declarations should be inserted before existing executable script so the controller compiler accepts them");

    args = new object[] { "global REAL pi = 3.14\r\nPTP X, 10", false };
    updated = (string)method.Invoke(null, args)!;

    AssertContains(updated, "global REAL pi = 3.141592653589793", "variant pi declaration should be canonicalized to the required value");
    AssertEqual(1, CountOccurrences(updated, "global REAL pi"), "D-Buffer script should not keep duplicate pi declarations when canonicalizing variants");
}

void TestAcsInitEnsuresDBufferLciDeclarations()
{
    var initSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\InitCard.cs");
    var bufferSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\BufferProgram.cs");
    var initBody = ReadMethodBody(initSource, "protected override bool DoInit()");

    AssertContains(initBody, "EnsureDBufferLciDeclarations();", "ACS init should ensure D-Buffer LCI declarations after connection setup");
    AssertContainsBefore(initBody, "DoConfigure();", "EnsureDBufferLciDeclarations();", "D-Buffer declaration ensure should run after configure");
    AssertContainsBefore(initBody, "EnsureDBufferLciDeclarations();", "return true;", "D-Buffer declaration ensure should happen before successful init returns");

    var ensureBody = ReadMethodBody(bufferSource, "public bool TryEnsureDBufferLciDeclarations(out string message)");
    AssertContains(ensureBody, "TryGetDBufferNo(out var dBufferNo", "D-Buffer ensure should use the same D-Buffer lookup path as the editor button");
    AssertContains(ensureBody, "TryUploadProgramBuffer(dBufferNo", "D-Buffer ensure should use the same read path as the editor button");
    AssertContains(bufferSource, "\"global LCI lc !定义激光变量\"", "D-Buffer ensure should write the LCI global declaration with the required comment");
    AssertContains(bufferSource, "\"global REAL pi = 3.141592653589793\"", "D-Buffer ensure should write the pi global declaration");
    AssertContains(bufferSource, "NormalizeAcsplDeclaration", "D-Buffer ensure should tolerate case and whitespace differences");
    AssertContains(ensureBody, "BuildDBufferLciDeclarationScript", "D-Buffer ensure should build an updated script only when declarations are missing");
    AssertContains(ensureBody, "TryLoadProgramBuffer(dBufferNo, updatedScript", "D-Buffer ensure should use the same download path as the editor button");
    AssertContains(ensureBody, "TryCompileProgramBuffer(dBufferNo", "D-Buffer ensure should use the same compile path as the editor button");
    AssertContains(ensureBody, "catch (Exception ex)", "D-Buffer ensure should catch controller read/write errors");
    AssertFalse(ensureBody.Contains("_api.", StringComparison.Ordinal), "D-Buffer ensure should reuse the editor button backend wrappers instead of bypassing them");
}

void TestAcsDBufferEnsureUsesBufferEditorFlowAndVerifiesResult()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\BufferProgram.cs");
    var method = typeof(AcsControlCard).GetMethod("TryEnsureDBufferLciDeclarations");
    AssertTrue(method != null, "ACS should expose a testable D-Buffer declaration ensure operation");

    var body = ReadMethodBody(source, "public bool TryEnsureDBufferLciDeclarations(out string message)");

    AssertContainsBefore(body, "TryGetDBufferNo(out var dBufferNo", "TryUploadProgramBuffer(dBufferNo", "D-Buffer ensure should first resolve then read the current controller script");
    AssertContainsBefore(body, "TryUploadProgramBuffer(dBufferNo", "TryLoadProgramBuffer(dBufferNo, updatedScript", "D-Buffer ensure should download only after merging the read script");
    AssertContainsBefore(body, "TryLoadProgramBuffer(dBufferNo, updatedScript", "TryCompileProgramBuffer(dBufferNo", "D-Buffer ensure should compile the downloaded script");
    AssertTrue(CountOccurrences(body, "TryUploadProgramBuffer(dBufferNo") >= 2,
        "D-Buffer ensure should read back after download/compile to verify the controller contains the required declarations");
    AssertContains(body, "ContainsRequiredDBufferDeclarations(verifiedScript)", "D-Buffer ensure should verify the uploaded controller script after writing");
    AssertFalse(body.Contains("_api.LoadBuffer", StringComparison.Ordinal), "D-Buffer ensure should not bypass the editor download wrapper");
    AssertFalse(body.Contains("_api.CompileBuffer", StringComparison.Ordinal), "D-Buffer ensure should not bypass the editor compile wrapper");
}

void TestAcsBufferScriptDialogExposesProgramControls()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsBufferScriptView.xaml");

    AssertContains(xaml, "Title=\"ACS Buffer脚本\"", "Buffer script editor should be a dedicated dialog");
    AssertContains(xaml, "Command=\"{Binding UploadBufferCommand}\"", "Buffer dialog should bind upload command");
    AssertContains(xaml, "Command=\"{Binding DownloadBufferCommand}\"", "Buffer dialog should bind download command");
    AssertContains(xaml, "Command=\"{Binding CompileBufferCommand}\"", "Buffer dialog should bind compile command");
    AssertContains(xaml, "Command=\"{Binding RunBufferCommand}\"", "Buffer dialog should bind run command");
    AssertContains(xaml, "Command=\"{Binding StopBufferCommand}\"", "Buffer dialog should bind stop command");
    AssertContains(xaml, "Command=\"{Binding ClearBufferCommand}\"", "Buffer dialog should bind clear command");
    AssertContains(xaml, "Text=\"{Binding BufferStateText, Mode=OneWay}\"", "Buffer dialog should display running state");
    AssertContains(xaml, "Text=\"{Binding BufferCompiledText, Mode=OneWay}\"", "Buffer dialog should display compiled state");
    AssertContains(xaml, "IsChecked=\"{Binding BufferIsRunning, Mode=OneWay}\"", "Buffer dialog should expose running indicator");
    AssertContains(xaml, "IsChecked=\"{Binding BufferIsCompiled, Mode=OneWay}\"", "Buffer dialog should expose compiled indicator");
    AssertContains(xaml, "Text=\"{Binding BufferScript, Mode=TwoWay", "Buffer dialog should keep two-way script editing");
    AssertContains(xaml, "Text=\"{Binding BufferStatus, Mode=OneWay", "Buffer dialog should keep read-only status log binding");
}

void TestAcsBufferScriptDialogUsesDropdownAndDBufferButton()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsBufferScriptView.xaml");

    AssertContains(xaml, "<ComboBox", "Buffer number should use a dropdown instead of a text input");
    AssertContains(xaml, "ItemsSource=\"{Binding BufferNoOptions}\"", "Buffer dropdown should bind 0..9 buffer options");
    AssertContains(xaml, "SelectedItem=\"{Binding SelectedBufferNo, Mode=TwoWay", "Buffer dropdown should update selected buffer number");
    AssertFalse(xaml.Contains("Text=\"{Binding SelectedBufferNo, Mode=TwoWay", StringComparison.Ordinal),
        "Buffer number should no longer be edited through a TextBox binding");
    AssertContains(xaml, "Content=\"打开D-Buffer\"", "Buffer dialog should expose a D-Buffer open button");
    AssertContains(xaml, "Command=\"{Binding OpenDBufferCommand}\"", "D-Buffer open button should bind its command");
}

void TestAcsConfigViewModelExposesConnectCommand()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    AssertTrue(viewModelType!.GetProperty("ConnectCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand, "ConnectCommand should be available");
}

void TestAcsConfigViewBindsConnectCommand()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertTrue(xaml.Contains("Command=\"{Binding ConnectCommand}\"", StringComparison.Ordinal), "config view should bind connect button to ConnectCommand");
}

void TestAcsConfigViewModelMergedCommandSurface()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Config.AllAxis.Add(new SingleAxisParam { AxisNum = En_AxisNum.X, AxisNo = 1, IsUsing = true });
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var type = viewModelType!;
    var viewModel = Activator.CreateInstance(type, card);
    var requiredCommands = new[]
    {
        "InitializeCommand",
        "CloseCommand",
        "RefreshStateCommand",
        "OpenBufferScriptCommand",
        "RefreshInputsCommand",
        "RefreshOutputsCommand",
        "SetOutputCommand",
        "ReadSelectedIoCommand",
        "EnableAxisCommand",
        "DisableAxisCommand",
        "MoveRelativeCommand",
        "MoveAbsoluteCommand",
        "JogPositiveCommand",
        "JogNegativeCommand",
        "StopAxisCommand",
        "ResetFeedbackCommand",
        "RefreshAxisStatusCommand",
        "GoHomeCommand",
        "RunLciFixedDistancePulseXsegCommand"
    };

    foreach (var commandName in requiredCommands)
    {
        AssertTrue(type.GetProperty(commandName)?.GetValue(viewModel) is System.Windows.Input.ICommand,
            $"merged config view model should expose {commandName}");
    }

    AssertTrue(type.GetProperty("SelectedAxisContext")?.GetValue(viewModel) != null,
        "merged axis page should expose a selected-axis context object");
    AssertTrue(type.GetProperty("ExecuteTransactionCommand") == null,
        "connection page should not expose ACSPL transaction command execution");
    AssertTrue(type.GetProperty("TransactionCommandText") == null,
        "connection page should not expose ACSPL command text");
    AssertTrue(type.GetProperty("TransactionResponse") == null,
        "connection page should not expose ACSPL command response text");

    AssertTrue(type.GetProperty("SelectedAxisConfig")?.GetValue(viewModel) is SingleAxisParam,
        "merged config view model should expose the selected axis config");
    AssertTrue(type.GetProperty("SelectedHomeBuffer")?.GetValue(viewModel) is AcsAxisHomeBufferConfig,
        "merged config view model should expose the selected axis home Buffer config");
    AssertTrue(type.GetProperty("Monitor")?.GetValue(viewModel) is IDisposable,
        "merged config view model should own the fixed monitor panel view model");
    AssertTrue(type.GetProperty("OpenTestPageCommand") == null,
        "merged config page should not expose a command that opens a separate test page");
    AssertTrue(type.GetProperty("OpenMonitorPageCommand") == null,
        "merged config page should not expose a command that opens a separate monitor page");
}

void TestAcsConfigViewBindsFixedMonitorPanel()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertContains(xaml, "状态监控", "merged config view should show the monitor title");
    AssertContains(xaml, "Grid.Column=\"2\"", "merged config view should keep a fixed right-side monitor column");
    AssertContains(xaml, "ItemsSource=\"{Binding Monitor.AxisRows}\"", "fixed monitor should bind axis rows");
    AssertContains(xaml, "Text=\"{Binding Monitor.ConnectionStateText}", "fixed monitor should show connection state");
    AssertContains(xaml, "Command=\"{Binding Monitor.RefreshMonitorCommand}\"", "fixed monitor should allow manual refresh");
    AssertFalse(xaml.Contains("Header=\"状态监控\"", StringComparison.Ordinal),
        "status monitor should not be hidden behind a tab header");
}

void TestAcsConfigViewBindsSelectedAxisHomeBufferEditor()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertContains(xaml, "Header=\"轴控制\"", "merged config view should expose a unified axis control tab");
    AssertFalse(xaml.Contains("Header=\"轴参数/回零\"", StringComparison.Ordinal), "axis parameters and home should not remain a separate tab");
    AssertFalse(xaml.Contains("Header=\"单轴运动\"", StringComparison.Ordinal), "single-axis motion should not remain a separate tab");
    AssertContains(xaml, "SelectedItem=\"{Binding SelectedAxis, Mode=TwoWay}\"", "axis config page should select the active axis");
    AssertContains(xaml, "Text=\"{Binding SelectedAxisConfig.AxisNo, Mode=TwoWay", "axis config page should edit the physical axis number");
    AssertContains(xaml, "IsChecked=\"{Binding SelectedAxisConfig.IsUsing, Mode=TwoWay", "axis config page should edit whether the axis is used");
    AssertContains(xaml, "ItemsSource=\"{Binding SelectedAxisSpeedRows}\"", "axis config page should edit selected-axis speed profiles in one table");
    AssertContains(xaml, "Text=\"{Binding SelectedAxisHomeBufferNo, Mode=TwoWay", "home Buffer number should be tied to the selected axis");
    AssertContains(xaml, "Command=\"{Binding OpenBufferScriptCommand}\"", "axis page should open the separate Buffer script editor");
}

void TestAcsConfigViewCombinesAxisControls()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);

    AssertTrue(axisTabStart >= 0, "axis control tab should exist");
    AssertTrue(ioTabStart > axisTabStart, "IO tab should follow the axis control tab");

    var axisTab = xaml[axisTabStart..ioTabStart];
    AssertContains(axisTab, "SelectedAxisConfig.AxisNo", "axis tab should keep axis parameter editing");
    AssertContains(axisTab, "SelectedAxisHomeBufferNo", "axis tab should keep home Buffer settings");
    AssertContains(axisTab, "MoveRelativeCommand", "axis tab should include relative move controls");
    AssertContains(axisTab, "MoveAbsoluteCommand", "axis tab should include absolute move controls");
    AssertContains(axisTab, "StartPositiveContinuousMoveCommand", "axis tab should include hold-to-move controls");
    AssertContains(axisTab, "SelectedAxisSpeedRows", "axis tab should include selected-axis speed editing");
    AssertFalse(axisTab.Contains("SelectedAxisConfig.SpeedDict1", StringComparison.Ordinal), "axis tab should not keep a selected-axis-only speed table");
    AssertContains(axisTab, "AxisMotionProfiles", "axis tab should keep ACS motion profile operations");
    AssertContains(axisTab, "RefreshMotionProfilesCommand", "axis tab should keep motion profile refresh");
    AssertContains(axisTab, "ApplySelectedMotionProfileCommand", "axis tab should allow applying selected motion profile");
    AssertContains(axisTab, "ApplyAllMotionProfilesCommand", "axis tab should allow applying all motion profiles");
}

void TestAcsConfigViewModelExposesSelectedAxisSpeedRows()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Config.AllAxis.Add(new SingleAxisParam
    {
        AxisNum = En_AxisNum.X,
        AxisNo = 0,
        IsUsing = true,
        NickName = "X Axis",
        SpeedDict1 = new()
        {
            new SpeedSetting { SpeedType = EN_SpeedType.Low, SpeedDescribe = "X low", StartSpeed = 1, MaxSpeed = 10, AccSpeed = 100 },
            new SpeedSetting { SpeedType = EN_SpeedType.Work, SpeedDescribe = "X work", StartSpeed = 2, MaxSpeed = 20, AccSpeed = 200 }
        }
    });
    card.Config.AllAxis.Add(new SingleAxisParam
    {
        AxisNum = En_AxisNum.Y,
        AxisNo = 1,
        IsUsing = true,
        NickName = "Y Axis",
        SpeedDict1 = new()
        {
            new SpeedSetting { SpeedType = EN_SpeedType.Low, SpeedDescribe = "Y low", StartSpeed = 3, MaxSpeed = 30, AccSpeed = 300 }
        }
    });
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    var allRows = ((System.Collections.IEnumerable?)viewModelType!.GetProperty("AxisSpeedRows")?.GetValue(viewModel))?
        .Cast<object>()
        .ToList();
    AssertEqual(5, allRows!.Count, "AxisSpeedRows should remain available as an all-axis compatibility collection");

    var selectedRowsProperty = viewModelType.GetProperty("SelectedAxisSpeedRows");
    AssertTrue(selectedRowsProperty != null, "SelectedAxisSpeedRows should be available for the selected-axis speed table");
    var selectedRows = ((System.Collections.IEnumerable?)selectedRowsProperty!.GetValue(viewModel))?
        .Cast<object>()
        .ToList();

    AssertTrue(selectedRows != null, "SelectedAxisSpeedRows should be enumerable");
    AssertEqual(3, selectedRows!.Count, "SelectedAxisSpeedRows should initially show only X speed settings plus Custom");
    AssertTrue(selectedRows.All(row => Equals(row.GetType().GetProperty("Axis")?.GetValue(row), En_AxisNum.X)),
        "initial selected-axis rows should all belong to X");

    viewModelType.GetProperty("SelectedAxis")?.SetValue(viewModel, En_AxisNum.Y);
    selectedRows = ((System.Collections.IEnumerable?)selectedRowsProperty.GetValue(viewModel))?
        .Cast<object>()
        .ToList();

    AssertTrue(selectedRows != null, "SelectedAxisSpeedRows should still be enumerable after selecting Y");
    AssertEqual(2, selectedRows!.Count, "SelectedAxisSpeedRows should update after selecting Y plus Custom");
    var yRow = selectedRows[0];
    var rowType = yRow.GetType();
    AssertEqual(En_AxisNum.Y, rowType.GetProperty("Axis")?.GetValue(yRow), "selected-axis speed row should expose the selected axis");

    rowType.GetProperty("MaxSpeed")?.SetValue(yRow, 66d);
    AssertEqual(66d, card.Config.AllAxis[1].SpeedDict1[0].MaxSpeed, "selected-axis speed row should write through to the selected axis speed setting");
}

void TestControlCardSpeedTypesExposeCustomSetting()
{
    AssertTrue(Enum.GetValues<EN_SpeedType>().Contains(EN_SpeedType.Custom), "speed type enum should include Custom");

    var deserializedAxis = new SingleAxisParam();
    var onDeserialized = typeof(SingleAxisParam).GetMethod(
        "OnDeserializedMethod",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(onDeserialized != null, "SingleAxisParam deserialization hook should exist");
    onDeserialized!.Invoke(deserializedAxis, new object[] { new System.Runtime.Serialization.StreamingContext() });
    AssertTrue(deserializedAxis.SpeedDict1.Any(speed => speed.SpeedType == EN_SpeedType.Custom),
        "deserialized axis default speeds should include Custom");

    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Config.AllAxis.Add(new SingleAxisParam
    {
        AxisNum = En_AxisNum.X,
        AxisNo = 1,
        IsUsing = true,
        SpeedDict1 = new()
        {
            new SpeedSetting { SpeedType = EN_SpeedType.Low, SpeedDescribe = "X low", StartSpeed = 1, MaxSpeed = 10, AccSpeed = 100 }
        }
    });
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    var speedTypeOptions = ((System.Collections.IEnumerable?)viewModelType!.GetProperty("SpeedTypeOptions")?.GetValue(viewModel))?
        .Cast<object>()
        .ToList();
    AssertTrue(speedTypeOptions?.Contains(EN_SpeedType.Custom) == true, "ACS speed type selector should expose Custom");

    var selectedRows = ((System.Collections.IEnumerable?)viewModelType.GetProperty("SelectedAxisSpeedRows")?.GetValue(viewModel))?
        .Cast<object>()
        .ToList();
    AssertTrue(selectedRows != null, "selected-axis speed rows should be available");
    var customRow = selectedRows!.FirstOrDefault(row => Equals(row.GetType().GetProperty("SpeedType")?.GetValue(row), EN_SpeedType.Custom));
    AssertTrue(customRow != null, "existing axis speed rows should be backfilled with Custom");
    AssertEqual("自定义速度", customRow!.GetType().GetProperty("SpeedDescribe")?.GetValue(customRow), "custom speed row should use a readable description");

    customRow.GetType().GetProperty("MaxSpeed")?.SetValue(customRow, 88d);
    var customSetting = card.Config.AllAxis[0].SpeedDict1.First(speed => speed.SpeedType == EN_SpeedType.Custom);
    AssertEqual(88d, customSetting.MaxSpeed, "custom speed row should be editable and write through to SpeedDict1");
}

void TestAcsConfigViewUsesSelectedAxisSpeedTable()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertContains(xaml, "ItemsSource=\"{Binding SelectedAxisSpeedRows}\"", "axis control tab should bind selected-axis speed rows");
    AssertFalse(xaml.Contains("ItemsSource=\"{Binding AxisSpeedRows}\"", StringComparison.Ordinal), "axis control tab should not bind all-axis speed rows");
    AssertContains(xaml, "SpeedDict1", "axis control tab should explain selected-axis speed editing");

    var speedTableStart = xaml.IndexOf("ItemsSource=\"{Binding SelectedAxisSpeedRows}\"", StringComparison.Ordinal);
    AssertTrue(speedTableStart >= 0, "selected-axis speed table should exist in XAML");
    var columnsStart = xaml.IndexOf("<DataGrid.Columns>", speedTableStart, StringComparison.Ordinal);
    var columnsEnd = xaml.IndexOf("</DataGrid.Columns>", speedTableStart, StringComparison.Ordinal);
    AssertTrue(columnsStart >= 0 && columnsEnd > columnsStart, "selected-axis speed table should have explicit columns");
    var speedColumns = xaml[columnsStart..columnsEnd];

    AssertFalse(speedColumns.Contains("Binding=\"{Binding Axis}\"", StringComparison.Ordinal), "selected-axis speed table should not repeat the axis column");
    AssertFalse(speedColumns.Contains("Binding=\"{Binding AxisNo", StringComparison.Ordinal), "selected-axis speed table should not repeat the physical axis column");
    AssertFalse(speedColumns.Contains("Binding=\"{Binding AxisName", StringComparison.Ordinal), "selected-axis speed table should not repeat the axis name column");
    AssertFalse(speedColumns.Contains("Binding=\"{Binding IsUsing", StringComparison.Ordinal), "selected-axis speed table should not repeat the axis enabled column");
    AssertContains(speedColumns, "Binding=\"{Binding SpeedType}", "selected-axis speed table should show speed type");
    AssertContains(speedColumns, "Binding=\"{Binding SpeedDescribe", "selected-axis speed table should show speed description");
    AssertContains(speedColumns, "Binding=\"{Binding StartSpeed", "selected-axis speed table should show start speed");
    AssertContains(speedColumns, "Binding=\"{Binding MaxSpeed", "selected-axis speed table should show max speed");
    AssertContains(speedColumns, "Binding=\"{Binding AccSpeed", "selected-axis speed table should show acceleration");

    AssertContains(xaml, "SelectedAxisHomeBufferNo", "axis tab should keep selected-axis home Buffer settings");
    AssertContains(xaml, "Options.XyHomeBuffer", "axis tab should keep ACS X/Y combined home Buffer settings");
    AssertContains(xaml, "StartPositiveContinuousMoveCommand", "axis tab should keep hold-to-move controls");
}

void TestAcsConfigViewAxisTabUsesScrollableNonOverlappingLayout()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);

    AssertTrue(axisTabStart >= 0, "axis control tab should exist");
    AssertTrue(ioTabStart > axisTabStart, "IO tab should follow axis control tab");

    var axisTab = xaml[axisTabStart..ioTabStart];
    AssertContains(axisTab, "x:Name=\"AxisWorkspaceGrid\"", "axis tab should expose the workspace grid");
    AssertContains(axisTab, "x:Name=\"AxisConfigColumn\"", "axis tab should expose the left configuration column");
    AssertContains(axisTab, "x:Name=\"AxisSettingsColumn\"", "axis tab should expose the middle settings column");
    AssertContains(axisTab, "x:Name=\"AxisDebugColumn\"", "axis tab should expose the right debug column");
    AssertFalse(axisTab.Contains("MinWidth=\"900\"", StringComparison.Ordinal),
        "axis tab should not rely on the old broad horizontal scroll layout");
    AssertFalse(axisTab.Contains("<ScrollViewer VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Auto\"", StringComparison.Ordinal),
        "normal axis layout should avoid the old top-level horizontal scroll viewer");
}

void TestAcsConfigViewAxisTabUsesThreeColumnWorkspace()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);

    AssertTrue(axisTabStart >= 0, "axis control tab should exist");
    AssertTrue(ioTabStart > axisTabStart, "IO tab should follow axis control tab");

    var axisTab = xaml[axisTabStart..ioTabStart];
    AssertContains(axisTab, "x:Name=\"AxisContextBar\"", "axis tab should have a top selected-axis context bar");
    AssertContains(axisTab, "当前轴", "axis context bar should label the selected axis");
    AssertContains(axisTab, "SelectedItem=\"{Binding SelectedAxis, Mode=TwoWay}\"", "axis context bar should bind selected axis");
    AssertContains(axisTab, "当前轴基础配置", "left column should be titled as current-axis basic configuration");
    AssertContains(axisTab, "速度参数", "middle column should expose selected-axis speed parameters");
    AssertContains(axisTab, "单轴回零 Buffer", "middle column should expose selected-axis home Buffer settings");
    AssertContains(axisTab, "调试操作", "right column should expose debug operations");
    AssertContains(axisTab, "Grid.Column=\"0\"", "workspace should place content in the left column");
    AssertContains(axisTab, "Grid.Column=\"1\"", "workspace should place content in the middle column");
    AssertContains(axisTab, "Grid.Column=\"2\"", "workspace should place content in the right column");
}

void TestAcsConfigViewAxisTabKeepsFixedDebugPanel()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);

    AssertTrue(axisTabStart >= 0, "axis control tab should exist");
    AssertTrue(ioTabStart > axisTabStart, "IO tab should follow axis control tab");

    var axisTab = xaml[axisTabStart..ioTabStart];
    var debugStart = axisTab.IndexOf("x:Name=\"AxisDebugColumn\"", StringComparison.Ordinal);

    AssertTrue(debugStart >= 0, "axis debug column should exist");
    var debugPanel = axisTab[debugStart..];

    AssertContains(debugPanel, "Command=\"{Binding EnableAxisCommand}\"", "debug panel should keep axis enable command");
    AssertContains(debugPanel, "Command=\"{Binding DisableAxisCommand}\"", "debug panel should keep axis disable command");
    AssertContains(debugPanel, "Command=\"{Binding StopAxisCommand}\"", "debug panel should keep stop command visible");
    AssertContains(debugPanel, "Command=\"{Binding MoveRelativeCommand}\"", "debug panel should keep relative move command");
    AssertContains(debugPanel, "Command=\"{Binding MoveAbsoluteCommand}\"", "debug panel should keep absolute move command");
    AssertContains(debugPanel, "Command=\"{Binding StartPositiveContinuousMoveCommand}\"", "debug panel should keep positive hold-to-move command");
    AssertContains(debugPanel, "Command=\"{Binding StartNegativeContinuousMoveCommand}\"", "debug panel should keep negative hold-to-move command");
    AssertContains(debugPanel, "Command=\"{Binding StopContinuousMoveCommand}\"", "debug panel should keep hold-to-move stop command");
    AssertContains(debugPanel, "Text=\"{Binding AxisStatusText, Mode=OneWay}\"", "debug panel should keep status output visible");
}

void TestAcsConfigViewAxisTabKeepsAcsSpecificSettings()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);

    AssertTrue(axisTabStart >= 0, "axis control tab should exist");
    AssertTrue(ioTabStart > axisTabStart, "IO tab should follow axis control tab");

    var axisTab = xaml[axisTabStart..ioTabStart];

    AssertContains(axisTab, "ItemsSource=\"{Binding SelectedAxisSpeedRows}\"", "axis tab should keep selected-axis speed rows");
    AssertContains(axisTab, "SelectedAxisHomeBufferNo", "axis tab should keep per-axis home Buffer number");
    AssertContains(axisTab, "SelectedAxisHomeBufferTimeout", "axis tab should keep per-axis home Buffer timeout");
    AssertContains(axisTab, "Options.XyHomeBuffer", "axis tab should keep ACS XY combined home Buffer settings");
    AssertContains(axisTab, "Style=\"{DynamicResource ExpanderStyle}\"", "ACS-specific global settings should use the shared expander style");
    AssertContains(axisTab, "ItemsSource=\"{Binding AxisMotionProfiles}\"", "axis tab should keep ACS motion profile rows");
    AssertContains(axisTab, "SelectedItem=\"{Binding SelectedMotionProfile, Mode=TwoWay}\"", "axis tab should bind selected ACS motion profile");
}

void TestAcsConfigViewRemovesAcsplTransactionControls()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertContains(xaml, "Header=\"连接\"", "connection tab should remain available");
    AssertFalse(xaml.Contains("Header=\"连接/命令\"", StringComparison.Ordinal), "connection tab should no longer mention commands");
    AssertFalse(xaml.Contains("ACSPL命令", StringComparison.Ordinal), "connection tab should remove ACSPL command label");
    AssertFalse(xaml.Contains("TransactionCommandText", StringComparison.Ordinal), "connection tab should remove ACSPL command input binding");
    AssertFalse(xaml.Contains("TransactionResponse", StringComparison.Ordinal), "connection tab should remove ACSPL response binding");
    AssertFalse(xaml.Contains("ExecuteTransactionCommand", StringComparison.Ordinal), "connection tab should remove ACSPL execute command binding");
}

void TestAcsOptionsExposeXyCombinedHomeBufferDefaults()
{
    var options = new AcsControlCardOptions();
    var property = typeof(AcsControlCardOptions).GetProperty("XyHomeBuffer");

    AssertTrue(property != null, "ACS options should expose the XY combined home Buffer config");

    var config = property!.GetValue(options);
    AssertTrue(config != null, "XY combined home Buffer config should be created by default");
    AssertEqual(true, GetReflectedProperty<bool>(config!, "IsEnabled"), "XY combined home Buffer should default to enabled");
    AssertEqual(1, GetReflectedProperty<int>(config!, "XBufferNo"), "XY two-buffer home should default X to Buffer 1");
    AssertEqual(2, GetReflectedProperty<int>(config!, "YBufferNo"), "XY two-buffer home should default Y to Buffer 2");
    AssertEqual(60000, GetReflectedProperty<int>(config!, "Timeout"), "XY combined home Buffer should allow both axes to finish");
    AssertEqual(true, GetReflectedProperty<bool>(config!, "StopAxesBeforeRun"), "XY combined home should stop axes before running");
    AssertEqual(2, GetReflectedProperty<int>(config!, "XPhysicalAxis"), "XY combined home should use ACS axis 2 for X");
    AssertEqual(0, GetReflectedProperty<int>(config!, "YPhysicalAxis"), "XY combined home should use ACS axis 0 for Y");
}

void TestAcsXyCombinedHomeScriptMatchesReference()
{
    var configType = Type.GetType("ReeYin_V.Hardware.ControlCard.ACS.App.AcsXyHomeBufferConfig, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(configType != null, "AcsXyHomeBufferConfig type should exist");

    var config = Activator.CreateInstance(configType!);
    var xMethod = typeof(AcsControlCard).GetMethod(
        "BuildXHomeBufferScript",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    var yMethod = typeof(AcsControlCard).GetMethod(
        "BuildYHomeBufferScript",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

    AssertTrue(xMethod != null, "ACS should expose a builder for the generated X home Buffer script");
    AssertTrue(yMethod != null, "ACS should expose a builder for the generated Y home Buffer script");

    var xScript = xMethod!.Invoke(null, new[] { config })?.ToString() ?? string.Empty;
    var yScript = yMethod!.Invoke(null, new[] { config })?.ToString() ?? string.Empty;

    AssertContains(xScript, "!HOMING X", "X home script should have an X homing header");
    AssertContains(yScript, "!HOMING Y", "Y home script should have a Y homing header");
    AssertEqual(1, CountOccurrences(xScript, "FCLEAR ALL"), "X home script should clear faults once");
    AssertEqual(1, CountOccurrences(yScript, "FCLEAR ALL"), "Y home script should clear faults once");
    AssertEqual(1, CountOccurrences(xScript, "STOP"), "X home script should stop once at the end");
    AssertEqual(1, CountOccurrences(yScript, "STOP"), "Y home script should stop once at the end");

    foreach (var expected in new[]
    {
        "AXIS=2",
        "VEL(AXIS)= 20",
        "ACC(AXIS)= 100",
        "DEC(AXIS)= 100",
        "JERK(AXIS)= 900",
        "KDEC(AXIS)= 300",
        "PTP (AXIS),-340"
    })
    {
        AssertContains(xScript, expected, $"X home script should contain '{expected}'");
    }

    foreach (var expected in new[]
    {
        "AXIS=0",
        "VEL(AXIS)= 10",
        "ACC(AXIS)= 50",
        "DEC(AXIS)= 50",
        "JERK(AXIS)= 500",
        "KDEC(AXIS)= 150",
        "!COMMUT (AXIS)",
        "PTP (AXIS),-48"
    })
    {
        AssertContains(yScript, expected, $"Y home script should contain '{expected}'");
    }

    AssertFalse(xScript.Contains("AXIS=0", StringComparison.Ordinal), "X Buffer script should not contain Y axis sequence");
    AssertFalse(yScript.Contains("AXIS=2", StringComparison.Ordinal), "Y Buffer script should not contain X axis sequence");
    AssertContainsBefore(xScript, "AXIS=2", "PTP (AXIS),-340", "X sequence should move after selecting X axis");
    AssertContainsBefore(yScript, "AXIS=0", "PTP (AXIS),-48", "Y sequence should move after selecting Y axis");
}

void TestAcsGoHomeUsesXyCombinedBuffer()
{
    var goHomeSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\GoHome.cs");
    var xyHomeSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsXyHome.cs");
    var body = ReadMethodBody(goHomeSource, "protected override bool DoGoHome(out string message)");

    AssertContains(body, "TryBuildXyHomePlan(axes", "DoGoHome should split X/Y from the per-axis home plan");
    AssertContains(body, "RunXyHomeBuffer(xAxis, yAxis, xyHomeConfig", "DoGoHome should run the XY two-buffer home routine");
    AssertContainsBefore(body, "RunXyHomeBuffer", "RunAxisHomeBuffer", "XY combined home should run before remaining per-axis homes");
    AssertContains(xyHomeSource, "remainingAxes = axes.Where(axis => !IsXyHomeAxis(axis.AxisNum)).ToArray();",
        "XY axes should be removed from the per-axis plan when combined home is enabled");
    AssertContains(xyHomeSource, "xAxis.IsResetCompleted = true;", "combined home should mark X reset complete");
    AssertContains(xyHomeSource, "yAxis.IsResetCompleted = true;", "combined home should mark Y reset complete");
    AssertContains(xyHomeSource, "TryPrepareProgramBuffer(homeConfig.XBufferNo, out var xBuffer", "XY home should prepare the X Buffer");
    AssertContains(xyHomeSource, "TryPrepareProgramBuffer(homeConfig.YBufferNo, out var yBuffer", "XY home should prepare the Y Buffer");
    AssertContains(xyHomeSource, "BuildXHomeBufferScript(homeConfig)", "XY home should build a dedicated X script");
    AssertContains(xyHomeSource, "BuildYHomeBufferScript(homeConfig)", "XY home should build a dedicated Y script");
    AssertContains(xyHomeSource, "_api.LoadBuffer(xBuffer, xScript);", "XY home should download the generated X script into X Buffer");
    AssertContains(xyHomeSource, "_api.LoadBuffer(yBuffer, yScript);", "XY home should download the generated Y script into Y Buffer");
    AssertContains(xyHomeSource, "_api.RunBuffer(xBuffer, null);", "XY home should start X Buffer");
    AssertContains(xyHomeSource, "_api.RunBuffer(yBuffer, null);", "XY home should start Y Buffer");
    AssertContainsBefore(xyHomeSource, "_api.RunBuffer(xBuffer, null);", "_api.WaitProgramEnd(xBuffer, timeout);", "X Buffer should start before waiting");
    AssertContainsBefore(xyHomeSource, "_api.RunBuffer(yBuffer, null);", "_api.WaitProgramEnd(xBuffer, timeout);", "Y Buffer should start before waiting on either Buffer");
    AssertContains(xyHomeSource, "_api.WaitProgramEnd(xBuffer, timeout);", "XY home should wait for the X Buffer to finish");
    AssertContains(xyHomeSource, "_api.WaitProgramEnd(yBuffer, timeout);", "XY home should wait for the Y Buffer to finish");
}

void TestAcsGoHomeEnsuresDBufferLciDeclarationsAfterReset()
{
    var goHomeSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\GoHome.cs");
    var body = ReadMethodBody(goHomeSource, "protected override bool DoGoHome(out string message)");

    AssertContains(body, "EnsureDBufferLciDeclarations(out var dBufferMessage)", "DoGoHome should ensure D-Buffer declarations after successful reset");
    AssertContainsBefore(body, "EnsureDBufferLciDeclarations(out var dBufferMessage)", "IsAxisHomed = true;", "D-Buffer ensure should succeed before the reset is marked complete");
    AssertContainsBefore(body, "EnsureDBufferLciDeclarations(out var dBufferMessage)", "message = $\"ACS 已完成", "D-Buffer ensure should run before reporting reset success");
    AssertContains(body, "D-Buffer LCI 全局声明写入失败", "DoGoHome should surface D-Buffer write failures instead of reporting reset success");
}

void TestAcsConfigViewBindsXyCombinedHomeBuffer()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertContains(xaml, "X/Y合并回零", "axis home page should expose XY combined home configuration");
    AssertContains(xaml, "IsChecked=\"{Binding Options.XyHomeBuffer.IsEnabled, Mode=TwoWay", "XY combined home enabled flag should be editable");
    AssertContains(xaml, "Text=\"{Binding Options.XyHomeBuffer.XBufferNo, Mode=TwoWay", "XY combined home X Buffer number should be editable");
    AssertContains(xaml, "Text=\"{Binding Options.XyHomeBuffer.YBufferNo, Mode=TwoWay", "XY combined home Y Buffer number should be editable");
    AssertContains(xaml, "Text=\"{Binding Options.XyHomeBuffer.Timeout, Mode=TwoWay", "XY combined home timeout should be editable");
    AssertContains(xaml, "IsChecked=\"{Binding Options.XyHomeBuffer.StopAxesBeforeRun, Mode=TwoWay", "XY combined home stop-before-run flag should be editable");
    AssertContains(xaml, "Text=\"{Binding Options.XyHomeBuffer.XPhysicalAxis, Mode=TwoWay", "XY combined home X ACS axis should be editable");
    AssertContains(xaml, "Text=\"{Binding Options.XyHomeBuffer.YPhysicalAxis, Mode=TwoWay", "XY combined home Y ACS axis should be editable");
}

void TestAcsConfigViewRemovesObsoleteEntries()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertFalse(xaml.Contains("PEG输出", StringComparison.Ordinal), "merged config view should remove the PEG output page");
    AssertFalse(xaml.Contains("Header=\"回零Buffer\"", StringComparison.Ordinal), "merged config view should remove the standalone home Buffer grid page");
    AssertFalse(xaml.Contains("Header=\"插补测试\"", StringComparison.Ordinal), "merged config view should remove interpolation test page");
    AssertFalse(xaml.Contains("Command=\"{Binding OpenTestPageCommand}\"", StringComparison.Ordinal), "merged config view should not open a separate test page");
    AssertFalse(xaml.Contains("Command=\"{Binding OpenMonitorPageCommand}\"", StringComparison.Ordinal), "merged config view should not open a separate monitor page");
    AssertFalse(xaml.Contains("RunLineInterpolationCommand", StringComparison.Ordinal), "merged config view should not bind interpolation test commands");
}

void TestAcsRelativeMoveUsesAcsMotionPipelineDirectly()
{
    var methodBody = ReadAcsRelativeMoveMethodBody();

    AssertFalse(methodBody.Contains("base.Move", StringComparison.Ordinal), "ACS relative Move should not delegate to base.Move");
    AssertTrue(methodBody.Contains("IsConnected", StringComparison.Ordinal), "ACS relative Move should validate connection");
    AssertTrue(methodBody.Contains("DoGetAxisEnable", StringComparison.Ordinal), "ACS relative Move should validate axis enable state");
    AssertTrue(methodBody.Contains("DoGetAxisStopped", StringComparison.Ordinal), "ACS relative Move should validate axis stopped state before moving");
    AssertTrue(methodBody.Contains("DoMoveAxis", StringComparison.Ordinal), "ACS relative Move should call ACS relative motion implementation");
    AssertTrue(methodBody.Contains("WaitUntilAxisStopped", StringComparison.Ordinal), "ACS relative Move should wait with the ACS timeout helper");
}

void TestAcsRelativeMoveAppliesDirectionToSignedDistance()
{
    var methodBody = ReadAcsRelativeMoveMethodBody();

    AssertTrue(methodBody.Contains("MoveDirection.反向 ? -Math.Abs(um) : Math.Abs(um)", StringComparison.Ordinal),
        "ACS relative Move should convert direction and distance to a signed relative distance");
}

void TestAcsOptionsExposeInterpolationBufferDefaults()
{
    var options = new AcsControlCardOptions();
    AssertEqual(9, options.InterpolationBufferNo, "default interpolation Buffer should avoid home and LCI defaults");

    options.InterpolationBufferNo = -1;
    AssertEqual(0, options.InterpolationBufferNo, "interpolation Buffer should clamp low values");

    options.InterpolationBufferNo = 100;
    AssertEqual(64, options.InterpolationBufferNo, "interpolation Buffer should clamp high values");

    var requestSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardSyncModels.cs");
    var axisModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisModel.cs");
    var coordinatedRequestSource = ReadBlockStartingAt(requestSource, "public sealed class CoordinatedMotionRequest");
    var lineInterpoParamSource = ReadBlockStartingAt(axisModelSource, "public class LineInterPoParam");
    var arcInterpoParamSource = ReadBlockStartingAt(axisModelSource, "public class ArcInterPoParam");

    AssertContains(coordinatedRequestSource, "public int? BufferNo", "coordinated motion request should carry an optional Buffer number");
    AssertContains(lineInterpoParamSource, "public int? BufferNo", "line interpolation params should carry an optional Buffer number");
    AssertContains(arcInterpoParamSource, "public int? BufferNo", "arc interpolation params should carry an optional Buffer number");
}

void TestAcsInterpolationUsesBufferScripts()
{
    var lineSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var arcSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\CircularInterpolation.cs");
    var xsegSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\XsegInterpolation.cs");
    var bufferSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\InterpolationBufferMotion.cs");

    AssertContains(bufferSource, "RunInterpolationBufferScript", "ACS interpolation should share a Buffer execution helper");
    AssertContains(bufferSource, "_api.LoadBuffer(buffer, script)", "interpolation Buffer helper should load script");
    AssertContains(bufferSource, "_api.CompileBuffer(buffer)", "interpolation Buffer helper should compile script");
    AssertContains(bufferSource, "_api.RunBuffer(buffer, null)", "interpolation Buffer helper should run script");
    AssertContains(bufferSource, "_api.WaitProgramEnd(buffer", "interpolation Buffer helper should wait when requested");

    AssertContains(bufferSource, "BuildLineInterpolationBufferScript", "line interpolation should have a script builder");
    AssertContains(bufferSource, "BuildXsegLineInterpolationBufferScript", "XSEG line interpolation should have a script builder");
    AssertContains(bufferSource, "BuildArcInterpolationBufferScript", "arc interpolation should have a script builder");
    AssertContains(bufferSource, "BuildXsegArcInterpolationBufferScript", "XSEG arc interpolation should have a script builder");

    AssertContains(ReadMethodBody(lineSource, "public override bool LineInterpoMoving(LineInterPoParam param)"),
        "RunInterpolationBufferScript", "basic line interpolation should run via Buffer");
    AssertContains(ReadMethodBody(lineSource, "public bool LineInterpoMovingXseg(LineInterPoParam param)"),
        "RunInterpolationBufferScript", "XSEG line interpolation should run via Buffer");
    AssertContains(ReadMethodBody(arcSource, "public override bool ArcInterpoMoving(ArcInterPoParam param)"),
        "RunInterpolationBufferScript", "basic arc interpolation should run via Buffer");
    AssertContains(ReadMethodBody(arcSource, "public bool ArcInterpoMovingXseg(ArcInterPoParam param)"),
        "RunInterpolationBufferScript", "XSEG arc interpolation should run via Buffer");

    foreach (var source in new[] { lineSource, arcSource, xsegSource, bufferSource })
    {
        AssertFalse(source.Contains("_api.Line(", StringComparison.Ordinal), "ACS interpolation must not call SDK Line");
        AssertFalse(source.Contains("_api.ExtLine(", StringComparison.Ordinal), "ACS interpolation must not call SDK ExtLine");
        AssertFalse(source.Contains("_api.Arc1(", StringComparison.Ordinal), "ACS interpolation must not call SDK Arc1");
        AssertFalse(source.Contains("_api.ExtArc1(", StringComparison.Ordinal), "ACS interpolation must not call SDK ExtArc1");
        AssertFalse(source.Contains("ExtendedSegmentedMotionV2", StringComparison.Ordinal), "ACS interpolation must not call SDK XSEG start");
        AssertFalse(source.Contains("SegmentLineV2", StringComparison.Ordinal), "ACS interpolation must not call SDK segment line");
        AssertFalse(source.Contains("SegmentArc1V2", StringComparison.Ordinal), "ACS interpolation must not call SDK segment arc");
        AssertFalse(source.Contains("EndSequenceM", StringComparison.Ordinal), "ACS interpolation must not call SDK XSEG end");
    }
}

void TestAcsLineInterpolationBufferMatchesLciPtpReferenceScript()
{
    var axes = new[] { (Axis)2, (Axis)0 };
    var startPoint = new[] { 0d, 0d };
    var target = new[] { 100d, 100d };

    var lineScript = AcsControlCard.BuildLineInterpolationBufferScript(axes, startPoint, target, 20d);
    var xsegScript = AcsControlCard.BuildXsegLineInterpolationBufferScript(axes, startPoint, target, 20d);
    var lineSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var basicLineBody = ReadMethodBody(lineSource, "public override bool LineInterpoMoving(LineInterPoParam param)");

    AssertLineInterpolationPtpReferenceFlow(lineScript, "ordinary line Buffer");
    AssertLineInterpolationPtpReferenceFlow(xsegScript, "XSEG line Buffer");
    AssertContains(basicLineBody, "TryGetCurrentInterpolationPoint(axisIds, out var startPoint)",
        "ordinary line Buffer should capture the current interpolation point for LCI start");
    AssertContains(basicLineBody, "BuildLineInterpolationBufferScript(axes, startPoint, target, motionProfile, param.PulseOutput)",
        "ordinary line Buffer should pass the captured start point and Work speed profile into the script builder");
}

void AssertLineInterpolationPtpReferenceFlow(string script, string label)
{
    AssertContains(script, "int AxX = 2", $"{label} should map first interpolation axis to AxX");
    AssertContains(script, "int AxY = 0", $"{label} should map second interpolation axis to AxY");
    AssertContains(script, "real XStartPos, YStartPos", $"{label} should declare XY start positions");
    AssertContains(script, "real XStopPos, YStopPos", $"{label} should declare XY stop positions");
    AssertContains(script, "ENABLE (AxX, AxY)", $"{label} should enable the two axes");
    AssertContains(script, "TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )", $"{label} should wait until both axes are enabled");
    AssertContains(script, "VEL(AxX) = 20", $"{label} should write the requested X-axis velocity");
    AssertContains(script, "VEL(AxY) = 20", $"{label} should write the requested Y-axis velocity");
    AssertContains(script, "XStartPos = 0", $"{label} should assign X start");
    AssertContains(script, "YStartPos = 0", $"{label} should assign Y start");
    AssertContains(script, "XStopPos = 100", $"{label} should assign X stop");
    AssertContains(script, "YStopPos = 100", $"{label} should assign Y stop");
    AssertContains(script, "PTP/e (AxX, AxY), XStartPos, YStartPos", $"{label} should PTP/e to the start point");
    AssertContains(script, "lc.Init()", $"{label} should initialize lc between start and stop moves");
    AssertContains(script, "PTP/e (AxX, AxY), XStopPos, YStopPos", $"{label} should PTP/e to the stop point");
    AssertContainsBefore(script, "PTP/e (AxX, AxY), XStartPos, YStartPos", "lc.Init()",
        $"{label} should move to start before lc.Init");
    AssertContainsBefore(script, "lc.Init()", "XStopPos = 100",
        $"{label} should initialize lc before assigning stop position");
    AssertContainsBefore(script, "XStopPos = 100", "PTP/e (AxX, AxY), XStopPos, YStopPos",
        $"{label} should assign stop before the processing move");
    AssertContainsBefore(script, "PTP/e (AxX, AxY), XStopPos, YStopPos", "STOP",
        $"{label} should stop after the final PTP/e move");
    AssertFalse(script.Contains("LINE", StringComparison.Ordinal), $"{label} should not use LINE in the reference PTP/e flow");
    AssertFalse(script.Contains("XSEG", StringComparison.Ordinal), $"{label} should not use XSEG in the reference PTP/e flow");
    AssertFalse(script.Contains("FixedDistPulse", StringComparison.Ordinal), $"{label} should not add fixed-distance pulse setup beyond the reference script");
    AssertFalse(script.Contains("LaserEnable", StringComparison.Ordinal), $"{label} should not add laser enable outside the reference script");
    AssertFalse(script.Contains("GetPulseCounts", StringComparison.Ordinal), $"{label} should not read pulse counts outside the reference script");
}

void TestAcsLineInterpolationBufferUsesConfiguredWorkSpeedProfile()
{
    var bufferSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\InterpolationBufferMotion.cs");
    var lineSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var basicLineBody = ReadMethodBody(lineSource, "public override bool LineInterpoMoving(LineInterPoParam param)");
    var xsegLineBody = ReadMethodBody(lineSource, "public bool LineInterpoMovingXseg(LineInterPoParam param)");

    AssertContains(bufferSource, "SpeedSetting motionProfile", "line PTP/LCI Buffer script builder should accept a configured speed profile");
    AssertContains(lineSource, "ResolveLineInterpolationWorkMotionProfile", "line interpolation should resolve the configured Work speed profile");
    AssertContains(basicLineBody, "ConfigureInterpolationAxes(axisIds, EN_SpeedType.Work)",
        "ordinary line interpolation should configure axes with the Work speed profile");
    AssertContains(xsegLineBody, "ConfigureInterpolationAxes(axisIds, EN_SpeedType.Work)",
        "XSEG line interpolation should configure axes with the Work speed profile");
    AssertContains(basicLineBody, "ResolveLineInterpolationWorkMotionProfile(axisIds",
        "ordinary line interpolation should resolve Work speed before script generation");
    AssertContains(xsegLineBody, "ResolveLineInterpolationWorkMotionProfile(axisIds",
        "XSEG line interpolation should resolve Work speed before script generation");
    AssertFalse(basicLineBody.Contains("GetInterpolationVelocity(axisIds, param.DefaultSpeed)", StringComparison.Ordinal),
        "ordinary line interpolation should not use the selected default speed for PTP/LCI script velocity");
    AssertFalse(xsegLineBody.Contains("GetInterpolationVelocity(axisIds, param.DefaultSpeed)", StringComparison.Ordinal),
        "XSEG line interpolation should not use the selected default speed for PTP/LCI script velocity");

    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    var workProfile = new SpeedSetting
    {
        SpeedType = EN_SpeedType.Work,
        SpeedDescribe = "X work",
        StartSpeed = 3,
        MaxSpeed = 44,
        AccSpeed = 440,
        DecSpeed = 441,
        KillDecSpeed = 4420,
        Jerk = 4430
    };
    card.Config.AllAxis.Add(new SingleAxisParam
    {
        AxisNum = En_AxisNum.X,
        AxisNo = 3,
        IsUsing = true,
        SpeedDict1 = new() { workProfile }
    });

    var resolver = typeof(AcsControlCard).GetMethod(
        "ResolveLineInterpolationWorkMotionProfile",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    AssertTrue(resolver != null, "line interpolation Work speed resolver should be available");
    var resolvedProfile = (SpeedSetting)resolver!.Invoke(card, new object[] { new[] { En_AxisNum.X, En_AxisNum.Y }, 20d })!;

    var builder = typeof(AcsControlCard).GetMethod(
        "BuildLineInterpolationBufferScript",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
        binder: null,
        types: new[] { typeof(Axis[]), typeof(double[]), typeof(double[]), typeof(SpeedSetting), typeof(LineInterpolationPulseOutputParam) },
        modifiers: null);
    AssertTrue(builder != null, "line interpolation Buffer builder should accept a SpeedSetting profile");

    var axes = new[] { (Axis)2, (Axis)0 };
    var startPoint = new[] { 0d, 0d };
    var target = new[] { 100d, 100d };
    var script = (string)builder!.Invoke(null, new object?[] { axes, startPoint, target, resolvedProfile, null })!;

    AssertMotionProfileAssignments(script, "AxX", workProfile, "ordinary line Work speed X");
    AssertMotionProfileAssignments(script, "AxY", workProfile, "ordinary line Work speed Y");
    AssertNoDerivedMotionProfile(script, "ordinary line Work speed");
}

void TestAcsLineInterpolationExposesPerMoveLciPulseOutput()
{
    var axisModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisModel.cs");
    var lineParamSource = ReadBlockStartingAt(axisModelSource, "public class LineInterPoParam");

    AssertContains(axisModelSource, "public class LineInterpolationPulseOutputParam", "line interpolation should expose a common pulse output model");
    var pulseParamSource = ReadBlockStartingAt(axisModelSource, "public class LineInterpolationPulseOutputParam");

    AssertContains(lineParamSource, "public LineInterpolationPulseOutputParam? PulseOutput", "line interpolation params should carry optional per-move pulse output settings");
    AssertContains(pulseParamSource, "public bool IsEnabled", "pulse output model should have an enable flag");
    AssertContains(pulseParamSource, "public double PulseWidth", "pulse output model should carry LCI pulse width");
    AssertContains(pulseParamSource, "public double Interval", "pulse output model should carry fixed-distance interval");
    AssertContains(pulseParamSource, "public double StartDistance", "pulse output model should carry pulse start distance");
    AssertContains(pulseParamSource, "public double EndDistance", "pulse output model should carry pulse end distance");
    AssertContains(pulseParamSource, "public bool RouteConfigOutput", "pulse output model should carry output routing intent");
    AssertContains(pulseParamSource, "public int ConfigOutputIndex", "pulse output model should carry ACS ConfigOut index");
    AssertContains(pulseParamSource, "public int ConfigOutputCode", "pulse output model should carry ACS ConfigOut code");
}

void TestAcsLineInterpolationPassesPulseOutputToBufferScripts()
{
    var lineSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var basicLineBody = ReadMethodBody(lineSource, "public override bool LineInterpoMoving(LineInterPoParam param)");
    var xsegLineBody = ReadMethodBody(lineSource, "public bool LineInterpoMovingXseg(LineInterPoParam param)");

    AssertContains(basicLineBody, "BuildLineInterpolationBufferScript(axes, startPoint, target, motionProfile, param.PulseOutput)",
        "ordinary line interpolation should pass per-move pulse output settings to the script builder");
    AssertContains(xsegLineBody, "BuildXsegLineInterpolationBufferScript(axes, startPoint, target, motionProfile, param.PulseOutput)",
        "XSEG line interpolation should pass per-move pulse output settings to the script builder");
}

void TestAcsLineInterpolationPulseOutputScriptMatchesFixedDistanceLciFlow()
{
    var axes = new[] { (Axis)2, (Axis)0 };
    var startPoint = new[] { 10d, 10d };
    var target = new[] { 100d, 100d };
    var pulseOutput = new LineInterpolationPulseOutputParam
    {
        IsEnabled = true,
        PulseWidth = 5d,
        Interval = 1d,
        StartDistance = 3d,
        EndDistance = 26d,
        RouteConfigOutput = true,
        ConfigOutputIndex = 2,
        ConfigOutputCode = 7
    };

    var script = AcsControlCard.BuildLineInterpolationBufferScript(axes, startPoint, target, 20d, pulseOutput);

    AssertContains(script, "global int RY_LCI_CHANNEL", "pulse line script should expose the LCI channel diagnostic");
    AssertContains(script, "global int RY_LCI_PULSE_COUNT", "pulse line script should expose the LCI pulse count diagnostic");
    AssertContains(script, "int AxX = 2", "pulse line script should map X axis");
    AssertContains(script, "int AxY = 0", "pulse line script should map Y axis");
    AssertContains(script, "real XStartPos, YStartPos", "pulse line script should declare start positions");
    AssertContains(script, "real XStopPos, YStopPos", "pulse line script should declare stop positions");
    AssertContains(script, "real PulseWidth", "pulse line script should declare pulse width");
    AssertContains(script, "real Interval", "pulse line script should declare interval");
    AssertContains(script, "real PulseStartPos, PulseEndPos", "pulse line script should declare pulse window");
    AssertContains(script, "int ch", "pulse line script should declare the LCI channel variable");
    AssertContains(script, "lc.SetSafetyMasks(1, 1)", "pulse line script should ignore LCI safety errors like the reference script");
    AssertContains(script, "ENABLE (AxX, AxY)", "pulse line script should enable the motion axes");
    AssertContains(script, "TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )", "pulse line script should wait for axes to be enabled");
    AssertContains(script, "VEL(AxX) = 20", "pulse line script should set the requested X-axis velocity");
    AssertContains(script, "VEL(AxY) = 20", "pulse line script should set the requested Y-axis velocity");
    AssertContains(script, "XStartPos = 10", "pulse line script should assign X start position");
    AssertContains(script, "YStartPos = 10", "pulse line script should assign Y start position");
    AssertContains(script, "XStopPos = 100", "pulse line script should assign X stop position");
    AssertContains(script, "YStopPos = 100", "pulse line script should assign Y stop position");
    AssertContains(script, "PulseWidth = 5", "pulse line script should assign pulse width");
    AssertContains(script, "Interval = 1", "pulse line script should assign interval");
    AssertContains(script, "PulseStartPos = 3", "pulse line script should assign pulse start distance");
    AssertContains(script, "PulseEndPos = 26", "pulse line script should assign pulse end distance");
    AssertContains(script, "PTP/e (AxX, AxY), XStartPos, YStartPos", "pulse line script should move to the start position");
    AssertContains(script, "PTP/e (AxX, AxY), XStopPos, YStopPos", "pulse line script should move to the stop position");
    AssertContains(script, "lc.SetMotionAxes(AxX, AxY)", "pulse line script should set LCI motion axes");
    AssertContains(script, "ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)", "pulse line script should initialize fixed-distance pulse mode");
    AssertContains(script, "lc.SetConfigOut(2, ch, 7)", "pulse line script should route LCI channel to ConfigOut when requested");
    AssertContains(script, "lc.LaserEnable()", "pulse line script should enable the laser before processing motion");
    AssertContains(script, "lc.LaserDisable()", "pulse line script should disable the laser after processing motion");
    AssertContains(script, "RY_LCI_CHANNEL = ch", "pulse line script should store the LCI channel");
    AssertContains(script, "RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)", "pulse line script should store pulse count");
    AssertContains(script, "lc.Stop(ch)", "pulse line script should stop the active LCI channel");
    AssertContainsBefore(script, "PTP/e (AxX, AxY), XStartPos, YStartPos", "lc.Init()",
        "pulse line script should move to start before initializing LCI");
    AssertContainsBefore(script, "lc.Init()", "ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)",
        "pulse line script should initialize LCI before fixed-distance pulse mode");
    AssertContainsBefore(script, "ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)", "lc.LaserEnable()",
        "pulse line script should configure pulse mode before laser enable");
    AssertContainsBefore(script, "lc.SetConfigOut(2, ch, 7)", "lc.LaserEnable()",
        "pulse line script should route ConfigOut before laser enable");
    AssertContainsBefore(script, "lc.LaserEnable()", "PTP/e (AxX, AxY), XStopPos, YStopPos",
        "pulse line script should enable laser before the stop-position motion");
    AssertContainsBefore(script, "PTP/e (AxX, AxY), XStopPos, YStopPos", "lc.LaserDisable()",
        "pulse line script should disable laser after the stop-position motion");
    AssertContainsBefore(script, "RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)", "lc.Stop(ch)",
        "pulse line script should store pulse count before stopping the LCI channel");
}

void TestAcsLineInterpolationPulseOutputDefaultsPulseWindowToMoveLength()
{
    var axes = new[] { (Axis)0, (Axis)1 };
    var startPoint = new[] { 0d, 0d };
    var target = new[] { 3d, 4d };
    var pulseOutput = new LineInterpolationPulseOutputParam
    {
        IsEnabled = true,
        PulseWidth = 0.5d,
        Interval = 1d,
        StartDistance = 0d,
        EndDistance = 0d,
        RouteConfigOutput = false
    };

    var script = AcsControlCard.BuildLineInterpolationBufferScript(axes, startPoint, target, 10d, pulseOutput);

    AssertContains(script, "PulseEndPos = 5", "line interpolation pulse output should default end distance to vector move length");
    AssertFalse(script.Contains("lc.SetConfigOut", StringComparison.Ordinal), "line interpolation pulse output should skip ConfigOut routing when disabled");
}

void TestAcsCoordinatedMotionPropagatesBufferTimeout()
{
    var axisModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisModel.cs");
    var syncSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCard.SyncCapabilities.cs");
    var lineSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\LineInterpolation.cs");
    var arcSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\CircularInterpolation.cs");

    var lineParamSource = ReadBlockStartingAt(axisModelSource, "public class LineInterPoParam");
    var arcParamSource = ReadBlockStartingAt(axisModelSource, "public class ArcInterPoParam");
    AssertContains(lineParamSource, "public int? Timeout", "line interpolation params should carry an optional Buffer wait timeout");
    AssertContains(arcParamSource, "public int? Timeout", "arc interpolation params should carry an optional Buffer wait timeout");

    var buildLineParamBody = ReadMethodBody(syncSource, "private LineInterPoParam BuildLineParam(");
    AssertContains(buildLineParamBody, "request.LineParam.Timeout ??= request.Timeout",
        "existing coordinated line params should inherit request timeout when not set");
    AssertContains(buildLineParamBody, "Timeout = request.Timeout",
        "new coordinated line params should carry request timeout");

    var moveArcBody = ReadMethodBody(syncSource, "private bool MoveCoordinatedArc(");
    AssertContains(moveArcBody, "request.ArcParam!.Timeout ??= request.Timeout",
        "coordinated arc params should inherit request timeout when not set");

    AssertContains(ReadMethodBody(lineSource, "public override bool LineInterpoMoving(LineInterPoParam param)"),
        "param.Timeout ?? InternalTimeout", "basic line Buffer run should use parameter timeout when provided");
    AssertContains(ReadMethodBody(lineSource, "public bool LineInterpoMovingXseg(LineInterPoParam param)"),
        "param.Timeout ?? InternalTimeout", "XSEG line Buffer run should use parameter timeout when provided");
    AssertContains(ReadMethodBody(arcSource, "public override bool ArcInterpoMoving(ArcInterPoParam param)"),
        "param.Timeout ?? InternalTimeout", "basic arc Buffer run should use parameter timeout when provided");
    AssertContains(ReadMethodBody(arcSource, "public bool ArcInterpoMovingXseg(ArcInterPoParam param)"),
        "param.Timeout ?? InternalTimeout", "XSEG arc Buffer run should use parameter timeout when provided");
}

void TestAcsInterpolationRejectsInvalidExplicitBufferNumber()
{
    var bufferSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\InterpolationBufferMotion.cs");
    var runBody = ReadMethodBody(bufferSource, "public bool RunInterpolationBufferScript(");
    var resolveBody = ReadMethodBody(bufferSource, "private bool TryResolveInterpolationBufferNo(");

    AssertContains(runBody, "TryResolveInterpolationBufferNo(requestedBufferNo, out var bufferNo, out message)",
        "interpolation Buffer runner should validate explicit Buffer number before preparing the Buffer");
    AssertContains(resolveBody, "requestedBufferNo.HasValue",
        "explicit Buffer validation should distinguish request values from option defaults");
    AssertContains(resolveBody, "requestedBufferNo.Value < 0 || requestedBufferNo.Value > 64",
        "explicit Buffer validation should reject values outside ACS Buffer range");
    AssertContains(resolveBody, "return false",
        "invalid explicit Buffer numbers should fail instead of being clamped silently");
    AssertContains(resolveBody, "Options.InterpolationBufferNo",
        "missing Buffer request should still use the configured default");
    AssertFalse(resolveBody.Contains("Math.Clamp(requestedBufferNo", StringComparison.Ordinal),
        "explicit Buffer numbers should not be silently clamped");
}

void TestAxisViewModelUsesCoordinatedMotionCapability()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");

    AssertFalse(source.Contains("ReeYin_V.Hardware.ControlCard.ACS", StringComparison.Ordinal),
        "AxisViewModel should not reference ACS namespace");
    AssertFalse(source.Contains("AcsControlCard", StringComparison.Ordinal),
        "AxisViewModel should not hard-code ACS control-card type");

    var moveBody = ReadSourceBetween(source, "case \"移动至指定位置\":", "case \"Z轴使能\":");
    AssertContains(moveBody, "TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray)",
        "AxisViewModel should route planar move through a helper");
    AssertContainsBefore(moveBody,
        "TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray)",
        "MoveAdditionalAxes(targetPositions)",
        "AxisViewModel should move extra axes after planar motion succeeds");

    var guard = ReadBlockStartingAt(moveBody, "if (!TryMovePlanarAxesToTarget(controlCard, targetPositions, targetPosArray))");
    AssertContains(guard, "return;", "AxisViewModel should stop when coordinated planar motion fails");

    var methodBody = ReadMethodBody(source, "private bool TryMovePlanarAxesToTarget(");
    AssertContains(methodBody, "ICoordinatedMotionCard", "AxisViewModel should use neutral coordinated motion capability");
    AssertContains(methodBody, "SupportsCoordinatedMotion", "AxisViewModel should check coordinated motion support");
    AssertContains(methodBody, "new CoordinatedMotionRequest", "AxisViewModel should build a coordinated motion request");
    AssertContains(methodBody, "MoveCoordinated", "AxisViewModel should call MoveCoordinated");
    AssertContains(methodBody, "CustomInterpolationMoving", "AxisViewModel should keep legacy fallback");
    AssertContainsBefore(methodBody, "MoveCoordinated", "CustomInterpolationMoving",
        "AxisViewModel should prefer coordinated motion before fallback");
}

void TestAcsXsegMethodsStayAcsSpecific()
{
    var interfaceSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCard.cs");
    var baseSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ControlCardBase.cs");

    AssertFalse(interfaceSource.Contains("LineInterpoMovingXseg", StringComparison.Ordinal), "IControlCard should not expose ACS-specific XSEG line interpolation");
    AssertFalse(interfaceSource.Contains("ArcInterpoMovingXseg", StringComparison.Ordinal), "IControlCard should not expose ACS-specific XSEG arc interpolation");
    AssertFalse(baseSource.Contains("LineInterpoMovingXseg", StringComparison.Ordinal), "ControlCardBase should not expose ACS-specific XSEG line interpolation");
    AssertFalse(baseSource.Contains("ArcInterpoMovingXseg", StringComparison.Ordinal), "ControlCardBase should not expose ACS-specific XSEG arc interpolation");
}


void TestControlCardSyncCapabilityInterfaces()
{
    var interfaceSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCardSyncCapabilities.cs");
    var legacyInterfaceSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Interface\IControlCard.cs");

    AssertContains(interfaceSource, "public interface ICoordinatedMotionCard", "coordinated motion capability interface should exist");
    AssertContains(interfaceSource, "public interface IBufferedMotionCard", "buffered motion capability interface should exist");
    AssertContains(interfaceSource, "public interface ISynchronizedTriggerCard", "synchronized trigger capability interface should exist");
    AssertContains(interfaceSource, "MoveCoordinated(CoordinatedMotionRequest request, out string message)", "coordinated motion should use common request model");
    AssertContains(interfaceSource, "RunSynchronizedTrigger(", "synchronized trigger should expose one common entry point");
    AssertFalse(legacyInterfaceSource.Contains("CoordinateArrayPulse", StringComparison.Ordinal), "IControlCard should not expose ACS coordinate-array pulse");
    AssertFalse(legacyInterfaceSource.Contains("Xseg", StringComparison.OrdinalIgnoreCase), "IControlCard should not expose ACS XSEG details");
}

void TestControlCardSyncRequestModels()
{
    var modelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardSyncModels.cs");

    AssertContains(modelSource, "public enum CoordinatedMotionKind", "coordinated motion kind enum should exist");
    AssertContains(modelSource, "public sealed class CoordinatedMotionRequest", "coordinated motion request should exist");
    AssertContains(modelSource, "public enum SynchronizedTriggerMode", "synchronized trigger mode enum should exist");
    AssertContains(modelSource, "public enum SynchronizedTriggerCapabilities", "trigger capability flags should exist");
    AssertContains(modelSource, "CoordinateArrayPulse", "coordinate-array pulse should be represented as a capability");
    AssertContains(modelSource, "PositionCompare", "Googol position compare should be represented as a capability");
    AssertContains(modelSource, "public List<Point> Points", "trigger requests should preserve ordered point inputs");
    AssertContains(modelSource, "public bool RouteConfigOutput", "trigger requests should carry vendor output routing intent");
    AssertContains(modelSource, "public int ConfigOutputIndex", "trigger requests should carry routed output index");
    AssertContains(modelSource, "public int ConfigOutputCode", "trigger requests should carry routed output code");
}


void TestAcsControlCardImplementsSyncCapabilityInterfaces()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCard.SyncCapabilities.cs");

    AssertContains(source, "ICoordinatedMotionCard", "ACS should implement coordinated motion capability");
    AssertContains(source, "IBufferedMotionCard", "ACS should implement buffered motion capability");
    AssertContains(source, "ISynchronizedTriggerCard", "ACS should implement synchronized trigger capability");
    AssertContains(source, "SynchronizedTriggerCapabilities.FixedDistancePulse", "ACS should advertise fixed-distance pulse");
    AssertContains(source, "SynchronizedTriggerCapabilities.CoordinateArrayPulse", "ACS should advertise coordinate-array pulse");
    AssertContains(source, "SynchronizedTriggerCapabilities.DataCollection", "ACS should advertise data collection");
}

void TestAcsSyncAdapterMapsCoordinateArrayPulse()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCard.SyncCapabilities.cs");

    AssertContains(source, "SynchronizedTriggerMode.CoordinateArrayPulse", "ACS adapter should handle coordinate-array pulse requests");
    AssertContains(source, "TryRunLciCoordinateArrayPulse", "ACS coordinate-array trigger should call the existing LCI runner");
    AssertContains(source, "request.Points.Select", "ACS coordinate-array trigger should preserve ordered request points");
    AssertContains(source, "new AcsPoint2D(point.X, point.Y)", "ACS coordinate-array trigger should map common points to ACS points");
    AssertContains(source, "RouteConfigOutput = request.RouteConfigOutput", "ACS coordinate-array trigger should preserve configured output routing");
    AssertContains(source, "ConfigOutputIndex = request.ConfigOutputIndex", "ACS coordinate-array trigger should preserve configured output index");
    AssertContains(source, "ConfigOutputCode = request.ConfigOutputCode", "ACS coordinate-array trigger should preserve configured output code");
}

void TestAcsReusesBaseControlCardHelpers()
{
    var axisSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AxisSetting.cs");
    var goHomeSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\GoHome.cs");

    AssertFalse(axisSource.Contains("private SpeedSetting? GetSpeedSetting", StringComparison.Ordinal), "ACS should reuse base speed resolution helper");
    AssertFalse(axisSource.Contains("private double GetAxisDefaultVelocity", StringComparison.Ordinal), "ACS should reuse base default velocity helper");
    AssertFalse(axisSource.Contains("private bool TryGetAxisConfig", StringComparison.Ordinal), "ACS should reuse base axis lookup helper");
    AssertContains(goHomeSource, "EnsurePositionBuffers(", "ACS should reuse base position-buffer helper");
}

void TestAcsJogWritesAndVerifiesSelectedSpeedProfile()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\JogMove.cs");

    var jogBody = ReadMethodBody(source, "public override bool JogAxis(En_AxisNum axisId, MoveDirection dir, EN_SpeedType spdType, bool isRunStop)");
    AssertContains(jogBody, "TryApplyJogSpeedProfile(axisId, spdType, out var profileMessage)",
        "ACS Jog should write and verify the selected speed profile before starting continuous motion");
    AssertContainsBefore(jogBody,
        "TryApplyJogSpeedProfile(axisId, spdType, out var profileMessage)",
        "return DoMoveContinue(axisId, dir)",
        "ACS Jog should verify the speed profile before issuing Jog motion");

    var profileBody = ReadMethodBody(source, "private bool TryApplyJogSpeedProfile(");
    AssertContains(profileBody, "ResolveSpeedSetting(axisId, speedType)",
        "ACS Jog speed profile should come from the requested SpeedDict1 row");
    AssertContains(profileBody, "_api.SetVelocity(axis, velocity)",
        "ACS Jog should write VEL for the selected speed profile");
    AssertContains(profileBody, "_api.SetAcceleration(axis, acceleration)",
        "ACS Jog should write ACC for the selected speed profile");
    AssertContains(profileBody, "_api.SetDeceleration(axis, acceleration)",
        "ACS Jog should write DEC for the selected speed profile");
    AssertContains(profileBody, "_api.GetVelocity(axis)",
        "ACS Jog should read VEL back after writing");
    AssertContains(profileBody, "_api.GetAcceleration(axis)",
        "ACS Jog should read ACC back after writing");
    AssertContains(profileBody, "_api.GetDeceleration(axis)",
        "ACS Jog should read DEC back after writing");
    AssertContains(profileBody, "ACS Jog speed profile verified",
        "ACS Jog should leave an explicit success trace after readback matches");
}


void TestGoogolControlCardImplementsSyncCapabilityInterfaces()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.SyncCapabilities.cs");

    AssertContains(source, "ICoordinatedMotionCard", "Googol should implement coordinated motion capability");
    AssertContains(source, "IBufferedMotionCard", "Googol should implement buffered motion capability");
    AssertContains(source, "ISynchronizedTriggerCard", "Googol should implement synchronized trigger capability");
    AssertContains(source, "SynchronizedTriggerCapabilities.PositionCompare", "Googol should advertise position compare");
    AssertContains(source, "SynchronizedTriggerCapabilities.BufferedIo", "Googol should advertise buffered IO");
}

void TestGoogolSyncAdapterKeepsFifoAndPositionCompareMapping()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\GoogolControlCard.SyncCapabilities.cs");
    var interpolationSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.Googol\App\InterpolationMove.cs");

    AssertContains(source, "CrdBufClear", "Googol buffered adapter should clear CRD FIFO");
    AssertContains(source, "CrdData", "Googol buffered adapter should push CRD data");
    AssertContains(source, "CrdMoveStart", "Googol buffered adapter should start CRD motion");
    AssertContains(source, "ControlPosComparison", "Googol position-compare trigger should reuse existing position compare");
    AssertContains(source, "InsertPosCompareData", "Googol position-compare trigger should insert ordered compare points");
    AssertContains(source, "request.Points", "Googol position-compare trigger should iterate ordered request points");
    AssertContains(interpolationSource, "new Barrier(coreCount)", "Googol multi-core start barrier should stay in the Googol implementation");
}

void TestMotionToolKeepsAcsDependencyOut()
{
    var csproj = ReadRepoFile(@"Tools\Hardware\HardwareTool.Motion\HardwareTool.Motion.csproj");
    var motionFiles = EnumerateRepoFiles(@"Tools\Hardware\HardwareTool.Motion", ".cs", ".xaml", ".csproj");

    AssertContains(
        csproj,
        @"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj",
        "Motion should reference the shared control-card abstraction");

    foreach (var file in motionFiles)
    {
        var source = File.ReadAllText(file);
        AssertFalse(
            source.Contains("ReeYin_V.Hardware.ControlCard.ACS", StringComparison.Ordinal),
            $"Motion should not reference ACS directly: {file}");
        AssertFalse(
            source.Contains("ReeYin_V.Hardware.ControlCard.ACS.csproj", StringComparison.Ordinal),
            $"Motion should not reference the ACS project directly: {file}");
    }
}

void TestMotionModelAxisAwareTargetMappingHelpers()
{
    var source = ReadRepoFile(@"Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs");

    AssertContains(source, "TryBuildTargetPositionMap(", "MotionModel should expose target map helper");
    var targetMapBody = ReadMethodBody(source, "TryBuildTargetPositionMap(");

    AssertContains(targetMapBody, "ControlCard.Config.AllAxis", "target map helper should use selected card axis config");
    AssertContains(targetMapBody, "AxisNo", "target map helper should use configured physical axis numbers");
    AssertContains(targetMapBody, "TargetPos", "target map helper should map target positions");
    AssertContains(source, "GetInterpolationAxes()", "MotionModel should resolve interpolation axes centrally");
    AssertContains(source, "GetPlanarInterpolationAxes()", "MotionModel should separate planar axis resolution from full interpolation axis resolution");
    var interpolationAxesBody = ReadMethodBody(source, "GetInterpolationAxes(");
    AssertTrue(
        interpolationAxesBody.Contains("En_AxisNum", StringComparison.Ordinal) ||
        interpolationAxesBody.Contains("ControlCard", StringComparison.Ordinal) ||
        interpolationAxesBody.Contains("DefaultInterpCS", StringComparison.Ordinal) ||
        interpolationAxesBody.Contains("AllAxis", StringComparison.Ordinal),
        "interpolation axis helper should use configured axis semantics");
    AssertFalse(
        interpolationAxesBody.Contains("Take(2)", StringComparison.Ordinal),
        "full interpolation axis helper should not silently truncate configured axis groups");
    AssertFalse(
        interpolationAxesBody.Contains("return [En_AxisNum.X, En_AxisNum.Y]", StringComparison.Ordinal),
        "full interpolation axis helper should not fabricate an XY plane when no legal axis group exists");
    var planarAxesBody = ReadMethodBody(source, "GetPlanarInterpolationAxes(");
    AssertContains(planarAxesBody, "GetInterpolationAxes()", "planar helper should derive its plane from the resolved interpolation axis group first");
    var targetArrayBody = ReadMethodBody(source, "BuildTargetPositionArray(");
    AssertContains(targetArrayBody, "AxisNo - 1", "target array helper should preserve physical axis slot semantics");
    AssertContains(targetArrayBody, "new double[maxAxisNo]", "target array helper should size by the maximum configured physical axis number");
    var planarTargetBody = ReadMethodBody(source, "TryApplyPlanarTargetPositions(");
    AssertContains(planarTargetBody, "targetPositions == null", "planar helper should reject null target dictionaries");
    AssertFalse(
        source.Contains("MovementLocus.AssignPosInfo.TargetPos[3]", StringComparison.Ordinal),
        "MotionModel should not hard-code Z1 target position index");
    AssertFalse(
        source.Contains("MovementLocus.AssignPosInfo.TargetPos[4]", StringComparison.Ordinal),
        "MotionModel should not hard-code Z2 target position index");
}

void TestMotionArcExecutionPassesCompleteParameters()
{
    var source = ReadRepoFile(@"Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs");
    var createArcBody = ReadMethodBody(source, "ArcInterPoParam CreateArcInterPoParam(");
    var executeArcBody = ReadMethodBody(source, "bool ExecuteArcInterpolationSequence(");

    AssertTrue(
        System.Text.RegularExpressions.Regex.IsMatch(
            createArcBody,
            @"InterPoAxiss\s*=\s*GetInterpolationAxes\s*\(",
            System.Text.RegularExpressions.RegexOptions.Singleline),
        "arc params should pass selected interpolation axes");
    var finalPositionAssignment = System.Text.RegularExpressions.Regex.Match(
        createArcBody,
        @"FinalPosDic\s*=\s*(?<value>[A-Za-z_]\w*)\b",
        System.Text.RegularExpressions.RegexOptions.Singleline);
    AssertTrue(finalPositionAssignment.Success, "arc params should pass final positions for ACS limit checks");
    AssertTrue(
        System.Text.RegularExpressions.Regex.IsMatch(
            createArcBody,
            @"Dictionary\s*<\s*En_AxisNum\s*,\s*double\s*>",
            System.Text.RegularExpressions.RegexOptions.Singleline) ||
        createArcBody.Contains("finalPositions", StringComparison.Ordinal),
        "arc params should receive final positions from an axis-aware map parameter");
    AssertFalse(
        System.Text.RegularExpressions.Regex.IsMatch(
            createArcBody,
            @"FinalPosDic\s*=\s*new\s+Dictionary",
            System.Text.RegularExpressions.RegexOptions.Singleline),
        "arc params should not create an empty final position map");
    var arcTargetMapMatch = System.Text.RegularExpressions.Regex.Match(
        executeArcBody,
        @"TryBuildPlanarTargetPositionMap\s*\([^;]*out\s+var\s+(?<map>\w+)[^;]*\)",
        System.Text.RegularExpressions.RegexOptions.Singleline);
    AssertTrue(arcTargetMapMatch.Success, "arc execution should build final target positions centrally");
    var finalPositionMapName = System.Text.RegularExpressions.Regex.Escape(arcTargetMapMatch.Groups["map"].Value);
    AssertTrue(
        System.Text.RegularExpressions.Regex.IsMatch(
            executeArcBody,
            $@"CreateArcInterPoParam\s*\([^)]*\b{finalPositionMapName}\b[^)]*\)",
            System.Text.RegularExpressions.RegexOptions.Singleline),
        "arc execution should pass the built final target positions into arc params");
    AssertContains(executeArcBody, "TryBuildPlanarTargetPositionMap", "arc execution should build final target positions centrally");
    AssertContains(executeArcBody, "CreateMoveToCircleParam", "arc move-to-circle step should share line parameter construction");
    AssertContains(executeArcBody, "TryCreateMoveToCircleParam", "arc move-to-circle should use an explicit failure-aware helper");
    AssertFalse(
        System.Text.RegularExpressions.Regex.IsMatch(
            executeArcBody,
            @"LineInterpoMoving\s*\(\s*CreateMoveToCircleParam\s*\(",
            System.Text.RegularExpressions.RegexOptions.Singleline),
        "arc execution should not inline a move-to-circle param builder that cannot signal failure");
    AssertContains(source, "bool TryCreateMoveToCircleParam(", "move-to-circle helper should expose an explicit success/failure contract");
    AssertContains(source, "out LineInterPoParam", "move-to-circle helper should return the line param via out parameter");
    var moveToCircleBody = ReadMethodBody(source, "bool TryCreateMoveToCircleParam(");
    AssertContains(moveToCircleBody, "return false;", "move-to-circle helper should fail explicitly when planar target remapping fails");
    AssertFalse(
        moveToCircleBody.Contains("Logs.LogWarning", StringComparison.Ordinal),
        "move-to-circle helper should not swallow planar remap failures by only logging");
}

void TestMotionExecutionUsesFocusedMovementDispatch()
{
    var source = ReadRepoFile(@"Tools\Hardware\HardwareTool.Motion\Models\MotionModel.cs");
    var executeBody = ReadMethodBody(source, "ExecuteModule(");
    var customMovingBody = ReadMethodBody(source, "CustomMoving(");

    AssertContains(source, "ExecuteMovementLocus(", "MotionModel should dispatch each movement through one helper");
    AssertContains(source, "ExecutePointMovement(", "MotionModel should have focused point helper");
    AssertContains(source, "ExecuteLineSegmentMovement(", "MotionModel should have focused line helper");
    AssertContains(source, "ExecuteArcSegmentMovement(", "MotionModel should have focused arc helper");
    AssertContains(executeBody, "ExecuteMovementLocus(", "module execution should use focused movement dispatch");
    AssertContains(customMovingBody, "ExecuteMovementLocus(", "manual execution should use the same movement dispatch");
    AssertFalse(executeBody.Contains("new CustomInterPoParam", StringComparison.Ordinal), "module execution should not duplicate custom interpolation construction");
    AssertFalse(customMovingBody.Contains("new CustomInterPoParam", StringComparison.Ordinal), "manual execution should not duplicate custom interpolation construction");
}

void TestAcsLciFixedDistancePulseScriptMatchesReferencePtpFlow()
{
    var param = new AcsLciFixedDistancePulseXsegParam
    {
        AxisX = 0,
        AxisY = 1,
        PulseWidth = 0.01,
        Interval = 1.0,
        StartDistance = 0,
        EndDistance = 200,
        MotionProfile = CreateLciTestMotionProfile(10),
        ConfigOutputIndex = 0,
        ConfigOutputCode = 7,
        Points = new()
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(100, 0),
            new AcsPoint2D(100, 100)
        }
    };

    var script = AcsControlCard.BuildLciFixedDistancePulseXsegScript(param);

    AssertContains(script, "global LCI lc", "LCI script should declare the LCI object");
    AssertContains(script, "global int RY_LCI_CHANNEL", "LCI script should expose the selected channel");
    AssertContains(script, "global int RY_LCI_PULSE_COUNT", "LCI script should expose the pulse count");
    AssertContains(script, "real XStartPos, YStartPos", "LCI script should declare start positions");
    AssertContains(script, "real XStopPos, YStopPos", "LCI script should declare stop positions");
    AssertContains(script, "real PulseStartPos, PulseEndPos", "LCI script should declare pulse distance window");
    AssertContains(script, "lc.SetSafetyMasks(1, 1)", "LCI script should ignore LCI safety errors and faults like the reference flow");
    AssertContains(script, "ENABLE (AxX, AxY)", "LCI script should enable both motion axes");
    AssertContains(script, "TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )", "LCI script should wait until both axes are enabled");
    AssertContains(script, "VEL(AxX) = 10", "LCI script should configure X velocity");
    AssertContains(script, "VEL(AxY) = 10", "LCI script should configure Y velocity");
    AssertContains(script, "XStartPos = 0", "LCI script should set start X from the first point");
    AssertContains(script, "YStartPos = 0", "LCI script should set start Y from the first point");
    AssertContains(script, "XStopPos = 100", "LCI script should set stop X from the last point");
    AssertContains(script, "YStopPos = 100", "LCI script should set stop Y from the last point");
    AssertContains(script, "PulseStartPos = 0", "LCI script should set pulse start distance");
    AssertContains(script, "PulseEndPos = 200", "LCI script should set pulse end distance");
    AssertContains(script, "PTP/e (AxX, AxY), XStartPos, YStartPos", "LCI script should move to the start point before initializing LCI");
    AssertContains(script, "lc.Init()", "LCI script should initialize LCI after reaching the start point");
    AssertContains(script, "lc.SetMotionAxes(AxX, AxY)", "LCI script should set two motion axes");
    AssertContains(script, "ch = lc.FixedDistPulse(PulseWidth, Interval, PulseStartPos, PulseEndPos)", "LCI script should configure fixed-distance pulse output by variables");
    AssertContains(script, "lc.SetConfigOut(0, ch, 7)", "LCI script should route the channel to configurable output");
    AssertContains(script, "PTP/e (AxX, AxY), XStopPos, YStopPos", "LCI script should move from start to stop after laser enable");
    AssertContains(script, "RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)", "LCI script should capture pulse count before stopping the channel");
    AssertContains(script, "lc.Stop(ch)", "LCI script should stop the LCI channel");
    AssertContainsBefore(script, "PTP/e (AxX, AxY), XStartPos, YStartPos", "lc.Init()", "LCI script should reach start before LCI initialization");
    AssertContainsBefore(script, "lc.FixedDistPulse", "lc.LaserEnable()", "LCI script should initialize pulse mode before enabling laser");
    AssertContainsBefore(script, "lc.LaserEnable()", "PTP/e (AxX, AxY), XStopPos, YStopPos", "LCI script should enable laser before the processing move");
    AssertContainsBefore(script, "PTP/e (AxX, AxY), XStopPos, YStopPos", "lc.LaserDisable()", "LCI script should finish the processing move before disabling laser");
    AssertContainsBefore(script, "lc.GetPulseCounts(ch)", "lc.Stop(ch)", "LCI script should read pulse count before stopping the channel");
    AssertFalse(script.Contains("XSEG", StringComparison.Ordinal), "LCI fixed-distance pulse script should not use XSEG");
    AssertFalse(script.Contains("ENDS", StringComparison.Ordinal), "LCI fixed-distance pulse script should not use ENDS");
    AssertFalse(script.Contains("GSEG", StringComparison.Ordinal), "LCI fixed-distance pulse script should not wait on GSEG");
}

void TestAcsLciFixedDistancePulseDefaultsPulseWindowToMoveLength()
{
    var param = new AcsLciFixedDistancePulseXsegParam
    {
        AxisX = 0,
        AxisY = 1,
        PulseWidth = 0.01,
        Interval = 1.0,
        MotionProfile = CreateLciTestMotionProfile(10),
        RouteConfigOutput = false,
        Points = new()
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(3, 4)
        }
    };

    var script = AcsControlCard.BuildLciFixedDistancePulseXsegScript(param);

    AssertContains(script, "PulseStartPos = 0", "default LCI pulse start distance should be zero");
    AssertContains(script, "PulseEndPos = 5", "default LCI pulse end distance should match the straight move length");
}

void TestAcsLciFixedDistancePulseRunnerUsesBufferPipeline()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs");
    var methodBody = ReadMethodBody(source, "public bool TryRunLciFixedDistancePulseXseg(");

    AssertContains(methodBody, "BuildLciFixedDistancePulseXsegScript(param)", "LCI runner should build the ACSPL+ script from the request");
    AssertContains(methodBody, "_api.LoadBuffer(buffer", "LCI runner should download script to the selected buffer");
    AssertContains(methodBody, "_api.CompileBuffer(buffer)", "LCI runner should compile the selected buffer");
    AssertContains(methodBody, "_api.RunBuffer(buffer, null)", "LCI runner should run the selected buffer");
    AssertContains(methodBody, "_api.WaitProgramEnd(buffer", "LCI runner should wait for the buffer to finish");
    AssertContains(methodBody, "_api.ReadIntegerScalar(\"RY_LCI_CHANNEL\"", "LCI runner should read the generated channel");
    AssertContains(methodBody, "_api.ReadIntegerScalar(\"RY_LCI_PULSE_COUNT\"", "LCI runner should read the pulse count");
    AssertContains(methodBody, "TryCleanupLciFixedDistancePulse", "LCI runner should attempt laser/channel cleanup on failure");
    AssertContainsBefore(methodBody, "_api.LoadBuffer(buffer", "_api.CompileBuffer(buffer)", "LCI runner should compile after loading");
    AssertContainsBefore(methodBody, "_api.CompileBuffer(buffer)", "_api.RunBuffer(buffer, null)", "LCI runner should run after compiling");
    AssertContainsBefore(methodBody, "_api.WaitProgramEnd(buffer", "_api.ReadIntegerScalar(\"RY_LCI_CHANNEL\"", "LCI runner should read globals after the buffer ends");
}

void TestAcsLciSegmentCircleScriptMatchesReferenceXsegFlow()
{
    var param = new AcsLciSegmentCircleParam
    {
        AxisX = 2,
        AxisY = 0,
        MotionProfile = CreateLciTestMotionProfile(50),
        StartX = 0,
        StartY = 0,
        CenterX = 10,
        CenterY = 5,
        Radius = 5,
        GateActiveState = 1
    };

    var script = AcsControlCard.BuildLciSegmentCircleScript(param);

    AssertContains(script, "global LCI lc", "LCI circle script should declare the LCI object");
    AssertContains(script, "global int RY_LCI_CHANNEL", "LCI circle script should expose the selected channel");
    AssertContains(script, "int AxX = 2", "LCI circle script should set X axis from parameter");
    AssertContains(script, "int AxY = 0", "LCI circle script should set Y axis from parameter");
    AssertContains(script, "real pi", "LCI circle script should declare pi");
    AssertContains(script, "pi = ACOS(-1)", "LCI circle script should define pi from ACOS(-1)");
    AssertContains(script, "Velocity = 50", "LCI circle script should set velocity from parameter");
    AssertContains(script, "XStartPos = 0", "LCI circle script should set XSEG start X");
    AssertContains(script, "YStartPos = 0", "LCI circle script should set XSEG start Y");
    AssertContains(script, "XCenterPos = 10", "LCI circle script should set center X");
    AssertContains(script, "YCenterPos = 5", "LCI circle script should set center Y");
    AssertContains(script, "Radius = 5", "LCI circle script should set radius");
    AssertContains(script, "XCircleStartPos = 10", "circle start X should be the center X");
    AssertContains(script, "YCircleStartPos = 0", "circle start Y should be center Y minus radius");
    AssertContains(script, "GateActiveState = 1", "LCI circle script should set active gate state");
    AssertContains(script, "PTP/e (AxX, AxY), XStartPos, YStartPos", "LCI circle script should move to XSEG start first");
    AssertContains(script, "ch = lc.SegmentGate()", "LCI circle script should initialize segment gate");
    AssertContains(script, "RY_LCI_CHANNEL = ch", "LCI circle script should store segment-gate channel");
    AssertContains(script, "lc.LaserEnable()", "LCI circle script should enable laser before XSEG motion");
    AssertContains(script, "XSEG(AxX, AxY), XStartPos, YStartPos", "LCI circle script should start XSEG at the start point");
    AssertContains(script, "LINE/p (AxX, AxY), XCircleStartPos, YCircleStartPos, 0", "LCI circle script should move to the circle before enabling gate");
    AssertContains(script, "ARC2/p (AxX, AxY), XCenterPos, YCenterPos, 2*pi, GateActiveState", "LCI circle script should draw a full circle by center and 2*pi");
    AssertContains(script, "ENDS(AxX, AxY)", "LCI circle script should close XSEG");
    AssertContains(script, "TILL GSEG(AxX) = -1", "LCI circle script should wait for segmented motion end");
    AssertContains(script, "lc.LaserDisable()", "LCI circle script should disable laser after motion");
    AssertContains(script, "lc.Stop()", "LCI circle script should stop the LCI mode");
    AssertContainsBefore(script, "lc.SegmentGate()", "lc.LaserEnable()", "segment gate should be initialized before laser enable");
    AssertContainsBefore(script, "lc.LaserEnable()", "XSEG(AxX, AxY), XStartPos, YStartPos", "laser should be enabled before segmented motion starts");
    AssertContainsBefore(script, "XSEG(AxX, AxY), XStartPos, YStartPos", "LINE/p (AxX, AxY), XCircleStartPos, YCircleStartPos, 0", "XSEG should start before moving to the circle");
    AssertContainsBefore(script, "LINE/p (AxX, AxY), XCircleStartPos, YCircleStartPos, 0", "ARC2/p (AxX, AxY), XCenterPos, YCenterPos, 2*pi, GateActiveState", "circle approach should precede circle drawing");
    AssertContainsBefore(script, "ARC2/p (AxX, AxY), XCenterPos, YCenterPos, 2*pi, GateActiveState", "ENDS(AxX, AxY)", "circle drawing should be inside the XSEG block");
    AssertContainsBefore(script, "ENDS(AxX, AxY)", "TILL GSEG(AxX) = -1", "script should end XSEG before waiting for GSEG");
    AssertContainsBefore(script, "TILL GSEG(AxX) = -1", "lc.LaserDisable()", "script should wait before disabling the laser");
}

void TestAcsLciSegmentCircleRunnerUsesBufferPipeline()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs");
    var methodBody = ReadMethodBody(source, "public bool TryRunLciSegmentCircle(");

    AssertContains(methodBody, "BuildLciSegmentCircleScript(param)", "LCI circle runner should build the ACSPL+ script from the request");
    AssertContains(methodBody, "_api.LoadBuffer(buffer", "LCI circle runner should download script to the selected buffer");
    AssertContains(methodBody, "_api.CompileBuffer(buffer)", "LCI circle runner should compile the selected buffer");
    AssertContains(methodBody, "_api.RunBuffer(buffer, null)", "LCI circle runner should run the selected buffer");
    AssertContains(methodBody, "_api.WaitProgramEnd(buffer", "LCI circle runner should wait for the buffer to finish");
    AssertContains(methodBody, "_api.ReadIntegerScalar(\"RY_LCI_CHANNEL\"", "LCI circle runner should read the generated channel");
    AssertContains(methodBody, "TryCleanupLciFixedDistancePulse", "LCI circle runner should attempt laser/channel cleanup on failure");
    AssertContainsBefore(methodBody, "_api.LoadBuffer(buffer", "_api.CompileBuffer(buffer)", "LCI circle runner should compile after loading");
    AssertContainsBefore(methodBody, "_api.CompileBuffer(buffer)", "_api.RunBuffer(buffer, null)", "LCI circle runner should run after compiling");
    AssertContainsBefore(methodBody, "_api.WaitProgramEnd(buffer", "_api.ReadIntegerScalar(\"RY_LCI_CHANNEL\"", "LCI circle runner should read globals after the buffer ends");
}

void TestAcsLciCoordinateArrayPulseScriptMatchesReferenceXsegFlow()
{
    var param = new AcsLciCoordinateArrayPulseParam
    {
        AxisX = 0,
        AxisY = 1,
        PulseWidth = 0.01,
        MultiAxWinSize = 0.001,
        MotionProfile = CreateLciTestMotionProfile(25),
        RouteConfigOutput = true,
        ConfigOutputIndex = 0,
        ConfigOutputCode = 7,
        Points = new()
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(10, 0),
            new AcsPoint2D(10, 20),
            new AcsPoint2D(25, 20)
        }
    };

    var script = AcsControlCard.BuildLciCoordinateArrayPulseScript(param);

    AssertContains(script, "global LCI lc", "coordinate array script should declare the LCI object");
    AssertContains(script, "global int RY_LCI_CHANNEL", "coordinate array script should expose the selected channel");
    AssertContains(script, "global int RY_LCI_PULSE_COUNT", "coordinate array script should expose the pulse count");
    AssertContains(script, "global real RY_LCI_XCOORD(4)", "coordinate array script should declare X coordinate array sized from points");
    AssertContains(script, "global real RY_LCI_YCOORD(4)", "coordinate array script should declare Y coordinate array sized from points");
    AssertContains(script, "int PointsNum", "coordinate array script should declare the point count variable");
    AssertContains(script, "int PointIndex", "coordinate array script should declare the point loop variable");
    AssertContains(script, "PulseWidth = 0.01", "coordinate array script should set pulse width from parameter");
    AssertContains(script, "PointsNum = 4", "coordinate array script should set point count from parameter");
    AssertContains(script, "lc.SetSafetyMasks(1, 1)", "coordinate array script should ignore safety/fault inputs like existing LCI scripts");
    AssertContains(script, "lc.Init()", "coordinate array script should initialize LCI");
    AssertContains(script, "ENABLE (AxX, AxY)", "coordinate array script should enable the motion axes");
    AssertContains(script, "TILL ( MST(AxX).#ENABLED & MST(AxY).#ENABLED )", "coordinate array script should wait for axes to enable");
    AssertContains(script, "VEL(AxX) = 25", "coordinate array script should set X velocity from parameter");
    AssertContains(script, "VEL(AxY) = 25", "coordinate array script should set Y velocity from parameter");
    AssertContains(script, "PTP/e (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)", "coordinate array script should move to the first point");
    AssertContains(script, "lc.SetMotionAxes(AxX, AxY)", "coordinate array script should set motion axes");
    AssertContains(script, "lc.MultiAxWinSize = 0.001", "coordinate array script should set coordinate trigger window");
    AssertContains(script, "ch = lc.CoordinateArrPulse(PointsNum, PulseWidth, RY_LCI_XCOORD, RY_LCI_YCOORD)", "coordinate array script should initialize coordinate-array pulse");
    AssertContains(script, "lc.SetConfigOut(0, ch, 7)", "coordinate array script should route pulse output");
    AssertContains(script, "lc.LaserEnable()", "coordinate array script should enable laser before XSEG motion");
    AssertContains(script, "XSEG (AxX, AxY), RY_LCI_XCOORD(0), RY_LCI_YCOORD(0)", "coordinate array script should start XSEG at the first point");
    AssertContains(script, "PointIndex = 1", "coordinate array script should start LINE loop from the second point");
    AssertContains(script, "while PointIndex < PointsNum", "coordinate array script should loop through remaining points");
    AssertContains(script, "LINE (AxX, AxY), RY_LCI_XCOORD(PointIndex), RY_LCI_YCOORD(PointIndex)", "coordinate array script should append points in array order");
    AssertContains(script, "PointIndex = PointIndex + 1", "coordinate array script should increment point index");
    AssertContains(script, "ENDS (AxX, AxY)", "coordinate array script should close XSEG");
    AssertContains(script, "till GSEG(AxX) = -1", "coordinate array script should wait for segmented motion end");
    AssertContains(script, "RY_LCI_PULSE_COUNT = lc.GetPulseCounts(ch)", "coordinate array script should read pulse count before stopping");
    AssertContains(script, "lc.Stop(ch)", "coordinate array script should stop the LCI channel");
    AssertContainsBefore(script, "lc.CoordinateArrPulse", "lc.LaserEnable()", "coordinate array mode should be initialized before laser enable");
    AssertContainsBefore(script, "lc.LaserEnable()", "XSEG (AxX, AxY)", "laser should be enabled before XSEG motion");
    AssertContainsBefore(script, "PointIndex = 1", "LINE (AxX, AxY)", "LINE loop should start after index initialization");
    AssertContainsBefore(script, "ENDS (AxX, AxY)", "till GSEG(AxX) = -1", "script should close XSEG before waiting for completion");
    AssertContainsBefore(script, "lc.GetPulseCounts(ch)", "lc.Stop(ch)", "script should read pulse count before stopping the channel");
}

void TestAcsLciCoordinateArrayPulseRunnerWritesArraysBeforeRun()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs");
    var methodBody = ReadMethodBody(source, "public bool TryRunLciCoordinateArrayPulse(");

    AssertContains(methodBody, "BuildLciCoordinateArrayPulseScript(param)", "coordinate array runner should build the ACSPL+ script from the request");
    AssertContains(methodBody, "_api.LoadBuffer(buffer", "coordinate array runner should download script to the selected buffer");
    AssertContains(methodBody, "_api.CompileBuffer(buffer)", "coordinate array runner should compile the selected buffer");
    AssertContains(methodBody, "WriteLciCoordinateArrayPulseVariables(param)", "coordinate array runner should write point arrays before running");
    AssertContains(methodBody, "_api.RunBuffer(buffer, null)", "coordinate array runner should run the selected buffer");
    AssertContains(methodBody, "_api.WaitProgramEnd(buffer", "coordinate array runner should wait for the buffer to finish");
    AssertContains(methodBody, "_api.ReadIntegerScalar(\"RY_LCI_CHANNEL\"", "coordinate array runner should read the generated channel");
    AssertContains(methodBody, "_api.ReadIntegerScalar(\"RY_LCI_PULSE_COUNT\"", "coordinate array runner should read the pulse count");
    AssertContains(methodBody, "TryCleanupLciFixedDistancePulse", "coordinate array runner should attempt laser/channel cleanup on failure");
    AssertContains(source, "_api.WriteVariable(xCoordinates", "coordinate array runner should write X coordinate array");
    AssertContains(source, "_api.WriteVariable(yCoordinates", "coordinate array runner should write Y coordinate array");
    AssertContainsBefore(methodBody, "_api.LoadBuffer(buffer", "_api.CompileBuffer(buffer)", "runner should compile after loading");
    AssertContainsBefore(methodBody, "_api.CompileBuffer(buffer)", "WriteLciCoordinateArrayPulseVariables(param)", "runner should write arrays after compile");
    AssertContainsBefore(methodBody, "WriteLciCoordinateArrayPulseVariables(param)", "_api.RunBuffer(buffer, null)", "runner should run after array writes");
    AssertContainsBefore(methodBody, "_api.WaitProgramEnd(buffer", "_api.ReadIntegerScalar(\"RY_LCI_CHANNEL\"", "runner should read globals after the buffer ends");
}

void TestAcsLciScriptsUseConfiguredMotionProfiles()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs");
    AssertFalse(source.Contains("AcsLciMotionProfile", StringComparison.Ordinal), "ACS LCI should reuse base SpeedSetting instead of a dedicated profile class");

    var fixedProfile = new SpeedSetting
    {
        MaxSpeed = 12,
        AccSpeed = 120,
        DecSpeed = 121,
        KillDecSpeed = 1220,
        Jerk = 1230
    };
    var fixedParam = new AcsLciFixedDistancePulseXsegParam
    {
        AxisX = 0,
        AxisY = 1,
        MotionProfile = fixedProfile,
        PulseWidth = 0.01,
        Interval = 1,
        Points = new()
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(10, 10)
        }
    };

    var fixedScript = AcsControlCard.BuildLciFixedDistancePulseXsegScript(fixedParam);
    AssertMotionProfileAssignments(fixedScript, "AxX", fixedProfile, "fixed pulse X");
    AssertMotionProfileAssignments(fixedScript, "AxY", fixedProfile, "fixed pulse Y");
    AssertNoDerivedMotionProfile(fixedScript, "fixed pulse");

    var circleProfile = new SpeedSetting
    {
        MaxSpeed = 55,
        AccSpeed = 550,
        DecSpeed = 560,
        KillDecSpeed = 5700,
        Jerk = 5800
    };
    var circleParam = new AcsLciSegmentCircleParam
    {
        AxisX = 2,
        AxisY = 0,
        MotionProfile = circleProfile,
        StartX = 0,
        StartY = 0,
        CenterX = 10,
        CenterY = 5,
        Radius = 5,
        GateActiveState = 1
    };

    var circleScript = AcsControlCard.BuildLciSegmentCircleScript(circleParam);
    AssertMotionProfileAssignments(circleScript, "AxX", circleProfile, "circle X");
    AssertMotionProfileAssignments(circleScript, "AxY", circleProfile, "circle Y");
    AssertNoDerivedMotionProfile(circleScript, "circle");

    var coordinateProfile = new SpeedSetting
    {
        MaxSpeed = 25,
        AccSpeed = 250,
        DecSpeed = 260,
        KillDecSpeed = 2700,
        Jerk = 2800
    };
    var coordinateParam = new AcsLciCoordinateArrayPulseParam
    {
        AxisX = 0,
        AxisY = 1,
        MotionProfile = coordinateProfile,
        PulseWidth = 0.01,
        MultiAxWinSize = 0.001,
        Points = new()
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(10, 0),
            new AcsPoint2D(10, 20)
        }
    };

    var coordinateScript = AcsControlCard.BuildLciCoordinateArrayPulseScript(coordinateParam);
    AssertMotionProfileAssignments(coordinateScript, "AxX", coordinateProfile, "coordinate array X");
    AssertMotionProfileAssignments(coordinateScript, "AxY", coordinateProfile, "coordinate array Y");
    AssertNoDerivedMotionProfile(coordinateScript, "coordinate array");
}

void TestAcsLciFallsBackToWorkSpeedWhenProfileUnset()
{
    var lciSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsLciFixedDistancePulse.cs");
    var optionsSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCardOptions.cs");
    var syncSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardSyncModels.cs");
    var fixedRunnerBody = ReadMethodBody(lciSource, "public bool TryRunLciFixedDistancePulseXseg(");
    var circleRunnerBody = ReadMethodBody(lciSource, "public bool TryRunLciSegmentCircle(");
    var coordinateRunnerBody = ReadMethodBody(lciSource, "public bool TryRunLciCoordinateArrayPulse(");

    AssertContains(lciSource, "ResolveLciWorkMotionProfile", "LCI runner should resolve missing speed values from the Work speed profile");
    AssertContains(lciSource, "EN_SpeedType.Work", "LCI Work fallback should explicitly request the Work speed type");
    AssertContains(lciSource, "ResolveSpeedSetting(axisNum.Value, EN_SpeedType.Work)", "LCI Work fallback should use the base speed-setting resolver");
    AssertContains(lciSource, "MergeConfiguredLciSpeedSetting", "LCI runner should merge explicit profile values over Work defaults");
    AssertContains(lciSource, "CreateUnset", "LCI default motion profile should represent unset values");
    AssertFalse(lciSource.Contains("MaxSpeed = 10d", StringComparison.Ordinal), "LCI fixed pulse should not hard-code a default velocity");
    AssertFalse(lciSource.Contains("MaxSpeed = 50d", StringComparison.Ordinal), "LCI circle should not hard-code a default velocity");
    AssertContainsBefore(fixedRunnerBody, "ApplyLciMotionProfileFallback(param)", "BuildLciFixedDistancePulseXsegScript(param)", "fixed pulse should apply Work fallback before script generation");
    AssertContainsBefore(circleRunnerBody, "ApplyLciMotionProfileFallback(param)", "BuildLciSegmentCircleScript(param)", "circle should apply Work fallback before script generation");
    AssertContainsBefore(coordinateRunnerBody, "ApplyLciMotionProfileFallback(param)", "BuildLciCoordinateArrayPulseScript(param)", "coordinate array should apply Work fallback before script generation");
    AssertContains(optionsSource, "private double _velocity;", "fixed pulse velocity should default to unset");
    AssertContains(optionsSource, "private double _acceleration;", "fixed pulse acceleration should default to unset");
    AssertContains(syncSource, "public double Velocity { get; set; }", "synchronized trigger velocity should default to unset");
    AssertFalse(syncSource.Contains("public double Velocity { get; set; } = 10d;", StringComparison.Ordinal), "synchronized trigger should not force a non-Work velocity default");

    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    var workProfile = new SpeedSetting
    {
        SpeedType = EN_SpeedType.Work,
        SpeedDescribe = "X work",
        StartSpeed = 3,
        MaxSpeed = 44,
        AccSpeed = 440,
        DecSpeed = 441,
        KillDecSpeed = 4420,
        Jerk = 4430
    };
    card.Config.AllAxis.Add(new SingleAxisParam
    {
        AxisNum = En_AxisNum.X,
        AxisNo = 1,
        IsUsing = true,
        SpeedDict1 = new() { workProfile }
    });
    var fixedParam = new AcsLciFixedDistancePulseXsegParam
    {
        AxisX = 0,
        AxisY = 1,
        PulseWidth = 0.01,
        Interval = 1,
        Points = new()
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(10, 10)
        }
    };

    InvokeLciMotionProfileFallback(card, fixedParam);
    var fixedScript = AcsControlCard.BuildLciFixedDistancePulseXsegScript(fixedParam);
    AssertMotionProfileAssignments(fixedScript, "AxX", workProfile, "fixed pulse unset profile Work fallback");

    var partialParam = new AcsLciFixedDistancePulseXsegParam
    {
        AxisX = 0,
        AxisY = 1,
        MotionProfile = new SpeedSetting
        {
            MaxSpeed = 99,
            AccSpeed = 990
        },
        PulseWidth = 0.01,
        Interval = 1,
        Points = new()
        {
            new AcsPoint2D(0, 0),
            new AcsPoint2D(10, 10)
        }
    };

    InvokeLciMotionProfileFallback(card, partialParam);
    var partialScript = AcsControlCard.BuildLciFixedDistancePulseXsegScript(partialParam);
    AssertContains(partialScript, "VEL(AxX) = 99", "explicit LCI velocity should override Work velocity");
    AssertContains(partialScript, "ACC(AxX) = 990", "explicit LCI acceleration should override Work acceleration");
    AssertContains(partialScript, $"DEC(AxX) = {workProfile.DecSpeed}", "unset LCI deceleration should stay on Work deceleration");
    AssertContains(partialScript, $"KDEC(AxX) = {workProfile.KillDecSpeed}", "unset LCI kill deceleration should stay on Work kill deceleration");
    AssertContains(partialScript, $"JERK(AxX) = {workProfile.Jerk}", "unset LCI jerk should stay on Work jerk");
}

void TestAcsConfigViewBindsLciMotionProfileConfig()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    var requiredBindings = new[]
    {
        "Options.LciFixedDistancePulse.Velocity",
        "Options.LciFixedDistancePulse.Acceleration",
        "Options.LciFixedDistancePulse.Deceleration",
        "Options.LciFixedDistancePulse.KillDeceleration",
        "Options.LciFixedDistancePulse.Jerk",
        "Options.LciSegmentCircle.Acceleration",
        "Options.LciSegmentCircle.Deceleration",
        "Options.LciSegmentCircle.KillDeceleration",
        "Options.LciSegmentCircle.Jerk"
    };

    foreach (var binding in requiredBindings)
    {
        AssertContains(xaml, binding, $"LCI config view should bind {binding}");
    }
}

void TestAcsMonitorViewModelStatusSurface()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardMonitorPanelViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS monitor view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    AssertTrue(ReferenceEquals(card, viewModelType!.GetProperty("Card")?.GetValue(viewModel)), "monitor view model keeps selected ACS card");
    AssertTrue(viewModelType.GetProperty("RefreshMonitorCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand, "RefreshMonitorCommand should be available");
    AssertTrue(viewModelType.GetProperty("StartAutoRefreshCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand, "StartAutoRefreshCommand should be available");
    AssertTrue(viewModelType.GetProperty("StopAutoRefreshCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand, "StopAutoRefreshCommand should be available");
    AssertTrue(viewModelType.GetProperty("AxisRows")?.GetValue(viewModel) is System.Collections.IEnumerable, "AxisRows should be available for the monitor table");
    AssertTrue(viewModelType.GetProperty("ConnectionStateText")?.GetValue(viewModel) is string, "connection state text should be available");
    AssertTrue(viewModelType.GetProperty("ControllerStateText")?.GetValue(viewModel) is string, "controller state text should be available");
    AssertTrue(viewModelType.GetProperty("LastRefreshText")?.GetValue(viewModel) is string, "last refresh text should be available");
}

void TestAcsMonitorViewBindsCoreStatusFields()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var requiredTexts = new[]
    {
        "状态监控",
        "连接状态",
        "控制器状态",
        "当前位置",
        "速度",
        "运动中",
        "刷新"
    };

    foreach (var requiredText in requiredTexts)
    {
        AssertTrue(xaml.Contains(requiredText, StringComparison.Ordinal), $"monitor view should contain readable Chinese text: {requiredText}");
    }

    var requiredBindings = new[]
    {
        "ItemsSource=\"{Binding Monitor.AxisRows}\"",
        "Binding=\"{Binding CurrentPositionText, Mode=OneWay}\"",
        "Binding=\"{Binding SpeedText, Mode=OneWay}\"",
        "Binding=\"{Binding StateText, Mode=OneWay}\"",
        "Command=\"{Binding Monitor.RefreshMonitorCommand}\""
    };

    foreach (var requiredBinding in requiredBindings)
    {
        AssertTrue(xaml.Contains(requiredBinding, StringComparison.Ordinal), $"monitor view should bind {requiredBinding}");
    }
}

void TestAcsMonitorViewReadOnlyTextBoxesBindOneWay()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertTrue(xaml.Contains("IsReadOnly=\"True\"", StringComparison.Ordinal), "monitor view should have read-only status text");
    AssertTrue(xaml.Contains("Text=\"{Binding Monitor.StatusText, Mode=OneWay", StringComparison.Ordinal), "monitor status text binding should be one-way");
}

void TestAcsConfigViewModelCommandSurface()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    AssertTrue(ReferenceEquals(card, viewModelType!.GetProperty("Card")?.GetValue(viewModel)), "config view model keeps selected ACS card");
    AssertTrue(ReferenceEquals(card.Options, viewModelType.GetProperty("Options")?.GetValue(viewModel)), "config view model exposes selected ACS options");

    var commandNames = new[]
    {
        "InitializeCommand",
        "CloseCommand",
        "RefreshStateCommand",
        "OpenBufferScriptCommand",
        "UploadBufferCommand",
        "DownloadBufferCommand",
        "CompileBufferCommand",
        "RunBufferCommand",
        "StopBufferCommand",
        "ClearBufferCommand",
        "RefreshBufferStatusCommand",
        "RefreshInputsCommand",
        "RefreshOutputsCommand",
        "SetOutputCommand",
        "ReadSelectedIoCommand",
        "EnableAxisCommand",
        "DisableAxisCommand",
        "MoveRelativeCommand",
        "MoveAbsoluteCommand",
        "JogPositiveCommand",
        "JogNegativeCommand",
        "StopAxisCommand",
        "ResetFeedbackCommand",
        "RefreshAxisStatusCommand",
        "GoHomeCommand",
        "RunLciFixedDistancePulseXsegCommand"
    };

    foreach (var commandName in commandNames)
    {
        AssertTrue(viewModelType.GetProperty(commandName)?.GetValue(viewModel) != null, $"{commandName} should be available");
    }

    AssertTrue(viewModelType.GetProperty("BufferStateText")?.GetValue(viewModel) is string, "BufferStateText should be available");
    AssertTrue(viewModelType.GetProperty("BufferCompiledText")?.GetValue(viewModel) is string, "BufferCompiledText should be available");
    AssertTrue(viewModelType.GetProperty("BufferIsRunning")?.GetValue(viewModel) is bool, "BufferIsRunning should be available");
    AssertTrue(viewModelType.GetProperty("BufferIsCompiled")?.GetValue(viewModel) is bool, "BufferIsCompiled should be available");
    AssertTrue(viewModelType.GetProperty("SelectedAxisContext")?.GetValue(viewModel) != null, "SelectedAxisContext should be available");
    AssertTrue(viewModelType.GetProperty("RunLineInterpolationCommand") == null, "config view model should not expose interpolation test commands");
    AssertTrue(viewModelType.GetProperty("RunArcInterpolationCommand") == null, "config view model should not expose interpolation test commands");
    AssertTrue(viewModelType.GetProperty("ExecuteTransactionCommand") == null, "config view model should not expose ACSPL transaction commands");
}

void TestAcsConfigViewModelHoldToMoveSurface()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    AssertTrue(viewModelType!.GetProperty("StartPositiveContinuousMoveCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand,
        "positive hold-to-move command should be available");
    AssertTrue(viewModelType.GetProperty("StartNegativeContinuousMoveCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand,
        "negative hold-to-move command should be available");
    AssertTrue(viewModelType.GetProperty("StopContinuousMoveCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand,
        "hold-to-move stop command should be available");
    AssertEqual(EN_SpeedType.Low, viewModelType.GetProperty("ContinuousMoveSpeedType")?.GetValue(viewModel),
        "hold-to-move should default to low speed for manual test safety");
    AssertTrue(viewModelType.GetProperty("SpeedTypeOptions")?.GetValue(viewModel) is System.Collections.IEnumerable,
        "speed mode options should be available for hold-to-move");
}

void TestAcsConfigViewModelLciFixedDistancePulseSurface()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    AssertTrue(viewModelType!.GetProperty("RunLciFixedDistancePulseXsegCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand,
        "RunLciFixedDistancePulseXsegCommand should be available");
    AssertTrue(viewModelType.GetProperty("RunLciSegmentCircleCommand")?.GetValue(viewModel) is System.Windows.Input.ICommand,
        "RunLciSegmentCircleCommand should be available");

    AssertEqual(10, viewModelType.GetProperty("LciFixedDistancePulseBufferNo")?.GetValue(viewModel), "default LCI buffer");
    AssertEqual(0, viewModelType.GetProperty("LciFixedDistancePulseAxisX")?.GetValue(viewModel), "default LCI X axis");
    AssertEqual(1, viewModelType.GetProperty("LciFixedDistancePulseAxisY")?.GetValue(viewModel), "default LCI Y axis");
    AssertEqual(0.01d, viewModelType.GetProperty("LciFixedDistancePulseWidth")?.GetValue(viewModel), "default LCI pulse width");
    AssertEqual(1d, viewModelType.GetProperty("LciFixedDistancePulseInterval")?.GetValue(viewModel), "default LCI interval");
    AssertEqual(0d, viewModelType.GetProperty("LciFixedDistancePulseStartDistance")?.GetValue(viewModel), "default LCI start distance");
    AssertEqual(0d, viewModelType.GetProperty("LciFixedDistancePulseEndDistance")?.GetValue(viewModel), "default LCI end distance should enable automatic move-length mode");
    AssertEqual(true, viewModelType.GetProperty("LciFixedDistancePulseRouteConfigOutput")?.GetValue(viewModel), "default LCI route output");
    AssertEqual(0, viewModelType.GetProperty("LciFixedDistancePulseConfigOutputIndex")?.GetValue(viewModel), "default LCI config output index");
    AssertEqual(7, viewModelType.GetProperty("LciFixedDistancePulseConfigOutputCode")?.GetValue(viewModel), "default LCI config output code");
    AssertEqual(60000, viewModelType.GetProperty("LciFixedDistancePulseTimeout")?.GetValue(viewModel), "default LCI timeout");

    var pointsText = viewModelType.GetProperty("LciFixedDistancePulsePointsText")?.GetValue(viewModel)?.ToString() ?? string.Empty;
    AssertContains(pointsText, "0,0", "default LCI points should include start point");
    AssertContains(pointsText, "100,100", "default LCI points should include end point");
    AssertTrue(viewModelType.GetProperty("LciFixedDistancePulseStatusText")?.GetValue(viewModel) is string,
        "LciFixedDistancePulseStatusText should be available");
    AssertTrue(viewModelType.GetProperty("LciFixedDistancePulseScript")?.GetValue(viewModel) is string,
        "LciFixedDistancePulseScript should be available");

    AssertEqual(10, viewModelType.GetProperty("LciSegmentCircleBufferNo")?.GetValue(viewModel), "default LCI circle buffer");
    AssertEqual(0, viewModelType.GetProperty("LciSegmentCircleAxisX")?.GetValue(viewModel), "default LCI circle X axis");
    AssertEqual(1, viewModelType.GetProperty("LciSegmentCircleAxisY")?.GetValue(viewModel), "default LCI circle Y axis");
    AssertEqual(0d, viewModelType.GetProperty("LciSegmentCircleVelocity")?.GetValue(viewModel), "default LCI circle velocity should be unset");
    AssertEqual(0d, viewModelType.GetProperty("LciSegmentCircleStartX")?.GetValue(viewModel), "default LCI circle start X");
    AssertEqual(0d, viewModelType.GetProperty("LciSegmentCircleStartY")?.GetValue(viewModel), "default LCI circle start Y");
    AssertEqual(10d, viewModelType.GetProperty("LciSegmentCircleCenterX")?.GetValue(viewModel), "default LCI circle center X");
    AssertEqual(5d, viewModelType.GetProperty("LciSegmentCircleCenterY")?.GetValue(viewModel), "default LCI circle center Y");
    AssertEqual(5d, viewModelType.GetProperty("LciSegmentCircleRadius")?.GetValue(viewModel), "default LCI circle radius");
    AssertEqual(1, viewModelType.GetProperty("LciSegmentCircleGateActiveState")?.GetValue(viewModel), "default LCI circle gate active state");
    AssertEqual(60000, viewModelType.GetProperty("LciSegmentCircleTimeout")?.GetValue(viewModel), "default LCI circle timeout");

    viewModelType.GetProperty("LciFixedDistancePulseBufferNo")?.SetValue(viewModel, 12);
    viewModelType.GetProperty("LciFixedDistancePulsePointsText")?.SetValue(viewModel, "1,2\r\n3,4");
    viewModelType.GetProperty("LciSegmentCircleRadius")?.SetValue(viewModel, 8d);
    viewModelType.GetProperty("LciSegmentCircleCenterX")?.SetValue(viewModel, 22d);

    AssertEqual(12, card.Options.LciFixedDistancePulse.BufferNo, "fixed pulse buffer should be persisted in card options");
    AssertEqual("1,2\r\n3,4", card.Options.LciFixedDistancePulse.PointsText, "fixed pulse points should be persisted in card options");
    AssertEqual(8d, card.Options.LciSegmentCircle.Radius, "circle radius should be persisted in card options");
    AssertEqual(22d, card.Options.LciSegmentCircle.CenterX, "circle center X should be persisted in card options");
}

void TestAcsConfigViewModelRemovesTransactionCommandSurface()
{
    var card = (AcsControlCard)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(AcsControlCard));
    card.Config = new ControlCardConfig();
    card.Options = new AcsControlCardOptions();

    var viewModelType = Type.GetType(
        "ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardConfigViewModel, ReeYin_V.Hardware.ControlCard.ACS");
    AssertTrue(viewModelType != null, "ACS config view model type should be discoverable from ACS assembly");

    var viewModel = Activator.CreateInstance(viewModelType!, card);
    AssertTrue(viewModel != null, "ACS config view model should be created");
    AssertTrue(viewModelType!.GetProperty("ExecuteTransactionCommand") == null, "ACSPL command executor should be removed from the config view model");
    AssertTrue(viewModelType.GetProperty("TransactionCommandText") == null, "ACSPL command text should be removed from the config view model");
    AssertTrue(viewModelType.GetProperty("TransactionResponse") == null, "ACSPL command response should be removed from the config view model");
}

void TestAcsConfigViewBufferDialogExposesProgramControls()
{
    var mainXaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var dialogXaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsBufferScriptView.xaml");

    AssertFalse(mainXaml.Contains("Header=\"Buffer脚本\"", StringComparison.Ordinal), "Buffer script editor should no longer be a main tab");
    AssertContains(mainXaml, "打开Buffer脚本", "main config view should expose a button to open the Buffer script dialog");
    AssertContains(mainXaml, "Command=\"{Binding OpenBufferScriptCommand}\"", "main config view should bind the Buffer dialog command");
    AssertContains(dialogXaml, "Command=\"{Binding UploadBufferCommand}\"", "Buffer dialog should bind upload command");
    AssertContains(dialogXaml, "Command=\"{Binding DownloadBufferCommand}\"", "Buffer dialog should bind download command");
    AssertContains(dialogXaml, "Command=\"{Binding CompileBufferCommand}\"", "Buffer dialog should bind compile command");
    AssertContains(dialogXaml, "Command=\"{Binding RunBufferCommand}\"", "Buffer dialog should bind run command");
    AssertContains(dialogXaml, "Command=\"{Binding StopBufferCommand}\"", "Buffer dialog should bind stop command");
    AssertContains(dialogXaml, "Command=\"{Binding ClearBufferCommand}\"", "Buffer dialog should bind clear command");
    AssertContains(dialogXaml, "Text=\"{Binding BufferStateText, Mode=OneWay}\"", "Buffer dialog should display running state");
    AssertContains(dialogXaml, "Text=\"{Binding BufferCompiledText, Mode=OneWay}\"", "Buffer dialog should display compiled state");
    AssertContains(dialogXaml, "IsChecked=\"{Binding BufferIsRunning, Mode=OneWay}\"", "Buffer dialog should expose running indicator");
    AssertContains(dialogXaml, "IsChecked=\"{Binding BufferIsCompiled, Mode=OneWay}\"", "Buffer dialog should expose compiled indicator");
}

void TestAcsConfigViewBindsHoldToMoveButtons()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertContains(xaml, "连续移动", "axis control page should describe hold-to-move behavior");
    AssertContains(xaml, "SelectedItem=\"{Binding ContinuousMoveSpeedType, Mode=TwoWay}\"", "single axis page should allow selecting continuous move speed");
    AssertContains(xaml, "Command=\"{Binding StartPositiveContinuousMoveCommand}\"", "positive hold button should start continuous move");
    AssertContains(xaml, "Command=\"{Binding StartNegativeContinuousMoveCommand}\"", "negative hold button should start continuous move");
    AssertContains(xaml, "Command=\"{Binding StopContinuousMoveCommand}\"", "hold buttons should stop continuous move");
    AssertContains(xaml, "EventName=\"PreviewMouseDown\"", "hold buttons should start on mouse down");
    AssertContains(xaml, "EventName=\"PreviewMouseUp\"", "hold buttons should stop on mouse up");
    AssertContains(xaml, "EventName=\"MouseLeave\"", "hold buttons should stop when the pointer leaves the button");
}

void TestAcsConfigViewLciFixedDistancePulseSurface()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");

    AssertContains(xaml, "Header=\"LCI\"", "config view should expose a dedicated LCI pulse tab");
    AssertContains(xaml, "Header=\"固定距离脉冲\"", "LCI fixed-distance pulse controls should be on their own sub-tab");
    AssertContains(xaml, "Header=\"圆弧分段\"", "LCI segment circle controls should be on their own sub-tab");
    AssertContains(xaml, "Options.LciFixedDistancePulse.BufferNo", "LCI buffer should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.AxisX", "LCI X axis should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.AxisY", "LCI Y axis should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.PulseWidth", "LCI pulse width should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.Interval", "LCI interval should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.StartDistance", "LCI start distance should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.EndDistance", "LCI end distance should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.RouteConfigOutput", "LCI route output should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.ConfigOutputIndex", "LCI config output index should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.ConfigOutputCode", "LCI config output code should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.Timeout", "LCI timeout should be persisted in options");
    AssertContains(xaml, "Options.LciFixedDistancePulse.PointsText", "LCI points should be persisted in options");
    AssertContains(xaml, "RunLciFixedDistancePulseXsegCommand", "LCI tab should bind run command");
    AssertContains(xaml, "LciFixedDistancePulseStatusText, Mode=OneWay", "LCI status should be displayed read-only");
    AssertContains(xaml, "LciFixedDistancePulseScript, Mode=OneWay", "LCI script should be displayed read-only");
    AssertContains(xaml, "Options.LciSegmentCircle.BufferNo", "LCI circle buffer should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.AxisX", "LCI circle X axis should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.AxisY", "LCI circle Y axis should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.Velocity", "LCI circle velocity should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.StartX", "LCI circle start X should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.StartY", "LCI circle start Y should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.CenterX", "LCI circle center X should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.CenterY", "LCI circle center Y should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.Radius", "LCI circle radius should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.GateActiveState", "LCI circle gate state should be persisted in options");
    AssertContains(xaml, "Options.LciSegmentCircle.Timeout", "LCI circle timeout should be persisted in options");
    AssertContains(xaml, "RunLciSegmentCircleCommand", "LCI tab should bind circle run command");
}

void TestAcsConfigViewFixesLciChineseLabels()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var lciStart = xaml.IndexOf("Header=\"LCI\"", StringComparison.Ordinal);

    AssertTrue(lciStart >= 0, "LCI tab should exist");
    var lciSection = xaml[lciStart..];
    AssertFalse(lciSection.Contains("??", StringComparison.Ordinal), "LCI tab should not contain placeholder question marks");
    AssertContains(lciSection, "LCI固定距离脉冲", "LCI fixed-distance title should be readable");
    AssertContains(lciSection, "Buffer编号", "LCI buffer label should be readable");
    AssertContains(lciSection, "运动轴 X / Y", "LCI axis label should be readable");
    AssertContains(lciSection, "脉冲宽度 / 间距", "LCI pulse label should be readable");
    AssertContains(lciSection, "起始距离 / 结束距离", "LCI distance label should be readable");
    AssertContains(lciSection, "运行LCI脉冲", "LCI pulse run button should be readable");
    AssertContains(lciSection, "运行圆弧分段", "LCI circle run button should be readable");
}

void TestAcsConfigViewReadOnlyTextBoxesBindOneWay()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var bufferDialogXaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsBufferScriptView.xaml");
    var readOnlyTextProperties = new[]
    {
        "InputStatusText",
        "OutputStatusText",
        "AxisStatusText",
        "LciFixedDistancePulseStatusText",
        "LciFixedDistancePulseScript",
        "OperationLog"
    };

    foreach (var propertyName in readOnlyTextProperties)
    {
        var expectedBinding = $"Text=\"{{Binding {propertyName}, Mode=OneWay";
        AssertTrue(xaml.Contains(expectedBinding, StringComparison.Ordinal), $"{propertyName} TextBox binding should be one-way");
    }

    AssertTrue(bufferDialogXaml.Contains("Text=\"{Binding BufferStatus, Mode=OneWay", StringComparison.Ordinal),
        "BufferStatus TextBox binding should be one-way in the Buffer script dialog");
}

void TestAcsTestViewFilesAndViewModelAreRemoved()
{
    AssertFalse(RepoFileExists(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardTestView.xaml"),
        "AcsControlCardTestView.xaml should be removed after merging into the config page");
    AssertFalse(RepoFileExists(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardTestView.xaml.cs"),
        "AcsControlCardTestView.xaml.cs should be removed after merging into the config page");
    AssertFalse(RepoFileExists(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ViewModels\AcsControlCardTestViewModel.cs"),
        "AcsControlCardTestViewModel.cs should be removed after merging into the config page");

    AssertTrue(Type.GetType("ReeYin_V.Hardware.ControlCard.ACS.ViewModels.AcsControlCardTestViewModel, ReeYin_V.Hardware.ControlCard.ACS") == null,
        "AcsControlCardTestViewModel type should no longer be exported by the ACS assembly");

    var configViewModelSource = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ViewModels\AcsControlCardConfigViewModel.cs");
    AssertFalse(configViewModelSource.Contains("AcsControlCardTestViewModel", StringComparison.Ordinal),
        "config view model should not inherit from the removed test view model");
}

void TestAcsSourceFilesUseReadableChineseText()
{
    var files = EnumerateRepoFiles(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS", ".cs", ".xaml")
        .Concat(EnumerateRepoFiles(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests", ".cs"));
    var offenders = new List<string>();
    var xmlEntityPrefix = "&" + "#x";
    var unicodeEscapePattern = @"\\" + "u[0-9A-Fa-f]{4}";

    foreach (var file in files)
    {
        var text = File.ReadAllText(file);
        if (text.Contains(xmlEntityPrefix, StringComparison.Ordinal) || System.Text.RegularExpressions.Regex.IsMatch(text, unicodeEscapePattern))
        {
            offenders.Add(file);
        }
    }

    AssertEqual(0, offenders.Count, $"source files should not contain escaped Chinese text: {string.Join(", ", offenders)}");
}

void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
        Environment.Exit(1);
    }
}

void AssertPoint(AcsPoint2D actual, double expectedX, double expectedY, string label, double tolerance = 0.000001)
{
    AssertClose(expectedX, actual.X, label + " X", tolerance);
    AssertClose(expectedY, actual.Y, label + " Y", tolerance);
}

void AssertClose(double expected, double actual, string label, double tolerance = 0.000001)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }
}

void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }
}

void AssertTrue(bool value, string label)
{
    if (!value)
    {
        throw new InvalidOperationException(label);
    }
}

void AssertFalse(bool value, string label)
{
    if (value)
    {
        throw new InvalidOperationException(label);
    }
}

void AssertContains(string source, string expected, string label)
{
    AssertTrue(source.Contains(expected, StringComparison.Ordinal), label);
}

void AssertContainsBefore(string source, string first, string second, string label)
{
    var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
    var secondIndex = source.IndexOf(second, StringComparison.Ordinal);

    AssertTrue(firstIndex >= 0, $"{label}: missing '{first}'");
    AssertTrue(secondIndex >= 0, $"{label}: missing '{second}'");
    AssertTrue(firstIndex < secondIndex, label);
}

void AssertMotionProfileAssignments(string script, string axisName, SpeedSetting profile, string label)
{
    AssertContains(script, $"VEL({axisName}) = {profile.MaxSpeed}", $"{label} should set configured velocity");
    AssertContains(script, $"ACC({axisName}) = {profile.AccSpeed}", $"{label} should set configured acceleration");
    AssertContains(script, $"DEC({axisName}) = {profile.DecSpeed}", $"{label} should set configured deceleration");
    AssertContains(script, $"KDEC({axisName}) = {profile.KillDecSpeed}", $"{label} should set configured kill deceleration");
    AssertContains(script, $"JERK({axisName}) = {profile.Jerk}", $"{label} should set configured jerk");
}

void AssertNoDerivedMotionProfile(string script, string label)
{
    AssertFalse(script.Contains("10 * VEL(", StringComparison.Ordinal), $"{label} should not derive acceleration or deceleration from velocity");
    AssertFalse(script.Contains("10 * ACC(", StringComparison.Ordinal), $"{label} should not derive jerk or kill deceleration from acceleration");
}

SpeedSetting CreateLciTestMotionProfile(double velocity)
{
    return new SpeedSetting
    {
        MaxSpeed = velocity,
        AccSpeed = velocity * 10d,
        DecSpeed = velocity * 10d,
        KillDecSpeed = velocity * 100d,
        Jerk = velocity * 100d
    };
}

void InvokeLciMotionProfileFallback(AcsControlCard card, AcsLciFixedDistancePulseXsegParam param)
{
    var method = typeof(AcsControlCard).GetMethod(
        "ApplyLciMotionProfileFallback",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
        binder: null,
        types: new[] { typeof(AcsLciFixedDistancePulseXsegParam) },
        modifiers: null);

    AssertTrue(method != null, "fixed pulse fallback method should be available");
    method!.Invoke(card, new object[] { param });
}

T GetReflectedProperty<T>(object instance, string propertyName)
{
    var value = instance.GetType().GetProperty(propertyName)?.GetValue(instance);
    AssertTrue(value is T, $"{instance.GetType().Name}.{propertyName} should be {typeof(T).Name}");
    return (T)value!;
}

int CountOccurrences(string source, string value)
{
    var count = 0;
    var index = 0;
    while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
    {
        count++;
        index += value.Length;
    }

    return count;
}

string ReadRepoFile(string relativePath)
{
    for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory != null; directory = directory.Parent)
    {
        var candidate = Path.Combine(directory.FullName, relativePath);
        if (File.Exists(candidate))
        {
            return File.ReadAllText(candidate);
        }
    }

    throw new FileNotFoundException($"Could not find repository file: {relativePath}");
}

bool RepoFileExists(string relativePath)
{
    for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory != null; directory = directory.Parent)
    {
        var candidate = Path.Combine(directory.FullName, relativePath);
        if (File.Exists(candidate))
        {
            return true;
        }
    }

    return false;
}

string ReadAcsRelativeMoveMethodBody()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\JogMove.cs");
    var methodStart = source.IndexOf("public override bool Move(En_AxisNum axisType, MoveDirection moveDirection, double um, out string message)", StringComparison.Ordinal);
    AssertTrue(methodStart >= 0, "ACS relative Move override should exist");

    var nextMethodStart = source.IndexOf("protected override bool DoMoveAxis", methodStart, StringComparison.Ordinal);
    AssertTrue(nextMethodStart > methodStart, "ACS relative Move method body should be followed by DoMoveAxis");

    return source[methodStart..nextMethodStart];
}

string ReadMethodBody(string source, string methodSignature)
{
    var methodStart = source.IndexOf(methodSignature, StringComparison.Ordinal);
    AssertTrue(methodStart >= 0, $"{methodSignature} should exist");

    var openBrace = source.IndexOf('{', methodStart);
    AssertTrue(openBrace > methodStart, $"{methodSignature} should have an opening brace");

    var depth = 0;
    for (var index = openBrace; index < source.Length; index++)
    {
        if (source[index] == '{')
        {
            depth++;
        }
        else if (source[index] == '}')
        {
            depth--;
            if (depth == 0)
            {
                return source[methodStart..(index + 1)];
            }
        }
    }

    throw new InvalidOperationException($"Could not find end of method: {methodSignature}");
}

string ReadSourceBetween(string source, string startMarker, string endMarker)
{
    var start = source.IndexOf(startMarker, StringComparison.Ordinal);
    AssertTrue(start >= 0, $"{startMarker} should exist");

    var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
    AssertTrue(end > start, $"{endMarker} should exist after {startMarker}");

    return source[start..end];
}

string ReadBlockStartingAt(string source, string startMarker)
{
    var start = source.IndexOf(startMarker, StringComparison.Ordinal);
    AssertTrue(start >= 0, $"{startMarker} should exist");

    var openBrace = source.IndexOf('{', start);
    AssertTrue(openBrace > start, $"{startMarker} should have an opening brace");

    var depth = 0;
    for (var index = openBrace; index < source.Length; index++)
    {
        if (source[index] == '{')
        {
            depth++;
        }
        else if (source[index] == '}')
        {
            depth--;
            if (depth == 0)
            {
                return source[start..(index + 1)];
            }
        }
    }

    throw new InvalidOperationException($"Could not find end of block: {startMarker}");
}

void AssertBlockEndsBefore(string source, string blockStartMarker, string laterMarker, string label)
{
    var block = ReadBlockStartingAt(source, blockStartMarker);
    var blockStart = source.IndexOf(blockStartMarker, StringComparison.Ordinal);
    var blockEnd = blockStart + block.Length;
    var later = source.IndexOf(laterMarker, blockEnd, StringComparison.Ordinal);
    AssertTrue(later >= blockEnd, label);
}

IEnumerable<string> EnumerateRepoFiles(string relativePath, params string[] extensions)
{
    for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory != null; directory = directory.Parent)
    {
        var candidate = Path.Combine(directory.FullName, relativePath);
        if (Directory.Exists(candidate))
        {
            return Directory.EnumerateFiles(candidate, "*.*", SearchOption.AllDirectories)
                .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .Where(file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) || part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }
    }

    throw new DirectoryNotFoundException($"Could not find repository directory: {relativePath}");
}

sealed class TestControlCard : ControlCardBase
{
    public List<double> RelativeMoveDistances { get; } = new();
    public List<(En_AxisNum Axis, MoveDirection Direction, EN_SpeedType SpeedType, bool IsRunStop)> JogCommands { get; } = new();
    public Queue<bool> AxisStoppedResponses { get; } = new();
    public int ConfigChangedCount { get; private set; }
    public int MotionTimeoutOverrideMs { get; set; } = 100;
    public bool AxisEnabled { get; set; } = true;
    public bool AxisStopped { get; set; } = true;
    public bool HomeResult { get; set; } = true;
    public string HomeMessage { get; set; } = "home ok";

    protected override int MotionTimeoutMs => MotionTimeoutOverrideMs;

    public void EnsureSpeedBuffersForTest(int requiredLength = 0)
    {
        EnsureSpeedBuffers(requiredLength);
    }

    protected override void OnConfigChanged()
    {
        ConfigChangedCount++;
    }

    protected override bool DoInit()
    {
        IsConnected = true;
        return true;
    }

    protected override void DoConfigure()
    {
    }

    protected override void DoStop(En_AxisNum? axisType, AxisStopMode stopMode)
    {
    }

    protected override void DoClose()
    {
        IsConnected = false;
    }

    protected override bool DoGetAxisEnable(En_AxisNum axisType)
    {
        return AxisEnabled;
    }

    protected override bool DoSetAxisEnable(En_AxisNum axisType, bool v)
    {
        AxisEnabled = v;
        return true;
    }

    protected override bool DoGetAxisStopped(En_AxisNum axisType)
    {
        return AxisStoppedResponses.Count > 0 ? AxisStoppedResponses.Dequeue() : AxisStopped;
    }

    protected override bool DoMoveAxis(En_AxisNum axisType, double um)
    {
        RelativeMoveDistances.Add(um);
        return true;
    }

    protected override bool DoMoveContinue(En_AxisNum axisType, MoveDirection moveDirection)
    {
        return true;
    }

    public override bool JogAxis(En_AxisNum axisId, MoveDirection dir, EN_SpeedType spdType, bool isRunStop)
    {
        JogCommands.Add((axisId, dir, spdType, isRunStop));
        return true;
    }

    protected override bool DoGoHome(out string message)
    {
        message = HomeMessage;
        return HomeResult;
    }
}

