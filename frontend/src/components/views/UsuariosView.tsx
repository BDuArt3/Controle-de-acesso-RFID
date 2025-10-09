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
import { toast } from 'sonner';
import { useAuth } from '../providers/AuthProvider';
import { usePermissions } from '../../hooks/usePermissions';
import { AccessDeniedView } from '../ui/AccessDeniedView';
import { services, TipoUsuario, type UsuarioDto, type CreateUsuarioDto } from '../../services';

interface User {
  id: string;
  nome: string;
  tipo: TipoUsuario;
  isActive: boolean;
  cartoes?: Array<{ id: string; uid: string; isActive: boolean }>;
}

export function UsuariosView() {
  const { user: currentUser } = useAuth();
  const { canView, canEdit } = usePermissions();
  const [users, setUsers] = useState<User[]>([]);
  const [filteredUsers, setFilteredUsers] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<string>('todos');
  const [filterStatus, setFilterStatus] = useState<string>('todos');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [formData, setFormData] = useState({
    nome: '',
    tipo: TipoUsuario.Aluno
  });

  // Verificação de permissão para acessar esta view
  if (!canView('users')) {
    return <AccessDeniedView />;
  }

  const canEditUsers = canEdit('users');

  // Função auxiliar para converter string para enum
  const getTipoUsuarioFromString = (tipo: string): TipoUsuario => {
    switch (tipo) {
      case 'aluno': return TipoUsuario.Aluno;
      case 'professor': return TipoUsuario.Professor;
      case 'funcionario': return TipoUsuario.Funcionario;
      case 'visitante': return TipoUsuario.Visitante;
      default: return TipoUsuario.Aluno;
    }
  };

  // Função auxiliar para converter enum para string
  const getTipoUsuarioString = (tipo: TipoUsuario): string => {
    switch (tipo) {
      case TipoUsuario.Aluno: return 'aluno';
      case TipoUsuario.Professor: return 'professor';
      case TipoUsuario.Funcionario: return 'funcionario';
      case TipoUsuario.Visitante: return 'visitante';
      default: return 'aluno';
    }
  };

  useEffect(() => {
    loadUsers();
  }, []);

  useEffect(() => {
    filterUsers();
  }, [users, searchTerm, filterType, filterStatus]);

  const loadUsers = async () => {
    setIsLoading(true);
    try {
      console.log('Carregando usuários da API...');
      const usuariosData = await services.usuario.getAll();
      
      // Converter UsuarioDto para User interface
      const usersConverted: User[] = usuariosData.map(usuario => ({
        id: usuario.id,
        nome: usuario.nome,
        tipo: usuario.tipo,
        isActive: usuario.isActive,
        cartoes: usuario.cartoes
      }));
      
      setUsers(usersConverted);
      console.log('Usuários carregados:', usersConverted);
    } catch (error) {
      console.error('Erro ao carregar usuários:', error);
      toast.error('Erro ao carregar usuários');
    } finally {
      setIsLoading(false);
    }
  };

  const filterUsers = () => {
    let filtered = users;

    if (searchTerm) {
      filtered = filtered.filter(user =>
        user.nome.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (filterType !== 'todos') {
      const tipoFiltro = getTipoUsuarioFromString(filterType);
      filtered = filtered.filter(user => user.tipo === tipoFiltro);
    }

    if (filterStatus !== 'todos') {
      const isActive = filterStatus === 'ativo';
      filtered = filtered.filter(user => user.isActive === isActive);
    }

    setFilteredUsers(filtered);
  };

  const handleCreateUser = () => {
    setEditingUser(null);
    setFormData({
      nome: '',
      tipo: TipoUsuario.Aluno
    });
    setIsModalOpen(true);
  };

  const handleEditUser = (user: User) => {
    setEditingUser(user);
    setFormData({
      nome: user.nome,
      tipo: user.tipo
    });
    setIsModalOpen(true);
  };

  const handleSaveUser = async () => {
    if (!formData.nome) {
      toast.error('Nome é obrigatório');
      return;
    }

    try {
      if (editingUser) {
        // Atualizar usuário existente
        const updateDto: CreateUsuarioDto = {
          nome: formData.nome,
          tipo: formData.tipo
        };
        
        const usuarioAtualizado = await services.usuario.update(editingUser.id, updateDto);
        
        setUsers(prev => prev.map(user => 
          user.id === editingUser.id 
            ? { ...user, nome: usuarioAtualizado.nome, tipo: usuarioAtualizado.tipo }
            : user
        ));
        
        toast.success('Usuário atualizado com sucesso');
      } else {
        // Criar novo usuário
        const createDto: CreateUsuarioDto = {
          nome: formData.nome,
          tipo: formData.tipo
        };
        
        const novoUsuario = await services.usuario.create(createDto);
        
        const newUser: User = {
          id: novoUsuario.id,
          nome: novoUsuario.nome,
          tipo: novoUsuario.tipo,
          isActive: novoUsuario.isActive,
          cartoes: novoUsuario.cartoes
        };
        
        setUsers(prev => [...prev, newUser]);
        toast.success('Usuário criado com sucesso');
      }

      setIsModalOpen(false);
    } catch (error: any) {
      console.error('Erro ao salvar usuário:', error);
      
      // Tratamento de erros específicos
      if (error.statusCode === 400) {
        toast.error('Dados inválidos: ' + (error.message || 'Verifique os campos'));
      } else if (error.statusCode === 401) {
        toast.error('Você não tem permissão para esta operação');
      } else {
        toast.error('Erro ao salvar usuário');
      }
    }
  };

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('Tem certeza que deseja desativar este usuário?')) return;

    try {
      await services.usuario.deleteUsuario(userId);
      setUsers(prev => prev.map(user => 
        user.id === userId 
          ? { ...user, isActive: false }
          : user
      ));
      toast.success('Usuário desativado com sucesso');
    } catch (error) {
      console.error('Erro ao desativar usuário:', error);
      toast.error('Erro ao desativar usuário');
    }
  };

  const handleToggleStatus = async (userId: string, currentActive: boolean) => {
    try {
      if (currentActive) {
        // Desativar
        await services.usuario.deleteUsuario(userId);
      } else {
        // Ativar
        await services.usuario.activate(userId);
      }
      
      setUsers(prev => prev.map(user => 
        user.id === userId 
          ? { ...user, isActive: !currentActive }
          : user
      ));
      
      toast.success(currentActive ? 'Usuário desativado' : 'Usuário ativado');
    } catch (error) {
      console.error('Erro ao alterar status:', error);
      toast.error('Erro ao alterar status do usuário');
    }
  };

  const getUserTypeLabel = (tipo: TipoUsuario) => {
    switch (tipo) {
      case TipoUsuario.Aluno: return 'Aluno';
      case TipoUsuario.Professor: return 'Professor';
      case TipoUsuario.Funcionario: return 'Funcionário';
      case TipoUsuario.Visitante: return 'Visitante';
      default: return 'Aluno';
    }
  };

  const getUserTypeColor = (tipo: TipoUsuario) => {
    switch (tipo) {
      case TipoUsuario.Aluno: return 'bg-blue-100 text-blue-800';
      case TipoUsuario.Professor: return 'bg-green-100 text-green-800';
      case TipoUsuario.Funcionario: return 'bg-purple-100 text-purple-800';
      case TipoUsuario.Visitante: return 'bg-orange-100 text-orange-800';
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
            {canEditUsers ? 'Gerencie alunos, professores, funcionários e visitantes' : 'Visualize alunos, professores, funcionários e visitantes'}
          </p>
        </div>
        {canEditUsers && (
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
                {canEditUsers && <TableHead className="text-right">Ações</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredUsers.map((user) => (
                <TableRow key={user.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <Avatar className="h-8 w-8">
                        <AvatarFallback className="bg-blue-100 text-blue-800 text-xs">
                          {user.nome.split(' ').map((n: string) => n[0]).join('')}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <p className="font-medium">{user.nome}</p>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={getUserTypeColor(user.tipo)}>
                      {getUserTypeLabel(user.tipo)}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      {canEditUsers ? (
                        <Switch
                          checked={user.isActive}
                          onCheckedChange={() => handleToggleStatus(user.id, user.isActive)}
                        />
                      ) : (
                        <Badge variant={user.isActive ? 'default' : 'secondary'}>
                          {user.isActive ? 'Ativo' : 'Inativo'}
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    {user.cartoes && user.cartoes.length > 0 ? (
                      <Badge variant="outline" className="bg-green-100 text-green-800">
                        <CreditCard className="h-3 w-3 mr-1" />
                        {user.cartoes[0].uid}
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="bg-gray-100 text-gray-600">
                        Sem cartão
                      </Badge>
                    )}
                  </TableCell>
                  {canEditUsers && (
                    <TableCell className="text-right">
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
                    </TableCell>
                  )}
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
      {canEditUsers && (
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
                <Label htmlFor="nome">Nome completo</Label>
                <Input
                  id="nome"
                  value={formData.nome}
                  onChange={(e) => setFormData(prev => ({ ...prev, nome: e.target.value }))}
                  placeholder="Digite o nome completo"
                />
              </div>
              
              <div>
                <Label htmlFor="tipo">Tipo de usuário</Label>
                <Select 
                  value={formData.tipo.toString()} 
                  onValueChange={(value: string) => setFormData(prev => ({ ...prev, tipo: parseInt(value) as TipoUsuario }))}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={TipoUsuario.Aluno.toString()}>Aluno</SelectItem>
                    <SelectItem value={TipoUsuario.Professor.toString()}>Professor</SelectItem>
                    <SelectItem value={TipoUsuario.Funcionario.toString()}>Funcionário</SelectItem>
                    <SelectItem value={TipoUsuario.Visitante.toString()}>Visitante</SelectItem>
                  </SelectContent>
                </Select>
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
      )}
    </div>
  );
}