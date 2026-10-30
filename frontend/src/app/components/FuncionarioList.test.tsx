import React from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import FuncionarioList from './FuncionarioList';
import { funcionarioService } from '../services/api';

vi.mock('../hooks/useAuth', () => ({
  useAuth: () => ({ user: { role: 'Admin' } }),
}));

vi.mock('../services/api', async importOriginal => {
  const actual = await importOriginal<typeof import('../services/api')>();
  return {
    ...actual,
    funcionarioService: {
      list: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      ativar: vi.fn(),
    },
  };
});

const CPF = '12345678909';
const funcionarios = [
  { id: 'f1', nomeFuncionario: 'Ana Admin', email: 'ana@x.com', tipo: 2, isActive: true },
  { id: 'f2', nomeFuncionario: 'Beto Operador', email: 'beto@x.com', tipo: 1, isActive: true },
];

describe('listagem de funcionários', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(funcionarioService.list).mockResolvedValue({ data: funcionarios } as any);
    vi.mocked(funcionarioService.delete).mockResolvedValue({ data: { message: 'Funcionário desativado' } } as any);
  });

  it('não exibe a coluna CPF nem valores de CPF', async () => {
    vi.mocked(funcionarioService.list).mockResolvedValue({
      data: funcionarios.map(item => ({ ...item, cpf: CPF })),
    } as any);
    render(<FuncionarioList />);

    expect(await screen.findByText('Ana Admin')).toBeInTheDocument();
    expect(screen.queryByRole('columnheader', { name: /cpf/i })).not.toBeInTheDocument();
    expect(screen.queryByText(CPF)).not.toBeInTheDocument();
  });

  it('busca apenas por nome ou email', async () => {
    render(<FuncionarioList />);
    await screen.findByText('Ana Admin');

    const input = screen.getByPlaceholderText('Buscar por nome ou email...');
    await userEvent.type(input, 'beto@');
    expect(screen.queryByText('Ana Admin')).not.toBeInTheDocument();
    expect(screen.getByText('Beto Operador')).toBeInTheDocument();
  });

  it('exclui funcionário de forma lógica, exige justificativa e atualiza a lista', async () => {
    vi.mocked(funcionarioService.list)
      .mockResolvedValueOnce({ data: funcionarios } as any)
      .mockResolvedValueOnce({ data: [funcionarios[1]] } as any);
    render(<FuncionarioList />);
    await screen.findByText('Ana Admin');

    await userEvent.click(screen.getByRole('button', { name: 'Excluir Ana Admin' }));
    expect(await screen.findByRole('heading', { name: 'Excluir funcionário' })).toBeInTheDocument();
    expect(screen.getByText(/A exclusão é lógica/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    expect(await screen.findByText('Informe um motivo.')).toBeInTheDocument();
    expect(funcionarioService.delete).not.toHaveBeenCalled();

    await userEvent.type(screen.getByLabelText('Motivo *'), 'Encerramento do vínculo');
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }));

    expect(await screen.findByText('Beto Operador')).toBeInTheDocument();
    expect(screen.queryByText('Ana Admin')).not.toBeInTheDocument();
    expect(funcionarioService.delete).toHaveBeenCalledWith('f1', 'Encerramento do vínculo');
  });
});
