-- Read predicate for [org].[Role] only.
--
-- Allows the normal tenant session context match, or a precise impersonation
-- by OrganizationMembershipResolver so login-time membership discovery can
-- require an active Role without an organization SESSION_CONTEXT. No other
-- principal and no SESSION_CONTEXT flag receives this exception. BLOCK
-- predicates on Role remain the strict tenant predicate.
CREATE FUNCTION [org].[fn_RoleReadPredicate]
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
