using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crudEbancoDist8.Migrations
{
    /// <inheritdoc />
    public partial class RelacionamentoCategoriaBebidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Bebidas");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Bebidas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bebidas_CategoryId",
                table: "Bebidas",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bebidas_Categorias_CategoryId",
                table: "Bebidas",
                column: "CategoryId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bebidas_Categorias_CategoryId",
                table: "Bebidas");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Bebidas_CategoryId",
                table: "Bebidas");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Bebidas");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Bebidas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
