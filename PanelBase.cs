using System.Diagnostics.CodeAnalysis;

namespace ImageToGifConverter
{
    /// <summary>
    /// 窗口面板的抽象基类
    /// </summary>
    public abstract class PanelBase : GroupBox
    {
        protected MainForm MainForm { get; }
        protected PictureBox PictureBox = null!;
        
        protected TextBox TxtOutputPath = null!;
        private Button btnSelectOutputPath = null!;
        private ProgressBar progressBar = null!;
        private Label lblProgress = null!;

        protected PanelBase(MainForm mainForm, string title)
        {
            MainForm = mainForm;
            Text = title;
            InitializeComponents();
            SetupEventHandlers();
        }

        [AllowNull] public sealed override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        private void InitializeComponents()
        {
            Size = new Size(400, 500);
            BackColor = Color.LightGreen;

            // 主预览区域
            PictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.DarkGray,
                AllowDrop = true
            };
            
            // 底部控制面板
            var controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
            };
            
            // 输出路径区域
            var pathPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Top = 30
            };

            var lblOutput = new Label { Text = "输出路径:", Size = new Size(60, 25), Location = new Point(10, 5) };
            TxtOutputPath = new TextBox
            {
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ""),
                Size = new Size(250, 25),
                Location = new Point(70, 2),
                ReadOnly = true
            };
            btnSelectOutputPath = new Button
            {
                Text = "...", 
                Size = new Size(30, 25),
                Location = new Point(320, 0),
                UseVisualStyleBackColor = true
            };
            
            pathPanel.Controls.AddRange([lblOutput, TxtOutputPath, btnSelectOutputPath]);
            
            // 状态和按钮区域
            var progressPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Top = 60
            };
            
            progressBar = new ProgressBar
            {
                Location = new Point(30, 5),
                Size = new Size(300, 20)
            };
            
            lblProgress = new Label
            {
                Text = "就绪",
                Location = new Point(30, 24),
                Size = new Size(300, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            progressPanel.Controls.Add(lblProgress);
            progressPanel.Controls.Add(progressBar);
            
            controlPanel.Controls.AddRange([pathPanel, progressPanel]);

            Controls.Add(PictureBox);
            Controls.Add(controlPanel);
        }

        private void SetupEventHandlers()
        {
            PictureBox.DragEnter += (_, e) => OnDragEnter(e);
            PictureBox.DragDrop += (_, e) => OnDragDrop(e);
            PictureBox.MouseDown += (_, e) => OnMouseDown(e);
            btnSelectOutputPath.Click += (_, _) => OnSelectOutputPath();
        }

        protected new virtual void OnDragEnter(DragEventArgs e)
        {
            e.Effect = CanAcceptFiles(e) ? DragDropEffects.All : DragDropEffects.None;
        }

        protected new virtual void OnDragDrop(DragEventArgs e) { }
        protected new virtual void OnMouseDown(MouseEventArgs e) { }
        protected virtual void OnSelectOutputPath() { }

        protected virtual bool CanAcceptFiles(DragEventArgs e)
        {
            return e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
        }

        // 公共方法：更新状态
        public void UpdateStatus(string message)
        {
            if (lblProgress.InvokeRequired)
            {
                lblProgress.Invoke(() => lblProgress.Text = message);
                return;
            }
            lblProgress.Text = message;
        }

        // 添加一个方法来同步进度条
        public void UpdateProgress(int value, int maximum = 100)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(() => UpdateProgress(value, maximum));
                return;
            }
            
            progressBar.Maximum = maximum;
            progressBar.Value = value;
        }
        
        // 公共方法：更新输出路径
        public void UpdateOutputPath(string path)
        {
            if (TxtOutputPath.InvokeRequired)
            {
                TxtOutputPath.Invoke(() => TxtOutputPath.Text = path);
                return;
            }
            TxtOutputPath.Text = path;
        }

        protected int GetRows()
        {
            return MainForm.GetRows();
        }

        protected int GetColumns()
        {
            return MainForm.GetColumns();
        }

        protected int GetFrameRate()
        {
            return MainForm.GetFrameRate();
        }

        protected string GetOutputPath()
        {
            return TxtOutputPath.Text;
        }

        protected string GetOutputSequencePath()
        {
            return GetOutputPath() + "sequence";
        }

        // 子类需要实现的抽象方法
        public abstract void ClearData();
        public abstract Task<bool> ProcessAsync();
        public abstract bool CanGenerate();
    }
}