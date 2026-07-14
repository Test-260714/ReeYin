#pragma once

#include <cstddef>

#ifdef _WIN32
#ifdef ALGOENCRYPTIONNATIVE_EXPORTS
#define ALGOENCRYPTIONNATIVE_API __declspec(dllexport)
#else
#define ALGOENCRYPTIONNATIVE_API __declspec(dllimport)
#endif
#else
#define ALGOENCRYPTIONNATIVE_API __attribute__((visibility("default")))
#endif

extern "C"
{
	ALGOENCRYPTIONNATIVE_API void File7zSetDllPath(const char* dllPath);

	// 判断文件是否被压缩(加密)
	ALGOENCRYPTIONNATIVE_API int File7zIsCompressed(const char* filePath, int& isCompressed);

	ALGOENCRYPTIONNATIVE_API int File7zIsCompressed2(const char* filePath, int& isCompressed);

	// 文件加密压缩
	ALGOENCRYPTIONNATIVE_API int File7zArchive(const char* filePath, const char* archivePath, bool encryptHeader = true);

	// 缓冲区加密压缩
	ALGOENCRYPTIONNATIVE_API int Buffer7zArchive(const unsigned char* buffer, size_t bufferSize, const char* innerName, const char* archivePath,
		                                         bool encryptHeader = true);

	// 文件解压
	ALGOENCRYPTIONNATIVE_API int File7zExtract(const char* archivePath, unsigned char** buffer, size_t* bufferSize);

	// 清空缓存
	ALGOENCRYPTIONNATIVE_API void File7zFreeBuffer(unsigned char* buffer);


}