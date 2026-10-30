# Roteiro Manual de Testes

Execute os cenários em ambiente local/de teste, sem usar o banco de produção.

## Preparação

1. Configure uma conexão de desenvolvimento para o SQL Server e aplique as migrations:
   ```powershell
   dotnet ef database update --project src/Infrastructure/ControleDeAcesso.Infrastructure.csproj --startup-project src/Api/ControleDeAcesso.Api.csproj
   ```
2. No Worker, configure `AccessPoint__Location` com o nome do ponto de acesso (se omitido, o sistema usa `Entrada Principal`).
3. Para testar o Arduino Uno/MFRC522: confirmar LEDs em D7/D6 e saída D5 para um módulo/driver de relé compatível. Não ligar bobina de relé diretamente ao Arduino; verificar polaridade do módulo.

## Cenários

| # | Funcionalidade / Ação | Resultado esperado |
|---|---|---|
| 1 | Apresentar cartão ativo com credencial válida ao leitor. | API autoriza; Worker envia `ACESSO:PERMITIDO`; Arduino acende LED verde, aciona relé por cerca de 2 s e depois desliga as saídas. O log registra o local configurado. |
| 2 | Apresentar cartão desconhecido, bloqueado ou expirado. | API nega; Worker envia `ACESSO:NEGADO`; Arduino acende LED vermelho por cerca de 2 s e mantém o relé desacionado. O log contém resultado, motivo e local. |
| 3 | Indisponibilizar a API ou enviar dados de leitura inválidos. | Worker falha fechado: envia `ACESSO:NEGADO` quando a porta serial está disponível. O erro é registrado; não ocorre liberação do relé. |
| 4 | Consultar Logs de Acesso e pesquisar pelo nome do local configurado. | A tela mostra `Local` em cada registro e filtra pelo local digitado, além dos filtros existentes de UID, usuário, período e resultado. |
| 5 | Exportar Logs de Acesso nos formatos CSV e PDF. | Ambos os arquivos contêm a coluna `Local`, com o valor correspondente ao registro e sem alterar os filtros escolhidos. |
| 6 | Desativar um usuário que possui cartão ativo, informando motivo. | Usuário fica inativo; cartões ativos ficam bloqueados e recebem histórico com motivo/responsável. Cartões já bloqueados/cancelados não são reativados nem alterados. Nova tentativa de acesso é negada. |
| 7 | Como Admin, usar **Excluir** em um funcionário e tentar confirmar sem motivo. | Sem motivo, a operação não é enviada. Após informar motivo, o funcionário é desativado logicamente, não removido fisicamente; cartões ativos são bloqueados e a lista é atualizada. |
| 8 | Entrar com credenciais inválidas e depois com credenciais válidas. | Falha mostra Toast e alerta inline, mantendo a tela de login. Sucesso autentica e encaminha ao sistema. |
| 9 | Encerrar a sessão pelo botão **Sair**. | Frontend chama a API para revogar o refresh token e depois limpa a sessão local e volta ao login. O token antigo não renova a sessão. |
| 10 | Deixar a sessão expirar ou provocar uma resposta 401 em uma chamada autenticada. | O hook tenta revogar o refresh token; encerra a sessão local e retorna ao login. Se a API de logout estiver indisponível, exibe Toast de erro sem manter a sessão local aberta. |
| 11 | Fazer login, reiniciar a API sem reiniciar o banco e renovar a sessão com o refresh token. | A renovação continua válida após reinício, retorna novo par de tokens e invalida o refresh token anterior. O logout revoga o token atual. |
| 12 | Entrar nos módulos com Admin, Operador e Visualizador. | Admin acessa Funcionários e edita; Operador pode cadastrar usuários e gerir cartões, mas não acessa Funcionários nem edita usuários; Visualizador permanece somente leitura. Os três podem consultar/filtrar e exportar Logs conforme a matriz RBAC. |
| 13 | Executar o resumo das medições do Worker com arquivo JSON Lines válido, uma linha corrompida e um arquivo legado em array JSON. | `--resumo-medicoes` mostra as estatísticas válidas; informa registros inválidos sem encerrar com exceção; ao registrar em arquivo legado, converte para JSON Lines. |

## Validação automatizada após os testes manuais

- Backend: `dotnet build` e `dotnet test`.
- Frontend: `npm test -- --run` e `npm run build:local`.
- Firmware: compilar e carregar `src/Arduino/arduino/arduino.ino` no Arduino IDE/CLI antes dos cenários físicos 1–3.
