using SQLite;
using FoodTour.Mobile.Models;

namespace FoodTour.Mobile.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;

        async Task Init()
        {
            if (_database is not null)
                return;

            // 1. Log đường dẫn file
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "FoodTour.db3");
            Console.WriteLine($"👉 [1] DB PATH: {dbPath}");

            _database = new SQLiteAsyncConnection(dbPath);

            // 2. Tạo bảng và Log kết quả (result = 1: Tạo mới, result = 0: Đã có)
            var result = await _database!.CreateTableAsync<PoiModel>();
            Console.WriteLine($"👉 [2] Kết quả tạo bảng PoiModel: {result}");

            // 3. Kiểm tra dữ liệu cũ
            int count = await _database!.Table<PoiModel>().CountAsync();
            Console.WriteLine($"👉 [3] Số lượng bản ghi hiện tại: {count}");

            // 4. Seed dữ liệu nếu rỗng
            if (count == 0)
            {
                Console.WriteLine("👉 [4] Database đang rỗng -> Đang thêm dữ liệu mẫu...");

                var rowsAdded = await _database!.InsertAllAsync(new List<PoiModel>
                {
                    new PoiModel { Id="1", Name = "Phở Thìn Lò Đúc", Address="13 Lò Đúc", Latitude = 21.0185, Longitude = 105.8550, Description = "Phở tái lăn trứ danh." },
                    new PoiModel { Id="2", Name = "Bún Chả Hương Liên", Address="24 Lê Văn Hưu", Latitude = 21.0180, Longitude = 105.8520, Description = "Quán Obama đã ăn." },
                    new PoiModel { Id="3", Name = "Cafe Giảng", Address="39 Nguyễn Hữu Huân", Latitude = 21.0343, Longitude = 105.8519, Description = "Cafe trứng nổi tiếng." }
                });

                Console.WriteLine($"👉 [5] Đã thêm xong! Tổng số dòng insert được: {rowsAdded}");
            }
            else
            {
                Console.WriteLine("👉 [4] Database đã có dữ liệu -> Bỏ qua bước thêm mẫu.");
            }
        }

        // --- CÁC HÀM CRUD ---

        public async Task<List<PoiModel>> GetPoisAsync()
        {
            await Init();
            var list = await _database!.Table<PoiModel>().ToListAsync();
            Console.WriteLine($"👉 [GetPoisAsync] Lấy ra được {list.Count} địa điểm.");
            return list;
        }

        public async Task<PoiModel> GetPoiAsync(string id)
        {
            await Init();
            return await _database!.Table<PoiModel>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> AddPoiAsync(PoiModel poi)
        {
            await Init();
            // Tự sinh ID nếu thiếu
            if (string.IsNullOrEmpty(poi.Id))
            {
                poi.Id = Guid.NewGuid().ToString();
            }

            var result = await _database!.InsertAsync(poi);
            Console.WriteLine($"👉 [AddPoiAsync] Đã thêm địa điểm mới: {poi.Name}. Kết quả: {result}");
            return result;
        }

        public async Task<int> UpdatePoiAsync(PoiModel poi)
        {
            await Init();
            return await _database!.UpdateAsync(poi);
        }

        public async Task<int> DeletePoiAsync(PoiModel poi)
        {
            await Init();
            return await _database!.DeleteAsync(poi);
        }
    }
}