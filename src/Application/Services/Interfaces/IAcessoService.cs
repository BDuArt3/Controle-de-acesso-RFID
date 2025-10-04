using ControleDeAcesso.Application.DTOs;
using System.Threading.Tasks;

namespace ControleDeAcesso.Application.Services.Interfaces
{
    public interface IAcessoService
    {
        Task<AccessCheckResponseDto> CheckAccessAsync(string cardUid);
    }
}