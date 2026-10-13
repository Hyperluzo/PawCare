const CLINICAL_STYLES: Record<string, string> = {
  UNDER_TREATMENT: "bg-amber-100 text-amber-700",
  STABLE: "bg-green-100 text-green-700",
  CRITICAL: "bg-red-100 text-red-700",
  RECOVERING: "bg-teal-50 text-teal-700",
  UNDER_OBSERVATION: "bg-amber-100 text-amber-700",
};

const ADMISSION_STYLES: Record<string, string> = {
  ADMITTED: "bg-teal-50 text-teal-700",
  DISCHARGED: "bg-green-100 text-green-700",
  DECEASED: "bg-[#EFEFEF] text-[#5B6B67]",
};

function label(status: string) {
  return status
    .toLowerCase()
    .split("_")
    .map((w) => w[0].toUpperCase() + w.slice(1))
    .join(" ");
}

export function ClinicalStatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium ${CLINICAL_STYLES[status] ?? "bg-line text-ink-muted"}`}>
      {label(status)}
    </span>
  );
}

export function AdmissionStatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium ${ADMISSION_STYLES[status] ?? "bg-line text-ink-muted"}`}>
      {label(status)}
    </span>
  );
}

export function StatusBadge({ status }: { status: string }) {
  const style = CLINICAL_STYLES[status] ?? ADMISSION_STYLES[status] ?? "bg-line text-ink-muted";
  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${style}`}>
      {label(status)}
    </span>
  );
}

export function NotificationStatusBadge({ status }: { status: string }) {
  const norm = status.toUpperCase();
  const styles: Record<string, string> = {
    SENT: "bg-emerald-50 text-emerald-700 border border-emerald-200",
    PENDING: "bg-amber-50 text-amber-700 border border-amber-200",
    FAILED: "bg-rose-50 text-rose-700 border border-rose-200",
  };
  const style = styles[norm] ?? "bg-slate-50 text-slate-700 border border-slate-200";
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold uppercase tracking-wider ${style}`}>
      <span className={`w-1.5 h-1.5 rounded-full ${norm === "SENT" ? "bg-emerald-500" : norm === "FAILED" ? "bg-rose-500" : "bg-amber-500"}`} />
      {norm}
    </span>
  );
}

export { label as formatStatusLabel };

