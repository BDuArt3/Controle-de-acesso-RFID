using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Core.Entities;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface IAcessoService
    {
        Task<AccessCheckResponseDto> CheckAccessAsync(
            string cardUid,
            string local = LogAcesso.LocalPadrao);
    }
}