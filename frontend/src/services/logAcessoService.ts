import { ApiService } from './apiClient';

// Enum para tipo de motivo de acesso (deve corresponder ao backend)
export enum TipoMotivoAcesso {
  AcessoAutorizado = 1,
  UidObrigatorio = 2,
  CartaoNaoEncontrado = 3,
  CartaoDesativado = 4,
  UsuarioNaoEncontrado = 5,
  UsuarioDesativado = 6,
  ErroInterno = 7
}

// Interface para dados do log de acesso
export interface LogAcessoDto {
  id: string;
  funcionarioId?: string;
  funcionario?: {
    id: string;
    nome: string;
    email: string;
    tipo: number;
  };
  cartaoId?: string;
  cartao?: {
    id: string;
    uid: string;
    status: number;
  };
  uid?: string;
  motivoTipo: TipoMotivoAcesso;
  motivo: string;
  dataHora: string;
  autorizado: boolean;
}

// Interface para filtros de pesquisa
export interface LogAcessoFilters {
  funcionarioId?: string;
  cartaoId?: string;
  uid?: string;
  motivoTipo?: TipoMotivoAcesso;
  autorizado?: boolean;
  dataInicio?: string;
  dataFim?: string;
  page?: number;
  pageSize?: number;
  orderBy?: string;
  orderDirection?: 'asc' | 'desc';
}

// Interface para resposta paginada
export interface PagedResult<T> {
  items: T[];
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

// Interface para criar log de acesso
export interface CreateLogAcessoDto {
  funcionarioId?: string;
  cartaoId?: string;
  uid?: string;
  motivoTipo: TipoMotivoAcesso;
  motivo: string;
  autorizado: boolean;
}

/**
 * Serviço para gerenciamento de logs de acesso
 */
class LogAcessoService extends ApiService {
  private readonly baseUrl = '/LogAcesso';

  /**
   * Obtém logs de acesso com filtros e paginação
   */
  async getAll(filters?: LogAcessoFilters): Promise<PagedResult<LogAcessoDto>> {
    const params = new URLSearchParams();
    
    if (filters) {
      if (filters.funcionarioId) params.append('funcionarioId', filters.funcionarioId);
      if (filters.cartaoId) params.append('cartaoId', filters.cartaoId);
      if (filters.uid) params.append('uid', filters.uid);
      if (filters.motivoTipo !== undefined) params.append('motivoTipo', filters.motivoTipo.toString());
      if (filters.autorizado !== undefined) params.append('autorizado', filters.autorizado.toString());
      if (filters.dataInicio) params.append('dataInicio', filters.dataInicio);
      if (filters.dataFim) params.append('dataFim', filters.dataFim);
      if (filters.page) params.append('page', filters.page.toString());
      if (filters.pageSize) params.append('pageSize', filters.pageSize.toString());
      if (filters.orderBy) params.append('orderBy', filters.orderBy);
      if (filters.orderDirection) params.append('orderDirection', filters.orderDirection);
    }

    const url = params.toString() ? `${this.baseUrl}?${params.toString()}` : this.baseUrl;
    return this.get<PagedResult<LogAcessoDto>>(url);
  }

  /**
   * Obtém um log de acesso por ID
   */
  async getById(id: string): Promise<LogAcessoDto> {
    return this.get<LogAcessoDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * Obtém logs de acesso por funcionário
   */
  async getByFuncionario(funcionarioId: string, filters?: Omit<LogAcessoFilters, 'funcionarioId'>): Promise<PagedResult<LogAcessoDto>> {
    return this.getAll({ ...filters, funcionarioId });
  }

  /**
   * Obtém logs de acesso por cartão
   */
  async getByCartao(cartaoId: string, filters?: Omit<LogAcessoFilters, 'cartaoId'>): Promise<PagedResult<LogAcessoDto>> {
    return this.getAll({ ...filters, cartaoId });
  }

  /**
   * Obtém logs de acesso por UID
   */
  async getByUid(uid: string, filters?: Omit<LogAcessoFilters, 'uid'>): Promise<PagedResult<LogAcessoDto>> {
    return this.getAll({ ...filters, uid });
  }

  /**
   * Obtém logs de acesso por tipo de motivo
   */
  async getByMotivoTipo(motivoTipo: TipoMotivoAcesso, filters?: Omit<LogAcessoFilters, 'motivoTipo'>): Promise<PagedResult<LogAcessoDto>> {
    return this.getAll({ ...filters, motivoTipo });
  }

  /**
   * Obtém logs de acesso autorizados/negados
   */
  async getByAutorizado(autorizado: boolean, filters?: Omit<LogAcessoFilters, 'autorizado'>): Promise<PagedResult<LogAcessoDto>> {
    return this.getAll({ ...filters, autorizado });
  }

  /**
   * Obtém logs de acesso de hoje
   */
  async getToday(filters?: Omit<LogAcessoFilters, 'dataInicio' | 'dataFim'>): Promise<PagedResult<LogAcessoDto>> {
    const today = new Date().toISOString().split('T')[0];
    return this.getAll({ 
      ...filters, 
      dataInicio: today + 'T00:00:00', 
      dataFim: today + 'T23:59:59' 
    });
  }

  /**
   * Obtém logs de acesso da última semana
   */
  async getLastWeek(filters?: Omit<LogAcessoFilters, 'dataInicio' | 'dataFim'>): Promise<PagedResult<LogAcessoDto>> {
    const hoje = new Date();
    const semanaPassada = new Date(hoje.getTime() - 7 * 24 * 60 * 60 * 1000);
    
    return this.getAll({ 
      ...filters, 
      dataInicio: semanaPassada.toISOString(), 
      dataFim: hoje.toISOString() 
    });
  }

  /**
   * Obtém logs de acesso do último mês
   */
  async getLastMonth(filters?: Omit<LogAcessoFilters, 'dataInicio' | 'dataFim'>): Promise<PagedResult<LogAcessoDto>> {
    const hoje = new Date();
    const mesPassado = new Date(hoje.getFullYear(), hoje.getMonth() - 1, hoje.getDate());
    
    return this.getAll({ 
      ...filters, 
      dataInicio: mesPassado.toISOString(), 
      dataFim: hoje.toISOString() 
    });
  }

  /**
   * Cria um novo log de acesso (geralmente usado pelo sistema automaticamente)
   */
  async create(logAcesso: CreateLogAcessoDto): Promise<LogAcessoDto> {
    return this.post<LogAcessoDto>(this.baseUrl, logAcesso);
  }

  /**
   * Obtém estatísticas de acesso
   */
  async getEstatisticas(dataInicio?: string, dataFim?: string): Promise<{
    totalAcessos: number;
    acessosAutorizados: number;
    acessosNegados: number;
    percentualSucesso: number;
    motivosMaisFrequentes: Array<{ motivo: TipoMotivoAcesso; quantidade: number }>;
  }> {
    const params = new URLSearchParams();
    if (dataInicio) params.append('dataInicio', dataInicio);
    if (dataFim) params.append('dataFim', dataFim);
    
    const url = params.toString() ? `${this.baseUrl}/estatisticas?${params.toString()}` : `${this.baseUrl}/estatisticas`;
    return this.get(url);
  }

  /**
   * Exporta logs para CSV
   */
  async exportToCsv(filters?: LogAcessoFilters): Promise<Blob> {
    const params = new URLSearchParams();
    
    if (filters) {
      if (filters.funcionarioId) params.append('funcionarioId', filters.funcionarioId);
      if (filters.cartaoId) params.append('cartaoId', filters.cartaoId);
      if (filters.uid) params.append('uid', filters.uid);
      if (filters.motivoTipo !== undefined) params.append('motivoTipo', filters.motivoTipo.toString());
      if (filters.autorizado !== undefined) params.append('autorizado', filters.autorizado.toString());
      if (filters.dataInicio) params.append('dataInicio', filters.dataInicio);
      if (filters.dataFim) params.append('dataFim', filters.dataFim);
    }

    const url = params.toString() ? `${this.baseUrl}/export/csv?${params.toString()}` : `${this.baseUrl}/export/csv`;
    const response = await fetch(url, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('access_token')}`,
      },
    });

    if (!response.ok) {
      throw new Error('Erro ao exportar dados');
    }

    return response.blob();
  }
}

/**
 * Utilitários para trabalhar com logs de acesso
 */
export class LogAcessoUtils {
  /**
   * Converte número do enum para TipoMotivoAcesso
   */
  static parseTipoMotivoAcesso(value: number): TipoMotivoAcesso {
    switch (value) {
      case 1:
        return TipoMotivoAcesso.AcessoAutorizado;
      case 2:
        return TipoMotivoAcesso.UidObrigatorio;
      case 3:
        return TipoMotivoAcesso.CartaoNaoEncontrado;
      case 4:
        return TipoMotivoAcesso.CartaoDesativado;
      case 5:
        return TipoMotivoAcesso.UsuarioNaoEncontrado;
      case 6:
        return TipoMotivoAcesso.UsuarioDesativado;
      case 7:
        return TipoMotivoAcesso.ErroInterno;
      default:
        return TipoMotivoAcesso.ErroInterno;
    }
  }

  /**
   * Converte TipoMotivoAcesso para string legível
   */
  static getTipoMotivoAcessoString(tipo: TipoMotivoAcesso): string {
    switch (tipo) {
      case TipoMotivoAcesso.AcessoAutorizado:
        return 'Acesso Autorizado';
      case TipoMotivoAcesso.UidObrigatorio:
        return 'UID Obrigatório';
      case TipoMotivoAcesso.CartaoNaoEncontrado:
        return 'Cartão Não Encontrado';
      case TipoMotivoAcesso.CartaoDesativado:
        return 'Cartão Desativado';
      case TipoMotivoAcesso.UsuarioNaoEncontrado:
        return 'Usuário Não Encontrado';
      case TipoMotivoAcesso.UsuarioDesativado:
        return 'Usuário Desativado';
      case TipoMotivoAcesso.ErroInterno:
        return 'Erro Interno';
      default:
        return 'Erro Interno';
    }
  }

  /**
   * Obtém a cor de badge para um tipo de motivo
   */
  static getTipoMotivoAcessoColor(tipo: TipoMotivoAcesso): string {
    switch (tipo) {
      case TipoMotivoAcesso.AcessoAutorizado:
        return 'bg-green-100 text-green-800';
      case TipoMotivoAcesso.UidObrigatorio:
        return 'bg-yellow-100 text-yellow-800';
      case TipoMotivoAcesso.CartaoNaoEncontrado:
        return 'bg-orange-100 text-orange-800';
      case TipoMotivoAcesso.CartaoDesativado:
        return 'bg-red-100 text-red-800';
      case TipoMotivoAcesso.UsuarioNaoEncontrado:
        return 'bg-purple-100 text-purple-800';
      case TipoMotivoAcesso.UsuarioDesativado:
        return 'bg-red-100 text-red-800';
      case TipoMotivoAcesso.ErroInterno:
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  /**
   * Formata data/hora para exibição
   */
  static formatDateTime(dateTime: string): string {
    return new Date(dateTime).toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  /**
   * Formata apenas a data para exibição
   */
  static formatDate(dateTime: string): string {
    return new Date(dateTime).toLocaleDateString('pt-BR');
  }

  /**
   * Formata apenas a hora para exibição
   */
  static formatTime(dateTime: string): string {
    return new Date(dateTime).toLocaleTimeString('pt-BR');
  }

  /**
   * Obtém as opções de tipo de motivo para filtros
   */
  static getTipoMotivoAcessoOptions() {
    return [
      { value: TipoMotivoAcesso.AcessoAutorizado, label: 'Acesso Autorizado' },
      { value: TipoMotivoAcesso.UidObrigatorio, label: 'UID Obrigatório' },
      { value: TipoMotivoAcesso.CartaoNaoEncontrado, label: 'Cartão Não Encontrado' },
      { value: TipoMotivoAcesso.CartaoDesativado, label: 'Cartão Desativado' },
      { value: TipoMotivoAcesso.UsuarioNaoEncontrado, label: 'Usuário Não Encontrado' },
      { value: TipoMotivoAcesso.UsuarioDesativado, label: 'Usuário Desativado' },
      { value: TipoMotivoAcesso.ErroInterno, label: 'Erro Interno' }
    ];
  }

  /**
   * Obtém texto descritivo baseado no tipo de motivo
   */
  static getTipoMotivoDescricao(tipo: TipoMotivoAcesso): string {
    switch (tipo) {
      case TipoMotivoAcesso.AcessoAutorizado:
        return 'Acesso foi autorizado com sucesso';
      case TipoMotivoAcesso.UidObrigatorio:
        return 'UID é obrigatório para acessar o sistema';
      case TipoMotivoAcesso.CartaoNaoEncontrado:
        return 'Cartão RFID não foi encontrado no sistema';
      case TipoMotivoAcesso.CartaoDesativado:
        return 'Cartão RFID está desativado';
      case TipoMotivoAcesso.UsuarioNaoEncontrado:
        return 'Usuário associado ao cartão não foi encontrado';
      case TipoMotivoAcesso.UsuarioDesativado:
        return 'Usuário associado ao cartão está desativado';
      case TipoMotivoAcesso.ErroInterno:
        return 'Erro interno do sistema durante o processamento';
      default:
        return 'Motivo desconhecido';
    }
  }
}

// Instância singleton do serviço
export const logAcessoService = new LogAcessoService();