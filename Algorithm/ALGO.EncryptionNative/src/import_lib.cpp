#if defined(_WIN32)
#	define U_OS_WINDOWS
#else
#   define U_OS_LINUX
#endif

#ifdef U_OS_WINDOWS
#if defined(_DEBUG)
#	pragma comment(lib, "bit7z.lib")
#else
#	pragma comment(lib, "bit7z.lib")
#endif


#endif // U_OS_WINDOWS