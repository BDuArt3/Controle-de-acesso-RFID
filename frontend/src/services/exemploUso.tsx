/**
 * Exemplo de uso dos serviços no frontend
 * Este arquivo demonstra como usar os serviços criados
 */

import React, { useState, useEffect } from 'react';
import { 
  services, 
  utils, 
  TipoUsuario, 
  StatusCartao, 
  TipoMotivoAcesso,
  type UsuarioDto,
  type CartaoDto,
  type LogAcessoDto,
  type VerificacaoAcessoResponse
} from './index';

export const ExemplosUsoServicos: React.FC = () => {
  const [usuarios, setUsuarios] = useState<UsuarioDto[]>([]);
  const [cartoes, setCartoes] = useState<CartaoDto[]>([]);
  const [logs, setLogs] = useState<LogAcessoDto[]>([]);
  const [loading, setLoading] = useState(false);

  // 1. EXEMPLO: Carregar dados iniciais
  useEffect(() => {
    carregarDados();
  }, []);

  const carregarDados = async () => {
    setLoading(true);
    try {
      // Carregar usuários
      const usuariosData = await services.usuario.getAll();
      setUsuarios(usuariosData);

      // Carregar cartões ativos
      const cartoesData = await services.cartao.getByStatus(StatusCartao.Ativo);
      setCartoes(cartoesData);

      // Carregar logs de hoje
      const logsData = await services.logAcesso.getToday({ pageSize: 20 });
      setLogs(logsData.items);

    } catch (error) {
      console.error('Erro ao carregar dados:', error);
    } finally {
      setLoading(false);
    }
  };

  // 2. EXEMPLO: Criar novo usuário
  const criarUsuario = async () => {
    try {
      const novoUsuario = await services.usuario.create({
        nome: 'João Silva',
        tipo: TipoUsuario.Aluno
      });
      
      // Atualizar lista
      setUsuarios(prev => [...prev, novoUsuario]);
      
      console.log('Usuário criado:', novoUsuario);
    } catch (error) {
      console.error('Erro ao criar usuário:', error);
    }
  };

  // 3. EXEMPLO: Associar cartão a usuário
  const associarCartao = async (usuarioId: string, uid: string) => {
    try {
      // Verificar se UID é único
      const isUnique = await services.cartao.isUidUnique(uid);
      if (!isUnique) {
        alert('UID já está em uso!');
        return;
      }

      // Criar cartão
      const novoCartao = await services.cartao.create({
        uid: utils.acesso.cleanUid(uid),
        funcionarioId: usuarioId,
        status: StatusCartao.Ativo
      });

      // Atualizar lista
      setCartoes(prev => [...prev, novoCartao]);
      
      console.log('Cartão associado:', novoCartao);
    } catch (error) {
      console.error('Erro ao associar cartão:', error);
    }
  };

  // 4. EXEMPLO: Simular acesso RFID
  const simularAcesso = async (uid: string) => {
    try {
      const resultado: VerificacaoAcessoResponse = await services.acesso.simularAcesso(uid);
      
      // Mostrar resultado
      if (resultado.autorizado) {
        console.log('✅ Acesso autorizado:', resultado.funcionario?.nome);
      } else {
        console.log('❌ Acesso negado:', resultado.motivo);
      }

      // Recarregar logs
      const novosLogs = await services.logAcesso.getToday({ pageSize: 20 });
      setLogs(novosLogs.items);

    } catch (error) {
      console.error('Erro na simulação:', error);
    }
  };

  // 5. EXEMPLO: Filtrar logs por tipo de motivo
  const filtrarLogsPorMotivo = async (motivoTipo: TipoMotivoAcesso) => {
    try {
      const logsData = await services.logAcesso.getByMotivoTipo(motivoTipo, {
        pageSize: 50,
        orderBy: 'dataHora',
        orderDirection: 'desc'
      });
      
      setLogs(logsData.items);
    } catch (error) {
      console.error('Erro ao filtrar logs:', error);
    }
  };

  // 6. EXEMPLO: Obter estatísticas
  const [estatisticas, setEstatisticas] = useState<any>(null);
  
  const carregarEstatisticas = async () => {
    try {
      const stats = await services.acesso.getEstatisticas();
      setEstatisticas(stats);
    } catch (error) {
      console.error('Erro ao carregar estatísticas:', error);
    }
  };

  // 7. EXEMPLO: Gerenciar status de cartão
  const alterarStatusCartao = async (cartaoId: string, novoStatus: StatusCartao) => {
    try {
      switch (novoStatus) {
        case StatusCartao.Ativo:
          await services.cartao.ativar(cartaoId);
          break;
        case StatusCartao.Inativo:
          await services.cartao.desativar(cartaoId);
          break;
        case StatusCartao.Bloqueado:
          await services.cartao.bloquear(cartaoId);
          break;
      }

      // Atualizar lista local
      setCartoes(prev => prev.map(cartao => 
        cartao.id === cartaoId 
          ? { ...cartao, status: novoStatus }
          : cartao
      ));

    } catch (error) {
      console.error('Erro ao alterar status:', error);
    }
  };

  // 8. EXEMPLO: Exportar relatório
  const exportarLogs = async () => {
    try {
      const blob = await services.logAcesso.exportToCsv({
        dataInicio: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
        dataFim: new Date().toISOString()
      });

      // Download do arquivo
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `logs-acesso-${new Date().toISOString().split('T')[0]}.csv`;
      a.click();
      window.URL.revokeObjectURL(url);

    } catch (error) {
      console.error('Erro ao exportar:', error);
    }
  };

  // 9. EXEMPLO: Monitoramento em tempo real
  useEffect(() => {
    const interval = setInterval(async () => {
      try {
        // Verificar últimos acessos
        const ultimosAcessos = await services.acesso.getUltimosAcessos(5);
        console.log('Últimos acessos:', ultimosAcessos);
        
        // Verificar status do sistema
        const status = await services.acesso.getStatusSistema();
        console.log('Status do sistema:', status);
        
      } catch (error) {
        console.error('Erro no monitoramento:', error);
      }
    }, 30000); // A cada 30 segundos

    return () => clearInterval(interval);
  }, []);

  // 10. EXEMPLO: Uso de utilitários
  const exemploUtilitarios = () => {
    // Formatar UID
    const uidFormatado = utils.acesso.formatUid('1234ABCD5678');
    console.log('UID formatado:', uidFormatado); // "12 34 AB CD 56 78"

    // Validar UID
    const uidValido = utils.acesso.isValidUid('1234ABCD');
    console.log('UID válido:', uidValido); // true

    // Converter enum para string
    const tipoTexto = utils.cartao.getStatusCartaoString(StatusCartao.Ativo);
    console.log('Status do cartão:', tipoTexto); // "Ativo"

    // Obter cor para badge
    const corStatus = utils.cartao.getStatusCartaoColor(StatusCartao.Bloqueado);
    console.log('Classe CSS:', corStatus); // "bg-red-100 text-red-800"

    // Formatar data/hora
    const dataFormatada = utils.logAcesso.formatDateTime(new Date().toISOString());
    console.log('Data formatada:', dataFormatada); // "01/01/2024 14:30:15"
  };

  if (loading) {
    return <div>Carregando...</div>;
  }

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-bold">Exemplos de Uso dos Serviços</h1>
      
      {/* Botões de exemplo */}
      <div className="flex flex-wrap gap-4">
        <button 
          onClick={criarUsuario}
          className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          Criar Usuário
        </button>
        
        <button 
          onClick={() => associarCartao('user-id', '1234ABCD')}
          className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600"
        >
          Associar Cartão
        </button>
        
        <button 
          onClick={() => simularAcesso('1234ABCD')}
          className="px-4 py-2 bg-yellow-500 text-white rounded hover:bg-yellow-600"
        >
          Simular Acesso
        </button>
        
        <button 
          onClick={carregarEstatisticas}
          className="px-4 py-2 bg-purple-500 text-white rounded hover:bg-purple-600"
        >
          Carregar Stats
        </button>
        
        <button 
          onClick={exportarLogs}
          className="px-4 py-2 bg-indigo-500 text-white rounded hover:bg-indigo-600"
        >
          Exportar Logs
        </button>
        
        <button 
          onClick={exemploUtilitarios}
          className="px-4 py-2 bg-gray-500 text-white rounded hover:bg-gray-600"
        >
          Testar Utilitários
        </button>
      </div>

      {/* Exibição de dados */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Usuários */}
        <div>
          <h2 className="text-lg font-semibold mb-3">Usuários ({usuarios.length})</h2>
          <div className="space-y-2">
            {usuarios.slice(0, 5).map(usuario => (
              <div key={usuario.id} className="p-3 border rounded">
                <div className="font-medium">{usuario.nome}</div>
                <div className="text-sm text-gray-600">
                  Tipo: {usuario.tipo}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Cartões */}
        <div>
          <h2 className="text-lg font-semibold mb-3">Cartões ({cartoes.length})</h2>
          <div className="space-y-2">
            {cartoes.slice(0, 5).map(cartao => (
              <div key={cartao.id} className="p-3 border rounded">
                <div className="font-medium">{utils.acesso.formatUid(cartao.uid)}</div>
                <div className={`text-sm px-2 py-1 rounded inline-block ${utils.cartao.getStatusCartaoColor(cartao.status)}`}>
                  {utils.cartao.getStatusCartaoString(cartao.status)}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Logs */}
        <div>
          <h2 className="text-lg font-semibold mb-3">Logs Recentes ({logs.length})</h2>
          <div className="space-y-2">
            {logs.slice(0, 5).map(log => (
              <div key={log.id} className="p-3 border rounded">
                <div className="font-medium">
                  {log.funcionario?.nome || 'Desconhecido'}
                </div>
                <div className={`text-sm px-2 py-1 rounded inline-block ${utils.logAcesso.getTipoMotivoAcessoColor(log.motivoTipo)}`}>
                  {utils.logAcesso.getTipoMotivoAcessoString(log.motivoTipo)}
                </div>
                <div className="text-xs text-gray-500">
                  {utils.logAcesso.formatDateTime(log.dataHora)}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Estatísticas */}
      {estatisticas && (
        <div className="p-4 bg-gray-50 rounded">
          <h3 className="font-semibold mb-2">Estatísticas de Hoje</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <div className="text-2xl font-bold text-blue-600">{estatisticas.acessosHoje}</div>
              <div className="text-sm text-gray-600">Total de Acessos</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-green-600">{estatisticas.acessosAutorizadosHoje}</div>
              <div className="text-sm text-gray-600">Autorizados</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-red-600">{estatisticas.acessosNegadosHoje}</div>
              <div className="text-sm text-gray-600">Negados</div>
            </div>
            <div>
              <div className="text-2xl font-bold text-purple-600">{estatisticas.percentualSucessoHoje}%</div>
              <div className="text-sm text-gray-600">Taxa de Sucesso</div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ExemplosUsoServicos;