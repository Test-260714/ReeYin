using ALGO.BlobAnalysis.Controls;
using HalconDotNet;
using ImageTool.Halcon.Model;
using System;
using System.Diagnostics;

namespace ALGO.BlobAnalysis
{
    public partial class BlobAnalysisModel
    {
        private void HControl_MouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            BlobAnalysisHalconControl control = ImageControl;
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

                roiRegion = roi as ROIRectangle2;
                if (roiRegion == null)
                {
                    return;
                }

                TempRegion.MidC = Math.Round(roiRegion.MidC, 3);
                TempRegion.MidR = Math.Round(roiRegion.MidR, 3);
                TempRegion.Length1 = Math.Round(roiRegion.Length1, 3);
                TempRegion.Length2 = Math.Round(roiRegion.Length2, 3);
                TempRegion.Phi = Math.Round(roiRegion.Phi, 6);

                InitRegionChangedFlag = true;
                CommitDraggedRegion(TempRegion);
                Run();
                InitAnalysisRegionMethod();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                InitRegionChangedFlag = false;
            }
        }

        public static void Affine2d(HTuple homMat2D, ROIRectangle2 inputRect, ROIRectangle2 tranRect)
        {
            HHomMat2D tempHomMat2D = new HHomMat2D(homMat2D);
            tranRect.Length1 = inputRect.Length1;
            tranRect.Length2 = inputRect.Length2;

            tranRect.MidR = tempHomMat2D.AffineTransPoint2d(inputRect.MidC, inputRect.MidR, out HTuple tempX);
            tranRect.MidC = tempX;

            double phi1 = ((HTuple)tempHomMat2D[0]).TupleAcos().D;
            double phi2 = ((HTuple)tempHomMat2D[1]).TupleAsin().D;
            double phi3 = ((HTuple)tempHomMat2D[4]).TupleAcos().D;
            double phi = phi2 <= 0 ? phi1 : -phi3;
            tranRect.Phi = inputRect.Phi - phi;
        }

        public void InitAnalysisRegionMethod()
        {
            BlobAnalysisHalconControl control = ImageControl;
            if (control == null)
            {
                return;
            }

            string roiKey = Serial.ToString();

            if (TranRegion.FlagLineStyle != null)
            {
                DrawRectangleRoi(control, roiKey, TranRegion.MidR, TranRegion.MidC, TranRegion.Phi, TranRegion.Length1, TranRegion.Length2);
                CaptureActiveRectangle(roiKey);
                return;
            }

            if (!RoiList.ContainsKey(roiKey))
            {
                if (TryCreateDefaultRectangle(control, roiKey))
                {
                    return;
                }

                DrawRectangleRoi(control, roiKey, InitRegionCenterY, InitRegionCenterX, DegreeToRadian(InitRegionAngleDeg), InitRegionLength1, InitRegionLength2);
                UpdateRegionState(InitRegionCenterX, InitRegionCenterY, InitRegionAngleDeg, InitRegionLength1, InitRegionLength2);
                CaptureActiveRectangle(roiKey);
                return;
            }

            if (TryGetInverseHomMat2D(out HTuple inverseHomMat2D))
            {
                DrawRectangleRoi(control, roiKey, TranRegion.MidR, TranRegion.MidC, TranRegion.Phi, TranRegion.Length1, TranRegion.Length2);
                Affine2d(inverseHomMat2D, TranRegion, InitRegion);
                NormalizeRectangle(InitRegion);
                if (InitRegionChangedFlag)
                {
                    UpdateInitRegionProperties(InitRegion);
                }
            }
            else
            {
                DrawRectangleRoi(control, roiKey, InitRegion.MidR, InitRegion.MidC, InitRegion.Phi, InitRegion.Length1, InitRegion.Length2);
                if (InitRegionChangedFlag)
                {
                    UpdateInitRegionProperties(InitRegion);
                }
            }

            CaptureActiveRectangle(roiKey);
        }

        private void InitRegionChanged()
        {
            if (InitRegionChangedFlag)
            {
                return;
            }

            InitRegion.MidC = InitRegionCenterX;
            InitRegion.MidR = InitRegionCenterY;
            InitRegion.Phi = DegreeToRadian(InitRegionAngleDeg);
            InitRegion.Length1 = InitRegionLength1;
            InitRegion.Length2 = InitRegionLength2;
            DisenableAffine2d = true;

            if (roiRegion == null)
            {
                InitAnalysisRegionMethod();
                return;
            }

            if (DisenableAffine2d && HasAffineTransform(HomMat2D))
            {
                Affine2d(HomMat2D, InitRegion, TempRegion);
                NormalizeRectangle(TempRegion);
            }
            else
            {
                CopyRectangle(InitRegion, TempRegion);
                roiRegion.MidC = InitRegion.MidC;
                roiRegion.MidR = InitRegion.MidR;
                roiRegion.Phi = InitRegion.Phi;
                roiRegion.Length1 = InitRegion.Length1;
                roiRegion.Length2 = InitRegion.Length2;
            }

            Run();
            InitAnalysisRegionMethod();
        }

        private bool TryCreateDefaultRectangle(BlobAnalysisHalconControl control, string roiKey)
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

            double centerY = InitRegionCenterY;
            double centerX = InitRegionCenterX;
            double angleDeg = InitRegionAngleDeg;
            double length1 = InitRegionLength1;
            double length2 = InitRegionLength2;

            if (IsDefaultInitRegion())
            {
                tempImage.GetImageSize(out int imageWidth, out int imageHeight);
                centerY = imageHeight / 2.0;
                centerX = imageWidth / 2.0;
                length1 = Math.Max(10, imageWidth / 4.0);
                length2 = Math.Max(10, imageHeight / 4.0);
                angleDeg = 0;
            }

            DrawRectangleRoi(control, roiKey, centerY, centerX, DegreeToRadian(angleDeg), length1, length2);
            UpdateRegionState(centerX, centerY, angleDeg, length1, length2);
            CaptureActiveRectangle(roiKey);
            return true;
        }

        private bool IsDefaultInitRegion()
        {
            bool isCurrentDefault =
                InitRegionCenterY == DefaultInitRegionCenterY &&
                InitRegionCenterX == DefaultInitRegionCenterX;
            bool isLegacyDefault =
                InitRegionCenterY == LegacyDefaultInitRegionCenterY &&
                InitRegionCenterX == LegacyDefaultInitRegionCenterX;

            return (isCurrentDefault || isLegacyDefault) &&
                InitRegionLength1 == DefaultInitRegionLength1 &&
                InitRegionLength2 == DefaultInitRegionLength2;
        }

        private void CommitDraggedRegion(ROIRectangle2 displayRegion)
        {
            if (displayRegion == null)
            {
                return;
            }

            NormalizeRectangle(displayRegion);
            CopyRectangle(displayRegion, TempRegion);

            if (TryGetInverseHomMat2D(out HTuple inverseHomMat2D))
            {
                Affine2d(inverseHomMat2D, displayRegion, InitRegion);
                NormalizeRectangle(InitRegion);
                CopyRectangle(displayRegion, TranRegion);
            }
            else
            {
                CopyRectangle(displayRegion, InitRegion);
                CopyRectangle(displayRegion, TranRegion);
            }

            UpdateInitRegionProperties(InitRegion);
            DisenableAffine2d = false;
        }

        private bool TryGetInverseHomMat2D(out HTuple inverseHomMat2D)
        {
            inverseHomMat2D = new HTuple();

            if (HasAffineTransform(HomMat2D_Inverse))
            {
                inverseHomMat2D = HomMat2D_Inverse;
                return true;
            }

            if (!HasAffineTransform(HomMat2D))
            {
                return false;
            }

            try
            {
                HOperatorSet.HomMat2dInvert(HomMat2D, out inverseHomMat2D);
                HomMat2D_Inverse = inverseHomMat2D;
                return HasAffineTransform(inverseHomMat2D);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                inverseHomMat2D = new HTuple();
                return false;
            }
        }

        private void DrawRectangleRoi(
            BlobAnalysisHalconControl control,
            string roiKey,
            double row,
            double col,
            double phi,
            double length1,
            double length2)
        {
            control.WindowH.genRect2(roiKey, row, col, phi, length1, length2, ref RoiList);
        }

        private void UpdateRegionState(double centerX, double centerY, double angleDeg, double length1, double length2)
        {
            _initRegionCenterX = centerX;
            _initRegionCenterY = centerY;
            _initRegionAngleDeg = angleDeg;
            _initRegionLength1 = length1;
            _initRegionLength2 = length2;
            RaisePropertyChanged(nameof(InitRegionCenterX));
            RaisePropertyChanged(nameof(InitRegionCenterY));
            RaisePropertyChanged(nameof(InitRegionAngleDeg));
            RaisePropertyChanged(nameof(InitRegionLength1));
            RaisePropertyChanged(nameof(InitRegionLength2));

            InitRegion.MidC = centerX;
            InitRegion.MidR = centerY;
            InitRegion.Phi = DegreeToRadian(angleDeg);
            InitRegion.Length1 = length1;
            InitRegion.Length2 = length2;
            CopyRectangle(InitRegion, TempRegion);
            CopyRectangle(InitRegion, TranRegion);
        }

        private void CaptureActiveRectangle(string roiKey)
        {
            if (!RoiList.TryGetValue(roiKey, out ROI roi))
            {
                return;
            }

            roiRegion = roi as ROIRectangle2;
            if (roiRegion == null)
            {
                return;
            }

            TempRegion.MidC = roiRegion.MidC;
            TempRegion.MidR = roiRegion.MidR;
            TempRegion.Phi = roiRegion.Phi;
            TempRegion.Length1 = roiRegion.Length1;
            TempRegion.Length2 = roiRegion.Length2;
        }

        private void UpdateInitRegionProperties(ROIRectangle2 rectangle)
        {
            _initRegionCenterX = Round4(rectangle.MidC);
            _initRegionCenterY = Round4(rectangle.MidR);
            _initRegionAngleDeg = Round4(((HTuple)rectangle.Phi).TupleDeg().D);
            _initRegionLength1 = Round4(rectangle.Length1);
            _initRegionLength2 = Round4(rectangle.Length2);
            RaisePropertyChanged(nameof(InitRegionCenterX));
            RaisePropertyChanged(nameof(InitRegionCenterY));
            RaisePropertyChanged(nameof(InitRegionAngleDeg));
            RaisePropertyChanged(nameof(InitRegionLength1));
            RaisePropertyChanged(nameof(InitRegionLength2));
        }

        private static void NormalizeRectangle(ROIRectangle2 rectangle)
        {
            rectangle.MidC = Round4(rectangle.MidC);
            rectangle.MidR = Round4(rectangle.MidR);
            rectangle.Phi = Math.Round(rectangle.Phi, 6);
            rectangle.Length1 = Math.Max(0.01, Round4(rectangle.Length1));
            rectangle.Length2 = Math.Max(0.01, Round4(rectangle.Length2));
        }

        private static bool HasAffineTransform(HTuple homMat2D)
        {
            return homMat2D != null && homMat2D.Length > 0;
        }

        private static void CopyRectangle(ROIRectangle2 source, ROIRectangle2 target)
        {
            target.MidC = source.MidC;
            target.MidR = source.MidR;
            target.Phi = source.Phi;
            target.Length1 = source.Length1;
            target.Length2 = source.Length2;
        }
    }
}
