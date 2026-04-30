<script lang="ts">
	import type { Food } from '$api';
	import { Trash2, Check } from 'lucide-svelte';
	import GiIcon from './GiIcon.svelte';
	import { getFoodState } from './food-context.js';
	import { giFromInt, giToInt } from './types.js';
	import type { GiLevel } from './types.js';
	import { FOOD_UNITS } from '$lib/components/food';

	interface Props {
		food: Food;
		onsave: (draft: Food) => void;
		oncancel: () => void;
	}

	const { food, onsave, oncancel }: Props = $props();
	const foodState = getFoodState();

	// Intentionally capture initial food value - draft is the local editing copy
	let draft = $state<Food>({ ...food });
	let confirming = $state(false);
	let attributionCount = $state(0);

	const giLevels: GiLevel[] = ['low', 'medium', 'high'];

	const subcategories = $derived.by(() => {
		if (!draft.category) return [];
		const subs = new Set<string>();
		for (const f of foodState.foods) {
			if (f.category === draft.category && f.subcategory) {
				subs.add(f.subcategory);
			}
		}
		return [...subs].sort();
	});

	async function handleDeleteClick() {
		if (!food._id) return;
		confirming = true;
		attributionCount = await foodState.getAttributionCount(food._id);
	}

	async function confirmDelete() {
		if (!food._id) return;
		await foodState.deleteFood(food._id, 'clear');
	}
</script>

<div class="border-y border-border px-4 py-4" style="background: oklch(0.17 0.03 263)">
	<!-- Delete confirmation bar -->
	{#if confirming}
		<div class="mb-4 flex items-center gap-3 rounded-lg px-4 py-3" style="background: oklch(0.25 0.06 25 / 0.5); border: 1px solid oklch(0.6 0.2 25 / 0.3)">
			<Trash2 size={16} class="shrink-0 text-red-400" />
			<span class="text-sm">
				Delete <strong>{food.name}</strong>?
				{#if attributionCount > 0}
					<span class="ml-1 text-muted-foreground">Used in {attributionCount} {'treatment' + (attributionCount === 1 ? '' : 's')}.</span>
				{/if}
			</span>
			<div class="ml-auto flex items-center gap-2">
				<button
					type="button"
					class="rounded-md px-3 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors"
					onclick={() => { confirming = false; }}
				>
					Cancel
				</button>
				<button
					type="button"
					class="rounded-md px-3 py-1.5 text-xs font-medium text-red-400 hover:bg-red-500/20 transition-colors"
					onclick={confirmDelete}
				>
					Delete
				</button>
			</div>
		</div>
	{/if}

	<!-- Section 1: Name, Carbs, Portion, Unit -->
	<div class="grid gap-4" style="grid-template-columns: 1.6fr 1fr 1fr 1fr">
		<!-- Name -->
		<div class="flex flex-col gap-1.5">
			<span class="text-muted-foreground font-semibold" style="font-size: 11px">Name *</span>
			<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				<input
					type="text"
					class="w-full bg-transparent text-sm outline-none"
					bind:value={draft.name}
					required
				/>
			</div>
		</div>

		<!-- Carbs -->
		<div class="flex flex-col gap-1.5">
			<span class="font-semibold" style="font-size: 11px; color: oklch(0.769 0.188 70.08)">Carbs *</span>
			<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(0.769 0.188 70.08 / 0.45); background: oklch(0.769 0.188 70.08 / 0.06)">
				<input
					type="number"
					class="w-full bg-transparent text-sm outline-none"
					bind:value={draft.carbs}
					min="0"
					step="0.1"
				/>
				<span class="ml-2 shrink-0 text-xs" style="color: oklch(0.769 0.188 70.08)">g</span>
			</div>
			<span class="text-muted-foreground" style="font-size: 10px">per {draft.portion ?? 100} {draft.unit ?? 'g'}</span>
		</div>

		<!-- Portion -->
		<div class="flex flex-col gap-1.5">
			<span class="text-muted-foreground font-semibold" style="font-size: 11px">Portion *</span>
			<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				<input
					type="number"
					class="w-full bg-transparent text-sm outline-none"
					bind:value={draft.portion}
					min="0"
					step="1"
				/>
				<span class="ml-2 shrink-0 text-xs text-muted-foreground">{draft.unit ?? 'g'}</span>
			</div>
		</div>

		<!-- Unit -->
		<div class="flex flex-col gap-1.5">
			<span class="text-muted-foreground font-semibold" style="font-size: 11px">Unit</span>
			<div class="flex items-center gap-0.5 rounded-md p-1" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				{#each FOOD_UNITS as unit (unit)}
					<button
						type="button"
						class="flex-1 rounded px-1.5 py-1.5 text-xs font-medium transition-colors {draft.unit === unit ? 'text-foreground' : 'text-muted-foreground'}"
						style:background={draft.unit === unit ? 'oklch(1 0 0 / 0.12)' : 'transparent'}
						onclick={() => { draft.unit = unit; }}
					>
						{unit}
					</button>
				{/each}
			</div>
		</div>
	</div>

	<hr class="my-4 border-border" />

	<!-- Section 2: GI, Fat, Protein, Energy -->
	<div class="grid gap-4" style="grid-template-columns: 1.4fr 1fr 1fr 1fr">
		<!-- GI -->
		<div class="flex flex-col gap-1.5">
			<span class="text-muted-foreground font-semibold" style="font-size: 11px">Glycemic Index</span>
			<div class="flex items-center gap-0.5 rounded-md p-1" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				{#each giLevels as level (level)}
					{@const selected = giFromInt(draft.gi) === level}
					<button
						type="button"
						class="flex flex-1 items-center justify-center gap-1.5 rounded px-2 py-1.5 text-xs font-medium transition-colors {selected ? 'text-foreground' : 'text-muted-foreground'}"
						style:background={selected ? 'oklch(1 0 0 / 0.12)' : 'transparent'}
						onclick={() => { draft.gi = giToInt(level); }}
					>
						<GiIcon {level} size={10} />
						<span class="capitalize">{level}</span>
					</button>
				{/each}
			</div>
		</div>

		<!-- Fat -->
		<div class="flex flex-col gap-1.5">
			<span class="font-semibold" style="font-size: 11px">
				<span class="text-muted-foreground">Fat</span>
				<span class="ml-1" style="font-size: 10px; color: oklch(1 0 0 / 0.3)">optional</span>
			</span>
			<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				<input
					type="number"
					class="w-full bg-transparent text-sm outline-none"
					bind:value={draft.fat}
					min="0"
					step="0.1"
				/>
				<span class="ml-2 shrink-0 text-xs text-muted-foreground">g</span>
			</div>
		</div>

		<!-- Protein -->
		<div class="flex flex-col gap-1.5">
			<span class="font-semibold" style="font-size: 11px">
				<span class="text-muted-foreground">Protein</span>
				<span class="ml-1" style="font-size: 10px; color: oklch(1 0 0 / 0.3)">optional</span>
			</span>
			<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				<input
					type="number"
					class="w-full bg-transparent text-sm outline-none"
					bind:value={draft.protein}
					min="0"
					step="0.1"
				/>
				<span class="ml-2 shrink-0 text-xs text-muted-foreground">g</span>
			</div>
		</div>

		<!-- Energy -->
		<div class="flex flex-col gap-1.5">
			<span class="font-semibold" style="font-size: 11px">
				<span class="text-muted-foreground">Energy</span>
				<span class="ml-1" style="font-size: 10px; color: oklch(1 0 0 / 0.3)">auto</span>
			</span>
			<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				<input
					type="number"
					class="w-full bg-transparent text-sm outline-none"
					bind:value={draft.energy}
					min="0"
					step="1"
				/>
				<span class="ml-2 shrink-0 text-xs text-muted-foreground">kcal</span>
			</div>
		</div>
	</div>

	<hr class="my-4 border-border" />

	<!-- Section 3: Category, Subcategory, Actions -->
	<div class="flex items-center justify-between">
		<div class="flex items-center gap-3">
			<!-- Category -->
			<select
				class="rounded-md px-3 py-2 text-sm outline-none"
				style="width: 180px; border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04); color: inherit"
				bind:value={draft.category}
			>
				<option value={undefined}>No category</option>
				{#each foodState.categories as cat (cat)}
					<option value={cat}>{cat}</option>
				{/each}
			</select>

			<!-- Subcategory -->
			<select
				class="rounded-md px-3 py-2 text-sm outline-none"
				style="width: 180px; border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04); color: inherit"
				bind:value={draft.subcategory}
			>
				<option value={undefined}>No subcategory</option>
				{#each subcategories as sub (sub)}
					<option value={sub}>{sub}</option>
				{/each}
			</select>
		</div>

		<div class="flex items-center gap-2">
			<!-- Delete -->
			<button
				type="button"
				class="flex items-center gap-1.5 rounded-md px-3 py-2 text-sm text-red-400 hover:bg-red-500/10 transition-colors"
				onclick={handleDeleteClick}
			>
				<Trash2 size={14} />
				Delete
			</button>

			<!-- Cancel -->
			<button
				type="button"
				class="rounded-md border border-border px-4 py-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
				onclick={oncancel}
			>
				Cancel
			</button>

			<!-- Save -->
			<button
				type="button"
				class="flex items-center gap-1.5 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors"
				onclick={() => onsave(draft)}
			>
				<Check size={14} />
				Save changes
			</button>
		</div>
	</div>
</div>
