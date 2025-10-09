import { ApiService } from './apiClient';

// Interface para dados do cartão (corresponde ao CartaoDto do backend)
export interface Cartao {
  id: string;
  uid: string;
  isActive: boolean;
  portadorId: string;
  nomePortador: string;
}

// Interface para criar cartão (corresponde ao CreateCartaoDto do backend)
export interface CreateCartaoRequest {
  uid: string;
  portadorId: string;
}

// Interface para solicitar gravação de cartão
export interface GravarCartaoRequest {
  portadorId: string;
}

// Interface para confirmar gravação
export interface ConfirmarGravacaoRequest {
  guidGravado: string;
}

// Interface para resposta de gravação
export interface GravacaoResponse {
  message: string;
  guidGerado: string;
  status: string;
}

/**
 * Serviço para gerenciamento de cartões RFID
 */
export class CartaoService extends ApiService {

  /**
   * Obtém todos os cartões
   */
  async getAll(): Promise<Cartao[]> {
    return this.get<Cartao[]>('/Cartao');
  }

  /**
   * Obtém um cartão por ID
   */
  async getById(id: string): Promise<Cartao> {
    return this.get<Cartao>(`/Cartao/${id}`);
  }

  /**
   * Obtém cartão por UID
   */
  async getByUid(uid: string): Promise<Cartao> {
    return this.get<Cartao>(`/Cartao/uid/${uid}`);
  }

  /**
   * Obtém cartões por portador
   */
  async getByPortador(portadorId: string): Promise<Cartao[]> {
    return this.get<Cartao[]>(`/Cartao/portador/${portadorId}`);
  }

  /**
   * Solicita gravação de um novo cartão RFID
   */
  async solicitarGravacao(request: GravarCartaoRequest): Promise<GravacaoResponse> {
    return this.post<GravacaoResponse>('/Cartao', request);
  }

  /**
   * Confirma a gravação de um cartão após Arduino processar
   */
  async confirmarGravacao(request: ConfirmarGravacaoRequest): Promise<{ message: string; cartaoId?: string; uid?: string }> {
    return this.post<{ message: string; cartaoId?: string; uid?: string }>('/Cartao/confirmar-gravacao', request);
  }

  /**
   * Atualiza um cartão existente
   */
  async update(id: string, cartao: CreateCartaoRequest): Promise<Cartao> {
    return this.put<Cartao>(`/Cartao/${id}`, cartao);
  }

  /**
   * Remove um cartão
   */
  async deleteCartao(id: string): Promise<{ message: string }> {
    return this.delete<{ message: string }>(`/Cartao/${id}`);
  }

  /**
   * Ativa um cartão
   */
  async ativar(id: string): Promise<{ message: string }> {
    return this.put<{ message: string }>(`/Cartao/${id}/ativar`, {});
  }

  /**
   * Desativa um cartão (exclusão lógica)
   */
  async desativar(id: string): Promise<{ message: string }> {
    return this.delete<{ message: string }>(`/Cartao/${id}`);
  }
}

/**
 * Utilitários para trabalhar com cartões
 */
export class CartaoUtils {
  /**
   * Obtém a cor de badge para o status ativo/inativo
   */
  static getStatusColor(isActive: boolean): string {
    return isActive 
      ? 'bg-green-100 text-green-800' 
      : 'bg-gray-100 text-gray-800';
  }

  /**
   * Obtém o texto do status
   */
  static getStatusText(isActive: boolean): string {
    return isActive ? 'Ativo' : 'Inativo';
  }

  /**
   * Verifica se um cartão pode ser ativado
   */
  static canActivate(isActive: boolean): boolean {
    return !isActive;
  }

  /**
   * Verifica se um cartão pode ser desativado
   */
  static canDeactivate(isActive: boolean): boolean {
    return isActive;
  }
}

// Instância singleton do serviço
export const cartaoService = new CartaoService();