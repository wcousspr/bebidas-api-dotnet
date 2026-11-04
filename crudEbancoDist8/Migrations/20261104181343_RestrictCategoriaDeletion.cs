using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crudEbancoDist8.Migrations
{
    /// <inheritdoc />
    public partial class RestrictCategoriaDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bebidas_Categorias_CategoryId",
                table: "Bebidas");

            migrationBuilder.AddForeignKey(
                name: "FK_Bebidas_Categorias_CategoryId",
                table: "Bebidas",
                column: "CategoryId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bebidas_Categorias_CategoryId",
                table: "Bebidas");

            migrationBuilder.AddForeignKey(
                name: "FK_Bebidas_Categorias_CategoryId",
                table: "Bebidas",
                column: "CategoryId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
