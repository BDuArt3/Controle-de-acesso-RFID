import { ApiService } from './apiClient';

// Tipos para funcionários
export interface Funcionario {
  id: string;
  nomeFuncionario: string;
  usuarioAcesso: string;
  tipo: string;
  ativo: boolean;
  dataCriacao?: string;
  dataUltimaAlteracao?: string;
}

export interface CreateFuncionarioRequest {
  nomeFuncionario: string;
  usuarioAcesso: string;
  senha: string;
  tipo: string;
}

export class FuncionarioService extends ApiService {

  async create(funcionarioData: CreateFuncionarioRequest): Promise<Funcionario> {
    return this.post<Funcionario>('/Funcionario', funcionarioData);
  }

  async getById(id: string): Promise<Funcionario> {
    return this.get<Funcionario>(`/Funcionario/${id}`);
  }

  async getAll(): Promise<Funcionario[]> {
    return this.get<Funcionario[]>('/Funcionario');
  }

  async update(id: string, funcionarioData: CreateFuncionarioRequest): Promise<Funcionario> {
    return this.put<Funcionario>(`/Funcionario/${id}`, funcionarioData);
  }

  async deleteFuncionario(id: string): Promise<{ message: string }> {
    return this.delete<{ message: string }>(`/Funcionario/${id}`);
  }

  async activate(id: string): Promise<{ message: string; funcionario: Funcionario }> {
    return this.patch<{ message: string; funcionario: Funcionario }>(`/Funcionario/${id}/ativar`);
  }
}

// Instância singleton do serviço de funcionários
export const funcionarioService = new FuncionarioService();