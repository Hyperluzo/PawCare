import { api } from "./api";
import type {
  Notification,
  NotificationDetail,
  NotificationListResponse,
  RetryNotificationResponse,
} from "../types/api";

export const notificationsApi = {
  getPatientNotifications: (patientId: number): Promise<Notification[]> =>
    api.get<Notification[]>(`/patients/${patientId}/notifications`),

  getAll: (params?: {
    status?: string;
    patientId?: number;
    page?: number;
    pageSize?: number;
  }): Promise<NotificationListResponse> => {
    const searchParams = new URLSearchParams();
    if (params?.status) searchParams.set("status", params.status);
    if (params?.patientId) searchParams.set("patientId", params.patientId.toString());
    if (params?.page) searchParams.set("page", params.page.toString());
    if (params?.pageSize) searchParams.set("pageSize", params.pageSize.toString());
    const query = searchParams.toString();
    return api.get<NotificationListResponse>(`/admin/notifications${query ? `?${query}` : ""}`);
  },

  getById: (id: number): Promise<NotificationDetail> =>
    api.get<NotificationDetail>(`/admin/notifications/${id}`),

  retry: (id: number): Promise<RetryNotificationResponse> =>
    api.post<RetryNotificationResponse>(`/admin/notifications/${id}/retry`),
};
