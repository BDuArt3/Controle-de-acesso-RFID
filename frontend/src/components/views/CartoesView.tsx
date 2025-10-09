import React, { useState, useEffect } from 'react';
import { CreditCard, Search, Zap, CheckCircle, XCircle, Clock, User, Trash2, Plus } from 'lucide-react';
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
import { services } from '../../services';
import { type Cartao, type GravarCartaoRequest, CartaoUtils } from '../../services/cartaoService';
import { type UsuarioDto } from '../../services/usuarioService';
type GravingStatus = 'idle' | 'waiting' | 'graving' | 'success' | 'error';
export function CartoesView() {
  const { user: currentUser } = useAuth();
  const [cartoes, setCartoes] = useState<Cartao[]>([]);
  const [filteredCartoes, setFilteredCartoes] = useState<Cartao[]>([]);
  const [usuarios, setUsuarios] = useState<UsuarioDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<string>('todos');
  const [isGravingModalOpen, setIsGravingModalOpen] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string>('');
  const [gravingStatus, setGravingStatus] = useState<GravingStatus>('idle');
  const [gravingProgress, setGravingProgress] = useState(0);
  const canEdit = currentUser?.role === 'admin' || currentUser?.role === 'operador';
  useEffect(() => {
    loadData();
  }, []);
  useEffect(() => {
    console.log('useEffect filterCartoes disparado. cartoes.length:', cartoes.length);
    filterCartoes();
  }, [cartoes, searchTerm, filterStatus]);
  const loadData = async () => {
    try {
      setIsLoading(true);
      
      console.log('Iniciando carregamento de dados...');
      
      const [cartoesData, usuariosData] = await Promise.all([
        services.cartao.getAll(),
        services.usuario.getAll()
      ]);
      
      console.log('Cartões carregados:', cartoesData);
      console.log('Usuários carregados:', usuariosData);
      
      setCartoes(cartoesData);
      setUsuarios(usuariosData);
      setFilteredCartoes(cartoesData); // Inicializar filteredCartoes com todos os cartões
      console.log('Estado dos cartões atualizado:', cartoesData.length);
    } catch (error) {
      console.error('Erro ao carregar dados:', error);
      toast.error("Não foi possível carregar os dados.");
    } finally {
      setIsLoading(false);
      console.log('Loading finalizado: isLoading agora é false');
    }
  };
  const filterCartoes = () => {
    console.log('Filtrando cartões. Total:', cartoes.length);
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
    console.log('Cartões filtrados:', filtered.length);
    setFilteredCartoes(filtered);
  };
  const handleGravarCartao = async () => {
    if (!selectedUserId) {
      toast.error("Selecione um usuário para gravar o cartão.");
      return;
    }
    try {
      setGravingStatus('waiting');
      setGravingProgress(0);
      const request: GravarCartaoRequest = {
        usuarioId: selectedUserId
      };
      const response = await services.cartao.solicitarGravacao(request);
      setGravingStatus('graving');
      setGravingProgress(50);
      toast.success(`Solicitação de gravação enviada: ${response.guidGerado}`);
      setTimeout(() => {
        setGravingProgress(100);
        setGravingStatus('success');
        loadData();
        setTimeout(() => {
          setIsGravingModalOpen(false);
          setGravingStatus('idle');
          setGravingProgress(0);
          setSelectedUserId('');
        }, 2000);
      }, 3000);
    } catch (error: any) {
      console.error('Erro ao gravar cartão:', error);
      setGravingStatus('error');
  };
  const handleToggleStatus = async (cartao: Cartao) => {
    try {
      if (cartao.isActive) {
        await services.cartao.desativar(cartao.id);
        toast.success("Cartão desativado com sucesso.");
      } else {
        await services.cartao.ativar(cartao.id);
        toast.success("Cartão ativado com sucesso.");
      }
      await loadData();
    } catch (error: any) {
      console.error('Erro ao alterar status do cartão:', error);
      toast.error(error.message || "Erro ao alterar status do cartão.");
    }
  };
  const handleDeleteCartao = async (cartao: Cartao) => {
    if (!confirm(`Tem certeza que deseja excluir o cartão ${cartao.uid}?`)) {
      return;
    }
    try {
      await services.cartao.deleteCartao(cartao.id);
      toast.success("Cartão excluído com sucesso.");
      await loadData();
    } catch (error: any) {
      console.error('Erro ao excluir cartão:', error);
      toast.error(error.message || "Erro ao excluir cartão.");
    }
  };
  const getUsuarioSemCartao = () => {
    return usuarios.filter(usuario => 
      !cartoes.some(cartao => cartao.usuarioId === usuario.id)
    );
  };
  const getInitials = (name: string) => {
    return name.split(' ').map(n => n[0]).join('').toUpperCase();
  };
  
  console.log('Renderizando CartoesView. isLoading:', isLoading, 'cartoes.length:', cartoes.length, 'filteredCartoes.length:', filteredCartoes.length);
  
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
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold">Gestão de Cartões</h1>
          <p className="text-muted-foreground">
            Gerencie cartões RFID do sistema de controle de acesso
          </p>
        </div>
        {canEdit && (
          <Button onClick={() => setIsGravingModalOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Gravar Novo Cartão
          </Button>
        )}
      </div>
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
                <p className="text-3xl font-bold text-gray-600">
                  {cartoes.filter(c => !c.isActive).length}
                </p>
              </div>
              <XCircle className="h-8 w-8 text-gray-600" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Usuários sem Cartão</p>
                <p className="text-3xl font-bold text-orange-600">
                  {getUsuarioSemCartao().length}
                </p>
              </div>
              <User className="h-8 w-8 text-orange-600" />
            </div>
          </CardContent>
        </Card>
      </div>
      <Card>
        <CardContent className="p-6">
          <div className="flex gap-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Buscar por nome do usuário ou UID do cartão..."
                  className="pl-10"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                />
              </div>
            </div>
            <Select value={filterStatus} onValueChange={setFilterStatus}>
              <SelectTrigger className="w-48">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="todos">Todos os Status</SelectItem>
                <SelectItem value="ativo">Ativos</SelectItem>
                <SelectItem value="inativo">Inativos</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Cartões Cadastrados</CardTitle>
          <CardDescription>
            Lista de todos os cartões RFID cadastrados no sistema
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Usuário</TableHead>
                <TableHead>UID do Cartão</TableHead>
                <TableHead>Status</TableHead>
                {canEdit && <TableHead>Ações</TableHead>}
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
                  {canEdit && (
                    <TableCell>
                      <div className="flex space-x-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleToggleStatus(cartao)}
                        >
                          {cartao.isActive ? (
                            <>
                              <XCircle className="h-4 w-4 mr-1" />
                              Desativar
                            </>
                          ) : (
                            <>
                              <CheckCircle className="h-4 w-4 mr-1" />
                              Ativar
                            </>
                          )}
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleDeleteCartao(cartao)}
                          className="text-red-600 hover:text-red-700"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
              {filteredCartoes.length === 0 && (
                <TableRow>
                  <TableCell colSpan={canEdit ? 4 : 3} className="text-center py-8">
                    <div className="text-muted-foreground">
                      <CreditCard className="h-12 w-12 mx-auto mb-4 opacity-50" />
                      <p>Nenhum cartão encontrado</p>
                      <p className="text-sm">
                        {searchTerm || filterStatus !== 'todos' 
                          ? 'Tente ajustar os filtros de busca'
                          : 'Nenhum cartão foi cadastrado ainda'
                        }
                      </p>
                    </div>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
      <Dialog open={isGravingModalOpen} onOpenChange={setIsGravingModalOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Gravar Novo Cartão RFID</DialogTitle>
            <DialogDescription>
              Selecione um usuário e posicione o cartão no leitor para gravação
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            {gravingStatus === 'idle' && (
              <div>
                <label className="text-sm font-medium">Usuário</label>
                <Select value={selectedUserId} onValueChange={setSelectedUserId}>
                  <SelectTrigger>
                    <SelectValue placeholder="Selecione um usuário" />
                  </SelectTrigger>
                  <SelectContent>
                    {getUsuarioSemCartao().map((usuario) => (
                      <SelectItem key={usuario.id} value={usuario.id}>
                        {usuario.nome}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            {gravingStatus !== 'idle' && (
              <div className="space-y-4">
                <div className="text-center">
                  {gravingStatus === 'waiting' && (
                    <div className="space-y-2">
                      <Clock className="h-12 w-12 mx-auto text-blue-600 animate-pulse" />
                      <p className="font-medium">Aguardando posicionamento do cartão...</p>
                      <p className="text-sm text-muted-foreground">
                        Posicione o cartão RFID no leitor
                      </p>
                    </div>
                  )}
                  {gravingStatus === 'graving' && (
                    <div className="space-y-2">
                      <Zap className="h-12 w-12 mx-auto text-yellow-600 animate-bounce" />
                      <p className="font-medium">Gravando cartão...</p>
                      <p className="text-sm text-muted-foreground">
                        Mantenha o cartão no leitor
                      </p>
                    </div>
                  )}
                  {gravingStatus === 'success' && (
                    <div className="space-y-2">
                      <CheckCircle className="h-12 w-12 mx-auto text-green-600" />
                      <p className="font-medium text-green-600">Cartão gravado com sucesso!</p>
                      <p className="text-sm text-muted-foreground">
                        O cartão foi associado ao usuário
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
                </div>
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
    </div>
  );
}
}