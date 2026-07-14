using ALGO.MeasureCircle.Controls;
using HalconDotNet;
using ImageTool.Halcon.Model;
using System;
using System.Diagnostics;

namespace ALGO.MeasureCircle
{
    public partial class MeasureCircleModel
    {
        private void HControl_MouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            MeasureCircleHalconControl control = ImageControl;
            if (control == null)
            {
                return;
            }

            try
            {
                ROI roi = control.WindowH.smallestActiveROI(out _, out string index);
                if (string.IsNullOrWhiteSpace(index))
                {
                    return;
                }

                roiCircle = roi as ROICircle;
                if (roiCircle == null)
                {
                    return;
                }

                TempCircle.CenterX = Math.Round(roiCircle.CenterX, 3);
                TempCircle.CenterY = Math.Round(roiCircle.CenterY, 3);
                TempCircle.Radius = Math.Round(roiCircle.Radius, 3);
                DisenableAffine2d = true;
                InitCircleChanged_Flag = true;
                Run();
                InitCircleMethod();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                InitCircleChanged_Flag = false;
            }
        }

        /// <summary>
        /// 对点应用任意加法 2D 变换 圆
        /// </summary>
        public static void Affine2d(HTuple HomMat2D, ROICircle intCircle, ROICircle tranCircle)
        {
            HHomMat2D tempHomMat2D = new HHomMat2D(HomMat2D);
            tranCircle.CenterY = tempHomMat2D.AffineTransPoint2d(
                intCircle.CenterY,
                intCircle.CenterX,
                out HTuple tempX
            );
            tranCircle.CenterX = tempX;
            tranCircle.Radius = intCircle.Radius;
        }

        public void InitCircleMethod()
        {
            MeasureCircleHalconControl control = ImageControl;
            if (control == null)
            {
                return;
            }

            string roiKey = Serial.ToString();

            if (TranCircle.FlagLineStyle != null)
            {
                DrawCircleRoi(control, roiKey, TranCircle.CenterY, TranCircle.CenterX, TranCircle.Radius);
                CaptureActiveCircle(roiKey);
                return;
            }

            if (!RoiList.ContainsKey(roiKey))
            {
                if (TryCreateDefaultCircle(control, roiKey))
                {
                    return;
                }

                DrawCircleRoi(control, roiKey, InitCircleCenterY, InitCircleCenterX, InitCircleRadius);
                UpdateCircleState(InitCircleCenterX, InitCircleCenterY, InitCircleRadius);
                CaptureActiveCircle(roiKey);
                return;
            }

            if (HomMat2D_Inverse != null && HomMat2D_Inverse.Length > 0)
            {
                DrawCircleRoi(control, roiKey, TranCircle.CenterY, TranCircle.CenterX, TranCircle.Radius);
                Affine2d(HomMat2D_Inverse, TranCircle, InitCircle);
                InitCircle.CenterX = Math.Round(InitCircle.CenterX, 3);
                InitCircle.CenterY = Math.Round(InitCircle.CenterY, 3);
                InitCircle.Radius = Math.Round(InitCircle.Radius, 3);
                if (InitCircleChanged_Flag)
                {
                    InitCircleCenterX = InitCircle.CenterX;
                    InitCircleCenterY = InitCircle.CenterY;
                    InitCircleRadius = InitCircle.Radius;
                }
            }
            else
            {
                DrawCircleRoi(control, roiKey, InitCircle.CenterY, InitCircle.CenterX, InitCircle.Radius);
                if (InitCircleChanged_Flag)
                {
                    InitCircleCenterX = InitCircle.CenterX;
                    InitCircleCenterY = InitCircle.CenterY;
                    InitCircleRadius = InitCircle.Radius;
                }
            }

            CaptureActiveCircle(roiKey);
        }

        private void InitCircleChanged()
        {
            if (InitCircleChanged_Flag)
            {
                return;
            }

            InitCircle.CenterX = InitCircleCenterX;
            InitCircle.CenterY = InitCircleCenterY;
            InitCircle.Radius = InitCircleRadius;
            DisenableAffine2d = true;

            if (roiCircle == null)
            {
                InitCircleMethod();
                return;
            }

            if (DisenableAffine2d && HomMat2D != null && HomMat2D.Length > 0)
            {
                Affine2d(HomMat2D, InitCircle, TempCircle);
            }
            else
            {
                roiCircle.CenterX = InitCircle.CenterX;
                roiCircle.CenterY = InitCircle.CenterY;
                roiCircle.Radius = InitCircle.Radius;
                TempCircle.CenterX = InitCircle.CenterX;
                TempCircle.CenterY = InitCircle.CenterY;
                TempCircle.Radius = InitCircle.Radius;
            }

            Run();
            InitCircleMethod();
        }

        private bool TryCreateDefaultCircle(MeasureCircleHalconControl control, string roiKey)
        {
            if (!(_inputImage?.Value is HObject inputImage) || !IsValidHObject(inputImage))
            {
                return false;
            }

            using HImage tempImage = new HImage(inputImage).Clone();
            if (tempImage == null || !tempImage.IsInitialized())
            {
                return false;
            }

            double centerY = InitCircleCenterY;
            double centerX = InitCircleCenterX;
            double radius = InitCircleRadius;

            if (InitCircleCenterY == 50.0 && InitCircleCenterX == 50.0 && InitCircleRadius == 40.0)
            {
                tempImage.GetImageSize(out int imageWidth, out int imageHeight);
                centerY = imageHeight / 2.0;
                centerX = imageWidth / 2.0;
                radius = imageWidth / 8.0;
            }

            DrawCircleRoi(control, roiKey, centerY, centerX, radius);
            UpdateCircleState(centerX, centerY, radius);
            CaptureActiveCircle(roiKey);
            return true;
        }

        private void DrawCircleRoi(MeasureCircleHalconControl control, string roiKey, double row, double col, double radius)
        {
            control.WindowH.genCircle(roiKey, row, col, radius, ref RoiList);
        }

        private void UpdateCircleState(double centerX, double centerY, double radius)
        {
            _initCircleCenterX = centerX;
            _initCircleCenterY = centerY;
            _initCircleRadius = radius;
            RaisePropertyChanged(nameof(InitCircleCenterX));
            RaisePropertyChanged(nameof(InitCircleCenterY));
            RaisePropertyChanged(nameof(InitCircleRadius));

            InitCircle.CenterX = centerX;
            InitCircle.CenterY = centerY;
            InitCircle.Radius = radius;

            TempCircle.CenterX = centerX;
            TempCircle.CenterY = centerY;
            TempCircle.Radius = radius;

            TranCircle.CenterX = centerX;
            TranCircle.CenterY = centerY;
            TranCircle.Radius = radius;
        }

        private void CaptureActiveCircle(string roiKey)
        {
            if (!RoiList.TryGetValue(roiKey, out ROI roi))
            {
                return;
            }

            roiCircle = roi as ROICircle;
            if (roiCircle == null)
            {
                return;
            }

            TempCircle.CenterX = roiCircle.CenterX;
            TempCircle.CenterY = roiCircle.CenterY;
            TempCircle.Radius = roiCircle.Radius;
        }
    }
}
