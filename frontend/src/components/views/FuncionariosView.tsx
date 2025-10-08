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
import { services, type Funcionario, type CreateFuncionarioRequest } from '../../services';

// Enum para tipos de funcionário (deve coincidir com o backend)
enum TipoFuncionario {
  Visualizador = 0,
  Operador = 1,
  Admin = 2
}

// Interface adaptada para corresponder ao DTO da API
interface Employee {
  id: string;
  nomeFuncionario: string;
  usuarioAcesso: string;
  tipo: TipoFuncionario;
  isActive: boolean;
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
    nomeFuncionario: '',
    usuarioAcesso: '',
    senha: '',
    tipo: TipoFuncionario.Visualizador
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

  // Funções auxiliares para conversão de tipos
  const getTipoFromString = (tipo: string): TipoFuncionario => {
    switch (tipo.toLowerCase()) {
      case 'admin':
      case 'administrator':
      case '2':
        return TipoFuncionario.Admin;
      case 'operador':
      case '1':
        return TipoFuncionario.Operador;
      case 'visualizador':
      case '0':
      default:
        return TipoFuncionario.Visualizador;
    }
  };

  const getTipoToString = (tipo: TipoFuncionario): string => {
    switch (tipo) {
      case TipoFuncionario.Admin:
        return 'admin';
      case TipoFuncionario.Operador:
        return 'operador';
      case TipoFuncionario.Visualizador:
      default:
        return 'visualizador';
    }
  };

  useEffect(() => {
    loadEmployees();
  }, []);

  useEffect(() => {
    filterEmployees();
  }, [employees, searchTerm, filterRole, filterStatus]);

  const loadEmployees = async () => {
    setIsLoading(true);
    try {
      console.log('Carregando funcionários da API...');
      const funcionariosData = await services.funcionario.getAll();
      
      // Converter Funcionario para Employee interface
      const employeesConverted: Employee[] = funcionariosData.map(funcionario => ({
        id: funcionario.id,
        nomeFuncionario: funcionario.nomeFuncionario,
        usuarioAcesso: funcionario.usuarioAcesso,
        tipo: getTipoFromString(funcionario.tipo),
        isActive: funcionario.ativo
      }));
      
      setEmployees(employeesConverted);
      console.log('Funcionários carregados:', employeesConverted);
    } catch (error) {
      console.error('Erro ao carregar funcionários:', error);
      toast.error('Erro ao carregar funcionários');
    } finally {
      setIsLoading(false);
    }
  };

  const filterEmployees = () => {
    let filtered = employees;

    if (searchTerm) {
      filtered = filtered.filter(employee =>
        employee.nomeFuncionario.toLowerCase().includes(searchTerm.toLowerCase()) ||
        employee.usuarioAcesso.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (filterRole !== 'todos') {
      const tipoFiltro = getTipoFromString(filterRole);
      filtered = filtered.filter(employee => employee.tipo === tipoFiltro);
    }

    if (filterStatus !== 'todos') {
      const isActive = filterStatus === 'ativo';
      filtered = filtered.filter(employee => employee.isActive === isActive);
    }

    setFilteredEmployees(filtered);
  };

  const handleCreateEmployee = () => {
    setEditingEmployee(null);
    setFormData({
      nomeFuncionario: '',
      usuarioAcesso: '',
      senha: '',
      tipo: TipoFuncionario.Visualizador
    });
    setIsModalOpen(true);
  };

  const handleEditEmployee = (employee: Employee) => {
    setEditingEmployee(employee);
    setFormData({
      nomeFuncionario: employee.nomeFuncionario,
      usuarioAcesso: employee.usuarioAcesso,
      senha: '',
      tipo: employee.tipo
    });
    setIsModalOpen(true);
  };

  const handleSaveEmployee = async () => {
    if (!formData.nomeFuncionario || !formData.usuarioAcesso) {
      toast.error('Nome e usuário de acesso são obrigatórios');
      return;
    }

    if (!editingEmployee && !formData.senha) {
      toast.error('Senha é obrigatória para novos funcionários');
      return;
    }

    // Check if username already exists (when creating or changing username)
    const usernameExists = employees.some(emp => 
      emp.usuarioAcesso === formData.usuarioAcesso && 
      emp.id !== editingEmployee?.id
    );

    if (usernameExists) {
      toast.error('Este nome de usuário já está em uso');
      return;
    }

    try {
      if (editingEmployee) {
        // Atualizar funcionário existente
        const updateDto: CreateFuncionarioRequest = {
          nomeFuncionario: formData.nomeFuncionario,
          usuarioAcesso: formData.usuarioAcesso,
          senha: formData.senha || '', // Senha vazia para não alterar
          tipo: getTipoToString(formData.tipo)
        };
        
        const funcionarioAtualizado = await services.funcionario.update(editingEmployee.id, updateDto);
        
        setEmployees(prev => prev.map(employee => 
          employee.id === editingEmployee.id 
            ? { 
                ...employee, 
                nomeFuncionario: funcionarioAtualizado.nomeFuncionario,
                usuarioAcesso: funcionarioAtualizado.usuarioAcesso,
                tipo: getTipoFromString(funcionarioAtualizado.tipo)
              }
            : employee
        ));
        
        toast.success('Funcionário atualizado com sucesso');
      } else {
        // Criar novo funcionário
        const createDto: CreateFuncionarioRequest = {
          nomeFuncionario: formData.nomeFuncionario,
          usuarioAcesso: formData.usuarioAcesso,
          senha: formData.senha,
          tipo: getTipoToString(formData.tipo)
        };
        
        const novoFuncionario = await services.funcionario.create(createDto);
        
        const newEmployee: Employee = {
          id: novoFuncionario.id,
          nomeFuncionario: novoFuncionario.nomeFuncionario,
          usuarioAcesso: novoFuncionario.usuarioAcesso,
          tipo: getTipoFromString(novoFuncionario.tipo),
          isActive: novoFuncionario.ativo
        };
        
        setEmployees(prev => [...prev, newEmployee]);
        toast.success('Funcionário criado com sucesso');
      }

      setIsModalOpen(false);
    } catch (error: any) {
      console.error('Erro ao salvar funcionário:', error);
      
      // Tratamento de erros específicos
      if (error.statusCode === 400 || error.statusCode === 409) {
        toast.error('Dados inválidos: ' + (error.message || 'Verifique os campos'));
      } else if (error.statusCode === 401) {
        toast.error('Você não tem permissão para esta operação');
      } else {
        toast.error('Erro ao salvar funcionário');
      }
    }
  };

  const handleDeleteEmployee = async (employeeId: string) => {
    const employee = employees.find(emp => emp.id === employeeId);
    
    if (employee?.id === currentUser?.id) {
      toast.error('Você não pode excluir sua própria conta');
      return;
    }

    if (!confirm('Tem certeza que deseja desativar este funcionário?')) return;

    try {
      await services.funcionario.deleteFuncionario(employeeId);
      setEmployees(prev => prev.map(employee => 
        employee.id === employeeId 
          ? { ...employee, isActive: false }
          : employee
      ));
      toast.success('Funcionário desativado com sucesso');
    } catch (error) {
      console.error('Erro ao desativar funcionário:', error);
      toast.error('Erro ao desativar funcionário');
    }
  };

  const handleToggleStatus = async (employeeId: string) => {
    const employee = employees.find(emp => emp.id === employeeId);
    
    if (employee?.id === currentUser?.id) {
      toast.error('Você não pode desativar sua própria conta');
      return;
    }

    try {
      if (employee?.isActive) {
        // Desativar
        await services.funcionario.deleteFuncionario(employeeId);
      } else {
        // Ativar
        await services.funcionario.activate(employeeId);
      }
      
      setEmployees(prev => prev.map(emp => 
        emp.id === employeeId 
          ? { ...emp, isActive: !emp.isActive }
          : emp
      ));
      
      toast.success(employee?.isActive ? 'Funcionário desativado' : 'Funcionário ativado');
    } catch (error) {
      console.error('Erro ao alterar status:', error);
      toast.error('Erro ao alterar status do funcionário');
    }
  };

  const getRoleLabel = (tipo: TipoFuncionario) => {
    switch (tipo) {
      case TipoFuncionario.Admin: return 'Administrador';
      case TipoFuncionario.Operador: return 'Operador';
      case TipoFuncionario.Visualizador: return 'Visualizador';
      default: return 'Visualizador';
    }
  };

  const getRoleColor = (tipo: TipoFuncionario) => {
    switch (tipo) {
      case TipoFuncionario.Admin: return 'bg-red-100 text-red-800';
      case TipoFuncionario.Operador: return 'bg-blue-100 text-blue-800';
      case TipoFuncionario.Visualizador: return 'bg-green-100 text-green-800';
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
                          {employee.nomeFuncionario.split(' ').map((n: string) => n[0]).join('')}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <p className="font-medium">{employee.nomeFuncionario}</p>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <code className="bg-muted px-2 py-1 rounded text-sm">
                      {employee.usuarioAcesso}
                    </code>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={getRoleColor(employee.tipo)}>
                      {getRoleLabel(employee.tipo)}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <Switch
                        checked={employee.isActive}
                        onCheckedChange={() => handleToggleStatus(employee.id)}
                        disabled={employee.id === currentUser?.id}
                      />
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className="text-sm text-muted-foreground">-</span>
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
              <Label htmlFor="nomeFuncionario">Nome completo</Label>
              <Input
                id="nomeFuncionario"
                value={formData.nomeFuncionario}
                onChange={(e) => setFormData(prev => ({ ...prev, nomeFuncionario: e.target.value }))}
                placeholder="Digite o nome completo"
              />
            </div>
            
            <div>
              <Label htmlFor="usuarioAcesso">Nome de usuário</Label>
              <Input
                id="usuarioAcesso"
                value={formData.usuarioAcesso}
                onChange={(e) => setFormData(prev => ({ ...prev, usuarioAcesso: e.target.value }))}
                placeholder="Digite o nome de usuário"
              />
            </div>
            
            <div>
              <Label htmlFor="senha">
                {editingEmployee ? 'Nova senha (deixe vazio para manter)' : 'Senha'}
              </Label>
              <div className="relative">
                <Input
                  id="senha"
                  type={showPassword ? 'text' : 'password'}
                  value={formData.senha}
                  onChange={(e) => setFormData(prev => ({ ...prev, senha: e.target.value }))}
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
              <Label htmlFor="tipo">Nível de acesso</Label>
              <Select 
                value={formData.tipo.toString()} 
                onValueChange={(value: string) => setFormData(prev => ({ ...prev, tipo: parseInt(value) as TipoFuncionario }))}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={TipoFuncionario.Admin.toString()}>Administrador</SelectItem>
                  <SelectItem value={TipoFuncionario.Operador.toString()}>Operador</SelectItem>
                  <SelectItem value={TipoFuncionario.Visualizador.toString()}>Visualizador</SelectItem>
                </SelectContent>
              </Select>
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