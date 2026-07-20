import { Building2 } from "lucide-react";

import {
  PropertyForm,
  type PropertyFormValues,
} from "@/features/properties/components/property-form";

export default function App() {
  const handleSubmit = async (values: PropertyFormValues) => {
    await new Promise((resolve) => window.setTimeout(resolve, 500));
    window.alert(`Đã submit: ${JSON.stringify(values, null, 2)}`);
  };

  return (
    <main className="min-h-screen px-4 py-10 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-3xl space-y-6">
        <header className="space-y-2">
          <div className="flex items-center gap-2 text-primary">
            <Building2 className="h-5 w-5" />
            <span className="text-sm font-semibold">Rental Manager SaaS</span>
          </div>
          <h1 className="text-2xl font-semibold tracking-tight">
            Reusable form system
          </h1>
          <p className="text-sm text-muted-foreground">
            React Hook Form + Zod + shadcn/ui. Feature chỉ khai báo schema,
            default values và ghép các field component chung.
          </p>
        </header>

        <div className="rounded-lg border bg-card p-6 text-card-foreground shadow-sm sm:p-8">
          <PropertyForm onSubmit={handleSubmit} />
        </div>
      </div>
    </main>
  );
}
