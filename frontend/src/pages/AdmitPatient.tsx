import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { wardsApi } from "../services/wards";
import { patientsApi } from "../services/patients";
import type { Ward } from "../types/api";
import { ApiError } from "../services/api";

export default function AdmitPatient() {
  const navigate = useNavigate();
  const [wards, setWards] = useState<Ward[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [patient, setPatient] = useState({ patientName: "", species: "", breed: "", age: 1, gender: "Male", wardId: 0 });
  const [owner, setOwner] = useState({ name: "", email: "", phone: "", address: "" });

  useEffect(() => {
    wardsApi.list().then((w) => {
      setWards(w);
      if (w.length) setPatient((p) => ({ ...p, wardId: w[0].id }));
    });
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const created = await patientsApi.admit({ ...patient, newOwner: owner });
      navigate(`/patients/${created.id}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't admit patient. Check the details and try again.");
    } finally {
      setSubmitting(false);
    }
  }

  const inputClass = "w-full rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600";
  const labelClass = "block text-xs font-medium text-ink-muted mb-1";

  return (
    <div className="max-w-2xl">
      <h1 className="font-display text-2xl font-semibold text-ink mb-1">Admit Patient</h1>
      <p className="text-sm text-ink-muted mb-6">Register a new animal and its owner.</p>

      {error && <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2 mb-4">{error}</p>}

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="bg-surface border border-line rounded-2xl p-5 space-y-4">
          <h2 className="font-display font-semibold text-ink text-sm">Patient details</h2>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={labelClass}>Name</label>
              <input required className={inputClass} value={patient.patientName}
                onChange={(e) => setPatient({ ...patient, patientName: e.target.value })} />
            </div>
            <div>
              <label className={labelClass}>Species</label>
              <input required className={inputClass} value={patient.species}
                onChange={(e) => setPatient({ ...patient, species: e.target.value })} />
            </div>
            <div>
              <label className={labelClass}>Breed</label>
              <input required className={inputClass} value={patient.breed}
                onChange={(e) => setPatient({ ...patient, breed: e.target.value })} />
            </div>
            <div>
              <label className={labelClass}>Age</label>
              <input required type="number" min={0} className={inputClass} value={patient.age}
                onChange={(e) => setPatient({ ...patient, age: Number(e.target.value) })} />
            </div>
            <div>
              <label className={labelClass}>Gender</label>
              <select className={inputClass} value={patient.gender}
                onChange={(e) => setPatient({ ...patient, gender: e.target.value })}>
                <option>Male</option>
                <option>Female</option>
              </select>
            </div>
            <div>
              <label className={labelClass}>Ward</label>
              <select required className={inputClass} value={patient.wardId}
                onChange={(e) => setPatient({ ...patient, wardId: Number(e.target.value) })}>
                {wards.map((w) => (
                  <option key={w.id} value={w.id}>{w.name}</option>
                ))}
              </select>
            </div>
          </div>
        </div>

        <div className="bg-surface border border-line rounded-2xl p-5 space-y-4">
          <h2 className="font-display font-semibold text-ink text-sm">Owner details</h2>
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2">
              <label className={labelClass}>Full name</label>
              <input required className={inputClass} value={owner.name}
                onChange={(e) => setOwner({ ...owner, name: e.target.value })} />
            </div>
            <div>
              <label className={labelClass}>Email</label>
              <input required type="email" className={inputClass} value={owner.email}
                onChange={(e) => setOwner({ ...owner, email: e.target.value })} />
            </div>
            <div>
              <label className={labelClass}>Phone</label>
              <input required className={inputClass} value={owner.phone}
                onChange={(e) => setOwner({ ...owner, phone: e.target.value })} />
            </div>
            <div className="col-span-2">
              <label className={labelClass}>Address</label>
              <input className={inputClass} value={owner.address}
                onChange={(e) => setOwner({ ...owner, address: e.target.value })} />
            </div>
          </div>
        </div>

        <button type="submit" disabled={submitting}
          className="rounded-lg bg-teal-700 text-white text-sm font-medium px-5 py-2.5 hover:bg-teal-800 transition-colors disabled:opacity-60">
          {submitting ? "Admitting…" : "Admit patient"}
        </button>
      </form>
    </div>
  );
}
