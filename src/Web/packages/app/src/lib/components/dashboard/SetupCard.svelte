<script lang="ts">
  import {
    HeartPulse,
    Smartphone,
    Syringe,
    Shield,
    Activity,
    CheckCircle2,
    ChevronRight,
    X,
  } from "lucide-svelte";
  import { Card, CardContent, CardHeader, CardTitle } from "$lib/components/ui/card";
  import { Button } from "$lib/components/ui/button";
  import * as patientRemote from "$api/generated/patientRecords.generated.remote";
  import * as profileRemote from "$api/generated/profiles.generated.remote";
  import { getMembers } from "$api/generated/memberInvites.generated.remote";

  let dismissed = $state(false);

  const patientRecord = patientRemote.getPatientRecord();
  const devices = patientRemote.getDevices();
  const insulins = patientRemote.getInsulins();
  const profileSummary = profileRemote.getProfileSummary(undefined);
  const members = getMembers(undefined);

  // Derive completion states
  const patientConfigured = $derived(!!patientRecord.current?.diabetesType);
  const devicesConfigured = $derived(
    (devices.current ?? []).some((d) => d.isCurrent === true)
  );
  const insulinsConfigured = $derived(
    (insulins.current ?? []).some((i) => i.isCurrent === true)
  );

  const publicMember = $derived(
    (members.current ?? []).find((m) => m.name === "Public")
  );
  const permissionsConfigured = $derived(
    (publicMember?.roles ?? []).length > 0 ||
      (publicMember?.directPermissions ?? []).length > 0
  );

  const profileConfigured = $derived(
    ((profileSummary.current?.basalSchedules as any[]) ?? []).length > 0
  );

  type SetupStep = {
    title: string;
    icon: typeof HeartPulse;
    complete: boolean;
    href: string;
  };

  const steps: SetupStep[] = $derived([
    {
      title: "Patient details",
      icon: HeartPulse,
      complete: patientConfigured,
      href: "/settings/patient",
    },
    {
      title: "Devices",
      icon: Smartphone,
      complete: devicesConfigured,
      href: "/settings/patient",
    },
    {
      title: "Insulins",
      icon: Syringe,
      complete: insulinsConfigured,
      href: "/settings/patient",
    },
    {
      title: "Sharing & Privacy",
      icon: Shield,
      complete: permissionsConfigured,
      href: "/settings/members",
    },
    {
      title: "Therapy profile",
      icon: Activity,
      complete: profileConfigured,
      href: "/settings/profile",
    },
  ]);

  const allComplete = $derived(steps.every((s) => s.complete));
  const visible = $derived(!dismissed && !allComplete);
</script>

{#if visible}
  <Card>
    <CardHeader class="flex flex-row items-center justify-between px-3 @md:px-6 pb-2">
      <CardTitle class="text-base">Finish setting up</CardTitle>
      <Button
        variant="ghost"
        size="icon"
        class="h-7 w-7 -mr-2"
        onclick={() => (dismissed = true)}
      >
        <X class="h-4 w-4" />
        <span class="sr-only">Dismiss</span>
      </Button>
    </CardHeader>
    <CardContent class="px-3 @md:px-6 pt-0">
      <ul class="space-y-1">
        {#each steps as step}
          <li>
            {#if step.complete}
              <div
                class="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm text-muted-foreground line-through"
              >
                <CheckCircle2 class="h-4 w-4 shrink-0 text-green-500" />
                <span>{step.title}</span>
              </div>
            {:else}
              <a
                href={step.href}
                class="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-muted transition-colors"
              >
                <step.icon class="h-4 w-4 shrink-0" />
                <span class="flex-1">{step.title}</span>
                <ChevronRight class="h-4 w-4 shrink-0 text-muted-foreground" />
              </a>
            {/if}
          </li>
        {/each}
      </ul>
    </CardContent>
  </Card>
{/if}
