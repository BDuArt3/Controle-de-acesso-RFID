import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

let hubConnection: HubConnection | null = null;
let refCount = 0;

export const signalRService = {
  async start() {
    if (hubConnection?.state === HubConnectionState.Connected) {
      refCount++;
      return true;
    }
    if (hubConnection && hubConnection.state !== HubConnectionState.Disconnected) {
      await hubConnection.stop();
    }

    hubConnection = new HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/notificacoes', {
        accessTokenFactory: () => localStorage.getItem('authToken') || ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    hubConnection.on('ReceiveNotification', (message: string) => {
      window.dispatchEvent(new CustomEvent('signalr-notification', {
        detail: { message, type: 'info' }
      }));
    });

    hubConnection.on('ReceiveAccessGranted', (uid: string, userName: string) => {
      window.dispatchEvent(new CustomEvent('signalr-access-granted', {
        detail: { uid, userName, timestamp: new Date().toISOString() }
      }));
      window.dispatchEvent(new CustomEvent('rfid:read', { detail: { uid } }));
    });

    hubConnection.on('ReceiveAccessDenied', (uid: string, reason: string) => {
      window.dispatchEvent(new CustomEvent('signalr-access-denied', {
        detail: { uid, reason, timestamp: new Date().toISOString() }
      }));
      window.dispatchEvent(new CustomEvent('rfid:read', { detail: { uid } }));
    });

    hubConnection.onreconnected(() => {
      window.dispatchEvent(new CustomEvent('signalr-connection-change', { detail: { connected: true } }));
    });

    hubConnection.onreconnecting(() => {
      window.dispatchEvent(new CustomEvent('signalr-connection-change', { detail: { connected: false } }));
    });

    hubConnection.onclose(() => {
      window.dispatchEvent(new CustomEvent('signalr-connection-change', { detail: { connected: false } }));
    });

    try {
      await hubConnection.start();
      refCount++;
      window.dispatchEvent(new CustomEvent('signalr-connection-change', { detail: { connected: true } }));
      console.log('SignalR connected');
      return true;
    } catch (err) {
      console.error('SignalR connection failed:', err);
      return false;
    }
  },

  async stop() {
    refCount = Math.max(0, refCount - 1);
    if (refCount === 0 && hubConnection) {
      await hubConnection.stop();
      hubConnection = null;
    }
  },

  getConnection() {
    return hubConnection;
  },

  isConnected() {
    return hubConnection?.state === HubConnectionState.Connected;
  }
};
