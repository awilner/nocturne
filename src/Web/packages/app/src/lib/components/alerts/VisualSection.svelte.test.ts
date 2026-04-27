import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import { describe, it, expect } from "vitest";
import VisualSection from "./VisualSection.svelte";

describe("VisualSection", () => {
	it("renders the section heading", async () => {
		render(VisualSection, {
			visual: {
				flashEnabled: false,
				flashColor: "#ff0000",
				persistentBanner: true,
				wakeScreen: false,
			},
		});

		await expect.element(page.getByText("Visual")).toBeVisible();
	});

	it("renders screen flash, persistent banner, and wake screen toggles", async () => {
		render(VisualSection, {
			visual: {
				flashEnabled: false,
				flashColor: "#ff0000",
				persistentBanner: true,
				wakeScreen: false,
			},
		});

		await expect.element(page.getByText("Screen Flash")).toBeVisible();
		await expect.element(page.getByText("Persistent Banner")).toBeVisible();
		await expect.element(page.getByText("Wake Screen")).toBeVisible();
	});

	it("hides flash color picker when flash is disabled", async () => {
		render(VisualSection, {
			visual: {
				flashEnabled: false,
				flashColor: "#ff0000",
				persistentBanner: true,
				wakeScreen: false,
			},
		});

		await expect
			.element(page.getByText("Flash Color"))
			.not.toBeInTheDocument();
	});

	it("shows flash color picker when flash is enabled", async () => {
		render(VisualSection, {
			visual: {
				flashEnabled: true,
				flashColor: "#ff0000",
				persistentBanner: true,
				wakeScreen: false,
			},
		});

		await expect.element(page.getByText("Flash Color")).toBeVisible();
	});
});
