-- Least privilege for the membership resolver: read memberships and the
-- matching dbo.User row only. No write rights. No rights on other [org] tables.
-- CONNECT is required so WITH EXECUTE AS on the login procedure can enter the
-- database as this WITHOUT LOGIN user.
GRANT CONNECT TO [OrganizationMembershipResolver];
GO

GRANT SELECT ON OBJECT::[org].[OrganizationUser] TO [OrganizationMembershipResolver];
GO

GRANT SELECT ON OBJECT::[dbo].[User] TO [OrganizationMembershipResolver];
GO
