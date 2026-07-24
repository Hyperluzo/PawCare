export type Role = "ADMIN" | "STAFF";

export type AdmissionStatus = "ADMITTED" | "DISCHARGED" | "DECEASED";
export type ClinicalStatus =
  | "UNDER_TREATMENT"
  | "STABLE"
  | "CRITICAL"
  | "RECOVERING"
  | "UNDER_OBSERVATION";

export interface StaffUser {
  id: number;
  name: string;
  email: string;
  accountType: Role;
  isActive: boolean;
}

export interface Ward {
  id: number;
  name: string;
  description: string;
  capacity: number;
  patientCount: number;
}

export interface Owner {
  id: number;
  name: string;
  email: string;
  phone: string;
  address?: string;
}

export interface Treatment {
  id: number;
  patientId: number;
  diagnosis: string;
  treatment: string;
  medication: string;
  notes?: string;
  updatedByUserName: string;
  createdAt: string;
}

export interface StatusHistoryEntry {
  id: number;
  oldStatus: string;
  newStatus: string;
  changedByUserName: string;
  changedAt: string;
}

export interface Patient {
  id: number;
  name: string;
  species: string;
  breed: string;
  age: number;
  gender: string;
  owner: Owner;
  ward: Ward;
  admissionStatus: AdmissionStatus;
  clinicalStatus: ClinicalStatus;
  admittedAt: string;
  dischargedAt?: string | null;
  treatments: Treatment[];
  statusHistory: StatusHistoryEntry[];
}

export interface AdmitPatientRequest {
  patientName: string;
  species: string;
  breed: string;
  age: number;
  gender: string;
  wardId: number;
  existingOwnerId?: number;
  newOwner?: {
    name: string;
    email: string;
    phone: string;
    address?: string;
  };
}

export interface StatusChangeRequest {
  newClinicalStatus: ClinicalStatus;
  newAdmissionStatus?: AdmissionStatus;
  confirm?: boolean;
}

export interface DashboardStats {
  totalPatients: number;
  underTreatment: number;
  treatedToday: number;
  deceasedMtd: number;
}

export interface PatientSearchResult {
  total: number;
  page: number;
  pageSize: number;
  results: Patient[];
}
