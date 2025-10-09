using Microsoft.AspNetCore;
using ControleDeAcesso.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Infrastructure.Data.Repositories;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Api.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
            builder.Services.AddScoped<IPortadorRepository, PortadorRepository>();
            builder.Services.AddScoped<ICartaoRepository, CartaoRepository>();
            builder.Services.AddScoped<ILogAcessoRepository, LogAcessoRepository>();

            // Registrar serviços
            builder.Services.AddScoped<IFuncionarioService, FuncionarioService>();
            builder.Services.AddScoped<IPortadorService, PortadorService>();
            builder.Services.AddScoped<ICartaoService, CartaoService>();
            builder.Services.AddScoped<ILogAcessoService, LogAcessoService>();
            builder.Services.AddScoped<IAcessoService, AcessoService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // Configuração JWT
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
            var key = Encoding.UTF8.GetBytes(secretKey);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Para desenvolvimento
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                // Política para Administradores - acesso total
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                
                // Política para Operadores e Administradores - pode cadastrar portadors e gravar cartões
                options.AddPolicy("AdminOrOperador", policy => policy.RequireRole("Admin", "Operador"));
                
                // Política para todos os funcionários - pode pelo menos visualizar
                options.AddPolicy("TodosFuncionarios", policy => policy.RequireRole("Admin", "Operador", "Visualizador"));
            });

            // Add services to the container.
            builder.Services.AddControllers();

            // Configurar CORS para permitir comunicação com frontend
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:5173", 
                            "http://localhost:3000", 
                            "http://localhost:4173",
                            "https://*.azurestaticapps.net",  // Permitir Azure Static Web Apps (wildcard)
                            "https://witty-mushroom-06bc22d0f.2.azurestaticapps.net")  // URL específica do seu site
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Adicionar SignalR para comunicação em tempo real
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Swagger habilitado para todos os ambientes (desenvolvimento e produção)
            app.UseSwagger();
            app.UseSwaggerUI();

            // Comentado para evitar problemas com certificados em desenvolvimento
            // app.UseHttpsRedirection();

            // Usar a política CORS configurada
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();


            // Mapear o Hub SignalR para gravação de cartões RFID
            app.MapHub<GravacaoHub>("/gravacaoHub");

            app.Run();


        }
    }
}