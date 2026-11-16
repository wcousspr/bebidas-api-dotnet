using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crudEbancoDist8.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePublicIdToBebidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePublicId",
                table: "Bebidas",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePublicId",
                table: "Bebidas");
        }
    }
}
