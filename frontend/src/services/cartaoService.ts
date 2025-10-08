import { ApiService } from './apiClient';

// Enum para status do cartão
export enum StatusCartao {
  Ativo = 1,
  Inativo = 2,
  Bloqueado = 3
}

// Interface para dados do cartão
export interface CartaoDto {
  id: string;
  uid: string;
  funcionarioId: string;
  funcionario?: {
    id: string;
    nome: string;
    email: string;
    tipo: number;
  };
  status: StatusCartao;
  dataUltilizacao?: string;
  dataCriacao: string;
}

// Interface para criar cartão
export interface CreateCartaoDto {
  uid: string;
  funcionarioId: string;
  status?: StatusCartao;
}

// Interface para atualizar cartão
export interface UpdateCartaoDto {
  uid?: string;
  funcionarioId?: string;
  status?: StatusCartao;
}

/**
 * Serviço para gerenciamento de cartões RFID
 */
class CartaoService extends ApiService {
  private readonly baseUrl = '/Cartao';

  /**
   * Obtém todos os cartões
   */
  async getAll(): Promise<CartaoDto[]> {
    return this.get<CartaoDto[]>(this.baseUrl);
  }

  /**
   * Obtém um cartão por ID
   */
  async getById(id: string): Promise<CartaoDto> {
    return this.get<CartaoDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * Obtém cartão por UID
   */
  async getByUid(uid: string): Promise<CartaoDto> {
    return this.get<CartaoDto>(`${this.baseUrl}/uid/${uid}`);
  }

  /**
   * Obtém cartões por funcionário
   */
  async getByFuncionario(funcionarioId: string): Promise<CartaoDto[]> {
    return this.get<CartaoDto[]>(`${this.baseUrl}/funcionario/${funcionarioId}`);
  }

  /**
   * Obtém cartões por status
   */
  async getByStatus(status: StatusCartao): Promise<CartaoDto[]> {
    return this.get<CartaoDto[]>(`${this.baseUrl}/status/${status}`);
  }

  /**
   * Cria um novo cartão
   */
  async create(cartao: CreateCartaoDto): Promise<CartaoDto> {
    return this.post<CartaoDto>(this.baseUrl, cartao);
  }

  /**
   * Atualiza um cartão
   */
  async update(id: string, cartao: UpdateCartaoDto): Promise<CartaoDto> {
    return this.put<CartaoDto>(`${this.baseUrl}/${id}`, cartao);
  }

  /**
   * Remove um cartão
   */
  async deleteCartao(id: string): Promise<void> {
    return this.delete(`${this.baseUrl}/${id}`);
  }

  /**
   * Ativa um cartão
   */
  async ativar(id: string): Promise<void> {
    return this.patch(`${this.baseUrl}/${id}/ativar`, {});
  }

  /**
   * Desativa um cartão
   */
  async desativar(id: string): Promise<void> {
    return this.patch(`${this.baseUrl}/${id}/desativar`, {});
  }

  /**
   * Bloqueia um cartão
   */
  async bloquear(id: string): Promise<void> {
    return this.patch(`${this.baseUrl}/${id}/bloquear`, {});
  }

  /**
   * Valida se um UID é único
   */
  async isUidUnique(uid: string, excludeId?: string): Promise<boolean> {
    const params = excludeId ? `?excludeId=${excludeId}` : '';
    return this.get<boolean>(`${this.baseUrl}/validate-uid/${uid}${params}`);
  }
}

/**
 * Utilitários para trabalhar com status de cartão
 */
export class CartaoUtils {
  /**
   * Converte número do enum para StatusCartao
   */
  static parseStatusCartao(value: number): StatusCartao {
    switch (value) {
      case 1:
        return StatusCartao.Ativo;
      case 2:
        return StatusCartao.Inativo;
      case 3:
        return StatusCartao.Bloqueado;
      default:
        return StatusCartao.Inativo;
    }
  }

  /**
   * Converte StatusCartao para string legível
   */
  static getStatusCartaoString(status: StatusCartao): string {
    switch (status) {
      case StatusCartao.Ativo:
        return 'Ativo';
      case StatusCartao.Inativo:
        return 'Inativo';
      case StatusCartao.Bloqueado:
        return 'Bloqueado';
      default:
        return 'Inativo';
    }
  }

  /**
   * Obtém a cor de badge para um status
   */
  static getStatusCartaoColor(status: StatusCartao): string {
    switch (status) {
      case StatusCartao.Ativo:
        return 'bg-green-100 text-green-800';
      case StatusCartao.Inativo:
        return 'bg-gray-100 text-gray-800';
      case StatusCartao.Bloqueado:
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  /**
   * Obtém as opções de status para formulários
   */
  static getStatusCartaoOptions() {
    return [
      { value: StatusCartao.Ativo, label: 'Ativo' },
      { value: StatusCartao.Inativo, label: 'Inativo' },
      { value: StatusCartao.Bloqueado, label: 'Bloqueado' }
    ];
  }

  /**
   * Verifica se um cartão pode ser ativado
   */
  static canActivate(status: StatusCartao): boolean {
    return status === StatusCartao.Inativo || status === StatusCartao.Bloqueado;
  }

  /**
   * Verifica se um cartão pode ser desativado
   */
  static canDeactivate(status: StatusCartao): boolean {
    return status === StatusCartao.Ativo;
  }

  /**
   * Verifica se um cartão pode ser bloqueado
   */
  static canBlock(status: StatusCartao): boolean {
    return status === StatusCartao.Ativo;
  }
}

// Instância singleton do serviço
export const cartaoService = new CartaoService();