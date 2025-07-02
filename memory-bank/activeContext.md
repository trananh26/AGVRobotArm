# Active Context: ROI Implementation for Image Processing

## Mục tiêu hiện tại
Đã hoàn thành việc thêm tính năng ROI (Region of Interest) cho hệ thống xử lý ảnh và cải tiến giao diện huấn luyện mô hình

## Tính năng ROI đã triển khai
- ✅ Tạo ROISelector UserControl với khả năng chọn vùng bằng chuột
- ✅ Tích hợp ROISelector vào MainWindow thay thế Image đơn giản
- ✅ Thêm xử lý sự kiện ROI được chọn
- ✅ Cập nhật DetectComponents để hỗ trợ ROI
- ✅ Thêm button "Xóa ROI" để reset vùng chọn
- ✅ Cải thiện UX với thông tin ROI và phím tắt ESC

## Tính năng mới cho wdTrainModel (Huấn luyện mô hình)
- ✅ **Xem trước ảnh mẫu**: Thêm button "Xem trước" để xem ảnh đã cắt trước khi lưu
- ✅ **Điều chỉnh vị trí ROI**: Thêm controls để tăng/giảm region.X và region.Y
- ✅ **Hiển thị thông tin ROI**: Hiển thị tọa độ X, Y và kích thước vùng đã chọn
- ✅ **Lưu có kiểm soát**: Thay thế lưu tự động bằng button "Lưu mẫu" riêng biệt
- ✅ **Cải thiện UX**: Panel điều chỉnh vị trí với giao diện trực quan

## Các tính năng chính của ROI
1. **Chọn vùng bằng chuột**: Kéo thả để chọn vùng ROI
2. **Hiển thị thông tin**: Hiển thị kích thước vùng đã chọn
3. **Phím tắt**: ESC để hủy chọn ROI
4. **Tự động nhận dạng**: Khi chọn ROI, tự động thực hiện nhận dạng
5. **Điều chỉnh tọa độ**: Tự động điều chỉnh tọa độ về ảnh gốc
6. **Lưu ảnh ROI**: Có thể lưu ảnh đã cắt theo ROI

## Workflow mới cho huấn luyện mô hình
1. **Mở ảnh**: Chọn ảnh cần huấn luyện
2. **Chọn loại linh kiện**: Chọn từ ComboBox
3. **Chọn vùng ROI**: Kéo chuột phải để chọn vùng
4. **Điều chỉnh vị trí**: Sử dụng nút +/- để điều chỉnh X, Y
5. **Xem trước**: Nhấn "Xem trước" để xem ảnh đã cắt
6. **Lưu mẫu**: Nhấn "Lưu mẫu" khi hài lòng với kết quả

## Cải tiến performance
- Tối ưu hóa xử lý ảnh chỉ trên vùng ROI thay vì toàn bộ ảnh
- Giảm thời gian nhận dạng khi ảnh lớn
- Cải thiện độ chính xác bằng cách tập trung vào vùng quan tâm

## Tiếp theo
- ⏳ Thêm tính năng chụp ảnh từ camera
- ⏳ Cải thiện giao diện người dùng
- ⏳ Thêm các preset ROI cho các loại sản phẩm khác nhau 