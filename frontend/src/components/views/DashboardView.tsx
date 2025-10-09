import React, { useState, useEffect } from 'react';
import { Users, CreditCard, Calendar, Shield, TrendingUp, Clock, CheckCircle, XCircle } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Progress } from '../ui/progress';
import { Avatar, AvatarFallback } from '../ui/avatar';

interface DashboardStats {
  totalUsers: number;
  accessesToday: number;
  activeCards: number;
  pendingAlerts: number;
}

interface RecentAccess {
  id: string;
  userName: string;
  userType: 'aluno' | 'professor' | 'funcionario' | 'visitante';
  time: string;
  status: 'permitido' | 'negado';
  location: string;
}

export function DashboardView() {
  const [stats, setStats] = useState<DashboardStats>({
    totalUsers: 0,
    accessesToday: 0,
    activeCards: 0,
    pendingAlerts: 0,
  });

  const [recentAccesses, setRecentAccesses] = useState<RecentAccess[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Simulate API call
    const loadDashboardData = async () => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      setStats({
        totalUsers: 1247,
        accessesToday: 156,
        activeCards: 1180,
        pendingAlerts: 3,
      });

      setRecentAccesses([
        {
          id: '1',
          userName: 'João Silva',
          userType: 'funcionario',
          time: '14:35',
          status: 'permitido',
          location: 'Entrada Principal'
        },
        {
          id: '2',
          userName: 'Maria Santos',
          userType: 'professor',
          time: '14:32',
          status: 'permitido',
          location: 'Laboratório 1'
        },
        {
          id: '3',
          userName: 'Visitante Anônimo',
          userType: 'visitante',
          time: '14:28',
          status: 'negado',
          location: 'Sala de Servidores'
        },
        {
          id: '4',
          userName: 'Ana Costa',
          userType: 'aluno',
          time: '14:25',
          status: 'permitido',
          location: 'Biblioteca'
        },
        {
          id: '5',
          userName: 'Pedro Oliveira',
          userType: 'funcionario',
          time: '14:20',
          status: 'permitido',
          location: 'Entrada Principal'
        }
      ]);

      setIsLoading(false);
    };

    loadDashboardData();

    // Simulate real-time updates
    const interval = setInterval(() => {
      setStats(prev => ({
        ...prev,
        accessesToday: prev.accessesToday + Math.floor(Math.random() * 3),
      }));
    }, 30000);

    return () => clearInterval(interval);
  }, []);

  const getUserTypeColor = (type: string) => {
    switch (type) {
      case 'aluno': return 'bg-blue-100 text-blue-800';
      case 'professor': return 'bg-green-100 text-green-800';
      case 'funcionario': return 'bg-purple-100 text-purple-800';
      case 'visitante': return 'bg-orange-100 text-orange-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getUserTypeLabel = (type: string) => {
    switch (type) {
      case 'aluno': return 'Aluno';
      case 'professor': return 'Professor';
      case 'funcionario': return 'Funcionário';
      case 'visitante': return 'Visitante';
      default: return type;
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <h1>Dashboard</h1>
        </div>
        
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <div className="h-4 bg-muted animate-pulse rounded w-20"></div>
                <div className="h-4 w-4 bg-muted animate-pulse rounded"></div>
              </CardHeader>
              <CardContent>
                <div className="h-8 bg-muted animate-pulse rounded w-16 mb-2"></div>
                <div className="h-3 bg-muted animate-pulse rounded w-24"></div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
        <div>
          <h1>Dashboard</h1>
        </div>      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total de Usuários</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">{stats.totalUsers.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">
              <span className="text-green-600">+12</span> desde ontem
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Acessos Hoje</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{stats.accessesToday}</div>
            <p className="text-xs text-muted-foreground">
              <span className="text-green-600">+8%</span> vs. média semanal
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Cartões Ativos</CardTitle>
            <CreditCard className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-purple-600">{stats.activeCards.toLocaleString()}</div>
            <div className="mt-2">
              <Progress value={(stats.activeCards / stats.totalUsers) * 100} className="h-2" />
              <p className="text-xs text-muted-foreground mt-1">
                {((stats.activeCards / stats.totalUsers) * 100).toFixed(1)}% dos usuários
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Alertas Pendentes</CardTitle>
            <Shield className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{stats.pendingAlerts}</div>
            <p className="text-xs text-muted-foreground">
              <span className="text-red-600">2</span> críticos
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Recent Accesses */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5" />
              Acessos Recentes
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {recentAccesses.map((access) => (
                <div key={access.id} className="flex items-center justify-between p-3 rounded-lg border bg-card">
                  <div className="flex items-center gap-3">
                    <Avatar className="h-8 w-8">
                      <AvatarFallback className="text-xs bg-blue-100 text-blue-800">
                        {access.userName.split(' ').map(n => n[0]).join('')}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <p className="font-medium text-sm">{access.userName}</p>
                      <div className="flex items-center gap-2 text-xs text-muted-foreground">
                        <Badge variant="outline" className={getUserTypeColor(access.userType)}>
                          {getUserTypeLabel(access.userType)}
                        </Badge>
                        <span>{access.location}</span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-sm text-muted-foreground">{access.time}</span>
                    {access.status === 'permitido' ? (
                      <CheckCircle className="h-4 w-4 text-green-600" />
                    ) : (
                      <XCircle className="h-4 w-4 text-red-600" />
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="h-5 w-5" />
              Estatísticas Rápidas
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span>Taxa de Sucesso</span>
                <span className="font-medium text-green-600">94.8%</span>
              </div>
              <Progress value={94.8} className="h-2" />
            </div>
            
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span>Horário de Pico</span>
                <span className="font-medium">08:00 - 09:00</span>
              </div>
              <div className="text-xs text-muted-foreground">
                156 acessos na última hora
              </div>
            </div>
            
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span>Status do Sistema</span>
                <Badge variant="outline" className="bg-green-100 text-green-800">
                  Online
                </Badge>
              </div>
              <div className="text-xs text-muted-foreground">
                Tempo de atividade: 99.9%
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}