using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleDeAcesso.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261021175600_OneActiveCardPerUser")]
    public partial class OneActiveCardPerUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT UsuarioId
                    FROM Cartoes
                    WHERE Status = 0
                    GROUP BY UsuarioId
                    HAVING COUNT(*) > 1
                )
                    THROW 51000, 'Existem usuários com mais de um cartão ativo. Resolva essas duplicidades antes de aplicar esta migração.', 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Cartoes_UsuarioId_Ativo",
                table: "Cartoes",
                column: "UsuarioId",
                unique: true,
                filter: "[Status] = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cartoes_UsuarioId_Ativo",
                table: "Cartoes");
        }
    }
}
