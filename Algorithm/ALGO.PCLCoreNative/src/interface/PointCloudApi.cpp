#include "pch.h"
#include "PointCloudApi.h"
#include "PointCloudNativeHandle.h"

#include <pcl/common/common.h>
#include <pcl/conversions.h>
#include <pcl/io/obj_io.h>
#include <pcl/io/pcd_io.h>
#include <pcl/io/ply_io.h>
#include <pcl/io/vtk_lib_io.h>

#include <vtkPolyData.h>
#include <vtkSTLReader.h>
#include <vtkImageData.h>
#include <vtkSmartPointer.h>
#include <vtkTIFFReader.h>

#include <algorithm>
#include <cctype>
#include <cmath>
#include <cstddef>
#include <cstdlib>
#include <cstring>
#include <cstdint>
#include <fstream>
#include <limits>
#include <sstream>
#include <string>
#include <vector>

namespace {
std::string ToLowerAscii(std::string value)
{
    std::transform(
        value.begin(),
        value.end(),
        value.begin(),
        [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
    return value;
}

std::string ToUpperAscii(std::string value)
{
    std::transform(
        value.begin(),
        value.end(),
        value.begin(),
        [](unsigned char c) { return static_cast<char>(std::toupper(c)); });
    return value;
}

std::string GetFileExtension(const char* path)
{
    if (path == nullptr) {
        return {};
    }

    std::string filePath(path);
    std::size_t dot = filePath.find_last_of('.');
    if (dot == std::string::npos) {
        return {};
    }

    return ToLowerAscii(filePath.substr(dot));
}

std::string TrimAscii(std::string value)
{
    auto isSpace = [](unsigned char c) { return std::isspace(c) != 0; };

    std::size_t begin = 0;
    while (begin < value.size() && isSpace(static_cast<unsigned char>(value[begin]))) {
        ++begin;
    }

    if (begin == value.size()) {
        return {};
    }

    std::size_t end = value.size();
    while (end > begin && isSpace(static_cast<unsigned char>(value[end - 1]))) {
        --end;
    }

    return value.substr(begin, end - begin);
}

std::vector<std::string> SplitWhitespace(const std::string& text)
{
    std::istringstream stream(text);
    std::vector<std::string> tokens;
    std::string token;
    while (stream >> token) {
        tokens.push_back(token);
    }
    return tokens;
}

bool ReadNextNumberToken(const char*& cursor, double& outValue)
{
    while (*cursor != '\0') {
        const unsigned char ch = static_cast<unsigned char>(*cursor);
        if (std::isspace(ch) == 0 && *cursor != ',' && *cursor != ';') {
            break;
        }
        ++cursor;
    }

    if (*cursor == '\0') {
        return false;
    }

    char* next = nullptr;
    outValue = std::strtod(cursor, &next);
    if (next == cursor) {
        return false;
    }

    cursor = next;
    return true;
}

bool TryParseXYZFromAsciiLine(
    const std::string& line,
    int xIndex,
    int yIndex,
    int zIndex,
    float& outX,
    float& outY,
    float& outZ)
{
    if (xIndex < 0 || yIndex < 0 || zIndex < 0) {
        return false;
    }

    const int maxIndex = std::max(xIndex, std::max(yIndex, zIndex));
    const char* cursor = line.c_str();
    bool hasX = false;
    bool hasY = false;
    bool hasZ = false;

    for (int tokenIndex = 0; tokenIndex <= maxIndex; ++tokenIndex) {
        double value = 0.0;
        if (!ReadNextNumberToken(cursor, value)) {
            break;
        }

        if (tokenIndex == xIndex) {
            outX = static_cast<float>(value);
            hasX = true;
        } else if (tokenIndex == yIndex) {
            outY = static_cast<float>(value);
            hasY = true;
        } else if (tokenIndex == zIndex) {
            outZ = static_cast<float>(value);
            hasZ = true;
        }
    }

    return hasX && hasY && hasZ;
}

void FinalizeUnorganizedCloud(pcl::PointCloud<pcl::PointXYZ>* pc)
{
    if (pc == nullptr) {
        return;
    }

    const std::size_t pointCount = pc->points.size();
    if (pointCount > static_cast<std::size_t>(std::numeric_limits<std::uint32_t>::max())) {
        pc->clear();
        return;
    }

    pc->width = static_cast<std::uint32_t>(pointCount);
    pc->height = 1;
    pc->is_dense = true;
}

void FinalizeUnorganizedCloud(pcl::PointCloud<pcl::PointXYZ>* pc, bool isDense)
{
    if (pc == nullptr) {
        return;
    }

    const std::size_t pointCount = pc->points.size();
    if (pointCount > static_cast<std::size_t>(std::numeric_limits<std::uint32_t>::max())) {
        pc->clear();
        return;
    }

    pc->width = static_cast<std::uint32_t>(pointCount);
    pc->height = 1;
    pc->is_dense = isDense;
}

bool AreDepthTiffLoadParametersValid(double spacingX, double spacingY, double spacingZ)
{
    return std::isfinite(spacingX)
        && std::isfinite(spacingY)
        && std::isfinite(spacingZ)
        && spacingX > 0.0
        && spacingY > 0.0
        && spacingZ > 0.0;
}

bool IsInvalidDepthValue(double pixelDepth, double invalidValue, bool useInvalidValue)
{
    if (std::isnan(pixelDepth) || !std::isfinite(pixelDepth)) {
        return true;
    }

    if (!useInvalidValue || !std::isfinite(invalidValue)) {
        return false;
    }

    const double tolerance = std::max(1.0e-8, std::abs(invalidValue) * 1.0e-8);
    return std::abs(pixelDepth - invalidValue) <= tolerance;
}

bool TryParseSizeT(const std::string& text, std::size_t& outValue)
{
    try {
        const unsigned long long parsed = std::stoull(text);
        outValue = static_cast<std::size_t>(parsed);
        return true;
    } catch (...) {
        return false;
    }
}

bool TryParseInt(const std::string& text, int& outValue)
{
    try {
        std::size_t consumed = 0;
        const int parsed = std::stoi(text, &consumed, 10);
        if (consumed != text.size()) {
            return false;
        }
        outValue = parsed;
        return true;
    } catch (...) {
        return false;
    }
}

enum class ScalarValueType : std::uint8_t
{
    Invalid = 0,
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Float32,
    Float64
};

ScalarValueType ToPlyScalarValueType(const std::string& typeName)
{
    const std::string lower = ToLowerAscii(typeName);
    if (lower == "char" || lower == "int8") {
        return ScalarValueType::Int8;
    }
    if (lower == "uchar" || lower == "uint8") {
        return ScalarValueType::UInt8;
    }
    if (lower == "short" || lower == "int16") {
        return ScalarValueType::Int16;
    }
    if (lower == "ushort" || lower == "uint16") {
        return ScalarValueType::UInt16;
    }
    if (lower == "int" || lower == "int32") {
        return ScalarValueType::Int32;
    }
    if (lower == "uint" || lower == "uint32") {
        return ScalarValueType::UInt32;
    }
    if (lower == "float" || lower == "float32") {
        return ScalarValueType::Float32;
    }
    if (lower == "double" || lower == "float64") {
        return ScalarValueType::Float64;
    }
    return ScalarValueType::Invalid;
}

ScalarValueType ToPcdScalarValueType(char typeCode, int typeSize)
{
    switch (typeCode) {
    case 'F':
        if (typeSize == 4) {
            return ScalarValueType::Float32;
        }
        if (typeSize == 8) {
            return ScalarValueType::Float64;
        }
        break;
    case 'I':
        if (typeSize == 1) {
            return ScalarValueType::Int8;
        }
        if (typeSize == 2) {
            return ScalarValueType::Int16;
        }
        if (typeSize == 4) {
            return ScalarValueType::Int32;
        }
        break;
    case 'U':
        if (typeSize == 1) {
            return ScalarValueType::UInt8;
        }
        if (typeSize == 2) {
            return ScalarValueType::UInt16;
        }
        if (typeSize == 4) {
            return ScalarValueType::UInt32;
        }
        break;
    default:
        break;
    }

    return ScalarValueType::Invalid;
}

std::size_t ScalarValueTypeSize(ScalarValueType type)
{
    switch (type) {
    case ScalarValueType::Int8:
    case ScalarValueType::UInt8:
        return 1;
    case ScalarValueType::Int16:
    case ScalarValueType::UInt16:
        return 2;
    case ScalarValueType::Int32:
    case ScalarValueType::UInt32:
    case ScalarValueType::Float32:
        return 4;
    case ScalarValueType::Float64:
        return 8;
    default:
        return 0;
    }
}

bool DecodeScalarLittleEndian(const unsigned char* data, ScalarValueType type, double& outValue)
{
    switch (type) {
    case ScalarValueType::Int8: {
        std::int8_t value = 0;
        std::memcpy(&value, data, sizeof(value));
        outValue = static_cast<double>(value);
        return true;
    }
    case ScalarValueType::UInt8: {
        std::uint8_t value = 0;
        std::memcpy(&value, data, sizeof(value));
        outValue = static_cast<double>(value);
        return true;
    }
    case ScalarValueType::Int16: {
        std::int16_t value = 0;
        std::memcpy(&value, data, sizeof(value));
        outValue = static_cast<double>(value);
        return true;
    }
    case ScalarValueType::UInt16: {
        std::uint16_t value = 0;
        std::memcpy(&value, data, sizeof(value));
        outValue = static_cast<double>(value);
        return true;
    }
    case ScalarValueType::Int32: {
        std::int32_t value = 0;
        std::memcpy(&value, data, sizeof(value));
        outValue = static_cast<double>(value);
        return true;
    }
    case ScalarValueType::UInt32: {
        std::uint32_t value = 0;
        std::memcpy(&value, data, sizeof(value));
        outValue = static_cast<double>(value);
        return true;
    }
    case ScalarValueType::Float32: {
        float value = 0.0f;
        std::memcpy(&value, data, sizeof(value));
        outValue = static_cast<double>(value);
        return true;
    }
    case ScalarValueType::Float64: {
        double value = 0.0;
        std::memcpy(&value, data, sizeof(value));
        outValue = value;
        return true;
    }
    default:
        return false;
    }
}

bool LoadPlyFast(
    const char* path,
    pcl::PointCloud<pcl::PointXYZ>* pc,
    PointCloudBoundsAccumulator* bounds)
{
    std::ifstream input(path, std::ios::binary);
    if (!input.is_open()) {
        return false;
    }

    bool isAscii = false;
    bool isBinaryLittle = false;
    bool inVertexElement = false;
    std::size_t vertexCount = 0;
    std::vector<std::string> vertexPropertyNames;
    std::vector<std::string> vertexPropertyTypes;
    std::streampos dataOffset = std::streampos(0);

    std::string line;
    if (!std::getline(input, line) || ToLowerAscii(TrimAscii(line)) != "ply") {
        return false;
    }

    while (std::getline(input, line)) {
        const std::string trimmed = TrimAscii(line);
        if (trimmed.empty()) {
            continue;
        }

        std::vector<std::string> parts = SplitWhitespace(trimmed);
        if (parts.empty()) {
            continue;
        }

        const std::string key = ToLowerAscii(parts[0]);
        if (key == "format") {
            if (parts.size() < 2) {
                return false;
            }

            const std::string format = ToLowerAscii(parts[1]);
            isAscii = format == "ascii";
            isBinaryLittle = format == "binary_little_endian";
            if (!isAscii && !isBinaryLittle) {
                return false;
            }
            continue;
        }

        if (key == "element") {
            inVertexElement = false;
            if (parts.size() >= 3 && ToLowerAscii(parts[1]) == "vertex") {
                if (!TryParseSizeT(parts[2], vertexCount)) {
                    return false;
                }

                vertexPropertyNames.clear();
                vertexPropertyTypes.clear();
                inVertexElement = true;
            }
            continue;
        }

        if (key == "property" && inVertexElement) {
            if (parts.size() >= 2 && ToLowerAscii(parts[1]) == "list") {
                return false;
            }
            if (parts.size() < 3) {
                return false;
            }

            vertexPropertyTypes.push_back(parts[1]);
            vertexPropertyNames.push_back(ToLowerAscii(parts[2]));
            continue;
        }

        if (key == "end_header") {
            dataOffset = input.tellg();
            break;
        }
    }

    if (vertexCount == 0 || vertexPropertyNames.empty()) {
        return false;
    }

    int xIndex = -1;
    int yIndex = -1;
    int zIndex = -1;
    for (int i = 0; i < static_cast<int>(vertexPropertyNames.size()); ++i) {
        if (vertexPropertyNames[static_cast<std::size_t>(i)] == "x") {
            xIndex = i;
        } else if (vertexPropertyNames[static_cast<std::size_t>(i)] == "y") {
            yIndex = i;
        } else if (vertexPropertyNames[static_cast<std::size_t>(i)] == "z") {
            zIndex = i;
        }
    }
    if (xIndex < 0 || yIndex < 0 || zIndex < 0) {
        return false;
    }

    pc->clear();
    pc->points.resize(vertexCount);
    bool isDense = true;

    input.clear();
    input.seekg(dataOffset, std::ios::beg);
    if (isAscii) {
        std::size_t pointsRead = 0;
        while (pointsRead < vertexCount && std::getline(input, line)) {
            const std::string trimmed = TrimAscii(line);
            if (trimmed.empty()) {
                continue;
            }

            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            if (!TryParseXYZFromAsciiLine(trimmed, xIndex, yIndex, zIndex, x, y, z)) {
                return false;
            }

            pcl::PointXYZ& point = pc->points[pointsRead];
            point.x = x;
            point.y = y;
            point.z = z;
            if (bounds != nullptr) {
                bounds->Add(point);
            }
            if (!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z)) {
                isDense = false;
            }
            ++pointsRead;
        }

        if (pointsRead != vertexCount) {
            return false;
        }
    } else {
        std::vector<std::size_t> offsets(vertexPropertyTypes.size(), 0);
        std::vector<ScalarValueType> scalarTypes(vertexPropertyTypes.size(), ScalarValueType::Invalid);
        std::size_t recordSize = 0;
        for (std::size_t i = 0; i < vertexPropertyTypes.size(); ++i) {
            offsets[i] = recordSize;
            const ScalarValueType scalarType = ToPlyScalarValueType(vertexPropertyTypes[i]);
            const std::size_t scalarSize = ScalarValueTypeSize(scalarType);
            if (scalarSize == 0) {
                return false;
            }
            scalarTypes[i] = scalarType;
            recordSize += scalarSize;
        }
        if (recordSize == 0) {
            return false;
        }

        const ScalarValueType xScalarType = scalarTypes[static_cast<std::size_t>(xIndex)];
        const ScalarValueType yScalarType = scalarTypes[static_cast<std::size_t>(yIndex)];
        const ScalarValueType zScalarType = scalarTypes[static_cast<std::size_t>(zIndex)];
        if (xScalarType == ScalarValueType::Invalid
            || yScalarType == ScalarValueType::Invalid
            || zScalarType == ScalarValueType::Invalid) {
            return false;
        }

        const std::size_t targetChunkBytes = 2U * 1024U * 1024U;
        const std::size_t pointsPerChunk = std::max<std::size_t>(1, targetChunkBytes / recordSize);
        std::vector<unsigned char> chunkBuffer(recordSize * pointsPerChunk, 0);

        std::size_t globalIndex = 0;
        while (globalIndex < vertexCount) {
            const std::size_t remaining = vertexCount - globalIndex;
            const std::size_t chunkPointCount = std::min(pointsPerChunk, remaining);
            const std::size_t chunkBytes = chunkPointCount * recordSize;

            if (!input.read(reinterpret_cast<char*>(chunkBuffer.data()), static_cast<std::streamsize>(chunkBytes))) {
                return false;
            }

            for (std::size_t localIndex = 0; localIndex < chunkPointCount; ++localIndex) {
                const unsigned char* record = chunkBuffer.data() + localIndex * recordSize;

                double xValue = 0.0;
                double yValue = 0.0;
                double zValue = 0.0;
                if (!DecodeScalarLittleEndian(record + offsets[static_cast<std::size_t>(xIndex)], xScalarType, xValue)
                    || !DecodeScalarLittleEndian(record + offsets[static_cast<std::size_t>(yIndex)], yScalarType, yValue)
                    || !DecodeScalarLittleEndian(record + offsets[static_cast<std::size_t>(zIndex)], zScalarType, zValue)) {
                    return false;
                }

                const float x = static_cast<float>(xValue);
                const float y = static_cast<float>(yValue);
                const float z = static_cast<float>(zValue);

                pcl::PointXYZ& point = pc->points[globalIndex + localIndex];
                point.x = x;
                point.y = y;
                point.z = z;
                if (bounds != nullptr) {
                    bounds->Add(point);
                }
                if (!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z)) {
                    isDense = false;
                }
            }

            globalIndex += chunkPointCount;
        }
    }

    FinalizeUnorganizedCloud(pc, isDense);
    return !pc->empty();
}

bool LoadPcdFast(
    const char* path,
    pcl::PointCloud<pcl::PointXYZ>* pc,
    PointCloudBoundsAccumulator* bounds)
{
    std::ifstream input(path, std::ios::binary);
    if (!input.is_open()) {
        return false;
    }

    std::vector<std::string> fields;
    std::vector<int> sizes;
    std::vector<char> types;
    std::vector<int> counts;
    std::size_t width = 0;
    std::size_t height = 1;
    std::size_t pointCount = 0;
    std::string dataMode;
    std::streampos dataOffset = std::streampos(0);

    std::string line;
    while (std::getline(input, line)) {
        const std::string trimmed = TrimAscii(line);
        if (trimmed.empty() || trimmed[0] == '#') {
            continue;
        }

        std::vector<std::string> parts = SplitWhitespace(trimmed);
        if (parts.empty()) {
            continue;
        }

        const std::string key = ToUpperAscii(parts[0]);
        if (key == "FIELDS" || key == "FIELD") {
            fields.assign(parts.begin() + 1, parts.end());
        } else if (key == "SIZE") {
            sizes.clear();
            for (std::size_t i = 1; i < parts.size(); ++i) {
                int value = 0;
                if (!TryParseInt(parts[i], value) || value <= 0) {
                    return false;
                }
                sizes.push_back(value);
            }
        } else if (key == "TYPE") {
            types.clear();
            for (std::size_t i = 1; i < parts.size(); ++i) {
                if (parts[i].empty()) {
                    return false;
                }
                types.push_back(static_cast<char>(std::toupper(static_cast<unsigned char>(parts[i][0]))));
            }
        } else if (key == "COUNT") {
            counts.clear();
            for (std::size_t i = 1; i < parts.size(); ++i) {
                int value = 0;
                if (!TryParseInt(parts[i], value) || value <= 0) {
                    return false;
                }
                counts.push_back(value);
            }
        } else if (key == "WIDTH") {
            if (parts.size() < 2 || !TryParseSizeT(parts[1], width)) {
                return false;
            }
        } else if (key == "HEIGHT") {
            if (parts.size() < 2 || !TryParseSizeT(parts[1], height)) {
                return false;
            }
        } else if (key == "POINTS") {
            if (parts.size() < 2 || !TryParseSizeT(parts[1], pointCount)) {
                return false;
            }
        } else if (key == "DATA") {
            if (parts.size() < 2) {
                return false;
            }
            dataMode = ToLowerAscii(parts[1]);
            dataOffset = input.tellg();
            break;
        }
    }

    if (fields.empty() || sizes.empty() || types.empty() || dataMode.empty()) {
        return false;
    }

    if (counts.empty()) {
        counts.assign(fields.size(), 1);
    }
    if (sizes.size() != fields.size() || types.size() != fields.size() || counts.size() != fields.size()) {
        return false;
    }
    if (pointCount == 0 && width > 0 && height > 0) {
        pointCount = width * height;
    }
    if (pointCount == 0) {
        return false;
    }

    std::vector<ScalarValueType> fieldScalarTypes(fields.size(), ScalarValueType::Invalid);
    for (std::size_t i = 0; i < fields.size(); ++i) {
        fieldScalarTypes[i] = ToPcdScalarValueType(types[i], sizes[i]);
        if (fieldScalarTypes[i] == ScalarValueType::Invalid) {
            return false;
        }
    }

    int xField = -1;
    int yField = -1;
    int zField = -1;
    int tokenOffset = 0;
    int xToken = -1;
    int yToken = -1;
    int zToken = -1;
    std::vector<std::size_t> byteOffsets(fields.size(), 0);
    std::size_t pointStep = 0;
    for (int i = 0; i < static_cast<int>(fields.size()); ++i) {
        const std::string lowerName = ToLowerAscii(fields[static_cast<std::size_t>(i)]);
        if (lowerName == "x") {
            xField = i;
            xToken = tokenOffset;
        } else if (lowerName == "y") {
            yField = i;
            yToken = tokenOffset;
        } else if (lowerName == "z") {
            zField = i;
            zToken = tokenOffset;
        }

        tokenOffset += counts[static_cast<std::size_t>(i)];
        byteOffsets[static_cast<std::size_t>(i)] = pointStep;
        pointStep += static_cast<std::size_t>(counts[static_cast<std::size_t>(i)])
            * static_cast<std::size_t>(sizes[static_cast<std::size_t>(i)]);
    }
    if (xField < 0 || yField < 0 || zField < 0) {
        return false;
    }

    const ScalarValueType xScalarType = fieldScalarTypes[static_cast<std::size_t>(xField)];
    const ScalarValueType yScalarType = fieldScalarTypes[static_cast<std::size_t>(yField)];
    const ScalarValueType zScalarType = fieldScalarTypes[static_cast<std::size_t>(zField)];
    if (xScalarType == ScalarValueType::Invalid
        || yScalarType == ScalarValueType::Invalid
        || zScalarType == ScalarValueType::Invalid) {
        return false;
    }

    pc->clear();
    pc->points.resize(pointCount);
    bool isDense = true;

    input.clear();
    input.seekg(dataOffset, std::ios::beg);
    if (dataMode == "ascii") {
        std::size_t pointsRead = 0;
        while (pointsRead < pointCount && std::getline(input, line)) {
            const std::string trimmed = TrimAscii(line);
            if (trimmed.empty() || trimmed[0] == '#') {
                continue;
            }

            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            if (!TryParseXYZFromAsciiLine(trimmed, xToken, yToken, zToken, x, y, z)) {
                return false;
            }

            pcl::PointXYZ& point = pc->points[pointsRead];
            point.x = x;
            point.y = y;
            point.z = z;
            if (bounds != nullptr) {
                bounds->Add(point);
            }
            if (!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z)) {
                isDense = false;
            }
            ++pointsRead;
        }

        if (pointsRead != pointCount) {
            return false;
        }
    } else if (dataMode == "binary") {
        if (pointStep == 0) {
            return false;
        }

        const std::size_t targetChunkBytes = 2U * 1024U * 1024U;
        const std::size_t pointsPerChunk = std::max<std::size_t>(1, targetChunkBytes / pointStep);
        std::vector<unsigned char> chunkBuffer(pointStep * pointsPerChunk, 0);

        std::size_t globalIndex = 0;
        while (globalIndex < pointCount) {
            const std::size_t remaining = pointCount - globalIndex;
            const std::size_t chunkPointCount = std::min(pointsPerChunk, remaining);
            const std::size_t chunkBytes = chunkPointCount * pointStep;
            if (!input.read(reinterpret_cast<char*>(chunkBuffer.data()), static_cast<std::streamsize>(chunkBytes))) {
                return false;
            }

            for (std::size_t localIndex = 0; localIndex < chunkPointCount; ++localIndex) {
                const unsigned char* record = chunkBuffer.data() + localIndex * pointStep;

                double xValue = 0.0;
                double yValue = 0.0;
                double zValue = 0.0;
                if (!DecodeScalarLittleEndian(record + byteOffsets[static_cast<std::size_t>(xField)], xScalarType, xValue)
                    || !DecodeScalarLittleEndian(record + byteOffsets[static_cast<std::size_t>(yField)], yScalarType, yValue)
                    || !DecodeScalarLittleEndian(record + byteOffsets[static_cast<std::size_t>(zField)], zScalarType, zValue)) {
                    return false;
                }

                const float x = static_cast<float>(xValue);
                const float y = static_cast<float>(yValue);
                const float z = static_cast<float>(zValue);

                pcl::PointXYZ& point = pc->points[globalIndex + localIndex];
                point.x = x;
                point.y = y;
                point.z = z;
                if (bounds != nullptr) {
                    bounds->Add(point);
                }
                if (!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z)) {
                    isDense = false;
                }
            }

            globalIndex += chunkPointCount;
        }
    } else {
        return false;
    }

    FinalizeUnorganizedCloud(pc, isDense);
    return !pc->empty();
}

bool IsObjVertexLine(const std::string& line, const char*& outCursor)
{
    const char* cursor = line.c_str();
    while (*cursor != '\0' && std::isspace(static_cast<unsigned char>(*cursor)) != 0) {
        ++cursor;
    }

    if (*cursor != 'v') {
        return false;
    }
    ++cursor;
    if (*cursor != ' ' && *cursor != '\t') {
        return false;
    }

    outCursor = cursor;
    return true;
}

bool LoadObjFast(
    const char* path,
    pcl::PointCloud<pcl::PointXYZ>* pc,
    PointCloudBoundsAccumulator* bounds)
{
    std::ifstream input(path);
    if (!input.is_open()) {
        return false;
    }

    std::size_t vertexCount = 0;
    std::string line;
    while (std::getline(input, line)) {
        const char* cursor = nullptr;
        if (IsObjVertexLine(line, cursor)) {
            ++vertexCount;
        }
    }

    if (vertexCount == 0) {
        return false;
    }

    input.clear();
    input.seekg(0, std::ios::beg);

    pc->clear();
    pc->points.resize(vertexCount);

    bool isDense = true;
    std::size_t vertexIndex = 0;
    while (vertexIndex < vertexCount && std::getline(input, line)) {
        const char* cursor = nullptr;
        if (!IsObjVertexLine(line, cursor)) {
            continue;
        }

        double xValue = 0.0;
        double yValue = 0.0;
        double zValue = 0.0;
        if (!ReadNextNumberToken(cursor, xValue)
            || !ReadNextNumberToken(cursor, yValue)
            || !ReadNextNumberToken(cursor, zValue)) {
            return false;
        }

        const float x = static_cast<float>(xValue);
        const float y = static_cast<float>(yValue);
        const float z = static_cast<float>(zValue);
        pcl::PointXYZ& point = pc->points[vertexIndex];
        point.x = x;
        point.y = y;
        point.z = z;
        if (bounds != nullptr) {
            bounds->Add(point);
        }

        if (!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z)) {
            isDense = false;
        }

        ++vertexIndex;
    }

    if (vertexIndex != vertexCount) {
        return false;
    }

    FinalizeUnorganizedCloud(pc, isDense);
    return !pc->empty();
}

bool LoadTxtFast(
    const char* path,
    pcl::PointCloud<pcl::PointXYZ>* pc,
    PointCloudBoundsAccumulator* bounds)
{
    std::ifstream input(path);
    if (!input.is_open()) {
        return false;
    }

    input.seekg(0, std::ios::end);
    const std::streampos fileSize = input.tellg();
    input.seekg(0, std::ios::beg);

    pc->clear();
    if (fileSize > 0) {
        const std::size_t estimated = std::min(
            static_cast<std::size_t>(fileSize) / 24U,
            static_cast<std::size_t>(50'000'000));
        if (estimated > 0) {
            pc->points.reserve(estimated);
        }
    }

    bool isDense = true;
    std::string line;
    while (std::getline(input, line)) {
        const std::string trimmed = TrimAscii(line);
        if (trimmed.empty()) {
            continue;
        }

        if (trimmed[0] == '#' || (trimmed.size() >= 2 && trimmed[0] == '/' && trimmed[1] == '/')) {
            continue;
        }

        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;
        if (!TryParseXYZFromAsciiLine(trimmed, 0, 1, 2, x, y, z)) {
            continue;
        }

        pcl::PointXYZ point;
        point.x = x;
        point.y = y;
        point.z = z;
        pc->points.push_back(point);
        if (bounds != nullptr) {
            bounds->Add(point);
        }

        if (!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z)) {
            isDense = false;
        }
    }

    if (pc->empty()) {
        return false;
    }

    FinalizeUnorganizedCloud(pc, isDense);
    return !pc->empty();
}

bool IsStlVertexLine(const std::string& line, const char*& outCursor)
{
    const char* cursor = line.c_str();
    while (*cursor != '\0' && std::isspace(static_cast<unsigned char>(*cursor)) != 0) {
        ++cursor;
    }

    const char keyword[] = "vertex";
    for (int i = 0; keyword[i] != '\0'; ++i) {
        if (cursor[i] == '\0') {
            return false;
        }
        if (std::tolower(static_cast<unsigned char>(cursor[i])) != keyword[i]) {
            return false;
        }
    }

    cursor += 6;
    if (*cursor != '\0' && std::isspace(static_cast<unsigned char>(*cursor)) == 0) {
        return false;
    }

    outCursor = cursor;
    return true;
}

bool LoadStlFast(
    const char* path,
    pcl::PointCloud<pcl::PointXYZ>* pc,
    PointCloudBoundsAccumulator* bounds)
{
    std::ifstream input(path, std::ios::binary);
    if (!input.is_open()) {
        return false;
    }

    input.seekg(0, std::ios::end);
    const std::streamoff fileSizeSigned = input.tellg();
    if (fileSizeSigned < 0) {
        return false;
    }
    const std::size_t fileSize = static_cast<std::size_t>(fileSizeSigned);
    input.seekg(0, std::ios::beg);

    if (fileSize >= 84) {
        char header[80] = {};
        if (!input.read(header, sizeof(header))) {
            return false;
        }

        std::uint32_t triangleCount = 0;
        if (!input.read(reinterpret_cast<char*>(&triangleCount), sizeof(triangleCount))) {
            return false;
        }

        const std::size_t triangleCountSizeT = static_cast<std::size_t>(triangleCount);
        if (triangleCountSizeT > (std::numeric_limits<std::size_t>::max() - 84U) / 50U) {
            return false;
        }

        const std::size_t expectedBinarySize = 84U + triangleCountSizeT * 50U;
        if (expectedBinarySize == fileSize) {
            if (triangleCountSizeT > std::numeric_limits<std::size_t>::max() / 3U) {
                return false;
            }

            const std::size_t pointCount = triangleCountSizeT * 3U;
            pc->clear();
            pc->points.resize(pointCount);

            bool isDense = true;
            const std::size_t triangleBytes = 50U;
            const std::size_t targetChunkBytes = 2U * 1024U * 1024U;
            const std::size_t trianglesPerChunk = std::max<std::size_t>(1, targetChunkBytes / triangleBytes);
            std::vector<unsigned char> chunkBuffer(triangleBytes * trianglesPerChunk, 0);

            std::size_t triangleIndex = 0;
            while (triangleIndex < triangleCountSizeT) {
                const std::size_t remaining = triangleCountSizeT - triangleIndex;
                const std::size_t chunkTriangleCount = std::min(trianglesPerChunk, remaining);
                const std::size_t chunkBytes = chunkTriangleCount * triangleBytes;

                if (!input.read(reinterpret_cast<char*>(chunkBuffer.data()), static_cast<std::streamsize>(chunkBytes))) {
                    return false;
                }

                for (std::size_t t = 0; t < chunkTriangleCount; ++t) {
                    const unsigned char* tri = chunkBuffer.data() + t * triangleBytes;
                    const unsigned char* vertexData = tri + 12U;
                    const std::size_t pointBase = (triangleIndex + t) * 3U;

                    for (std::size_t v = 0; v < 3; ++v) {
                        const unsigned char* xyz = vertexData + v * 12U;
                        float x = 0.0f;
                        float y = 0.0f;
                        float z = 0.0f;
                        std::memcpy(&x, xyz, sizeof(float));
                        std::memcpy(&y, xyz + 4U, sizeof(float));
                        std::memcpy(&z, xyz + 8U, sizeof(float));

                        pcl::PointXYZ& point = pc->points[pointBase + v];
                        point.x = x;
                        point.y = y;
                        point.z = z;
                        if (bounds != nullptr) {
                            bounds->Add(point);
                        }
                        if (!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z)) {
                            isDense = false;
                        }
                    }
                }

                triangleIndex += chunkTriangleCount;
            }

            FinalizeUnorganizedCloud(pc, isDense);
            return !pc->empty();
        }
    }

    input.clear();
    input.seekg(0, std::ios::beg);

    pc->clear();
    if (fileSize > 0) {
        const std::size_t estimated = std::min(fileSize / 48U, static_cast<std::size_t>(50'000'000));
        if (estimated > 0) {
            pc->points.reserve(estimated);
        }
    }

    bool isDense = true;
    std::string line;
    while (std::getline(input, line)) {
        const char* cursor = nullptr;
        if (!IsStlVertexLine(line, cursor)) {
            continue;
        }

        double xValue = 0.0;
        double yValue = 0.0;
        double zValue = 0.0;
        if (!ReadNextNumberToken(cursor, xValue)
            || !ReadNextNumberToken(cursor, yValue)
            || !ReadNextNumberToken(cursor, zValue)) {
            return false;
        }

        const float x = static_cast<float>(xValue);
        const float y = static_cast<float>(yValue);
        const float z = static_cast<float>(zValue);

        pcl::PointXYZ point;
        point.x = x;
        point.y = y;
        point.z = z;
        pc->points.push_back(point);
        if (bounds != nullptr) {
            bounds->Add(point);
        }

        if (!std::isfinite(x) || !std::isfinite(y) || !std::isfinite(z)) {
            isDense = false;
        }
    }

    if (pc->empty()) {
        return false;
    }

    FinalizeUnorganizedCloud(pc, isDense);
    return !pc->empty();
}
} // namespace

PointCloudNativeHandle* CallingConvention CreatePointCloud()
{
    return new PointCloudNativeHandle();
}

PointCloudNativeHandle* CallingConvention loadPcFile(char* path)
{
    if (path == nullptr) {
        return nullptr;
    }

    auto* handle = new PointCloudNativeHandle();
    if (loadPointCloudFile(path, handle) == 0) {
        delete handle;
        return nullptr;
    }

    return handle;
}

int CallingConvention loadPlyFile(char* path, PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr) {
        return 0;
    }

    pc->clear();
    PointCloudBoundsAccumulator bounds;
    if (LoadPlyFast(path, pc, &bounds)) {
        bounds.ApplyTo(handle);
        return 1;
    }

    pc->clear();
    if (pcl::io::loadPLYFile(path, *pc) == -1) {
        InvalidatePointCloudBounds(handle);
        return 0;
    }

    InvalidatePointCloudBounds(handle);
    return 1;
}

int CallingConvention loadPcdFile(char* path, PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr) {
        return 0;
    }

    pc->clear();
    PointCloudBoundsAccumulator bounds;
    if (LoadPcdFast(path, pc, &bounds)) {
        bounds.ApplyTo(handle);
        return 1;
    }

    pc->clear();
    if (pcl::io::loadPCDFile(path, *pc) == -1) {
        InvalidatePointCloudBounds(handle);
        return 0;
    }

    InvalidatePointCloudBounds(handle);
    return 1;
}

int CallingConvention loadObjFile(char* path, PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr) {
        return 0;
    }

    pc->clear();
    PointCloudBoundsAccumulator bounds;
    if (LoadObjFast(path, pc, &bounds)) {
        bounds.ApplyTo(handle);
        return 1;
    }

    pc->clear();
    pcl::PolygonMesh mesh;
    if (pcl::io::loadPolygonFile(path, mesh) < 0) {
        InvalidatePointCloudBounds(handle);
        return 0;
    }

    pc->clear();
    pcl::fromPCLPointCloud2(mesh.cloud, *pc);
    FinalizeUnorganizedCloud(pc);
    InvalidatePointCloudBounds(handle);
    return pc->empty() ? 0 : 1;
}

int CallingConvention loadTxtFile(char* path, PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr) {
        return 0;
    }

    pc->clear();
    PointCloudBoundsAccumulator bounds;
    if (!LoadTxtFast(path, pc, &bounds)) {
        InvalidatePointCloudBounds(handle);
        return 0;
    }

    bounds.ApplyTo(handle);
    return 1;
}

int CallingConvention loadPointCloudFile(char* path, PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr) {
        return 0;
    }

    const std::string extension = GetFileExtension(path);
    if (extension == ".ply") {
        return loadPlyFile(path, handle);
    }

    if (extension == ".pcd") {
        return loadPcdFile(path, handle);
    }

    if (extension == ".obj") {
        return loadObjFile(path, handle);
    }

    if (extension == ".txt"
        || extension == ".xyz"
        || extension == ".asc"
        || extension == ".csv"
        || extension == ".pts") {
        return loadTxtFile(path, handle);
    }

    if (extension == ".stl") {
        stl2PointCloud(path, handle);
        return pc->empty() ? 0 : 1;
    }

    return 0;
}

int CallingConvention loadDepthTiffFile(
    char* path,
    PointCloudNativeHandle* handle,
    double spacingX,
    double spacingY,
    double spacingZ,
    double invalidValue,
    int useInvalidValue)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr || !AreDepthTiffLoadParametersValid(spacingX, spacingY, spacingZ)) {
        return 0;
    }

    pc->clear();

    try {
        vtkSmartPointer<vtkTIFFReader> reader = vtkSmartPointer<vtkTIFFReader>::New();
        if (reader->CanReadFile(path) == 0) {
            InvalidatePointCloudBounds(handle);
            return 0;
        }

        reader->SetFileName(path);
        reader->Update();

        vtkImageData* image = reader->GetOutput();
        if (image == nullptr || image->GetNumberOfScalarComponents() <= 0) {
            InvalidatePointCloudBounds(handle);
            return 0;
        }

        int dimensions[3] = { 0, 0, 0 };
        image->GetDimensions(dimensions);
        const int width = dimensions[0];
        const int height = dimensions[1];
        if (width <= 0 || height <= 0) {
            InvalidatePointCloudBounds(handle);
            return 0;
        }

        const std::uint64_t pixelCount =
            static_cast<std::uint64_t>(width) * static_cast<std::uint64_t>(height);
        if (pixelCount > static_cast<std::uint64_t>(std::numeric_limits<std::uint32_t>::max())) {
            InvalidatePointCloudBounds(handle);
            return 0;
        }

        pc->points.reserve(static_cast<std::size_t>(pixelCount));
        const bool compareInvalidValue = useInvalidValue != 0;
        PointCloudBoundsAccumulator bounds;

        for (int row = 0; row < height; ++row) {
            for (int col = 0; col < width; ++col) {
                const double pixelDepth = image->GetScalarComponentAsDouble(col, row, 0, 0);
                if (IsInvalidDepthValue(pixelDepth, invalidValue, compareInvalidValue)) {
                    continue;
                }

                pcl::PointXYZ point;
                point.x = static_cast<float>(static_cast<double>(col) * spacingX);
                point.y = static_cast<float>(static_cast<double>(row) * spacingY);
                point.z = static_cast<float>(pixelDepth * spacingZ);
                pc->points.push_back(point);
                bounds.Add(point);
            }
        }

        FinalizeUnorganizedCloud(pc, true);
        bounds.ApplyTo(handle);
        return pc->empty() ? 0 : 1;
    } catch (...) {
        pc->clear();
        InvalidatePointCloudBounds(handle);
        return 0;
    }
}

void CallingConvention savePcdFile(char* path, PointCloudNativeHandle* handle, int binaryMode)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr) {
        return;
    }

    pcl::io::savePCDFile(path, *pc, binaryMode >= 1);
}

void CallingConvention savePlyFile(char* path, PointCloudNativeHandle* handle, int binaryMode)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr) {
        return;
    }

    pcl::io::savePLYFile(path, *pc, binaryMode >= 1);
}

void CallingConvention stl2PointCloud(char* path, PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (path == nullptr || pc == nullptr) {
        return;
    }

    pc->clear();
    PointCloudBoundsAccumulator bounds;
    if (LoadStlFast(path, pc, &bounds)) {
        bounds.ApplyTo(handle);
        return;
    }

    vtkSmartPointer<vtkSTLReader> stlReader = vtkSmartPointer<vtkSTLReader>::New();
    stlReader->SetFileName(path);
    stlReader->Update();

    vtkSmartPointer<vtkPolyData> polyData = stlReader->GetOutput();
    pc->clear();
    pcl::io::vtkPolyDataToPointCloud(polyData, *pc);
    FinalizeUnorganizedCloud(pc, true);
    InvalidatePointCloudBounds(handle);
}

void CallingConvention saveObjFile(char* path, PointCloudNativeHandle* handle)
{
    (void)path;
    (void)handle;
}

void CallingConvention DeletePointCloud(PointCloudNativeHandle* handle)
{
    delete handle;
}

int CallingConvention CountPointCloud(PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    return pc == nullptr ? 0 : static_cast<int>(pc->size());
}

int CallingConvention getPointCloudH(PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    return pc == nullptr ? 0 : static_cast<int>(pc->height);
}

int CallingConvention getPointCloudW(PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    return pc == nullptr ? 0 : static_cast<int>(pc->size());
}

void CallingConvention getMinMaxXYZ(PointCloudNativeHandle* handle, double* out_res)
{
    if (out_res == nullptr) {
        return;
    }

    if (!EnsurePointCloudBounds(handle)) {
        for (int i = 0; i < 6; ++i) {
            out_res[i] = 0.0;
        }
        return;
    }

    out_res[0] = handle->minPoint.x;
    out_res[1] = handle->maxPoint.x;
    out_res[2] = handle->minPoint.y;
    out_res[3] = handle->maxPoint.y;
    out_res[4] = handle->minPoint.z;
    out_res[5] = handle->maxPoint.z;
}

double CallingConvention getX(PointCloudNativeHandle* handle, int index)
{
    auto* pc = PointCloudData(handle);
    return pc == nullptr ? 0.0 : pc->points[index].x;
}

double CallingConvention getY(PointCloudNativeHandle* handle, int index)
{
    auto* pc = PointCloudData(handle);
    return pc == nullptr ? 0.0 : pc->points[index].y;
}

double CallingConvention getZ(PointCloudNativeHandle* handle, int index)
{
    auto* pc = PointCloudData(handle);
    return pc == nullptr ? 0.0 : pc->points[index].z;
}

void CallingConvention setX(PointCloudNativeHandle* handle, int index, double x)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr) {
        return;
    }

    pc->points[index].x = static_cast<float>(x);
    InvalidatePointCloudBounds(handle);
}

void CallingConvention setY(PointCloudNativeHandle* handle, int index, double y)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr) {
        return;
    }

    pc->points[index].y = static_cast<float>(y);
    InvalidatePointCloudBounds(handle);
}

void CallingConvention setZ(PointCloudNativeHandle* handle, int index, double z)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr) {
        return;
    }

    pc->points[index].z = static_cast<float>(z);
    InvalidatePointCloudBounds(handle);
}

void CallingConvention reSize(PointCloudNativeHandle* handle, int size)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr || size < 0) {
        return;
    }

    pc->points.resize(static_cast<std::size_t>(size));
    FinalizeUnorganizedCloud(pc);
    InvalidatePointCloudBounds(handle);
}

void CallingConvention push(PointCloudNativeHandle* handle, double x, double y, double z)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr) {
        return;
    }

    pcl::PointXYZ point(static_cast<float>(x), static_cast<float>(y), static_cast<float>(z));
    pc->points.push_back(point);
    FinalizeUnorganizedCloud(pc);
    InvalidatePointCloudBounds(handle);
}

void CallingConvention pop(PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr || pc->points.empty()) {
        return;
    }

    pc->points.pop_back();
    FinalizeUnorganizedCloud(pc);
    InvalidatePointCloudBounds(handle);
}

void CallingConvention clear(PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr) {
        return;
    }

    pc->clear();
    InvalidatePointCloudBounds(handle);
}

const float* CallingConvention getPointCloudInterleavedF32Ptr(PointCloudNativeHandle* handle)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr || pc->points.empty()) {
        return nullptr;
    }

    return reinterpret_cast<const float*>(pc->points.data());
}

int CallingConvention getPointCloudInterleavedStrideBytes()
{
    return static_cast<int>(sizeof(pcl::PointXYZ));
}

void CallingConvention copyPointCloudToSplitF64(
    PointCloudNativeHandle* handle,
    double* outX,
    double* outY,
    double* outZ,
    int count)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr || outX == nullptr || outY == nullptr || outZ == nullptr || count <= 0) {
        return;
    }

    const int copyCount = std::min(count, static_cast<int>(pc->points.size()));
    for (int i = 0; i < copyCount; i++) {
        outX[i] = pc->points[i].x;
        outY[i] = pc->points[i].y;
        outZ[i] = pc->points[i].z;
    }
}

void CallingConvention setPointCloudFromSplitF64(
    PointCloudNativeHandle* handle,
    const double* inX,
    const double* inY,
    const double* inZ,
    int count)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr) {
        return;
    }

    pc->clear();
    if (count <= 0 || inX == nullptr || inY == nullptr || inZ == nullptr) {
        SetPointCloudBoundsToZero(handle);
        MarkPointCloudModified(handle);
        return;
    }

    PointCloudBoundsAccumulator bounds;
    bool isDense = true;
    pc->points.resize(static_cast<std::size_t>(count));
    for (int i = 0; i < count; i++) {
        pcl::PointXYZ& point = pc->points[static_cast<std::size_t>(i)];
        point.x = static_cast<float>(inX[i]);
        point.y = static_cast<float>(inY[i]);
        point.z = static_cast<float>(inZ[i]);
        bounds.Add(point);
        if (!std::isfinite(point.x) || !std::isfinite(point.y) || !std::isfinite(point.z)) {
            isDense = false;
        }
    }

    FinalizeUnorganizedCloud(pc, isDense);
    bounds.ApplyTo(handle);
}

void CallingConvention setPointCloudFromInterleavedF32(
    PointCloudNativeHandle* handle,
    const float* xyzInterleaved,
    int count,
    int strideBytes)
{
    auto* pc = PointCloudData(handle);
    if (pc == nullptr) {
        return;
    }

    pc->clear();
    if (count <= 0 || xyzInterleaved == nullptr) {
        SetPointCloudBoundsToZero(handle);
        MarkPointCloudModified(handle);
        return;
    }

    if (strideBytes <= 0) {
        strideBytes = static_cast<int>(sizeof(pcl::PointXYZ));
    }

    PointCloudBoundsAccumulator bounds;
    bool isDense = true;
    const auto* base = reinterpret_cast<const unsigned char*>(xyzInterleaved);
    pc->points.resize(static_cast<std::size_t>(count));
    for (int i = 0; i < count; i++) {
        const auto* source = reinterpret_cast<const float*>(
            base + (static_cast<size_t>(i) * static_cast<size_t>(strideBytes)));
        pcl::PointXYZ& point = pc->points[static_cast<std::size_t>(i)];
        point.x = source[0];
        point.y = source[1];
        point.z = source[2];
        bounds.Add(point);
        if (!std::isfinite(point.x) || !std::isfinite(point.y) || !std::isfinite(point.z)) {
            isDense = false;
        }
    }

    FinalizeUnorganizedCloud(pc, isDense);
    bounds.ApplyTo(handle);
}

// 返回点云索引向量的指针
std::vector<pcl::PointIndices>* CallingConvention CreatePointIndices()
{
    return new std::vector<pcl::PointIndices>();
}

// 删除指针
void CallingConvention DeletePointIndices(std::vector<pcl::PointIndices>* in_indice)
{
    delete in_indice;
}

// 返回点云索引的大小
int CallingConvention CountPointIndices(std::vector<pcl::PointIndices>* in_indice)
{
    return static_cast<int>(in_indice->size());
}

pcl::PointIndices* CallingConvention getPointIndice(std::vector<pcl::PointIndices>* in_indice, int pos)
{
    auto* indices = new pcl::PointIndices();
    *indices = (*in_indice)[pos];
    return indices;
}

int CallingConvention getSizeOfIndice(std::vector<pcl::PointIndices>* in_indice, int pos)
{
    return static_cast<int>((*in_indice)[pos].indices.size());
}
