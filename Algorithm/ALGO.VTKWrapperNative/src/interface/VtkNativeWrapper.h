#pragma once

#ifdef VTKNATIVEWRAPPER_EXPORTS
#define VTK_API __declspec(dllexport)
#else
#define VTK_API __declspec(dllimport)
#endif

extern "C" {
    // ========== RenderWindow ==========
    VTK_API void* VtkRenderWindow_Create();
    VTK_API void VtkRenderWindow_Destroy(void* rw);
    VTK_API void VtkRenderWindow_SetWindowId(void* rw, void* hwnd);
    VTK_API void VtkRenderWindow_SetParentId(void* rw, void* hwnd);
    VTK_API void VtkRenderWindow_SetSize(void* rw, int width, int height);
    VTK_API void VtkRenderWindow_SetNumberOfLayers(void* rw, int layers);
    VTK_API void VtkRenderWindow_AddRenderer(void* rw, void* ren);
    VTK_API void VtkRenderWindow_Render(void* rw);
    VTK_API void VtkRenderWindow_Finalize(void* rw);
    VTK_API void VtkRenderWindow_GetSize(void* rw, int* size);

    // ========== Renderer ==========
    VTK_API void* VtkRenderer_Create();
    VTK_API void VtkRenderer_Destroy(void* ren);
    VTK_API void VtkRenderer_SetBackground(void* ren, double r, double g, double b);
    VTK_API void VtkRenderer_SetBackground2(void* ren, double r, double g, double b);
    VTK_API void VtkRenderer_GradientBackgroundOn(void* ren);
    VTK_API void VtkRenderer_GradientBackgroundOff(void* ren);
    VTK_API void VtkRenderer_AddActor(void* ren, void* actor);
    VTK_API void VtkRenderer_RemoveActor(void* ren, void* actor);
    VTK_API void VtkRenderer_RemoveAllViewProps(void* ren);
    VTK_API void VtkRenderer_ResetCamera(void* ren);
    VTK_API void VtkRenderer_SetLayer(void* ren, int layer);
    VTK_API void VtkRenderer_InteractiveOff(void* ren);
    VTK_API void VtkRenderer_DrawOn(void* ren);
    VTK_API void VtkRenderer_DrawOff(void* ren);
    VTK_API void VtkRenderer_SetPass(void* ren, void* pass);
    VTK_API void VtkRenderer_AddActor2D(void* ren, void* actor);
    VTK_API void VtkRenderer_GetActiveCameraFocalPoint(void* ren, double* xyz);
    VTK_API void VtkRenderer_GetActiveCameraPosition(void* ren, double* xyz);
    VTK_API void VtkRenderer_GetActiveCameraViewUp(void* ren, double* xyz);
    VTK_API void VtkRenderer_WorldToDisplay(void* ren, double x, double y, double z, double* xyz);
    VTK_API void VtkRenderer_SetActiveCamera(void* ren, void* cameraSourceRenderer);
    VTK_API void VtkRenderer_SetActiveCameraPosition(void* ren, double x, double y, double z);
    VTK_API void VtkRenderer_SetActiveCameraFocalPoint(void* ren, double x, double y, double z);
    VTK_API void VtkRenderer_SetActiveCameraViewUp(void* ren, double x, double y, double z);
    VTK_API void VtkRenderer_GetActiveCameraWindowCenter(void* ren, double* xy);
    VTK_API void VtkRenderer_SetActiveCameraWindowCenter(void* ren, double x, double y);
    VTK_API void VtkRenderer_ResetCameraClippingRange(void* ren);
    VTK_API int VtkRenderer_PickPoint(void* ren, int displayX, int displayY, double* xyz);

    // ========== Interactor ==========
    VTK_API void* VtkRenderWindowInteractor_Create();
    VTK_API void VtkRenderWindowInteractor_Destroy(void* iren);
    VTK_API void VtkRenderWindowInteractor_SetRenderWindow(void* iren, void* rw);
    VTK_API void VtkRenderWindowInteractor_SetInteractorStyle(void* iren, void* style);
    VTK_API void VtkRenderWindowInteractor_SetInstallMessageProc(void* iren, int install);
    VTK_API void VtkRenderWindowInteractor_Initialize(void* iren);
    VTK_API void VtkRenderWindowInteractor_Enable(void* iren);
    VTK_API int VtkRenderWindowInteractor_OnSize(void* iren, void* hwnd, unsigned int flags, int x, int y);
    VTK_API int VtkRenderWindowInteractor_OnMouseMove(void* iren, void* hwnd, unsigned int flags, int x, int y);
    VTK_API int VtkRenderWindowInteractor_OnLButtonDown(void* iren, void* hwnd, unsigned int flags, int x, int y, int repeat);
    VTK_API int VtkRenderWindowInteractor_OnLButtonUp(void* iren, void* hwnd, unsigned int flags, int x, int y);
    VTK_API int VtkRenderWindowInteractor_OnMButtonDown(void* iren, void* hwnd, unsigned int flags, int x, int y, int repeat);
    VTK_API int VtkRenderWindowInteractor_OnMButtonUp(void* iren, void* hwnd, unsigned int flags, int x, int y);
    VTK_API int VtkRenderWindowInteractor_OnRButtonDown(void* iren, void* hwnd, unsigned int flags, int x, int y, int repeat);
    VTK_API int VtkRenderWindowInteractor_OnRButtonUp(void* iren, void* hwnd, unsigned int flags, int x, int y);
    VTK_API int VtkRenderWindowInteractor_OnMouseWheelForward(void* iren, void* hwnd, unsigned int flags, int x, int y);
    VTK_API int VtkRenderWindowInteractor_OnMouseWheelBackward(void* iren, void* hwnd, unsigned int flags, int x, int y);
    VTK_API void VtkRenderWindowInteractor_SetEventInformationFlipY(void* iren, int x, int y);
    VTK_API void VtkRenderWindowInteractor_SetEventInformation(void* iren, int x, int y);
    VTK_API void VtkRenderWindowInteractor_InvokeEvent(void* iren, unsigned long event);
    VTK_API void VtkRenderWindowInteractor_MouseMoveEvent(void* iren);
    VTK_API void VtkRenderWindowInteractor_LeftButtonPressEvent(void* iren);
    VTK_API void VtkRenderWindowInteractor_LeftButtonReleaseEvent(void* iren);
    VTK_API void VtkRenderWindowInteractor_MiddleButtonPressEvent(void* iren);
    VTK_API void VtkRenderWindowInteractor_MiddleButtonReleaseEvent(void* iren);
    VTK_API void VtkRenderWindowInteractor_RightButtonPressEvent(void* iren);
    VTK_API void VtkRenderWindowInteractor_RightButtonReleaseEvent(void* iren);
    VTK_API void VtkRenderWindowInteractor_MouseWheelForwardEvent(void* iren);
    VTK_API void VtkRenderWindowInteractor_MouseWheelBackwardEvent(void* iren);
    VTK_API void* VtkInteractorStyleTrackballCamera_Create();
    VTK_API void VtkInteractorStyle_Destroy(void* style);

    // ========== Points ==========
    VTK_API void* VtkPoints_Create();
    VTK_API void VtkPoints_Destroy(void* pts);
    VTK_API void VtkPoints_SetNumberOfPoints(void* pts, int n);
    VTK_API void VtkPoints_SetPoint(void* pts, int id, double x, double y, double z);
    VTK_API void VtkPoints_InsertNextPoint(void* pts, double x, double y, double z);
    // 高效批量设置 - 直接传指针
    VTK_API void VtkPoints_SetData(void* pts, int n, const double* x, const double* y, const double* z);
    VTK_API void VtkPoints_SetDataInterleavedF32(void* pts, int n, const float* xyzInterleaved, int strideBytes);

    // ========== PolyData ==========
    VTK_API void* VtkPolyData_Create();
    VTK_API void VtkPolyData_Destroy(void* pd);
    VTK_API void VtkPolyData_SetPoints(void* pd, void* pts);
    VTK_API void VtkPolyData_SetLines(void* pd, void* lines);
    VTK_API void VtkPolyData_SetPolys(void* pd, void* polys);
    VTK_API void VtkPolyData_SetVerts(void* pd, void* verts);
    VTK_API void* VtkPolyData_GetPointData(void* pd);
    VTK_API void VtkPointData_SetScalars(void* pointData, void* scalars);

    // ========== CellArray ==========
    VTK_API void* VtkCellArray_Create();
    VTK_API void VtkCellArray_Destroy(void* cellArray);
    VTK_API void VtkCellArray_InsertNextCell(void* cellArray, long long pointCount);
    VTK_API void VtkCellArray_InsertCellPoint(void* cellArray, long long pointId);

    // ========== FloatArray ==========
    VTK_API void* VtkFloatArray_Create();
    VTK_API void VtkFloatArray_Destroy(void* arr);
    VTK_API void VtkFloatArray_SetName(void* arr, const char* name);
    VTK_API void VtkFloatArray_SetNumberOfTuples(void* arr, int n);
    VTK_API void VtkFloatArray_SetValue(void* arr, int id, float value);
    // 高效批量设置
    VTK_API void VtkFloatArray_SetData(void* arr, int n, const float* data);
    VTK_API void VtkFloatArray_SetDataFromInterleavedF32Axis(void* arr, int n, const float* xyzInterleaved, int strideBytes, int axis, float* outMin, float* outMax);

    // ========== VertexGlyphFilter ==========
    VTK_API void* VtkVertexGlyphFilter_Create();
    VTK_API void VtkVertexGlyphFilter_Destroy(void* filter);
    VTK_API void VtkVertexGlyphFilter_SetInputData(void* filter, void* pd);
    VTK_API void VtkVertexGlyphFilter_Update(void* filter);
    VTK_API void* VtkVertexGlyphFilter_GetOutput(void* filter);

    // ========== LookupTable ==========
    VTK_API void* VtkLookupTable_Create();
    VTK_API void VtkLookupTable_Destroy(void* lut);
    VTK_API void VtkLookupTable_SetNumberOfTableValues(void* lut, int n);
    VTK_API void VtkLookupTable_SetHueRange(void* lut, double min, double max);
    VTK_API void VtkLookupTable_SetRange(void* lut, double min, double max);
    VTK_API void VtkLookupTable_SetTableValue(void* lut, int index, double r, double g, double b, double a);
    VTK_API void VtkLookupTable_Build(void* lut);

    // ========== SphereSource ==========
    VTK_API void* VtkSphereSource_Create();
    VTK_API void VtkSphereSource_Destroy(void* source);
    VTK_API void VtkSphereSource_SetRadius(void* source, double radius);
    VTK_API void VtkSphereSource_SetThetaResolution(void* source, int resolution);
    VTK_API void VtkSphereSource_SetPhiResolution(void* source, int resolution);
    VTK_API void VtkSphereSource_Update(void* source);
    VTK_API void* VtkSphereSource_GetOutput(void* source);

    // ========== PolyDataMapper ==========
    VTK_API void* VtkPolyDataMapper_Create();
    VTK_API void VtkPolyDataMapper_Destroy(void* mapper);
    VTK_API void VtkPolyDataMapper_SetInputData(void* mapper, void* pd);
    VTK_API void VtkPolyDataMapper_SetInputConnection(void* mapper, void* outputPort);
    VTK_API void VtkPolyDataMapper_SetLookupTable(void* mapper, void* lut);
    VTK_API void VtkPolyDataMapper_SetScalarRange(void* mapper, double min, double max);
    VTK_API void VtkPolyDataMapper_ScalarVisibilityOn(void* mapper);
    VTK_API void VtkPolyDataMapper_ScalarVisibilityOff(void* mapper);

    // ========== Actor ==========
    VTK_API void* VtkActor_Create();
    VTK_API void VtkActor_Destroy(void* actor);
    VTK_API void VtkActor_SetMapper(void* actor, void* mapper);
    VTK_API void VtkActor_SetPointSize(void* actor, float size);
    VTK_API void VtkActor_SetLineWidth(void* actor, float width);
    VTK_API void VtkActor_SetColor(void* actor, double r, double g, double b);
    VTK_API void VtkActor_SetOpacity(void* actor, double opacity);
    VTK_API void VtkActor_SetPosition(void* actor, double x, double y, double z);
    VTK_API void VtkActor_SetScale(void* actor, double x, double y, double z);
    VTK_API void VtkActor_SetVisibility(void* actor, int visible);

    // ========== AxesActor ==========
    VTK_API void* VtkAxesActor_Create();
    VTK_API void VtkAxesActor_Destroy(void* actor);
    VTK_API void VtkAxesActor_SetTotalLength(void* actor, double x, double y, double z);
    VTK_API void VtkAxesActor_SetAxisLabels(void* actor, int enabled);
    VTK_API void VtkAxesActor_SetLabelFontFile(void* actor, const char* fontFile);
    VTK_API void VtkAxesActor_SyncLabelTextColorsWithAxisColors(void* actor);

    // ========== OrientationAxesMarker ==========
    VTK_API void* VtkOrientationAxesMarker_Create();
    VTK_API void VtkOrientationAxesMarker_Destroy(void* marker);

    // ========== ScalarBarActor ==========
    VTK_API void* VtkScalarBarActor_Create();
    VTK_API void VtkScalarBarActor_Destroy(void* actor);
    VTK_API void VtkScalarBarActor_SetLookupTable(void* actor, void* lut);
    VTK_API void VtkScalarBarActor_SetTitle(void* actor, const char* title);
    VTK_API void VtkScalarBarActor_SetNumberOfLabels(void* actor, int n);
    VTK_API void VtkScalarBarActor_SetPosition(void* actor, double x, double y);
    VTK_API void VtkScalarBarActor_SetMaximumWidthInPixels(void* actor, int width);
    VTK_API void VtkScalarBarActor_SetMaximumHeightInPixels(void* actor, int height);
    VTK_API void VtkScalarBarActor_SetUnconstrainedFontSize(void* actor, int enabled);
    VTK_API void VtkScalarBarActor_SetTitleFontSize(void* actor, int fontSize);
    VTK_API void VtkScalarBarActor_SetLabelFontSize(void* actor, int fontSize);
    VTK_API void VtkScalarBarActor_SetTitleFontFamily(void* actor, int family);
    VTK_API void VtkScalarBarActor_SetLabelFontFamily(void* actor, int family);
    VTK_API void VtkScalarBarActor_SetTitleTextStyle(void* actor, int bold, int italic, int shadow);
    VTK_API void VtkScalarBarActor_SetLabelTextStyle(void* actor, int bold, int italic, int shadow);
    VTK_API void VtkScalarBarActor_SetTitleFontFile(void* actor, const char* fontFile);
    VTK_API void VtkScalarBarActor_SetLabelFontFile(void* actor, const char* fontFile);
    VTK_API void VtkScalarBarActor_SetTitleTextColor(void* actor, double r, double g, double b);
    VTK_API void VtkScalarBarActor_SetLabelTextColor(void* actor, double r, double g, double b);
    VTK_API void VtkScalarBarActor_SetTitleTextOpacity(void* actor, double opacity);
    VTK_API void VtkScalarBarActor_SetLabelTextOpacity(void* actor, double opacity);

    // ========== OrientationMarkerWidget ==========
    VTK_API void* VtkOrientationMarkerWidget_Create();
    VTK_API void VtkOrientationMarkerWidget_Destroy(void* widget);
    VTK_API void VtkOrientationMarkerWidget_SetOrientationMarker(void* widget, void* prop);
    VTK_API void VtkOrientationMarkerWidget_SetInteractor(void* widget, void* interactor);
    VTK_API void VtkOrientationMarkerWidget_SetViewport(void* widget, double xmin, double ymin, double xmax, double ymax);
    VTK_API void VtkOrientationMarkerWidget_SetEnabled(void* widget, int enabled);
    VTK_API void VtkOrientationMarkerWidget_SetInteractive(void* widget, int interactive);

    // ========== EDL Pass ==========
    VTK_API void* VtkRenderStepsPass_Create();
    VTK_API void VtkRenderStepsPass_Destroy(void* pass);
    VTK_API void* VtkEDLShading_Create();
    VTK_API void VtkEDLShading_Destroy(void* edl);
    VTK_API void VtkEDLShading_SetDelegatePass(void* edl, void* pass);
    VTK_API void* VtkCloudCompareEDLPass_Create();
    VTK_API void VtkCloudCompareEDLPass_Destroy(void* pass);
    VTK_API void VtkCloudCompareEDLPass_SetDelegatePass(void* pass, void* delegatePass);
    VTK_API void VtkCloudCompareEDLPass_SetStrength(void* pass, float strength);
    VTK_API void VtkCloudCompareEDLPass_SetRadiusScales(void* pass, float perspective, float orthographic);
    VTK_API void VtkCloudCompareEDLPass_SetLightDirection(void* pass, float x, float y, float z);
}
