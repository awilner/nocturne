<script lang="ts">
	import { ChevronDown } from 'lucide-svelte';
	import type { SortMode } from './types';

	interface Props {
		sort: SortMode;
		onsort: (sort: SortMode) => void;
	}

	const { sort, onsort }: Props = $props();
</script>

<div class="header-row">
	<span></span>

	<button type="button" class="col-btn" class:active={sort === 'name'} onclick={() => onsort('name')}>
		Name
		{#if sort === 'name'}
			<ChevronDown size={12} />
		{/if}
	</button>

	<button
		type="button"
		class="col-btn"
		class:active={sort === 'carbs'}
		style:color={sort === 'carbs' ? 'oklch(0.85 0.18 75)' : undefined}
		onclick={() => onsort('carbs')}
	>
		Carbs
		{#if sort === 'carbs'}
			<ChevronDown size={12} />
		{/if}
	</button>

	<span class="col-label">Portion</span>

	<span class="col-label">GI</span>

	<span class="col-label" style="text-align: right;">Energy</span>

	<span></span>
</div>

<style>
	.header-row {
		display: grid;
		grid-template-columns: 24px 1fr 110px 130px 90px 70px 24px;
		gap: 12px;
		padding: 10px 16px;
		align-items: center;
		position: sticky;
		top: 0;
		z-index: 5;
		background: var(--card);
		border-bottom: 1px solid var(--border);
	}

	.col-label,
	.col-btn {
		font-size: 11px;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		color: oklch(from var(--muted-foreground) l c h / 0.7);
	}

	.col-btn {
		display: inline-flex;
		align-items: center;
		gap: 3px;
		padding: 0;
		border: none;
		background: none;
		cursor: pointer;
		font: inherit;
		font-size: 11px;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		color: oklch(from var(--muted-foreground) l c h / 0.7);
	}

	.col-btn.active {
		color: var(--foreground);
	}
</style>
