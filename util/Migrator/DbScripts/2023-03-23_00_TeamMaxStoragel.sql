-- Set Team max storage to 100GB
BEGIN
    UPDATE [dbo].[Organization]
    SET [MaxStorageGb] = 100;
END
GO
