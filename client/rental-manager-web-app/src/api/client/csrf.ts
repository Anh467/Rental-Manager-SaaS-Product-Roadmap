const CSRF_HEADER = "X-CSRF-TOKEN";

let cachedRequestToken: string | null = null;
let inflight: Promise<string> | null = null;

export function getCsrfHeaderName() {
  return CSRF_HEADER;
}

export function getCachedCsrfToken() {
  return cachedRequestToken;
}

export function clearCsrfToken() {
  cachedRequestToken = null;
  inflight = null;
}

export async function ensureCsrfToken(fetcher: () => Promise<string>): Promise<string> {
  if (cachedRequestToken) return cachedRequestToken;

  if (!inflight) {
    inflight = fetcher()
      .then((token) => {
        cachedRequestToken = token;
        return token;
      })
      .finally(() => {
        inflight = null;
      });
  }

  return inflight;
}

export async function refreshCsrfToken(fetcher: () => Promise<string>): Promise<string> {
  clearCsrfToken();
  return ensureCsrfToken(fetcher);
}

export function isUnsafeHttpMethod(method?: string) {
  const normalized = (method ?? "get").toUpperCase();
  return normalized === "POST"
    || normalized === "PUT"
    || normalized === "PATCH"
    || normalized === "DELETE";
}
