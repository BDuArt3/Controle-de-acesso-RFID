import React from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import CartaoValidadeInfo from './CartaoValidadeInfo';
import UserForm from './UserForm';
import { getCartaoValidade, parseDataExpiracao } from '../services/cartaoValidade';

vi.mock('../hooks/useAuth', () => ({
  useAuth: () => ({ user: { role: 'Admin' } }),
}));

const now = new Date('2026-10-21T12:00:00Z');

describe('validade de cartões', () => {
  it('visitante vigente', () => {
    render(<CartaoValidadeInfo card={{ status: 0, dataExpiracao: '2026-10-22T12:00:00Z' }} now={now} />);
    expect(screen.getByText('Vigente')).toBeInTheDocument();
    expect(screen.queryByText(/próxima leitura/i)).not.toBeInTheDocument();
  });

  it('visitante vencido ainda Ativo indica bloqueio na próxima leitura', () => {
    render(<CartaoValidadeInfo card={{ status: 0, dataExpiracao: '2026-10-21T11:00:00Z' }} now={now} />);
    expect(screen.getByText('Vencida')).toBeInTheDocument();
    expect(screen.getByText(/bloqueio será registrado na próxima leitura/i)).toBeInTheDocument();
  });

  it('cartão já Bloqueado vencido não mostra aviso pendente', () => {
    render(<CartaoValidadeInfo card={{ status: 1, dataExpiracao: '2026-10-21T11:00:00Z' }} now={now} />);
    expect(screen.getByText('Vencida')).toBeInTheDocument();
    expect(screen.queryByText(/próxima leitura/i)).not.toBeInTheDocument();
  });

  it.each([null, undefined])('DataExpiracao %s não mostra validade', value => {
    render(<CartaoValidadeInfo card={{ status: 0, dataExpiracao: value }} now={now} />);
    expect(screen.queryByText('Vigente')).not.toBeInTheDocument();
    expect(screen.queryByText('Vencida')).not.toBeInTheDocument();
    expect(getCartaoValidade({ status: 0, dataExpiracao: value }, now).estado).toBe('sem-validade');
  });

  it('data inválida não gera erro', () => {
    render(<CartaoValidadeInfo card={{ status: 0, dataExpiracao: 'lixo' }} now={now} />);
    expect(screen.getByText('Validade indisponível')).toBeInTheDocument();
  });

  it('interpreta datas sem offset como UTC', () => {
    expect(parseDataExpiracao('2026-10-22T12:00:00')?.toISOString()).toBe('2026-10-22T12:00:00.000Z');
    expect(parseDataExpiracao('2026-10-22T09:00:00-03:00')?.toISOString()).toBe('2026-10-22T12:00:00.000Z');
  });
});

describe('aviso no formulário de usuário', () => {
  it('só aparece para Visitante', async () => {
    render(<UserForm onNavigate={vi.fn()} />);
    expect(screen.queryByText(/validade de 24 horas/i)).not.toBeInTheDocument();
    await userEvent.click(screen.getByLabelText(/tipo/i));
    await userEvent.click(await screen.findByRole('option', { name: 'Professor' }));
    expect(screen.queryByText(/validade de 24 horas/i)).not.toBeInTheDocument();
    await userEvent.click(screen.getByLabelText(/tipo/i));
    await userEvent.click(await screen.findByRole('option', { name: 'Visitante' }));
    expect(screen.getByText(/validade de 24 horas a partir da emissão confirmada/i)).toBeInTheDocument();
  });
});
