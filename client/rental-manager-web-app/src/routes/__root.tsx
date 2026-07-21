import type { QueryClient } from "@tanstack/react-query";
import { Outlet, createRootRouteWithContext } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import { isApiError } from "@/api/client";
import { ErrorState, LoadingState, PageContent } from "@/components/common/page";
import { translateApiError } from "@/i18n/api-message";

export type RouterContext = {
  queryClient: QueryClient;
};

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
  pendingComponent: RootPendingComponent,
  errorComponent: RootErrorComponent,
  notFoundComponent: RootNotFoundComponent,
});

function RootComponent() {
  return <Outlet />;
}

function RootPendingComponent() {
  const { t } = useTranslation("common");
  return <PageContent><LoadingState label={t("state.loadingPage")} /></PageContent>;
}

function RootErrorComponent({ error, reset }: { error: unknown; reset: () => void }) {
  const { t } = useTranslation("common");
  const description = isApiError(error)
    ? translateApiError(error)
    : error instanceof Error
      ? error.message
      : t("state.unknownError");

  return (
    <PageContent>
      <ErrorState title={t("state.pageLoadError")} description={description} onRetry={reset} />
    </PageContent>
  );
}

function RootNotFoundComponent() {
  const { t } = useTranslation("common");
  return (
    <PageContent>
      <ErrorState title={t("state.notFound")} description={t("state.notFoundDescription")} />
    </PageContent>
  );
}
