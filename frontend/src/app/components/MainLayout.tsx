import React, { useState } from 'react';
import { Navigate, useNavigate } from 'react-router';
import { useAuth } from '../hooks/useAuth';
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
      <header className="bg-white shadow-sm border-b h-16 flex items-center justify-between px-6">
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
        <div className="flex items-center space-x-4">
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
