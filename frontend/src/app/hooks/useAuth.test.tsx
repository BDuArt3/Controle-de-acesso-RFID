import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider, useAuth } from './useAuth';
import { authService, tokenStorage } from '../services/api';
import { ToastContainer } from '../components/Toast';

vi.mock('../services/api', async importOriginal => {
  const actual = await importOriginal<typeof import('../services/api')>();
  return {
    ...actual,
    authService: {
      ...actual.authService,
      getMe: vi.fn(),
      logout: vi.fn(),
    },
  };
});

function AuthStateProbe() {
  const { isAuthenticated, logout } = useAuth();
  return (
    <>
      <p data-testid="auth-state">{isAuthenticated ? 'signed-in' : 'signed-out'}</p>
      <button onClick={() => void logout()}>Logout</button>
    </>
  );
}

function renderAuthProvider() {
  return render(
    <AuthProvider>
      <AuthStateProbe />
      <ToastContainer />
    </AuthProvider>,
  );
}

describe('useAuth logout', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    vi.mocked(authService.getMe).mockResolvedValue({
      data: { id: 'employee-1', nomeFuncionario: 'Operador', email: 'operator@example.test', tipo: 'Operador' },
    } as any);
  });

  it('revokes the server refresh token before clearing the local session', async () => {
    tokenStorage.set({
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      tokenType: 'Bearer',
    });
    vi.mocked(authService.logout).mockResolvedValue({ data: { message: 'Logout realizado' } } as any);

    renderAuthProvider();
    await screen.findByText('signed-in');

    fireEvent.click(screen.getByRole('button', { name: 'Logout' }));

    await waitFor(() => expect(authService.logout).toHaveBeenCalledWith('refresh-token'));
    await waitFor(() => expect(screen.getByTestId('auth-state')).toHaveTextContent('signed-out'));
    expect(tokenStorage.getRefreshToken()).toBeNull();
  });

  it('clears the local session and surfaces an error when server revocation fails', async () => {
    tokenStorage.set({
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      tokenType: 'Bearer',
    });
    vi.mocked(authService.logout).mockRejectedValue(new Error('API indisponível'));

    renderAuthProvider();
    await screen.findByText('signed-in');

    fireEvent.click(screen.getByRole('button', { name: 'Logout' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/não foi possível revogar o token no servidor/i);
    await waitFor(() => expect(screen.getByTestId('auth-state')).toHaveTextContent('signed-out'));
    expect(tokenStorage.getRefreshToken()).toBeNull();
  });

  it('attempts to revoke the refresh token when the session expires', async () => {
    tokenStorage.set({
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresAt: new Date(Date.now() - 1_000).toISOString(),
      tokenType: 'Bearer',
    });
    vi.mocked(authService.logout).mockResolvedValue({ data: { message: 'Logout realizado' } } as any);

    renderAuthProvider();

    await waitFor(() => expect(authService.logout).toHaveBeenCalledWith('refresh-token'));
    await waitFor(() => expect(screen.getByTestId('auth-state')).toHaveTextContent('signed-out'));
  });

  it('attempts to revoke the refresh token after an authentication-expired event', async () => {
    tokenStorage.set({
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      tokenType: 'Bearer',
    });
    vi.mocked(authService.logout).mockResolvedValue({ data: { message: 'Logout realizado' } } as any);

    renderAuthProvider();
    await screen.findByText('signed-in');
    window.dispatchEvent(new Event('auth:logout'));

    await waitFor(() => expect(authService.logout).toHaveBeenCalledWith('refresh-token'));
    await waitFor(() => expect(screen.getByTestId('auth-state')).toHaveTextContent('signed-out'));
  });
});
