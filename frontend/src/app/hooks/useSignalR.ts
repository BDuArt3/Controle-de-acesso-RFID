import { useEffect, useState } from 'react';
import { signalRService } from '../services/signalR';
import { showToast } from '../components/Toast';

export interface AccessEvent {
  uid: string;
  userName?: string;
  reason?: string;
  type: 'granted' | 'denied';
  timestamp: string;
}

export function useSignalR() {
  const [isConnected, setIsConnected] = useState(signalRService.isConnected());
  const [recentEvents, setRecentEvents] = useState<AccessEvent[]>([]);
  const [isReading, setIsReading] = useState(false);
  const [lastReadUID, setLastReadUID] = useState('');

  useEffect(() => {
    signalRService.start().then((connected) => {
      setIsConnected(connected);
    });

    const handleGranted = (e: Event) => {
      const { uid, userName, timestamp } = (e as CustomEvent).detail as { uid: string; userName: string; timestamp: string };
      setRecentEvents(prev => [{ uid, userName, type: 'granted', timestamp }, ...prev].slice(0, 50));
      setLastReadUID(uid);
      setTimeout(() => setLastReadUID(''), 5000);
      showToast(`Acesso permitido: ${userName || uid}`, 'success');
    };

    const handleDenied = (e: Event) => {
      const { uid, reason, timestamp } = (e as CustomEvent).detail as { uid: string; reason: string; timestamp: string };
      setRecentEvents(prev => [{ uid, reason, type: 'denied', timestamp }, ...prev].slice(0, 50));
      setLastReadUID(uid);
      setTimeout(() => setLastReadUID(''), 5000);
      showToast(`Acesso negado: ${reason || uid}`, 'error');
    };

    const handleNotification = (e: Event) => {
      const { message } = (e as CustomEvent).detail as { message: string };
      showToast(message, 'info');
    };

    const handleConnectionChange = (e: Event) => {
      const { connected } = (e as CustomEvent).detail as { connected: boolean };
      setIsConnected(connected);
    };

    window.addEventListener('signalr-access-granted', handleGranted);
    window.addEventListener('signalr-access-denied', handleDenied);
    window.addEventListener('signalr-notification', handleNotification);
    window.addEventListener('signalr-connection-change', handleConnectionChange);

    return () => {
      window.removeEventListener('signalr-access-granted', handleGranted);
      window.removeEventListener('signalr-access-denied', handleDenied);
      window.removeEventListener('signalr-notification', handleNotification);
      window.removeEventListener('signalr-connection-change', handleConnectionChange);
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
