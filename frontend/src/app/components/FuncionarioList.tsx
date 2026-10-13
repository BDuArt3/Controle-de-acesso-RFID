import React, { useEffect, useState } from 'react';
import { Briefcase, Plus, Search, Pencil, Trash2, Loader2 } from 'lucide-react';
import { funcionarioService, errorMessage, isActive, type FuncionarioDto } from '../services/api';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Switch } from './ui/switch';
import { Badge } from './ui/badge';
import { Card, CardContent } from './ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from './ui/dialog';
import { showToast } from './Toast';

const formatCpf = (value: string) =>
  value.replace(/\D/g, '').slice(0, 11).replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d{1,2})$/, '$1-$2');

const emptyForm = { nome: '', cpf: '', departamento: 'TI', cargo: '', status: 'ativo' };

export default function FuncionarioList() {
  const [items, setItems] = useState<FuncionarioDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [pending, setPending] = useState(false);
  const [deleting, setDeleting] = useState<FuncionarioDto | null>(null);

  const load = async () => {
    setLoading(true);
    setError('');
    try { setItems((await funcionarioService.list()).data); }
    catch (failure) { setError(errorMessage(failure)); }
    finally { setLoading(false); }
  };

  useEffect(() => { void load(); }, []);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    if (form.cpf.replace(/\D/g, '').length !== 11 || !form.nome.trim()) {
      showToast('Informe o nome e um CPF com 11 dígitos.', 'error');
      return;
    }
    setPending(true);
    try {
      const payload = { ...form, nome: form.nome.trim(), cpf: form.cpf.replace(/\D/g, '') };
      if (editing) await funcionarioService.update(editing, payload);
      else await funcionarioService.create(payload);
      showToast(editing ? 'Funcionário atualizado com sucesso.' : 'Funcionário criado com sucesso.', 'success');
      setOpen(false);
      await load();
    } catch (failure) { showToast(errorMessage(failure), 'error'); }
    finally { setPending(false); }
  };

  const toggle = async (item: FuncionarioDto) => {
    setPending(true);
    try {
      await funcionarioService.update(item.id, { ...item, status: isActive(item.status) ? 'inativo' : 'ativo' });
      showToast('Status atualizado.', 'success');
      await load();
    } catch (failure) { showToast(errorMessage(failure), 'error'); }
    finally { setPending(false); }
  };

  const remove = async () => {
    if (!deleting) return;
    setPending(true);
    try {
      await funcionarioService.delete(deleting.id);
      setDeleting(null);
      showToast('Funcionário excluído.', 'success');
      await load();
    } catch (failure) { showToast(errorMessage(failure), 'error'); }
    finally { setPending(false); }
  };

  const filtered = items.filter(item =>
    item.nome.toLowerCase().includes(search.toLowerCase()) ||
    (search.replace(/\D/g, '').length > 0 && item.cpf.replace(/\D/g, '').includes(search.replace(/\D/g, '')))
  );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="mb-2 text-xs font-medium uppercase tracking-[0.15em] text-muted-foreground">Gestão de acesso / Pessoas</p>
          <h2 className="text-3xl font-semibold">Funcionários</h2>
          <p className="mt-2 text-sm text-muted-foreground">Organize sua equipe e gerencie quem tem acesso.</p>
        </div>
        <Button onClick={() => { setEditing(null); setForm(emptyForm); setOpen(true); }}>
          <Plus className="mr-2 size-4" />Adicionar Funcionário
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          <div className="flex flex-wrap items-center justify-between gap-4 border-b p-5">
            <div className="flex items-center gap-2">
              <Briefcase className="size-5" />
              <h3 className="font-medium">Equipe cadastrada</h3>
              <Badge variant="secondary">{items.length}</Badge>
            </div>
            <div className="relative w-full sm:w-72">
              <Search className="absolute left-3 top-3 size-4 text-muted-foreground" />
              <Input aria-label="Buscar funcionário" placeholder="Buscar por nome ou CPF..." className="pl-9" value={search} onChange={e => setSearch(e.target.value)} />
            </div>
          </div>

          {loading ? (
            <p role="status" className="flex items-center justify-center gap-2 p-12 text-muted-foreground">
              <Loader2 className="size-5 animate-spin" />Carregando funcionários...
            </p>
          ) : error ? (
            <div role="alert" className="p-6 text-red-700">
              {error}
              <Button variant="outline" className="ml-3" onClick={load}>Tentar novamente</Button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted/40 text-muted-foreground">
                  <tr>
                    {['Nome', 'CPF', 'Departamento', 'Cargo', 'Status', 'Ações'].map(label => (
                      <th key={label} className="px-5 py-4 font-medium">{label}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(item => (
                    <tr key={item.id} className="border-t transition-colors hover:bg-muted/30">
                      <td className="whitespace-nowrap px-5 py-5 font-medium">{item.nome}</td>
                      <td className="whitespace-nowrap px-5 font-mono text-xs">{formatCpf(item.cpf)}</td>
                      <td className="px-5">{item.departamento || '—'}</td>
                      <td className="px-5">{item.cargo || '—'}</td>
                      <td className="px-5">
                        <div className="flex items-center gap-2">
                          <Switch aria-label={`Alterar status de ${item.nome}`} checked={isActive(item.status)} disabled={pending} onCheckedChange={() => toggle(item)} />
                          <span>{isActive(item.status) ? 'Ativo' : 'Inativo'}</span>
                        </div>
                      </td>
                      <td className="px-5">
                        <div className="flex gap-1">
                          <Button variant="ghost" size="icon" aria-label={`Editar ${item.nome}`} disabled={pending}
                            onClick={() => { setEditing(item.id); setForm({ nome: item.nome, cpf: formatCpf(item.cpf), departamento: item.departamento || 'Outros', cargo: item.cargo || '', status: isActive(item.status) ? 'ativo' : 'inativo' }); setOpen(true); }}>
                            <Pencil className="size-4" />
                          </Button>
                          <Button variant="ghost" size="icon" aria-label={`Excluir ${item.nome}`} disabled={pending} onClick={() => setDeleting(item)}>
                            <Trash2 className="size-4 text-red-600" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {!filtered.length && <p className="p-12 text-center text-muted-foreground">Nenhum funcionário encontrado.</p>}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={open} onOpenChange={value => { if (!pending) setOpen(value); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? 'Editar funcionário' : 'Adicionar funcionário'}</DialogTitle>
            <DialogDescription>Preencha os dados de identificação e vínculo profissional.</DialogDescription>
          </DialogHeader>
          <form onSubmit={save} className="space-y-4">
            <label className="block space-y-2">
              <span className="text-sm">Nome *</span>
              <Input required value={form.nome} onChange={e => setForm({ ...form, nome: e.target.value })} />
            </label>
            <label className="block space-y-2">
              <span className="text-sm">CPF *</span>
              <Input required inputMode="numeric" placeholder="000.000.000-00" value={form.cpf} onChange={e => setForm({ ...form, cpf: formatCpf(e.target.value) })} />
            </label>
            <label className="block space-y-2">
              <span className="text-sm">Departamento</span>
              <select className="h-10 w-full rounded-md border bg-card px-3 text-sm" value={form.departamento} onChange={e => setForm({ ...form, departamento: e.target.value })}>
                {['TI', 'RH', 'Operacional', 'Administrativo', 'Outros'].map(v => <option key={v}>{v}</option>)}
              </select>
            </label>
            <label className="block space-y-2">
              <span className="text-sm">Cargo</span>
              <Input value={form.cargo} onChange={e => setForm({ ...form, cargo: e.target.value })} />
            </label>
            <div className="flex items-center justify-between rounded-md border p-3">
              <span className="text-sm">Funcionário ativo</span>
              <Switch aria-label="Funcionário ativo" checked={isActive(form.status)} onCheckedChange={value => setForm({ ...form, status: value ? 'ativo' : 'inativo' })} />
            </div>
            <div className="flex justify-end gap-2">
              <Button type="button" variant="outline" disabled={pending} onClick={() => setOpen(false)}>Cancelar</Button>
              <Button disabled={pending} type="submit">{pending ? 'Salvando...' : 'Salvar funcionário'}</Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={!!deleting} onOpenChange={value => { if (!value && !pending) setDeleting(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Excluir funcionário?</DialogTitle>
            <DialogDescription>Você está prestes a excluir {deleting?.nome}. Esta ação não pode ser desfeita.</DialogDescription>
          </DialogHeader>
          <div className="flex justify-end gap-2">
            <Button variant="outline" disabled={pending} onClick={() => setDeleting(null)}>Cancelar</Button>
            <Button variant="destructive" disabled={pending} onClick={remove}>{pending ? 'Excluindo...' : 'Confirmar exclusão'}</Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
