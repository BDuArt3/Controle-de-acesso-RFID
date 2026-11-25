# Controle de Acesso RFID: Frontend

Interface web do sistema de controle de acesso por cartão RFID. Aqui o funcionário faz login, cadastra pessoas e funcionários, emite cartões e acompanha os logs de acesso.

> Esta pasta é só a interface. A arquitetura do sistema completo (Arduino, Worker .NET, API ASP.NET Core e SQL Server) e o passo a passo para rodar tudo estão no [README principal](../README.md).

Trabalho de Conclusão de Curso – Bacharelado em Sistemas de Informação, UNIPAM (2025).

<!-- TODO: imagem - print do dashboard -->

## Telas

- **Login** com JWT e renovação automática do token
- **Dashboard** com o resumo dos acessos
- **Logs de acesso** com busca, filtros e exportação em CSV/PDF
- **Pessoas (usuários)**, **funcionários** e **cartões**, com histórico de alterações
- Permissões por perfil (Admin, Operador e Visualizador): cada perfil vê só o que pode usar

## Tecnologias

React 18, TypeScript, Vite, Tailwind CSS, componentes shadcn/ui (Radix), Axios e Vitest + Testing Library.

## Como rodar

Pré-requisitos: Node.js 20+ e a API rodando (veja o [README principal](../README.md)).

```powershell
cd frontend
npm install
copy .env.example .env.api-local
npm run dev
```

O `.env.api-local` define `VITE_API_BASE_URL`, o endereço da API (por padrão `http://localhost:5143/api`).

Outros comandos:

| Comando | O que faz |
|---|---|
| `npm test` | Roda os testes (Vitest) |
| `npm run build:local` | Gera o build usando `.env.api-local` |
| `npm run build` | Gera o build de produção usando `.env.api-azure` |

## Autor

**Bruno Duarte**: [LinkedIn](https://www.linkedin.com/in/brunoduartes) · [GitHub](https://github.com/BDuArt3)

Licença MIT (veja [LICENSE](../LICENSE)). Componentes de terceiros listados em [src/app/Attributions.md](src/app/Attributions.md).
