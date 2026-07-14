#include "trt_segmentation.h"

#include "tensorrt/common/cuda_tools.hpp"



namespace Segmentation
{
	__global__ void warp_affine_nearest_neighbor_general_kernel(int* src, int src_line_size, int src_width, int src_height,
        int* dst, int dst_width, int dst_height, float* warp_affine_matrix_2_3, int edge)
	{
        int position = blockDim.x * blockIdx.x + threadIdx.x;
        if (position >= edge) return;

        // 读取仿射矩阵元素
        float m_x1 = warp_affine_matrix_2_3[0];
        float m_y1 = warp_affine_matrix_2_3[1];
        float m_z1 = warp_affine_matrix_2_3[2];
        float m_x2 = warp_affine_matrix_2_3[3];
        float m_y2 = warp_affine_matrix_2_3[4];
        float m_z2 = warp_affine_matrix_2_3[5];

        int dx = position % dst_width;
        int dy = position / dst_width;

        float src_x = m_x1 * dx + m_y1 * dy + m_z1;
        float src_y = m_x2 * dx + m_y2 * dy + m_z2;

        int channel = (int)(src_line_size / src_width);

        // 对最近邻：取四舍五入到最近整数像素
        int sx = (int)roundf(src_x);
        int sy = (int)roundf(src_y);

        bool oob = (src_x <= -1.0f || src_x >= src_width || src_y <= -1.0f || src_y >= src_height) || 
                    (sx < 0) || (sx >= src_width) || (sy < 0) || (sy >= src_height);

        int c0;
        if (oob)
        {
            c0 = 0;
        }
        else
        {
            int* psrc = src + sy * src_line_size + sx * 1;
            c0 = psrc[0];
        }

        int* pdst_c0 = dst + dy * dst_width + dx;
        *pdst_c0 = c0;

	}



	void warp_affine_nearest_neighbor_general(int* src, int src_line_size, int src_width, int src_height,
        int* dst, int dst_width, int dst_height, float* matrix_2_3, cudaStream_t stream)
	{
		int jobs = dst_width * dst_height;
		auto grid = CUDATools::grid_dims(jobs);
		auto block = CUDATools::block_dims(jobs);

		checkCudaKernel(warp_affine_nearest_neighbor_general_kernel << <grid, block, 0, stream >> > (
			src, src_line_size, src_width, src_height, dst, dst_width, dst_height, matrix_2_3, jobs));
	}

}