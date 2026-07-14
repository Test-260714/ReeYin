using HalconDotNet;
using HandyControl.Controls;
using Microsoft.Win32;
using ReeYin_V.Core.IOC;
using ReeYin_V.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#if false
namespace ReeYin_V.UI.Controls
{
    public class ImageEditView : Control
    {
        private HSmartWindowControlWPF hSmart;
        private HWindow hWindow;
        private TextBlock txtMsg;
        private TextBlock txtPos; // 用于显示坐标
        private ObservableCollection<DrawingObjectInfo> subscribedDrawObjectList;
        // 安全获取当前有效的HWindow
        private HWindow SafeHWindow => hWindow ?? (hSmart?.HalconWindow ?? null);

        #region Dependency Properties

        public HObject Image
        {
            get { return (HObject)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(HObject), typeof(ImageEditView),
                new PropertyMetadata(null, ImageChangedCallBack));

        public HWindow HWindow
        {
            get { return (HWindow)GetValue(HWindowProperty); }
            private set { SetValue(HWindowProperty, value); }
        }
        public static readonly DependencyProperty HWindowProperty =
            DependencyProperty.Register("HWindow", typeof(HWindow), typeof(ImageEditView),
                new PropertyMetadata(null));

        public HObject MaskObject
        {
            get { return (HObject)GetValue(MaskObjectProperty); }
            set { SetValue(MaskObjectProperty, value); }
        }
        public static readonly DependencyProperty MaskObjectProperty =
            DependencyProperty.Register("MaskObject", typeof(HObject), typeof(ImageEditView),
                new PropertyMetadata(null, MaskObjectChangedCallBack));

        public ObservableCollection<DrawingObjectInfo> DrawObjectList
        {
            get { return (ObservableCollection<DrawingObjectInfo>)GetValue(DrawObjectListProperty); }
            set { SetValue(DrawObjectListProperty, value); }
        }
        public static readonly DependencyProperty DrawObjectListProperty =
            DependencyProperty.Register("DrawObjectList", typeof(ObservableCollection<DrawingObjectInfo>),
                typeof(ImageEditView), new PropertyMetadata(null, DrawObjectListChangedCallBack));

        #endregion

        static ImageEditView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageEditView),
                new FrameworkPropertyMetadata(typeof(ImageEditView)));
        }

        public ImageEditView()
        {
            DrawObjectList ??= new ObservableCollection<DrawingObjectInfo>();
            UpdateDrawObjectListSubscription(null, DrawObjectList);
        }

        public static void ImageChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageEditView view)
            {
                view.Dispatcher.Invoke(view.RefreshDisplay);
            }
        }

        private static void MaskObjectChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageEditView view)
            {
                view.Dispatcher.Invoke(view.RefreshDisplay);
            }
        }

        private static void DrawObjectListChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageEditView view)
            {
                view.UpdateDrawObjectListSubscription(
                    e.OldValue as ObservableCollection<DrawingObjectInfo>,
                    e.NewValue as ObservableCollection<DrawingObjectInfo>);
                view.Dispatcher.Invoke(view.RefreshDisplay);
            }
        }

        private void UpdateDrawObjectListSubscription(
            ObservableCollection<DrawingObjectInfo> oldValue,
            ObservableCollection<DrawingObjectInfo> newValue)
        {
            if (ReferenceEquals(subscribedDrawObjectList, oldValue) && oldValue != null)
            {
                subscribedDrawObjectList.CollectionChanged -= DrawObjectList_CollectionChanged;
            }

            if (ReferenceEquals(subscribedDrawObjectList, newValue))
            {
                return;
            }

            if (subscribedDrawObjectList != null)
            {
                subscribedDrawObjectList.CollectionChanged -= DrawObjectList_CollectionChanged;
            }

            subscribedDrawObjectList = newValue;

            if (subscribedDrawObjectList != null)
            {
                subscribedDrawObjectList.CollectionChanged += DrawObjectList_CollectionChanged;
            }
        }

        private void DrawObjectList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(RefreshDisplay));
        }

        private void RefreshDisplay()
        {
            Display(Image);
        }

        private void Display(HObject hObject)
        {
            if (SafeHWindow == null)
            {
                UpdateMessage("窗口未初始化，无法显示图像");
                return;
            }

            try
            {
                SafeHWindow.ClearWindow();

                if (hObject == null || hObject.IsInitialized() == false)
                {
                    UpdateMessage("图像对象无效");
                    return;
                }
                //会拉伸为符合页面的大小
                // 2️⃣ 根据当前图像尺寸设置 Part（关键）
                //HOperatorSet.GetImageSize(hObject, out HTuple width, out HTuple height);
                SafeHWindow.DispObj(hObject);
                //SafeHWindow.SetPart(0, 0, (int)height - 1, (int)width - 2);
                SafeHWindow.SetPart(0, 0, - 2, - 2);
                DisplayOverlayObjects();

                UpdateMessage(string.Empty);
            }
            catch (HOperatorException ex)
            {
                UpdateMessage($"显示图像失败: {ex.Message}");
            }
        }

        private void DisplayOverlayObjects()
        {
            if (DrawObjectList != null)
            {
                foreach (DrawingObjectInfo drawObject in DrawObjectList.ToList())
                {
                    DisplayOverlayObject(drawObject?.Hobject, drawObject?.Color, drawObject?.IsFillDisplay ?? false);
                }
            }

            DisplayOverlayObject(MaskObject, "#00ff00", true);
        }

        private void DisplayOverlayObject(HObject hObject, string color, bool isFillDisplay)
        {
            if (SafeHWindow == null || hObject == null || !hObject.IsInitialized())
            {
                return;
            }

            try
            {
                HOperatorSet.SetColor(SafeHWindow, string.IsNullOrWhiteSpace(color) ? "yellow" : color);
                HOperatorSet.SetDraw(SafeHWindow, isFillDisplay ? "fill" : "margin");
                SafeHWindow.DispObj(hObject);
            }
            catch (HOperatorException)
            {
            }
            finally
            {
                try
                {
                    HOperatorSet.SetDraw(SafeHWindow, "margin");
                }
                catch
                {
                }
            }
        }

        public override void OnApplyTemplate()
        {
            // 清理旧事件订阅
            if (hSmart != null)
            {
                hSmart.Loaded -= HSmart_Loaded;
                hSmart.MouseMove -= HSmart_MouseMove;
                hSmart.SizeChanged -= HSmart_SizeChanged;
            }

            // 初始化控件引用
            txtMsg = GetTemplateChild("PART_MSG") as TextBlock;
            txtPos = GetTemplateChild("PART_POS") as TextBlock;
            hSmart = GetTemplateChild("PART_SMART") as HSmartWindowControlWPF;

            // 仅在hSmart有效时订阅事件
            if (hSmart != null)
            {
                hSmart.Loaded += HSmart_Loaded;
                hSmart.MouseMove += HSmart_MouseMove;
                hSmart.SizeChanged += HSmart_SizeChanged;

                if (hSmart.IsLoaded)
                {
                    InitializeHWindow();
                }
            }
            else
            {
                UpdateMessage("未找到HSmartWindow控件");
            }

            // --- 绘制按钮事件初始化 ---
            if (GetTemplateChild("PART_Rectangle") is MenuItem btnRect)
                btnRect.Click += BtnRect_Click;

            if (GetTemplateChild("PART_Ellipse") is MenuItem btnEllipse)
                btnEllipse.Click += BtnEllipse_Click;

            if (GetTemplateChild("PART_Circle") is MenuItem btnCircle)
                btnCircle.Click += BtnCircle_Click;

            if (GetTemplateChild("PART_Region") is MenuItem btnRegion)
                btnRegion.Click += BtnRegion_Click;

            if (GetTemplateChild("PART_MASK") is MenuItem btnMask)
                btnMask.Click += BtnMask_Click;

            if (GetTemplateChild("PART_Clear") is MenuItem btnClear)
                btnClear.Click += (s, e) => ClearDrawing();

            if (GetTemplateChild("PART_SaveImage") is MenuItem btnSave)
                btnSave.Click += BtnSaveImage_Click;

            base.OnApplyTemplate();
        }

        private void InitializeHWindow()
        {
            if (hSmart == null) return;

            try
            {
                var newWindow = hSmart.HalconWindow;
                if (newWindow != null && !newWindow.Equals(hWindow))
                {
                    hWindow = newWindow;
                    HWindow = hWindow; // 同步依赖属性
                    UpdateMessage("窗口初始化完成");

                    if (Image != null && Image.IsInitialized())
                    {
                        Display(Image);
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateMessage($"窗口初始化失败: {ex.Message}");
            }
        }

        private void HSmart_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeHWindow();
        }

        private void HSmart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Image != null && Image.IsInitialized())
                {
                    RefreshDisplay();
                }
            }));
        }

        // ⭐ 实时显示鼠标坐标和像素值
        private void HSmart_MouseMove(object sender, MouseEventArgs e)
        {
            // 确保窗口、文本框和图像都已初始化
            if (SafeHWindow == null || txtPos == null || Image == null || !Image.IsInitialized())
            {
                txtPos?.Dispatcher.Invoke(() => txtPos.Text = string.Empty);
                return;
            }

            try
            {
                HTuple width, height;

                // 1. 获取图像尺寸
                HOperatorSet.GetImageSize(Image, out width, out height);

                // 2. 获取鼠标在图像中的坐标（子像素级）
                HOperatorSet.GetMpositionSubPix(SafeHWindow, out HTuple rowF, out HTuple colF, out HTuple button);

                // 3. 读取图像灰度值或RGB值
                HTuple grayValue = new HTuple();
                double dRow = rowF.D;
                double dCol = colF.D;

                try
                {
                    // 检查鼠标是否在图像的有效边界内
                    if (dRow >= 0 && dCol >= 0 && dRow < height.D && dCol < width.D)
                    {
                        HOperatorSet.GetGrayval(Image, dRow, dCol, out grayValue);
                    }
                }
                catch (HOperatorException)
                {
                    // 忽略读取像素值时的常见错误
                }

                // 4. 格式化并显示坐标和像素值 (X/列, Y/行)
                string text = $"X: {dCol:F1} | Y: {dRow:F1}";

                if (grayValue.Length > 0)
                {
                    if (grayValue.Length == 1)
                    {
                        text += $" | Value: {grayValue.I}";
                    }
                    else if (grayValue.Length >= 3)
                    {
                        text += $" | RGB: ({grayValue[0].I}, {grayValue[1].I}, {grayValue[2].I})";
                    }
                }

                // 使用 Dispatcher 更新 UI 线程的 TextBlock
                txtPos.Dispatcher.Invoke(() => txtPos.Text = text);
            }
            catch (Exception ex)
            {
                // 防止在快速移动鼠标或 Halcon 状态异常时 UI 线程崩溃
                txtPos?.Dispatcher.Invoke(() => txtPos.Text = "坐标读取错误");
            }
        }

        private void ClearDrawing()
        {
            if (SafeHWindow == null)
            {
                UpdateMessage("窗口未初始化，无法清除");
                return;
            }

            try
            {
                // 释放 DrawObjectList 中的 Halcon 资源
                foreach (var obj in DrawObjectList)
                {
                    obj.Hobject?.Dispose();
                }
                DrawObjectList.Clear();

                // 释放 MaskObject 的 Halcon 资源
                MaskObject?.Dispose();

                // ⭐ 修复：使用 Dispatcher 确保对依赖属性的访问安全
                Dispatcher.Invoke(() =>
                {
                    MaskObject = null;
                });
                RefreshDisplay();
            }
            catch (HOperatorException ex)
            {
                UpdateMessage($"清除失败: {ex.Message}");
            }
        }

        private void BtnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (Image == null || Image.IsInitialized() == false)
            {
                UpdateMessage("没有图像可保存。");
                return;
            }
            PrismProvider.Dispatcher.Invoke(() =>
            {
                // DispObj 必须在 UI 线程执行
                SafeHWindow?.DispObj(Image);
            });


            // 使用 SaveFileDialog 获取保存路径 (此对话框必须在 UI 线程运行)
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG 图像 (*.png)|*.png|JPEG 图像 (*.jpg)|*.jpg|TIFF 图像 (*.tif)|*.tif|所有文件 (*.*)|*.*",
                FileName = "HalconImage",
                DefaultExt = ".png"
            };

            if (saveDialog.ShowDialog() == true)
            {
                string filePath = saveDialog.FileName;
                string extension = System.IO.Path.GetExtension(filePath).ToLower();
                string format = "png";

                switch (extension)
                {
                    case ".png": format = "png"; break;
                    case ".jpg": case ".jpeg": format = "jpeg"; break;
                    case ".tif": case ".tiff": format = "tiff"; break;
                    default: format = "png"; break;
                }

                UpdateMessage($"正在保存图像为 {format} 格式...");

                try
                {
                    // 在后台线程执行 Halcon 存图操作，避免 UI 阻塞
                    Task.Run(() =>
                    {
                        // 关键：克隆图像对象以确保线程安全和资源独立性
                        using (HObject clonedImage = Image.Clone())
                        {
                            try
                            {
                                HOperatorSet.WriteImage(clonedImage, format, 0, filePath);

                                // 返回 UI 线程更新状态
                                Dispatcher.Invoke(() => UpdateMessage($"图像成功保存到: {filePath}"));
                            }
                            catch (HOperatorException ex)
                            {
                                Dispatcher.Invoke(() => UpdateMessage($"保存图像失败: {ex.Message}"));
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(() => UpdateMessage($"发生错误: {ex.Message}"));
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    UpdateMessage($"启动保存任务失败: {ex.Message}");
                }
            }
            else
            {
                UpdateMessage("保存操作已取消。");
            }
        }

        private void BtnMask_Click(object sender, RoutedEventArgs e) => DrawShape(ShapeType.Region);
        private void BtnRegion_Click(object sender, RoutedEventArgs e) => DrawShape(ShapeType.Region);
        private void BtnCircle_Click(object sender, RoutedEventArgs e) => DrawShape(ShapeType.Circle);
        private void BtnEllipse_Click(object sender, RoutedEventArgs e) => DrawShape(ShapeType.Ellipse);
        private void BtnRect_Click(object sender, RoutedEventArgs e) => DrawShape(ShapeType.Rectangle);

        private async void DrawShape(ShapeType shapeType)
        {
            if (SafeHWindow == null)
            {
                UpdateMessage("窗口未初始化，无法绘制");
                return;
            }

            UpdateMessage("按鼠标左键绘制，右键结束。");
            HObject drawObj = null;
            HTuple[] hTuples = null;

            HSmartWindowControlWPF.ZoomContent originalZoomMode = HSmartWindowControlWPF.ZoomContent.WheelForwardZoomsIn;

            try
            {
                if (hSmart != null)
                {
                    originalZoomMode = hSmart.HZoomContent;
                    hSmart.HZoomContent = HSmartWindowControlWPF.ZoomContent.Off; // 禁用缩放
                }

                // 绘图操作（在后台线程执行 Halcon API）
                await Task.Run(() =>
                {
                    try
                    {
                        switch (shapeType)
                        {
                            case ShapeType.Rectangle:
                                HOperatorSet.DrawRectangle1(SafeHWindow, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                                hTuples = new[] { row1, col1, row2, col2 };
                                HOperatorSet.GenRectangle1(out drawObj, row1, col1, row2, col2);
                                break;
                            case ShapeType.Ellipse:
                                HOperatorSet.DrawEllipse(SafeHWindow, out HTuple row, out HTuple col, out HTuple phi, out HTuple r1, out HTuple r2);
                                hTuples = new[] { row, col, phi, r1, r2 };
                                HOperatorSet.GenEllipse(out drawObj, row, col, phi, r1, r2);
                                break;
                            case ShapeType.Circle:
                                HOperatorSet.DrawCircle(SafeHWindow, out HTuple cRow, out HTuple cCol, out HTuple radius);
                                hTuples = new[] { cRow, cCol, radius };
                                HOperatorSet.GenCircle(out drawObj, cRow, cCol, radius);
                                break;
                            case ShapeType.Region:
                                HOperatorSet.DrawRegion(out drawObj, SafeHWindow);
                                break;
                        }
                    }
                    catch (HOperatorException ex)
                    {
                        Dispatcher.Invoke(() => UpdateMessage($"绘制失败: {ex.Message}"));
                        drawObj?.Dispose();
                    }
                });

                // 处理绘制结果 (在 await 之后，默认回到 UI 线程)
                if (drawObj != null && drawObj.IsInitialized())
                {
                    if (shapeType == ShapeType.Region)
                    {
                        MaskObject?.Dispose();

                        // ⭐ 修复：使用 Dispatcher 确保对依赖属性的访问安全
                        Dispatcher.Invoke(() =>
                        {
                            MaskObject = drawObj;
                        });

                        UpdateMessage("屏蔽区已创建。");
                    }
                    else
                    {
                        // DrawObjectList 是 ObservableCollection，必须在 UI 线程修改
                        Dispatcher.Invoke(() =>
                        {
                            DrawObjectList.Add(new DrawingObjectInfo
                            {
                                ShapeType = shapeType,
                                Hobject = drawObj,
                                HTuples = hTuples
                            });
                        });
                        UpdateMessage("绘制完成。");
                    }
                }
                else
                {
                    UpdateMessage(string.Empty);
                }
            }
            finally
            {
                // 恢复原始缩放模式 (必须在 UI 线程)
                if (hSmart != null)
                {
                    hSmart.HZoomContent = originalZoomMode;
                }
                UpdateMessage(string.Empty);
            }
        }

        // 安全更新消息（处理UI线程）
        private void UpdateMessage(string message)
        {
            if (txtMsg != null)
            {
                if (txtMsg.Dispatcher.CheckAccess())
                    txtMsg.Text = message;
                else
                    txtMsg.Dispatcher.Invoke(() => txtMsg.Text = message);
            }
        }

        #region ⭐ 中心十字




        #endregion
    }

}
#endif

namespace ReeYin_V.UI.Controls
{
    public class ImageEditView : VMHWindowControl
    {
        static ImageEditView()
        {
            System.Windows.FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ImageEditView),
                new System.Windows.FrameworkPropertyMetadata(typeof(ImageEditView)));
        }
    }
}
