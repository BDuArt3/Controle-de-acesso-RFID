import React, { useEffect, useMemo, useState } from 'react';
import { Download, Filter, Search, UserCheck, UserX } from 'lucide-react';
import { errorMessage, type ExportFormat, isGranted, type LogAcessoDto, logAcessoService } from '../services/api';
import { Alert, AlertDescription } from './ui/alert';
import { Badge } from './ui/badge';
import { Button } from './ui/button';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from './ui/dropdown-menu';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';

type ResultFilter = 'all' | 'granted' | 'denied';
type DateFilter = 'today' | 'week' | 'month' | 'custom';

function getDisplayName(log: LogAcessoDto) {
  return log.usuarioNome || log.cartaoUid || log.uidInformado || 'Usuário desconhecido';
}

function getAccessResult(log: LogAcessoDto): ResultFilter {
  return log.acessoPermitido ? 'granted' : 'denied';
}

function getDateRange(dateFilter: DateFilter, startDate: string, endDate: string) {
  if (dateFilter === 'custom') {
    return {
      start: startDate ? new Date(`${startDate}T00:00:00`) : null,
      end: endDate ? new Date(`${endDate}T23:59:59.999`) : null,
    };
  }
  const start = new Date();
  start.setHours(0, 0, 0, 0);
  if (dateFilter === 'week') {
    start.setDate(start.getDate() - 6);
  } else if (dateFilter === 'month') {
    start.setMonth(start.getMonth() - 1);
  }
  const end = new Date();
  end.setHours(23, 59, 59, 999);
  return { start, end };
}

function triggerDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  try {
    link.click();
  } finally {
    link.remove();
    URL.revokeObjectURL(url);
  }
}

function formatDateTime(value: string) {
  const date = new Date(value);
  return {
    date: date.toLocaleDateString('pt-BR'),
    time: date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }),
  };
}

export default function AccessLogs() {
  const [searchQuery, setSearchQuery] = useState('');
  const [resultFilter, setResultFilter] = useState<ResultFilter>('all');
  const [dateFilter, setDateFilter] = useState<DateFilter>('today');
  const [logs, setLogs] = useState<LogAcessoDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [exportError, setExportError] = useState('');
  const [isExporting, setIsExporting] = useState(false);
  useEffect(() => {
    let active = true;

    const load = async () => {
      try {
        const { data } = await logAcessoService.list();
        if (active) {
          setLogs([...data].sort((first, second) => Date.parse(second.dataHora) - Date.parse(first.dataHora)));
        }
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

  const filteredLogs = useMemo(() => {
    const normalizedSearch = searchQuery.trim().toLowerCase();

    return logs.filter((log) => {
      const result = getAccessResult(log);
      const displayName = getDisplayName(log).toLowerCase();
      const uid = (log.cartaoUid || log.uidInformado || '').toLowerCase();
      const local = (log.local || '').toLowerCase();
      const matchesSearch = !normalizedSearch ||
        displayName.includes(normalizedSearch) ||
        uid.includes(normalizedSearch) ||
        local.includes(normalizedSearch);
      const matchesResult = resultFilter === 'all' || result === resultFilter;

      const date = new Date(log.dataHora);
      const { start, end } = getDateRange(dateFilter, startDate, endDate);
      const matchesDate = (!start || date >= start) && (!end || date <= end);

      return matchesSearch && matchesResult && matchesDate;
    });
  }, [dateFilter, endDate, logs, resultFilter, searchQuery, startDate]);

  const handleExport = async (formato: ExportFormat) => {
    const { start, end } = getDateRange(dateFilter, startDate, endDate);
    if (!start || !end) {
      setExportError('Informe as datas inicial e final do período para exportar.');
      return;
    }
    if (start > end) {
      setExportError('A data inicial não pode ser posterior à data final.');
      return;
    }

    setExportError('');
    setIsExporting(true);
    try {
      const { blob, fileName } = await logAcessoService.exportar({
        dataInicio: start.toISOString(),
        dataFim: end.toISOString(),
        ...(resultFilter === 'all' ? {} : { acessoPermitido: resultFilter === 'granted' }),
        formato,
      });
      triggerDownload(blob, fileName);
    } catch (failure) {
      setExportError(`Não foi possível exportar os logs. ${errorMessage(failure)}`);
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div className="space-y-6">
      {isLoading && <p role="status" className="text-muted-foreground">Carregando...</p>}
      {error && <Alert variant="destructive"><AlertDescription>{error}</AlertDescription></Alert>}

      <div className="flex items-center justify-between gap-4">
        <div>
          <h1>Logs de Acesso</h1>
          <p className="text-muted-foreground">Auditoria de tentativas de acesso ao sistema.</p>
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" disabled={isLoading || !!error || isExporting}>
              <Download className="mr-2 h-4 w-4" />
              {isExporting ? 'Exportando...' : 'Exportar'}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onSelect={() => void handleExport('csv')}>CSV</DropdownMenuItem>
            <DropdownMenuItem onSelect={() => void handleExport('pdf')}>PDF</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      {exportError && <Alert variant="destructive"><AlertDescription>{exportError}</AlertDescription></Alert>}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Filter className="mr-2 h-5 w-5" />
            Filtros
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="search">Buscar</Label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="search"
                  placeholder="Nome, UID, cartão ou local..."
                  value={searchQuery}
                  onChange={(event) => setSearchQuery(event.target.value)}
                  className="pl-10"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label>Período</Label>
              <Select value={dateFilter} onValueChange={(value) => setDateFilter(value as DateFilter)}>
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
                  <Input id="startDate" type="date" value={startDate} onChange={event => setStartDate(event.target.value)} />
                  <Label htmlFor="endDate">Até</Label>
                  <Input id="endDate" type="date" min={startDate} value={endDate} onChange={event => setEndDate(event.target.value)} />
                </div>
              )}
            </div>

            <div className="space-y-2">
              <Label>Resultado</Label>
              <Select value={resultFilter} onValueChange={(value) => setResultFilter(value as ResultFilter)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos</SelectItem>
                  <SelectItem value="granted">Permitidos</SelectItem>
                  <SelectItem value="denied">Negados</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Registros de Acesso</CardTitle>
          <p className="text-sm text-muted-foreground">{filteredLogs.length} registro(s) encontrado(s)</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {filteredLogs.map((log) => {
              const { date, time } = formatDateTime(log.dataHora);
              const granted = isGranted(log.acessoPermitido);
              return (
                <div key={log.id} className="flex items-center justify-between rounded-lg border p-4 transition-colors hover:bg-muted/20">
                  <div className="flex items-center space-x-4">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10">
                      {granted ? (
                        <UserCheck className="h-5 w-5 text-green-600" />
                      ) : (
                        <UserX className="h-5 w-5 text-red-600" />
                      )}
                    </div>
                    <div>
                      <h3 className="font-medium">{getDisplayName(log)}</h3>
                      <p className="text-sm text-muted-foreground">
                        UID: {log.cartaoUid || log.uidInformado || '—'}
                      </p>
                      <p className="text-sm text-muted-foreground">
                        Local: {log.local || 'Entrada Principal'}
                      </p>
                      {log.motivo && (
                        <p className={`mt-1 text-sm ${granted ? 'text-green-600' : 'text-red-600'}`}>
                          {log.motivo}
                        </p>
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
                        variant={granted ? 'default' : 'destructive'}
                        className={granted ? 'bg-green-100 text-green-800' : ''}
                      >
                        {granted ? 'Permitido' : 'Negado'}
                      </Badge>
                    </div>
                  </div>
                </div>
              );
            })}

            {filteredLogs.length === 0 && (
              <div className="py-8 text-center">
                <p className="text-muted-foreground">Nenhum registro encontrado com os filtros aplicados.</p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
