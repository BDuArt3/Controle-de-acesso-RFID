import React, { useState, useEffect } from 'react';
import { logAcessoService, isGranted, errorMessage } from '../services/api';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Button } from './ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Badge } from './ui/badge';
import { Calendar, Search, Filter, Download, UserCheck, UserX } from 'lucide-react';

interface AccessLog {
  id: string;
  uid: string;
  userName: string;
  datetime: string;
  result: 'granted' | 'denied';
  reason?: string;
}

export default function AccessLogs() {
  const [searchQuery, setSearchQuery] = useState('');
  const [resultFilter, setResultFilter] = useState('all');
  const [dateFilter, setDateFilter] = useState('today');
  const [logs, setLogs] = useState<AccessLog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const { data } = await logAcessoService.list();
        if (active) setLogs([...data].sort(
          (first, second) => Date.parse(second.dataHora) - Date.parse(first.dataHora)
        ).map(log => ({
          id: String(log.id),
          uid: log.uid || '',
          userName: log.userName || 'Usuário desconhecido',
          datetime: log.dataHora,
          result: isGranted(log.resultado) ? 'granted' : 'denied',
          reason: log.motivo
        })));
      } catch (error) { if (active) setError(errorMessage(error)); }
      finally { if (active) setIsLoading(false); }
    };
    void load();
    return () => { active = false; };
  }, []);

  const filteredLogs = logs.filter(log => {
    const matchesSearch =
      log.userName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      log.uid.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesResult = resultFilter === 'all' || log.result === resultFilter;

    const date = new Date(log.datetime);
    const lower = new Date();
    lower.setHours(0, 0, 0, 0);
    if (dateFilter === 'week') lower.setDate(lower.getDate() - 6);
    if (dateFilter === 'month') lower.setMonth(lower.getMonth() - 1);
    const upper = new Date();
    upper.setHours(23, 59, 59, 999);
    const matchesDate = dateFilter === 'custom'
      ? (!startDate || date >= new Date(startDate + 'T00:00:00')) && (!endDate || date <= new Date(endDate + 'T23:59:59.999'))
      : date >= lower && date <= upper;
    return matchesSearch && matchesResult && matchesDate;
  });

  const handleExport = () => {
    const cell = (value: string) => '"' + value.replace(/^[=+@-]/, match => "'" + match).replace(/"/g, '""') + '"';
    const rows = [['ID', 'UID', 'Usuário', 'Data e hora', 'Resultado', 'Motivo'],
      ...filteredLogs.map(log => [log.id, log.uid, log.userName, log.datetime,
        log.result === 'granted' ? 'Permitido' : 'Negado', log.reason || ''])];
    const blob = new Blob(['\uFEFF' + rows.map(row => row.map(cell).join(';')).join('\r\n')],
      { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'logs-de-acesso.csv';
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  };

  const formatDateTime = (datetime: string) => {
    const date = new Date(datetime);
    return {
      date: date.toLocaleDateString('pt-BR'),
      time: date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
    };
  };

  return (
    <div className="space-y-6">
      {isLoading && <p role="status" className="text-muted-foreground">Carregando...</p>}
      {error && <p role="alert" className="text-destructive">{error}</p>}

      <div className="flex justify-between items-center">
        <div>
          <h1>Logs de Acesso</h1>
          <p className="text-muted-foreground">
            Auditoria de todas as tentativas de acesso ao sistema
          </p>
        </div>
        <Button variant="outline" onClick={handleExport} disabled={isLoading || !!error}>
          <Download className="h-4 w-4 mr-2" />Exportar
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Filter className="h-5 w-5 mr-2" />Filtros
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label htmlFor="search">Buscar</Label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
                <Input
                  id="search"
                  placeholder="Nome ou UID do cartão..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label>Período</Label>
              <Select value={dateFilter} onValueChange={setDateFilter}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="today">Hoje</SelectItem>
                  <SelectItem value="week">Última semana</SelectItem>
                  <SelectItem value="month">Último mês</SelectItem>
                  <SelectItem value="custom">Período personalizado</SelectItem>
                </SelectContent>
              </Select>
              {dateFilter === 'custom' && (
                <div className="space-y-2">
                  <Label htmlFor="startDate">De</Label>
                  <Input id="startDate" type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
                  <Label htmlFor="endDate">Até</Label>
                  <Input id="endDate" type="date" min={startDate} value={endDate} onChange={e => setEndDate(e.target.value)} />
                </div>
              )}
            </div>

            <div className="space-y-2">
              <Label>Resultado</Label>
              <Select value={resultFilter} onValueChange={setResultFilter}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  <SelectItem value="granted">Permitido</SelectItem>
                  <SelectItem value="denied">Negado</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Registros de Acesso</CardTitle>
          <p className="text-sm text-muted-foreground">
            {filteredLogs.length} registro(s) encontrado(s)
          </p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {filteredLogs.map((log) => {
              const { date, time } = formatDateTime(log.datetime);
              return (
                <div key={log.id} className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/20 transition-colors">
                  <div className="flex items-center space-x-4">
                    <div className="w-10 h-10 rounded-full flex items-center justify-center bg-primary/10">
                      {log.result === 'granted' ? (
                        <UserCheck className="h-5 w-5 text-green-600" />
                      ) : (
                        <UserX className="h-5 w-5 text-red-600" />
                      )}
                    </div>
                    <div>
                      <h3 className="font-medium">{log.userName}</h3>
                      <p className="text-sm text-muted-foreground">UID: {log.uid}</p>
                      {log.reason && (
                        <p className="text-sm text-red-600 mt-1">{log.reason}</p>
                      )}
                    </div>
                  </div>

                  <div className="text-right">
                    <div className="flex items-center space-x-3">
                      <div className="text-sm">
                        <p className="font-medium">{time}</p>
                        <p className="text-muted-foreground">{date}</p>
                      </div>
                      <Badge
                        variant={log.result === 'granted' ? 'default' : 'destructive'}
                        className={log.result === 'granted' ? 'bg-green-100 text-green-800' : ''}
                      >
                        {log.result === 'granted' ? 'Permitido' : 'Negado'}
                      </Badge>
                    </div>
                  </div>
                </div>
              );
            })}

            {filteredLogs.length === 0 && (
              <div className="text-center py-8">
                <p className="text-muted-foreground">
                  Nenhum registro encontrado com os filtros aplicados.
                </p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
