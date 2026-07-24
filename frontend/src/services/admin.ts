import { api } from "./api";
import type { StaffUser } from "../types/api";

export const adminApi = {
  listUsers: () => api.get<StaffUser[]>("/admin/users"),
  createUser: (data: { name: string; email: string; password: string; accountType: "ADMIN" | "STAFF" }) =>
    api.post<StaffUser>("/admin/users", data),
  updateUser: (id: number, data: Partial<StaffUser>) => api.put<StaffUser>(`/admin/users/${id}`, data),
  setActive: (id: number, isActive: boolean) =>
    api.patch<void>(`/admin/users/${id}/status`, { isActive }),
  resetPassword: (id: number, newPassword: string) =>
    api.post<void>(`/admin/users/${id}/reset-password`, { newPassword }),
};
