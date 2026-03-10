-------- lệnh chạy ---------

dotnet build -t:Run -f net9.0-android

----------------------------------------------------------------------------

dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Maui.Controls.Maps
dotnet add package sqlite-net-pcl
dotnet add package SQLitePCLRaw.bundle_green

----------------------------------------------------------------------------
Bản đồ (Microsoft.Maui.Controls.Maps): Cần gói riêng vì nó nặng.

MVVM (CommunityToolkit.Mvvm): Cần gói riêng để code ngắn gọn.

GPS, Giọng nói (TTS), Rung: Đã có sẵn trong lõi của .NET MAUI rồi (nằm trong Microsoft.Maui.Devices và Microsoft.Maui.Media

--------web admin cách chạy và dùng-------
Hướng dẫn cách bạn chạy thử ngay bây giờ
Bạn mở Terminal (hoặc Command Prompt / PowerShell) lên và gõ lần lượt 3 lệnh này:

Lệnh 1: Di chuyển vào thư mục chứa code API
(Lưu ý: Đổi đường dẫn d:\FoodTour_System\FoodTour_WebAdmin.Api thành đường dẫn thực tế trên máy bạn)

Bash
cd d:\FoodTour_System\FoodTour_WebAdmin.Api
Lệnh 2: Cập nhật Database (Để nó tạo file foodtour.db và nạp 8 quán ăn mẫu vào)

Bash
dotnet ef database update
Lệnh 3: Chạy Project

Bash
dotnet run
Sau khi chạy lệnh 3, terminal sẽ hiện ra một đường link (thường là http://localhost:5154). Bạn copy link đó dán vào trình duyệt web (Chrome/Edge) là sẽ thấy tận mắt trang Web Admin cực xịn mà nó vừa làm cho bạn! Tương lai app MAUI của bạn cũng sẽ gọi API từ cái link http://localhost:5154/api/shops này để lấy danh sách quán ăn hiển thị lên điện thoại.


----------- các bảng trong hệ thống -----------------

<img width="637" height="612" alt="FoodTour_tables" src="https://github.com/user-attachments/assets/be1f32cc-ca4b-49e4-abdc-92df1768f596" />


