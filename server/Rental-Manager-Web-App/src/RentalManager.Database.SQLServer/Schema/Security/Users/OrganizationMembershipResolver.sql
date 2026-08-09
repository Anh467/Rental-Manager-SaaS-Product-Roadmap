-- Dedicated principal for login-time membership discovery. No login: cannot be
-- used as an application connection. Only the membership procedure may
-- impersonate this user, and only FILTER on [org].[StaffMembership] grants it
-- a narrow read exception.
CREATE USER [OrganizationMembershipResolver] WITHOUT LOGIN;
