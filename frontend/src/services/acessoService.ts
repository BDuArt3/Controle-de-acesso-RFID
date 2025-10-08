import { ApiService } from './apiClient';
import { TipoMotivoAcesso } from './logAcessoService';

// Interface para resposta de verificação de acesso
export interface VerificacaoAcessoResponse {
  autorizado: boolean;
  motivo: string;
  motivoTipo: TipoMotivoAcesso;
  funcionario?: {
    id: string;
    nome: string;
    email: string;
    tipo: number;
  };
  cartao?: {
    id: string;
    uid: string;
    status: number;
  };
  logId: string;
  dataHora: string;
}

// Interface para dados de tentativa de acesso
export interface TentativaAcessoDto {
  uid: string;
  tentativaManual?: boolean;
  origem?: string;
}

// Interface para estatísticas de acesso em tempo real
export interface EstatisticasAcesso {
  acessosHoje: number;
  acessosAutorizadosHoje: number;
  acessosNegadosHoje: number;
  percentualSucessoHoje: number;
  ultimosAcessos: Array<{
    id: string;
    funcionarioNome?: string;
    uid: string;
    autorizado: boolean;
    dataHora: string;
    motivo: string;
  }>;
  acessosPorHora: Array<{
    hora: number;
    quantidade: number;
  }>;
}

// Interface para configurações do sistema de acesso
export interface ConfiguracaoAcesso {
  permitirAcessoSemCartao: boolean;
  logNivelDetalhamento: 'basico' | 'detalhado' | 'completo';
  timeoutLeituraRfid: number;
  intervaloMinimoBetweenTentativas: number;
  maximoTentativasSequenciais: number;
  notificarAcessosNegados: boolean;
  emailNotificacao?: string;
}

/**
 * Serviço para operações de controle de acesso RFID
 */
class AcessoService extends ApiService {
  private readonly baseUrl = '/Acesso';

  /**
   * Verifica acesso através de UID RFID
   */
  async verificarAcesso(tentativa: TentativaAcessoDto): Promise<VerificacaoAcessoResponse> {
    return this.post<VerificacaoAcessoResponse>(`${this.baseUrl}/verificar`, tentativa);
  }

  /**
   * Simula uma tentativa de acesso (para testes)
   */
  async simularAcesso(uid: string): Promise<VerificacaoAcessoResponse> {
    return this.post<VerificacaoAcessoResponse>(`${this.baseUrl}/simular`, { 
      uid, 
      tentativaManual: true,
      origem: 'interface-web' 
    });
  }

  /**
   * Obtém estatísticas de acesso em tempo real
   */
  async getEstatisticas(): Promise<EstatisticasAcesso> {
    return this.get<EstatisticasAcesso>(`${this.baseUrl}/estatisticas`);
  }

  /**
   * Obtém estatísticas de acesso para um período específico
   */
  async getEstatisticasPeriodo(dataInicio: string, dataFim: string): Promise<EstatisticasAcesso> {
    const params = new URLSearchParams({
      dataInicio,
      dataFim
    });
    
    return this.get<EstatisticasAcesso>(`${this.baseUrl}/estatisticas?${params.toString()}`);
  }

  /**
   * Obtém os últimos acessos (em tempo real)
   */
  async getUltimosAcessos(quantidade: number = 10): Promise<VerificacaoAcessoResponse[]> {
    return this.get<VerificacaoAcessoResponse[]>(`${this.baseUrl}/ultimos?quantidade=${quantidade}`);
  }

  /**
   * Obtém acessos em andamento (para monitoramento)
   */
  async getAcessosEmAndamento(): Promise<Array<{
    uid: string;
    inicioTentativa: string;
    origem: string;
  }>> {
    return this.get(`${this.baseUrl}/em-andamento`);
  }

  /**
   * Obtém configurações do sistema de acesso
   */
  async getConfiguracoes(): Promise<ConfiguracaoAcesso> {
    return this.get<ConfiguracaoAcesso>(`${this.baseUrl}/configuracoes`);
  }

  /**
   * Atualiza configurações do sistema de acesso
   */
  async updateConfiguracoes(configuracoes: Partial<ConfiguracaoAcesso>): Promise<ConfiguracaoAcesso> {
    return this.put<ConfiguracaoAcesso>(`${this.baseUrl}/configuracoes`, configuracoes);
  }

  /**
   * Testa conectividade com o leitor RFID
   */
  async testarConectividadeRfid(): Promise<{
    conectado: boolean;
    versaoFirmware?: string;
    ultimaLeitura?: string;
    erro?: string;
  }> {
    return this.get(`${this.baseUrl}/teste-rfid`);
  }

  /**
   * Força sincronização com o dispositivo RFID
   */
  async sincronizarRfid(): Promise<{
    sucesso: boolean;
    mensagem: string;
    dadosSincronizados: {
      cartoesAtivos: number;
      ultimaSincronizacao: string;
    };
  }> {
    return this.post(`${this.baseUrl}/sincronizar-rfid`, {});
  }

  /**
   * Obtém status do sistema de acesso
   */
  async getStatusSistema(): Promise<{
    sistemaAtivo: boolean;
    leituraRfidAtiva: boolean;
    bancoDadosConectado: boolean;
    servicosAtivos: string[];
    ultimaManutencao?: string;
    proximaManutencao?: string;
  }> {
    return this.get(`${this.baseUrl}/status`);
  }

  /**
   * Ativa/desativa o sistema de acesso
   */
  async toggleSistema(ativo: boolean): Promise<{
    sucesso: boolean;
    sistemaAtivo: boolean;
    mensagem: string;
  }> {
    return this.post(`${this.baseUrl}/toggle-sistema`, { ativo });
  }

  /**
   * Reinicia o serviço de leitura RFID
   */
  async reiniciarServicoRfid(): Promise<{
    sucesso: boolean;
    mensagem: string;
    tempoReinicio: string;
  }> {
    return this.post(`${this.baseUrl}/reiniciar-rfid`, {});
  }

  /**
   * Obtém logs de sistema (diferentes dos logs de acesso)
   */
  async getLogsSistema(nivel: 'info' | 'warning' | 'error' = 'info', ultimasHoras: number = 24): Promise<Array<{
    timestamp: string;
    nivel: string;
    componente: string;
    mensagem: string;
    detalhes?: any;
  }>> {
    const params = new URLSearchParams({
      nivel,
      ultimasHoras: ultimasHoras.toString()
    });
    
    return this.get(`${this.baseUrl}/logs-sistema?${params.toString()}`);
  }

  /**
   * Limpa cache do sistema
   */
  async limparCache(): Promise<{
    sucesso: boolean;
    mensagem: string;
    itensLimpos: string[];
  }> {
    return this.post(`${this.baseUrl}/limpar-cache`, {});
  }

  /**
   * Executa diagnóstico do sistema
   */
  async executarDiagnostico(): Promise<{
    status: 'saudavel' | 'atencao' | 'critico';
    verificacoes: Array<{
      componente: string;
      status: 'ok' | 'warning' | 'error';
      mensagem: string;
      detalhes?: any;
    }>;
    recomendacoes: string[];
    executadoEm: string;
  }> {
    return this.post(`${this.baseUrl}/diagnostico`, {});
  }

  /**
   * Força backup dos dados críticos
   */
  async executarBackup(): Promise<{
    sucesso: boolean;
    arquivoBackup: string;
    tamanho: number;
    dataHora: string;
    mensagem: string;
  }> {
    return this.post(`${this.baseUrl}/backup`, {});
  }

  /**
   * Obtém métricas de performance do sistema
   */
  async getMetricasPerformance(): Promise<{
    tempoRespostaMs: number;
    cpuUsage: number;
    memoryUsage: number;
    diskUsage: number;
    activeConnections: number;
    requestsPerMinute: number;
    errosUltimaHora: number;
  }> {
    return this.get(`${this.baseUrl}/metricas`);
  }
}

/**
 * Utilitários para trabalhar com controle de acesso
 */
export class AcessoUtils {
  /**
   * Formata UID para exibição (com espaçamento)
   */
  static formatUid(uid: string): string {
    return uid.replace(/(.{2})/g, '$1 ').trim().toUpperCase();
  }

  /**
   * Valida formato de UID RFID
   */
  static isValidUid(uid: string): boolean {
    // Remove espaços e converte para maiúsculo
    const cleanUid = uid.replace(/\s/g, '').toUpperCase();
    
    // Verifica se tem entre 8 e 20 caracteres hexadecimais
    return /^[0-9A-F]{8,20}$/.test(cleanUid);
  }

  /**
   * Limpa UID removendo espaços e caracteres especiais
   */
  static cleanUid(uid: string): string {
    return uid.replace(/[^0-9A-Fa-f]/g, '').toUpperCase();
  }

  /**
   * Converte status do sistema para cor
   */
  static getStatusColor(status: 'saudavel' | 'atencao' | 'critico'): string {
    switch (status) {
      case 'saudavel':
        return 'bg-green-100 text-green-800';
      case 'atencao':
        return 'bg-yellow-100 text-yellow-800';
      case 'critico':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  /**
   * Converte nível de log para cor
   */
  static getLogLevelColor(nivel: string): string {
    switch (nivel.toLowerCase()) {
      case 'info':
        return 'bg-blue-100 text-blue-800';
      case 'warning':
        return 'bg-yellow-100 text-yellow-800';
      case 'error':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  /**
   * Calcula percentual de sucesso
   */
  static calcularPercentualSucesso(sucessos: number, total: number): number {
    return total > 0 ? Math.round((sucessos / total) * 100) : 0;
  }

  /**
   * Formata métricas de performance
   */
  static formatarMetricas(metricas: any): {
    cpu: string;
    memoria: string;
    disco: string;
    tempoResposta: string;
  } {
    return {
      cpu: `${metricas.cpuUsage?.toFixed(1) || '0'}%`,
      memoria: `${metricas.memoryUsage?.toFixed(1) || '0'}%`,
      disco: `${metricas.diskUsage?.toFixed(1) || '0'}%`,
      tempoResposta: `${metricas.tempoRespostaMs || 0}ms`
    };
  }

  /**
   * Determina se o sistema precisa de atenção baseado nas métricas
   */
  static avaliarSaude(metricas: any): 'saudavel' | 'atencao' | 'critico' {
    const cpu = metricas.cpuUsage || 0;
    const memoria = metricas.memoryUsage || 0;
    const disco = metricas.diskUsage || 0;
    const tempoResposta = metricas.tempoRespostaMs || 0;

    if (cpu > 90 || memoria > 90 || disco > 95 || tempoResposta > 5000) {
      return 'critico';
    } else if (cpu > 70 || memoria > 70 || disco > 80 || tempoResposta > 2000) {
      return 'atencao';
    } else {
      return 'saudavel';
    }
  }
}

// Instância singleton do serviço
export const acessoService = new AcessoService();