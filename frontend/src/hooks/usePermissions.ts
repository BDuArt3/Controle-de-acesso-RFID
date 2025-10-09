import { useAuth } from '../components/providers/AuthProvider';

export type Permission = 
  | 'view_users' 
  | 'edit_users' 
  | 'view_employees' 
  | 'edit_employees' 
  | 'view_cards' 
  | 'edit_cards' 
  | 'view_logs';

export function usePermissions() {
  const { user } = useAuth();

  const hasPermission = (permission: Permission): boolean => {
    if (!user) return false;

    const role = user.role;

    switch (permission) {
      case 'view_users':
        return ['admin', 'operador', 'visualizador'].includes(role);
      case 'edit_users':
        return ['admin', 'operador'].includes(role);
      case 'view_employees':
        return ['admin'].includes(role);
      case 'edit_employees':
        return ['admin'].includes(role);
      case 'view_cards':
        return ['admin', 'operador', 'visualizador'].includes(role);
      case 'edit_cards':
        return ['admin', 'operador'].includes(role);
      case 'view_logs':
        return ['admin', 'operador', 'visualizador'].includes(role);
      default:
        return false;
    }
  };

  const canEdit = (entity: 'users' | 'employees' | 'cards'): boolean => {
    switch (entity) {
      case 'users':
        return hasPermission('edit_users');
      case 'employees':
        return hasPermission('edit_employees');
      case 'cards':
        return hasPermission('edit_cards');
      default:
        return false;
    }
  };

  const canView = (entity: 'users' | 'employees' | 'cards' | 'logs'): boolean => {
    switch (entity) {
      case 'users':
        return hasPermission('view_users');
      case 'employees':
        return hasPermission('view_employees');
      case 'cards':
        return hasPermission('view_cards');
      case 'logs':
        return hasPermission('view_logs');
      default:
        return false;
    }
  };

  return {
    hasPermission,
    canEdit,
    canView,
    user
  };
}