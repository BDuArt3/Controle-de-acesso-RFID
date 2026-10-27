# 🚀 Guia de Setup e Deploy - Sistema de Controle de Acesso RFID

Este documento descreve a estrutura do projeto, como rodar localmente e estratégias para deploy em nuvem (Azure).

---

## 1. 🏗️ Arquitetura do Projeto

A solução `ControleDeAcessoRfid.sln` é composta por **5 projetos** em arquitetura em camadas:

| Projeto | Camada | Responsabilidade |
| --- | --- | --- |
| `ControleDeAcesso.Core` | Domínio | Entidades (`Usuario`, `Cartao`, `Funcionario`, `LogAcesso`) |
| `ControleDeAcesso.Application` | Serviços | Lógica de negócio, DTOs, Interfaces de Repositório |
| `ControleDeAcesso.Infrastructure` | Dados | EF Core (`AppDbContext`), Migrations, Implementação de Repositórios |
| `ControleDeAcesso.Api` | Backend Web | Controllers REST, SignalR Hub (`GravacaoHub`), JWT Auth |
| `ControleDeAcesso.Worker` | Background | Comunicação com Arduino via Serial Port + SignalR Client |

---

## 2. 💻 Execução Local

⚠️ **Atenção:** `dotnet run` na raiz **não funciona**. A raiz contém apenas o arquivo `.sln`. É necessário apontar para o projeto específico.

### Pré-requisitos
- .NET 10 SDK instalado
- SQL Server (Local ou Express)
- Azure CLI (opcional, para deploy)

### Passos para rodar (ambos os processos simultaneamente)

1. **API (Backend Principal)**
   ```bash
   dotnet run --project src/Api/ControleDeAcesso.Api.csproj
   ```
   *O Swagger estará disponível em `http://localhost:5000/swagger`*

2. **Worker (Comunicação Serial com Arduino)**
   ```bash
   dotnet run --project src/Worker/ControleDeAcesso.Worker.csproj
   ```
   *Este processo deve rodar no mesmo PC onde o Arduino está conectado via USB.*

---

## 3. ⚙️ Configurações (`appsettings.json`)

O arquivo `appsettings.json` está excluído do `.gitignore` por segurança e **não está versionado**. Você deve criá-lo manualmente.

### `src/Api/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ControleDeAcesso;User Id=sa;Password=[SENHA_AQUI];TrustServerCertificate=true;"
  },
  "Jwt": {
    "SecretKey": "[SUA_CHAVE_SEGURA_AQUI_32_PLUS_CARACTERES]",
    "Issuer": "ControleDeAcesso.Api",
    "Audience": "ControleDeAcesso.Client",
    "ExpirationInMinutes": "60"
  },
  "WorkerAuth": {
    "ApiKey": "[SEGREDO_FORTE_COMPARTILHADO_COM_O_WORKER]"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### `src/Worker/appsettings.json`
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000"
  },
  "SerialPortSettings": {
    "ComPortName": "COM4"
  },
  "WorkerAuth": {
    "ApiKey": "[O_MESMO_SEGREDO_CONFIGURADO_NA_API]"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

Em produção, configure o segredo como `WorkerAuth__ApiKey` nas variáveis de ambiente da API e do Worker (o mesmo valor em ambos); não versione nem registre esse segredo. O Worker usa essa chave no SignalR e nos callbacks autenticados de confirmação/falha e verificação de acesso.

### 🔄 Banco de Dados
O fluxo de solicitações persistentes requer uma nova tabela `SolicitacoesGravacaoCartao`. **Não execute `dotnet ef database update` ainda:** o modelo EF já apresentava diferenças em relação à última migration antes desta etapa. Primeiro compare o esquema publicado com essas diferenças e gere/revise uma migration compatível; a migration desta etapa não foi criada nem aplicada para evitar incluir alterações de esquema anteriores sem confirmação.

Depois de reconciliar o drift e revisar o script SQL, aplique a migration no ambiente apropriado:
```bash
cd src/Api
dotnet ef database update
```

### 📄 Exportação de logs de acesso

Usuários com perfil `Admin` ou `Operador` podem exportar logs pela rota autenticada:

```text
GET /api/log-acesso/exportar?dataInicio=2026-10-01T00:00:00Z&dataFim=2026-10-02T00:00:00Z&formato=pdf
```

`dataInicio` e `dataFim` são obrigatórios. `acessoPermitido` é opcional (`true` para permitidos, `false` para negados). `formato` aceita `csv` ou `pdf`; se omitido, permanece `csv` para compatibilidade. O intervalo máximo continua sendo 90 dias, e ambos os formatos usam os mesmos filtros. O CSV mantém os campos e o conteúdo atuais. O PDF usa colunas amigáveis (data e hora, UID do cartão, usuário, tipo de usuário, resultado e motivo), mostra o período/filtro e totais de permitidos e negados, e converte as datas para o horário de Brasília. A tabela A4 horizontal repete o cabeçalho nas páginas; quando não há registros, o PDF exibe uma mensagem e os totais zerados. A API expõe o cabeçalho `Content-Disposition` via CORS para que o frontend possa obter o nome do arquivo.

A geração de PDF usa QuestPDF sob a Community License. Antes de usar em produção, confirme a elegibilidade da organização conforme os termos vigentes do QuestPDF; organizações não elegíveis precisam obter uma licença paga ou substituir a biblioteca por uma alternativa apropriada.

---

## 4. ☁️ Deploy em Nuvem (Azure)

Estratégia recomendada para manter o sistema rodando 24/7:

### A. API (Backend) → Azure App Service
A API é stateless e perfeita para o App Service.
- **Plano:** Linux Basic B1S ou Superior.
- **Build:** `dotnet publish -c Release -o ./publish`
- **Variáveis de Ambiente:** Configurar `ConnectionStrings:DefaultConnection`, `Jwt:SecretKey` etc., no Portal do Azure.

### B. Worker (Arduino Bridge) → VM Ubuntu (Azure Virtual Machine)
O Worker precisa de acesso à porta serial (USB) onde o Arduino está. O App Service **não suporta portas serial**.
- **Opção 1 (Recomendada):** Azure VM Ubuntu (B1s) + USB Passthrough (RDP + USB IP driver para conectar o cabo USB do PC local à VM).
- **Opção 2 (Simples):** Rodar o Worker em um **PC local** no colégio e conectar na API que está na nuvem.
- **Opção 3 (Avançada):** Docker no Container Apps (requer configuração especial de porta serial).

### C. Frontend
O frontend é mantido e publicado em outro repositório. Configure no App Service a origem pública do site em `Cors__AllowedOrigins__0` (e outros índices, se houver mais origens). Em desenvolvimento, a API permite as origens locais comuns.

---

## 5. 🛡️ Boas Práticas para Produção

1. **Service Windows para o Worker:**
   Para que o Worker nunca pare, instale-o como um serviço do Windows usando a biblioteca `Microsoft.Extensions.Hosting.WindowsServices`:
   ```bash
   dotnet publish -c Release -o ./publish
   sc create "RFIDWorker" binPath= "C:\caminho\do\worker.exe" start= auto
   ```
2. **Persistência de Refresh Tokens:**
   Atualmente os tokens refresh ficam em memória (`Dictionary`). Para produção na nuvem, substitua por um banco de dados (Redis ou tabela `RefreshTokens` no SQL).
3. **Segurança JWT:**
   Nunca compartilhe o `SecretKey`. Use Gerenciadores de Segredos (Azure Key Vault ou `dotnet user-secrets` em dev).
