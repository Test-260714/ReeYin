#pragma once

#include "calibration_board_calib.h"

#include <memory>
#include <string>

namespace calib::boards
{
    std::unique_ptr<ICalibrationBoardCalibrator> createCalibrationBoardCalibrator(
        CalibrationBoardType type,
        std::string& errorMessage);
}
