#include "pch.h"

#include "utils.h"

#include "utils/ilogger.hpp"
#include <fstream>

bool file2json(const std::string& file, Json::Value& v)
{
    std::ifstream inputFile(file);
    if (!inputFile.is_open())
    {
        INFOE("Failed to open file: %s", file.c_str());
        return false;
    }

    Json::CharReaderBuilder readerBuilder;
    std::string errs;

    bool parsingSuccessful = Json::parseFromStream(readerBuilder, inputFile, &v, &errs);
    inputFile.close();

    if (!parsingSuccessful)
    {
        INFOE("Failed to parse JSON file: %s, error: %s", file.c_str(), errs.c_str());
        return false;
    }

    return true;
}


static float iou(const Result& a, const Result& b)
{
    float a_left = a.cx - (a.width) * 0.5;
    float a_right = a.cx + (a.width) * 0.5;
    float a_top = a.cy - (a.height) * 0.5;
    float a_bottom = a.cy + (a.height) * 0.5;

    float b_left = b.cx - (b.width) * 0.5;
    float b_right = b.cx + (b.width) * 0.5;
    float b_top = b.cy - (b.height) * 0.5;
    float b_bottom = b.cy + (b.height) * 0.5;

    float cleft = std::max(a_left, b_left);
    float ctop = std::max(a_top, b_top);
    float cright = std::min(a_right, b_right);
    float cbottom = std::min(a_bottom, b_bottom);

    float c_area = std::max(cright - cleft, 0.0f) * std::max(cbottom - ctop, 0.0f);
    if (c_area == 0.0f)
        return 0.0f;

    float a_area = std::max(0.0f, a.width) * std::max(0.0f, a.height);
    float b_area = std::max(0.0f, b.width) * std::max(0.0f, b.height);
    return c_area / (a_area + b_area - c_area);
}


BoxArray cpu_nms(BoxArray& boxes, float threshold)
{

    std::sort(boxes.begin(), boxes.end(), [](BoxArray::const_reference a, BoxArray::const_reference b)
        {return a.confidence > b.confidence; });

    BoxArray output;
    output.reserve(boxes.size());

    std::vector<bool> remove_flags(boxes.size());
    for (int i = 0; i < boxes.size(); ++i)
    {
        if (remove_flags[i])
            continue;

        auto& a = boxes[i];
        output.emplace_back(a);

        for (int j = i + 1; j < boxes.size(); ++j)
        {
            if (remove_flags[j])
                continue;

            auto& b = boxes[j];
            if (b.class_id == a.class_id)
            {
                if (iou(a, b) >= threshold)
                    remove_flags[j] = true;
            }
        }
    }
    return output;
}



std::tuple<float, float, float> convariance_matrix(const Result& obb)
{
    float w = obb.width;
    float h = obb.height;
    float r = obb.angle;
    float a = w * w / 12.0;
    float b = h * h / 12.0;

    float cos_r = std::cos(r);
    float sin_r = std::sin(r);

    float a_val = a * cos_r * cos_r + b * sin_r * sin_r;
    float b_val = a * sin_r * sin_r + b * cos_r * cos_r;
    float c_val = (a - b) * sin_r * cos_r;

    return std::make_tuple(a_val, b_val, c_val);
}

static float probiou(const Result& obb1, const Result& obb2, float eps = 1e-7)
{
    // Calculate the prob iou between oriented bounding boxes, https://arxiv.org/pdf/2106.06072v1.pdf.
    float a1, b1, c1, a2, b2, c2;
    std::tie(a1, b1, c1) = convariance_matrix(obb1);
    std::tie(a2, b2, c2) = convariance_matrix(obb2);

    float x1 = obb1.cx, y1 = obb1.cy;
    float x2 = obb2.cx, y2 = obb2.cy;

    float t1 = ((a1 + a2) * std::pow(y1 - y2, 2) + (b1 + b2) * std::pow(x1 - x2, 2)) / ((a1 + a2) * (b1 + b2) - std::pow(c1 + c2, 2) + eps);
    float t2 = ((c1 + c2) * (x2 - x1) * (y1 - y2)) / ((a1 + a2) * (b1 + b2) - std::pow(c1 + c2, 2) + eps);
    float t3 = std::log(((a1 + a2) * (b1 + b2) - std::pow(c1 + c2, 2)) / (4 * std::sqrt(std::max(a1 * b1 - c1 * c1, 0.0f)) * std::sqrt(std::max(a2 * b2 - c2 * c2, 0.0f)) + eps) + eps);

    float bd = 0.25 * t1 + 0.5 * t2 + 0.5 * t3;
    bd = std::max(std::min(bd, 100.0f), eps);
    float hd = std::sqrt(1.0 - std::exp(-bd) + eps);

    return 1 - hd;
}

BoxArray cpu_obb_nms(BoxArray& boxes, float threshold)
{
    std::sort(boxes.begin(), boxes.end(), [](BoxArray::const_reference a, BoxArray::const_reference b)
        {
            return a.confidence > b.confidence;
        });

    BoxArray output;
    output.reserve(boxes.size());

    std::vector<bool> remove_flags(boxes.size());
    for (int i = 0; i < boxes.size(); ++i)
    {
        if (remove_flags[i])
            continue;

        auto& a = boxes[i];
        output.emplace_back(a);

        for (int j = i + 1; j < boxes.size(); ++j)
        {
            if (remove_flags[j])
                continue;

            auto& b = boxes[j];
            if (b.class_id == a.class_id)
            {
                if (probiou(a, b) >= threshold)
                    remove_flags[j] = true;
            }
        }
    }
    return output;
}



std::vector<int> calculateCoordinates(int imgSize, int windowSize, int overlapSize)
{
    std::vector<int> coords;
    int actualWindow = std::min(windowSize, imgSize);
    int stride = std::max(1, actualWindow - overlapSize);

    for (int pos = 0; pos <= std::max(0, imgSize - actualWindow); pos += stride)
    {
        coords.push_back(pos);

        // 处理剩余不足stride的情况
        if (pos + stride > imgSize - actualWindow && pos != imgSize - actualWindow)
        {
            coords.push_back(imgSize - actualWindow);
            break;
        }
    }
    return coords;
}


int GetImagePatches(const cv::Mat& image, const cv::Mat& depth, int patchWidth, int patchHeight, float overlapRate, std::vector<ImagePatch>& patches)
{
    int imageOriW;
    int imageOriH;

    if (!image.empty())
    {
        imageOriW = image.cols;
        imageOriH = image.rows;
    }
    else if (!depth.empty())
    {
        imageOriW = depth.cols;
        imageOriH = depth.rows;
    }
    else
    {
        INFOE("Image is empty.");
        return -1;
    }
        
    patchWidth = std::min(patchWidth, imageOriW);
    patchHeight = std::min(patchHeight, imageOriH);

    int overlapX = patchWidth * overlapRate;
    int overlapY = patchHeight * overlapRate;

    std::vector<int> xCoords = calculateCoordinates(imageOriW, patchWidth, overlapX);
    std::vector<int> yCoords = calculateCoordinates(imageOriH, patchHeight, overlapY);

    for (int j = 0; j < yCoords.size(); j++)
    {
        for (int i = 0; i < xCoords.size(); i++)
        {
            int x = xCoords[i];
            int y = yCoords[j];

            ImagePatch p;
            p.patchRoi = cv::Rect(x, y, patchWidth, patchHeight);

            int validStartX;
            if (i == 0)
                validStartX = x;
            else
                validStartX = std::floor(((xCoords[i - 1] + patchWidth) + x) * 0.5);

            int validStartY;
            if (j == 0)
                validStartY = y;
            else
                validStartY = std::floor(((yCoords[j - 1] + patchHeight) + y) * 0.5);

            int validEndX;
            if (i == (xCoords.size() - 1))
                validEndX = x + patchWidth;
            else
                validEndX = std::ceil(((x + patchWidth) + xCoords[i + 1]) * 0.5);

            int validEndY;
            if (j == (yCoords.size() - 1))
                validEndY = y + patchHeight;
            else
                validEndY = std::ceil(((y + patchHeight) + yCoords[j + 1]) * 0.5);

            p.validRoi = cv::Rect(validStartX, validStartY, validEndX - validStartX, validEndY - validStartY);

            if (!image.empty())
                p.image = image(p.patchRoi).clone();
            if (!depth.empty())
                p.depth = depth(p.patchRoi).clone();   
            
            patches.push_back(p);
        }
    }

    return 0;
}


int MergePatches(std::vector<std::shared_future<BoxArray>> insPtrs, std::vector<cv::Rect> patchRois,
                 std::vector<cv::Rect> validRois, float nmsThresh, ModelType modelType, BoxArray& result)
{
    BoxArray merged;

    for (int i = 0; i < insPtrs.size(); i++)
    {
        BoxArray boxes = insPtrs[i].get();
        cv::Rect patchRoi = patchRois[i];
        cv::Rect validRoi = validRois[i];

        for (int j = 0; j < boxes.size(); j++)
        {
            Result newBox = boxes[j];

            if (modelType == ModelType::MODEL_DETECTION_BBOX || 
                modelType == ModelType::MODEL_DETECTION_SEG ||
                modelType == ModelType::MODEL_DETECTION_OBB)
            {
                newBox.cx += patchRoi.x;
                newBox.cy += patchRoi.y;
                newBox.segmentation.affine_matrix[2] += patchRoi.x;
                newBox.segmentation.affine_matrix[5] += patchRoi.y;

                //float newBox_left = newBox.cx - (newBox.width) * 0.5;
                //float newBox_right = newBox.cx + (newBox.width) * 0.5;
                //float newBox_top = newBox.cy - (newBox.height) * 0.5;
                //float newBox_bottom = newBox.cy + (newBox.height) * 0.5;

                //float interLeft = std::max((float)validRoi.x, newBox_left);
                //float interTop = std::max((float)validRoi.y, newBox_top);
                //float interRight = std::min((float)validRoi.x + validRoi.width, newBox_right);
                //float interBottom = std::min((float)validRoi.y + validRoi.height, newBox_bottom);
                //float interArea = std::max(interRight - interLeft, 0.0f) * std::max(interBottom - interTop, 0.0f);
                //float boxArea = std::max(0.0f, newBox_right - newBox_left) * std::max(0.0f, newBox_bottom - newBox_top);
                //float rate = interArea / boxArea;
                //if (rate > 0.5)
                //    merged.push_back(newBox);
                merged.push_back(newBox);
            }
            else if(modelType == ModelType::MODEL_SEGMENTATION)
            {
                newBox.segmentation.affine_matrix[2] += patchRoi.x;
                newBox.segmentation.affine_matrix[5] += patchRoi.y;

                merged.push_back(newBox);
            }
            else
            {
                merged.push_back(newBox);
            }

        }
    }

    if(modelType == ModelType::MODEL_DETECTION_BBOX || modelType == ModelType::MODEL_DETECTION_SEG)
        result = cpu_nms(merged, nmsThresh);
    else if(modelType == ModelType::MODEL_DETECTION_OBB)
        result = cpu_obb_nms(merged, nmsThresh);
    else
        result = merged;

    return 0;
}

