import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import { describe, it, expect } from "vitest";
import SnoozeTab from "./SnoozeTab.svelte";

describe("SnoozeTab", () => {
	const default_snooze = {
		defaultMinutes: 15,
		options: [5, 15, 30, 60],
		maxCount: 5,
		smartSnooze: false,
		smartSnoozeExtendMinutes: 10,
	};

	it("renders default snooze duration input", async () => {
		render(SnoozeTab, { snooze: { ...default_snooze } });

		await expect
			.element(page.getByText("Default Snooze Duration (minutes)"))
			.toBeVisible();
	});

	it("renders snooze options as badges", async () => {
		render(SnoozeTab, { snooze: { ...default_snooze } });

		await expect.element(page.getByText("5m", { exact: true })).toBeVisible();
		await expect.element(page.getByText("15m")).toBeVisible();
		await expect.element(page.getByText("30m")).toBeVisible();
		await expect.element(page.getByText("60m")).toBeVisible();
	});

	it("renders max snooze count input", async () => {
		render(SnoozeTab, { snooze: { ...default_snooze } });

		await expect
			.element(page.getByText("Max Snooze Count"))
			.toBeVisible();
	});

	it("renders smart snooze toggle", async () => {
		render(SnoozeTab, { snooze: { ...default_snooze } });

		await expect.element(page.getByText("Smart Snooze")).toBeVisible();
	});

	it("hides smart snooze extend input when smart snooze is off", async () => {
		render(SnoozeTab, { snooze: { ...default_snooze } });

		await expect
			.element(page.getByText("Smart Snooze Extend (minutes)"))
			.not.toBeInTheDocument();
	});

	it("shows smart snooze extend input when smart snooze is on", async () => {
		render(SnoozeTab, {
			snooze: { ...default_snooze, smartSnooze: true },
		});

		await expect
			.element(page.getByText("Smart Snooze Extend (minutes)"))
			.toBeVisible();
		await expect
			.element(
				page.getByText(
					"Automatically extends snooze when glucose trend is favorable",
				),
			)
			.toBeVisible();
	});

	it("renders add button for snooze options", async () => {
		render(SnoozeTab, { snooze: { ...default_snooze } });

		await expect
			.element(page.getByRole("button", { name: /Add/i }))
			.toBeVisible();
	});

	it("renders snooze options with no options", async () => {
		render(SnoozeTab, {
			snooze: { ...default_snooze, options: [] },
		});

		await expect.element(page.getByText("Snooze Options")).toBeVisible();
		// Should still show add button
		await expect
			.element(page.getByRole("button", { name: /Add/i }))
			.toBeVisible();
	});
});
