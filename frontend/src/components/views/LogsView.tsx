import React, { useState, useEffect } from 'react';
import { Search, Filter, Download, Calendar, CheckCircle, XCircle, Clock, MapPin } from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Avatar, AvatarFallback } from '../ui/avatar';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table';
import { Popover, PopoverContent, PopoverTrigger } from '../ui/popover';
import { Calendar as CalendarComponent } from '../ui/calendar';
import { format } from 'date-fns';
import { ptBR } from 'date-fns/locale';

interface AccessLog {
  id: string;
  userId: string;
  userName: string;
  userType: 'aluno' | 'professor' | 'funcionario' | 'visitante';
  cardId: string;
  location: string;
  status: 'permitido' | 'negado';
  reason?: string;
  timestamp: string;
  deviceId: string;
}

interface LogStats {
  total: number;
  permitidos: number;
  negados: number;
  uniqueUsers: number;
}

export function LogsView() {
  const [logs, setLogs] = useState<AccessLog[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<AccessLog[]>([]);
  const [stats, setStats] = useState<LogStats>({ total: 0, permitidos: 0, negados: 0, uniqueUsers: 0 });
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<string>('todos');
  const [filterStatus, setFilterStatus] = useState<string>('todos');
  const [filterLocation, setFilterLocation] = useState<string>('todos');
  const [dateRange, setDateRange] = useState<{from?: Date, to?: Date}>({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 20;

  useEffect(() => {
    loadLogs();
  }, []);

  useEffect(() => {
    filterLogs();
  }, [logs, searchTerm, filterType, filterStatus, filterLocation, dateRange]);

  const loadLogs = async () => {
    setIsLoading(true);
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    const mockLogs: AccessLog[] = [];
    const users = [
      { id: '1', name: 'João Silva', type: 'funcionario' as const, cardId: 'CARD001' },
      { id: '2', name: 'Maria Santos', type: 'professor' as const, cardId: 'CARD002' },
      { id: '3', name: 'Ana Costa', type: 'aluno' as const, cardId: 'CARD003' },
      { id: '4', name: 'Pedro Oliveira', type: 'visitante' as const, cardId: 'CARD004' },
      { id: '5', name: 'Luisa Ferreira', type: 'aluno' as const, cardId: 'CARD005' },
    ];

    const locations = [
      'Entrada Principal',
      'Laboratório 1',
      'Laboratório 2',
      'Biblioteca',
      'Sala de Servidores',
      'Auditório',
      'Sala dos Professores'
    ];

    // Generate logs for the last 30 days
    for (let i = 0; i < 200; i++) {
      const user = users[Math.floor(Math.random() * users.length)];
      const location = locations[Math.floor(Math.random() * locations.length)];
      const status = Math.random() > 0.15 ? 'permitido' : 'negado'; // 85% success rate
      const timestamp = new Date();
      timestamp.setDate(timestamp.getDate() - Math.floor(Math.random() * 30));
      timestamp.setHours(Math.floor(Math.random() * 24));
      timestamp.setMinutes(Math.floor(Math.random() * 60));

      mockLogs.push({
        id: i.toString(),
        userId: user.id,
        userName: user.name,
        userType: user.type,
        cardId: user.cardId,
        location,
        status,
        reason: status === 'negado' ? 
          ['Cartão inválido', 'Acesso não autorizado', 'Horário restrito'][Math.floor(Math.random() * 3)] : 
          undefined,
        timestamp: timestamp.toISOString(),
        deviceId: `DEVICE${Math.floor(Math.random() * 5) + 1}`
      });
    }

    // Sort by timestamp descending
    mockLogs.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
    
    setLogs(mockLogs);
    
    // Calculate stats
    const uniqueUsers = new Set(mockLogs.map(log => log.userId)).size;
    const permitidos = mockLogs.filter(log => log.status === 'permitido').length;
    const negados = mockLogs.filter(log => log.status === 'negado').length;
    
    setStats({
      total: mockLogs.length,
      permitidos,
      negados,
      uniqueUsers
    });
    
    setIsLoading(false);
  };

  const filterLogs = () => {
    let filtered = logs;

    if (searchTerm) {
      filtered = filtered.filter(log =>
        log.userName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        log.cardId.toLowerCase().includes(searchTerm.toLowerCase()) ||
        log.location.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (filterType !== 'todos') {
      filtered = filtered.filter(log => log.userType === filterType);
    }

    if (filterStatus !== 'todos') {
      filtered = filtered.filter(log => log.status === filterStatus);
    }

    if (filterLocation !== 'todos') {
      filtered = filtered.filter(log => log.location === filterLocation);
    }

    if (dateRange.from) {
      filtered = filtered.filter(log => {
        const logDate = new Date(log.timestamp);
        const fromDate = new Date(dateRange.from!);
        fromDate.setHours(0, 0, 0, 0);
        return logDate >= fromDate;
      });
    }

    if (dateRange.to) {
      filtered = filtered.filter(log => {
        const logDate = new Date(log.timestamp);
        const toDate = new Date(dateRange.to!);
        toDate.setHours(23, 59, 59, 999);
        return logDate <= toDate;
      });
    }

    setFilteredLogs(filtered);
    setCurrentPage(1);
  };

  const handleExportLogs = () => {
    // Simulate export functionality
    const csvContent = [
      'Data/Hora,Usuário,Tipo,Cartão,Local,Status,Motivo,Dispositivo',
      ...filteredLogs.map(log => [
        new Date(log.timestamp).toLocaleString('pt-BR'),
        log.userName,
        getUserTypeLabel(log.userType),
        log.cardId,
        log.location,
        log.status === 'permitido' ? 'Permitido' : 'Negado',
        log.reason || '',
        log.deviceId
      ].join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `logs_acesso_${format(new Date(), 'yyyy-MM-dd')}.csv`;
    link.click();
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

  const getUserTypeColor = (type: string) => {
    switch (type) {
      case 'aluno': return 'bg-blue-100 text-blue-800';
      case 'professor': return 'bg-green-100 text-green-800';
      case 'funcionario': return 'bg-purple-100 text-purple-800';
      case 'visitante': return 'bg-orange-100 text-orange-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getUniqueLocations = () => {
    return Array.from(new Set(logs.map(log => log.location))).sort();
  };

  const paginatedLogs = filteredLogs.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  const totalPages = Math.ceil(filteredLogs.length / itemsPerPage);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-muted animate-pulse rounded w-48"></div>
        <div className="grid gap-4 md:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <div className="h-8 bg-muted animate-pulse rounded w-16 mb-2"></div>
                <div className="h-4 bg-muted animate-pulse rounded w-24"></div>
              </CardContent>
            </Card>
          ))}
        </div>
        <Card>
          <CardContent className="p-6">
            <div className="space-y-4">
              {[...Array(10)].map((_, i) => (
                <div key={i} className="h-16 bg-muted animate-pulse rounded"></div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-start">
        <div>
          <h1>Logs de Acesso</h1>
          <p className="text-muted-foreground">
            Histórico completo de tentativas de acesso ao sistema
          </p>
        </div>
        <Button onClick={handleExportLogs} variant="outline">
          <Download className="h-4 w-4 mr-2" />
          Exportar
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total de Acessos</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.total.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">
              Últimos 30 dias
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Acessos Permitidos</CardTitle>
            <CheckCircle className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{stats.permitidos.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">
              {((stats.permitidos / stats.total) * 100).toFixed(1)}% do total
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Acessos Negados</CardTitle>
            <XCircle className="h-4 w-4 text-red-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{stats.negados.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">
              {((stats.negados / stats.total) * 100).toFixed(1)}% do total
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Usuários Únicos</CardTitle>
            <MapPin className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">{stats.uniqueUsers}</div>
            <p className="text-xs text-muted-foreground">
              Usuários diferentes
            </p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col space-y-4">
            {/* Search and Export */}
            <div className="flex flex-col md:flex-row gap-4">
              <div className="flex-1">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Buscar por usuário, cartão ou local..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
              </div>
            </div>

            {/* Filters */}
            <div className="flex flex-wrap gap-2">
              <Select value={filterType} onValueChange={setFilterType}>
                <SelectTrigger className="w-36">
                  <SelectValue placeholder="Tipo" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos os tipos</SelectItem>
                  <SelectItem value="aluno">Aluno</SelectItem>
                  <SelectItem value="professor">Professor</SelectItem>
                  <SelectItem value="funcionario">Funcionário</SelectItem>
                  <SelectItem value="visitante">Visitante</SelectItem>
                </SelectContent>
              </Select>

              <Select value={filterStatus} onValueChange={setFilterStatus}>
                <SelectTrigger className="w-32">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos</SelectItem>
                  <SelectItem value="permitido">Permitido</SelectItem>
                  <SelectItem value="negado">Negado</SelectItem>
                </SelectContent>
              </Select>

              <Select value={filterLocation} onValueChange={setFilterLocation}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="Local" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos os locais</SelectItem>
                  {getUniqueLocations().map(location => (
                    <SelectItem key={location} value={location}>
                      {location}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Popover>
                <PopoverTrigger asChild>
                  <Button variant="outline" className="w-60">
                    <Calendar className="h-4 w-4 mr-2" />
                    {dateRange.from ? (
                      dateRange.to ? (
                        <>
                          {format(dateRange.from, "dd/MM/yyyy", { locale: ptBR })} - {format(dateRange.to, "dd/MM/yyyy", { locale: ptBR })}
                        </>
                      ) : (
                        format(dateRange.from, "dd/MM/yyyy", { locale: ptBR })
                      )
                    ) : (
                      "Selecionar período"
                    )}
                  </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="start">
                  <CalendarComponent
                    initialFocus
                    mode="range"
                    defaultMonth={dateRange.from}
                    selected={{ from: dateRange.from, to: dateRange.to }}
                    onSelect={setDateRange}
                    numberOfMonths={2}
                    locale={ptBR}
                  />
                  <div className="p-3 border-t">
                    <Button 
                      variant="outline" 
                      size="sm" 
                      onClick={() => setDateRange({})}
                      className="w-full"
                    >
                      Limpar filtro de data
                    </Button>
                  </div>
                </PopoverContent>
              </Popover>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Data/Hora</TableHead>
                <TableHead>Usuário</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead>Cartão</TableHead>
                <TableHead>Local</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Dispositivo</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paginatedLogs.map((log) => (
                <TableRow key={log.id}>
                  <TableCell>
                    <div>
                      <p className="text-sm">
                        {new Date(log.timestamp).toLocaleDateString('pt-BR')}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(log.timestamp).toLocaleTimeString('pt-BR', { 
                          hour: '2-digit', 
                          minute: '2-digit',
                          second: '2-digit'
                        })}
                      </p>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <Avatar className="h-8 w-8">
                        <AvatarFallback className="bg-blue-100 text-blue-800 text-xs">
                          {log.userName.split(' ').map(n => n[0]).join('')}
                        </AvatarFallback>
                      </Avatar>
                      <span className="font-medium">{log.userName}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={getUserTypeColor(log.userType)}>
                      {getUserTypeLabel(log.userType)}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <code className="bg-muted px-2 py-1 rounded text-sm">
                      {log.cardId}
                    </code>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <MapPin className="h-3 w-3 text-muted-foreground" />
                      <span className="text-sm">{log.location}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      {log.status === 'permitido' ? (
                        <Badge variant="outline" className="bg-green-100 text-green-800">
                          <CheckCircle className="h-3 w-3 mr-1" />
                          Permitido
                        </Badge>
                      ) : (
                        <div className="space-y-1">
                          <Badge variant="outline" className="bg-red-100 text-red-800">
                            <XCircle className="h-3 w-3 mr-1" />
                            Negado
                          </Badge>
                          {log.reason && (
                            <p className="text-xs text-muted-foreground">{log.reason}</p>
                          )}
                        </div>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <code className="bg-muted px-2 py-1 rounded text-xs">
                      {log.deviceId}
                    </code>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {filteredLogs.length === 0 && (
            <div className="text-center py-8">
              <p className="text-muted-foreground">Nenhum log encontrado</p>
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-6">
              <p className="text-sm text-muted-foreground">
                Mostrando {(currentPage - 1) * itemsPerPage + 1} a {Math.min(currentPage * itemsPerPage, filteredLogs.length)} de {filteredLogs.length} registros
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                  disabled={currentPage === 1}
                >
                  Anterior
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                  disabled={currentPage === totalPages}
                >
                  Próxima
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}