using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncHardwareIDChanges : Migration
    {
        /// <inheritdoc />
        // Migration trắng: Các cột đã được thêm thủ công bằng SQL trên Database.
        // File này chỉ phục vụ đồng bộ ModelSnapshot của EF Core với schema hiện tại.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Không thực thi lệnh nào — cột đã tồn tại trong DB
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không thực thi lệnh nào — giữ nguyên schema hiện tại
        }
    }
}
