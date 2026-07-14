#include "pch.h"

#include "calibration_board_calib_factory.h"

#include "charuco/charuco_calib.h"
#include "chessboard/chessboard_calib.h"

namespace calib::boards
{
    std::unique_ptr<ICalibrationBoardCalibrator> createCalibrationBoardCalibrator(
        CalibrationBoardType type,
        std::string& errorMessage)
    {
        errorMessage.clear();
        switch (type)
        {
        case CHARUCO:
            return std::make_unique<CharucoCalibration>();
        case CHESSBOARD:
            return std::make_unique<ChessboardCalibration>();
        case CIRCLES_GRID:
            errorMessage = "Circles grid calibration not yet implemented.";
            return nullptr;
        case ASYMMETRIC_CIRCLES_GRID:
            errorMessage = "Asymmetric circles grid calibration not yet implemented.";
            return nullptr;
        default:
            errorMessage = "Unsupported calibration board type.";
            return nullptr;
        }
    }
}
