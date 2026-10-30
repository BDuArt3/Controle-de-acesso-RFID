using ControleDeAcesso.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ControleDeAcesso.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Cartao> Cartoes => Set<Cartao>();
        public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
        public DbSet<LogAcesso> LogsAcesso => Set<LogAcesso>();
        public DbSet<HistoricoAssociacoesCartao> HistoricoAssociacoesCartao => Set<HistoricoAssociacoesCartao>();
        public DbSet<HistoricoStatusCartao> HistoricoStatusCartao => Set<HistoricoStatusCartao>();
        public DbSet<HistoricoStatusUsuario> HistoricoStatusUsuario => Set<HistoricoStatusUsuario>();
        public DbSet<HistoricoStatusFuncionario> HistoricoStatusFuncionario => Set<HistoricoStatusFuncionario>();
        public DbSet<SolicitacaoGravacaoCartao> SolicitacoesGravacaoCartao => Set<SolicitacaoGravacaoCartao>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(usuario => usuario.Id);
                entity.Property(usuario => usuario.Id).HasMaxLength(36);
                entity.Property(usuario => usuario.Nome).IsRequired().HasMaxLength(100);
                entity.Property(usuario => usuario.Email).HasMaxLength(254);
                entity.Property(usuario => usuario.Documento).IsRequired();
                entity.Property(usuario => usuario.Tipo).IsRequired();
                entity.Property(usuario => usuario.IsActive).IsRequired();

                entity.HasIndex(usuario => usuario.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Usuarios_Email")
                    .HasFilter("[Email] IS NOT NULL");

                entity.HasMany(usuario => usuario.Cartoes)
                    .WithOne(cartao => cartao.Usuario)
                    .HasForeignKey(cartao => cartao.UsuarioId)
                    .HasConstraintName("FK_Cartoes_Usuarios_UsuarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.Navigation(usuario => usuario.Cartoes)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);

                entity.HasMany(usuario => usuario.LogsAcesso)
                    .WithOne(log => log.Usuario)
                    .HasForeignKey(log => log.UsuarioId)
                    .HasConstraintName("FK_LogsAcesso_Usuarios_UsuarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(usuario => usuario.HistoricosStatus)
                    .WithOne(historico => historico.Usuario)
                    .HasForeignKey(historico => historico.UsuarioId)
                    .HasConstraintName("FK_HistoricoStatusUsuario_Usuarios_UsuarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(usuario => usuario.Funcionario)
                    .WithOne(funcionario => funcionario.Usuario)
                    .HasForeignKey<Funcionario>(funcionario => funcionario.UsuarioId)
                    .HasConstraintName("FK_Funcionarios_Usuarios_UsuarioId")
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Cartao>(entity =>
            {
                entity.HasKey(cartao => cartao.Id);
                entity.Property(cartao => cartao.Id).HasMaxLength(36);
                entity.Property(cartao => cartao.Uid).IsRequired().HasMaxLength(50);
                entity.Property(cartao => cartao.UsuarioId).IsRequired().HasMaxLength(36);
                entity.Property(cartao => cartao.Status).IsRequired();
                entity.Property(cartao => cartao.DataRegistro).IsRequired();
                entity.Property(cartao => cartao.DataExpiracao);

                entity.HasIndex(cartao => cartao.Uid)
                    .IsUnique()
                    .HasDatabaseName("IX_Cartoes_Uid");
                entity.HasIndex(cartao => cartao.UsuarioId)
                    .HasDatabaseName("IX_Cartoes_UsuarioId");
                entity.HasIndex(cartao => cartao.UsuarioId)
                    .IsUnique()
                    .HasDatabaseName("IX_Cartoes_UsuarioId_Ativo")
                    .HasFilter("[Status] = 0");

                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_Cartoes_Status",
                    "[Status] IN (0, 1, 2)"));

                entity.HasMany(cartao => cartao.LogsAcesso)
                    .WithOne(log => log.Cartao)
                    .HasForeignKey(log => log.CartaoId)
                    .HasConstraintName("FK_LogsAcesso_Cartoes_CartaoId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(cartao => cartao.HistoricoAssociacaoCartao)
                    .WithOne(historico => historico.Cartao)
                    .HasForeignKey<HistoricoAssociacoesCartao>(historico => historico.CartaoId)
                    .HasConstraintName("FK_HistoricoAssociacoesCartao_Cartoes_CartaoId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(cartao => cartao.HistoricosStatus)
                    .WithOne(historico => historico.Cartao)
                    .HasForeignKey(historico => historico.CartaoId)
                    .HasConstraintName("FK_HistoricoStatusCartao_Cartoes_CartaoId")
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Funcionario>(entity =>
            {
                entity.HasKey(funcionario => funcionario.Id);
                entity.Property(funcionario => funcionario.Id).HasMaxLength(36);
                entity.Property(funcionario => funcionario.UsuarioId).IsRequired().HasMaxLength(36);
                entity.Property(funcionario => funcionario.SenhaHash).IsRequired().HasMaxLength(255);
                entity.Property(funcionario => funcionario.Departamento).IsRequired();
                entity.Property(funcionario => funcionario.Cargo).IsRequired();
                entity.Property(funcionario => funcionario.Tipo).IsRequired();
                entity.Property(funcionario => funcionario.IsActive).IsRequired();

                entity.HasIndex(funcionario => funcionario.UsuarioId)
                    .IsUnique()
                    .HasDatabaseName("IX_Funcionarios_UsuarioId");

                entity.HasMany(funcionario => funcionario.HistoricosAssociacaoRealizadas)
                    .WithOne(historico => historico.AssociadoPorFuncionario)
                    .HasForeignKey(historico => historico.AssociadoPorFuncionarioId)
                    .HasConstraintName("FK_HistoricoAssociacoesCartao_Funcionarios_AssociadoPorFuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(funcionario => funcionario.HistoricosAssociacaoEncerradas)
                    .WithOne(historico => historico.DesassociadoPorFuncionario)
                    .HasForeignKey(historico => historico.DesassociadoPorFuncionarioId)
                    .HasConstraintName("FK_HistoricoAssociacoesCartao_Funcionarios_DesassociadoPorFuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(funcionario => funcionario.HistoricosStatusCartaoAlterados)
                    .WithOne(historico => historico.AlteradoPorFuncionario)
                    .HasForeignKey(historico => historico.AlteradoPorFuncionarioId)
                    .HasConstraintName("FK_HistoricoStatusCartao_Funcionarios_AlteradoPorFuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(funcionario => funcionario.HistoricosStatusUsuarioAlterados)
                    .WithOne(historico => historico.AlteradoPorFuncionario)
                    .HasForeignKey(historico => historico.AlteradoPorFuncionarioId)
                    .HasConstraintName("FK_HistoricoStatusUsuario_Funcionarios_AlteradoPorFuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(funcionario => funcionario.HistoricosStatusFuncionario)
                    .WithOne(historico => historico.Funcionario)
                    .HasForeignKey(historico => historico.FuncionarioId)
                    .HasConstraintName("FK_HistoricoStatusFuncionario_Funcionarios_FuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasMany(funcionario => funcionario.HistoricosStatusFuncionarioAlterados)
                    .WithOne(historico => historico.AlteradoPorFuncionario)
                    .HasForeignKey(historico => historico.AlteradoPorFuncionarioId)
                    .HasConstraintName("FK_HistoricoStatusFuncionario_Funcionarios_AlteradoPorFuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<LogAcesso>(entity =>
            {
                entity.HasKey(log => log.Id);
                entity.Property(log => log.DataHora).IsRequired();
                entity.Property(log => log.Local)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValue(LogAcesso.LocalPadrao);
                entity.Property(log => log.CartaoId).HasMaxLength(36);
                entity.Property(log => log.UsuarioId).HasMaxLength(36);
                entity.Property(log => log.UidInformado).HasMaxLength(50);
                entity.Property(log => log.AcessoPermitido).IsRequired();
                entity.Property(log => log.Motivo).IsRequired().HasMaxLength(200);
                entity.Property(log => log.MotivoTipo).IsRequired();

                entity.HasIndex(log => log.CartaoId)
                    .HasDatabaseName("IX_LogsAcesso_CartaoId");
                entity.HasIndex(log => log.UsuarioId)
                    .HasDatabaseName("IX_LogsAcesso_UsuarioId");

                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_LogsAcesso_Identificadores",
                    "(([CartaoId] IS NULL AND [UsuarioId] IS NULL) OR ([CartaoId] IS NOT NULL AND [UsuarioId] IS NOT NULL)) AND ([CartaoId] IS NULL OR [UidInformado] IS NULL)"));
            });

            modelBuilder.Entity<HistoricoAssociacoesCartao>(entity =>
            {
                entity.HasKey(historico => historico.Id);
                entity.Property(historico => historico.CartaoId).IsRequired().HasMaxLength(36);
                entity.Property(historico => historico.DataAssociacao).IsRequired();
                entity.Property(historico => historico.AssociadoPorFuncionarioId).HasMaxLength(36);
                entity.Property(historico => historico.DesassociadoPorFuncionarioId).HasMaxLength(36);

                entity.HasIndex(historico => historico.CartaoId)
                    .IsUnique()
                    .HasDatabaseName("IX_HistoricoAssociacoesCartao_CartaoId");
                entity.HasIndex(historico => historico.AssociadoPorFuncionarioId)
                    .HasDatabaseName("IX_HistoricoAssociacoesCartao_AssociadoPorFuncionarioId");
                entity.HasIndex(historico => historico.DesassociadoPorFuncionarioId)
                    .HasDatabaseName("IX_HistoricoAssociacoesCartao_DesassociadoPorFuncionarioId");
            });

            modelBuilder.Entity<HistoricoStatusCartao>(entity =>
            {
                entity.HasKey(historico => historico.Id);
                entity.Property(historico => historico.CartaoId).IsRequired().HasMaxLength(36);
                entity.Property(historico => historico.Status).IsRequired();
                entity.Property(historico => historico.DataHora).IsRequired();
                entity.Property(historico => historico.Motivo).HasMaxLength(200);
                entity.Property(historico => historico.AlteradoPorFuncionarioId).HasMaxLength(36);

                entity.HasIndex(historico => historico.CartaoId)
                    .HasDatabaseName("IX_HistoricoStatusCartao_CartaoId");
                entity.HasIndex(historico => historico.AlteradoPorFuncionarioId)
                    .HasDatabaseName("IX_HistoricoStatusCartao_AlteradoPorFuncionarioId");

                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_HistoricoStatusCartao_Status",
                    "[Status] IN (0, 1, 2)"));
            });

            modelBuilder.Entity<HistoricoStatusUsuario>(entity =>
            {
                entity.HasKey(historico => historico.Id);
                entity.Property(historico => historico.UsuarioId).IsRequired().HasMaxLength(36);
                entity.Property(historico => historico.IsActive).IsRequired();
                entity.Property(historico => historico.DataHora).IsRequired();
                entity.Property(historico => historico.Motivo).HasMaxLength(200);
                entity.Property(historico => historico.AlteradoPorFuncionarioId).HasMaxLength(36);

                entity.HasIndex(historico => historico.UsuarioId)
                    .HasDatabaseName("IX_HistoricoStatusUsuario_UsuarioId");
                entity.HasIndex(historico => historico.AlteradoPorFuncionarioId)
                    .HasDatabaseName("IX_HistoricoStatusUsuario_AlteradoPorFuncionarioId");
            });

            modelBuilder.Entity<HistoricoStatusFuncionario>(entity =>
            {
                entity.HasKey(historico => historico.Id);
                entity.Property(historico => historico.FuncionarioId).IsRequired().HasMaxLength(36);
                entity.Property(historico => historico.IsActive).IsRequired();
                entity.Property(historico => historico.DataHora).IsRequired();
                entity.Property(historico => historico.Motivo).HasMaxLength(200);
                entity.Property(historico => historico.AlteradoPorFuncionarioId).HasMaxLength(36);

                entity.HasIndex(historico => historico.FuncionarioId)
                    .HasDatabaseName("IX_HistoricoStatusFuncionario_FuncionarioId");
                entity.HasIndex(historico => historico.AlteradoPorFuncionarioId)
                    .HasDatabaseName("IX_HistoricoStatusFuncionario_AlteradoPorFuncionarioId");
            });

            modelBuilder.Entity<SolicitacaoGravacaoCartao>(entity =>
            {
                entity.HasKey(solicitacao => solicitacao.Id);
                entity.Property(solicitacao => solicitacao.Id).HasMaxLength(36);
                entity.Property(solicitacao => solicitacao.ChaveIdempotencia).IsRequired().HasMaxLength(128);
                entity.Property(solicitacao => solicitacao.Credencial).IsRequired().HasMaxLength(16);
                entity.Property(solicitacao => solicitacao.UsuarioId).IsRequired().HasMaxLength(36);
                entity.Property(solicitacao => solicitacao.SolicitanteFuncionarioId).IsRequired().HasMaxLength(36);
                entity.Property(solicitacao => solicitacao.CartaoAnteriorId).HasMaxLength(36);
                entity.Property(solicitacao => solicitacao.CartaoCriadoId).HasMaxLength(36);
                entity.Property(solicitacao => solicitacao.Status).IsRequired();
                entity.Property(solicitacao => solicitacao.DataSolicitacao).IsRequired();
                entity.Property(solicitacao => solicitacao.MotivoReemissao).HasMaxLength(200);
                entity.Property(solicitacao => solicitacao.MotivoFalha).HasMaxLength(200);
                entity.Property(solicitacao => solicitacao.CanceladoPorFuncionarioId).HasMaxLength(36);

                entity.HasIndex(solicitacao => new
                    {
                        solicitacao.SolicitanteFuncionarioId,
                        solicitacao.ChaveIdempotencia
                    })
                    .IsUnique()
                    .HasDatabaseName("IX_SolicitacoesGravacaoCartao_Solicitante_Chave");
                entity.HasIndex(solicitacao => solicitacao.Credencial)
                    .IsUnique()
                    .HasDatabaseName("IX_SolicitacoesGravacaoCartao_Credencial");
                entity.HasIndex(solicitacao => solicitacao.CartaoAnteriorId)
                    .IsUnique()
                    .HasFilter("[CartaoAnteriorId] IS NOT NULL AND [Status] IN (0, 1, 3)")
                    .HasDatabaseName("IX_SolicitacoesGravacaoCartao_CartaoAnterior_Aberta");
                entity.HasIndex(solicitacao => solicitacao.UsuarioId)
                    .HasDatabaseName("IX_SolicitacoesGravacaoCartao_UsuarioId");
                entity.HasIndex(solicitacao => solicitacao.CanceladoPorFuncionarioId)
                    .HasDatabaseName("IX_SolicitacoesGravacaoCartao_CanceladoPorFuncionarioId");

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(solicitacao => solicitacao.UsuarioId)
                    .HasConstraintName("FK_SolicitacoesGravacaoCartao_Usuarios_UsuarioId")
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<Funcionario>()
                    .WithMany()
                    .HasForeignKey(solicitacao => solicitacao.SolicitanteFuncionarioId)
                    .HasConstraintName("FK_SolicitacoesGravacaoCartao_Funcionarios_SolicitanteFuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<Cartao>()
                    .WithMany()
                    .HasForeignKey(solicitacao => solicitacao.CartaoAnteriorId)
                    .HasConstraintName("FK_SolicitacoesGravacaoCartao_Cartoes_CartaoAnteriorId")
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<Cartao>()
                    .WithMany()
                    .HasForeignKey(solicitacao => solicitacao.CartaoCriadoId)
                    .HasConstraintName("FK_SolicitacoesGravacaoCartao_Cartoes_CartaoCriadoId")
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<Funcionario>()
                    .WithMany()
                    .HasForeignKey(solicitacao => solicitacao.CanceladoPorFuncionarioId)
                    .HasConstraintName("FK_SolicitacoesGravacaoCartao_Funcionarios_CanceladoPorFuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);

                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_SolicitacoesGravacaoCartao_Status",
                    "[Status] IN (0, 1, 2, 3, 4)"));
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(token => token.TokenHash);
                entity.Property(token => token.TokenHash).IsRequired().HasMaxLength(64);
                entity.Property(token => token.FuncionarioId).IsRequired().HasMaxLength(36);
                entity.Property(token => token.CreatedAtUtc).IsRequired();
                entity.Property(token => token.ExpiresAtUtc).IsRequired();
                entity.Property(token => token.RevokedAtUtc);
                entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);

                entity.HasIndex(token => token.ExpiresAtUtc)
                    .HasDatabaseName("IX_RefreshTokens_ExpiresAtUtc");

                entity.HasOne<Funcionario>()
                    .WithMany()
                    .HasForeignKey(token => token.FuncionarioId)
                    .HasConstraintName("FK_RefreshTokens_Funcionarios_FuncionarioId")
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
