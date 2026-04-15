<script lang="ts">
  import { onMount, onDestroy } from "svelte";
  import { goto } from "$app/navigation";
  import {
    getUploaderApps,
    getUploaderSetup,
    getActiveDataSources,
    getServicesOverview,
  } from "$api/generated/services.generated.remote";
  import type {
    UploaderApp,
    UploaderSetupResponse,
    DataSourceInfo,
    AvailableConnector,
    SyncResult,
  } from "$lib/api/generated/nocturne-api-client";
  import { markSetupComplete } from "../setup.remote";
  import ConnectorSetup from "$lib/components/connectors/ConnectorSetup.svelte";
  import * as Card from "$lib/components/ui/card";
  import { Button } from "$lib/components/ui/button";
  import { Badge } from "$lib/components/ui/badge";
  import {
    AlertCircle,
    CheckCircle,
    ChevronLeft,
    ChevronRight,
    Copy,
    Check,
    ExternalLink,
    Loader2,
    Upload,
    Activity,
    Smartphone,
    Clock,
    Cloud,
    Plug,
  } from "lucide-svelte";
  import Apple from "lucide-svelte/icons/apple";

  // ── View state ──────────────────────────────────────────────────────

  type ViewState = "selection" | "connector" | "uploader";
  let viewState = $state<ViewState>("selection");

  // ── Connector state ───────────────────────────────────────────────
  let selectedConnectorId = $state<string | null>(null);

  // ── Uploader state ────────────────────────────────────────────────
  let selectedApp = $state<UploaderApp | null>(null);
  let setupResponse = $state<UploaderSetupResponse | null>(null);
  let setupLoading = $state(false);

  // ── Data state ────────────────────────────────────────────────────

  let uploaderApps = $state<UploaderApp[]>([]);
  let connectors = $state<AvailableConnector[]>([]);
  let dataSources = $state<DataSourceInfo[]>([]);
  let isLoading = $state(true);
  let loadError = $state<string | null>(null);

  // ── Copy state ────────────────────────────────────────────────────

  let copiedField = $state<string | null>(null);

  // ── Connection polling ────────────────────────────────────────────

  let pollInterval = $state<ReturnType<typeof setInterval> | null>(null);

  // ── Platform filter ──────────────────────────────────────────────

  type PlatformFilter = "all" | "ios" | "android";
  let platformFilter = $state<PlatformFilter>("all");

  // ── Categories ────────────────────────────────────────────────────

  const categoryLabels: Record<string, string> = {
    cgm: "CGM Apps",
    "aid-system": "AID Systems",
    uploader: "General Uploaders",
  };

  const categoryOrder = ["cgm", "aid-system", "uploader"];

  const filteredApps = $derived(
    platformFilter === "all"
      ? uploaderApps
      : uploaderApps.filter((app) => app.platform === platformFilter),
  );

  const groupedApps = $derived.by(() => {
    const groups: Record<string, UploaderApp[]> = {};
    for (const app of filteredApps) {
      const cat = app.category ?? "uploader";
      if (!groups[cat]) groups[cat] = [];
      groups[cat].push(app);
    }
    return categoryOrder
      .filter((cat) => groups[cat]?.length)
      .map((cat) => ({ category: cat, label: categoryLabels[cat] ?? cat, apps: groups[cat] }));
  });

  // ── Detect which uploaders have sent data ─────────────────────────

  function isDetected(appId: string | undefined): boolean {
    if (!appId) return false;
    return dataSources.some(
      (ds) => ds.sourceType?.toLowerCase() === appId.toLowerCase(),
    );
  }

  function getDataSource(appId: string | undefined): DataSourceInfo | undefined {
    if (!appId) return undefined;
    return dataSources.find(
      (ds) => ds.sourceType?.toLowerCase() === appId.toLowerCase(),
    );
  }

  // ── Load data ─────────────────────────────────────────────────────

  onMount(async () => {
    await loadData();
  });

  onDestroy(() => {
    stopPolling();
  });

  async function loadData() {
    isLoading = true;
    loadError = null;

    try {
      const [apps, sources, overview] = await Promise.all([
        getUploaderApps(),
        getActiveDataSources().catch(() => [] as DataSourceInfo[]),
        getServicesOverview(),
      ]);

      uploaderApps = apps ?? [];
      dataSources = sources ?? [];

      // Filter out "nightscout" connector (handled on the triage page)
      connectors = (overview?.availableConnectors ?? []).filter(
        (c) => c.id?.toLowerCase() !== "nightscout",
      );
    } catch (e) {
      loadError = e instanceof Error ? e.message : "Failed to load data sources";
    } finally {
      isLoading = false;
    }
  }

  // ── Select a connector ────────────────────────────────────────────

  function selectConnector(connector: AvailableConnector) {
    selectedConnectorId = connector.id ?? null;
    viewState = "connector";
  }

  async function handleConnectorComplete(_result: SyncResult) {
    await markSetupComplete();
    await goto("/", { invalidateAll: true });
  }

  function handleConnectorCancel() {
    selectedConnectorId = null;
    viewState = "selection";
  }

  // ── Select an uploader app ────────────────────────────────────────

  async function selectApp(app: UploaderApp) {
    selectedApp = app;
    setupLoading = true;
    viewState = "uploader";

    try {
      const result = await getUploaderSetup(app.id!);
      setupResponse = result;
    } catch {
      setupResponse = null;
    } finally {
      setupLoading = false;
    }

    // Start polling for connection if not already detected
    if (!isDetected(app.id)) {
      startPolling();
    }
  }

  function handleBackToSelection() {
    stopPolling();
    selectedApp = null;
    setupResponse = null;
    selectedConnectorId = null;
    copiedField = null;
    viewState = "selection";
  }

  // ── Polling ───────────────────────────────────────────────────────

  function startPolling() {
    stopPolling();
    pollInterval = setInterval(async () => {
      try {
        const sources = await getActiveDataSources();
        dataSources = sources ?? [];

        // Stop polling if we detect data from the selected app
        if (selectedApp && isDetected(selectedApp.id)) {
          stopPolling();
        }
      } catch {
        // Silently continue polling
      }
    }, 15_000);
  }

  function stopPolling() {
    if (pollInterval) {
      clearInterval(pollInterval);
      pollInterval = null;
    }
  }

  // ── Copy to clipboard ─────────────────────────────────────────────

  async function copyToClipboard(value: string, fieldName: string) {
    try {
      await navigator.clipboard.writeText(value);
      copiedField = fieldName;
      setTimeout(() => {
        copiedField = null;
      }, 2000);
    } catch {
      // Clipboard API not available
    }
  }

  // ── Skip ──────────────────────────────────────────────────────────

  async function handleSkip() {
    await markSetupComplete();
    await goto("/", { invalidateAll: true });
  }

  // ── Platform icon helper ──────────────────────────────────────────

  function getPlatformLabel(platform: string | undefined): string {
    switch (platform) {
      case "ios":
        return "iOS";
      case "android":
        return "Android";
      case "desktop":
        return "Desktop";
      case "web":
        return "Web";
      default:
        return platform ?? "Unknown";
    }
  }

  function getCategoryIcon(category: string | undefined) {
    switch (category) {
      case "cgm":
        return Activity;
      case "aid-system":
        return Activity;
      case "uploader":
        return Upload;
      default:
        return Upload;
    }
  }
</script>

<svelte:head>
  <title>Connect a Data Source - Setup - Nocturne</title>
</svelte:head>

<div class="container mx-auto max-w-2xl p-6 space-y-6">
  {#if viewState === "selection"}
    <!-- ── Selection View ──────────────────────────────────────── -->
    <div>
      <h1 class="text-2xl font-bold tracking-tight">Connect a Data Source</h1>
      <p class="text-muted-foreground">
        Choose a cloud service or phone app to start sending glucose and treatment data to Nocturne.
      </p>
    </div>

    {#if isLoading}
      <div class="flex items-center justify-center py-12">
        <Loader2 class="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    {:else if loadError}
      <Card.Root class="border-destructive">
        <Card.Content class="flex items-center gap-3 pt-6">
          <AlertCircle class="h-5 w-5 text-destructive" />
          <div>
            <p class="font-medium">Failed to load data sources</p>
            <p class="text-sm text-muted-foreground">{loadError}</p>
          </div>
        </Card.Content>
      </Card.Root>
    {:else}
      <div class="space-y-8">
        <!-- Cloud Services (Connectors) -->
        {#if connectors.length > 0}
          <div class="space-y-3">
            <h2 class="text-sm font-medium text-muted-foreground flex items-center gap-2">
              <Cloud class="h-4 w-4" />
              Cloud Services
            </h2>
            <div class="grid gap-3 sm:grid-cols-2">
              {#each connectors as connector (connector.id)}
                {@const configured = connector.isConfigured ?? false}
                <button
                  type="button"
                  class="flex items-center gap-4 p-4 rounded-lg border transition-colors text-left group {configured
                    ? 'border-green-500/30 bg-green-500/5 hover:bg-green-500/10'
                    : 'bg-muted/30 hover:border-primary/50 hover:bg-accent/50'}"
                  onclick={() => selectConnector(connector)}
                >
                  <div
                    class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg {configured
                      ? 'bg-green-500/10 text-green-600'
                      : 'bg-primary/10 text-primary'}"
                  >
                    {#if configured}
                      <CheckCircle class="h-5 w-5" />
                    {:else}
                      <Plug class="h-5 w-5" />
                    {/if}
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 flex-wrap">
                      <span class="font-medium">{connector.name}</span>
                      {#if configured}
                        <Badge variant="secondary" class="text-xs text-green-600">
                          Connected
                        </Badge>
                      {/if}
                    </div>
                    {#if connector.description}
                      <p class="text-sm text-muted-foreground line-clamp-1">
                        {connector.description}
                      </p>
                    {/if}
                  </div>
                  <ChevronRight
                    class="h-4 w-4 text-muted-foreground group-hover:text-foreground transition-colors shrink-0"
                  />
                </button>
              {/each}
            </div>
          </div>
        {/if}

        <!-- Phone Apps (Uploaders) -->
        {#if uploaderApps.length > 0}
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <h2 class="text-sm font-medium text-muted-foreground flex items-center gap-2">
                <Smartphone class="h-4 w-4" />
                Phone Apps
              </h2>
              <div class="flex items-center gap-1">
                <Button
                  variant={platformFilter === "all" ? "default" : "outline"}
                  size="sm"
                  onclick={() => (platformFilter = "all")}
                >
                  All
                </Button>
                <Button
                  variant={platformFilter === "ios" ? "default" : "outline"}
                  size="sm"
                  class="gap-1.5"
                  onclick={() => (platformFilter = "ios")}
                >
                  <Apple class="h-3.5 w-3.5" />
                  iOS
                </Button>
                <Button
                  variant={platformFilter === "android" ? "default" : "outline"}
                  size="sm"
                  class="gap-1.5"
                  onclick={() => (platformFilter = "android")}
                >
                  <Smartphone class="h-3.5 w-3.5" />
                  Android
                </Button>
              </div>
            </div>

            <div class="space-y-6">
              {#each groupedApps as group (group.category)}
                <div class="space-y-3">
                  <h3 class="text-sm font-medium text-muted-foreground">{group.label}</h3>
                  <div class="grid gap-3 sm:grid-cols-2">
                    {#each group.apps as app (app.id)}
                      {@const detected = isDetected(app.id)}
                      {@const Icon = getCategoryIcon(app.category)}
                      <button
                        type="button"
                        class="flex items-center gap-4 p-4 rounded-lg border transition-colors text-left group {detected
                          ? 'border-green-500/30 bg-green-500/5 hover:bg-green-500/10'
                          : 'bg-muted/30 hover:border-primary/50 hover:bg-accent/50'}"
                        onclick={() => selectApp(app)}
                      >
                        <div
                          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg {detected
                            ? 'bg-green-500/10 text-green-600'
                            : 'bg-primary/10 text-primary'}"
                        >
                          {#if detected}
                            <CheckCircle class="h-5 w-5" />
                          {:else}
                            <Icon class="h-5 w-5" />
                          {/if}
                        </div>
                        <div class="flex-1 min-w-0">
                          <div class="flex items-center gap-2 flex-wrap">
                            <span class="font-medium">{app.name}</span>
                            <Badge variant="outline" class="text-xs gap-1">
                              {getPlatformLabel(app.platform)}
                            </Badge>
                            {#if detected}
                              <Badge variant="secondary" class="text-xs text-green-600">
                                Connected
                              </Badge>
                            {/if}
                          </div>
                          {#if app.description}
                            <p class="text-sm text-muted-foreground line-clamp-1">
                              {app.description}
                            </p>
                          {/if}
                        </div>
                        <ChevronRight
                          class="h-4 w-4 text-muted-foreground group-hover:text-foreground transition-colors shrink-0"
                        />
                      </button>
                    {/each}
                  </div>
                </div>
              {/each}
            </div>
          </div>
        {/if}

        <!-- Empty state when both are empty -->
        {#if connectors.length === 0 && uploaderApps.length === 0}
          <Card.Root>
            <Card.Content class="py-8 text-center">
              <Plug class="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
              <p class="font-medium">No data sources available</p>
              <p class="text-sm text-muted-foreground mt-1">
                There are no data sources available at this time.
              </p>
            </Card.Content>
          </Card.Root>
        {/if}
      </div>

      <!-- Skip link -->
      <div class="pt-4 text-center">
        <button
          type="button"
          class="text-sm text-muted-foreground hover:text-foreground underline-offset-4 hover:underline transition-colors"
          onclick={handleSkip}
        >
          Skip for now
        </button>
      </div>
    {/if}

  {:else if viewState === "connector"}
    <!-- ── Connector View ──────────────────────────────────────── -->
    <div class="space-y-4">
      <Button
        variant="ghost"
        size="sm"
        class="gap-1 -ml-2"
        onclick={handleConnectorCancel}
      >
        <ChevronLeft class="h-4 w-4" />
        Back to data sources
      </Button>

      <ConnectorSetup
        connectorId={selectedConnectorId ?? undefined}
        onComplete={handleConnectorComplete}
        onCancel={handleConnectorCancel}
        showToggle={false}
        showDangerZone={false}
        showCapabilities={false}
        primaryAction="save-and-sync"
      />
    </div>

  {:else if viewState === "uploader"}
    <!-- ── Uploader View ───────────────────────────────────────── -->
    <div class="space-y-4">
      <Button
        variant="ghost"
        size="sm"
        class="gap-1 -ml-2"
        onclick={handleBackToSelection}
      >
        <ChevronLeft class="h-4 w-4" />
        Back to data sources
      </Button>

      {#if setupLoading}
        <div class="flex items-center justify-center py-8">
          <Loader2 class="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      {:else if setupResponse}
        <div>
          <h2 class="text-lg font-semibold">{selectedApp?.name}</h2>
          {#if selectedApp?.description}
            <p class="text-sm text-muted-foreground">
              {selectedApp.description}
            </p>
          {/if}
        </div>

        <!-- API URLs to copy -->
        <Card.Root>
          <Card.Header class="pb-3">
            <Card.Title class="text-sm">Connection Details</Card.Title>
            <Card.Description>
              Copy these values into your {selectedApp?.name} app settings.
            </Card.Description>
          </Card.Header>
          <Card.Content class="space-y-3">
            <!-- Full API URL -->
            <div class="space-y-1">
              <span class="text-xs font-medium text-muted-foreground">API URL</span>
              <div class="flex items-center gap-2">
                <code
                  class="flex-1 rounded-md border bg-muted px-3 py-2 text-sm font-mono break-all"
                >
                  {setupResponse.fullApiUrl}
                </code>
                <Button
                  variant="outline"
                  size="icon"
                  class="shrink-0"
                  onclick={() => copyToClipboard(setupResponse!.fullApiUrl!, "apiUrl")}
                >
                  {#if copiedField === "apiUrl"}
                    <Check class="h-4 w-4 text-green-500" />
                  {:else}
                    <Copy class="h-4 w-4" />
                  {/if}
                </Button>
              </div>
            </div>

            <!-- xDrip-style URL (with embedded secret) -->
            {#if setupResponse.xdripStyleUrl}
              <div class="space-y-1">
                <span class="text-xs font-medium text-muted-foreground">
                  xDrip-style URL (with embedded secret)
                </span>
                <div class="flex items-center gap-2">
                  <code
                    class="flex-1 rounded-md border bg-muted px-3 py-2 text-sm font-mono break-all"
                  >
                    {setupResponse.xdripStyleUrl}
                  </code>
                  <Button
                    variant="outline"
                    size="icon"
                    class="shrink-0"
                    onclick={() => copyToClipboard(setupResponse!.xdripStyleUrl!, "xdripUrl")}
                  >
                    {#if copiedField === "xdripUrl"}
                      <Check class="h-4 w-4 text-green-500" />
                    {:else}
                      <Copy class="h-4 w-4" />
                    {/if}
                  </Button>
                </div>
              </div>
            {/if}

            <!-- API Secret -->
            {#if setupResponse.apiSecretPlaceholder}
              <div class="space-y-1">
                <span class="text-xs font-medium text-muted-foreground">API Secret</span>
                <div class="flex items-center gap-2">
                  <code
                    class="flex-1 rounded-md border bg-muted px-3 py-2 text-sm font-mono break-all"
                  >
                    {setupResponse.apiSecretPlaceholder}
                  </code>
                  <Button
                    variant="outline"
                    size="icon"
                    class="shrink-0"
                    onclick={() =>
                      copyToClipboard(setupResponse!.apiSecretPlaceholder!, "apiSecret")}
                  >
                    {#if copiedField === "apiSecret"}
                      <Check class="h-4 w-4 text-green-500" />
                    {:else}
                      <Copy class="h-4 w-4" />
                    {/if}
                  </Button>
                </div>
              </div>
            {/if}
          </Card.Content>
        </Card.Root>

        <!-- App download link -->
        {#if selectedApp?.url}
          <a
            href={selectedApp.url}
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-2 text-sm text-primary hover:underline"
          >
            <ExternalLink class="h-4 w-4" />
            Download {selectedApp.name}
          </a>
        {/if}

        <!-- Connection status -->
        {@const detected = isDetected(selectedApp?.id)}
        {@const ds = getDataSource(selectedApp?.id)}
        {#if detected && ds}
          <Card.Root class="border-green-500/30">
            <Card.Content class="flex items-center gap-3 pt-6">
              <CheckCircle class="h-5 w-5 text-green-500" />
              <div>
                <p class="font-medium text-green-600">Connected</p>
                <p class="text-sm text-muted-foreground">
                  {selectedApp?.name} is sending data. {ds.entriesLast24h ?? 0} entries in the last 24 hours.
                </p>
              </div>
            </Card.Content>
          </Card.Root>

          <div class="flex justify-end">
            <Button onclick={handleSkip}>
              Continue to Dashboard
            </Button>
          </div>
        {:else}
          <Card.Root class="border-muted">
            <Card.Content class="flex items-center gap-3 pt-6">
              <Clock class="h-5 w-5 text-muted-foreground" />
              <div>
                <p class="font-medium">Waiting for data</p>
                <p class="text-sm text-muted-foreground">
                  Once you've configured {selectedApp?.name}, it can take up to five minutes for the first glucose data to arrive. This page will update automatically.
                </p>
              </div>
            </Card.Content>
          </Card.Root>
        {/if}
      {:else}
        <Card.Root class="border-destructive">
          <Card.Content class="flex items-center gap-3 pt-6">
            <AlertCircle class="h-5 w-5 text-destructive" />
            <div>
              <p class="font-medium">Failed to load setup instructions</p>
              <p class="text-sm text-muted-foreground">
                Could not load setup details for {selectedApp?.name}. Please try again.
              </p>
            </div>
          </Card.Content>
        </Card.Root>
      {/if}
    </div>
  {/if}
</div>
