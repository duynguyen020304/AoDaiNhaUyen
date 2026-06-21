import { request, requestPaginated } from "./client";
import type { PaginatedApiEnvelope } from "@/types/api";
import type {
  EmailJobDetail,
  EmailJobListItem,
  EmailTemplateDetail,
  EmailTemplateListItem,
  ImportSubscribersRequest,
  ImportSubscribersResult,
  MarketingCampaignSendResult,
  MarketingContentOption,
  MarketingStats,
  SendMarketingCampaignRequest,
  SubscriberDetail,
  SubscriberListItem,
} from "@/types/admin";

function qs(params: Record<string, string | number | boolean | undefined>) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== "") search.set(k, String(v));
  });
  return search.toString();
}

export const getMarketingStats = () =>
  request<MarketingStats>("/api/admin/marketing/stats");
export const getMarketingContentOptions = () =>
  request<MarketingContentOption[]>("/api/admin/marketing/content-options");
export const sendMarketingCampaign = (data: SendMarketingCampaignRequest) =>
  request<MarketingCampaignSendResult>("/api/admin/marketing/campaigns/send", {
    method: "POST",
    body: JSON.stringify(data),
  });
export const getEmailTemplates = (
  search = "",
  includeDeleted = false,
  page = 1,
  pageSize = 20,
): Promise<PaginatedApiEnvelope<EmailTemplateListItem[]>> =>
  requestPaginated(
    `/api/admin/email-templates?${qs({ search, includeDeleted, page, pageSize })}`,
  );
export const getEmailTemplate = (id: string) =>
  request<EmailTemplateDetail>(`/api/admin/email-templates/${id}`);
export const getSubscribers = (
  search = "",
  status = "",
  includeDeleted = false,
  page = 1,
  pageSize = 20,
): Promise<PaginatedApiEnvelope<SubscriberListItem[]>> =>
  requestPaginated(
    `/api/admin/subscribers?${qs({ search, status, includeDeleted, page, pageSize })}`,
  );
export const getSubscriber = (id: string) =>
  request<SubscriberDetail>(`/api/admin/subscribers/${id}`);
export const unsubscribeSubscriber = (id: string) =>
  request<void>(`/api/admin/subscribers/${id}/unsubscribe`, {
    method: "PATCH",
  });
export const deleteSubscriber = (id: string) =>
  request<void>(`/api/admin/subscribers/${id}`, { method: "DELETE" });
export const importSubscribers = (data: ImportSubscribersRequest) =>
  request<ImportSubscribersResult>("/api/admin/subscribers/import", {
    method: "POST",
    body: JSON.stringify(data),
  });

export const getEmailJobs = (
  status = "",
  page = 1,
  pageSize = 20,
): Promise<PaginatedApiEnvelope<EmailJobListItem[]>> =>
  requestPaginated(`/api/admin/email-jobs?${qs({ status, page, pageSize })}`);
export const getEmailJob = (id: string) =>
  request<EmailJobDetail>(`/api/admin/email-jobs/${id}`);
export const retryEmailJob = (id: string) =>
  request<void>(`/api/admin/email-jobs/${id}/retry`, { method: "PATCH" });
export const cancelEmailJob = (id: string) =>
  request<void>(`/api/admin/email-jobs/${id}/cancel`, { method: "PATCH" });
export const deleteEmailJob = (id: string) =>
  request<void>(`/api/admin/email-jobs/${id}`, { method: "DELETE" });
