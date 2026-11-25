# Worker (.NET)

Serviço que roda no computador ligado ao Arduino por USB. Ele é a ponte entre o hardware e a API: recebe as leituras pela porta serial, pergunta à API se o acesso é permitido e devolve a resposta ao Arduino. Também recebe da API, por SignalR, os comandos para gravar cartões novos.

[← Voltar ao README principal](../../README.md)

## Como funciona

**Leitura de cartão:**

1. O Arduino manda duas linhas pela serial: `UID:<uid do chip>` e `GUID:<credencial gravada no cartão>`.
2. O Worker junta as duas e chama `POST /api/acesso/verificar` com o header `X-Worker-Key`.
3. Se a API responder `200`, ele envia `ACESSO:PERMITIDO` ao Arduino. Qualquer outra resposta, inclusive erro de rede, vira `ACESSO:NEGADO`.
4. O tempo entre a leitura e a resposta é gravado em `medicoes-tempo-resposta.json` (formato JSON Lines).

**Gravação de cartão:**

1. A API envia o comando `ReceberComandoGravacao` pelo hub SignalR `/gravacaoHub`.
2. O Worker manda `WRITE:<credencial>` ao Arduino e espera até 17 segundos pela resposta.
3. Conforme o Arduino responda `SUCCESS:`, `ERROR:` ou `TIMEOUT:`, ele chama `POST /api/cartao/confirmar-gravacao` ou `POST /api/cartao/falhar-gravacao`.

O Worker tenta reconectar sozinho se a porta serial ou o SignalR caírem, e repete as chamadas à API em caso de falha temporária.

## Arquivos

| Arquivo | O que faz |
|---|---|
| `Worker.cs` | Loop principal: porta serial, SignalR e fluxo de leitura e gravação |
| `WorkerApiClient.cs` | Chamadas HTTP à API, com retentativa |
| `AccessFeedbackSerialProtocol.cs` | Converte a resposta da API em `ACESSO:PERMITIDO` ou `ACESSO:NEGADO` |
| `MedicaoTempoResposta.cs` | Grava e resume as medições de tempo de resposta |
| `Program.cs` | Inicialização do serviço e o comando `--resumo-medicoes` |

## Configuração

Copie o [`appsettings.Example.json`](appsettings.Example.json) para `appsettings.json` e preencha:

| Chave | Para que serve |
|---|---|
| `ApiSettings:BaseUrl` | Endereço da API (ex.: `http://localhost:5143`) |
| `WorkerAuth:ApiKey` | A mesma chave configurada em `WorkerAuth:ApiKey` na API |
| `SerialPortSettings:ComPortName` | Porta do Arduino (padrão `COM4`) |
| `AccessPoint:Location` | Nome do ponto de acesso gravado nos logs (ex.: "Portaria principal") |
| `Medicao:Habilitada`, `Medicao:CaminhoArquivo` | Liga a medição de tempo e define onde salvar |

## Como rodar

Com a API no ar e o Arduino conectado:

```powershell
dotnet run --project src/Worker
```

Para ver a média, o mínimo e o máximo dos tempos de resposta medidos:

```powershell
dotnet run --project src/Worker -- --resumo-medicoes
```
