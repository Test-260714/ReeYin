using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReeYin_V.Core.ResultsDisplay
{
    /// <summary>
    /// 图像结果展示
    /// </summary>
    public class ImageResultsDisplay : ResultsDisplayBase
    {
        /// <summary>
        /// 灰度图
        /// </summary>
        public Mat GrayImage { get; set; }

        /// <summary>
        /// 高度图
        /// </summary>
        public Mat HeightImage { get; set; }



    }
}
