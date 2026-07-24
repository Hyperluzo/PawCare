import { FormEvent, useEffect, useState } from "react";
import { Plus } from "lucide-react";
import { wardsApi } from "../services/wards";
import type { Ward } from "../types/api";

export default function Wards() {
  const [wards, setWards] = useState<Ward[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: "", description: "", capacity: 10 });
  const [error, setError] = useState<string | null>(null);

  function load() {
    setLoading(true);
    wardsApi
      .list()
      .then(setWards)
      .catch(() => setError("Couldn't load wards."))
      .finally(() => setLoading(false));
  }

  useEffect(load, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await wardsApi.create(form);
      setForm({ name: "", description: "", capacity: 10 });
      setShowForm(false);
      load();
    } catch {
      setError("Couldn't create ward. Check the details and try again.");
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <h1 className="font-display text-2xl font-semibold text-ink">Wards</h1>
        <button
          onClick={() => setShowForm((s) => !s)}
          className="inline-flex items-center gap-1.5 rounded-lg bg-teal-700 text-white text-sm font-medium px-3.5 py-2 hover:bg-teal-800 transition-colors"
        >
          <Plus className="w-4 h-4" />
          New Ward
        </button>
      </div>
      <p className="text-sm text-ink-muted mb-6">Manage wards and see occupancy at a glance.</p>

      {error && <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2 mb-4">{error}</p>}

      {showForm && (
        <form onSubmit={handleCreate} className="bg-surface border border-line rounded-2xl p-5 mb-6 grid grid-cols-1 sm:grid-cols-4 gap-3 items-end">
          <div className="sm:col-span-2">
            <label className="block text-xs font-medium text-ink-muted mb-1">Name</label>
            <input required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })}
              className="w-full rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600" />
          </div>
          <div>
            <label className="block text-xs font-medium text-ink-muted mb-1">Capacity</label>
            <input required type="number" min={1} value={form.capacity}
              onChange={(e) => setForm({ ...form, capacity: Number(e.target.value) })}
              className="w-full rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600" />
          </div>
          <div>
            <button type="submit" className="w-full rounded-lg bg-teal-700 text-white text-sm font-medium py-2 hover:bg-teal-800 transition-colors">
              Create
            </button>
          </div>
          <div className="sm:col-span-4">
            <label className="block text-xs font-medium text-ink-muted mb-1">Description</label>
            <input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })}
              className="w-full rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600" />
          </div>
        </form>
      )}

      {loading ? (
        <p className="text-sm text-ink-muted">Loading wards…</p>
      ) : wards.length === 0 ? (
        <div className="bg-surface border border-line rounded-2xl p-8 text-center text-sm text-ink-muted">
          No wards yet. Create one to start admitting patients.
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {wards.map((w) => (
            <div key={w.id} className="bg-surface border border-line rounded-2xl p-5">
              <div className="font-display font-semibold text-ink">{w.name}</div>
              <p className="text-sm text-ink-muted mt-1 mb-3">{w.description || "No description."}</p>
              <div className="text-xs text-ink-muted">
                <span className="font-medium text-ink">{w.patientCount}</span> / {w.capacity} occupied
              </div>
              <div className="mt-2 h-1.5 rounded-full bg-line overflow-hidden">
                <div
                  className="h-full bg-teal-600"
                  style={{ width: `${Math.min(100, (w.patientCount / w.capacity) * 100)}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
