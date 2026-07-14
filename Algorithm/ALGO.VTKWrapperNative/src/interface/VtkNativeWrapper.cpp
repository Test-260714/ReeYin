#include "pch.h"

#include "VtkNativeWrapper.h"
#include "vtkCloudCompareEDLPass.h"

#include <vtkAutoInit.h>
VTK_MODULE_INIT(vtkRenderingOpenGL2);
VTK_MODULE_INIT(vtkRenderingVolumeOpenGL2);
VTK_MODULE_INIT(vtkRenderingFreeType);

#include <vtkWin32OpenGLRenderWindow.h>
#include <vtkWin32RenderWindowInteractor.h>
#include <vtkRenderer.h>
#include <vtkCamera.h>
#include <vtkNew.h>
#include <vtkInteractorStyleTrackballCamera.h>
#include <vtkPoints.h>
#include <vtkCellArray.h>
#include <vtkPolyData.h>
#include <vtkPointData.h>
#include <vtkFloatArray.h>
#include <vtkVertexGlyphFilter.h>
#include <vtkLookupTable.h>
#include <vtkSphereSource.h>
#include <vtkPolyDataMapper.h>
#include <vtkActor.h>
#include <vtkProperty.h>
#include <vtkAxesActor.h>
#include <vtkCaptionActor2D.h>
#include <vtkFollower.h>
#include <vtkScalarBarActor.h>
#include <vtkTextProperty.h>
#include <vtkOrientationMarkerWidget.h>
#include <vtkProp.h>
#include <vtkPropAssembly.h>
#include <vtkPropCollection.h>
#include <vtkRenderStepsPass.h>
#include <vtkEDLShading.h>
#include <vtkRenderPass.h>
#include <vtkActor2D.h>
#include <vtkPointPicker.h>
#include <vtkVectorText.h>
#include <algorithm>
#include <cmath>
#include <cstddef>
#include <limits>

namespace
{
constexpr double kOrientationLabelScale = 0.20;
constexpr double kOrientationLabelOffset = 1.14;
constexpr double kOrientationLabelSideOffset = -0.06;

constexpr double kXAxisColor[3] = { 1.0, 0.33, 0.33 };
constexpr double kYAxisColor[3] = { 0.36, 1.0, 0.36 };
constexpr double kZAxisColor[3] = { 0.25, 0.75, 1.0 };

void SetPropertyColor(vtkProperty* property, const double color[3])
{
    if (!property) {
        return;
    }

    property->SetColor(color[0], color[1], color[2]);
}

void ConfigureOrientationAxesActor(vtkAxesActor* axes)
{
    if (!axes) {
        return;
    }

    axes->SetTotalLength(1.0, 1.0, 1.0);
    axes->SetAxisLabels(false);

    SetPropertyColor(axes->GetXAxisShaftProperty(), kXAxisColor);
    SetPropertyColor(axes->GetXAxisTipProperty(), kXAxisColor);
    SetPropertyColor(axes->GetYAxisShaftProperty(), kYAxisColor);
    SetPropertyColor(axes->GetYAxisTipProperty(), kYAxisColor);
    SetPropertyColor(axes->GetZAxisShaftProperty(), kZAxisColor);
    SetPropertyColor(axes->GetZAxisTipProperty(), kZAxisColor);
}

void AddOrientationAxisLabel(vtkPropAssembly* marker, const char* text, double x, double y, double z, const double color[3])
{
    if (!marker || !text || !text[0]) {
        return;
    }

    vtkNew<vtkVectorText> textSource;
    textSource->SetText(text);
    textSource->Update();

    vtkPolyData* textOutput = textSource->GetOutput();
    if (!textOutput) {
        return;
    }

    vtkNew<vtkPolyData> geometry;
    geometry->ShallowCopy(textOutput);

    double bounds[6] = {};
    geometry->GetBounds(bounds);

    vtkNew<vtkPolyDataMapper> mapper;
    mapper->SetInputData(geometry);
    mapper->ScalarVisibilityOff();

    vtkNew<vtkFollower> actor;
    actor->SetMapper(mapper);
    actor->SetScale(kOrientationLabelScale, kOrientationLabelScale, kOrientationLabelScale);
    actor->SetPosition(
        x - (bounds[0] + (bounds[1] - bounds[0]) * 0.5) * kOrientationLabelScale,
        y - (bounds[2] + (bounds[3] - bounds[2]) * 0.5) * kOrientationLabelScale,
        z);
    actor->GetProperty()->SetColor(color[0], color[1], color[2]);
    actor->GetProperty()->SetLighting(false);

    marker->AddPart(actor);
}

void AssignFollowerCamera(vtkProp* prop, vtkCamera* camera)
{
    if (!prop || !camera) {
        return;
    }

    if (auto* follower = vtkFollower::SafeDownCast(prop)) {
        follower->SetCamera(camera);
        return;
    }

    auto* assembly = vtkPropAssembly::SafeDownCast(prop);
    if (!assembly) {
        return;
    }

    vtkPropCollection* parts = assembly->GetParts();
    if (!parts) {
        return;
    }

    parts->InitTraversal();
    while (vtkProp* child = parts->GetNextProp()) {
        AssignFollowerCamera(child, camera);
    }
}
}

// ========== RenderWindow ==========
void* VtkRenderWindow_Create() { return vtkWin32OpenGLRenderWindow::New(); }
void VtkRenderWindow_Destroy(void* rw) { static_cast<vtkRenderWindow*>(rw)->Delete(); }
void VtkRenderWindow_SetWindowId(void* rw, void* hwnd) { static_cast<vtkWin32OpenGLRenderWindow*>(rw)->SetWindowId(hwnd); }
void VtkRenderWindow_SetParentId(void* rw, void* hwnd) { static_cast<vtkWin32OpenGLRenderWindow*>(rw)->SetParentId(hwnd); }
void VtkRenderWindow_SetSize(void* rw, int w, int h) { static_cast<vtkRenderWindow*>(rw)->SetSize(w, h); }
void VtkRenderWindow_SetNumberOfLayers(void* rw, int layers) { static_cast<vtkRenderWindow*>(rw)->SetNumberOfLayers(layers); }
void VtkRenderWindow_AddRenderer(void* rw, void* ren) { static_cast<vtkRenderWindow*>(rw)->AddRenderer(static_cast<vtkRenderer*>(ren)); }
void VtkRenderWindow_Render(void* rw) { static_cast<vtkRenderWindow*>(rw)->Render(); }
void VtkRenderWindow_Finalize(void* rw) { static_cast<vtkRenderWindow*>(rw)->Finalize(); }
void VtkRenderWindow_GetSize(void* rw, int* size) {
    int* s = static_cast<vtkRenderWindow*>(rw)->GetSize();
    size[0] = s[0];
    size[1] = s[1];
}

// ========== Renderer ==========
void* VtkRenderer_Create() { return vtkRenderer::New(); }
void VtkRenderer_Destroy(void* ren) { static_cast<vtkRenderer*>(ren)->Delete(); }
void VtkRenderer_SetBackground(void* ren, double r, double g, double b) { static_cast<vtkRenderer*>(ren)->SetBackground(r, g, b); }
void VtkRenderer_SetBackground2(void* ren, double r, double g, double b) { static_cast<vtkRenderer*>(ren)->SetBackground2(r, g, b); }
void VtkRenderer_GradientBackgroundOn(void* ren) { static_cast<vtkRenderer*>(ren)->GradientBackgroundOn(); }
void VtkRenderer_GradientBackgroundOff(void* ren) { static_cast<vtkRenderer*>(ren)->GradientBackgroundOff(); }
void VtkRenderer_AddActor(void* ren, void* actor) { static_cast<vtkRenderer*>(ren)->AddActor(static_cast<vtkActor*>(actor)); }
void VtkRenderer_RemoveActor(void* ren, void* actor) { static_cast<vtkRenderer*>(ren)->RemoveActor(static_cast<vtkActor*>(actor)); }
void VtkRenderer_RemoveAllViewProps(void* ren) { static_cast<vtkRenderer*>(ren)->RemoveAllViewProps(); }
void VtkRenderer_ResetCamera(void* ren) { static_cast<vtkRenderer*>(ren)->ResetCamera(); }
void VtkRenderer_SetLayer(void* ren, int layer) { static_cast<vtkRenderer*>(ren)->SetLayer(layer); }
void VtkRenderer_InteractiveOff(void* ren) { static_cast<vtkRenderer*>(ren)->InteractiveOff(); }
void VtkRenderer_DrawOn(void* ren) { static_cast<vtkRenderer*>(ren)->DrawOn(); }
void VtkRenderer_DrawOff(void* ren) { static_cast<vtkRenderer*>(ren)->DrawOff(); }
void VtkRenderer_SetPass(void* ren, void* pass) { static_cast<vtkRenderer*>(ren)->SetPass(static_cast<vtkRenderPass*>(pass)); }
void VtkRenderer_AddActor2D(void* ren, void* actor) { static_cast<vtkRenderer*>(ren)->AddActor2D(static_cast<vtkActor2D*>(actor)); }
void VtkRenderer_GetActiveCameraFocalPoint(void* ren, double* xyz) {
    double* focal = static_cast<vtkRenderer*>(ren)->GetActiveCamera()->GetFocalPoint();
    xyz[0] = focal[0];
    xyz[1] = focal[1];
    xyz[2] = focal[2];
}
void VtkRenderer_GetActiveCameraPosition(void* ren, double* xyz) {
    double* position = static_cast<vtkRenderer*>(ren)->GetActiveCamera()->GetPosition();
    xyz[0] = position[0];
    xyz[1] = position[1];
    xyz[2] = position[2];
}
void VtkRenderer_GetActiveCameraViewUp(void* ren, double* xyz) {
    double* viewUp = static_cast<vtkRenderer*>(ren)->GetActiveCamera()->GetViewUp();
    xyz[0] = viewUp[0];
    xyz[1] = viewUp[1];
    xyz[2] = viewUp[2];
}
void VtkRenderer_WorldToDisplay(void* ren, double x, double y, double z, double* xyz) {
    if (!ren || !xyz) {
        return;
    }

    auto* renderer = static_cast<vtkRenderer*>(ren);
    renderer->SetWorldPoint(x, y, z, 1.0);
    renderer->WorldToDisplay();
    double* display = renderer->GetDisplayPoint();
    xyz[0] = display[0];
    xyz[1] = display[1];
    xyz[2] = display[2];
}
void VtkRenderer_SetActiveCamera(void* ren, void* cameraSourceRenderer) {
    auto renderer = static_cast<vtkRenderer*>(ren);
    auto sourceRenderer = static_cast<vtkRenderer*>(cameraSourceRenderer);
    renderer->SetActiveCamera(sourceRenderer->GetActiveCamera());
}
void VtkRenderer_SetActiveCameraPosition(void* ren, double x, double y, double z) {
    static_cast<vtkRenderer*>(ren)->GetActiveCamera()->SetPosition(x, y, z);
}
void VtkRenderer_SetActiveCameraFocalPoint(void* ren, double x, double y, double z) {
    static_cast<vtkRenderer*>(ren)->GetActiveCamera()->SetFocalPoint(x, y, z);
}
void VtkRenderer_SetActiveCameraViewUp(void* ren, double x, double y, double z) {
    static_cast<vtkRenderer*>(ren)->GetActiveCamera()->SetViewUp(x, y, z);
}
void VtkRenderer_GetActiveCameraWindowCenter(void* ren, double* xy) {
    if (!ren || !xy) {
        return;
    }

    double* center = static_cast<vtkRenderer*>(ren)->GetActiveCamera()->GetWindowCenter();
    xy[0] = center[0];
    xy[1] = center[1];
}
void VtkRenderer_SetActiveCameraWindowCenter(void* ren, double x, double y) {
    static_cast<vtkRenderer*>(ren)->GetActiveCamera()->SetWindowCenter(x, y);
}
void VtkRenderer_ResetCameraClippingRange(void* ren) {
    static_cast<vtkRenderer*>(ren)->ResetCameraClippingRange();
}
int VtkRenderer_PickPoint(void* ren, int displayX, int displayY, double* xyz) {
    if (!ren || !xyz) {
        return 0;
    }

    vtkNew<vtkPointPicker> picker;
    picker->SetTolerance(0.01);

    if (!picker->Pick(displayX, displayY, 0.0, static_cast<vtkRenderer*>(ren))) {
        return 0;
    }

    if (picker->GetPointId() < 0) {
        return 0;
    }

    double* p = picker->GetPickPosition();
    xyz[0] = p[0];
    xyz[1] = p[1];
    xyz[2] = p[2];
    return 1;
}

// ========== Interactor ==========
void* VtkRenderWindowInteractor_Create() { return vtkWin32RenderWindowInteractor::New(); }
void VtkRenderWindowInteractor_Destroy(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->Delete(); }
void VtkRenderWindowInteractor_SetRenderWindow(void* iren, void* rw) { static_cast<vtkRenderWindowInteractor*>(iren)->SetRenderWindow(static_cast<vtkRenderWindow*>(rw)); }
void VtkRenderWindowInteractor_SetInteractorStyle(void* iren, void* style) { static_cast<vtkRenderWindowInteractor*>(iren)->SetInteractorStyle(static_cast<vtkInteractorStyle*>(style)); }
void VtkRenderWindowInteractor_SetInstallMessageProc(void* iren, int install) { static_cast<vtkWin32RenderWindowInteractor*>(iren)->SetInstallMessageProc(install); }
void VtkRenderWindowInteractor_Initialize(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->Initialize(); }
void VtkRenderWindowInteractor_Enable(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->Enable(); }
int VtkRenderWindowInteractor_OnSize(void* iren, void* hwnd, unsigned int flags, int x, int y) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnSize(static_cast<HWND>(hwnd), flags, x, y); }
int VtkRenderWindowInteractor_OnMouseMove(void* iren, void* hwnd, unsigned int flags, int x, int y) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnMouseMove(static_cast<HWND>(hwnd), flags, x, y); }
int VtkRenderWindowInteractor_OnLButtonDown(void* iren, void* hwnd, unsigned int flags, int x, int y, int repeat) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnLButtonDown(static_cast<HWND>(hwnd), flags, x, y, repeat); }
int VtkRenderWindowInteractor_OnLButtonUp(void* iren, void* hwnd, unsigned int flags, int x, int y) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnLButtonUp(static_cast<HWND>(hwnd), flags, x, y); }
int VtkRenderWindowInteractor_OnMButtonDown(void* iren, void* hwnd, unsigned int flags, int x, int y, int repeat) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnMButtonDown(static_cast<HWND>(hwnd), flags, x, y, repeat); }
int VtkRenderWindowInteractor_OnMButtonUp(void* iren, void* hwnd, unsigned int flags, int x, int y) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnMButtonUp(static_cast<HWND>(hwnd), flags, x, y); }
int VtkRenderWindowInteractor_OnRButtonDown(void* iren, void* hwnd, unsigned int flags, int x, int y, int repeat) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnRButtonDown(static_cast<HWND>(hwnd), flags, x, y, repeat); }
int VtkRenderWindowInteractor_OnRButtonUp(void* iren, void* hwnd, unsigned int flags, int x, int y) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnRButtonUp(static_cast<HWND>(hwnd), flags, x, y); }
int VtkRenderWindowInteractor_OnMouseWheelForward(void* iren, void* hwnd, unsigned int flags, int x, int y) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnMouseWheelForward(static_cast<HWND>(hwnd), flags, x, y); }
int VtkRenderWindowInteractor_OnMouseWheelBackward(void* iren, void* hwnd, unsigned int flags, int x, int y) { return static_cast<vtkWin32RenderWindowInteractor*>(iren)->OnMouseWheelBackward(static_cast<HWND>(hwnd), flags, x, y); }
void VtkRenderWindowInteractor_SetEventInformationFlipY(void* iren, int x, int y) { static_cast<vtkRenderWindowInteractor*>(iren)->SetEventInformationFlipY(x, y); }
void VtkRenderWindowInteractor_SetEventInformation(void* iren, int x, int y) { static_cast<vtkRenderWindowInteractor*>(iren)->SetEventInformation(x, y); }
void VtkRenderWindowInteractor_InvokeEvent(void* iren, unsigned long evt) { static_cast<vtkRenderWindowInteractor*>(iren)->InvokeEvent(evt); }
void VtkRenderWindowInteractor_MouseMoveEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->MouseMoveEvent(); }
void VtkRenderWindowInteractor_LeftButtonPressEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->LeftButtonPressEvent(); }
void VtkRenderWindowInteractor_LeftButtonReleaseEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->LeftButtonReleaseEvent(); }
void VtkRenderWindowInteractor_MiddleButtonPressEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->MiddleButtonPressEvent(); }
void VtkRenderWindowInteractor_MiddleButtonReleaseEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->MiddleButtonReleaseEvent(); }
void VtkRenderWindowInteractor_RightButtonPressEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->RightButtonPressEvent(); }
void VtkRenderWindowInteractor_RightButtonReleaseEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->RightButtonReleaseEvent(); }
void VtkRenderWindowInteractor_MouseWheelForwardEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->MouseWheelForwardEvent(); }
void VtkRenderWindowInteractor_MouseWheelBackwardEvent(void* iren) { static_cast<vtkRenderWindowInteractor*>(iren)->MouseWheelBackwardEvent(); }
void* VtkInteractorStyleTrackballCamera_Create() { return vtkInteractorStyleTrackballCamera::New(); }
void VtkInteractorStyle_Destroy(void* style) { static_cast<vtkInteractorStyle*>(style)->Delete(); }

// ========== Points ==========
void* VtkPoints_Create() { return vtkPoints::New(); }
void VtkPoints_Destroy(void* pts) { static_cast<vtkPoints*>(pts)->Delete(); }
void VtkPoints_SetNumberOfPoints(void* pts, int n) { static_cast<vtkPoints*>(pts)->SetNumberOfPoints(n); }
void VtkPoints_SetPoint(void* pts, int id, double x, double y, double z) { static_cast<vtkPoints*>(pts)->SetPoint(id, x, y, z); }
void VtkPoints_InsertNextPoint(void* pts, double x, double y, double z) { static_cast<vtkPoints*>(pts)->InsertNextPoint(x, y, z); }

// 高效批量设置
void VtkPoints_SetData(void* pts, int n, const double* x, const double* y, const double* z) {
    auto points = static_cast<vtkPoints*>(pts);
    points->SetNumberOfPoints(n);
    for (int i = 0; i < n; ++i) {
        points->SetPoint(i, x[i], y[i], z[i]);
    }
}

void VtkPoints_SetDataInterleavedF32(void* pts, int n, const float* xyzInterleaved, int strideBytes) {
    auto* points = static_cast<vtkPoints*>(pts);
    if (!points) {
        return;
    }

    if (n <= 0 || xyzInterleaved == nullptr) {
        points->SetNumberOfPoints(0);
        return;
    }

    if (strideBytes <= 0) {
        strideBytes = static_cast<int>(sizeof(float) * 3);
    }

    points->SetNumberOfPoints(n);
    const unsigned char* base = reinterpret_cast<const unsigned char*>(xyzInterleaved);
    for (int i = 0; i < n; ++i) {
        const auto* point = reinterpret_cast<const float*>(base + static_cast<std::size_t>(i) * static_cast<std::size_t>(strideBytes));
        points->SetPoint(i, static_cast<double>(point[0]), static_cast<double>(point[1]), static_cast<double>(point[2]));
    }
}

// ========== PolyData ==========
void* VtkPolyData_Create() { return vtkPolyData::New(); }
void VtkPolyData_Destroy(void* pd) { static_cast<vtkPolyData*>(pd)->Delete(); }
void VtkPolyData_SetPoints(void* pd, void* pts) { static_cast<vtkPolyData*>(pd)->SetPoints(static_cast<vtkPoints*>(pts)); }
void VtkPolyData_SetLines(void* pd, void* lines) { static_cast<vtkPolyData*>(pd)->SetLines(static_cast<vtkCellArray*>(lines)); }
void VtkPolyData_SetPolys(void* pd, void* polys) { static_cast<vtkPolyData*>(pd)->SetPolys(static_cast<vtkCellArray*>(polys)); }
void VtkPolyData_SetVerts(void* pd, void* verts) { static_cast<vtkPolyData*>(pd)->SetVerts(static_cast<vtkCellArray*>(verts)); }
void* VtkPolyData_GetPointData(void* pd) { return static_cast<vtkPolyData*>(pd)->GetPointData(); }
void VtkPointData_SetScalars(void* pointData, void* scalars) { static_cast<vtkPointData*>(pointData)->SetScalars(static_cast<vtkDataArray*>(scalars)); }

// ========== CellArray ==========
void* VtkCellArray_Create() { return vtkCellArray::New(); }
void VtkCellArray_Destroy(void* cellArray) { static_cast<vtkCellArray*>(cellArray)->Delete(); }
void VtkCellArray_InsertNextCell(void* cellArray, long long pointCount) { static_cast<vtkCellArray*>(cellArray)->InsertNextCell(pointCount); }
void VtkCellArray_InsertCellPoint(void* cellArray, long long pointId) { static_cast<vtkCellArray*>(cellArray)->InsertCellPoint(pointId); }

// ========== FloatArray ==========
void* VtkFloatArray_Create() { return vtkFloatArray::New(); }
void VtkFloatArray_Destroy(void* arr) { static_cast<vtkFloatArray*>(arr)->Delete(); }
void VtkFloatArray_SetName(void* arr, const char* name) { static_cast<vtkFloatArray*>(arr)->SetName(name); }
void VtkFloatArray_SetNumberOfTuples(void* arr, int n) { static_cast<vtkFloatArray*>(arr)->SetNumberOfTuples(n); }
void VtkFloatArray_SetValue(void* arr, int id, float value) { static_cast<vtkFloatArray*>(arr)->SetValue(id, value); }
void VtkFloatArray_SetData(void* arr, int n, const float* data) {
    auto floatArr = static_cast<vtkFloatArray*>(arr);
    floatArr->SetNumberOfTuples(n);
    for (int i = 0; i < n; ++i) floatArr->SetValue(i, data[i]);
}

void VtkFloatArray_SetDataFromInterleavedF32Axis(void* arr, int n, const float* xyzInterleaved, int strideBytes, int axis, float* outMin, float* outMax) {
    auto* floatArr = static_cast<vtkFloatArray*>(arr);
    if (!floatArr) {
        return;
    }

    if (axis < 0 || axis > 2) {
        axis = 2;
    }

    if (n <= 0 || xyzInterleaved == nullptr) {
        floatArr->SetNumberOfTuples(0);
        if (outMin != nullptr) {
            *outMin = 0.0f;
        }
        if (outMax != nullptr) {
            *outMax = 1.0f;
        }
        return;
    }

    if (strideBytes <= 0) {
        strideBytes = static_cast<int>(sizeof(float) * 3);
    }

    floatArr->SetNumberOfTuples(n);

    const unsigned char* base = reinterpret_cast<const unsigned char*>(xyzInterleaved);
    float minValue = std::numeric_limits<float>::infinity();
    float maxValue = -std::numeric_limits<float>::infinity();
    float lastFiniteValue = 0.0f;
    bool hasFiniteValue = false;

    for (int i = 0; i < n; ++i) {
        const auto* point = reinterpret_cast<const float*>(base + static_cast<std::size_t>(i) * static_cast<std::size_t>(strideBytes));
        float value = point[axis];
        if (!std::isfinite(value)) {
            value = hasFiniteValue ? lastFiniteValue : 0.0f;
        } else {
            lastFiniteValue = value;
            hasFiniteValue = true;
            if (value < minValue) {
                minValue = value;
            }
            if (value > maxValue) {
                maxValue = value;
            }
        }

        floatArr->SetValue(i, value);
    }

    if (!hasFiniteValue) {
        minValue = 0.0f;
        maxValue = 1.0f;
    } else if (maxValue <= minValue) {
        float epsilon = std::max(1e-6f, std::abs(minValue) * 1e-6f);
        minValue -= epsilon;
        maxValue += epsilon;
    }

    if (outMin != nullptr) {
        *outMin = minValue;
    }
    if (outMax != nullptr) {
        *outMax = maxValue;
    }
}

// ========== VertexGlyphFilter ==========
void* VtkVertexGlyphFilter_Create() { return vtkVertexGlyphFilter::New(); }
void VtkVertexGlyphFilter_Destroy(void* filter) { static_cast<vtkVertexGlyphFilter*>(filter)->Delete(); }
void VtkVertexGlyphFilter_SetInputData(void* filter, void* pd) { static_cast<vtkVertexGlyphFilter*>(filter)->SetInputData(static_cast<vtkPolyData*>(pd)); }
void VtkVertexGlyphFilter_Update(void* filter) { static_cast<vtkVertexGlyphFilter*>(filter)->Update(); }
void* VtkVertexGlyphFilter_GetOutput(void* filter) { return static_cast<vtkVertexGlyphFilter*>(filter)->GetOutput(); }

// ========== LookupTable ==========
void* VtkLookupTable_Create() { return vtkLookupTable::New(); }
void VtkLookupTable_Destroy(void* lut) { static_cast<vtkLookupTable*>(lut)->Delete(); }
void VtkLookupTable_SetNumberOfTableValues(void* lut, int n) { static_cast<vtkLookupTable*>(lut)->SetNumberOfTableValues(n); }
void VtkLookupTable_SetHueRange(void* lut, double min, double max) { static_cast<vtkLookupTable*>(lut)->SetHueRange(min, max); }
void VtkLookupTable_SetRange(void* lut, double min, double max) { static_cast<vtkLookupTable*>(lut)->SetRange(min, max); }
void VtkLookupTable_SetTableValue(void* lut, int index, double r, double g, double b, double a) {
    static_cast<vtkLookupTable*>(lut)->SetTableValue(index, r, g, b, a);
}
void VtkLookupTable_Build(void* lut) { static_cast<vtkLookupTable*>(lut)->Build(); }

// ========== SphereSource ==========
void* VtkSphereSource_Create() { return vtkSphereSource::New(); }
void VtkSphereSource_Destroy(void* source) { static_cast<vtkSphereSource*>(source)->Delete(); }
void VtkSphereSource_SetRadius(void* source, double radius) { static_cast<vtkSphereSource*>(source)->SetRadius(radius); }
void VtkSphereSource_SetThetaResolution(void* source, int resolution) { static_cast<vtkSphereSource*>(source)->SetThetaResolution(resolution); }
void VtkSphereSource_SetPhiResolution(void* source, int resolution) { static_cast<vtkSphereSource*>(source)->SetPhiResolution(resolution); }
void VtkSphereSource_Update(void* source) { static_cast<vtkSphereSource*>(source)->Update(); }
void* VtkSphereSource_GetOutput(void* source) { return static_cast<vtkSphereSource*>(source)->GetOutput(); }

// ========== PolyDataMapper ==========
void* VtkPolyDataMapper_Create() { return vtkPolyDataMapper::New(); }
void VtkPolyDataMapper_Destroy(void* mapper) { static_cast<vtkPolyDataMapper*>(mapper)->Delete(); }
void VtkPolyDataMapper_SetInputData(void* mapper, void* pd) { static_cast<vtkPolyDataMapper*>(mapper)->SetInputData(static_cast<vtkPolyData*>(pd)); }
void VtkPolyDataMapper_SetInputConnection(void* mapper, void* port) { static_cast<vtkPolyDataMapper*>(mapper)->SetInputConnection(static_cast<vtkAlgorithmOutput*>(port)); }
void VtkPolyDataMapper_SetLookupTable(void* mapper, void* lut) { static_cast<vtkPolyDataMapper*>(mapper)->SetLookupTable(static_cast<vtkLookupTable*>(lut)); }
void VtkPolyDataMapper_SetScalarRange(void* mapper, double min, double max) { static_cast<vtkPolyDataMapper*>(mapper)->SetScalarRange(min, max); }
void VtkPolyDataMapper_ScalarVisibilityOn(void* mapper) { static_cast<vtkPolyDataMapper*>(mapper)->ScalarVisibilityOn(); }
void VtkPolyDataMapper_ScalarVisibilityOff(void* mapper) { static_cast<vtkPolyDataMapper*>(mapper)->ScalarVisibilityOff(); }

// ========== Actor ==========
void* VtkActor_Create() { return vtkActor::New(); }
void VtkActor_Destroy(void* actor) { static_cast<vtkActor*>(actor)->Delete(); }
void VtkActor_SetMapper(void* actor, void* mapper) { static_cast<vtkActor*>(actor)->SetMapper(static_cast<vtkMapper*>(mapper)); }
void VtkActor_SetPointSize(void* actor, float size) { static_cast<vtkActor*>(actor)->GetProperty()->SetPointSize(size); }
void VtkActor_SetLineWidth(void* actor, float width) { static_cast<vtkActor*>(actor)->GetProperty()->SetLineWidth(width); }
void VtkActor_SetColor(void* actor, double r, double g, double b) { static_cast<vtkActor*>(actor)->GetProperty()->SetColor(r, g, b); }
void VtkActor_SetOpacity(void* actor, double opacity) { static_cast<vtkActor*>(actor)->GetProperty()->SetOpacity(opacity); }
void VtkActor_SetPosition(void* actor, double x, double y, double z) { static_cast<vtkActor*>(actor)->SetPosition(x, y, z); }
void VtkActor_SetScale(void* actor, double x, double y, double z) { static_cast<vtkActor*>(actor)->SetScale(x, y, z); }
void VtkActor_SetVisibility(void* actor, int visible) { static_cast<vtkActor*>(actor)->SetVisibility(visible != 0); }

// ========== AxesActor ==========
void* VtkAxesActor_Create() { return vtkAxesActor::New(); }
void VtkAxesActor_Destroy(void* actor) { static_cast<vtkAxesActor*>(actor)->Delete(); }
void VtkAxesActor_SetTotalLength(void* actor, double x, double y, double z) { static_cast<vtkAxesActor*>(actor)->SetTotalLength(x, y, z); }
void VtkAxesActor_SetAxisLabels(void* actor, int enabled) { static_cast<vtkAxesActor*>(actor)->SetAxisLabels(enabled != 0); }
void VtkAxesActor_SetLabelFontFile(void* actor, const char* fontFile) {
    if (!fontFile || !fontFile[0]) {
        return;
    }

    auto applyFont = [fontFile](vtkCaptionActor2D* caption) {
        if (!caption) {
            return;
        }
        auto* text = caption->GetCaptionTextProperty();
        if (!text) {
            return;
        }
        text->SetFontFamilyAsString("File");
        text->SetFontFile(fontFile);
        text->SetBold(false);
        text->SetItalic(false);
        text->SetShadow(false);
    };

    auto* axes = static_cast<vtkAxesActor*>(actor);
    applyFont(axes->GetXAxisCaptionActor2D());
    applyFont(axes->GetYAxisCaptionActor2D());
    applyFont(axes->GetZAxisCaptionActor2D());
}
void VtkAxesActor_SyncLabelTextColorsWithAxisColors(void* actor) {
    auto* axes = static_cast<vtkAxesActor*>(actor);

    auto applyColor = [](vtkCaptionActor2D* caption, vtkProperty* axisProperty) {
        if (!caption || !axisProperty) {
            return;
        }

        auto* text = caption->GetCaptionTextProperty();
        if (!text) {
            return;
        }

        double color[3];
        axisProperty->GetColor(color);
        text->SetColor(color[0], color[1], color[2]);
    };

    applyColor(axes->GetXAxisCaptionActor2D(), axes->GetXAxisShaftProperty());
    applyColor(axes->GetYAxisCaptionActor2D(), axes->GetYAxisShaftProperty());
    applyColor(axes->GetZAxisCaptionActor2D(), axes->GetZAxisShaftProperty());
}

// ========== OrientationAxesMarker ==========
void* VtkOrientationAxesMarker_Create() {
    auto* marker = vtkPropAssembly::New();

    vtkNew<vtkAxesActor> axes;
    ConfigureOrientationAxesActor(axes);
    marker->AddPart(axes);

    AddOrientationAxisLabel(
        marker,
        "X",
        kOrientationLabelOffset,
        kOrientationLabelSideOffset,
        kOrientationLabelSideOffset,
        kXAxisColor);
    AddOrientationAxisLabel(
        marker,
        "Y",
        kOrientationLabelSideOffset,
        kOrientationLabelOffset,
        kOrientationLabelSideOffset,
        kYAxisColor);
    AddOrientationAxisLabel(
        marker,
        "Z",
        kOrientationLabelSideOffset,
        kOrientationLabelSideOffset,
        kOrientationLabelOffset,
        kZAxisColor);

    return marker;
}
void VtkOrientationAxesMarker_Destroy(void* marker) {
    if (!marker) {
        return;
    }

    static_cast<vtkPropAssembly*>(marker)->Delete();
}

// ========== ScalarBarActor ==========
void* VtkScalarBarActor_Create() { return vtkScalarBarActor::New(); }
void VtkScalarBarActor_Destroy(void* actor) { static_cast<vtkScalarBarActor*>(actor)->Delete(); }
void VtkScalarBarActor_SetLookupTable(void* actor, void* lut) { static_cast<vtkScalarBarActor*>(actor)->SetLookupTable(static_cast<vtkLookupTable*>(lut)); }
void VtkScalarBarActor_SetTitle(void* actor, const char* title) { static_cast<vtkScalarBarActor*>(actor)->SetTitle(title); }
void VtkScalarBarActor_SetNumberOfLabels(void* actor, int n) { static_cast<vtkScalarBarActor*>(actor)->SetNumberOfLabels(n); }
void VtkScalarBarActor_SetPosition(void* actor, double x, double y) { static_cast<vtkScalarBarActor*>(actor)->SetPosition(x, y); }
void VtkScalarBarActor_SetMaximumWidthInPixels(void* actor, int width) { static_cast<vtkScalarBarActor*>(actor)->SetMaximumWidthInPixels(width); }
void VtkScalarBarActor_SetMaximumHeightInPixels(void* actor, int height) { static_cast<vtkScalarBarActor*>(actor)->SetMaximumHeightInPixels(height); }
void VtkScalarBarActor_SetUnconstrainedFontSize(void* actor, int enabled) { static_cast<vtkScalarBarActor*>(actor)->SetUnconstrainedFontSize(enabled != 0); }
void VtkScalarBarActor_SetTitleFontSize(void* actor, int fontSize) { static_cast<vtkScalarBarActor*>(actor)->GetTitleTextProperty()->SetFontSize(fontSize); }
void VtkScalarBarActor_SetLabelFontSize(void* actor, int fontSize) { static_cast<vtkScalarBarActor*>(actor)->GetLabelTextProperty()->SetFontSize(fontSize); }
void VtkScalarBarActor_SetTitleFontFamily(void* actor, int family) { static_cast<vtkScalarBarActor*>(actor)->GetTitleTextProperty()->SetFontFamily(family); }
void VtkScalarBarActor_SetLabelFontFamily(void* actor, int family) { static_cast<vtkScalarBarActor*>(actor)->GetLabelTextProperty()->SetFontFamily(family); }
void VtkScalarBarActor_SetTitleTextStyle(void* actor, int bold, int italic, int shadow) {
    auto* text = static_cast<vtkScalarBarActor*>(actor)->GetTitleTextProperty();
    text->SetBold(bold != 0);
    text->SetItalic(italic != 0);
    text->SetShadow(shadow != 0);
}
void VtkScalarBarActor_SetLabelTextStyle(void* actor, int bold, int italic, int shadow) {
    auto* text = static_cast<vtkScalarBarActor*>(actor)->GetLabelTextProperty();
    text->SetBold(bold != 0);
    text->SetItalic(italic != 0);
    text->SetShadow(shadow != 0);
}
void VtkScalarBarActor_SetTitleFontFile(void* actor, const char* fontFile) {
    if (!fontFile || !fontFile[0]) {
        return;
    }
    auto* text = static_cast<vtkScalarBarActor*>(actor)->GetTitleTextProperty();
    text->SetFontFamilyAsString("File");
    text->SetFontFile(fontFile);
}
void VtkScalarBarActor_SetLabelFontFile(void* actor, const char* fontFile) {
    if (!fontFile || !fontFile[0]) {
        return;
    }
    auto* text = static_cast<vtkScalarBarActor*>(actor)->GetLabelTextProperty();
    text->SetFontFamilyAsString("File");
    text->SetFontFile(fontFile);
}
void VtkScalarBarActor_SetTitleTextColor(void* actor, double r, double g, double b) {
    static_cast<vtkScalarBarActor*>(actor)->GetTitleTextProperty()->SetColor(r, g, b);
}
void VtkScalarBarActor_SetLabelTextColor(void* actor, double r, double g, double b) {
    static_cast<vtkScalarBarActor*>(actor)->GetLabelTextProperty()->SetColor(r, g, b);
}
void VtkScalarBarActor_SetTitleTextOpacity(void* actor, double opacity) {
    static_cast<vtkScalarBarActor*>(actor)->GetTitleTextProperty()->SetOpacity(opacity);
}
void VtkScalarBarActor_SetLabelTextOpacity(void* actor, double opacity) {
    static_cast<vtkScalarBarActor*>(actor)->GetLabelTextProperty()->SetOpacity(opacity);
}

// ========== OrientationMarkerWidget ==========
void* VtkOrientationMarkerWidget_Create() { return vtkOrientationMarkerWidget::New(); }
void VtkOrientationMarkerWidget_Destroy(void* widget) { static_cast<vtkOrientationMarkerWidget*>(widget)->Delete(); }
void VtkOrientationMarkerWidget_SetOrientationMarker(void* widget, void* prop) {
    auto* orientationWidget = static_cast<vtkOrientationMarkerWidget*>(widget);
    auto* markerProp = static_cast<vtkProp*>(prop);
    orientationWidget->SetOrientationMarker(markerProp);

    if (auto* markerRenderer = orientationWidget->GetRenderer()) {
        AssignFollowerCamera(markerProp, markerRenderer->GetActiveCamera());
    }
}
void VtkOrientationMarkerWidget_SetInteractor(void* widget, void* interactor) {
    static_cast<vtkOrientationMarkerWidget*>(widget)->SetInteractor(static_cast<vtkRenderWindowInteractor*>(interactor));
}
void VtkOrientationMarkerWidget_SetViewport(void* widget, double xmin, double ymin, double xmax, double ymax) {
    static_cast<vtkOrientationMarkerWidget*>(widget)->SetViewport(xmin, ymin, xmax, ymax);
}
void VtkOrientationMarkerWidget_SetEnabled(void* widget, int enabled) {
    static_cast<vtkOrientationMarkerWidget*>(widget)->SetEnabled(enabled != 0);
}
void VtkOrientationMarkerWidget_SetInteractive(void* widget, int interactive) {
    static_cast<vtkOrientationMarkerWidget*>(widget)->SetInteractive(interactive != 0);
}

// ========== EDL ==========
void* VtkRenderStepsPass_Create() { return vtkRenderStepsPass::New(); }
void VtkRenderStepsPass_Destroy(void* pass) { static_cast<vtkRenderStepsPass*>(pass)->Delete(); }
void* VtkEDLShading_Create() { return vtkEDLShading::New(); }
void VtkEDLShading_Destroy(void* edl) { static_cast<vtkEDLShading*>(edl)->Delete(); }
void VtkEDLShading_SetDelegatePass(void* edl, void* pass) { static_cast<vtkEDLShading*>(edl)->SetDelegatePass(static_cast<vtkRenderPass*>(pass)); }
void* VtkCloudCompareEDLPass_Create() { return vtkCloudCompareEDLPass::New(); }
void VtkCloudCompareEDLPass_Destroy(void* pass) { static_cast<vtkCloudCompareEDLPass*>(pass)->Delete(); }
void VtkCloudCompareEDLPass_SetDelegatePass(void* pass, void* delegatePass) {
    static_cast<vtkCloudCompareEDLPass*>(pass)->SetDelegatePass(static_cast<vtkRenderPass*>(delegatePass));
}
void VtkCloudCompareEDLPass_SetStrength(void* pass, float strength) {
    static_cast<vtkCloudCompareEDLPass*>(pass)->SetStrength(strength);
}
void VtkCloudCompareEDLPass_SetRadiusScales(void* pass, float perspective, float orthographic) {
    auto* edl = static_cast<vtkCloudCompareEDLPass*>(pass);
    edl->SetRadiusPerspective(perspective);
    edl->SetRadiusOrthographic(orthographic);
}
void VtkCloudCompareEDLPass_SetLightDirection(void* pass, float x, float y, float z) {
    static_cast<vtkCloudCompareEDLPass*>(pass)->SetLightDir(x, y, z);
}
