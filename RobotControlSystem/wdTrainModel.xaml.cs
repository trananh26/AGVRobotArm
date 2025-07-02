using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;

namespace RobotControlSystem
{
    /// <summary>
    /// Interaction logic for wdTrainModel.xaml
    /// </summary>
    public partial class wdTrainModel : Window
    {
        private System.Windows.Point startPoint;
        private System.Windows.Point startPointUI;
        private bool isSelecting = false;
        private List<SampleInfo> sampleList = new List<SampleInfo>();
        private string templateFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplateData");
        
        // Thêm biến để lưu trữ vùng ROI hiện tại
        private Rect currentRegion;
        private bool hasSelectedRegion = false;
        private string currentComponentType = "";

        public class SampleInfo
        {
            public int STT { get; set; }
            public string ComponentType { get; set; }
            public int Count { get; set; }
        }

        public wdTrainModel(ImageSource sourceImage = null)
        {
            InitializeComponent();
            if (!System.IO.Directory.Exists(templateFolder)) System.IO.Directory.CreateDirectory(templateFolder);
            if (sourceImage != null)
            {
                if (sourceImage is BitmapSource bmpSrc)
                    imgTraining.Source = bmpSrc.Clone();
                else
                    imgTraining.Source = sourceImage;
            }
            //else
            //{
            //    MessageBox.Show("sourceImage NULL! Không có ảnh để huấn luyện.", "Debug");
            //}
            UpdateTemplateList();
            UpdateAdjustmentControls();
        }

        private System.Drawing.Bitmap ConvertBitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            byte[] pixels = new byte[height * stride];
            bitmapSource.CopyPixels(pixels, stride, 0);
            return new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                              System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(pixels, 0));
        }

        private BitmapSource ConvertToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var handle = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(handle);
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        private void UpdateTemplateList()
        {
            var files = System.IO.Directory.GetFiles(templateFolder, "*.png");
            var labelCount = files.Select(f => System.IO.Path.GetFileName(f).Split('_')[0])
                                  .GroupBy(l => l)
                                  .Select(g => new SampleInfo { ComponentType = g.Key, Count = g.Count() })
                                  .ToList();
            for (int i = 0; i < labelCount.Count; i++) labelCount[i].STT = i + 1;
            dgSamples.ItemsSource = labelCount;
        }

        private void btnOpenImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                Title = "Chọn ảnh"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.EndInit();
                    imgTraining.Source = bitmap;
                    selectionRect.Visibility = Visibility.Collapsed;
                    
                    // Reset trạng thái khi mở ảnh mới
                    hasSelectedRegion = false;
                    currentComponentType = "";
                    imgSelectedSample.Source = null;
                    
                    UpdateStatus();
                    UpdateTemplateList();
                    UpdateAdjustmentControls();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi mở ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private System.Windows.Point GetImageCoordinates(System.Windows.Point mousePosition)
        {
            if (imgTraining.Source == null) return mousePosition;
            double imgW = imgTraining.Source.Width;
            double imgH = imgTraining.Source.Height;
            double ctrlW = imgTraining.ActualWidth;
            double ctrlH = imgTraining.ActualHeight;
            double ratio = Math.Min(ctrlW / imgW, ctrlH / imgH);
            double displayW = imgW * ratio;
            double displayH = imgH * ratio;
            double offsetX = (ctrlW - displayW) / 2;
            double offsetY = (ctrlH - displayH) / 2;
            double x = (mousePosition.X - offsetX) / ratio;
            double y = (mousePosition.Y - offsetY) / ratio;
            return new System.Windows.Point(Math.Max(0, Math.Min(x, imgW)), Math.Max(0, Math.Min(y, imgH)));
        }

        private void imgTraining_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (imgTraining.Source == null) return;
            startPointUI = e.GetPosition(imageContainer);
            startPoint = GetImageCoordinates(startPointUI); 
            isSelecting = true;
            selectionRect.Visibility = Visibility.Visible;
            selectionRect.Margin = new Thickness(startPointUI.X, startPointUI.Y, 0, 0);
            selectionRect.Width = 0;
            selectionRect.Height = 0;
            this.PreviewMouseRightButtonUp += Window_PreviewMouseRightButtonUp;
        }

        private void imgTraining_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isSelecting) return;
            System.Windows.Point currentUI = e.GetPosition(imageContainer);
            selectionRect.Margin = new Thickness(
                Math.Min(currentUI.X, startPointUI.X),
                Math.Min(currentUI.Y, startPointUI.Y),
                0, 0);
            selectionRect.Width = Math.Abs(currentUI.X - startPointUI.X);
            selectionRect.Height = Math.Abs(currentUI.Y - startPointUI.Y);
        }

        private void Window_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isSelecting) return;
            isSelecting = false;
            System.Windows.Point endPoint = GetImageCoordinates(e.GetPosition(imageContainer));
            currentRegion = new Rect(
                Math.Min(startPoint.X, endPoint.X),
                Math.Min(startPoint.Y, endPoint.Y),
                Math.Abs(endPoint.X - startPoint.X),
                Math.Abs(endPoint.Y - startPoint.Y)
            );
            
            if (currentRegion.Width > 10 && currentRegion.Height > 10)
            {
                var selectedItem = cboComponentType.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    currentComponentType = selectedItem.Content.ToString();
                    hasSelectedRegion = true;
                    UpdateAdjustmentControls();
                    UpdateSelectionRect();
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn loại linh kiện trước khi chọn vùng", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    selectionRect.Visibility = Visibility.Collapsed;
                    hasSelectedRegion = false;
                    currentComponentType = "";
                    UpdateAdjustmentControls();
                }
            }
            else
            {
                selectionRect.Visibility = Visibility.Collapsed;
                hasSelectedRegion = false;
                currentComponentType = "";
                UpdateAdjustmentControls();
            }
            this.PreviewMouseRightButtonUp -= Window_PreviewMouseRightButtonUp;
        }

        private void SaveTemplateImage(Rect region, string savePath)
        {
            if (imgTraining.Source is BitmapSource bmpSrc)
            {
                using (System.Drawing.Bitmap originalBitmap = ConvertBitmapSourceToBitmap(bmpSrc))
                {
                    System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(
                        (int)Math.Max(0, Math.Min(region.X, originalBitmap.Width - 1)),
                        (int)Math.Max(0, Math.Min(region.Y, originalBitmap.Height - 1)),
                        (int)Math.Min(region.Width, originalBitmap.Width - (int)Math.Max(0, Math.Min(region.X, originalBitmap.Width - 1))),
                        (int)Math.Min(region.Height, originalBitmap.Height - (int)Math.Max(0, Math.Min(region.Y, originalBitmap.Height - 1)))
                    );

                    if (cropRect.Width <= 0 || cropRect.Height <= 0) return;

                    using (System.Drawing.Bitmap croppedBitmap = originalBitmap.Clone(cropRect, originalBitmap.PixelFormat))
                    {
                        croppedBitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }

        private void imgTraining_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Không cần xử lý ở đây nữa vì đã chuyển sang Window_PreviewMouseRightButtonUp
        }

        private void UpdateStatus()
        {
            txtStatus.Text = $"Đã chọn {templateFolder} vùng";
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Các mẫu đã được lưu vào thư mục TemplateData", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnDeleteSample_Click(object sender, RoutedEventArgs e)
        {
            if (dgSamples.SelectedItem is SampleInfo selected)
            {
                string label = selected.ComponentType;
                var files = System.IO.Directory.GetFiles(templateFolder, $"{label}_*.png");
                if (files.Length == 0)
                {
                    MessageBox.Show($"Không tìm thấy mẫu nào để xóa cho '{label}'", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (MessageBox.Show($"Bạn có chắc muốn xóa tất cả mẫu của '{label}'?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    foreach (var file in files)
                    {
                        try { System.IO.File.Delete(file); } catch { }
                    }
                    MessageBox.Show($"Đã xóa tất cả mẫu của '{label}'", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateTemplateList();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn loại mẫu muốn xóa trong bảng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateAdjustmentControls()
        {
            bool hasRegion = hasSelectedRegion && currentRegion.Width > 10 && currentRegion.Height > 10;
            btnDecreaseX.IsEnabled = hasRegion;
            btnIncreaseX.IsEnabled = hasRegion;
            btnDecreaseY.IsEnabled = hasRegion;
            btnIncreaseY.IsEnabled = hasRegion;
            btnPreview.IsEnabled = hasRegion && !string.IsNullOrEmpty(currentComponentType);
            btnSaveSample.IsEnabled = hasRegion && !string.IsNullOrEmpty(currentComponentType);
            
            if (hasRegion)
            {
                txtRegionX.Text = ((int)currentRegion.X).ToString();
                txtRegionY.Text = ((int)currentRegion.Y).ToString();
                txtRegionSize.Text = $"{(int)currentRegion.Width} x {(int)currentRegion.Height}";
            }
            else
            {
                txtRegionX.Text = "0";
                txtRegionY.Text = "0";
                txtRegionSize.Text = "0 x 0";
            }
        }

        private int GetStepPixel()
        {
            int step = 1;
            if (!int.TryParse(txtStepPixel.Text, out step) || step < 1)
                step = 1;
            return step;
        }

        private void btnDecreaseX_Click(object sender, RoutedEventArgs e)
        {
            if (hasSelectedRegion)
            {
                int step = GetStepPixel();
                currentRegion.X = Math.Max(0, currentRegion.X - step);
                UpdateSelectionRect();
                UpdateAdjustmentControls();
            }
        }

        private void btnIncreaseX_Click(object sender, RoutedEventArgs e)
        {
            if (hasSelectedRegion && imgTraining.Source != null)
            {
                int step = GetStepPixel();
                double maxX = imgTraining.Source.Width - currentRegion.Width;
                currentRegion.X = Math.Min(maxX, currentRegion.X + step);
                UpdateSelectionRect();
                UpdateAdjustmentControls();
            }
        }

        private void btnDecreaseY_Click(object sender, RoutedEventArgs e)
        {
            if (hasSelectedRegion)
            {
                int step = GetStepPixel();
                currentRegion.Y = Math.Max(0, currentRegion.Y - step);
                UpdateSelectionRect();
                UpdateAdjustmentControls();
            }
        }

        private void btnIncreaseY_Click(object sender, RoutedEventArgs e)
        {
            if (hasSelectedRegion && imgTraining.Source != null)
            {
                int step = GetStepPixel();
                double maxY = imgTraining.Source.Height - currentRegion.Height;
                currentRegion.Y = Math.Min(maxY, currentRegion.Y + step);
                UpdateSelectionRect();
                UpdateAdjustmentControls();
            }
        }

        private void UpdateSelectionRect()
        {
            if (!hasSelectedRegion || imgTraining.Source == null) return;
            
            double imgW = imgTraining.Source.Width;
            double imgH = imgTraining.Source.Height;
            double ctrlW = imgTraining.ActualWidth;
            double ctrlH = imgTraining.ActualHeight;
            double ratio = Math.Min(ctrlW / imgW, ctrlH / imgH);
            double displayW = imgW * ratio;
            double displayH = imgH * ratio;
            double offsetX = (ctrlW - displayW) / 2;
            double offsetY = (ctrlH - displayH) / 2;
            
            double rectX = offsetX + currentRegion.X * ratio;
            double rectY = offsetY + currentRegion.Y * ratio;
            double rectWidth = currentRegion.Width * ratio;
            double rectHeight = currentRegion.Height * ratio;
            
            selectionRect.Margin = new Thickness(rectX, rectY, 0, 0);
            selectionRect.Width = rectWidth;
            selectionRect.Height = rectHeight;
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            if (hasSelectedRegion && !string.IsNullOrEmpty(currentComponentType))
            {
                PreviewTemplateImage(currentRegion);
            }
        }

        private void btnSaveSample_Click(object sender, RoutedEventArgs e)
        {
            if (hasSelectedRegion && !string.IsNullOrEmpty(currentComponentType))
            {
                string fileName = $"{currentComponentType}_{DateTime.Now:yyyyMMddHHmmssfff}.png";
                string savePath = System.IO.Path.Combine(templateFolder, fileName);
                SaveTemplateImage(currentRegion, savePath);
                MessageBox.Show($"Đã lưu template: {fileName}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateTemplateList();
                
                // Reset sau khi lưu
                hasSelectedRegion = false;
                currentComponentType = "";
                selectionRect.Visibility = Visibility.Collapsed;
                UpdateAdjustmentControls();
            }
        }

        private void PreviewTemplateImage(Rect region)
        {
            if (imgTraining.Source is BitmapSource bmpSrc)
            {
                using (System.Drawing.Bitmap originalBitmap = ConvertBitmapSourceToBitmap(bmpSrc))
                {
                    System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(
                        (int)Math.Max(0, Math.Min(region.X, originalBitmap.Width - 1)),
                        (int)Math.Max(0, Math.Min(region.Y, originalBitmap.Height - 1)),
                        (int)Math.Min(region.Width, originalBitmap.Width - (int)Math.Max(0, Math.Min(region.X, originalBitmap.Width - 1))),
                        (int)Math.Min(region.Height, originalBitmap.Height - (int)Math.Max(0, Math.Min(region.Y, originalBitmap.Height - 1)))
                    );

                    if (cropRect.Width <= 0 || cropRect.Height <= 0) return;

                    using (System.Drawing.Bitmap croppedBitmap = originalBitmap.Clone(cropRect, originalBitmap.PixelFormat))
                    {
                        imgSelectedSample.Source = ConvertToBitmapSource(croppedBitmap);
                    }
                }
            }
        }

        private void cboComponentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (hasSelectedRegion)
            {
                var selectedItem = cboComponentType.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    currentComponentType = selectedItem.Content.ToString();
                    UpdateAdjustmentControls();
                }
            }
        }
    }
}
