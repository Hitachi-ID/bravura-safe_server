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
        [Seats] = 1000000,
        [Use2fa] = 1,
        [UseSso] = 1,
        [UseResetPassword] = 1, 
        [RevisionDate] = GETUTCDATE()
    WHERE
        [Id] = @Id
END
