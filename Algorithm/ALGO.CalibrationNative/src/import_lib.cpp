#include "pch.h"
#if defined(_WIN32)
#	define U_OS_WINDOWS
#else
#   define U_OS_LINUX
#endif

#ifdef U_OS_WINDOWS
#if defined(_DEBUG)
#	pragma comment(lib, "ippicvmt.lib")
#	pragma comment(lib, "ippiwd.lib")
#	pragma comment(lib, "ittnotifyd.lib")
#	pragma comment(lib, "zlibd.lib")

#	pragma comment(lib, "IlmImfd.lib")
#	pragma comment(lib, "libjpeg-turbod.lib")
#	pragma comment(lib, "libopenjp2d.lib")
#	pragma comment(lib, "libpngd.lib")
#	pragma comment(lib, "libtiffd.lib")
#	pragma comment(lib, "libwebpd.lib")
#	pragma comment(lib, "opencv_world481d.lib")

#   pragma comment(lib, "ceres-debug.lib")

#else
#	pragma comment(lib, "ippicvmt.lib")
#	pragma comment(lib, "ippiw.lib")
#	pragma comment(lib, "ittnotify.lib")
#	pragma comment(lib, "zlib.lib")

#	pragma comment(lib, "IlmImf.lib")
#	pragma comment(lib, "libjpeg-turbo.lib")
#	pragma comment(lib, "libopenjp2.lib")
#	pragma comment(lib, "libpng.lib")
#	pragma comment(lib, "libtiff.lib")
#	pragma comment(lib, "libwebp.lib")
#	pragma comment(lib, "opencv_world481.lib")

#   pragma comment(lib, "ceres.lib")
#endif


#endif // U_OS_WINDOWS
