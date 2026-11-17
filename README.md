# Controle-de-acesso-RFID

Sistema de controle de acesso RFID com API ASP.NET Core, serviço Worker conectado
ao Arduino e persistência em SQL Server.

Para entender a arquitetura, as regras de negócio, as tecnologias, os pontos
importantes do código e se preparar para uma apresentação, consulte o
[Guia de estudo e apresentação](docs/GUIA_DE_ESTUDO_E_APRESENTACAO.md).

```mermaid

graph TD
    subgraph "Arduino UNO"
        A_GND[GND]
        A_3V3[3.3V]
        A_P9["Pino 9 - RST"]
        A_P10["Pino 10 - SDA"]
        A_P11["Pino 11 - MOSI"]
        A_P12["Pino 12 - MISO"]
        A_P13["Pino 13 - SCK"]
        A_USB(("USB para PC"))
    end

    subgraph "Leitor MFRC522"
        R_GND[GND]
        R_3V3[3.3V]
        R_RST[RST]
        R_SDA[SDA]
        R_MOSI[MOSI]
        R_MISO[MISO]
        R_SCK[SCK]
    end

    %% Conexões do Arduino para o Leitor MFRC522
    A_GND -- "GND" --> R_GND
    A_3V3 -- "Alimentação 3.3V" --> R_3V3
    A_P9 -- "Reset" --> R_RST
    A_P10 -- "Slave Select" --> R_SDA
    A_P11 -- "MOSI" --> R_MOSI
    A_P12 -- "MISO" --> R_MISO
    A_P13 -- "Clock" --> R_SCK

    %% Conexão USB para PC
    A_USB -.-> |"Comunicação Serial"| PC[("💻 PC<br>(Aplicação C#)")]

    ```