import React, { createContext, useContext, useState, useEffect } from 'react';

interface User {
  id: string;
  name: string;
  username: string;
  role: 'admin' | 'operador' | 'visualizador';
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  currentView: string;
  setCurrentView: (view: string) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Mock users for demonstration
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

  useEffect(() => {
    // Check for saved user in localStorage
    const savedUser = localStorage.getItem('rfid_user');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
    }
  }, []);

  const login = async (username: string, password: string): Promise<boolean> => {
    // Simulate API call delay
    await new Promise(resolve => setTimeout(resolve, 1000));

    if (mockUsers[username] && mockUsers[username].password === password) {
      const userData = mockUsers[username].user;
      setUser(userData);
      localStorage.setItem('rfid_user', JSON.stringify(userData));
      setCurrentView('dashboard');
      return true;
    }
    return false;
  };

  const logout = () => {
    setUser(null);
    localStorage.removeItem('rfid_user');
    setCurrentView('dashboard');
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    login,
    logout,
    currentView,
    setCurrentView,
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