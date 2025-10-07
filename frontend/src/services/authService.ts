import { ApiService } from './apiClient';

// Tipos para autenticação
export interface LoginRequest {
  usuarioAcesso: string;
  senha: string;
}

export interface LoginResponse {
  success: boolean;
  message?: string;
  accessToken?: string;
  refreshToken?: string;
  expiresIn?: number;
  funcionario?: {
    id: string;
    nomeFuncionario: string;
    usuarioAcesso: string;
    tipo: string;
    ativo: boolean;
  };
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
    if (response.success && response.accessToken && response.refreshToken) {
      localStorage.setItem('access_token', response.accessToken);
      localStorage.setItem('refresh_token', response.refreshToken);
      
      // Salvar dados do usuário
      if (response.funcionario) {
        localStorage.setItem('rfid_user', JSON.stringify({
          id: response.funcionario.id,
          name: response.funcionario.nomeFuncionario,
          username: response.funcionario.usuarioAcesso,
          role: response.funcionario.tipo.toLowerCase() as 'admin' | 'operador' | 'visualizador'
        }));
      }
    }
    
    return response;
  }

  async refreshToken(refreshToken: string): Promise<LoginResponse> {
    const response = await this.post<LoginResponse>('/Auth/refresh-token', { refreshToken });
    
    if (response.success && response.accessToken && response.refreshToken) {
      localStorage.setItem('access_token', response.accessToken);
      localStorage.setItem('refresh_token', response.refreshToken);
    }
    
    return response;
  }

  async logout(): Promise<void> {
    try {
      const refreshToken = localStorage.getItem('refresh_token');
      if (refreshToken) {
        await this.post('/Auth/logout', { refreshToken });
      }
    } catch (error) {
      // Ignorar erro de logout, apenas limpar dados locais
      console.warn('Erro ao fazer logout no servidor:', error);
    } finally {
      // Sempre limpar dados locais
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