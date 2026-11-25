# Testes do backend

79 testes automatizados com xUnit, Moq e EF Core com SQLite em memória. Eles não precisam de SQL Server nem de Arduino e rodam no GitHub Actions a cada push.

[← Voltar ao README principal](../../README.md)

## Como rodar

Na raiz do repositório:

```powershell
dotnet test ControleDeAcessoRfid.sln
```

## O que é testado

| Arquivo | Cobre |
|---|---|
| `AccessAttemptTests.cs`, `AcessoControllerTests.cs` | Decisão de acesso: cartão desconhecido, bloqueado, titular inativo, visitante vencido |
| `AuditTransactionTests.cs` | Rollback da operação quando a gravação da auditoria falha |
| `AuthControllerTests.cs`, `RefreshTokenPersistenceTests.cs` | Login, rotação e revogação de refresh token |
| `CardIssuanceTests.cs` | Emissão e reemissão de cartão, confirmação repetida, colisão de credencial |
| `StatusTransitionTests.cs` | Transições de status permitidas e proibidas (cartão cancelado não volta) |
| `UsuarioServiceTests.cs`, `FuncionarioListingTests.cs` | Cadastro e listagem de usuários e funcionários |
| `HistoricoServiceTests.cs` | Histórico de alterações |
| `LogAcessoExportTests.cs` | Exportação dos logs em CSV e PDF |
| `WorkerApiClientTests.cs`, `AccessFeedbackSerialProtocolTests.cs` | Cliente HTTP do Worker, retentativa e protocolo serial |
| `MedicaoTempoRespostaTests.cs` | Gravação e resumo das medições de tempo de resposta |

Os testes do frontend ficam em `frontend/` e rodam com `npm test` (veja o [README do frontend](../../frontend/README.md)).
