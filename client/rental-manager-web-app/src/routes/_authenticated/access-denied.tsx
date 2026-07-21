import { createFileRoute } from "@tanstack/react-router";
import { ShieldX } from "lucide-react";

import { EmptyState } from "@/components/common/page";

export const Route = createFileRoute("/_authenticated/access-denied")({
  component: AccessDeniedPage,
});

function AccessDeniedPage() {
  return (
    <div className="p-6">
      <EmptyState
        title="Bạn không có quyền truy cập"
        description="Hãy liên hệ quản trị viên của tổ chức để được cấp quyền phù hợp."
        action={<ShieldX className="h-6 w-6 text-destructive" />}
      />
    </div>
  );
}
