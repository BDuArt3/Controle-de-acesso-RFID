/*
    Read-only pre-migration inventory for the legacy SQL Server schema.

    Run against a restored copy first. The script contains SELECT statements
    only; do not add UPDATE/INSERT/DELETE or DDL here. It assumes the last
    legacy migration (20261012193927_AddCamposExtras) has been applied.
    Some result sets contain personal data and must only be viewed/exported
    in an authorized, access-controlled environment.
*/

SET NOCOUNT ON;

-- 1. Applied migrations and baseline table counts.
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;

SELECT 'Usuarios' AS Tabela, COUNT_BIG(*) AS Total FROM dbo.Usuarios
UNION ALL
SELECT 'Funcionarios', COUNT_BIG(*) FROM dbo.Funcionarios
UNION ALL
SELECT 'Cartoes', COUNT_BIG(*) FROM dbo.Cartoes
UNION ALL
SELECT 'LogsAcesso', COUNT_BIG(*) FROM dbo.LogsAcesso;

-- 2. Employee/user candidates. Matches use normalized email OR non-empty document.
-- Candidate counts identify ambiguity; this result intentionally omits PII.
WITH Candidatos AS
(
    SELECT f.Id AS FuncionarioId, u.Id AS UsuarioId
    FROM dbo.Funcionarios AS f
    INNER JOIN dbo.Usuarios AS u
        ON (
            NULLIF(UPPER(LTRIM(RTRIM(f.Email))), '') IS NOT NULL
            AND NULLIF(UPPER(LTRIM(RTRIM(u.Email))), '') =
                NULLIF(UPPER(LTRIM(RTRIM(f.Email))), '')
        )
        OR (
            NULLIF(UPPER(LTRIM(RTRIM(f.Cpf))), '') IS NOT NULL
            AND NULLIF(UPPER(LTRIM(RTRIM(u.Documento))), '') =
                NULLIF(UPPER(LTRIM(RTRIM(f.Cpf))), '')
        )
),
PorFuncionario AS
(
    SELECT f.Id AS FuncionarioId, COUNT(DISTINCT c.UsuarioId) AS Candidatos
    FROM dbo.Funcionarios AS f
    LEFT JOIN Candidatos AS c ON c.FuncionarioId = f.Id
    GROUP BY f.Id
)
SELECT
    COUNT_BIG(*) AS FuncionariosTotal,
    SUM(CASE WHEN Candidatos = 0 THEN CONVERT(bigint, 1) ELSE 0 END) AS SemCandidato,
    SUM(CASE WHEN Candidatos = 1 THEN CONVERT(bigint, 1) ELSE 0 END) AS CandidatoUnico,
    SUM(CASE WHEN Candidatos > 1 THEN CONVERT(bigint, 1) ELSE 0 END) AS Ambiguos
FROM PorFuncionario;

-- Number of existing Tipo=Funcionario users and duplicate normalized emails.
SELECT COUNT_BIG(*) AS UsuariosTipoFuncionario
FROM dbo.Usuarios
WHERE Tipo = 3;

SELECT COUNT_BIG(*) AS GruposEmailDuplicadoEmFuncionarios
FROM
(
    SELECT UPPER(LTRIM(RTRIM(Email))) AS EmailNormalizado
    FROM dbo.Funcionarios
    WHERE NULLIF(LTRIM(RTRIM(Email)), '') IS NOT NULL
    GROUP BY UPPER(LTRIM(RTRIM(Email)))
    HAVING COUNT_BIG(*) > 1
) AS Duplicados;

SELECT COUNT_BIG(*) AS GruposEmailDuplicadoEmUsuarios
FROM
(
    SELECT UPPER(LTRIM(RTRIM(Email))) AS EmailNormalizado
    FROM dbo.Usuarios
    WHERE NULLIF(LTRIM(RTRIM(Email)), '') IS NOT NULL
    GROUP BY UPPER(LTRIM(RTRIM(Email)))
    HAVING COUNT_BIG(*) > 1
) AS Duplicados;

SELECT
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(Email)), '') IS NULL THEN CONVERT(bigint, 1) ELSE 0 END) AS EmailVazio,
    SUM(CASE WHEN LEN(LTRIM(RTRIM(Email))) > 254 THEN CONVERT(bigint, 1) ELSE 0 END) AS EmailAcimaDe254
FROM dbo.Usuarios;

SELECT
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(Email)), '') IS NULL THEN CONVERT(bigint, 1) ELSE 0 END) AS EmailFuncionarioVazio,
    SUM(CASE WHEN LEN(LTRIM(RTRIM(Email))) > 254 THEN CONVERT(bigint, 1) ELSE 0 END) AS EmailFuncionarioAcimaDe254,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(Cpf)), '') IS NULL THEN CONVERT(bigint, 1) ELSE 0 END) AS CpfFuncionarioVazio
FROM dbo.Funcionarios;

SELECT COUNT_BIG(*) AS GruposCpfDuplicadoEmFuncionarios
FROM
(
    SELECT UPPER(LTRIM(RTRIM(Cpf))) AS CpfNormalizado
    FROM dbo.Funcionarios
    WHERE NULLIF(LTRIM(RTRIM(Cpf)), '') IS NOT NULL
    GROUP BY UPPER(LTRIM(RTRIM(Cpf)))
    HAVING COUNT_BIG(*) > 1
) AS Duplicados;

SELECT
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(Documento)), '') IS NULL THEN CONVERT(bigint, 1) ELSE 0 END) AS DocumentoVazio
FROM dbo.Usuarios;

-- 3. Cards, UID uniqueness, orphaned owners, and sentinel registration dates.
SELECT
    SUM(CASE WHEN IsActive = 1 THEN CONVERT(bigint, 1) ELSE 0 END) AS CartoesAtivos,
    SUM(CASE WHEN IsActive = 0 THEN CONVERT(bigint, 1) ELSE 0 END) AS CartoesInativos,
    SUM(CASE WHEN DataRegistro = CONVERT(datetime2, '0001-01-01T00:00:00') THEN CONVERT(bigint, 1) ELSE 0 END) AS DataRegistroSentinela
FROM dbo.Cartoes;

SELECT COUNT_BIG(*) AS CartoesSemUsuario
FROM dbo.Cartoes AS c
LEFT JOIN dbo.Usuarios AS u ON u.Id = c.UsuarioId
WHERE u.Id IS NULL;

SELECT COUNT_BIG(*) AS GruposUidDuplicado
FROM
(
    SELECT Uid
    FROM dbo.Cartoes
    GROUP BY Uid
    HAVING COUNT_BIG(*) > 1
) AS Duplicados;

-- 4. Legacy access-log classification. These checks are also safe aggregates.
SELECT
    COUNT_BIG(*) AS LogsTotal,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(l.CartaoUid)), '') IS NULL THEN CONVERT(bigint, 1) ELSE 0 END) AS LogsUidVazio,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(l.CartaoUid)), '') IS NOT NULL AND c.Id IS NULL THEN CONVERT(bigint, 1) ELSE 0 END) AS LogsUidSemCartao,
    SUM(CASE WHEN c.Id IS NOT NULL THEN CONVERT(bigint, 1) ELSE 0 END) AS LogsComCartao
FROM dbo.LogsAcesso AS l
LEFT JOIN dbo.Cartoes AS c ON c.Uid = l.CartaoUid;

/*
    Restricted manual review query. Run only in an authorized tool with
    controlled output; it returns employee/person PII needed to adjudicate
    candidate mappings. Do not save its result in Git or application logs.

    SELECT f.Id AS FuncionarioId, f.NomeFuncionario, f.Email, f.Cpf,
           u.Id AS UsuarioIdCandidato, u.Nome AS NomeUsuario,
           u.Email AS EmailUsuario, u.Documento AS DocumentoUsuario,
           u.Tipo AS TipoUsuario, u.IsActive AS UsuarioAtivo
    FROM dbo.Funcionarios AS f
    LEFT JOIN dbo.Usuarios AS u
      ON (
          NULLIF(UPPER(LTRIM(RTRIM(f.Email))), '') IS NOT NULL
          AND NULLIF(UPPER(LTRIM(RTRIM(u.Email))), '') =
              NULLIF(UPPER(LTRIM(RTRIM(f.Email))), '')
      )
      OR (
          NULLIF(UPPER(LTRIM(RTRIM(f.Cpf))), '') IS NOT NULL
          AND NULLIF(UPPER(LTRIM(RTRIM(u.Documento))), '') =
              NULLIF(UPPER(LTRIM(RTRIM(f.Cpf))), '')
      )
    ORDER BY f.Id, u.Id;
*/
