#include "pch.h"

#if defined(_WIN32)
#	define U_OS_WINDOWS
#else
#   define U_OS_LINUX
#endif

#ifdef U_OS_WINDOWS
#if defined(_DEBUG)
#	pragma comment(lib, "vtksys-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonCore-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonDataModel-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonExecutionModel-9.4-gd.lib")
#	pragma comment(lib, "vtkFiltersGeneral-9.4-gd.lib")
#	pragma comment(lib, "vtkFiltersSources-9.4-gd.lib")
#	pragma comment(lib, "vtkInteractionStyle-9.4-gd.lib")
#	pragma comment(lib, "vtkInteractionWidgets-9.4-gd.lib")
#	pragma comment(lib, "vtkRenderingAnnotation-9.4-gd.lib")
#	pragma comment(lib, "vtkRenderingCore-9.4-gd.lib")
#	pragma comment(lib, "vtkRenderingFreeType-9.4-gd.lib")
#	pragma comment(lib, "vtkRenderingOpenGL2-9.4-gd.lib")
#	pragma comment(lib, "vtkRenderingUI-9.4-gd.lib")
#	pragma comment(lib, "vtkRenderingVolume-9.4-gd.lib")
#	pragma comment(lib, "vtkRenderingVolumeOpenGL2-9.4-gd.lib")
#else
#	pragma comment(lib, "vtksys-9.4.lib")
#	pragma comment(lib, "vtkCommonCore-9.4.lib")
#	pragma comment(lib, "vtkCommonDataModel-9.4.lib")
#	pragma comment(lib, "vtkCommonExecutionModel-9.4.lib")
#	pragma comment(lib, "vtkFiltersGeneral-9.4.lib")
#	pragma comment(lib, "vtkFiltersSources-9.4.lib")
#	pragma comment(lib, "vtkInteractionStyle-9.4.lib")
#	pragma comment(lib, "vtkInteractionWidgets-9.4.lib")
#	pragma comment(lib, "vtkRenderingAnnotation-9.4.lib")
#	pragma comment(lib, "vtkRenderingCore-9.4.lib")
#	pragma comment(lib, "vtkRenderingFreeType-9.4.lib")
#	pragma comment(lib, "vtkRenderingOpenGL2-9.4.lib")
#	pragma comment(lib, "vtkRenderingUI-9.4.lib")
#	pragma comment(lib, "vtkRenderingVolume-9.4.lib")
#	pragma comment(lib, "vtkRenderingVolumeOpenGL2-9.4.lib")
#endif


#endif // U_OS_WINDOWS
