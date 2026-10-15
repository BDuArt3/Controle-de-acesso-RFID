import { useEffect, useMemo, useRef, useState } from 'react';
import { AlertCircle, CheckCircle2, CreditCard, Loader2, Plus, RefreshCw, Search, ShieldAlert } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import {
  cartaoService,
  createIdempotencyKey,
  errorMessage,
  isActive,
  type CartaoDto,
  type CartaoSolicitacaoStatus,
  type SolicitacaoCartaoDto,
  type UsuarioDto,
  usuarioService,
} from '../services/api';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Badge } from './ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from './ui/dialog';
import { Label } from './ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Textarea } from './ui/textarea';
import { showToast } from './Toast';

type IssueMode = 'new' | 'reissue';
type CardActionMode = 'block' | 'activate' | 'cancel';

const terminalStatuses: CartaoSolicitacaoStatus[] = ['Concluida', 'Falhou', 'Cancelada', 'Expirada'];
const POLL_INTERVAL_MS = 1500;
const POLL_TIMEOUT_MS = 30000;

function cardOwner(card: CartaoDto, users: UsuarioDto[]) {
  return card.usuarioNome
    || card.nomeUsuario
    || users.find(user => String(user.id) === String(card.usuarioId))?.nome
    || 'Sem vínculo identificado';
}

function cardStatusLabel(status: CartaoDto['status']) {
  if (status === 0) {
    return 'Ativo';
  }
  if (status === 1) {
    return 'Bloqueado';
  }
  return 'Cancelado';
}

export default function CartaoList() {
  const { user: currentUser } = useAuth();
  const [items, setItems] = useState<CartaoDto[]>([]);
  const [users, setUsers] = useState<UsuarioDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [requestOpen, setRequestOpen] = useState(false);
  const [issueMode, setIssueMode] = useState<IssueMode>('new');
  const [selectedUserId, setSelectedUserId] = useState('');
  const [previousCardId, setPreviousCardId] = useState('');
  const [reemissionReason, setReemissionReason] = useState('');
  const [requestCancelReason, setRequestCancelReason] = useState('');
  const [request, setRequest] = useState<SolicitacaoCartaoDto | null>(null);
  const [actionTarget, setActionTarget] = useState<CartaoDto | null>(null);
  const [actionMode, setActionMode] = useState<CardActionMode | null>(null);
  const [actionReason, setActionReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [pollingError, setPollingError] = useState('');
  const [timedOut, setTimedOut] = useState(false);
  const [pollNonce, setPollNonce] = useState(0);
  const requestId = request?.solicitacaoId ?? null;
  const idempotencyKeyRef = useRef<string>('');

  const canManage = currentUser?.role === 'Admin' || currentUser?.role === 'Operador';

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const [cardsResponse, usersResponse] = await Promise.all([
        cartaoService.list(),
        usuarioService.list(),
      ]);
      setItems(cardsResponse.data);
      setUsers(usersResponse.data);
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const activeUsers = useMemo(() => users.filter(user => isActive(user.isActive)), [users]);

  const filtered = useMemo(() => {
    const normalized = search.trim().toLowerCase();
    if (!normalized) {
      return items;
    }
    return items.filter(item => {
      const owner = cardOwner(item, users).toLowerCase();
      const uid = item.uid.toLowerCase();
      return owner.includes(normalized) || uid.includes(normalized);
    });
  }, [items, search, users]);

  const resetRequestForm = () => {
    setIssueMode('new');
    setSelectedUserId('');
    setPreviousCardId('');
    setReemissionReason('');
    setRequestCancelReason('');
    setRequest(null);
    setPollingError('');
    idempotencyKeyRef.current = '';
  };

  const openRequestDialog = () => {
    resetRequestForm();
    setRequestOpen(true);
  };

  const closeRequestDialog = () => {
    if (busy) {
      return;
    }
    setRequestOpen(false);
  };

  const openActionDialog = (card: CartaoDto, mode: CardActionMode) => {
    setActionTarget(card);
    setActionMode(mode);
    setActionReason('');
  };

  const closeActionDialog = () => {
    if (busy) {
      return;
    }
    setActionTarget(null);
    setActionMode(null);
    setActionReason('');
  };

  const submitRequest = async () => {
    if (!selectedUserId) {
      setError('Selecione um usuário.');
      return;
    }

    if (issueMode === 'reissue' && (!previousCardId || !reemissionReason.trim())) {
      setError('Informe o cartão anterior e o motivo da reemissão.');
      return;
    }

    setBusy(true);
    setError('');
    try {
      if (!idempotencyKeyRef.current) {
        idempotencyKeyRef.current = createIdempotencyKey();
      }

      const { data } = await cartaoService.create({
        usuarioId: selectedUserId,
        ...(issueMode === 'reissue'
          ? {
              cartaoAnteriorId: previousCardId,
              motivoReemissao: reemissionReason.trim(),
            }
          : {}),
      }, idempotencyKeyRef.current);

      setRequest(data);
      showToast('Solicitação de cartão registrada.', 'success');
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  };

  const submitAction = async () => {
    if (!actionTarget || !actionMode) {
      return;
    }
    if (!actionReason.trim()) {
      setError('Informe um motivo.');
      return;
    }

    setBusy(true);
    setError('');
    try {
      if (actionMode === 'block') {
        await cartaoService.bloquear(actionTarget.id, actionReason.trim());
      } else if (actionMode === 'activate') {
        await cartaoService.ativar(actionTarget.id, actionReason.trim());
      } else {
        await cartaoService.cancelar(actionTarget.id, actionReason.trim());
      }
      showToast('Status do cartão atualizado.', 'success');
      await load();
      closeActionDialog();
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  };

  const retryRequest = async () => {
    if (!request) {
      return;
    }
    setBusy(true);
    setPollingError('');
    try {
      const { data } = await cartaoService.retentarSolicitacao(request.solicitacaoId);
      setRequest(data);
      setTimedOut(false);
      setPollNonce(value => value + 1);
      showToast('Retentativa enviada.', 'success');
    } catch (failure) {
      setPollingError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  };

  const cancelRequest = async (motivo: string) => {
    if (!request) {
      return;
    }
    if (!motivo.trim()) {
      setPollingError('Informe um motivo para cancelar.');
      return;
    }
    setBusy(true);
    setPollingError('');
    try {
      const { data } = await cartaoService.cancelarSolicitacao(request.solicitacaoId, motivo.trim());
      showToast(data.message || 'Solicitação cancelada.', 'success');
      setRequest(null);
      await load();
    } catch (failure) {
      setPollingError(errorMessage(failure));
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => {
    if (!requestId) {
      return;
    }

    let cancelled = false;
    let inFlight = false;
    const startedAt = Date.now();
    setTimedOut(false);

    const stop = () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };

    const poll = async () => {
      if (cancelled || inFlight) {
        return;
      }
      if (Date.now() - startedAt >= POLL_TIMEOUT_MS) {
        stop();
        setTimedOut(true);
        return;
      }

      inFlight = true;
      try {
        const { data } = await cartaoService.getSolicitacao(requestId);
        if (cancelled) {
          return;
        }
        setRequest(data);
        if (terminalStatuses.includes(data.status)) {
          stop();
          if (data.status === 'Concluida') {
            showToast('Cartão gravado com sucesso.', 'success');
            await load();
          } else {
            setPollingError(data.mensagem || 'A solicitação não foi concluída.');
          }
        }
      } catch (failure) {
        if (!cancelled) {
          stop();
          setPollingError(errorMessage(failure));
        }
      } finally {
        inFlight = false;
      }
    };

    const intervalId = window.setInterval(() => void poll(), POLL_INTERVAL_MS);

    return stop;
  }, [requestId, pollNonce]);

  const isWaiting = !!request && !terminalStatuses.includes(request.status) && !timedOut;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="mb-2 text-xs font-medium uppercase tracking-[0.15em] text-muted-foreground">Gestão de acesso / Credenciais</p>
          <h2 className="text-3xl font-semibold">Cartões RFID</h2>
          <p className="mt-2 text-sm text-muted-foreground">Solicite emissões, acompanhe o despacho e gerencie bloqueios ou cancelamentos.</p>
        </div>
        {canManage && (
          <Button onClick={openRequestDialog}>
            <Plus className="mr-2 size-4" />
            Nova Solicitação
          </Button>
        )}
      </div>

      <Card>
        <CardHeader className="space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <CreditCard className="size-5" />
              <CardTitle>Cartões cadastrados</CardTitle>
              <Badge variant="secondary">{items.length}</Badge>
            </div>
            <div className="relative w-full sm:w-72">
              <Search className="absolute left-3 top-3 size-4 text-muted-foreground" />
              <Input
                aria-label="Buscar cartão"
                className="pl-9"
                placeholder="Buscar por UID ou usuário..."
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </div>
          </div>

          {request && (
            <div className="rounded-lg border bg-muted/20 p-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-medium">Solicitação atual</p>
                  <p className="text-sm text-muted-foreground">
                    {request.nomeUsuario} · {request.status}{request.mensagem ? ` · ${request.mensagem}` : ''}
                  </p>
                  <p className="text-xs text-muted-foreground">Solicitação: {request.solicitacaoId}</p>
                  {isWaiting && (
                    <p role="status" className="mt-2 flex items-center gap-2 text-sm">
                      <Loader2 className="size-4 animate-spin" />
                      Aguardando o cartão ser aproximado do leitor...
                    </p>
                  )}
                  {timedOut && <p role="alert" className="mt-2 text-sm text-amber-700">Tempo esgotado</p>}
                  {request.status === 'Concluida' && (
                    <p role="status" className="mt-2 flex items-center gap-2 text-sm text-green-700">
                      <CheckCircle2 className="size-4" />
                      Cartão gravado com sucesso.
                    </p>
                  )}
                </div>
                <Badge
                  variant={request.status === 'Concluida' ? 'default' : ['Falhou', 'Expirada'].includes(request.status) ? 'destructive' : 'secondary'}
                  className={request.status === 'Concluida' ? 'bg-green-100 text-green-800' : ''}
                >
                  {request.status}
                </Badge>
              </div>
              {pollingError && <p role="alert" className="mt-2 text-sm text-red-600">{pollingError}</p>}
              <div className="mt-3 flex flex-wrap gap-2">
                {(request.status === 'Falhou' || request.status === 'Expirada' || timedOut) && (
                  <Button variant="outline" disabled={busy} onClick={() => void retryRequest()}>
                    <RefreshCw className="mr-2 size-4" />
                    Tentar novamente
                  </Button>
                )}
                {(request.status === 'Pendente' || request.status === 'Enviada') && (
                  <>
                    <div className="space-y-2">
                      <Label htmlFor="cancelReason">Motivo do cancelamento</Label>
                      <Textarea
                        id="cancelReason"
                        value={requestCancelReason}
                        onChange={(event) => setRequestCancelReason(event.target.value)}
                        rows={3}
                        placeholder="Explique por que a solicitação será cancelada..."
                        disabled={busy}
                      />
                    </div>
                    <Button variant="outline" disabled={busy} onClick={() => void cancelRequest(requestCancelReason)}>
                      Cancelar solicitação
                    </Button>
                  </>
                )}
              </div>
            </div>
          )}
        </CardHeader>
        <CardContent className="p-0">
          {loading ? (
            <p role="status" className="flex justify-center gap-2 p-12 text-muted-foreground">
              <Loader2 className="size-5 animate-spin" />
              Carregando cartões...
            </p>
          ) : error ? (
            <div role="alert" className="p-6 text-red-700">
              {error}
              <Button className="ml-3" variant="outline" onClick={() => void load()}>Tentar novamente</Button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted/40 text-muted-foreground">
                  <tr>
                    {['UID', 'Usuário', 'Status', 'Data', 'Ações'].map(label => (
                      <th className="whitespace-nowrap px-5 py-4 font-medium" key={label}>{label}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((item) => {
                    const active = isActive(item.status);
                    return (
                      <tr key={item.id} className="border-t hover:bg-muted/30">
                        <td className="px-5 py-5 font-mono">{item.uid}</td>
                        <td className="whitespace-nowrap px-5 font-medium">{cardOwner(item, users)}</td>
                        <td className="px-5">
                          <Badge variant={active ? 'default' : 'destructive'} className={active ? 'bg-green-100 text-green-800' : ''}>
                            {cardStatusLabel(item.status)}
                          </Badge>
                        </td>
                        <td className="whitespace-nowrap px-5 text-muted-foreground">
                          {item.dataCadastro || item.dataSolicitacao || '—'}
                        </td>
                        <td className="px-5">
                          <div className="flex flex-wrap gap-2">
                            <Button
                              size="sm"
                              variant="outline"
                              disabled={!canManage}
                              onClick={() => openActionDialog(item, active ? 'block' : 'activate')}
                            >
                              {active ? 'Bloquear' : 'Reativar'}
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              disabled={!canManage}
                              onClick={() => openActionDialog(item, 'cancel')}
                            >
                              Cancelar
                            </Button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              {!filtered.length && <p className="p-12 text-center text-muted-foreground">Nenhum cartão encontrado.</p>}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={requestOpen} onOpenChange={value => { if (!busy && !value) closeRequestDialog(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Nova solicitação de cartão</DialogTitle>
            <DialogDescription>Escolha o usuário e gere uma solicitação para o Worker confirmar a emissão.</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="issueMode">Tipo de solicitação</Label>
              <Select value={issueMode} onValueChange={value => setIssueMode(value as IssueMode)} disabled={busy}>
                <SelectTrigger id="issueMode">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="new">Nova emissão</SelectItem>
                  <SelectItem value="reissue">Reemissão</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="userId">Usuário</Label>
              <Select value={selectedUserId} onValueChange={setSelectedUserId} disabled={busy}>
                <SelectTrigger id="userId">
                  <SelectValue placeholder="Selecione um usuário ativo" />
                </SelectTrigger>
                <SelectContent>
                  {activeUsers.map(user => (
                    <SelectItem key={user.id} value={user.id}>
                      {user.nome} · {user.documento}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {issueMode === 'reissue' && (
              <>
                <div className="space-y-2">
                  <Label htmlFor="previousCardId">Cartão anterior *</Label>
                  <Select value={previousCardId} onValueChange={setPreviousCardId} disabled={busy}>
                    <SelectTrigger id="previousCardId">
                      <SelectValue placeholder="Selecione o cartão que será bloqueado" />
                    </SelectTrigger>
                    <SelectContent>
                      {items.map(card => (
                        <SelectItem key={card.id} value={card.id}>
                          {card.uid} · {cardOwner(card, users)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="reemissionReason">Motivo da reemissão *</Label>
                  <Textarea
                    id="reemissionReason"
                    value={reemissionReason}
                    onChange={event => setReemissionReason(event.target.value)}
                    rows={4}
                    placeholder="Explique o motivo da reemissão..."
                    disabled={busy}
                  />
                </div>
              </>
            )}
          </div>

          <div className="flex justify-end gap-2">
            <Button variant="outline" disabled={busy} onClick={closeRequestDialog}>Cancelar</Button>
            <Button disabled={busy} onClick={() => void submitRequest()}>
              {busy ? 'Enviando...' : 'Solicitar'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={actionTarget !== null} onOpenChange={value => { if (!busy && !value) closeActionDialog(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {actionMode === 'activate' ? 'Reativar cartão' : actionMode === 'cancel' ? 'Cancelar cartão' : 'Bloquear cartão'}
            </DialogTitle>
            <DialogDescription>
              {actionTarget && `${actionTarget.uid} · ${cardOwner(actionTarget, users)}`}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-2">
            <Label htmlFor="actionReason">Motivo *</Label>
            <Textarea
              id="actionReason"
              value={actionReason}
              onChange={event => setActionReason(event.target.value)}
              rows={4}
              placeholder="Descreva o motivo da alteração..."
              disabled={busy}
            />
          </div>

          <div className="flex justify-end gap-2">
            <Button variant="outline" disabled={busy} onClick={closeActionDialog}>Cancelar</Button>
            <Button disabled={busy} onClick={() => void submitAction()}>
              {busy ? 'Processando...' : 'Confirmar'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
