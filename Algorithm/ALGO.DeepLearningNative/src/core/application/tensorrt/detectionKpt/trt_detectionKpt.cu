#include "trt_detectionKpt.h"

#include "tensorrt/common/cuda_tools.hpp"


namespace DetectionKpt
{

    __device__  __host__ void affine_project(float* matrix, float x, float y, float* ox, float* oy)
    {
        *ox = matrix[0] * x + matrix[1] * y + matrix[2];
        *oy = matrix[3] * x + matrix[4] * y + matrix[5];
    }


    static __global__ void decode_kernel_yolov8_kpt(float* predict, int num_bboxes, int num_classes, float confidence_threshold,
        float* invert_affine_matrix, float* parray, int MAX_NumKPTS, int MAX_IMAGE_BOXES)
    {
        int position = blockDim.x * blockIdx.x + threadIdx.x;
        if (position >= num_bboxes)
            return;

        int NUM_BOX_ELEMENT = 7 + 3 * MAX_NumKPTS;

        float* pitem = predict + (4 + num_classes + 3 * MAX_NumKPTS) * position;
        float* class_confidence = pitem + 4;
        float confidence = *class_confidence++;
        int label = 0;
        for (int i = 1; i < num_classes; ++i, ++class_confidence)
        {
            if (*class_confidence > confidence)
            {
                confidence = *class_confidence;
                label = i;
            }
        }

        if (confidence < confidence_threshold)
            return;

        int index = atomicAdd(parray, 1);
        if (index >= MAX_IMAGE_BOXES)
            return;

        float cx = *pitem++;
        float cy = *pitem++;
        float width = *pitem++;
        float height = *pitem++;
        float left = cx - width * 0.5f;
        float top = cy - height * 0.5f;
        float right = cx + width * 0.5f;
        float bottom = cy + height * 0.5f;

        affine_project(invert_affine_matrix, left, top, &left, &top);
        affine_project(invert_affine_matrix, right, bottom, &right, &bottom);

        float* pout_item = parray + 1 + index * NUM_BOX_ELEMENT;
        *pout_item++ = left;
        *pout_item++ = top;
        *pout_item++ = right;
        *pout_item++ = bottom;
        *pout_item++ = confidence;
        *pout_item++ = label;
        *pout_item++ = 1;  // 1 = keep, 0 = ignore

        pitem++;
        for (int i = 0; i < MAX_NumKPTS; ++i)
        {
            float keypoint_x = *pitem++;
            float keypoint_y = *pitem++;
            float keypoint_confidence = *pitem++;

            affine_project(invert_affine_matrix, keypoint_x, keypoint_y, &keypoint_x, &keypoint_y);

            *pout_item++ = keypoint_x;
            *pout_item++ = keypoint_y;
            *pout_item++ = keypoint_confidence;
        }
    }


    static __global__ void decode_kernel_detr_kpt(float* predict, int num_bboxes, int num_classes, float confidence_threshold,
        int input_width, int input_height, float* invert_affine_matrix,
        float* parray, int MAX_NumKPTS, int MAX_IMAGE_BOXES)
    {
        int position = blockDim.x * blockIdx.x + threadIdx.x;
        if (position >= num_bboxes)
            return;

        int NUM_BOX_ELEMENT = 7 + 3 * MAX_NumKPTS;

        float* pitem = predict + (4 + 1 + 1) * position;

        float confidence = *(pitem + 4);
        int label = *(pitem + 5);

        if (confidence < confidence_threshold)
            return;

        int index = atomicAdd(parray, 1);
        if (index >= MAX_IMAGE_BOXES)
            return;

        float left = (*pitem++) * input_width;
        float top = (*pitem++) * input_height;
        float right = (*pitem++) * input_width;
        float bottom = (*pitem++) * input_height;

        affine_project(invert_affine_matrix, left, top, &left, &top);
        affine_project(invert_affine_matrix, right, bottom, &right, &bottom);

        float* pout_item = parray + 1 + index * NUM_BOX_ELEMENT;
        *pout_item++ = left;
        *pout_item++ = top;
        *pout_item++ = right;
        *pout_item++ = bottom;
        *pout_item++ = confidence;
        *pout_item++ = label;
        *pout_item++ = 1;  // 1 = keep, 0 = ignore

        pitem++;
        for (int i = 0; i < MAX_NumKPTS; ++i)
        {
            float keypoint_x = (*pitem++) * input_width;
            float keypoint_y = (*pitem++) * input_height;
            float keypoint_confidence = *pitem++;

            affine_project(invert_affine_matrix, keypoint_x, keypoint_y, &keypoint_x, &keypoint_y);

            *pout_item++ = keypoint_x;
            *pout_item++ = keypoint_y;
            *pout_item++ = keypoint_confidence;
        }
    }


    static __device__ float box_iou(float aleft, float atop, float aright, float abottom,
        float bleft, float btop, float bright, float bbottom)
    {

        float cleft = max(aleft, bleft);
        float ctop = max(atop, btop);
        float cright = min(aright, bright);
        float cbottom = min(abottom, bbottom);

        float c_area = max(cright - cleft, 0.0f) * max(cbottom - ctop, 0.0f);
        if (c_area == 0.0f)
            return 0.0f;

        float a_area = max(0.0f, aright - aleft) * max(0.0f, abottom - atop);
        float b_area = max(0.0f, bright - bleft) * max(0.0f, bbottom - btop);
        return c_area / (a_area + b_area - c_area);
    }


    static __global__ void nms_kernel(float* bboxes, int MAX_NumKPTS, int max_objects, float threshold)
    {
        int position = (blockDim.x * blockIdx.x + threadIdx.x);
        int count = min((int)*bboxes, max_objects);
        if (position >= count)
            return;

        int NUM_BOX_ELEMENT = 7 + 3 * MAX_NumKPTS;

        // left, top, right, bottom, confidence, class, keepflag
        float* pcurrent = bboxes + 1 + position * NUM_BOX_ELEMENT;
        for (int i = 0; i < count; ++i)
        {
            float* pitem = bboxes + 1 + i * NUM_BOX_ELEMENT;
            if (i == position || pcurrent[5] != pitem[5])
                continue;

            if (pitem[4] >= pcurrent[4])
            {
                if (pitem[4] == pcurrent[4] && i < position)
                    continue;

                float iou = box_iou(pcurrent[0], pcurrent[1], pcurrent[2], pcurrent[3],
                    pitem[0], pitem[1], pitem[2], pitem[3]);

                if (iou > threshold)
                {
                    pcurrent[6] = 0;  // 1=keep, 0=ignore
                    return;
                }
            }
        }
    }


    void yolo_bbox_decode_kernel_invoker(float* predict, int num_bboxes, int num_classes, float confidence_threshold,
        float* invert_affine_matrix, float* parray, int max_numkpts, int max_objects, cudaStream_t stream)
    {
        auto grid = CUDATools::grid_dims(num_bboxes);
        auto block = CUDATools::block_dims(num_bboxes);

        checkCudaKernel(decode_kernel_yolov8_kpt << <grid, block, 0, stream >> > (predict, num_bboxes, num_classes, confidence_threshold,
            invert_affine_matrix, parray, max_numkpts, max_objects));
    }


    void detr_bbox_decode_kernel_invoker(float* predict, int num_bboxes, int num_classes, float confidence_threshold,
        int input_width, int input_height, float* invert_affine_matrix,
        float* parray, int max_numkpts, int max_objects, cudaStream_t stream)
    {
        auto grid = CUDATools::grid_dims(num_bboxes);
        auto block = CUDATools::block_dims(num_bboxes);

        checkCudaKernel(decode_kernel_detr_kpt << <grid, block, 0, stream >> > (predict, num_bboxes, num_classes, confidence_threshold,
            input_width, input_height, invert_affine_matrix, parray, max_numkpts, max_objects));
    }


    void nms_kernel_invoker(float* parray, float nms_threshold, int max_numkpts, int max_objects, cudaStream_t stream)
    {
        auto grid = CUDATools::grid_dims(max_objects);
        auto block = CUDATools::block_dims(max_objects);
        checkCudaKernel(nms_kernel << <grid, block, 0, stream >> > (parray, max_numkpts, max_objects, nms_threshold));
    }
}




