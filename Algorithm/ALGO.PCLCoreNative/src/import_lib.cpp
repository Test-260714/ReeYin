#include "pch.h"

#if defined(_WIN32)
#	define U_OS_WINDOWS
#else
#   define U_OS_LINUX
#endif

#ifdef U_OS_WINDOWS
#if defined(_DEBUG)
#	pragma comment(lib, "pcl_iod.lib")
#	pragma comment(lib, "pcl_io_plyd.lib")
#	pragma comment(lib, "pcl_commond.lib")
#	pragma comment(lib, "vtksys-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonCore-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonDataModel-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonExecutionModel-9.4-gd.lib")
#	pragma comment(lib, "vtkIOGeometry-9.4-gd.lib")
#	pragma comment(lib, "vtkIOImage-9.4-gd.lib")
#	pragma comment(lib, "vtktiff-9.4-gd.lib")
#else
#	pragma comment(lib, "pcl_io.lib")
#	pragma comment(lib, "pcl_io_ply.lib")
#	pragma comment(lib, "pcl_common.lib")
#	pragma comment(lib, "vtksys-9.4.lib")
#	pragma comment(lib, "vtkCommonCore-9.4.lib")
#	pragma comment(lib, "vtkCommonDataModel-9.4.lib")
#	pragma comment(lib, "vtkCommonExecutionModel-9.4.lib")
#	pragma comment(lib, "vtkIOGeometry-9.4.lib")
#	pragma comment(lib, "vtkIOImage-9.4.lib")
#	pragma comment(lib, "vtktiff-9.4.lib")
#endif


#endif // U_OS_WINDOWS
