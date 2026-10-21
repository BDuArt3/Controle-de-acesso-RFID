import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import UserForm from './UserForm';
import UserManagement from './UserManagement';
import { usuarioService } from '../services/api';

vi.mock('../hooks/useAuth', () => ({
  useAuth: () => ({ user: { role: 'Admin' } }),
}));

vi.mock('../services/api', async importOriginal => {
  const actual = await importOriginal<typeof import('../services/api')>();
  return {
    ...actual,
    usuarioService: {
      list: vi.fn(),
      get: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      deletar: vi.fn(),
      ativar: vi.fn(),
    },
  };
});

const CPF = '123.456.789-09';
const apiUser = { id: 'u1', nome: 'Maria Silva', email: 'maria@x.com', tipo: 0, isActive: true, cartoes: [] };

describe('telas de usuários sem documento/CPF', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('listagem não exibe CPF nem campo de busca por documento', async () => {
    // simula resposta legada que ainda traga o campo
    vi.mocked(usuarioService.list).mockResolvedValue({ data: [{ ...apiUser, documento: CPF }] } as any);
    render(<UserManagement onNavigate={vi.fn()} />);

    expect(await screen.findByText('Maria Silva')).toBeInTheDocument();
    expect(screen.queryByText(CPF)).not.toBeInTheDocument();
    expect(screen.queryByPlaceholderText(/documento/i)).not.toBeInTheDocument();
  });

  it('formulário de edição não exibe CPF/documento', async () => {
    vi.mocked(usuarioService.get).mockResolvedValue({ data: { ...apiUser, documento: CPF } } as any);
    render(<UserForm editingUserId="u1" onNavigate={vi.fn()} />);

    await waitFor(() => expect(screen.getByDisplayValue('Maria Silva')).toBeInTheDocument());
    expect(screen.queryByLabelText(/documento|cpf/i)).not.toBeInTheDocument();
    expect(screen.queryByDisplayValue(CPF)).not.toBeInTheDocument();
    expect(screen.queryByText(CPF)).not.toBeInTheDocument();
  });

  it('cadastra usuário sem documento', async () => {
    vi.mocked(usuarioService.create).mockResolvedValue({ data: apiUser } as any);
    const { container } = render(<UserForm onNavigate={vi.fn()} />);

    expect(screen.queryByLabelText(/documento|cpf/i)).not.toBeInTheDocument();
    await userEvent.type(screen.getByLabelText(/nome/i), 'Maria Silva');
    await userEvent.type(screen.getByLabelText(/email/i), 'maria@x.com');
    await userEvent.click(screen.getByLabelText(/tipo/i));
    await userEvent.click(await screen.findByRole('option', { name: 'Aluno' }));
    fireEvent.submit(container.querySelector('form')!);

    await waitFor(() => expect(usuarioService.create).toHaveBeenCalledTimes(1));
    const payload = vi.mocked(usuarioService.create).mock.calls[0][0];
    expect(payload).toEqual({ nome: 'Maria Silva', email: 'maria@x.com', tipo: 0 });
    expect(payload).not.toHaveProperty('documento');
  });
});
