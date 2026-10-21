import type { CartaoDto } from './api';

export type CartaoValidadeEstado = 'sem-validade' | 'invalida' | 'vigente' | 'vencida';

export interface CartaoValidade {
  estado: CartaoValidadeEstado;
  expiraEm: Date | null;
  texto: string;
  // Vencido mas ainda Ativo na API: o bloqueio só é registrado na próxima leitura.
  bloqueioPendente: boolean;
}

const HAS_TIMEZONE = /(Z|[+-]\d{2}:?\d{2})$/i;

// A API trabalha em UTC; valores sem offset são tratados como UTC.
export function parseDataExpiracao(value: string | null | undefined): Date | null {
  if (!value || typeof value !== 'string') {
    return null;
  }
  const trimmed = value.trim();
  const normalized = HAS_TIMEZONE.test(trimmed) ? trimmed : `${trimmed}Z`;
  const date = new Date(normalized);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function formatDataExpiracao(date: Date) {
  return `${date.toLocaleDateString('pt-BR')} ${date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`;
}

export function getCartaoValidade(card: Pick<CartaoDto, 'dataExpiracao' | 'status'>, now: Date = new Date()): CartaoValidade {
  if (card.dataExpiracao == null || card.dataExpiracao === '') {
    return { estado: 'sem-validade', expiraEm: null, texto: '', bloqueioPendente: false };
  }
  const expiraEm = parseDataExpiracao(card.dataExpiracao);
  if (!expiraEm) {
    return { estado: 'invalida', expiraEm: null, texto: 'Validade indisponível', bloqueioPendente: false };
  }
  const vencida = expiraEm.getTime() <= now.getTime();
  return {
    estado: vencida ? 'vencida' : 'vigente',
    expiraEm,
    texto: formatDataExpiracao(expiraEm),
    bloqueioPendente: vencida && card.status === 0,
  };
}
