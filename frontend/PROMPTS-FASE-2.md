# Fase 2: Gestão de Funcionários, Cartões e Melhorias

## Tarefas da Fase 2

### Tarefa 2.1: Criar componente `FuncionarioList.tsx`

Nova página para gestão de funcionários (dados já mapeados no backend).

**Endpoints:**
```
GET    /api/funcionario
POST   /api/funcionario
PUT    /api/funcionario/{id}
DELETE /api/funcionario/{id}
```

**Estrutura do componente:**
- Layout similar a `UserManagement.tsx` (card com lista)
- Colunas da tabela: Nome, CPF, Departamento, Cargo, Status (ativo/inativo), Ações
- Busca por nome/CPF
- Botão "Adicionar Funcionário" → abre modal de formulário
- Ações: editar, excluir (com confirmação)
- Conectar com `funcionarioService.list()`, `create()`, `update()`, `delete()`
- Status toggle: ativar/desativar

**Formulário de funcionário (inline ou modal):**
- Nome (obrigatório)
- CPF (obrigatório, formatado)
- Departamento (select: TI, RH, Operacional, Administrativo, Outros)
- Cargo (input texto)
- Status (ativo/inativo, toggle)
- Botão salvar

**Integração com SignalR (pré-visual):**
- Ao criar/editar/excluir funcionário, emitir toast de sucesso
- Notificação de erro em caso de falha

### Tarefa 2.2: Criar componente `CartaoList.tsx`

Nova página para gestão de cartões RFID.

**Endpoints:**
```
GET    /api/cartao
POST   /api/cartao
PUT    /api/cartao/{id}/desativar
PUT    /api/cartao/{id}/reativar
```

**Estrutura do componente:**
- Layout similar a `UserManagement.tsx`
- Colunas: UID, Funcionário Vinculado, Status (ativo/inativo), Data de Registro, Ações
- Busca por UID ou nome do funcionário
- Botão "Registrar Novo Cartão" → abre modal para leitura RFID
- Ações: desativar, reativar, ver detalhes
- Conectar com `cartaoService.list()`, `desativar()`, `reativar()`

**Modal de registro de cartão:**
- Campo UID (após leitura SignalR, preenche automaticamente)
- Select: funcionário vinculado (lista de funcionários ativos)
- Botão "Ler Cartão" → inicia modo de espera SignalR
- Ao ler com sucesso: preenche UID automaticamente
- Botão "Salvar" → chama `POST /api/cartao` com { uid, funcionarioId }

**Integração SignalR (visual):**
- Estado `isReadingCard`: mostra spinner + mensagem "Aproxime o cartão RFID do leitor..."
- Ao ler: feedback visual com cor verde e mensagem "Cartão lido: UID XXXX"
- Toast de erro se falhar a leitura

### Tarefa 2.3: Adicionar rotas no MainLayout

Em `MainLayout.tsx`:
1. Adicionar itens à sidebar: "Funcionários" e "Cartões"
2. Tipos de rota: `'dashboard' | 'users' | 'logs' | 'funcionarios' | 'cartoes' | 'user-form'`
3. Ícones: `Briefcase` (funcionários), `CreditCard` (cartões) — lucide-react
4. Renderização: `<FuncionarioList />` e `<CartaoList />`
5. Manter sidebar items existentes: Dashboard, Usuários, Logs

### Tarefa 2.4: Adicionar toast de notificações

Criar componente `Toast.tsx` em `app/components/` (usar shadcn/ui ou estilização simples):

```typescript
// app/components/Toast.tsx
import { useEffect } from 'react';

interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info';
}

let toasts: Toast[] = [];
const listeners: Array<(t: Toast[]) => void> = [];

export function showToast(message: string, type: Toast['type'] = 'info') {
  const id = Date.now().toString();
  toasts = [...toasts, { id, message, type }];
  listeners.forEach(fn => fn(toasts));
  setTimeout(() => removeToast(id), 4000);
}

export function removeToast(id: string) {
  toasts = toasts.filter(t => t.id !== id);
  listeners.forEach(fn => fn(toasts));
}

export function ToastContainer() {
  // Renderizar toasts no canto superior direito
}
```

Integrar em `App.tsx` (renderizar `<ToastContainer />` no topo).

### Tarefa 2.5: Atualizar App.tsx com novas rotas

```typescript
// Novas rotas no App.tsx
const ProtectedRoutes = () => {
  return (
    <Routes>
      <Route path="/" element={<MainLayout />} />
      <Route path="/usuarios" element={<UserManagement />} />
      <Route path="/usuarios/novo" element={<UserForm />} />
      <Route path="/usuarios/editar/:id" element={<UserForm />} />
      <Route path="/logs" element={<AccessLogs />} />
      <Route path="/funcionarios" element={<FuncionarioList />} />
      <Route path="/cartoes" element={<CartaoList />} />
      <Route path="*" element={<Navigate to="/" />} />
    </Routes>
  );
};
```

### Tarefa 2.6: Adicionar export CSV real em AccessLogs

Em `AccessLogs.tsx`, substituir a função mock `handleExport`:

```typescript
const handleExport = () => {
  // Gerar CSV da lista filtrada
  const headers = 'ID,UID,Usuário,Data/Hora,Resultado,Motivo\n';
  const rows = filteredLogs.map(log =>
    `${log.id},${log.uid},"${log.userName}",${log.datetime},${log.result},"${log.reason || ''}"`
  );
  const csv = headers + rows.join('\n');
  
  // Download do arquivo
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `logs-acesso-${new Date().toISOString().slice(0, 10)}.csv`;
  link.click();
  URL.revokeObjectURL(link.href);
};
```

## Regras Importantes

- Manter design consistente com componentes existentes (shadcn/ui, lucide-react)
- Layout responsivo: mobile-friendly
- Loading states para todos os componentes de lista
- Erros visíveis ao usuário (toast ou alert inline)
- Não adicionar novas dependências
- Não remover componentes existentes

## Checklist de Validação

- [ ] Funcionários: listagem, criação, edição, exclusão funcionam
- [ ] Funcionários: tabela com colunas corretas
- [ ] Funcionários: busca por nome/CPF funciona
- [ ] Cartões: listagem, desativar, reativar funcionam
- [ ] Cartões: modal de registro com leitura RFID (pré-visual)
- [ ] Cartões: vinculação com funcionário via select
- [ ] Sidebar: novos itens (Funcionários, Cartões) aparecem e navegam
- [ ] Toasts: notificações aparecem em ações de sucesso/erro
- [ ] Export CSV: arquivo gerado e baixado corretamente
- [ ] Responsivo: tudo funciona em telas pequenas

## Ordem de Execução Sugerida

1. Criar `api.ts` atualizado (funcionarioService, cartaoService já devem existir)
2. Criar `FuncionarioList.tsx`
3. Criar `CartaoList.tsx`
4. Atualizar `MainLayout.tsx` (sidebar + rotas)
5. Criar `Toast.tsx` e integrar em `App.tsx`
6. Atualizar `App.tsx` (rotas)
7. Adicionar export CSV em `AccessLogs.tsx`
8. Validar checklist
