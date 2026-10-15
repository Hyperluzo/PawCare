import { api } from "./api";
import type {
  AdmitPatientRequest,
  Patient,
  PatientSearchResult,
  StatusChangeRequest,
  StatusHistoryEntry,
  Treatment,
} from "../types/api";

export interface PatientSearchParams {
  search?: string;
  name?: string;
  species?: string;
  breed?: string;
  owner?: string;
  status?: string;
  clinicalStatus?: string;
  admissionStatus?: string;
  wardId?: number;
  sortBy?: string;
  sortOrder?: string;
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
    data: { diagnosis: string; treatment: string; medication: string; notes?: string; attachmentUrl?: string; attachmentFileName?: string }
  ) => api.post<Treatment>(`/patients/${id}/treatments`, data),
  uploadPhoto: (id: number, file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.postFormData<FileUploadResponse>(`/patients/${id}/photo`, formData);
  },
  uploadDocument: (id: number, file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.postFormData<FileUploadResponse>(`/patients/${id}/document`, formData);
  },
  uploadTreatmentAttachment: (id: number, treatmentId: number, file: File) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.postFormData<FileUploadResponse>(`/patients/${id}/treatments/${treatmentId}/attachment`, formData);
  },
  changeStatus: (id: number, data: StatusChangeRequest) =>
    api.patch<Patient>(`/patients/${id}/status`, data),
  getStatusHistory: (id: number) =>
    api.get<StatusHistoryEntry[]>(`/patients/${id}/status-history`),
};
