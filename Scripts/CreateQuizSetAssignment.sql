-- One student ↔ one quiz set assignment for a parent quiz family.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'[dbo].[QuizSetAssignment]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[QuizSetAssignment]
    (
        [QuizSetAssignmentId] INT IDENTITY(1,1) NOT NULL,
        [ParentQuizId] INT NOT NULL,
        [QuizId] INT NOT NULL,
        [UserId] INT NULL,
        [AssignedDate] DATETIME2 NOT NULL CONSTRAINT [DF_QuizSetAssignment_AssignedDate] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_QuizSetAssignment] PRIMARY KEY ([QuizSetAssignmentId]),
        CONSTRAINT [FK_QuizSetAssignment_ParentQuiz] FOREIGN KEY ([ParentQuizId]) REFERENCES [dbo].[Quiz]([QuizId]),
        CONSTRAINT [FK_QuizSetAssignment_Quiz] FOREIGN KEY ([QuizId]) REFERENCES [dbo].[Quiz]([QuizId]),
        CONSTRAINT [FK_QuizSetAssignment_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]),
        CONSTRAINT [UQ_QuizSetAssignment_QuizId] UNIQUE ([QuizId])
    );
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_QuizSetAssignment_Parent_User'
      AND object_id = OBJECT_ID(N'dbo.QuizSetAssignment')
)
BEGIN
    CREATE UNIQUE INDEX [UX_QuizSetAssignment_Parent_User]
        ON [dbo].[QuizSetAssignment] ([ParentQuizId], [UserId])
        WHERE [UserId] IS NOT NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_QuizSetAssignment_ParentQuizId'
      AND object_id = OBJECT_ID(N'dbo.QuizSetAssignment')
)
BEGIN
    CREATE INDEX [IX_QuizSetAssignment_ParentQuizId]
        ON [dbo].[QuizSetAssignment] ([ParentQuizId]);
END
