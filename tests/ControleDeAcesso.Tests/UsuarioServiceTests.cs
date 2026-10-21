using System.Text.Json;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public class UsuarioServiceTests
{
    [Fact]
    public async Task CreateAsync_WithoutDocumentoCreatesUser()
    {
        Usuario? persistedUsuario = null;
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(item => item.EmailExistsAsync("usuario@example.com", null)).ReturnsAsync(false);
        repository.Setup(item => item.CreateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario usuario) =>
            {
                persistedUsuario = usuario;
                return usuario;
            });
        var service = new UsuarioService(repository.Object, NullLogger<UsuarioService>.Instance);

        var result = await service.CreateAsync(new CreateUsuarioDto
        {
            Nome = "Usuario",
            Email = "usuario@example.com",
            Documento = string.Empty
        });

        Assert.NotNull(persistedUsuario);
        Assert.Equal(string.Empty, persistedUsuario.Documento);
        Assert.DoesNotContain("Documento", JsonSerializer.Serialize(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotExposeDocumentoInResponse()
    {
        const string cpf = "12345678900";
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(item => item.GetAllAsync()).ReturnsAsync(
            new[] { new Usuario { Nome = "Usuario", Documento = cpf, IsActive = true } });
        var service = new UsuarioService(repository.Object, NullLogger<UsuarioService>.Instance);

        var result = await service.GetAllAsync();
        var json = JsonSerializer.Serialize(result);

        Assert.DoesNotContain(cpf, json);
        Assert.DoesNotContain("Documento", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_WithoutDocumentoPreservesExistingValue()
    {
        const string cpf = "12345678900";
        var usuario = new Usuario
        {
            Id = "usuario-id",
            Nome = "Usuario",
            Email = "usuario@example.com",
            Documento = cpf
        };
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(item => item.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);
        repository.Setup(item => item.EmailExistsAsync(usuario.Email, usuario.Id)).ReturnsAsync(false);
        repository.Setup(item => item.UpdateAsync(usuario)).ReturnsAsync(usuario);
        var service = new UsuarioService(repository.Object, NullLogger<UsuarioService>.Instance);

        await service.UpdateAsync(usuario.Id, new CreateUsuarioDto
        {
            Nome = "Usuario Atualizado",
            Email = usuario.Email,
            Documento = string.Empty
        });

        Assert.Equal(cpf, usuario.Documento);
    }
}
