import { FormEvent, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  AlertTriangle,
  ArrowLeft,
  ArrowRight,
  CheckCircle2,
  Clock,
  Eye,
  FileText,
  History,
  Mail,
  Pill,
  Plus,
  RefreshCw,
  Send,
  Stethoscope,
  User,
  X,
} from "lucide-react";
import { patientsApi } from "../services/patients";
import { notificationsApi } from "../services/notifications";
import { useAuth } from "../context/AuthContext";
import type {
  AdmissionStatus,
  ClinicalStatus,
  Notification,
  NotificationDetail,
  Patient,
} from "../types/api";
import {
  AdmissionStatusBadge,
  ClinicalStatusBadge,
  NotificationStatusBadge,
  StatusBadge,
} from "../components/StatusBadge";
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
  const { user } = useAuth();
  const isAdmin = user?.role === "ADMIN";

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

  // Notification states
  const [retryingNotificationId, setRetryingNotificationId] = useState<number | null>(null);
  const [notificationMsg, setNotificationMsg] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [previewNotification, setPreviewNotification] = useState<NotificationDetail | null>(null);
  const [loadingPreviewId, setLoadingPreviewId] = useState<number | null>(null);

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
    setNotificationMsg(null);
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
      setStatusSuccess("Patient status updated and logged to audit trail. Automated owner email dispatched via AWS SES.");
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

  async function handleRetryNotification(notificationId: number) {
    setRetryingNotificationId(notificationId);
    setNotificationMsg(null);
    try {
      const res = await notificationsApi.retry(notificationId);
      if (res.success) {
        setNotificationMsg({ type: "success", text: res.message });
      } else {
        setNotificationMsg({ type: "error", text: res.message });
      }
      load();
    } catch (err) {
      setNotificationMsg({
        type: "error",
        text: err instanceof ApiError ? err.message : "Notification retry request failed.",
      });
    } finally {
      setRetryingNotificationId(null);
    }
  }

  async function handleOpenPreview(n: Notification) {
    setLoadingPreviewId(n.id);
    try {
      if (isAdmin) {
        const detail = await notificationsApi.getById(n.id);
        setPreviewNotification(detail);
      } else {
        // Staff preview display
        setPreviewNotification({
          ...n,
          subject: `PawCare Medical Update: ${patient?.name ?? n.patientName}`,
          bodyHtml: `<p>Status notification (${n.notificationType}) dispatched to ${n.recipientEmail}.</p>`,
          bodyText: `Status notification (${n.notificationType}) dispatched to ${n.recipientEmail}.`,
        });
      }
    } catch {
      setPreviewNotification({
        ...n,
        subject: `PawCare Medical Update: ${patient?.name ?? n.patientName}`,
        bodyHtml: `<p>Status notification (${n.notificationType}) recorded for ${n.recipientEmail}.</p>`,
        bodyText: `Status notification (${n.notificationType}) recorded for ${n.recipientEmail}.`,
      });
    } finally {
      setLoadingPreviewId(null);
    }
  }

  if (error && !patient) return <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2">{error}</p>;
  if (!patient) return <p className="text-sm text-ink-muted">Loading…</p>;

  const inputClass =
    "w-full rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600 bg-surface";
  const labelClass = "block text-xs font-medium text-ink-muted mb-1";

  const notifications = patient.notifications ?? [];

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
            <h1 className="font-display font-semibold text-2xl text-ink tracking-tight">{patient.name}</h1>
            <p className="text-sm text-ink-muted mt-0.5">
              {patient.species} &bull; {patient.breed} &bull; {patient.age} yrs &bull; {patient.gender}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <AdmissionStatusBadge status={patient.admissionStatus} />
            <ClinicalStatusBadge status={patient.clinicalStatus} />
          </div>
        </div>
      </div>

      {/* Global Alerts */}
      {error && (
        <div className="flex items-center gap-2 p-3.5 rounded-xl bg-red-50 border border-red-200 text-red-800 text-sm">
          <AlertTriangle className="w-4 h-4 shrink-0 text-red-600" />
          <span>{error}</span>
        </div>
      )}

      {statusSuccess && (
        <div className="flex items-center gap-2 p-3.5 rounded-xl bg-green-50 border border-green-200 text-green-800 text-sm">
          <CheckCircle2 className="w-4 h-4 shrink-0 text-green-600" />
          <span>{statusSuccess}</span>
        </div>
      )}

      {treatmentSuccess && (
        <div className="flex items-center gap-2 p-3.5 rounded-xl bg-teal-50 border border-teal-200 text-teal-800 text-sm">
          <CheckCircle2 className="w-4 h-4 shrink-0 text-teal-600" />
          <span>{treatmentSuccess}</span>
        </div>
      )}

      {notificationMsg && (
        <div
          className={`flex items-center gap-2 p-3.5 rounded-xl text-sm border ${
            notificationMsg.type === "success"
              ? "bg-emerald-50 border-emerald-200 text-emerald-800"
              : "bg-red-50 border-red-200 text-red-800"
          }`}
        >
          {notificationMsg.type === "success" ? (
            <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600" />
          ) : (
            <AlertTriangle className="w-4 h-4 shrink-0 text-red-600" />
          )}
          <span>{notificationMsg.text}</span>
        </div>
      )}

      {/* Primary Details Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
        {/* Ward Info */}
        <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs">
          <h2 className="font-display font-semibold text-ink text-sm mb-3">Ward Assignment</h2>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-ink-muted">Ward</span>
              <span className="font-medium text-ink">{patient.ward.name}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-ink-muted">Capacity</span>
              <span className="font-medium text-ink">
                {patient.ward.patientCount} / {patient.ward.capacity} occupied
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-ink-muted">Admitted At</span>
              <span className="font-medium text-ink">
                {new Date(patient.admittedAt).toLocaleDateString()}
              </span>
            </div>
            {patient.dischargedAt && (
              <div className="flex justify-between">
                <span className="text-ink-muted">Discharged At</span>
                <span className="font-medium text-ink">
                  {new Date(patient.dischargedAt).toLocaleDateString()}
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Owner Info */}
        <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs">
          <h2 className="font-display font-semibold text-ink text-sm mb-3">Owner Information</h2>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-ink-muted">Name</span>
              <span className="font-medium text-ink">{patient.owner.name}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-ink-muted">Email</span>
              <span className="font-medium text-ink">{patient.owner.email}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-ink-muted">Phone</span>
              <span className="font-medium text-ink">{patient.owner.phone}</span>
            </div>
            {patient.owner.address && (
              <div className="flex justify-between">
                <span className="text-ink-muted">Address</span>
                <span className="font-medium text-ink truncate max-w-[160px]">{patient.owner.address}</span>
              </div>
            )}
          </div>
        </div>

        {/* Update Status Form */}
        <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs">
          <div className="flex items-center justify-between mb-3">
            <h2 className="font-display font-semibold text-ink text-sm">Change Status</h2>
            <span className="text-[11px] font-medium text-teal-700 bg-teal-50 px-2 py-0.5 rounded-full inline-flex items-center gap-1">
              <Send className="w-2.5 h-2.5" /> Auto-SES Email
            </span>
          </div>

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
                    {s.replace("_", " ")}
                  </option>
                ))}
              </select>
            </div>

            {patient.admissionStatus === "ADMITTED" && (
              <div className="space-y-2 pt-1 border-t border-line/60">
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

            <div className="text-[11px] text-ink-muted bg-bg p-2 rounded-lg leading-relaxed">
              Updating status logs to the audit history and dispatches an automated notification email to{" "}
              <strong className="text-ink">{patient.owner.email}</strong>.
            </div>

            <button
              type="submit"
              disabled={savingStatus}
              className="w-full rounded-lg bg-teal-700 text-white text-sm font-medium px-4 py-2 hover:bg-teal-800 transition-colors disabled:opacity-60 shadow-xs inline-flex items-center justify-center gap-2"
            >
              {savingStatus ? (
                <span>Recording & Dispatching…</span>
              ) : (
                <>
                  <Send className="w-3.5 h-3.5" />
                  <span>Update Status & Notify Owner</span>
                </>
              )}
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
              <label className={labelClass}>Medication / Prescriptions</label>
              <input
                className={inputClass}
                placeholder="e.g. Amoxicillin 250mg BID, Meloxicam 0.1mg/kg"
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

      {/* Owner Email Notifications Section (Day 12 SES Integration) */}
      <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Mail className="w-4 h-4 text-teal-700" />
            <h2 className="font-display font-semibold text-ink text-sm">Owner Email Notifications (AWS SES)</h2>
          </div>
          <span className="text-xs px-2.5 py-0.5 rounded-full bg-teal-50 text-teal-700 font-medium">
            {notifications.length} {notifications.length === 1 ? "notification" : "notifications"}
          </span>
        </div>

        {notifications.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-center border border-dashed border-line rounded-xl">
            <Mail className="w-7 h-7 text-ink-muted/50 mb-2" />
            <p className="text-sm font-medium text-ink">No owner notifications logged yet</p>
            <p className="text-xs text-ink-muted max-w-sm mt-0.5">
              Whenever you update {patient.name}'s clinical or admission status, an email notification is automatically queued, rendered via AWS SES templates, and logged here.
            </p>
          </div>
        ) : (
          <div className="divide-y divide-line">
            {notifications.map((item) => (
              <div key={item.id} className="py-4 first:pt-0 last:pb-0 space-y-2">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5">
                  <div className="flex items-center gap-2 flex-wrap">
                    <NotificationStatusBadge status={item.deliveryStatus} />
                    <span className="text-xs font-semibold px-2 py-0.5 rounded bg-bg text-ink border border-line">
                      {item.notificationType.replace("_", " ")}
                    </span>
                    <span className="text-xs text-ink-muted">
                      Recipient: <strong className="font-medium text-ink">{item.recipientEmail}</strong>
                    </span>
                  </div>

                  <div className="flex items-center gap-2 text-xs text-ink-muted flex-wrap">
                    <div className="inline-flex items-center gap-1.5">
                      <Clock className="w-3.5 h-3.5 text-ink-muted" />
                      <span>
                        {new Date(item.createdAt).toLocaleDateString()}{" "}
                        {new Date(item.createdAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                      </span>
                    </div>

                    <button
                      type="button"
                      disabled={loadingPreviewId === item.id}
                      onClick={() => handleOpenPreview(item)}
                      className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium border border-line hover:bg-bg transition-colors text-ink"
                    >
                      <Eye className="w-3 h-3 text-ink-muted" />
                      <span>Preview Email</span>
                    </button>

                    {item.deliveryStatus === "FAILED" && isAdmin && (
                      <button
                        type="button"
                        disabled={retryingNotificationId === item.id}
                        onClick={() => handleRetryNotification(item.id)}
                        className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium bg-teal-50 text-teal-700 hover:bg-teal-100 transition-colors disabled:opacity-50"
                      >
                        <RefreshCw className={`w-3 h-3 ${retryingNotificationId === item.id ? "animate-spin" : ""}`} />
                        <span>{retryingNotificationId === item.id ? "Retrying…" : "Retry Delivery"}</span>
                      </button>
                    )}
                  </div>
                </div>

                {item.deliveryStatus === "FAILED" && item.errorMessage && (
                  <div className="p-2.5 rounded-lg bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-start gap-2">
                    <AlertTriangle className="w-3.5 h-3.5 text-rose-600 shrink-0 mt-0.5" />
                    <div>
                      <strong className="font-semibold">Delivery Failure:</strong> {item.errorMessage}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
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

      {/* Email Preview Modal */}
      {previewNotification && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs">
          <div className="bg-surface rounded-2xl border border-line shadow-xl max-w-2xl w-full p-6 animate-in fade-in zoom-in-95 duration-150 max-h-[85vh] flex flex-col">
            <div className="flex items-start justify-between gap-3 mb-4 border-b border-line pb-3">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-teal-50 flex items-center justify-center text-teal-700 shrink-0">
                  <Mail className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="font-display font-semibold text-ink text-base">SES Email Notification Preview</h3>
                  <p className="text-xs text-ink-muted">
                    To: {previewNotification.recipientEmail} &bull; Type: {previewNotification.notificationType}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setPreviewNotification(null)}
                className="text-ink-muted hover:text-ink transition-colors p-1 rounded-lg hover:bg-bg"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="space-y-3 mb-4 text-sm overflow-y-auto pr-1 flex-1">
              <div className="p-2.5 rounded-lg bg-bg border border-line text-xs font-medium text-ink">
                <span className="text-ink-muted">Subject:</span> {previewNotification.subject}
              </div>

              <div className="border border-line rounded-xl overflow-hidden bg-white">
                <div className="p-2 bg-slate-50 border-b border-line text-[11px] text-slate-500 font-mono">
                  HTML Template Render:
                </div>
                <div
                  className="p-4 overflow-x-auto text-xs"
                  dangerouslySetInnerHTML={{ __html: previewNotification.bodyHtml }}
                />
              </div>

              {previewNotification.bodyText && (
                <div className="border border-line rounded-xl p-3 bg-slate-50">
                  <div className="text-[11px] text-slate-500 font-mono mb-1">Plain Text Alternative:</div>
                  <pre className="text-xs text-slate-700 whitespace-pre-wrap font-mono">
                    {previewNotification.bodyText}
                  </pre>
                </div>
              )}
            </div>

            <div className="flex items-center justify-end gap-2.5 pt-2 border-t border-line">
              <button
                type="button"
                onClick={() => setPreviewNotification(null)}
                className="px-4 py-2 rounded-lg border border-line text-ink text-sm font-medium hover:bg-bg transition-colors"
              >
                Close Preview
              </button>
            </div>
          </div>
        </div>
      )}

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
                  <li>Queue and dispatch a compassionate memorial email to owner <strong>{patient.owner.name}</strong> ({patient.owner.email})</li>
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
                  I confirm that <strong>{patient.name}</strong> has passed away, and I explicitly approve recording this clinical milestone in the official audit log and sending the notification.
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
                {savingStatus ? "Recording & Dispatching…" : "Confirm & Mark Deceased"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
