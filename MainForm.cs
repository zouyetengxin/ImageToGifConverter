using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageMagick;

namespace ImageToGifConverter
{
    public partial class MainForm : Form
    {
        private Bitmap? originalImage;
        private Bitmap? displayImage;
        private int rows = 1;
        private int columns = 1;
        private bool showGridLines = true;
        private string? generatedGifPath;
        private int frameRate = 60; // 默认60fps
        private ImageAtlasForm? atlasForm;

        public MainForm()
        {
            InitializeComponent();
            SetupEventHandlers();
            
            // 确保初始帧率同步
            if (int.TryParse(txtFrameRate.Text, out int fps) && fps > 0 && fps <= 120)
            {
                frameRate = fps;
            }
        }

        private void SetupEventHandlers()
        {

            btnGenerateGif.Click += BtnGenerateGif_Click;
            txtRows.TextChanged += TxtRows_TextChanged;
            txtColumns.TextChanged += TxtColumns_TextChanged;
            txtFrameRate.TextChanged += TxtFrameRate_TextChanged;
            chkShowGrid.CheckedChanged += ChkShowGrid_CheckedChanged;
            picImage.Paint += PicImage_Paint;
            
            // 添加拖拽支持
            picImage.AllowDrop = true;
            picImage.DragEnter += PicImage_DragEnter;
            picImage.DragDrop += PicImage_DragDrop;
            
            // GIF预览区域支持鼠标事件（用于拖拽）
            picGifPreview.MouseDown += PicGifPreview_MouseDown;
        }



        private void PicImage_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0 && (files[0].ToLower().EndsWith(".png") || files[0].ToLower().EndsWith(".jpg") || files[0].ToLower().EndsWith(".jpeg") || files[0].ToLower().EndsWith(".bmp")))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void BtnOpenAtlas_Click(object? sender, EventArgs e)
        {
            if (atlasForm == null || atlasForm.IsDisposed)
            {
                atlasForm = new ImageAtlasForm();
                atlasForm.FormClosing += (s, args) =>
                {
                    // 当图集窗口关闭时，如果生成了图集，加载到主窗口
                    var generatedAtlas = atlasForm.GetGeneratedAtlas();
                    if (generatedAtlas != null)
                    {
                        LoadImageFromBitmap(generatedAtlas);
                    }
                    atlasForm = null;
                };
            }
            
            atlasForm.Show();
            atlasForm.BringToFront();
        }

        private void PicImage_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0)
                {
                    LoadImageFromFile(files[0]);
                }
            }
        }

        private void LoadImageFromFile(string filePath)
        {
            try
            {
                originalImage = new Bitmap(filePath);
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图片失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadImageFromBitmap(Bitmap bitmap)
        {
            try
            {
                originalImage = new Bitmap(bitmap);
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图集失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PicGifPreview_MouseDown(object? sender, MouseEventArgs e)
        {
            // 当鼠标按下时开始拖拽GIF表情包
            if (picGifPreview.Image != null && generatedGifPath != null && File.Exists(generatedGifPath) && e.Button == MouseButtons.Left)
            {
                try
                {
                    // 创建DataObject包含多种格式，支持表情包拖拽
                    DataObject dataObject = new DataObject();
                    
                    // 添加文件格式（备用）
                    string[] files = { generatedGifPath };
                    dataObject.SetData(DataFormats.FileDrop, files);
                    
                    // 添加图片格式（表情包形式）
                    using MemoryStream gifStream = new MemoryStream();
                    picGifPreview.Image.Save(gifStream, System.Drawing.Imaging.ImageFormat.Gif);
                    gifStream.Position = 0;
                    dataObject.SetData(DataFormats.Bitmap, true, picGifPreview.Image);
                    
                    // 延迟执行拖拽，避免与鼠标事件冲突
                    this.BeginInvoke(() =>
                    {
                        picGifPreview.DoDragDrop(dataObject, DragDropEffects.All);
                    });
                }
                catch (Exception)
                {
                    // 如果表情包拖拽失败，回退到文件拖拽
                    string[] files = { generatedGifPath };
                    DataObject fileDataObject = new DataObject(DataFormats.FileDrop, files);
                    
                    this.BeginInvoke(() =>
                    {
                        picGifPreview.DoDragDrop(fileDataObject, DragDropEffects.All);
                    });
                }
            }
        }

        /// <summary>
        /// 清理旧的GIF资源，确保文件不被占用
        /// </summary>
        private void CleanupOldGifResources()
        {
            try
            {
                // 清理预览窗口的图像资源
                if (picGifPreview.Image != null)
                {
                    var oldImage = picGifPreview.Image;
                    picGifPreview.Image = null;
                    
                    // 异步释放图像资源
                    Task.Run(() =>
                    {
                        if (oldImage != null)
                        {
                            oldImage.Dispose();
                        }
                    });
                }

                // 等待一小段时间确保资源释放
                Task.Delay(100).Wait();

                // 强制垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                // 清理失败不影响主流程
                System.Diagnostics.Debug.WriteLine($"清理资源时出错: {ex.Message}");
            }
        }

        private void TxtRows_TextChanged(object? sender, EventArgs e)
        {
            if (int.TryParse(txtRows.Text, out int r) && r > 0)
            {
                rows = r;
                UpdateDisplay();
            }
        }

        private void TxtColumns_TextChanged(object? sender, EventArgs e)
        {
            if (int.TryParse(txtColumns.Text, out int c) && c > 0)
            {
                columns = c;
                UpdateDisplay();
            }
        }

        private void TxtFrameRate_TextChanged(object? sender, EventArgs e)
        {
            if (int.TryParse(txtFrameRate.Text, out int fps) && fps > 0 && fps <= 120)
            {
                frameRate = fps;
            }
        }

        private void ChkShowGrid_CheckedChanged(object? sender, EventArgs e)
        {
            showGridLines = chkShowGrid.Checked;
            picImage.Invalidate();
        }

        private void UpdateDisplay()
        {
            if (originalImage == null) return;

            displayImage = new Bitmap(originalImage);
            picImage.Image = displayImage;
            picImage.SizeMode = PictureBoxSizeMode.Zoom;
            picImage.Invalidate();
        }

        private void PicImage_Paint(object? sender, PaintEventArgs e)
        {
            if (!showGridLines || originalImage == null) return;

            Graphics g = e.Graphics;
            PictureBox pb = (PictureBox)sender!;
            
            if (pb.Image == null) return;

            float scaleX = (float)pb.ClientSize.Width / pb.Image.Width;
            float scaleY = (float)pb.ClientSize.Height / pb.Image.Height;
            float scale = Math.Min(scaleX, scaleY);

            int scaledWidth = (int)(pb.Image.Width * scale);
            int scaledHeight = (int)(pb.Image.Height * scale);
            int offsetX = (pb.ClientSize.Width - scaledWidth) / 2;
            int offsetY = (pb.ClientSize.Height - scaledHeight) / 2;

            using Pen gridPen = new Pen(Color.Red, 2);
            
            for (int i = 1; i < rows; i++)
            {
                int y = offsetY + (i * scaledHeight / rows);
                g.DrawLine(gridPen, offsetX, y, offsetX + scaledWidth, y);
            }

            for (int i = 1; i < columns; i++)
            {
                int x = offsetX + (i * scaledWidth / columns);
                g.DrawLine(gridPen, x, offsetY, x, offsetY + scaledHeight);
            }
        }

        private async void BtnGenerateGif_Click(object? sender, EventArgs e)
        {
            if (originalImage == null)
            {
                MessageBox.Show("请先上传图片", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnGenerateGif.Enabled = false;
            btnGenerateGif.Text = "生成中...";
            progressBar.Visible = true;
            progressBar.Value = 0;
            lblProgress.Text = "准备生成GIF...";
            lblProgress.Visible = true;

            // 清理旧的GIF资源
            CleanupOldGifResources();

            try
            {
                string gifPath = await GenerateGifAsync();
                generatedGifPath = gifPath;
                
                // 显示生成的GIF
                picGifPreview.Image = Image.FromFile(gifPath);
                picGifPreview.SizeMode = PictureBoxSizeMode.Zoom;
                
                lblProgress.Text = "GIF生成成功！可以拖拽预览图到QQ聊天窗口";
                MessageBox.Show("GIF生成成功！可以拖拽右侧预览图到QQ聊天窗口", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblProgress.Text = "生成失败";
                MessageBox.Show($"GIF生成失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerateGif.Enabled = true;
                btnGenerateGif.Text = "生成GIF";
            }
        }

        private async Task<string> GenerateGifAsync()
        {
            if (originalImage == null) 
                throw new InvalidOperationException("原始图片为空");

            // 确保获取最新的帧率设置
            if (int.TryParse(txtFrameRate.Text, out int currentFps) && currentFps > 0 && currentFps <= 120)
            {
                frameRate = currentFps;
            }

            return await Task.Run(() =>
            {
                int cellWidth = originalImage.Width / columns;
                int cellHeight = originalImage.Height / rows;
                int totalFrames = rows * columns;

                using MagickImageCollection collection = new MagickImageCollection();

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        int frameIndex = row * columns + col;
                        
                        // 更新进度
                        Invoke(() =>
                        {
                            progressBar.Value = (frameIndex * 100) / totalFrames;
                            lblProgress.Text = $"正在裁剪第 {frameIndex + 1}/{totalFrames} 块...";
                        });

                        Rectangle cropRect = new Rectangle(
                            col * cellWidth,
                            row * cellHeight,
                            cellWidth,
                            cellHeight
                        );

                        using Bitmap croppedPiece = new Bitmap(cellWidth, cellHeight);
                        using (Graphics g = Graphics.FromImage(croppedPiece))
                        {
                            g.DrawImage(originalImage, 
                                new Rectangle(0, 0, cellWidth, cellHeight),
                                cropRect, 
                                GraphicsUnit.Pixel);
                        }

                        MagickImage magickImage = new MagickImage();
                        using (MemoryStream ms = new MemoryStream())
                        {
                            croppedPiece.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Position = 0;
                            magickImage.Read(ms);
                        }

                        // 计算每帧的延迟时间（ImageMagick使用百分之一秒为单位）
                        // 延迟时间(百分之一秒) = 100 / frameRate
                        int delay = (int)(100.0 / frameRate);
                        magickImage.AnimationDelay = delay;
                        // magickImage.AnimationTicksPerSecond = frameRate;

                        // 更新进度显示帧率信息
                        if (frameIndex == 0)
                        {
                            Invoke(() =>
                            {
                                lblProgress.Text += $" (帧率: {frameRate}fps, 延迟: {delay}×10ms)";
                            });
                        }
                        
                        collection.Add(magickImage);
                        // 不要在这里释放magickImage，因为它现在属于collection
                    }
                }

                // 选择保存路径
                string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"output_{DateTime.Now:yyyyMMdd_HHmmss}.gif");

                Invoke(() =>
                {
                    progressBar.Value = 90;
                    lblProgress.Text = "正在保存GIF文件...";
                });

                QuantizeSettings settings = new QuantizeSettings();
                settings.Colors = 256;
                collection.Quantize(settings);
                collection.Optimize();
                collection.Write(savePath, MagickFormat.Gif);

                Invoke(() =>
                {
                    progressBar.Value = 100;
                    lblProgress.Text = "GIF生成完成！";
                });

                return savePath;
            });
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Text = "图片转GIF工具";
            this.Size = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // 顶部控制区域 - 第一个窗口：图集合成
            Button btnOpenAtlas = new Button();
            btnOpenAtlas.Text = "图集合成";
            btnOpenAtlas.Location = new Point(20, 20);
            btnOpenAtlas.Size = new Size(100, 30);
            btnOpenAtlas.BackColor = Color.DeepSkyBlue;
            btnOpenAtlas.ForeColor = Color.White;
            btnOpenAtlas.Click += BtnOpenAtlas_Click;
            this.Controls.Add(btnOpenAtlas);
            
            Label lblRows = new Label();
            lblRows.Text = "行数:";
            lblRows.Location = new Point(450, 20);
            lblRows.Size = new Size(50, 23);
            this.Controls.Add(lblRows);
            
            txtRows = new TextBox();
            txtRows.Text = "1";
            txtRows.Location = new Point(500, 20);
            txtRows.Size = new Size(50, 30);
            this.Controls.Add(txtRows);
            
            Label lblColumns = new Label();
            lblColumns.Text = "列数:";
            lblColumns.Location = new Point(560, 20);
            lblColumns.Size = new Size(50, 23);
            this.Controls.Add(lblColumns);
            
            txtColumns = new TextBox();
            txtColumns.Text = "1";
            txtColumns.Location = new Point(610, 20);
            txtColumns.Size = new Size(50, 30);
            this.Controls.Add(txtColumns);
            
            Label lblFrameRate = new Label();
            lblFrameRate.Text = "帧率(fps):";
            lblFrameRate.Location = new Point(670, 20);
            lblFrameRate.Size = new Size(70, 23);
            this.Controls.Add(lblFrameRate);
            
            txtFrameRate = new TextBox();
            txtFrameRate.Text = "60";
            txtFrameRate.Location = new Point(740, 20);
            txtFrameRate.Size = new Size(40, 30);
            this.Controls.Add(txtFrameRate);
            
            chkShowGrid = new CheckBox();
            chkShowGrid.Text = "显示网格线";
            chkShowGrid.Checked = true;
            chkShowGrid.Location = new Point(790, 20);
            chkShowGrid.Size = new Size(100, 23);
            this.Controls.Add(chkShowGrid);
            
            btnGenerateGif = new Button();
            btnGenerateGif.Text = "生成GIF";
            btnGenerateGif.Location = new Point(900, 20);
            btnGenerateGif.Size = new Size(100, 30);
            this.Controls.Add(btnGenerateGif);
            
            // 进度显示区域
            progressBar = new ProgressBar();
            progressBar.Location = new Point(20, 60);
            progressBar.Size = new Size(980, 20);
            progressBar.Visible = false;
            this.Controls.Add(progressBar);
            
            lblProgress = new Label();
            lblProgress.Text = "";
            lblProgress.Location = new Point(1010, 60);
            lblProgress.Size = new Size(170, 20);
            lblProgress.Visible = false;
            this.Controls.Add(lblProgress);
            
            // 原图预览区域
            Label lblOriginal = new Label();
            lblOriginal.Text = "原图预览 (可拖拽图片到这里)";
            lblOriginal.Location = new Point(20, 90);
            lblOriginal.Size = new Size(300, 23);
            lblOriginal.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            this.Controls.Add(lblOriginal);
            
            picImage = new PictureBox();
            picImage.Location = new Point(20, 115);
            picImage.Size = new Size(570, 450);
            picImage.BorderStyle = BorderStyle.FixedSingle;
            picImage.BackColor = Color.LightGray;
            picImage.SizeMode = PictureBoxSizeMode.Zoom;
            this.Controls.Add(picImage);
            
            // GIF预览区域
            Label lblGif = new Label();
            lblGif.Text = "GIF预览 (可拖拽到QQ聊天窗口)";
            lblGif.Location = new Point(610, 90);
            lblGif.Size = new Size(300, 23);
            lblGif.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            this.Controls.Add(lblGif);
            
            picGifPreview = new PictureBox();
            picGifPreview.Location = new Point(610, 115);
            picGifPreview.Size = new Size(570, 450);
            picGifPreview.BorderStyle = BorderStyle.FixedSingle;
            picGifPreview.BackColor = Color.LightGray;
            picGifPreview.SizeMode = PictureBoxSizeMode.Zoom;
            this.Controls.Add(picGifPreview);
            
            this.ResumeLayout(false);
        }


        private TextBox txtRows = null!;
        private TextBox txtColumns = null!;
        private TextBox txtFrameRate = null!;
        private CheckBox chkShowGrid = null!;
        private Button btnGenerateGif = null!;
        private PictureBox picImage = null!;
        private PictureBox picGifPreview = null!;
        private ProgressBar progressBar = null!;
        private Label lblProgress = null!;

        /// <summary>
        /// 窗体关闭时清理所有资源
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CleanupOldGifResources();
            
            // 清理原图资源
            if (originalImage != null)
            {
                originalImage.Dispose();
                originalImage = null;
            }
            
            if (displayImage != null)
            {
                displayImage.Dispose();
                displayImage = null;
            }
            
            base.OnFormClosing(e);
        }
    }
}