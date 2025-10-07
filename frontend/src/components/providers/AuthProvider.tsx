import React, { createContext, useContext, useState, useEffect } from 'react';
import { authService, LoginRequest } from '../../services';

interface User {
  id: string;
  name: string;
  username: string;
  role: 'admin' | 'operador' | 'visualizador';
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  currentView: string;
  setCurrentView: (view: string) => void;
  error: string | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const mockUsers: { [key: string]: { password: string; user: User } } = {
  'admin': {
    password: 'admin123',
    user: {
      id: '1',
      name: 'João Administrador',
      username: 'admin',
      role: 'admin'
    }
  },
  'operador': {
    password: 'op123',
    user: {
      id: '2',
      name: 'Maria Operadora',
      username: 'operador',
      role: 'operador'
    }
  },
  'visualizador': {
    password: 'vis123',
    user: {
      id: '3',
      name: 'Carlos Visualizador',
      username: 'visualizador',
      role: 'visualizador'
    }
  }
};

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [currentView, setCurrentView] = useState('dashboard');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const initializeAuth = async () => {
      try {
        setError(null);
        
        if (authService.isAuthenticated()) {
          const storedUser = authService.getStoredUser();
          if (storedUser) {
            setUser(storedUser);
          } else {
            try {
              const currentUser = await authService.getCurrentUser();
              const userData: User = {
                id: currentUser.id,
                name: currentUser.nome,
                username: currentUser.usuarioAcesso,
                role: currentUser.tipo.toLowerCase() as 'admin' | 'operador' | 'visualizador'
              };
              setUser(userData);
              localStorage.setItem('rfid_user', JSON.stringify(userData));
            } catch (error) {
              console.warn('Erro ao obter dados do usuário:', error);
              await authService.logout();
            }
          }
        }
      } catch (error) {
        console.error('Erro ao inicializar autenticação:', error);
        setError('Erro ao conectar com o servidor');
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();
  }, []);

  const login = async (username: string, password: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    
    try {
      const loginData: LoginRequest = {
        usuarioAcesso: username,
        senha: password
      };

      const response = await authService.login(loginData);
      
      if (response.success && response.funcionario) {
        const userData: User = {
          id: response.funcionario.id,
          name: response.funcionario.nomeFuncionario,
          username: response.funcionario.usuarioAcesso,
          role: response.funcionario.tipo.toLowerCase() as 'admin' | 'operador' | 'visualizador'
        };
        
        setUser(userData);
        setCurrentView('dashboard');
        return true;
      } else {
        setError(response.message || 'Credenciais inválidas');
        return false;
      }
      
    } catch (error: any) {
      console.error('Erro no login com API:', error);
      
      console.warn('API indisponível, usando autenticação mock como fallback');
      setError('API indisponível, usando modo offline');
      
      await new Promise(resolve => setTimeout(resolve, 1000));

      if (mockUsers[username] && mockUsers[username].password === password) {
        const userData = mockUsers[username].user;
        setUser(userData);
        localStorage.setItem('rfid_user', JSON.stringify(userData));
        setCurrentView('dashboard');
        setError(null);
        return true;
      } else {
        setError('Credenciais inválidas');
        return false;
      }
      
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    setIsLoading(true);
    setError(null);
    
    try {
      await authService.logout();
    } catch (error) {
      console.error('Erro no logout:', error);
    } finally {
      setUser(null);
      setCurrentView('dashboard');
      setIsLoading(false);
    }
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    logout,
    currentView,
    setCurrentView,
    error,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}