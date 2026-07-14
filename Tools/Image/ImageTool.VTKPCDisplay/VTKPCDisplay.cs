using HalconDotNet;
using ImageTool.VTKPCDisplay.Model;
using Kitware.VTK;
using PointCloudSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageTool.VTKPCDisplay
{
    public partial class VTKPCDisplay : UserControl
    {
        #region Fields
        //创建VTK控件renderWindowControl1
        private Kitware.VTK.RenderWindowControl renderWindowControl1;

        //点云文件路径
        string url;

        //输入的点云对象
        PointCloudXYZ cloud = new PointCloudXYZ();

        #endregion

        #region Constructor
        public VTKPCDisplay()
        {
            InitializeComponent();

            //将VTK控件renderWindowControl添加到父控件panel里
            //必须使用panel控件充当容器装载vtk控件，直接在界面中拖曳vtk控件会报错
            renderWindowControl1 = new Kitware.VTK.RenderWindowControl();
            //将renderWindowControl1边框填充到其父组件里
            renderWindowControl1.Dock = DockStyle.Fill;
            //将指定的renderWindowControl1控件添加到panel1控件集合中
            panel_Container.Controls.Add(renderWindowControl1);

        }
        #endregion

        #region Methods
        /*按照指定的轴设定点云颜色属性
 极小值处设置为蓝色（0，0，255），并且蓝色通道的数值由255向中间位置按坐标变化递减至0，
 绿色通道的数值由0按坐标变化递增至255；中间位置设置为绿色（0，255，0），
 并且绿色通道的数值由255按坐标变化递减至0，红色通道数值由0按坐标变化递增至255；
 极大值位置设置为（255，0，0）*/
        vtkUnsignedCharArray setColorBaseAxis(char axis, PointCloudXYZ in_pc)
        {
            vtkUnsignedCharArray colors_rgb = vtkUnsignedCharArray.New();
            //点云的极值,第一第二个元素分别是x的最小最大值，yz依次类推
            double[] minmax = new double[6];
            in_pc.GetMinMaxXYZ(minmax);
            double z = minmax[5] - minmax[4];//z轴的差值
            double y = minmax[3] - minmax[2];//y轴的差值
            double x = minmax[1] - minmax[0];//x轴的差值
            double z_median = z / 2;
            double y_median = y / 2;
            double x_median = x / 2;
            colors_rgb.SetNumberOfComponents(3);//设置颜色的组分，因为是rgb，所以组分为3
            double r = 0, g = 0, b = 0;
            if (axis == 'x')
            {
                for (int i = 0; i < in_pc.Size; i++)
                {
                    //中间值为界，x值大于中间值的b组分为0，r组分逐渐变大
                    if ((in_pc.GetX(i) - minmax[0]) > x_median)
                    {
                        //x值要先归一化再乘以255，不然数值将会超出255

                        r = (255 * ((in_pc.GetX(i) - minmax[0] - x_median) / x_median)); ;
                        g = (255 * (1 - ((in_pc.GetX(i) - minmax[0] - x_median) / x_median)));
                        b = 0;
                        colors_rgb.InsertNextTuple3(r, g, b);
                    }
                    //中间值为界，x值小于中间值的r组分为0，b组分逐渐变大
                    else
                    {
                        //x值要先归一化再乘以255，不然数值将会超出255
                        r = 0;
                        g = (255 * ((in_pc.GetX(i) - minmax[0]) / x_median));
                        b = (255 * (1 - ((in_pc.GetX(i) - minmax[0]) / x_median))); ;
                        colors_rgb.InsertNextTuple3(r, g, b);
                    }
                }
            }
            else if (axis == 'y')
            {
                for (int i = 0; i < in_pc.Size; i++)
                {
                    //中间值为界，y值大于中间值的b组分为0，r组分逐渐变大
                    if ((in_pc.GetY(i) - minmax[2]) > y_median)
                    {
                        //y值要先归一化再乘以255，不然数值将会超出255
                        r = (255 * ((in_pc.GetY(i) - minmax[2] - y_median) / y_median)); ;
                        g = (255 * (1 - ((in_pc.GetY(i) - minmax[2] - y_median) / y_median)));
                        b = 0;
                        colors_rgb.InsertNextTuple3(r, g, b);
                    }
                    //中间值为界，y值小于中间值的r组分为0，b组分逐渐变大
                    else
                    {
                        r = 0;
                        g = (255 * ((in_pc.GetY(i) - minmax[2]) / y_median));
                        b = (255 * (1 - ((in_pc.GetY(i) - minmax[2]) / y_median))); ;
                        colors_rgb.InsertNextTuple3(r, g, b);
                    }
                }
            }
            else if (axis == 'z')
            {

                for (int i = 0; i < in_pc.Size; i++)
                {
                    //中间值为界，z值大于中间值的b组分为0，r组分逐渐变大
                    if ((in_pc.GetZ(i) - minmax[4]) > z_median)
                    {
                        //z值要先归一化再乘以255，不然数值将会超出255
                        r = (255 * ((in_pc.GetZ(i) - minmax[4] - z_median) / z_median)); ;
                        g = (255 * (1 - ((in_pc.GetZ(i) - minmax[4] - z_median) / z_median)));
                        b = 0;
                        colors_rgb.InsertNextTuple3(r, g, b);
                    }
                    //中间值为界，z值小于中间值的r组分为0，b组分逐渐变大
                    else
                    {
                        r = 0;
                        g = (255 * ((in_pc.GetZ(i) - minmax[4]) / z_median));
                        b = (255 * (1 - ((in_pc.GetZ(i) - minmax[4]) / z_median)));
                        colors_rgb.InsertNextTuple3(r, g, b);
                    }
                }
            }

            return colors_rgb;
        }

        //将点云对象可视化
        vtkRenderer showPointCloud(PointCloudXYZ in_pc)
        {
            //显示点云
            vtkPoints points = vtkPoints.New();
            //把点云指针中的点依次放进points
            for (int i = 0; i < cloud.Size; i++)
            {
                points.InsertNextPoint(cloud.GetX(i), cloud.GetY(i), cloud.GetZ(i));

            }

            //创建每个点的属性数据，这里代表颜色
            vtkUnsignedCharArray colors_rgb = setColorBaseAxis('z', cloud);
            vtkPolyData polydata = vtkPolyData.New();
            //将points数据传进polydata
            polydata.SetPoints(points);
            //将点数据的颜色属性传进polydata
            polydata.GetPointData().SetScalars(colors_rgb);

            vtkVertexGlyphFilter glyphFilter = vtkVertexGlyphFilter.New();
            glyphFilter.SetInputConnection(polydata.GetProducerPort());
            // 新建制图器
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            //设置颜色模式，这个是默认模式，不加也行
            //即把unsigned char类型的标量属性数据当作颜色值，不执行隐式。对于其他类型的标量数据，将通过查询表映射
            mapper.SetColorModeToDefault();

            mapper.SetScalarVisibility(1);
            mapper.SetInputConnection(glyphFilter.GetOutputPort());// 连接管道

            vtkActor actor = vtkActor.New(); // 新建角色
            actor.SetMapper(mapper); // 传递制图器

            //给图中添加颜色刻度表,刻度值和颜色表的还没对应
            vtkScalarBarActor scalarBar = vtkScalarBarActor.New();
            scalarBar.SetLookupTable(mapper.GetLookupTable());
            scalarBar.SetTitle("Point Cloud");
            scalarBar.SetHeight(0.7);
            scalarBar.SetWidth(0.1);
            scalarBar.SetNumberOfLabels(10);
            scalarBar.GetLabelTextProperty().SetFontSize(4);
            vtkRenderer out_render = vtkRenderer.New();

            out_render.AddActor(actor);
            //添加颜色刻度表
            out_render.AddActor(scalarBar);
            // 设置Viewport窗口
            out_render.SetViewport(0.0, 0.0, 1.0, 1.0);
            // 打开渐变色背景开关
            out_render.GradientBackgroundOn();
            out_render.SetBackground(0.2, 0.3, 0.3);
            out_render.SetBackground2(0.8, 0.8, 0.8);
            return out_render;
        }

        vtkRenderer ShowPointCloud(PointCloudXYZ in_pc)
        {
            int n = in_pc.Size;

            // 1) Points：预分配 + SetPoint（比 InsertNextPoint 快很多）
            vtkPoints points = vtkPoints.New();
            points.SetDataTypeToFloat();           // 可选：降低内存
            points.SetNumberOfPoints(n);

            // 2) 标量：用 Z 作为标量（用于 LUT 映射 + ScalarBar）
            vtkFloatArray scalars = vtkFloatArray.New();
            scalars.SetName("Z");
            scalars.SetNumberOfTuples(n);

            double zMin = double.MaxValue, zMax = double.MinValue;

            for (int i = 0; i < n; i++)
            {
                double x = in_pc.GetX(i);
                double y = in_pc.GetY(i);
                double z = in_pc.GetZ(i);

                points.SetPoint(i, x, y, z);
                scalars.SetValue(i, (float)z);

                if (z < zMin) zMin = z;
                if (z > zMax) zMax = z;
            }

            vtkPolyData polydata = vtkPolyData.New();
            polydata.SetPoints(points);
            polydata.GetPointData().SetScalars(scalars);

            vtkTrivialProducer tp = vtkTrivialProducer.New();
            tp.SetOutput(polydata);

            vtkVertexGlyphFilter glyphFilter = vtkVertexGlyphFilter.New();
            glyphFilter.SetInputConnection(tp.GetOutputPort());
            glyphFilter.Update();

            // 4) LUT：让 ScalarBar 与点颜色一致
            vtkLookupTable lut = vtkLookupTable.New();
            lut.SetNumberOfTableValues(256);
            lut.SetHueRange(0.667, 0.0); // 蓝->红（可按喜好调）
            lut.SetRange(zMin, zMax);
            lut.Build();

            // 5) Mapper/Actor
            vtkPolyDataMapper mapper = vtkPolyDataMapper.New();
            mapper.SetInputConnection(glyphFilter.GetOutputPort());
            mapper.SetLookupTable(lut);
            mapper.SetScalarRange(zMin, zMax);
            mapper.SetScalarVisibility(1);

            vtkActor actor = vtkActor.New();
            actor.SetMapper(mapper);
            actor.GetProperty().SetPointSize(2);

            // 6) ScalarBar
            vtkScalarBarActor scalarBar = vtkScalarBarActor.New();
            scalarBar.SetLookupTable(lut);
            scalarBar.SetTitle("Z");
            scalarBar.SetNumberOfLabels(10);

            // 7) Renderer
            vtkRenderer renderer = vtkRenderer.New();
            renderer.AddActor(actor);
            renderer.AddActor2D(scalarBar); // 标尺通常用 AddActor2D
            renderer.SetViewport(0.0, 0.0, 1.0, 1.0);
            renderer.GradientBackgroundOn();
            renderer.SetBackground(0.2, 0.3, 0.3);
            renderer.SetBackground2(0.8, 0.8, 0.8);

            renderer.ResetCamera();
            return renderer;
        }

        private CancellationTokenSource _cts;

        public async void LoadPointCloudFile(string type)
        {
            if (type != "Ply" && type != "Obj") return;

            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "请选择点云文件";
                ofd.InitialDirectory = @"\";
                ofd.Filter = "点云文件|*.*";
                if (ofd.ShowDialog() != DialogResult.OK) return;
                url = ofd.FileName;
            }

            // 取消上一次
            if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }
            _cts = new CancellationTokenSource();

            try
            {
                if (type == "Obj")
                {
                    cloud.Clear();
                    PclCSharp.Io.loadObjFile(url, cloud.PointCloudXYZPointer);
                    await RenderPointCloudAsync(cloud, _cts.Token);
                    return;
                }

                // PLY
                var previewCloud = new PointCloudXYZ();
                var fullCloud = new PointCloudXYZ();

                var progress = new Progress<int>(n => txtStatus.Text = "加载中: " + n);

                // 1) 预览数据加载（后台）
                await PlyToPointCloudXYZ.LoadPreviewIntoAsync(url, previewCloud, maxPoints: 50000, token: _cts.Token);
                txtStatus.Text = "预览点数: " + previewCloud.Size;
                await RenderPointCloudAsync(previewCloud, _cts.Token);

                // 2) 全量数据加载（后台）
                await PlyToPointCloudXYZ.LoadIntoAsync(url, fullCloud, progress, _cts.Token);
                txtStatus.Text = "全量点数: " + fullCloud.Size;
                await RenderPointCloudAsync(fullCloud, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                txtStatus.Text = "已取消";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "加载失败: " + ex.Message;
            }
        }

        public async void LoadPointCloud(IList<double[]> points)
        {
            if (points == null || points.Count == 0)
            {
                txtStatus.Text = "无点云数据";
                return;
            }

            if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }
            _cts = new CancellationTokenSource();

            try
            {
                txtStatus.Text = "加载中...";

                var localPoints = points;
                var previewCloud = new PointCloudXYZ();
                var fullCloud = new PointCloudXYZ();

                await Task.Run(() =>
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    FillPointCloudFromList(localPoints, previewCloud, previewMaxPoints: 50000, _cts.Token);
                }, _cts.Token);
                await RenderPointCloudAsync(previewCloud, _cts.Token);

                await Task.Run(() =>
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    FillPointCloudFromList(localPoints, fullCloud, previewMaxPoints: 0, _cts.Token);
                }, _cts.Token);

                cloud = fullCloud;
                txtStatus.Text = "点数: " + points.Count;
                await RenderPointCloudAsync(cloud, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                txtStatus.Text = "已取消";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "加载失败: " + ex.Message;
            }
        }

        public async void LoadPointCloud(PointCloudXYZ pc)
        {
            if (pc == null || pc.Size == 0)
            {
                txtStatus.Text = "无点云数据";
                return;
            }

            if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }
            _cts = new CancellationTokenSource();

            try
            {
                txtStatus.Text = "加载中...";

                var previewCloud = new PointCloudXYZ();
                var localPc = pc;

                await Task.Run(() =>
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    FillPointCloudFromPointCloud(localPc, previewCloud, previewMaxPoints: 50000, _cts.Token);
                }, _cts.Token);
                await RenderPointCloudAsync(previewCloud, _cts.Token);

                cloud = pc;
                txtStatus.Text = "点数: " + cloud.Size;
                await RenderPointCloudAsync(cloud, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                txtStatus.Text = "已取消";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "加载失败: " + ex.Message;
            }
        }

        private static void FillPointCloudFromList(IList<double[]> points, PointCloudXYZ target, int previewMaxPoints, CancellationToken token)
        {
            int total = points.Count;
            int step = 1;
            if (previewMaxPoints > 0)
                step = Math.Max(1, (int)Math.Ceiling(total / (double)previewMaxPoints));

            int count = (total + step - 1) / step;

            target.Clear();
            target.ReSize(count);

            int idx = 0;
            for (int i = 0; i < total; i += step)
            {
                var p = points[i];
                if (p == null || p.Length < 3)
                    throw new ArgumentException("points 中存在长度不足 3 的点");

                target.SetX(idx, p[0]);
                target.SetY(idx, p[1]);
                target.SetZ(idx, p[2]);

                idx++;
                if ((i & 0x3FFF) == 0)
                    token.ThrowIfCancellationRequested();
            }
        }

        private static void FillPointCloudFromPointCloud(PointCloudXYZ source, PointCloudXYZ target, int previewMaxPoints, CancellationToken token)
        {
            int total = source.Size;
            int step = 1;
            if (previewMaxPoints > 0)
                step = Math.Max(1, (int)Math.Ceiling(total / (double)previewMaxPoints));

            int count = (total + step - 1) / step;

            target.Clear();
            target.ReSize(count);

            int idx = 0;
            for (int i = 0; i < total; i += step)
            {
                target.SetX(idx, source.GetX(i));
                target.SetY(idx, source.GetY(i));
                target.SetZ(idx, source.GetZ(i));

                idx++;
                if ((i & 0x3FFF) == 0)
                    token.ThrowIfCancellationRequested();
            }
        }

        private Task RenderPointCloudAsync(PointCloudXYZ pc, CancellationToken token)
        {
            return Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var renderer = ShowPointCloud(pc);   // 重活：后台做

                token.ThrowIfCancellationRequested();
                this.BeginInvoke(new Action(() => ApplyRenderer(renderer))); // UI线程只做“挂载+Render”
            }, token);
        }

        private void ApplyRenderer(vtkRenderer renderer)
        {
            var renWin = renderWindowControl1.RenderWindow;

            var renderers = renWin.GetRenderers();
            if (renderers != null) renderers.RemoveAllItems();

            renWin.AddRenderer(renderer);
            renderer.ResetCamera();
            renWin.Render();          // 比 panel.Refresh 更直接、更可靠
        }


        #endregion

        #region Commands

        #endregion
    }
}
