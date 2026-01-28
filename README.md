-------- lệnh chạy ---------

dotnet build -t:Run -f net9.0-android

----------------------------------------------------------------------------

dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Maui.Controls.Maps

----------------------------------------------------------------------------
Bản đồ (Microsoft.Maui.Controls.Maps): Cần gói riêng vì nó nặng.

MVVM (CommunityToolkit.Mvvm): Cần gói riêng để code ngắn gọn.

GPS, Giọng nói (TTS), Rung: Đã có sẵn trong lõi của .NET MAUI rồi (nằm trong Microsoft.Maui.Devices và Microsoft.Maui.Media
