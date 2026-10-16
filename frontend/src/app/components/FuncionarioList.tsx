import React, { useEffect, useMemo, useState } from 'react';
import { Briefcase, History, Loader2, Pencil, Plus, Search, Trash2, Unlock, Lock } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import {
  errorMessage,
  isActive,
  type CreateFuncionarioDto,
  type FuncionarioDto,
  funcionarioService,
} from '../services/api';
import { Alert, AlertDescription } from './ui/alert';
import { Badge } from './ui/badge';
import { Button } from './ui/button';
import { Card, CardContent } from './ui/card';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from './ui/dialog';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Textarea } from './ui/textarea';
import { showToast } from './Toast';
import { FuncionarioHistoryDialog } from './HistoryDialogs';

type FormState = {
  nomeFuncionario: string;
  email: string;
  cpf: string;
  senha: string;
  tipo: string;
};

const emptyForm: FormState = {
  nomeFuncionario: '',
  email: '',
  cpf: '',
  senha: '',
  tipo: '1',
};

const tipoLabels: Record<number, string> = {
  0: 'Visualizador',
  1: 'Operador',
  2: 'Admin',
};

export default function FuncionarioList() {
  const { user: currentUser } = useAuth();
  const [items, setItems] = useState<FuncionarioDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<FuncionarioDto | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [reasonDialog, setReasonDialog] = useState<{ open: boolean; funcionario: FuncionarioDto | null; mode: 'delete' | 'activate' | null }>({
    open: false,
    funcionario: null,
    mode: null,
  });
  const [reason, setReason] = useState('');
  const [historyTarget, setHistoryTarget] = useState<FuncionarioDto | null>(null);

  const canAccess = currentUser?.role === 'Admin';

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const { data } = await funcionarioService.list();
      setItems(data);
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!canAccess) {
      setLoading(false);
      return;
    }
    void load();
  }, [canAccess]);

  const filtered = useMemo(() => {
    const normalized = search.trim().toLowerCase();
    if (!normalized) {
      return items;
    }
    return items.filter(item =>
      item.nomeFuncionario.toLowerCase().includes(normalized)
      || item.cpf.toLowerCase().includes(normalized)
      || item.email.toLowerCase().includes(normalized),
    );
  }, [items, search]);

  const openForm = (funcionario?: FuncionarioDto) => {
    if (funcionario) {
      setEditing(funcionario);
      setForm({
        nomeFuncionario: funcionario.nomeFuncionario,
        email: funcionario.email,
        cpf: funcionario.cpf,
        senha: '',
        tipo: String(funcionario.tipo),
      });
    } else {
      setEditing(null);
      setForm(emptyForm);
    }
    setFormOpen(true);
  };

  const closeForm = () => {
    if (saving) {
      return;
    }
    setFormOpen(false);
    setEditing(null);
    setForm(emptyForm);
  };

  const updateForm = (field: keyof FormState, value: string) => {
    setForm(previous => ({ ...previous, [field]: value }));
    setError('');
  };

  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');

    if (!form.nomeFuncionario.trim() || !form.email.trim() || !form.cpf.trim() || !form.tipo) {
      setError('Informe nome, email, CPF e tipo.');
      return;
    }

    if (!editing && !form.senha.trim()) {
      setError('A senha é obrigatória no cadastro.');
      return;
    }

    const payload: CreateFuncionarioDto = {
      nomeFuncionario: form.nomeFuncionario.trim(),
      email: form.email.trim(),
      cpf: form.cpf.replace(/\D/g, ''),
      senha: form.senha.trim(),
      tipo: Number(form.tipo) as CreateFuncionarioDto['tipo'],
    };

    setSaving(true);
    try {
      if (editing) {
        await funcionarioService.update(editing.id, payload);
        showToast('Funcionário atualizado com sucesso.', 'success');
      } else {
        await funcionarioService.create(payload);
        showToast('Funcionário criado com sucesso.', 'success');
      }
      closeForm();
      await load();
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setSaving(false);
    }
  };

  const openReasonDialog = (funcionario: FuncionarioDto, mode: 'delete' | 'activate') => {
    setReason('');
    setReasonDialog({ open: true, funcionario, mode });
  };

  const closeReasonDialog = () => {
    if (busyId) {
      return;
    }
    setReasonDialog({ open: false, funcionario: null, mode: null });
    setReason('');
  };

  const submitReason = async () => {
    if (!reasonDialog.funcionario || !reasonDialog.mode) {
      return;
    }
    if (!reason.trim()) {
      setError('Informe um motivo.');
      return;
    }

    setBusyId(reasonDialog.funcionario.id);
    setError('');
    try {
      if (reasonDialog.mode === 'delete') {
        await funcionarioService.delete(reasonDialog.funcionario.id, reason.trim());
      } else {
        await funcionarioService.ativar(reasonDialog.funcionario.id, reason.trim());
      }
      showToast('Status do funcionário atualizado.', 'success');
      await load();
      closeReasonDialog();
    } catch (failure) {
      setError(errorMessage(failure));
    } finally {
      setBusyId(null);
    }
  };

  if (!canAccess) {
    return (
      <div className="space-y-4">
        <h1>Funcionários</h1>
        <Alert>
          <AlertDescription>Esta área é restrita para administradores.</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="mb-2 text-xs font-medium uppercase tracking-[0.15em] text-muted-foreground">Gestão de acesso / Pessoas</p>
          <h2 className="text-3xl font-semibold">Funcionários</h2>
          <p className="mt-2 text-sm text-muted-foreground">Cadastre, atualize e mantenha contas administrativas e operacionais.</p>
        </div>
        <Button onClick={() => openForm()}>
          <Plus className="mr-2 size-4" />
          Adicionar Funcionário
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          <div className="flex flex-wrap items-center justify-between gap-4 border-b p-5">
            <div className="flex items-center gap-2">
              <Briefcase className="size-5" />
              <h3 className="font-medium">Funcionários cadastrados</h3>
              <Badge variant="secondary">{items.length}</Badge>
            </div>
            <div className="relative w-full sm:w-72">
              <Search className="absolute left-3 top-3 size-4 text-muted-foreground" />
              <Input
                aria-label="Buscar funcionário"
                placeholder="Buscar por nome, email ou CPF..."
                className="pl-9"
                value={search}
                onChange={event => setSearch(event.target.value)}
              />
            </div>
          </div>

          {loading ? (
            <p role="status" className="flex items-center justify-center gap-2 p-12 text-muted-foreground">
              <Loader2 className="size-5 animate-spin" />
              Carregando funcionários...
            </p>
          ) : error ? (
            <div role="alert" className="p-6 text-red-700">
              {error}
              <Button variant="outline" className="ml-3" onClick={() => void load()}>Tentar novamente</Button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted/40 text-muted-foreground">
                  <tr>
                    {['Nome', 'Email', 'CPF', 'Tipo', 'Status', 'Ações'].map(label => (
                      <th key={label} className="px-5 py-4 font-medium">{label}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((item) => {
                    const active = isActive(item.isActive);
                    return (
                      <tr key={item.id} className="border-t transition-colors hover:bg-muted/30">
                        <td className="whitespace-nowrap px-5 py-5 font-medium">{item.nomeFuncionario}</td>
                        <td className="whitespace-nowrap px-5">{item.email}</td>
                        <td className="whitespace-nowrap px-5 font-mono text-xs">{item.cpf}</td>
                        <td className="px-5">{tipoLabels[item.tipo] ?? item.tipo}</td>
                        <td className="px-5">
                          <Badge
                            variant={active ? 'default' : 'destructive'}
                            className={active ? 'bg-green-100 text-green-800' : ''}
                          >
                            {active ? 'Ativo' : 'Inativo'}
                          </Badge>
                        </td>
                        <td className="px-5">
                          <div className="flex gap-1">
                            <Button variant="ghost" size="icon" aria-label={`Editar ${item.nomeFuncionario}`} disabled={saving || busyId !== null} onClick={() => openForm(item)}>
                              <Pencil className="size-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              aria-label={active ? `Desativar ${item.nomeFuncionario}` : `Reativar ${item.nomeFuncionario}`}
                              disabled={saving || busyId !== null}
                              onClick={() => openReasonDialog(item, active ? 'delete' : 'activate')}
                            >
                              {active ? <Lock className="size-4 text-red-600" /> : <Unlock className="size-4 text-green-600" />}
                            </Button>
                            <Button variant="ghost" size="icon" aria-label={`Excluir ${item.nomeFuncionario}`} disabled={saving || busyId !== null} onClick={() => openReasonDialog(item, 'delete')}>
                              <Trash2 className="size-4 text-red-600" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="icon"
                              aria-label={`Histórico de ${item.nomeFuncionario}`}
                              disabled={saving || busyId !== null}
                              onClick={() => setHistoryTarget(item)}
                            >
                              <History className="size-4" />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
              {!filtered.length && <p className="p-12 text-center text-muted-foreground">Nenhum funcionário encontrado.</p>}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={formOpen} onOpenChange={value => { if (!value) closeForm(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? 'Editar funcionário' : 'Adicionar funcionário'}</DialogTitle>
            <DialogDescription>Preencha os dados de identificação e vínculo profissional.</DialogDescription>
          </DialogHeader>
          <form onSubmit={save} className="space-y-4">
            <label className="block space-y-2">
              <span className="text-sm">Nome completo *</span>
              <Input value={form.nomeFuncionario} onChange={event => updateForm('nomeFuncionario', event.target.value)} />
            </label>
            <label className="block space-y-2">
              <span className="text-sm">Email *</span>
              <Input type="email" value={form.email} onChange={event => updateForm('email', event.target.value)} />
            </label>
            <label className="block space-y-2">
              <span className="text-sm">CPF *</span>
              <Input value={form.cpf} onChange={event => updateForm('cpf', event.target.value)} />
            </label>
            <label className="block space-y-2">
              <span className="text-sm">Tipo *</span>
              <Select value={form.tipo} onValueChange={value => updateForm('tipo', value)}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecione o tipo" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Visualizador</SelectItem>
                  <SelectItem value="1">Operador</SelectItem>
                  <SelectItem value="2">Admin</SelectItem>
                </SelectContent>
              </Select>
            </label>
            <label className="block space-y-2">
              <span className="text-sm">{editing ? 'Nova senha (opcional)' : 'Senha inicial *'}</span>
              <Input
                type="password"
                value={form.senha}
                onChange={event => updateForm('senha', event.target.value)}
                placeholder={editing ? 'Deixe em branco para manter a senha atual' : 'Senha inicial'}
              />
            </label>
            <div className="flex justify-end gap-2">
              <Button type="button" variant="outline" disabled={saving} onClick={closeForm}>Cancelar</Button>
              <Button disabled={saving} type="submit">
                {saving ? 'Salvando...' : 'Salvar funcionário'}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={reasonDialog.open} onOpenChange={value => { if (!value) closeReasonDialog(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {reasonDialog.mode === 'activate' ? 'Reativar funcionário' : 'Desativar funcionário'}
            </DialogTitle>
            <DialogDescription>Informe um motivo para registrar a alteração de status.</DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="reason">Motivo *</Label>
            <Textarea
              id="reason"
              value={reason}
              onChange={event => setReason(event.target.value)}
              rows={4}
              placeholder="Descreva o motivo..."
              disabled={busyId !== null}
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" disabled={busyId !== null} onClick={closeReasonDialog}>Cancelar</Button>
            <Button onClick={() => void submitReason()} disabled={busyId !== null}>
              {busyId !== null ? 'Processando...' : 'Confirmar'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {historyTarget && (
        <FuncionarioHistoryDialog
          open={historyTarget !== null}
          onOpenChange={(value) => {
            if (!value) {
              setHistoryTarget(null);
            }
          }}
          funcionarioId={historyTarget.id}
          funcionarioNome={historyTarget.nomeFuncionario}
        />
      )}
    </div>
  );
}
