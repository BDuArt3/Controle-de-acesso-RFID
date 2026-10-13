import { useEffect, useState } from 'react';
import { CreditCard, Plus, Search, Loader2, Radio, CheckCircle2, Eye, Power } from 'lucide-react';
import { cartaoService, funcionarioService, isActive, errorMessage, type CartaoDto, type FuncionarioDto } from '../services/api';
import { useSignalR } from '../hooks/useSignalR';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Badge } from './ui/badge';
import { Card, CardContent } from './ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from './ui/dialog';
import { showToast } from './Toast';

export default function CartaoList() {
  const [items, setItems] = useState<CartaoDto[]>([]);
  const [employees, setEmployees] = useState<FuncionarioDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [open, setOpen] = useState(false);
  const [uid, setUid] = useState('');
  const [employeeId, setEmployeeId] = useState('');
  const [isReadingCard, setIsReadingCard] = useState(false);
  const [pending, setPending] = useState(false);
  const [details, setDetails] = useState<CartaoDto | null>(null);

  const { isConnected } = useSignalR();

  const load = async () => {
    setLoading(true); setError('');
    try { const [cards, people] = await Promise.all([cartaoService.list(), funcionarioService.list()]); setItems(cards.data); setEmployees(people.data); }
    catch (failure) { setError(errorMessage(failure)); } finally { setLoading(false); }
  };
  useEffect(() => { void load(); }, []);

  // Listen for RFID reads (from SignalR via rfid:read event or manual simulation)
  useEffect(() => {
    if (!isReadingCard || !open) return;
    const onRead = (event: Event) => {
      const value = (event as CustomEvent<{ uid?: string }>).detail?.uid;
      if (!value?.trim()) { showToast('Falha na leitura do cartão.', 'error'); setIsReadingCard(false); return; }
      setUid(value.trim()); setIsReadingCard(false);
    };
    window.addEventListener('rfid:read', onRead);
    const timer = window.setTimeout(() => { setIsReadingCard(false); showToast('Tempo de leitura esgotado. Tente novamente.', 'error'); }, 30000);
    return () => { clearTimeout(timer); window.removeEventListener('rfid:read', onRead); };
  }, [isReadingCard, open]);

  const name = (item: CartaoDto) => item.funcionario?.nome || item.funcionarioNome || employees.find(person => String(person.id) === String(item.funcionarioId))?.nome || 'Sem vínculo identificado';
  const date = (value: string) => value && !Number.isNaN(Date.parse(value)) ? new Date(value).toLocaleString('pt-BR') : '—';

  const save = async () => {
    if (!uid.trim() || uid.startsWith('PREVIEW-') || !employees.some(person => String(person.id) === employeeId && isActive(person.status))) return;
    setPending(true);
    try { await cartaoService.create({ uid: uid.trim(), funcionarioId: employeeId }); setOpen(false); showToast('Cartão registrado com sucesso.', 'success'); await load(); }
    catch (failure) { showToast(errorMessage(failure), 'error'); } finally { setPending(false); }
  };

  const toggle = async (item: CartaoDto) => {
    setPending(true);
    try { if (isActive(item.status)) await cartaoService.desativar(item.id); else await cartaoService.reativar(item.id); showToast('Status do cartão atualizado.', 'success'); await load(); }
    catch (failure) { showToast(errorMessage(failure), 'error'); } finally { setPending(false); }
  };

  const filtered = items.filter(item => `${item.uid} ${name(item)}`.toLocaleLowerCase().includes(search.toLocaleLowerCase()));

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="mb-2 text-xs font-medium uppercase tracking-[0.15em] text-muted-foreground">Gestão de acesso / Credenciais</p>
          <h2 className="text-3xl font-semibold">Cartões RFID</h2>
          <p className="mt-2 text-sm text-muted-foreground">Registre, vincule e controle as credenciais da sua equipe.</p>
        </div>
        <Button onClick={() => { setUid(''); setEmployeeId(''); setIsReadingCard(false); setOpen(true); }}>
          <Plus className="mr-2 size-4" />Registrar Novo Cartão
        </Button>
      </div>

      <Card>
        <CardContent className="p-0">
          <div className="flex flex-wrap items-center justify-between gap-4 border-b p-5">
            <div className="flex items-center gap-2">
              <CreditCard className="size-5" />
              <h3 className="font-medium">Cartões cadastrados</h3>
              <Badge variant="secondary">{items.length}</Badge>
            </div>
            <div className="relative w-full sm:w-72">
              <Search className="absolute left-3 top-3 size-4 text-muted-foreground" />
              <Input aria-label="Buscar cartão" className="pl-9" placeholder="Buscar por UID ou funcionário..." value={search} onChange={event => setSearch(event.target.value)} />
            </div>
          </div>

          {loading ? (
            <p role="status" className="flex justify-center gap-2 p-12 text-muted-foreground"><Loader2 className="size-5 animate-spin" />Carregando cartões...</p>
          ) : error ? (
            <div role="alert" className="p-6 text-red-700">{error}<Button className="ml-3" variant="outline" onClick={load}>Tentar novamente</Button></div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-muted/40 text-muted-foreground">
                  <tr>{['UID', 'Funcionário Vinculado', 'Status', 'Data de Registro', 'Ações'].map(label => <th className="whitespace-nowrap px-5 py-4 font-medium" key={label}>{label}</th>)}</tr>
                </thead>
                <tbody>
                  {filtered.map(item => (
                    <tr key={item.id} className="border-t hover:bg-muted/30">
                      <td className="px-5 py-5 font-mono">{item.uid}</td>
                      <td className="whitespace-nowrap px-5 font-medium">{name(item)}</td>
                      <td className="px-5"><Badge variant="secondary" className={isActive(item.status) ? 'bg-emerald-50 text-emerald-800' : ''}>{isActive(item.status) ? 'Ativo' : 'Inativo'}</Badge></td>
                      <td className="whitespace-nowrap px-5 text-muted-foreground">{date(item.dataRegistro)}</td>
                      <td className="px-5">
                        <div className="flex gap-1">
                          <Button size="icon" variant="ghost" aria-label={`Ver detalhes de ${item.uid}`} onClick={() => setDetails(item)}><Eye className="size-4" /></Button>
                          <Button disabled={pending} variant="ghost" onClick={() => toggle(item)}><Power className="mr-2 size-4" />{isActive(item.status) ? 'Desativar' : 'Reativar'}</Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {!filtered.length && <p className="p-12 text-center text-muted-foreground">Nenhum cartão encontrado.</p>}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Register card modal */}
      <Dialog open={open} onOpenChange={value => { if (!pending) { setOpen(value); setIsReadingCard(false); } }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Registrar novo cartão</DialogTitle>
            <DialogDescription>Vincule uma credencial RFID a um funcionário ativo.</DialogDescription>
          </DialogHeader>

          <div className={`flex flex-col items-center gap-3 rounded-lg border border-dashed p-6 ${uid ? 'border-emerald-300 bg-emerald-50 text-emerald-800' : 'bg-muted/30'}`} role="status">
            {isReadingCard ? <Loader2 className="size-9 animate-spin" /> : uid ? <CheckCircle2 className="size-9" /> : <Radio className="size-9 text-muted-foreground" />}
            <p className="text-center text-sm">
              {isReadingCard ? 'Aproxime o cartão RFID do leitor...' : uid ? `Cartão lido: UID ${uid}` : 'Pronto para ler sua credencial'}
            </p>
            <Button type="button" variant="outline" disabled={isReadingCard || pending} onClick={() => { setUid(''); setIsReadingCard(true); }}>
              Ler Cartão
            </Button>
            {isReadingCard && !isConnected && (
              <Button type="button" variant="ghost" onClick={() => window.dispatchEvent(new CustomEvent('rfid:read', { detail: { uid: 'PREVIEW-' + crypto.randomUUID().slice(0, 8).toUpperCase() } }))}>
                Simular leitura (pré-visual)
              </Button>
            )}
          </div>

          {!isConnected && (
            <p className="text-xs text-muted-foreground">
              Hub SignalR desconectado. Use a simulação ou informe o UID manualmente. UIDs simulados não podem ser salvos.
            </p>
          )}

          <label className="space-y-2 text-sm">
            UID
            <Input value={uid} disabled={isReadingCard || pending} placeholder="UID do cartão" onChange={event => setUid(event.target.value)} />
          </label>

          <label className="space-y-2 text-sm">
            Funcionário vinculado
            <select className="h-10 w-full rounded-md border bg-card px-3" value={employeeId} onChange={event => setEmployeeId(event.target.value)}>
              <option value="">Selecione um funcionário</option>
              {employees.filter(person => isActive(person.status)).map(person => (
                <option key={person.id} value={person.id}>{person.nome}</option>
              ))}
            </select>
          </label>

          {!employees.some(person => isActive(person.status)) && (
            <p className="text-sm text-amber-800">Cadastre ou ative um funcionário antes de registrar um cartão.</p>
          )}

          <div className="flex justify-end gap-2">
            <Button variant="outline" disabled={pending} onClick={() => { setOpen(false); setIsReadingCard(false); }}>Cancelar</Button>
            <Button
              disabled={pending || isReadingCard || !uid.trim() || uid.startsWith('PREVIEW-') || !employeeId}
              onClick={save}
            >
              {pending ? 'Salvando...' : 'Salvar cartão'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* Details modal */}
      <Dialog open={!!details} onOpenChange={value => { if (!value) setDetails(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Detalhes do cartão</DialogTitle>
            <DialogDescription>Identificação e vínculo da credencial.</DialogDescription>
          </DialogHeader>
          {details && (
            <dl className="space-y-4 text-sm">
              {[['UID', details.uid], ['Funcionário', name(details)], ['Status', isActive(details.status) ? 'Ativo' : 'Inativo'], ['Data de registro', date(details.dataRegistro)]].map(([label, value]) => (
                <div key={label} className="flex justify-between gap-4 border-b pb-3">
                  <dt className="text-muted-foreground">{label}</dt>
                  <dd className="break-all text-right font-medium">{value}</dd>
                </div>
              ))}
            </dl>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
