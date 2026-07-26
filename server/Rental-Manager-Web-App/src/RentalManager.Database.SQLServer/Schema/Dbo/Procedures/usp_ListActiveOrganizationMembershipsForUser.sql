-- Login-time membership lookup. EXECUTE AS OWNER bypasses RLS so the API can
-- discover which organizations a user belongs to before an organization token
-- has been issued.
CREATE PROCEDURE [dbo].[usp_ListActiveOrganizationMembershipsForUser]
    @UserId UNIQUEIDENTIFIER
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        membership.[OrganizationId],
        membership.[RoleId],
        membership.[CreatedAt]
    FROM [org].[OrganizationUser] AS membership
    INNER JOIN [dbo].[User] AS [user]
        ON [user].[Id] = membership.[UserId]
    WHERE membership.[UserId] = @UserId
      AND membership.[IsActive] = 1
      AND [user].[IsActive] = 1
      AND [user].[DeletedAt] IS NULL
    ORDER BY membership.[CreatedAt] ASC, membership.[OrganizationId] ASC;
END
