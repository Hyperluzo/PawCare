import { api } from "./api";
import type { Patient, Ward } from "../types/api";

export const wardsApi = {
  list: () => api.get<Ward[]>("/wards"),
  create: (ward: { name: string; description: string; capacity: number }) =>
    api.post<Ward>("/wards", ward),
  update: (id: number, ward: Partial<Ward>) => api.put<Ward>(`/wards/${id}`, ward),
  patients: (id: number) => api.get<Patient[]>(`/wards/${id}/patients`),
};
