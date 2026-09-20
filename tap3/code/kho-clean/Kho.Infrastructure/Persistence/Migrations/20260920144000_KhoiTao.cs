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
                name: "outbox");

            migrationBuilder.DropTable(
                name: "san_pham");

            migrationBuilder.DropTable(
                name: "SanPhamDocs");
        }
    }
}
