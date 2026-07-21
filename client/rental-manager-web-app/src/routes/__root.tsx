import type { QueryClient } from "@tanstack/react-query";
import { Outlet, createRootRouteWithContext } from "@tanstack/react-router";

import { ErrorState, LoadingState } from "@/components/common/page";

export type RouterContext = {
  queryClient: QueryClient;
};

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
  pendingComponent: () => (
    <div className="p-6">
      <LoadingState label="Đang tải trang..." />
    </div>
  ),
  errorComponent: ({ error, reset }) => (
    <div className="p-6">
      <ErrorState
        title="Không thể tải trang"
        description={error instanceof Error ? error.message : "Đã xảy ra lỗi không xác định."}
        onRetry={reset}
      />
    </div>
  ),
  notFoundComponent: () => (
    <div className="p-6">
      <ErrorState title="Không tìm thấy trang" description="Đường dẫn bạn truy cập không tồn tại." />
    </div>
  ),
});

function RootComponent() {
  return <Outlet />;
}
