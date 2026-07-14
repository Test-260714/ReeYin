using ALGO.BlobAnalysis.Controls;
using HalconDotNet;
using ImageTool.Halcon.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ALGO.BlobAnalysis
{
    public partial class BlobAnalysisModel
    {
        public void EnsureImageControl()
        {
            if (ImageControl == null)
            {
                ImageControl = new BlobAnalysisHalconControl();
            }
        }

        public void AttachImageControl(BlobAnalysisHalconControl control)
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

        public void DetachImageControl(BlobAnalysisHalconControl control = null)
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
            InitAnalysisRegionMethod();
        }

        public void ShowHRoi()
        {
            BlobAnalysisHalconControl control = ImageControl;
            if (control == null)
            {
                return;
            }

            List<HRoi> roiList;
            lock (_roiSyncRoot)
            {
                roiList = mHRoi.Where(item => item.ModuleName == ModuleName).ToList();
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

        public void ShowHRoi(HRoi roi)
        {
            if (roi == null)
            {
                return;
            }

            try
            {
                lock (_roiSyncRoot)
                {
                    int index = mHRoi.FindIndex(item => item.roiType == roi.roiType && item.ModuleName == roi.ModuleName);
                    if (roi.fors)
                    {
                        mHRoi.Add(roi);
                        return;
                    }

                    if (index >= 0)
                    {
                        mHRoi[index] = roi;
                    }
                    else
                    {
                        mHRoi.Add(roi);
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
            BlobAnalysisHalconControl control = ImageControl;
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

        private void HookImageControlEvents(BlobAnalysisHalconControl control)
        {
            control.RoiMouseUp -= HControl_MouseUp;
            control.RoiMouseUp += HControl_MouseUp;
        }

        private void UnhookImageControlEvents(BlobAnalysisHalconControl control)
        {
            if (control == null)
            {
                return;
            }

            control.RoiMouseUp -= HControl_MouseUp;
        }

        private void RenderDisplayRoi(BlobAnalysisHalconControl control, HRoi roi)
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
                ShowTool.SetFont(control.hControl.HalconWindow, roiText.size, "false", "false");
                ShowTool.SetMsg(control.hControl.HalconWindow, roiText.text, "image", roiText.row, roiText.col, roiText.drawColor, "false");
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

        private static void RunOnImageControlUi(BlobAnalysisHalconControl control, Action action)
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
