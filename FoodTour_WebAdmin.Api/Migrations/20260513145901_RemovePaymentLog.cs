using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FoodTour_WebAdmin.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: false),
                    PaymentType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLogs_CreatedAt",
                table: "PaymentLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLogs_DeviceId",
                table: "PaymentLogs",
                column: "DeviceId");
        }
    }
}
