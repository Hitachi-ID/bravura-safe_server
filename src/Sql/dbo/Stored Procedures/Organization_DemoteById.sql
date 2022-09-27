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