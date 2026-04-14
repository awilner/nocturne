<script lang="ts">
	import type { PageData } from "./$types";

	let { data }: { data: PageData } = $props();
</script>

<div class="flex flex-col items-center justify-center min-h-screen gap-4 p-6 text-center">
	{#if data.success}
		<h1 class="text-2xl font-bold">Discord account linked</h1>
		<p class="text-muted-foreground max-w-md">
			Your Discord account is now linked as <strong>{data.displayName}</strong>
			(label <code>{data.label}</code>). Run <code>/bg</code> in Discord to get started.
		</p>
		<a
			href="/settings/integrations/discord"
			class="px-6 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
		>
			Back to Settings
		</a>
	{:else}
		<h1 class="text-2xl font-bold">Link failed</h1>
		<p class="text-destructive max-w-md">{data.message}</p>
		<a
			href="/settings/integrations/discord"
			class="px-6 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
		>
			Back to Settings
		</a>
	{/if}
</div>
