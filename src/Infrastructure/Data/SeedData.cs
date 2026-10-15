using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Infrastructure.Data
{
    public static class SeedData
    {
        public static void Seed(AppDbContext context, string? adminPassword, string? adminDocument)
        {
            if (context.Funcionarios.Any(f => f.Tipo == TipoFuncionario.Admin))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(adminDocument))
            {
                throw new InvalidOperationException(
                    "Configure BootstrapAdmin:Password e BootstrapAdmin:Document para criar o primeiro administrador.");
            }

            const string adminEmail = "admin@tcc.com";
            if (context.Usuarios.Any(usuario =>
                    usuario.Email != null &&
                    usuario.Email.Trim().ToLower() == adminEmail))
            {
                throw new InvalidOperationException(
                    "O email configurado para o administrador já está associado a um usuário sem conta de funcionário.");
            }

            var now = DateTime.UtcNow;
            var usuario = new Usuario
            {
                Nome = "Administrador",
                Email = adminEmail,
                Documento = adminDocument.Trim(),
                Tipo = TipoUsuario.Colaborador,
                IsActive = true
            };
            var admin = new Funcionario
            {
                Usuario = usuario,
                UsuarioId = usuario.Id,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Tipo = TipoFuncionario.Admin,
                IsActive = true
            };
            usuario.Funcionario = admin;
            usuario.HistoricosStatus.Add(new HistoricoStatusUsuario
            {
                IsActive = true,
                DataHora = now,
                Motivo = "Cadastro inicial do administrador."
            });
            admin.HistoricosStatusFuncionario.Add(new HistoricoStatusFuncionario
            {
                IsActive = true,
                DataHora = now,
                Motivo = "Cadastro inicial do administrador."
            });
            context.Funcionarios.Add(admin);
            // SaveChanges já é transacional; transação manual é incompatível com a estratégia de retry.
            context.SaveChanges();
        }
    }
}