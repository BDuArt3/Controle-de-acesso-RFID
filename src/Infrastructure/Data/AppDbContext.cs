
using ControleDeAcesso.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ControleDeAcesso.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cartao> Cartoes { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<LogAcesso> LogsAcesso { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da entidade Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(36);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Tipo).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();

                // Relacionamento 1:N com Cartao
                entity.HasMany(e => e.Cartoes)
                      .WithOne(c => c.Usuario)
                      .HasForeignKey(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuração da entidade Cartao
            modelBuilder.Entity<Cartao>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(36);
                entity.Property(e => e.Uid).IsRequired().HasMaxLength(50);
                entity.Property(e => e.UsuarioId).IsRequired().HasMaxLength(36);
                entity.Property(e => e.IsActive).IsRequired();

                // Índice único para o UID do cartão
                entity.HasIndex(e => e.Uid).IsUnique();
            });

            // Configuração da entidade Funcionario
            modelBuilder.Entity<Funcionario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(36);
                entity.Property(e => e.NomeFuncionario).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UsuarioAcesso).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SenhaHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Tipo).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();

                // Índice único para o usuário de acesso
                entity.HasIndex(e => e.UsuarioAcesso).IsUnique();
            });

            // Configuração da entidade LogAcesso
            modelBuilder.Entity<LogAcesso>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CartaoUid).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DataHora).IsRequired();
                entity.Property(e => e.AcessoPermitido).IsRequired();
                entity.Property(e => e.Motivo).HasMaxLength(200);
                entity.Property(e => e.MotivoTipo).IsRequired();
            });
        }
    }
}