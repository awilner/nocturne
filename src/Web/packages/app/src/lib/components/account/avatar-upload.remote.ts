/**
 * Avatar upload and delete remote functions
 *
 * Manual remote functions for avatar management. These forward multipart
 * FormData (upload) and DELETE requests to the API's avatar endpoint.
 * Once the NSwag client is regenerated with avatar support, the generated
 * remote functions could replace these -- but FormData uploads typically
 * need manual handling regardless.
 */

import { command, getRequestEvent } from "$app/server";
import { getApiBaseUrl } from "$lib/server/api-client-factory";
import { AUTH_COOKIE_NAMES } from "$lib/config/auth-cookies";

function getAuthHeaders(event: ReturnType<typeof getRequestEvent>): Record<string, string> {
  const headers: Record<string, string> = {};
  const accessToken = event?.cookies.get(AUTH_COOKIE_NAMES.accessToken);
  if (accessToken) {
    headers["Authorization"] = `Bearer ${accessToken}`;
  }
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

  const response = await event.fetch(`${baseUrl}/api/v4/me/avatar`, {
    method: "POST",
    headers: getAuthHeaders(event),
    body: formData,
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(body || `Upload failed (${response.status})`);
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

  const response = await event.fetch(`${baseUrl}/api/v4/me/avatar`, {
    method: "DELETE",
    headers: getAuthHeaders(event),
  });

  if (!response.ok && response.status !== 204) {
    throw new Error(`Delete failed (${response.status})`);
  }

  return { success: true };
});
