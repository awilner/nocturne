import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import { describe, it, expect, vi } from "vitest";
import QuietHoursCard from "./QuietHoursCard.svelte";

describe("QuietHoursCard", () => {
	it("renders the card title", async () => {
		render(QuietHoursCard, {
			enabled: false,
			start: "22:00",
			end: "07:00",
			overrideCritical: true,
			saving: false,
			onSave: () => {},
		});

		await expect.element(page.getByText("Quiet Hours", { exact: true })).toBeVisible();
		await expect
			.element(page.getByText("Suppress non-critical alerts during specific hours"))
			.toBeVisible();
	});

	it("shows enable toggle", async () => {
		render(QuietHoursCard, {
			enabled: false,
			start: "22:00",
			end: "07:00",
			overrideCritical: true,
			saving: false,
			onSave: () => {},
		});

		await expect
			.element(page.getByText("Enable quiet hours"))
			.toBeVisible();
	});

	it("hides time inputs when disabled", async () => {
		render(QuietHoursCard, {
			enabled: false,
			start: "22:00",
			end: "07:00",
			overrideCritical: true,
			saving: false,
			onSave: () => {},
		});

		await expect
			.element(page.getByText("Start Time"))
			.not.toBeInTheDocument();
		await expect
			.element(page.getByText("End Time"))
			.not.toBeInTheDocument();
	});

	it("shows time inputs and critical override when enabled", async () => {
		render(QuietHoursCard, {
			enabled: true,
			start: "22:00",
			end: "07:00",
			overrideCritical: true,
			saving: false,
			onSave: () => {},
		});

		await expect.element(page.getByText("Start Time")).toBeVisible();
		await expect.element(page.getByText("End Time")).toBeVisible();
		await expect
			.element(page.getByText("Allow critical alerts during quiet hours"))
			.toBeVisible();
	});

	it("renders save button", async () => {
		render(QuietHoursCard, {
			enabled: false,
			start: "22:00",
			end: "07:00",
			overrideCritical: true,
			saving: false,
			onSave: () => {},
		});

		await expect
			.element(page.getByRole("button", { name: /Save/i }))
			.toBeVisible();
	});

	it("disables save button when saving", async () => {
		render(QuietHoursCard, {
			enabled: false,
			start: "22:00",
			end: "07:00",
			overrideCritical: true,
			saving: true,
			onSave: () => {},
		});

		await expect
			.element(page.getByRole("button", { name: /Save/i }))
			.toBeDisabled();
	});
});
