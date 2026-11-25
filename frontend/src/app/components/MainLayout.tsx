import React, { useMemo, useState } from 'react';
import { Navigate, useNavigate } from 'react-router';
import { Briefcase, Building2, ChevronDown, CreditCard, FileText, LayoutDashboard, LogOut, Menu, Shield, Users } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { type RoleName } from '../services/api';
import { Button } from './ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from './ui/dropdown-menu';
import Dashboard from './Dashboard';
import UserManagement from './UserManagement';
import AccessLogs from './AccessLogs';
import UserForm from './UserForm';
import FuncionarioList from './FuncionarioList';
import CartaoList from './CartaoList';
import { showToast } from './Toast';

type Route = 'dashboard' | 'users' | 'logs' | 'user-form' | 'funcionarios' | 'cartoes';

interface NavigationState {
  route: Route;
  editingUserId?: string;
}

interface SidebarItem {
  id: Route;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  roles: RoleName[];
}

export default function MainLayout() {
  const { user: currentUser, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [navigation, setNavigation] = useState<NavigationState>({ route: 'dashboard' });
  const [sidebarOpen, setSidebarOpen] = useState(true);

  const displayName = currentUser?.name?.trim() || currentUser?.email?.split('@')[0] || '';

  const sidebarItems = useMemo<SidebarItem[]>(() => [
    { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard, roles: ['Visualizador', 'Operador', 'Admin'] },
    { id: 'users', label: 'Usuários', icon: Users, roles: ['Visualizador', 'Operador', 'Admin'] },
    { id: 'logs', label: 'Logs de Acesso', icon: FileText, roles: ['Visualizador', 'Operador', 'Admin'] },
    { id: 'cartoes', label: 'Cartões', icon: CreditCard, roles: ['Visualizador', 'Operador', 'Admin'] },
    { id: 'funcionarios', label: 'Funcionários', icon: Briefcase, roles: ['Admin'] },
  ], []);

  const visibleItems = sidebarItems.filter(item => item.roles.includes(currentUser?.role ?? 'Visualizador'));

  const handleNavigate = (route: Route, editingUserId?: string) => {
    const role = currentUser?.role ?? 'Visualizador';
    if (route === 'funcionarios' && role !== 'Admin') {
      showToast('Acesso restrito para administradores.', 'error');
      setNavigation({ route: 'dashboard' });
      return;
    }
    if (route === 'user-form') {
      if (editingUserId && role !== 'Admin') {
        showToast('Apenas administradores podem editar usuários.', 'error');
        setNavigation({ route: 'users' });
        return;
      }
      if (!editingUserId && role !== 'Admin' && role !== 'Operador') {
        showToast('Apenas administradores e operadores podem cadastrar usuários.', 'error');
        setNavigation({ route: 'users' });
        return;
      }
    }
    setNavigation({ route, editingUserId });
  };

  const onLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  const renderContent = () => {
    const role = currentUser?.role ?? 'Visualizador';
    switch (navigation.route) {
      case 'dashboard':
        return <Dashboard />;
      case 'users':
        return <UserManagement onNavigate={handleNavigate} />;
      case 'logs':
        return <AccessLogs />;
      case 'user-form':
        if (navigation.editingUserId && role !== 'Admin') {
          return <UserManagement onNavigate={handleNavigate} />;
        }
        if (!navigation.editingUserId && role !== 'Admin' && role !== 'Operador') {
          return <UserManagement onNavigate={handleNavigate} />;
        }
        return <UserForm editingUserId={navigation.editingUserId} onNavigate={handleNavigate} />;
      case 'funcionarios':
        if (role !== 'Admin') {
          return <Dashboard />;
        }
        return <FuncionarioList />;
      case 'cartoes':
        return <CartaoList />;
      default:
        return <Dashboard />;
    }
  };

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="flex min-h-16 flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b bg-white px-6 py-2 shadow-sm">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" size="sm" onClick={() => setSidebarOpen(value => !value)}>
            <Menu className="h-5 w-5" />
          </Button>
          <div className="flex items-center space-x-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary">
              <Building2 className="h-5 w-5 text-primary-foreground" />
            </div>
            <h1>Sistema de Controle de Acesso</h1>
          </div>
        </div>
        <div className="flex w-full flex-wrap items-center justify-end gap-4 sm:w-auto">
          <span className="text-sm text-muted-foreground">
            {displayName ? `Olá, ${displayName}` : 'Olá'}
          </span>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="space-x-2" aria-label="Abrir menu da conta">
                <span>{currentUser?.email || 'Minha conta'}</span>
                <ChevronDown className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>
                <div className="flex flex-col">
                  <span>{displayName || 'Minha conta'}</span>
                  {currentUser?.email && (
                    <span className="text-xs font-normal text-muted-foreground">{currentUser.email}</span>
                  )}
                </div>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => void onLogout()}>
                <LogOut className="mr-2 h-4 w-4" />
                Sair
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>

      <div className="flex">
        {sidebarOpen && (
          <aside className="min-h-[calc(100vh-4rem)] w-64 border-r bg-white shadow-sm">
            <nav className="space-y-2 p-4">
              {visibleItems.map((item) => {
                const Icon = item.icon;
                const active = navigation.route === item.id;
                return (
                  <Button
                    key={item.id}
                    variant={active ? 'secondary' : 'ghost'}
                    className="w-full justify-start space-x-3"
                    onClick={() => handleNavigate(item.id)}
                  >
                    <Icon className="h-5 w-5" />
                    <span>{item.label}</span>
                  </Button>
                );
              })}
            </nav>
          </aside>
        )}
        <main className="flex-1 p-6">{renderContent()}</main>
      </div>
    </div>
  );
}
