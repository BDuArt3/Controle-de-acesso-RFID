using Microsoft.AspNetCore;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Infrastructure.Data.Repositories;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Api.Hubs;

namespace ControleDeAcesso.Api
{

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                }));

            // Registrar repositórios
            builder.Services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddScoped<ICartaoRepository, CartaoRepository>();
            builder.Services.AddScoped<ILogAcessoRepository, LogAcessoRepository>();

            // Registrar serviços
            builder.Services.AddScoped<IFuncionarioService, FuncionarioService>();
            builder.Services.AddScoped<IUsuarioService, UsuarioService>();
            builder.Services.AddScoped<ICartaoService, CartaoService>();
            builder.Services.AddScoped<ILogAcessoService, LogAcessoService>();
            builder.Services.AddScoped<IAcessoService, AcessoService>();

            // Add services to the container.
            builder.Services.AddControllers();

            // Adicionar SignalR para comunicação em tempo real
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Swagger habilitado para todos os ambientes (desenvolvimento e produção)
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();


            // Mapear o Hub SignalR para gravação de cartões RFID
            app.MapHub<GravacaoHub>("/gravacaoHub").RequireCors("SignalRPolicy");

            app.Run();


        }
    }
}