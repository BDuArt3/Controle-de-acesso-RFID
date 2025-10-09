// Exportações dos serviços base
export * from './apiClient';
export { ApiService } from './apiClient';
export type { ApiResponse, ApiError } from './apiClient';

// Exportações do serviço de autenticação
export * from './authService';

// Exportações do serviço de funcionários (compatibilidade)
export * from './funcionarioService';

// Exportações do serviço de portadors
export * from './portadorService';

// Exportações do serviço de cartões
export * from './cartaoService';

// Exportações do serviço de logs de acesso
export * from './logAcessoService';

// Exportações do serviço de controle de acesso
export * from './acessoService';

// Importações para criar instâncias
import { authService } from './authService';
import { funcionarioService } from './funcionarioService';
import { portadorService } from './portadorService';
import { cartaoService, CartaoUtils } from './cartaoService';
import { logAcessoService, LogAcessoUtils } from './logAcessoService';
import { acessoService, AcessoUtils } from './acessoService';

// Instâncias dos serviços (para conveniência)
export const services = {
  auth: authService,
  funcionario: funcionarioService,
  portador: portadorService,
  cartao: cartaoService,
  logAcesso: logAcessoService,
  acesso: acessoService
};

// Utilitários (para conveniência)
export const utils = {
  cartao: CartaoUtils,
  logAcesso: LogAcessoUtils,
  acesso: AcessoUtils
};