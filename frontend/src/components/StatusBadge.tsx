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
