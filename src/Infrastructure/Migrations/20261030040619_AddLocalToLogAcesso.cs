using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleDeAcesso.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalToLogAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Local",
                table: "LogsAcesso",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Entrada Principal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Local",
                table: "LogsAcesso");
        }
    }
}
