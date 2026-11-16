using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crudEbancoDist8.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToBebidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Bebidas",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Bebidas");
        }
    }
}
