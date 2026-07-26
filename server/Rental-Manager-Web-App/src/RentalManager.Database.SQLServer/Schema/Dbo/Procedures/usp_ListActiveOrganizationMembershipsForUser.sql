-- Login-time membership lookup. Runs as OrganizationMembershipResolver so the
-- dedicated OrganizationUser FILTER predicate can return rows before an
-- organization SESSION_CONTEXT exists. Not EXECUTE AS OWNER — owners do not
-- bypass RLS.
CREATE PROCEDURE [dbo].[usp_ListActiveOrganizationMembershipsForUser]
    @UserId UNIQUEIDENTIFIER
WITH EXECUTE AS N'OrganizationMembershipResolver'
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
