import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AccessLogs from './AccessLogs';
import { logAcessoService } from '../services/api';

vi.mock('../services/api', async importOriginal => {
  const actual = await importOriginal<typeof import('../services/api')>();
  return {
    ...actual,
    logAcessoService: {
      ...actual.logAcessoService,
      list: vi.fn(),
      exportar: vi.fn(),
    },
  };
});

const createObjectURL = vi.fn(() => 'blob:temp');
const revokeObjectURL = vi.fn();
let clickedDownloads: string[];

async function openMenu() {
  await userEvent.click(await screen.findByRole('button', { name: 'Exportar' }));
}

describe('exportação dos logs de acesso', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clickedDownloads = [];
    vi.mocked(logAcessoService.list).mockResolvedValue({ data: [] } as any);
    URL.createObjectURL = createObjectURL;
    URL.revokeObjectURL = revokeObjectURL;
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      clickedDownloads.push(this.download);
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('oferece as opções CSV e PDF, com CSV primeiro', async () => {
    render(<AccessLogs />);
    await openMenu();

    const items = screen.getAllByRole('menuitem').map(item => item.textContent);
    expect(items).toEqual(['CSV', 'PDF']);
  });

  it('exibe e permite buscar logs pelo ponto de acesso', async () => {
    vi.mocked(logAcessoService.list).mockResolvedValue({
      data: [{
        id: 'log-1',
        dataHora: new Date().toISOString(), // a tela abre filtrando os logs de hoje
        local: 'Entrada Bloco A',
        cartaoUid: 'ABCDEF0123456789',
        acessoPermitido: true,
        motivo: 'Acesso autorizado',
        motivoTipo: 1,
      }],
    } as any);

    render(<AccessLogs />);

    expect(await screen.findByText('Local: Entrada Bloco A')).toBeInTheDocument();
    const search = screen.getByLabelText('Buscar');
    await userEvent.type(search, 'Bloco A');
    expect(screen.getByText('1 registro(s) encontrado(s)')).toBeInTheDocument();

    await userEvent.clear(search);
    await userEvent.type(search, 'Portaria');
    expect(screen.getByText('0 registro(s) encontrado(s)')).toBeInTheDocument();
  });

  it.each([
    ['CSV', 'csv'],
    ['PDF', 'pdf'],
  ])('exporta %s com o período e sem acessoPermitido quando o filtro é "Todos"', async (label, formato) => {
    vi.mocked(logAcessoService.exportar).mockResolvedValue({ blob: new Blob(['x']), fileName: `relatorio.${formato}` });
    render(<AccessLogs />);
    await openMenu();
    await userEvent.click(screen.getByRole('menuitem', { name: label }));

    await waitFor(() => expect(clickedDownloads).toEqual([`relatorio.${formato}`]));
    const params = vi.mocked(logAcessoService.exportar).mock.calls[0][0];
    expect(params.formato).toBe(formato);
    expect(Number.isNaN(Date.parse(params.dataInicio))).toBe(false);
    expect(Number.isNaN(Date.parse(params.dataFim))).toBe(false);
    expect(Date.parse(params.dataInicio)).toBeLessThan(Date.parse(params.dataFim));
    expect(params).not.toHaveProperty('acessoPermitido');
    expect(createObjectURL).toHaveBeenCalledTimes(1);
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:temp');
  });

  it('envia acessoPermitido e o período personalizado conforme os filtros atuais', async () => {
    vi.mocked(logAcessoService.exportar).mockResolvedValue({ blob: new Blob(['x']), fileName: 'a.pdf' });
    render(<AccessLogs />);

    await userEvent.click((await screen.findAllByRole('combobox'))[0]);
    await userEvent.click(await screen.findByRole('option', { name: 'Período personalizado' }));
    await userEvent.type(screen.getByLabelText('De'), '2026-01-10');
    await userEvent.type(screen.getByLabelText('Até'), '2026-01-12');

    await userEvent.click(screen.getAllByRole('combobox')[1]);
    await userEvent.click(await screen.findByRole('option', { name: 'Negados' }));

    await openMenu();
    await userEvent.click(screen.getByRole('menuitem', { name: 'PDF' }));

    await waitFor(() => expect(logAcessoService.exportar).toHaveBeenCalledTimes(1));
    expect(logAcessoService.exportar).toHaveBeenCalledWith({
      dataInicio: new Date('2026-01-10T00:00:00').toISOString(),
      dataFim: new Date('2026-01-12T23:59:59.999').toISOString(),
      acessoPermitido: false,
      formato: 'pdf',
    });
    expect(screen.getAllByRole('combobox')[1]).toHaveTextContent('Negados');
  });

  it('exige as datas no período personalizado', async () => {
    render(<AccessLogs />);
    await userEvent.click((await screen.findAllByRole('combobox'))[0]);
    await userEvent.click(await screen.findByRole('option', { name: 'Período personalizado' }));

    await openMenu();
    await userEvent.click(screen.getByRole('menuitem', { name: 'CSV' }));

    expect(await screen.findByText('Informe as datas inicial e final do período para exportar.')).toBeInTheDocument();
    expect(logAcessoService.exportar).not.toHaveBeenCalled();
  });

  it('mostra mensagem de erro e não baixa arquivo quando a API falha', async () => {
    vi.mocked(logAcessoService.exportar).mockRejectedValue(new Error('Falha no servidor'));
    render(<AccessLogs />);
    await openMenu();
    await userEvent.click(screen.getByRole('menuitem', { name: 'CSV' }));

    expect(await screen.findByText(/Não foi possível exportar os logs\. Falha no servidor/)).toBeInTheDocument();
    expect(clickedDownloads).toEqual([]);
    expect(createObjectURL).not.toHaveBeenCalled();
  });
});

describe('logAcessoService.exportar', () => {
  const realExportar = vi.importActual<typeof import('../services/api')>('../services/api');

  afterEach(() => {
    localStorage.clear();
  });

  it('chama GET /log-acesso/exportar com token Bearer, params e resposta binária', async () => {
    const actual = await realExportar;
    const adapter = vi.fn(async (config: any) => ({
      data: new Blob(['%PDF']),
      status: 200,
      statusText: 'OK',
      headers: { 'content-disposition': 'attachment; filename="logs 2026.pdf"' },
      config,
    }));
    const previous = actual.default.defaults.adapter;
    actual.default.defaults.adapter = adapter as any;
    localStorage.setItem('authToken', 'abc123');

    try {
      const result = await actual.logAcessoService.exportar({
        dataInicio: '2026-01-01T00:00:00.000Z',
        dataFim: '2026-01-02T00:00:00.000Z',
        acessoPermitido: true,
        formato: 'pdf',
      });

      const config = adapter.mock.calls[0][0];
      expect(config.method).toBe('get');
      expect(config.url).toBe('/log-acesso/exportar');
      expect(config.responseType).toBe('blob');
      expect(config.params).toEqual({
        dataInicio: '2026-01-01T00:00:00.000Z',
        dataFim: '2026-01-02T00:00:00.000Z',
        acessoPermitido: true,
        formato: 'pdf',
      });
      expect(config.headers.Authorization ?? config.headers.get?.('Authorization')).toBe('Bearer abc123');
      expect(result.fileName).toBe('logs 2026.pdf');
      expect(result.blob).toBeInstanceOf(Blob);
    } finally {
      actual.default.defaults.adapter = previous;
    }
  });

  it('usa a mensagem do corpo JSON em erros HTTP sem devolver o erro como arquivo', async () => {
    const actual = await realExportar;
    const adapter = vi.fn(async (config: any) => {
      const error: any = new Error('Request failed');
      error.isAxiosError = true;
      error.config = config;
      error.response = {
        status: 400,
        data: new Blob([JSON.stringify({ message: 'Período inválido.' })]),
        headers: {},
        config,
      };
      throw error;
    });
    const previous = actual.default.defaults.adapter;
    actual.default.defaults.adapter = adapter as any;

    try {
      const failure = await actual.logAcessoService
        .exportar({ dataInicio: 'a', dataFim: 'b', formato: 'csv' })
        .catch((error: unknown) => error);
      expect(actual.errorMessage(failure)).toBe('Período inválido.');
    } finally {
      actual.default.defaults.adapter = previous;
    }
  });
});
