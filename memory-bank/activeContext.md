# Active Context: ROI Implementation for Image Processing

## Mục tiêu hiện tại
Đã hoàn thành việc thêm tính năng ROI (Region of Interest) cho hệ thống xử lý ảnh

## Tính năng ROI đã triển khai
- ✅ Tạo ROISelector UserControl với khả năng chọn vùng bằng chuột
- ✅ Tích hợp ROISelector vào MainWindow thay thế Image đơn giản
- ✅ Thêm xử lý sự kiện ROI được chọn
- ✅ Cập nhật DetectComponents để hỗ trợ ROI
- ✅ Thêm button "Xóa ROI" để reset vùng chọn
- ✅ Cải thiện UX với thông tin ROI và phím tắt ESC

## Các tính năng chính của ROI
1. **Chọn vùng bằng chuột**: Kéo thả để chọn vùng ROI
2. **Hiển thị thông tin**: Hiển thị kích thước vùng đã chọn
3. **Phím tắt**: ESC để hủy chọn ROI
4. **Tự động nhận dạng**: Khi chọn ROI, tự động thực hiện nhận dạng
5. **Điều chỉnh tọa độ**: Tự động điều chỉnh tọa độ về ảnh gốc
6. **Lưu ảnh ROI**: Có thể lưu ảnh đã cắt theo ROI

## Cải tiến performance
- Tối ưu hóa xử lý ảnh chỉ trên vùng ROI thay vì toàn bộ ảnh
- Giảm thời gian nhận dạng khi ảnh lớn
- Cải thiện độ chính xác bằng cách tập trung vào vùng quan tâm

## Tiếp theo
- ⏳ Thêm tính năng chụp ảnh từ camera
- ⏳ Cải thiện giao diện người dùng
- ⏳ Thêm các preset ROI cho các loại sản phẩm khác nhau 