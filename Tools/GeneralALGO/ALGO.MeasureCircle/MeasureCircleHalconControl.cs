using HalconDotNet;
using ImageTool.Halcon.Model;
using ReeYin_V.UI.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ALGO.MeasureCircle.Controls
{
    public partial class MeasureCircleHalconControl : UserControl
    {
        private const double HandlePickDistance = 16d;

        private HSmartWindowControlWPF smartWindow;
        private bool isDragging;
        private bool roiCommitted;
        private ROICircle activeCircle;
        private string activeRoiName = string.Empty;

        public MeasureCircleHalconControl()
        {
            InitializeComponent();
            WindowH = new MeasureCircleViewWindowAdapter(this);
            Loaded += MeasureCircleHalconControl_Loaded;
            Unloaded += MeasureCircleHalconControl_Unloaded;
        }

        public MeasureCircleViewWindowAdapter WindowH { get; }

        public HSmartWindowControlWPF hControl => smartWindow ??= innerControl?.getHWindowControl();

        public event EventHandler<HSmartWindowControlWPF.HMouseEventArgsWPF> RoiMouseUp;

        public void HobjectToHimage(HObject hobject)
        {
            RunOnUi(() =>
            {
                innerControl?.HobjectToHimage(hobject);
                RenderActiveRoiCore();
            });
        }

        public void ClearROI()
        {
            RunOnUi(() =>
            {
                innerControl?.ClearROI();
                RenderActiveRoiCore();
            });
        }

        public void ClearWindow()
        {
            RunOnUi(() =>
            {
                innerControl?.ClearWindow();
                RenderActiveRoiCore();
            });
        }

        public void DispObj(HObject obj, string color, bool isFillDisp)
        {
            RunOnUi(() => innerControl?.DispObj(obj, color, isFillDisp));
        }

        private void MeasureCircleHalconControl_Loaded(object sender, RoutedEventArgs e)
        {
            RunOnUi(() =>
            {
                smartWindow = innerControl?.getHWindowControl();
                HookInteractionEvents();
                RenderActiveRoiCore();
            });
        }

        private void MeasureCircleHalconControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnhookInteractionEvents();
        }

        internal ROI GetActiveRoi(out string info, out string index)
        {
            info = activeRoiName;
            index = roiCommitted && activeCircle != null ? activeRoiName : string.Empty;
            roiCommitted = false;
            return activeCircle;
        }

        internal void SetActiveCircle(string name, ROICircle circle)
        {
            RunOnUi(() =>
            {
                activeRoiName = name ?? string.Empty;
                activeCircle = circle;
                RenderActiveRoiCore();
            });
        }

        private void HookInteractionEvents()
        {
            if (smartWindow == null)
            {
                return;
            }

            smartWindow.HMouseDown -= SmartWindow_HMouseDown;
            smartWindow.HMouseMove -= SmartWindow_HMouseMove;
            smartWindow.HMouseUp -= SmartWindow_HMouseUp;

            smartWindow.HMouseDown += SmartWindow_HMouseDown;
            smartWindow.HMouseMove += SmartWindow_HMouseMove;
            smartWindow.HMouseUp += SmartWindow_HMouseUp;
        }

        private void UnhookInteractionEvents()
        {
            if (smartWindow == null)
            {
                return;
            }

            smartWindow.HMouseDown -= SmartWindow_HMouseDown;
            smartWindow.HMouseMove -= SmartWindow_HMouseMove;
            smartWindow.HMouseUp -= SmartWindow_HMouseUp;
        }

        private void SmartWindow_HMouseDown(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (e.Button != MouseButton.Left || activeCircle == null)
            {
                return;
            }

            double distance = activeCircle.DistToClosestHandle(e.Column, e.Row);
            if (distance > HandlePickDistance)
            {
                return;
            }

            isDragging = true;
            innerControl.DrawModel = true;
        }

        private void SmartWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!isDragging || activeCircle == null)
            {
                return;
            }

            activeCircle.moveByHandle(e.Column, e.Row);
            RenderActiveRoi();
        }

        private void SmartWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (isDragging && activeCircle != null)
            {
                activeCircle.moveByHandle(e.Column, e.Row);
                isDragging = false;
                roiCommitted = true;
                innerControl.DrawModel = false;
                RenderActiveRoi();
            }

            RoiMouseUp?.Invoke(this, e);
        }

        private void RenderActiveRoi()
        {
            RunOnUi(RenderActiveRoiCore);
        }

        private void RenderActiveRoiCore()
        {
            if (innerControl?.DrawObjectList == null)
            {
                return;
            }

            if (activeCircle == null)
            {
                innerControl.DrawObjectList.Clear();
                return;
            }

            HObject circleContour = null;
            HObject centerHandle = null;
            HObject radiusHandle = null;

            try
            {
                HOperatorSet.GenCircleContourXld(
                    out circleContour,
                    activeCircle.CenterY,
                    activeCircle.CenterX,
                    activeCircle.Radius,
                    0,
                    6.28318,
                    "positive",
                    1
                );
                HOperatorSet.GenRectangle2(
                    out centerHandle,
                    activeCircle.CenterY,
                    activeCircle.CenterX,
                    0,
                    4,
                    4
                );
                HOperatorSet.GenRectangle2(
                    out radiusHandle,
                    activeCircle.StartPhi,
                    activeCircle.EndPhi,
                    0,
                    4,
                    4
                );

                innerControl.DrawObjectList.Clear();
                innerControl.DrawObjectList.Add(
                    new DrawingObjectInfo
                    {
                        ShapeType = ShapeType.Circle,
                        Hobject = circleContour,
                        Color = "cyan"
                    }
                );
                innerControl.DrawObjectList.Add(
                    new DrawingObjectInfo
                    {
                        ShapeType = ShapeType.Rectangle,
                        Hobject = centerHandle,
                        Color = "cyan",
                        IsFillDisplay = true
                    }
                );
                innerControl.DrawObjectList.Add(
                    new DrawingObjectInfo
                    {
                        ShapeType = ShapeType.Rectangle,
                        Hobject = radiusHandle,
                        Color = "cyan",
                        IsFillDisplay = true
                    }
                );

                circleContour = null;
                centerHandle = null;
                radiusHandle = null;
            }
            finally
            {
                circleContour?.Dispose();
                centerHandle?.Dispose();
                radiusHandle?.Dispose();
            }
        }

        private void RunOnUi(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.Invoke(action);
            }
        }
    }

    public sealed class MeasureCircleViewWindowAdapter
    {
        private readonly MeasureCircleHalconControl owner;

        public MeasureCircleViewWindowAdapter(MeasureCircleHalconControl owner)
        {
            this.owner = owner;
        }

        public void genCircle(string name, double row, double col, double radius, ref Dictionary<string, ROI> rois)
        {
            rois ??= new Dictionary<string, ROI>();

            ROICircle circle;
            if (rois.TryGetValue(name, out ROI roi) && roi is ROICircle roiCircle)
            {
                circle = roiCircle;
            }
            else
            {
                circle = new ROICircle();
            }

            circle.CreateCircle(row, col, radius);
            circle.Type = ROIType.Circle;
            circle.Color = "cyan";
            rois[name] = circle;
            owner.SetActiveCircle(name, circle);
        }

        public ROI smallestActiveROI(out string info, out string index)
        {
            return owner.GetActiveRoi(out info, out index);
        }

        public void DispHobject(HObject obj, string color, bool isFillDisp)
        {
            owner.DispObj(obj, color, isFillDisp);
        }
    }
}
