import { FormEvent, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { patientsApi } from "../services/patients";
import type { ClinicalStatus, Patient } from "../types/api";
import { AdmissionStatusBadge, ClinicalStatusBadge } from "../components/StatusBadge";
import { ApiError } from "../services/api";

const CLINICAL_OPTIONS: ClinicalStatus[] = ["UNDER_TREATMENT", "STABLE", "CRITICAL", "RECOVERING", "UNDER_OBSERVATION"];

export default function PatientDetail() {
  const { id } = useParams();
  const patientId = Number(id);
  const [patient, setPatient] = useState<Patient | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [treatment, setTreatment] = useState({ diagnosis: "", treatment: "", medication: "", notes: "" });
  const [savingTreatment, setSavingTreatment] = useState(false);

  const [newStatus, setNewStatus] = useState<ClinicalStatus>("UNDER_TREATMENT");
  const [markDischarged, setMarkDischarged] = useState(false);
  const [markDeceased, setMarkDeceased] = useState(false);
  const [confirmDeceased, setConfirmDeceased] = useState(false);
  const [savingStatus, setSavingStatus] = useState(false);

  function load() {
    patientsApi.get(patientId).then((p) => {
      setPatient(p);
      setNewStatus(p.clinicalStatus);
    }).catch(() => setError("Couldn't load patient."));
  }

  useEffect(load, [patientId]);

  async function handleAddTreatment(e: FormEvent) {
    e.preventDefault();
    setSavingTreatment(true);
    try {
      await patientsApi.addTreatment(patientId, treatment);
      setTreatment({ diagnosis: "", treatment: "", medication: "", notes: "" });
      load();
    } catch {
      setError("Couldn't save treatment.");
    } finally {
      setSavingTreatment(false);
    }
  }

  async function handleStatusChange(e: FormEvent) {
    e.preventDefault();
    if (markDeceased && !confirmDeceased) {
      setError("Please confirm before marking a patient as deceased.");
      return;
    }
    setSavingStatus(true);
    setError(null);
    try {
      await patientsApi.changeStatus(patientId, {
        newClinicalStatus: newStatus,
        newAdmissionStatus: markDeceased ? "DECEASED" : markDischarged ? "DISCHARGED" : undefined,
        confirm: markDeceased ? confirmDeceased : undefined,
      });
      setMarkDeceased(false);
      setMarkDischarged(false);
      setConfirmDeceased(false);
      load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't update status.");
    } finally {
      setSavingStatus(false);
    }
  }

  if (error && !patient) return <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2">{error}</p>;
  if (!patient) return <p className="text-sm text-ink-muted">Loading…</p>;

  const inputClass = "w-full rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600";
  const labelClass = "block text-xs font-medium text-ink-muted mb-1";

  return (
    <div>
      <div className="flex items-start justify-between mb-1">
        <div>
          <h1 className="font-display text-2xl font-semibold text-ink">{patient.name}</h1>
          <p className="text-sm text-ink-muted">{patient.species} · {patient.breed} · {patient.age} yrs · {patient.gender}</p>
        </div>
        <div className="flex gap-2">
          <AdmissionStatusBadge status={patient.admissionStatus} />
          <ClinicalStatusBadge status={patient.clinicalStatus} />
        </div>
      </div>

      {error && <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2 mt-4">{error}</p>}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 mt-6">
        <div className="bg-surface border border-line rounded-2xl p-5">
          <h2 className="font-display font-semibold text-ink text-sm mb-3">Owner & Ward</h2>
          <dl className="text-sm space-y-1.5">
            <div className="flex justify-between"><dt className="text-ink-muted">Owner</dt><dd className="text-ink">{patient.owner.name}</dd></div>
            <div className="flex justify-between"><dt className="text-ink-muted">Contact</dt><dd className="text-ink">{patient.owner.phone}</dd></div>
            <div className="flex justify-between"><dt className="text-ink-muted">Email</dt><dd className="text-ink">{patient.owner.email}</dd></div>
            <div className="flex justify-between"><dt className="text-ink-muted">Ward</dt><dd className="text-ink">{patient.ward?.name}</dd></div>
            <div className="flex justify-between"><dt className="text-ink-muted">Admitted</dt><dd className="text-ink">{new Date(patient.admittedAt).toLocaleDateString()}</dd></div>
          </dl>
        </div>

        <div className="bg-surface border border-line rounded-2xl p-5">
          <h2 className="font-display font-semibold text-ink text-sm mb-3">Update status</h2>
          <form onSubmit={handleStatusChange} className="space-y-3">
            <div>
              <label className={labelClass}>Clinical status</label>
              <select className={inputClass} value={newStatus} onChange={(e) => setNewStatus(e.target.value as ClinicalStatus)}>
                {CLINICAL_OPTIONS.map((s) => <option key={s} value={s}>{s.replaceAll("_", " ")}</option>)}
              </select>
            </div>

            {patient.admissionStatus === "ADMITTED" && (
              <div className="space-y-2">
                <label className="flex items-center gap-2 text-sm text-ink">
                  <input type="checkbox" checked={markDischarged} onChange={(e) => { setMarkDischarged(e.target.checked); if (e.target.checked) setMarkDeceased(false); }} />
                  Mark as discharged
                </label>
                <label className="flex items-center gap-2 text-sm text-ink">
                  <input type="checkbox" checked={markDeceased} onChange={(e) => { setMarkDeceased(e.target.checked); if (e.target.checked) setMarkDischarged(false); }} />
                  Mark as deceased
                </label>
                {markDeceased && (
                  <label className="flex items-center gap-2 text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2">
                    <input type="checkbox" checked={confirmDeceased} onChange={(e) => setConfirmDeceased(e.target.checked)} />
                    I confirm this patient should be marked deceased. The owner will be notified.
                  </label>
                )}
              </div>
            )}

            <button type="submit" disabled={savingStatus}
              className="rounded-lg bg-teal-700 text-white text-sm font-medium px-4 py-2 hover:bg-teal-800 transition-colors disabled:opacity-60">
              {savingStatus ? "Saving…" : "Update status"}
            </button>
          </form>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 mt-5">
        <div className="bg-surface border border-line rounded-2xl p-5">
          <h2 className="font-display font-semibold text-ink text-sm mb-3">Add treatment</h2>
          <form onSubmit={handleAddTreatment} className="space-y-3">
            <div>
              <label className={labelClass}>Diagnosis</label>
              <input required className={inputClass} value={treatment.diagnosis} onChange={(e) => setTreatment({ ...treatment, diagnosis: e.target.value })} />
            </div>
            <div>
              <label className={labelClass}>Treatment</label>
              <input required className={inputClass} value={treatment.treatment} onChange={(e) => setTreatment({ ...treatment, treatment: e.target.value })} />
            </div>
            <div>
              <label className={labelClass}>Medication</label>
              <input className={inputClass} value={treatment.medication} onChange={(e) => setTreatment({ ...treatment, medication: e.target.value })} />
            </div>
            <div>
              <label className={labelClass}>Notes</label>
              <textarea className={inputClass} rows={2} value={treatment.notes} onChange={(e) => setTreatment({ ...treatment, notes: e.target.value })} />
            </div>
            <button type="submit" disabled={savingTreatment}
              className="rounded-lg bg-teal-700 text-white text-sm font-medium px-4 py-2 hover:bg-teal-800 transition-colors disabled:opacity-60">
              {savingTreatment ? "Saving…" : "Add treatment"}
            </button>
          </form>
        </div>

        <div className="bg-surface border border-line rounded-2xl p-5">
          <h2 className="font-display font-semibold text-ink text-sm mb-3">Treatment history</h2>
          {patient.treatments.length === 0 ? (
            <p className="text-sm text-ink-muted">No treatments logged yet.</p>
          ) : (
            <ul className="space-y-3">
              {patient.treatments.map((t) => (
                <li key={t.id} className="border-b border-line last:border-0 pb-3 last:pb-0">
                  <div className="text-sm font-medium text-ink">{t.diagnosis}</div>
                  <div className="text-sm text-ink-muted">{t.treatment}{t.medication ? ` · ${t.medication}` : ""}</div>
                  <div className="text-xs text-ink-muted mt-1">{t.updatedByUserName} · {new Date(t.createdAt).toLocaleString()}</div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}
