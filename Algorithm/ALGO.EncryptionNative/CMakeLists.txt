cmake_minimum_required(VERSION 3.20)

project(ALGO.EncryptionNative LANGUAGES CXX)

if(WIN32)
    message(FATAL_ERROR "Use ALGO.EncryptionNative.vcxproj for Windows/MSVC builds. This CMake entry is for Linux.")
endif()

if(NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type" FORCE)
endif()

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(BIT7Z_INCLUDE_DIR
    "/home/zhuwei/workspace/code/00_XAnyLabeling/00_thirdparty/bit7z-v4.0.10/install/include/bit7z"
    CACHE PATH
    "Path to bit7z headers, either the include directory or include/bit7z directory"
)
set(BIT7Z_LIBRARY_RELEASE
    "/home/zhuwei/workspace/code/00_XAnyLabeling/00_thirdparty/bit7z-v4.0.10/install/lib/x64/libbit7z64.a"
    CACHE FILEPATH
    "Path to Release libbit7z static library"
)
set(BIT7Z_LIBRARY_DEBUG
    "/home/zhuwei/workspace/code/00_XAnyLabeling/00_thirdparty/bit7z-v4.0.10/install/lib/x64/libbit7z64_d.a"
    CACHE FILEPATH
    "Path to Debug libbit7z static library"
)
set(ALGOENCRYPTIONNATIVE_7Z_LIBRARY_PATH
    "/usr/lib/7zip/7z.so"
    CACHE FILEPATH
    "Runtime path to 7-Zip shared library used by bit7z"
)

if(EXISTS "${BIT7Z_INCLUDE_DIR}/bit7z/bit7zlibrary.hpp")
    set(_bit7z_include_root "${BIT7Z_INCLUDE_DIR}")
elseif(EXISTS "${BIT7Z_INCLUDE_DIR}/bit7zlibrary.hpp")
    get_filename_component(_bit7z_include_root "${BIT7Z_INCLUDE_DIR}" DIRECTORY)
else()
    message(FATAL_ERROR "Cannot find bit7zlibrary.hpp from BIT7Z_INCLUDE_DIR='${BIT7Z_INCLUDE_DIR}'")
endif()

if(NOT EXISTS "${BIT7Z_LIBRARY_RELEASE}")
    message(FATAL_ERROR "Cannot find Release bit7z library: ${BIT7Z_LIBRARY_RELEASE}")
endif()

if(NOT EXISTS "${BIT7Z_LIBRARY_DEBUG}")
    message(FATAL_ERROR "Cannot find Debug bit7z library: ${BIT7Z_LIBRARY_DEBUG}")
endif()

add_library(bit7z::bit7z STATIC IMPORTED)
set_target_properties(bit7z::bit7z PROPERTIES
    IMPORTED_LOCATION "${BIT7Z_LIBRARY_RELEASE}"
    IMPORTED_LOCATION_RELEASE "${BIT7Z_LIBRARY_RELEASE}"
    IMPORTED_LOCATION_RELWITHDEBINFO "${BIT7Z_LIBRARY_RELEASE}"
    IMPORTED_LOCATION_MINSIZEREL "${BIT7Z_LIBRARY_RELEASE}"
    IMPORTED_LOCATION_DEBUG "${BIT7Z_LIBRARY_DEBUG}"
    INTERFACE_INCLUDE_DIRECTORIES "${_bit7z_include_root}"
)

add_library(ALGO.EncryptionNative SHARED
    src/interface/encryption.cpp
)

target_include_directories(ALGO.EncryptionNative PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/src"
)

target_compile_definitions(ALGO.EncryptionNative PRIVATE
    ALGOENCRYPTIONNATIVE_EXPORTS
    "ALGOENCRYPTIONNATIVE_DEFAULT_7Z_PATH=\"${ALGOENCRYPTIONNATIVE_7Z_LIBRARY_PATH}\""
)

target_link_libraries(ALGO.EncryptionNative PRIVATE
    bit7z::bit7z
    ${CMAKE_DL_LIBS}
)

set_target_properties(ALGO.EncryptionNative PROPERTIES
    POSITION_INDEPENDENT_CODE ON
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON
    OUTPUT_NAME "ALGO.EncryptionNative"
)
