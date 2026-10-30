/*
 * --------------------------------------------------------------------------------------------------------------------
 * TCC - Sistema de Controle de Acesso RFID
 * Firmware para o dispositivo de borda (Arduino UNO + MFRC522)
 * Versão: 2.0 - Leitura e Gravação
 *
 * Funcionalidade:
 * 1. MODO LEITURA (Padrão): Ao aproximar um cartão, lê o UID físico e tenta ler um GUID
 * gravado em um bloco de memória específico. Envia ambos os dados para o computador.
 * 2. MODO GRAVAÇÃO (Ativado por Comando): Ouve a porta serial por um comando "WRITE:<GUID>".
 * Ao receber o comando, espera um cartão ser aproximado e grava o GUID fornecido na memória dele.
 * 3. FEEDBACK DE ACESSO: interpreta ACESSO:PERMITIDO/ACESSO:NEGADO para sinalização visual
 * e acionamento temporizado do relé da catraca.
 * --------------------------------------------------------------------------------------------------------------------
 */

// Inclusão das bibliotecas necessárias
#include <SPI.h>
#include <MFRC522.h>

// Definição dos pinos de controle do leitor
#define SS_PIN  10
#define RST_PIN 9

// Pinos livres no Uno considerando MFRC522 (D9-D13) e serial USB (D0-D1).
#define LED_VERDE_PIN 7
#define LED_VERMELHO_PIN 6
#define CATRACA_RELE_PIN 5
#define CATRACA_RELE_NIVEL_ATIVO HIGH
#define TEMPO_FEEDBACK_ACESSO_MS 2000UL

// Cria a instância do leitor MFRC522
MFRC522 mfrc522(SS_PIN, RST_PIN);

// Chave de autenticação padrão para cartões MIFARE Classic virgens.
// Essencial para ter permissão de ler e escrever nos blocos de memória.
MFRC522::MIFARE_Key key;

// Bloco da memória do cartão onde vamos ler e gravar o GUID.
// Bloco 4 é uma escolha segura, pois não é um "sector trailer" (que armazena as chaves).
byte blocoGuid = 4;

bool feedbackAcessoAtivo = false;
unsigned long fimFeedbackAcessoMs = 0;

//=======================================================================
// FUNÇÃO SETUP: Executada uma única vez ao ligar o Arduino.
//=======================================================================
void setup() {
  pinMode(LED_VERDE_PIN, OUTPUT);
  pinMode(LED_VERMELHO_PIN, OUTPUT);
  pinMode(CATRACA_RELE_PIN, OUTPUT);
  digitalWrite(LED_VERDE_PIN, LOW);
  digitalWrite(LED_VERMELHO_PIN, LOW);
  digitalWrite(CATRACA_RELE_PIN, CATRACA_RELE_NIVEL_ATIVO == HIGH ? LOW : HIGH);

  Serial.begin(9600); // Inicia comunicação com o PC
  while (!Serial);

  SPI.begin();      // Inicia o barramento SPI
  mfrc522.PCD_Init(); // Inicia o leitor MFRC522

  // Prepara a chave de autenticação (6 bytes com valor 0xFF)
  for (byte i = 0; i < 6; i++) {
    key.keyByte[i] = 0xFF;
  }

  // Diagnóstico: 0x00 ou 0xFF indica que o MFRC522 não responde (fiação/alimentação)
  byte versaoLeitor = mfrc522.PCD_ReadRegister(MFRC522::VersionReg);
  if (versaoLeitor == 0x00 || versaoLeitor == 0xFF) {
    Serial.println(F("LEITOR NAO RESPONDE. Verifique fiacao e alimentacao 3.3V."));
  } else {
    Serial.print(F("LEITOR OK. Versao do chip: 0x"));
    Serial.println(versaoLeitor, HEX);
  }

  Serial.println(F("Dispositivo de Borda Pronto."));
  Serial.println(F("Aguardando leitura de cartao ou comando de gravacao..."));
}

//=======================================================================
// FUNÇÃO LOOP: Executada continuamente.
//=======================================================================
void loop() {
  atualizarFeedbackAcesso();

  // --- Verificação de Comando de Gravação ---
  // Primeiro, verifica se o Worker Service enviou algum comando.
  if (Serial.available() > 0) {
    String comando = Serial.readStringUntil('\n');
    comando.trim();

    if (comando == "ACESSO:PERMITIDO") {
      iniciarFeedbackAcesso(true);
    } else if (comando == "ACESSO:NEGADO") {
      iniciarFeedbackAcesso(false);
    // Se o comando for para gravar (começa com "WRITE:")
    } else if (comando.startsWith("WRITE:")) {
      String guidParaGravar = comando.substring(6); // Extrai apenas o GUID do comando
      if (guidParaGravar.length() == 16) {
        Serial.println("Comando de gravacao recebido. Aproxime o cartao para gravar o GUID: " + guidParaGravar);
        aguardarParaGravar(guidParaGravar); // Chama a função que gerencia a gravação
      } else {
        Serial.println("ERROR: GUID recebido tem tamanho invalido. Esperado: 16 caracteres, Recebido: " + String(guidParaGravar.length()));
      }
    }
  }

  // --- Lógica de Leitura Padrão ---
  // Se não houver cartão, não faz nada e recomeça o loop.
  if (!mfrc522.PICC_IsNewCardPresent()) {
    return;
  }

  // Tenta ler o serial do cartão
  if (!mfrc522.PICC_ReadCardSerial()) {
    return;
  }

  // Se chegou até aqui, um cartão foi lido com sucesso.
  lerEEnviarDadosDoCartao();

  // Para a comunicação com o cartão e espera um pouco para evitar leituras repetidas.
  mfrc522.PICC_HaltA();
  delay(1500);
}

void iniciarFeedbackAcesso(bool permitido) {
  digitalWrite(LED_VERDE_PIN, LOW);
  digitalWrite(LED_VERMELHO_PIN, LOW);
  digitalWrite(CATRACA_RELE_PIN, CATRACA_RELE_NIVEL_ATIVO == HIGH ? LOW : HIGH);

  if (permitido) {
    digitalWrite(LED_VERDE_PIN, HIGH);
    digitalWrite(CATRACA_RELE_PIN, CATRACA_RELE_NIVEL_ATIVO);
    Serial.println(F("INFO: Acesso permitido; rele acionado."));
  } else {
    digitalWrite(LED_VERMELHO_PIN, HIGH);
    Serial.println(F("INFO: Acesso negado; catraca permanece bloqueada."));
  }

  feedbackAcessoAtivo = true;
  fimFeedbackAcessoMs = millis() + TEMPO_FEEDBACK_ACESSO_MS;
}

void atualizarFeedbackAcesso() {
  if (!feedbackAcessoAtivo ||
      (long)(millis() - fimFeedbackAcessoMs) < 0) {
    return;
  }

  digitalWrite(LED_VERDE_PIN, LOW);
  digitalWrite(LED_VERMELHO_PIN, LOW);
  digitalWrite(CATRACA_RELE_PIN, CATRACA_RELE_NIVEL_ATIVO == HIGH ? LOW : HIGH);
  feedbackAcessoAtivo = false;
}

//=======================================================================
// FUNÇÃO PARA LER E ENVIAR OS DADOS DO CARTÃO
//=======================================================================
void lerEEnviarDadosDoCartao() {
  // 1. Lê e envia o UID Físico
  String uidFisico = "";
  for (byte i = 0; i < mfrc522.uid.size; i++) {
    if (mfrc522.uid.uidByte[i] < 0x10) uidFisico += "0";
    uidFisico += String(mfrc522.uid.uidByte[i], HEX);
  }
  uidFisico.toUpperCase();
  Serial.println("UID:" + uidFisico); // Envia com prefixo "UID:"

  // 2. Tenta ler e enviar o GUID gravado na memória
  byte buffer[18];
  byte tamanho = sizeof(buffer);
  
  // Autentica o bloco para leitura
  MFRC522::StatusCode status = mfrc522.PCD_Authenticate(MFRC522::PICC_CMD_MF_AUTH_KEY_A, blocoGuid, &key, &(mfrc522.uid));

  if (status == MFRC522::STATUS_OK) {
    // Lê o bloco
    status = mfrc522.MIFARE_Read(blocoGuid, buffer, &tamanho);
    if (status == MFRC522::STATUS_OK) {
      String guidGravado = "";
      for (uint8_t i = 0; i < 16; i++) {
        guidGravado += (char)buffer[i];
      }
      Serial.println("GUID:" + guidGravado); // Envia com prefixo "GUID:"
    } else {
      Serial.println("GUID:Falha na leitura");
    }
  } else {
    Serial.println("GUID:Falha na autenticacao");
  }
  mfrc522.PCD_StopCrypto1();
}

//=======================================================================
// FUNÇÃO QUE GERENCIA O PROCESSO DE GRAVAÇÃO
//=======================================================================
void aguardarParaGravar(String guid) {
  long tempoInicio = millis();
  // Fica esperando por um cartão por até 15 segundos
  while ((millis() - tempoInicio) < 15000) {
    atualizarFeedbackAcesso();

    if (mfrc522.PICC_IsNewCardPresent() && mfrc522.PICC_ReadCardSerial()) {
      
      // Verificar o tipo do cartão
      MFRC522::PICC_Type piccType = mfrc522.PICC_GetType(mfrc522.uid.sak);
      Serial.println("DEBUG: Cartao detectado. Tipo: " + String(mfrc522.PICC_GetTypeName(piccType)));
      
      // Verificar se é um cartão compatível
      if (piccType != MFRC522::PICC_TYPE_MIFARE_MINI &&
          piccType != MFRC522::PICC_TYPE_MIFARE_1K &&
          piccType != MFRC522::PICC_TYPE_MIFARE_4K) {
        Serial.println("ERROR: Cartao nao compativel. Use cartao MIFARE Classic.");
        mfrc522.PICC_HaltA();
        return;
      }
      
      // Mostrar UID do cartão
      String uidString = "";
      for (byte i = 0; i < mfrc522.uid.size; i++) {
        if (mfrc522.uid.uidByte[i] < 0x10) uidString += "0";
        uidString += String(mfrc522.uid.uidByte[i], HEX);
      }
      Serial.println("DEBUG: UID do cartao: " + uidString);
      
      // Converte a String do GUID para um array de bytes
      byte dadosParaGravar[16];
      guid.getBytes(dadosParaGravar, 17);
      
      Serial.println("DEBUG: Tentando gravar no bloco " + String(blocoGuid) + "...");

      // Tenta gravar no cartão
      if (escreverNoBloco(blocoGuid, dadosParaGravar)) {
        Serial.println("SUCCESS: Gravacao concluida.");
      } else {
        Serial.println("ERROR: Falha na gravacao - verifique se o cartao nao esta protegido contra escrita.");
      }
      
      // Finalizar comunicação com o cartão e resetar estado
      mfrc522.PICC_HaltA();
      mfrc522.PCD_StopCrypto1();
      
      // Pequena pausa antes de voltar ao modo normal
      delay(1000);
      
      Serial.println("INFO: Voltando ao modo de leitura normal...");
      return; // Sai da função após a tentativa
    }
  }
  Serial.println("TIMEOUT: Nenhum cartao aproximado em 15 segundos.");
}

//=======================================================================
// FUNÇÃO DE BAIXO NÍVEL PARA ESCREVER EM UM BLOCO
//=======================================================================
bool escreverNoBloco(byte bloco, byte dados[]) {
  // Autentica o bloco para escrita
  MFRC522::StatusCode status = mfrc522.PCD_Authenticate(MFRC522::PICC_CMD_MF_AUTH_KEY_A, bloco, &key, &(mfrc522.uid));
  if (status != MFRC522::STATUS_OK) {
    Serial.println("ERROR: Falha na autenticacao para escrita. Status: " + String(status));
    mfrc522.PCD_StopCrypto1(); // Limpar estado de autenticação
    return false; // Falha na autenticação
  }

  // Escreve os dados no bloco
  status = mfrc522.MIFARE_Write(bloco, dados, 16);
  if (status != MFRC522::STATUS_OK) {
    Serial.println("ERROR: Falha na escrita no bloco " + String(bloco) + ". Status: " + String(status));
    mfrc522.PCD_StopCrypto1(); // Limpar estado de autenticação
    return false; // Falha na escrita
  }

  Serial.println("DEBUG: Gravacao no bloco " + String(bloco) + " bem-sucedida.");
  mfrc522.PCD_StopCrypto1(); // Limpar estado de autenticação
  return true; // Sucesso
}
