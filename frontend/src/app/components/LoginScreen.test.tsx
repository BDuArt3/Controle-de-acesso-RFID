import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import LoginScreen from './LoginScreen';
import { ToastContainer } from './Toast';

const { loginMock } = vi.hoisted(() => ({ loginMock: vi.fn() }));

vi.mock('../hooks/useAuth', () => ({
  useAuth: () => ({ login: loginMock }),
}));

describe('LoginScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows an error toast and remains on the login screen after invalid credentials', async () => {
    loginMock.mockRejectedValue(new Error('Email ou senha inválidos.'));
    const onLogin = vi.fn();
    render(
      <>
        <LoginScreen onLogin={onLogin} />
        <ToastContainer />
      </>,
    );

    await userEvent.type(screen.getByLabelText('Email'), 'operador@example.test');
    await userEvent.type(screen.getByLabelText('Senha'), 'senha-incorreta');
    fireEvent.click(screen.getByRole('button', { name: 'Entrar' }));

    await waitFor(() => expect(loginMock).toHaveBeenCalledWith(
      'operador@example.test',
      'senha-incorreta',
    ));
    expect(await screen.findAllByText('Email ou senha inválidos.')).toHaveLength(2);
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Senha')).toBeInTheDocument();
    expect(onLogin).not.toHaveBeenCalled();
  });

  it('continues to the application after successful authentication', async () => {
    loginMock.mockResolvedValue(undefined);
    const onLogin = vi.fn();
    render(<LoginScreen onLogin={onLogin} />);

    await userEvent.type(screen.getByLabelText('Email'), 'operador@example.test');
    await userEvent.type(screen.getByLabelText('Senha'), 'senha-valida');
    fireEvent.click(screen.getByRole('button', { name: 'Entrar' }));

    await waitFor(() => expect(onLogin).toHaveBeenCalledWith('operador@example.test'));
  });
});
