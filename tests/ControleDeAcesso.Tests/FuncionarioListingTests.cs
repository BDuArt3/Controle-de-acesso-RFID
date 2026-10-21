using System.Text.Json;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public class FuncionarioListingTests
{
    [Fact]
    public async Task GetAllAsync_DoesNotExposeCpf()
    {
        const string cpf = "12345678900";
        var funcionarioRepository = new Mock<IFuncionarioRepository>();
        funcionarioRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync(
            new[]
            {
                new Funcionario
                {
                    Tipo = TipoFuncionario.Admin,
                    IsActive = true,
                    Usuario = new Usuario
                    {
                        Nome = "Funcionario",
                        Email = "funcionario@example.com",
                        Documento = cpf
                    }
                }
            });
        var service = new FuncionarioService(
            funcionarioRepository.Object,
            Mock.Of<IUsuarioRepository>(),
            Mock.Of<ICartaoRepository>(),
            Mock.Of<IUnitOfWork>(),
            NullLogger<FuncionarioService>.Instance);

        var result = await service.GetAllAsync();
        var json = JsonSerializer.Serialize(result);

        Assert.DoesNotContain(cpf, json);
        Assert.DoesNotContain("Cpf", json, StringComparison.OrdinalIgnoreCase);
    }
}
