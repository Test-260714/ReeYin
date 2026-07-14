using System.Runtime.InteropServices;

namespace PointCloud.VTKWPF.Native;

internal static class VtkNativeMethods
{
    private const string DllName = "ALGO.VTKWrapperNative.dll";

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr RenderWindow_Create();

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_Destroy(IntPtr renderWindow);

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_SetWindowId", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_SetWindowId(IntPtr renderWindow, IntPtr hwnd);

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_SetParentId", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_SetParentId(IntPtr renderWindow, IntPtr hwnd);

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_SetSize", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_SetSize(IntPtr renderWindow, int width, int height);

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_SetNumberOfLayers", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_SetNumberOfLayers(IntPtr renderWindow, int layers);

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_AddRenderer", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_AddRenderer(IntPtr renderWindow, IntPtr renderer);

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_Render", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_Render(IntPtr renderWindow);

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_Finalize", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_Finalize(IntPtr renderWindow);

    [DllImport(DllName, EntryPoint = "VtkRenderWindow_GetSize", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindow_GetSize(IntPtr renderWindow, [Out] int[] size);

    [DllImport(DllName, EntryPoint = "VtkRenderer_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Renderer_Create();

    [DllImport(DllName, EntryPoint = "VtkRenderer_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_Destroy(IntPtr renderer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_SetBackground", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_SetBackground(IntPtr renderer, double r, double g, double b);

    [DllImport(DllName, EntryPoint = "VtkRenderer_SetBackground2", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_SetBackground2(IntPtr renderer, double r, double g, double b);

    [DllImport(DllName, EntryPoint = "VtkRenderer_GradientBackgroundOn", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_GradientBackgroundOn(IntPtr renderer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_GradientBackgroundOff", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_GradientBackgroundOff(IntPtr renderer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_AddActor", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_AddActor(IntPtr renderer, IntPtr actor);

    [DllImport(DllName, EntryPoint = "VtkRenderer_RemoveActor", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_RemoveActor(IntPtr renderer, IntPtr actor);

    [DllImport(DllName, EntryPoint = "VtkRenderer_RemoveAllViewProps", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_RemoveAllViewProps(IntPtr renderer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_ResetCamera", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_ResetCamera(IntPtr renderer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_SetLayer", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_SetLayer(IntPtr renderer, int layer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_InteractiveOff", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_InteractiveOff(IntPtr renderer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_GetActiveCameraFocalPoint", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_GetActiveCameraFocalPoint(IntPtr renderer, [Out] double[] xyz);

    [DllImport(DllName, EntryPoint = "VtkRenderer_GetActiveCameraPosition", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_GetActiveCameraPosition(IntPtr renderer, [Out] double[] xyz);

    [DllImport(DllName, EntryPoint = "VtkRenderer_GetActiveCameraViewUp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_GetActiveCameraViewUp(IntPtr renderer, [Out] double[] xyz);

    [DllImport(DllName, EntryPoint = "VtkRenderer_SetActiveCameraPosition", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_SetActiveCameraPosition(IntPtr renderer, double x, double y, double z);

    [DllImport(DllName, EntryPoint = "VtkRenderer_SetActiveCameraFocalPoint", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_SetActiveCameraFocalPoint(IntPtr renderer, double x, double y, double z);

    [DllImport(DllName, EntryPoint = "VtkRenderer_SetActiveCameraViewUp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_SetActiveCameraViewUp(IntPtr renderer, double x, double y, double z);

    [DllImport(DllName, EntryPoint = "VtkRenderer_ResetCameraClippingRange", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_ResetCameraClippingRange(IntPtr renderer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_AddActor2D", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_AddActor2D(IntPtr renderer, IntPtr actor);

    [DllImport(DllName, EntryPoint = "VtkRenderer_SetActiveCamera", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_SetActiveCamera(IntPtr renderer, IntPtr cameraSourceRenderer);

    [DllImport(DllName, EntryPoint = "VtkRenderer_SetPass", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Renderer_SetPass(IntPtr renderer, IntPtr pass);

    [DllImport(DllName, EntryPoint = "VtkRenderer_PickPoint", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Renderer_PickPoint(IntPtr renderer, int displayX, int displayY, [Out] double[] xyz);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr RenderWindowInteractor_Create();

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_Destroy(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_SetRenderWindow", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_SetRenderWindow(IntPtr interactor, IntPtr renderWindow);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_SetInteractorStyle", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_SetInteractorStyle(IntPtr interactor, IntPtr style);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_SetInstallMessageProc", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_SetInstallMessageProc(IntPtr interactor, int install);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_Initialize", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_Initialize(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_Enable", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_Enable(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnSize", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnSize(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnMouseMove", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnMouseMove(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnLButtonDown", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnLButtonDown(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y, int repeat);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnLButtonUp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnLButtonUp(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnMButtonDown", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnMButtonDown(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y, int repeat);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnMButtonUp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnMButtonUp(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnRButtonDown", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnRButtonDown(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y, int repeat);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnRButtonUp", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnRButtonUp(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnMouseWheelForward", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnMouseWheelForward(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_OnMouseWheelBackward", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int RenderWindowInteractor_OnMouseWheelBackward(IntPtr interactor, IntPtr hwnd, uint flags, int x, int y);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_SetEventInformationFlipY", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_SetEventInformationFlipY(IntPtr interactor, int x, int y);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_MouseMoveEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_MouseMoveEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_LeftButtonPressEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_LeftButtonPressEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_LeftButtonReleaseEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_LeftButtonReleaseEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_MiddleButtonPressEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_MiddleButtonPressEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_MiddleButtonReleaseEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_MiddleButtonReleaseEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_RightButtonPressEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_RightButtonPressEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_RightButtonReleaseEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_RightButtonReleaseEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_MouseWheelForwardEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_MouseWheelForwardEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkRenderWindowInteractor_MouseWheelBackwardEvent", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderWindowInteractor_MouseWheelBackwardEvent(IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkInteractorStyleTrackballCamera_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr InteractorStyleTrackballCamera_Create();

    [DllImport(DllName, EntryPoint = "VtkInteractorStyle_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void InteractorStyle_Destroy(IntPtr style);

    [DllImport(DllName, EntryPoint = "VtkPoints_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Points_Create();

    [DllImport(DllName, EntryPoint = "VtkPoints_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Points_Destroy(IntPtr points);

    [DllImport(DllName, EntryPoint = "VtkPoints_SetNumberOfPoints", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Points_SetNumberOfPoints(IntPtr points, int count);

    [DllImport(DllName, EntryPoint = "VtkPoints_SetPoint", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Points_SetPoint(IntPtr points, int id, double x, double y, double z);

    [DllImport(DllName, EntryPoint = "VtkPoints_SetDataInterleavedF32", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Points_SetDataInterleavedF32(IntPtr points, int count, IntPtr interleavedBuffer, int strideBytes);

    [DllImport(DllName, EntryPoint = "VtkPolyData_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr PolyData_Create();

    [DllImport(DllName, EntryPoint = "VtkPolyData_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyData_Destroy(IntPtr polyData);

    [DllImport(DllName, EntryPoint = "VtkPolyData_SetPoints", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyData_SetPoints(IntPtr polyData, IntPtr points);

    [DllImport(DllName, EntryPoint = "VtkPolyData_SetLines", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyData_SetLines(IntPtr polyData, IntPtr lines);

    [DllImport(DllName, EntryPoint = "VtkPolyData_SetPolys", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyData_SetPolys(IntPtr polyData, IntPtr polys);

    [DllImport(DllName, EntryPoint = "VtkPolyData_SetVerts", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyData_SetVerts(IntPtr polyData, IntPtr verts);

    [DllImport(DllName, EntryPoint = "VtkPolyData_GetPointData", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr PolyData_GetPointData(IntPtr polyData);

    [DllImport(DllName, EntryPoint = "VtkPointData_SetScalars", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PointData_SetScalars(IntPtr pointData, IntPtr scalars);

    [DllImport(DllName, EntryPoint = "VtkCellArray_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr CellArray_Create();

    [DllImport(DllName, EntryPoint = "VtkCellArray_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CellArray_Destroy(IntPtr cellArray);

    [DllImport(DllName, EntryPoint = "VtkCellArray_InsertNextCell", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CellArray_InsertNextCell(IntPtr cellArray, long pointCount);

    [DllImport(DllName, EntryPoint = "VtkCellArray_InsertCellPoint", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CellArray_InsertCellPoint(IntPtr cellArray, long pointId);

    [DllImport(DllName, EntryPoint = "VtkFloatArray_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr FloatArray_Create();

    [DllImport(DllName, EntryPoint = "VtkFloatArray_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FloatArray_Destroy(IntPtr array);

    [DllImport(DllName, EntryPoint = "VtkFloatArray_SetName", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern void FloatArray_SetName(IntPtr array, [MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(DllName, EntryPoint = "VtkFloatArray_SetDataFromInterleavedF32Axis", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FloatArray_SetDataFromInterleavedF32Axis(
        IntPtr array,
        int count,
        IntPtr interleavedBuffer,
        int strideBytes,
        int axis,
        out float minValue,
        out float maxValue);

    [DllImport(DllName, EntryPoint = "VtkVertexGlyphFilter_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr VertexGlyphFilter_Create();

    [DllImport(DllName, EntryPoint = "VtkVertexGlyphFilter_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void VertexGlyphFilter_Destroy(IntPtr filter);

    [DllImport(DllName, EntryPoint = "VtkVertexGlyphFilter_SetInputData", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void VertexGlyphFilter_SetInputData(IntPtr filter, IntPtr polyData);

    [DllImport(DllName, EntryPoint = "VtkVertexGlyphFilter_Update", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void VertexGlyphFilter_Update(IntPtr filter);

    [DllImport(DllName, EntryPoint = "VtkVertexGlyphFilter_GetOutput", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr VertexGlyphFilter_GetOutput(IntPtr filter);

    [DllImport(DllName, EntryPoint = "VtkLookupTable_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr LookupTable_Create();

    [DllImport(DllName, EntryPoint = "VtkLookupTable_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void LookupTable_Destroy(IntPtr lookupTable);

    [DllImport(DllName, EntryPoint = "VtkLookupTable_SetNumberOfTableValues", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void LookupTable_SetNumberOfTableValues(IntPtr lookupTable, int count);

    [DllImport(DllName, EntryPoint = "VtkLookupTable_SetHueRange", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void LookupTable_SetHueRange(IntPtr lookupTable, double minHue, double maxHue);

    [DllImport(DllName, EntryPoint = "VtkLookupTable_SetRange", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void LookupTable_SetRange(IntPtr lookupTable, double minValue, double maxValue);

    [DllImport(DllName, EntryPoint = "VtkLookupTable_SetTableValue", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void LookupTable_SetTableValue(IntPtr lookupTable, int index, double r, double g, double b, double a);

    [DllImport(DllName, EntryPoint = "VtkLookupTable_Build", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void LookupTable_Build(IntPtr lookupTable);

    [DllImport(DllName, EntryPoint = "VtkSphereSource_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr SphereSource_Create();

    [DllImport(DllName, EntryPoint = "VtkSphereSource_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SphereSource_Destroy(IntPtr source);

    [DllImport(DllName, EntryPoint = "VtkSphereSource_SetRadius", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SphereSource_SetRadius(IntPtr source, double radius);

    [DllImport(DllName, EntryPoint = "VtkSphereSource_SetThetaResolution", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SphereSource_SetThetaResolution(IntPtr source, int resolution);

    [DllImport(DllName, EntryPoint = "VtkSphereSource_SetPhiResolution", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SphereSource_SetPhiResolution(IntPtr source, int resolution);

    [DllImport(DllName, EntryPoint = "VtkSphereSource_Update", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SphereSource_Update(IntPtr source);

    [DllImport(DllName, EntryPoint = "VtkSphereSource_GetOutput", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr SphereSource_GetOutput(IntPtr source);

    [DllImport(DllName, EntryPoint = "VtkPolyDataMapper_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr PolyDataMapper_Create();

    [DllImport(DllName, EntryPoint = "VtkPolyDataMapper_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyDataMapper_Destroy(IntPtr mapper);

    [DllImport(DllName, EntryPoint = "VtkPolyDataMapper_SetInputData", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyDataMapper_SetInputData(IntPtr mapper, IntPtr polyData);

    [DllImport(DllName, EntryPoint = "VtkPolyDataMapper_SetLookupTable", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyDataMapper_SetLookupTable(IntPtr mapper, IntPtr lookupTable);

    [DllImport(DllName, EntryPoint = "VtkPolyDataMapper_SetScalarRange", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyDataMapper_SetScalarRange(IntPtr mapper, double minValue, double maxValue);

    [DllImport(DllName, EntryPoint = "VtkPolyDataMapper_ScalarVisibilityOn", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyDataMapper_ScalarVisibilityOn(IntPtr mapper);

    [DllImport(DllName, EntryPoint = "VtkPolyDataMapper_ScalarVisibilityOff", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PolyDataMapper_ScalarVisibilityOff(IntPtr mapper);

    [DllImport(DllName, EntryPoint = "VtkActor_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Actor_Create();

    [DllImport(DllName, EntryPoint = "VtkActor_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_Destroy(IntPtr actor);

    [DllImport(DllName, EntryPoint = "VtkActor_SetMapper", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_SetMapper(IntPtr actor, IntPtr mapper);

    [DllImport(DllName, EntryPoint = "VtkActor_SetPointSize", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_SetPointSize(IntPtr actor, float size);

    [DllImport(DllName, EntryPoint = "VtkActor_SetLineWidth", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_SetLineWidth(IntPtr actor, float width);

    [DllImport(DllName, EntryPoint = "VtkActor_SetColor", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_SetColor(IntPtr actor, double r, double g, double b);

    [DllImport(DllName, EntryPoint = "VtkActor_SetOpacity", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_SetOpacity(IntPtr actor, double opacity);

    [DllImport(DllName, EntryPoint = "VtkActor_SetPosition", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_SetPosition(IntPtr actor, double x, double y, double z);

    [DllImport(DllName, EntryPoint = "VtkActor_SetScale", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_SetScale(IntPtr actor, double x, double y, double z);

    [DllImport(DllName, EntryPoint = "VtkActor_SetVisibility", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Actor_SetVisibility(IntPtr actor, int visible);

    [DllImport(DllName, EntryPoint = "VtkAxesActor_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr AxesActor_Create();

    [DllImport(DllName, EntryPoint = "VtkAxesActor_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void AxesActor_Destroy(IntPtr actor);

    [DllImport(DllName, EntryPoint = "VtkAxesActor_SetTotalLength", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void AxesActor_SetTotalLength(IntPtr actor, double x, double y, double z);

    [DllImport(DllName, EntryPoint = "VtkAxesActor_SetAxisLabels", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void AxesActor_SetAxisLabels(IntPtr actor, int enabled);

    [DllImport(DllName, EntryPoint = "VtkAxesActor_SetLabelFontFile", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern void AxesActor_SetLabelFontFile(IntPtr actor, [MarshalAs(UnmanagedType.LPStr)] string fontFile);

    [DllImport(DllName, EntryPoint = "VtkAxesActor_SyncLabelTextColorsWithAxisColors", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void AxesActor_SyncLabelTextColorsWithAxisColors(IntPtr actor);

    [DllImport(DllName, EntryPoint = "VtkOrientationAxesMarker_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr OrientationAxesMarker_Create();

    [DllImport(DllName, EntryPoint = "VtkOrientationAxesMarker_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OrientationAxesMarker_Destroy(IntPtr marker);

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr ScalarBarActor_Create();

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ScalarBarActor_Destroy(IntPtr actor);

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_SetLookupTable", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ScalarBarActor_SetLookupTable(IntPtr actor, IntPtr lookupTable);

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_SetTitle", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern void ScalarBarActor_SetTitle(IntPtr actor, [MarshalAs(UnmanagedType.LPStr)] string title);

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_SetNumberOfLabels", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ScalarBarActor_SetNumberOfLabels(IntPtr actor, int count);

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_SetPosition", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ScalarBarActor_SetPosition(IntPtr actor, double x, double y);

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_SetMaximumWidthInPixels", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ScalarBarActor_SetMaximumWidthInPixels(IntPtr actor, int width);

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_SetMaximumHeightInPixels", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ScalarBarActor_SetMaximumHeightInPixels(IntPtr actor, int height);

    [DllImport(DllName, EntryPoint = "VtkScalarBarActor_SetUnconstrainedFontSize", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void ScalarBarActor_SetUnconstrainedFontSize(IntPtr actor, int enabled);

    [DllImport(DllName, EntryPoint = "VtkOrientationMarkerWidget_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr OrientationMarkerWidget_Create();

    [DllImport(DllName, EntryPoint = "VtkOrientationMarkerWidget_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OrientationMarkerWidget_Destroy(IntPtr widget);

    [DllImport(DllName, EntryPoint = "VtkOrientationMarkerWidget_SetOrientationMarker", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OrientationMarkerWidget_SetOrientationMarker(IntPtr widget, IntPtr prop);

    [DllImport(DllName, EntryPoint = "VtkOrientationMarkerWidget_SetInteractor", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OrientationMarkerWidget_SetInteractor(IntPtr widget, IntPtr interactor);

    [DllImport(DllName, EntryPoint = "VtkOrientationMarkerWidget_SetViewport", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OrientationMarkerWidget_SetViewport(IntPtr widget, double xmin, double ymin, double xmax, double ymax);

    [DllImport(DllName, EntryPoint = "VtkOrientationMarkerWidget_SetEnabled", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OrientationMarkerWidget_SetEnabled(IntPtr widget, int enabled);

    [DllImport(DllName, EntryPoint = "VtkOrientationMarkerWidget_SetInteractive", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void OrientationMarkerWidget_SetInteractive(IntPtr widget, int interactive);

    [DllImport(DllName, EntryPoint = "VtkRenderStepsPass_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr RenderStepsPass_Create();

    [DllImport(DllName, EntryPoint = "VtkRenderStepsPass_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void RenderStepsPass_Destroy(IntPtr pass);

    [DllImport(DllName, EntryPoint = "VtkCloudCompareEDLPass_Create", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr CloudCompareEdlPass_Create();

    [DllImport(DllName, EntryPoint = "VtkCloudCompareEDLPass_Destroy", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CloudCompareEdlPass_Destroy(IntPtr pass);

    [DllImport(DllName, EntryPoint = "VtkCloudCompareEDLPass_SetDelegatePass", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CloudCompareEdlPass_SetDelegatePass(IntPtr pass, IntPtr delegatePass);

    [DllImport(DllName, EntryPoint = "VtkCloudCompareEDLPass_SetStrength", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CloudCompareEdlPass_SetStrength(IntPtr pass, float strength);

    [DllImport(DllName, EntryPoint = "VtkCloudCompareEDLPass_SetRadiusScales", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CloudCompareEdlPass_SetRadiusScales(IntPtr pass, float perspective, float orthographic);

    [DllImport(DllName, EntryPoint = "VtkCloudCompareEDLPass_SetLightDirection", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void CloudCompareEdlPass_SetLightDirection(IntPtr pass, float x, float y, float z);
}
