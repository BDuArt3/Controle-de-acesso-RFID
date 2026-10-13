import React from 'react';
import { useSignalR } from '../hooks/useSignalR';
import { Badge } from './ui/badge';
import { UserCheck, UserX } from 'lucide-react';

export default function RealTimeFeed() {
  const { recentEvents, isConnected } = useSignalR();

  if (!isConnected || recentEvents.length === 0) return null;

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
              <Badge
                variant={event.type === 'granted' ? 'default' : 'destructive'}
                className={event.type === 'granted' ? 'bg-green-100 text-green-800' : ''}
              >
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
