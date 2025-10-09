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
      
      if (response.success && response.usuario) {
        // Função para mapear o tipo de funcionário
        const mapTipoFuncionario = (tipo: number): 'admin' | 'operador' | 'visualizador' => {
          switch (tipo) {
            case 2: return 'admin';
            case 1: return 'operador';
            case 0: return 'visualizador';
            default: return 'visualizador';
          }
        };

        const userData: User = {
          id: response.usuario.id,
          name: response.usuario.nomeFuncionario,
          username: response.usuario.usuarioAcesso,
          role: mapTipoFuncionario(response.usuario.tipo)
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
      
      // Definir mensagem de erro baseada no tipo de erro
      if (error.statusCode === 0) {
        setError('Não foi possível conectar ao servidor. Verifique sua conexão.');
      } else if (error.statusCode === 401) {
        setError('Usuário ou senha incorretos.');
      } else {
        setError(error.message || 'Erro ao tentar fazer login. Tente novamente.');
      }
      
      return false;
      
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