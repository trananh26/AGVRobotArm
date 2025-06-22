# Project Brief: AGV Robot Arm Control System

## Tổng quan dự án
Hệ thống điều khiển AGV (Automated Guided Vehicle) với cánh tay robot, được phát triển bằng C# WPF.

## Kiến trúc dự án
- **RobotControlSystem**: Ứng dụng WPF chính
- **ACS.BL**: Business Logic Layer
- **ACS.DL**: Data Layer  
- **ACS.Common**: Common utilities và models
- **Conf**: Cấu hình hệ thống (Nodes.txt, Edges.txt)

## Mục tiêu chính
- Điều khiển AGV tự động
- Quản lý cánh tay robot
- Giao diện người dùng thân thiện
- Xử lý real-time data
- Báo cáo và giám sát

## Công nghệ sử dụng
- C# WPF (.NET Framework)
- Material Design Themes
- LiveCharts cho biểu đồ
- Emgu CV cho xử lý ảnh
- AForge cho video capture
- Aspose.Cells cho Excel export 