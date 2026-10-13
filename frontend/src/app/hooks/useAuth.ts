import { createContext, createElement, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { authService, type AuthUser } from '../services/api';

interface AuthContextType {
  user: AuthUser | null; isAuthenticated: boolean; isLoading: boolean;
  login: (email: string, password: string) => Promise<void>; logout: () => void; refreshUser: () => Promise<void>;
}
const AuthContext = createContext<AuthContextType | undefined>(undefined);
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const generation = useRef(0);
  const logout = useCallback(() => {
    generation.current += 1;
    localStorage.removeItem('authToken'); setUser(null); setIsLoading(false);
  }, []);
  const refreshUser = useCallback(async () => {
    const token = localStorage.getItem('authToken');
    const current = generation.current;
    if (!token) { setUser(null); setIsLoading(false); return; }
    try {
      const { data } = await authService.getMe();
      if (current === generation.current) setUser({ ...data, token: data.token || token });
    } catch { if (current === generation.current) logout(); }
    finally { setIsLoading(false); }
  }, [logout]);
  useEffect(() => {
    window.addEventListener('auth:logout', logout);
    void refreshUser();
    return () => window.removeEventListener('auth:logout', logout);
  }, [logout, refreshUser]);
  useEffect(() => {
    if (!user) return;
    const check = () => { if (Date.now() >= Date.parse(user.expiration)) logout(); };
    check();
    const timer = window.setInterval(check, 1000);
    return () => window.clearInterval(timer);
  }, [user, logout]);
  const login = async (email: string, password: string) => {
    try {
      const { data } = await authService.login(email, password);
      if (!data.token || !Number.isFinite(Date.parse(data.expiration)) || Date.parse(data.expiration) <= Date.now()) throw new Error('Resposta de autenticação inválida');
      generation.current += 1;
      localStorage.setItem('authToken', data.token); setUser(data);
    } catch (error) { throw error; }
  };
  return createElement(AuthContext.Provider, { value: { user, isAuthenticated: !!user, isLoading, login, logout, refreshUser } }, children);
}
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
}
