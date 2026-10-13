import React, { useState } from 'react';
import { Navigate, useNavigate } from 'react-router';
import { useAuth } from '../hooks/useAuth';
import { useSignalR } from '../hooks/useSignalR';
import { Button } from './ui/button';
import {
  LayoutDashboard,
  Users,
  FileText,
  LogOut,
  Menu,
  Building2,
  ChevronDown,
  Briefcase,
  CreditCard
} from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from './ui/dropdown-menu';
import Dashboard from './Dashboard';
import UserManagement from './UserManagement';
import AccessLogs from './AccessLogs';
import UserForm from './UserForm';
import FuncionarioList from './FuncionarioList';
import CartaoList from './CartaoList';

type Route = 'dashboard' | 'users' | 'logs' | 'user-form' | 'funcionarios' | 'cartoes';

interface NavigationState {
  route: Route;
  editingUserId?: string;
}

export default function MainLayout() {
  const { user: currentUser, logout, isAuthenticated } = useAuth();
  const { isConnected } = useSignalR();
  const navigate = useNavigate();
  const onLogout = () => { logout(); navigate('/login', { replace: true }); };
  const [navigation, setNavigation] = useState<NavigationState>({ route: 'dashboard' });
  const [sidebarOpen, setSidebarOpen] = useState(true);

  const handleNavigate = (route: Route, editingUserId?: string) => {
    setNavigation({ route, editingUserId });
  };

  const sidebarItems = [
    { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard, active: navigation.route === 'dashboard' },
    { id: 'users', label: 'Usuários', icon: Users, active: navigation.route === 'users' },
    { id: 'logs', label: 'Logs de Acesso', icon: FileText, active: navigation.route === 'logs' },
    { id: 'funcionarios', label: 'Funcionários', icon: Briefcase, active: navigation.route === 'funcionarios' },
    { id: 'cartoes', label: 'Cartões', icon: CreditCard, active: navigation.route === 'cartoes' },
  ];

  const renderContent = () => {
    switch (navigation.route) {
      case 'dashboard': return <Dashboard />;
      case 'users': return <UserManagement onNavigate={handleNavigate} />;
      case 'logs': return <AccessLogs />;
      case 'user-form': return <UserForm editingUserId={navigation.editingUserId} onNavigate={handleNavigate} />;
      case 'funcionarios': return <FuncionarioList />;
      case 'cartoes': return <CartaoList />;
      default: return <Dashboard />;
    }
  };

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow-sm border-b min-h-16 flex flex-wrap items-center justify-between gap-x-4 gap-y-2 px-6 py-2">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" size="sm" onClick={() => setSidebarOpen(!sidebarOpen)}>
            <Menu className="h-5 w-5" />
          </Button>
          <div className="flex items-center space-x-3">
            <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center">
              <Building2 className="w-5 h-5 text-primary-foreground" />
            </div>
            <h1>Sistema de Controle de Acesso</h1>
          </div>
        </div>
        <div className="flex w-full flex-wrap items-center justify-end gap-4 sm:w-auto">
          <span className="text-sm text-muted-foreground">
            Olá, {currentUser?.name}
          </span>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="space-x-2">
                <span>{currentUser?.email}</span>
                <ChevronDown className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={onLogout}>
                <LogOut className="h-4 w-4 mr-2" />
                Sair
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          <div className={`flex shrink-0 items-center space-x-2 rounded-lg border px-3 py-2 shadow-sm ${
            isConnected ? 'bg-green-50 border-green-200 text-green-800' : 'bg-red-50 border-red-200 text-red-800'
          }`}>
            <div className={`h-2 w-2 rounded-full ${
              isConnected ? 'bg-green-500 animate-pulse' : 'bg-red-500'
            }`} />
            <span className="text-xs font-medium">
              {isConnected ? 'Hub Conectado' : 'Hub Desconectado'}
            </span>
          </div>
        </div>
      </header>

      <div className="flex">
        {sidebarOpen && (
          <aside className="w-64 bg-white shadow-sm min-h-[calc(100vh-4rem)] border-r">
            <nav className="p-4 space-y-2">
              {sidebarItems.map((item) => {
                const Icon = item.icon;
                return (
                  <Button
                    key={item.id}
                    variant={item.active ? "secondary" : "ghost"}
                    className="w-full justify-start space-x-3"
                    onClick={() => handleNavigate(item.id as Route)}
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
