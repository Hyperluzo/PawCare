import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Search } from "lucide-react";
import { patientsApi } from "../services/patients";
import type { Patient } from "../types/api";
import { ClinicalStatusBadge } from "../components/StatusBadge";

const STATUS_OPTIONS = ["", "UNDER_TREATMENT", "STABLE", "CRITICAL", "RECOVERING", "UNDER_OBSERVATION"];

export default function Patients() {
  const [results, setResults] = useState<Patient[]>([]);
  const [total, setTotal] = useState(0);
  const [name, setName] = useState("");
  const [status, setStatus] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handle = setTimeout(() => {
      setLoading(true);
      patientsApi
        .search({ name, status })
        .then((r) => {
          setResults(r.results);
          setTotal(r.total);
        })
        .catch(() => setError("Couldn't load patients."))
        .finally(() => setLoading(false));
    }, 300);
    return () => clearTimeout(handle);
  }, [name, status]);

  return (
    <div>
      <h1 className="font-display text-2xl font-semibold text-ink mb-1">Patients</h1>
      <p className="text-sm text-ink-muted mb-6">{total} patient{total === 1 ? "" : "s"} on record.</p>

      <div className="flex flex-col sm:flex-row gap-3 mb-5">
        <div className="relative flex-1">
          <Search className="w-4 h-4 text-ink-muted absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Search by patient name…"
            className="w-full rounded-lg border border-line pl-9 pr-3 py-2 text-sm outline-none focus:border-teal-600"
          />
        </div>
        <select
          value={status}
          onChange={(e) => setStatus(e.target.value)}
          className="rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600"
        >
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s ? s.replaceAll("_", " ") : "All statuses"}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2 mb-4">{error}</p>}

      <div className="bg-surface border border-line rounded-2xl overflow-hidden">
        {loading ? (
          <p className="text-sm text-ink-muted p-6">Loading…</p>
        ) : results.length === 0 ? (
          <p className="text-sm text-ink-muted p-6">No patients match your search.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-ink-muted border-b border-line">
                <th className="px-5 py-3 font-medium">Name</th>
                <th className="px-5 py-3 font-medium">Species / Breed</th>
                <th className="px-5 py-3 font-medium">Ward</th>
                <th className="px-5 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {results.map((p) => (
                <tr key={p.id} className="border-b border-line last:border-0 hover:bg-bg transition-colors">
                  <td className="px-5 py-3">
                    <Link to={`/patients/${p.id}`} className="font-medium text-ink hover:text-teal-700">
                      {p.name}
                    </Link>
                  </td>
                  <td className="px-5 py-3 text-ink-muted">{p.species} · {p.breed}</td>
                  <td className="px-5 py-3 text-ink-muted">{p.ward?.name}</td>
                  <td className="px-5 py-3">
                    <ClinicalStatusBadge status={p.clinicalStatus} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
