import axios, { type AxiosError, type AxiosRequestConfig, type AxiosResponse } from 'axios';
import { API_BASE_URL } from './apiConfig';

export type RoleName = 'Visualizador' | 'Operador' | 'Admin';
export type LoginRoleIndex = 0 | 1 | 2;
export type UsuarioTipo = 0 | 1 | 2 | 3;
export type FuncionarioTipo = 0 | 1 | 2;
export type CartaoStatus = 0 | 1 | 2;
export type CartaoSolicitacaoStatus = 'Pendente' | 'Enviada' | 'Concluida' | 'Falhou' | 'Cancelada' | 'Expirada';
export type MotivoTipo =
  | 1
  | 2
  | 3
  | 4
  | 5
  | 6
  | 7
  | 8;

export interface TokenDto {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  tokenType: string;
}

export interface LoginUsuarioDto {
  id: string;
  nomeFuncionario: string;
  email: string;
  tipo: LoginRoleIndex;
  isActive: boolean;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  token: TokenDto | null;
  usuario: LoginUsuarioDto | null;
}

export interface AuthMeResponse {
  id: string;
  nomeFuncionario: string;
  email: string;
  tipo: RoleName;
  isAuthenticated: boolean;
}

export interface UsuarioCartaoResumo {
  id: string;
  uid?: string;
  status?: CartaoStatus;
  isActive?: boolean;
}

export interface UsuarioDto {
  id: string;
  nome: string;
  email?: string | null;
  tipo: UsuarioTipo;
  isActive: boolean;
  isFuncionario: boolean;
  cartoes?: UsuarioCartaoResumo[];
}

export interface CreateUsuarioDto {
  nome: string;
  email?: string | null;
  tipo: UsuarioTipo;
}

export interface FuncionarioDto {
  id: string;
  nomeFuncionario: string;
  email: string;
  tipo: FuncionarioTipo;
  isActive: boolean;
}

export interface CreateFuncionarioDto {
  nomeFuncionario: string;
  email: string;
  cpf: string;
  senha: string;
  tipo: FuncionarioTipo;
}

export interface UpdateFuncionarioDto extends CreateFuncionarioDto {
  senha?: string;
}

export interface CartaoDto {
  id: string;
  usuarioId: string;
  uid: string;
  status: CartaoStatus;
  isActive: boolean;
  usuarioNome?: string;
  nomeUsuario?: string;
  cartaoAnteriorId?: string | null;
  motivoReemissao?: string | null;
  dataRegistro: string;
  dataExpiracao?: string | null;
}

export interface CreateCartaoDto {
  usuarioId: string;
  cartaoAnteriorId?: string;
  motivoReemissao?: string;
}

export interface SolicitacaoCartaoDto {
  solicitacaoId: string;
  status: CartaoSolicitacaoStatus;
  mensagem: string;
  usuarioId: string;
  nomeUsuario: string;
  dataSolicitacao: string;
  cartaoId: string | null;
}

export interface LogAcessoDto {
  id: string;
  dataHora: string;
  cartaoId?: string | null;
  usuarioId?: string | null;
  uidInformado?: string | null;
  cartaoUid?: string | null;
  acessoPermitido: boolean;
  motivo: string;
  motivoTipo: MotivoTipo;
  usuarioNome?: string | null;
  usuarioTipo?: string | null;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  tokenType: string;
}

export interface LogoutResponse {
  message: string;
}

export interface RequestErrorBody {
  message?: string;
  errors?: Record<string, unknown> | string[] | string;
  title?: string;
  detail?: string;
}

export interface HistoricoPaginado<TItem> {
  items: TItem[];
  page: number;
  pageSize: number;
  totalItems: number;
}

export interface StatusHistoricoItem {
  id: number;
  isActive: boolean;
  dataHora: string;
  motivo: string;
  alteradoPorFuncionarioId: string | null;
  alteradoPorNome: string | null;
}

export interface CartaoStatusHistoricoItem {
  id: number;
  status: CartaoStatus;
  statusNome: string;
  dataHora: string;
  motivo: string;
  alteradoPorFuncionarioId: string | null;
  alteradoPorNome: string | null;
}

export interface CartaoAssociacaoHistorico {
  id: number;
  dataAssociacao: string;
  dataDesassociacao: string | null;
  associadoPorFuncionarioId: string | null;
  associadoPorNome: string | null;
  desassociadoPorFuncionarioId: string | null;
  desassociadoPorNome: string | null;
}

export type UsuarioHistoricoResponse = HistoricoPaginado<StatusHistoricoItem>;
export type FuncionarioHistoricoResponse = HistoricoPaginado<StatusHistoricoItem>;

export interface CartaoHistoricoResponse {
  cartaoId: string;
  uid: string;
  associacao: CartaoAssociacaoHistorico | null;
  status: HistoricoPaginado<CartaoStatusHistoricoItem>;
}

export interface HistoricoQueryParams {
  page?: number;
  pageSize?: number;
}

const ACCESS_TOKEN_KEY = 'authToken';
const REFRESH_TOKEN_KEY = 'authRefreshToken';
const EXPIRES_AT_KEY = 'authExpiresAt';
const TOKEN_TYPE_KEY = 'authTokenType';

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const tokenStorage = {
  getAccessToken() {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  },
  getRefreshToken() {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },
  getExpiresAt() {
    return localStorage.getItem(EXPIRES_AT_KEY);
  },
  getTokenType() {
    return localStorage.getItem(TOKEN_TYPE_KEY) || 'Bearer';
  },
  set(tokens: AuthTokens) {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    localStorage.setItem(EXPIRES_AT_KEY, tokens.expiresAt);
    localStorage.setItem(TOKEN_TYPE_KEY, tokens.tokenType || 'Bearer');
  },
  clear() {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
    localStorage.removeItem(TOKEN_TYPE_KEY);
  },
};

export function buildMessageFromError(error: unknown) {
  if (!axios.isAxiosError(error)) {
    if (error instanceof Error && error.message) {
      return error.message;
    }
    return 'Não foi possível completar a operação.';
  }

  const status = error.response?.status;
  if (status === 401) {
    return 'Sessão expirada ou credenciais inválidas. Entre novamente.';
  }
  if (status === 403) {
    return 'Você não tem permissão para executar esta ação.';
  }

  const data = error.response?.data as RequestErrorBody | string | undefined;
  if (typeof data === 'string' && data.trim()) {
    return data;
  }
  if (data && typeof data === 'object') {
    if (typeof data.message === 'string' && data.message.trim()) {
      return data.message;
    }
    if (typeof data.detail === 'string' && data.detail.trim()) {
      return data.detail;
    }
    if (typeof data.title === 'string' && data.title.trim()) {
      return data.title;
    }
    if (Array.isArray(data.errors)) {
      const messages = data.errors
        .map(item => (typeof item === 'string' ? item : ''))
        .filter(Boolean);
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }
    if (data.errors && typeof data.errors === 'object') {
      const nestedMessages = Object.values(data.errors)
        .flatMap(value => (Array.isArray(value) ? value : [value]))
        .map(value => (typeof value === 'string' ? value : ''))
        .filter(Boolean);
      if (nestedMessages.length > 0) {
        return nestedMessages.join(' ');
      }
    }
  }

  return 'Não foi possível completar a operação. Verifique a API e a configuração de CORS.';
}

export const errorMessage = buildMessageFromError;

export function isConflictError(error: unknown) {
  return axios.isAxiosError(error) && error.response?.status === 409;
}

export function conflictMessage(error: unknown, fallback: string) {
  const data = axios.isAxiosError(error) ? error.response?.data as RequestErrorBody | undefined : undefined;
  const message = data && typeof data === 'object' ? data.message : undefined;
  return typeof message === 'string' && message.trim() ? message.trim() : fallback;
}

export function isActive(value: unknown) {
  if (typeof value === 'boolean') {
    return value;
  }
  if (typeof value === 'number') {
    return value === 0;
  }
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase();
    return normalized === 'ativo'
      || normalized === 'active'
      || normalized === 'true'
      || normalized === 'sim'
      || normalized === '1';
  }
  return false;
}

export function isGranted(value: unknown) {
  if (typeof value === 'boolean') {
    return value;
  }
  const normalized = String(value).trim().toLowerCase();
  return normalized === 'permitido'
    || normalized === 'autorizado'
    || normalized === 'granted'
    || normalized === 'true'
    || normalized === '1';
}

export function roleFromLoginType(tipo: LoginRoleIndex | number | string | null | undefined): RoleName {
  const text = String(tipo ?? '').trim().toLowerCase();
  if (text.startsWith('admin')) {
    return 'Admin';
  }
  if (text.startsWith('operador')) {
    return 'Operador';
  }
  const numeric = typeof tipo === 'number' ? tipo : Number(tipo);
  if (numeric === 2) {
    return 'Admin';
  }
  if (numeric === 1) {
    return 'Operador';
  }
  return 'Visualizador';
}

export function normalizeEmail(email: string | null | undefined) {
  const value = email?.trim();
  return value ? value : null;
}

export function createIdempotencyKey() {
  if (globalThis.crypto && typeof globalThis.crypto.randomUUID === 'function') {
    return globalThis.crypto.randomUUID();
  }
  return `idem-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

api.interceptors.request.use((config) => {
  const token = tokenStorage.getAccessToken();
  if (token) {
    const headers = config.headers ?? {};
    (headers as Record<string, string>).Authorization = `Bearer ${token}`;
    config.headers = headers;
  }
  return config;
});

api.interceptors.response.use(
  response => response,
  (error: AxiosError) => {
    const url = error.config?.url ?? '';
    if (error.response?.status === 401 && !url.includes('/auth/login')) {
      tokenStorage.clear();
      window.dispatchEvent(new Event('auth:logout'));
    }
    return Promise.reject(error);
  },
);

function withIdempotencyKey(config?: AxiosRequestConfig, idempotencyKey?: string): AxiosRequestConfig | undefined {
  if (!idempotencyKey) {
    return config;
  }
  return {
    ...(config ?? {}),
    headers: {
      ...(config?.headers ?? {}),
      'Idempotency-Key': idempotencyKey,
    },
  };
}

export const authService = {
  login: (email: string, senha: string) => api.post<LoginResponse>('/auth/login', { email, senha }),
  refreshToken: (refreshToken: string) => api.post<TokenDto>('/auth/refresh-token', { refreshToken }),
  logout: (refreshToken: string) => api.post<LogoutResponse>('/auth/logout', { refreshToken }),
  getMe: () => api.get<AuthMeResponse>('/auth/me'),
  changeSenha: (currentsenha: string, newsenha: string) => api.post('/auth/change-senha', { currentsenha, newsenha }),
};

export const usuarioService = {
  list: () => api.get<UsuarioDto[]>('/usuario'),
  get: (id: string) => api.get<UsuarioDto>(`/usuario/${encodeURIComponent(id)}`),
  create: (data: CreateUsuarioDto) => api.post<UsuarioDto>('/usuario', {
    nome: data.nome,
    email: normalizeEmail(data.email),
    tipo: data.tipo,
  }),
  update: (id: string, data: CreateUsuarioDto) => api.put<UsuarioDto>(`/usuario/${encodeURIComponent(id)}`, {
    nome: data.nome,
    email: normalizeEmail(data.email),
    tipo: data.tipo,
  }),
  deletar: (id: string, motivo: string) => api.delete<{ message: string }>(`/usuario/${encodeURIComponent(id)}`, { data: { motivo } }),
  ativar: (id: string, motivo: string) => api.put<{ message: string }>(`/usuario/${encodeURIComponent(id)}/ativar`, { motivo }),
};

export const usuarioHistoricoService = {
  listByUsuarioId: (id: string, params?: HistoricoQueryParams) =>
    api.get<UsuarioHistoricoResponse>(`/usuario/${encodeURIComponent(id)}/historico`, { params }),
};

export const funcionarioService = {
  list: () => api.get<FuncionarioDto[]>('/funcionario'),
  get: (id: string) => api.get<FuncionarioDto>(`/funcionario/${encodeURIComponent(id)}`),
  create: (data: CreateFuncionarioDto) => api.post<FuncionarioDto>('/funcionario', data),
  update: (id: string, data: UpdateFuncionarioDto) => api.put<FuncionarioDto>(`/funcionario/${encodeURIComponent(id)}`, data),
  delete: (id: string, motivo: string) => api.delete<{ message: string }>(`/funcionario/${encodeURIComponent(id)}`, { data: { motivo } }),
  ativar: (id: string, motivo: string) => api.patch<{ message: string }>(`/funcionario/${encodeURIComponent(id)}/ativar`, { motivo }),
};

export const funcionarioHistoricoService = {
  listByFuncionarioId: (id: string, params?: HistoricoQueryParams) =>
    api.get<FuncionarioHistoricoResponse>(`/funcionario/${encodeURIComponent(id)}/historico`, { params }),
};

export const cartaoService = {
  list: () => api.get<CartaoDto[]>('/cartao'),
  get: (id: string) => api.get<CartaoDto>(`/cartao/${encodeURIComponent(id)}`),
  getByUid: (uid: string) => api.get<CartaoDto>(`/cartao/uid/${encodeURIComponent(uid)}`),
  getByUsuario: (usuarioId: string) => api.get<CartaoDto[]>(`/cartao/usuario/${encodeURIComponent(usuarioId)}`),
  create: (data: CreateCartaoDto, idempotencyKey?: string) =>
    api.post<SolicitacaoCartaoDto>('/cartao', data, withIdempotencyKey(undefined, idempotencyKey)),
  getSolicitacao: (id: string) => api.get<SolicitacaoCartaoDto>(`/cartao/solicitacoes/${encodeURIComponent(id)}`),
  retentarSolicitacao: (id: string) => api.post<SolicitacaoCartaoDto>(`/cartao/solicitacoes/${encodeURIComponent(id)}/retentar`),
  cancelarSolicitacao: (id: string, motivo: string) => api.post<{ message: string }>(`/cartao/solicitacoes/${encodeURIComponent(id)}/cancelar`, { motivo }),
  update: (id: string, data: Partial<CreateCartaoDto>) => api.put<CartaoDto>(`/cartao/${encodeURIComponent(id)}`, data),
  bloquear: (id: string, motivo: string) => api.delete<{ message: string }>(`/cartao/${encodeURIComponent(id)}`, { data: { motivo } }),
  ativar: (id: string, motivo: string) => api.put<{ message: string }>(`/cartao/${encodeURIComponent(id)}/ativar`, { motivo }),
  cancelar: (id: string, motivo: string) => api.post<{ message: string }>(`/cartao/${encodeURIComponent(id)}/cancelar`, { motivo }),
};

export const cartaoHistoricoService = {
  listByCartaoId: (id: string, params?: HistoricoQueryParams) =>
    api.get<CartaoHistoricoResponse>(`/cartao/${encodeURIComponent(id)}/historico`, { params }),
};

export type ExportFormat = 'csv' | 'pdf';

export interface ExportLogsParams {
  dataInicio: string;
  dataFim: string;
  acessoPermitido?: boolean;
  formato: ExportFormat;
}

export interface ExportedFile {
  blob: Blob;
  fileName: string;
}

export function fileNameFromContentDisposition(header: unknown, fallback: string) {
  const value = typeof header === 'string' ? header : '';
  const encoded = /filename\*\s*=\s*(?:UTF-8|utf-8)''([^;]+)/i.exec(value)?.[1];
  if (encoded) {
    try {
      return decodeURIComponent(encoded.trim().replace(/^"|"$/g, ''));
    } catch {
      // usa o nome simples abaixo
    }
  }
  const plain = /filename\s*=\s*(?:"([^"]+)"|([^;]+))/i.exec(value);
  const name = (plain?.[1] ?? plain?.[2])?.trim();
  return name || fallback;
}

async function readBlobError(error: unknown) {
  if (!axios.isAxiosError(error) || !(error.response?.data instanceof Blob)) {
    return error;
  }
  const blob = error.response.data;
  let parsed: unknown = undefined;
  try {
    const text = await blob.text();
    try {
      parsed = JSON.parse(text);
    } catch {
      parsed = text;
    }
  } catch {
    // mantém o erro original
  }
  error.response.data = parsed;
  return error;
}

async function exportLogs(params: ExportLogsParams): Promise<ExportedFile> {
  try {
    const response = await api.get<Blob>('/log-acesso/exportar', { params, responseType: 'blob' });
    const headers = response.headers as Record<string, unknown> | undefined;
    return {
      blob: response.data,
      fileName: fileNameFromContentDisposition(
        headers?.['content-disposition'],
        `logs-de-acesso.${params.formato}`,
      ),
    };
  } catch (error) {
    throw await readBlobError(error);
  }
}

export const logAcessoService = {
  list: () => api.get<LogAcessoDto[]>('/log-acesso'),
  exportar: exportLogs,
  get: (id: string) => api.get<LogAcessoDto>(`/log-acesso/${encodeURIComponent(id)}`),
  getByCartaoUid: (uid: string) => api.get<LogAcessoDto[]>(`/log-acesso/cartao/${encodeURIComponent(uid)}`),
  getByPeriodo: (dataInicio: string, dataFim: string) =>
    api.get<LogAcessoDto[]>('/log-acesso/periodo', { params: { dataInicio, dataFim } }),
  getPermitidosByPeriodo: (dataInicio: string, dataFim: string) =>
    api.get<LogAcessoDto[]>('/log-acesso/periodo/permitidos', { params: { dataInicio, dataFim } }),
  getNegadosByPeriodo: (dataInicio: string, dataFim: string) =>
    api.get<LogAcessoDto[]>('/log-acesso/periodo/negados', { params: { dataInicio, dataFim } }),
  getUltimos: (quantidade = 50) => api.get<LogAcessoDto[]>('/log-acesso/ultimos', { params: { quantidade } }),
  getEstatisticasHoje: () => api.get('/log-acesso/estatisticas/hoje'),
  getRelatorioDiario: (data: string) => api.get('/log-acesso/relatorio/diario', { params: { data } }),
  create: (data: Record<string, unknown>) => api.post('/log-acesso', data),
};

export const acessoService = {
  registrar: (data: Record<string, unknown>) => api.post('/acesso', data),
};

export default api;
