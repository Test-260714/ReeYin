#pragma once

#include "../boards/calibration_board_calib.h"
#include "../interface.h"

#include <opencv2/opencv.hpp>

#include <string>
#include <vector>

class ChessboardCalibration final : public calib::boards::ICalibrationBoardCalibrator
{
public:
    /**
     * @brief 检测图像中的棋盘格标定板角点
     * @param image 输入图像
     * @param boardParams 棋盘格标定板参数
     * @param observation 输出的标定板观测结果
     * @param errorMessage 输出的错误信息
     * @return 是否检测成功
     */
    bool detect(
        const cv::Mat& image,
        const CalibrationBoardParams& boardParams,
        calib::multicam::BoardObservation& observation,
        std::string& errorMessage) const override;

    /**
     * @brief 基于图像列表执行棋盘格相机标定
     * @param cameraId 相机ID
     * @param boardParams 棋盘格标定板参数
     * @param images 标定板图像列表
     * @param intrinsic 输出的相机内参矩阵
     * @param distortion 输出的畸变系数
     * @param rvecs 输出的旋转向量
     * @param tvecs 输出的平移向量
     * @param rmsError 输出的RMS重投影误差
     * @param errorMessage 输出的错误信息
     * @return 是否标定成功
     */
    bool calibrate(
        const std::string& cameraId,
        const CalibrationBoardParams& boardParams,
        const std::vector<cv::Mat>& images,
        cv::Mat& intrinsic,
        cv::Mat& distortion,
        std::vector<cv::Mat>& rvecs,
        std::vector<cv::Mat>& tvecs,
        double& rmsError,
        std::string& errorMessage) const override;

    /**
     * @brief 基于已检测观测结果执行棋盘格相机标定
     * @param cameraId 相机ID
     * @param boardParams 棋盘格标定板参数
     * @param observations 标定板观测结果列表
     * @param imageSize 标定图像尺寸
     * @param intrinsic 输出的相机内参矩阵
     * @param distortion 输出的畸变系数
     * @param rvecs 输出的旋转向量
     * @param tvecs 输出的平移向量
     * @param rmsError 输出的RMS重投影误差
     * @param errorMessage 输出的错误信息
     * @return 是否标定成功
     */
    bool calibrate(
        const std::string& cameraId,
        const CalibrationBoardParams& boardParams,
        const std::vector<calib::multicam::BoardObservation>& observations,
        const cv::Size& imageSize,
        cv::Mat& intrinsic,
        cv::Mat& distortion,
        std::vector<cv::Mat>& rvecs,
        std::vector<cv::Mat>& tvecs,
        double& rmsError,
        std::string& errorMessage) const override;

    /**
     * @brief 计算棋盘格点ID对应的物理坐标
     * @param boardParams 棋盘格标定板参数
     * @param pointIds 棋盘格点ID列表
     * @param physicalCoords 输出的物理坐标列表
     * @param errorMessage 输出的错误信息
     * @return 是否计算成功
     */
    bool computePhysicalCoords(
        const CalibrationBoardParams& boardParams,
        const std::vector<int>& pointIds,
        std::vector<cv::Point3d>& physicalCoords,
        std::string& errorMessage) const override;

    /**
     * @brief 计算棋盘格标定板到图像平面的单应性矩阵
     * @param boardParams 棋盘格标定板参数
     * @param image 输入图像
     * @param cameraMatrix 相机内参矩阵
     * @param distCoeffs 畸变系数
     * @param homography 输出的单应性矩阵
     * @param errorMessage 输出的错误信息
     * @return 是否计算成功
     */
    bool getHomographyMatrix(
        const CalibrationBoardParams& boardParams,
        const cv::Mat& image,
        const cv::Mat& cameraMatrix,
        const cv::Mat& distCoeffs,
        cv::Mat& homography,
        std::string& errorMessage) const override;

private:
    static std::vector<cv::Point3d> createObjectPoints(const CalibrationBoardParams& boardParams);
};
