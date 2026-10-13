import React, { useEffect, useState } from 'react';
import { usuarioService, logAcessoService, isActive, isGranted, errorMessage } from '../services/api';
import { useSignalR } from '../hooks/useSignalR';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Badge } from './ui/badge';
import {
  Users,
  UserCheck,
  UserX,
  Wifi,
  WifiOff
} from 'lucide-react';
import RealTimeFeed from './RealTimeFeed';

interface RecentAccess {
  id: string;
  name: string;
  time: string;
  status: string;
  uid: string;
}

export default function Dashboard() {
  const [metrics, setMetrics] = useState({ totalUsers: 0, accessesToday: 0, deniedToday: 0 });
  const [recentAccesses, setRecentAccesses] = useState<RecentAccess[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  const { isConnected, recentEvents } = useSignalR();

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const [users, logs] = await Promise.all([usuarioService.list(), logAcessoService.list()]);
        if (!active) return;
        const today = new Date().toDateString();
        const daily = logs.data.filter(log => new Date(log.dataHora).toDateString() === today);
        setMetrics({
          totalUsers: users.data.filter(user => isActive(user.status)).length,
          accessesToday: daily.filter(log => isGranted(log.resultado)).length,
          deniedToday: daily.filter(log => !isGranted(log.resultado)).length,
        });
        setRecentAccesses([...logs.data]
          .sort((first, second) => Date.parse(second.dataHora) - Date.parse(first.dataHora))
          .slice(0, 5)
          .map(log => ({
            id: String(log.id),
            name: log.userName || 'Usuário desconhecido',
            uid: log.uid,
            time: new Date(log.dataHora).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }),
            status: isGranted(log.resultado) ? 'granted' : 'denied'
          }))
        );
      } catch (err) { if (active) setError(errorMessage(err)); }
      finally { if (active) setIsLoading(false); }
    };
    void load();
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (recentEvents.length === 0) return;
    const latest = recentEvents[0];

    if (latest.type === 'granted') {
      setMetrics(prev => ({ ...prev, accessesToday: prev.accessesToday + 1 }));
    } else {
      setMetrics(prev => ({ ...prev, deniedToday: prev.deniedToday + 1 }));
    }

    const newAccess: RecentAccess = {
      id: `rt-${latest.timestamp}`,
      name: latest.userName || 'Usuário Desconhecido',
      uid: latest.uid,
      time: new Date(latest.timestamp).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }),
      status: latest.type === 'granted' ? 'granted' : 'denied'
    };
    setRecentAccesses(prev => [newAccess, ...prev].slice(0, 10));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [recentEvents[0]?.timestamp]);

  if (isLoading) return <p role="status" className="text-muted-foreground">Carregando...</p>;
  if (error) return <p role="alert" className="text-destructive">{error}</p>;

  return (
    <div className="space-y-6">
      <div>
        <h1>Dashboard</h1>
        <p className="text-muted-foreground">
          Visão geral do sistema de controle de acesso
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total de Usuários Ativos</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{metrics.totalUsers}</div>
            <p className="text-xs text-muted-foreground">Usuários cadastrados no sistema</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Acessos Hoje</CardTitle>
            <UserCheck className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{metrics.accessesToday}</div>
            <p className="text-xs text-muted-foreground">Tentativas de acesso permitidas</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Acessos Negados Hoje</CardTitle>
            <UserX className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{metrics.deniedToday}</div>
            <p className="text-xs text-muted-foreground">Tentativas de acesso negadas</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Status do Leitor</CardTitle>
            {isConnected ? (
              <Wifi className="h-4 w-4 text-green-600" />
            ) : (
              <WifiOff className="h-4 w-4 text-muted-foreground" />
            )}
          </CardHeader>
          <CardContent>
            <div className="flex items-center space-x-2">
              <Badge
                variant={isConnected ? 'default' : 'outline'}
                className={isConnected ? 'bg-green-100 text-green-800' : ''}
              >
                {isConnected ? 'Conectado ao Hub' : 'Hub desconectado'}
              </Badge>
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              {isConnected ? 'SignalR ativo — eventos em tempo real' : 'Aguardando conexão SignalR'}
            </p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Últimos Acessos</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {recentAccesses.length === 0 && <p className="text-muted-foreground">Nenhum acesso registrado.</p>}
            {recentAccesses.map((access) => (
              <div
                key={access.id}
                className="flex items-center justify-between p-3 bg-muted/20 rounded-lg"
              >
                <div className="flex items-center space-x-3">
                  <div className="w-10 h-10 bg-primary/10 rounded-full flex items-center justify-center">
                    {access.status === 'granted' ? (
                      <UserCheck className="h-5 w-5 text-green-600" />
                    ) : (
                      <UserX className="h-5 w-5 text-red-600" />
                    )}
                  </div>
                  <div>
                    <p className="font-medium">{access.name}</p>
                    <p className="text-sm text-muted-foreground">UID: {access.uid}</p>
                  </div>
                </div>

                <div className="text-right">
                  <p className="text-sm font-medium">{access.time}</p>
                  <Badge
                    variant={access.status === 'granted' ? 'default' : 'destructive'}
                    className={access.status === 'granted' ? 'bg-green-100 text-green-800' : ''}
                  >
                    {access.status === 'granted' ? 'Permitido' : 'Negado'}
                  </Badge>
                </div>
              </div>
            ))}
          </div>

          <RealTimeFeed />
        </CardContent>
      </Card>
    </div>
  );
}
