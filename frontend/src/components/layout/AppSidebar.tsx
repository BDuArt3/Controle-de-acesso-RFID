import { 
  Home, 
  Users, 
  Building2, 
  CreditCard, 
  FileText, 
  User, 
  LogOut,
  Shield
} from "lucide-react"
import { Avatar, AvatarFallback, AvatarImage } from "../ui/avatar"
import { Button } from "../ui/button"
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarHeader,
  SidebarFooter,
} from "../ui/sidebar"
import { useAuth } from "../providers/AuthProvider"
import { usePermissions } from "../../hooks/usePermissions"

const menuItems = [
  {
    title: "Dashboard",
    url: "dashboard",
    icon: Home,
    permission: null // Todos podem acessar
  },
  {
    title: "Portadors",
    url: "portadors",
    icon: Users,
    permission: 'view_users' as const
  },
  {
    title: "Funcionários",
    url: "funcionarios",
    icon: Building2,
    permission: 'view_employees' as const
  },
  {
    title: "Cartões RFID",
    url: "cartoes",
    icon: CreditCard,
    permission: 'view_cards' as const
  },
  {
    title: "Logs de Acesso",
    url: "logs",
    icon: FileText,
    permission: 'view_logs' as const
  },
]

export function AppSidebar() {
  const { user, logout, currentView, setCurrentView } = useAuth();
  const { hasPermission } = usePermissions();

  const canAccessItem = (permission: string | null) => {
    if (!permission) return true; // Dashboard pode ser acessado por todos
    return hasPermission(permission as any);
  };

  const handleLogout = () => {
    logout();
  };

  return (
    <Sidebar className="border-r bg-card">
      <SidebarHeader className="border-b p-6">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-600 text-white">
            <Shield className="h-6 w-6" />
          </div>
          <div>
            <h1 className="text-lg font-semibold">RFID Access</h1>
            <p className="text-sm text-muted-foreground">Sistema de Controle</p>
          </div>
        </div>
      </SidebarHeader>
      
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Navegação</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {menuItems.map((item) => (
                canAccessItem(item.permission) && (
                  <SidebarMenuItem key={item.title}>
                    <SidebarMenuButton 
                      isActive={currentView === item.url}
                      onClick={() => setCurrentView(item.url)}
                      className="w-full justify-start"
                    >
                      <item.icon className="h-4 w-4" />
                      <span>{item.title}</span>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                )
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="border-t p-4">
        <div className="flex items-center gap-3 mb-3">
          <Avatar className="h-8 w-8">
            <AvatarImage src="" />
            <AvatarFallback className="bg-blue-600 text-white">
              {user?.name?.split(' ').map(n => n[0]).join('').toUpperCase() || 'U'}
            </AvatarFallback>
          </Avatar>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium truncate">{user?.name}</p>
            <p className="text-xs text-muted-foreground capitalize">{user?.role}</p>
          </div>
        </div>
        
        <div className="flex gap-2">
          <Button 
            variant="outline" 
            size="sm" 
            className="flex-1"
            onClick={() => setCurrentView('perfil')}
          >
            <User className="h-4 w-4 mr-2" />
            Perfil
          </Button>
          <Button 
            variant="outline" 
            size="sm"
            onClick={handleLogout}
          >
            <LogOut className="h-4 w-4" />
          </Button>
        </div>
      </SidebarFooter>
    </Sidebar>
  )
}