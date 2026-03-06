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

            // 1. Đường dẫn Database
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "FoodTour.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            // 2. Tạo bảng ShopModel (thay thế PoiModel) và DishModel
            await _database.CreateTableAsync<ShopModel>();
            await _database.CreateTableAsync<DishModel>();

            // 3. Kiểm tra và Seed dữ liệu chuẩn đường Vĩnh Khánh
            int count = await _database.Table<ShopModel>().CountAsync();
            if (count == 0)
            {
                await SeedInitialData();
            }
        }

        private async Task SeedInitialData()
        {
            // Tạo ID cố định để liên kết Shop và Dish
            var shop1Id = Guid.NewGuid().ToString();
            var shop2Id = Guid.NewGuid().ToString();
            var shop3Id = Guid.NewGuid().ToString();

            // --- SEED SHOPS (3 quán tiêu biểu trên đường Vĩnh Khánh) ---
            var shops = new List<ShopModel>
            {
                new ShopModel 
                { 
                    Id = shop1Id, 
                    Name = "Ốc Oanh 1", 
                    Address = "534 Vĩnh Khánh, Quận 4", 
                    Latitude = 10.75895, // Cập nhật: Sát mép đường Phường 8
                    Longitude = 106.70945, 
                    Description = "Quán ốc nổi tiếng nhất nhì Sài Gòn, nằm trong Michelin Guide 2024.", 
                    Rating = 4.8, 
                    ImageUrl = "shop_01.jpg" 
                },
                new ShopModel 
                { 
                    Id = shop2Id, 
                    Name = "Ốc Đào II", 
                    Address = "232/123 Vĩnh Khánh, Quận 4", 
                    Latitude = 10.76042, // Cập nhật: Vị trí mặt tiền đường
                    Longitude = 106.70589, 
                    Description = "Chi nhánh của thương hiệu ốc Đào nổi tiếng, nước sốt đậm đà, không gian rộng rãi.", 
                    Rating = 4.5, 
                    ImageUrl = "shop_02.jpg" 
                },
                new ShopModel 
                { 
                    Id = shop3Id, 
                    Name = "Ốc Vũ", 
                    Address = "395 Vĩnh Khánh, Quận 4", 
                    Latitude = 10.75933, // Cập nhật: Ngay đoạn giữa khu phố ốc
                    Longitude = 106.70814, 
                    Description = "Quán ốc bình dân với không khí nhộn nhịp đặc trưng của phố ẩm thực.", 
                    Rating = 4.2, 
                    ImageUrl = "shop_03.jpg" 
                }
            };
            await _database!.InsertAllAsync(shops);

            // --- SEED DISHES (Mỗi quán 5 món ăn) ---
            var dishes = new List<DishModel>
            {
                // Món của Ốc Oanh (shop_01)
                new DishModel { ShopId = shop1Id, Name = "Càng ghẹ rang muối", Price = 120000, ImageUrl = "dish_01.jpg" },
                new DishModel { ShopId = shop1Id, Name = "Hào nướng phô mai", Price = 50000, ImageUrl = "dish_02.jpg" },
                new DishModel { ShopId = shop1Id, Name = "Nghêu hấp Thái", Price = 120000, ImageUrl = "dish_03.jpg" },
                new DishModel { ShopId = shop1Id, Name = "Bạch tuộc nướng muối ớt", Price = 160000, ImageUrl = "dish_04.jpg" },
                new DishModel { ShopId = shop1Id, Name = "Chem chép xào tỏi", Price = 120000, ImageUrl = "dish_05.jpg" },

                // Món của Ốc Đào II (shop_02)
                new DishModel { ShopId = shop2Id, Name = "Ốc hương rang muối ớt", Price = 110000, ImageUrl = "dish_01.jpg" },
                new DishModel { ShopId = shop2Id, Name = "Ốc hương xào bơ", Price = 110000, ImageUrl = "dish_02.jpg" },
                new DishModel { ShopId = shop2Id, Name = "Sò huyết xào tỏi", Price = 110000, ImageUrl = "dish_03.jpg" },
                new DishModel { ShopId = shop2Id, Name = "Nghêu hấp sả", Price = 80000, ImageUrl = "dish_04.jpg" },
                new DishModel { ShopId = shop2Id, Name = "Ốc mỡ xào me", Price = 110000, ImageUrl = "dish_05.jpg" },

                // Món của Ốc Vũ (shop_03)
                new DishModel { ShopId = shop3Id, Name = "Lưỡi vịt Sapo", Price = 100000, ImageUrl = "dish_01.jpg" },
                new DishModel { ShopId = shop3Id, Name = "Ốc tỏi nướng mắm", Price = 60000, ImageUrl = "dish_02.jpg" },
                new DishModel { ShopId = shop3Id, Name = "Răng mực rang muối", Price = 60000, ImageUrl = "dish_03.jpg" },
                new DishModel { ShopId = shop3Id, Name = "Bò lúc lắc", Price = 110000, ImageUrl = "dish_04.jpg" },
                new DishModel { ShopId = shop3Id, Name = "Khoai tây chiên", Price = 50000, ImageUrl = "dish_05.jpg" }
            };
            await _database.InsertAllAsync(dishes);
        }

        // --- CÁC HÀM CRUD SHOP ---
        public async Task<List<ShopModel>> GetShopsAsync()
        {
            await Init();
            return await _database!.Table<ShopModel>().ToListAsync();
        }

        public async Task<ShopModel> GetShopAsync(string id)
        {
            await Init();
            return await _database!.Table<ShopModel>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> AddShopAsync(ShopModel shop)
        {
            await Init();
            if (string.IsNullOrEmpty(shop.Id)) shop.Id = Guid.NewGuid().ToString();
            return await _database!.InsertAsync(shop);
        }

        // --- CÁC HÀM TRUY VẤN MÓN ĂN ---
        public async Task<List<DishModel>> GetDishesByShopAsync(string shopId)
        {
            await Init();
            return await _database!.Table<DishModel>().Where(d => d.ShopId == shopId).ToListAsync();
        }

        public async Task<int> DeleteShopAsync(ShopModel shop)
        {
            await Init();
            return await _database!.DeleteAsync(shop);
        }
    }
}