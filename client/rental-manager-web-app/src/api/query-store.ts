import { mergeQueryKeys, type inferQueryKeyStore } from "@lukemorales/query-key-factory";

import { propertiesQueryStore } from "@/api/routes/properties/queries";
import { roomsQueryStore } from "@/api/routes/rooms/queries";
import { globalFieldsQueryStore } from "@/api/routes/global-fields/queries";

export const queryStore = mergeQueryKeys(propertiesQueryStore, roomsQueryStore, globalFieldsQueryStore);
export type QueryStore = inferQueryKeyStore<typeof queryStore>;
