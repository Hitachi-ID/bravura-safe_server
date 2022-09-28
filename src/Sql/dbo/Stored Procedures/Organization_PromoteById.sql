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