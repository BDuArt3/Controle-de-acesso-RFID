import React, { useState, useEffect } from 'react';
import { usuarioService, isActive, errorMessage } from '../services/api';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Input } from './ui/input';
import { Button } from './ui/button';
import { Badge } from './ui/badge';
import { Plus, Search, Edit, Lock, Unlock, MoreHorizontal } from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from './ui/dropdown-menu';

interface User {
  id: string;
  name: string;
  document: string;
  userType: string;
  status: 'active' | 'blocked';
  uid?: string;
}

interface UserManagementProps {
  onNavigate: (route: any, editingUserId?: string) => void;
}

export default function UserManagement({ onNavigate }: UserManagementProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [users, setUsers] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [pendingId, setPendingId] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const { data } = await usuarioService.list();
        if (active) setUsers(data.map(user => ({
          id: String(user.id),
          name: user.nome,
          document: user.documento,
          userType: user.tipo,
          uid: user.uid,
          status: isActive(user.status) ? 'active' : 'blocked'
        })));
      } catch (error) { if (active) setError(errorMessage(error)); }
      finally { if (active) setIsLoading(false); }
    };
    void load();
    return () => { active = false; };
  }, []);

  const toggleUserStatus = async (userId: string) => {
    const user = users.find(item => item.id === userId);
    if (!user || pendingId) return;
    setPendingId(userId); setError('');
    try {
      if (user.status === 'active') await usuarioService.bloquear(userId);
      else await usuarioService.desbloquear(userId);
      setUsers(previous => previous.map(item =>
        item.id === userId ? { ...item, status: user.status === 'active' ? 'blocked' : 'active' } : item
      ));
    } catch (error) { setError(errorMessage(error)); }
    finally { setPendingId(null); }
  };

  const filteredUsers = users.filter(user =>
    user.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    user.document.includes(searchQuery)
  );

  return (
    <div className="space-y-6">
      {isLoading && <p role="status" className="text-muted-foreground">Carregando...</p>}
      {error && <p role="alert" className="text-destructive">{error}</p>}

      <div className="flex justify-between items-center">
        <div>
          <h1>Gerenciamento de Usuários</h1>
          <p className="text-muted-foreground">Gerencie usuários e seus acessos ao sistema</p>
        </div>
        <Button onClick={() => onNavigate('user-form')}>
          <Plus className="h-4 w-4 mr-2" />Adicionar Novo Usuário
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Lista de Usuários</CardTitle>
          <div className="flex items-center space-x-2">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
              <Input
                placeholder="Buscar por nome ou documento..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {filteredUsers.map((user) => (
              <div key={user.id} className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/20 transition-colors">
                <div className="flex items-center space-x-4">
                  <div className="w-12 h-12 bg-primary/10 rounded-full flex items-center justify-center">
                    <span className="font-medium text-primary">
                      {user.name.split(' ').map(n => n[0]).join('').slice(0, 2)}
                    </span>
                  </div>
                  <div>
                    <h3 className="font-medium">{user.name}</h3>
                    <p className="text-sm text-muted-foreground">{user.document}</p>
                    <div className="flex items-center space-x-2 mt-1">
                      <Badge variant="outline">{user.userType}</Badge>
                      {user.uid && <span className="text-xs text-muted-foreground">UID: {user.uid}</span>}
                    </div>
                  </div>
                </div>

                <div className="flex items-center space-x-2">
                  <Badge
                    variant={user.status === 'active' ? 'default' : 'destructive'}
                    className={user.status === 'active' ? 'bg-green-100 text-green-800' : ''}
                  >
                    {user.status === 'active' ? 'Ativo' : 'Bloqueado'}
                  </Badge>

                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="sm">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem onClick={() => onNavigate('user-form', user.id)}>
                        <Edit className="h-4 w-4 mr-2" />Editar
                      </DropdownMenuItem>
                      <DropdownMenuItem disabled={pendingId !== null} onClick={() => void toggleUserStatus(user.id)}>
                        {user.status === 'active' ? (
                          <><Lock className="h-4 w-4 mr-2" />Bloquear</>
                        ) : (
                          <><Unlock className="h-4 w-4 mr-2" />Desbloquear</>
                        )}
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>
            ))}

            {filteredUsers.length === 0 && (
              <div className="text-center py-8">
                <p className="text-muted-foreground">Nenhum usuário encontrado.</p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
