import React, { useEffect, useMemo, useState } from 'react';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { errorMessage, normalizeEmail, type UsuarioDto, usuarioService } from '../services/api';
import { Alert, AlertDescription } from './ui/alert';
import { Badge } from './ui/badge';
import { Button } from './ui/button';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';

interface UserFormProps {
  editingUserId?: string;
  onNavigate: (route: any) => void;
}

interface UserFormState {
  nome: string;
  email: string;
  documento: string;
  tipo: string;
}

const emptyForm: UserFormState = {
  nome: '',
  email: '',
  documento: '',
  tipo: '',
};

const userTypeOptions = [
  { value: '0', label: 'Aluno' },
  { value: '1', label: 'Professor' },
  { value: '2', label: 'Visitante' },
  { value: '3', label: 'Colaborador' },
];

function mapUserType(value: UsuarioDto['tipo']) {
  const option = userTypeOptions.find(item => Number(item.value) === Number(value));
  return option?.label ?? '—';
}

export default function UserForm({ editingUserId, onNavigate }: UserFormProps) {
  const { user: currentUser } = useAuth();
  const [form, setForm] = useState<UserFormState>(emptyForm);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [existingUser, setExistingUser] = useState<UsuarioDto | null>(null);

  const canEdit = useMemo(() => currentUser?.role === 'Admin', [currentUser?.role]);
  const canCreate = useMemo(() => currentUser?.role === 'Admin' || currentUser?.role === 'Operador', [currentUser?.role]);

  useEffect(() => {
    let active = true;

    if (!editingUserId) {
      setExistingUser(null);
      setForm(emptyForm);
      return () => {
        active = false;
      };
    }

    setIsLoading(true);
    const load = async () => {
      try {
        const { data } = await usuarioService.get(editingUserId);
        if (!active) {
          return;
        }
        setExistingUser(data);
        setForm({
          nome: data.nome,
          email: data.email || '',
          documento: data.documento,
          tipo: String(data.tipo),
        });
      } catch (err) {
        if (active) {
          setError(errorMessage(err));
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    };

    void load();
    return () => {
      active = false;
    };
  }, [editingUserId]);

  const updateField = (field: keyof UserFormState, value: string) => {
    setForm(previous => ({ ...previous, [field]: value }));
    setError('');
    setSuccess('');
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');

    if (editingUserId ? !canEdit : !canCreate) {
      setError('Você não tem permissão para executar esta ação.');
      return;
    }

    if (!form.nome.trim() || !form.documento.trim() || !form.tipo) {
      setError('Preencha nome, documento e tipo.');
      return;
    }

    if (!form.email.trim()) {
      setError('Informe o email.');
      return;
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
      setError('Informe um email válido.');
      return;
    }

    const payload = {
      nome: form.nome.trim(),
      email: normalizeEmail(form.email),
      documento: form.documento.trim(),
      tipo: Number(form.tipo) as UsuarioDto['tipo'],
    };

    setIsSaving(true);
    try {
      if (editingUserId) {
        await usuarioService.update(editingUserId, payload);
        setSuccess('Usuário atualizado com sucesso.');
      } else {
        await usuarioService.create(payload);
        setSuccess('Usuário cadastrado com sucesso.');
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

  if (isLoading) {
    return <p role="status" className="text-muted-foreground">Carregando...</p>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" size="sm" onClick={handleCancel}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Voltar
        </Button>
        <div>
          <h1>{editingUserId ? `Editar Usuário: ${form.nome}` : 'Adicionar Novo Usuário'}</h1>
          <p className="text-muted-foreground">
            {editingUserId ? 'Atualize os dados cadastrais do usuário RFID.' : 'Cadastre uma nova pessoa autorizada.'}
          </p>
        </div>
      </div>

      <Card className="max-w-2xl">
        <CardHeader className="space-y-2">
          <CardTitle>Dados do Usuário</CardTitle>
          {existingUser && (
            <Badge variant={existingUser.isActive ? 'default' : 'destructive'} className={existingUser.isActive ? 'bg-green-100 text-green-800' : ''}>
              {existingUser.isActive ? 'Ativo' : 'Inativo'}
            </Badge>
          )}
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
                <Label htmlFor="nome">Nome Completo *</Label>
                <Input
                  id="nome"
                  type="text"
                  placeholder="Digite o nome completo"
                  value={form.nome}
                  onChange={(event) => updateField('nome', event.target.value)}
                  disabled={isSaving}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">Email *</Label>
                <Input
                  id="email"
                  type="email"
                                  required
                                  placeholder="nome@dominio.com"
                  value={form.email}
                  onChange={(event) => updateField('email', event.target.value)}
                  disabled={isSaving}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="documento">Documento *</Label>
                <Input
                  id="documento"
                  type="text"
                  placeholder="CPF, RG ou outro documento"
                  value={form.documento}
                  onChange={(event) => updateField('documento', event.target.value)}
                  disabled={isSaving}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="tipo">Tipo *</Label>
                <Select
                  value={form.tipo}
                  onValueChange={(value) => updateField('tipo', value)}
                  disabled={isSaving}
                >
                  <SelectTrigger id="tipo">
                    <SelectValue placeholder="Selecione o tipo" />
                  </SelectTrigger>
                  <SelectContent>
                    {userTypeOptions.map(option => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="flex justify-end space-x-4 pt-6 border-t">
              <Button type="button" variant="outline" onClick={handleCancel} disabled={isSaving}>
                Cancelar
              </Button>
              <Button type="submit" disabled={isSaving || (!editingUserId && !canCreate) || (editingUserId && !canEdit)}>
                {isSaving ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    {editingUserId ? 'Atualizando...' : 'Salvando...'}
                  </>
                ) : (
                  editingUserId ? 'Atualizar' : 'Salvar'
                )}
              </Button>
            </div>
            {editingUserId && !canEdit && (
              <p className="text-sm text-muted-foreground">
                Somente administradores podem atualizar usuários existentes.
              </p>
            )}
          </form>
        </CardContent>
      </Card>
      <div className="max-w-2xl text-sm text-muted-foreground">
        Tipo atual: {editingUserId ? mapUserType(existingUser?.tipo ?? Number(form.tipo)) : 'Novo cadastro'}
      </div>
    </div>
  );
}
