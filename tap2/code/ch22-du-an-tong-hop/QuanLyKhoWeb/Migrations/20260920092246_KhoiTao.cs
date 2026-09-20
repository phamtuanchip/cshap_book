using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyKhoWeb.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "san_pham",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ma = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Nhom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DonGia = table.Column<double>(type: "REAL", nullable: false),
                    TonKho = table.Column<int>(type: "INTEGER", nullable: false),
                    MucCanhBao = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_san_pham", x => x.Id);
                    table.CheckConstraint("ck_gia_khong_am", "DonGia >= 0");
                    table.CheckConstraint("ck_ton_khong_am", "TonKho >= 0");
                });

            migrationBuilder.CreateTable(
                name: "giao_dich",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SanPhamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Loai = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SoLuong = table.Column<int>(type: "INTEGER", nullable: false),
                    GhiChu = table.Column<string>(type: "TEXT", nullable: true),
                    NguoiThucHien = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_giao_dich", x => x.Id);
                    table.ForeignKey(
                        name: "FK_giao_dich_san_pham_SanPhamId",
                        column: x => x.SanPhamId,
                        principalTable: "san_pham",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_giao_dich_SanPhamId_ThoiGian",
                table: "giao_dich",
                columns: new[] { "SanPhamId", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_Ma",
                table: "san_pham",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_Nhom",
                table: "san_pham",
                column: "Nhom");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "giao_dich");

            migrationBuilder.DropTable(
                name: "san_pham");
        }
    }
}
