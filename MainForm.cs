namespace ImageToGifConverter
{
    public class MainForm : Form
    {
        private SequencePanel sequencePanel = null!;
        private AtlasPanel atlasPanel = null!;
        private GifPanel gifPanel = null!;
        
        private TextBox txtRows = null!;
        private TextBox txtColumns = null!;
        private TextBox txtFrameRate = null!;
        
        // UI控件
        private Button btnClearAll = null!;

        public MainForm()
        {
            InitializeComponent();
            SetupPanels();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            
            Text = "图片转GIF工具";
            Size = new Size(1280, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.DarkGray;
            
            // 顶部标题区域
            var titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                // BackColor = Color.Gray
            };
            
            // 参数设置区域
            var paramPanel = new Panel
            {
                // Dock = DockStyle.Top,
                Width = 1280,
                Height = 30,
                Top = 10,
            };
            
            // 行数
            var lblRows = new Label { Text = "行数:", Width = 35, Height = 25, Location = new Point(460, 3)};
            txtRows = new TextBox { Text = "6", Width = 30, Height = 25, Left = 495 };
            txtRows.TextChanged += (_, _) => OnParametersChanged();
            
            // 列数
            var lblColumns = new Label { Text = "列数:", Width = 35, Height = 25, Location = new Point(545, 3) };
            txtColumns = new TextBox { Text = "6", Width = 30, Height = 25, Left = 580};
            txtColumns.TextChanged += (_, _) => OnParametersChanged();
            
            // 帧率
            var lblFrameRate = new Label { Text = "帧数:", Width = 35, Height = 25, Location = new Point(630, 3) };
            txtFrameRate = new TextBox { Text = "30", Width = 30, Height = 25, Left = 665};
            txtFrameRate.TextChanged += (_, _) => OnParametersChanged();
            
            paramPanel.Controls.AddRange([lblRows, txtRows, lblColumns, txtColumns, lblFrameRate, txtFrameRate]);

            btnClearAll = new Button
            {
                Text = "清空所有窗口",
                Size = new Size(120, 25),
                Location = new Point(520, 40),
                BackColor = Color.Red,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnClearAll.Click += BtnClearAll_Click;
            
            titlePanel.Controls.Add(btnClearAll);
            titlePanel.Controls.Add(paramPanel);
            
            Controls.Add(titlePanel);
            
            ResumeLayout(false);
        }

        private void SetupPanels()
        {
            // 创建三个功能面板
            sequencePanel = new SequencePanel(this);
            atlasPanel = new AtlasPanel(this);
            gifPanel = new GifPanel(this);
            
            // 布局：从左到右排列
            sequencePanel.Location = new Point(8, 70);
            sequencePanel.Size = new Size(470, 600);
            
            atlasPanel.Location = new Point(487, 70);
            atlasPanel.Size = new Size(410, 600);
            
            gifPanel.Location = new Point(906, 70);
            gifPanel.Size = new Size(350, 600);
            
            Controls.Add(sequencePanel);
            Controls.Add(atlasPanel);
            Controls.Add(gifPanel);
            // 设置默认输出路径为桌面
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            sequencePanel.UpdateOutputPath(desktopPath);
            atlasPanel.UpdateOutputPath(desktopPath);
            gifPanel.UpdateOutputPath(desktopPath);
        }

        // 提供给其他面板调用的方法
        public void SetAtlasImage(Bitmap atlas)
        {
            atlasPanel.SetAtlasImage(atlas);
        }

        public void SetGifImage(string gifPath)
        {
            gifPanel.SetGifImage(gifPath);
        }

        public void SetSequenceImages(List<Bitmap> images)
        {
            // 清空现有数据
            sequencePanel.ClearData();
            
            // 这里需要添加方法来设置序列帧图像
            // 由于SequencePanel的设计是文件路径，我们需要保存到临时文件
            var tempDir = Path.Combine(Path.GetTempPath(), "SequenceFrames");
            Directory.CreateDirectory(tempDir);
            
            for (var i = 0; i < images.Count; i++)
            {
                var tempPath = Path.Combine(tempDir, $"frame_{i:D4}.png");
                images[i].Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            
            // 模拟拖拽文件
            var files = Directory.GetFiles(tempDir, "*.png");
            sequencePanel.LoadImagesFromFilesPublic(files);
            
            sequencePanel.UpdateStatus($"已加载 {images.Count} 个序列帧");
        }

        private void OnParametersChanged()
        {
            atlasPanel.OnParametersChanged();
        }
        
        private void BtnClearAll_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("确定要清空所有窗口吗？", "确认", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;
            sequencePanel.ClearData();
            atlasPanel.ClearData();
            gifPanel.ClearData();
            Application.DoEvents(); // 确保UI更新
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 清理所有面板资源
            sequencePanel.Dispose();
            atlasPanel.Dispose();
            gifPanel.Dispose();
            
            base.OnFormClosing(e);
        }
        
        public int GetRows()
        {
            return int.TryParse(txtRows.Text, out var rows) && rows > 0 ? rows : 1;
        }

        public int GetColumns()
        {
            return int.TryParse(txtColumns.Text, out var cols) && cols > 0 ? cols : 1;
        }

        public int GetFrameRate()
        {
            return int.TryParse(txtFrameRate.Text, out var fps) && fps > 0 && fps <= 120 ? fps : 30;
        }
    }
}