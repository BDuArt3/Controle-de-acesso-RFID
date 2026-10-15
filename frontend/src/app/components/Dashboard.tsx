import React, { useEffect, useState } from 'react';
import { UserCheck, UserX, Users } from 'lucide-react';
import { errorMessage, isActive, isGranted, logAcessoService, usuarioService } from '../services/api';
import { Badge } from './ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';

interface RecentAccess {
  id: string;
  name: string;
  time: string;
  status: 'granted' | 'denied';
  uid: string;
}

export default function Dashboard() {
  const [metrics, setMetrics] = useState({ totalUsers: 0, accessesToday: 0, deniedToday: 0 });
  const [recentAccesses, setRecentAccesses] = useState<RecentAccess[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const [usersResponse, logsResponse] = await Promise.all([usuarioService.list(), logAcessoService.list()]);
        if (!active) {
          return;
        }

        const today = new Date().toDateString();
        const daily = logsResponse.data.filter(log => new Date(log.dataHora).toDateString() === today);
        setMetrics({
          totalUsers: usersResponse.data.filter(user => isActive(user.isActive)).length,
          accessesToday: daily.filter(log => isGranted(log.acessoPermitido)).length,
          deniedToday: daily.filter(log => !isGranted(log.acessoPermitido)).length,
        });

        setRecentAccesses([...logsResponse.data]
          .sort((first, second) => Date.parse(second.dataHora) - Date.parse(first.dataHora))
          .slice(0, 5)
          .map(log => ({
            id: String(log.id),
            name: log.usuarioNome || log.cartaoUid || log.uidInformado || 'Usuário desconhecido',
            uid: log.cartaoUid || log.uidInformado || '—',
            time: new Date(log.dataHora).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }),
            status: log.acessoPermitido ? 'granted' : 'denied',
          })),
        );
      } catch (failure) {
        if (active) {
          setError(errorMessage(failure));
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    };

    void load();
    return () => {
      active = false;
    };
  }, []);

  if (isLoading) {
    return <p role="status" className="text-muted-foreground">Carregando...</p>;
  }

  if (error) {
    return <p role="alert" className="text-destructive">{error}</p>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1>Dashboard</h1>
        <p className="text-muted-foreground">Visão geral do sistema de controle de acesso.</p>
      </div>

      <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
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
                className="flex items-center justify-between rounded-lg bg-muted/20 p-3"
              >
                <div className="flex items-center space-x-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10">
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
        </CardContent>
      </Card>
    </div>
  );
}
