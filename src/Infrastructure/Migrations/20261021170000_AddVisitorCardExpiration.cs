using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleDeAcesso.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261021170000_AddVisitorCardExpiration")]
    public partial class AddVisitorCardExpiration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataExpiracao",
                table: "Cartoes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE cartao
                SET DataExpiracao = DATEADD(hour, 24, cartao.DataRegistro)
                FROM Cartoes AS cartao
                INNER JOIN Usuarios AS usuario ON usuario.Id = cartao.UsuarioId
                WHERE usuario.Tipo = 2;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataExpiracao",
                table: "Cartoes");
        }
    }
}
