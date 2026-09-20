using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CuaHang.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "san_pham",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ma = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Gia = table.Column<double>(type: "REAL", nullable: false),
                    Ton = table.Column<int>(type: "INTEGER", nullable: false),
                    NhomId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_san_pham", x => x.Id);
                    table.CheckConstraint("ck_san_pham_gia", "Gia >= 0");
                    table.ForeignKey(
                        name: "FK_san_pham_nhom_NhomId",
                        column: x => x.NhomId,
                        principalTable: "nhom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "nhom",
                columns: new[] { "Id", "Ten" },
                values: new object[,]
                {
                    { 1, "Phu kien" },
                    { 2, "Thiet bi" }
                });

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
                name: "san_pham");

            migrationBuilder.DropTable(
                name: "nhom");
        }
    }
}
