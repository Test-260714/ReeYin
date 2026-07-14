using Custom.WaferFlatnessMeasure;
using Custom.WaferFlatnessMeasure.Models;
using Custom.WaferFlatnessMeasure.ViewModels;
using ReeYin_V.Core.MovingRelated;
using ReeYin_V.Hardware.ControlCard.ACS.App;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.UI.UserControls.TrajectoryDesigner.Models;
using ReeYin_V.UI.UserControls.TrajectoryDesigner.Services;
using System.IO;
using System.Reflection;
using System.Windows.Threading;

Run("median filter replaces spikes and updates core original values", TestMedianFilterReplacesSpike);
Run("smooth filter averages edge windows and keeps positions", TestSmoothFilterKeepsPositions);
Run("filtered CSV storage and trajectory soft-limit warning are wired", TestFilteredCsvStorageAndTrajectorySoftLimitWarning);
Run("sensor trajectory package snapshots collected parameters", TestSensorTrajectoryPackageSnapshot);
Run("collection view exposes filter controls", TestCollectionViewExposesFilterControls);
Run("point spectrum collection view exposes PreDatas CSV directory selector", TestPointSpectrumCollectionViewExposesPreDatasCsvDirectorySelector);
Run("point spectrum storage paths are not fixed to a user desktop directory", TestPointSpectrumStoragePathsAreNotFixedToUserDesktopDirectory);
Run("point temperature csv writes eight temperature columns per point", TestPointTemperatureCsvWritesEightTemperatureColumnsPerPoint);
Run("point temperature records read every address for every point", TestPointTemperatureRecordsReadEveryAddressForEveryPoint);
Run("point temperature csv uses arrival captured records", TestPointTemperatureCsvUsesArrivalCapturedRecords);
Run("gripper clamp defaults use jog and pressure addresses", TestGripperClampDefaultsUseJogAndPressureAddresses);
Run("gripper clamp view exposes jog controls and pressure address config", TestGripperClampViewExposesJogControlsAndPressureAddressConfig);
Run("sensor data collection view exposes point temperature address editor", TestSensorDataCollectionViewExposesPointTemperatureAddressEditor);
Run("point temperature live capture uses configurable addresses", TestPointTemperatureLiveCaptureUsesConfigurableAddresses);
Run("line spectrum converter scales height and skips invalid rows", TestLineSpectrumConverterScalesHeightAndSkipsInvalidRows);
Run("line spectrum tiff export plan stores one image per stop batch", TestLineSpectrumTiffExportPlanStoresOneImagePerStopBatch);
Run("line spectrum collection view exposes tiff output path selector", TestLineSpectrumCollectionViewExposesTiffOutputPathSelector);
Run("ACS LCI fixed-distance pulse script uses configured pulse width and interval by default", TestAcsLciFixedDistancePulseScriptUsesConfiguredPulseValuesByDefault);
Run("sampling spacing uses control-card pulse settings", TestSamplingSpacingUsesControlCardPulseSettings);
Run("line segment summary analysis selects only enabled algorithms", TestLineSegmentSummaryAnalysisSelectsOnlyEnabledAlgorithms);
Run("line segment summary analysis uses fixed upper and lower surface sources", TestLineSegmentSummaryAnalysisUsesFixedUpperAndLowerSurfaceSources);
Run("line segment summary csv triggers upper and lower surface analysis", TestLineSegmentSummaryCsvTriggersUpperAndLowerSurfaceAnalysis);
Run("line segment filtered csv uses Filtered suffix", TestLineSegmentFilteredCsvUsesFilteredSuffix);
Run("line segment model stores raw and filtered summaries", TestLineSegmentModelStoresRawAndFilteredSummaries);
Run("line segment raw summary csv retains analysis results", TestLineSegmentRawSummaryCsvRetainsAnalysisResults);
Run("trajectory designer switches back to select after adding one shape", TestTrajectoryDesignerSwitchesBackToSelectAfterAddingOneShape);
Run("trajectory designer box-selects shapes and circles rotate by handle", TestTrajectoryDesignerBoxSelectsShapesAndCirclesRotateByHandle);
Run("trajectory designer constrains shape centers to XY coordinate bounds", TestTrajectoryDesignerConstrainsShapeCentersToCoordinateBounds);
Run("trajectory designer uses the full canvas as the XY plane and resizes circles with a handle", TestTrajectoryDesignerUsesFullCanvasCoordinatePlaneAndCircleRadiusHandle);
Run("trajectory designer draws millimeter ticks and keeps circles visually round with thinner strokes", TestTrajectoryDesignerDrawsMillimeterTicksAndKeepsCirclesRoundWithThinnerStrokes);
Run("trajectory designer keeps X and Y coordinate axes at one uniform scale", TestTrajectoryDesignerKeepsCoordinateAxesUniformScale);
Run("trajectory designer keeps plot area and coordinate system aligned", TestTrajectoryDesignerKeepsPlotAreaAndCoordinateSystemAligned);
Run("trajectory coordinate mapper fits uniformly and clamps letterbox points", TestTrajectoryCoordinateMapperFitsUniformlyAndClampsLetterboxPoints);
Run("trajectory designer supports 20mm ticks point shapes and drag resize handles", TestTrajectoryDesignerSupports20MillimeterTicksPointShapesAndDragResizeHandles);
Run("trajectory designer labels coordinates and dimensions on related segments", TestTrajectoryDesignerLabelsCoordinatesAndDimensionsOnRelatedSegments);
Run("trajectory coordinate settings scroll and rectangles rotate by drag handle", TestTrajectoryCoordinateSettingsScrollAndRectanglesRotateByDragHandle);
Run("trajectory designer creates and edits line segments", TestTrajectoryDesignerCreatesAndEditsLineSegments);
Run("trajectory designer line preview anchors clicked start point", TestTrajectoryDesignerLinePreviewAnchorsClickedStartPoint);
Run("trajectory designer zooms and resets the coordinate viewport", TestTrajectoryDesignerZoomsAndResetsCoordinateViewport);
Run("trajectory designer pans the coordinate viewport by left dragging blank space", TestTrajectoryDesignerPansCoordinateViewportByLeftDraggingBlankSpace);
Run("trajectory designer toolbar operations use icons instead of text", TestTrajectoryDesignerToolbarOperationsUseIconsInsteadOfText);
Run("trajectory designer keeps shape table fixed at the bottom and exposes enabled column", TestTrajectoryDesignerKeepsShapeTableFixedAtBottomAndExposesEnabledColumn);
Run("trajectory designer right-drags viewport and floats single-shape parameters", TestTrajectoryDesignerRightDragsViewportAndFloatsSingleShapeParameters);
Run("trajectory designer guards null selected shape collection", TestTrajectoryDesignerGuardsNullSelectedShapeCollection);
Run("designed trajectory treats circle and rectangle outlines as areas only", TestDesignedTrajectoryTreatsCircleAndRectangleOutlinesAsAreasOnly);
Run("designed trajectory builder expands a polyline into connected line steps", TestDesignedTrajectoryBuilderExpandsPolylineIntoConnectedLineSteps);
Run("trajectory geometry service generates shared region points and lines", TestTrajectoryGeometryServiceGeneratesSharedRegionPointsAndLines);
Run("designed trajectory builder reuses shared geometry service", TestDesignedTrajectoryBuilderReusesSharedGeometryService);
Run("trajectory designer is owned by shared UI and consumed by wafer module", TestTrajectoryDesignerIsOwnedBySharedUiAndConsumedByWaferModule);
Run("designed trajectory result model and builder expose executable structure", TestDesignedTrajectoryResultModelAndBuilderExposeExecutableStructure);
Run("sensor motion model consumes designed trajectory plan", TestSensorMotionModelConsumesDesignedTrajectoryPlan);
Run("wafer flatness config opens designer editor instead of old generator", TestWaferFlatnessConfigOpensDesignerEditorInsteadOfOldGenerator);
Run("wafer flatness config uses read only Skia designer preview", TestWaferFlatnessConfigUsesReadOnlySkiaDesignerPreview);
Run("wafer flatness config marshals background trajectory refresh to the UI thread", TestWaferFlatnessConfigMarshalsBackgroundTrajectoryRefreshToUiThread);

Console.WriteLine("Wafer flatness data analysis tests passed.");

void TestMedianFilterReplacesSpike()
{
    var data = new List<PreprocessDatasetModel>
    {
        CreatePreData(0, 0, up: 1, down: 10, thickness: 100, extra: 5),
        CreatePreData(1, 0, up: 100, down: 1000, thickness: 10000, extra: 50),
        CreatePreData(2, 0, up: 2, down: 20, thickness: 200, extra: 7)
    };

    var result = PreprocessDatasetFilter.Apply(data, new MeasurementDataFilterOptions
    {
        MedianFilterEnabled = true,
        MedianFilterWindowSize = 3,
        FilterOriginalDataValues = true
    });

    AssertEqual(3, result.Count, "filtered count");
    AssertClose(2, result[1].UpSurface, "median up surface");
    AssertClose(20, result[1].DownSurface, "median down surface");
    AssertClose(200, result[1].Thickness, "median thickness");
    AssertClose(2, result[1].OriginalDataValues[PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName], "core up original value");
    AssertClose(20, result[1].OriginalDataValues[PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName], "core down original value");
    AssertClose(7, result[1].OriginalDataValues["Channel6.THICKNESS"], "extra original value");
    AssertClose(100, data[1].UpSurface, "source is not mutated");
}

void TestSmoothFilterKeepsPositions()
{
    var data = new List<PreprocessDatasetModel>
    {
        CreatePreData(10, 20, up: 1, down: 10, thickness: 100, extra: 5),
        CreatePreData(11, 21, up: 3, down: 30, thickness: 300, extra: 7),
        CreatePreData(12, 22, up: 5, down: 50, thickness: 500, extra: 9)
    };

    var result = PreprocessDatasetFilter.Apply(data, new MeasurementDataFilterOptions
    {
        SmoothFilterEnabled = true,
        SmoothFilterWindowSize = 3,
        FilterOriginalDataValues = false
    });

    AssertClose(10, result[0].PosX, "position X is unchanged");
    AssertClose(20, result[0].PosY, "position Y is unchanged");
    AssertClose(2, result[0].UpSurface, "edge average up surface");
    AssertClose(3, result[1].UpSurface, "center average up surface");
    AssertClose(20, result[0].DownSurface, "edge average down surface");
    AssertClose(300, result[1].Thickness, "center average thickness");
    AssertClose(7, result[1].OriginalDataValues["Channel6.THICKNESS"], "extra original value remains raw");
    AssertClose(3, result[1].OriginalDataValues[PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName], "core up original value tracks filtered surface");
}

void TestFilteredCsvStorageAndTrajectorySoftLimitWarning()
{
    var storage = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Storage\SensorDataStorageService.cs");
    var collectionModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Models\SensorDataCollectionModel.cs");
    var collectionView = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Views\SensorDataCollectionView.xaml");
    var motionHelper = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Services\WaferTrajectoryMotionHelper.cs");
    var motionModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Models\SensorMotionControlModel.cs");

    AssertContains(storage, "SaveFilteredPreDatasToCsvIfNeeded", "storage should export filtered point data separately");
    AssertContains(storage, "SaveFilteredLineSegmentPreDatasCsv", "storage should export filtered line-segment data separately");
    AssertContains(storage, "GetLineSegmentFilteredPreDatasCsvPath", "storage should name filtered line-segment data with a suffix");
    AssertDoesNotContain(storage, "ResolveFilteredLineSegmentSessionDirectory", "filtered line-segment data should stay in the original session directory");
    AssertDoesNotContain(storage, "ResolveFilteredPreDatasCsvDirectory", "filtered CSV data should stay in the configured directory");
    AssertContains(collectionModel, "_unfilteredPreDatas", "collection model should retain unfiltered data for the primary CSV");
    AssertContains(collectionModel, "LastFilteredPreDatasCsvPath", "collection model should expose the latest filtered CSV path");
    AssertContains(collectionModel, "IsPreprocessFilterEnabled", "collection model should only export a filtered CSV when preprocessing is enabled");
    AssertContains(collectionView, "最近滤波 CSV", "collection view should expose the latest filtered CSV path");

    AssertContains(motionHelper, "TryGetTrajectoryTargetSoftLimitViolation", "motion helper should report the first trajectory soft-limit violation");
    AssertContains(motionHelper, "double zHeight", "trajectory target builder should validate the configured Z height");
    AssertContains(motionHelper, "double z1Height", "trajectory target builder should validate the configured Z1 height");
    AssertContains(motionModel, "HasTrajectoryTargetOutOfSoftLimitWithWarning", "motion model should cancel motion before issuing an out-of-limit trajectory");
    AssertContains(motionModel, "BuildLineTrajectoryTargets(orderedSegments, ZHight, Z1Hight)", "line validation should use the actual configured heights");
    AssertContains(motionModel, "BuildPointTrajectoryTargets(orderedLocusInfos, ZHight, Z1Hight)", "point validation should use the actual configured heights");
    AssertContains(motionModel, "ShowTrajectorySoftLimitWarning", "motion model should show an operator warning for invalid trajectories");
    AssertContains(motionModel, "轨迹超限", "soft-limit warning should use a clear Chinese title");
}

void TestSensorTrajectoryPackageSnapshot()
{
    var preDatas = new List<PreprocessDatasetModel>
    {
        CreatePreData(1, 2, up: 3, down: 4, thickness: 5, extra: 6)
    };
    var steps = new List<PointCollectionStepInfo>
    {
        new PointCollectionStepInfo { X = 1, Y = 2, IsCalibrationReference = false, NormalPointIndex = 0 }
    };
    var starts = new List<LineSegmentStartPositionInfo>
    {
        new LineSegmentStartPositionInfo { SegmentIndex = 2, StartX = 10, StartY = 20 }
    };

    var package = SensorTrajectoryDataPackage.Create(
        isPoint: true,
        preDatas: preDatas,
        pointCollectionSteps: steps,
        lineSegmentStartPositions: starts,
        lineSegmentCsvSessionDirectory: @"D:\session",
        currentLineSegmentIndex: 2,
        expectedLineSegmentCount: 3,
        sourceCsvPath: @"D:\raw.csv",
        lastPreDatasCsvPath: @"D:\pre.csv",
        lastCalibrationWaferDataCsvPath: @"D:\calib.csv",
        sensorModelName: "SensorA",
        upSurfaceOriginalDataValueName: PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName,
        downSurfaceOriginalDataValueName: PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName,
        filterOptions: new MeasurementDataFilterOptions { MedianFilterEnabled = true, MedianFilterWindowSize = 5 });

    preDatas[0].PosX = 99;
    steps[0].X = 99;
    starts[0].StartX = 99;

    AssertEqual(1, package.PreDataCount, "package pre data count");
    AssertEqual(1, package.CollectPoints.Count, "collect point count");
    AssertClose(1, package.PreDatas[0].PosX, "pre data is cloned");
    AssertClose(1, package.PointCollectionSteps[0].X, "point steps are cloned");
    AssertClose(10, package.LineSegmentStartPositions[0].StartX, "line starts are cloned");
    AssertEqual(2, package.CurrentLineSegmentIndex, "line segment index");
    AssertEqual(3, package.ExpectedLineSegmentCount, "expected line count");
    AssertTrue(package.FilterOptions.MedianFilterEnabled, "filter options are included");
}

void TestCollectionViewExposesFilterControls()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Views\SensorDataCollectionView.xaml");

    AssertContains(xaml, "ModelParam.IsMedianFilterEnabled", "view should bind median filter enabled");
    AssertContains(xaml, "ModelParam.MedianFilterWindowSize", "view should bind median filter window");
    AssertContains(xaml, "ModelParam.IsSmoothFilterEnabled", "view should bind smooth filter enabled");
    AssertContains(xaml, "ModelParam.SmoothFilterWindowSize", "view should bind smooth filter window");
    AssertContains(xaml, "ModelParam.IsFilterOriginalDataValues", "view should bind original data filter option");
}

void TestPointSpectrumCollectionViewExposesPreDatasCsvDirectorySelector()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Views\SensorDataCollectionView.xaml");
    var viewModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\ViewModels\SensorDataCollectionViewModel.cs");

    AssertContains(xaml, "ModelParam.PreDatasCsvDirectory", "view should bind PreDatas CSV directory");
    AssertContains(xaml, "SelectPreDatasCsvDirectory", "view should expose PreDatas CSV directory selector");
    AssertContains(viewModel, "SelectPreDatasCsvDirectory", "view model should expose PreDatas folder picker command");
}

void TestPointSpectrumStoragePathsAreNotFixedToUserDesktopDirectory()
{
    var model = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Models\SensorDataCollectionModel.cs");
    var service = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Storage\SensorDataStorageService.cs");

    AssertDoesNotContain(model, @"C:\Users\REECHI-JHXM\Desktop\ReceiveDatas", "model should not default to a fixed desktop directory");
    AssertDoesNotContain(service, @"C:\Users\REECHI-JHXM\Desktop\ReceiveDatas", "storage service should not default to a fixed desktop directory");
    AssertDoesNotContain(service, "DefaultPreDatasCsvDirectory", "storage service should not keep a fixed fallback directory");
    AssertDoesNotContain(model, "+ \"\\\\test.csv\"", "raw CSV export should not build a fixed test.csv path by string concatenation");
}

void TestPointTemperatureCsvWritesEightTemperatureColumnsPerPoint()
{
    var points = new List<PreprocessDatasetModel>
    {
        CreatePreData(10, 20, up: 1, down: 2, thickness: 3, extra: 4),
        CreatePreData(11, 21, up: 1, down: 2, thickness: 3, extra: 4)
    };
    var temperatures = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        ["1:D160"] = 31.25,
        ["1:D162"] = 32.5,
        ["1:D164"] = 33.75,
        ["1:D166"] = 34.25,
        ["1:D170"] = 35.5,
        ["1:D172"] = 36.75,
        ["1:D174"] = 37.25,
        ["1:D176"] = 38.5,
        ["2:D160"] = 41.25,
        ["2:D162"] = 42.5,
        ["2:D164"] = 43.75,
        ["2:D166"] = 44.25,
        ["2:D170"] = 45.5,
        ["2:D172"] = 46.75,
        ["2:D174"] = 47.25,
        ["2:D176"] = 48.5
    };

    var records = PointTemperatureStorageService.BuildRecords(
        points,
        (pointIndex, address) => temperatures.TryGetValue($"{pointIndex}:{address}", out double value) ? value : null);

    AssertEqual(2, records.Count, "temperature record count");
    AssertEqual(8, records[0].Temperatures.Count, "first point temperature count");
    AssertClose(31.25, records[0].Temperatures["D160"]!.Value, "first point D160 temperature");
    AssertClose(38.5, records[0].Temperatures["D176"]!.Value, "first point D176 temperature");
    AssertClose(41.25, records[1].Temperatures["D160"]!.Value, "second point D160 temperature");
    AssertClose(48.5, records[1].Temperatures["D176"]!.Value, "second point D176 temperature");

    string directory = Path.Combine(Path.GetTempPath(), $"wafer-point-temp-{Guid.NewGuid():N}");
    string csvPath = PointTemperatureStorageService.SavePointTemperaturesCsv(
        records,
        directory,
        new DateTime(2026, 6, 24, 17, 30, 0));

    AssertTrue(File.Exists(csvPath), "temperature CSV should be created");
    AssertContains(csvPath, "260624173000_PointTemperatures.csv", "temperature CSV filename");
    string[] lines = File.ReadAllLines(csvPath);
    AssertEqual("PointIndex,X,Y,D160,D162,D164,D166,D170,D172,D174,D176", lines[0], "temperature CSV header");
    AssertEqual("1,10,20,31.25,32.5,33.75,34.25,35.5,36.75,37.25,38.5", lines[1], "first temperature CSV row");
    AssertEqual("2,11,21,41.25,42.5,43.75,44.25,45.5,46.75,47.25,48.5", lines[2], "second temperature CSV row");
}

void TestPointTemperatureRecordsReadEveryAddressForEveryPoint()
{
    var points = Enumerable.Range(0, 2)
        .Select(index => CreatePreData(index, index + 1, up: 1, down: 2, thickness: 3, extra: 4))
        .ToList();
    var readKeys = new List<string>();

    var records = PointTemperatureStorageService.BuildRecords(points, (pointIndex, address) =>
    {
        readKeys.Add($"{pointIndex}:{address}");
        return 20 + readKeys.Count;
    });

    AssertEqual(2, records.Count, "all points should be represented");
    AssertEqual(16, readKeys.Count, "each point should read all eight addresses");
    AssertEqual("1:D160", readKeys[0], "first read key");
    AssertEqual("1:D176", readKeys[7], "eighth read key");
    AssertEqual("2:D160", readKeys[8], "ninth read key");
    AssertEqual("2:D176", readKeys[15], "last read key");
}

void TestPointTemperatureCsvUsesArrivalCapturedRecords()
{
    var points = new List<PreprocessDatasetModel>
    {
        CreatePreData(10, 20, up: 1, down: 2, thickness: 3, extra: 4),
        CreatePreData(11, 21, up: 1, down: 2, thickness: 3, extra: 4),
        CreatePreData(12, 22, up: 1, down: 2, thickness: 3, extra: 4)
    };
    var capturedRecords = new List<PointTemperatureRecord>
    {
        CreateCapturedTemperatureRecord(2, 11, 21, 41.25, 48.5),
        CreateCapturedTemperatureRecord(1, 10, 20, 31.25, 38.5)
    };

    var records = PointTemperatureStorageService.MergeCapturedRecords(points, capturedRecords);

    AssertEqual(3, records.Count, "merged record count");
    AssertClose(31.25, records[0].Temperatures["D160"]!.Value, "first point captured D160");
    AssertClose(38.5, records[0].Temperatures["D176"]!.Value, "first point captured D176");
    AssertClose(41.25, records[1].Temperatures["D160"]!.Value, "second point captured D160");
    AssertClose(48.5, records[1].Temperatures["D176"]!.Value, "second point captured D176");
    AssertEqual(0, records[2].Temperatures.Count, "missing point keeps empty temperatures");
}

void TestGripperClampDefaultsUseJogAndPressureAddresses()
{
    var model = new GripperClampControlModel();
    AssertEqual(4, model.Grippers.Count, "default gripper count");

    string[] expectedForward = { "M3600", "M3610", "M3620", "M3630" };
    string[] expectedReverse = { "M3601", "M3611", "M3621", "M3631" };
    string[] expectedPressure = { "D220", "D222", "D224", "D226" };
    string[] expectedPositions = { "上", "下", "左", "右" };

    for (int index = 0; index < model.Grippers.Count; index++)
    {
        object gripper = model.Grippers[index];
        AssertEqual(expectedForward[index], GetStringProperty(gripper, "ForwardJogAddress"), $"gripper {index + 1} forward jog address");
        AssertEqual(expectedReverse[index], GetStringProperty(gripper, "ReverseJogAddress"), $"gripper {index + 1} reverse jog address");
        AssertEqual(expectedPressure[index], GetStringProperty(gripper, "PressureAddress"), $"gripper {index + 1} pressure address");
        AssertEqual(expectedPositions[index], GetStringProperty(gripper, "PositionName"), $"gripper {index + 1} position name");
        AssertEqual(expectedForward[index], GetStringProperty(gripper, "ClampCommandAddress"), $"gripper {index + 1} compatibility clamp address");
        AssertEqual(expectedReverse[index], GetStringProperty(gripper, "ReleaseCommandAddress"), $"gripper {index + 1} compatibility release address");
    }
}

void TestGripperClampViewExposesJogControlsAndPressureAddressConfig()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\Views\GripperClampControlView.xaml");
    var viewModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\ViewModels\GripperClampControlViewModel.cs");

    AssertContains(xaml, "StartForwardJog", "view should expose forward jog command");
    AssertContains(xaml, "StartReverseJog", "view should expose reverse jog command");
    AssertContains(xaml, "StopJog", "view should stop jog on button release");
    AssertContains(xaml, "PressureValue", "view should display pressure value");
    AssertContains(xaml, "PressureAddress", "view should expose editable pressure address");
    AssertContains(xaml, "ForwardJogAddress", "view should expose editable forward jog address");
    AssertContains(xaml, "ReverseJogAddress", "view should expose editable reverse jog address");
    AssertContains(viewModel, "StartForwardJog", "view model should handle forward jog command");
    AssertContains(viewModel, "StartReverseJog", "view model should handle reverse jog command");
    AssertContains(viewModel, "StopJog", "view model should handle jog stop command");
}

void TestSensorDataCollectionViewExposesPointTemperatureAddressEditor()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Views\SensorDataCollectionView.xaml");
    var model = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Models\SensorDataCollectionModel.cs");

    AssertContains(xaml, "温度采集", "view should contain a point temperature address tab");
    AssertContains(xaml, "ModelParam.PointTemperatureAddresses", "view should bind point temperature addresses");
    AssertContains(xaml, "ResetPointTemperatureAddresses", "view should expose reset temperature address command");
    AssertContains(model, "PointTemperatureAddresses", "model should persist point temperature addresses");
}

void TestPointTemperatureLiveCaptureUsesConfigurableAddresses()
{
    var motionModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Models\SensorMotionControlModel.cs");
    var storageService = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Storage\PointTemperatureStorageService.cs");

    AssertContains(storageService, "PointTemperatureAddressModel", "temperature storage should expose editable address model");
    AssertContains(motionModel, "PointTemperatureAddressConfigUpdatedEventName", "motion model should subscribe to temperature address config updates");
    AssertContains(motionModel, "GetActivePointTemperatureAddresses", "motion model should read active configured temperature addresses");
    AssertDoesNotContain(motionModel, "foreach (string address in PointTemperatureStorageService.DefaultTemperatureAddresses)", "live capture should not directly loop over fixed default addresses");
}

void TestLineSpectrumConverterScalesHeightAndSkipsInvalidRows()
{
    var rows = new List<ReeYin_V.Core.Services.DataCollectRelated.MeasureData>
    {
        new ReeYin_V.Core.Services.DataCollectRelated.MeasureData
        {
            AreaData = new List<float[]>
            {
                new[] { 1f, 2f },
                new[] { 10f, 20f }
            }
        },
        new ReeYin_V.Core.Services.DataCollectRelated.MeasureData
        {
            AreaData = new List<float[]>
            {
                new[] { 3f, 4f }
            }
        },
        new ReeYin_V.Core.Services.DataCollectRelated.MeasureData
        {
            AreaData = new List<float[]>()
        }
    };

    var converted = LineSpectrumDataCollectionModel.ConvertToMeasureRows(rows, 10d);

    AssertEqual(2, converted.HeightRows.Count, "height row count");
    AssertEqual(1, converted.GrayRows.Count, "gray row count");
    AssertClose(10, converted.HeightRows[0][0], "height scale first value");
    AssertClose(20, converted.HeightRows[0][1], "height scale second value");
    AssertClose(30, converted.HeightRows[1][0], "height-only row first value");
    AssertClose(10, converted.GrayRows[0][0], "gray first value");
    AssertClose(20, converted.GrayRows[0][1], "gray second value");
}

void TestLineSpectrumTiffExportPlanStoresOneImagePerStopBatch()
{
    var rows = new LineSpectrumMeasureRows(
        new List<float[]>
        {
            new[] { 1f, 2f },
            new[] { 3f, 4f }
        },
        new List<float[]>
        {
            new[] { 10f, 20f },
            new[] { 30f, 40f }
        });
    var positions = new Dictionary<int, LineSegmentStartPositionInfo>
    {
        [1] = new LineSegmentStartPositionInfo
        {
            SegmentIndex = 1,
            StartX = 10,
            StartY = 20,
            EndX = 100,
            EndY = 200,
            HasEndPosition = true
        },
        [2] = new LineSegmentStartPositionInfo
        {
            SegmentIndex = 2,
            StartX = 11,
            StartY = 21,
            EndX = 101.5,
            EndY = 221.25,
            HasEndPosition = true
        }
    };

    var plan = LineSpectrumTiffExportService.BuildExportPlan(@"D:\LineTiff", rows, positions, 0);

    AssertEqual(1, plan.Count, "export plan batch count");
    AssertEqual(0, plan[0].BatchIndex, "first batch index");
    AssertEqual(2, plan[0].HeightRows.Count, "first batch height image row count");
    AssertEqual(2, plan[0].GrayRows.Count, "first batch gray image row count");
    AssertContains(plan[0].DepthFilePath, @"D:\LineTiff\0\depth\2.9_5.0_0.1_-50000.0_50000.0_0_0.tif", "first batch depth path");
    AssertContains(plan[0].ImageFilePath, @"D:\LineTiff\0\image\2.9_5.0_0.1_-50000.0_50000.0_0_0.tif", "first batch image path");
    AssertClose(0, plan[0].YOffset, "first batch Y offset");
    AssertClose(0, plan[0].XOffset, "first batch X offset");

    var secondPlan = LineSpectrumTiffExportService.BuildExportPlan(@"D:\LineTiff", rows, positions, 1);

    AssertEqual(1, secondPlan.Count, "second export plan batch count");
    AssertEqual(1, secondPlan[0].BatchIndex, "second batch index");
    AssertEqual(2, secondPlan[0].HeightRows.Count, "second batch height image row count");
    AssertEqual(2, secondPlan[0].GrayRows.Count, "second batch gray image row count");
    AssertContains(secondPlan[0].DepthFilePath, @"D:\LineTiff\1\depth\2.9_5.0_0.1_-50000.0_50000.0_201.25_91.5.tif", "second batch depth path");
    AssertContains(secondPlan[0].ImageFilePath, @"D:\LineTiff\1\image\2.9_5.0_0.1_-50000.0_50000.0_201.25_91.5.tif", "second batch image path");
    AssertClose(201.25, secondPlan[0].YOffset, "second batch Y offset");
    AssertClose(91.5, secondPlan[0].XOffset, "second batch X offset");
}

void TestLineSpectrumCollectionViewExposesTiffOutputPathSelector()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\Views\LineSpectrumDataCollectionView.xaml");
    var viewModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\ViewModels\LineSpectrumDataCollectionViewModel.cs");

    AssertContains(xaml, "ModelParam.TiffOutputDirectory", "view should bind tiff output directory");
    AssertContains(xaml, "SelectTiffOutputDirectoryCommand", "view should expose tiff output directory selector");
    AssertContains(viewModel, "OpenFolderDialog", "view model should use folder picker");
    AssertContains(viewModel, "SelectTiffOutputDirectoryCommand", "view model should expose folder picker command");
}

void TestAcsLciFixedDistancePulseScriptUsesConfiguredPulseValuesByDefault()
{
    var model = new SensorMotionControlModel();
    model.AcsLciPulseParam.PulseWidth = 0.123456d;
    model.AcsLciPulseParam.Interval = 2.5d;
    model.AcsLciPulseParam.StartDistance = 0d;
    model.AcsLciPulseParam.EndDistance = 10d;

    var segment = new LineSegment
    {
        Start = new Point2D { X = 0d, Y = 0d },
        End = new Point2D { X = 10d, Y = 0d }
    };

    var param = BuildAcsLciFixedDistancePulseParam(model, segment);

    AssertClose(0.123456d, param.PulseWidth, "configured ACS LCI pulse width should be mapped");
    AssertClose(2.5d, param.Interval, "configured ACS LCI pulse interval should be mapped");

    param.MotionProfile = CreateAcsLciTestMotionProfile();
    var script = AcsControlCard.BuildLciFixedDistancePulseXsegScript(param);
    AssertContains(script, "PulseWidth = 0.123456", "ACS script should use configured pulse width");
    AssertContains(script, "Interval = 2.5", "ACS script should use configured pulse interval");
}

void TestSamplingSpacingUsesControlCardPulseSettings()
{
    var xaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Views\WaferFlatnessConfigView.xaml");
    var model = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Models\SensorMotionControlModel.cs");
    var acsConfig = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Models\AcsLciFixedDistancePulseConfig.cs");

    AssertClose(0.5d, SensorMotionControlModel.ConvertGoogolPulseIntervalToDistance(5000, 10000), "Googol pulse interval should convert to millimeters");
    AssertDoesNotContain(xaml, "ModelParam.SensorInterval", "input parameters should not expose a separate sampling interval");
    AssertContains(xaml, "ModelParam.PosComparisonParam.syncPos", "Googol pulse interval should remain configured in the control-card tab");
    AssertContains(model, "GetSamplingSpacing", "motion model should centralize sampling-spacing resolution");
    AssertContains(model, "PulseEquivalent", "Googol sampling should use the coordinate system pulse equivalent");
    AssertDoesNotContain(model, "SensorInterval", "motion execution should not use the removed sampling interval");
    AssertDoesNotContain(acsConfig, "UseSensorInterval", "ACS LCI should always use its configured pulse interval");
}

void TestLineSegmentSummaryAnalysisSelectsOnlyEnabledAlgorithms()
{
    var algorithms = new[]
    {
        new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.Flatness, true),
        new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.Parallelism, false),
        new DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind.TTV, true)
    };

    var selected = LineSegmentSurfaceAnalysisSelection.GetEnabledAlgorithms(algorithms);

    AssertEqual(2, selected.Count, "only checked algorithms should be selected");
    AssertTrue(selected.Contains(DataAnalysisAlgorithmKind.Flatness), "flatness should be selected");
    AssertTrue(selected.Contains(DataAnalysisAlgorithmKind.TTV), "TTV should be selected");
    AssertTrue(!selected.Contains(DataAnalysisAlgorithmKind.Parallelism), "unchecked parallelism should not be selected");
}

void TestLineSegmentSummaryAnalysisUsesFixedUpperAndLowerSurfaceSources()
{
    var sources = LineSegmentSurfaceAnalysisSelection.CreateUpperLowerSurfaceSources();

    AssertEqual(2, sources.Count, "line segment summary should use only upper and lower surfaces");
    AssertEqual(SensorDataCollectionModel.UpSurfaceResultOption, sources[0].Name, "first source should be upper surface");
    AssertEqual(PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName, sources[0].OriginalDataValueName, "upper source original data");
    AssertEqual(SensorDataCollectionModel.DownSurfaceResultOption, sources[1].Name, "second source should be lower surface");
    AssertEqual(PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName, sources[1].OriginalDataValueName, "lower source original data");
}

void TestLineSegmentSummaryCsvTriggersUpperAndLowerSurfaceAnalysis()
{
    var model = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Models\SensorDataCollectionModel.cs");
    var runAlgo = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Processing\RunALGO.cs");

    AssertContains(model, "RunLineSegmentSummaryAnalysis(rawSummaryCsvPath, rawSummaryPreDatas)", "raw summary CSV save should trigger line segment analysis");
    AssertContains(model, "RunLineSegmentSummaryAnalysis(filteredSummaryCsvPath, filteredSummaryPreDatas)", "filtered summary CSV save should trigger line segment analysis");
    AssertContains(runAlgo, "RunLineSegmentSummaryAnalysis", "line segment analysis method should live with analysis code");
    AssertContains(runAlgo, "LineSegmentSurfaceAnalysisSelection.CreateUpperLowerSurfaceSources()", "line segment analysis should use fixed upper/lower sources");
}

void TestLineSegmentFilteredCsvUsesFilteredSuffix()
{
    var service = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Storage\SensorDataStorageService.cs");

    AssertContains(service, "GetLineSegmentFilteredPreDatasCsvPath", "storage service should expose filtered line segment PreDatas path");
    AssertContains(service, "SaveLineSegmentFilteredPreDatasCsv", "storage service should save filtered line segment PreDatas");
    AssertContains(service, "LineSegments_All_PreDatas_Filtered.csv", "filtered summary CSV should use _Filtered suffix");
    AssertContains(service, "PreDatas_Filtered", "filtered segment CSV should use _Filtered suffix");
}

void TestLineSegmentModelStoresRawAndFilteredSummaries()
{
    var model = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Models\SensorDataCollectionModel.cs");

    AssertContains(model, "_lineSegmentSummaryRawPreDatas", "model should keep raw line segment summary data");
    AssertContains(model, "_lineSegmentSummaryFilteredPreDatas", "model should keep filtered line segment summary data");
    AssertContains(model, "SaveLineSegmentFilteredPreDatasCsv", "model should save filtered per-line PreDatas");
    AssertContains(model, "SaveLineSegmentFilteredSummaryCsv", "model should save filtered summary PreDatas");
    AssertContains(model, "RunLineSegmentSummaryAnalysis(filteredSummaryCsvPath, filteredSummaryPreDatas)", "summary analysis should use filtered summary data");
}

void TestLineSegmentRawSummaryCsvRetainsAnalysisResults()
{
    var model = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Models\SensorDataCollectionModel.cs");

    AssertContains(model, "RunLineSegmentSummaryAnalysis(rawSummaryCsvPath, rawSummaryPreDatas)", "raw summary CSV should run its own summary analysis");
    AssertContains(model, "RunLineSegmentSummaryAnalysis(filteredSummaryCsvPath, filteredSummaryPreDatas)", "filtered summary CSV should still run filtered summary analysis");
    AssertDoesNotContain(model, "else if (!string.IsNullOrWhiteSpace(rawSummaryCsvPath))", "raw summary analysis must not be skipped when filtered summary exists");
}

void TestTrajectoryDesignerSwitchesBackToSelectAfterAddingOneShape()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(control, "ActiveTool = TrajectoryDesignerTool.Select;", "creating a shape should return the designer to select mode");
}

void TestTrajectoryDesignerBoxSelectsShapesAndCirclesRotateByHandle()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");
    var view = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryDesignerTestView.xaml");
    var viewModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\ViewModels\TrajectoryDesignerTestViewModel.cs");

    AssertContains(control, "SelectedShapesProperty", "control should expose multi-selection state");
    AssertContains(control, "_isSelectingByBox", "left drag on blank canvas should track a selection box");
    AssertContains(control, "BeginSelectionBox", "control should start box selection on left down");
    AssertContains(control, "UpdateSelectionBox", "control should update the selection box while dragging");
    AssertContains(control, "CompleteSelectionBox", "control should select shapes included by the box on mouse up");
    AssertContains(control, "DrawSelectionBox", "selection box should be visible during drag");
    AssertContains(control, "GetShapesInsideSelectionBox", "selection should be based on shapes included in the dragged box");
    AssertContains(control, "BeginDragSelectedShapes", "selected shapes should be dragged as a group");
    AssertDoesNotContain(control, "SelectAllShapes();", "blank right-click should not select all shapes anymore");
    AssertContains(control, "CircleRotate", "circle drag handles should include a rotation handle");
    AssertContains(control, "HitTestCircleRotationHandle", "selected circles should expose a rotation hit-test");
    AssertContains(control, "BeginCircleRotation", "circle rotation drag should have an explicit begin path");
    AssertContains(control, "UpdateCircleRotationFromScreenPoint", "dragging the circle rotation handle should update RotationAngle");
    AssertContains(control, "DrawCircleRotationHandle", "selected circles should draw the rotation handle");
    AssertContains(control, "GetCircleRotationHandlePoint", "circle rotation handle should be positioned relative to the circle");
    AssertContains(control, "SelectedShapes.Contains(shape)", "canvas rendering should highlight every selected shape");
    AssertContains(view, "SelectedShapes=\"{Binding SelectedShapes}\"", "test view should bind multi-selection state");
    AssertContains(viewModel, "public ObservableCollection<EditableTrajectoryShape> SelectedShapes", "test view model should expose selected shapes");
}

void TestTrajectoryDesignerConstrainsShapeCentersToCoordinateBounds()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");
    var view = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryDesignerTestView.xaml");
    var viewModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\ViewModels\TrajectoryDesignerTestViewModel.cs");

    AssertContains(control, "CoordinateStartXProperty", "control should expose XY range start X");
    AssertContains(control, "CoordinateEndYProperty", "control should expose XY range end Y");
    AssertContains(control, "ClampPointToCoordinateBounds", "shape centers should be clamped to the configured XY range");
    AssertContains(control, "CoordinateToScreen", "canvas should map XY coordinates onto the full drawing surface");
    AssertContains(view, "CoordinateStartX=\"{Binding CoordinateStartX, Mode=TwoWay}\"", "test view should bind range start X");
    AssertContains(view, "CoordinateEndY=\"{Binding CoordinateEndY, Mode=TwoWay}\"", "test view should bind range end Y");
    AssertContains(viewModel, "public double CoordinateStartX", "test view model should expose range start X");
    AssertContains(viewModel, "public double CoordinateEndY", "test view model should expose range end Y");
}

void TestTrajectoryDesignerUsesFullCanvasCoordinatePlaneAndCircleRadiusHandle()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");
    var view = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryDesignerTestView.xaml");
    var viewCodeBehind = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryDesignerTestView.xaml.cs");
    var settingsWindow = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryCoordinateSettingsWindow.xaml");
    var settingsWindowCodeBehind = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryCoordinateSettingsWindow.xaml.cs");

    AssertContains(control, "ScreenToCoordinate", "control should convert mouse screen points into the configured XY plane");
    AssertContains(control, "CoordinateToScreen", "control should convert selected shape handles back to screen points");
    AssertContains(control, "CoordinateEndX - CoordinateStartX", "full canvas should map from start X to end X");
    AssertContains(control, "CoordinateEndY - CoordinateStartY", "full canvas should map from start Y to end Y");
    AssertDoesNotContain(control, "DrawCoordinateBounds(canvas);", "full canvas is the coordinate range, so no extra bounds rectangle should be drawn");
    AssertContains(control, "HitTestCircleRadiusHandle", "selected circles should expose a draggable radius handle");
    AssertContains(control, "BeginCircleRadiusResize", "control should begin circle radius resize before move drag");
    AssertContains(control, "UpdateCircleRadiusFromScreenPoint", "dragging the radius handle should update the circle radius");
    AssertContains(control, "DrawCircleRadiusHandle", "selected circles should render the radius handle");

    AssertContains(view, "CoordinateSettingsButton_Click", "test page should open coordinate settings in a separate window");
    AssertDoesNotContain(view, "Text=\"{Binding CoordinateStartX", "coordinate start X editor should not stay in the main panel");
    AssertDoesNotContain(view, "Text=\"{Binding CoordinateEndY", "coordinate end Y editor should not stay in the main panel");
    AssertContains(viewCodeBehind, "TrajectoryCoordinateSettingsWindow", "test page code-behind should create the coordinate settings window");
    AssertContains(settingsWindow, "CoordinateStartX", "coordinate settings window should edit range start X");
    AssertContains(settingsWindow, "CoordinateEndY", "coordinate settings window should edit range end Y");
    AssertContains(settingsWindow, "DefaultCenterX", "coordinate settings window should edit fixed center X");
    AssertContains(settingsWindowCodeBehind, "DialogResult = true", "coordinate settings window should close as confirmed");
}

void TestTrajectoryDesignerDrawsMillimeterTicksAndKeepsCirclesRoundWithThinnerStrokes()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(control, "TickIntervalMillimeters = 20d", "designer should use 20mm tick label spacing");
    AssertContains(control, "DrawCoordinateTicks(canvas, viewportWidth, viewportHeight);", "designer should draw coordinate tick labels");
    AssertContains(control, "DrawTickLabel", "designer should label major coordinate ticks");
    AssertContains(control, "CoordinateLengthToScreenRadius", "shape sizes should use one uniform screen scale");
    AssertContains(control, "DrawCircleScreenShape", "circle drawing should use screen-space radius so it stays round");
    AssertContains(control, "StrokeWidth = isSelected ? 2f : 1.2f", "shape strokes should be thinner than before");
    AssertContains(control, "StrokeWidth = 0.8f", "internal trajectory and tick strokes should be thin");
}

void TestTrajectoryDesignerKeepsCoordinateAxesUniformScale()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");
    var mapper = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Services\TrajectoryCoordinateMapper.cs");

    AssertDoesNotContain(control, "private readonly struct CoordinateScreenTransform", "coordinate fitting data should live outside the UI code-behind");
    AssertContains(control, "GetCoordinateScreenTransform", "designer should calculate one shared screen transform for the XY plane");
    AssertContains(control, "TrajectoryCoordinateMapper.GetTransform", "designer should delegate uniform coordinate fitting to the mapper");
    AssertContains(mapper, "UniformScale", "coordinate transform should expose a single mm-to-pixel scale for both axes");
    AssertContains(mapper, "Math.Min(viewportWidth / rangeX, viewportHeight / rangeY)", "uniform scale should fit the full coordinate window without stretching one axis");
    AssertContains(control, "PlotLeft", "coordinate transform should horizontally center the fitted coordinate plot");
    AssertContains(control, "PlotTop", "coordinate transform should vertically center the fitted coordinate plot");
    AssertContains(control, "TrajectoryCoordinateMapper.ScreenToCoordinate", "designer should delegate screen-to-coordinate conversion to the mapper");
    AssertContains(control, "TrajectoryCoordinateMapper.CoordinateToScreen", "designer should delegate coordinate-to-screen conversion to the mapper");
    AssertContains(control, "TrajectoryCoordinateMapper.LengthToScreen", "shape dimensions should use the mapper's uniform scale");
    AssertContains(control, "DrawCoordinateTicks", "coordinate ticks should be drawn against the fitted plot area");
    AssertContains(control, "transform.PlotWidth", "viewport panning should measure horizontal movement against the fitted coordinate plot width");
    AssertContains(control, "transform.PlotHeight", "viewport panning should measure vertical movement against the fitted coordinate plot height");
}

void TestTrajectoryDesignerKeepsPlotAreaAndCoordinateSystemAligned()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(control, "DrawGrid(canvas);", "grid should use the same fitted plot area as the coordinate system");
    AssertDoesNotContain(control, "DrawGrid(canvas, viewportWidth, viewportHeight);", "grid should not fill the whole control when the coordinate plot is letterboxed");
    AssertContains(control, "SKRect plotRect = SKRect.Create(", "plot rectangle should come from the coordinate transform");
    AssertContains(control, "(float)transform.PlotLeft", "plot rectangle should use the fitted coordinate plot left edge");
    AssertContains(control, "canvas.DrawRect(plotRect, plotFillPaint)", "only the coordinate plot area should be filled as the drawable plane");
    AssertContains(control, "canvas.ClipRect(plotRect", "grid lines should be clipped to the coordinate plot area");
    AssertContains(control, "CoordinateToScreen(x, startY)", "vertical grid lines should be derived from coordinate ticks");
    AssertContains(control, "CoordinateToScreen(startX, y)", "horizontal grid lines should be derived from coordinate ticks");
    AssertContains(control, "TrajectoryCoordinateMapper.ScreenToCoordinate", "mouse conversion should use mapper clamping at the coordinate plot boundary");
}

void TestTrajectoryCoordinateMapperFitsUniformlyAndClampsLetterboxPoints()
{
    var transform = TrajectoryCoordinateMapper.GetTransform(640d, 480d, 0d, 0d, 640d, 320d);

    AssertClose(0d, transform.PlotLeft, "uniform plot left");
    AssertClose(80d, transform.PlotTop, "uniform plot top should center the letterboxed coordinate area");
    AssertClose(640d, transform.PlotWidth, "uniform plot width");
    AssertClose(320d, transform.PlotHeight, "uniform plot height");
    AssertClose(1d, transform.UniformScale, "X and Y should share one screen scale");

    var start = TrajectoryCoordinateMapper.CoordinateToScreen(transform, 0d, 0d);
    AssertClose(0d, start.X, "coordinate start X maps to plot left");
    AssertClose(400d, start.Y, "coordinate start Y maps to plot bottom");

    var end = TrajectoryCoordinateMapper.CoordinateToScreen(transform, 640d, 320d);
    AssertClose(640d, end.X, "coordinate end X maps to plot right");
    AssertClose(80d, end.Y, "coordinate end Y maps to plot top");

    var center = TrajectoryCoordinateMapper.CoordinateToScreen(transform, 320d, 160d);
    AssertClose(320d, center.X, "coordinate center X maps to plot center");
    AssertClose(240d, center.Y, "coordinate center Y maps to plot center");
    AssertClose(25d, TrajectoryCoordinateMapper.LengthToScreen(transform, 25d), "coordinate length uses uniform scale");

    var clampedTop = TrajectoryCoordinateMapper.ScreenToCoordinate(transform, 320d, 0d);
    AssertClose(320d, clampedTop.X, "letterbox top X stays aligned");
    AssertClose(320d, clampedTop.Y, "letterbox top clamps to coordinate max Y");

    var clampedBottomLeft = TrajectoryCoordinateMapper.ScreenToCoordinate(transform, -50d, 500d);
    AssertClose(0d, clampedBottomLeft.X, "off-plot left clamps to coordinate min X");
    AssertClose(0d, clampedBottomLeft.Y, "off-plot bottom clamps to coordinate min Y");
}

void TestTrajectoryDesignerSupports20MillimeterTicksPointShapesAndDragResizeHandles()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");
    var controlXaml = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml");
    var tool = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Models\TrajectoryDesignerTool.cs");
    var kind = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Models\TrajectoryShapeKind.cs");

    AssertContains(control, "TickIntervalMillimeters = 20d", "coordinate labels should be spaced every 20mm");
    AssertDoesNotContain(control, "MinorTickMillimeters = 1d", "1mm tick marks should no longer be drawn");
    AssertDoesNotContain(control, "MajorTickMillimeters = 10d", "10mm label spacing should no longer be used");
    AssertContains(tool, "Point", "designer should expose a point drawing tool");
    AssertContains(kind, "Point", "designer should expose a point shape kind");
    AssertContains(controlXaml, "PointToolButton", "toolbar should include a point tool button");
    AssertContains(control, "DrawPointScreenShape", "designer should render point shapes");
    AssertContains(control, "ContainsPointShape", "point shapes should be hit-testable");
    AssertContains(control, "HitTestRectangleResizeHandle", "selected rectangles should expose drag resize handles");
    AssertContains(control, "BeginRectangleResize", "rectangle resize drag should have an explicit begin path");
    AssertContains(control, "UpdateRectangleSizeFromScreenPoint", "rectangle resize drag should update width and height");
    AssertContains(control, "DrawRectangleResizeHandles", "selected rectangles should draw resize handles");
    AssertContains(control, "HitTestArcResizeHandle", "selected arcs should expose drag handles");
    AssertContains(control, "BeginArcResize", "arc resize drag should have an explicit begin path");
    AssertContains(control, "UpdateArcFromScreenPoint", "arc drag should update radius/start/sweep");
    AssertContains(control, "DrawArcResizeHandles", "selected arcs should draw resize handles");
    AssertContains(control, "DrawShapeDimensionLabel", "shapes should draw dimension labels");
    AssertContains(control, "FormatCircleDimensionText", "circle labels should include radius");
    AssertContains(control, "FormatRectangleDimensionText", "rectangle labels should include width and height");
    AssertContains(control, "FormatArcDimensionText", "arc labels should include sweep angle");
}

void TestTrajectoryDesignerLabelsCoordinatesAndDimensionsOnRelatedSegments()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(control, "DrawShapeCenterCoordinateLabel", "points and shape centers should render their XY coordinate labels");
    AssertContains(control, "FormatShapeCenterCoordinateText", "coordinate label should format the shape center coordinates");
    AssertContains(control, "DrawCircleRadiusDimensionLabel", "circle radius text should be drawn on the radius segment");
    AssertContains(control, "DrawRectangleDimensionLabelsOnSegments", "rectangle width and height text should be drawn on related edges");
    AssertContains(control, "DrawArcSweepDimensionLabel", "arc sweep text should be drawn near the arc segment");
    AssertContains(control, "DrawSegmentLabel", "dimension labels should align directly with their related line segment");
    AssertContains(control, "FormatRectangleWidthDimensionText", "rectangle width label should be independent");
    AssertContains(control, "FormatRectangleHeightDimensionText", "rectangle height label should be independent");
}

void TestTrajectoryCoordinateSettingsScrollAndRectanglesRotateByDragHandle()
{
    var settingsWindow = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryCoordinateSettingsWindow.xaml");
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(settingsWindow, "<ScrollViewer", "coordinate settings content should be scrollable when the window is short");
    AssertContains(settingsWindow, "VerticalScrollBarVisibility=\"Auto\"", "coordinate settings should show a vertical scrollbar only when needed");
    AssertContains(settingsWindow, "ResizeMode=\"CanResize\"", "coordinate settings window should be resizable instead of fixed height");
    AssertDoesNotContain(settingsWindow, "ResizeMode=\"NoResize\"", "coordinate settings should not be locked to a fixed size");

    AssertContains(control, "RectangleRotate", "rectangle drag handles should include a rotation handle");
    AssertContains(control, "HitTestRectangleRotationHandle", "selected rectangles should expose a rotation hit-test");
    AssertContains(control, "BeginRectangleRotation", "rectangle rotation drag should have an explicit begin path");
    AssertContains(control, "UpdateRectangleRotationFromScreenPoint", "dragging the rotation handle should update RotationAngle");
    AssertContains(control, "DrawRectangleRotationHandle", "selected rectangles should draw the rotation handle");
    AssertContains(control, "GetRectangleRotationHandlePoint", "rotation handle should be positioned relative to the rectangle");
}

void TestTrajectoryDesignerCreatesAndEditsLineSegments()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");
    var controlXaml = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml");
    var tool = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Models\TrajectoryDesignerTool.cs");
    var kind = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Models\TrajectoryShapeKind.cs");

    AssertContains(tool, "Line", "designer should expose a line segment drawing tool");
    AssertContains(kind, "Line", "designer should expose a line segment shape kind");
    AssertContains(controlXaml, "LineToolButton", "toolbar should include a line segment tool button");
    AssertContains(control, "_lineStartCoordinate", "line tool should remember the first clicked start point");
    AssertContains(control, "TryBeginLinePlacement", "line creation should start from the first click");
    AssertContains(control, "CompleteLinePlacement", "line creation should finish from a second clicked end point");
    AssertContains(control, "if (ActiveTool == TrajectoryDesignerTool.Line && _lineStartCoordinate.HasValue)", "line preview should keep the first clicked point as a fixed start");
    AssertContains(control, "CreateLineShapeFromEndpoints", "line shape should be built from start and end coordinates");
    AssertContains(control, "DrawLineScreenShape", "designer should render line segment shapes");
    AssertContains(control, "ContainsLine", "line center/body should be hit-testable for dragging");
    AssertContains(control, "HitTestLineEndpointHandle", "selected lines should expose endpoint hit-tests");
    AssertContains(control, "BeginLineEndpointResize", "line endpoint drag should have an explicit begin path");
    AssertContains(control, "UpdateLineEndpointFromScreenPoint", "dragging a line endpoint should update length and angle");
    AssertContains(control, "DrawLineEndpointHandles", "selected lines should draw start/end handles");
    AssertContains(control, "GetLineEndpointHandlePoints", "line endpoints should be derived from the line center and angle");
}

void TestTrajectoryDesignerLinePreviewAnchorsClickedStartPoint()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(control, "GetLineEndpointScreenPoints", "line preview should draw from coordinate endpoints, not a uniform screen-scale proxy");
    AssertContains(control, "CoordinateToScreen(endpoints.StartX, endpoints.StartY)", "line start screen point should come from the clicked coordinate start");
    AssertContains(control, "CoordinateToScreen(endpoints.EndX, endpoints.EndY)", "line end screen point should come from the current coordinate end");
    AssertContains(control, "DrawLineScreenShape(canvas, shape, strokePaint, startPoint, endPoint)", "line drawing should use fixed endpoint screen points");
    AssertContains(control, "ContainsLineScreenPoint", "line hit testing should use endpoint screen geometry");
    AssertDoesNotContain(control, "TrajectoryResizeHandleKind.LineStart, ToRotatedScreenPoint(shape, -halfLength, 0d)", "line start handle should not drift with uniform screen scaling");
}

void TestTrajectoryDesignerZoomsAndResetsCoordinateViewport()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");
    var controlXaml = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml");

    AssertContains(controlXaml, "ZoomInButton", "toolbar should expose a coordinate zoom-in button");
    AssertContains(controlXaml, "ZoomOutButton", "toolbar should expose a coordinate zoom-out button");
    AssertContains(controlXaml, "ResetZoomButton", "toolbar should expose a coordinate viewport reset button");
    AssertContains(controlXaml, "MouseWheel=\"DrawingSurface_MouseWheel\"", "drawing surface should support mouse wheel viewport zoom");
    AssertContains(control, "ZoomCoordinateViewport", "control should zoom the visible coordinate viewport without changing shape data");
    AssertContains(control, "ResetCoordinateViewport", "control should restore the full configured coordinate bounds");
    AssertContains(control, "GetVisibleCoordinateWindow", "screen-coordinate conversion should use a visible viewport window");
    AssertContains(control, "ClampViewportToCoordinateBounds", "zoomed viewport should stay inside configured coordinate bounds");
    AssertContains(control, "GetCoordinateViewportZoomFactor", "status text should expose the current coordinate zoom factor");
}

void TestTrajectoryDesignerPansCoordinateViewportByLeftDraggingBlankSpace()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(control, "_isPanningCoordinateViewport", "control should track blank-space viewport panning");
    AssertContains(control, "BeginCoordinateViewportPan", "left down on blank space should start viewport panning");
    AssertContains(control, "UpdateCoordinateViewportPan", "mouse move should update the visible coordinate viewport while panning");
    AssertContains(control, "EndCoordinateViewportPan", "mouse up should end viewport panning");
    AssertContains(control, "PanCoordinateViewport", "viewport panning should move the visible coordinate window without changing shape data");
    AssertContains(control, "正在平移坐标系", "status text should communicate coordinate viewport panning");
}

void TestTrajectoryDesignerToolbarOperationsUseIconsInsteadOfText()
{
    var controlXaml = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml");

    foreach (var textContent in new[]
    {
        "Content=\"选择\"",
        "Content=\"点\"",
        "Content=\"线段\"",
        "Content=\"圆\"",
        "Content=\"矩形\"",
        "Content=\"圆弧\"",
        "Content=\"放大\"",
        "Content=\"缩小\"",
        "Content=\"还原\""
    })
    {
        AssertDoesNotContain(controlXaml, textContent, "toolbar operations should not use text content");
    }

    foreach (var tooltip in new[]
    {
        "ToolTip=\"选择\"",
        "ToolTip=\"点\"",
        "ToolTip=\"线段\"",
        "ToolTip=\"圆\"",
        "ToolTip=\"矩形\"",
        "ToolTip=\"圆弧\"",
        "ToolTip=\"放大\"",
        "ToolTip=\"缩小\"",
        "ToolTip=\"还原\""
    })
    {
        AssertContains(controlXaml, tooltip, "toolbar icon buttons should keep Chinese tooltips");
    }

    AssertContains(controlXaml, "<Viewbox", "toolbar icon content should be vector based");
    AssertContains(controlXaml, "<Canvas", "toolbar icons should compose simple vector shapes");
    AssertContains(controlXaml, "ControlTemplate TargetType=\"{x:Type RadioButton}\"", "radio tool buttons should use a custom template without the default radio dot");
    AssertContains(controlXaml, "ToolButtonChrome", "radio tool buttons should expose a selectable background chrome");
    AssertContains(controlXaml, "Property=\"IsChecked\"", "radio tool buttons should define a checked visual state");
    AssertContains(controlXaml, "Value=\"#FFE1E5EA\"", "checked radio tool buttons should use a gray background");
    AssertDoesNotContain(controlXaml, "BulletDecorator", "radio tool buttons should not render the default selected dot");
}

void TestTrajectoryDesignerKeepsShapeTableFixedAtBottomAndExposesEnabledColumn()
{
    var viewXaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryDesignerTestView.xaml");
    var shapeModel = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Models\EditableTrajectoryShape.cs");

    AssertContains(viewXaml, "ShapeTableGrid", "shape table should be named so it is intentionally anchored at the bottom");
    AssertContains(viewXaml, "Grid.Row=\"3\"", "shape table should live in the bottom row outside the scrolling settings area");
    AssertContains(viewXaml, "<RowDefinition Height=\"220\"", "right panel should reserve a fixed bottom row for the table");
    AssertContains(viewXaml, "ShapeEnabledColumn", "shape table should include an enabled checkbox column");
    AssertContains(viewXaml, "DataGridCheckBoxColumn", "enabled state should be edited through a checkbox column");
    AssertContains(viewXaml, "Binding=\"{Binding IsEnabled", "enabled checkbox should bind to the shape enabled state");
    AssertContains(viewXaml, "CategoryText", "shape table should show point line and region categories");
    AssertContains(shapeModel, "private bool _isEnabled = true;", "new shapes should be enabled by default");
    AssertContains(shapeModel, "public bool IsEnabled", "shape rows should expose an enabled flag");
}

void TestTrajectoryDesignerRightDragsViewportAndFloatsSingleShapeParameters()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");
    var controlXaml = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml");
    var viewXaml = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryDesignerTestView.xaml");

    AssertContains(controlXaml, "MouseRightButtonUp=\"DrawingSurface_MouseRightButtonUp\"", "right-button release should complete pan or open the delete menu");
    AssertContains(control, "_isRightButtonPanningCoordinateViewport", "right-button drag should track viewport panning");
    AssertContains(control, "BeginRightButtonCoordinateViewportPan", "right-button drag should start coordinate panning");
    AssertContains(control, "UpdateRightButtonCoordinateViewportPan", "right-button drag should update coordinate panning");
    AssertContains(control, "EndRightButtonCoordinateViewportPan", "right-button release should end coordinate panning");
    AssertContains(control, "RightButtonPanThreshold", "right-click should distinguish click delete from drag pan");
    AssertContains(control, "ShowDeleteMenu(_rightButtonDownShape", "right-click without dragging should still open delete menu for a shape");

    AssertContains(viewXaml, "FloatingSelectedShapePanel", "single-shape parameters should be hosted in a floating panel");
    AssertContains(viewXaml, "HorizontalAlignment=\"Right\"", "floating parameter panel should be placed near the upper-right of the coordinate canvas");
    AssertContains(viewXaml, "VerticalAlignment=\"Top\"", "floating parameter panel should be placed near the upper-right of the coordinate canvas");
    AssertContains(viewXaml, "DataTrigger Binding=\"{Binding HasSelectedShape}\" Value=\"False\"", "floating parameter panel should hide when no single shape is selected");
    AssertTrue(
        viewXaml.IndexOf("FloatingSelectedShapePanel", StringComparison.Ordinal) < viewXaml.IndexOf("<Border Grid.Column=\"2\"", StringComparison.Ordinal),
        "floating parameter panel should live over the coordinate canvas instead of the fixed right panel");
}

void TestTrajectoryDesignerGuardsNullSelectedShapeCollection()
{
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(control, "EnsureSelectedShapes", "designer should centralize null-safe selected-shape collection access");
    AssertContains(control, "get => EnsureSelectedShapes()", "SelectedShapes getter should never return null");
    AssertContains(control, "SetCurrentValue(SelectedShapesProperty, value)", "SelectedShapes getter should restore a local fallback without breaking an existing binding");
    AssertContains(control, "foreach (EditableTrajectoryShape shape in EnsureSelectedShapes())", "selected shape enumeration should tolerate a null dependency property value");
}

void TestDesignedTrajectoryTreatsCircleAndRectangleOutlinesAsAreasOnly()
{
    var circleArea = new DesignedTrajectoryShape
    {
        Kind = TrajectoryShapeKind.Circle,
        X = 100d,
        Y = 100d,
        Radius = 20d,
        InnerPattern = TrajectoryInnerPattern.None
    };
    var rectangleArea = new DesignedTrajectoryShape
    {
        Kind = TrajectoryShapeKind.Rectangle,
        X = 160d,
        Y = 100d,
        Width = 40d,
        Height = 20d,
        InnerPattern = TrajectoryInnerPattern.None
    };

    var outlineOnlySteps = DesignedTrajectoryBuilder.BuildRunSteps(new[] { circleArea, rectangleArea }, 1d);
    AssertEqual(0, outlineOnlySteps.Count, "circle and rectangle outlines should only define an area");
    AssertEqual(TrajectoryShapeCategory.Region, circleArea.Category, "circle should be classified as a region");
    AssertEqual(TrajectoryShapeCategory.Region, rectangleArea.Category, "rectangle should be classified as a region");

    circleArea.InnerPattern = TrajectoryInnerPattern.EquidistantPoints;
    circleArea.InnerSpacing = 20d;
    rectangleArea.InnerPattern = TrajectoryInnerPattern.HorizontalLines;
    rectangleArea.InnerSpacing = 10d;

    var regionSteps = DesignedTrajectoryBuilder.BuildRunSteps(new[] { circleArea, rectangleArea }, 1d);
    AssertTrue(regionSteps.Any(step => step.SourceShapeId == circleArea.Id && step.Kind == DesignedTrajectoryRunStepKind.Point), "area internal points should be executable");
    AssertTrue(regionSteps.Any(step => step.SourceShapeId == rectangleArea.Id && step.Kind == DesignedTrajectoryRunStepKind.Line), "area internal lines should be executable");

    var point = new DesignedTrajectoryShape { Kind = TrajectoryShapeKind.Point, X = 1d, Y = 2d };
    var line = new DesignedTrajectoryShape { Kind = TrajectoryShapeKind.Line, X = 5d, Y = 5d, Width = 10d };
    var directSteps = DesignedTrajectoryBuilder.BuildRunSteps(new[] { point, line }, 1d);
    AssertTrue(directSteps.Any(step => step.SourceShapeId == point.Id && step.Kind == DesignedTrajectoryRunStepKind.Point), "point shapes should be executable");
    AssertTrue(directSteps.Any(step => step.SourceShapeId == line.Id && step.Kind == DesignedTrajectoryRunStepKind.Line), "line shapes should be executable");
}

void TestDesignedTrajectoryBuilderExpandsPolylineIntoConnectedLineSteps()
{
    var polyline = new DesignedTrajectoryShape
    {
        Kind = TrajectoryShapeKind.Polyline,
        PolylinePoints = new List<TrajectoryPoint>
        {
            new() { X = 10d, Y = 20d },
            new() { X = 30d, Y = 40d },
            new() { X = 50d, Y = 25d }
        }
    };

    List<DesignedTrajectoryRunStep> steps = DesignedTrajectoryBuilder.BuildRunSteps(new[] { polyline }, 1d);
    AssertEqual(2, steps.Count, "a three-vertex polyline should generate two connected run steps");
    AssertEqual(DesignedTrajectoryRunStepKind.Line, steps[0].Kind, "polyline first step should be a line");
    AssertClose(10d, steps[0].StartX, "polyline first step start X");
    AssertClose(20d, steps[0].StartY, "polyline first step start Y");
    AssertClose(30d, steps[0].EndX, "polyline first step end X");
    AssertClose(40d, steps[0].EndY, "polyline first step end Y");
    AssertClose(30d, steps[1].StartX, "polyline second step shares prior end X");
    AssertClose(40d, steps[1].StartY, "polyline second step shares prior end Y");
    AssertClose(50d, steps[1].EndX, "polyline second step end X");
    AssertClose(25d, steps[1].EndY, "polyline second step end Y");

    EditableTrajectoryShape editable = DesignedTrajectoryBuilder.ToEditableShape(polyline);
    AssertEqual(3, editable.PolylinePoints.Count, "polyline vertices should survive plan-to-editor conversion");
}

void TestTrajectoryGeometryServiceGeneratesSharedRegionPointsAndLines()
{
    var circleShape = new DesignedTrajectoryShape
    {
        Kind = TrajectoryShapeKind.Circle,
        X = 10d,
        Y = 20d,
        Radius = 20d,
        InnerPattern = TrajectoryInnerPattern.EquidistantPoints,
        InnerSpacing = 20d
    };

    var circle = TrajectoryShapeGeometry.FromEditable(DesignedTrajectoryBuilder.ToEditableShape(circleShape));
    var localCirclePoints = TrajectoryGeometryService.GenerateLocalInnerPoints(circle)
        .OrderBy(point => point.Y)
        .ThenBy(point => point.X)
        .ToList();
    AssertEqual(5, localCirclePoints.Count, "circle should place one center point and four cardinal points at radius spacing");
    AssertTrue(localCirclePoints.Any(point => NearlyEqual(point.X, 0d) && NearlyEqual(point.Y, 0d)), "circle local points should include the center");
    AssertTrue(localCirclePoints.Any(point => NearlyEqual(point.X, 20d) && NearlyEqual(point.Y, 0d)), "circle local points should include right cardinal point");

    var worldCirclePoints = TrajectoryGeometryService.GenerateInnerPoints(circle).ToList();
    AssertTrue(worldCirclePoints.Any(point => NearlyEqual(point.X, 10d) && NearlyEqual(point.Y, 20d)), "circle world points should include the shape center");
    AssertTrue(worldCirclePoints.Any(point => NearlyEqual(point.X, 30d) && NearlyEqual(point.Y, 20d)), "circle world points should translate local points by center");

    var rectangleShape = new DesignedTrajectoryShape
    {
        Kind = TrajectoryShapeKind.Rectangle,
        X = 100d,
        Y = 50d,
        Width = 40d,
        Height = 20d,
        RotationAngle = 90d,
        InnerPattern = TrajectoryInnerPattern.HorizontalLines,
        InnerSpacing = 10d
    };

    var rectangle = TrajectoryShapeGeometry.FromEditable(DesignedTrajectoryBuilder.ToEditableShape(rectangleShape));
    var rectangleLines = TrajectoryGeometryService.GenerateInnerLines(rectangle).ToList();
    AssertEqual(3, rectangleLines.Count, "rectangle horizontal internal lines should include both edges and center");
    AssertClose(110d, rectangleLines[0].StartX, "rotated rectangle first line start X");
    AssertClose(30d, rectangleLines[0].StartY, "rotated rectangle first line start Y");
    AssertClose(110d, rectangleLines[0].EndX, "rotated rectangle first line end X");
    AssertClose(70d, rectangleLines[0].EndY, "rotated rectangle first line end Y");

    var lineShape = new DesignedTrajectoryShape
    {
        Kind = TrajectoryShapeKind.Line,
        X = 5d,
        Y = 5d,
        Width = 10d,
        RotationAngle = 90d
    };
    var endpoints = TrajectoryGeometryService.GetLineEndpointCoordinates(
        TrajectoryShapeGeometry.FromEditable(DesignedTrajectoryBuilder.ToEditableShape(lineShape)));
    AssertClose(5d, endpoints.StartX, "line start X should stay anchored around center");
    AssertClose(0d, endpoints.StartY, "line start Y should use half length");
    AssertClose(5d, endpoints.EndX, "line end X should stay anchored around center");
    AssertClose(10d, endpoints.EndY, "line end Y should use half length");

    AssertTrue(
        TrajectoryGeometryService.ContainsPoint(0d, 0d, 6d, 8d, 10d),
        "point hit testing should use the supplied distance tolerance");
    AssertTrue(
        TrajectoryGeometryService.ContainsLineSegmentPoint(0d, 0d, 10d, 0d, 5d, 3d, 3d),
        "line hit testing should use the closest point on the segment");
    AssertTrue(
        !TrajectoryGeometryService.ContainsLineSegmentPoint(0d, 0d, 10d, 0d, 5d, 3.1d, 3d),
        "line hit testing should reject points outside the supplied tolerance");
    AssertTrue(
        TrajectoryGeometryService.ContainsLineSegmentPoint(0d, 0d, 0d, 0d, 7d, 0d, 6d, 8d),
        "a zero-length line should retain its larger endpoint hit tolerance");
}

void TestDesignedTrajectoryBuilderReusesSharedGeometryService()
{
    var builder = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Services\DesignedTrajectoryBuilder.cs");
    var control = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(builder, "TrajectoryGeometryService.GenerateInnerPoints", "builder should reuse shared point geometry");
    AssertContains(builder, "TrajectoryGeometryService.GenerateInnerLines", "builder should reuse shared line geometry");
    AssertContains(builder, "TrajectoryGeometryService.GetLineEndpointCoordinates", "builder should reuse shared line endpoint geometry");
    AssertContains(control, "TrajectoryCoordinateMapper.GetTransform", "designer should delegate coordinate fitting to the mapper");
    AssertContains(control, "TrajectoryGeometryService.GenerateLocalInnerPoints", "designer should draw internal points from shared local geometry");
    AssertContains(control, "TrajectoryGeometryService.GenerateLocalInnerLines", "designer should draw internal lines from shared local geometry");
    AssertContains(control, "TrajectoryGeometryService.ContainsPoint", "designer should delegate point distance hit testing");
    AssertContains(control, "TrajectoryGeometryService.ContainsLineSegmentPoint", "designer should delegate line distance hit testing");
    AssertDoesNotContain(control, "private readonly struct CoordinateScreenTransform", "coordinate transform should not remain embedded in the UI code-behind");
}

void TestTrajectoryDesignerIsOwnedBySharedUiAndConsumedByWaferModule()
{
    const string uiControlRelativePath = @"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs";
    const string uiShapeRelativePath = @"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Models\EditableTrajectoryShape.cs";
    var uiProject = ReadRepoFile(@"Core\ReeYin_V.UI\ReeYin_V.UI.csproj");
    var uiGeometry = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Services\TrajectoryShapeGeometry.cs");
    var waferProject = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj");
    var configView = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Views\WaferFlatnessConfigView.xaml");

    AssertTrue(File.Exists(FindRepoPath(uiControlRelativePath)), "shared UI project should own the Skia trajectory designer control");
    AssertTrue(File.Exists(FindRepoPath(uiShapeRelativePath)), "shared UI project should own editable trajectory shapes");
    AssertContains(uiProject, "SkiaSharp.Views.WPF", "shared UI project should own the Skia WPF dependency");
    AssertDoesNotContain(uiGeometry, "DesignedTrajectoryShape", "shared UI geometry must not depend on wafer-specific trajectory plans");
    AssertContains(waferProject, @"Core\ReeYin_V.UI\ReeYin_V.UI.csproj", "wafer module should directly reference the shared UI contract");
    AssertContains(configView, "ReeYin_V.UI.UserControls.TrajectoryDesigner.Views", "wafer configuration preview should use the shared UI control namespace");
    AssertDoesNotContain(configView, "clr-namespace:Custom.WaferFlatnessMeasure.Controls", "wafer configuration preview should not bind to the legacy local control namespace");
}

void TestDesignedTrajectoryResultModelAndBuilderExposeExecutableStructure()
{
    var planModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models\DesignedTrajectoryPlan.cs");
    var builder = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Services\DesignedTrajectoryBuilder.cs");
    var geometry = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Services\TrajectoryGeometryService.cs");

    AssertContains(planModel, "class DesignedTrajectoryPlan", "designer result should expose a top-level plan class");
    AssertContains(planModel, "List<DesignedTrajectoryShape> Shapes", "plan should keep editable shape definitions");
    AssertContains(planModel, "List<DesignedTrajectoryRunStep> RunSteps", "plan should keep flattened executable steps");
    AssertContains(planModel, "CoordinateStartX", "plan should store coordinate start X");
    AssertContains(planModel, "CoordinateEndY", "plan should store coordinate end Y");
    AssertContains(planModel, "DefaultCenterX", "plan should store default center X");
    AssertContains(planModel, "DesignedTrajectoryExecutionMode", "plan should expose execution mode");
    AssertContains(planModel, "DesignedTrajectoryRunStepKind", "run steps should distinguish points and lines");
    AssertContains(planModel, "TrajectoryShapeCategory", "shape definitions should expose point line and region categories");
    AssertContains(planModel, "CategoryText", "shape categories should be visible in UI-friendly result data");

    AssertContains(builder, "class DesignedTrajectoryBuilder", "builder should convert designer shapes into a plan");
    AssertContains(builder, "BuildPlan", "builder should expose a plan build entry point");
    AssertContains(builder, "ToEditableShapes", "builder should restore editable shapes from a saved plan");
    AssertContains(builder, "ToLocusInfos", "builder should convert run steps to existing LocusInfo execution data");
    AssertContains(builder, "EditableTrajectoryShape", "builder should consume the Skia designer shape model");
    AssertContains(geometry, "TrajectoryShapeKind.Circle", "shared geometry should handle circle shapes");
    AssertContains(builder, "TrajectoryShapeKind.Rectangle", "builder should handle rectangle shapes");
    AssertContains(builder, "TrajectoryShapeKind.Arc", "builder should handle arc shapes");
    AssertContains(geometry, "TrajectoryInnerPattern.EquidistantPoints", "shared geometry should emit internal point trajectories");
    AssertContains(builder, "GenerateRegionRunSteps", "region shapes should emit only internal executable trajectories");
    AssertContains(builder, "IsRegionShape", "builder should classify circle and rectangle as region shapes");
    AssertDoesNotContain(builder, "GenerateCircleBoundarySteps", "circle outlines should not become executable line steps");
}

void TestSensorMotionModelConsumesDesignedTrajectoryPlan()
{
    var model = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Models\SensorMotionControlModel.cs");

    AssertContains(model, "DesignedTrajectoryPlan", "motion model should store the designer plan");
    AssertContains(model, "DesignedTrajectoryRunSteps", "motion model should expose run steps for UI preview");
    AssertContains(model, "DesignedTrajectorySummaryText", "motion model should expose a readable plan summary");
    AssertContains(model, "ApplyDesignedTrajectoryPlan", "motion model should synchronize plan results into AllLocusInfo");
    AssertContains(model, "DesignedTrajectoryBuilder.ToLocusInfos", "motion model should reuse the converter for execution data");
    AssertContains(model, "DesignedTrajectoryExecutionMode.Points", "motion model should support point-only plan execution");
    AssertContains(model, "DesignedTrajectoryExecutionMode.Lines", "motion model should support line-based plan execution");
}

void TestWaferFlatnessConfigOpensDesignerEditorInsteadOfOldGenerator()
{
    var view = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Views\WaferFlatnessConfigView.xaml");
    var viewModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\ViewModels\WaferFlatnessConfigViewModel.cs");
    var editorView = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\TrajectoryDesignerEditWindow.xaml");
    var editorViewModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\ViewModels\TrajectoryDesignerEditViewModel.cs");

    AssertContains(view, "EditDesignedTrajectoryCommand", "config view should expose an edit trajectory button");
    AssertContains(view, "ModelParam.DesignedTrajectorySummaryText", "config view should show the designed trajectory summary");
    AssertContains(view, "ModelParam.DesignedTrajectoryRunSteps", "config view should show executable run steps");
    AssertDoesNotContain(view, "GenerateCircleTrajectoryCommand", "config view should no longer use the old inline circle generator button");

    AssertContains(viewModel, "EditDesignedTrajectoryCommand", "config view model should open the editor");
    AssertContains(viewModel, "TrajectoryDesignerEditWindow", "config view model should launch the designer edit window");
    AssertContains(viewModel, "ApplyDesignedTrajectoryPlan", "config view model should apply the returned plan to the motion model");

    AssertContains(editorView, "SkiaTrajectoryDesignerControl", "editor window should host the Skia designer control");
    AssertContains(editorView, "ReturnCommand", "editor window should expose a return/apply command");
    AssertContains(editorView, "FloatingSelectedShapePanel", "editor should keep selected-shape parameters over the coordinate plane");
    AssertContains(editorView, "ShapeTableGrid", "editor should keep the shape table visible at the bottom");

    AssertContains(editorViewModel, "ResultPlan", "editor view model should expose the applied plan result");
    AssertContains(editorViewModel, "DesignedTrajectoryBuilder.BuildPlan", "editor view model should build the result plan on return");
}

void TestWaferFlatnessConfigUsesReadOnlySkiaDesignerPreview()
{
    var view = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Views\WaferFlatnessConfigView.xaml");
    var viewModel = ReadRepoFile(@"CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\ViewModels\WaferFlatnessConfigViewModel.cs");
    var controlXaml = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml");
    var controlCode = ReadRepoFile(@"Core\ReeYin_V.UI\UserControls\TrajectoryDesigner\Views\SkiaTrajectoryDesignerControl.xaml.cs");

    AssertContains(view, "xmlns:trajectoryControls=\"clr-namespace:ReeYin_V.UI.UserControls.TrajectoryDesigner.Views;assembly=ReeYin_V.UI\"", "config view should import the shared Skia trajectory control namespace");
    AssertContains(view, "trajectoryControls:SkiaTrajectoryDesignerControl", "trajectory preview should reuse the Skia trajectory designer control");
    AssertContains(view, "IsPreviewOnly=\"True\"", "config trajectory preview should be read-only");
    AssertContains(view, "Shapes=\"{Binding DesignedTrajectoryPreviewShapes}\"", "config trajectory preview should render designed trajectory shapes");
    AssertContains(view, "DesignedTrajectoryPreviewCoordinateStartX", "preview should bind to the designed trajectory coordinate window");
    AssertDoesNotContain(view, "ItemsSource=\"{Binding TrajectoryShapes}\"", "config preview should not use the old WPF Shape preview list");

    AssertContains(viewModel, "ObservableCollection<EditableTrajectoryShape> DesignedTrajectoryPreviewShapes", "view model should expose Skia preview shapes");
    AssertContains(viewModel, "RefreshDesignedTrajectoryPreview", "view model should refresh the Skia preview after plan changes");
    AssertContains(viewModel, "DesignedTrajectoryBuilder.ToEditableShapes", "preview should reuse the designer shape conversion");
    AssertContains(viewModel, "nameof(SensorMotionControlModel.DesignedTrajectoryPlan)", "preview should refresh when the designed plan changes");

    AssertContains(controlXaml, "x:Name=\"ToolStackPanel\"", "designer control should be able to hide editing tools in preview mode");
    AssertContains(controlCode, "IsPreviewOnlyProperty", "designer control should expose a preview-only dependency property");
    AssertContains(controlCode, "if (IsPreviewOnly)", "designer control should suppress editing mouse interactions in preview mode");
    AssertContains(controlCode, "ToolStackPanel.Visibility", "designer control should hide editing tools in preview mode");
}

void TestWaferFlatnessConfigMarshalsBackgroundTrajectoryRefreshToUiThread()
{
    using var uiReady = new ManualResetEventSlim(false);
    using var refreshCompleted = new ManualResetEventSlim(false);
    Dispatcher? dispatcher = null;
    WaferFlatnessConfigViewModel? viewModel = null;
    SensorMotionControlModel? model = null;
    Exception? uiException = null;

    var uiThread = new Thread(() =>
    {
        try
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            viewModel = new WaferFlatnessConfigViewModel();
            model = new SensorMotionControlModel();
            viewModel.ModelParam = model;

            MethodInfo attachListeners = typeof(WaferFlatnessConfigViewModel).GetMethod(
                "AttachModelListeners",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AttachModelListeners was not found.");
            attachListeners.Invoke(viewModel, null);
            uiReady.Set();
            Dispatcher.Run();
        }
        catch (Exception ex)
        {
            uiException = ex;
            uiReady.Set();
            refreshCompleted.Set();
        }
    });

    uiThread.SetApartmentState(ApartmentState.STA);
    uiThread.Start();
    AssertTrue(uiReady.Wait(TimeSpan.FromSeconds(5)), "UI dispatcher thread should initialize");
    AssertTrue(dispatcher != null && model != null && viewModel != null, "UI test state should initialize");

    Exception? backgroundException = null;
    try
    {
        Task.Run(() => model!.ApplyDesignedTrajectoryPlan(new DesignedTrajectoryPlan
        {
            CoordinateStartX = 0d,
            CoordinateStartY = 0d,
            CoordinateEndX = 100d,
            CoordinateEndY = 100d,
            Shapes = new List<DesignedTrajectoryShape>
            {
                new DesignedTrajectoryShape
                {
                    Kind = TrajectoryShapeKind.Point,
                    X = 20d,
                    Y = 30d
                }
            }
        })).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        backgroundException = ex;
    }

    dispatcher!.BeginInvoke(new Action(() =>
    {
        try
        {
            AssertTrue(
                viewModel!.DesignedTrajectoryPreviewShapes.Count == 1,
                "background plan updates should refresh the preview on the UI thread");
        }
        catch (Exception ex)
        {
            uiException = ex;
        }
        finally
        {
            refreshCompleted.Set();
            dispatcher.BeginInvokeShutdown(DispatcherPriority.Send);
        }
    }), DispatcherPriority.ContextIdle);

    AssertTrue(refreshCompleted.Wait(TimeSpan.FromSeconds(5)), "queued UI refresh should complete");
    AssertTrue(uiThread.Join(TimeSpan.FromSeconds(5)), "UI dispatcher thread should stop");

    if (backgroundException != null)
    {
        throw new InvalidOperationException(
            "background trajectory application should not execute WPF preview code directly",
            backgroundException);
    }

    if (uiException != null)
    {
        throw new InvalidOperationException("UI trajectory refresh failed", uiException);
    }
}

AcsLciFixedDistancePulseXsegParam BuildAcsLciFixedDistancePulseParam(SensorMotionControlModel model, LineSegment segment)
{
    var method = typeof(SensorMotionControlModel).GetMethod(
        "BuildAcsLciFixedDistancePulseParam",
        BindingFlags.Instance | BindingFlags.NonPublic,
        binder: null,
        types: new[] { typeof(Type), typeof(LineSegment) },
        modifiers: null)
        ?? throw new MissingMethodException(nameof(SensorMotionControlModel), "BuildAcsLciFixedDistancePulseParam");

    return (AcsLciFixedDistancePulseXsegParam)(method.Invoke(
        model,
        new object[] { typeof(AcsLciFixedDistancePulseXsegParam), segment })
        ?? throw new InvalidOperationException("ACS LCI parameter builder returned null."));
}

SpeedSetting CreateAcsLciTestMotionProfile()
{
    return new SpeedSetting
    {
        MaxSpeed = 10d,
        AccSpeed = 100d,
        DecSpeed = 100d,
        KillDecSpeed = 1000d,
        Jerk = 1000d
    };
}

PointTemperatureRecord CreateCapturedTemperatureRecord(
    int pointIndex,
    double x,
    double y,
    double d160,
    double d176)
{
    return new PointTemperatureRecord
    {
        PointIndex = pointIndex,
        X = x,
        Y = y,
        Temperatures = new Dictionary<string, double?>(StringComparer.OrdinalIgnoreCase)
        {
            ["D160"] = d160,
            ["D176"] = d176
        }
    };
}

PreprocessDatasetModel CreatePreData(double x, double y, double up, double down, double thickness, double extra)
{
    return new PreprocessDatasetModel
    {
        PosX = x,
        PosY = y,
        UpSurface = up,
        DownSurface = down,
        Thickness = thickness,
        OriginalDataValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            [PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName] = up,
            [PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName] = down,
            ["Channel6.THICKNESS"] = extra
        }
    };
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

void AssertClose(double expected, double actual, string label, double tolerance = 0.000001)
{
    if (double.IsNaN(expected) && double.IsNaN(actual))
    {
        return;
    }

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

bool NearlyEqual(double left, double right, double tolerance = 0.000001)
{
    return Math.Abs(left - right) <= tolerance;
}

void AssertContains(string source, string expected, string label)
{
    AssertTrue(source.Contains(expected, StringComparison.Ordinal), label);
}

string GetStringProperty(object instance, string propertyName)
{
    var property = instance.GetType().GetProperty(propertyName)
        ?? throw new MissingMemberException(instance.GetType().Name, propertyName);
    return property.GetValue(instance)?.ToString() ?? string.Empty;
}

void AssertDoesNotContain(string source, string unexpected, string label)
{
    AssertTrue(!source.Contains(unexpected, StringComparison.Ordinal), label);
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

string FindRepoPath(string relativePath)
{
    for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory != null; directory = directory.Parent)
    {
        string candidate = Path.Combine(directory.FullName, relativePath);
        if (File.Exists(candidate) || Directory.Exists(Path.GetDirectoryName(candidate)))
        {
            return candidate;
        }
    }

    throw new DirectoryNotFoundException($"Repository root was not found for '{relativePath}'.");
}
