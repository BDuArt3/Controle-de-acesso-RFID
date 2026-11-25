# API (ASP.NET Core)

API REST que centraliza as regras do sistema: decide se um cartão pode passar, registra cada tentativa, gerencia pessoas, funcionários e cartões, e envia ao Worker os comandos de gravação de cartão por SignalR.

[← Voltar ao README principal](../../README.md)

## Responsabilidades

- **Decidir o acesso** (`POST /api/acesso/verificar`): confere o cartão, o titular, o status e a validade de visitante, grava o log e responde `200` (permitido) ou `403` (negado). Se o log não puder ser gravado, o acesso é negado.
- **Autenticar funcionários** com JWT e refresh token com rotação. As senhas ficam guardadas com hash BCrypt.
- **Autenticar o Worker** com uma chave de máquina no header `X-Worker-Key` (pelo menos 32 caracteres, comparada em tempo constante).
- **Emitir cartões:** cria a solicitação, envia o comando ao Worker pelo hub SignalR `/gravacaoHub` e só ativa o cartão depois da confirmação do hardware.
- **Gerar relatórios** de acesso em CSV e PDF (QuestPDF).

## Perfis e permissões

| Política | Quem passa | Usada em |
|---|---|---|
| `AdminOnly` | Admin | Funcionários e histórico de alterações |
| `AdminOrOperador` | Admin, Operador | Criar e alterar usuários e cartões |
| `TodosFuncionarios` | Admin, Operador, Visualizador | Listagens, logs e exportação |
| `WorkerCallback` | Só o Worker (chave de máquina) | Verificação de acesso, confirmação de gravação e hub SignalR |

## Principais rotas

| Área | Rotas |
|---|---|
| Autenticação | `POST /api/auth/login`, `POST /api/auth/refresh-token`, `POST /api/auth/logout`, `GET /api/auth/me` |
| Acesso físico | `POST /api/acesso/verificar` |
| Usuários (titulares) | `GET/POST/PUT/DELETE /api/usuario` |
| Funcionários | `GET/POST/PUT/DELETE /api/funcionario`, `PATCH /api/funcionario/{id}/ativar` |
| Cartões | `GET/POST/PUT/DELETE /api/cartao`, `/api/cartao/solicitacoes/{id}` (consultar, retentar, cancelar) |
| Callbacks do Worker | `POST /api/cartao/confirmar-gravacao`, `POST /api/cartao/falhar-gravacao` |
| Logs | `GET /api/log-acesso`, `/api/log-acesso/exportar`, `/periodo`, `/ultimos`, `/estatisticas/hoje` |
| Histórico | `GET /api/usuario/{id}/historico`, `/api/funcionario/{id}/historico`, `/api/cartao/{id}/historico` |
| Infraestrutura | `GET /health`, `/swagger` |

A lista completa, com os modelos de requisição e resposta, aparece no Swagger.

## Organização do código

| Pasta | Conteúdo |
|---|---|
| `Controllers/` | Endpoints REST |
| `Hubs/` | `GravacaoHub`, o canal SignalR com o Worker |
| `Security/` | Autenticação do Worker por chave de máquina |
| `Reports/` | Gerador do relatório PDF |
| `Program.cs` | Injeção de dependência, JWT, políticas, CORS, Swagger e seed do primeiro administrador |

As regras de negócio não ficam aqui: os controllers chamam os serviços de `src/Application`, que usam as entidades de `src/Core` e os repositórios de `src/Infrastructure`.

## Configuração

Copie o [`appsettings.Example.json`](appsettings.Example.json) para `appsettings.Development.json` e preencha:

| Chave | Para que serve |
|---|---|
| `ConnectionStrings:DefaultConnection` | Conexão com o SQL Server |
| `Jwt:SecretKey`, `Issuer`, `Audience`, `ExpirationInMinutes` | Assinatura e validade do token |
| `WorkerAuth:ApiKey` | Chave compartilhada com o Worker |
| `BootstrapAdmin:Password`, `BootstrapAdmin:Document` | Primeiro administrador (`admin@tcc.com`), criado na primeira execução |
| `Cors:AllowedOrigins` | Endereços do frontend que podem chamar a API |

## Como rodar

Na raiz do repositório:

```powershell
cd src/Api
dotnet tool restore
dotnet ef database update --project ../Infrastructure --startup-project .
cd ../..
dotnet run --project src/Api
```

A API sobe em `http://localhost:5143` (Swagger em `/swagger`).
