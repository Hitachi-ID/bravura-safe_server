-- Update Organization_DemoteById
IF OBJECT_ID('[dbo].[Organization_DemoteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_DemoteById]
END
GO

CREATE PROCEDURE [dbo].[Organization_DemoteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN

    SET NOCOUNT ON

    UPDATE
        [dbo].[Organization]
    SET
        [Plan] = 'Teams',
        [PlanType] = 12,
        [Use2fa] = 0,
        [UseSso] = 0,
        [UseDirectory] = 0,
        [UseResetPassword] = 0,
        [RevisionDate] = GETUTCDATE()
    WHERE
        [Id] = @Id
END
GO

-- Update Organization_PromoteById
IF OBJECT_ID('[dbo].[Organization_PromoteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_PromoteById]
END
GO

CREATE PROCEDURE [dbo].[Organization_PromoteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN

    SET NOCOUNT ON

    UPDATE
        [dbo].[Organization]
    SET
        [Plan] = 'Enterprise',
        [PlanType] = 13,
        [RevisionDate] = GETUTCDATE()
    WHERE
        [Id] = @Id
END
GO
