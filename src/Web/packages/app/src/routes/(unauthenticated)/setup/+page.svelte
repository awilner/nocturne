<script lang="ts">
  import * as Card from "$lib/components/ui/card";
  import { Button } from "$lib/components/ui/button";
  import { ArrowRightLeft, Plus } from "lucide-svelte";
  import { goto } from "$app/navigation";
  import { markSetupComplete } from "./setup.remote";
  import ConnectorSetup from "$lib/components/connectors/ConnectorSetup.svelte";
  import { getAuthState } from "$routes/(unauthenticated)/auth/auth.remote";

  const authStateQuery = getAuthState();
  const isAuthenticated = $derived(
    authStateQuery.current?.isAuthenticated ?? false,
  );

  // Redirect to account creation if not authenticated
  $effect(() => {
    if (!authStateQuery.loading && !isAuthenticated) {
      goto("/setup/account", { replaceState: true });
    }
  });

  let showMigration = $state(false);

  async function handleStartFresh() {
    await goto("/setup/connect");
  }

  async function handleMigrationComplete() {
    await markSetupComplete();
    await goto("/", { invalidateAll: true });
  }
</script>

<div class="container mx-auto max-w-2xl p-6 space-y-6">
  <div>
    <h1 class="text-2xl font-bold tracking-tight">Get Started</h1>
    <p class="text-muted-foreground">
      Choose how you'd like to set up Nocturne.
    </p>
  </div>

  {#if showMigration}
    <div class="space-y-4">
      <Button variant="ghost" size="sm" onclick={() => (showMigration = false)}>
        &larr; Back
      </Button>
      <ConnectorSetup
        connectorId="nightscout"
        primaryAction="save-and-sync"
        showToggle={false}
        showDangerZone={false}
        showCapabilities={true}
        onComplete={handleMigrationComplete}
      />
      <div class="flex justify-end pt-2">
        <Button variant="outline" onclick={handleMigrationComplete}>
          Continue to Dashboard
        </Button>
      </div>
    </div>
  {:else}
    <div class="grid gap-4 sm:grid-cols-2">
      <button class="text-left" onclick={() => (showMigration = true)}>
        <Card.Root class="h-full transition-colors hover:bg-muted/50">
          <Card.Header class="space-y-3 p-6">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-blue-500/10 text-blue-600">
              <ArrowRightLeft class="h-5 w-5" />
            </div>
            <div>
              <Card.Title class="text-sm font-medium">Migrate from Nightscout</Card.Title>
              <Card.Description class="text-xs">
                Import your existing data — entries, treatments, and history.
              </Card.Description>
            </div>
          </Card.Header>
        </Card.Root>
      </button>

      <button class="text-left" onclick={handleStartFresh}>
        <Card.Root class="h-full transition-colors hover:bg-muted/50">
          <Card.Header class="space-y-3 p-6">
            <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-green-500/10 text-green-600">
              <Plus class="h-5 w-5" />
            </div>
            <div>
              <Card.Title class="text-sm font-medium">Start Fresh</Card.Title>
              <Card.Description class="text-xs">
                Connect a glucose data source to get started.
              </Card.Description>
            </div>
          </Card.Header>
        </Card.Root>
      </button>
    </div>
  {/if}
</div>
