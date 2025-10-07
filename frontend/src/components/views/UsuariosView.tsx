import React, { useState, useEffect } from 'react';
import { Plus, Search, Edit, Trash2, CreditCard, Filter, MoreVertical } from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Avatar, AvatarFallback } from '../ui/avatar';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '../ui/dropdown-menu';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../ui/dialog';
import { Label } from '../ui/label';
import { Switch } from '../ui/switch';
import { toast } from 'sonner@2.0.3';
import { useAuth } from '../providers/AuthProvider';

interface User {
  id: string;
  name: string;
  type: 'aluno' | 'professor' | 'funcionario' | 'visitante';
  status: 'ativo' | 'inativo';
  cardId?: string;
  email: string;
  registeredAt: string;
}

export function UsuariosView() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<User[]>([]);
  const [filteredUsers, setFilteredUsers] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<string>('todos');
  const [filterStatus, setFilterStatus] = useState<string>('todos');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    type: 'aluno' as User['type'],
    status: 'ativo' as User['status']
  });

  const canEdit = currentUser?.role === 'admin' || currentUser?.role === 'operador';

  useEffect(() => {
    loadUsers();
  }, []);

  useEffect(() => {
    filterUsers();
  }, [users, searchTerm, filterType, filterStatus]);

  const loadUsers = async () => {
    setIsLoading(true);
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    const mockUsers: User[] = [
      {
        id: '1',
        name: 'João Silva',
        type: 'funcionario',
        status: 'ativo',
        cardId: 'CARD001',
        email: 'joao.silva@empresa.com',
        registeredAt: '2024-01-15'
      },
      {
        id: '2',
        name: 'Maria Santos',
        type: 'professor',
        status: 'ativo',
        cardId: 'CARD002',
        email: 'maria.santos@escola.edu',
        registeredAt: '2024-02-10'
      },
      {
        id: '3',
        name: 'Ana Costa',
        type: 'aluno',
        status: 'ativo',
        email: 'ana.costa@escola.edu',
        registeredAt: '2024-03-05'
      },
      {
        id: '4',
        name: 'Pedro Oliveira',
        type: 'visitante',
        status: 'inativo',
        email: 'pedro.oliveira@gmail.com',
        registeredAt: '2024-03-20'
      },
      {
        id: '5',
        name: 'Luisa Ferreira',
        type: 'aluno',
        status: 'ativo',
        cardId: 'CARD005',
        email: 'luisa.ferreira@escola.edu',
        registeredAt: '2024-03-22'
      }
    ];
    
    setUsers(mockUsers);
    setIsLoading(false);
  };

  const filterUsers = () => {
    let filtered = users;

    if (searchTerm) {
      filtered = filtered.filter(user =>
        user.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        user.email.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (filterType !== 'todos') {
      filtered = filtered.filter(user => user.type === filterType);
    }

    if (filterStatus !== 'todos') {
      filtered = filtered.filter(user => user.status === filterStatus);
    }

    setFilteredUsers(filtered);
  };

  const handleCreateUser = () => {
    setEditingUser(null);
    setFormData({
      name: '',
      email: '',
      type: 'aluno',
      status: 'ativo'
    });
    setIsModalOpen(true);
  };

  const handleEditUser = (user: User) => {
    setEditingUser(user);
    setFormData({
      name: user.name,
      email: user.email,
      type: user.type,
      status: user.status
    });
    setIsModalOpen(true);
  };

  const handleSaveUser = async () => {
    if (!formData.name || !formData.email) {
      toast.error('Nome e email são obrigatórios');
      return;
    }

    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 500));

    if (editingUser) {
      // Update existing user
      setUsers(prev => prev.map(user => 
        user.id === editingUser.id 
          ? { ...user, ...formData }
          : user
      ));
      toast.success('Usuário atualizado com sucesso');
    } else {
      // Create new user
      const newUser: User = {
        id: Date.now().toString(),
        ...formData,
        registeredAt: new Date().toISOString().split('T')[0]
      };
      setUsers(prev => [...prev, newUser]);
      toast.success('Usuário criado com sucesso');
    }

    setIsModalOpen(false);
  };

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('Tem certeza que deseja excluir este usuário?')) return;

    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 500));

    setUsers(prev => prev.filter(user => user.id !== userId));
    toast.success('Usuário removido com sucesso');
  };

  const handleToggleStatus = async (userId: string) => {
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 300));

    setUsers(prev => prev.map(user => 
      user.id === userId 
        ? { ...user, status: user.status === 'ativo' ? 'inativo' : 'ativo' }
        : user
    ));
    toast.success('Status do usuário atualizado');
  };

  const getUserTypeLabel = (type: string) => {
    switch (type) {
      case 'aluno': return 'Aluno';
      case 'professor': return 'Professor';
      case 'funcionario': return 'Funcionário';
      case 'visitante': return 'Visitante';
      default: return type;
    }
  };

  const getUserTypeColor = (type: string) => {
    switch (type) {
      case 'aluno': return 'bg-blue-100 text-blue-800';
      case 'professor': return 'bg-green-100 text-green-800';
      case 'funcionario': return 'bg-purple-100 text-purple-800';
      case 'visitante': return 'bg-orange-100 text-orange-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-muted animate-pulse rounded w-48"></div>
        <Card>
          <CardContent className="p-6">
            <div className="space-y-4">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="h-16 bg-muted animate-pulse rounded"></div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-start">
        <div>
          <h1>Gestão de Usuários</h1>
          <p className="text-muted-foreground">
            Gerencie alunos, professores, funcionários e visitantes
          </p>
        </div>
        {canEdit && (
          <Button onClick={handleCreateUser} className="bg-blue-600 hover:bg-blue-700">
            <Plus className="h-4 w-4 mr-2" />
            Novo Usuário
          </Button>
        )}
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col md:flex-row gap-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Buscar por nome ou email..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
            </div>
            <div className="flex gap-2">
              <Select value={filterType} onValueChange={setFilterType}>
                <SelectTrigger className="w-36">
                  <SelectValue placeholder="Tipo" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos os tipos</SelectItem>
                  <SelectItem value="aluno">Aluno</SelectItem>
                  <SelectItem value="professor">Professor</SelectItem>
                  <SelectItem value="funcionario">Funcionário</SelectItem>
                  <SelectItem value="visitante">Visitante</SelectItem>
                </SelectContent>
              </Select>
              <Select value={filterStatus} onValueChange={setFilterStatus}>
                <SelectTrigger className="w-32">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos</SelectItem>
                  <SelectItem value="ativo">Ativo</SelectItem>
                  <SelectItem value="inativo">Inativo</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Usuário</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Cartão</TableHead>
                <TableHead>Cadastrado em</TableHead>
                <TableHead className="text-right">Ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredUsers.map((user) => (
                <TableRow key={user.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <Avatar className="h-8 w-8">
                        <AvatarFallback className="bg-blue-100 text-blue-800 text-xs">
                          {user.name.split(' ').map(n => n[0]).join('')}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <p className="font-medium">{user.name}</p>
                        <p className="text-sm text-muted-foreground">{user.email}</p>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={getUserTypeColor(user.type)}>
                      {getUserTypeLabel(user.type)}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      {canEdit ? (
                        <Switch
                          checked={user.status === 'ativo'}
                          onCheckedChange={() => handleToggleStatus(user.id)}
                        />
                      ) : (
                        <Badge variant={user.status === 'ativo' ? 'default' : 'secondary'}>
                          {user.status === 'ativo' ? 'Ativo' : 'Inativo'}
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    {user.cardId ? (
                      <Badge variant="outline" className="bg-green-100 text-green-800">
                        <CreditCard className="h-3 w-3 mr-1" />
                        {user.cardId}
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="bg-gray-100 text-gray-600">
                        Sem cartão
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    {new Date(user.registeredAt).toLocaleDateString('pt-BR')}
                  </TableCell>
                  <TableCell className="text-right">
                    {canEdit && (
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="sm">
                            <MoreVertical className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={() => handleEditUser(user)}>
                            <Edit className="h-4 w-4 mr-2" />
                            Editar
                          </DropdownMenuItem>
                          <DropdownMenuItem 
                            onClick={() => handleDeleteUser(user.id)}
                            className="text-red-600"
                          >
                            <Trash2 className="h-4 w-4 mr-2" />
                            Excluir
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {filteredUsers.length === 0 && (
            <div className="text-center py-8">
              <p className="text-muted-foreground">Nenhum usuário encontrado</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Modal de Criação/Edição */}
      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingUser ? 'Editar Usuário' : 'Novo Usuário'}
            </DialogTitle>
            <DialogDescription>
              {editingUser ? 'Altere as informações do usuário.' : 'Preencha os dados para criar um novo usuário.'}
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-4">
            <div>
              <Label htmlFor="name">Nome completo</Label>
              <Input
                id="name"
                value={formData.name}
                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                placeholder="Digite o nome completo"
              />
            </div>
            
            <div>
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
                placeholder="Digite o email"
              />
            </div>
            
            <div>
              <Label htmlFor="type">Tipo de usuário</Label>
              <Select 
                value={formData.type} 
                onValueChange={(value: User['type']) => setFormData(prev => ({ ...prev, type: value }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="aluno">Aluno</SelectItem>
                  <SelectItem value="professor">Professor</SelectItem>
                  <SelectItem value="funcionario">Funcionário</SelectItem>
                  <SelectItem value="visitante">Visitante</SelectItem>
                </SelectContent>
              </Select>
            </div>
            
            <div className="flex items-center space-x-2">
              <Switch
                id="status"
                checked={formData.status === 'ativo'}
                onCheckedChange={(checked) => setFormData(prev => ({ 
                  ...prev, 
                  status: checked ? 'ativo' : 'inativo' 
                }))}
              />
              <Label htmlFor="status">Usuário ativo</Label>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsModalOpen(false)}>
              Cancelar
            </Button>
            <Button onClick={handleSaveUser} className="bg-blue-600 hover:bg-blue-700">
              {editingUser ? 'Salvar' : 'Criar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}