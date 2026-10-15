using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleDeAcesso.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitacoesGravacaoCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cartoes_Usuarios_UsuarioId",
                table: "Cartoes");

            migrationBuilder.DropIndex(
                name: "IX_Funcionarios_Email",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CartaoUid",
                table: "LogsAcesso");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "NomeFuncionario",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Cartoes");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CartaoId",
                table: "LogsAcesso",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UidInformado",
                table: "LogsAcesso",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "LogsAcesso",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "Funcionarios",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Cartoes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HistoricoAssociacoesCartao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartaoId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    DataAssociacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataDesassociacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssociadoPorFuncionarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    DesassociadoPorFuncionarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoAssociacoesCartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoAssociacoesCartao_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistoricoAssociacoesCartao_Funcionarios_AssociadoPorFuncionarioId",
                        column: x => x.AssociadoPorFuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistoricoAssociacoesCartao_Funcionarios_DesassociadoPorFuncionarioId",
                        column: x => x.DesassociadoPorFuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistoricoStatusCartao",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartaoId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AlteradoPorFuncionarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoStatusCartao", x => x.Id);
                    table.CheckConstraint("CK_HistoricoStatusCartao_Status", "[Status] IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_HistoricoStatusCartao_Cartoes_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistoricoStatusCartao_Funcionarios_AlteradoPorFuncionarioId",
                        column: x => x.AlteradoPorFuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistoricoStatusFuncionario",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FuncionarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AlteradoPorFuncionarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoStatusFuncionario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoStatusFuncionario_Funcionarios_AlteradoPorFuncionarioId",
                        column: x => x.AlteradoPorFuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistoricoStatusFuncionario_Funcionarios_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistoricoStatusUsuario",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DataHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AlteradoPorFuncionarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoStatusUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoStatusUsuario_Funcionarios_AlteradoPorFuncionarioId",
                        column: x => x.AlteradoPorFuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HistoricoStatusUsuario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SolicitacoesGravacaoCartao",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ChaveIdempotencia = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Credencial = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    SolicitanteFuncionarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    CartaoAnteriorId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CartaoCriadoId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataUltimoEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataConclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataCancelamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuantidadeEnvios = table.Column<int>(type: "int", nullable: false),
                    MotivoReemissao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MotivoFalha = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CanceladoPorFuncionarioId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesGravacaoCartao", x => x.Id);
                    table.CheckConstraint("CK_SolicitacoesGravacaoCartao_Status", "[Status] IN (0, 1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_SolicitacoesGravacaoCartao_Cartoes_CartaoAnteriorId",
                        column: x => x.CartaoAnteriorId,
                        principalTable: "Cartoes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitacoesGravacaoCartao_Cartoes_CartaoCriadoId",
                        column: x => x.CartaoCriadoId,
                        principalTable: "Cartoes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitacoesGravacaoCartao_Funcionarios_CanceladoPorFuncionarioId",
                        column: x => x.CanceladoPorFuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitacoesGravacaoCartao_Funcionarios_SolicitanteFuncionarioId",
                        column: x => x.SolicitanteFuncionarioId,
                        principalTable: "Funcionarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolicitacoesGravacaoCartao_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcesso_CartaoId",
                table: "LogsAcesso",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAcesso_UsuarioId",
                table: "LogsAcesso",
                column: "UsuarioId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LogsAcesso_Identificadores",
                table: "LogsAcesso",
                sql: "(([CartaoId] IS NULL AND [UsuarioId] IS NULL) OR ([CartaoId] IS NOT NULL AND [UsuarioId] IS NOT NULL)) AND ([CartaoId] IS NULL OR [UidInformado] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_UsuarioId",
                table: "Funcionarios",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cartoes_Status",
                table: "Cartoes",
                sql: "[Status] IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAssociacoesCartao_AssociadoPorFuncionarioId",
                table: "HistoricoAssociacoesCartao",
                column: "AssociadoPorFuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAssociacoesCartao_CartaoId",
                table: "HistoricoAssociacoesCartao",
                column: "CartaoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoAssociacoesCartao_DesassociadoPorFuncionarioId",
                table: "HistoricoAssociacoesCartao",
                column: "DesassociadoPorFuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoStatusCartao_AlteradoPorFuncionarioId",
                table: "HistoricoStatusCartao",
                column: "AlteradoPorFuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoStatusCartao_CartaoId",
                table: "HistoricoStatusCartao",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoStatusFuncionario_AlteradoPorFuncionarioId",
                table: "HistoricoStatusFuncionario",
                column: "AlteradoPorFuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoStatusFuncionario_FuncionarioId",
                table: "HistoricoStatusFuncionario",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoStatusUsuario_AlteradoPorFuncionarioId",
                table: "HistoricoStatusUsuario",
                column: "AlteradoPorFuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoStatusUsuario_UsuarioId",
                table: "HistoricoStatusUsuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesGravacaoCartao_CanceladoPorFuncionarioId",
                table: "SolicitacoesGravacaoCartao",
                column: "CanceladoPorFuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesGravacaoCartao_CartaoAnterior_Aberta",
                table: "SolicitacoesGravacaoCartao",
                column: "CartaoAnteriorId",
                unique: true,
                filter: "[CartaoAnteriorId] IS NOT NULL AND [Status] IN (0, 1, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesGravacaoCartao_CartaoCriadoId",
                table: "SolicitacoesGravacaoCartao",
                column: "CartaoCriadoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesGravacaoCartao_Credencial",
                table: "SolicitacoesGravacaoCartao",
                column: "Credencial",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesGravacaoCartao_Solicitante_Chave",
                table: "SolicitacoesGravacaoCartao",
                columns: new[] { "SolicitanteFuncionarioId", "ChaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesGravacaoCartao_UsuarioId",
                table: "SolicitacoesGravacaoCartao",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cartoes_Usuarios_UsuarioId",
                table: "Cartoes",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Funcionarios_Usuarios_UsuarioId",
                table: "Funcionarios",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LogsAcesso_Cartoes_CartaoId",
                table: "LogsAcesso",
                column: "CartaoId",
                principalTable: "Cartoes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LogsAcesso_Usuarios_UsuarioId",
                table: "LogsAcesso",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cartoes_Usuarios_UsuarioId",
                table: "Cartoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Funcionarios_Usuarios_UsuarioId",
                table: "Funcionarios");

            migrationBuilder.DropForeignKey(
                name: "FK_LogsAcesso_Cartoes_CartaoId",
                table: "LogsAcesso");

            migrationBuilder.DropForeignKey(
                name: "FK_LogsAcesso_Usuarios_UsuarioId",
                table: "LogsAcesso");

            migrationBuilder.DropTable(
                name: "HistoricoAssociacoesCartao");

            migrationBuilder.DropTable(
                name: "HistoricoStatusCartao");

            migrationBuilder.DropTable(
                name: "HistoricoStatusFuncionario");

            migrationBuilder.DropTable(
                name: "HistoricoStatusUsuario");

            migrationBuilder.DropTable(
                name: "SolicitacoesGravacaoCartao");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_LogsAcesso_CartaoId",
                table: "LogsAcesso");

            migrationBuilder.DropIndex(
                name: "IX_LogsAcesso_UsuarioId",
                table: "LogsAcesso");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LogsAcesso_Identificadores",
                table: "LogsAcesso");

            migrationBuilder.DropIndex(
                name: "IX_Funcionarios_UsuarioId",
                table: "Funcionarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cartoes_Status",
                table: "Cartoes");

            migrationBuilder.DropColumn(
                name: "CartaoId",
                table: "LogsAcesso");

            migrationBuilder.DropColumn(
                name: "UidInformado",
                table: "LogsAcesso");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "LogsAcesso");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Funcionarios");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Cartoes");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CartaoUid",
                table: "LogsAcesso",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "Funcionarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Funcionarios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NomeFuncionario",
                table: "Funcionarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Cartoes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_Email",
                table: "Funcionarios",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cartoes_Usuarios_UsuarioId",
                table: "Cartoes",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
