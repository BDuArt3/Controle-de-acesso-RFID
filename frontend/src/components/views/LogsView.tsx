import React, { useState, useEffect } from 'react';
import { Search, Filter, Download, Calendar, CheckCircle, XCircle, Clock } from 'lucide-react';
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
import { usePermissions } from '../../hooks/usePermissions';
import { AccessDeniedView } from '../ui/AccessDeniedView';
import { services } from '../../services';
import { LogAcessoDto, TipoMotivoAcesso, LogAcessoUtils } from '../../services/logAcessoService';
import { toast } from 'sonner';

// Interface adaptada para o sistema real
interface AccessLog {
  id: number;
  userId?: string;
  userName: string;
  userType: string;
  cardId: string; // cartaoUid
  location: string; // Universidade (ponto único de acesso)
  status: 'permitido' | 'negado'; // acessoPermitido
  reason: string; // motivo
  reasonType: TipoMotivoAcesso; // motivoTipo
  timestamp: string; // dataHora
  deviceId: string; // Dispositivo RFID
}

interface LogStats {
  total: number;
  permitidos: number;
  negados: number;
  uniqueUsers: number;
}

export function LogsView() {
  const { canView } = usePermissions();
  const [logs, setLogs] = useState<AccessLog[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<AccessLog[]>([]);
  const [stats, setStats] = useState<LogStats>({ total: 0, permitidos: 0, negados: 0, uniqueUsers: 0 });
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<string>('todos');
  const [filterStatus, setFilterStatus] = useState<string>('todos');
  const [filterReason, setFilterReason] = useState<string>('todos');
  const [dateRange, setDateRange] = useState<{from?: Date, to?: Date}>({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 20;

  // Verificação de permissão para acessar esta view
  if (!canView('logs')) {
    return <AccessDeniedView />;
  }

  useEffect(() => {
    loadLogs();
  }, []);

  useEffect(() => {
    filterLogs();
  }, [logs, searchTerm, filterType, filterStatus, filterReason, dateRange]);

  // Função para converter LogAcessoDto para AccessLog
  const convertToAccessLog = (logDto: LogAcessoDto): AccessLog => {
    return {
      id: logDto.id,
      userId: logDto.usuarioNome ? `user-${logDto.id}` : undefined,
      userName: logDto.usuarioNome || 'Usuário Desconhecido',
      userType: logDto.usuarioTipo || 'visitante',
      cardId: logDto.cartaoUid,
      location: 'Entrada da Universidade', // Ponto único de acesso
      status: logDto.acessoPermitido ? 'permitido' : 'negado',
      reason: logDto.motivo,
      reasonType: logDto.motivoTipo,
      timestamp: logDto.dataHora,
      deviceId: 'RFID-001' // Dispositivo único de acesso
    };
  };

  const loadLogs = async () => {
    setIsLoading(true);
    try {
      // Buscar logs reais do backend
      const logsData = await services.logAcesso.getAll();
      
      // Converter para o formato esperado pela UI
      const accessLogs = logsData.map(convertToAccessLog);
      
      // Ordenar por timestamp descrescente (mais recente primeiro)
      accessLogs.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
      
      setLogs(accessLogs);
      
      // Calcular estatísticas
      const uniqueUsers = new Set(accessLogs.filter(log => log.userId).map(log => log.userId)).size;
      const permitidos = accessLogs.filter(log => log.status === 'permitido').length;
      const negados = accessLogs.filter(log => log.status === 'negado').length;
      
      setStats({
        total: accessLogs.length,
        permitidos,
        negados,
        uniqueUsers
      });
      
    } catch (error: any) {
      console.error('Erro ao carregar logs:', error);
      toast.error('Erro ao carregar logs de acesso: ' + (error.message || 'Erro desconhecido'));
      
      // Em caso de erro, manter lista vazia
      setLogs([]);
      setStats({ total: 0, permitidos: 0, negados: 0, uniqueUsers: 0 });
    } finally {
      setIsLoading(false);
    }
  };

  const filterLogs = () => {
    let filtered = logs;

    if (searchTerm) {
      filtered = filtered.filter(log =>
        log.userName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        log.cardId.toLowerCase().includes(searchTerm.toLowerCase()) ||
        log.reason.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (filterType !== 'todos') {
      filtered = filtered.filter(log => log.userType === filterType);
    }

    if (filterStatus !== 'todos') {
      filtered = filtered.filter(log => log.status === filterStatus);
    }

    if (filterReason !== 'todos') {
      const motivoTipo = parseInt(filterReason) as TipoMotivoAcesso;
      filtered = filtered.filter(log => log.reasonType === motivoTipo);
    }

    if (dateRange.from) {
      filtered = filtered.filter(log => new Date(log.timestamp) >= dateRange.from!);
    }

    if (dateRange.to) {
      const endDate = new Date(dateRange.to);
      endDate.setHours(23, 59, 59, 999);
      filtered = filtered.filter(log => new Date(log.timestamp) <= endDate);
    }

    setFilteredLogs(filtered);
    setCurrentPage(1);
  };

  const exportToCSV = () => {
    const headers = ['Data/Hora', 'Usuário', 'Cartão', 'Status', 'Motivo', 'Tipo de Usuário'];
    const csvData = [
      headers.join(','),
      ...filteredLogs.map(log => [
        LogAcessoUtils.formatDateTime(log.timestamp),
        log.userName,
        log.cardId,
        log.status === 'permitido' ? 'Permitido' : 'Negado',
        log.reason,
        getUserTypeLabel(log.userType)
      ].map(field => `"${field}"`).join(','))
    ].join('\n');

    const blob = new Blob([csvData], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    if (link.download !== undefined) {
      const url = URL.createObjectURL(blob);
      link.setAttribute('href', url);
      link.setAttribute('download', `logs-acesso-${format(new Date(), 'yyyy-MM-dd', { locale: ptBR })}.csv`);
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  };

  const getUserTypeLabel = (type: string) => {
    switch (type) {
      case 'aluno': return 'Aluno';
      case 'professor': return 'Professor';
      case 'funcionario': return 'Funcionário';
      case 'visitante': return 'Visitante';
      default: return 'Visitante';
    }
  };

  const getUserTypeColor = (type: string) => {
    switch (type) {
      case 'aluno': return 'bg-blue-100 text-blue-700';
      case 'professor': return 'bg-green-100 text-green-700';
      case 'funcionario': return 'bg-purple-100 text-purple-700';
      case 'visitante': return 'bg-orange-100 text-orange-700';
      default: return 'bg-gray-100 text-gray-700';
    }
  };

  const paginatedLogs = filteredLogs.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  const totalPages = Math.ceil(filteredLogs.length / itemsPerPage);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-lg">Carregando logs de acesso...</div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Logs de Acesso</h1>
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
              {filteredLogs.length !== stats.total && `${filteredLogs.length} filtrados`}
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
              {stats.total > 0 ? ((stats.permitidos / stats.total) * 100).toFixed(1) : 0}% do total
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
              {stats.total > 0 ? ((stats.negados / stats.total) * 100).toFixed(1) : 0}% do total
            </p>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Usuários Únicos</CardTitle>
            <Search className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{stats.uniqueUsers.toLocaleString()}</div>
            <p className="text-xs text-muted-foreground">
              Diferentes usuários
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filtros</CardTitle>
          <CardDescription>
            Filtre os logs por diferentes critérios
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-6">
            <div className="space-y-2">
              <label htmlFor="search" className="text-sm font-medium">Buscar</label>
              <Input
                id="search"
                placeholder="Nome, cartão, motivo..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full"
              />
            </div>
            
            <div className="space-y-2">
              <label htmlFor="filter-type" className="text-sm font-medium">Tipo de Usuário</label>
              <Select value={filterType} onValueChange={setFilterType}>
                <SelectTrigger>
                  <SelectValue placeholder="Todos os tipos" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos os tipos</SelectItem>
                  <SelectItem value="aluno">Aluno</SelectItem>
                  <SelectItem value="professor">Professor</SelectItem>
                  <SelectItem value="funcionario">Funcionário</SelectItem>
                  <SelectItem value="visitante">Visitante</SelectItem>
                </SelectContent>
              </Select>
            </div>
            
            <div className="space-y-2">
              <label htmlFor="filter-status" className="text-sm font-medium">Status</label>
              <Select value={filterStatus} onValueChange={setFilterStatus}>
                <SelectTrigger>
                  <SelectValue placeholder="Todos os status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos os status</SelectItem>
                  <SelectItem value="permitido">Permitido</SelectItem>
                  <SelectItem value="negado">Negado</SelectItem>
                </SelectContent>
              </Select>
            </div>
            
            <div className="space-y-2">
              <label htmlFor="filter-reason" className="text-sm font-medium">Motivo</label>
              <Select value={filterReason} onValueChange={setFilterReason}>
                <SelectTrigger>
                  <SelectValue placeholder="Todos os motivos" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos os motivos</SelectItem>
                  {LogAcessoUtils.getTipoMotivoAcessoOptions().map((option) => (
                    <SelectItem key={option.value} value={option.value.toString()}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            
            <div className="space-y-2">
              <label className="text-sm font-medium">Data Inicial</label>
              <Popover>
                <PopoverTrigger asChild>
                  <Button variant="outline" className="w-full justify-start text-left font-normal">
                    <Calendar className="mr-2 h-4 w-4" />
                    {dateRange.from ? format(dateRange.from, 'dd/MM/yyyy', { locale: ptBR }) : 'Selecionar'}
                  </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="start">
                  <CalendarComponent
                    mode="single"
                    selected={dateRange.from}
                    onSelect={(date: Date | undefined) => setDateRange(prev => ({ ...prev, from: date }))}
                    initialFocus
                  />
                </PopoverContent>
              </Popover>
            </div>
            
            <div className="space-y-2">
              <label className="text-sm font-medium">Data Final</label>
              <Popover>
                <PopoverTrigger asChild>
                  <Button variant="outline" className="w-full justify-start text-left font-normal">
                    <Calendar className="mr-2 h-4 w-4" />
                    {dateRange.to ? format(dateRange.to, 'dd/MM/yyyy', { locale: ptBR }) : 'Selecionar'}
                  </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="start">
                  <CalendarComponent
                    mode="single"
                    selected={dateRange.to}
                    onSelect={(date: Date | undefined) => setDateRange(prev => ({ ...prev, to: date }))}
                    initialFocus
                  />
                </PopoverContent>
              </Popover>
            </div>
          </div>
          
          <div className="flex items-center justify-between mt-4">
            <div className="text-sm text-muted-foreground">
              {filteredLogs.length} de {logs.length} logs
            </div>
            <div className="flex gap-2">
              <Button 
                variant="outline" 
                onClick={() => {
                  setSearchTerm('');
                  setFilterType('todos');
                  setFilterStatus('todos');
                  setFilterReason('todos');
                  setDateRange({});
                }}
              >
                Limpar Filtros
              </Button>
              <Button onClick={exportToCSV} disabled={filteredLogs.length === 0}>
                <Download className="mr-2 h-4 w-4" />
                Exportar CSV
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Logs Table */}
      <Card>
        <CardHeader>
          <CardTitle>Registros de Acesso</CardTitle>
          <CardDescription>
            Histórico detalhado de todas as tentativas de acesso
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Usuário</TableHead>
                <TableHead>Cartão</TableHead>
                <TableHead>Data/Hora</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Motivo</TableHead>
                <TableHead>Local</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paginatedLogs.map((log) => (
                <TableRow key={log.id}>
                  <TableCell>
                    <div className="flex items-center space-x-3">
                      <Avatar className="h-8 w-8">
                        <AvatarFallback className="text-xs">
                          {log.userName.split(' ').map(n => n[0]).join('')}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <div className="font-medium">{log.userName}</div>
                        <Badge variant="outline" className={getUserTypeColor(log.userType)}>
                          {getUserTypeLabel(log.userType)}
                        </Badge>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <code className="text-sm bg-muted px-2 py-1 rounded">
                      {log.cardId}
                    </code>
                  </TableCell>
                  <TableCell>
                    {LogAcessoUtils.formatDateTime(log.timestamp)}
                  </TableCell>
                  <TableCell>
                    <Badge variant={log.status === 'permitido' ? 'default' : 'destructive'}>
                      {log.status === 'permitido' ? 'Permitido' : 'Negado'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Badge 
                      variant="outline" 
                      className={LogAcessoUtils.getTipoMotivoAcessoColor(log.reasonType)}
                    >
                      {LogAcessoUtils.getTipoMotivoAcessoString(log.reasonType)}
                    </Badge>
                    {log.reason && (
                      <div className="text-xs text-muted-foreground mt-1">
                        {log.reason}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="text-sm">{log.location}</div>
                    <div className="text-xs text-muted-foreground">{log.deviceId}</div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          
          {filteredLogs.length === 0 && (
            <div className="text-center py-8">
              <p className="text-muted-foreground">Nenhum log encontrado com os filtros aplicados.</p>
            </div>
          )}
          
          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <div className="text-sm text-muted-foreground">
                Página {currentPage} de {totalPages}
              </div>
              <div className="flex space-x-2">
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