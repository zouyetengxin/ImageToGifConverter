using ImageMagick;

namespace ImageToGifConverter
{
    /// <summary>
    /// 窗口1：序列帧相关功能窗口
    /// 包含上传，预览，拖拽排序
    /// </summary>
    public class SequencePanel : PanelBase
    {
        private FlowLayoutPanel thumbnailPanel = null!;
        private readonly List<string> imagePaths = new();
        private readonly List<Bitmap> imagePreviews = new();
        private readonly List<Bitmap> imageFullSizes = new();
        private Button btnConverterToAtlas = null!;
        private Button btnConverterToGif = null!;
        private Button btnSaveSequenceToFile = null!;

        public SequencePanel(MainForm mainForm) : base(mainForm, "序列帧预览")
        {
            InitializeSequencePanel();
            
            var titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
            };
            
            var lblWindow = new Label
            {
                Text = "• 拖拽多个图片上传\n• 支持拖拽图片排序\n• 支持按住Ctrl拖拽预览图片到QQ聊天窗口",
                Location = new Point(20, 0),
                Size = new Size(400, 70),
                BackColor = Color.LightGreen
            };
            titlePanel.Controls.Add(lblWindow);
            Controls.Add(titlePanel);
        }

        private void InitializeSequencePanel()
        {
            // 修改PictureBox为FlowLayoutPanel来显示缩略图
            PictureBox.Dispose();
            
            thumbnailPanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.DarkGray,
                AutoScroll = true,
                AllowDrop = true
            };
            
            thumbnailPanel.DragEnter += ThumbnailPanel_DragEnter;
            thumbnailPanel.DragDrop += ThumbnailPanel_DragDrop;
            
            Controls.Add(thumbnailPanel);
            Controls.SetChildIndex(thumbnailPanel, 0);
            
            // 添加生成按钮
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Top = 60
            };
            
            btnConverterToAtlas = new Button()
            {
                Text = "转为图集",
                Width = 80,
                Height = 25,
                Left = 0,
                BackColor = Color.DeepSkyBlue,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnConverterToAtlas.Click += (_, _) => OnConverterToAtlas();
            
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
            
            btnSaveSequenceToFile = new Button()
            {
                Text = "保存序列",
                Width = 80,
                Height = 25,
                Left = 180,
                BackColor = Color.Orange,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnSaveSequenceToFile.Click += (_, _) => OnSaveSequenceToFile();
            
            buttonPanel.Controls.AddRange([btnConverterToAtlas, btnConverterToGif, btnSaveSequenceToFile]);
            Controls.Add(buttonPanel);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            LoadImagesFromFiles(files);
        }

        private void LoadImagesFromFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (imagePaths.Contains(filePath)) continue;
                var fileExt = Path.GetExtension(filePath).ToLower();
                if (fileExt != ".jpg" && fileExt != ".jpeg" && fileExt != ".png" && fileExt != ".bmp") continue;
                imagePaths.Add(filePath);
                        
                try
                {
                    var fullImage = new Bitmap(filePath);
                    imageFullSizes.Add(fullImage);
                            
                    // 创建缩略图
                    var thumbnail = new Bitmap(60, 60);
                    using (var g = Graphics.FromImage(thumbnail))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(fullImage, 0, 0, 60, 60);
                    }
                            
                    imagePreviews.Add(thumbnail);
                    CreateImageItem(thumbnail, imagePreviews.Count - 1);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图片失败 {filePath}: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            
            UpdateStatus($"已加载 {imagePreviews.Count} 张图片");
        }

        private void CreateImageItem(Bitmap thumbnail, int index)
        {
            var itemPanel = new Panel
            {
                Size = new Size(70, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(2),
                Tag = index
            };
            
            var pictureBox = new PictureBox
            {
                Image = thumbnail,
                Size = new Size(60, 60),
                Location = new Point(5, 5),
                SizeMode = PictureBoxSizeMode.Zoom,
                AllowDrop = true
            };
            
            // 支持拖拽重排序和拖拽到QQ
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.DragEnter += PictureBox_DragEnter;
            pictureBox.DragDrop += PictureBox_DragDrop;
            
            var label = new Label
            {
                Text = $"{index + 1}",
                Location = new Point(5, 65),
                Size = new Size(60, 15),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 8)
            };
            
            itemPanel.Controls.Add(pictureBox);
            itemPanel.Controls.Add(label);
            
            thumbnailPanel.Controls.Add(itemPanel);
        }

        private PictureBox? draggedPictureBox;
        private Point dragStartPoint;
        private bool isDragging = false;

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            
            // 如果按住Ctrl键，直接拖拽到QQ
            if (Control.ModifierKeys == Keys.Control)
            {
                StartDragToQQ((PictureBox)sender!);
                return;
            }
            
            draggedPictureBox = sender as PictureBox;
            dragStartPoint = e.Location;
            isDragging = false;
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (draggedPictureBox == null || e.Button != MouseButtons.Left) return;
            
            if (Math.Abs(e.X - dragStartPoint.X) > 5 || Math.Abs(e.Y - dragStartPoint.Y) > 5)
            {
                // 开始拖拽重排序
                isDragging = true;
                draggedPictureBox.DoDragDrop(draggedPictureBox, DragDropEffects.Move);
            }
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && draggedPictureBox != null && !isDragging)
            {
                // 如果没有拖拽移动，直接拖拽到QQ
                StartDragToQQ((PictureBox)sender!);
            }
            draggedPictureBox = null;
            isDragging = false;
        }

        private void StartDragToQQ(PictureBox pictureBox)
        {
            try
            {
                var itemPanel = (Panel)pictureBox.Parent!;
                var index = (int)itemPanel.Tag!;

                if (index < 0 || index >= imageFullSizes.Count) return;
                var bitmap = imageFullSizes[index];
                
                // 简化为与AtlasPanel相同的实现
                var dataObject = new DataObject();
                    
                // 添加图片格式（表情包形式）
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    dataObject.SetData(DataFormats.Bitmap, true, bitmap);
                }
                    
                isDragging = true;
                pictureBox.DoDragDrop(dataObject, DragDropEffects.All);
            }
            catch (Exception)
            {
                MessageBox.Show("拖拽失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PictureBox_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(PictureBox)) == true)
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void PictureBox_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(PictureBox)) != true) return;
            
            var sourcePictureBox = (PictureBox)e.Data.GetData(typeof(PictureBox))!;
            var targetPictureBox = sender as PictureBox;
            
            if (sourcePictureBox == targetPictureBox) return;
            
            // 获取源图片和目标图片的索引
            var sourcePanel = (Panel)sourcePictureBox.Parent!;
            var targetPanel = (Panel)targetPictureBox!.Parent!;
            var sourceIndex = (int)sourcePanel.Tag!;
            var targetIndex = (int)targetPanel.Tag!;
            
            // 交换数据和标签
            SwapImages(sourceIndex, targetIndex);
            
            // 更新界面显示
            UpdateThumbnailDisplay();
        }

        private void ThumbnailPanel_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(PictureBox)) == true)
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void ThumbnailPanel_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(PictureBox)) != true) return;
            
            var sourcePictureBox = (PictureBox)e.Data.GetData(typeof(PictureBox))!;
            
            // 获取鼠标位置找到目标PictureBox
            var point = thumbnailPanel.PointToClient(new Point(e.X, e.Y));
            var targetControl = thumbnailPanel.GetChildAtPoint(point);
            
            if (targetControl is Panel targetPanel && targetPanel != sourcePictureBox.Parent)
            {
                var sourceIndex = (int)((Panel)sourcePictureBox.Parent!).Tag!;
                var targetIndex = (int)targetPanel.Tag!;
                
                // 交换数据和标签
                SwapImages(sourceIndex, targetIndex);
                
                // 更新界面显示
                UpdateThumbnailDisplay();
            }
        }

        private void SwapImages(int index1, int index2)
        {
            // 交换图片路径
            (imagePaths[index1], imagePaths[index2]) = (imagePaths[index2], imagePaths[index1]);
            
            // 交换预览图
            (imagePreviews[index1], imagePreviews[index2]) = (imagePreviews[index2], imagePreviews[index1]);
            
            // 交换全尺寸图
            (imageFullSizes[index1], imageFullSizes[index2]) = (imageFullSizes[index2], imageFullSizes[index1]);
        }

        private void UpdateThumbnailDisplay()
        {
            // 重新生成缩略图显示
            thumbnailPanel.Controls.Clear();
            
            for (var i = 0; i < imagePreviews.Count; i++)
            {
                CreateImageItem(imagePreviews[i], i);
            }
        }

        private async void OnConverterToAtlas()
        {
            if (imageFullSizes.Count == 0)
            {
                MessageBox.Show("请先添加图片", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConverterToAtlas.Enabled = false;
            UpdateStatus("转化图集中...");

            try
            {
                await Task.Run(GenerateAtlas);
                
                MessageBox.Show("转化图集完成！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"转化图集失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConverterToAtlas.Enabled = true;
            }
        }

        private void GenerateAtlas()
        {
            var maxImages = GetRows() * GetColumns();
            var fullImagesToUse = imageFullSizes.Take(maxImages).ToList();
            
            if (fullImagesToUse.Count == 0) return;
            
            var maxWidth = fullImagesToUse.Max(img => img.Width);
            var maxHeight = fullImagesToUse.Max(img => img.Height);
            
            var atlas = new Bitmap(GetColumns() * maxWidth, GetRows() * maxHeight);
            using var g = Graphics.FromImage(atlas);
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

            // 生成图集后显示在窗口2
            MainForm.SetAtlasImage(atlas);
        }

        private async void OnConverterToGif()
        {
            if (imageFullSizes.Count == 0)
            {
                MessageBox.Show("请先添加图片", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConverterToGif.Enabled = false;
            UpdateStatus("转化GIF中...");

            try
            {
                var gifPath = await Task.Run(GenerateGifFromSequence);
                
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

        private string GenerateGifFromSequence()
        {
            using var collection = new MagickImageCollection();
            var maxImages = GetRows() * GetColumns();
            var imagesToUse = imageFullSizes.Take(maxImages).ToList();
                
            foreach (var bitmap in imagesToUse)
            {
                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                var magickImage = new MagickImage(ms);
                var delay = (int)(100.0 / GetFrameRate());
                magickImage.AnimationDelay = delay;
                        
                collection.Add(magickImage);
            }

            var outputPath = Path.Combine(GetOutputPath(), $"sequence_{DateTime.Now:yyyyMMdd_HHmmss}.gif");
                
            var settings = new QuantizeSettings { Colors = 256 };
            collection.Quantize(settings);
            collection.Optimize();
            collection.Write(outputPath, MagickFormat.Gif);
            
            UpdateStatus($"GIF已保存到: {Path.GetFileName(outputPath)}");
            return outputPath;
        }

        private void OnSaveSequenceToFile()
        {
            if (imageFullSizes.Count == 0)
            {
                MessageBox.Show("请先添加图片", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 创建输出目录
            var outputFile = Path.Combine(GetOutputPath(), $"Sequence_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(outputFile);

            try
            {
                // 保存每张图片
                for (var i = 0; i < imageFullSizes.Count; i++)
                {
                    var bitmap = imageFullSizes[i];
                    var imagePath = Path.Combine(outputFile, $"{i + 1}.png");
                    bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                }

                UpdateStatus($"序列帧已保存到: {outputFile}");
                MessageBox.Show($"已保存 {imageFullSizes.Count} 张图片到:\n{outputFile}", "保存成功", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存序列帧失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        public override void ClearData()
        {
            // 清理所有图片
            foreach (var preview in imagePreviews)
            {
                preview.Dispose();
            }
            imagePreviews.Clear();
            
            foreach (var fullImg in imageFullSizes)
            {
                fullImg.Dispose();
            }
            imageFullSizes.Clear();
            
            imagePaths.Clear();
            thumbnailPanel.Controls.Clear();
            UpdateStatus("就绪");
        }

        public override async Task<bool> ProcessAsync()
        {
            return await Task.Run(() => imageFullSizes.Count > 0);
        }

        public override bool CanGenerate()
        {
            return imageFullSizes.Count > 0;
        }

        // 公共方法：从外部加载图片文件
        public void LoadImagesFromFilesPublic(string[] filePaths)
        {
            LoadImagesFromFiles(filePaths);
        }
    }
}