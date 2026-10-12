import { FormEvent, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  AlertTriangle,
  ArrowLeft,
  ArrowRight,
  CheckCircle2,
  Clock,
  FileText,
  History,
  Pill,
  Plus,
  Stethoscope,
  User,
  X,
} from "lucide-react";
import { patientsApi } from "../services/patients";
import type { AdmissionStatus, ClinicalStatus, Patient } from "../types/api";
import { AdmissionStatusBadge, ClinicalStatusBadge, StatusBadge } from "../components/StatusBadge";
import { ApiError } from "../services/api";

const CLINICAL_OPTIONS: ClinicalStatus[] = [
  "UNDER_TREATMENT",
  "STABLE",
  "CRITICAL",
  "RECOVERING",
  "UNDER_OBSERVATION",
];

export default function PatientDetail() {
  const { id } = useParams();
  const patientId = Number(id);
  const [patient, setPatient] = useState<Patient | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [treatment, setTreatment] = useState({ diagnosis: "", treatment: "", medication: "", notes: "" });
  const [savingTreatment, setSavingTreatment] = useState(false);
  const [treatmentSuccess, setTreatmentSuccess] = useState<string | null>(null);

  const [newStatus, setNewStatus] = useState<ClinicalStatus>("UNDER_TREATMENT");
  const [markDischarged, setMarkDischarged] = useState(false);
  const [markDeceased, setMarkDeceased] = useState(false);
  const [showDeceasedModal, setShowDeceasedModal] = useState(false);
  const [modalConfirmed, setModalConfirmed] = useState(false);
  const [savingStatus, setSavingStatus] = useState(false);
  const [statusSuccess, setStatusSuccess] = useState<string | null>(null);

  function load() {
    patientsApi
      .get(patientId)
      .then((p) => {
        setPatient(p);
        setNewStatus(p.clinicalStatus);
      })
      .catch(() => setError("Couldn't load patient."));
  }

  useEffect(load, [patientId]);

  async function handleAddTreatment(e: FormEvent) {
    e.preventDefault();
    if (!treatment.diagnosis.trim() || !treatment.treatment.trim()) {
      setError("Please provide both diagnosis and treatment plan.");
      return;
    }
    setSavingTreatment(true);
    setError(null);
    setTreatmentSuccess(null);
    setStatusSuccess(null);
    try {
      await patientsApi.addTreatment(patientId, treatment);
      setTreatment({ diagnosis: "", treatment: "", medication: "", notes: "" });
      setTreatmentSuccess("Treatment recorded successfully and attributed to your staff account.");
      load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't save treatment record.");
    } finally {
      setSavingTreatment(false);
    }
  }

  async function executeStatusChange(admissionStatus?: AdmissionStatus, confirm?: boolean) {
    setSavingStatus(true);
    setError(null);
    setStatusSuccess(null);
    setTreatmentSuccess(null);
    try {
      await patientsApi.changeStatus(patientId, {
        newClinicalStatus: newStatus,
        newAdmissionStatus: admissionStatus,
        confirm: confirm,
      });
      setMarkDeceased(false);
      setMarkDischarged(false);
      setShowDeceasedModal(false);
      setModalConfirmed(false);
      setStatusSuccess("Patient status updated and logged to audit trail successfully.");
      load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't update status.");
    } finally {
      setSavingStatus(false);
    }
  }

  function handleStatusFormSubmit(e: FormEvent) {
    e.preventDefault();
    if (markDeceased) {
      setShowDeceasedModal(true);
      return;
    }

    const admissionStatus: AdmissionStatus | undefined = markDischarged ? "DISCHARGED" : undefined;
    executeStatusChange(admissionStatus, false);
  }

  function handleConfirmDeceasedSubmit() {
    if (!modalConfirmed) return;
    executeStatusChange("DECEASED", true);
  }

  if (error && !patient) return <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2">{error}</p>;
  if (!patient) return <p className="text-sm text-ink-muted">Loading…</p>;

  const inputClass =
    "w-full rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600 bg-surface";
  const labelClass = "block text-xs font-medium text-ink-muted mb-1";

  return (
    <div className="space-y-6">
      {/* Top Bar with back link and status badges */}
      <div>
        <Link
          to="/patients"
          className="inline-flex items-center gap-1.5 text-xs text-ink-muted hover:text-teal-700 transition-colors mb-2"
        >
          <ArrowLeft className="w-3.5 h-3.5" /> Back to Patients
        </Link>
        <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold text-ink">{patient.name}</h1>
            <p className="text-sm text-ink-muted">
              {patient.species} · {patient.breed} · {patient.age} yrs · {patient.gender}
            </p>
          </div>
          <div className="flex flex-wrap gap-2 items-center">
            <AdmissionStatusBadge status={patient.admissionStatus} />
            <ClinicalStatusBadge status={patient.clinicalStatus} />
          </div>
        </div>
      </div>

      {error && (
        <div className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3.5 py-2.5">
          {error}
        </div>
      )}

      {statusSuccess && (
        <div className="text-sm text-teal-800 bg-teal-50 border border-teal-200 rounded-lg px-3.5 py-2.5 flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 text-teal-600 shrink-0" />
          <span>{statusSuccess}</span>
        </div>
      )}

      {treatmentSuccess && (
        <div className="text-sm text-teal-800 bg-teal-50 border border-teal-200 rounded-lg px-3.5 py-2.5 flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 text-teal-600 shrink-0" />
          <span>{treatmentSuccess}</span>
        </div>
      )}

      {/* Owner & Ward Info + Update Status */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
        <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs">
          <h2 className="font-display font-semibold text-ink text-sm mb-3">Owner & Ward</h2>
          <dl className="text-sm space-y-2">
            <div className="flex justify-between border-b border-line/60 pb-1.5">
              <dt className="text-ink-muted">Owner</dt>
              <dd className="text-ink font-medium">{patient.owner.name}</dd>
            </div>
            <div className="flex justify-between border-b border-line/60 pb-1.5">
              <dt className="text-ink-muted">Contact Phone</dt>
              <dd className="text-ink">{patient.owner.phone}</dd>
            </div>
            <div className="flex justify-between border-b border-line/60 pb-1.5">
              <dt className="text-ink-muted">Email</dt>
              <dd className="text-ink">{patient.owner.email}</dd>
            </div>
            <div className="flex justify-between border-b border-line/60 pb-1.5">
              <dt className="text-ink-muted">Ward</dt>
              <dd className="text-ink font-medium">{patient.ward?.name ?? "Unassigned"}</dd>
            </div>
            <div className="flex justify-between border-b border-line/60 pb-1.5">
              <dt className="text-ink-muted">Admitted Date</dt>
              <dd className="text-ink">{new Date(patient.admittedAt).toLocaleDateString()}</dd>
            </div>
            {patient.dischargedAt && (
              <div className="flex justify-between">
                <dt className="text-ink-muted">Discharged / Concluded</dt>
                <dd className="text-ink font-medium">
                  {new Date(patient.dischargedAt).toLocaleDateString()}{" "}
                  {new Date(patient.dischargedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                </dd>
              </div>
            )}
          </dl>
        </div>

        <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs">
          <h2 className="font-display font-semibold text-ink text-sm mb-3">Update Status</h2>
          <form onSubmit={handleStatusFormSubmit} className="space-y-3">
            <div>
              <label className={labelClass}>Clinical Status</label>
              <select
                className={inputClass}
                value={newStatus}
                onChange={(e) => setNewStatus(e.target.value as ClinicalStatus)}
              >
                {CLINICAL_OPTIONS.map((s) => (
                  <option key={s} value={s}>
                    {s.replaceAll("_", " ")}
                  </option>
                ))}
              </select>
            </div>

            {patient.admissionStatus === "ADMITTED" && (
              <div className="space-y-2 pt-1">
                <label className="flex items-center gap-2 text-sm text-ink cursor-pointer">
                  <input
                    type="checkbox"
                    checked={markDischarged}
                    onChange={(e) => {
                      setMarkDischarged(e.target.checked);
                      if (e.target.checked) setMarkDeceased(false);
                    }}
                  />
                  <span>Mark as discharged</span>
                </label>
                <label className="flex items-center gap-2 text-sm text-red-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={markDeceased}
                    onChange={(e) => {
                      setMarkDeceased(e.target.checked);
                      if (e.target.checked) setMarkDischarged(false);
                    }}
                  />
                  <span>Mark as deceased (requires confirmation modal)</span>
                </label>
              </div>
            )}

            <button
              type="submit"
              disabled={savingStatus}
              className="rounded-lg bg-teal-700 text-white text-sm font-medium px-4 py-2 hover:bg-teal-800 transition-colors disabled:opacity-60 shadow-xs"
            >
              {savingStatus ? "Saving…" : "Update status"}
            </button>
          </form>
        </div>
      </div>

      {/* Treatments: Add Treatment Form & Treatment History */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
        {/* Add Treatment Form */}
        <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs">
          <div className="flex items-center gap-2 mb-3">
            <Stethoscope className="w-4 h-4 text-teal-700" />
            <h2 className="font-display font-semibold text-ink text-sm">Add Treatment Record</h2>
          </div>
          <form onSubmit={handleAddTreatment} className="space-y-3">
            <div>
              <label className={labelClass}>Diagnosis *</label>
              <input
                required
                className={inputClass}
                placeholder="e.g. Canine Parvovirus, Corneal Ulcer"
                value={treatment.diagnosis}
                onChange={(e) => setTreatment({ ...treatment, diagnosis: e.target.value })}
              />
            </div>
            <div>
              <label className={labelClass}>Treatment Plan / Actions *</label>
              <input
                required
                className={inputClass}
                placeholder="e.g. IV Fluid Therapy, Wound debridement & dressing"
                value={treatment.treatment}
                onChange={(e) => setTreatment({ ...treatment, treatment: e.target.value })}
              />
            </div>
            <div>
              <label className={labelClass}>Medication / Dosage</label>
              <input
                className={inputClass}
                placeholder="e.g. Cefazolin 22mg/kg IV q8h, Meloxicam 0.1mg/kg"
                value={treatment.medication}
                onChange={(e) => setTreatment({ ...treatment, medication: e.target.value })}
              />
            </div>
            <div>
              <label className={labelClass}>Clinical Notes</label>
              <textarea
                className={inputClass}
                rows={3}
                placeholder="Observations, patient response, vital signs..."
                value={treatment.notes}
                onChange={(e) => setTreatment({ ...treatment, notes: e.target.value })}
              />
            </div>
            <button
              type="submit"
              disabled={savingTreatment}
              className="inline-flex items-center gap-2 rounded-lg bg-teal-700 text-white text-sm font-medium px-4 py-2 hover:bg-teal-800 transition-colors disabled:opacity-60 shadow-xs"
            >
              {savingTreatment ? (
                <span>Recording…</span>
              ) : (
                <>
                  <Plus className="w-4 h-4" />
                  <span>Log Treatment</span>
                </>
              )}
            </button>
          </form>
        </div>

        {/* Treatment History */}
        <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs flex flex-col">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <FileText className="w-4 h-4 text-teal-700" />
              <h2 className="font-display font-semibold text-ink text-sm">Treatment History</h2>
            </div>
            <span className="text-xs px-2 py-0.5 rounded-full bg-teal-50 text-teal-700 font-medium">
              {patient.treatments.length} {patient.treatments.length === 1 ? "record" : "records"}
            </span>
          </div>

          {patient.treatments.length === 0 ? (
            <div className="flex-1 flex flex-col items-center justify-center py-10 text-center">
              <Stethoscope className="w-8 h-8 text-ink-muted/50 mb-2" />
              <p className="text-sm font-medium text-ink">No treatments logged yet</p>
              <p className="text-xs text-ink-muted max-w-xs mt-0.5">
                Use the form on the left to record the first diagnosis, treatment, and medication for {patient.name}.
              </p>
            </div>
          ) : (
            <ul className="space-y-3.5 max-h-[500px] overflow-y-auto pr-1 divide-y divide-line">
              {patient.treatments.map((t) => (
                <li key={t.id} className="pt-3.5 first:pt-0 space-y-1.5">
                  <div className="flex items-start justify-between gap-2">
                    <span className="text-sm font-semibold text-ink">{t.diagnosis}</span>
                    <span className="inline-flex items-center gap-1 text-[11px] text-ink-muted whitespace-nowrap shrink-0">
                      <Clock className="w-3 h-3" />
                      {new Date(t.createdAt).toLocaleDateString()}{" "}
                      {new Date(t.createdAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                    </span>
                  </div>

                  <div className="text-sm text-ink">
                    <span className="text-ink-muted">Plan: </span>
                    {t.treatment}
                  </div>

                  {t.medication && (
                    <div className="inline-flex items-center gap-1.5 text-xs text-teal-800 bg-teal-50 px-2 py-0.5 rounded-md">
                      <Pill className="w-3 h-3 text-teal-600 shrink-0" />
                      <span>{t.medication}</span>
                    </div>
                  )}

                  {t.notes && (
                    <div className="text-xs text-ink-muted bg-bg/80 border border-line/60 rounded-lg p-2 italic">
                      {t.notes}
                    </div>
                  )}

                  <div className="flex items-center gap-1.5 text-xs text-ink-muted pt-0.5">
                    <User className="w-3.5 h-3.5 text-ink-muted" />
                    <span>
                      Logged by <strong className="font-medium text-ink">{t.updatedByUserName}</strong>
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Status History & Audit Trail */}
      <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <History className="w-4 h-4 text-teal-700" />
            <h2 className="font-display font-semibold text-ink text-sm">Status History & Audit Trail</h2>
          </div>
          <span className="text-xs px-2.5 py-0.5 rounded-full bg-teal-50 text-teal-700 font-medium">
            {patient.statusHistory.length} {patient.statusHistory.length === 1 ? "audit entry" : "audit entries"}
          </span>
        </div>

        {patient.statusHistory.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-center border border-dashed border-line rounded-xl">
            <History className="w-7 h-7 text-ink-muted/50 mb-2" />
            <p className="text-sm font-medium text-ink">No status changes recorded yet</p>
            <p className="text-xs text-ink-muted max-w-sm mt-0.5">
              All clinical and admission status transitions for {patient.name} will be tracked here with staff attribution and timestamps.
            </p>
          </div>
        ) : (
          <div className="divide-y divide-line">
            {patient.statusHistory.map((item) => (
              <div key={item.id} className="py-3.5 first:pt-0 last:pb-0 flex flex-col sm:flex-row sm:items-center justify-between gap-2.5">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-xs text-ink-muted font-medium">Transition:</span>
                  <StatusBadge status={item.oldStatus} />
                  <ArrowRight className="w-3.5 h-3.5 text-ink-muted shrink-0" />
                  <StatusBadge status={item.newStatus} />
                </div>
                <div className="flex items-center gap-4 text-xs text-ink-muted flex-wrap">
                  <div className="inline-flex items-center gap-1.5">
                    <User className="w-3.5 h-3.5 text-ink-muted" />
                    <span>
                      Changed by <strong className="font-medium text-ink">{item.changedByUserName}</strong>
                    </span>
                  </div>
                  <div className="inline-flex items-center gap-1.5">
                    <Clock className="w-3.5 h-3.5 text-ink-muted" />
                    <span>
                      {new Date(item.changedAt).toLocaleDateString()}{" "}
                      {new Date(item.changedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Confirmation Modal for DECEASED Status */}
      {showDeceasedModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs">
          <div className="bg-surface rounded-2xl border border-line shadow-xl max-w-lg w-full p-6 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-start justify-between gap-3 mb-4">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-red-100 flex items-center justify-center text-red-600 shrink-0">
                  <AlertTriangle className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="font-display font-semibold text-ink text-base">Confirm Deceased Status</h3>
                  <p className="text-xs text-ink-muted">Irreversible clinical status change</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  setShowDeceasedModal(false);
                  setModalConfirmed(false);
                }}
                className="text-ink-muted hover:text-ink transition-colors p-1 rounded-lg hover:bg-bg"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="space-y-3 mb-5 text-sm">
              <div className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-900 text-xs leading-relaxed">
                <strong>Attention:</strong> Marking <strong>{patient.name}</strong> as <strong>DECEASED</strong> is an irreversible clinical milestone. This will:
                <ul className="list-disc list-inside mt-1.5 space-y-0.5">
                  <li>Record an audit entry in the permanent status history under your staff credentials</li>
                  <li>Queue a sensitive notification to owner <strong>{patient.owner.name}</strong> ({patient.owner.email})</li>
                  <li>Release any active ward assignment and set discharge timestamp</li>
                </ul>
              </div>

              <label className="flex items-start gap-2.5 p-3 rounded-xl border border-line bg-bg/50 cursor-pointer">
                <input
                  type="checkbox"
                  className="mt-0.5"
                  checked={modalConfirmed}
                  onChange={(e) => setModalConfirmed(e.target.checked)}
                />
                <span className="text-xs text-ink">
                  I confirm that <strong>{patient.name}</strong> has passed away, and I explicitly approve recording this clinical milestone in the official audit log.
                </span>
              </label>
            </div>

            <div className="flex items-center justify-end gap-2.5">
              <button
                type="button"
                disabled={savingStatus}
                onClick={() => {
                  setShowDeceasedModal(false);
                  setModalConfirmed(false);
                }}
                className="px-4 py-2 rounded-lg border border-line text-ink text-sm font-medium hover:bg-bg transition-colors"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={!modalConfirmed || savingStatus}
                onClick={handleConfirmDeceasedSubmit}
                className="px-4 py-2 rounded-lg bg-red-600 text-white text-sm font-medium hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed shadow-xs"
              >
                {savingStatus ? "Recording…" : "Confirm & Mark Deceased"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
