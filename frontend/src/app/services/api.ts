import axios from 'axios';

export interface AuthUser { token: string; expiration: string; role: string; name: string; email: string }
export interface UsuarioDto { id: string; nome: string; email: string; documento: string; tipo: string; status: string; uid: string }
export interface LogDto { id: string; uid: string; usuarioId: string; userName: string; dataHora: string; resultado: string; motivo?: string }
export interface FuncionarioDto { id: string; nome: string; cpf: string; departamento: string; cargo: string; status: string }
export interface CartaoDto { id: string; uid: string; funcionarioId: string; funcionario?: { nome: string }; funcionarioNome?: string; status: string; dataRegistro: string }
export type UsuarioInput = Omit<UsuarioDto, 'id'> & { password?: string };
type Payload = Record<string, unknown>;
export const isActive = (status: string) => ['ativo', 'active'].includes(String(status).toLowerCase());
export const isGranted = (result: string) => ['permitido', 'autorizado', 'granted'].includes(String(result).toLowerCase());
export const errorMessage = (error: unknown) => {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 401) return 'Sessão expirada ou credenciais inválidas. Entre novamente.';
    const message = error.response?.data?.message;
    return typeof message === 'string' ? message : 'Não foi possível completar a operação. Verifique a API e a configuração de CORS.';
  }
  return 'Não foi possível completar a operação.';
};
const api = axios.create({ baseURL: 'http://localhost:5038/api', timeout: 15000, headers: { 'Content-Type': 'application/json' } });
api.interceptors.request.use(config => {
  const token = localStorage.getItem('authToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
api.interceptors.response.use(response => response, error => {
  if (error.response?.status === 401 && error.config?.url !== '/auth/login') {
    localStorage.removeItem('authToken');
    window.dispatchEvent(new Event('auth:logout'));
  }
  return Promise.reject(error);
});
export const authService = {
  login: (email: string, password: string) => api.post<AuthUser>('/auth/login', { email, password }),
  getMe: () => api.get<AuthUser>('/auth/me'),
  changePassword: (currentPassword: string, newPassword: string) => api.post('/auth/change-password', { currentPassword, newPassword })
};
export const usuarioService = {
  list: () => api.get<UsuarioDto[]>('/usuario'),
  get: (id: string) => api.get<UsuarioDto>(`/usuario/${encodeURIComponent(id)}`),
  create: (data: UsuarioInput) => api.post('/usuario', data),
  update: (id: string, data: UsuarioInput) => api.put(`/usuario/${encodeURIComponent(id)}`, data),
  bloquear: (id: string) => api.put(`/usuario/${encodeURIComponent(id)}/bloquear`),
  desbloquear: (id: string) => api.put(`/usuario/${encodeURIComponent(id)}/desbloquear`)
};
export const logAcessoService = {
  list: () => api.get<LogDto[]>('/logacesso'),
  get: (id: string) => api.get<LogDto>(`/logacesso/${encodeURIComponent(id)}`),
  create: (data: Payload) => api.post('/logacesso', data),
  getByUser: (id: string) => api.get<LogDto[]>(`/logacesso/usuario/${encodeURIComponent(id)}`),
  getDaily: (date: string) => api.get<LogDto[]>(`/logacesso/diario/${encodeURIComponent(date)}`)
};
export const acessoService = { registrar: (data: Payload) => api.post('/acesso', data) };
export const funcionarioService = {
  list: () => api.get('/funcionario'), get: (id: string) => api.get(`/funcionario/${encodeURIComponent(id)}`),
  create: (data: Payload) => api.post('/funcionario', data), update: (id: string, data: Payload) => api.put(`/funcionario/${encodeURIComponent(id)}`, data),
  delete: (id: string) => api.delete(`/funcionario/${encodeURIComponent(id)}`)
};
export const cartaoService = {
  list: () => api.get('/cartao'), get: (id: string) => api.get(`/cartao/${encodeURIComponent(id)}`),
  create: (data: { uid: string; funcionarioId: string }) => api.post('/cartao', data),
  desativar: (id: string) => api.put(`/cartao/${encodeURIComponent(id)}/desativar`),
  reativar: (id: string) => api.put(`/cartao/${encodeURIComponent(id)}/reativar`)
};
export default api;
