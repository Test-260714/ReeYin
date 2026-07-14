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
#endif

// link cuda
#pragma comment(lib, "cuda.lib")
#pragma comment(lib, "cudart.lib")
#pragma comment(lib, "cublas.lib")

// link TensorRT
#pragma comment(lib, "nvinfer_10.lib")
#pragma comment(lib, "nvinfer_plugin_10.lib")
#pragma comment(lib, "nvonnxparser_10.lib")

// link OpenVINO
#if defined(_DEBUG)
#pragma comment(lib, "openvinod.lib")
#pragma comment(lib, "openvino_cd.lib")
#pragma comment(lib, "openvino_onnx_frontendd.lib")
#else
#pragma comment(lib, "openvino.lib")
#pragma comment(lib, "openvino_c.lib")
#pragma comment(lib, "openvino_onnx_frontend.lib")
#endif

#if defined(_DEBUG)
#	pragma comment(lib, "ALGO.EncryptionNative.lib")
#else
#	pragma comment(lib, "ALGO.EncryptionNative.lib")
#endif


#endif // U_OS_WINDOWS
