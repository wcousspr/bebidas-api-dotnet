using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace crudEbancoDist8.Migrations
{
    /// <inheritdoc />
    public partial class RenameCategoryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Categorias",
                newName: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Categorias",
                newName: "Nome");
        }
    }
}
