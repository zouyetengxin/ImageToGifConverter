using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageToGifConverter
{
    public partial class ImageAtlasForm : Form
    {
        private List<string> imagePaths = new List<string>();
        private List<Bitmap> imagePreviews = new List<Bitmap>();
        private PictureBox previewPictureBox = null!;
        private FlowLayoutPanel imageFlowPanel = null!;
        private Button btnGenerateAtlas = null!;
        private Label lblInfo = null!;
        private Button btnUploadFolder = null!;
        private Button btnUploadImages = null!;
        private Label lblRows = null!;
        private TextBox txtRows = null!;
        private Label lblColumns = null!;
        private TextBox txtColumns = null!;
        
        public ImageAtlasForm()
        {
            InitializeComponent();
            this.FormClosing += ImageAtlasForm_FormClosing;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "图片图集合成工具";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // 控制面板
            Panel controlPanel = new Panel();
            controlPanel.Dock = DockStyle.Top;
            controlPanel.Height = 60;
            controlPanel.BackColor = Color.LightGray;
            
            // 上传文件夹按钮
            btnUploadFolder = new Button();
            btnUploadFolder.Text = "上传文件夹";
            btnUploadFolder.Location = new Point(10, 15);
            btnUploadFolder.Size = new Size(100, 30);
            btnUploadFolder.Click += BtnUploadFolder_Click;
            controlPanel.Controls.Add(btnUploadFolder);
            
            // 上传多张图片按钮
            btnUploadImages = new Button();
            btnUploadImages.Text = "添加图片";
            btnUploadImages.Location = new Point(120, 15);
            btnUploadImages.Size = new Size(100, 30);
            btnUploadImages.Click += BtnUploadImages_Click;
            controlPanel.Controls.Add(btnUploadImages);
            
            // 行数标签和输入框
            lblRows = new Label();
            lblRows.Text = "行数:";
            lblRows.Location = new Point(250, 20);
            lblRows.Size = new Size(40, 23);
            controlPanel.Controls.Add(lblRows);
            
            txtRows = new TextBox();
            txtRows.Text = "1";
            txtRows.Location = new Point(290, 18);
            txtRows.Size = new Size(40, 25);
            txtRows.TextChanged += TxtRows_TextChanged;
            controlPanel.Controls.Add(txtRows);
            
            // 列数标签和输入框
            lblColumns = new Label();
            lblColumns.Text = "列数:";
            lblColumns.Location = new Point(340, 20);
            lblColumns.Size = new Size(40, 23);
            controlPanel.Controls.Add(lblColumns);
            
            txtColumns = new TextBox();
            txtColumns.Text = "1";
            txtColumns.Location = new Point(380, 18);
            txtColumns.Size = new Size(40, 25);
            txtColumns.TextChanged += TxtColumns_TextChanged;
            controlPanel.Controls.Add(txtColumns);
            
            // 生成图集按钮
            btnGenerateAtlas = new Button();
            btnGenerateAtlas.Text = "生成图集";
            btnGenerateAtlas.Location = new Point(450, 15);
            btnGenerateAtlas.Size = new Size(100, 30);
            btnGenerateAtlas.BackColor = Color.Green;
            btnGenerateAtlas.ForeColor = Color.White;
            btnGenerateAtlas.Click += BtnGenerateAtlas_Click;
            controlPanel.Controls.Add(btnGenerateAtlas);
            
            // 信息标签
            lblInfo = new Label();
            lblInfo.Text = "请拖拽文件夹或多张图片到下方区域";
            lblInfo.Location = new Point(570, 20);
            lblInfo.Size = new Size(300, 23);
            controlPanel.Controls.Add(lblInfo);
            
            this.Controls.Add(controlPanel);
            
            // 主要内容区域
            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterDistance = 400;
            
            // 左侧图片列表区域
            imageFlowPanel = new FlowLayoutPanel();
            imageFlowPanel.Dock = DockStyle.Fill;
            imageFlowPanel.BackColor = Color.WhiteSmoke;
            imageFlowPanel.AutoScroll = true;
            imageFlowPanel.AllowDrop = true;
            imageFlowPanel.DragEnter += ImageFlowPanel_DragEnter;
            imageFlowPanel.DragDrop += ImageFlowPanel_DragDrop;
            imageFlowPanel.DragOver += ImageFlowPanel_DragOver;
            
            // 右侧预览区域
            Panel previewPanel = new Panel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.BackColor = Color.LightYellow;
            
            previewPictureBox = new PictureBox();
            previewPictureBox.Dock = DockStyle.Fill;
            previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            previewPictureBox.BackColor = Color.White;
            previewPanel.Controls.Add(previewPictureBox);
            
            splitContainer.Panel1.Controls.Add(imageFlowPanel);
            splitContainer.Panel2.Controls.Add(previewPanel);
            
            this.Controls.Add(splitContainer);
            
            this.ResumeLayout(false);
        }

        private void BtnUploadFolder_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "选择包含图片的文件夹";
                folderDialog.ShowNewFolderButton = false;
                
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadImagesFromFolder(folderDialog.SelectedPath);
                }
            }
        }

        private void BtnUploadImages_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*";
                openFileDialog.Title = "选择多张图片";
                openFileDialog.Multiselect = true;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadImagesFromFiles(openFileDialog.FileNames);
                }
            }
        }

        private void LoadImagesFromFolder(string folderPath)
        {
            try
            {
                var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .ToArray();
                
                LoadImagesFromFiles(files);
                lblInfo.Text = $"已加载 {files.Length} 张图片";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件夹失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadImagesFromFiles(string[] filePaths)
        {
            foreach (string filePath in filePaths)
            {
                if (!imagePaths.Contains(filePath))
                {
                    imagePaths.Add(filePath);
                    
                    try
                    {
                        using (var bitmap = new Bitmap(filePath))
                        {
                            var thumbnail = new Bitmap(100, 100);
                            using (var g = Graphics.FromImage(thumbnail))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.DrawImage(bitmap, 0, 0, 100, 100);
                            }
                            
                            imagePreviews.Add(thumbnail);
                            CreateImageItem(thumbnail, imagePaths.Count - 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载图片失败 {filePath}: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            
            UpdatePreviewLayout();
        }

        private void CreateImageItem(Bitmap thumbnail, int index)
        {
            Panel itemPanel = new Panel();
            itemPanel.Size = new Size(110, 130);
            itemPanel.BackColor = Color.White;
            itemPanel.BorderStyle = BorderStyle.FixedSingle;
            itemPanel.Margin = new Padding(5);
            itemPanel.Tag = index;
            
            PictureBox pictureBox = new PictureBox();
            pictureBox.Image = thumbnail;
            pictureBox.Size = new Size(100, 100);
            pictureBox.Location = new Point(5, 5);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.AllowDrop = true;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            
            Label label = new Label();
            label.Text = $"{index + 1}";
            label.Location = new Point(5, 105);
            label.Size = new Size(100, 20);
            label.TextAlign = ContentAlignment.MiddleCenter;
            
            itemPanel.Controls.Add(pictureBox);
            itemPanel.Controls.Add(label);
            
            imageFlowPanel.Controls.Add(itemPanel);
        }

        private PictureBox? draggedPictureBox;
        private Point dragStartPoint;

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                draggedPictureBox = sender as PictureBox;
                dragStartPoint = e.Location;
            }
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (draggedPictureBox != null && e.Button == MouseButtons.Left)
            {
                if (Math.Abs(e.X - dragStartPoint.X) > 5 || Math.Abs(e.Y - dragStartPoint.Y) > 5)
                {
                    draggedPictureBox.DoDragDrop(draggedPictureBox, DragDropEffects.Move);
                }
            }
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            draggedPictureBox = null;
        }

        private void ImageFlowPanel_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(PictureBox)) == true || e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void ImageFlowPanel_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(PictureBox)) == true)
            {
                var draggedBox = e.Data.GetData(typeof(PictureBox)) as PictureBox;
                var targetPanel = GetPanelAtPoint(sender as Control, e);
                
                if (targetPanel != null && draggedBox?.Parent != targetPanel)
                {
                    // 这里可以实现拖拽重排序
                }
            }
        }

        private void ImageFlowPanel_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    LoadImagesFromFiles(files);
                }
            }
        }

        private Panel? GetPanelAtPoint(Control? container, DragEventArgs e)
        {
            if (container == null) return null;
            
            var screenPoint = new Point(e.X, e.Y);
            var clientPoint = container.PointToClient(screenPoint);
            return container.GetChildAtPoint(clientPoint) as Panel;
        }

        private void TxtRows_TextChanged(object? sender, EventArgs e)
        {
            UpdatePreviewLayout();
        }

        private void TxtColumns_TextChanged(object? sender, EventArgs e)
        {
            UpdatePreviewLayout();
        }

        private void UpdatePreviewLayout()
        {
            if (int.TryParse(txtRows.Text, out int rows) && int.TryParse(txtColumns.Text, out int cols) && rows > 0 && cols > 0)
            {
                GeneratePreviewAtlas(rows, cols);
            }
        }

        private void GeneratePreviewAtlas(int rows, int cols)
        {
            if (imagePreviews.Count == 0) return;
            
            var maxImages = rows * cols;
            var imagesToUse = imagePreviews.Take(maxImages).ToList();
            
            if (imagesToUse.Count == 0) return;
            
            var cellWidth = 100;
            var cellHeight = 100;
            var totalWidth = cols * cellWidth;
            var totalHeight = rows * cellHeight;
            
            var atlas = new Bitmap(totalWidth, totalHeight);
            using (var g = Graphics.FromImage(atlas))
            {
                g.Clear(Color.White);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                
                for (int i = 0; i < imagesToUse.Count; i++)
                {
                    var row = i / cols;
                    var col = i % cols;
                    var x = col * cellWidth;
                    var y = row * cellHeight;
                    
                    g.DrawImage(imagesToUse[i], x, y, cellWidth, cellHeight);
                    
                    // 添加边框
                    using (var pen = new Pen(Color.LightGray, 1))
                    {
                        g.DrawRectangle(pen, x, y, cellWidth, cellHeight);
                    }
                    
                    // 添加序号
                    using (var brush = new SolidBrush(Color.Black))
                    using (var font = new Font("Arial", 12, FontStyle.Bold))
                    {
                        g.DrawString($"{i + 1}", font, brush, x + 5, y + 5);
                    }
                }
            }
            
            previewPictureBox.Image = atlas;
        }

        private async void BtnGenerateAtlas_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtRows.Text, out int rows) || !int.TryParse(txtColumns.Text, out int cols) || rows <= 0 || cols <= 0)
            {
                MessageBox.Show("请输入有效的行数和列数", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            if (imagePaths.Count == 0)
            {
                MessageBox.Show("请先添加图片", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            btnGenerateAtlas.Enabled = false;
            btnGenerateAtlas.Text = "生成中...";
            
            try
            {
                await System.Threading.Tasks.Task.Run(() => GenerateFinalAtlas(rows, cols));
                MessageBox.Show("图集生成完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成图集失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerateAtlas.Enabled = true;
                btnGenerateAtlas.Text = "生成图集";
            }
        }

        private void GenerateFinalAtlas(int rows, int cols)
        {
            var maxImages = rows * cols;
            var pathsToUse = imagePaths.Take(maxImages).ToList();
            
            if (pathsToUse.Count == 0) return;
            
            // 计算最大尺寸
            var maxWidth = 0;
            var maxHeight = 0;
            var fullImages = new List<Bitmap>();
            
            foreach (var path in pathsToUse)
            {
                var img = new Bitmap(path);
                fullImages.Add(img);
                maxWidth = Math.Max(maxWidth, img.Width);
                maxHeight = Math.Max(maxHeight, img.Height);
            }
            
            var atlas = new Bitmap(cols * maxWidth, rows * maxHeight);
            using (var g = Graphics.FromImage(atlas))
            {
                g.Clear(Color.White);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                
                for (int i = 0; i < fullImages.Count; i++)
                {
                    var row = i / cols;
                    var col = i % cols;
                    var x = col * maxWidth;
                    var y = row * maxHeight;
                    
                    g.DrawImage(fullImages[i], x, y, maxWidth, maxHeight);
                }
            }
            
            // 保存到桌面
            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"atlas_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            atlas.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
            
            // 更新预览
            Invoke(() =>
            {
                previewPictureBox.Image = atlas;
                lblInfo.Text = $"图集已保存到: {Path.GetFileName(savePath)}";
            });
            
            // 清理资源
            foreach (var img in fullImages)
            {
                img.Dispose();
            }
        }

        private void ImageAtlasForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // 清理预览图资源
            foreach (var preview in imagePreviews)
            {
                preview.Dispose();
            }
            imagePreviews.Clear();
            
            if (previewPictureBox.Image != null)
            {
                previewPictureBox.Image.Dispose();
            }
        }

        public Bitmap? GetGeneratedAtlas()
        {
            return previewPictureBox.Image as Bitmap;
        }
    }
}