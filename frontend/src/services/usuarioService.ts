import { ApiService } from './apiClient';

// Enum para tipos de usuário (deve coincidir com o backend)
export enum TipoUsuario {
  Aluno = 0,
  Professor = 1,
  Visitante = 2,
  Funcionario = 3
}

// Interfaces para DTOs
export interface UsuarioDto {
  id: string;
  nome: string;
  tipo: TipoUsuario;
  isActive: boolean;
  cartoes: Array<{
    id: string;
    uid: string;
    isActive: boolean;
  }>;
}

export interface CreateUsuarioDto {
  nome: string;
  tipo: TipoUsuario;
}

export interface UpdateUsuarioDto {
  nome: string;
  tipo: TipoUsuario;
}

// Service para gerenciar usuários
export class UsuarioService extends ApiService {
  private readonly baseUrl = '/usuario';

  /**
   * Cria um novo usuário
   */
  async create(createUsuarioDto: CreateUsuarioDto): Promise<UsuarioDto> {
    const response = await this.post<UsuarioDto>(this.baseUrl, createUsuarioDto);
    return response;
  }

  /**
   * Busca um usuário por ID
   */
  async getById(id: string): Promise<UsuarioDto> {
    const response = await this.get<UsuarioDto>(`${this.baseUrl}/${id}`);
    return response;
  }

  /**
   * Lista todos os usuários ativos
   */
  async getAll(): Promise<UsuarioDto[]> {
    const response = await this.get<UsuarioDto[]>(this.baseUrl);
    return response;
  }

  /**
   * Atualiza um usuário existente
   */
  async update(id: string, updateUsuarioDto: UpdateUsuarioDto): Promise<UsuarioDto> {
    const response = await this.put<UsuarioDto>(`${this.baseUrl}/${id}`, updateUsuarioDto);
    return response;
  }

  /**
   * Desativa um usuário (exclusão lógica)
   */
  async deleteUsuario(id: string): Promise<void> {
    return this.delete(`${this.baseUrl}/${id}`);
  }

  /**
   * Reativa um usuário
   */
  async activate(id: string): Promise<void> {
    await this.put<void>(`${this.baseUrl}/${id}/ativar`, {});
  }

  /**
   * Converte string do enum para número (para compatibilidade)
   */
  static getTipoUsuarioFromString(tipo: string): TipoUsuario {
    switch (tipo.toLowerCase()) {
      case 'aluno':
        return TipoUsuario.Aluno;
      case 'professor':
        return TipoUsuario.Professor;
      case 'visitante':
        return TipoUsuario.Visitante;
      case 'funcionario':
        return TipoUsuario.Funcionario;
      default:
        return TipoUsuario.Aluno;
    }
  }

  /**
   * Converte número do enum para string legível
   */
  static getTipoUsuarioString(tipo: TipoUsuario): string {
    switch (tipo) {
      case TipoUsuario.Aluno:
        return 'Aluno';
      case TipoUsuario.Professor:
        return 'Professor';
      case TipoUsuario.Visitante:
        return 'Visitante';
      case TipoUsuario.Funcionario:
        return 'Funcionário';
      default:
        return 'Aluno';
    }
  }

  /**
   * Obtém as opções de tipo de usuário para formulários
   */
  static getTipoUsuarioOptions() {
    return [
      { value: TipoUsuario.Aluno, label: 'Aluno' },
      { value: TipoUsuario.Professor, label: 'Professor' },
      { value: TipoUsuario.Visitante, label: 'Visitante' },
      { value: TipoUsuario.Funcionario, label: 'Funcionário' }
    ];
  }
}

// Instância singleton do serviço
export const usuarioService = new UsuarioService();
