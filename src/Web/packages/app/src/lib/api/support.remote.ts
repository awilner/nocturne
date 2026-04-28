import { getRequestEvent, command } from '$app/server';
import { error, redirect } from '@sveltejs/kit';

export { getFallbackUrl } from '$api/generated/supports.generated.remote';

export const createIssue = command(async (params: {
  template: string;
  title: string;
  description: string;
  stepsToReproduce?: string;
  expectedBehavior?: string;
  actualBehavior?: string;
  cgmSource?: string;
  timeRange?: string;
  diagnosticInfo: string;
  images: File[];
}) => {
  const apiClient = getRequestEvent().locals.apiClient;
  try {
    const formData = new FormData();
    formData.append('template', params.template);
    formData.append('title', params.title);
    formData.append('description', params.description);
    if (params.stepsToReproduce) formData.append('stepsToReproduce', params.stepsToReproduce);
    if (params.expectedBehavior) formData.append('expectedBehavior', params.expectedBehavior);
    if (params.actualBehavior) formData.append('actualBehavior', params.actualBehavior);
    if (params.cgmSource) formData.append('cgmSource', params.cgmSource);
    if (params.timeRange) formData.append('timeRange', params.timeRange);
    formData.append('diagnosticInfo', params.diagnosticInfo);
    for (const image of params.images) {
      formData.append('images', image);
    }

    const url = apiClient.baseUrl + '/api/v4/support/issues';
    const response = await (apiClient as any).http.fetch(url, {
      body: formData,
      method: 'POST',
      headers: { 'Accept': 'application/json' },
    });
    if (!response.ok) {
      const err: any = new Error(`Issue creation failed (${response.status})`);
      err.status = response.status;
      throw err;
    }
    return await response.json();
  } catch (err) {
    const status = (err as any)?.status;
    if (status === 401) { const { url } = getRequestEvent(); throw redirect(302, `/auth/login?returnUrl=${encodeURIComponent(url.pathname + url.search)}`); }
    if (status === 403) throw error(403, 'Forbidden');
    console.error('Error in support.createIssue:', err);
    throw error(500, 'Failed to create issue');
  }
});
