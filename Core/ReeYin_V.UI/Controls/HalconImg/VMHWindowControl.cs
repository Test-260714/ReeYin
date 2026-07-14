using HalconDotNet;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReeYin_V.UI.Controls
{
    public class VMHWindowControl : Control
    {
        private const string DefaultColor = "red";
        private readonly List<HalconDisplayObject> overlays = new();
        private HSmartWindowControlWPF smart;
        private HWindow window;
        private TextBlock messageBlock;
        private TextBlock statusBlock;
        private Border statusBar;
        private ContextMenu menu;
        private MenuItem fitWindowItem;
        private MenuItem fitImageItem;
        private MenuItem statusItem;
        private MenuItem crossItem;
        private MenuItem saveImageItem;
        private MenuItem saveWindowItem;
        private MenuItem clearItem;
        private HImage currentImage;
        private int imageWidth;
        private int imageHeight;
        private string imageSizeText = string.Empty;
        private bool suppressRefresh;
        private ViewMode viewMode = ViewMode.FitImage;

        private enum ViewMode
        {
            FitImage,
            FitWindow
        }

        static VMHWindowControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VMHWindowControl), new FrameworkPropertyMetadata(typeof(VMHWindowControl)));
        }

        public VMHWindowControl()
        {
            SetCurrentValue(BackgroundProperty, Brushes.Black);
            DrawObjectList = new ObservableCollection<DrawingObjectInfo>();
            DrawObjectList.CollectionChanged += DrawObjectList_CollectionChanged;
        }

        public HObject Image
        {
            get => (HObject)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
            nameof(Image),
            typeof(HObject),
            typeof(VMHWindowControl),
            new PropertyMetadata(null, (d, e) => ((VMHWindowControl)d).HandleImageChanged(e.NewValue as HObject)));

        public HWindow HWindow
        {
            get => (HWindow)GetValue(HWindowProperty);
            private set => SetValue(HWindowProperty, value);
        }

        public static readonly DependencyProperty HWindowProperty = DependencyProperty.Register(
            nameof(HWindow),
            typeof(HWindow),
            typeof(VMHWindowControl),
            new PropertyMetadata(null));

        public HObject MaskObject
        {
            get => (HObject)GetValue(MaskObjectProperty);
            set => SetValue(MaskObjectProperty, value);
        }

        public static readonly DependencyProperty MaskObjectProperty = DependencyProperty.Register(
            nameof(MaskObject),
            typeof(HObject),
            typeof(VMHWindowControl),
            new PropertyMetadata(null, (d, e) => ((VMHWindowControl)d).OnVisualStateChanged()));

        public ObservableCollection<DrawingObjectInfo> DrawObjectList
        {
            get => (ObservableCollection<DrawingObjectInfo>)GetValue(DrawObjectListProperty);
            set => SetValue(DrawObjectListProperty, value);
        }

        public static readonly DependencyProperty DrawObjectListProperty = DependencyProperty.Register(
            nameof(DrawObjectList),
            typeof(ObservableCollection<DrawingObjectInfo>),
            typeof(VMHWindowControl),
            new PropertyMetadata(null));

        public bool IsStatusBarVisible
        {
            get => (bool)GetValue(IsStatusBarVisibleProperty);
            set => SetValue(IsStatusBarVisibleProperty, value);
        }

        public static readonly DependencyProperty IsStatusBarVisibleProperty = DependencyProperty.Register(
            nameof(IsStatusBarVisible),
            typeof(bool),
            typeof(VMHWindowControl),
            new PropertyMetadata(false, (d, e) => ((VMHWindowControl)d).OnVisualStateChanged()));

        public bool IsCrossVisible
        {
            get => (bool)GetValue(IsCrossVisibleProperty);
            set => SetValue(IsCrossVisibleProperty, value);
        }

        public static readonly DependencyProperty IsCrossVisibleProperty = DependencyProperty.Register(
            nameof(IsCrossVisible),
            typeof(bool),
            typeof(VMHWindowControl),
            new PropertyMetadata(false, (d, e) => ((VMHWindowControl)d).OnVisualStateChanged()));

        public bool DrawModel
        {
            get => (bool)GetValue(DrawModelProperty);
            set => SetValue(DrawModelProperty, value);
        }

        public static readonly DependencyProperty DrawModelProperty = DependencyProperty.Register(
            nameof(DrawModel),
            typeof(bool),
            typeof(VMHWindowControl),
            new PropertyMetadata(false, (d, e) => ((VMHWindowControl)d).UpdateInteractionMode()));

        public bool IsShapeDrawingEnabled
        {
            get => (bool)GetValue(IsShapeDrawingEnabledProperty);
            set => SetValue(IsShapeDrawingEnabledProperty, value);
        }

        public static readonly DependencyProperty IsShapeDrawingEnabledProperty = DependencyProperty.Register(
            nameof(IsShapeDrawingEnabled),
            typeof(bool),
            typeof(VMHWindowControl),
            new PropertyMetadata(true, (d, e) => ((VMHWindowControl)d).RebuildMenu()));

        public IntPtr HWindowHalconID => smart?.HalconID ?? IntPtr.Zero;

        public HSmartWindowControlWPF getHWindowControl() => smart;

        public override void OnApplyTemplate()
        {
            UnhookEvents();
            base.OnApplyTemplate();
            messageBlock = GetTemplateChild("PART_MSG") as TextBlock;
            statusBlock = GetTemplateChild("PART_STATUS") as TextBlock;
            statusBar = GetTemplateChild("PART_STATUS_BAR") as Border;
            smart = GetTemplateChild("PART_SMART") as HSmartWindowControlWPF;
            RebuildMenu();
            HookEvents();
            InitializeWindow();
            UpdateStatusVisibility();
            UpdateMenuState();
            RefreshDisplay(true);
        }

        public void HobjectToHimage(HObject hobject) => RunOnUi(() => Image = hobject);
        public void DispImageFitImage() => RunOnUi(() => { viewMode = ViewMode.FitImage; RefreshDisplay(true); });
        public void DispImageFitWindow() => RunOnUi(() => { viewMode = ViewMode.FitWindow; RefreshDisplay(true); });
        public void showStatusBar() => IsStatusBarVisible = true;
        public void DispObj(HObject hObj) => DispObj(hObj, DefaultColor, false);
        public void DispObj(HObject hObj, bool isFillDis) => DispObj(hObj, DefaultColor, isFillDis);
        public void DispObj(HObject hObj, string color) => DispObj(hObj, color, false);

        public void DispObj(HObject hObj, string color, bool isFillDisp)
        {
            if (!IsValid(hObj))
            {
                return;
            }

            RunOnUi(() =>
            {
                overlays.Add(new HalconDisplayObject(hObj.Clone(), color, isFillDisp));
                RefreshDisplay(false);
            });
        }

        public void ClearROI()
        {
            RunOnUi(() =>
            {
                ClearAnnotations();
                RefreshDisplay(false);
            });
        }

        public void ClearWindow()
        {
            RunOnUi(() =>
            {
                suppressRefresh = true;
                ClearAnnotations();
                DisposeCurrentImage();
                suppressRefresh = false;
                imageWidth = 0;
                imageHeight = 0;
                imageSizeText = string.Empty;
                window?.ClearWindow();
                UpdateStatusText(null, null);
                UpdateStatusVisibility();
                UpdateMenuState();
            });
        }

        public void OpenImage()
        {
            RunOnUi(() =>
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "所有图像文件|*.bmp;*.pcx;*.png;*.jpg;*.jpeg;*.gif;*.tif;*.tiff;*.ico;*.dxf;*.cgm;*.cdr;*.wmf;*.eps;*.emf"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                using HImage image = new HImage();
                image.ReadImage(dialog.FileName);
                Image = image.Clone();
                SetMessage($"Opened image: {Path.GetFileName(dialog.FileName)}");
            });
        }

        private void HandleImageChanged(HObject newImage)
        {
            RunOnUi(() =>
            {
                DisposeCurrentImage();
                ClearAnnotations();
                imageWidth = 0;
                imageHeight = 0;
                imageSizeText = string.Empty;
                if (!IsValid(newImage))
                {
                    RefreshDisplay(true);
                    return;
                }

                try
                {
                    HOperatorSet.GetObjClass(newImage, out HTuple objClass);
                    if (objClass.Length == 0 || objClass.S != "image")
                    {
                        SetMessage("The input object is not an image.");
                        return;
                    }

                    currentImage = new HImage(newImage);
                    currentImage.GetImageSize(out imageWidth, out imageHeight);
                    imageSizeText = $"W:{imageWidth},H:{imageHeight}";
                    viewMode = ViewMode.FitImage;
                    SetMessage(string.Empty);
                    RefreshDisplay(true);
                }
                catch (HOperatorException ex)
                {
                    SetMessage($"Failed to load image: {ex.Message}");
                }
            });
        }

        private void DrawObjectList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!suppressRefresh)
            {
                RefreshDisplay(false);
            }
        }

        private void HookEvents()
        {
            if (smart == null)
            {
                return;
            }

            smart.Loaded += Smart_Loaded;
            smart.HInitWindow += Smart_HInitWindow;
            smart.HMouseMove += Smart_HMouseMove;
            smart.HMouseWheel += Smart_HMouseWheel;
            smart.HMouseUp += Smart_HMouseUp;
            smart.SizeChanged += Smart_SizeChanged;
        }

        private void UnhookEvents()
        {
            if (smart == null)
            {
                return;
            }

            smart.Loaded -= Smart_Loaded;
            smart.HInitWindow -= Smart_HInitWindow;
            smart.HMouseMove -= Smart_HMouseMove;
            smart.HMouseWheel -= Smart_HMouseWheel;
            smart.HMouseUp -= Smart_HMouseUp;
            smart.SizeChanged -= Smart_SizeChanged;
        }

        private void Smart_Loaded(object sender, RoutedEventArgs e) => InitializeWindow();
        private void Smart_HInitWindow(object sender, EventArgs e) => InitializeWindow();
        private void Smart_HMouseMove(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e) => UpdateStatusText(e.Row, e.Column);
        private void Smart_HMouseWheel(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e) { if (!DrawModel) Dispatcher.BeginInvoke(new Action(() => RefreshDisplay(false))); }
        private void Smart_HMouseUp(object sender, HSmartWindowControlWPF.HMouseEventArgsWPF e) { if (!DrawModel) Dispatcher.BeginInvoke(new Action(() => RefreshDisplay(false))); }
        private void Smart_SizeChanged(object sender, SizeChangedEventArgs e) { if (HasImage()) DispImageFitImage(); }

        private void InitializeWindow()
        {
            if (smart?.HalconWindow == null)
            {
                return;
            }

            window = smart.HalconWindow;
            HWindow = window;
            smart.HDoubleClickToFitContent = false;
            smart.HZoomContent = HSmartWindowControlWPF.ZoomContent.WheelForwardZoomsIn;
            smart.HMoveContent = true;
            smart.ContextMenu = DrawModel ? null : menu;
            UpdateInteractionMode();
        }

        private void BuildMenu()
        {
            if (menu != null)
            {
                return;
            }

            fitWindowItem = CreateMenuItem("适应窗口", (_, __) => DispImageFitWindow());
            fitImageItem = CreateMenuItem("适应图片", (_, __) => DispImageFitImage());
            statusItem = new MenuItem { Header = "显示/隐藏图像信息", IsCheckable = true };
            statusItem.Checked += (_, __) => IsStatusBarVisible = true;
            statusItem.Unchecked += (_, __) => IsStatusBarVisible = false;
            crossItem = new MenuItem { Header = "显示/隐藏十字", IsCheckable = true };
            crossItem.Checked += (_, __) => IsCrossVisible = true;
            crossItem.Unchecked += (_, __) => IsCrossVisible = false;
            saveImageItem = CreateMenuItem("保存原始图像", (_, __) => SaveImage());
            saveWindowItem = CreateMenuItem("保存窗口截图", (_, __) => SaveWindowDump());
            clearItem = CreateMenuItem("清空所有标注", (_, __) => ClearROI());
            MenuItem openItem = CreateMenuItem("打开图片", (_, __) => OpenImage());
            MenuItem drawMenu = new MenuItem { Header = "绘制" };
            drawMenu.Items.Add(CreateMenuItem("绘制矩形", (_, __) => DrawShape(ShapeType.Rectangle)));
            drawMenu.Items.Add(CreateMenuItem("绘制椭圆", (_, __) => DrawShape(ShapeType.Ellipse)));
            drawMenu.Items.Add(CreateMenuItem("绘制圆形", (_, __) => DrawShape(ShapeType.Circle)));
            drawMenu.Items.Add(CreateMenuItem("绘制区域", (_, __) => DrawShape(ShapeType.Region)));
            drawMenu.Items.Add(CreateMenuItem("创建屏蔽区", (_, __) => DrawShape(ShapeType.Mask)));

            menu = new ContextMenu();
            menu.Items.Add(fitWindowItem);
            menu.Items.Add(fitImageItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(crossItem);
            menu.Items.Add(statusItem);
            if (IsShapeDrawingEnabled)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(drawMenu);
            }
            menu.Items.Add(clearItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(saveImageItem);
            menu.Items.Add(saveWindowItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(openItem);
        }

        private void RebuildMenu()
        {
            menu = null;
            BuildMenu();
            UpdateInteractionMode();
            UpdateMenuState();
        }

        private static MenuItem CreateMenuItem(string header, RoutedEventHandler handler)
        {
            MenuItem item = new MenuItem { Header = header };
            item.Click += handler;
            return item;
        }

        private void UpdateInteractionMode()
        {
            if (smart == null)
            {
                return;
            }

            smart.HMoveContent = !DrawModel;
            smart.HZoomContent = DrawModel ? HSmartWindowControlWPF.ZoomContent.Off : HSmartWindowControlWPF.ZoomContent.WheelForwardZoomsIn;
            smart.ContextMenu = DrawModel ? null : menu;
        }

        private void OnVisualStateChanged()
        {
            if (suppressRefresh)
            {
                return;
            }

            UpdateStatusVisibility();
            UpdateMenuState();
            RefreshDisplay(false);
        }

        private void RefreshDisplay(bool resetPart)
        {
            RunOnUi(() =>
            {
                if (window == null)
                {
                    return;
                }

                if (!HasImage())
                {
                    window.ClearWindow();
                    UpdateStatusText(null, null);
                    UpdateStatusVisibility();
                    UpdateMenuState();
                    return;
                }

                WindowPart part = resetPart ? null : CapturePart();
                window.ClearWindow();
                ApplyPart(resetPart, part);
                window.DispObj(currentImage);

                foreach (HalconDisplayObject overlay in overlays.ToList())
                {
                    DrawObject(overlay.HObject, overlay.Color, overlay.IsFillDisplay);
                }

                if (DrawObjectList != null)
                {
                    foreach (DrawingObjectInfo drawObject in DrawObjectList.ToList())
                    {
                        if (IsValid(drawObject?.Hobject))
                        {
                            DrawObject(
                                drawObject.Hobject,
                                string.IsNullOrWhiteSpace(drawObject.Color)
                                    ? DefaultColor
                                    : drawObject.Color,
                                drawObject.IsFillDisplay
                            );
                        }
                    }
                }

                if (IsValid(MaskObject))
                {
                    DrawObject(MaskObject, "#00ff00", true);
                }

                if (IsCrossVisible)
                {
                    DrawCross();
                }

                UpdateStatusVisibility();
                UpdateMenuState();
            });
        }

        private WindowPart CapturePart()
        {
            try
            {
                window.GetPart(out int row1, out int col1, out int row2, out int col2);
                return new WindowPart(row1, col1, row2, col2);
            }
            catch
            {
                return null;
            }
        }

        private void ApplyPart(bool resetPart, WindowPart part)
        {
            if (!resetPart && part != null)
            {
                window.SetPart(part.Row1, part.Col1, part.Row2, part.Col2);
                return;
            }

            if (viewMode == ViewMode.FitWindow)
            {
                window.SetPart(0, 0, imageHeight - 1, imageWidth - 1);
                return;
            }

            double winWidth = Math.Max(1d, smart?.ActualWidth ?? imageWidth);
            double winHeight = Math.Max(1d, smart?.ActualHeight ?? imageHeight);
            double ratioWin = winWidth / winHeight;
            double ratioImg = imageWidth / (double)imageHeight;
            double row1;
            double col1;
            double row2;
            double col2;

            if (ratioWin >= ratioImg)
            {
                row1 = 0;
                row2 = imageHeight - 1;
                col1 = -imageWidth * (ratioWin / ratioImg - 1d) / 2d;
                col2 = imageWidth - 1 + imageWidth * (ratioWin / ratioImg - 1d) / 2d;
            }
            else
            {
                col1 = 0;
                col2 = imageWidth - 1;
                row1 = -imageHeight * (ratioImg / ratioWin - 1d) / 2d;
                row2 = imageHeight - 1 + imageHeight * (ratioImg / ratioWin - 1d) / 2d;
            }

            window.SetPart(row1, col1, row2, col2);
        }

        private void DrawObject(HObject obj, string color, bool fill)
        {
            try
            {
                window.SetColor(string.IsNullOrWhiteSpace(color) ? DefaultColor : color);
                window.SetDraw(fill ? "fill" : "margin");
                window.DispObj(obj);
            }
            catch
            {
            }
            finally
            {
                window.SetDraw("margin");
                window.SetColor(DefaultColor);
            }
        }

        private void DrawCross()
        {
            double row = imageHeight / 2.0;
            double col = imageWidth / 2.0;
            window.SetColor("green");
            window.DispLine(row - 5, col, row + 5, col);
            window.DispLine(row, col - 5, row, col + 5);
            if (col > 50)
            {
                window.DispLine(row, 0, row, col - 50);
            }
            if (col + 50 < imageWidth - 1)
            {
                window.DispLine(row, col + 50, row, imageWidth - 1);
            }
            if (row > 50)
            {
                window.DispLine(0, col, row - 50, col);
            }
            if (row + 50 < imageHeight - 1)
            {
                window.DispLine(row + 50, col, imageHeight - 1, col);
            }
            window.SetColor(DefaultColor);
        }

        private void UpdateStatusVisibility()
        {
            if (statusBar != null)
            {
                statusBar.Visibility = IsStatusBarVisible && HasImage()
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void UpdateStatusText(double? row, double? column)
        {
            if (statusBlock == null)
            {
                return;
            }

            if (!HasImage() || !IsStatusBarVisible)
            {
                statusBlock.Text = string.Empty;
                return;
            }

            string text = imageSizeText;
            if (row.HasValue && column.HasValue)
            {
                text += $"    X:{column.Value:0000.0}, Y:{row.Value:0000.0}";
                int rowIndex = (int)Math.Floor(row.Value);
                int colIndex = (int)Math.Floor(column.Value);
                if (
                    rowIndex >= 0
                    && rowIndex < imageHeight
                    && colIndex >= 0
                    && colIndex < imageWidth
                )
                {
                    text += $"    {ReadPixelInfo(rowIndex, colIndex)}";
                }
            }

            statusBlock.Text = text;
        }

        private string ReadPixelInfo(int row, int column)
        {
            try
            {
                int channelCount = currentImage.CountChannels();
                if (channelCount == 1)
                {
                    return $"Gray:{currentImage.GetGrayval(row, column):000.0}";
                }

                if (channelCount >= 3)
                {
                    using HImage red = currentImage.AccessChannel(1);
                    using HImage green = currentImage.AccessChannel(2);
                    using HImage blue = currentImage.AccessChannel(3);
                    return string.Format(
                        "RGB:({0:000.0},{1:000.0},{2:000.0})",
                        red.GetGrayval(row, column),
                        green.GetGrayval(row, column),
                        blue.GetGrayval(row, column)
                    );
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private void UpdateMenuState()
        {
            bool hasImage = HasImage();
            bool hasOverlay =
                overlays.Count > 0 || (DrawObjectList?.Count ?? 0) > 0 || IsValid(MaskObject);
            if (fitWindowItem != null)
                fitWindowItem.IsEnabled = hasImage;
            if (fitImageItem != null)
                fitImageItem.IsEnabled = hasImage;
            if (statusItem != null)
            {
                statusItem.IsEnabled = hasImage;
                statusItem.IsChecked = IsStatusBarVisible;
            }
            if (crossItem != null)
            {
                crossItem.IsEnabled = hasImage;
                crossItem.IsChecked = IsCrossVisible;
            }
            if (saveImageItem != null)
                saveImageItem.IsEnabled = hasImage;
            if (saveWindowItem != null)
                saveWindowItem.IsEnabled = hasImage;
            if (clearItem != null)
                clearItem.IsEnabled = hasOverlay;
        }

        private void SaveImage()
        {
            if (!HasImage())
            {
                SetMessage("No image available to save.");
                return;
            }

            SaveFileDialog dialog = CreateSaveDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            HOperatorSet.WriteImage(currentImage, GetFormat(dialog.FileName), 0, dialog.FileName);
            SetMessage($"Image saved to: {dialog.FileName}");
        }

        private void SaveWindowDump()
        {
            if (window == null || !HasImage())
            {
                SetMessage("There is no window content available to save.");
                return;
            }

            SaveFileDialog dialog = CreateSaveDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            window.DumpWindow(GetFormat(dialog.FileName), dialog.FileName);
            SetMessage($"Window snapshot saved to: {dialog.FileName}");
        }

        private static SaveFileDialog CreateSaveDialog()
        {
            return new SaveFileDialog
            {
                Filter = "PNG|*.png|BMP|*.bmp|JPG|*.jpg;*.jpeg|TIFF|*.tif;*.tiff",
                FilterIndex = 1,
                DefaultExt = ".png"
            };
        }

        private static string GetFormat(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".bmp" => "bmp",
                ".jpg" => "jpeg",
                ".jpeg" => "jpeg",
                ".tif" => "tiff",
                ".tiff" => "tiff",
                _ => "png"
            };
        }

        private void DrawShape(ShapeType shapeType)
        {
            if (window == null || !HasImage())
            {
                SetMessage("Please load an image before drawing.");
                return;
            }

            HObject drawObject = null;
            HTuple[] parameters = null;
            bool previousDrawMode = DrawModel;

            try
            {
                DrawModel = true;
                SetMessage("Draw with the left mouse button and finish with the right mouse button.");

                switch (shapeType)
                {
                    case ShapeType.Rectangle:
                        HOperatorSet.DrawRectangle1(
                            window,
                            out HTuple row1,
                            out HTuple col1,
                            out HTuple row2,
                            out HTuple col2
                        );
                        parameters = new[] { row1, col1, row2, col2 };
                        HOperatorSet.GenRectangle1(out drawObject, row1, col1, row2, col2);
                        break;
                    case ShapeType.Ellipse:
                        HOperatorSet.DrawEllipse(
                            window,
                            out HTuple row,
                            out HTuple column,
                            out HTuple phi,
                            out HTuple radius1,
                            out HTuple radius2
                        );
                        parameters = new[] { row, column, phi, radius1, radius2 };
                        HOperatorSet.GenEllipse(
                            out drawObject,
                            row,
                            column,
                            phi,
                            radius1,
                            radius2
                        );
                        break;
                    case ShapeType.Circle:
                        HOperatorSet.DrawCircle(
                            window,
                            out HTuple centerRow,
                            out HTuple centerCol,
                            out HTuple radius
                        );
                        parameters = new[] { centerRow, centerCol, radius };
                        HOperatorSet.GenCircle(out drawObject, centerRow, centerCol, radius);
                        break;
                    case ShapeType.Region:
                    case ShapeType.Mask:
                        HOperatorSet.DrawRegion(out drawObject, window);
                        break;
                }

                if (!IsValid(drawObject))
                {
                    return;
                }

                if (shapeType == ShapeType.Mask)
                {
                    suppressRefresh = true;
                    MaskObject?.Dispose();
                    MaskObject = drawObject;
                    suppressRefresh = false;
                    SetMessage("Mask region created.");
                }
                else
                {
                    DrawObjectList.Add(
                        new DrawingObjectInfo
                        {
                            ShapeType = shapeType,
                            Hobject = drawObject,
                            HTuples = parameters,
                            Color = "yellow",
                            IsFillDisplay = false
                        }
                    );
                    SetMessage("Drawing completed.");
                }
            }
            catch (HOperatorException ex)
            {
                drawObject?.Dispose();
                SetMessage($"Drawing failed: {ex.Message}");
            }
            finally
            {
                DrawModel = previousDrawMode;
                RefreshDisplay(false);
            }
        }

        private void ClearAnnotations()
        {
            bool previous = suppressRefresh;
            suppressRefresh = true;

            foreach (HalconDisplayObject overlay in overlays)
            {
                overlay.Dispose();
            }

            overlays.Clear();

            if (DrawObjectList != null)
            {
                foreach (DrawingObjectInfo item in DrawObjectList.ToList())
                {
                    try
                    {
                        item.Hobject?.Dispose();
                    }
                    catch
                    {
                    }
                }

                DrawObjectList.Clear();
            }

            if (MaskObject != null)
            {
                try
                {
                    MaskObject.Dispose();
                }
                catch
                {
                }

                MaskObject = null;
            }

            suppressRefresh = previous;
        }

        private void DisposeCurrentImage()
        {
            try
            {
                currentImage?.Dispose();
            }
            catch
            {
            }

            currentImage = null;
        }

        private bool HasImage() => IsValid(currentImage);

        private static bool IsValid(HObject obj)
        {
            try
            {
                return obj != null && obj.IsInitialized();
            }
            catch
            {
                return false;
            }
        }

        private void RunOnUi(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.Invoke(action);
            }
        }

        private void SetMessage(string message)
        {
            if (messageBlock == null)
            {
                return;
            }

            string englishMessage = TranslateMessageToEnglish(message);

            if (messageBlock.Dispatcher.CheckAccess())
            {
                messageBlock.Text = englishMessage;
            }
            else
            {
                messageBlock.Dispatcher.Invoke(() => messageBlock.Text = englishMessage);
            }
        }

        private static string TranslateMessageToEnglish(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            if (TryTranslatePrefix(message, "已打开图像: ", "Opened image: ", out string translated))
                return translated;
            if (TryTranslatePrefix(message, "宸叉墦寮€鍥惧儚: ", "Opened image: ", out translated))
                return translated;
            if (TryTranslatePrefix(message, "图像加载失败: ", "Failed to load image: ", out translated))
                return translated;
            if (TryTranslatePrefix(message, "鍥惧儚鍔犺浇澶辫触: ", "Failed to load image: ", out translated))
                return translated;
            if (TryTranslatePrefix(message, "图像已保存到: ", "Image saved to: ", out translated))
                return translated;
            if (TryTranslatePrefix(message, "窗口截图已保存到: ", "Window snapshot saved to: ", out translated))
                return translated;
            if (TryTranslatePrefix(message, "绘制失败: ", "Drawing failed: ", out translated))
                return translated;

            if (message.Contains("传入对象不是图像", StringComparison.Ordinal)
                || message.Contains("浼犲叆瀵硅薄", StringComparison.Ordinal))
                return "The input object is not an image.";
            if (message == "没有图像可保存。")
                return "No image available to save.";
            if (message == "当前没有可保存的窗口内容。")
                return "There is no window content available to save.";
            if (message == "请先加载图像后再绘制。")
                return "Please load an image before drawing.";
            if (message == "按左键绘制，右键结束。")
                return "Draw with the left mouse button and finish with the right mouse button.";
            if (message == "屏蔽区已创建。")
                return "Mask region created.";
            if (message == "绘制完成。")
                return "Drawing completed.";

            return message;
        }

        private static bool TryTranslatePrefix(
            string message,
            string sourcePrefix,
            string targetPrefix,
            out string translated)
        {
            if (message.StartsWith(sourcePrefix, StringComparison.Ordinal))
            {
                translated = targetPrefix + message[sourcePrefix.Length..];
                return true;
            }

            translated = string.Empty;
            return false;
        }

        private sealed class WindowPart
        {
            public WindowPart(int row1, int col1, int row2, int col2)
            {
                Row1 = row1;
                Col1 = col1;
                Row2 = row2;
                Col2 = col2;
            }

            public int Row1 { get; }
            public int Col1 { get; }
            public int Row2 { get; }
            public int Col2 { get; }
        }
    }
}
