import { createFileRoute } from "@tanstack/react-router";

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
  const property = Route.useLoaderData();

  return (
    <div className="p-6">
      <Card>
        <CardHeader>
          <CardTitle>{property.name}</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-3 text-sm sm:grid-cols-2">
          <p>
            <span className="text-muted-foreground">Mã:</span> {property.code}
          </p>
          <p>
            <span className="text-muted-foreground">Loại:</span>{" "}
            {property.propertyTypeName ?? property.propertyTypeId}
          </p>
          <p>
            <span className="text-muted-foreground">Số tầng:</span> {property.totalFloors}
          </p>
          <p>
            <span className="text-muted-foreground">Trạng thái:</span>{" "}
            {property.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}
          </p>
          <p className="sm:col-span-2">
            <span className="text-muted-foreground">Địa chỉ:</span> {property.address}
          </p>
          {property.description ? (
            <p className="sm:col-span-2">
              <span className="text-muted-foreground">Mô tả:</span> {property.description}
            </p>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
