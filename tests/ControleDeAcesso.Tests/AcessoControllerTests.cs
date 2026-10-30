using ControleDeAcesso.Api.Controllers;
using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class AcessoControllerTests
{
    private const string PhysicalUid = "04A1B2C3";
    private const string LogicalCredential = "0123456789ABCDEF";
    private const string AccessLocation = "Entrada Bloco A";

    [Fact]
    public async Task VerificarAcesso_PrioritizesGuidCredencialOverPhysicalUid()
    {
        var acessoService = new Mock<IAcessoService>();
        acessoService
            .Setup(s => s.CheckAccessAsync(LogicalCredential, AccessLocation))
            .ReturnsAsync(new AccessCheckResponseDto
            {
                Allowed = true,
                Message = "Acesso autorizado para Teste",
                UsuarioNome = "Teste"
            });

        var controller = new AcessoController(
            acessoService.Object,
            NullLogger<AcessoController>.Instance);

        var request = new VerificarAcessoRequestDto
        {
            Uid = PhysicalUid,
            Guid = LogicalCredential,
            Local = AccessLocation
        };

        var response = await controller.VerificarAcesso(request);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var dto = Assert.IsType<AccessCheckResponseDto>(okResult.Value);
        Assert.True(dto.Allowed);
        acessoService.Verify(s => s.CheckAccessAsync(LogicalCredential, AccessLocation), Times.Once);
        acessoService.Verify(s => s.CheckAccessAsync(PhysicalUid, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerificarAcesso_FallsBackToUid_WhenGuidIsEmpty()
    {
        var acessoService = new Mock<IAcessoService>();
        acessoService
            .Setup(s => s.CheckAccessAsync(PhysicalUid, AccessLocation))
            .ReturnsAsync(new AccessCheckResponseDto
            {
                Allowed = true,
                Message = "Acesso autorizado"
            });

        var controller = new AcessoController(
            acessoService.Object,
            NullLogger<AcessoController>.Instance);

        var request = new VerificarAcessoRequestDto
        {
            Uid = PhysicalUid,
            Guid = "",
            Local = AccessLocation
        };

        var response = await controller.VerificarAcesso(request);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var dto = Assert.IsType<AccessCheckResponseDto>(okResult.Value);
        Assert.True(dto.Allowed);
        acessoService.Verify(s => s.CheckAccessAsync(PhysicalUid, AccessLocation), Times.Once);
    }

    [Fact]
    public async Task VerificarAcesso_Returns403_WhenAccessDenied()
    {
        var acessoService = new Mock<IAcessoService>();
        acessoService
            .Setup(s => s.CheckAccessAsync(LogicalCredential, AccessLocation))
            .ReturnsAsync(new AccessCheckResponseDto
            {
                Allowed = false,
                Message = "Cartão desativado."
            });

        var controller = new AcessoController(
            acessoService.Object,
            NullLogger<AcessoController>.Instance);

        var request = new VerificarAcessoRequestDto
        {
            Uid = PhysicalUid,
            Guid = LogicalCredential,
            Local = AccessLocation
        };

        var response = await controller.VerificarAcesso(request);

        var statusResult = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(403, statusResult.StatusCode);
        var dto = Assert.IsType<AccessCheckResponseDto>(statusResult.Value);
        Assert.False(dto.Allowed);
    }

    [Fact]
    public async Task VerificarAcesso_Returns500_WhenExceptionOccurs()
    {
        var acessoService = new Mock<IAcessoService>();
        acessoService
            .Setup(s => s.CheckAccessAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Falha no banco"));

        var controller = new AcessoController(
            acessoService.Object,
            NullLogger<AcessoController>.Instance);

        var request = new VerificarAcessoRequestDto
        {
            Uid = PhysicalUid,
            Guid = LogicalCredential,
            Local = AccessLocation
        };

        var response = await controller.VerificarAcesso(request);

        var statusResult = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(500, statusResult.StatusCode);
        var dto = Assert.IsType<AccessCheckResponseDto>(statusResult.Value);
        Assert.False(dto.Allowed);
        Assert.Equal("Erro interno do servidor", dto.Message);
    }
}
