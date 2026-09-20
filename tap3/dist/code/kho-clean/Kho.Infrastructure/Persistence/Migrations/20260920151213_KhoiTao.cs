using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "don_hang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ma = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    KhachHang = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TrangThai = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TongTien = table.Column<double>(type: "REAL", nullable: false),
                    DatLuc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_don_hang", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency",
                columns: table => new
                {
                    Khoa = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DauVan = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    MaTrangThai = table.Column<int>(type: "INTEGER", nullable: false),
                    NoiDung = table.Column<string>(type: "TEXT", nullable: true),
                    LoaiNoiDung = table.Column<string>(type: "TEXT", nullable: true),
                    TaoLucMs = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency", x => x.Khoa);
                });

            migrationBuilder.CreateTable(
                name: "nhat_ky_kiem_toan",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Luc = table.Column<long>(type: "INTEGER", nullable: false),
                    Nguoi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HanhDong = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DoiTuong = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Truoc = table.Column<string>(type: "TEXT", nullable: true),
                    Sau = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nhat_ky_kiem_toan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Loai = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "TEXT", nullable: false),
                    TaoLuc = table.Column<long>(type: "INTEGER", nullable: false),
                    XuLyLuc = table.Column<long>(type: "INTEGER", nullable: true),
                    SoLanThu = table.Column<int>(type: "INTEGER", nullable: false),
                    LoiCuoi = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.Id);
                });

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
                name: "SanPhamDocs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Ma = table.Column<string>(type: "TEXT", nullable: false),
                    Ten = table.Column<string>(type: "TEXT", nullable: false),
                    Nhom = table.Column<string>(type: "TEXT", nullable: false),
                    DonGia = table.Column<double>(type: "REAL", nullable: false),
                    TonKho = table.Column<int>(type: "INTEGER", nullable: false),
                    MucCanhBao = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "dong_don_hang",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaSanPham = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SoLuong = table.Column<int>(type: "INTEGER", nullable: false),
                    DonGia = table.Column<double>(type: "REAL", nullable: false),
                    DonHangId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dong_don_hang", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dong_don_hang_don_hang_DonHangId",
                        column: x => x.DonHangId,
                        principalTable: "don_hang",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_don_hang_Ma",
                table: "don_hang",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dong_don_hang_DonHangId",
                table: "dong_don_hang",
                column: "DonHangId");

            migrationBuilder.CreateIndex(
                name: "IX_nhat_ky_kiem_toan_Luc",
                table: "nhat_ky_kiem_toan",
                column: "Luc");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_XuLyLuc_TaoLuc",
                table: "outbox",
                columns: new[] { "XuLyLuc", "TaoLuc" });

            migrationBuilder.CreateIndex(
                name: "IX_san_pham_Ma",
                table: "san_pham",
                column: "Ma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dong_don_hang");

            migrationBuilder.DropTable(
                name: "idempotency");

            migrationBuilder.DropTable(
                name: "nhat_ky_kiem_toan");

            migrationBuilder.DropTable(
                name: "outbox");

            migrationBuilder.DropTable(
                name: "san_pham");

            migrationBuilder.DropTable(
                name: "SanPhamDocs");

            migrationBuilder.DropTable(
                name: "don_hang");
        }
    }
}
