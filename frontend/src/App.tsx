import React from 'react';
import { Toaster } from './components/ui/sonner';
import { AuthProvider, useAuth } from './components/providers/AuthProvider';
import { Layout } from './components/layout/Layout';
import { LoginView } from './components/views/LoginView';
import { DashboardView } from './components/views/DashboardView';
import { UsuariosView } from './components/views/UsuariosView';
import { FuncionariosView } from './components/views/FuncionariosView';
import { CartoesView } from './components/views/CartoesView';
import { LogsView } from './components/views/LogsView';
import { PerfilView } from './components/views/PerfilView';

function AppContent() {
  const { isAuthenticated, currentView } = useAuth();

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