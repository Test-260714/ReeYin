using HalconDotNet;
using ImageTool.Halcon.Model;
using ReeYin_V.UI.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ALGO.BlobAnalysis.Controls
{
    public partial class BlobAnalysisHalconControl : UserControl
    {
        private const double HandlePickDistance = 16d;

        private HSmartWindowControlWPF smartWindow;
        private bool isDragging;
        private bool roiCommitted;
        private ROIRectangle2 activeRectangle;
        private string activeRoiName = string.Empty;

        public BlobAnalysisHalconControl()
        {
            InitializeComponent();
            WindowH = new BlobAnalysisViewWindowAdapter(this);
            Loaded += BlobAnalysisHalconControl_Loaded;
            Unloaded += BlobAnalysisHalconControl_Unloaded;
        }

        public BlobAnalysisViewWindowAdapter WindowH { get; }

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

        private void BlobAnalysisHalconControl_Loaded(object sender, RoutedEventArgs e)
        {
            RunOnUi(() =>
            {
                smartWindow = innerControl?.getHWindowControl();
                HookInteractionEvents();
                RenderActiveRoiCore();
            });
        }

        private void BlobAnalysisHalconControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnhookInteractionEvents();
        }

        internal ROI GetActiveRoi(out string info, out string index)
        {
            info = activeRoiName;
            index = roiCommitted && activeRectangle != null ? activeRoiName : string.Empty;
            roiCommitted = false;
            return activeRectangle;
        }

        internal void SetActiveRectangle(string name, ROIRectangle2 rectangle)
        {
            RunOnUi(() =>
            {
                activeRoiName = name ?? string.Empty;
                activeRectangle = rectangle;
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
            if (e.Button != MouseButton.Left || activeRectangle == null)
            {
                return;
            }

            double distance = activeRectangle.DistToClosestHandle(e.Column, e.Row);
            if (distance > HandlePickDistance)
            {
                return;
            }

            isDragging = true;
            innerControl.DrawModel = true;
        }

        private void SmartWindow_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (!isDragging || activeRectangle == null)
            {
                return;
            }

            activeRectangle.moveByHandle(e.Column, e.Row);
            RenderActiveRoi();
        }

        private void SmartWindow_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e)
        {
            if (isDragging && activeRectangle != null)
            {
                activeRectangle.moveByHandle(e.Column, e.Row);
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

            innerControl.DrawObjectList.Clear();
            if (activeRectangle == null)
            {
                return;
            }

            HObject rectangleContour = null;
            HObject[] handles = new HObject[6];
            HObject directionLine = null;

            try
            {
                HOperatorSet.GenRectangle2ContourXld(
                    out rectangleContour,
                    activeRectangle.MidR,
                    activeRectangle.MidC,
                    activeRectangle.Phi,
                    activeRectangle.Length1,
                    activeRectangle.Length2);

                (double row, double col)[] points = GetHandlePoints(activeRectangle);
                for (int i = 0; i < points.Length; i++)
                {
                    HOperatorSet.GenRectangle2(
                        out handles[i],
                        points[i].row,
                        points[i].col,
                        0,
                        4,
                        4);
                }

                HOperatorSet.GenContourPolygonXld(
                    out directionLine,
                    new HTuple(new[] { activeRectangle.MidR, points[5].row }),
                    new HTuple(new[] { activeRectangle.MidC, points[5].col }));

                innerControl.DrawObjectList.Add(new DrawingObjectInfo
                {
                    ShapeType = ShapeType.Rectangle,
                    Hobject = rectangleContour,
                    Color = "cyan",
                });

                innerControl.DrawObjectList.Add(new DrawingObjectInfo
                {
                    ShapeType = ShapeType.Region,
                    Hobject = directionLine,
                    Color = "cyan",
                });

                foreach (HObject handle in handles)
                {
                    innerControl.DrawObjectList.Add(new DrawingObjectInfo
                    {
                        ShapeType = ShapeType.Rectangle,
                        Hobject = handle,
                        Color = "cyan",
                        IsFillDisplay = true,
                    });
                }

                rectangleContour = null;
                directionLine = null;
                Array.Clear(handles, 0, handles.Length);
            }
            finally
            {
                rectangleContour?.Dispose();
                directionLine?.Dispose();
                foreach (HObject handle in handles)
                {
                    handle?.Dispose();
                }
            }
        }

        private static (double row, double col)[] GetHandlePoints(ROIRectangle2 rectangle)
        {
            return new[]
            {
                TransformPoint(rectangle, -rectangle.Length1, -rectangle.Length2),
                TransformPoint(rectangle,  rectangle.Length1, -rectangle.Length2),
                TransformPoint(rectangle,  rectangle.Length1,  rectangle.Length2),
                TransformPoint(rectangle, -rectangle.Length1,  rectangle.Length2),
                TransformPoint(rectangle, 0, 0),
                TransformPoint(rectangle, rectangle.Length1 * 0.6, 0),
            };
        }

        private static (double row, double col) TransformPoint(
            ROIRectangle2 rectangle,
            double localX,
            double localY)
        {
            double cos = Math.Cos(rectangle.Phi);
            double sin = Math.Sin(rectangle.Phi);
            double col = rectangle.MidC + (localX * cos) - (localY * sin);
            double row = rectangle.MidR + (localX * sin) + (localY * cos);
            return (row, col);
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

    public sealed class BlobAnalysisViewWindowAdapter
    {
        private readonly BlobAnalysisHalconControl owner;

        public BlobAnalysisViewWindowAdapter(BlobAnalysisHalconControl owner)
        {
            this.owner = owner;
        }

        public void genRect2(
            string name,
            double row,
            double col,
            double phi,
            double length1,
            double length2,
            ref Dictionary<string, ROI> rois)
        {
            rois ??= new Dictionary<string, ROI>();

            ROIRectangle2 rectangle;
            if (rois.TryGetValue(name, out ROI roi) && roi is ROIRectangle2 roiRectangle)
            {
                rectangle = roiRectangle;
            }
            else
            {
                rectangle = new ROIRectangle2();
            }

            rectangle.CreateRectangle2(row, col, phi, length1, length2);
            rectangle.Type = ROIType.Rectangle2;
            rectangle.Color = "cyan";
            rois[name] = rectangle;
            owner.SetActiveRectangle(name, rectangle);
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
