-- Row level security predicate for every [org] table.
--
-- Fails closed by construction: when SESSION_CONTEXT('OrganizationId') has not
-- been set the cast yields NULL, the comparison evaluates to UNKNOWN and the
-- function returns no row. There is deliberately no IS_MEMBER check and no
-- bypass flag, so a global administrator must also select an organization they
-- have been authorized for.
CREATE FUNCTION [org].[fn_OrganizationAccessPredicate]
(
    @OrganizationId UNIQUEIDENTIFIER
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS [fn_AccessResult]
    WHERE @OrganizationId = CAST(SESSION_CONTEXT(N'OrganizationId') AS UNIQUEIDENTIFIER);
