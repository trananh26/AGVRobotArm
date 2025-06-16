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

namespace AGVControlSystem
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
                imgTraining.Source = sourceImage;
            }
            UpdateTemplateList();
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
                    UpdateStatus();
                    UpdateTemplateList();
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
            Rect selectedRegion = new Rect(
                Math.Min(startPoint.X, endPoint.X),
                Math.Min(startPoint.Y, endPoint.Y),
                Math.Abs(endPoint.X - startPoint.X),
                Math.Abs(endPoint.Y - startPoint.Y)
            );
            if (selectedRegion.Width > 10 && selectedRegion.Height > 10)
            {
                var selectedItem = cboComponentType.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string label = selectedItem.Content.ToString();
                    string fileName = $"{label}_{DateTime.Now:yyyyMMddHHmmssfff}.png";
                    string savePath = System.IO.Path.Combine(templateFolder, fileName);
                    SaveTemplateImage(selectedRegion, savePath);
                    MessageBox.Show($"Đã lưu template: {fileName}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateTemplateList();
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn loại linh kiện trước khi chọn vùng", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    selectionRect.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                selectionRect.Visibility = Visibility.Collapsed;
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
                        //Mếu lỗi kích thước train thì thăng giảm ở đây
                        (int)Math.Max(0, Math.Min(region.X-151, originalBitmap.Width - 1)),
                        (int)Math.Max(0, Math.Min(region.Y, originalBitmap.Height - 1)),
                        (int)Math.Min(region.Width, originalBitmap.Width - (int)Math.Max(0, Math.Min(region.X, originalBitmap.Width - 1))),
                        (int)Math.Min(region.Height, originalBitmap.Height - (int)Math.Max(0, Math.Min(region.Y, originalBitmap.Height - 1)))
                    );

                    if (cropRect.Width <= 0 || cropRect.Height <= 0) return;

                    using (System.Drawing.Bitmap croppedBitmap = originalBitmap.Clone(cropRect, originalBitmap.PixelFormat))
                    {
                        croppedBitmap.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
                        imgSelectedSample.Source = ConvertToBitmapSource(croppedBitmap);
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
    }
}
