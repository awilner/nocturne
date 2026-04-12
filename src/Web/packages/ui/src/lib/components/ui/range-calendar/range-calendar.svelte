<script lang="ts">
	import { RangeCalendar as RangeCalendarPrimitive } from "bits-ui";
	import Cell from "./range-calendar-cell.svelte";
	import Day from "./range-calendar-day.svelte";
	import Grid from "./range-calendar-grid.svelte";
	import Header from "./range-calendar-header.svelte";
	import Months from "./range-calendar-months.svelte";
	import GridRow from "./range-calendar-grid-row.svelte";
	import GridBody from "./range-calendar-grid-body.svelte";
	import GridHead from "./range-calendar-grid-head.svelte";
	import HeadCell from "./range-calendar-head-cell.svelte";
	import NextButton from "./range-calendar-next-button.svelte";
	import PrevButton from "./range-calendar-prev-button.svelte";
	import Caption from "./range-calendar-caption.svelte";
	import Nav from "./range-calendar-nav.svelte";
	import Month from "./range-calendar-month.svelte";
	import { cn, type WithoutChildrenOrChild } from "../../../utils";
	import type { ButtonVariant } from "../button/index.js";
	import type { Snippet } from "svelte";
	import { isEqualMonth, type DateValue } from "@internationalized/date";

	let {
		ref = $bindable(null),
		value = $bindable(),
		placeholder = $bindable(),
		weekdayFormat = "short",
		class: className,
		buttonVariant = "ghost",
		captionLayout = "label",
		locale = "en-US",
		months: monthsProp,
		years,
		monthFormat: monthFormatProp,
		yearFormat = "numeric",
		day,
		disableDaysOutsideMonth = false,
		...restProps
	}: WithoutChildrenOrChild<RangeCalendarPrimitive.RootProps> & {
		buttonVariant?: ButtonVariant;
		captionLayout?: "dropdown" | "dropdown-months" | "dropdown-years" | "label";
		months?: RangeCalendarPrimitive.MonthSelectProps["months"];
		years?: RangeCalendarPrimitive.YearSelectProps["years"];
		monthFormat?: RangeCalendarPrimitive.MonthSelectProps["monthFormat"];
		yearFormat?: RangeCalendarPrimitive.YearSelectProps["yearFormat"];
		day?: Snippet<[{ day: DateValue; outsideMonth: boolean }]>;
	} = $props();

	const monthFormat = $derived.by(() => {
		if (monthFormatProp) return monthFormatProp;
		if (captionLayout.startsWith("dropdown")) return "short";
		return "long";
	});
</script>

<RangeCalendarPrimitive.Root
	bind:ref
	bind:value
	bind:placeholder
	{weekdayFormat}
	{disableDaysOutsideMonth}
	class={cn(
		"bg-background group/calendar p-3 [--cell-size:--spacing(8)] [[data-slot=card-content]_&]:bg-transparent [[data-slot=popover-content]_&]:bg-transparent",
		className
	)}
	{locale}
	{monthFormat}
	{yearFormat}
	{...restProps}
>
	{#snippet children({ months, weekdays })}
		<Months>
			<Nav>
				<PrevButton variant={buttonVariant} />
				<NextButton variant={buttonVariant} />
			</Nav>
			{#each months as month, monthIndex (month)}
				<Month>
					<Header>
						<Caption
							{captionLayout}
							months={monthsProp}
							{monthFormat}
							{years}
							{yearFormat}
							month={month.value}
							bind:placeholder
							{locale}
							{monthIndex}
						/>
					</Header>

					<Grid>
						<GridHead>
							<GridRow class="select-none">
								{#each weekdays as weekday (weekday)}
									<HeadCell>
										{weekday.slice(0, 2)}
									</HeadCell>
								{/each}
							</GridRow>
						</GridHead>
						<GridBody>
							{#each month.weeks as weekDates (weekDates)}
								<GridRow class="mt-2 w-full">
									{#each weekDates as date (date)}
										<Cell {date} month={month.value}>
											{#if day}
												{@render day({
													day: date,
													outsideMonth: !isEqualMonth(date, month.value),
												})}
											{:else}
												<Day />
											{/if}
										</Cell>
									{/each}
								</GridRow>
							{/each}
						</GridBody>
					</Grid>
				</Month>
			{/each}
		</Months>
	{/snippet}
</RangeCalendarPrimitive.Root>
