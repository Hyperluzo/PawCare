import { api } from "./api";
import type {
  AdmitPatientRequest,
  Patient,
  PatientSearchResult,
  StatusChangeRequest,
  Treatment,
} from "../types/api";

export interface PatientSearchParams {
  name?: string;
  species?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export const patientsApi = {
  search: (params: PatientSearchParams = {}) => {
    const qs = new URLSearchParams(
      Object.entries(params)
        .filter(([, v]) => v !== undefined && v !== "")
        .map(([k, v]) => [k, String(v)])
    );
    return api.get<PatientSearchResult>(`/patients?${qs.toString()}`);
  },
  get: (id: number) => api.get<Patient>(`/patients/${id}`),
  admit: (data: AdmitPatientRequest) => api.post<Patient>("/patients", data),
  update: (id: number, data: Partial<Patient>) => api.put<Patient>(`/patients/${id}`, data),
  addTreatment: (
    id: number,
    data: { diagnosis: string; treatment: string; medication: string; notes?: string }
  ) => api.post<Treatment>(`/patients/${id}/treatments`, data),
  changeStatus: (id: number, data: StatusChangeRequest) =>
    api.patch<void>(`/patients/${id}/status`, data),
};
