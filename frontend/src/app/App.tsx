import { createBrowserRouter, Navigate, RouterProvider, useNavigate } from 'react-router';
import { AuthProvider, useAuth } from './hooks/useAuth';
import LoginScreen from './components/LoginScreen';
import MainLayout from './components/MainLayout';
import { ToastContainer } from './components/Toast';

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
      <RouterProvider router={router} />
    </AuthProvider>
  );
}
