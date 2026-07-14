#include "pch.h"

#include "interface.h"
#include <iostream>
#include <filesystem>
#include <vector>
#include <string>
#include <map>
#include <memory>
#include <cstring>
#include <algorithm>
#include <cctype>

//// Forward declarations for OpenCV types
//namespace cv 
//{
//    class Mat;
//    template<typename T> class Point_;
//    typedef Point_<float> Point2d;
//    typedef Point_<float> Point3d;
//    
//    class FileStorage;
//    class aruco_CharucoBoard;
//    class aruco_Dictionary;
//    class aruco_DetectorParameters;
//}


#include "boards/calibration_board_calib_factory.h"
#include "multicam/fused_plane_grid_model.h"
#include "multicam/multi_camera_calib.h"
#include <opencv2/aruco.hpp>
#include <opencv2/calib3d.hpp>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/imgcodecs.hpp>

enum class SingleCalibrationModelType
{
    PixelRatio,
    PinholeCamera,
    FusedPlaneGrid
};

namespace
{
    constexpr const char* kMultiCameraFormatName = "ALGO.CalibrationNative.MultiCamera.v1";
    constexpr const char* kFusedPlaneGridFormatName = "ALGO.CalibrationNative.FusedPlaneGrid.v1";

    bool isYamlFilePath(const std::filesystem::path& path)
    {
        std::string extension = path.extension().string();
        std::transform(extension.begin(), extension.end(), extension.begin(),
            [](unsigned char ch) { return static_cast<char>(std::tolower(ch)); });
        return extension == ".yaml" || extension == ".yml";
    }

    std::string boardTypeToFileSegment(CalibrationBoardType type)
    {
        switch (type)
        {
        case PIXEL_RATIO:
            return "PixelRatio";
        case CHESSBOARD:
            return "Chessboard";
        case CHARUCO:
            return "Charuco";
        default:
            return "Board";
        }
    }

    std::string sanitizeFileNameSegment(const std::string& value, const std::string& fallback)
    {
        static const std::string invalidChars = "<>:\"/\\|?*";

        std::string sanitized;
        sanitized.reserve(value.size());
        for (char ch : value)
        {
            unsigned char uch = static_cast<unsigned char>(ch);
            sanitized.push_back(invalidChars.find(ch) != std::string::npos || uch < 32 ? '_' : ch);
        }

        while (!sanitized.empty() && std::isspace(static_cast<unsigned char>(sanitized.front())))
        {
            sanitized.erase(sanitized.begin());
        }
        while (!sanitized.empty() && std::isspace(static_cast<unsigned char>(sanitized.back())))
        {
            sanitized.pop_back();
        }

        return sanitized.empty() ? fallback : sanitized;
    }

    bool isFiniteValue(double value)
    {
        return std::isfinite(value);
    }

    bool isNumericNode(const cv::FileNode& node)
    {
        return !node.empty() && (node.isInt() || node.isReal());
    }

    bool readRequiredString(const cv::FileNode& parent, const std::string& fieldName, std::string& value, std::string& errorMessage)
    {
        const cv::FileNode node = parent[fieldName];
        if (node.empty() || !node.isString())
        {
            errorMessage = "Field '" + fieldName + "' must be a string.";
            return false;
        }
        node >> value;
        if (value.empty())
        {
            errorMessage = "Field '" + fieldName + "' must not be empty.";
            return false;
        }
        return true;
    }

    bool readRequiredInt(const cv::FileNode& parent, const std::string& fieldName, int& value, std::string& errorMessage)
    {
        const cv::FileNode node = parent[fieldName];
        if (node.empty() || !node.isInt())
        {
            errorMessage = "Field '" + fieldName + "' must be an integer.";
            return false;
        }
        value = static_cast<int>(node);
        return true;
    }

    bool readRequiredDouble(const cv::FileNode& parent, const std::string& fieldName, double& value, std::string& errorMessage)
    {
        const cv::FileNode node = parent[fieldName];
        if (!isNumericNode(node))
        {
            errorMessage = "Field '" + fieldName + "' must be numeric.";
            return false;
        }
        value = static_cast<double>(node);
        if (!isFiniteValue(value))
        {
            errorMessage = "Field '" + fieldName + "' must be finite.";
            return false;
        }
        return true;
    }

    bool readOptionalDouble(const cv::FileNode& parent, const std::string& fieldName, double& value, std::string& errorMessage)
    {
        const cv::FileNode node = parent[fieldName];
        if (node.empty())
        {
            return true;
        }
        if (!isNumericNode(node))
        {
            errorMessage = "Field '" + fieldName + "' must be numeric.";
            return false;
        }
        value = static_cast<double>(node);
        if (!isFiniteValue(value))
        {
            errorMessage = "Field '" + fieldName + "' must be finite.";
            return false;
        }
        return true;
    }

    CalibrationBoardType boardTypeFromNode(const cv::FileNode& node)
    {
        if (node.isString())
        {
            std::string value;
            node >> value;
            if (value == "PIXEL_RATIO")
            {
                return PIXEL_RATIO;
            }
            if (value == "CHESSBOARD")
            {
                return CHESSBOARD;
            }
            if (value == "CHARUCO")
            {
                return CHARUCO;
            }
            if (value == "CIRCLES_GRID")
            {
                return CIRCLES_GRID;
            }
            if (value == "ASYMMETRIC_CIRCLES_GRID")
            {
                return ASYMMETRIC_CIRCLES_GRID;
            }
            return BOARD_UNKNOWN;
        }
        if (node.isInt())
        {
            return static_cast<CalibrationBoardType>(static_cast<int>(node));
        }
        return BOARD_UNKNOWN;
    }

    bool readRequiredMatrix(const cv::FileNode& parent, const std::string& fieldName, cv::Mat& matrix, std::string& errorMessage)
    {
        const cv::FileNode node = parent[fieldName];
        if (node.empty())
        {
            errorMessage = "Missing matrix field '" + fieldName + "'.";
            return false;
        }
        node >> matrix;
        if (matrix.empty())
        {
            errorMessage = "Matrix field '" + fieldName + "' is empty.";
            return false;
        }
        if (matrix.type() != CV_64F)
        {
            matrix.convertTo(matrix, CV_64F);
        }
        for (int row = 0; row < matrix.rows; ++row)
        {
            for (int col = 0; col < matrix.cols; ++col)
            {
                if (!isFiniteValue(matrix.at<double>(row, col)))
                {
                    errorMessage = "Matrix field '" + fieldName + "' contains non-finite values.";
                    return false;
                }
            }
        }
        return true;
    }

    bool readRequiredHomography(const cv::FileNode& parent, const std::string& fieldName, cv::Mat& matrix, std::string& errorMessage)
    {
        if (!readRequiredMatrix(parent, fieldName, matrix, errorMessage))
        {
            return false;
        }
        if (matrix.rows != 3 || matrix.cols != 3)
        {
            errorMessage = "Homography field '" + fieldName + "' must be 3x3.";
            return false;
        }
        return true;
    }

    bool readOptionalValidMask(const cv::FileNode& parent, cv::Mat& validMask, int rows, int cols, std::string& errorMessage)
    {
        const cv::FileNode node = parent["validMask"];
        if (node.empty())
        {
            validMask.release();
            return true;
        }
        node >> validMask;
        if (validMask.empty())
        {
            errorMessage = "Residual grid validMask is empty.";
            return false;
        }
        if (validMask.rows != rows || validMask.cols != cols || validMask.type() != CV_8U)
        {
            errorMessage = "Residual grid validMask must be CV_8U with gridRows x gridCols dimensions.";
            return false;
        }
        return true;
    }

    bool readResidualGrid(
        const cv::FileNode& parent,
        calib::multicam::ResidualGridDomain domain,
        const std::string& originXField,
        const std::string& originYField,
        const std::string& stepXField,
        const std::string& stepYField,
        const std::string& dxField,
        const std::string& dyField,
        calib::multicam::ResidualGrid& grid,
        std::string& errorMessage)
    {
        if (parent.empty() || !parent.isMap())
        {
            errorMessage = "Residual grid node is missing or not a map.";
            return false;
        }

        std::string type;
        if (!readRequiredString(parent, "type", type, errorMessage))
        {
            return false;
        }
        if (type != "GRID_LUT")
        {
            errorMessage = "Residual grid type must be GRID_LUT.";
            return false;
        }

        std::string interpolation;
        if (!readRequiredString(parent, "interpolation", interpolation, errorMessage))
        {
            return false;
        }
        if (interpolation != "BILINEAR")
        {
            errorMessage = "Residual grid interpolation must be BILINEAR.";
            return false;
        }

        grid = calib::multicam::ResidualGrid();
        grid.domain = domain;
        grid.enabled = true;
        if (!readRequiredDouble(parent, originXField, grid.originX, errorMessage) ||
            !readRequiredDouble(parent, originYField, grid.originY, errorMessage) ||
            !readRequiredDouble(parent, stepXField, grid.stepX, errorMessage) ||
            !readRequiredDouble(parent, stepYField, grid.stepY, errorMessage) ||
            !readRequiredInt(parent, "gridCols", grid.cols, errorMessage) ||
            !readRequiredInt(parent, "gridRows", grid.rows, errorMessage) ||
            !readRequiredMatrix(parent, dxField, grid.dx, errorMessage) ||
            !readRequiredMatrix(parent, dyField, grid.dy, errorMessage))
        {
            return false;
        }
        if (grid.dx.rows != grid.rows || grid.dx.cols != grid.cols ||
            grid.dy.rows != grid.rows || grid.dy.cols != grid.cols)
        {
            errorMessage = "Residual grid dx/dy dimensions must match gridRows/gridCols.";
            return false;
        }
        return readOptionalValidMask(parent, grid.validMask, grid.rows, grid.cols, errorMessage);
    }
}

// Internal C++ class definition
class CameraCalibrationFramework 
{
private:
    struct CameraParameters 
    {
        double error;
        double intervalX = std::numeric_limits<double>::quiet_NaN();;
        double intervalY = std::numeric_limits<double>::quiet_NaN();;
        cv::Mat intrinsic;
        cv::Mat distortion;
        cv::Mat extrinsic;
        cv::Mat rvec;              // 旋转向量
        cv::Mat tvec;              // 平移向量
        cv::Mat homographyMatrix;  // Homography matrix
        std::string cameraId;
        double frameWidth;
        double frameHeight;
        cv::Mat remapMap1;
        cv::Mat remapMap2;
        SingleCalibrationModelType modelType = SingleCalibrationModelType::PinholeCamera;
        calib::multicam::FusedPlaneGridModel fusedPlaneGrid;
    };

    std::vector<std::string> cameraIds_;
    std::shared_ptr<CalibrationBoardParams> boardParams_;
    MeasurementPlaneParams measurementPlaneParams_{};
    std::map<std::string, std::vector<cv::Mat>> calibrationImages_;
    std::map<std::string, CameraParameters> cameraParams_;

public:
    void addCamera(const std::string& cameraId) 
    {
        std::vector<std::string>().swap(cameraIds_);
        std::map<std::string, std::vector<cv::Mat>>().swap(calibrationImages_);

        if (std::find(cameraIds_.begin(), cameraIds_.end(), cameraId) == cameraIds_.end()) 
        {
            cameraIds_.push_back(cameraId);
            calibrationImages_[cameraId] = std::vector<cv::Mat>();
        }
    }

    void setCalibrationBoardParams(const std::shared_ptr<CalibrationBoardParams>& params) 
    {
        boardParams_ = params;
    }

    void getCalibrationBoardParams(std::shared_ptr<CalibrationBoardParams>& params)
    {
        params = boardParams_;
    }

    bool setMeasurementPlaneParams(const MeasurementPlaneParams& params, std::string& errorMessage)
    {
        if (!isFiniteValue(params.heightCompensation))
        {
            errorMessage = "heightCompensation must be finite.";
            return false;
        }

        measurementPlaneParams_ = params;
        return true;
    }

    MeasurementPlaneParams getMeasurementPlaneParams() const
    {
        return measurementPlaneParams_;
    }

    bool getCameraParams(CameraParams* params)
    {
        if (cameraIds_.size() > 0)
        {
            auto it = cameraParams_.find(cameraIds_[0]);
            if (it != cameraParams_.end())
            {
                CameraParameters internalParams = it->second;
                std::memset(params, 0, sizeof(*params));

                params->error = internalParams.error;
                params->hasError = 1;

                params->intervalX = internalParams.intervalX;
                params->hasIntervalX = 1;
                params->intervalY = internalParams.intervalY;
                params->hasIntervalY = 1;

                // 复制相机ID
                strncpy_s(params->cameraId, internalParams.cameraId.c_str(), sizeof(params->cameraId) - 1);
                params->cameraId[sizeof(params->cameraId) - 1] = '\0';

                if (internalParams.modelType == SingleCalibrationModelType::FusedPlaneGrid)
                {
                    params->intervalX = 0.0;
                    params->intervalY = 0.0;
                    params->hasIntervalX = 0;
                    params->hasIntervalY = 0;
                    if (!internalParams.homographyMatrix.empty() &&
                        internalParams.homographyMatrix.rows == 3 &&
                        internalParams.homographyMatrix.cols == 3)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            params->homographyMatrix[i] = internalParams.homographyMatrix.at<double>(i / 3, i % 3);
                        }
                        params->hasHomographyMatrix = 1;
                    }
                    return true;
                }

                // 复制内参矩阵
                if (!internalParams.intrinsic.empty() && internalParams.intrinsic.rows == 3 && internalParams.intrinsic.cols == 3)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        params->intrinsic[i] = internalParams.intrinsic.at<double>(i / 3, i % 3);
                    }
                    params->hasIntrinsic = 1;
                }
                else
                {
                    memset(params->intrinsic, 0, sizeof(params->intrinsic));
                    params->hasIntrinsic = 0;
                }

                // 复制畸变参数 (最多支持8个参数)
                if (!internalParams.distortion.empty())
                {
                    int count = std::min(internalParams.distortion.cols * internalParams.distortion.rows, 8);
                    for (int i = 0; i < count; i++)
                    {
                        params->distortion[i] = internalParams.distortion.at<double>(i / internalParams.distortion.cols, i % internalParams.distortion.cols);
                    }
                    // 清零未使用的元素
                    for (int i = count; i < 8; i++)
                    {
                        params->distortion[i] = 0.0;
                    }
                    params->hasDistortion = 1;
                }
                else
                {
                    memset(params->distortion, 0, sizeof(params->distortion));
                    params->hasDistortion = 0;
                }

                // 复制外参矩阵
                if (!internalParams.extrinsic.empty() && internalParams.extrinsic.rows == 4 && internalParams.extrinsic.cols == 4)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        params->extrinsic[i] = internalParams.extrinsic.at<double>(i / 4, i % 4);
                    }
                    params->hasExtrinsic = 1;
                }
                else
                {
                    memset(params->extrinsic, 0, sizeof(params->extrinsic));
                    params->hasExtrinsic = 0;
                }

                // 复制旋转向量
                if (!internalParams.rvec.empty() && internalParams.rvec.rows * internalParams.rvec.cols == 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        params->rvec[i] = internalParams.rvec.at<double>(i / internalParams.rvec.cols, i % internalParams.rvec.cols);
                    }
                    params->hasRvec = 1;
                }
                else
                {
                    memset(params->rvec, 0, sizeof(params->rvec));
                    params->hasRvec = 0;
                }

                // 复制平移向量
                if (!internalParams.tvec.empty() && internalParams.tvec.rows * internalParams.tvec.cols == 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        params->tvec[i] = internalParams.tvec.at<double>(i / internalParams.tvec.cols, i % internalParams.tvec.cols);
                    }
                    params->hasTvec = 1;
                }
                else
                {
                    memset(params->tvec, 0, sizeof(params->tvec));
                    params->hasTvec = 0;
                }

                // 复制单应性矩阵
                if (!internalParams.homographyMatrix.empty() && internalParams.homographyMatrix.rows == 3 && internalParams.homographyMatrix.cols == 3)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        params->homographyMatrix[i] = internalParams.homographyMatrix.at<double>(i / 3, i % 3);
                    }
                    params->hasHomographyMatrix = 1;
                }
                else
                {
                    memset(params->homographyMatrix, 0, sizeof(params->homographyMatrix));
                    params->hasHomographyMatrix = 0;
                }

                return true;
            }
        }
        
        return false;
    }


    void addCalibrationImage(const std::string& cameraId, const cv::Mat& image) 
    {
        if (std::find(cameraIds_.begin(), cameraIds_.end(), cameraId) != cameraIds_.end()) 
        {
            calibrationImages_[cameraId].push_back(image.clone());
        } 
        else 
        {
            std::cerr << "Camera ID " << cameraId << " not found. Please add camera first." << std::endl;
        }
    }

    bool calibrateCameraWithBoard(
        const std::string& cameraId,
        const std::vector<cv::Mat>* images,
        CameraParameters& camParams)
    {
        if (!images || images->empty())
        {
            std::cerr << "No calibration images for camera " << cameraId << std::endl;
            return false;
        }

        std::string errorMessage;
        auto calibrator = calib::boards::createCalibrationBoardCalibrator(boardParams_->type, errorMessage);
        if (!calibrator)
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }

        cv::Mat cameraMatrix, distCoeffs;
        std::vector<cv::Mat> rvecs, tvecs;
        double error = 0.0;
        if (!calibrator->calibrate(
            cameraId,
            *boardParams_,
            *images,
            cameraMatrix,
            distCoeffs,
            rvecs,
            tvecs,
            error,
            errorMessage))
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }

        camParams.error = error;
        camParams.intrinsic = cameraMatrix.clone();
        camParams.distortion = distCoeffs.clone();

        cv::Mat HomographyMatrix = cv::Mat::eye(3, 3, CV_64F);
        cv::Mat computedHomography;
        std::string homographyError;
        if (calibrator->getHomographyMatrix(*boardParams_, (*images)[0], cameraMatrix, distCoeffs, computedHomography, homographyError))
        {
            HomographyMatrix = computedHomography;
        }
        else if (!homographyError.empty())
        {
            std::cerr << homographyError << std::endl;
        }
        camParams.homographyMatrix = HomographyMatrix.clone();

        if (!rvecs.empty() && !tvecs.empty())
        {
            camParams.rvec = rvecs[0].clone();
            camParams.tvec = tvecs[0].clone();

            camParams.extrinsic = cv::Mat::eye(4, 4, CV_64F);
            cv::Mat rotMat;
            cv::Rodrigues(rvecs[0], rotMat);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    camParams.extrinsic.at<double>(i, j) = rotMat.at<double>(i, j);
                }
                camParams.extrinsic.at<double>(i, 3) = tvecs[0].at<double>(i, 0);
            }
        }

        cv::Size imageSize;
        imageSize.width = (*images)[0].cols;
        imageSize.height = (*images)[0].rows;
        int u = imageSize.width / 2;
        int v = imageSize.height / 2;
        std::vector<cv::Point2d> pixels = { cv::Point2d(u, v),
                                           cv::Point2d(u + 1, v),
                                           cv::Point2d(u, v + 1)
                                           };

        cameraParams_[cameraId] = camParams;
        std::vector<cv::Point3d> worldPts = pixelToWorld(cameraId, pixels);
        if (worldPts.size() < 3)
        {
            camParams.intervalX = 0;
            camParams.intervalY = 0;
        }
        else
        {
            camParams.intervalX = cv::norm(worldPts[1] - worldPts[0]);
            camParams.intervalY = cv::norm(worldPts[2] - worldPts[0]);
        }

        camParams.frameWidth = imageSize.width;
        camParams.frameHeight = imageSize.height;

        cv::Mat newCameraMatrix = cv::getOptimalNewCameraMatrix(cameraMatrix, distCoeffs, imageSize, 1, imageSize, 0);
        cv::initUndistortRectifyMap(cameraMatrix, distCoeffs, cv::Mat(), newCameraMatrix, imageSize, CV_16SC2, camParams.remapMap1, camParams.remapMap2);

        cameraParams_[cameraId] = camParams;
        return true;
    }

    bool calibrate() 
    {
        if (!boardParams_) 
        {
            std::cerr << "Calibration board parameters not set." << std::endl;
            return false;
        }

        bool success = true;

        // 对每个相机分别进行标定
        for (const auto& cameraId : cameraIds_) 
        {
            auto imageIt = calibrationImages_.find(cameraId);

            CameraParameters camParams;
            camParams.cameraId = cameraId;

            // 根据标定板类型执行不同的标定方法
            switch (boardParams_->type) 
            {
                case PIXEL_RATIO:
                {
                    // Pixel ratio calibration
                    auto pixelRatioParams = std::dynamic_pointer_cast<CalibrationBoardParams>(boardParams_);

                    camParams.modelType = SingleCalibrationModelType::PixelRatio;
                    camParams.intervalX = pixelRatioParams->distanceReal / pixelRatioParams->distancePixel;
                    camParams.intervalY = pixelRatioParams->distanceReal / pixelRatioParams->distancePixel;

                    camParams.frameWidth = 0;
                    camParams.frameHeight = 0;
                    camParams.error = 0;

                    cameraParams_[cameraId] = camParams;
                    break;
                }
                case CHESSBOARD:
                {
                    camParams.modelType = SingleCalibrationModelType::PinholeCamera;
                    // Chessboard calibration
                    const std::vector<cv::Mat>* images = imageIt != calibrationImages_.end() ? &imageIt->second : nullptr;
                    if (!calibrateCameraWithBoard(cameraId, images, camParams))
                    {
                        success = false;
                    }
                    break;
                }
                case CHARUCO: 
                {
                    if (imageIt == calibrationImages_.end() || imageIt->second.empty())
                    {
                        std::cerr << "No calibration images for camera " << cameraId << std::endl;
                        success = false;
                        continue;
                    }

                    // Charuco标定
                    auto charucoParams = std::dynamic_pointer_cast<CalibrationBoardParams>(boardParams_);
                    if (charucoParams) 
                    {
                        cv::Mat cameraMatrix, distCoeffs;
                        std::vector<cv::Mat> rvecs, tvecs;
                        double error;

                        std::string errorMessage;
                        auto calibrator = calib::boards::createCalibrationBoardCalibrator(boardParams_->type, errorMessage);
                        if (!calibrator)
                        {
                            std::cerr << errorMessage << std::endl;
                            success = false;
                            break;
                        }

                        bool calibSuccess = calibrator->calibrate(cameraId, *boardParams_, imageIt->second, cameraMatrix, distCoeffs, rvecs, tvecs, error, errorMessage);

                        if (calibSuccess) 
                        {
                            camParams.modelType = SingleCalibrationModelType::PinholeCamera;
                            camParams.error = error;

                            cv::Mat HomographyMatrix = cv::Mat::eye(3, 3, CV_64F);
                            if(imageIt->second.size() > 0)
                            {
                                cv::Mat computedHomography;
                                std::string homographyError;
                                if (calibrator->getHomographyMatrix(*boardParams_, imageIt->second[0], cameraMatrix, distCoeffs, computedHomography, homographyError))
                                {
                                    HomographyMatrix = computedHomography;
                                }
                                else if (!homographyError.empty())
                                {
                                    std::cerr << homographyError << std::endl;
                                }
                            }
                            else
                            {
                                HomographyMatrix = cv::Mat::eye(3, 3, CV_64F);
                            }

                            camParams.homographyMatrix = HomographyMatrix.clone();

                            camParams.intrinsic = cameraMatrix.clone();
                            camParams.distortion = distCoeffs.clone();

                            // 简化处理：只保存第一幅图像的外参
                            if (!rvecs.empty() && !tvecs.empty()) 
                            {
                                camParams.rvec = rvecs[0].clone();
                                camParams.tvec = tvecs[0].clone();

                                camParams.extrinsic = cv::Mat::eye(4, 4, CV_64F);
                                cv::Mat rotMat;
                                cv::Rodrigues(rvecs[0], rotMat);
                                for (int i = 0; i < 3; i++) 
                                {
                                    for (int j = 0; j < 3; j++) 
                                    {
                                        camParams.extrinsic.at<double>(i, j) = rotMat.at<double>(i, j);
                                    }
                                    camParams.extrinsic.at<double>(i, 3) = tvecs[0].at<double>(i, 0);
                                }
                            }

                            // 估算X、Y方向的像素当量
                            cv::Size imageSize;
                            imageSize.width = imageIt->second[0].cols;
                            imageSize.height = imageIt->second[0].rows;
                            // 图像中心和相邻点
                            int u = imageSize.width / 2;
                            int v = imageSize.height / 2;
                            std::vector<cv::Point2d> pixels = {cv::Point2d(u, v),
                                                               cv::Point2d(u + 1, v),
                                                               cv::Point2d(u, v + 1)
                                                               };

                            cameraParams_[cameraId] = camParams;
                            std::vector<cv::Point3d> worldPts = pixelToWorld(cameraId, pixels);
                            double dx; // x方向
                            double dy; // y方向
                            if (worldPts.size() < 3)
                            {
                                dx = 0;
                                dy = 0;
                            }
                            else
                            {
                                dx = cv::norm(worldPts[1] - worldPts[0]);
                                dy = cv::norm(worldPts[2] - worldPts[0]);
                            }
                            camParams.intervalX = dx;
                            camParams.intervalY = dy;

                            camParams.frameWidth = imageSize.width;
                            camParams.frameHeight = imageSize.height;

                            cv::Mat newCameraMatrix = cv::getOptimalNewCameraMatrix(cameraMatrix, distCoeffs, imageSize, 1, imageSize, 0);
                            cv::initUndistortRectifyMap(cameraMatrix, distCoeffs, cv::Mat(), newCameraMatrix, imageSize, CV_16SC2, camParams.remapMap1, camParams.remapMap2);

                            cameraParams_[cameraId] = camParams;
                        } 
                        else 
                        {
                            std::cerr << errorMessage << std::endl;
                            success = false;
                        }
                    } 
                    else 
                    {
                        std::cerr << "Invalid Charuco board parameters for camera " << cameraId << std::endl;
                        success = false;
                    }
                    break;
                }
                case CIRCLES_GRID:
                {
                    std::cout << "Circles grid calibration not yet implemented for camera " << cameraId << std::endl;
                    success = false;
                    break;
                }
                case ASYMMETRIC_CIRCLES_GRID:
                {
                    std::cout << "Asymmetric circles grid calibration not yet implemented for camera " << cameraId << std::endl;
                    success = false;
                    break;
                }
                    
                default:
                {
                    std::cerr << "Unsupported calibration board type for camera " << cameraId << std::endl;
                    success = false;
                }  
            }
        }

        return success;
    }

    bool saveCalibrationResults(const std::string& outputPath) 
    {
        // 确保输出目录存在
        std::filesystem::path requestedPath(outputPath);
        const bool outputIsFile = isYamlFilePath(requestedPath);
        if (outputIsFile && cameraIds_.size() != 1)
        {
            std::cerr << "Exact single-camera calibration output file requires exactly one camera." << std::endl;
            return false;
        }

        if (outputIsFile)
        {
            std::filesystem::path parentPath = requestedPath.parent_path();
            if (!parentPath.empty() && !std::filesystem::exists(parentPath))
            {
                std::filesystem::create_directories(parentPath);
            }
        }
        else if (!std::filesystem::exists(requestedPath))
        {
            std::filesystem::create_directories(requestedPath);
        }

        for (const auto& cameraId : cameraIds_) 
        {
            auto it = cameraParams_.find(cameraId);
            if (it != cameraParams_.end()) 
            {
                std::filesystem::path filename = outputIsFile
                    ? requestedPath
                    : requestedPath / (
                        "camera_" +
                        sanitizeFileNameSegment(cameraId, "camera") +
                        "_" +
                        boardTypeToFileSegment(boardParams_->type) +
                        ".yaml");
                cv::FileStorage fs(filename.string(), cv::FileStorage::WRITE);
                if (fs.isOpened()) 
                {
                    fs << "boardType" << (int)boardParams_->type;
                    fs << "boardCols" << boardParams_->width;
                    fs << "boardRows" << boardParams_->height;
                    fs << "squareSize" << boardParams_->squareSize;

                    fs << "dictionaryId" << boardParams_->dictionaryId;
                    fs << "markerSize" << boardParams_->markerSize;
                    fs << "squareSizePixel" << boardParams_->squareSizePixel;
                    fs << "markerSizePixel" << boardParams_->markerSizePixel;
                    fs << "measurementPlane" << "{"
                        << "heightCompensation" << measurementPlaneParams_.heightCompensation
                        << "}";

                    fs << "intervalX" << it->second.intervalX;
                    fs << "intervalY" << it->second.intervalY;
                    fs << "frameWidth" << it->second.frameWidth;
                    fs << "frameHeight" << it->second.frameHeight;
                    fs << "cameraId" << it->second.cameraId;
                    fs << "intrinsic" << it->second.intrinsic;
                    fs << "distortion" << it->second.distortion;
                    fs << "rvec" << it->second.rvec;
                    fs << "tvec" << it->second.tvec;
                    fs << "extrinsic" << it->second.extrinsic;
                    fs << "homographyMatrix" << it->second.homographyMatrix;

                    fs << "error" << it->second.error;

                    fs.release();
                    std::cout << "Saved calibration for camera " << cameraId << " to " << filename.string() << std::endl;
                } 
                else 
                {
                    std::cerr << "Failed to save calibration for camera " << cameraId << std::endl;
                    return false;
                }
            }
        }
        return true;
    }

    bool loadMultiCameraSingleCompatibility(cv::FileStorage& fs)
    {
        std::string errorMessage;

        const cv::FileNode boardNode = fs["board"];
        if (boardNode.empty() || !boardNode.isMap())
        {
            std::cerr << "Missing top-level board map in multi-camera calibration file." << std::endl;
            return false;
        }

        const cv::FileNode boardTypeNode = boardNode["type"];
        if (boardTypeNode.empty() || (!boardTypeNode.isString() && !boardTypeNode.isInt()))
        {
            std::cerr << "Field 'board.type' must be a board type string or integer." << std::endl;
            return false;
        }

        CalibrationBoardParams loadedBoard{};
        loadedBoard.type = boardTypeFromNode(boardTypeNode);
        if (loadedBoard.type == BOARD_UNKNOWN)
        {
            std::cerr << "Unsupported board.type in multi-camera calibration file." << std::endl;
            return false;
        }
        if (!readRequiredInt(boardNode, "width", loadedBoard.width, errorMessage) ||
            !readRequiredInt(boardNode, "height", loadedBoard.height, errorMessage) ||
            !readRequiredDouble(boardNode, "squareSize", loadedBoard.squareSize, errorMessage))
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }

        const cv::FileNode node = fs["singleCompatibility"];
        if (node.empty() || !node.isMap())
        {
            std::cerr << "Missing singleCompatibility for fused-plane single-camera compatibility." << std::endl;
            return false;
        }

        std::string compatibilityFormat;
        if (!readRequiredString(node, "format", compatibilityFormat, errorMessage))
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }
        if (compatibilityFormat != kFusedPlaneGridFormatName)
        {
            std::cerr << "Unsupported singleCompatibility format: " << compatibilityFormat << std::endl;
            return false;
        }

        std::string modelType;
        if (!readRequiredString(node, "modelType", modelType, errorMessage))
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }
        if (modelType != "FUSED_PLANE_GRID")
        {
            std::cerr << "Unsupported singleCompatibility modelType: " << modelType << std::endl;
            return false;
        }

        calib::multicam::FusedPlaneGridModel model;
        if (!readRequiredString(node, "virtualCameraId", model.virtualCameraId, errorMessage) ||
            !readRequiredString(node, "coordinateFrame", model.coordinateFrame, errorMessage) ||
            !readRequiredDouble(node, "heightCompensation", model.heightCompensation, errorMessage))
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }
        measurementPlaneParams_.heightCompensation = model.heightCompensation;
        if (model.virtualCameraId != "FUSED_CANVAS")
        {
            std::cerr << "Only virtualCameraId FUSED_CANVAS is supported in this compatibility version." << std::endl;
            return false;
        }

        const cv::FileNode canvasNode = node["canvas"];
        if (canvasNode.empty() || !canvasNode.isMap())
        {
            std::cerr << "singleCompatibility.canvas must be a map." << std::endl;
            return false;
        }
        if (!readRequiredInt(canvasNode, "width", model.canvasWidth, errorMessage) ||
            !readRequiredInt(canvasNode, "height", model.canvasHeight, errorMessage) ||
            !readRequiredHomography(node, "H_canvas_to_board", model.HCanvasToBoard, errorMessage) ||
            !readRequiredHomography(node, "H_board_to_canvas", model.HBoardToCanvas, errorMessage))
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }

        if (!readResidualGrid(
                node["canvasToBoardResidual"],
                calib::multicam::ResidualGridDomain::CanvasPixel,
                "gridOriginPixelX",
                "gridOriginPixelY",
                "gridStepPixelX",
                "gridStepPixelY",
                "dxWorld",
                "dyWorld",
                model.canvasToBoardResidual,
                errorMessage) ||
            !readResidualGrid(
                node["boardToCanvasResidual"],
                calib::multicam::ResidualGridDomain::BoardWorld,
                "gridOriginWorldX",
                "gridOriginWorldY",
                "gridStepWorldX",
                "gridStepWorldY",
                "dxPixel",
                "dyPixel",
                model.boardToCanvasResidual,
                errorMessage))
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }

        if (!model.validate(errorMessage))
        {
            std::cerr << errorMessage << std::endl;
            return false;
        }

        auto boardParams = std::make_shared<CalibrationBoardParams>();
        *boardParams = loadedBoard;
        boardParams_ = boardParams;

        CameraParameters params;
        params.cameraId = model.virtualCameraId;
        params.error = 0.0;
        params.modelType = SingleCalibrationModelType::FusedPlaneGrid;
        params.fusedPlaneGrid = model;
        params.homographyMatrix = model.HCanvasToBoard.clone();

        std::vector<std::string>().swap(cameraIds_);
        std::map<std::string, CameraParameters>().swap(cameraParams_);
        cameraIds_.push_back(params.cameraId);
        cameraParams_[params.cameraId] = params;
        return true;
    }

    bool loadCalibrationFile(const std::string& filePath) 
    {
        cv::FileStorage fs(filePath, cv::FileStorage::READ);
        if (!fs.isOpened()) 
        {
            std::cerr << "Failed to open calibration file: " << filePath << std::endl;
            return false;
        }

        // 加载标定板参数
        std::string errorMessage;
        std::string format;
        const cv::FileNode formatNode = fs["format"];
        if (!formatNode.empty() && formatNode.isString())
        {
            formatNode >> format;
        }
        if (format == kMultiCameraFormatName)
        {
            return loadMultiCameraSingleCompatibility(fs);
        }

        CalibrationBoardType boardType;
        int boardWidth, boardHeight;
        double squareSize;
        fs["boardType"] >> boardType;
        fs["boardCols"] >> boardWidth;
        fs["boardRows"] >> boardHeight;
        fs["squareSize"] >> squareSize;

        MeasurementPlaneParams loadedMeasurementPlane{};
        const cv::FileNode measurementPlaneNode = fs["measurementPlane"];
        if (!measurementPlaneNode.empty())
        {
            if (!measurementPlaneNode.isMap())
            {
                std::cerr << "measurementPlane must be a map." << std::endl;
                return false;
            }
            if (!readRequiredDouble(
                    measurementPlaneNode,
                    "heightCompensation",
                    loadedMeasurementPlane.heightCompensation,
                    errorMessage))
            {
                std::cerr << errorMessage << std::endl;
                return false;
            }
        }
        measurementPlaneParams_ = loadedMeasurementPlane;

        // 加载相机参数
        CameraParameters params;
        fs["intervalX"] >> params.intervalX;
        fs["intervalY"] >> params.intervalY;
        fs["frameWidth"] >> params.frameWidth;
        fs["frameHeight"] >> params.frameHeight;
        fs["cameraId"] >> params.cameraId;
        fs["intrinsic"] >> params.intrinsic;
        fs["distortion"] >> params.distortion;
        fs["rvec"] >> params.rvec;
        fs["tvec"] >> params.tvec;
        fs["extrinsic"] >> params.extrinsic;
        fs["homographyMatrix"] >> params.homographyMatrix;
        fs["error"] >> params.error;
        params.modelType = boardType == CalibrationBoardType::PIXEL_RATIO
            ? SingleCalibrationModelType::PixelRatio
            : SingleCalibrationModelType::PinholeCamera;

        switch (boardType) 
        {
            case PIXEL_RATIO:
            {
                boardParams_ = std::make_shared<CalibrationBoardParams>();
                boardParams_->type = static_cast<CalibrationBoardType>(boardType);
                break;
            }
            case CHESSBOARD:
            {
                cv::Size imageSize;
                imageSize.width = static_cast<int>(params.frameWidth);
                imageSize.height = static_cast<int>(params.frameHeight);
                cv::Mat newCameraMatrix = cv::getOptimalNewCameraMatrix(params.intrinsic, params.distortion, imageSize, 1, imageSize, 0);
                cv::initUndistortRectifyMap(params.intrinsic, params.distortion, cv::Mat(), newCameraMatrix, imageSize, CV_16SC2, params.remapMap1, params.remapMap2);

                std::shared_ptr<CalibrationBoardParams> chessboardParams = std::make_shared<CalibrationBoardParams>();
                chessboardParams->type = static_cast<CalibrationBoardType>(boardType);
                chessboardParams->width = boardWidth;
                chessboardParams->height = boardHeight;
                chessboardParams->squareSize = squareSize;
                boardParams_ = std::static_pointer_cast<CalibrationBoardParams>(chessboardParams);
                break;
            }
            case CHARUCO:
            {
                cv::Size imageSize;
                imageSize.width = static_cast<int>(params.frameWidth);
                imageSize.height = static_cast<int>(params.frameHeight);
                cv::Mat newCameraMatrix = cv::getOptimalNewCameraMatrix(params.intrinsic, params.distortion, imageSize, 1, imageSize, 0);
                cv::initUndistortRectifyMap(params.intrinsic, params.distortion, cv::Mat(), newCameraMatrix, imageSize, CV_16SC2, params.remapMap1, params.remapMap2);

                int dictionaryId;
                double markerSize;
                double squareSizePixel = -1.0;
                double markerSizePixel = -1.0;
                fs["dictionaryId"] >> dictionaryId;
                fs["markerSize"] >> markerSize;
                const cv::FileNode rootNode = fs.root();
                if (!readOptionalDouble(rootNode, "squareSizePixel", squareSizePixel, errorMessage) ||
                    !readOptionalDouble(rootNode, "markerSizePixel", markerSizePixel, errorMessage))
                {
                    std::cerr << errorMessage << std::endl;
                    return false;
                }

                // 创建并设置Charuco标定板参数
                boardParams_ = std::make_shared<CalibrationBoardParams>();
                boardParams_->type = static_cast<CalibrationBoardType>(boardType);
                boardParams_->width = boardWidth;
                boardParams_->height = boardHeight;
                boardParams_->squareSize = squareSize;
                boardParams_->dictionaryId = dictionaryId;
                boardParams_->markerSize = markerSize;
                boardParams_->squareSizePixel = squareSizePixel;
                boardParams_->markerSizePixel = markerSizePixel;

                break;
            }
            case CIRCLES_GRID:
            {
                std::cout << "Circles grid calibration not yet implemented for camera " << params.cameraId << std::endl;
                return false;
            }
            case ASYMMETRIC_CIRCLES_GRID:
            {
                std::cout << "Asymmetric circles grid calibration not yet implemented for camera " << params.cameraId << std::endl;
                return false;
            }
            default:
            {
                std::cerr << "Unsupported calibration board type: " << boardType << std::endl;
            }
                
            return false;
        }

        fs.release();

        std::vector<std::string>().swap(cameraIds_);
        std::map<std::string, CameraParameters>().swap(cameraParams_);
        cameraIds_.push_back(params.cameraId);
        cameraParams_[params.cameraId] = params;

        return true;
    }

    std::vector<cv::Point3d> pixelToWorld(const std::string& cameraId, const std::vector<cv::Point2d>& pixelPoints)
    {
        std::vector<cv::Point3d> worldPoints;

        auto it = cameraParams_.find(cameraId);
        if (it == cameraParams_.end())
        {
            std::cerr << "Camera parameters for " << cameraId << " not found." << std::endl;
            return worldPoints;
        }

        const CameraParameters& camParams = it->second;

        if (camParams.modelType == SingleCalibrationModelType::FusedPlaneGrid)
        {
            worldPoints.reserve(pixelPoints.size());
            for (const auto& pixel : pixelPoints)
            {
                cv::Point3d world;
                std::string errorMessage;
                if (!camParams.fusedPlaneGrid.pixelToWorld(pixel, world, errorMessage))
                {
                    std::cerr << errorMessage << std::endl;
                    return {};
                }
                worldPoints.push_back(world);
            }
            return worldPoints;
        }
        else if (camParams.modelType == SingleCalibrationModelType::PixelRatio)
        {
            if (std::isnan(camParams.intervalX) || std::isnan(camParams.intervalY))
            {
                std::cerr << "Camera parameters for " << cameraId << " are not calibrated." << std::endl;
                return worldPoints;
            }
            else
            {
                for (int i = 0; i < pixelPoints.size(); i++)
                {
                    cv::Point3d tmpPoint;
                    tmpPoint.x = pixelPoints[i].x * camParams.intervalX;
                    tmpPoint.y = pixelPoints[i].y * camParams.intervalY;
                    tmpPoint.z = measurementPlaneParams_.heightCompensation;
                    worldPoints.push_back(tmpPoint);
                } 
            }
        }
        else
        {
            if (camParams.intrinsic.empty() || camParams.distortion.empty() || camParams.rvec.empty() || camParams.tvec.empty())
            {
                std::cerr << "Camera parameters for " << cameraId << " are not calibrated." << std::endl;
                return worldPoints;
            }

            // 去畸变并转换为归一化图像坐标
            std::vector<cv::Point2d> undistortedPoints;
            cv::undistortPoints(pixelPoints, undistortedPoints, camParams.intrinsic, camParams.distortion);

            //// 将旋转向量转换为旋转矩阵
            //cv::Mat rotMat;
            //cv::Rodrigues(camParams.rvec, rotMat);
            //// 确保tvec是列向量
            //cv::Mat tvec = camParams.tvec.reshape(1, 3);  // 转换为3x1矩阵

            // 相机外参
            cv::Mat extrinsic = camParams.extrinsic;
            const double planeZ = measurementPlaneParams_.heightCompensation;

            // Back-project each point to the measurement plane.
            for (const auto& pt : undistortedPoints)
            {
                double x = pt.x;
                double y = pt.y;

                // 构造线性方程组: A * [X, Y]^T = b
                cv::Mat A = (cv::Mat_<double>(2, 2) <<
                    extrinsic.at<double>(0, 0) - extrinsic.at<double>(2, 0) * x, extrinsic.at<double>(0, 1) - extrinsic.at<double>(2, 1) * x,
                    extrinsic.at<double>(1, 0) - extrinsic.at<double>(2, 0) * y, extrinsic.at<double>(1, 1) - extrinsic.at<double>(2, 1) * y);

                cv::Mat b = (cv::Mat_<double>(2, 1) <<
                    x * (extrinsic.at<double>(2, 2) * planeZ + extrinsic.at<double>(2, 3)) -
                        (extrinsic.at<double>(0, 2) * planeZ + extrinsic.at<double>(0, 3)),
                    y * (extrinsic.at<double>(2, 2) * planeZ + extrinsic.at<double>(2, 3)) -
                        (extrinsic.at<double>(1, 2) * planeZ + extrinsic.at<double>(1, 3)));

                // 求解方程组
                cv::Mat worldXY;
                cv::solve(A, b, worldXY);

                // Append the solved world point.
                worldPoints.push_back(cv::Point3d(static_cast<double>(worldXY.at<double>(0, 0)),
                    static_cast<double>(worldXY.at<double>(1, 0)),
                    planeZ));
            }
        }

        return worldPoints;
    }


    std::vector<cv::Point3d> pixelToWorldByHomography(const std::string& cameraId, const std::vector<cv::Point2d>& pixelPoints)
    {
        std::vector<cv::Point3d> worldPoints;

        auto it = cameraParams_.find(cameraId);
        if (it == cameraParams_.end())
        {
            std::cerr << "Camera parameters for " << cameraId << " not found." << std::endl;
            return worldPoints;
        }

        const CameraParameters& camParams = it->second;

        if (camParams.modelType == SingleCalibrationModelType::PixelRatio)
        {
            if (std::isnan(camParams.intervalX) || std::isnan(camParams.intervalY))
            {
                std::cerr << "Camera parameters for " << cameraId << " are not calibrated." << std::endl;
                return worldPoints;
            }
            else
            {
                for (int i = 0; i < pixelPoints.size(); i++)
                {
                    cv::Point3d tmpPoint;
                    tmpPoint.x = pixelPoints[i].x * camParams.intervalX;
                    tmpPoint.y = pixelPoints[i].y * camParams.intervalY;
                    tmpPoint.z = measurementPlaneParams_.heightCompensation;
                    worldPoints.push_back(tmpPoint);
                }
            }
        }
        else
        {
            if (camParams.intrinsic.empty() || camParams.distortion.empty() || camParams.rvec.empty() || camParams.tvec.empty())
            {
                std::cerr << "Camera parameters for " << cameraId << " are not calibrated." << std::endl;
                return worldPoints;
            }

            // 去畸变并转换为归一化图像坐标
            std::vector<cv::Point2d> undistortedPoints;
            cv::undistortPoints(pixelPoints, undistortedPoints, camParams.intrinsic, camParams.distortion);

            // 单应性矩阵
            cv::Mat homographyMatrix = camParams.homographyMatrix;

            // Map each point through the homography plane.
            cv::Mat homogeneousPixels = cv::Mat::ones(3, static_cast<int>(undistortedPoints.size()), CV_64F);
            for (size_t i = 0; i < undistortedPoints.size(); ++i)
            {
                const int column = static_cast<int>(i);
                homogeneousPixels.at<double>(0, column) = undistortedPoints[i].x;
                homogeneousPixels.at<double>(1, column) = undistortedPoints[i].y;
            }

            // 应用单应性变换 H * homogeneousPixels
            cv::Mat realCoordsHomo = homographyMatrix * homogeneousPixels;

            // Convert back from homogeneous coordinates and use the configured output Z.
            worldPoints.reserve(undistortedPoints.size());
            for (size_t i = 0; i < undistortedPoints.size(); ++i)
            {
                const int column = static_cast<int>(i);
                double w = realCoordsHomo.at<double>(2, column);
                double x = static_cast<double>(realCoordsHomo.at<double>(0, column) / w);
                double y = static_cast<double>(realCoordsHomo.at<double>(1, column) / w);
                worldPoints.emplace_back(x, y, measurementPlaneParams_.heightCompensation);
            }
        }

        return worldPoints;
    }


    std::vector<cv::Point2d> worldToPixel(const std::string& cameraId, const std::vector<cv::Point3d>& worldPoints)
    {
        std::vector<cv::Point2d> pixelPoints;

        auto it = cameraParams_.find(cameraId);
        if (it == cameraParams_.end())
        {
            std::cerr << "Camera parameters for " << cameraId << " not found." << std::endl;
            return pixelPoints;
        }

        const CameraParameters& camParams = it->second;

        if (camParams.modelType == SingleCalibrationModelType::FusedPlaneGrid)
        {
            pixelPoints.reserve(worldPoints.size());
            for (const auto& world : worldPoints)
            {
                cv::Point2d pixel;
                std::string errorMessage;
                if (!camParams.fusedPlaneGrid.worldToPixel(world, pixel, errorMessage))
                {
                    std::cerr << errorMessage << std::endl;
                    return {};
                }
                pixelPoints.push_back(pixel);
            }
            return pixelPoints;
        }
        else if (camParams.modelType == SingleCalibrationModelType::PixelRatio)
        {
            if (std::isnan(camParams.intervalX) || std::isnan(camParams.intervalY) ||
                camParams.intervalX == 0 || camParams.intervalY == 0)
            {
                std::cerr << "Camera parameters for " << cameraId << " are not calibrated." << std::endl;
                return pixelPoints;
            }
            else
            {
                for (int i = 0; i < worldPoints.size(); i++)
                {
                    cv::Point2d tmpPoint;
                    tmpPoint.x = worldPoints[i].x / camParams.intervalX;
                    tmpPoint.y = worldPoints[i].y / camParams.intervalY;
                    pixelPoints.push_back(tmpPoint);
                }
            }
        }
        else
        {
            if (camParams.intrinsic.empty() || camParams.distortion.empty() || camParams.rvec.empty() || camParams.tvec.empty())
            {
                std::cerr << "Camera parameters for " << cameraId << " are not calibrated." << std::endl;
                return pixelPoints;
            }

            // 使用OpenCV的projectPoints函数将3D点投影到图像平面
            cv::projectPoints(worldPoints, camParams.rvec, camParams.tvec, camParams.intrinsic, camParams.distortion, pixelPoints);
        }

        return pixelPoints;
    }


    cv::Mat imageCorrection(const std::string& cameraId, void* inImageData, int inW, int inH, int inC, int inType)
    {
        int depth = CV_MAT_DEPTH(inType);
        cv::Mat image = cv::Mat(inH, inW, CV_MAKETYPE(depth, inC), inImageData);

        auto it = cameraParams_.find(cameraId);
        if (it == cameraParams_.end())
        {
            std::cerr << "Camera parameters for " << cameraId << " not found." << std::endl;
            return image;
        }

        const CameraParameters& camParams = it->second;

        cv::Mat outImage;
        if (boardParams_->type == CalibrationBoardType::PIXEL_RATIO)
        {
            std::cerr << "Pixel ratio calibration mode does not support image correction." << std::endl;
            return image;
        }
        else
        {
            // Step 1: 去畸变
            cv::Mat undistorted;
            if (!camParams.remapMap1.empty() && !camParams.remapMap2.empty())
            {
                cv::remap(image, undistorted, camParams.remapMap1, camParams.remapMap2, cv::INTER_LINEAR);
            }
            else
            {
                // 如果没有预计算的 remapMap，就直接用 undistort
                cv::undistort(image, undistorted, camParams.intrinsic, camParams.distortion);
            }

            // float outW = (boardParams_->squareSize * boardParams_->width) / camParams.intervalX;
            // float outH = (boardParams_->squareSize * boardParams_->height) / camParams.intervalY;
            // float offsetX = (camParams.frameWidth - outW) / 2.0f;
            // float offsetY = (camParams.frameHeight - outH) / 2.0f;

            // std::vector<cv::Point2f> dstCorners =
            // {
            //     cv::Point2f(0 + offsetX,        0 + offsetY),
            //     cv::Point2f(outW - 1 + offsetX, 0 + offsetY),
            //     cv::Point2f(outW - 1 + offsetX, outH - 1 + offsetY),
            //     cv::Point2f(0 + offsetX,        outH - 1 + offsetY)
            // };

            // std::vector<cv::Point3f> objCorners =
            // {
            //     cv::Point3f(0, 0, 0),
            //     cv::Point3f(boardParams_->squareSize * boardParams_->width, 0, 0),
            //     cv::Point3f(boardParams_->squareSize * boardParams_->width, boardParams_->squareSize * boardParams_->height, 0),
            //     cv::Point3f(0, boardParams_->squareSize * boardParams_->height, 0)
            // };

            // std::vector<cv::Point2f> srcCorners;
            // //cv::projectPoints(objCorners, camParams.rvec, camParams.tvec, camParams.intrinsic, camParams.distortion, srcCorners);
            // cv::projectPoints(objCorners, camParams.rvec, camParams.tvec, camParams.intrinsic, cv::noArray(), srcCorners);

            // cv::Mat H = cv::findHomography(srcCorners, dstCorners);
            // cv::warpPerspective(undistorted, undistorted, H, cv::Size(camParams.frameWidth, camParams.frameHeight));

            outImage = undistorted;
        }

        return outImage;
    }


};


// 线程局部错误字符串（每线程一份）
static thread_local std::string g_last_error;

static void set_last_error(const std::string& msg)
{
    g_last_error = msg;
}

struct MultiCameraCalibrationFrameworkImpl
{
    calib::multicam::MultiCameraCalibrationFramework framework;
};

namespace
{
    std::string readFixedString(const char* value, std::size_t capacity)
    {
        if (!value || capacity == 0)
        {
            return {};
        }
        return std::string(value, strnlen_s(value, capacity));
    }

    void copyStringToFixedBuffer(const std::string& value, char* buffer, std::size_t capacity)
    {
        if (!buffer || capacity == 0)
        {
            return;
        }
        buffer[0] = '\0';
        strncpy_s(buffer, capacity, value.c_str(), _TRUNCATE);
    }

    bool copyOptions(const MultiCameraCalibrationOptions& source, calib::multicam::MultiCameraOptions& target, std::string& errorMessage)
    {
        switch (source.anchorMode)
        {
        case MULTICAM_ANCHOR_BOARD_POSE:
            target.anchorMode = calib::multicam::AnchorMode::BoardPose;
            break;
        case MULTICAM_ANCHOR_CAMERA:
            target.anchorMode = calib::multicam::AnchorMode::Camera;
            break;
        case MULTICAM_ANCHOR_EXTERNAL:
            target.anchorMode = calib::multicam::AnchorMode::External;
            break;
        default:
            errorMessage = "Unsupported multi-camera anchor mode.";
            return false;
        }

        target.referenceCameraId = readFixedString(source.referenceCameraId, sizeof(source.referenceCameraId));
        target.referenceCaptureId = readFixedString(source.referenceCaptureId, sizeof(source.referenceCaptureId));
        target.refineIntrinsics = source.refineIntrinsics != 0;
        target.refineDistortion = source.refineDistortion != 0;
        target.minCornersPerObservation = source.minCornersPerObservation > 0 ? source.minCornersPerObservation : target.minCornersPerObservation;
        target.maxReprojectionErrorForInit = source.maxReprojectionErrorForInit > 0.0 ? source.maxReprojectionErrorForInit : target.maxReprojectionErrorForInit;
        target.robustLossScale = source.robustLossScale > 0.0 ? source.robustLossScale : target.robustLossScale;
        target.maxIterations = source.maxIterations > 0 ? source.maxIterations : target.maxIterations;
        return true;
    }

    bool copyBlendMode(MultiCameraBlendMode source, calib::multicam::BlendMode& target, std::string& errorMessage)
    {
        switch (source)
        {
        case MULTICAM_BLEND_OVERLAY:
            target = calib::multicam::BlendMode::Overlay;
            return true;
        case MULTICAM_BLEND_AVERAGE:
            target = calib::multicam::BlendMode::Average;
            return true;
        default:
            errorMessage = "Unsupported multi-camera blend mode.";
            return false;
        }
    }

    void copyMat3x3ToArray(const cv::Mat& matrix, double* values)
    {
        for (int index = 0; index < 9; ++index)
        {
            values[index] = matrix.empty() ? 0.0 : matrix.at<double>(index / 3, index % 3);
        }
    }

    void copyDistortionToArray(const cv::Mat& distortion, double* values)
    {
        for (int index = 0; index < 8; ++index)
        {
            values[index] = 0.0;
        }
        if (distortion.empty())
        {
            return;
        }

        cv::Mat converted;
        distortion.convertTo(converted, CV_64F);
        cv::Mat flat = converted.reshape(1, 1);
        const int count = std::min(8, static_cast<int>(flat.total()));
        for (int index = 0; index < count; ++index)
        {
            values[index] = flat.at<double>(0, index);
        }
    }

    void copyTransformToArray(const cv::Mat& transform, double* values)
    {
        for (int index = 0; index < 16; ++index)
        {
            values[index] = transform.empty() ? 0.0 : transform.at<double>(index / 4, index % 4);
        }
    }

    void copyCommonFromCameraPose(const cv::Mat& transform, double* rvecValues, double* tvecValues)
    {
        for (int index = 0; index < 3; ++index)
        {
            rvecValues[index] = 0.0;
            tvecValues[index] = 0.0;
        }
        if (transform.empty() || transform.rows != 4 || transform.cols != 4 || transform.type() != CV_64FC1)
        {
            return;
        }

        cv::Mat rotation = transform(cv::Rect(0, 0, 3, 3)).clone();
        cv::Mat rvec;
        cv::Rodrigues(rotation, rvec);
        for (int index = 0; index < 3; ++index)
        {
            rvecValues[index] = rvec.at<double>(index, 0);
            tvecValues[index] = transform.at<double>(index, 3);
        }
    }

    void copyReport(const calib::multicam::MultiCameraReport& source, MultiCameraCalibrationReport* target)
    {
        target->cameraCount = source.cameraCount;
        target->captureCount = source.captureCount;
        target->observationCount = source.observationCount;
        target->residualCount = source.residualCount;
        target->initialRmsError = source.initialRmsError;
        target->finalRmsError = source.finalRmsError;
        target->maxReprojectionError = source.maxReprojectionError;
        target->connectedComponentCount = source.connectedComponentCount;
        target->converged = source.converged ? 1 : 0;
        target->ceresTerminationType = source.ceresTerminationType;
    }

    int errorCodeForMessage(const std::string& message)
    {
        if (message.find("disconnected") != std::string::npos)
        {
            return CAMCALIB_ERR_GRAPH_DISCONNECTED;
        }
        if (message.find("loaded or calibrated") != std::string::npos ||
            message.find("successfully loaded or calibrated") != std::string::npos)
        {
            return CAMCALIB_ERR_NOT_CALIBRATED;
        }
        if (message.find("Ceres") != std::string::npos ||
            message.find("Bundle adjustment") != std::string::npos ||
            message.find("optimization") != std::string::npos)
        {
            return CAMCALIB_ERR_OPTIMIZATION;
        }
        return CAMCALIB_ERR_UNEXP;
    }
}


// C接口实现
extern "C" 
{
    CAMERA_CALIBRATION_SDK_API const char* getLastError()
    {
        return g_last_error.empty() ? nullptr : g_last_error.c_str();
    }

    // 创建标定框架实例
    CAMERA_CALIBRATION_SDK_API CameraCalibrationFrameworkHandle createCalibrationFramework() 
    {
        try 
        {
            CameraCalibrationFramework* framework = new CameraCalibrationFramework();
            return reinterpret_cast<CameraCalibrationFrameworkHandle>(framework);
        } 
        catch (...) 
        {
            return nullptr;
        }
    }

    // 销毁标定框架实例
    CAMERA_CALIBRATION_SDK_API void destroyCalibrationFramework(CameraCalibrationFrameworkHandle handle) 
    {
        if (handle) 
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            delete framework;
        }
    }

    // 添加相机
    CAMERA_CALIBRATION_SDK_API int addCamera(CameraCalibrationFrameworkHandle handle, const char* cameraId) 
    {
        if (!handle || !cameraId)
        {
            set_last_error("Camera calibration handle or camera id is null.");
            return CAMCALIB_ERR_NULL;
        }
            
        try 
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            framework->addCamera(std::string(cameraId));
            return 0;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in addCamera: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...) 
        {
            set_last_error("Unknown exception in addCamera");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    // 设置标定板参数
    CAMERA_CALIBRATION_SDK_API int setCalibrationBoardParams(CameraCalibrationFrameworkHandle handle, const CalibrationBoardParams* params)
    {
        if (!handle)
        {
            set_last_error("Camera calibration handle is null.");
            return CAMCALIB_ERR_NULL;
        }
        if (!params)
        {
            set_last_error("CalibrationBoardParams pointer is null.");
            return CAMCALIB_ERR_NULL;
        }
            
        try 
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);

            switch (params->type)
            {
                case PIXEL_RATIO:
                {
                    auto p = std::make_shared<CalibrationBoardParams>();
                    p->type = PIXEL_RATIO;
                    p->distanceReal = params->distanceReal;
                    p->distancePixel = params->distancePixel;

                    framework->setCalibrationBoardParams(p);
                    break;
                }
                case CHESSBOARD:
                {
                    auto p = std::make_shared<CalibrationBoardParams>();
                    p->type = CHESSBOARD;
                    p->width = params->width;
                    p->height = params->height;
                    p->squareSize = params->squareSize;

                    if (p->width <= 0 || p->height <= 0 || p->squareSize <= 0.0f)
                    {
                        set_last_error("Chessboard: width/height/squareSize must be > 0");
                        return CAMCALIB_ERR_INVALID;
                    }

                    framework->setCalibrationBoardParams(p);
                    break;
                }
                case CHARUCO:
                {
                    if (params->width <= 0 || params->height <= 0)
                    {
                        set_last_error("Charuco: width/height must be > 0");
                        return CAMCALIB_ERR_INVALID;
                    }
                    if (params->dictionaryId < 0)
                    {
                        set_last_error("Charuco: dictionaryId must be > 0 (no default allowed)");
                        return CAMCALIB_ERR_INVALID;
                    }
                    if (params->markerSize <= 0.0f)
                    {
                        set_last_error("Charuco: markerSize must be > 0 (no default allowed)");
                        return CAMCALIB_ERR_INVALID;
                    }
                    if (params->squareSize <= 0.0f)
                    {
                        set_last_error("Charuco: squareSize must be > 0");
                        return CAMCALIB_ERR_INVALID;
                    }
                    if (params->markerSize >= params->squareSize)
                    {
                        set_last_error("Charuco: markerSize must be < squareSize");
                        return CAMCALIB_ERR_INVALID;
                    }
                    if (params->squareSizePixel > 0.0 && params->markerSizePixel > 0.0 &&
                        params->markerSizePixel >= params->squareSizePixel)
                    {
                        set_last_error("Charuco: markerSizePixel must be < squareSizePixel when both are positive");
                        return CAMCALIB_ERR_INVALID;
                    }

                    auto p = std::make_shared<CalibrationBoardParams>();
                    p->type = CHARUCO;
                    p->width = params->width;
                    p->height = params->height;
                    p->squareSize = params->squareSize;
                    p->dictionaryId = params->dictionaryId;
                    p->markerSize = params->markerSize;
                    p->squareSizePixel = params->squareSizePixel;
                    p->markerSizePixel = params->markerSizePixel;
                    framework->setCalibrationBoardParams(p);
                    break;
                }
                case CIRCLES_GRID:
                {
                    set_last_error("Circles grid calibration not yet implemented.");
                    return CAMCALIB_ERR_UNSUP;
                }
                case ASYMMETRIC_CIRCLES_GRID:
                {
                    set_last_error("Asymmetric circles grid calibration not yet implemented.");
                    return CAMCALIB_ERR_UNSUP;
                }
                default:
                {
                    set_last_error("Unsupported calibration board type");
                    return CAMCALIB_ERR_UNSUP;
                }
            }

            set_last_error(""); // 清空
            return CAMCALIB_OK;
        } 
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in setCalibrationBoardParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in setCalibrationBoardParams");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    // 获取标定板参数
    CAMERA_CALIBRATION_SDK_API int getCalibrationBoardParams(CameraCalibrationFrameworkHandle handle, CalibrationBoardParams* params)
    {
        if (!handle)
        {
            set_last_error("Camera calibration handle is null.");
            return CAMCALIB_ERR_NULL;
        }

        if (!params)
        {
            set_last_error("CalibrationBoardParams pointer is null.");
            return CAMCALIB_ERR_NULL;
        }

        try
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            std::shared_ptr<CalibrationBoardParams> p = std::make_shared<CalibrationBoardParams>();
            framework->getCalibrationBoardParams(p);
            if (!p)
            {
                set_last_error("Calibration board parameters not set.");
                return CAMCALIB_ERR_INVALID;
            }

            params->type = p->type;
            params->height = p->height;
            params->width = p->width;
            params->squareSize = p->squareSize;
            params->markerSize = p->markerSize;
            params->dictionaryId = p->dictionaryId;
            params->squareSizePixel = p->squareSizePixel;
            params->markerSizePixel = p->markerSizePixel;
            params->distanceReal = p->distanceReal;
            params->distancePixel = p->distancePixel;

            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in getCalibrationBoardParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in getCalibrationBoardParams");
            return CAMCALIB_ERR_UNEXP;
        }
        
    }


    // 获取相机内参外参
    CAMERA_CALIBRATION_SDK_API int setMeasurementPlaneParams(CameraCalibrationFrameworkHandle handle, const MeasurementPlaneParams* params)
    {
        if (!handle || !params)
        {
            set_last_error("Camera calibration handle or measurement plane params is null.");
            return CAMCALIB_ERR_NULL;
        }

        try
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            std::string errorMessage;
            if (!framework->setMeasurementPlaneParams(*params, errorMessage))
            {
                set_last_error(errorMessage);
                return CAMCALIB_ERR_INVALID;
            }

            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in setMeasurementPlaneParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in setMeasurementPlaneParams");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int getMeasurementPlaneParams(CameraCalibrationFrameworkHandle handle, MeasurementPlaneParams* params)
    {
        if (!handle || !params)
        {
            set_last_error("Camera calibration handle or measurement plane params output is null.");
            return CAMCALIB_ERR_NULL;
        }

        try
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            *params = framework->getMeasurementPlaneParams();
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in getMeasurementPlaneParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in getMeasurementPlaneParams");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_C_API int getCameraParams(CameraCalibrationFrameworkHandle handle, CameraParams* params)
    {
        if (!handle)
        {
            set_last_error("Camera calibration handle is null.");
            return CAMCALIB_ERR_NULL;
        }

        if (!params)
        {
            set_last_error("CameraParams pointer is null.");
            return CAMCALIB_ERR_NULL;
        }

        try
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);

            // 获取指定相机的参数
            if (!framework->getCameraParams(params))
            {
                set_last_error("Camera parameters not found.");
                return CAMCALIB_ERR_INVALID;
            }

            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in getCameraParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in getCameraParams");
            return CAMCALIB_ERR_UNEXP;
        }


    }


    // 添加相机标定图片 (传入图片路径)
    CAMERA_CALIBRATION_SDK_API int addCalibrationImagePath(CameraCalibrationFrameworkHandle handle,
                                                           const char* cameraId,
                                                           const char* imagePath) 
    {
        if (!handle || !cameraId || !imagePath)
        {
            set_last_error("Camera calibration handle or camera id or image path is null.");
            return CAMCALIB_ERR_NULL;
        }
        try 
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            cv::Mat image = cv::imread(imagePath, 0);
            if (image.empty())
            {
                set_last_error("Failed to open the image in addCalibrationImagePath.");
                return CAMCALIB_ERR_IO; // 图片读取失败
            }  
            framework->addCalibrationImage(std::string(cameraId), image);

            set_last_error("");
            return CAMCALIB_OK;
        } 
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in addCalibrationImagePath: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in addCalibrationImagePath");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    // 执行标定
    CAMERA_CALIBRATION_SDK_API int calibrate(CameraCalibrationFrameworkHandle handle) 
    {
        if (!handle) 
        {
            set_last_error("Camera calibration handle is null.");
            return CAMCALIB_ERR_NULL;
        }
        try 
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            bool result = framework->calibrate();

            if (result)
            {
                set_last_error("");
                return CAMCALIB_OK;
            }
            else
            {
                set_last_error("Calibration failed in calibrate.");
                return CAMCALIB_ERR_UNEXP;
            }
        } 
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in calibrate: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in calibrate");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    // 保存标定结果到yaml文件
    CAMERA_CALIBRATION_SDK_API int saveCalibrationResults(CameraCalibrationFrameworkHandle handle,
                                                          const char* outputPath) 
    {
        if (!handle || !outputPath) 
        {
            set_last_error("Camera calibration handle or outputPath is null.");
            return CAMCALIB_ERR_NULL;
        }
        try 
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            bool result = framework->saveCalibrationResults(std::string(outputPath));
            
            if (result)
            {
                set_last_error("");
                return CAMCALIB_OK;
            }
            else
            {
                set_last_error("Failed in saveCalibrationResults.");
                return CAMCALIB_ERR_UNEXP;
            }
        } 
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in saveCalibrationResults: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in saveCalibrationResults");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    // 加载标定文件
    CAMERA_CALIBRATION_SDK_API int loadCalibrationFile(CameraCalibrationFrameworkHandle handle,
                                                       const char* filePath) 
    {
        if (!handle || !filePath) 
        {
            set_last_error("Camera calibration handle or calib file Path is null.");
            return CAMCALIB_ERR_NULL;
        }
        try 
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            bool result = framework->loadCalibrationFile(std::string(filePath));
            
            if (result)
            {
                set_last_error("");
                return CAMCALIB_OK;
            }
            else
            {
                set_last_error("Failed in loadCalibrationFile.");
                return CAMCALIB_ERR_UNEXP;
            }
        } 
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in loadCalibrationFile: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in loadCalibrationFile");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    // 像素坐标转世界坐标
    CAMERA_CALIBRATION_SDK_API int pixelToWorld(CameraCalibrationFrameworkHandle handle, const char* cameraId, 
        double pixelX, double pixelY, double* worldX, double* worldY, double* worldZ)
    {
        if (!handle || !cameraId) 
        {
            set_last_error("Camera calibration handle or camera Id is null.");
            return CAMCALIB_ERR_NULL;
        }
        try 
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            std::vector<cv::Point2d> pixelPoints = { cv::Point2d(pixelX, pixelY) };
            std::vector<cv::Point3d> worldPoints = framework->pixelToWorld(std::string(cameraId), pixelPoints);
            
            if (worldPoints.empty())
            {
                set_last_error("Failed in pixelToWorld.");
                return CAMCALIB_ERR_UNEXP;
            }
                
            *worldX = worldPoints[0].x;
            *worldY = worldPoints[0].y;
            *worldZ = worldPoints[0].z;

            set_last_error("");
            return CAMCALIB_OK;
        } 
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in pixelToWorld: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in pixelToWorld");
            return CAMCALIB_ERR_UNEXP;
        }
    }


    CAMERA_CALIBRATION_SDK_API int pixelToWorldByHomography(CameraCalibrationFrameworkHandle handle, const char* cameraId,
        double pixelX, double pixelY, double* worldX, double* worldY, double* worldZ)
    {
        if (!handle || !cameraId)
        {
            set_last_error("Camera calibration handle or camera Id is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            std::vector<cv::Point2d> pixelPoints = { cv::Point2d(pixelX, pixelY) };
            std::vector<cv::Point3d> worldPoints = framework->pixelToWorldByHomography(std::string(cameraId), pixelPoints);

            if (worldPoints.empty())
            {
                set_last_error("Failed in pixelToWorldByHomography.");
                return CAMCALIB_ERR_UNEXP;
            }

            *worldX = worldPoints[0].x;
            *worldY = worldPoints[0].y;
            *worldZ = worldPoints[0].z;

            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in pixelToWorldByHomography: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in pixelToWorldByHomography");
            return CAMCALIB_ERR_UNEXP;
        }
    }


    // 世界坐标转像素坐标
    CAMERA_CALIBRATION_SDK_API int worldToPixel(CameraCalibrationFrameworkHandle handle, const char* cameraId,
        double worldX, double worldY, double worldZ, double* pixelX, double* pixelY)
    {
        if (!handle || !cameraId)
        {
            set_last_error("Camera calibration handle or camera Id is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            std::vector<cv::Point3d> worldPoints = { cv::Point3d(worldX, worldY, worldZ) };
            std::vector<cv::Point2d> pixelPoints = framework->worldToPixel(std::string(cameraId), worldPoints);

            if (pixelPoints.empty())
            {
                set_last_error("Failed in worldToPixel.");
                return CAMCALIB_ERR_UNEXP;
            }

            *pixelX = pixelPoints[0].x;
            *pixelY = pixelPoints[0].y;

            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in worldToPixel: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in worldToPixel");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    // 图像校正
    CAMERA_CALIBRATION_SDK_API int imageCorrection(CameraCalibrationFrameworkHandle handle, const char* cameraId,
                                                   void* inImageData, int inW, int inH, int inC, int inType, 
                                                   void** outImageData, int& outW, int& outH, int& outC, int& outType)
    {
        if (!handle || !cameraId)
        {
            set_last_error("Camera calibration handle or camera Id is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            CameraCalibrationFramework* framework = reinterpret_cast<CameraCalibrationFramework*>(handle);
            cv::Mat image = framework->imageCorrection(std::string(cameraId), inImageData, inW, inH, inC, inType);
            
            outW = image.cols;
            outH = image.rows;
            outC = image.channels();
            outType = image.type();

            int depth = CV_MAT_DEPTH(inType);    // 获取像素深度
            int channels = outC;                 // 已经通过 image.channels() 获取

            int elementSize = image.elemSize();
            size_t dataSize = static_cast<size_t>(outW) * static_cast<size_t>(outH) * elementSize;

            *outImageData = malloc(dataSize);
            memcpy(*outImageData, image.data, dataSize);

            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in imageCorrection: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in imageCorrection");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API MultiCameraCalibrationFrameworkHandle createMultiCameraCalibrationFramework()
    {
        try
        {
            auto* framework = new MultiCameraCalibrationFrameworkImpl();
            set_last_error("");
            return framework;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in createMultiCameraCalibrationFramework: ") + e.what());
            return nullptr;
        }
        catch (...)
        {
            set_last_error("Unknown exception in createMultiCameraCalibrationFramework");
            return nullptr;
        }
    }

    CAMERA_CALIBRATION_SDK_API void destroyMultiCameraCalibrationFramework(MultiCameraCalibrationFrameworkHandle handle)
    {
        delete handle;
    }

    CAMERA_CALIBRATION_SDK_API int multiSetCalibrationBoardParams(MultiCameraCalibrationFrameworkHandle handle, const CalibrationBoardParams* params)
    {
        if (!handle || !params)
        {
            set_last_error("Multi-camera calibration handle or board params is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            if (!handle->framework.setBoardParams(*params, errorMessage))
            {
                set_last_error(errorMessage);
                if (params->type == CIRCLES_GRID || params->type == ASYMMETRIC_CIRCLES_GRID)
                {
                    return CAMCALIB_ERR_UNSUP;
                }
                return CAMCALIB_ERR_INVALID;
            }
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiSetCalibrationBoardParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiSetCalibrationBoardParams");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiSetMeasurementPlaneParams(MultiCameraCalibrationFrameworkHandle handle, const MeasurementPlaneParams* params)
    {
        if (!handle || !params)
        {
            set_last_error("Multi-camera calibration handle or measurement plane params is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            if (!handle->framework.setMeasurementPlaneParams(*params, errorMessage))
            {
                set_last_error(errorMessage);
                return CAMCALIB_ERR_INVALID;
            }
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiSetMeasurementPlaneParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiSetMeasurementPlaneParams");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiGetMeasurementPlaneParams(MultiCameraCalibrationFrameworkHandle handle, MeasurementPlaneParams* params)
    {
        if (!handle || !params)
        {
            set_last_error("Multi-camera calibration handle or measurement plane params output is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            *params = handle->framework.getMeasurementPlaneParams();
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiGetMeasurementPlaneParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiGetMeasurementPlaneParams");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiSetCalibrationOptions(MultiCameraCalibrationFrameworkHandle handle, const MultiCameraCalibrationOptions* options)
    {
        if (!handle || !options)
        {
            set_last_error("Multi-camera calibration handle or options is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            calib::multicam::MultiCameraOptions nativeOptions;
            if (!copyOptions(*options, nativeOptions, errorMessage))
            {
                set_last_error(errorMessage);
                return CAMCALIB_ERR_INVALID;
            }
            handle->framework.setOptions(nativeOptions);
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiSetCalibrationOptions: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiSetCalibrationOptions");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiAddCamera(MultiCameraCalibrationFrameworkHandle handle, const char* cameraId, int imageWidth, int imageHeight)
    {
        if (!handle || !cameraId)
        {
            set_last_error("Multi-camera calibration handle or camera id is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            if (!handle->framework.addCamera(cameraId, imageWidth, imageHeight, errorMessage))
            {
                set_last_error(errorMessage);
                return CAMCALIB_ERR_INVALID;
            }
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiAddCamera: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiAddCamera");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiSetInitialCameraParams(MultiCameraCalibrationFrameworkHandle handle, const char* cameraId, const CameraParams* params, int fixIntrinsic)
    {
        if (!handle || !cameraId || !params)
        {
            set_last_error("Multi-camera calibration handle, camera id, or params is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            if (!handle->framework.setInitialCameraParams(cameraId, *params, fixIntrinsic != 0, errorMessage))
            {
                set_last_error(errorMessage);
                return CAMCALIB_ERR_INVALID;
            }
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiSetInitialCameraParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiSetInitialCameraParams");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiAddObservationImagePath(MultiCameraCalibrationFrameworkHandle handle, const char* cameraId, const char* captureId, const char* imagePath)
    {
        if (!handle || !cameraId || !captureId || !imagePath)
        {
            set_last_error("Multi-camera calibration handle, camera id, capture id, or image path is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            if (!handle->framework.addObservationImagePath(cameraId, captureId, imagePath, errorMessage))
            {
                set_last_error(errorMessage);
                return errorMessage.find("load observation image") != std::string::npos ? CAMCALIB_ERR_IO : CAMCALIB_ERR_INVALID;
            }
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiAddObservationImagePath: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiAddObservationImagePath");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiCalibrate(MultiCameraCalibrationFrameworkHandle handle)
    {
        if (!handle)
        {
            set_last_error("Multi-camera calibration handle is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            if (!handle->framework.calibrate(errorMessage))
            {
                set_last_error(errorMessage);
                return errorCodeForMessage(errorMessage);
            }
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiCalibrate: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiCalibrate");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiGetCameraParams(MultiCameraCalibrationFrameworkHandle handle, const char* cameraId, MultiCameraCameraParams* params)
    {
        if (!handle || !cameraId || !params)
        {
            set_last_error("Multi-camera calibration handle, camera id, or output params is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            calib::multicam::CameraRigParams camera;
            if (!handle->framework.getCameraParams(cameraId, camera))
            {
                set_last_error("Multi-camera camera params were not found.");
                return CAMCALIB_ERR_INVALID;
            }

            std::memset(params, 0, sizeof(*params));
            copyStringToFixedBuffer(camera.cameraId, params->cameraId, sizeof(params->cameraId));
            params->imageWidth = camera.imageSize.width;
            params->imageHeight = camera.imageSize.height;
            params->rmsError = camera.rmsError;
            copyMat3x3ToArray(camera.intrinsic, params->intrinsic);
            copyDistortionToArray(camera.distortion, params->distortion);
            copyCommonFromCameraPose(camera.transformCommonFromCamera, params->rvecCommonFromCamera, params->tvecCommonFromCamera);
            copyTransformToArray(camera.transformCommonFromCamera, params->extrinsicCommonFromCamera);
            params->hasIntrinsic = camera.hasIntrinsic ? 1 : 0;
            params->hasDistortion = camera.distortion.empty() ? 0 : 1;
            params->hasExtrinsicCommonFromCamera = camera.transformCommonFromCamera.empty() ? 0 : 1;
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiGetCameraParams: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiGetCameraParams");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiGetCalibrationReport(MultiCameraCalibrationFrameworkHandle handle, MultiCameraCalibrationReport* report)
    {
        if (!handle || !report)
        {
            set_last_error("Multi-camera calibration handle or report is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            copyReport(handle->framework.report(), report);
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiGetCalibrationReport: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiGetCalibrationReport");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiGetCameraCount(MultiCameraCalibrationFrameworkHandle handle, int* cameraCount)
    {
        if (!handle || !cameraCount)
        {
            set_last_error("Multi-camera calibration handle or camera count output is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            const std::vector<std::string> cameraIds = handle->framework.cameraIds();
            *cameraCount = static_cast<int>(cameraIds.size());
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiGetCameraCount: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiGetCameraCount");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiGetCameraId(MultiCameraCalibrationFrameworkHandle handle, int index, char* cameraId, int cameraIdCapacity)
    {
        if (!handle || !cameraId)
        {
            set_last_error("Multi-camera calibration handle or camera id output is null.");
            return CAMCALIB_ERR_NULL;
        }
        if (cameraIdCapacity <= 0)
        {
            set_last_error("Camera id output capacity must be positive.");
            return CAMCALIB_ERR_INVALID;
        }
        try
        {
            const std::vector<std::string> cameraIds = handle->framework.cameraIds();
            if (index < 0 || index >= static_cast<int>(cameraIds.size()))
            {
                set_last_error("Camera id index is out of range.");
                return CAMCALIB_ERR_INVALID;
            }

            copyStringToFixedBuffer(cameraIds[static_cast<std::size_t>(index)], cameraId, static_cast<std::size_t>(cameraIdCapacity));
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiGetCameraId: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiGetCameraId");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiPixelToCommonWorld(MultiCameraCalibrationFrameworkHandle handle, const char* cameraId, double pixelX, double pixelY, double* worldX, double* worldY, double* worldZ)
    {
        if (!handle || !cameraId || !worldX || !worldY || !worldZ)
        {
            set_last_error("Multi-camera calibration handle, camera id, or output world pointer is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            cv::Point3d worldPoint;
            std::string errorMessage;
            if (!handle->framework.pixelToCommonWorld(cameraId, pixelX, pixelY, worldPoint, errorMessage))
            {
                set_last_error(errorMessage);
                return errorCodeForMessage(errorMessage);
            }
            *worldX = worldPoint.x;
            *worldY = worldPoint.y;
            *worldZ = worldPoint.z;
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiPixelToCommonWorld: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiPixelToCommonWorld");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiCommonWorldToPixel(MultiCameraCalibrationFrameworkHandle handle, const char* cameraId, double worldX, double worldY, double worldZ, double* pixelX, double* pixelY)
    {
        if (!handle || !cameraId || !pixelX || !pixelY)
        {
            set_last_error("Multi-camera calibration handle, camera id, or output pixel pointer is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            cv::Point2d pixelPoint;
            std::string errorMessage;
            if (!handle->framework.commonWorldToPixel(cameraId, cv::Point3d(worldX, worldY, worldZ), pixelPoint, errorMessage))
            {
                set_last_error(errorMessage);
                return errorCodeForMessage(errorMessage);
            }
            *pixelX = pixelPoint.x;
            *pixelY = pixelPoint.y;
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiCommonWorldToPixel: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiCommonWorldToPixel");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiStitchImages(MultiCameraCalibrationFrameworkHandle handle, const MultiCameraImageInput* images, int imageCount, MultiCameraBlendMode blendMode, MultiCameraImageOutput* output)
    {
        if (!handle || !images || !output)
        {
            set_last_error("Multi-camera calibration handle, images, or output is null.");
            return CAMCALIB_ERR_NULL;
        }
        if (imageCount <= 0)
        {
            set_last_error("Image count must be positive.");
            return CAMCALIB_ERR_INVALID;
        }

        try
        {
            std::vector<calib::multicam::StitchImageInput> nativeImages;
            nativeImages.reserve(static_cast<std::size_t>(imageCount));
            for (int index = 0; index < imageCount; ++index)
            {
                const MultiCameraImageInput& imageInput = images[index];
                if (!imageInput.imageData || imageInput.width <= 0 || imageInput.height <= 0)
                {
                    set_last_error("Stitch image input has invalid image data or dimensions.");
                    return CAMCALIB_ERR_INVALID;
                }
                if (CV_MAT_CN(imageInput.cvType) != imageInput.channels)
                {
                    set_last_error("Stitch image input channel count does not match cvType.");
                    return CAMCALIB_ERR_INVALID;
                }

                cv::Mat image(imageInput.height, imageInput.width, imageInput.cvType, imageInput.imageData);
                nativeImages.push_back({ readFixedString(imageInput.cameraId, sizeof(imageInput.cameraId)), image });
            }

            calib::multicam::BlendMode nativeBlendMode = calib::multicam::BlendMode::Average;
            std::string errorMessage;
            if (!copyBlendMode(blendMode, nativeBlendMode, errorMessage))
            {
                set_last_error(errorMessage);
                return CAMCALIB_ERR_INVALID;
            }

            cv::Mat stitched;
            if (!handle->framework.stitch(nativeImages, nativeBlendMode, stitched, errorMessage))
            {
                set_last_error(errorMessage);
                return errorCodeForMessage(errorMessage);
            }
            if (stitched.empty())
            {
                set_last_error("Multi-camera stitching produced an empty image.");
                return CAMCALIB_ERR_UNEXP;
            }

            const std::size_t dataSize = stitched.total() * stitched.elemSize();
            void* outputData = malloc(dataSize);
            if (!outputData)
            {
                set_last_error("Failed to allocate multi-camera stitching output buffer.");
                return CAMCALIB_ERR_MEMORY;
            }
            std::memcpy(outputData, stitched.data, dataSize);
            output->imageData = outputData;
            output->width = stitched.cols;
            output->height = stitched.rows;
            output->channels = stitched.channels();
            output->cvType = stitched.type();
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiStitchImages: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiStitchImages");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiSaveCalibrationResults(MultiCameraCalibrationFrameworkHandle handle, const char* outputFile)
    {
        if (!handle || !outputFile)
        {
            set_last_error("Multi-camera calibration handle or output file is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            if (!handle->framework.save(outputFile, errorMessage))
            {
                set_last_error(errorMessage);
                return errorCodeForMessage(errorMessage);
            }
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiSaveCalibrationResults: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiSaveCalibrationResults");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int multiLoadCalibrationFile(MultiCameraCalibrationFrameworkHandle handle, const char* filePath)
    {
        if (!handle || !filePath)
        {
            set_last_error("Multi-camera calibration handle or file path is null.");
            return CAMCALIB_ERR_NULL;
        }
        try
        {
            std::string errorMessage;
            if (!handle->framework.load(filePath, errorMessage))
            {
                set_last_error(errorMessage);
                return CAMCALIB_ERR_INVALID;
            }
            set_last_error("");
            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in multiLoadCalibrationFile: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in multiLoadCalibrationFile");
            return CAMCALIB_ERR_UNEXP;
        }
    }

    CAMERA_CALIBRATION_SDK_API int freePtr(void* ptr)
    {
        try
        {
            if (ptr)
            {
                free(ptr);
            }

            return CAMCALIB_OK;
        }
        catch (const std::exception& e)
        {
            set_last_error(std::string("Exception in worldToPixel: ") + e.what());
            return CAMCALIB_ERR_UNEXP;
        }
        catch (...)
        {
            set_last_error("Unknown exception in worldToPixel");
            return CAMCALIB_ERR_UNEXP;
        }
        
    }
}
