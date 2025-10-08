using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleDeAcesso.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMotivoTipoToLogAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MotivoTipo",
                table: "LogsAcesso",
                type: "int",
                nullable: false,
                defaultValue: 7); // TipoMotivoAcesso.ErroInterno
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotivoTipo",
                table: "LogsAcesso");
        }
    }
}
