/**
 * Avatar upload and delete remote functions
 *
 * Manual remote functions for avatar management. The NSwag-generated
 * ApiClient does not support multipart FormData, so these use the same
 * auth-forwarding infrastructure (event.fetch, cookies, instance key,
 * X-Forwarded-Host) directly.
 */

import { command, getRequestEvent } from "$app/server";
import { getApiBaseUrl, getHashedInstanceKey } from "$lib/server/api-client-factory";
import { AUTH_COOKIE_NAMES } from "$lib/config/auth-cookies";

function buildHeaders(event: ReturnType<typeof getRequestEvent>): Headers {
  const headers = new Headers();

  // Forward auth cookies as the API expects
  const cookies: string[] = [];
  const accessToken = event?.cookies.get(AUTH_COOKIE_NAMES.accessToken);
  const refreshToken = event?.cookies.get(AUTH_COOKIE_NAMES.refreshToken);
  if (accessToken) cookies.push(`${AUTH_COOKIE_NAMES.accessToken}=${accessToken}`);
  if (refreshToken) cookies.push(`${AUTH_COOKIE_NAMES.refreshToken}=${refreshToken}`);
  if (cookies.length > 0) headers.set("Cookie", cookies.join("; "));

  // Instance key for service authentication
  const instanceKey = getHashedInstanceKey();
  if (instanceKey) headers.set("X-Instance-Key", instanceKey);

  // Forward original host for tenant resolution behind YARP
  const forwardedHost = event?.request.headers.get("x-forwarded-host")
    ?? event?.request.headers.get("host");
  if (forwardedHost) headers.set("X-Forwarded-Host", forwardedHost);

  return headers;
}

/**
 * Upload an avatar image. The API resizes to 256x256 WebP.
 * Returns the public avatar URL on success.
 */
export const uploadAvatar = command(async (file: File) => {
  const event = getRequestEvent();
  const baseUrl = getApiBaseUrl();
  if (!baseUrl) throw new Error("API URL not configured");

  const formData = new FormData();
  formData.append("file", file);

  const headers = buildHeaders(event);
  // Do not set Content-Type — fetch sets it with the multipart boundary

  const response = await event.fetch(`${baseUrl}/api/v4/me/avatar`, {
    method: "POST",
    headers,
    body: formData,
  });

  if (!response.ok) {
    let detail = `Upload failed (${response.status})`;
    try {
      const body = await response.json();
      if (body?.detail) detail = body.detail;
    } catch { /* use default message */ }
    throw new Error(detail);
  }

  const result: { avatarUrl: string } = await response.json();
  return result;
});

/**
 * Delete the current user's avatar.
 */
export const deleteAvatar = command(async () => {
  const event = getRequestEvent();
  const baseUrl = getApiBaseUrl();
  if (!baseUrl) throw new Error("API URL not configured");

  const headers = buildHeaders(event);

  const response = await event.fetch(`${baseUrl}/api/v4/me/avatar`, {
    method: "DELETE",
    headers,
  });

  if (!response.ok) {
    throw new Error(`Delete failed (${response.status})`);
  }

  return { success: true };
});
