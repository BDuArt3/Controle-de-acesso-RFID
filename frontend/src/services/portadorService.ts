import { ApiService } from './apiClient';

// Enum para tipos de portador (deve coincidir com o backend)
export enum TipoPortador {
  Aluno = 0,
  Professor = 1,
  Visitante = 2,
  Funcionario = 3
}

// Interfaces para DTOs
export interface PortadorDto {
  id: string;
  nome: string;
  tipo: TipoPortador;
  isActive: boolean;
  cartoes: Array<{
    id: string;
    uid: string;
    isActive: boolean;
  }>;
}

export interface CreatePortadorDto {
  nome: string;
  tipo: TipoPortador;
}

export interface UpdatePortadorDto {
  nome: string;
  tipo: TipoPortador;
}

// Service para gerenciar portadors
export class PortadorService extends ApiService {
  private readonly baseUrl = '/portador';

  /**
   * Cria um novo portador
   */
  async create(createPortadorDto: CreatePortadorDto): Promise<PortadorDto> {
    const response = await this.post<PortadorDto>(this.baseUrl, createPortadorDto);
    return response;
  }

  /**
   * Busca um portador por ID
   */
  async getById(id: string): Promise<PortadorDto> {
    const response = await this.get<PortadorDto>(`${this.baseUrl}/${id}`);
    return response;
  }

  /**
   * Lista todos os portadors ativos
   */
  async getAll(): Promise<PortadorDto[]> {
    const response = await this.get<PortadorDto[]>(this.baseUrl);
    return response;
  }

  /**
   * Atualiza um portador existente
   */
  async update(id: string, updatePortadorDto: UpdatePortadorDto): Promise<PortadorDto> {
    const response = await this.put<PortadorDto>(`${this.baseUrl}/${id}`, updatePortadorDto);
    return response;
  }

  /**
   * Desativa um portador (exclusão lógica)
   */
  async deletePortador(id: string): Promise<void> {
    return this.delete(`${this.baseUrl}/${id}`);
  }

  /**
   * Reativa um portador
   */
  async activate(id: string): Promise<void> {
    await this.put<void>(`${this.baseUrl}/${id}/ativar`, {});
  }

  /**
   * Converte string do enum para número (para compatibilidade)
   */
  static getTipoPortadorFromString(tipo: string): TipoPortador {
    switch (tipo.toLowerCase()) {
      case 'aluno':
        return TipoPortador.Aluno;
      case 'professor':
        return TipoPortador.Professor;
      case 'visitante':
        return TipoPortador.Visitante;
      case 'funcionario':
        return TipoPortador.Funcionario;
      default:
        return TipoPortador.Aluno;
    }
  }

  /**
   * Converte número do enum para string legível
   */
  static getTipoPortadorString(tipo: TipoPortador): string {
    switch (tipo) {
      case TipoPortador.Aluno:
        return 'Aluno';
      case TipoPortador.Professor:
        return 'Professor';
      case TipoPortador.Visitante:
        return 'Visitante';
      case TipoPortador.Funcionario:
        return 'Funcionário';
      default:
        return 'Aluno';
    }
  }

  /**
   * Obtém as opções de tipo de portador para formulários
   */
  static getTipoPortadorOptions() {
    return [
      { value: TipoPortador.Aluno, label: 'Aluno' },
      { value: TipoPortador.Professor, label: 'Professor' },
      { value: TipoPortador.Visitante, label: 'Visitante' },
      { value: TipoPortador.Funcionario, label: 'Funcionário' }
    ];
  }
}

// Instância singleton do serviço
export const portadorService = new PortadorService();
