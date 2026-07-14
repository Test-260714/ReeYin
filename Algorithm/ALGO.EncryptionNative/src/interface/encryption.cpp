#include "pch.h"

#include "encryption.h"

#include <bit7z/bit7zlibrary.hpp>
#include <bit7z/bitarchivereader.hpp>
#include <bit7z/bitfilecompressor.hpp>
#include <bit7z/bitmemcompressor.hpp>
#include <bit7z/bitfileextractor.hpp>
#include <bit7z/bitformat.hpp>
#include <string>
#include <vector>
#include <optional>
#include <iostream>
#include <fstream>
#include <array>
#include <cstdlib>
#include <cstring>
#include <filesystem>

#ifdef _WIN32
#include <Windows.h>
#endif

#ifndef ALGOENCRYPTIONNATIVE_DEFAULT_7Z_PATH
#ifdef _WIN32
#define ALGOENCRYPTIONNATIVE_DEFAULT_7Z_PATH "./7z.dll"
#else
#define ALGOENCRYPTIONNATIVE_DEFAULT_7Z_PATH "/usr/lib/7zip/7z.so"
#endif
#endif

static std::string g_7zDllPath;


#ifdef _WIN32
static std::string AnsiToUtf8(const std::string& ansiStr)
{
    if (ansiStr.empty())
    {
        return std::string();
    }

    int wideLen = MultiByteToWideChar(CP_ACP, 0, ansiStr.c_str(), -1, nullptr, 0);
    if (wideLen <= 0)
    {
        return ansiStr;
    }

    std::wstring wideStr(wideLen, L'\0');
    MultiByteToWideChar(CP_ACP, 0, ansiStr.c_str(), -1, &wideStr[0], wideLen);

    int utf8Len = WideCharToMultiByte(CP_UTF8, 0, wideStr.c_str(), -1, nullptr, 0, nullptr, nullptr);
    if (utf8Len <= 0)
    {
        return ansiStr;
    }

    std::string utf8Str(utf8Len, '\0');
    WideCharToMultiByte(CP_UTF8, 0, wideStr.c_str(), -1, &utf8Str[0], utf8Len, nullptr, nullptr);

    utf8Str.resize(strlen(utf8Str.c_str()));
    return utf8Str;
}
#else
static std::string AnsiToUtf8(const std::string& str)
{
    return str;
}
#endif


extern "C"
{
    ALGOENCRYPTIONNATIVE_API void File7zSetDllPath(const char* dllPath) 
    {
        g_7zDllPath = dllPath ? dllPath : "";
    }

    ALGOENCRYPTIONNATIVE_API int File7zArchive(const char* filePath, const char* archivePath, bool encryptHeader)
    {
        namespace fs = std::filesystem;

        try
        {
            std::string password = "reechi@2025-1114";

            if (filePath == nullptr || archivePath == nullptr)
            {
                return -1;
            }
            
            std::string srcPath(filePath);
            if (!fs::exists(srcPath) || !fs::is_regular_file(srcPath))
            {
                return -1;
            }

            std::string dllPath = g_7zDllPath.empty() ? std::string(ALGOENCRYPTIONNATIVE_DEFAULT_7Z_PATH) : g_7zDllPath;
            bit7z::Bit7zLibrary lib{ AnsiToUtf8(dllPath) };

            bit7z::BitFileCompressor compressor{ lib, bit7z::BitFormat::SevenZip };

            compressor.setCompressionLevel(bit7z::BitCompressionLevel::Fastest);

            compressor.setPassword(password, encryptHeader);

            std::string srcPathUtf8 = AnsiToUtf8(srcPath);
            std::vector<std::string> files{ srcPathUtf8 };

            std::string dstPath(archivePath);
            if (fs::exists(dstPath))
            {
                std::error_code ec;
                bool removed = fs::remove(dstPath, ec);
                if (!removed || ec)
                {
                    return -2;
                }
            }

            std::string dstPathUtf8 = AnsiToUtf8(dstPath);
            compressor.compress(files, dstPathUtf8);

            std::error_code ec;
            bool removed = fs::remove(srcPath, ec);
            if (!removed || ec) 
            {
                return -2;
            }

            return 0;
        }
        catch (std::exception ex)
        {
            std::cout << "File7zArchive exception:" << ex.what() << std::endl;

            return -1;
        }
    }

    ALGOENCRYPTIONNATIVE_API int Buffer7zArchive(const unsigned char* buffer, size_t bufferSize,
                                                 const char* innerName, const char* archivePath,
                                                 bool encryptHeader)
    {
        try 
        {
            if (!buffer || bufferSize == 0 || !archivePath) 
                return -1;

            std::string password = "reechi@2025-1114";

            std::string dllPath = g_7zDllPath.empty() ? std::string(ALGOENCRYPTIONNATIVE_DEFAULT_7Z_PATH) : g_7zDllPath;
            bit7z::Bit7zLibrary lib{ AnsiToUtf8(dllPath) };

            bit7z::BitMemCompressor compressor{ lib, bit7z::BitFormat::SevenZip };
            compressor.setCompressionLevel(bit7z::BitCompressionLevel::Fastest);
            compressor.setPassword(password, encryptHeader);

            std::vector<bit7z::byte_t> inFile(buffer, buffer + bufferSize);

            std::string dstPathUtf8 = AnsiToUtf8(std::string(archivePath));
            std::string nameInArchive = (innerName && innerName[0] != '\0') ? innerName : "model.onnx";

            compressor.compressFile(inFile, dstPathUtf8, AnsiToUtf8(nameInArchive));

            return 0;
        }
        catch (std::exception ex)
        {
            std::cout << "File7zArchive exception:" << ex.what() << std::endl;

            return -1;
        }
    }


    ALGOENCRYPTIONNATIVE_API int File7zExtract(const char* archivePath, unsigned char** buffer, size_t* bufferSize)
    {
        namespace fs = std::filesystem;

        if (buffer == nullptr || bufferSize == nullptr || archivePath == nullptr)
        {
            return -1;
        }

        *buffer = nullptr;
        *bufferSize = 0;

        try 
        {
            std::string password = "reechi@2025-1114";

            if (!fs::exists(archivePath)) 
            {
                return -1;
            }

            std::string dllPath = g_7zDllPath.empty() ? std::string(ALGOENCRYPTIONNATIVE_DEFAULT_7Z_PATH) : g_7zDllPath;
            bit7z::Bit7zLibrary lib{ AnsiToUtf8(dllPath) };

            bit7z::BitFileExtractor extractor{ lib, bit7z::BitFormat::SevenZip };

            extractor.setPassword(password);

            std::string archivePathUtf8 = AnsiToUtf8(std::string(archivePath));
            std::vector<bit7z::byte_t> tmp;
            extractor.extract(archivePathUtf8, tmp);

            if (tmp.empty()) 
            {
                return -1;
            }

            size_t size = tmp.size();
            unsigned char* buf = static_cast<unsigned char*>(std::malloc(size));
            if (!buf) 
            {
                return -1;
            }

            std::memcpy(buf, tmp.data(), size);

            *buffer = buf;
            *bufferSize = size;

            return 0;
        }
        catch (std::exception ex)
        {
            std::cout << "File7zExtract exception:" << ex.what() << std::endl;

            return -1;
        }
    }


    ALGOENCRYPTIONNATIVE_API void File7zFreeBuffer(unsigned char* buffer)
    {
        if (buffer) 
        {
            std::free(buffer);

            buffer = nullptr;
        }
    }


    ALGOENCRYPTIONNATIVE_API int File7zIsCompressed(const char* filePath, int& isCompressed)
    {
        namespace fs = std::filesystem;

        isCompressed = 0;

        try
        {
            std::string path(filePath);

            if (!fs::exists(path) || !fs::is_regular_file(path))
            {
                return -1;
            }

            std::ifstream ifs(path, std::ios::binary);
            if (!ifs)
            {
                return -1;
            }

            std::array<unsigned char, 6> sig{};
            ifs.read(reinterpret_cast<char*>(sig.data()), sig.size());
            if (ifs.gcount() != static_cast<std::streamsize>(sig.size()))
            {
                isCompressed = 0;
                return 0;
            }

            // 7z Ä§Êý£º37 7A BC AF 27 1C
            static const std::array<unsigned char, 6> k7zSig = 
            {
                0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C
            };

            if (sig == k7zSig)
            {
                isCompressed = 1;
            }
            else
            {
                isCompressed = 0;
            }

            return 0;
        }
        catch (std::exception ex)
        {
            std::cout << "File7zIsCompressed exception:" << ex.what() << std::endl;

            return -1;
        }
    }


    ALGOENCRYPTIONNATIVE_API int File7zIsCompressed2(const char* filePath, int& isCompressed)
    {
        namespace fs = std::filesystem;

        isCompressed = 0;

        std::string password = "reechi@2025-1114";

        try
        {
            std::string path(filePath);

            if (!fs::exists(path) || !fs::is_regular_file(path))
            {
                return -1;
            }

            std::string dllPath = g_7zDllPath.empty() ? std::string(ALGOENCRYPTIONNATIVE_DEFAULT_7Z_PATH) : g_7zDllPath;
            bit7z::Bit7zLibrary lib{ AnsiToUtf8(dllPath) };

            try 
            {
                std::string pathUtf8 = AnsiToUtf8(path);
                bit7z::BitArchiveReader reader{ lib, pathUtf8, bit7z::BitFormat::SevenZip, password };

                isCompressed = 1;
                return 0;
            }
            catch (const bit7z::BitException&) 
            {
                isCompressed = 0;
                return 0;
            }
        }
        catch (std::exception ex)
        {
            std::cout << "File7zIsCompressed2 exception:" << ex.what() << std::endl;

            return -1;
        }
    }

}

