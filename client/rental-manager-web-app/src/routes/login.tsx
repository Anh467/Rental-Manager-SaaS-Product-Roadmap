import { useState } from "react";
import { createFileRoute, redirect, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

import { useAuth } from "@/features/auth/auth-provider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/login")({
  beforeLoad: () => {
    if (localStorage.getItem("access_token")) throw redirect({ to: "/", replace: true });
  },
  component: LoginPage,
});

function LoginPage() {
  const { t } = useTranslation("common");
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [organizationId, setOrganizationId] = useState("");
  const [error, setError] = useState<string>();
  const [submitting, setSubmitting] = useState(false);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(undefined);
    setSubmitting(true);
    try {
      await login({ email, password, organizationId: organizationId || undefined });
      await navigate({ to: "/" });
    } catch {
      setError("Unable to sign in. Check your credentials and try again.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <main className="grid min-h-dvh place-items-center bg-muted/30 p-4">
      <form onSubmit={(event) => void submit(event)} className="w-full max-w-md space-y-5 rounded-lg border bg-background p-6 shadow-sm">
        <div><h1 className="text-xl font-semibold">Rental Manager</h1><p className="mt-1 text-sm text-muted-foreground">Sign in to continue.</p></div>
        {error ? <p role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">{error}</p> : null}
        <div className="space-y-2"><Label htmlFor="email">Email</Label><Input id="email" type="email" autoComplete="email" required value={email} onChange={(event) => setEmail(event.target.value)} /></div>
        <div className="space-y-2"><Label htmlFor="password">Password</Label><Input id="password" type="password" autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} /></div>
        <div className="space-y-2"><Label htmlFor="organizationId">Organization ID (optional)</Label><Input id="organizationId" value={organizationId} onChange={(event) => setOrganizationId(event.target.value)} /></div>
        <Button className="w-full" type="submit" disabled={submitting}>{submitting ? t("actions.saving") : "Sign in"}</Button>
      </form>
    </main>
  );
}
