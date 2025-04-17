using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Windows.Controls.Primitives;
using OpenCvSharp.WpfExtensions;
using LamaCleaner_TorchSharp.Common;
using System.Windows.Threading;
using System.Windows.Ink;
using SkiaSharp;
using System.Threading;
using TorchSharp;
using Ookii.Dialogs.Wpf;
using System.Threading.Tasks;
using System.Diagnostics;

namespace InpaintDesktop
{
    public partial class MainWindow : Window
    {
        // --- Commands for Hotkeys ---
        public static readonly RoutedCommand OpenImageCommand = new RoutedCommand();
        public static readonly RoutedCommand InpaintCommand = new RoutedCommand();
        public static readonly RoutedCommand IncreaseBrushCommand = new RoutedCommand();
        public static readonly RoutedCommand DecreaseBrushCommand = new RoutedCommand();
        public static readonly RoutedCommand SaveImageCommand = new RoutedCommand();

        private double brushSize = 20;
        private bool isEraserMode = false;
        private readonly SolidColorBrush maskBrush = new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));

        private readonly Color drawingColor = Color.FromArgb(204, 255, 255, 0); // Yellow, 0.8 alpha

        public MainWindow()
        {
            InitializeComponent();
            InitializeCommandBindings();
            InitializeHotKeys();
        }
        private void InitializeCommandBindings()
        {
            this.CommandBindings.Add(new CommandBinding(OpenImageCommand, OpenImage_Executed));
            this.CommandBindings.Add(new CommandBinding(InpaintCommand, Inpaint_Executed));
            this.CommandBindings.Add(new CommandBinding(IncreaseBrushCommand, IncreaseBrush_Executed));
            this.CommandBindings.Add(new CommandBinding(DecreaseBrushCommand, DecreaseBrush_Executed));
            this.CommandBindings.Add(new CommandBinding(SaveImageCommand, SaveImage_Executed));
        }

        private void InitializeHotKeys()
        {
            this.InputBindings.Add(new KeyBinding(OpenImageCommand, Key.O, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(InpaintCommand, Key.I, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(IncreaseBrushCommand, Key.OemPlus, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(DecreaseBrushCommand, Key.OemMinus, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(SaveImageCommand, Key.S, ModifierKeys.Control));
        }


        // --- Command Execution Handlers ---
        private void OpenImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadImageButton_Click(sender, e);
        }

        private void Inpaint_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            InpaintButton_Click(sender, e);
        }

        private void IncreaseBrush_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            IncreaseBrushButton_Click(sender, e);
        }

        private void DecreaseBrush_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DecreaseBrushButton_Click(sender, e);
        }

        private void SaveImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveButton_Click(sender, e);
        }

        // --- Button Click Handlers ---
        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadImage(openFileDialog.FileName);
            }
        }

        private void LoadImage(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();

                MainImage.Source = bitmap;
                MaskCanvas.Strokes.Clear();
                UpdateImageCanvasSize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Localization.LocalizationHelper.GetString("Error_LoadingImage", "Error loading image:")} {ex.Message}",
                               Localization.LocalizationHelper.GetString("Error_Title", "Error"),
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateImageCanvasSize()
        {
            if (MainImage.Source is BitmapSource bitmapSource)
            {
                ImageCanvas.Width = bitmapSource.PixelWidth;
                ImageCanvas.Height = bitmapSource.PixelHeight;
                MaskCanvas.Width = ImageCanvas.Width;
                MaskCanvas.Height = ImageCanvas.Height;
                SetDrawingMode();
            }
            else
            {
                ImageCanvas.Width = 0;
                ImageCanvas.Height = 0;
                MaskCanvas.Width = 0;
                MaskCanvas.Height = 0;
            }
        }

      
        private void SetDrawingMode()
        {
            MaskCanvas.EditingMode = InkCanvasEditingMode.Ink; // 设置为绘图模式

            // 配置画笔属性
            DrawingAttributes inkAttributes = MaskCanvas.DefaultDrawingAttributes;
            inkAttributes.Color = drawingColor;
            inkAttributes.Width = brushSize;
            inkAttributes.Height = brushSize;
            inkAttributes.StylusTip = StylusTip.Ellipse;
            inkAttributes.IsHighlighter = false;  //确保不是荧光笔

            // 应用修改 (如果需要新建实例)
            MaskCanvas.DefaultDrawingAttributes = inkAttributes; // 通常修改现有实例即可
        }

        private void SetEraserMode()
        {
            var ellipse = new EllipseStylusShape(brushSize, brushSize);
            MaskCanvas.EraserShape = ellipse;
            // 选择一种橡皮擦模式:
            // EraseByPoint: 基于点的橡皮擦，需要设置橡皮擦大小
            // EraseByStroke: 点击即可擦除整个笔划
            MaskCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;


        }
        private void BrushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            brushSize = e.NewValue; 
            MaskCanvas.EditingMode = InkCanvasEditingMode.Ink;
            if (isEraserMode)
            {
                SetEraserMode();
            }
            else
            {
                SetDrawingMode();
            }
            if (BrushSizeText != null)
            {
                BrushSizeText.Text = brushSize.ToString("0");
            }
        }

        private void IncreaseBrushButton_Click(object sender, RoutedEventArgs e)
        {
            BrushSizeSlider.Value = Math.Min(BrushSizeSlider.Maximum, BrushSizeSlider.Value + 5);
        }

        private void DecreaseBrushButton_Click(object sender, RoutedEventArgs e)
        {
            BrushSizeSlider.Value = Math.Max(BrushSizeSlider.Minimum, BrushSizeSlider.Value - 5);
        }

        private void EraserToggle_Click(object sender, RoutedEventArgs e)
        {
            isEraserMode = !isEraserMode;
            if (isEraserMode)
            {
                SetEraserMode();
            }
            else
            {
                SetDrawingMode();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            MainImage.Source = null;
            MaskCanvas.Children.Clear();
            isEraserMode = false;
            if (EraserToggle != null) EraserToggle.IsChecked = false;
            UpdateImageCanvasSize();
        }

   
        private BitmapSource? CreateMaskBitmap()
        {
            if (MaskCanvas.ActualWidth <= 0 || MaskCanvas.ActualHeight <= 0)
            {
                MessageBox.Show("Canvas size is invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var renderWidth = (int)MaskCanvas.ActualWidth;
            var renderHeight = (int)MaskCanvas.ActualHeight;

            // Use a DrawingVisual to manually construct the opaque mask
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // 1. Draw the mandatory black background for the mask
                //    This represents the default (non-masked) state.
                drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, renderWidth, renderHeight));

                // 2. Iterate through the remaining strokes on the InkCanvas
                //    (EraseByPoint modifies this collection directly)
                foreach (var stroke in MaskCanvas.Strokes)
                {
                    // We only care about drawing strokes that *remain* after erasing.
                    // Get the geometry representing the stroke's area.
                    // Use the stroke's attributes to ensure thickness/shape is considered.
                    Geometry geometry = stroke.GetGeometry(stroke.DrawingAttributes);
                    geometry.Freeze(); // Optimize geometry

                    // Draw the geometry onto the visual using OPAQUE WHITE
                    // regardless of the stroke's visual transparency.
                    drawingContext.DrawGeometry(Brushes.White, null, geometry);
                }
            } // DrawingContext is disposed here

            // Render the constructed DrawingVisual (Black background + White strokes)
            var renderTargetBitmap = new RenderTargetBitmap(
                renderWidth, renderHeight,
                96, 96, // DPI
                PixelFormats.Pbgra32); // Render first in color

            renderTargetBitmap.Render(drawingVisual);

            // Convert the result to a pure Black and White format
            var maskBitmap = new FormatConvertedBitmap(renderTargetBitmap, PixelFormats.BlackWhite, null, 0);
            maskBitmap.Freeze();

            return maskBitmap;
        }

        private void InpaintButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainImage.Source == null)
            {
                MessageBox.Show(Localization.LocalizationHelper.GetString("Error_LoadImageFirst", "Please load an image first."),
                                Localization.LocalizationHelper.GetString("Error_Title", "Error"),
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BitmapSource? mask = CreateMaskBitmap();// RenderToBitmap(MaskCanvas);// CreateMaskBitmap();
            if (mask == null)
            {
                return;
            }

            BitmapSource? originalImage = MainImage.Source as BitmapSource;
            if (originalImage == null)
            {
                MessageBox.Show(Localization.LocalizationHelper.GetString("Error_InvalidImageFormat", "Invalid image format."),
                                Localization.LocalizationHelper.GetString("Error_Title", "Error"),
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var imgmat = originalImage.ToMat();
            var maskmat = mask.ToMat();
            var inpaintimg = Processor.Process(imgmat,maskmat);

            // TODO: Implement the actual Inpainting logic
            BitmapSource ? inpaintedResult = inpaintimg.ToBitmapSource();
            MainImage.Source = inpaintedResult;
            MaskCanvas.Strokes.Clear();

            MessageBox.Show(Localization.LocalizationHelper.GetString("Info_InpaintNotImplemented", "Inpaint function called. Mask generated, but actual inpainting is not implemented yet."),
                            Localization.LocalizationHelper.GetString("Info_Title", "Info"),
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            BitmapSource? imageToSave = MainImage.Source as BitmapSource;

            if (imageToSave == null)
            {
                MessageBox.Show(Localization.LocalizationHelper.GetString("Info_NoImageToSave", "No image to save."),
                                Localization.LocalizationHelper.GetString("Info_Title", "Info"),
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
                DefaultExt = ".png",
                FileName = "inpainted_result.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapEncoder encoder;
                    string ext = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLowerInvariant();
                    switch (ext)
                    {
                        case ".jpg":
                        case ".jpeg":
                            encoder = new JpegBitmapEncoder { QualityLevel = 90 };
                            break;
                        case ".bmp":
                            encoder = new BmpBitmapEncoder();
                            break;
                        default:
                            encoder = new PngBitmapEncoder();
                            break;
                    }
                    encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                    using (var fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{Localization.LocalizationHelper.GetString("Error_SavingImage", "Error saving image:")} {ex.Message}",
                                    Localization.LocalizationHelper.GetString("Error_Title", "Error"),
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImageCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    string ext = System.IO.Path.GetExtension(files[0]).ToLowerInvariant();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                    {
                        e.Effects = DragDropEffects.Copy;
                        return;
                    }
                }
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void ImageCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[]? files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    string ext = System.IO.Path.GetExtension(files[0]).ToLowerInvariant();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                    {
                        LoadImage(files[0]);
                    }
                }
            }
        }

        private async void BatchInpaintButton_Click(object sender, EventArgs e)
        {
            BitmapSource? mask = CreateMaskBitmap();// RenderToBitmap(MaskCanvas);// CreateMaskBitmap();
            if (mask == null)
            {
                return;
            }
            var maskmat = mask.ToMat();
            // --- 1. 弹出文件选择框 ---
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                // 设置常见图像格式过滤器
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Files (*.png)|*.png|BMP Files (*.bmp)|*.bmp|GIF Files (*.gif)|*.gif|TIFF Files (*.tiff)|*.tiff|All Files (*.*)|*.*",
                Title = "Select Images for Batch Inpainting"
            };

            bool? result = openFileDialog.ShowDialog();

            // --- 2. 检查是否选择了文件 ---
            if (result != true || !openFileDialog.FileNames.Any())
            {
                MessageBox.Show("No images selected or operation cancelled.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return; // 用户未选择文件或取消
            }

            string[] selectedImagePaths = openFileDialog.FileNames;

            // --- 3. 弹出目录选择框 ---
            var folderDialog = new VistaFolderBrowserDialog // 使用 Ookii.Dialogs.Wpf
            {
                Description = "Select Directory to Save Processed Images",
                UseDescriptionForTitle = true, // 在标题栏显示 Description
                ShowNewFolderButton = true     // 允许用户创建新文件夹
            };

            bool? folderResult = folderDialog.ShowDialog(this); // 将当前窗口作为所有者

            if (folderResult != true || string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                MessageBox.Show("Save directory selection cancelled or invalid.", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);
                return; // 用户未选择目录或取消
            }

            string saveDirectory = folderDialog.SelectedPath;

            // --- 4. 准备异步处理和UI更新 ---
            BatchInpaintButton.IsEnabled = false; // 禁用按钮，防止重复点击
            progressBar.Maximum = selectedImagePaths.Length; // 设置进度条最大值
            progressBar.Value = 0;                         // 重置进度条
            progressBar.Visibility = Visibility.Visible;

            int totalItems = selectedImagePaths.Length;

            var progressReporter = new Progress<int>(reportedValue =>
            {
                progressBar.Value = reportedValue;
            });

            await Task.Run(() => ProcessItemsInBackground(maskmat,selectedImagePaths, saveDirectory,progressReporter))
                .ContinueWith(task => 
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BatchInpaintButton.IsEnabled = true;
                        if (task.IsFaulted)
                        {
                            MessageBox.Show($"Error: {task.Exception?.InnerExceptions.FirstOrDefault()?.Message}");
                            progressBar.Value = 0;
                        }
                        else if (task.IsCompletedSuccessfully)
                        {
                            MessageBox.Show("Processing Complete!");
                        }
                    });

                }); 

        }

        private void ProcessItemsInBackground(OpenCvSharp.Mat mask, string[] selectedImagePaths, string outputDirectory, IProgress<int> progress)
        {
            try
            {
                for (int i = 0; i < selectedImagePaths.Length; i++)
                {
                    process(mask, selectedImagePaths[i], outputDirectory);
                    progress?.Report(i + 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in background task: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Placeholder for your actual image processing logic.
        /// This should load the image, perform inpainting, and save the result.
        /// Make it async Task if the operations within are I/O bound or CPU intensive
        /// enough to benefit from yielding.
        /// </summary>
        /// <param name="inputFilePath">Full path to the input image.</param>
        /// <param name="outputDirectory">Directory where the processed image should be saved.</param>
        /// <param name="cancellationToken">Token to check for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private void process(OpenCvSharp.Mat mask, string inputFilePath, string outputDirectory)
        {
            var sourceimg = OpenCvSharp.Cv2.ImRead(inputFilePath);
            var inpaintimg = Processor.Process(sourceimg,mask);
            inpaintimg.SaveImage(System.IO.Path.Combine(outputDirectory,System.IO.Path.GetFileName(inputFilePath)));
        }

    }
}