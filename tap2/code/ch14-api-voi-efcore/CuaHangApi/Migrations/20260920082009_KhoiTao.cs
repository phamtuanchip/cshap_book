using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuaHangApi.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "khach_hang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ten = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_khach_hang", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "nhom",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ten = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nhom", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "don_hang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KhachHangId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ngay = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_don_hang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_don_hang_khach_hang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "khach_hang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "san_pham",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ma = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Gia = table.Column<double>(type: "REAL", nullable: false),
                    Ton = table.Column<int>(type: "INTEGER", nullable: false),
                    DaXoa = table.Column<bool>(type: "INTEGER", nullable: false),
                    NhomId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_san_pham", x => x.Id);
                    table.CheckConstraint("ck_san_pham_ton", "Ton >= 0");
                    table.ForeignKey(
                        name: "FK_san_pham_nhom_NhomId",
                        column: x => x.NhomId,
                        principalTable: "nhom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chi_tiet_don",
                columns: table => new
                {
                    DonHangId = table.Column<int>(type: "INTEGER", nullable: false),
                    SanPhamId = table.Column<int>(type: "INTEGER", nullable: false),
                    SoLuong = table.Column<int>(type: "INTEGER", nullable: false),
                    DonGia = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chi_tiet_don", x => new { x.DonHangId, x.SanPhamId });
                    table.ForeignKey(
                        name: "FK_chi_tiet_don_don_hang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "don_hang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chi_tiet_don_san_pham_SanPhamId",
                        column: x => x.SanPhamId,
                        principalTable: "san_pham",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chi_tiet_don_SanPhamId",
                table: "chi_tiet_don",
                column: "SanPhamId");

            migrationBuilder.CreateIndex(
                name: "IX_don_hang_KhachHangId",
                table: "don_hang",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_khach_hang_Email",
                table: "khach_hang",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nhom_Ten",
                table: "nhom",
                column: "Ten",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_Ma",
                table: "san_pham",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_NhomId",
                table: "san_pham",
                column: "NhomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chi_tiet_don");

            migrationBuilder.DropTable(
                name: "don_hang");

            migrationBuilder.DropTable(
                name: "san_pham");

            migrationBuilder.DropTable(
                name: "khach_hang");

            migrationBuilder.DropTable(
                name: "nhom");
        }
    }
}
