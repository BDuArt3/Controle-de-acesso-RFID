import React from 'react';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import UserManagement from './UserManagement';
import { usuarioService } from '../services/api';

let mockCurrentUserRole = 'Admin';

vi.mock('../hooks/useAuth', () => ({
  useAuth: () => ({
    user: { role: mockCurrentUserRole, name: 'Test User', email: 'test@tcc.com' },
    isAuthenticated: true,
  }),
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

const mockUsers = [
  { id: 'u1', nome: 'Aluno Um', email: 'aluno@x.com', tipo: 0, isActive: true, isFuncionario: false, cartoes: [] },
  { id: 'u2', nome: 'Funcionario Dois', email: 'func@x.com', tipo: 3, isActive: true, isFuncionario: true, cartoes: [] },
];

describe('RBAC - Permissões de Usuários (Seção 2.1 da Auditoria)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(usuarioService.list).mockResolvedValue({ data: mockUsers } as any);
  });

  it('Administrador visualiza botão de criar usuário e link para tela de funcionários', async () => {
    mockCurrentUserRole = 'Admin';
    render(<UserManagement onNavigate={vi.fn()} />);

    expect(await screen.findByText('Aluno Um')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /adicionar novo usuário/i })).toBeInTheDocument();
    expect(screen.getByText('Funcionário – gerencie na tela de Funcionários')).toBeInTheDocument();
  });

  it('Operador pode criar usuário mas NÃO vê link para tela de funcionários', async () => {
    mockCurrentUserRole = 'Operador';
    render(<UserManagement onNavigate={vi.fn()} />);

    expect(await screen.findByText('Aluno Um')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /adicionar novo usuário/i })).toBeInTheDocument();
    expect(screen.queryByText('Funcionário – gerencie na tela de Funcionários')).not.toBeInTheDocument();
    expect(screen.getByText('Funcionário')).toBeInTheDocument();
  });

  it('Visualizador NÃO pode criar usuário nem vê link para tela de funcionários', async () => {
    mockCurrentUserRole = 'Visualizador';
    render(<UserManagement onNavigate={vi.fn()} />);

    expect(await screen.findByText('Aluno Um')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /adicionar novo usuário/i })).not.toBeInTheDocument();
    expect(screen.queryByText('Funcionário – gerencie na tela de Funcionários')).not.toBeInTheDocument();
    expect(screen.getByText('Funcionário')).toBeInTheDocument();
  });
});
