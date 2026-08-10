-- Read predicate for [org].[StaffRole] only.
--
-- Allows the normal tenant session context match, or a precise impersonation
-- by OrganizationMembershipResolver so login-time membership discovery can
-- require an active StaffRole without an organization SESSION_CONTEXT. No
-- other principal and no SESSION_CONTEXT flag receives this exception. BLOCK
-- predicates on StaffRole remain the strict tenant predicate.
CREATE FUNCTION [org].[fn_StaffRoleReadPredicate]
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
