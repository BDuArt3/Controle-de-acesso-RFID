import React, { useState, useEffect } from 'react';
import { Plus, Search, Edit, Trash2, Shield, Eye, EyeOff, MoreVertical } from 'lucide-react';
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
import { Alert, AlertDescription } from '../ui/alert';
import { toast } from 'sonner';
import { useAuth } from '../providers/AuthProvider';

interface Employee {
  id: string;
  name: string;
  username: string;
  role: 'admin' | 'operador' | 'visualizador';
  status: 'ativo' | 'inativo';
  email: string;
  lastAccess?: string;
  createdAt: string;
}

export function FuncionariosView() {
  const { user: currentUser } = useAuth();
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [filteredEmployees, setFilteredEmployees] = useState<Employee[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterRole, setFilterRole] = useState<string>('todos');
  const [filterStatus, setFilterStatus] = useState<string>('todos');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    username: '',
    email: '',
    password: '',
    role: 'visualizador' as Employee['role'],
    status: 'ativo' as Employee['status']
  });

  // Only admins can access this view
  if (currentUser?.role !== 'admin') {
    return (
      <div className="space-y-6">
        <Card>
          <CardContent className="p-8 text-center">
            <Shield className="h-16 w-16 mx-auto text-muted-foreground mb-4" />
            <h2>Acesso Restrito</h2>
            <p className="text-muted-foreground">
              Apenas administradores podem acessar a gestão de funcionários.
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  useEffect(() => {
    loadEmployees();
  }, []);

  useEffect(() => {
    filterEmployees();
  }, [employees, searchTerm, filterRole, filterStatus]);

  const loadEmployees = async () => {
    setIsLoading(true);
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    const mockEmployees: Employee[] = [
      {
        id: '1',
        name: 'João Administrador',
        username: 'admin',
        role: 'admin',
        status: 'ativo',
        email: 'admin@empresa.com',
        lastAccess: '2024-03-25T14:30:00Z',
        createdAt: '2024-01-01'
      },
      {
        id: '2',
        name: 'Maria Operadora',
        username: 'operador',
        role: 'operador',
        status: 'ativo',
        email: 'maria.operadora@empresa.com',
        lastAccess: '2024-03-25T13:45:00Z',
        createdAt: '2024-01-15'
      },
      {
        id: '3',
        name: 'Carlos Visualizador',
        username: 'visualizador',
        role: 'visualizador',
        status: 'ativo',
        email: 'carlos.visualizador@empresa.com',
        lastAccess: '2024-03-24T16:20:00Z',
        createdAt: '2024-02-10'
      },
      {
        id: '4',
        name: 'Ana Silva',
        username: 'ana.silva',
        role: 'operador',
        status: 'inativo',
        email: 'ana.silva@empresa.com',
        createdAt: '2024-03-01'
      }
    ];
    
    setEmployees(mockEmployees);
    setIsLoading(false);
  };

  const filterEmployees = () => {
    let filtered = employees;

    if (searchTerm) {
      filtered = filtered.filter(employee =>
        employee.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        employee.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
        employee.email.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (filterRole !== 'todos') {
      filtered = filtered.filter(employee => employee.role === filterRole);
    }

    if (filterStatus !== 'todos') {
      filtered = filtered.filter(employee => employee.status === filterStatus);
    }

    setFilteredEmployees(filtered);
  };

  const handleCreateEmployee = () => {
    setEditingEmployee(null);
    setFormData({
      name: '',
      username: '',
      email: '',
      password: '',
      role: 'visualizador',
      status: 'ativo'
    });
    setIsModalOpen(true);
  };

  const handleEditEmployee = (employee: Employee) => {
    setEditingEmployee(employee);
    setFormData({
      name: employee.name,
      username: employee.username,
      email: employee.email,
      password: '',
      role: employee.role,
      status: employee.status
    });
    setIsModalOpen(true);
  };

  const handleSaveEmployee = async () => {
    if (!formData.name || !formData.username || !formData.email) {
      toast.error('Nome, usuário e email são obrigatórios');
      return;
    }

    if (!editingEmployee && !formData.password) {
      toast.error('Senha é obrigatória para novos funcionários');
      return;
    }

    // Check if username already exists (when creating or changing username)
    const usernameExists = employees.some(emp => 
      emp.username === formData.username && 
      emp.id !== editingEmployee?.id
    );

    if (usernameExists) {
      toast.error('Este nome de usuário já está em uso');
      return;
    }

    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 500));

    if (editingEmployee) {
      // Update existing employee
      setEmployees(prev => prev.map(employee => 
        employee.id === editingEmployee.id 
          ? { 
              ...employee, 
              name: formData.name,
              username: formData.username,
              email: formData.email,
              role: formData.role,
              status: formData.status
            }
          : employee
      ));
      toast.success('Funcionário atualizado com sucesso');
    } else {
      // Create new employee
      const newEmployee: Employee = {
        id: Date.now().toString(),
        name: formData.name,
        username: formData.username,
        email: formData.email,
        role: formData.role,
        status: formData.status,
        createdAt: new Date().toISOString().split('T')[0]
      };
      setEmployees(prev => [...prev, newEmployee]);
      toast.success('Funcionário criado com sucesso');
    }

    setIsModalOpen(false);
  };

  const handleDeleteEmployee = async (employeeId: string) => {
    const employee = employees.find(emp => emp.id === employeeId);
    
    if (employee?.id === currentUser?.id) {
      toast.error('Você não pode excluir sua própria conta');
      return;
    }

    if (!confirm('Tem certeza que deseja excluir este funcionário?')) return;

    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 500));

    setEmployees(prev => prev.filter(employee => employee.id !== employeeId));
    toast.success('Funcionário removido com sucesso');
  };

  const handleToggleStatus = async (employeeId: string) => {
    const employee = employees.find(emp => emp.id === employeeId);
    
    if (employee?.id === currentUser?.id) {
      toast.error('Você não pode desativar sua própria conta');
      return;
    }

    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 300));

    setEmployees(prev => prev.map(employee => 
      employee.id === employeeId 
        ? { ...employee, status: employee.status === 'ativo' ? 'inativo' : 'ativo' }
        : employee
    ));
    toast.success('Status do funcionário atualizado');
  };

  const getRoleLabel = (role: string) => {
    switch (role) {
      case 'admin': return 'Administrador';
      case 'operador': return 'Operador';
      case 'visualizador': return 'Visualizador';
      default: return role;
    }
  };

  const getRoleColor = (role: string) => {
    switch (role) {
      case 'admin': return 'bg-red-100 text-red-800';
      case 'operador': return 'bg-blue-100 text-blue-800';
      case 'visualizador': return 'bg-green-100 text-green-800';
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
              {[...Array(4)].map((_, i) => (
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
          <h1>Gestão de Funcionários</h1>
          <p className="text-muted-foreground">
            Gerencie administradores, operadores e visualizadores do sistema
          </p>
        </div>
        <Button onClick={handleCreateEmployee} className="bg-blue-600 hover:bg-blue-700">
          <Plus className="h-4 w-4 mr-2" />
          Novo Funcionário
        </Button>
      </div>

      <Alert>
        <Shield className="h-4 w-4" />
        <AlertDescription>
          <strong>Níveis de Acesso:</strong> Admin (acesso total), Operador (sem gestão de funcionários), Visualizador (apenas visualização)
        </AlertDescription>
      </Alert>

      <Card>
        <CardHeader>
          <div className="flex flex-col md:flex-row gap-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Buscar por nome, usuário ou email..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
            </div>
            <div className="flex gap-2">
              <Select value={filterRole} onValueChange={setFilterRole}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="Nível" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todos">Todos os níveis</SelectItem>
                  <SelectItem value="admin">Administrador</SelectItem>
                  <SelectItem value="operador">Operador</SelectItem>
                  <SelectItem value="visualizador">Visualizador</SelectItem>
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
                <TableHead>Funcionário</TableHead>
                <TableHead>Usuário</TableHead>
                <TableHead>Nível</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Último Acesso</TableHead>
                <TableHead className="text-right">Ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredEmployees.map((employee) => (
                <TableRow key={employee.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <Avatar className="h-8 w-8">
                        <AvatarFallback className="bg-blue-100 text-blue-800 text-xs">
                          {employee.name.split(' ').map(n => n[0]).join('')}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <p className="font-medium">{employee.name}</p>
                        <p className="text-sm text-muted-foreground">{employee.email}</p>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <code className="bg-muted px-2 py-1 rounded text-sm">
                      {employee.username}
                    </code>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={getRoleColor(employee.role)}>
                      {getRoleLabel(employee.role)}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <Switch
                        checked={employee.status === 'ativo'}
                        onCheckedChange={() => handleToggleStatus(employee.id)}
                        disabled={employee.id === currentUser?.id}
                      />
                    </div>
                  </TableCell>
                  <TableCell>
                    {employee.lastAccess ? (
                      <div>
                        <p className="text-sm">
                          {new Date(employee.lastAccess).toLocaleDateString('pt-BR')}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {new Date(employee.lastAccess).toLocaleTimeString('pt-BR', { 
                            hour: '2-digit', 
                            minute: '2-digit' 
                          })}
                        </p>
                      </div>
                    ) : (
                      <span className="text-sm text-muted-foreground">Nunca</span>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="sm">
                          <MoreVertical className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => handleEditEmployee(employee)}>
                          <Edit className="h-4 w-4 mr-2" />
                          Editar
                        </DropdownMenuItem>
                        <DropdownMenuItem 
                          onClick={() => handleDeleteEmployee(employee.id)}
                          disabled={employee.id === currentUser?.id}
                          className="text-red-600"
                        >
                          <Trash2 className="h-4 w-4 mr-2" />
                          Excluir
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {filteredEmployees.length === 0 && (
            <div className="text-center py-8">
              <p className="text-muted-foreground">Nenhum funcionário encontrado</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Modal de Criação/Edição */}
      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>
              {editingEmployee ? 'Editar Funcionário' : 'Novo Funcionário'}
            </DialogTitle>
            <DialogDescription>
              {editingEmployee ? 'Altere as informações do funcionário.' : 'Preencha os dados para criar um novo funcionário.'}
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
              <Label htmlFor="username">Nome de usuário</Label>
              <Input
                id="username"
                value={formData.username}
                onChange={(e) => setFormData(prev => ({ ...prev, username: e.target.value }))}
                placeholder="Digite o nome de usuário"
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
              <Label htmlFor="password">
                {editingEmployee ? 'Nova senha (deixe vazio para manter)' : 'Senha'}
              </Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={formData.password}
                  onChange={(e) => setFormData(prev => ({ ...prev, password: e.target.value }))}
                  placeholder={editingEmployee ? 'Nova senha' : 'Digite a senha'}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="absolute right-0 top-0 h-10 px-3 hover:bg-transparent"
                  onClick={() => setShowPassword(!showPassword)}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </Button>
              </div>
            </div>
            
            <div>
              <Label htmlFor="role">Nível de acesso</Label>
              <Select 
                value={formData.role} 
                onValueChange={(value: Employee['role']) => setFormData(prev => ({ ...prev, role: value }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="admin">Administrador</SelectItem>
                  <SelectItem value="operador">Operador</SelectItem>
                  <SelectItem value="visualizador">Visualizador</SelectItem>
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
              <Label htmlFor="status">Funcionário ativo</Label>
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setIsModalOpen(false)}>
              Cancelar
            </Button>
            <Button onClick={handleSaveEmployee} className="bg-blue-600 hover:bg-blue-700">
              {editingEmployee ? 'Salvar' : 'Criar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}