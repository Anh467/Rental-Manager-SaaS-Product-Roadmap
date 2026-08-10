/**
 * Client mirror of the canonical backend message catalog in
 * `RentalManager.BuildingBlocks.Contracts.Messaging.MessageCatalog`.
 *
 * Do not treat this file as an independent source of truth: CI
 * `MessageCatalogParityTests` compares these arrays (and locale JSON) to the
 * C# catalog and fails on missing/extra/typo keys. Keys are never renumbered
 * or reused: retiring a message moves it from "active" to "deprecated".
 */

export const ACTIVE_SUCCESS_MESSAGE_KEYS = [
  "SCS-001", "SCS-002", "SCS-003", "SCS-004", "SCS-005",
  "SCS-006", "SCS-007", "SCS-008", "SCS-009", "SCS-010",
  "SCS-011", "SCS-012", "SCS-013", "SCS-014", "SCS-015",
  "SCS-016", "SCS-017", "SCS-018", "SCS-019", "SCS-020",
] as const;

/** No success keys have been retired yet. Kept for symmetry with errors. */
export const DEPRECATED_SUCCESS_MESSAGE_KEYS = [] as const;

export const ACTIVE_ERROR_MESSAGE_KEYS = [
  "ERR-001", "ERR-002", "ERR-003", "ERR-004", "ERR-005",
  "ERR-006", "ERR-007", "ERR-008", "ERR-009", "ERR-010",
  "ERR-011", "ERR-012", "ERR-013", "ERR-014", "ERR-015",
  "ERR-016", "ERR-017", "ERR-018", "ERR-019", "ERR-020",
  "ERR-021", "ERR-022", "ERR-023", "ERR-024", "ERR-025",
  "ERR-026", "ERR-028", "ERR-029", "ERR-030", "ERR-031",
  "ERR-032", "ERR-033", "ERR-040", "ERR-041", "ERR-048",
  "ERR-049", "ERR-050",
] as const;

export const DEPRECATED_ERROR_MESSAGE_KEYS = [
  "ERR-027", "ERR-034", "ERR-035", "ERR-036", "ERR-037",
  "ERR-038", "ERR-039", "ERR-042", "ERR-043", "ERR-044",
  "ERR-045", "ERR-046", "ERR-047",
] as const;

export const ALL_SUCCESS_MESSAGE_KEYS = [
  ...ACTIVE_SUCCESS_MESSAGE_KEYS,
  ...DEPRECATED_SUCCESS_MESSAGE_KEYS,
] as const;

export const ALL_ERROR_MESSAGE_KEYS = [
  ...ACTIVE_ERROR_MESSAGE_KEYS,
  ...DEPRECATED_ERROR_MESSAGE_KEYS,
] as const;

/** Every key in the catalog, success and error, active and deprecated. Used for locale parity checks. */
export const ALL_MESSAGE_KEYS = [
  ...ALL_SUCCESS_MESSAGE_KEYS,
  ...ALL_ERROR_MESSAGE_KEYS,
] as const;

export type ActiveSuccessMessageKey = (typeof ACTIVE_SUCCESS_MESSAGE_KEYS)[number];
export type DeprecatedSuccessMessageKey = (typeof DEPRECATED_SUCCESS_MESSAGE_KEYS)[number];
export type ActiveErrorMessageKey = (typeof ACTIVE_ERROR_MESSAGE_KEYS)[number];
export type DeprecatedErrorMessageKey = (typeof DEPRECATED_ERROR_MESSAGE_KEYS)[number];

/**
 * New responses must only ever carry an active success key. There is no
 * legitimate reason to parse a deprecated success key, since none exist.
 */
export type SuccessMessageKey = ActiveSuccessMessageKey;

/**
 * Error responses may still be read from services/caches that predate a
 * deprecation, so parsing accepts both active and deprecated keys. New
 * code must never *emit* a deprecated key.
 */
export type ErrorMessageKey = ActiveErrorMessageKey | DeprecatedErrorMessageKey;

export type MessageKey = SuccessMessageKey | ErrorMessageKey;

const activeErrorKeySet: ReadonlySet<string> = new Set(ACTIVE_ERROR_MESSAGE_KEYS);
const deprecatedErrorKeySet: ReadonlySet<string> = new Set(DEPRECATED_ERROR_MESSAGE_KEYS);
const activeSuccessKeySet: ReadonlySet<string> = new Set(ACTIVE_SUCCESS_MESSAGE_KEYS);
const allErrorKeySet: ReadonlySet<string> = new Set(ALL_ERROR_MESSAGE_KEYS);

export function isActiveSuccessMessageKey(value: string): value is ActiveSuccessMessageKey {
  return activeSuccessKeySet.has(value);
}

export function isActiveErrorMessageKey(value: string): value is ActiveErrorMessageKey {
  return activeErrorKeySet.has(value);
}

export function isDeprecatedErrorMessageKey(value: string): value is DeprecatedErrorMessageKey {
  return deprecatedErrorKeySet.has(value);
}

export function isKnownErrorMessageKey(value: string): value is ErrorMessageKey {
  return allErrorKeySet.has(value);
}
