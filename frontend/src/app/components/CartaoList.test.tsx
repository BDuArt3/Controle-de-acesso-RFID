import React from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import CartaoList from './CartaoList';
import { cartaoService, usuarioService } from '../services/api';

vi.mock('../hooks/useAuth', () => ({
  useAuth: () => ({ user: { role: 'Admin' } }),
}));

vi.mock('./Toast', () => ({ showToast: vi.fn() }));

vi.mock('../services/api', async importOriginal => {
  const actual = await importOriginal<typeof import('../services/api')>();
  return {
    ...actual,
    usuarioService: { list: vi.fn() },
    cartaoService: {
      list: vi.fn(),
      getByUsuario: vi.fn(),
      create: vi.fn(),
      bloquear: vi.fn(),
      ativar: vi.fn(),
      cancelar: vi.fn(),
      getSolicitacao: vi.fn(),
    },
  };
});

const user = { id: 'u1', nome: 'Maria Silva', tipo: 0, isActive: true };
const activeCard = { id: 'c-active', usuarioId: 'u1', uid: 'AAAA1111', status: 0, isActive: true, usuarioNome: 'Maria Silva' };
const blockedCard = { id: 'c-blocked', usuarioId: 'u1', uid: 'BBBB2222', status: 1, isActive: false, usuarioNome: 'Maria Silva' };
const cancelledCard = { id: 'c-cancelled', usuarioId: 'u1', uid: 'CCCC3333', status: 2, isActive: false, usuarioNome: 'Maria Silva' };

function conflict(message?: string) {
  return { isAxiosError: true, response: { status: 409, data: message ? { message } : {} } };
}

async function openDialogAndSelectUser() {
  await userEvent.click(await screen.findByRole('button', { name: /nova solicitação/i }));
  await userEvent.click(screen.getByLabelText('Usuário'));
  await userEvent.click(await screen.findByRole('option', { name: 'Maria Silva' }));
}

describe('CartaoList - um cartão ativo por usuário', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(usuarioService.list).mockResolvedValue({ data: [user] } as any);
    vi.mocked(cartaoService.list).mockResolvedValue({ data: [activeCard, blockedCard, cancelledCard] } as any);
    vi.mocked(cartaoService.getByUsuario).mockResolvedValue({ data: [activeCard, blockedCard, cancelledCard] } as any);
  });

  it('mantém bloqueados e cancelados visíveis no histórico', async () => {
    render(<CartaoList />);
    expect(await screen.findByText('AAAA1111')).toBeInTheDocument();
    expect(screen.getByText('BBBB2222')).toBeInTheDocument();
    expect(screen.getByText('CCCC3333')).toBeInTheDocument();
  });

  it('não permite emissão inicial quando já há cartão ativo', async () => {
    render(<CartaoList />);
    await openDialogAndSelectUser();

    const alert = await screen.findByText(/já possui um cartão ativo/i);
    expect(alert.closest('[role="alert"]')).toHaveTextContent('AAAA1111');
    expect(screen.getByRole('button', { name: /bloquear cartão atual/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Solicitar' })).toBeDisabled();
    expect(cartaoService.create).not.toHaveBeenCalled();
  });

  it('reemite usando o cartão ativo atual e o motivo informado', async () => {
    vi.mocked(cartaoService.create).mockResolvedValue({
      data: { solicitacaoId: 's1', status: 'Pendente', mensagem: '', usuarioId: 'u1', nomeUsuario: 'Maria Silva', dataSolicitacao: '', cartaoId: null },
    } as any);
    vi.mocked(cartaoService.getSolicitacao).mockResolvedValue({ data: { solicitacaoId: 's1', status: 'Pendente' } } as any);

    render(<CartaoList />);
    await openDialogAndSelectUser();
    await userEvent.click(await screen.findByRole('button', { name: /iniciar reemissão/i }));
    await userEvent.type(await screen.findByLabelText(/motivo da reemissão/i), 'Cartão perdido');

    const submit = screen.getByRole('button', { name: 'Solicitar' });
    await waitFor(() => expect(submit).toBeEnabled());
    await userEvent.click(submit);

    await waitFor(() => expect(cartaoService.create).toHaveBeenCalledTimes(1));
    expect(vi.mocked(cartaoService.create).mock.calls[0][0]).toEqual({
      usuarioId: 'u1',
      cartaoAnteriorId: 'c-active',
      motivoReemissao: 'Cartão perdido',
    });
  });

  it('exibe a mensagem da API em 409 na reemissão', async () => {
    vi.mocked(cartaoService.create).mockRejectedValue(conflict('Conflito vindo da API'));

    render(<CartaoList />);
    await openDialogAndSelectUser();
    await userEvent.click(await screen.findByRole('button', { name: /iniciar reemissão/i }));
    await userEvent.type(await screen.findByLabelText(/motivo da reemissão/i), 'Dano');
    const submit = screen.getByRole('button', { name: 'Solicitar' });
    await waitFor(() => expect(submit).toBeEnabled());
    await userEvent.click(submit);

    expect(await screen.findByText('Conflito vindo da API')).toBeInTheDocument();
  });

  it('informa claramente 409 ao reativar e preserva o motivo preenchido', async () => {
    vi.mocked(cartaoService.ativar).mockRejectedValue(conflict());

    render(<CartaoList />);
    await userEvent.click(await screen.findByRole('button', { name: /ações do cartão BBBB2222/i }));
    await userEvent.click(await screen.findByRole('menuitem', { name: /reativar/i }));
    const dialog = await screen.findByRole('dialog');
    await userEvent.type(within(dialog).getByLabelText(/motivo/i), 'Encontrado');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Confirmar' }));

    expect(await within(dialog).findByText(/titular já possui outro cartão ativo/i)).toBeInTheDocument();
    expect(within(dialog).getByLabelText(/motivo/i)).toHaveValue('Encontrado');
    expect(cartaoService.ativar).toHaveBeenCalledWith('c-blocked', 'Encontrado');
  });
});
