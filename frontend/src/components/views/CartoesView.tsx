import React, { useState, useEffect } from 'react';
import { CreditCard, Search, Zap, CheckCircle, XCircle, Clock, User, Plus } from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Avatar, AvatarFallback } from '../ui/avatar';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../ui/dialog';
import { Progress } from '../ui/progress';
import { Alert, AlertDescription } from '../ui/alert';
import { toast } from 'sonner';
import { useAuth } from '../providers/AuthProvider';
import { usePermissions } from '../../hooks/usePermissions';
import { AccessDeniedView } from '../ui/AccessDeniedView';
import { services } from '../../services';
import { type Cartao, type GravarCartaoRequest, CartaoUtils } from '../../services/cartaoService';
import { type UsuarioDto } from '../../services/usuarioService';

export function CartoesView() {
  const { user: currentUser } = useAuth();
  const { canView, canEdit } = usePermissions();
  const [cartoes, setCartoes] = useState<Cartao[]>([]);
  const [filteredCartoes, setFilteredCartoes] = useState<Cartao[]>([]);
  const [usuarios, setUsuarios] = useState<UsuarioDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<'todos' | 'ativo' | 'inativo'>('todos');
  
  // Estados para modal de gravação
  const [isGravingModalOpen, setIsGravingModalOpen] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string>('');
  const [gravingStatus, setGravingStatus] = useState<'idle' | 'waiting' | 'success' | 'error'>('idle');
  const [gravingProgress, setGravingProgress] = useState(0);

  // Verificação de permissão para acessar esta view
  if (!canView('cards')) {
    return <AccessDeniedView />;
  }

  const canEditCards = canEdit('cards');

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    filterCartoes();
  }, [cartoes, searchTerm, filterStatus]);

  const loadData = async () => {
    try {
      setIsLoading(true);
      
      const [cartoesData, usuariosData] = await Promise.all([
        services.cartao.getAll(),
        services.usuario.getAll()
      ]);
      
      setCartoes(cartoesData);
      setUsuarios(usuariosData);
      setFilteredCartoes(cartoesData);
    } catch (error) {
      console.error('Erro ao carregar dados:', error);
      toast.error("Não foi possível carregar os dados.");
    } finally {
      setIsLoading(false);
    }
  };

  const filterCartoes = () => {
    let filtered = cartoes;
    
    if (searchTerm) {
      filtered = filtered.filter(cartao => 
        cartao.nomeUsuario.toLowerCase().includes(searchTerm.toLowerCase()) ||
        cartao.uid.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }
    
    if (filterStatus !== 'todos') {
      const isActive = filterStatus === 'ativo';
      filtered = filtered.filter(cartao => cartao.isActive === isActive);
    }
    
    setFilteredCartoes(filtered);
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  };

  const getUsuarioSemCartao = () => {
    return usuarios.filter(usuario => 
      usuario.isActive && // Apenas usuários ativos
      (!usuario.cartoes || !usuario.cartoes.some(cartao => cartao.isActive)) // Sem cartão ativo
    );
  };

  const handleGravarCartao = async () => {
    if (!selectedUserId) {
      toast.error("Selecione um usuário para gravar o cartão.");
      return;
    }

    try {
      setGravingStatus('waiting');
      setGravingProgress(0);

      // Simular progresso de gravação
      const progressInterval = setInterval(() => {
        setGravingProgress(prev => {
          if (prev >= 100) {
            clearInterval(progressInterval);
            return 100;
          }
          return prev + 10;
        });
      }, 200);

      // Solicitar gravação
      await services.cartao.solicitarGravacao({ usuarioId: selectedUserId });
      
      // Aguardar alguns segundos simulando gravação
      await new Promise(resolve => setTimeout(resolve, 3000));
      
      clearInterval(progressInterval);
      setGravingProgress(100);
      setGravingStatus('success');
      
      // Recarregar dados
      await loadData();
      
      // Fechar modal após sucesso
      setTimeout(() => {
        setIsGravingModalOpen(false);
        setGravingStatus('idle');
        setGravingProgress(0);
        setSelectedUserId('');
      }, 2000);
      
      toast.success("Cartão gravado com sucesso!");
      
    } catch (error) {
      console.error('Erro ao gravar cartão:', error);
      setGravingStatus('error');
      toast.error("Erro ao gravar cartão. Tente novamente.");
    }
  };

  const handleToggleStatus = async (cartao: Cartao) => {
    try {
      if (cartao.isActive) {
        await services.cartao.desativar(cartao.id);
        toast.success("Cartão desativado com sucesso!");
      } else {
        await services.cartao.ativar(cartao.id);
        toast.success("Cartão ativado com sucesso!");
      }
      
      await loadData();
    } catch (error) {
      console.error('Erro ao alterar status do cartão:', error);
      toast.error("Erro ao alterar status do cartão.");
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Card>
          <CardContent className="p-8 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-4"></div>
            <p>Carregando cartões...</p>
          </CardContent>
        </Card>
      </div>
    );
  }
  
  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Gestão de Cartões</h1>
          <p className="text-muted-foreground">
            {canEditCards ? 'Gerencie cartões RFID do sistema de controle de acesso' : 'Visualize cartões RFID do sistema de controle de acesso'}
          </p>
        </div>
        {canEditCards && (
          <Button onClick={() => setIsGravingModalOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Gravar Novo Cartão
          </Button>
        )}
      </div>

      {/* Cards de estatísticas */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Total de Cartões</p>
                <p className="text-3xl font-bold">{cartoes.length}</p>
              </div>
              <CreditCard className="h-8 w-8 text-blue-600" />
            </div>
          </CardContent>
        </Card>
        
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Cartões Ativos</p>
                <p className="text-3xl font-bold text-green-600">
                  {cartoes.filter(c => c.isActive).length}
                </p>
              </div>
              <CheckCircle className="h-8 w-8 text-green-600" />
            </div>
          </CardContent>
        </Card>
        
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Cartões Inativos</p>
                <p className="text-3xl font-bold text-red-600">
                  {cartoes.filter(c => !c.isActive).length}
                </p>
              </div>
              <XCircle className="h-8 w-8 text-red-600" />
            </div>
          </CardContent>
        </Card>
        
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Portadores sem Cartão</p>
                <p className="text-3xl font-bold text-orange-600">
                  {getUsuarioSemCartao().length}
                </p>
              </div>
              <User className="h-8 w-8 text-orange-600" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filtros */}
      <Card>
        <CardHeader>
          <CardTitle>Filtros</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4">
            <div className="flex-1">
              <Input
                placeholder="Buscar por portador ou UID do cartão..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full"
              />
            </div>
            <Select value={filterStatus} onValueChange={(value: any) => setFilterStatus(value)}>
              <SelectTrigger className="w-48">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="todos">Todos</SelectItem>
                <SelectItem value="ativo">Ativos</SelectItem>
                <SelectItem value="inativo">Inativos</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Tabela de cartões */}
      <Card>
        <CardHeader>
          <CardTitle>Cartões RFID ({filteredCartoes.length})</CardTitle>
          <CardDescription>
            Lista de todos os cartões RFID do sistema
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Portador</TableHead>
                <TableHead>UID do Cartão</TableHead>
                <TableHead>Status</TableHead>
                {canEditCards && <TableHead>Ações</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredCartoes.map((cartao) => (
                <TableRow key={cartao.id}>
                  <TableCell>
                    <div className="flex items-center space-x-3">
                      <Avatar className="h-8 w-8">
                        <AvatarFallback className="text-xs">
                          {getInitials(cartao.nomeUsuario)}
                        </AvatarFallback>
                      </Avatar>
                      <span className="font-medium">{cartao.nomeUsuario}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <code className="bg-muted px-2 py-1 rounded text-sm">
                      {cartao.uid}
                    </code>
                  </TableCell>
                  <TableCell>
                    <Badge 
                      variant={cartao.isActive ? 'default' : 'secondary'}
                      className={CartaoUtils.getStatusColor(cartao.isActive)}
                    >
                      {CartaoUtils.getStatusText(cartao.isActive)}
                    </Badge>
                  </TableCell>
                  {canEditCards && (
                    <TableCell>
                      <div className="flex space-x-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleToggleStatus(cartao)}
                        >
                          {cartao.isActive ? 'Desativar' : 'Ativar'}
                        </Button>
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
              {filteredCartoes.length === 0 && (
                <TableRow>
                  <TableCell colSpan={canEditCards ? 4 : 3} className="text-center py-8">
                    <div className="text-muted-foreground">
                      <CreditCard className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p>Nenhum cartão encontrado</p>
                    </div>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Modal de gravação */}
      {canEditCards && (
        <Dialog open={isGravingModalOpen} onOpenChange={setIsGravingModalOpen}>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Gravar Novo Cartão RFID</DialogTitle>
              <DialogDescription>
                Selecione um usuário e siga as instruções para gravar um novo cartão RFID.
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              {gravingStatus === 'idle' && (
                <div className="space-y-4">
                  <Select value={selectedUserId} onValueChange={setSelectedUserId}>
                    <SelectTrigger>
                      <SelectValue placeholder="Selecione um portador" />
                    </SelectTrigger>
                    <SelectContent>
                      {getUsuarioSemCartao().map((usuario) => (
                        <SelectItem key={usuario.id} value={usuario.id}>
                          {usuario.nome} ({services.usuario.getTipoUsuarioString(usuario.tipo)})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {getUsuarioSemCartao().length === 0 && (
                    <Alert>
                      <AlertDescription>
                        Todos os portadores ativos já possuem cartões ativos associados.
                      </AlertDescription>
                    </Alert>
                  )}
                </div>
              )}
              
              {gravingStatus !== 'idle' && (
                <div className="space-y-4 text-center">
                  {gravingStatus === 'waiting' && (
                    <div className="space-y-2">
                      <Zap className="h-12 w-12 mx-auto text-blue-600 animate-pulse" />
                      <p className="font-medium text-blue-600">Aguardando cartão RFID</p>
                      <p className="text-sm text-muted-foreground">
                        Aproxime o cartão do leitor para gravar
                      </p>
                    </div>
                  )}
                  {gravingStatus === 'success' && (
                    <div className="space-y-2">
                      <CheckCircle className="h-12 w-12 mx-auto text-green-600" />
                      <p className="font-medium text-green-600">Cartão gravado com sucesso!</p>
                      <p className="text-sm text-muted-foreground">
                        O cartão foi associado ao portador
                      </p>
                    </div>
                  )}
                  {gravingStatus === 'error' && (
                    <div className="space-y-2">
                      <XCircle className="h-12 w-12 mx-auto text-red-600" />
                      <p className="font-medium text-red-600">Erro na gravação</p>
                      <p className="text-sm text-muted-foreground">
                        Tente novamente ou verifique o leitor
                      </p>
                    </div>
                  )}
                  <Progress value={gravingProgress} className="w-full" />
                </div>
              )}
            </div>
            <DialogFooter>
              {gravingStatus === 'idle' && (
                <>
                  <Button 
                    variant="outline" 
                    onClick={() => setIsGravingModalOpen(false)}
                  >
                    Cancelar
                  </Button>
                  <Button 
                    onClick={handleGravarCartao}
                    disabled={!selectedUserId}
                  >
                    <Zap className="h-4 w-4 mr-2" />
                    Iniciar Gravação
                  </Button>
                </>
              )}
              {gravingStatus === 'error' && (
                <Button 
                  onClick={() => {
                    setGravingStatus('idle');
                    setGravingProgress(0);
                  }}
                >
                  Tentar Novamente
                </Button>
              )}
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}