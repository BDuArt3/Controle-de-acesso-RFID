import React, { useState, useEffect } from 'react';
import { CreditCard, Search, Zap, CheckCircle, XCircle, Clock, User, Trash2 } from 'lucide-react';
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

interface CardRecord {
  id: string;
  cardId: string;
  userId: string;
  userName: string;
  userType: 'aluno' | 'professor' | 'funcionario' | 'visitante';
  status: 'ativo' | 'inativo';
  registeredAt: string;
  lastUsed?: string;
}

interface User {
  id: string;
  name: string;
  type: 'aluno' | 'professor' | 'funcionario' | 'visitante';
  hasCard: boolean;
}

type GravingStatus = 'idle' | 'waiting' | 'graving' | 'success' | 'error';

export function CartoesView() {
  const { user: currentUser } = useAuth();
  const [cards, setCards] = useState<CardRecord[]>([]);
  const [filteredCards, setFilteredCards] = useState<CardRecord[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterType, setFilterType] = useState<string>('todos');
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
    filterCards();
  }, [cards, searchTerm, filterType, filterStatus]);

  const loadData = async () => {
    setIsLoading(true);
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    const mockUsers: User[] = [
      { id: '1', name: 'João Silva', type: 'funcionario', hasCard: true },
      { id: '2', name: 'Maria Santos', type: 'professor', hasCard: true },
      { id: '3', name: 'Ana Costa', type: 'aluno', hasCard: false },
      { id: '4', name: 'Pedro Oliveira', type: 'visitante', hasCard: false },
      { id: '5', name: 'Luisa Ferreira', type: 'aluno', hasCard: true },
    ];

    const mockCards: CardRecord[] = [
      {
        id: '1',
        cardId: 'CARD001',
        userId: '1',
        userName: 'João Silva',
        userType: 'funcionario',
        status: 'ativo',
        registeredAt: '2024-01-15T10:30:00Z',
        lastUsed: '2024-03-25T14:35:00Z'
      },
      {
        id: '2',
        cardId: 'CARD002',
        userId: '2',
        userName: 'Maria Santos',
        userType: 'professor',
        status: 'ativo',
        registeredAt: '2024-02-10T09:15:00Z',
        lastUsed: '2024-03-25T14:32:00Z'
      },
      {
        id: '5',
        cardId: 'CARD005',
        userId: '5',
        userName: 'Luisa Ferreira',
        userType: 'aluno',
        status: 'ativo',
        registeredAt: '2024-03-22T11:45:00Z',
        lastUsed: '2024-03-24T16:20:00Z'
      }
    ];
    
    setUsers(mockUsers);
    setCards(mockCards);
    setIsLoading(false);
  };

  const filterCards = () => {
    let filtered = cards;

    if (searchTerm) {
      filtered = filtered.filter(card =>
        card.userName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        card.cardId.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (filterType !== 'todos') {
      filtered = filtered.filter(card => card.userType === filterType);
    }

    if (filterStatus !== 'todos') {
      filtered = filtered.filter(card => card.status === filterStatus);
    }

    setFilteredCards(filtered);
  };

  const handleStartGraving = () => {
    setSelectedUserId('');
    setGravingStatus('idle');
    setGravingProgress(0);
    setIsGravingModalOpen(true);
  };

  const handleGraveCard = async () => {
    if (!selectedUserId) {
      toast.error('Selecione um usuário');
      return;
    }

    const selectedUser = users.find(u => u.id === selectedUserId);
    if (!selectedUser) return;

    if (selectedUser.hasCard) {
      toast.error('Este usuário já possui um cartão ativo');
      return;
    }

    // Start graving process
    setGravingStatus('waiting');
    setGravingProgress(10);

    // Simulate waiting for card
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    if (Math.random() > 0.2) { // 80% success rate
      setGravingStatus('graving');
      setGravingProgress(30);

      // Simulate graving process
      for (let i = 30; i <= 90; i += 20) {
        await new Promise(resolve => setTimeout(resolve, 500));
        setGravingProgress(i);
      }

      setGravingStatus('success');
      setGravingProgress(100);

      // Create new card record
      const newCard: CardRecord = {
        id: Date.now().toString(),
        cardId: `CARD${String(cards.length + 1).padStart(3, '0')}`,
        userId: selectedUser.id,
        userName: selectedUser.name,
        userType: selectedUser.type,
        status: 'ativo',
        registeredAt: new Date().toISOString()
      };

      // Update data
      setCards(prev => [...prev, newCard]);
      setUsers(prev => prev.map(user => 
        user.id === selectedUserId 
          ? { ...user, hasCard: true }
          : user
      ));

      toast.success('Cartão gravado com sucesso!');

      // Auto close after success
      setTimeout(() => {
        setIsGravingModalOpen(false);
      }, 2000);
    } else {
      // Simulate error
      setGravingStatus('error');
      toast.error('Erro na gravação do cartão');
    }
  };

  const handleDeleteCard = async (cardId: string) => {
    if (!confirm('Tem certeza que deseja remover este cartão?')) return;

    const card = cards.find(c => c.id === cardId);
    if (!card) return;

    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 500));

    setCards(prev => prev.filter(c => c.id !== cardId));
    setUsers(prev => prev.map(user => 
      user.id === card.userId 
        ? { ...user, hasCard: false }
        : user
    ));

    toast.success('Cartão removido com sucesso');
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

  const getGravingStatusInfo = () => {
    switch (gravingStatus) {
      case 'waiting':
        return {
          title: 'Aguardando cartão...',
          description: 'Aproxime o cartão RFID do leitor',
          icon: <Clock className="h-8 w-8 text-blue-600 animate-pulse" />,
          color: 'text-blue-600'
        };
      case 'graving':
        return {
          title: 'Gravando cartão...',
          description: 'Não remova o cartão do leitor',
          icon: <Zap className="h-8 w-8 text-yellow-600 animate-pulse" />,
          color: 'text-yellow-600'
        };
      case 'success':
        return {
          title: 'Gravação concluída!',
          description: 'Cartão gravado com sucesso',
          icon: <CheckCircle className="h-8 w-8 text-green-600" />,
          color: 'text-green-600'
        };
      case 'error':
        return {
          title: 'Erro na gravação',
          description: 'Tente novamente',
          icon: <XCircle className="h-8 w-8 text-red-600" />,
          color: 'text-red-600'
        };
      default:
        return {
          title: 'Selecione um usuário',
          description: 'Escolha o usuário para gravar um novo cartão',
          icon: <User className="h-8 w-8 text-muted-foreground" />,
          color: 'text-muted-foreground'
        };
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

  if (!canEdit) {
    return (
      <div className="space-y-6">
        <div>
          <h1>Cartões RFID</h1>
          <p className="text-muted-foreground">Visualização dos cartões cadastrados</p>
        </div>
        
        <Card>
          <CardHeader>
            <div className="flex flex-col md:flex-row gap-4">
              <div className="flex-1">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Buscar por usuário ou código do cartão..."
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
                  <TableHead>Código do Cartão</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Último Uso</TableHead>
                  <TableHead>Registrado em</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredCards.map((card) => (
                  <TableRow key={card.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <Avatar className="h-8 w-8">
                          <AvatarFallback className="bg-blue-100 text-blue-800 text-xs">
                            {card.userName.split(' ').map(n => n[0]).join('')}
                          </AvatarFallback>
                        </Avatar>
                        <span className="font-medium">{card.userName}</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <code className="bg-muted px-2 py-1 rounded text-sm">
                        {card.cardId}
                      </code>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className={getUserTypeColor(card.userType)}>
                        {getUserTypeLabel(card.userType)}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={card.status === 'ativo' ? 'default' : 'secondary'}>
                        {card.status === 'ativo' ? 'Ativo' : 'Inativo'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {card.lastUsed ? (
                        <div>
                          <p className="text-sm">
                            {new Date(card.lastUsed).toLocaleDateString('pt-BR')}
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {new Date(card.lastUsed).toLocaleTimeString('pt-BR', { 
                              hour: '2-digit', 
                              minute: '2-digit' 
                            })}
                          </p>
                        </div>
                      ) : (
                        <span className="text-sm text-muted-foreground">Nunca usado</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {new Date(card.registeredAt).toLocaleDateString('pt-BR')}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>

            {filteredCards.length === 0 && (
              <div className="text-center py-8">
                <p className="text-muted-foreground">Nenhum cartão encontrado</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-start">
        <div>
          <h1>Gravação de Cartões RFID</h1>
          <p className="text-muted-foreground">
            Gerencie e grave novos cartões para usuários
          </p>
        </div>
        <Button onClick={handleStartGraving} className="bg-blue-600 hover:bg-blue-700">
          <Zap className="h-4 w-4 mr-2" />
          Gravar Cartão
        </Button>
      </div>

      <Alert>
        <CreditCard className="h-4 w-4" />
        <AlertDescription>
          Certifique-se de que o leitor RFID está conectado antes de iniciar a gravação de cartões.
        </AlertDescription>
      </Alert>

      <Card>
        <CardHeader>
          <div className="flex flex-col md:flex-row gap-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Buscar por usuário ou código do cartão..."
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
                <TableHead>Código do Cartão</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Último Uso</TableHead>
                <TableHead>Registrado em</TableHead>
                <TableHead className="text-right">Ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredCards.map((card) => (
                <TableRow key={card.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <Avatar className="h-8 w-8">
                        <AvatarFallback className="bg-blue-100 text-blue-800 text-xs">
                          {card.userName.split(' ').map(n => n[0]).join('')}
                        </AvatarFallback>
                      </Avatar>
                      <span className="font-medium">{card.userName}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <code className="bg-muted px-2 py-1 rounded text-sm">
                      {card.cardId}
                    </code>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={getUserTypeColor(card.userType)}>
                      {getUserTypeLabel(card.userType)}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Badge variant={card.status === 'ativo' ? 'default' : 'secondary'}>
                      {card.status === 'ativo' ? 'Ativo' : 'Inativo'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    {card.lastUsed ? (
                      <div>
                        <p className="text-sm">
                          {new Date(card.lastUsed).toLocaleDateString('pt-BR')}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {new Date(card.lastUsed).toLocaleTimeString('pt-BR', { 
                            hour: '2-digit', 
                            minute: '2-digit' 
                          })}
                        </p>
                      </div>
                    ) : (
                      <span className="text-sm text-muted-foreground">Nunca usado</span>
                    )}
                  </TableCell>
                  <TableCell>
                    {new Date(card.registeredAt).toLocaleDateString('pt-BR')}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleDeleteCard(card.id)}
                      className="text-red-600 hover:text-red-700 hover:bg-red-50"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {filteredCards.length === 0 && (
            <div className="text-center py-8">
              <p className="text-muted-foreground">Nenhum cartão encontrado</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Modal de Gravação */}
      <Dialog open={isGravingModalOpen} onOpenChange={setIsGravingModalOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <CreditCard className="h-5 w-5" />
              Gravar Novo Cartão
            </DialogTitle>
            <DialogDescription>
              Selecione o usuário e inicie o processo de gravação
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-6">
            {gravingStatus === 'idle' && (
              <div>
                <label className="text-sm font-medium">Selecionar usuário</label>
                <Select value={selectedUserId} onValueChange={setSelectedUserId}>
                  <SelectTrigger className="mt-2">
                    <SelectValue placeholder="Escolha um usuário sem cartão" />
                  </SelectTrigger>
                  <SelectContent>
                    {users
                      .filter(user => !user.hasCard)
                      .map(user => (
                        <SelectItem key={user.id} value={user.id}>
                          <div className="flex items-center gap-2">
                            <Badge variant="outline" className={getUserTypeColor(user.type)}>
                              {getUserTypeLabel(user.type)}
                            </Badge>
                            {user.name}
                          </div>
                        </SelectItem>
                      ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="text-center py-8">
              <div className="flex flex-col items-center gap-4">
                {getGravingStatusInfo().icon}
                <div>
                  <h3 className={`font-medium ${getGravingStatusInfo().color}`}>
                    {getGravingStatusInfo().title}
                  </h3>
                  <p className="text-sm text-muted-foreground mt-1">
                    {getGravingStatusInfo().description}
                  </p>
                </div>
              </div>

              {(gravingStatus === 'waiting' || gravingStatus === 'graving') && (
                <div className="mt-4">
                  <Progress value={gravingProgress} className="h-2" />
                  <p className="text-xs text-muted-foreground mt-2">
                    {gravingProgress}% concluído
                  </p>
                </div>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button 
              variant="outline" 
              onClick={() => setIsGravingModalOpen(false)}
              disabled={gravingStatus === 'waiting' || gravingStatus === 'graving'}
            >
              {gravingStatus === 'success' ? 'Fechar' : 'Cancelar'}
            </Button>
            {gravingStatus === 'idle' && (
              <Button 
                onClick={handleGraveCard} 
                className="bg-blue-600 hover:bg-blue-700"
                disabled={!selectedUserId}
              >
                <Zap className="h-4 w-4 mr-2" />
                Iniciar Gravação
              </Button>
            )}
            {gravingStatus === 'error' && (
              <Button 
                onClick={() => setGravingStatus('idle')} 
                className="bg-blue-600 hover:bg-blue-700"
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