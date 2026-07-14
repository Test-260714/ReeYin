#include "pch.h"

#if defined(_WIN32)
#	define U_OS_WINDOWS
#else
#   define U_OS_LINUX
#endif

#ifdef U_OS_WINDOWS
#if defined(_DEBUG)
#	pragma comment(lib, "pcl_commond.lib")
#	pragma comment(lib, "pcl_octreed.lib")
#	pragma comment(lib, "pcl_io_plyd.lib")
#	pragma comment(lib, "pcl_iod.lib")
#	pragma comment(lib, "pcl_kdtreed.lib")
#	pragma comment(lib, "pcl_searchd.lib")
#	pragma comment(lib, "pcl_sample_consensusd.lib")
#	pragma comment(lib, "pcl_filtersd.lib")
#	pragma comment(lib, "pcl_featuresd.lib")
#	pragma comment(lib, "pcl_mld.lib")
#	pragma comment(lib, "pcl_segmentationd.lib")
#	pragma comment(lib, "vtksys-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonCore-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonDataModel-9.4-gd.lib")
#	pragma comment(lib, "vtkCommonExecutionModel-9.4-gd.lib")
#	pragma comment(lib, "vtkIOGeometry-9.4-gd.lib")
#else
#	pragma comment(lib, "pcl_common.lib")
#	pragma comment(lib, "pcl_octree.lib")
#	pragma comment(lib, "pcl_io_ply.lib")
#	pragma comment(lib, "pcl_io.lib")
#	pragma comment(lib, "pcl_kdtree.lib")
#	pragma comment(lib, "pcl_search.lib")
#	pragma comment(lib, "pcl_sample_consensus.lib")
#	pragma comment(lib, "pcl_filters.lib")
#	pragma comment(lib, "pcl_features.lib")
#	pragma comment(lib, "pcl_ml.lib")
#	pragma comment(lib, "pcl_segmentation.lib")
#	pragma comment(lib, "vtksys-9.4.lib")
#	pragma comment(lib, "vtkCommonCore-9.4.lib")
#	pragma comment(lib, "vtkCommonDataModel-9.4.lib")
#	pragma comment(lib, "vtkCommonExecutionModel-9.4.lib")
#	pragma comment(lib, "vtkIOGeometry-9.4.lib")
#endif


#endif // U_OS_WINDOWS
