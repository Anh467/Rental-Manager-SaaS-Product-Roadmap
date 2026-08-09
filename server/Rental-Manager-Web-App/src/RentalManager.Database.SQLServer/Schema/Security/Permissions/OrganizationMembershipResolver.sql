-- Least privilege for the membership resolver: read active memberships, the
-- active StaffRole → Role join needed to enforce usable roles at list/select
-- time, and the matching dbo.User / dbo.Organization rows. No write rights.
-- No rights on other [org] tables. CONNECT is required so WITH EXECUTE AS on
-- the login procedure can enter the database as this WITHOUT LOGIN user.
GRANT CONNECT TO [OrganizationMembershipResolver];
GO

GRANT SELECT ON OBJECT::[org].[StaffMembership] TO [OrganizationMembershipResolver];
GO

GRANT SELECT ON OBJECT::[org].[StaffRole] TO [OrganizationMembershipResolver];
GO

GRANT SELECT ON OBJECT::[org].[Role] TO [OrganizationMembershipResolver];
GO

GRANT SELECT ON OBJECT::[dbo].[User] TO [OrganizationMembershipResolver];
GO

GRANT SELECT ON OBJECT::[dbo].[Organization] TO [OrganizationMembershipResolver];
GO
