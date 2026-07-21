import { createFileRoute, redirect } from "@tanstack/react-router";

export const Route = createFileRoute("/")({
  beforeLoad: () => {
    throw redirect({
      to: "/properties",
      search: {
        page: 1,
        pageSize: 20,
        search: "",
        propertyTypeId: "",
      },
      replace: true,
    });
  },
  component: () => null,
});
