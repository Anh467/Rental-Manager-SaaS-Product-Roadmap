import type { OrganizationOption } from "@/api/routes/auth";

export type PendingOrganizationSelection = {
  selectionTicket: string;
  organizations: OrganizationOption[];
};

let pendingSelection: PendingOrganizationSelection | null = null;

export function setPendingOrganizationSelection(selection: PendingOrganizationSelection) {
  pendingSelection = selection;
}

export function getPendingOrganizationSelection() {
  return pendingSelection;
}

export function clearPendingOrganizationSelection() {
  pendingSelection = null;
}
