# FoodTour - Hệ thống Audio Guide Phố Ẩm Thực Vĩnh Khánh

Dự án phát triển hệ thống hỗ trợ du lịch thông minh (Audio Guide) tập trung vào trải nghiệm người dùng đa ngôn ngữ và chế độ Offline/Online tại phố ẩm thực Vĩnh Khánh.

---

## 🛠 Bộ Công Cụ & Thư Viện (Tech Stack)

Hệ thống được xây dựng trên nền tảng **.NET 9**, chia làm 2 thành phần chính:

### 1. Web Admin & API (Backend)
* **Lưu trữ Cloud:** PostgreSQL lưu trữ trên điện toán đám mây Supabase.
* **ORM:** Entity Framework Core.
* **Giao diện quản trị:** Blazor Server + MudBlazor.

### 2. Mobile App (Frontend)
* 📍 **Bản đồ:** `Microsoft.Maui.Controls.Maps` (Tích hợp Google Maps chuyên sâu).
* 🏗 **Mô hình:** `CommunityToolkit.Mvvm` (Giúp code sạch, dễ bảo trì theo chuẩn MVVM).
* 💾 **Lưu trữ Offline:** `sqlite-net-pcl` & `SQLitePCLRaw.bundle_green` (Quản lý database SQLite).
* ⚡ **Tính năng lõi:** Tích hợp sẵn GPS (Geolocator), Giọng nói (Text-to-Speech), và Rung (Vibration).

---

## 🏗 Cấu Trúc Database (Database Schema)

Hệ thống sử dụng mô hình **Localization Data** (Tách biệt dữ liệu vật lý và dữ liệu dịch thuật) để hỗ trợ 5 ngôn ngữ: **Việt, Anh, Nhật, Nga, Trung**.

| Thực thể | Vai trò |
| :--- | :--- |
| **ShopModel / DishModel** | Lưu trữ ID, Tọa độ, Giá cả, Hình ảnh (Dữ liệu cố định). |
| **ShopTranslation / DishTranslation** | Lưu trữ Tên, Địa chỉ, Script thuyết minh (Dữ liệu theo ngôn ngữ). |

---

## 🗄️ Cấu hình Database Backend (PostgreSQL / Supabase)

Sử dụng các lệnh .NET CLI sau trong thư mục `FoodTour_WebAdmin.Api` để quản lý database:

**1. Cài đặt Provider cho PostgreSQL**
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
---

## 🚀 Hướng Dẫn Khởi Chạy (Quick Start)

### Khởi động Web Admin & API
Mở Terminal, di chuyển vào thư mục dự án và thực thi các lệnh sau:

```bash
# Di chuyển tới thư mục API
cd d:\FoodTour_System\FoodTour_WebAdmin.Api

# Cập nhật cấu trúc lên Database Cloud (Supabase)
dotnet ef database update

# Chạy Server
dotnet run

# Dừng server
taskkill /IM "FoodTour_WebAdmin.Api.exe" /F
```

> 💡 **Lưu ý:** Cổng mặc định là `http://localhost:5154` - Bạn có thể truy cập `http://localhost:5154/api/shops` để kiểm tra dữ liệu JSON. Hoặc kiểm tra trên môi trường Production tại link Render.

### Chạy Ứng Dụng Mobile (Android)
Đảm bảo bạn đã mở máy ảo Android (Emulator) trước khi chạy lệnh:
```bash
dotnet build -t:Run -f net9.0-android
```

---

## 📦 Các Gói Cần Cài Đặt (Dependencies cho Mobile)

Nếu bạn thiết lập dự án từ đầu, hãy chạy các lệnh sau trong thư mục dự án Mobile:

```bash
dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Maui.Controls.Maps
dotnet add package sqlite-net-pcl
dotnet add package SQLitePCLRaw.bundle_green
```

---

## 📝 Ghi Chú Phát Triển

* **Database:** Sử dụng `RunInTransaction` khi đồng bộ dữ liệu trên Mobile để đảm bảo an toàn.
* **Localization:** UI tĩnh sử dụng file `.json`, Dữ liệu động sử dụng các bảng Translation trong SQLite.
* **Audio:** Sử dụng Text-to-Speech để đọc kịch bản từ cột `Description` trong bảng dịch.

---
*© 2026 FoodTour Project - Sai Gon University (SGU)*
```
