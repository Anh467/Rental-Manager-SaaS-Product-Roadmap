-- Login-time membership lookup. Runs as OrganizationMembershipResolver so the
-- dedicated StaffMembership / Role / StaffRole FILTER predicates can return
-- rows before an organization SESSION_CONTEXT exists. Not EXECUTE AS OWNER —
-- owners do not bypass RLS.
--
-- Returns only Active StaffMembership rows where the User and Organization are
-- also active and at least one active StaffRole → active org.Role exists.
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
      AND EXISTS
      (
          SELECT 1
          FROM [org].[StaffRole] AS staffRole
          INNER JOIN [org].[Role] AS [role]
              ON [role].[Id] = staffRole.[RoleId]
             AND [role].[OrganizationId] = staffRole.[OrganizationId]
          WHERE staffRole.[StaffMembershipId] = membership.[Id]
            AND staffRole.[OrganizationId] = membership.[OrganizationId]
            AND [role].[IsActive] = 1
            AND [role].[DeletedAt] IS NULL
      )
    ORDER BY membership.[CreatedAt] ASC, membership.[OrganizationId] ASC;
END
