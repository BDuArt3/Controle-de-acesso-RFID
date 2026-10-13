# Análise Comparativa: TCC/Projeto vs. Frontend vs. Backend

> Gerado em: 2026-10-05. Objetivo: mapear o que já existe, o que falta, e o que precisa ser diferente do design original.

---

## 1. Arquitetura do Sistema

| Camada | TCC/Projeto | Backend (Atual) | Frontend (Atual) |
|--------|------------|----------------|-----------------|
| Backend API | ASP.NET Core 8 | ✅ Impl. | ✅ Conectado via fetch |
| Worker (Arduino) | C# Worker + SerialPort COM4 | ✅ Impl. | ❌ Sem integração direta |
| Tempo real | SignalR (CartaoHub, GravacaoHub) | ✅ Impl. | ❌ Sem conexão SignalR |
| Banco | SQL Server + EF Core | ✅ Migrations prontas | N/A |
| Autenticação | JWT Bearer | ✅ Impl. | ✅ Impl. |
| Frontend | ReactJS | N/A | ✅ Impl. (v7 componentes) |

---

## 2. Mapeamento de Fluxos

### 2.1 Autenticação (Login)

| Fluxo | TCC | Backend | Frontend | Status |
|-------|-----|---------|----------|--------|
| Tela de login (email/senha) | ✅ | ✅ POST /api/auth/login | ✅ `LoginScreen.tsx` | ✅ Coberto |
| JWT token storage (localStorage) | ✅ | ✅ Retorna token | ✅ `localStorage.setItem` | ✅ Coberto |
| Redirecionamento pós-login | ✅ | ✅ | ✅ `window.location.href` | ✅ Coberto |
| Logout (remover token) | ✅ | ✅ POST /api/auth/logout | ❌ Não existe | 🔴 **FALTA** |
| Refresh token / renew | ✅ (mencionado) | ❌ Não implementado | ❌ | ⚠️ **Parcial** |
| Senha esquecida / reset | ❌ (não está no escopo) | ❌ | ❌ | ✅ Fora do escopo |

### 2.2 Dashboard

| Fluxo | TCC | Backend | Frontend | Status |
|-------|-----|---------|----------|--------|
| Cards de estatísticas (total acessos, permitidos, negados) | ✅ Fig. + texto | ✅ GET /api/log/acesso/estatisticas/hoje | ❌ Dashboard vazio (sem fetch) | 🔴 **FALTA integração** |
| Gráficos (acessos permitidos vs negados, gráfico de rosca) | ✅ Fig. (3 gráficos) | ⚠️ API retorna stats, mas não dados para gráfico | ❌ Sem componente gráfico | 🔴 **FALTA** |
| Acesso recente (últimas 5 entradas) | ✅ Fig. | ✅ GET /api/log/acesso/ultimos?quantidade=50 | ❌ Não exibe | 🔴 **FALTA integração** |
| Atualização em tempo real via SignalR | ✅ Fig. + texto | ✅ GravacaoHub.SendNotification | ❌ Sem conexão SignalR | 🔴 **FALTA** |

### 2.3 Gestão de Usuários

| Fluxo | TCC | Backend | Frontend | Status |
|-------|-----|---------|----------|--------|
| Listar usuários (tabela com filtros) | ✅ | ✅ GET /api/usuario | ✅ `UserManagement.tsx` (fetch + tabela) | ✅ Coberto |
| Criar usuário (modal/form) | ✅ | ✅ POST /api/usuario | ✅ `UserForm.tsx` (POST) | ✅ Coberto |
| Editar usuário (modal/form) | ✅ | ✅ PUT /api/usuario/:id | ✅ `UserForm.tsx` (PUT) | ✅ Coberto |
| Desativar usuário (soft delete) | ✅ | ✅ DELETE /api/usuario/:id | ✅ `UserManagement.tsx` (deleta) | ✅ Coberto |
| Reativar usuário | ✅ (soft delete) | ✅ PUT /api/usuario/:id/ativar | ❌ Não existe | 🔴 **FALTA** |
| Filtro por nome/tipo | ✅ | ⚠️ API não tem filtro params | ⚠️ Filtro local no `filterUsers()` | ✅ Funciona (local) |
| Paginação | ❌ (não mencionado) | ❌ | ❌ | ✅ Fora do escopo |
| Criação de usuário + funcionário vinculados | ✅ (TCC: usuário e funcionário são entidades separadas) | ✅ FuncionarioController separado | ❌ **Não vincula funcionário** | 🔴 **GAP semântico** |

> **⚠️ GAP SEMÂNTICO CRÍTICO**: O TCC define duas entidades distintas — `Usuario` (login, email, senha, tipo) e `Funcionario` (nome, departamento, cargo, cartão). O frontend só gerencia `Usuario`. Não há componente de Gestão de Funcionários.

### 2.4 Gestão de Cartões RFID

| Fluxo | TCC | Backend | Frontend | Status |
|-------|-----|---------|----------|--------|
| Gravar cartão no funcionário | ✅ (UID + GUID, via Arduino) | ✅ POST /api/cartao + GravacaoHub | ❌ **Seção inexistente** | 🔴 **FALTA** |
| Status "Aguardando aproximação" | ✅ Fig. + texto | ✅ GravacaoHub.SendStatusUpdate | ❌ Sem componente | 🔴 **FALTA** |
| Listar cartões vinculados | ✅ | ✅ GET /api/cartao | ❌ Não existe | 🔴 **FALTA** |
| Desativar cartão (perda/roubo) | ✅ | ✅ DELETE /api/cartao/:id | ❌ Não existe | 🔴 **FALTA** |
| Reativar cartão | ✅ | ✅ PUT /api/cartao/:id/ativar | ❌ Não existe | 🔴 **FALTA** |
| Ver UID do cartão | ✅ | ✅ GET /api/cartao/uid/:uid | ❌ Não existe | 🔴 **FALTA** |

> **🔴 GAP MAIOR**: A gestão de cartões é um fluxo central do TCC (escaneamento físico, gravação GUID+UID, SignalR). **Não existe nenhum componente no frontend**.

### 2.5 Logs de Acesso

| Fluxo | TCC | Backend | Frontend | Status |
|-------|-----|---------|----------|--------|
| Listar logs de acesso | ✅ | ✅ GET /api/log/acesso | ✅ `AccessLogs.tsx` (fetch + tabela) | ✅ Coberto |
| Filtro por período | ✅ Fig. + texto | ✅ GET /api/log/acesso/periodo | ⚠️ Filtro por data no `filterLogs()` | ✅ Funciona (local) |
| Acessos permitidos vs negados | ✅ Fig. (badges) | ✅ GET /api/log/acesso/periodo/permitidos | ⚠️ Badge logic no componente | ✅ Coberto |
| Detalhes do log (cartão, funcionário, data) | ✅ | ✅ GET /api/log/acesso/:id | ⚠️ Exibe basicamente | ✅ Parcial |
| Buscar logs por UID do cartão | ✅ | ✅ GET /api/log/acesso/cartao/:uid | ❌ Não existe | ⚠️ **Parcial** |
| Estatísticas do dia | ✅ | ✅ GET /api/log/acesso/estatisticas/hoje | ❌ Não acessa | 🔴 **FALTA integração** |
| Relatório diário | ✅ (TCC) | ✅ GET /api/log/acesso/relatorio/diario | ❌ Não existe | 🔴 **FALTA** |
| Paginação | ❌ (não mencionado) | ❌ | ❌ | ✅ Fora do escopo |

### 2.6 Monitoramento em Tempo Real

| Fluxo | TCC | Backend | Frontend | Status |
|-------|-----|---------|----------|--------|
| Conexão SignalR ao backend | ✅ (texto TCC) | ✅ CartaoHub + GravacaoHub | ❌ **Sem conexão** | 🔴 **FALTA** |
| Notificação de novo acesso (toast) | ✅ Fig. (toast notification) | ✅ SendNotification via Hub | ❌ Sem toast/notifications | 🔴 **FALTA** |
| Status de gravação de cartão | ✅ Fig. (progresso) | ✅ SendStatusUpdate via Hub | ❌ Sem componente | 🔴 **FALTA** |
| Indicador de status online/offline | ❌ | ❌ | ❌ | ✅ Fora do escopo |

---

## 3. GAP Analysis Resumido

### ✅ Já coberto (funcional ou parcialmente funcional)
1. **Autenticação** — login, token storage, redirecionamento
2. **Gestão de Usuários** — CRUD completo (exceto reativação)
3. **Logs de Acesso** — listagem, filtro local, badges permitir/negado
4. **Layout** — `MainLayout.tsx` com sidebar e navegação

### 🔴 Falta implementar (novos componentes)
1. **Dashboard real** — conectar estatísticas (hoje), gráficos, acessos recentes
2. **Gestão de Funcionários** — página nova, CRUD (API já existe: `/api/funcionario`)
3. **Gestão de Cartões** — listagem, desativação, reativação
4. **Gravação de Cartão RFID** — fluxo com SignalR, status "aguardando aproximação"
5. **Monitoramento em tempo real** — SignalR connection, notificações toast
6. **Relatórios** — relatório diário, acessos permitidos/negados por período
7. **Logout** — endpoint existe, mas botão não existe no frontend
8. **Reativação de usuários/cartões** — endpoints existem, UI não

### ⚠️ Parcialmente coberto (existe mas com limitações)
1. **Filtros** — funcionam localmente no frontend, não usam filtros da API
2. **Gráficos** — placeholder visual no dashboard, sem dados reais
3. **Dados do log** — exibe informações básicas, mas não detalhes completos (sem busca por UID)

---

## 4. Diferenças: Figma vs. Realidade do Produto

### O que o Figma tem que o produto final NÃO precisa
- Referências visuais ao Figma (comentários, referências de design)
- Estados de "prototype" (hover states, transições de design apenas)

### O que o produto final PRECISA que o Figma não detalhou
- Integração real com Arduino (SerialPort -> Worker -> Hub -> API)
- Fluxo de gravação de cartão com feedback em tempo real
- Notificações toast para eventos do sistema
- Gestão de Funcionários (entidade separada de Usuário)
- Relatórios exportáveis
- Estados de loading/error em todas as operações

---

## 5. Recomendações de Prioridade

### Fase 1 — Fundamentos (antes de qualquer novo componente)
1. **Conectar Dashboard às estatísticas reais** — chamar `/api/log/acesso/estatisticas/hoje`
2. **Implementar SignalR** — conexão centralizada, receber notificações
3. **Implementar Logout** — simples, remove token e redireciona

### Fase 2 — Gestão de Funcionários e Cartões
4. **Página de Funcionários** — usar `FuncionarioController` API (já existe)
5. **Página de Cartões** — listagem, desativação, reativação
6. **Fluxo de Gravação de Cartão** — SignalR + UI de "aguardando aproximação"

### Fase 3 — Observabilidade
7. **Notificações toast** — em tempo real via SignalR
8. **Relatório diário** — chamar `/api/log/acesso/relatorio/diario`
9. **Gráficos reais** — substituir placeholders do dashboard

---

## 6. Observações sobre o TCC/Artigo

O TCC menciona funcionalidades que podem ser **ajustadas no artigo** para refletir o produto final:

- **Gráficos no dashboard** — o artigo descreve "gráficos de acesso" como requisito. Se não houver tempo para implementar, o artigo pode ser ajustado para focar em estatísticas numéricas em vez de gráficos visuais.
- **Entidade Funcionário vs. Usuário** — o TCC define ambas. O frontend só tem Usuário. Recomendo: (a) implementar Funcionário no frontend, ou (b) no artigo, consolidar como "entidade única de usuário".
- **Relatórios exportáveis** — mencionado no TCC mas não implementado. Pode ser removido do escopo do artigo ou mantido como "futuro trabalho".
- **Monitoramento em tempo real** — o TCC enfatiza SignalR. Se implementado, o artigo fica fortalecido. Se não, reduzir menções a "monitoramento em tempo real".
