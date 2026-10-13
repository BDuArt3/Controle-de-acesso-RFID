import React, { useState, useEffect } from 'react';
import { usuarioService, errorMessage } from '../services/api';
import { useSignalR } from '../hooks/useSignalR';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Button } from './ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Alert, AlertDescription } from './ui/alert';
import { Badge } from './ui/badge';
import { ArrowLeft, CreditCard, Loader2 } from 'lucide-react';

interface UserFormProps {
  editingUserId?: string;
  onNavigate: (route: any) => void;
}

interface UserData {
  name: string;
  document: string;
  userType: string;
  uid: string;
  email: string;
  password: string;
  status: string;
}

export default function UserForm({ editingUserId, onNavigate }: UserFormProps) {
  const [userData, setUserData] = useState<UserData>({
    name: '',
    document: '',
    userType: '',
    uid: '',
    email: '',
    password: '',
    status: 'Ativo'
  });
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const { isReading, lastReadUID, startReading, stopReading } = useSignalR();

  useEffect(() => {
    let active = true;
    if (!editingUserId) return;
    setIsLoading(true);
    const load = async () => {
      try {
        const { data } = await usuarioService.get(editingUserId);
        if (active) setUserData({
          name: data.nome,
          document: data.documento,
          userType: data.tipo.toLowerCase(),
          uid: data.uid || '',
          email: data.email,
          password: '',
          status: data.status
        });
      } catch (err) { if (active) setError(errorMessage(err)); }
      finally { if (active) setIsLoading(false); }
    };
    void load();
    return () => { active = false; };
  }, [editingUserId]);

  useEffect(() => {
    if (!lastReadUID) return;
    setUserData(prev => ({ ...prev, uid: lastReadUID }));
    setSuccess('Cartão lido com sucesso!');
    stopReading();
    const timer = setTimeout(() => setSuccess(''), 3000);
    return () => clearTimeout(timer);
  }, [lastReadUID]);

  const handleInputChange = (field: keyof UserData, value: string) => {
    setUserData(prev => ({ ...prev, [field]: value }));
    setError('');
  };

  const handleReadCard = () => {
    setError('');
    startReading();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsSaving(true);

    if (!userData.name || !userData.document || !userData.userType || !userData.email || (!editingUserId && !userData.password)) {
      setError('Por favor, preencha todos os campos obrigatórios.');
      setIsSaving(false);
      return;
    }

    try {
      const data = {
        nome: userData.name.trim(),
        email: userData.email.trim(),
        documento: userData.document,
        tipo: userData.userType,
        status: userData.status,
        uid: userData.uid
      };
      if (editingUserId) {
        await usuarioService.update(editingUserId, data);
      } else {
        await usuarioService.create({ ...data, password: userData.password });
      }
      onNavigate('users');
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setIsSaving(false);
    }
  };

  const handleCancel = () => {
    onNavigate('users');
  };

  if (isLoading) return <p role="status" className="text-muted-foreground">Carregando...</p>;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" size="sm" onClick={handleCancel}>
          <ArrowLeft className="h-4 w-4 mr-2" />Voltar
        </Button>
        <div>
          <h1>
            {editingUserId ? `Editar Usuário: ${userData.name}` : 'Adicionar Novo Usuário'}
          </h1>
          <p className="text-muted-foreground">
            {editingUserId ? 'Atualize os dados do usuário' : 'Preencha os dados do novo usuário'}
          </p>
        </div>
      </div>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>Dados do Usuário</CardTitle>
        </CardHeader>
        <CardContent>
          {error && (
            <Alert variant="destructive" className="mb-6">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {success && (
            <Alert className="mb-6 border-green-200 bg-green-50">
              <AlertDescription className="text-green-800">{success}</AlertDescription>
            </Alert>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="name">Nome Completo *</Label>
                <Input
                  id="name"
                  type="text"
                  placeholder="Digite o nome completo"
                  value={userData.name}
                  onChange={(e) => handleInputChange('name', e.target.value)}
                  disabled={isSaving}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">Email *</Label>
                <Input
                  id="email"
                  type="email"
                  required
                  value={userData.email}
                  onChange={(e) => handleInputChange('email', e.target.value)}
                  disabled={isSaving}
                  placeholder="Digite o email"
                />
              </div>

              {!editingUserId && (
                <div className="space-y-2">
                  <Label htmlFor="newPassword">Senha *</Label>
                  <Input
                    id="newPassword"
                    type="password"
                    autoComplete="new-password"
                    required
                    value={userData.password}
                    onChange={(e) => handleInputChange('password', e.target.value)}
                    disabled={isSaving}
                    placeholder="Digite a senha"
                  />
                </div>
              )}

              <div className="space-y-2">
                <Label htmlFor="document">Documento *</Label>
                <Input
                  id="document"
                  type="text"
                  placeholder="CPF, RG ou outro documento"
                  value={userData.document}
                  onChange={(e) => handleInputChange('document', e.target.value)}
                  disabled={isSaving}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="userType">Tipo de Usuário *</Label>
                <Select
                  value={userData.userType}
                  onValueChange={(value) => handleInputChange('userType', value)}
                  disabled={isSaving}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Selecione o tipo de usuário" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="funcionario">Funcionário</SelectItem>
                    <SelectItem value="administrador">Administrador</SelectItem>
                    <SelectItem value="visitante">Visitante</SelectItem>
                    <SelectItem value="terceirizado">Terceirizado</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="border-t pt-6">
              <div className="space-y-4">
                <div>
                  <Label>Cartão RFID</Label>
                  <p className="text-sm text-muted-foreground">
                    Associe um cartão RFID ao usuário para controle de acesso
                  </p>
                </div>

                <div className="flex items-center space-x-4">
                  <div className="flex-1">
                    <Label htmlFor="uid">UID do Cartão</Label>
                    <div className="flex items-center space-x-2 mt-2">
                      <Input
                        id="uid"
                        type="text"
                        value={userData.uid}
                        readOnly
                        placeholder="Nenhum cartão associado"
                        className="bg-muted"
                      />
                      {userData.uid && (
                        <Badge variant="outline" className="bg-green-50 text-green-700 whitespace-nowrap">
                          ✅ Cartão associado
                        </Badge>
                      )}
                    </div>
                  </div>
                </div>

                <Button
                  type="button"
                  variant="outline"
                  onClick={handleReadCard}
                  disabled={isReading || isSaving}
                  className="w-full sm:w-auto"
                >
                  {isReading ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Aguardando aproximação do cartão RFID...
                    </>
                  ) : (
                    <>
                      <CreditCard className="h-4 w-4 mr-2" />
                      Ler Novo Cartão
                    </>
                  )}
                </Button>

                {isReading && (
                  <Alert className="border-blue-200 bg-blue-50">
                    <AlertDescription className="text-blue-800">
                      Aproxime o cartão RFID do leitor para fazer a leitura.
                    </AlertDescription>
                  </Alert>
                )}
              </div>
            </div>

            <div className="flex justify-end space-x-4 pt-6 border-t">
              <Button type="button" variant="outline" onClick={handleCancel} disabled={isSaving}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSaving}>
                {isSaving ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    {editingUserId ? 'Atualizando...' : 'Salvando...'}
                  </>
                ) : (
                  editingUserId ? 'Atualizar' : 'Salvar'
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
