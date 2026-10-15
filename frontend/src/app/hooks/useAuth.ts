import { createContext, createElement, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import {
  authService,
  roleFromLoginType,
  tokenStorage,
  type AuthMeResponse,
  type AuthTokens,
  type RoleName,
} from '../services/api';

export interface AuthUser {
  token: string;
  refreshToken: string;
  expiration: string;
  role: RoleName;
  name: string;
  email: string;
}

function getProfileName(profile: Partial<AuthMeResponse> & { nome?: string; nomeFuncionario?: string; email?: string }) {
  return profile.nomeFuncionario?.trim()
    || profile.nome?.trim()
    || profile.email?.split('@')[0]?.trim()
    || '';
}

function readStoredUser(): AuthUser | null {
  const accessToken = tokenStorage.getAccessToken();
  const refreshToken = tokenStorage.getRefreshToken();
  const expiration = tokenStorage.getExpiresAt() || '';
  if (!accessToken || !refreshToken) {
    return null;
  }

  return {
    token: accessToken,
    refreshToken,
    expiration,
    role: 'Visualizador',
    name: '',
    email: '',
  };
}

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, senha: string) => Promise<void>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => readStoredUser());
  const [isLoading, setIsLoading] = useState(true);
  const generation = useRef(0);

  const clearSession = useCallback(() => {
    generation.current += 1;
    tokenStorage.clear();
    setUser(null);
    setIsLoading(false);
  }, []);

  const logout = useCallback(() => {
    clearSession();
  }, [clearSession]);

  const refreshUser = useCallback(async () => {
    const accessToken = tokenStorage.getAccessToken();
    const refreshToken = tokenStorage.getRefreshToken();
    const current = generation.current;

    if (!accessToken || !refreshToken) {
      clearSession();
      return;
    }

    try {
      const { data } = await authService.getMe();
      if (current !== generation.current) {
        return;
      }

      setUser({
        token: accessToken,
        refreshToken,
        expiration: tokenStorage.getExpiresAt() || '',
        role: roleFromLoginType(data.tipo),
        name: getProfileName(data),
        email: data.email || '',
      });
    } catch {
      if (current === generation.current) {
        clearSession();
      }
    } finally {
      if (current === generation.current) {
        setIsLoading(false);
      }
    }
  }, [clearSession]);

  useEffect(() => {
    const handleLogout = () => {
      clearSession();
    };

    window.addEventListener('auth:logout', handleLogout);
    void refreshUser();

    return () => {
      window.removeEventListener('auth:logout', handleLogout);
    };
  }, [clearSession, refreshUser]);

  useEffect(() => {
    if (!user?.expiration) {
      return;
    }

    const check = () => {
      if (Date.now() >= Date.parse(user.expiration)) {
        clearSession();
      }
    };

    check();
    const timer = window.setInterval(check, 1000);
    return () => window.clearInterval(timer);
  }, [clearSession, user]);

  const login = async (email: string, senha: string) => {
    const { data } = await authService.login(email, senha);

    if (!data.success || !data.token || !data.usuario) {
      throw new Error(data.message || 'Resposta de autenticação inválida');
    }

    const tokens: AuthTokens = data.token;
    const role = roleFromLoginType(data.usuario.tipo);

    tokenStorage.set(tokens);
    generation.current += 1;
    setUser({
      token: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      expiration: tokens.expiresAt,
      role,
      name: data.usuario.nomeFuncionario?.trim() || data.usuario.email.split('@')[0] || '',
      email: data.usuario.email,
    });
  };

  return createElement(AuthContext.Provider, { value: { user, isAuthenticated: !!user, isLoading, login, logout, refreshUser } }, children);
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
