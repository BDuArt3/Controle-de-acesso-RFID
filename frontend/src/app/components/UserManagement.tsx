import React, { useEffect, useMemo, useState } from 'react';
import { Briefcase, Edit, History, Lock, MoreHorizontal, Plus, Search, Unlock } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import {
  conflictMessage,
  errorMessage,
  isActive,
  isConflictError,
  type UsuarioDto,
  usuarioService,
} from '../services/api';
import { Alert, AlertDescription } from './ui/alert';
import { Badge } from './ui/badge';
import { Button } from './ui/button';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from './ui/dialog';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Textarea } from './ui/textarea';
import { UsuarioHistoryDialog } from './HistoryDialogs';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from './ui/dropdown-menu';

interface UserManagementProps {
  onNavigate: (route: any, editingUserId?: string) => void;
}

const userTypeLabels: Record<number, string> = {
  0: 'Aluno',
  1: 'Professor',
  2: 'Visitante',
  3: 'Colaborador',
};

function userTypeLabel(value: UsuarioDto['tipo']) {
  return userTypeLabels[value] ?? String(value);
}

export default function UserManagement({ onNavigate }: UserManagementProps) {
  const { user: currentUser } = useAuth();
  const [searchQuery, setSearchQuery] = useState('');
  const [users, setUsers] = useState<UsuarioDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [reasonDialog, setReasonDialog] = useState<{ open: boolean; user: UsuarioDto | null; mode: 'delete' | 'activate' | null }>({
    open: false,
    user: null,
    mode: null,
  });
  const [reason, setReason] = useState('');
  const [historyTarget, setHistoryTarget] = useState<UsuarioDto | null>(null);

  const canCreate = currentUser?.role === 'Admin' || currentUser?.role === 'Operador';
  const canAdminister = currentUser?.role === 'Admin';

  useEffect(() => {
    let active = true;

    const load = async () => {
      try {
        const { data } = await usuarioService.list();
        if (active) {
          setUsers(data);
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

  const filteredUsers = useMemo(() => {
    const normalized = searchQuery.trim().toLowerCase();
    if (!normalized) {
      return users;
    }
    return users.filter(user =>
      user.nome.toLowerCase().includes(normalized)
      || user.email?.toLowerCase().includes(normalized),
    );
  }, [searchQuery, users]);

  const refresh = async () => {
    const { data } = await usuarioService.list();
    setUsers(data);
  };

  const openReasonDialog = (user: UsuarioDto, mode: 'delete' | 'activate') => {
    setReason('');
    setReasonDialog({ open: true, user, mode });
  };

  const closeReasonDialog = () => {
    if (pendingId) {
      return;
    }
    setReasonDialog({ open: false, user: null, mode: null });
    setReason('');
  };

  const submitReason = async () => {
    if (!reasonDialog.user || !reasonDialog.mode) {
      return;
    }
    if (!reason.trim()) {
      setError('Informe um motivo.');
      return;
    }

    setPendingId(reasonDialog.user.id);
    setError('');
    try {
      if (reasonDialog.mode === 'delete') {
        await usuarioService.deletar(reasonDialog.user.id, reason.trim());
      } else {
        await usuarioService.ativar(reasonDialog.user.id, reason.trim());
      }
      await refresh();
      closeReasonDialog();
    } catch (failure) {
      setError(isConflictError(failure) ? conflictMessage(failure, errorMessage(failure)) : errorMessage(failure));
    } finally {
      setPendingId(null);
    }
  };

  const handleEdit = (user: UsuarioDto) => {
    onNavigate('user-form', user.id);
  };

  return (
    <div className="space-y-6">
      {isLoading && <p role="status" className="text-muted-foreground">Carregando...</p>}
      {error && <Alert variant="destructive"><AlertDescription>{error}</AlertDescription></Alert>}

      <div className="flex items-center justify-between gap-4">
        <div>
          <h1>Gerenciamento de Usuários</h1>
          <p className="text-muted-foreground">Gerencie cadastros RFID e vínculos com cartões.</p>
        </div>
        {canCreate && (
          <Button onClick={() => onNavigate('user-form')}>
            <Plus className="mr-2 h-4 w-4" />
            Adicionar Novo Usuário
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Lista de Usuários</CardTitle>
          <div className="flex items-center space-x-2">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Buscar por nome ou email..."
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                className="pl-10"
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {filteredUsers.map((user) => {
              const active = isActive(user.isActive ?? user.cartoes?.some(card => isActive(card.isActive ?? card.status)));
              return (
                <div key={user.id} className="flex flex-wrap items-center justify-between gap-4 rounded-lg border p-4 transition-colors hover:bg-muted/20">
                  <div className="flex items-center space-x-4">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
                      <span className="font-medium text-primary">
                        {user.nome.split(' ').map(part => part[0]).join('').slice(0, 2)}
                      </span>
                    </div>
                    <div>
                      <h3 className="font-medium">{user.nome}</h3>
                      <div className="mt-1 flex flex-wrap items-center gap-2">
                        <Badge variant="outline">{userTypeLabel(user.tipo)}</Badge>
                        {user.isFuncionario && (
                          <Badge
                            variant="secondary"
                            title="Funcionário – gerencie na tela de Funcionários"
                            className="cursor-pointer"
                            role="link"
                            tabIndex={0}
                            onClick={() => onNavigate('funcionarios')}
                            onKeyDown={(event) => { if (event.key === 'Enter') onNavigate('funcionarios'); }}
                          >
                            Funcionário – gerencie na tela de Funcionários
                          </Badge>
                        )}
                        {user.email && <span className="text-xs text-muted-foreground">{user.email}</span>}
                        <span className="text-xs text-muted-foreground">
                          {user.cartoes?.length ? `${user.cartoes.length} cartão(ões)` : 'Sem cartões conhecidos'}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center space-x-2">
                    <Badge
                      variant={active ? 'default' : 'destructive'}
                      className={active ? 'bg-green-100 text-green-800' : ''}
                    >
                      {active ? 'Ativo' : 'Inativo'}
                    </Badge>

                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="sm" disabled={pendingId !== null}>
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        {!user.isFuncionario && (
                          <>
                            <DropdownMenuItem onClick={() => handleEdit(user)} disabled={!canAdminister}>
                              <Edit className="mr-2 h-4 w-4" />
                              Editar
                            </DropdownMenuItem>
                            {active ? (
                              <DropdownMenuItem onClick={() => openReasonDialog(user, 'delete')} disabled={!canAdminister || pendingId !== null}>
                                <Lock className="mr-2 h-4 w-4" />
                                Desativar
                              </DropdownMenuItem>
                            ) : (
                              <DropdownMenuItem onClick={() => openReasonDialog(user, 'activate')} disabled={!canAdminister || pendingId !== null}>
                                <Unlock className="mr-2 h-4 w-4" />
                                Reativar
                              </DropdownMenuItem>
                            )}
                          </>
                        )}
                        {user.isFuncionario && (
                          <DropdownMenuItem onClick={() => onNavigate('funcionarios')}>
                            <Briefcase className="mr-2 h-4 w-4" />
                            Gerenciar em Funcionários
                          </DropdownMenuItem>
                        )}
                        {canAdminister && (
                          <DropdownMenuItem onClick={() => setHistoryTarget(user)} disabled={pendingId !== null}>
                            <History className="mr-2 h-4 w-4" />
                            Histórico
                          </DropdownMenuItem>
                        )}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                </div>
              );
            })}

            {filteredUsers.length === 0 && (
              <div className="py-8 text-center">
                <p className="text-muted-foreground">Nenhum usuário encontrado.</p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <Dialog open={reasonDialog.open} onOpenChange={value => !value && closeReasonDialog()}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {reasonDialog.mode === 'delete' ? 'Desativar usuário' : 'Reativar usuário'}
            </DialogTitle>
            <DialogDescription>
              Informe um motivo para prosseguir com a alteração do status do usuário.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-2">
            <Label htmlFor="reason">Motivo *</Label>
            <Textarea
              id="reason"
              value={reason}
              onChange={event => setReason(event.target.value)}
              rows={4}
              placeholder="Explique o motivo da alteração..."
              disabled={pendingId !== null}
            />
          </div>

          <div className="flex justify-end gap-2">
            <Button variant="outline" disabled={pendingId !== null} onClick={closeReasonDialog}>
              Cancelar
            </Button>
            <Button onClick={() => void submitReason()} disabled={pendingId !== null}>
              {pendingId !== null ? 'Processando...' : 'Confirmar'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {historyTarget && (
        <UsuarioHistoryDialog
          open={historyTarget !== null}
          onOpenChange={(value) => {
            if (!value) {
              setHistoryTarget(null);
            }
          }}
          usuarioId={historyTarget.id}
          usuarioNome={historyTarget.nome}
        />
      )}
    </div>
  );
}
