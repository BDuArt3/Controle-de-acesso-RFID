# Fase 3: SignalR e Integração RFID em Tempo Real

## Contexto do SignalR

O backend possui um hub SignalR em `http://localhost:5038/hubs/notificacoes` com métodos:
- `sendNotification(message)`
- `sendAccessGranted(uid, userName)`
- `sendAccessDenied(uid, reason)`

O Worker Service em Python (Arduino + MFRC522) envia o UID via Serial para o Worker C# que processa e chama o hub.

## Tarefas da Fase 3

### Tarefa 3.1: Criar serviço SignalR (`app/services/signalR.ts`)

```typescript
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

let hubConnection: HubConnection | null = null;

export const signalRService = {
  async start() {
    if (hubConnection) {
      await hubConnection.stop();
    }

    hubConnection = new HubConnectionBuilder()
      .withUrl('http://localhost:5038/hubs/notificacoes', {
        accessTokenFactory: () => localStorage.getItem('authToken') || ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    // Handler: nova notificação
    hubConnection.on('ReceiveNotification', (message: string) => {
      window.dispatchEvent(new CustomEvent('signalr-notification', {
        detail: { message, type: 'info' }
      }));
    });

    // Handler: acesso autorizado
    hubConnection.on('ReceiveAccessGranted', (uid: string, userName: string) => {
      window.dispatchEvent(new CustomEvent('signalr-access-granted', {
        detail: { uid, userName, timestamp: new Date().toISOString() }
      }));
    });

    // Handler: acesso negado
    hubConnection.on('ReceiveAccessDenied', (uid: string, reason: string) => {
      window.dispatchEvent(new CustomEvent('signalr-access-denied', {
        detail: { uid, reason, timestamp: new Date().toISOString() }
      }));
    });

    try {
      await hubConnection.start();
      console.log('SignalR connected');
      return true;
    } catch (err) {
      console.error('SignalR connection failed:', err);
      return false;
    }
  },

  async stop() {
    if (hubConnection) {
      await hubConnection.stop();
      hubConnection = null;
    }
  },

  getConnection() {
    return hubConnection;
  }
};
```

### Tarefa 3.2: Instalar dependência SignalR

```bash
npm install @microsoft/signalr
```

### Tarefa 3.3: Criar hook SignalR (`app/hooks/useSignalR.ts`)

```typescript
import { useEffect, useState, useCallback } from 'react';
import { signalRService } from '../services/signalR';

interface AccessEvent {
  uid: string;
  userName?: string;
  reason?: string;
  type: 'granted' | 'denied';
  timestamp: string;
}

export function useSignalR() {
  const [isConnected, setIsConnected] = useState(false);
  const [recentEvents, setRecentEvents] = useState<AccessEvent[]>([]);
  const [isReading, setIsReading] = useState(false);
  const [lastReadUID, setLastReadUID] = useState('');

  useEffect(() => {
    signalRService.start().then((connected) => {
      setIsConnected(connected);
    });

    // Listener: acesso autorizado
    const handleGranted = (e: CustomEvent) => {
      const { uid, userName, timestamp } = e.detail;
      setRecentEvents(prev => [{ uid, userName, type: 'granted', timestamp }, ...prev].slice(0, 50));
      setLastReadUID(uid);
      // Auto-clear lastReadUID after 5 seconds
      setTimeout(() => setLastReadUID(''), 5000);
    };

    // Listener: acesso negado
    const handleDenied = (e: CustomEvent) => {
      const { uid, reason, timestamp } = e.detail;
      setRecentEvents(prev => [{ uid, reason, type: 'denied', timestamp }, ...prev].slice(0, 50));
      setLastReadUID(uid);
      setTimeout(() => setLastReadUID(''), 5000);
    };

    // Listener: notificação geral
    const handleNotification = (e: CustomEvent) => {
      window.dispatchEvent(new CustomEvent('show-toast', {
        detail: { message: e.detail.message, type: 'info' }
      }));
    };

    window.addEventListener('signalr-access-granted', handleGranted as EventListener);
    window.addEventListener('signalr-access-denied', handleDenied as EventListener);
    window.addEventListener('signalr-notification', handleNotification as EventListener);

    return () => {
      window.removeEventListener('signalr-access-granted', handleGranted as EventListener);
      window.removeEventListener('signalr-access-denied', handleDenied as EventListener);
      window.removeEventListener('signalr-notification', handleNotification as EventListener);
      signalRService.stop();
    };
  }, []);

  const startReading = () => setIsReading(true);
  const stopReading = () => setIsReading(false);

  return {
    isConnected,
    recentEvents,
    isReading,
    lastReadUID,
    startReading,
    stopReading,
    setIsConnected,
    setRecentEvents
  };
}
```

### Tarefa 3.4: Atualizar Dashboard para usar SignalR

Em `Dashboard.tsx`:

1. Importar `useSignalR` de `app/hooks/useSignalR`
2. Adicionar estado de eventos em tempo real via `recentEvents`
3. Quando receber `signalr-access-granted` ou `signalr-access-denied`:
   - Adicionar ao topo da lista "Últimos Acessos"
   - Atualizar contadores de acessos do dia
   - Mostrar toast de sucesso/erro
4. Mostrar status de conexão SignalR no badge "Status do Leitor":
   - Se `isConnected` → Verde "Conectado ao Hub"
   - Se não conectado → Vermelho "Hub desconectado"
5. Manter dados existentes (carregados da API) + eventos em tempo real

**Atualização dos contadores em tempo real:**
```typescript
// Quando receber evento de acesso, atualizar contadores:
if (event.type === 'granted') {
  setMetrics(prev => ({ ...prev, accessesToday: prev.accessesToday + 1 }));
} else {
  setMetrics(prev => ({ ...prev, deniedToday: prev.deniedToday + 1 }));
}
```

### Tarefa 3.5: Atualizar UserForm para SignalR Real

Em `UserForm.tsx`, substituir o `handleReadCard` mockado:

```typescript
import { useSignalR } from '../hooks/useSignalR';

// No componente:
const { isReading, lastReadUID, startReading, stopReading } = useSignalR();

const handleReadCard = () => {
  startReading();  // Entra em modo de espera
  
  // O SignalR já está escutando; quando receber o UID via evento,
  // o lastReadUID será atualizado automaticamente
};

// useEffect para capturar lastReadUID quando mudar:
useEffect(() => {
  if (lastReadUID) {
    setUserData(prev => ({ ...prev, uid: lastReadUID }));
    setSuccess('Cartão lido com sucesso!');
    setTimeout(() => setSuccess(''), 3000);
    // Parar modo de leitura após capturar
    // (mas pode continuar lendo para próxima vez)
  }
}, [lastReadUID]);
```

**Estado visual:**
- Quando `isReading` é true → mostra spinner + "Aguardando aproximação do cartão RFID..."
- Quando `lastReadUID` muda → mostra badge "✅ Cartão associado" em verde
- Quando `lastReadUID` aparece novamente → atualiza o campo UID automaticamente

### Tarefa 3.6: Atualizar CartaoList para SignalR Real

No `CartaoList.tsx`, no modal de registro:

1. Usar `useSignalR` para gerenciar leitura RFID
2. Botão "Ler Cartão" → chama `startReading()`
3. Quando `lastReadUID` muda → preenche o campo UID automaticamente no modal
4. Quando UID preenchido → habilita select de funcionário
5. Ao salvar → `POST /api/cartao` com { uid, funcionarioId }
6. Fechar modal e mostrar toast de sucesso

### Tarefa 3.7: Criar componente de Notificações em Tempo Real

Criar `app/components/RealTimeFeed.tsx` (componente opcional que pode aparecer no Dashboard):

```typescript
// app/components/RealTimeFeed.tsx
import { useSignalR } from '../hooks/useSignalR';
import { Badge } from './ui/badge';
import { UserCheck, UserX } from 'lucide-react';

export default function RealTimeFeed() {
  const { recentEvents, isConnected } = useSignalR();

  if (!isConnected) return null;
  if (recentEvents.length === 0) return null;

  return (
    <div className="mt-6">
      <h3 className="text-lg font-semibold mb-4">Acessos em Tempo Real</h3>
      <div className="space-y-2">
        {recentEvents.slice(0, 5).map((event, index) => (
          <div
            key={`${event.timestamp}-${index}`}
            className={`flex items-center justify-between p-3 rounded-lg ${
              event.type === 'granted' ? 'bg-green-50' : 'bg-red-50'
            }`}
          >
            <div className="flex items-center space-x-3">
              {event.type === 'granted' ? (
                <UserCheck className="h-5 w-5 text-green-600" />
              ) : (
                <UserX className="h-5 w-5 text-red-600" />
              )}
              <div>
                <p className="font-medium">
                  {event.userName || 'Usuário Desconhecido'}
                </p>
                <p className="text-sm text-muted-foreground">
                  UID: {event.uid}
                </p>
              </div>
            </div>
            <div className="text-right">
              <Badge variant={event.type === 'granted' ? 'default' : 'destructive'}>
                {event.type === 'granted' ? 'Permitido' : 'Negado'}
              </Badge>
              {event.type === 'denied' && event.reason && (
                <p className="text-xs text-red-600 mt-1">{event.reason}</p>
              )}
              <p className="text-xs text-muted-foreground mt-1">
                {new Date(event.timestamp).toLocaleTimeString('pt-BR')}
              </p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
```

Integrar ao Dashboard abaixo da tabela "Últimos Acessos".

### Tarefa 3.8: Atualizar App.tsx com SignalR Connection Status

Em `App.tsx`, no topo da aplicação:

```typescript
// Status de conexão SignalR no header
<div className="fixed top-4 right-4 z-50">
  <div className={`flex items-center space-x-2 px-3 py-2 rounded-lg ${
    signalRConnected ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
  }`}>
    <div className={`w-2 h-2 rounded-full ${
      signalRConnected ? 'bg-green-500 animate-pulse' : 'bg-red-500'
    }`} />
    <span className="text-sm font-medium">
      {signalRConnected ? 'Hub Conectado' : 'Hub Desconectado'}
    </span>
  </div>
</div>
```

## Regras Importantes

- SignalR deve reconectar automaticamente (`.withAutomaticReconnect()`)
- Se hub desconectar, mostrar indicador visual claro
- Se hub reconectar, esconder indicador
- Não travar a UI se SignalR falhar
- Manter dados de API (carregados de HTTP) + dados em tempo real (SignalR) separados
- Toasts não devem sobrepor conteúdo importante

## Checklist de Validação

- [ ] SignalR conecta ao hub ao abrir a aplicação
- [ ] Badge de status mostra "Hub Conectado" em verde
- [ ] Badge muda para "Hub Desconectado" em vermelho se cair
- [ ] Ao aproximoar cartão RFID no leitor: aparece no Dashboard automaticamente
- [ ] Toast aparece ao receber acesso autorizado/negado
- [ ] Contadores do Dashboard atualizam em tempo real
- [ ] UserForm: campo UID preenche automaticamente ao ler cartão
- [ ] UserForm: mostra "Aguardando aproximação do cartão..." durante leitura
- [ ] CartaoList: modal registra cartão com UID real do SignalR
- [ ] RealTimeFeed: mostra últimos 5 eventos em tempo real no Dashboard
- [ ] Reconexão automática funciona (desligar/religar servidor)
- [ ] Nenhum travamento da UI se SignalR falhar

## Dependências Adicionais

```bash
npm install @microsoft/signalr
```

## Ordem de Execução Sugerida

1. Instalar `@microsoft/signalr`
2. Criar `signalR.ts` (serviço)
3. Criar `useSignalR.ts` (hook)
4. Criar `RealTimeFeed.tsx` (componente)
5. Atualizar `Dashboard.tsx` (conectar SignalR)
6. Atualizar `UserForm.tsx` (leitura RFID real)
7. Atualizar `CartaoList.tsx` (leitura RFID real no modal)
8. Atualizar `App.tsx` (status indicator)
9. Validar checklist
