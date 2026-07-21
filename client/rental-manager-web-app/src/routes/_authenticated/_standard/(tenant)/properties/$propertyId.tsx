import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import { propertyQueries } from "@/api/routes/properties";
import { PageContent } from "@/components/common/page";
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
    <PageContent>
      <Card>
        <CardHeader>
          <CardTitle>{property.name}</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-x-6 gap-y-4 text-sm sm:grid-cols-2">
            <div className="min-w-0">
              <dt className="text-muted-foreground">{t("detail.code")}</dt>
              <dd className="mt-1 break-words">{property.code}</dd>
            </div>
            <div className="min-w-0">
              <dt className="text-muted-foreground">{t("detail.type")}</dt>
              <dd className="mt-1 break-words">
                {propertyTypeKey ? t(propertyTypeKey) : property.propertyTypeName ?? property.propertyTypeId}
              </dd>
            </div>
            <div className="min-w-0">
              <dt className="text-muted-foreground">{t("detail.totalFloors")}</dt>
              <dd className="mt-1">{property.totalFloors}</dd>
            </div>
            <div className="min-w-0">
              <dt className="text-muted-foreground">{t("detail.status")}</dt>
              <dd className="mt-1">{property.isActive ? t("detail.active") : t("detail.inactive")}</dd>
            </div>
            <div className="min-w-0 sm:col-span-2">
              <dt className="text-muted-foreground">{t("detail.address")}</dt>
              <dd className="mt-1 break-words">{property.address}</dd>
            </div>
            {property.description ? (
              <div className="min-w-0 sm:col-span-2">
                <dt className="text-muted-foreground">{t("detail.description")}</dt>
                <dd className="mt-1 whitespace-pre-wrap break-words">{property.description}</dd>
              </div>
            ) : null}
          </dl>
        </CardContent>
      </Card>
    </PageContent>
  );
}
