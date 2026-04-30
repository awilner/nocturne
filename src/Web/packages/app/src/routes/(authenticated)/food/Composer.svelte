<script lang="ts">
	import type { Food } from '$api';
	import { Plus, X } from 'lucide-svelte';
	import GiIcon from './GiIcon.svelte';
	import { getFoodState } from './food-context.js';
	import { giFromInt, giToInt } from './types.js';
	import type { GiLevel } from './types.js';
	import { FOOD_UNITS, DEFAULT_PORTION, DEFAULT_GI } from '$lib/components/food';

	interface Props {
		onadd: (food: Food) => void;
		onclose: () => void;
	}

	const { onadd, onclose }: Props = $props();
	const foodState = getFoodState();

	const giLevels: GiLevel[] = ['low', 'medium', 'high'];

	function emptyDraft(): Food {
		return {
			name: undefined,
			carbs: undefined,
			portion: DEFAULT_PORTION,
			unit: 'g',
			gi: DEFAULT_GI,
			type: 'food',
			fat: undefined,
			protein: undefined,
			energy: undefined,
			category: undefined,
			subcategory: undefined,
		};
	}

	let draft = $state<Food>(emptyDraft());
	let showDetails = $state(false);
	let nameInput: HTMLInputElement | undefined = $state();

	const canSave = $derived(!!draft.name && draft.carbs !== undefined && !!draft.portion);

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

	$effect(() => {
		nameInput?.focus();
	});

	function submit(addAnother: boolean) {
		if (!canSave) return;
		onadd(draft);
		if (addAnother) {
			const keepPortion = draft.portion;
			const keepUnit = draft.unit;
			const keepGi = draft.gi;
			const keepCategory = draft.category;
			draft = emptyDraft();
			draft.portion = keepPortion;
			draft.unit = keepUnit;
			draft.gi = keepGi;
			draft.category = keepCategory;
			nameInput?.focus();
		} else {
			onclose();
		}
	}

	function handleKeydown(e: KeyboardEvent) {
		const mod = e.metaKey || e.ctrlKey;
		if (mod && e.key === 'Enter') {
			e.preventDefault();
			submit(true);
		} else if (e.shiftKey && e.key === 'Enter') {
			e.preventDefault();
			submit(false);
		} else if (e.key === 'Escape') {
			e.preventDefault();
			onclose();
		}
	}
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
	class="mx-4 my-3 rounded-[10px] p-3.5"
	style="border: 1px solid oklch(0.769 0.188 70.08 / 0.22); background: oklch(0.769 0.188 70.08 / 0.04)"
	onkeydown={handleKeydown}
>
	<!-- Header -->
	<div class="mb-3 flex items-center gap-3">
		<div class="flex items-center justify-center rounded-[7px]" style="width: 26px; height: 26px; background: oklch(0.769 0.188 70.08 / 0.15)">
			<Plus size={14} style="color: oklch(0.769 0.188 70.08)" />
		</div>
		<span class="font-semibold" style="font-size: 13px">Add food</span>
		<span class="text-muted-foreground" style="font-size: 11px">
			Tab through fields · ⌘+Enter to save and add another · Esc to close
		</span>
		<button
			type="button"
			class="ml-auto flex items-center justify-center rounded-md p-1 text-muted-foreground hover:text-foreground transition-colors"
			onclick={onclose}
		>
			<X size={16} />
		</button>
	</div>

	<!-- Single-row form -->
	<div class="grid items-end gap-3" style="grid-template-columns: 1.6fr 110px 90px 1fr 1.4fr; height: 42px">
		<!-- Name -->
		<div class="flex h-full flex-col gap-1">
			<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">Name</span>
			<div class="flex flex-1 items-center rounded-md px-3" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				<input
					type="text"
					class="w-full bg-transparent text-sm outline-none"
					placeholder="e.g. Greek yogurt, plain"
					bind:this={nameInput}
					bind:value={draft.name}
				/>
			</div>
		</div>

		<!-- Carbs -->
		<div class="flex h-full flex-col gap-1">
			<span class="font-medium uppercase" style="font-size: 10px; color: oklch(0.769 0.188 70.08)">Carbs</span>
			<div class="flex flex-1 items-center rounded-md px-3" style="border: 1px solid oklch(0.769 0.188 70.08 / 0.45); background: oklch(0.769 0.188 70.08 / 0.06)">
				<input
					type="number"
					class="w-full bg-transparent text-sm outline-none"
					bind:value={draft.carbs}
					min="0"
					step="0.1"
				/>
				<span class="ml-1 shrink-0 text-xs" style="color: oklch(0.769 0.188 70.08)">g</span>
			</div>
		</div>

		<!-- Per (portion) -->
		<div class="flex h-full flex-col gap-1">
			<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">Per</span>
			<div class="flex flex-1 items-center rounded-md px-3" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				<input
					type="number"
					class="w-full bg-transparent text-sm outline-none"
					bind:value={draft.portion}
					min="0"
					step="1"
				/>
			</div>
		</div>

		<!-- Unit -->
		<div class="flex h-full flex-col gap-1">
			<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">Unit</span>
			<div class="flex flex-1 items-center gap-0.5 rounded-md p-1" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				{#each FOOD_UNITS as unit (unit)}
					<button
						type="button"
						class="flex-1 rounded px-1 py-1 text-xs font-medium transition-colors {draft.unit === unit ? 'text-foreground' : 'text-muted-foreground'}"
						style:background={draft.unit === unit ? 'oklch(1 0 0 / 0.12)' : 'transparent'}
						onclick={() => { draft.unit = unit; }}
					>
						{unit}
					</button>
				{/each}
			</div>
		</div>

		<!-- GI -->
		<div class="flex h-full flex-col gap-1">
			<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">GI</span>
			<div class="flex flex-1 items-center gap-0.5 rounded-md p-1" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
				{#each giLevels as level (level)}
					{@const selected = giFromInt(draft.gi) === level}
					<button
						type="button"
						class="flex flex-1 items-center justify-center gap-1 rounded px-1 py-1 text-xs font-medium transition-colors {selected ? 'text-foreground' : 'text-muted-foreground'}"
						style:background={selected ? 'oklch(1 0 0 / 0.12)' : 'transparent'}
						onclick={() => { draft.gi = giToInt(level); }}
					>
						<GiIcon {level} size={9} />
						<span class="capitalize">{level}</span>
					</button>
				{/each}
			</div>
		</div>
	</div>

	<!-- Footer -->
	<div class="mt-3 flex items-center justify-between">
		<!-- Details toggle -->
		<details bind:open={showDetails}>
			<summary class="cursor-pointer text-xs text-muted-foreground hover:text-foreground transition-colors select-none">
				Add fat, protein, category...
			</summary>
			<div class="mt-3 grid gap-3" style="grid-template-columns: 1fr 1fr 1fr 1fr 1fr">
				<!-- Fat -->
				<div class="flex flex-col gap-1">
					<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">Fat</span>
					<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
						<input
							type="number"
							class="w-full bg-transparent text-sm outline-none"
							bind:value={draft.fat}
							min="0"
							step="0.1"
						/>
						<span class="ml-1 shrink-0 text-xs text-muted-foreground">g</span>
					</div>
				</div>

				<!-- Protein -->
				<div class="flex flex-col gap-1">
					<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">Protein</span>
					<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
						<input
							type="number"
							class="w-full bg-transparent text-sm outline-none"
							bind:value={draft.protein}
							min="0"
							step="0.1"
						/>
						<span class="ml-1 shrink-0 text-xs text-muted-foreground">g</span>
					</div>
				</div>

				<!-- Energy -->
				<div class="flex flex-col gap-1">
					<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">Energy</span>
					<div class="flex items-center rounded-md px-3 py-2" style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04)">
						<input
							type="number"
							class="w-full bg-transparent text-sm outline-none"
							bind:value={draft.energy}
							min="0"
							step="1"
						/>
						<span class="ml-1 shrink-0 text-xs text-muted-foreground">kcal</span>
					</div>
				</div>

				<!-- Category -->
				<div class="flex flex-col gap-1">
					<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">Category</span>
					<select
						class="rounded-md px-3 py-2 text-sm outline-none"
						style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04); color: inherit"
						bind:value={draft.category}
					>
						<option value={undefined}>None</option>
						{#each foodState.categories as cat (cat)}
							<option value={cat}>{cat}</option>
						{/each}
					</select>
				</div>

				<!-- Subcategory -->
				<div class="flex flex-col gap-1">
					<span class="text-muted-foreground font-medium uppercase" style="font-size: 10px">Subcategory</span>
					<select
						class="rounded-md px-3 py-2 text-sm outline-none"
						style="border: 1px solid oklch(1 0 0 / 0.18); background: oklch(1 0 0 / 0.04); color: inherit"
						bind:value={draft.subcategory}
					>
						<option value={undefined}>None</option>
						{#each subcategories as sub (sub)}
							<option value={sub}>{sub}</option>
						{/each}
					</select>
				</div>
			</div>
		</details>

		<!-- Action buttons -->
		<div class="flex items-center gap-2">
			<button
				type="button"
				class="rounded-md border border-border px-4 py-2 text-sm text-muted-foreground hover:text-foreground transition-colors disabled:opacity-40"
				disabled={!canSave}
				onclick={() => submit(false)}
			>
				Save
			</button>
			<button
				type="button"
				class="flex items-center gap-1.5 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors disabled:opacity-40"
				disabled={!canSave}
				onclick={() => submit(true)}
			>
				Save and add another
				<kbd class="ml-1 rounded border border-white/20 px-1 py-0.5 text-[10px] font-normal opacity-60">⌘+Enter</kbd>
			</button>
		</div>
	</div>
</div>
