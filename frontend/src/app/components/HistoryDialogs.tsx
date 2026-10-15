import { useEffect, useMemo, useState } from 'react';
import { Loader2 } from 'lucide-react';
import {
  cartaoHistoricoService,
  errorMessage,
  funcionarioHistoricoService,
  type HistoricoPaginado,
  type CartaoHistoricoResponse,
  type CartaoStatus,
  type CartaoStatusHistoricoItem,
  type StatusHistoricoItem,
  usuarioHistoricoService,
} from '../services/api';
import { Alert, AlertDescription } from './ui/alert';
import { Badge } from './ui/badge';
import { Button } from './ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from './ui/dialog';
import { Label } from './ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';

const PAGE_SIZE_OPTIONS = [20, 50, 100];
const DEFAULT_PAGE_SIZE = 20;
const UNKNOWN_ACTOR = 'Sistema / cadastro inicial';

function formatDateTime(value: string | null | undefined) {
  if (!value) {
    return '—';
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '—';
  }
  return `${date.toLocaleDateString('pt-BR')} ${date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}`;
}

function DateTimeValue({ value }: { value: string | null | undefined }) {
  if (!value || Number.isNaN(new Date(value).getTime())) {
    return <span className="text-muted-foreground">—</span>;
  }
  const date = new Date(value);

  return (
    <div className="whitespace-nowrap">
      <p className="font-medium">{date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</p>
      <p className="text-xs text-muted-foreground">{date.toLocaleDateString('pt-BR')}</p>
    </div>
  );
}

function actorName(name: string | null | undefined) {
  return name?.trim() || UNKNOWN_ACTOR;
}

function activeBadge(isActive: boolean) {
  return (
    <Badge variant={isActive ? 'default' : 'destructive'} className={isActive ? 'bg-green-100 text-green-800' : ''}>
      {isActive ? 'Ativo' : 'Inativo'}
    </Badge>
  );
}

function cardStatusBadge(status: CartaoStatus) {
  if (status === 0) {
    return <Badge className="bg-green-100 text-green-800">Ativo</Badge>;
  }
  if (status === 1) {
    return <Badge variant="destructive">Bloqueado</Badge>;
  }
  return <Badge className="bg-slate-200 text-slate-800">Cancelado</Badge>;
}

function PaginationControls({
  page,
  pageSize,
  totalItems,
  loading,
  onPageChange,
  onPageSizeChange,
}: {
  page: number;
  pageSize: number;
  totalItems: number;
  loading: boolean;
  onPageChange: (value: number) => void;
  onPageSizeChange: (value: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
      <p className="text-sm text-muted-foreground">
        Página {page} de {totalPages} · {totalItems} registro(s)
      </p>

      <div className="flex items-center gap-2">
        <Label htmlFor="historyPageSize" className="text-xs text-muted-foreground">Itens por página</Label>
        <Select value={String(pageSize)} onValueChange={value => onPageSizeChange(Number(value))} disabled={loading}>
          <SelectTrigger id="historyPageSize" className="w-24">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {PAGE_SIZE_OPTIONS.map(size => (
              <SelectItem key={size} value={String(size)}>{size}</SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Button variant="outline" size="sm" disabled={loading || page <= 1} onClick={() => onPageChange(page - 1)}>
          Anterior
        </Button>
        <Button variant="outline" size="sm" disabled={loading || page >= totalPages} onClick={() => onPageChange(page + 1)}>
          Próxima
        </Button>
      </div>
    </div>
  );
}

function EntityStatusHistoryDialog({
  open,
  onOpenChange,
  title,
  description,
  loader,
}: {
  open: boolean;
  onOpenChange: (value: boolean) => void;
  title: string;
  description: string;
  loader: (page: number, pageSize: number) => Promise<HistoricoPaginado<StatusHistoricoItem>>;
}) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [history, setHistory] = useState<HistoricoPaginado<StatusHistoricoItem> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    if (!open) {
      setPage(1);
      setPageSize(DEFAULT_PAGE_SIZE);
      setHistory(null);
      setError('');
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError('');

    loader(page, pageSize)
      .then((data) => {
        if (!cancelled) {
          setHistory(data);
        }
      })
      .catch((failure) => {
        if (!cancelled) {
          setError(errorMessage(failure));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [loader, open, page, pageSize, retryCount]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        {loading && (
          <p role="status" className="flex items-center justify-center gap-2 py-12 text-muted-foreground">
            <Loader2 className="size-4 animate-spin" />
            Carregando histórico...
          </p>
        )}

        {error && (
          <div role="alert" className="space-y-4 rounded-lg border border-destructive/30 p-6 text-center">
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
            <Button variant="outline" onClick={() => setRetryCount(value => value + 1)}>
              Tentar novamente
            </Button>
          </div>
        )}

        {!loading && !error && (
          <>
            <div className="max-h-80 overflow-auto rounded-lg border">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted/40 text-muted-foreground">
                  <tr>
                    <th className="whitespace-nowrap px-5 py-4 font-medium">Data e hora</th>
                    <th className="px-5 py-4 font-medium">Estado</th>
                    <th className="px-5 py-4 font-medium">Motivo</th>
                    <th className="px-5 py-4 font-medium">Alterado por</th>
                  </tr>
                </thead>
                <tbody>
                  {history?.items.map(item => (
                    <tr key={item.id} className="border-t transition-colors hover:bg-muted/30">
                      <td className="px-5 py-4"><DateTimeValue value={item.dataHora} /></td>
                      <td className="px-5 py-4">{activeBadge(item.isActive)}</td>
                      <td className="px-5 py-4">{item.motivo || '—'}</td>
                      <td className="px-5 py-4">{actorName(item.alteradoPorNome)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {(history?.items.length ?? 0) === 0 && (
                <p className="p-12 text-center text-muted-foreground">Nenhum histórico encontrado.</p>
              )}
            </div>

            <PaginationControls
              page={history?.page ?? page}
              pageSize={history?.pageSize ?? pageSize}
              totalItems={history?.totalItems ?? 0}
              loading={loading}
              onPageChange={setPage}
              onPageSizeChange={(value) => {
                setPage(1);
                setPageSize(value);
              }}
            />
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}

export function UsuarioHistoryDialog({
  open,
  onOpenChange,
  usuarioId,
  usuarioNome,
}: {
  open: boolean;
  onOpenChange: (value: boolean) => void;
  usuarioId: string;
  usuarioNome: string;
}) {
  const loader = useMemo(
    () => (page: number, pageSize: number) => usuarioHistoricoService
      .listByUsuarioId(usuarioId, { page, pageSize })
      .then(({ data }) => data),
    [usuarioId],
  );

  return (
    <EntityStatusHistoryDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Histórico do usuário"
      description={`Alterações de status para ${usuarioNome}.`}
      loader={loader}
    />
  );
}

export function FuncionarioHistoryDialog({
  open,
  onOpenChange,
  funcionarioId,
  funcionarioNome,
}: {
  open: boolean;
  onOpenChange: (value: boolean) => void;
  funcionarioId: string;
  funcionarioNome: string;
}) {
  const loader = useMemo(
    () => (page: number, pageSize: number) => funcionarioHistoricoService
      .listByFuncionarioId(funcionarioId, { page, pageSize })
      .then(({ data }) => data),
    [funcionarioId],
  );

  return (
    <EntityStatusHistoryDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Histórico do funcionário"
      description={`Alterações de status para ${funcionarioNome}.`}
      loader={loader}
    />
  );
}

export function CartaoHistoryDialog({
  open,
  onOpenChange,
  cartaoId,
  cartaoUid,
}: {
  open: boolean;
  onOpenChange: (value: boolean) => void;
  cartaoId: string;
  cartaoUid: string;
}) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [history, setHistory] = useState<CartaoHistoricoResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [retryCount, setRetryCount] = useState(0);

  useEffect(() => {
    if (!open) {
      setPage(1);
      setPageSize(DEFAULT_PAGE_SIZE);
      setHistory(null);
      setError('');
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError('');
    cartaoHistoricoService
      .listByCartaoId(cartaoId, { page, pageSize })
      .then(({ data }) => {
        if (!cancelled) {
          setHistory(data);
        }
      })
      .catch((failure) => {
        if (!cancelled) {
          setError(errorMessage(failure));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [cartaoId, open, page, pageSize, retryCount]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>Histórico do cartão</DialogTitle>
          <DialogDescription>UID {cartaoUid}</DialogDescription>
        </DialogHeader>

        {loading && (
          <p role="status" className="flex items-center justify-center gap-2 py-12 text-muted-foreground">
            <Loader2 className="size-4 animate-spin" />
            Carregando histórico...
          </p>
        )}

        {error && (
          <div role="alert" className="space-y-4 rounded-lg border border-destructive/30 p-6 text-center">
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
            <Button variant="outline" onClick={() => setRetryCount(value => value + 1)}>
              Tentar novamente
            </Button>
          </div>
        )}

        {!loading && !error && (
          <div className="space-y-4">
            <div className="rounded-lg border p-5">
              <h4 className="mb-3 font-medium">Associação</h4>
              {history?.associacao ? (
                <div className="grid gap-2 text-sm sm:grid-cols-2">
                  <p><span className="text-muted-foreground">Associado em:</span> {formatDateTime(history.associacao.dataAssociacao)}</p>
                  <p><span className="text-muted-foreground">Associado por:</span> {actorName(history.associacao.associadoPorNome)}</p>
                  <p><span className="text-muted-foreground">Desassociado em:</span> {formatDateTime(history.associacao.dataDesassociacao)}</p>
                  <p><span className="text-muted-foreground">Desassociado por:</span> {actorName(history.associacao.desassociadoPorNome)}</p>
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Este cartão ainda não possui histórico de associação.</p>
              )}
            </div>

            <div className="max-h-80 overflow-auto rounded-lg border">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted/40 text-muted-foreground">
                  <tr>
                    <th className="whitespace-nowrap px-5 py-4 font-medium">Data e hora</th>
                    <th className="px-5 py-4 font-medium">Estado</th>
                    <th className="px-5 py-4 font-medium">Motivo</th>
                    <th className="px-5 py-4 font-medium">Alterado por</th>
                  </tr>
                </thead>
                <tbody>
                  {history?.status.items.map((item: CartaoStatusHistoricoItem) => (
                    <tr key={item.id} className="border-t transition-colors hover:bg-muted/30">
                      <td className="px-5 py-4"><DateTimeValue value={item.dataHora} /></td>
                      <td className="px-5 py-4">{cardStatusBadge(item.status)}</td>
                      <td className="px-5 py-4">{item.motivo || '—'}</td>
                      <td className="px-5 py-4">{actorName(item.alteradoPorNome)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {(history?.status.items.length ?? 0) === 0 && (
                <p className="p-12 text-center text-muted-foreground">Nenhum histórico encontrado.</p>
              )}
            </div>

            <PaginationControls
              page={history?.status.page ?? page}
              pageSize={history?.status.pageSize ?? pageSize}
              totalItems={history?.status.totalItems ?? 0}
              loading={loading}
              onPageChange={setPage}
              onPageSizeChange={(value) => {
                setPage(1);
                setPageSize(value);
              }}
            />
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
