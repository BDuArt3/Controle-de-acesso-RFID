import { ApiService } from './apiClient';

/**
 * Serviço de Log de Acesso - Sistema de Controle de Acesso Universitário
 * 
 * Este serviço gerencia os logs de entrada e saída da universidade através
 * de cartões RFID. O sistema tem um único ponto de controle (portaria da 
 * universidade) onde são registradas todas as tentativas de acesso.
 * 
 * Funcionalidades:
 * - Registro automático de tentativas de acesso
 * - Consulta de histórico de movimentação
 * - Relatórios e estatísticas de acesso
 * - Filtros por período, portador e tipo de resultado
 * 
 * @author Sistema de Controle de Acesso RFID
 * @version 2.0
 */

// Enum para tipo de motivo de acesso (deve corresponder ao backend)
export enum TipoMotivoAcesso {
  AcessoAutorizado = 1,
  UidObrigatorio = 2,
  CartaoNaoEncontrado = 3,
  CartaoDesativado = 4,
  PortadorNaoEncontrado = 5,
  PortadorDesativado = 6,
  ErroInterno = 7
}

// Interface para dados do log de acesso
export interface LogAcessoDto {
  id: number;
  dataHora: string;
  cartaoUid: string;
  acessoPermitido: boolean;
  motivo: string;
  motivoTipo: TipoMotivoAcesso;
  portadorNome?: string;
  portadorTipo?: string;
}

// Interface para filtros de pesquisa
export interface LogAcessoFilters {
  cartaoUid?: string;
  motivoTipo?: TipoMotivoAcesso;
  acessoPermitido?: boolean;
  dataInicio?: string;
  dataFim?: string;
  quantidade?: number;
}

// Interface para criar log de acesso
export interface CreateLogAcessoDto {
  cartaoUid: string;
  acessoPermitido: boolean;
  motivo: string;
  motivoTipo: TipoMotivoAcesso;
}

// Interface para estatísticas do dia
export interface EstatisticasHojeDto {
  totalAcessos: number;
  acessosPermitidos: number;
  acessosNegados: number;
  percentualSucesso: number;
  motivosMaisFrequentes: Array<{
    motivo: TipoMotivoAcesso;
    quantidade: number;
  }>;
}

// Interface para relatório diário
export interface RelatorioDiarioDto {
  data: string;
  totalAcessos: number;
  acessosPermitidos: number;
  acessosNegados: number;
  percentualSucesso: number;
  primeiroAcesso?: string;
  ultimoAcesso?: string;
  horariosPico: Array<{
    hora: string;
    quantidade: number;
  }>;
  portadorsMaisAtivos: Array<{
    portadorNome: string;
    quantidade: number;
  }>;
  motivosDetalheados: Array<{
    motivo: TipoMotivoAcesso;
    quantidade: number;
    percentual: number;
  }>;
}

/**
 * Serviço para gerenciamento de logs de acesso
 */
class LogAcessoService extends ApiService {
  private readonly baseUrl = '/LogAcesso';

  /**
   * Registra uma tentativa de acesso no sistema
   */
  async registrarTentativa(createLogAcessoDto: CreateLogAcessoDto): Promise<LogAcessoDto> {
    return this.post<LogAcessoDto>(this.baseUrl, createLogAcessoDto);
  }

  /**
   * Obtém um log de acesso específico pelo ID
   */
  async getById(id: number): Promise<LogAcessoDto> {
    return this.get<LogAcessoDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * Obtém todos os logs de acesso
   */
  async getAll(): Promise<LogAcessoDto[]> {
    return this.get<LogAcessoDto[]>(this.baseUrl);
  }

  /**
   * Obtém logs de acesso por UID do cartão
   */
  async getByCartaoUid(uid: string): Promise<LogAcessoDto[]> {
    return this.get<LogAcessoDto[]>(`${this.baseUrl}/cartao/${encodeURIComponent(uid)}`);
  }

  /**
   * Obtém logs de acesso por período
   */
  async getByPeriodo(dataInicio: Date, dataFim: Date): Promise<LogAcessoDto[]> {
    const params = new URLSearchParams();
    params.append('dataInicio', dataInicio.toISOString());
    params.append('dataFim', dataFim.toISOString());

    return this.get<LogAcessoDto[]>(`${this.baseUrl}/periodo?${params.toString()}`);
  }

  /**
   * Obtém apenas acessos permitidos por período
   */
  async getAcessosPermitidos(dataInicio: Date, dataFim: Date): Promise<LogAcessoDto[]> {
    const params = new URLSearchParams();
    params.append('dataInicio', dataInicio.toISOString());
    params.append('dataFim', dataFim.toISOString());

    return this.get<LogAcessoDto[]>(`${this.baseUrl}/periodo/permitidos?${params.toString()}`);
  }

  /**
   * Obtém apenas acessos negados por período
   */
  async getAcessosNegados(dataInicio: Date, dataFim: Date): Promise<LogAcessoDto[]> {
    const params = new URLSearchParams();
    params.append('dataInicio', dataInicio.toISOString());
    params.append('dataFim', dataFim.toISOString());

    return this.get<LogAcessoDto[]>(`${this.baseUrl}/periodo/negados?${params.toString()}`);
  }

  /**
   * Obtém os últimos acessos registrados
   */
  async getUltimos(quantidade: number = 50): Promise<LogAcessoDto[]> {
    const params = new URLSearchParams();
    params.append('quantidade', quantidade.toString());

    return this.get<LogAcessoDto[]>(`${this.baseUrl}/ultimos?${params.toString()}`);
  }

  /**
   * Obtém estatísticas de acesso do dia atual
   */
  async getEstatisticasHoje(): Promise<EstatisticasHojeDto> {
    return this.get<EstatisticasHojeDto>(`${this.baseUrl}/estatisticas/hoje`);
  }

  /**
   * Obtém relatório completo de acessos para uma data específica
   */
  async getRelatorioDiario(data: Date): Promise<RelatorioDiarioDto> {
    const params = new URLSearchParams();
    params.append('data', data.toISOString().split('T')[0]);

    return this.get<RelatorioDiarioDto>(`${this.baseUrl}/relatorio/diario?${params.toString()}`);
  }

  /**
   * Métodos de conveniência para datas específicas
   */

  /**
   * Obtém logs de acesso de hoje
   */
  async getHoje(): Promise<LogAcessoDto[]> {
    const hoje = new Date();
    const inicioDia = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate());
    const fimDia = new Date(inicioDia.getTime() + 24 * 60 * 60 * 1000 - 1);
    
    return this.getByPeriodo(inicioDia, fimDia);
  }

  /**
   * Obtém logs de acesso da última semana
   */
  async getUltimaSemana(): Promise<LogAcessoDto[]> {
    const hoje = new Date();
    const semanaPassada = new Date(hoje.getTime() - 7 * 24 * 60 * 60 * 1000);
    
    return this.getByPeriodo(semanaPassada, hoje);
  }

  /**
   * Obtém logs de acesso do último mês
   */
  async getUltimoMes(): Promise<LogAcessoDto[]> {
    const hoje = new Date();
    const mesPassado = new Date(hoje.getFullYear(), hoje.getMonth() - 1, hoje.getDate());
    
    return this.getByPeriodo(mesPassado, hoje);
  }

  /**
   * Métodos específicos para controle de acesso universitário
   */

  /**
   * Obtém entradas autorizadas de hoje
   */
  async getEntradasHoje(): Promise<LogAcessoDto[]> {
    const hoje = new Date();
    const inicioDia = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate());
    const fimDia = new Date(inicioDia.getTime() + 24 * 60 * 60 * 1000 - 1);
    
    return this.getAcessosPermitidos(inicioDia, fimDia);
  }

  /**
   * Obtém tentativas de acesso negadas de hoje
   */
  async getTentativasNegadasHoje(): Promise<LogAcessoDto[]> {
    const hoje = new Date();
    const inicioDia = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate());
    const fimDia = new Date(inicioDia.getTime() + 24 * 60 * 60 * 1000 - 1);
    
    return this.getAcessosNegados(inicioDia, fimDia);
  }

  /**
   * Obtém histórico completo de um portador por UID do cartão
   */
  async getHistoricoPortador(cartaoUid: string): Promise<LogAcessoDto[]> {
    return this.getByCartaoUid(cartaoUid);
  }

  /**
   * Obtém resumo de atividade da universidade (últimas 24 horas)
   */
  async getResumoAtividade(): Promise<{
    totalMovimentacao: number;
    entradasAutorizadas: number;
    tentativasNegadas: number;
    ultimaAtividade?: LogAcessoDto;
  }> {
    const ultimasAtividades = await this.getUltimos(1);
    const atividadeHoje = await this.getHoje();
    
    const entradasAutorizadas = atividadeHoje.filter(log => log.acessoPermitido).length;
    const tentativasNegadas = atividadeHoje.filter(log => !log.acessoPermitido).length;
    
    return {
      totalMovimentacao: atividadeHoje.length,
      entradasAutorizadas,
      tentativasNegadas,
      ultimaAtividade: ultimasAtividades[0] || undefined
    };
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
        return TipoMotivoAcesso.PortadorNaoEncontrado;
      case 6:
        return TipoMotivoAcesso.PortadorDesativado;
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
        return 'Cartão Não Cadastrado';
      case TipoMotivoAcesso.CartaoDesativado:
        return 'Cartão Desativado';
      case TipoMotivoAcesso.PortadorNaoEncontrado:
        return 'Portador Não Encontrado';
      case TipoMotivoAcesso.PortadorDesativado:
        return 'Portador Desativado';
      case TipoMotivoAcesso.ErroInterno:
        return 'Erro do Sistema';
      default:
        return 'Erro do Sistema';
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
      case TipoMotivoAcesso.PortadorNaoEncontrado:
        return 'bg-purple-100 text-purple-800';
      case TipoMotivoAcesso.PortadorDesativado:
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
      { value: TipoMotivoAcesso.CartaoNaoEncontrado, label: 'Cartão Não Cadastrado' },
      { value: TipoMotivoAcesso.CartaoDesativado, label: 'Cartão Desativado' },
      { value: TipoMotivoAcesso.PortadorNaoEncontrado, label: 'Portador Não Encontrado' },
      { value: TipoMotivoAcesso.PortadorDesativado, label: 'Portador Desativado' },
      { value: TipoMotivoAcesso.ErroInterno, label: 'Erro do Sistema' }
    ];
  }

  /**
   * Obtém texto descritivo baseado no tipo de motivo
   */
  static getTipoMotivoDescricao(tipo: TipoMotivoAcesso): string {
    switch (tipo) {
      case TipoMotivoAcesso.AcessoAutorizado:
        return 'Entrada/saída autorizada na universidade';
      case TipoMotivoAcesso.UidObrigatorio:
        return 'UID é obrigatório para acessar a universidade';
      case TipoMotivoAcesso.CartaoNaoEncontrado:
        return 'Cartão RFID não cadastrado no sistema da universidade';
      case TipoMotivoAcesso.CartaoDesativado:
        return 'Cartão RFID desativado - acesso negado à universidade';
      case TipoMotivoAcesso.PortadorNaoEncontrado:
        return 'Portador não encontrado no sistema da universidade';
      case TipoMotivoAcesso.PortadorDesativado:
        return 'Portador desativado - acesso negado à universidade';
      case TipoMotivoAcesso.ErroInterno:
        return 'Erro interno do sistema de controle de acesso';
      default:
        return 'Motivo desconhecido';
    }
  }

  /**
   * Determina se um acesso é permitido baseado no tipo de motivo
   */
  static isAcessoPermitido(tipo: TipoMotivoAcesso): boolean {
    return tipo === TipoMotivoAcesso.AcessoAutorizado;
  }

  /**
   * Obtém ícone para o tipo de motivo
   */
  static getTipoMotivoIcon(tipo: TipoMotivoAcesso): string {
    switch (tipo) {
      case TipoMotivoAcesso.AcessoAutorizado:
        return '✅';
      case TipoMotivoAcesso.UidObrigatorio:
        return '⚠️';
      case TipoMotivoAcesso.CartaoNaoEncontrado:
        return '🔍';
      case TipoMotivoAcesso.CartaoDesativado:
        return '🚫';
      case TipoMotivoAcesso.PortadorNaoEncontrado:
        return '👤';
      case TipoMotivoAcesso.PortadorDesativado:
        return '🔒';
      case TipoMotivoAcesso.ErroInterno:
        return '❌';
      default:
        return '❓';
    }
  }
}

// Instância singleton do serviço
export const logAcessoService = new LogAcessoService();