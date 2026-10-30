using System.Text.Json;
using System.Reflection;
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

    [Fact]
    public async Task StatusAndUpdateOperations_RejectUsuarioLinkedToFuncionario()
    {
        var usuario = new Usuario
        {
            Id = "usuario-id",
            Nome = "Funcionario",
            Email = "func@example.com",
            IsActive = true,
            Funcionario = new Funcionario()
        };
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(item => item.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);
        var service = new UsuarioService(repository.Object, NullLogger<UsuarioService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(
            usuario.Id, new CreateUsuarioDto { Nome = "Novo", Email = usuario.Email }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(usuario.Id, "resp-id", "motivo"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ActivateAsync(usuario.Id, "resp-id", "motivo"));

        Assert.True(usuario.IsActive);
        repository.Verify(item => item.UpdateAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_StillDeactivatesRegularUsuario()
    {
        var usuario = new Usuario { Id = "usuario-id", Nome = "Aluno", IsActive = true };
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(item => item.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);
        repository.Setup(item => item.UpdateAsync(usuario)).ReturnsAsync(usuario);
        var service = new UsuarioService(repository.Object, NullLogger<UsuarioService>.Instance);

        var result = await service.DeleteAsync(usuario.Id, "resp-id", "motivo");

        Assert.True(result);
        Assert.False(usuario.IsActive);
        repository.Verify(item => item.UpdateAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_BlocksActiveCardsAndRecordsStatusHistory()
    {
        var activeCard = CreateCard("card-active", StatusCartao.Ativo);
        var blockedCard = CreateCard("card-blocked", StatusCartao.Bloqueado);
        var cancelledCard = CreateCard("card-cancelled", StatusCartao.Cancelado);
        var usuario = CreateUsuarioWithCards("usuario-id", true, activeCard, blockedCard, cancelledCard);
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(item => item.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);
        repository.Setup(item => item.UpdateAsync(usuario)).ReturnsAsync(usuario);
        var service = new UsuarioService(repository.Object, NullLogger<UsuarioService>.Instance);

        var result = await service.DeleteAsync(usuario.Id, "funcionario-id", "Vínculo encerrado");

        Assert.True(result);
        Assert.False(usuario.IsActive);
        Assert.Equal(StatusCartao.Bloqueado, activeCard.Status);
        Assert.Equal(StatusCartao.Bloqueado, blockedCard.Status);
        Assert.Equal(StatusCartao.Cancelado, cancelledCard.Status);
        var history = Assert.Single(activeCard.HistoricosStatus);
        Assert.Equal(StatusCartao.Bloqueado, history.Status);
        Assert.Equal("Vínculo encerrado", history.Motivo);
        Assert.Equal("funcionario-id", history.AlteradoPorFuncionarioId);
        Assert.Single(usuario.HistoricosStatus);
        repository.Verify(item => item.UpdateAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_BlocksLegacyActiveCard_WhenUsuarioIsAlreadyInactive()
    {
        var activeCard = CreateCard("legacy-card", StatusCartao.Ativo);
        var usuario = CreateUsuarioWithCards("usuario-inativo", false, activeCard);
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(item => item.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);
        repository.Setup(item => item.UpdateAsync(usuario)).ReturnsAsync(usuario);
        var service = new UsuarioService(repository.Object, NullLogger<UsuarioService>.Instance);

        var result = await service.DeleteAsync(usuario.Id, "funcionario-id", "Correção de estado legado");

        Assert.True(result);
        Assert.False(usuario.IsActive);
        Assert.Equal(StatusCartao.Bloqueado, activeCard.Status);
        Assert.Equal("Correção de estado legado", Assert.Single(activeCard.HistoricosStatus).Motivo);
        Assert.Empty(usuario.HistoricosStatus);
        repository.Verify(item => item.UpdateAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotUpdate_WhenUsuarioAndCardsAreAlreadyInactive()
    {
        var blockedCard = CreateCard("card-blocked", StatusCartao.Bloqueado);
        var usuario = CreateUsuarioWithCards("usuario-inativo", false, blockedCard);
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(item => item.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);
        var service = new UsuarioService(repository.Object, NullLogger<UsuarioService>.Instance);

        var result = await service.DeleteAsync(usuario.Id, "funcionario-id", "Motivo");

        Assert.True(result);
        repository.Verify(item => item.UpdateAsync(It.IsAny<Usuario>()), Times.Never);
    }

    private static Usuario CreateUsuarioWithCards(string id, bool isActive, params Cartao[] cards)
    {
        var usuario = new Usuario { Id = id, Nome = "Aluno", IsActive = isActive };
        var backingCollection = (List<Cartao>)typeof(Usuario)
            .GetField("_cartoes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(usuario)!;
        backingCollection.AddRange(cards);
        return usuario;
    }

    private static Cartao CreateCard(string id, StatusCartao status)
    {
        var cartao = new Cartao
        {
            Id = id,
            Uid = id,
            UsuarioId = "usuario-id"
        };
        if (status != StatusCartao.Ativo)
        {
            cartao.AlterarStatus(status);
        }
        return cartao;
    }
}
