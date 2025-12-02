using ImageMagick;

namespace ImageToGifConverter
{
    /// <summary>
    /// 窗口3：GIF预览功能
    /// </summary>
    public class GifPanel : PanelBase
    {
        private string? currentGifPath;
        private Bitmap? currentGifImage;
        private MagickImageCollection collection = null!;
        private Button btnConverterToSequence = null!;
        private Button btnConverterToAtlas = null!;
        private Button btnSaveGifToFile = null!;

        public GifPanel(MainForm mainForm) : base(mainForm, "GIF预览")
        {
            InitializeGifPanel();
            
            var titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
            };
            
            var lblWindow = new Label
            {
                Text = "• 拖拽GIF图片上传\n• 拖拽预览GIF图片到QQ聊天窗口",
                Location = new Point(20, 0),
                Size = new Size(400, 70),
                BackColor = Color.LightGreen
            };
            titlePanel.Controls.Add(lblWindow);
            Controls.Add(titlePanel);
        }

        private void InitializeGifPanel()
        {
            // 添加按钮
            var buttonPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 30,
                Top = 60
            };
            
            btnConverterToSequence = new Button()
            {
                Text = "转为序列",
                Width = 80,
                Height = 25,
                Left = 0,
                BackColor = Color.Purple,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnConverterToSequence.Click += (_, _) => OnConverterToSequence();
            
            btnConverterToAtlas = new Button()
            {
                Text = "转为图集",
                Width = 80,
                Height = 25,
                Left = 90,
                BackColor = Color.Purple,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnConverterToAtlas.Click += (_, _) => OnConverterToAtlas();
            
            btnSaveGifToFile = new Button()
            {
                Text = "保存GIF",
                Width = 80,
                Height = 25,
                Left = 180,
                BackColor = Color.Purple,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnSaveGifToFile.Click += (_, _) => OnSaveGifToFile();
            
            buttonPanel.Controls.AddRange([btnConverterToSequence, btnConverterToAtlas, btnSaveGifToFile]);
            Controls.Add(buttonPanel);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                LoadGifFromFile(files[0]);
            }
        }

        protected override bool CanAcceptFiles(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0)
                {
                    var ext = Path.GetExtension(files[0]).ToLower();
                    return ext == ".gif";
                }
            }
            return false;
        }

        private void LoadGifFromFile(string filePath)
        {
            try
            {
                currentGifPath = filePath;
                currentGifImage?.Dispose();
                currentGifImage = new Bitmap(filePath);
                PictureBox.Image = currentGifImage;
                UpdateStatus($"已加载GIF: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载GIF失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (currentGifImage == null || currentGifPath == null ||
                !File.Exists(currentGifPath) || e.Button != MouseButtons.Left) return;
            try
            {
                var dataObject = new DataObject();
                    
                // 添加文件格式（备用）
                var files = new[] { currentGifPath };
                dataObject.SetData(DataFormats.FileDrop, files);
                    
                // 添加图片格式（表情包形式）
                using (var gifStream = new MemoryStream())
                {
                    currentGifImage.Save(gifStream, System.Drawing.Imaging.ImageFormat.Gif);
                    gifStream.Position = 0;
                    dataObject.SetData(DataFormats.Bitmap, true, currentGifImage);
                }
                    
                // 延迟执行拖拽，避免与鼠标事件冲突
                BeginInvoke(() => PictureBox.DoDragDrop(dataObject, DragDropEffects.All));
            }
            catch (Exception)
            {
                // 如果表情包拖拽失败，回退到文件拖拽
                var files = new[] { currentGifPath };
                var fileDataObject = new DataObject(DataFormats.FileDrop, files);
                    
                BeginInvoke(() => PictureBox.DoDragDrop(fileDataObject, DragDropEffects.All));
            }
        }

        private async void OnConverterToSequence()
        {
            if (currentGifPath == null || !File.Exists(currentGifPath))
            {
                MessageBox.Show("请先加载GIF", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConverterToSequence.Enabled = false;
            UpdateStatus("转化序列中...");

            try
            {
                var sequenceImages = await Task.Run(ExtractFramesFromGif);
                
                // 将GIF帧发送到窗口1
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

        private async void OnConverterToAtlas()
        {
            if (currentGifPath == null || !File.Exists(currentGifPath))
            {
                MessageBox.Show("请先加载GIF", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConverterToSequence.Enabled = false;
            UpdateStatus("转化图集中...");

            try
            {
                await Task.Run(ConvertGifToAtlas);
                
                MessageBox.Show("转化图集完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"转化图集失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConverterToSequence.Enabled = true;
            }
        }

        private List<Bitmap> ExtractFramesFromGif()
        {
            var sequenceImages = new List<Bitmap>();

            if (currentGifPath != null)
            {
                // collection?.Dispose();
                using var tempCollection = new MagickImageCollection(currentGifPath);
                foreach (var frame in tempCollection)
                {
                    // 将MagickImage转换为Bitmap
                    using var ms = new MemoryStream();
                    frame.Write(ms, MagickFormat.Png);
                    ms.Position = 0;

                    var bitmap = new Bitmap(ms);
                    sequenceImages.Add(bitmap);
                }
            }

            UpdateStatus($"GIF已提取为 {sequenceImages.Count} 个序列帧");
            return sequenceImages;
        }

        private void ConvertGifToAtlas()
        {
            var imageFullSizes = ExtractFramesFromGif();
            var maxImages = GetRows() * GetColumns();
            var fullImagesToUse = imageFullSizes.Take(maxImages).ToList();
            
            if (fullImagesToUse.Count == 0) return;
            
            var maxWidth = fullImagesToUse.Max(img => img.Width);
            var maxHeight = fullImagesToUse.Max(img => img.Height);
            
            var atlas = new Bitmap(GetColumns() * maxWidth, GetRows() * maxHeight);
            using (var g = Graphics.FromImage(atlas))
            {
                g.Clear(Color.White);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                
                for (var i = 0; i < fullImagesToUse.Count; i++)
                {
                    var row = i / GetColumns();
                    var col = i % GetColumns();
                    var x = col * maxWidth;
                    var y = row * maxHeight;
                    
                    g.DrawImage(fullImagesToUse[i], x, y, maxWidth, maxHeight);
                }
            }
            
            // 将图集发送到窗口2
            MainForm.SetAtlasImage(atlas);
        }

        private void OnSaveGifToFile()
        {
            var outputPath = Path.Combine(GetOutputPath(), $"sequence_{DateTime.Now:yyyyMMdd_HHmmss}.gif");
                
            var settings = new QuantizeSettings { Colors = 256 };
            collection.Quantize(settings);
            collection.Optimize();
            collection.Write(outputPath, MagickFormat.Gif);
            
            UpdateStatus($"GIF已保存到: {Path.GetFileName(outputPath)}");
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

        public void SetGifImage(string gifPath)
        {
            currentGifPath = gifPath;
            currentGifImage?.Dispose();
            currentGifImage = new Bitmap(gifPath);
            PictureBox.Image = currentGifImage;
            UpdateStatus("已设置GIF");
        }

        public override void ClearData()
        {
            currentGifImage?.Dispose();
            currentGifImage = null;
            currentGifPath = null;
            PictureBox.Image = null;
            UpdateStatus("就绪");
        }

        public override async Task<bool> ProcessAsync()
        {
            return await Task.Run(() => !string.IsNullOrEmpty(currentGifPath) && File.Exists(currentGifPath));
        }

        public override bool CanGenerate()
        {
            return !string.IsNullOrEmpty(currentGifPath) && File.Exists(currentGifPath);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                currentGifImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}