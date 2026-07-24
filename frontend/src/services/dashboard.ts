import { api } from "./api";
import type { DashboardStats } from "../types/api";

export const dashboardApi = {
  stats: () => api.get<DashboardStats>("/dashboard/stats"),
};
