BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [Cartoes] DROP CONSTRAINT [FK_Cartoes_Usuarios_UsuarioId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    DROP INDEX [IX_Funcionarios_Email] ON [Funcionarios];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Usuarios]') AND [c].[name] = N'Uid');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Usuarios] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Usuarios] DROP COLUMN [Uid];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LogsAcesso]') AND [c].[name] = N'CartaoUid');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [LogsAcesso] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [LogsAcesso] DROP COLUMN [CartaoUid];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Funcionarios]') AND [c].[name] = N'Cpf');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Funcionarios] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Funcionarios] DROP COLUMN [Cpf];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Funcionarios]') AND [c].[name] = N'Email');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Funcionarios] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Funcionarios] DROP COLUMN [Email];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Funcionarios]') AND [c].[name] = N'NomeFuncionario');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Funcionarios] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Funcionarios] DROP COLUMN [NomeFuncionario];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cartoes]') AND [c].[name] = N'IsActive');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Cartoes] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Cartoes] DROP COLUMN [IsActive];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Usuarios]') AND [c].[name] = N'Email');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Usuarios] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Usuarios] ALTER COLUMN [Email] nvarchar(254) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [LogsAcesso] ADD [CartaoId] nvarchar(36) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [LogsAcesso] ADD [UidInformado] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [LogsAcesso] ADD [UsuarioId] nvarchar(36) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [Funcionarios] ADD [UsuarioId] nvarchar(36) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [Cartoes] ADD [Status] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE TABLE [HistoricoAssociacoesCartao] (
        [Id] bigint NOT NULL IDENTITY,
        [CartaoId] nvarchar(36) NOT NULL,
        [DataAssociacao] datetime2 NOT NULL,
        [DataDesassociacao] datetime2 NULL,
        [AssociadoPorFuncionarioId] nvarchar(36) NULL,
        [DesassociadoPorFuncionarioId] nvarchar(36) NULL,
        CONSTRAINT [PK_HistoricoAssociacoesCartao] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistoricoAssociacoesCartao_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [Cartoes] ([Id]),
        CONSTRAINT [FK_HistoricoAssociacoesCartao_Funcionarios_AssociadoPorFuncionarioId] FOREIGN KEY ([AssociadoPorFuncionarioId]) REFERENCES [Funcionarios] ([Id]),
        CONSTRAINT [FK_HistoricoAssociacoesCartao_Funcionarios_DesassociadoPorFuncionarioId] FOREIGN KEY ([DesassociadoPorFuncionarioId]) REFERENCES [Funcionarios] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE TABLE [HistoricoStatusCartao] (
        [Id] bigint NOT NULL IDENTITY,
        [CartaoId] nvarchar(36) NOT NULL,
        [Status] int NOT NULL,
        [DataHora] datetime2 NOT NULL,
        [Motivo] nvarchar(200) NULL,
        [AlteradoPorFuncionarioId] nvarchar(36) NULL,
        CONSTRAINT [PK_HistoricoStatusCartao] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_HistoricoStatusCartao_Status] CHECK ([Status] IN (0, 1, 2)),
        CONSTRAINT [FK_HistoricoStatusCartao_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [Cartoes] ([Id]),
        CONSTRAINT [FK_HistoricoStatusCartao_Funcionarios_AlteradoPorFuncionarioId] FOREIGN KEY ([AlteradoPorFuncionarioId]) REFERENCES [Funcionarios] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE TABLE [HistoricoStatusFuncionario] (
        [Id] bigint NOT NULL IDENTITY,
        [FuncionarioId] nvarchar(36) NOT NULL,
        [IsActive] bit NOT NULL,
        [DataHora] datetime2 NOT NULL,
        [Motivo] nvarchar(200) NULL,
        [AlteradoPorFuncionarioId] nvarchar(36) NULL,
        CONSTRAINT [PK_HistoricoStatusFuncionario] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistoricoStatusFuncionario_Funcionarios_AlteradoPorFuncionarioId] FOREIGN KEY ([AlteradoPorFuncionarioId]) REFERENCES [Funcionarios] ([Id]),
        CONSTRAINT [FK_HistoricoStatusFuncionario_Funcionarios_FuncionarioId] FOREIGN KEY ([FuncionarioId]) REFERENCES [Funcionarios] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE TABLE [HistoricoStatusUsuario] (
        [Id] bigint NOT NULL IDENTITY,
        [UsuarioId] nvarchar(36) NOT NULL,
        [IsActive] bit NOT NULL,
        [DataHora] datetime2 NOT NULL,
        [Motivo] nvarchar(200) NULL,
        [AlteradoPorFuncionarioId] nvarchar(36) NULL,
        CONSTRAINT [PK_HistoricoStatusUsuario] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistoricoStatusUsuario_Funcionarios_AlteradoPorFuncionarioId] FOREIGN KEY ([AlteradoPorFuncionarioId]) REFERENCES [Funcionarios] ([Id]),
        CONSTRAINT [FK_HistoricoStatusUsuario_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE TABLE [SolicitacoesGravacaoCartao] (
        [Id] nvarchar(36) NOT NULL,
        [ChaveIdempotencia] nvarchar(128) NOT NULL,
        [Credencial] nvarchar(16) NOT NULL,
        [UsuarioId] nvarchar(36) NOT NULL,
        [SolicitanteFuncionarioId] nvarchar(36) NOT NULL,
        [CartaoAnteriorId] nvarchar(36) NULL,
        [CartaoCriadoId] nvarchar(36) NULL,
        [Status] int NOT NULL,
        [DataSolicitacao] datetime2 NOT NULL,
        [DataUltimoEnvio] datetime2 NULL,
        [DataConclusao] datetime2 NULL,
        [DataCancelamento] datetime2 NULL,
        [QuantidadeEnvios] int NOT NULL,
        [MotivoReemissao] nvarchar(200) NULL,
        [MotivoFalha] nvarchar(200) NULL,
        [CanceladoPorFuncionarioId] nvarchar(36) NULL,
        CONSTRAINT [PK_SolicitacoesGravacaoCartao] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SolicitacoesGravacaoCartao_Status] CHECK ([Status] IN (0, 1, 2, 3, 4)),
        CONSTRAINT [FK_SolicitacoesGravacaoCartao_Cartoes_CartaoAnteriorId] FOREIGN KEY ([CartaoAnteriorId]) REFERENCES [Cartoes] ([Id]),
        CONSTRAINT [FK_SolicitacoesGravacaoCartao_Cartoes_CartaoCriadoId] FOREIGN KEY ([CartaoCriadoId]) REFERENCES [Cartoes] ([Id]),
        CONSTRAINT [FK_SolicitacoesGravacaoCartao_Funcionarios_CanceladoPorFuncionarioId] FOREIGN KEY ([CanceladoPorFuncionarioId]) REFERENCES [Funcionarios] ([Id]),
        CONSTRAINT [FK_SolicitacoesGravacaoCartao_Funcionarios_SolicitanteFuncionarioId] FOREIGN KEY ([SolicitanteFuncionarioId]) REFERENCES [Funcionarios] ([Id]),
        CONSTRAINT [FK_SolicitacoesGravacaoCartao_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]) WHERE [Email] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_LogsAcesso_CartaoId] ON [LogsAcesso] ([CartaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_LogsAcesso_UsuarioId] ON [LogsAcesso] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    EXEC(N'ALTER TABLE [LogsAcesso] ADD CONSTRAINT [CK_LogsAcesso_Identificadores] CHECK ((([CartaoId] IS NULL AND [UsuarioId] IS NULL) OR ([CartaoId] IS NOT NULL AND [UsuarioId] IS NOT NULL)) AND ([CartaoId] IS NULL OR [UidInformado] IS NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Funcionarios_UsuarioId] ON [Funcionarios] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    EXEC(N'ALTER TABLE [Cartoes] ADD CONSTRAINT [CK_Cartoes_Status] CHECK ([Status] IN (0, 1, 2))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_HistoricoAssociacoesCartao_AssociadoPorFuncionarioId] ON [HistoricoAssociacoesCartao] ([AssociadoPorFuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HistoricoAssociacoesCartao_CartaoId] ON [HistoricoAssociacoesCartao] ([CartaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_HistoricoAssociacoesCartao_DesassociadoPorFuncionarioId] ON [HistoricoAssociacoesCartao] ([DesassociadoPorFuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_HistoricoStatusCartao_AlteradoPorFuncionarioId] ON [HistoricoStatusCartao] ([AlteradoPorFuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_HistoricoStatusCartao_CartaoId] ON [HistoricoStatusCartao] ([CartaoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_HistoricoStatusFuncionario_AlteradoPorFuncionarioId] ON [HistoricoStatusFuncionario] ([AlteradoPorFuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_HistoricoStatusFuncionario_FuncionarioId] ON [HistoricoStatusFuncionario] ([FuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_HistoricoStatusUsuario_AlteradoPorFuncionarioId] ON [HistoricoStatusUsuario] ([AlteradoPorFuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_HistoricoStatusUsuario_UsuarioId] ON [HistoricoStatusUsuario] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesGravacaoCartao_CanceladoPorFuncionarioId] ON [SolicitacoesGravacaoCartao] ([CanceladoPorFuncionarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SolicitacoesGravacaoCartao_CartaoAnterior_Aberta] ON [SolicitacoesGravacaoCartao] ([CartaoAnteriorId]) WHERE [CartaoAnteriorId] IS NOT NULL AND [Status] IN (0, 1, 3)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesGravacaoCartao_CartaoCriadoId] ON [SolicitacoesGravacaoCartao] ([CartaoCriadoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SolicitacoesGravacaoCartao_Credencial] ON [SolicitacoesGravacaoCartao] ([Credencial]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SolicitacoesGravacaoCartao_Solicitante_Chave] ON [SolicitacoesGravacaoCartao] ([SolicitanteFuncionarioId], [ChaveIdempotencia]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    CREATE INDEX [IX_SolicitacoesGravacaoCartao_UsuarioId] ON [SolicitacoesGravacaoCartao] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [Cartoes] ADD CONSTRAINT [FK_Cartoes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [Funcionarios] ADD CONSTRAINT [FK_Funcionarios_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [LogsAcesso] ADD CONSTRAINT [FK_LogsAcesso_Cartoes_CartaoId] FOREIGN KEY ([CartaoId]) REFERENCES [Cartoes] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    ALTER TABLE [LogsAcesso] ADD CONSTRAINT [FK_LogsAcesso_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261015144920_AddSolicitacoesGravacaoCartao'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261015144920_AddSolicitacoesGravacaoCartao', N'10.0.13');
END;

COMMIT;
GO

