-- Read predicate for [org].[StaffMembership] only.
--
-- Allows the normal tenant session context match, or a precise impersonation
-- by OrganizationMembershipResolver so login can discover memberships before
-- an organization token exists. No other principal and no SESSION_CONTEXT flag
-- receives this exception.
CREATE FUNCTION [org].[fn_StaffMembershipReadPredicate]
(
    @OrganizationId UNIQUEIDENTIFIER
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS [fn_AccessResult]
    WHERE @OrganizationId = CAST(SESSION_CONTEXT(N'OrganizationId') AS UNIQUEIDENTIFIER)
       OR USER_NAME() = N'OrganizationMembershipResolver';
