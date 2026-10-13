# Fase 1: Dashboard Real, Autenticação e Logout

## Contexto do Projeto

Sistema de controle de acesso RFID com leitor MFRC522 + Arduino Uno. Backend em C# ASP.NET Core 8 com JWT, SignalR, API REST e Worker Service. Frontend React com TypeScript, Vite, Tailwind CSS, shadcn/ui, lucide-react, axios, react-router-dom.

Backend em: C:/Github/Controle-de-acesso-RFID/
Frontend em: C:/Github/Controle-de-acesso-RFID-Frontend/
Documentação: C:/Github/Controle-de-acesso-RFID-Frontend/FLUXO-FRONTEND.md

## API Base: http://localhost:5038/api

### Autenticação (AuthController)
```
POST   /api/auth/login            → body: { email, password }
       Response: { token, expiration, role, name, email }
GET    /api/auth/me               → header: Authorization: Bearer {token}
       Response: { token, expiration, role, name, email }
POST   /api/auth/change-password  → body: { currentPassword, newPassword }
```

### Usuários (UsuarioController)
```
GET    /api/usuario               → response: UsuarioDto[]
       Response: { id, nome, email, documento, tipo, status, uid }
GET    /api/usuario/{id}          → response: UsuarioDto
POST   /api/usuario               → body: { nome, email, password, documento, tipo, status, uid }
PUT    /api/usuario/{id}          → body: { nome, email, documento, tipo, status, uid }
PUT    /api/usuario/{id}/bloquear → bloqueia usuário
PUT    /api/usuario/{id}/desbloquear → desbloqueia usuário
```

### Logs de Acesso (LogAcessoController)
```
GET    /api/logacesso             → response: LogDto[]
       Response: { id, uid, usuarioId, userName, dataHora, resultado, motivo }
GET    /api/logacesso/{id}        → response: LogDto
POST   /api/logacesso             → body: { uid, resultado, motivo }
GET    /api/logacesso/usuario/{userId}
GET    /api/logacesso/diario/{data}
```

### Endpoint de Acesso (AcessoController)
```
POST   /api/acesso                → body: { uid, resultado, motivo }
       Response: { success, logId, message }
```

### Funcionário (FuncionarioController)
```
GET    /api/funcionario
POST   /api/funcionario
PUT    /api/funcionario/{id}
DELETE /api/funcionario/{id}
GET    /api/funcionario/{id}
```

### Cartão (CartaoController)
```
GET    /api/cartao                → response: CartaoDto[]
GET    /api/cartao/{id}
POST   /api/cartao                → body: { uid, funcionarioId }
PUT    /api/cartao/{id}/desativar
PUT    /api/cartao/{id}/reativar
```

### SignalR Hub
```
Hub URL: http://localhost:5038/hubs/notificacoes
Method: sendNotification(message)
Method: sendAccessGranted(uid, userName)
Method: sendAccessDenied(uid, reason)
```

## Estado do Frontend Atual

### App.tsx (Rotas)
- `/` → redirect para `/login`
- `/login` → LoginScreen
- `/` → MainLayout com children

### LoginScreen.tsx
- Email/senha, validação, redireciona para `/` ao logar
- Salva token no localStorage
- **PRECISA CONECTAR** com `POST /api/auth/login`

### Dashboard.tsx
- 4 cards de métricas com dados mockados (totalUsers, accessesToday, deniedToday, readerStatus)
- Tabela de últimos acessos com dados mockados
- **PRECISA CONECTAR** com `GET /api/logacesso` e `GET /api/usuario`

### UserManagement.tsx
- Lista de usuários com search local, edit, bloquear/desbloquear
- Botão "Adicionar Novo" vai para formulário
- **PRECISA CONECTAR** com `GET/POST/PUT/PUT bloquear/PUT desbloquear`

### AccessLogs.tsx
- Lista de logs com filtros (busca, período, resultado)
- Botão exportar (mock)
- **PRECISA CONECTAR** com `GET /api/logacesso`

### UserForm.tsx
- Formulário para criar/editar usuário
- Botão "Ler Novo Cartão" (simulado com setTimeout)
- **PRECISA CONECTAR** com SignalR para leitura RFID real

### MainLayout.tsx
- Header com nome do usuário e dropdown de logout
- Sidebar com Dashboard, Usuários, Logs
- **PRECISA ADICIONAR** logout funcional

## Tarefas da Fase 1

### Tarefa 1.1: Criar arquivo de serviço API (`app/services/api.ts`)

Criar arquivo em `app/services/api.ts` que centraliza todas as chamadas HTTP:

```typescript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5038/api',
  headers: { 'Content-Type': 'application/json' }
});

// Interceptor para adicionar token JWT automaticamente
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Auth services
export const authService = {
  login: (email: string, password: string) => 
    api.post('/auth/login', { email, password }),
  getMe: () => 
    api.get('/auth/me'),
  changePassword: (current: string, newPw: string) => 
    api.post('/auth/change-password', { currentPassword: current, newPassword: newPw })
};

// Usuario services
export const usuarioService = {
  list: () => api.get('/usuario'),
  get: (id: string) => api.get(`/usuario/${id}`),
  create: (data: any) => api.post('/usuario', data),
  update: (id: string, data: any) => api.put(`/usuario/${id}`, data),
  bloquear: (id: string) => api.put(`/usuario/${id}/bloquear`),
  desbloquear: (id: string) => api.put(`/usuario/${id}/desbloquear`)
};

// LogAcesso services
export const logAcessoService = {
  list: () => api.get('/logacesso'),
  get: (id: string) => api.get(`/logacesso/${id}`),
  create: (data: any) => api.post('/logacesso', data),
  getByUser: (userId: string) => api.get(`/logacesso/usuario/${userId}`),
  getDaily: (data: string) => api.get(`/logacesso/diario/${data}`)
};

// Acesso service (endpoint genérico)
export const acessoService = {
  registrar: (data: any) => api.post('/acesso', data)
};

// Funcionario services
export const funcionarioService = {
  list: () => api.get('/funcionario'),
  get: (id: string) => api.get(`/funcionario/${id}`),
  create: (data: any) => api.post('/funcionario', data),
  update: (id: string, data: any) => api.put(`/funcionario/${id}`, data),
  delete: (id: string) => api.delete(`/funcionario/${id}`)
};

// Cartao services
export const cartaoService = {
  list: () => api.get('/cartao'),
  get: (id: string) => api.get(`/cartao/${id}`),
  create: (data: any) => api.post('/cartao', data),
  desativar: (id: string) => api.put(`/cartao/${id}/desativar`),
  reativar: (id: string) => api.put(`/cartao/${id}/reativar`)
};

export default api;
```

### Tarefa 1.2: Criar hook de autenticação (`app/hooks/useAuth.ts`)

```typescript
import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { authService } from '../services/api';

interface AuthUser {
  token: string;
  expiration: string;
  role: string;
  name: string;
  email: string;
}

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('authToken');
    if (token) {
      refreshUser();
    } else {
      setIsLoading(false);
    }
  }, []);

  const refreshUser = async () => {
    const token = localStorage.getItem('authToken');
    if (!token) {
      setUser(null);
      setIsLoading(false);
      return;
    }
    try {
      const response = await authService.getMe();
      setUser(response.data);
    } catch (error) {
      localStorage.removeItem('authToken');
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (email: string, password: string) => {
    const response = await authService.login(email, password);
    const data = response.data;
    localStorage.setItem('authToken', data.token);
    setUser(data);
  };

  const logout = () => {
    localStorage.removeItem('authToken');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated: !!user,
      isLoading,
      login,
      logout,
      refreshUser
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
}
```

### Tarefa 1.3: Conectar LoginScreen com API

Em `LoginScreen.tsx`, substituir:
1. Imports: adicionar `useAuth` de `app/hooks/useAuth` e `useNavigate` de `react-router-dom`
2. Estado: adicionar `isLoading` e `error` para feedback visual
3. handleSubmit: chamar `auth.login(email, password)` e redirecionar para `/`
4. Remove dados mockados — não há mais dados fixos no login
5. Manter design visual (inputs, botão, estilos) — apenas conectar com API real

### Tarefa 1.4: Conectar Dashboard com API

Em `Dashboard.tsx`, substituir:
1. Imports: adicionar `useEffect`, `useState`, e `useAuth`
2. Estados:
   - `metrics`: `{ totalUsers, accessesToday, deniedToday, readerStatus }`
   - `recentAccesses`: array de acessos recentes
   - `isLoading`: boolean para loading state
3. useEffect de loading: ao montar o componente, fazer:
   - `GET /api/usuario` para contar usuários ativos (totalUsers)
   - `GET /api/logacesso` para obter acessos do dia (acessesToday, deniedToday)
   - Filtrar logs por data atual e resultado
4. RecentAccesses: pegar os últimos 5 logs da API
5. Mantém o layout e styling visual — troca dados mockados por dados reais
6. Adiciona estado de loading: mostrar skeleton ou "Carregando..." enquanto carrega

### Tarefa 1.5: Conectar UserManagement com API

Em `UserManagement.tsx`, substituir:
1. Importar `usuarioService` de `app/services/api`
2. Estados: `users`, `isLoading`, `searchQuery`
3. useEffect de loading: `GET /api/usuario` para carregar lista
4. toggleUserStatus: chamar `PUT /api/usuario/{id}/bloquear` ou `desbloquear`
5. onNavigate('user-form'): passar id do usuário para edição
6. Search: manter filtro local (é rápido e não sobrecarrega a API)
7. Remove dados mockados — tudo vem da API

### Tarefa 1.6: Conectar AccessLogs com API

Em `AccessLogs.tsx`, substituir:
1. Importar `logAcessoService` de `app/services/api`
2. Estados: `logs`, `searchQuery`, `resultFilter`, `dateFilter`, `isLoading`
3. useEffect de loading: `GET /api/logacesso` para carregar todos os logs
4. Filtros: manter filtros locais (nome/UID, resultado, período) — a API não filtra por esses campos ainda
5. Export: criar função que gera CSV da lista filtrada
6. Mantém layout e styling — troca dados mockados por API real

### Tarefa 1.7: Conectar UserForm com API

Em `UserForm.tsx`, substituir:
1. Importar `usuarioService` de `app/services/api`
2. useEffect de loading: se `editingUserId`, faz `GET /api/usuario/{id}` para carregar dados
3. handleSubmit:
   - Se edição: `PUT /api/usuario/{id}` com dados atualizados
   - Se criação: `POST /api/usuario` com dados do form
4. handleReadCard: manter estado `isReading` — a conexão SignalR virá na Fase 3
5. Remove dados mockados — tudo é via API

### Tarefa 1.8: Conectar MainLayout com Auth

Em `MainLayout.tsx`, substituir:
1. Importar `useAuth` e `useNavigate`
2. `currentUser` vem do hook `useAuth()` (remover prop)
3. onLogout: chamar `auth.logout()` e redirecionar para `/login`
4. Mantém todo o layout visual — apenas conecta com contexto de auth
5. Adiciona proteção: se não autenticado, redireciona para login

### Tarefa 1.9: Configurar App.tsx com AuthProvider

Em `App.tsx`:
1. Importar `AuthProvider` de `app/hooks/useAuth`
2. Envolver toda a aplicação com `<AuthProvider>`
3. Rotas protegidas: MainLayout só renderiza se autenticado
4. Se não autenticado → mostra LoginScreen
5. Mantém rotas existentes

## Regras Importantes

- **NÃO modificar estilos visuais** existentes — apenas conectar dados com API
- **NÃO adicionar novas funcionalidades** nesta fase
- Manter nomes de componentes e props existentes
- Usar `try/catch` em todas as chamadas API
- Em caso de erro 401, fazer logout automático
- Loading states: usar spinner simples ou skeleton básico
- Não adicionar novos imports de bibliotecas além dos já instalados (axios, react-router-dom, lucide-react)
- Tratar CORS: backend deve estar rodando em `localhost:5038`

## Checklist de Validação

- [ ] Login com credenciais reais funciona
- [ ] Token JWT é salvo e reutilizado
- [ ] `/api/auth/me` retorna dados corretos
- [ ] Dashboard mostra métricas reais (contagem de usuários, acessos do dia)
- [ ] UserManagement lista usuários da API
- [ ] Bloquear/desbloquear usuário funciona
- [ ] AccessLogs mostra logs reais com filtros
- [ ] UserForm cria/edita usuários via API
- [ ] Logout remove token e redireciona para login
- [ ] Página protegida: URL `/` sem auth vai para login
- [ ] Nenhum dado mockado restante

## Ordem de Execução Sugerida

1. Criar `api.ts` (serviço)
2. Criar `useAuth.ts` (hook + contexto)
3. Atualizar `App.tsx` (AuthProvider + rotas protegidas)
4. Conectar `LoginScreen.tsx`
5. Conectar `MainLayout.tsx`
6. Conectar `Dashboard.tsx`
7. Conectar `UserManagement.tsx`
8. Conectar `AccessLogs.tsx`
9. Conectar `UserForm.tsx`
10. Validar checklist
