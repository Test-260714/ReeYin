#include "trt_detectionSeg.h"

#include "tensorrt/common/cuda_tools.hpp"


namespace DetectionSeg
{
    const int NUM_BOX_ELEMENT = 8;      // left, top, right, bottom, confidence, class, keepflag, anchor_index

    __device__  __host__ void affine_project(float* matrix, float x, float y, float* ox, float* oy)
    {
        *ox = matrix[0] * x + matrix[1] * y + matrix[2];
        *oy = matrix[3] * x + matrix[4] * y + matrix[5];
    }


    static __global__ void decode_kernel_yolov8_seg(float* predict, int num_bboxes, int num_classes, float confidence_threshold,
                                                    float* invert_affine_matrix, float* parray, int MAX_IMAGE_BOXES) 
    {
        int position = blockDim.x * blockIdx.x + threadIdx.x;
        if (position >= num_bboxes) 
            return;

        float* pitem = predict + (4 + num_classes + 32) * position;
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
        *pout_item++ = position;  // row_index
    }


    static __global__ void decode_kernel_detr_seg(int* labels, float* boxes, float* scores, int num_bboxes, float conf_thresh,
                                                  int input_width, int input_height, float* invert_affine_matrix, float* pBoxArray, int MAX_IMAGE_BOXES)
    {
        int position = blockDim.x * blockIdx.x + threadIdx.x;
        if (position >= num_bboxes)
            return;

        float confidence = scores[position];
        if (confidence < conf_thresh)
            return;

        int index = atomicAdd(pBoxArray, 1);
        if (index >= MAX_IMAGE_BOXES)
            return;

        int label = labels[position];

        float left = boxes[position * 4 + 0] * input_width;
        float top = boxes[position * 4 + 1] * input_height;
        float right = boxes[position * 4 + 2] * input_width;
        float bottom = boxes[position * 4 + 3] * input_height;

        //printf("decode_kernel: %d, %f, %f, %f, %f, %f, %d\n", position, left, top, right, bottom, confidence, label);

        affine_project(invert_affine_matrix, left, top, &left, &top);
        affine_project(invert_affine_matrix, right, bottom, &right, &bottom);

        float* pout_item = pBoxArray + 1 + index * NUM_BOX_ELEMENT;
        *pout_item++ = left;
        *pout_item++ = top;
        *pout_item++ = right;
        *pout_item++ = bottom;
        *pout_item++ = confidence;
        *pout_item++ = label;
        *pout_item++ = 1; // 1 = keep, 0 = ignore
        *pout_item++ = position;
    }

    static __global__ void cast_int64_labels_to_int32_kernel(const int64_t* src, int* dst, int count)
    {
        int position = blockDim.x * blockIdx.x + threadIdx.x;
        if (position >= count)
            return;

        dst[position] = static_cast<int>(src[position]);
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


    static __global__ void nms_kernel(float* bboxes, int max_objects, float threshold) 
    {
        int position = (blockDim.x * blockIdx.x + threadIdx.x);
        int count = min((int)*bboxes, max_objects);
        if (position >= count)
            return;

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


    static __global__ void yolo_mask_decode_single_kernel(int left, int top, float* mask_weights, float* mask_predict,
                                                          int mask_width, int mask_height, float* mask_out, int mask_dim, 
                                                          int out_width, int out_height) 
    {
        // mask_predict to mask_out
        // mask_weights @ mask_predict
        int dx = blockDim.x * blockIdx.x + threadIdx.x;
        int dy = blockDim.y * blockIdx.y + threadIdx.y;
        if (dx >= out_width || dy >= out_height) 
            return;

        int sx = left + dx;
        int sy = top + dy;
        if (sx < 0 || sx >= mask_width || sy < 0 || sy >= mask_height) 
        {
            mask_out[dy * out_width + dx] = 0;
            return;
        }

        float cumprod = 0;
        for (int ic = 0; ic < mask_dim; ++ic) 
        {
            float cval = mask_predict[ic * mask_height * mask_width + sy * mask_width + sx];
            float wval = mask_weights[ic];
            cumprod += cval * wval;
        }

        float alpha = 0.0f;
        // sigmoid
        if (cumprod >= 0.0f)
        {
            float z = expf(-cumprod);
            alpha = 1.0f / (1.0f + z);
        }
        else
        {
            float z = expf(cumprod);
            alpha = z / (1.0f + z);
        }
        mask_out[dy * out_width + dx] = alpha;
    }


    static __global__ void sigmoid_single_kernel(const float* src, float* dst, int count)
    {
        int position = blockDim.x * blockIdx.x + threadIdx.x;
        if (position >= count)
            return;

        float x = src[position];
        if (x >= 0.0f)
        {
            float z = expf(-x);
            dst[position] = 1.0f / (1.0f + z);
        }
        else
        {
            float z = expf(x);
            dst[position] = z / (1.0f + z);
        }
    }


    void yolo_bbox_decode_kernel_invoker(float* predict, int num_bboxes, int num_classes, float confidence_threshold,
                                         float* invert_affine_matrix, float* parray, int max_objects, cudaStream_t stream) 
    {
        auto grid = CUDATools::grid_dims(num_bboxes);
        auto block = CUDATools::block_dims(num_bboxes);

        checkCudaKernel(decode_kernel_yolov8_seg << <grid, block, 0, stream >> > (predict, num_bboxes, num_classes, confidence_threshold, 
                                                                              invert_affine_matrix, parray, max_objects));
    }


    void detr_bbox_decode_kernel_invoker(int* label_array, float* box_array, float* score_array, int num_bboxes,
                                         float conf_thresh, int input_width, int input_height, float* invert_affine_matrix,
                                         float* pBoxArray, int max_objects, cudaStream_t stream)
    {
        auto grid = CUDATools::grid_dims(num_bboxes);
        auto block = CUDATools::block_dims(num_bboxes);

        checkCudaKernel(decode_kernel_detr_seg << <grid, block, 0, stream >> > (label_array, box_array, score_array, num_bboxes, conf_thresh,
                                                                            input_width, input_height, invert_affine_matrix, pBoxArray, max_objects));
    }

    void cast_int64_labels_to_int32_kernel_invoker(const int64_t* src, int* dst, int count, cudaStream_t stream)
    {
        auto grid = CUDATools::grid_dims(count);
        auto block = CUDATools::block_dims(count);

        checkCudaKernel(cast_int64_labels_to_int32_kernel << <grid, block, 0, stream >> > (src, dst, count));
    }


    void nms_kernel_invoker(float* parray, float nms_threshold, int max_objects, cudaStream_t stream) 
    {
        auto grid = CUDATools::grid_dims(max_objects);
        auto block = CUDATools::block_dims(max_objects);
        checkCudaKernel(nms_kernel << <grid, block, 0, stream >> > (parray, max_objects, nms_threshold));
    }

    void yolo_mask_decode_single(float left, float top, float* mask_weights, float* mask_predict,
        int mask_width, int mask_height, float* mask_out, int mask_dim, int out_width, int out_height, cudaStream_t stream) 
    {
        // mask_weights is mask_dim(32 element) gpu pointer
        dim3 grid((out_width + 31) / 32, (out_height + 31) / 32);
        dim3 block(32, 32);

        checkCudaKernel(yolo_mask_decode_single_kernel << <grid, block, 0, stream >> > (left, top, mask_weights, mask_predict, mask_width,
                                                                                   mask_height, mask_out, mask_dim, out_width, out_height));
    }

    void detr_mask_sigmoid_single(const float* src, float* dst, int count, cudaStream_t stream)
    {
        if (src == nullptr || dst == nullptr || count <= 0)
            return;

        auto grid = CUDATools::grid_dims(count);
        auto block = CUDATools::block_dims(count);

        checkCudaKernel(sigmoid_single_kernel << <grid, block, 0, stream >> > (src, dst, count));
    }
}




