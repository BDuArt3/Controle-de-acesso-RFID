# Firmware do Arduino

Código do Arduino UNO que lê e grava cartões RFID com o módulo MFRC522 e aciona os LEDs e o relé da catraca conforme a resposta que chega do Worker.

[← Voltar ao README principal](../../README.md)

<!-- TODO: imagem - foto do circuito montado -->

## Componentes

- Arduino UNO
- Leitor RFID MFRC522 (alimentado em **3.3V**)
- Cartões ou chaveiros MIFARE Classic 1K
- LED verde, LED vermelho e resistores
- Módulo relé (representa a trava da catraca)
- Cabo USB até o computador que roda o Worker

## Ligações

| MFRC522 | Arduino UNO |
|---|---|
| SDA (SS) | Pino 10 |
| SCK | Pino 13 |
| MOSI | Pino 11 |
| MISO | Pino 12 |
| RST | Pino 9 |
| 3.3V | 3.3V |
| GND | GND |

| Saída | Pino |
|---|---|
| LED verde (acesso permitido) | 7 |
| LED vermelho (acesso negado) | 6 |
| Relé da catraca | 5 |

O diagrama completo das ligações está no [README principal](../../README.md#ligação-do-arduino-com-o-leitor).

## Protocolo serial (9600 baud)

**Arduino → Worker**

| Mensagem | Quando |
|---|---|
| `UID:<uid>` | Um cartão foi aproximado (identificador físico do chip) |
| `GUID:<credencial>` | Credencial de 16 caracteres lida do bloco 4 do cartão |
| `SUCCESS: ...` | Gravação concluída |
| `ERROR: ...` | Cartão incompatível, falha de autenticação ou de escrita |
| `TIMEOUT: ...` | Nenhum cartão aproximado em 15 segundos durante a gravação |

**Worker → Arduino**

| Comando | Efeito |
|---|---|
| `ACESSO:PERMITIDO` | Acende o LED verde e aciona o relé por 2 segundos |
| `ACESSO:NEGADO` | Acende o LED vermelho por 2 segundos; a catraca continua travada |
| `WRITE:<credencial>` | Espera um cartão e grava a credencial no bloco 4 |

O Arduino não decide nada sozinho: quem libera ou nega o acesso é a API.

## Como gravar na placa

1. Instale a [Arduino IDE](https://www.arduino.cc/en/software).
2. Em **Ferramentas → Gerenciar bibliotecas**, instale a biblioteca **MFRC522**.
3. Abra `arduino/arduino.ino`, selecione a placa **Arduino UNO** e a porta COM e clique em **Carregar**.
4. Abra o Monitor Serial em 9600 baud. Deve aparecer `LEITOR OK` e `Dispositivo de Borda Pronto.`.
5. Feche o Monitor Serial antes de iniciar o Worker, porque os dois não podem usar a mesma porta ao mesmo tempo.
