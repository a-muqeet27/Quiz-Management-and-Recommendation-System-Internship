-- Allow decimal scores / marks for questions and quiz attempts.
-- Run against DatabaseQuizPortal before deploying the app changes.

-- Question.Score has a CHECK constraint that blocks ALTER COLUMN.
IF OBJECT_ID(N'[dbo].[CK__Question__Score__440B1D61]', N'C') IS NOT NULL
    ALTER TABLE [dbo].[Question] DROP CONSTRAINT [CK__Question__Score__440B1D61];

IF COL_LENGTH(N'dbo.Question', N'Score') IS NOT NULL
    AND EXISTS (
        SELECT 1
        FROM sys.columns c
        JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.Question')
          AND c.name = N'Score'
          AND t.name = N'int'
    )
BEGIN
    ALTER TABLE [dbo].[Question]
        ALTER COLUMN [Score] DECIMAL(10, 2) NOT NULL;
END

IF OBJECT_ID(N'[dbo].[CK_Question_Score]', N'C') IS NULL
    AND OBJECT_ID(N'[dbo].[CK__Question__Score__440B1D61]', N'C') IS NULL
BEGIN
    ALTER TABLE [dbo].[Question]
        ADD CONSTRAINT [CK_Question_Score] CHECK ([Score] > 0 AND [Score] <= 100);
END

ALTER TABLE [dbo].[Quiz]
    ALTER COLUMN [TotalMarks] DECIMAL(10, 2) NOT NULL;

ALTER TABLE [dbo].[QuizScore]
    ALTER COLUMN [TotalMarks] DECIMAL(10, 2) NOT NULL;

ALTER TABLE [dbo].[QuizScore]
    ALTER COLUMN [ObtainedMarks] DECIMAL(10, 2) NOT NULL;

IF OBJECT_ID(N'[dbo].[DF__QuizChoic__Marks__1F98B2C1]', N'D') IS NOT NULL
    ALTER TABLE [dbo].[QuizChoice] DROP CONSTRAINT [DF__QuizChoic__Marks__1F98B2C1];

ALTER TABLE [dbo].[QuizChoice]
    ALTER COLUMN [MarksObtained] DECIMAL(10, 2) NOT NULL;

IF OBJECT_ID(N'[dbo].[DF_QuizChoice_MarksObtained]', N'D') IS NULL
    ALTER TABLE [dbo].[QuizChoice]
        ADD CONSTRAINT [DF_QuizChoice_MarksObtained] DEFAULT ((0)) FOR [MarksObtained];
