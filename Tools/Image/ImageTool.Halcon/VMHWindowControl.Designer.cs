namespace ImageTool.Halcon
{
    partial class VMHWindowControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mCtrl_HWindow = new HalconDotNet.HWindowControl();
            this.m_CtrlImageList = new System.Windows.Forms.ImageList(this.components);
            this.m_CtrlHStatusLabelCtrl = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // mCtrl_HWindow
            // 
            this.mCtrl_HWindow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.mCtrl_HWindow.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.mCtrl_HWindow.ImagePart = new System.Drawing.Rectangle(0, 0, 640, 480);
            this.mCtrl_HWindow.Location = new System.Drawing.Point(0, 0);
            this.mCtrl_HWindow.Name = "mCtrl_HWindow";
            this.mCtrl_HWindow.Size = new System.Drawing.Size(446, 353);
            this.mCtrl_HWindow.TabIndex = 0;
            this.mCtrl_HWindow.WindowSize = new System.Drawing.Size(446, 353);
            // 
            // m_CtrlImageList
            // 
            this.m_CtrlImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.m_CtrlImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.m_CtrlImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // m_CtrlHStatusLabelCtrl
            // 
            this.m_CtrlHStatusLabelCtrl.AutoSize = true;
            this.m_CtrlHStatusLabelCtrl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.m_CtrlHStatusLabelCtrl.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.m_CtrlHStatusLabelCtrl.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.m_CtrlHStatusLabelCtrl.ForeColor = System.Drawing.Color.White;
            this.m_CtrlHStatusLabelCtrl.Location = new System.Drawing.Point(0, 341);
            this.m_CtrlHStatusLabelCtrl.Name = "m_CtrlHStatusLabelCtrl";
            this.m_CtrlHStatusLabelCtrl.Size = new System.Drawing.Size(31, 12);
            this.m_CtrlHStatusLabelCtrl.TabIndex = 1;
            this.m_CtrlHStatusLabelCtrl.Text = "显示";
            // 
            // VMHWindowControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.m_CtrlHStatusLabelCtrl);
            this.Controls.Add(this.mCtrl_HWindow);
            this.Name = "VMHWindowControl";
            this.Size = new System.Drawing.Size(446, 353);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private HalconDotNet.HWindowControl mCtrl_HWindow;
        private System.Windows.Forms.ImageList m_CtrlImageList;
        private System.Windows.Forms.Label m_CtrlHStatusLabelCtrl;
    }
}
