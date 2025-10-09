import React from 'react';
import { Toaster } from './components/ui/sonner';
import { AuthProvider, useAuth } from './components/providers/AuthProvider';
import { Layout } from './components/layout/Layout';
import { LoginView } from './components/views/LoginView';
import { DashboardView } from './components/views/DashboardView';
import { UsuariosView } from './components/views/UsuariosView';
import { FuncionariosView } from './components/views/FuncionariosView';
import { CartoesView } from './components/views/CartoesView_novo';
import { LogsView } from './components/views/LogsView';
import { PerfilView } from './components/views/PerfilView';

function AppContent() {
  const { isAuthenticated, currentView, isLoading } = useAuth();

  // Mostrar loading durante a inicialização
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <LoginView />;
  }

  const renderCurrentView = () => {
    switch (currentView) {
      case 'dashboard':
        return <DashboardView />;
      case 'usuarios':
        return <UsuariosView />;
      case 'funcionarios':
        return <FuncionariosView />;
      case 'cartoes':
        return <CartoesView />;
      case 'logs':
        return <LogsView />;
      case 'perfil':
        return <PerfilView />;
      default:
        return <DashboardView />;
    }
  };

  return (
    <Layout>
      {renderCurrentView()}
    </Layout>
  );
}

export default function App() {
  return (
    <div className="size-full">
      <AuthProvider>
        <AppContent />
        <Toaster position="top-right" />
      </AuthProvider>
    </div>
  );
}