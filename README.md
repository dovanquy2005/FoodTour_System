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



# Navigate to the API directory
cd d:\FoodTour_System\FoodTour_WebAdmin.Api

# Update the database (if needed on another machine)
dotnet ef database update

# Run the project
dotnet run

