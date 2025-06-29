# Hướng dẫn sử dụng tính năng ROI (Region of Interest)

## Tổng quan
Tính năng ROI cho phép bạn chọn một vùng cụ thể trên ảnh để tập trung vào việc nhận dạng đối tượng, giúp cải thiện độ chính xác và tốc độ xử lý.

## Cách sử dụng

### 1. Mở ảnh
- Nhấn nút **"Chọn ảnh"** để mở file ảnh từ máy tính
- Hệ thống sẽ hiển thị ảnh trong vùng xử lý

### 2. Chọn vùng ROI
- **Kéo chuột**: Nhấn và giữ chuột trái, kéo để chọn vùng quan tâm
- **Hiển thị**: Vùng được chọn sẽ hiển thị bằng khung màu đỏ
- **Thông tin**: Kích thước vùng sẽ hiển thị ở góc trên bên trái

### 3. Nhận dạng tự động
- Khi chọn xong ROI, hệ thống sẽ tự động thực hiện nhận dạng
- Kết quả sẽ hiển thị trong bảng bên dưới
- Trạng thái sẽ được cập nhật (OK/NG)

### 4. Điều khiển ROI
- **Xóa ROI**: Nhấn nút **"Xóa ROI"** để xóa vùng đã chọn
- **Hủy chọn**: Nhấn phím **ESC** để hủy chọn đang thực hiện
- **Chọn lại**: Có thể chọn vùng mới bất cứ lúc nào

## Tính năng nâng cao

### Huấn luyện với ROI
- Chọn vùng ROI chứa đối tượng cần huấn luyện
- Nhấn nút **"Huấn luyện"** để tạo template từ vùng ROI
- Template sẽ được lưu với tên và thời gian

### Lưu ảnh ROI
- Vùng ROI có thể được lưu thành file ảnh riêng
- Sử dụng cho việc phân tích hoặc tạo template

### Điều chỉnh tọa độ
- Hệ thống tự động điều chỉnh tọa độ về ảnh gốc
- Kết quả nhận dạng hiển thị vị trí chính xác trên ảnh gốc

## Lưu ý quan trọng

### Kích thước tối thiểu
- ROI phải có kích thước tối thiểu 10x10 pixel
- Vùng quá nhỏ sẽ bị bỏ qua

### Độ chính xác
- Chọn vùng càng chính xác, kết quả nhận dạng càng tốt
- Tránh chọn vùng có nhiều đối tượng khác nhau

### Performance
- ROI giúp tăng tốc độ xử lý với ảnh lớn
- Tập trung vào vùng quan tâm thay vì toàn bộ ảnh

## Xử lý lỗi

### Không có template
- Đảm bảo đã có dữ liệu template trong thư mục TemplateData
- Sử dụng tính năng huấn luyện để tạo template

### Lỗi tọa độ
- Nếu tọa độ không chính xác, thử chọn lại vùng ROI
- Đảm bảo ảnh được hiển thị đúng tỷ lệ

### Lỗi lưu file
- Kiểm tra quyền ghi file trong thư mục đích
- Đảm bảo đủ dung lượng ổ đĩa

## Phím tắt
- **ESC**: Hủy chọn ROI đang thực hiện
- **Chuột trái**: Chọn vùng ROI
- **Chuột phải**: Không sử dụng (để tránh xung đột)

## Ví dụ sử dụng

### Trường hợp 1: Nhận dạng linh kiện
1. Mở ảnh chứa linh kiện
2. Chọn vùng ROI bao quanh linh kiện cần nhận dạng
3. Hệ thống tự động nhận dạng và hiển thị kết quả
4. Nếu kết quả chính xác, nhấn "Cho Pass"
5. Nếu không chính xác, nhấn "Cho Fail"

### Trường hợp 2: Huấn luyện template mới
1. Mở ảnh chứa đối tượng cần huấn luyện
2. Chọn vùng ROI chính xác bao quanh đối tượng
3. Nhấn "Huấn luyện" để tạo template
4. Template sẽ được lưu và có thể sử dụng cho lần sau 