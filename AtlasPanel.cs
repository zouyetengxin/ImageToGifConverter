using ImageMagick;

namespace ImageToGifConverter
{
    /// <summary>
    /// 窗口2：图集上传预览功能
    /// </summary>
    public class AtlasPanel : PanelBase
    {
        private Bitmap? currentAtlas;
        private Button btnConverterToSequence = null!;
        private Button btnConverterToGif = null!;
        private Button btnSaveAtlasToFile = null!;
        private readonly Pen gridPen;

        public AtlasPanel(MainForm mainForm) : base(mainForm, "图集预览")
        {
            InitializeAtlasPanel();
            gridPen = new Pen(Color.Red, 1) { DashPattern = [5, 5] };
            
            var titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
            };
            
            var lblWindow = new Label
            {
                Text = "• 拖拽图集图片上传\n• 拖拽预览图片到QQ聊天窗口",
                Location = new Point(20, 0),
                Size = new Size(400, 70),
                BackColor = Color.LightGreen
            };
            titlePanel.Controls.Add(lblWindow);
            Controls.Add(titlePanel);
        }

        private void InitializeAtlasPanel()
        {
            // 重绘事件来显示网格线
            PictureBox.Paint += PictureBox_Paint;
            
            // 添加按钮
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Top = 60
            };
            
            btnConverterToSequence = new Button
            {
                Text = "转为序列",
                Width = 80,
                Height = 25,
                Left = 0,
                BackColor = Color.Orange,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnConverterToSequence.Click += (_, _) => OnConverterToSequence();
            
            btnConverterToGif = new Button()
            {
                Text = "转为GIF",
                Width = 80,
                Height = 25,
                Left = 90,
                BackColor = Color.Green,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnConverterToGif.Click += (_, _) => OnConverterToGif();
            
            btnSaveAtlasToFile = new Button()
            {
                Text = "保存图集",
                Width = 80,
                Height = 25,
                Left = 180,
                BackColor = Color.Yellow,
                ForeColor = Color.Black,
                UseVisualStyleBackColor = false
            };
            btnSaveAtlasToFile.Click += (_, _) => OnSaveAtlasToFile();
            
            buttonPanel.Controls.AddRange([btnConverterToSequence, btnConverterToGif, btnSaveAtlasToFile]);
            Controls.Add(buttonPanel);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            LoadAtlasFromFile(files[0]);
        }

        protected override bool CanAcceptFiles(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true) return false;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (files.Length <= 0) return false;
            var ext = Path.GetExtension(files[0]).ToLower();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp";
        }

        private void LoadAtlasFromFile(string filePath)
        {
            try
            {
                currentAtlas?.Dispose();
                currentAtlas = new Bitmap(filePath);
                PictureBox.Image = currentAtlas;
                UpdateStatus($"已加载图集: {Path.GetFileName(filePath)}");
                PictureBox.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图集失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PictureBox_Paint(object? sender, PaintEventArgs e)
        {
            if (currentAtlas == null) return;
            var rows = GetRows();
            var columns = GetColumns();
            if (rows <= 1 && columns <= 1) return;
            var g = e.Graphics;
            var pb = (PictureBox)sender!;
            
            if (pb.Image == null) return;

            var scaleX = (float)pb.ClientSize.Width / pb.Image.Width;
            var scaleY = (float)pb.ClientSize.Height / pb.Image.Height;
            var scale = Math.Min(scaleX, scaleY);

            var scaledWidth = (int)(pb.Image.Width * scale);
            var scaledHeight = (int)(pb.Image.Height * scale);
            var offsetX = (pb.ClientSize.Width - scaledWidth) / 2;
            var offsetY = (pb.ClientSize.Height - scaledHeight) / 2;

            for (var i = 1; i < rows; i++)
            {
                var y = offsetY + (i * scaledHeight / rows);
                g.DrawLine(gridPen, offsetX, y, offsetX + scaledWidth, y);
            }

            for (var i = 1; i < columns; i++)
            {
                var x = offsetX + (i * scaledWidth / columns);
                g.DrawLine(gridPen, x, offsetY, x, offsetY + scaledHeight);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (currentAtlas == null || e.Button != MouseButtons.Left) return;
            try
            {
                var dataObject = new DataObject();
                    
                // 添加图片格式（表情包形式）
                using (var ms = new MemoryStream())
                {
                    currentAtlas.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    dataObject.SetData(DataFormats.Bitmap, true, currentAtlas);
                }
                    
                BeginInvoke(() => PictureBox.DoDragDrop(dataObject, DragDropEffects.All));
            }
            catch (Exception)
            {
                MessageBox.Show("拖拽失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void OnConverterToSequence()
        {
            if (currentAtlas == null)
            {
                MessageBox.Show("请先加载图集", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConverterToSequence.Enabled = false;
            UpdateStatus("转化序列中...");

            try
            {
                var sequenceImages = await Task.Run(CropAtlasToSequence);
                
                // 将裁剪后的序列帧发送到窗口1
                MainForm.SetSequenceImages(sequenceImages);
                MessageBox.Show("转化序列完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"转化序列失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConverterToSequence.Enabled = true;
            }
        }

        private List<Bitmap> CropAtlasToSequence()
        {
            var sequenceImages = new List<Bitmap>();
            if (currentAtlas != null)
            {
                var cellWidth = currentAtlas.Width / GetColumns();
                var cellHeight = currentAtlas.Height / GetRows();
                var totalImages = GetRows() * GetColumns();

                for (var i = 0; i < totalImages; i++)
                {
                    var row = i / GetColumns();
                    var col = i % GetColumns();
                    var cropRect = new Rectangle(
                        col * cellWidth,
                        row * cellHeight,
                        Math.Min(cellWidth, currentAtlas.Width - col * cellWidth),
                        Math.Min(cellHeight, currentAtlas.Height - row * cellHeight)
                    );

                    var croppedImage = new Bitmap(cropRect.Width, cropRect.Height);
                    using (var g = Graphics.FromImage(croppedImage))
                    {
                        g.DrawImage(currentAtlas, 0, 0, cropRect, GraphicsUnit.Pixel);
                    }
                
                    sequenceImages.Add(croppedImage);
                }
            }

            UpdateStatus($"图集已转化为 {sequenceImages.Count} 个序列帧");
            return sequenceImages;
        }

        private async void OnConverterToGif()
        {
            if (currentAtlas == null)
            {
                MessageBox.Show("请先加载图集", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConverterToGif.Enabled = false;
            UpdateStatus("转化GIF中...");

            try
            {
                var gifPath = await Task.Run(GenerateGifFromAtlas);
                
                // 生成GIF后显示在窗口3
                MainForm.SetGifImage(gifPath);
                MessageBox.Show("转化GIF成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"转化GIF失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConverterToGif.Enabled = true;
            }
        }

        private string GenerateGifFromAtlas()
        {
            using var collection = new MagickImageCollection();
            if (currentAtlas != null)
            {
                var cellWidth = currentAtlas.Width / GetColumns();
                var cellHeight = currentAtlas.Height / GetRows();
                var totalImages = GetRows() * GetColumns();

                for (var i = 0; i < totalImages; i++)
                {
                    var row = i / GetColumns();
                    var col = i % GetColumns();
                    var cropRect = new Rectangle(
                        col * cellWidth,
                        row * cellHeight,
                        Math.Min(cellWidth, currentAtlas.Width - col * cellWidth),
                        Math.Min(cellHeight, currentAtlas.Height - row * cellHeight)
                    );

                    using var croppedPiece = new Bitmap(cropRect.Width, cropRect.Height);
                    using var g = Graphics.FromImage(croppedPiece);
                    g.DrawImage(currentAtlas, 0, 0, cropRect, GraphicsUnit.Pixel);

                    using var ms = new MemoryStream();
                    croppedPiece.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;

                    var magickImage = new MagickImage(ms);
                    var delay = (int)(100.0 / GetFrameRate());
                    magickImage.AnimationDelay = delay;
                            
                    collection.Add(magickImage);
                }
            }

            var outputPath = Path.Combine(GetOutputPath(), $"atlas_{DateTime.Now:yyyyMMdd_HHmmss}.gif");
                
            var settings = new QuantizeSettings() { Colors = 256 };
            collection.Quantize(settings);
            collection.Optimize();
            collection.Write(outputPath, MagickFormat.Gif);
                
            UpdateStatus($"GIF已保存到: {Path.GetFileName(outputPath)}");
            return outputPath;
        }

        private void OnSaveAtlasToFile()
        {
            // 保存图集
            var outputPath = Path.Combine(GetOutputPath(), $"atlas_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            currentAtlas?.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            
            UpdateStatus($"图集已保存到: {Path.GetFileName(outputPath)}");
        }
        
        public void OnParametersChanged()
        {
            // 参数改变时重绘网格
            PictureBox.Invalidate();
        }

        protected override void OnSelectOutputPath()
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "选择输出路径";
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                UpdateOutputPath(folderDialog.SelectedPath);
            }
        }

        public void SetAtlasImage(Bitmap atlas)
        {
            currentAtlas?.Dispose();
            currentAtlas = new Bitmap(atlas);
            PictureBox.Image = currentAtlas;
            UpdateStatus("已设置图集");
            PictureBox.Invalidate();
        }

        public override void ClearData()
        {
            currentAtlas?.Dispose();
            currentAtlas = null;
            PictureBox.Image = null;
            UpdateStatus("就绪");
        }

        public override async Task<bool> ProcessAsync()
        {
            return await Task.Run(() => currentAtlas != null);
        }

        public override bool CanGenerate()
        {
            return currentAtlas != null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                currentAtlas?.Dispose();
                gridPen.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}