-- Login-time membership lookup. Runs as OrganizationMembershipResolver so the
-- dedicated StaffMembership FILTER predicate can return rows before an
-- organization SESSION_CONTEXT exists. Not EXECUTE AS OWNER — owners do not
-- bypass RLS.
--
-- Returns only Active StaffMembership rows where the User and Organization are
-- also active. Role activity is enforced later under organization context when
-- permissions are resolved; listing does not require SELECT on [org].[Role].
CREATE PROCEDURE [dbo].[usp_ListActiveOrganizationMembershipsForUser]
    @UserId UNIQUEIDENTIFIER
WITH EXECUTE AS N'OrganizationMembershipResolver'
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        membership.[Id] AS [StaffMembershipId],
        membership.[OrganizationId],
        organization.[Name],
        membership.[CreatedAt]
    FROM [org].[StaffMembership] AS membership
    INNER JOIN [dbo].[User] AS [user]
        ON [user].[Id] = membership.[UserId]
    INNER JOIN [dbo].[Organization] AS organization
        ON organization.[Id] = membership.[OrganizationId]
    WHERE membership.[UserId] = @UserId
      AND membership.[Status] = 2 -- Active
      AND membership.[DeletedAt] IS NULL
      AND [user].[IsActive] = 1
      AND [user].[DeletedAt] IS NULL
      AND organization.[IsActive] = 1
      AND organization.[DeletedAt] IS NULL
    ORDER BY membership.[CreatedAt] ASC, membership.[OrganizationId] ASC;
END
