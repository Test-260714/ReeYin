using ALGO.MeasureCircle.Controls;
using HalconDotNet;
using ImageTool.Halcon;
using ImageTool.Halcon.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ALGO.MeasureCircle
{
    public partial class MeasureCircleModel
    {
        public void EnsureImageControl()
        {
            if (ImageControl == null)
            {
                ImageControl = new MeasureCircleHalconControl();
            }
        }

        public void AttachImageControl(MeasureCircleHalconControl control)
        {
            if (control == null)
            {
                return;
            }

            if (ReferenceEquals(ImageControl, control))
            {
                HookImageControlEvents(control);
                RefreshLinkedImage();
                return;
            }

            DetachImageControl();
            ImageControl = control;
            HookImageControlEvents(control);
            RefreshLinkedImage();
        }

        public void DetachImageControl(MeasureCircleHalconControl control = null)
        {
            if (ImageControl == null)
            {
                return;
            }

            if (control != null && !ReferenceEquals(ImageControl, control))
            {
                return;
            }

            UnhookImageControlEvents(ImageControl);
            ImageControl = null;
        }

        public void InitImg()
        {
            if (ImageControl == null)
            {
                return;
            }

            ShowHRoi();
            InitCircleMethod();
        }

        public void ShowHRoi()
        {
            MeasureCircleHalconControl control = ImageControl;
            if (control == null)
            {
                return;
            }

            List<HRoi> roiList;
            lock (_roiSyncRoot)
            {
                roiList = mHRoi.Where(c => c.ModuleName == ModuleName).ToList();
            }

            RunOnImageControlUi(control, () =>
            {
                if (!ReferenceEquals(ImageControl, control))
                {
                    return;
                }

                control.ClearROI();
                foreach (HRoi roi in roiList)
                {
                    RenderDisplayRoi(control, roi);
                }
            });
        }

        public void ShowHRoi(HRoi ROI)
        {
            if (ROI == null)
            {
                return;
            }

            try
            {
                lock (_roiSyncRoot)
                {
                    int index = mHRoi.FindIndex(e => e.roiType == ROI.roiType && e.ModuleName == ROI.ModuleName);
                    if (ROI.fors)
                    {
                        mHRoi.Add(ROI);
                        return;
                    }

                    if (index > -1)
                    {
                        mHRoi[index] = ROI;
                    }
                    else
                    {
                        mHRoi.Add(ROI);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void RefreshLinkedImage()
        {
            MeasureCircleHalconControl control = ImageControl;
            if (control == null)
            {
                return;
            }

            try
            {
                HObject displayImage = null;
                if (_inputImage?.Value is HObject hObject && IsValidHObject(hObject))
                {
                    displayImage = new HImage(hObject);
                }

                if (!IsValidHObject(displayImage))
                {
                    control.ClearWindow();
                    InitImg();
                    return;
                }

                control.HobjectToHimage(displayImage);
                InitImg();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void HookImageControlEvents(MeasureCircleHalconControl control)
        {
            control.RoiMouseUp -= HControl_MouseUp;
            control.RoiMouseUp += HControl_MouseUp;
        }

        private void UnhookImageControlEvents(MeasureCircleHalconControl control)
        {
            if (control == null)
            {
                return;
            }

            control.RoiMouseUp -= HControl_MouseUp;
        }

        private void RenderDisplayRoi(MeasureCircleHalconControl control, HRoi roi)
        {
            if (control == null || roi == null)
            {
                return;
            }

            if (roi.roiType == HRoiType.文字显示)
            {
                if (control.hControl?.HalconWindow == null)
                {
                    return;
                }

                HText roiText = (HText)roi;
                ShowTool.SetFont(
                    control.hControl.HalconWindow,
                    roiText.size,
                    "false",
                    "false"
                );
                ShowTool.SetMsg(
                    control.hControl.HalconWindow,
                    roiText.text,
                    "image",
                    roiText.row,
                    roiText.col,
                    roiText.drawColor,
                    "false"
                );
                return;
            }

            if (IsValidHObject(roi.hobject))
            {
                control.WindowH.DispHobject(roi.hobject, roi.drawColor, roi.IsFillDisp);
            }
        }

        private void ClearDisplayRois()
        {
            lock (_roiSyncRoot)
            {
                mHRoi.Clear();
            }
        }

        private static bool IsValidHObject(HObject hObject)
        {
            try
            {
                return hObject != null && hObject.IsInitialized();
            }
            catch
            {
                return false;
            }
        }

        private static void RunOnImageControlUi(MeasureCircleHalconControl control, Action action)
        {
            if (control?.Dispatcher == null || action == null)
            {
                return;
            }

            if (control.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                control.Dispatcher.BeginInvoke(action);
            }
        }
    }
}
