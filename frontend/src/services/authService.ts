import { ApiService } from './apiClient';

// Tipos para autenticação
export interface LoginRequest {
  usuarioAcesso: string;
  senha: string;
}

export interface LoginResponse {
  success: boolean;
  message?: string;
  token?: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    tokenType: string;
  };
  usuario?: {
    id: string;
    nomeFuncionario: string;
    usuarioAcesso: string;
    tipo: number; // Enum: 0=Visualizador, 1=Operador, 2=Admin
    isActive: boolean;
  };
}

// Função para mapear o tipo de funcionário
function mapTipoFuncionario(tipo: number): 'admin' | 'operador' | 'visualizador' {
  switch (tipo) {
    case 2: return 'admin';
    case 1: return 'operador';
    case 0: return 'visualizador';
    default: return 'visualizador';
  }
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface UserInfo {
  id: string;
  nome: string;
  usuarioAcesso: string;
  tipo: string;
  isAuthenticated: boolean;
}

export class AuthService extends ApiService {
  
  async login(loginData: LoginRequest): Promise<LoginResponse> {
    const response = await this.post<LoginResponse>('/Auth/login', loginData);
    
    // Se login bem-sucedido, salvar tokens
    if (response.success && response.token?.accessToken && response.token?.refreshToken) {
      localStorage.setItem('access_token', response.token.accessToken);
      localStorage.setItem('refresh_token', response.token.refreshToken);
      
      // Salvar dados do usuário
      if (response.usuario) {
        localStorage.setItem('rfid_user', JSON.stringify({
          id: response.usuario.id,
          name: response.usuario.nomeFuncionario,
          username: response.usuario.usuarioAcesso,
          role: mapTipoFuncionario(response.usuario.tipo)
        }));
      }
    }
    
    return response;
  }

  async refreshToken(refreshToken: string): Promise<LoginResponse> {
    const response = await this.post<LoginResponse>('/Auth/refresh-token', { refreshToken });
    
    if (response.success && response.token?.accessToken && response.token?.refreshToken) {
      localStorage.setItem('access_token', response.token.accessToken);
      localStorage.setItem('refresh_token', response.token.refreshToken);
    }
    
    return response;
  }

  async logout(): Promise<void> {
    try {
      const refreshToken = localStorage.getItem('refresh_token');
      if (refreshToken) {
        // Tentar fazer logout no servidor, mas não falhar se der erro
        try {
          await this.post('/Auth/logout', { refreshToken });
        } catch (serverError: any) {
          // Log do erro, mas não propagar (pode ser token expirado, etc.)
          console.warn('Erro ao fazer logout no servidor (ignorado):', serverError);
        }
      }
    } catch (error) {
      // Capturar qualquer erro inesperado
      console.warn('Erro inesperado durante logout:', error);
    } finally {
      // SEMPRE limpar dados locais, independente do que aconteça
      localStorage.removeItem('access_token');
      localStorage.removeItem('refresh_token');
      localStorage.removeItem('rfid_user');
    }
  }

  async getCurrentUser(): Promise<UserInfo> {
    return this.get<UserInfo>('/Auth/me');
  }

  isAuthenticated(): boolean {
    const token = localStorage.getItem('access_token');
    const user = localStorage.getItem('rfid_user');
    return !!(token && user);
  }

  getStoredUser(): any {
    const userData = localStorage.getItem('rfid_user');
    return userData ? JSON.parse(userData) : null;
  }

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }
}

// Instância singleton do serviço de autenticação
export const authService = new AuthService();