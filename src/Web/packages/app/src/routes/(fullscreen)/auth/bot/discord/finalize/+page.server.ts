import type { PageServerLoad } from "./$types";
import { error, redirect } from "@sveltejs/kit";

/**
 * Tenant-scoped finalize hop for the Discord OAuth2 link flow.
 *
 * The apex `/auth/bot/discord/callback` redirects here after successfully
 * proving the user's Discord identity. The page consumes the short-lived
 * claim token (which the apex callback issued via the cross-tenant
 * pending-links endpoint) and creates the directory link in this tenant's
 * context.
 *
 * If the user isn't currently signed in to this tenant, they get bounced to
 * `/auth/login?returnUrl=...` first. After successful login they come back
 * here and the token is still (hopefully) valid.
 */
export const load: PageServerLoad = async ({ url, locals }) => {
	const token = url.searchParams.get("token");
	if (!token) {
		throw error(400, "Missing token parameter.");
	}

	if (!locals.isAuthenticated) {
		const returnUrl = `/auth/bot/discord/finalize?token=${token}`;
		throw redirect(303, `/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
	}

	try {
		const link = await locals.apiClient.chatIdentity.claimLink({ token });
		return {
			success: true as const,
			label: link.label ?? "",
			displayName: link.displayName ?? "",
		};
	} catch (err: unknown) {
		console.error("Failed to claim OAuth2 link:", err);
		return {
			success: false as const,
			message: "We couldn't complete the Discord link. The token may have expired. Please try again from Settings.",
		};
	}
};
