-- Single isolation policy covering every [org] table.
--
-- FILTER hides rows belonging to another organization, which is what makes a
-- cross-organization read indistinguishable from a missing row. AFTER INSERT and
-- AFTER UPDATE block predicates stop a write from stamping a row with an
-- OrganizationId other than the one in the session context. BEFORE predicates
-- are unnecessary because the filter already removes other organizations' rows
-- from the update and delete targets.
CREATE SECURITY POLICY [org].[OrganizationIsolationPolicy]
    ADD FILTER PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Field],
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Field] AFTER INSERT,
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Field] AFTER UPDATE,

    ADD FILTER PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[FieldOption],
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[FieldOption] AFTER INSERT,
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[FieldOption] AFTER UPDATE,

    ADD FILTER PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Role],
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Role] AFTER INSERT,
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Role] AFTER UPDATE,

    ADD FILTER PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[RolePermission],
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[RolePermission] AFTER INSERT,
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[RolePermission] AFTER UPDATE,

    ADD FILTER PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Organization],
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Organization] AFTER INSERT,
    ADD BLOCK PREDICATE [org].[fn_OrganizationAccessPredicate]([OrganizationId])
        ON [org].[Organization] AFTER UPDATE
WITH (STATE = ON, SCHEMABINDING = ON);
