using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Infrastructure.Data
{
    public static class SeedData
    {
        public static void Seed(AppDbContext context, string? adminPassword)
        {
            if (context.Funcionarios.Any(f => f.Tipo == TipoFuncionario.Admin))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "Configure BootstrapAdmin:Password para criar o primeiro administrador.");
            }

            var admin = new Funcionario
            {
                NomeFuncionario = "Administrador",
                Email = "admin@tcc.com",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Tipo = TipoFuncionario.Admin,
                IsActive = true
            };
            context.Funcionarios.Add(admin);
            context.SaveChanges();
        }
    }
}