# Serviços Frontend - Sistema de Controle de Acesso RFID

Esta pasta contém todos os serviços para comunicação com a API backend do sistema de controle de acesso RFID.

## Estrutura dos Serviços

### 📁 Serviços Principais

#### `apiClient.ts`
- **Descrição**: Cliente HTTP base com configuração do Axios
- **Funcionalidades**: 
  - Interceptadores para autenticação automática
  - Renovação automática de tokens
  - Tratamento global de erros
  - Classe base `ApiService` para outros serviços

#### `authService.ts`
- **Descrição**: Gerenciamento de autenticação e autorização
- **Funcionalidades**:
  - Login/logout
  - Refresh de tokens
  - Gerenciamento de estado do usuário
  - Verificação de permissões

#### `usuarioService.ts`
- **Descrição**: Operações CRUD para usuários/funcionários
- **Funcionalidades**:
  - Listagem, criação, edição e exclusão de usuários
  - Ativação/desativação de contas
  - Enum `TipoUsuario` (Aluno, Professor, Visitante, Funcionário)
  - Métodos utilitários para conversão de tipos

#### `cartaoService.ts`
- **Descrição**: Gerenciamento de cartões RFID
- **Funcionalidades**:
  - CRUD completo de cartões
  - Controle de status (Ativo, Inativo, Bloqueado)
  - Validação de UID único
  - Associação com funcionários
  - Utilitários para formatação e cores de status

#### `logAcessoService.ts`
- **Descrição**: Consulta e análise de logs de acesso universitário
- **Funcionalidades**:
  - Registro de tentativas de acesso
  - Consulta por cartão UID, período, status
  - Enum `TipoMotivoAcesso` com 7 categorias
  - Estatísticas diárias e relatórios
  - Métodos específicos para entrada/saída universitária
  - Utilitários para formatação e visualização

#### `acessoService.ts`
- **Descrição**: Controle de acesso e monitoramento em tempo real
- **Funcionalidades**:
  - Verificação de acesso via UID
  - Simulação de tentativas (para testes)
  - Estatísticas em tempo real
  - Configurações do sistema
  - Diagnósticos e métricas de performance
  - Controle do serviço RFID

### 📁 Arquivos de Suporte

#### `funcionarioService.ts`
- **Descrição**: Serviço legado (mantido para compatibilidade)
- **Status**: Deprecated - usar `usuarioService.ts`

#### `index.ts`
- **Descrição**: Ponto central de exportação
- **Funcionalidades**:
  - Exporta todos os serviços e tipos
  - Objeto `services` com instâncias prontas
  - Objeto `utils` com utilitários

## 🚀 Como Usar

### Importação Básica
```typescript
import { usuarioService, cartaoService, logAcessoService } from '@/services';
```

### Usando Instâncias Diretas
```typescript
import { services } from '@/services';

// Listar usuários
const usuarios = await services.usuario.getAll();

// Verificar acesso
const acesso = await services.acesso.verificarAcesso({ uid: '1234ABCD' });
```

### Usando Utilitários
```typescript
import { utils } from '@/services';

// Formatar status de cartão
const statusText = utils.cartao.getStatusCartaoString(StatusCartao.Ativo);

// Formatar data de log
const dataFormatada = utils.logAcesso.formatDateTime(log.dataHora);
```

## 🔧 Tipos e Enums

### Enums Principais
```typescript
enum TipoUsuario {
  Aluno = 1,
  Professor = 2,
  Visitante = 3,
  Funcionario = 4
}

enum StatusCartao {
  Ativo = 1,
  Inativo = 2,
  Bloqueado = 3
}

enum TipoMotivoAcesso {
  AcessoAutorizado = 1,
  UidObrigatorio = 2,
  CartaoNaoEncontrado = 3,
  CartaoDesativado = 4,
  UsuarioNaoEncontrado = 5,
  UsuarioDesativado = 6,
  ErroInterno = 7
}
```

### Interfaces Principais
```typescript
interface UsuarioDto {
  id: string;
  nome: string;
  tipo: TipoUsuario;
  isActive: boolean;
  cartoes: Array<{ id: string; uid: string; isActive: boolean }>;
}

interface CartaoDto {
  id: string;
  uid: string;
  funcionarioId: string;
  status: StatusCartao;
  dataUltilizacao?: string;
  dataCriacao: string;
}

interface LogAcessoDto {
  id: number;
  dataHora: string;
  cartaoUid: string;
  acessoPermitido: boolean;
  motivo: string;
  motivoTipo: TipoMotivoAcesso;
  usuarioNome?: string;
  usuarioTipo?: string;
}
```

## 🔐 Autenticação

Todos os serviços automaticamente:
- Incluem o token JWT nas requisições
- Renovam tokens expirados automaticamente
- Redirecionam para login quando necessário

## 📊 Tratamento de Erros

```typescript
try {
  const usuario = await usuarioService.getById('123');
} catch (error: ApiError) {
  console.error(error.message);
  console.error('Status:', error.statusCode);
}
```

## 🔍 Filtros e Paginação

```typescript
// Logs com filtros
const logs = await logAcessoService.getAll({
  autorizado: false,
  dataInicio: '2024-01-01T00:00:00',
  dataFim: '2024-01-31T23:59:59',
  page: 1,
  pageSize: 50
});

// Resultado paginado
console.log(`${logs.currentPage}/${logs.totalPages}`);
console.log(`${logs.items.length} de ${logs.totalItems} itens`);
```

## ⚡ Performance

- Instâncias singleton dos serviços
- Cache automático de tokens
- Interceptadores otimizados
- Tipagem forte para melhor IntelliSense

## 🔄 Sincronização Backend

Todos os tipos e enums estão sincronizados com o backend:
- `TipoUsuario` → `TipoUsuario` (C#)
- `StatusCartao` → Status do cartão (C#)
- `TipoMotivoAcesso` → `TipoMotivoAcesso` (C#)

## 📝 Manutenção

Para adicionar novos serviços:
1. Criar arquivo na pasta `/services`
2. Estender `ApiService`
3. Adicionar ao `index.ts`
4. Atualizar este README

Para modificar serviços existentes:
1. Manter compatibilidade com interfaces existentes
2. Adicionar versionamento se necessário
3. Atualizar testes correspondentes