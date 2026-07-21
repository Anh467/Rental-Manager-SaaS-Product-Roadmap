import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import { propertyQueries } from "@/api/routes/properties";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export const Route = createFileRoute(
  "/_authenticated/_standard/(tenant)/properties/$propertyId",
)({
  loader: ({ context, params }) =>
    context.queryClient.ensureQueryData(propertyQueries.detail(params.propertyId)),
  component: PropertyDetailPage,
});

function PropertyDetailPage() {
  const { t } = useTranslation("property");
  const property = Route.useLoaderData();
  const propertyTypeKey = {
    "boarding-house": "form.types.boardingHouse",
    apartment: "form.types.apartment",
    "shared-house": "form.types.sharedHouse",
  }[property.propertyTypeId];

  return (
    <div className="p-6">
      <Card>
        <CardHeader>
          <CardTitle>{property.name}</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-3 text-sm sm:grid-cols-2">
          <p><span className="text-muted-foreground">{t("detail.code")}:</span> {property.code}</p>
          <p>
            <span className="text-muted-foreground">{t("detail.type")}:</span>{" "}
            {propertyTypeKey ? t(propertyTypeKey) : property.propertyTypeName ?? property.propertyTypeId}
          </p>
          <p><span className="text-muted-foreground">{t("detail.totalFloors")}:</span> {property.totalFloors}</p>
          <p>
            <span className="text-muted-foreground">{t("detail.status")}:</span>{" "}
            {property.isActive ? t("detail.active") : t("detail.inactive")}
          </p>
          <p className="sm:col-span-2">
            <span className="text-muted-foreground">{t("detail.address")}:</span> {property.address}
          </p>
          {property.description ? (
            <p className="sm:col-span-2">
              <span className="text-muted-foreground">{t("detail.description")}:</span> {property.description}
            </p>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
