import { createBrowserRouter, Navigate, RouterProvider, useNavigate } from 'react-router';
import { AuthProvider, useAuth } from './hooks/useAuth';
import { useSignalR } from './hooks/useSignalR';
import LoginScreen from './components/LoginScreen';
import MainLayout from './components/MainLayout';
import { ToastContainer } from './components/Toast';

function SignalRStatusBadge() {
  const { isConnected } = useSignalR();
  return (
    <div className="fixed top-4 right-4 z-50">
      <div className={`flex items-center space-x-2 px-3 py-2 rounded-lg shadow-sm border ${
        isConnected ? 'bg-green-50 border-green-200 text-green-800' : 'bg-red-50 border-red-200 text-red-800'
      }`}>
        <div className={`w-2 h-2 rounded-full ${
          isConnected ? 'bg-green-500 animate-pulse' : 'bg-red-500'
        }`} />
        <span className="text-xs font-medium">
          {isConnected ? 'Hub Conectado' : 'Hub Desconectado'}
        </span>
      </div>
    </div>
  );
}

function AuthenticatedApp() {
  const auth = useAuth();
  if (auth.isLoading) return <p role="status" className="p-6 text-muted-foreground">Carregando...</p>;
  return auth.isAuthenticated ? <MainLayout /> : <Navigate to="/login" replace />;
}

function LoginRoute() {
  const auth = useAuth();
  const navigate = useNavigate();
  if (auth.isLoading) return <p role="status" className="p-6 text-muted-foreground">Carregando...</p>;
  return auth.isAuthenticated ? <Navigate to="/" replace /> : <LoginScreen onLogin={() => navigate('/', { replace: true })} />;
}

const router = createBrowserRouter([
  { path: '/', Component: AuthenticatedApp },
  { path: '/login', Component: LoginRoute },
  { path: '*', element: <Navigate to="/" replace /> }
]);

export default function App() {
  return (
    <AuthProvider>
      <ToastContainer />
      <SignalRStatusBadge />
      <RouterProvider router={router} />
    </AuthProvider>
  );
}
