import { FormEvent, useEffect, useState } from "react";
import { UserPlus } from "lucide-react";
import { adminApi } from "../services/admin";
import type { StaffUser } from "../types/api";
import { ApiError } from "../services/api";

export default function AdminUsers() {
  const [users, setUsers] = useState<StaffUser[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: "", email: "", password: "", accountType: "STAFF" as "ADMIN" | "STAFF" });
  const [error, setError] = useState<string | null>(null);

  function load() {
    adminApi.listUsers().then(setUsers).catch(() => setError("Couldn't load staff."));
  }

  useEffect(load, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await adminApi.createUser(form);
      setForm({ name: "", email: "", password: "", accountType: "STAFF" });
      setShowForm(false);
      load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Couldn't create staff account.");
    }
  }

  async function toggleActive(u: StaffUser) {
    try {
      await adminApi.setActive(u.id, !u.isActive);
      load();
    } catch {
      setError("Couldn't update account status.");
    }
  }

  const inputClass = "w-full rounded-lg border border-line px-3 py-2 text-sm outline-none focus:border-teal-600";
  const labelClass = "block text-xs font-medium text-ink-muted mb-1";

  return (
    <div>
      <div className="flex items-center justify-between mb-1">
        <h1 className="font-display text-2xl font-semibold text-ink">Staff Management</h1>
        <button onClick={() => setShowForm((s) => !s)}
          className="inline-flex items-center gap-1.5 rounded-lg bg-teal-700 text-white text-sm font-medium px-3.5 py-2 hover:bg-teal-800 transition-colors">
          <UserPlus className="w-4 h-4" />
          New Staff
        </button>
      </div>
      <p className="text-sm text-ink-muted mb-6">Create and manage Admin and Staff accounts.</p>

      {error && <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2 mb-4">{error}</p>}

      {showForm && (
        <form onSubmit={handleCreate} className="bg-surface border border-line rounded-2xl p-5 mb-6 grid grid-cols-1 sm:grid-cols-5 gap-3 items-end">
          <div>
            <label className={labelClass}>Name</label>
            <input required className={inputClass} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </div>
          <div>
            <label className={labelClass}>Email</label>
            <input required type="email" className={inputClass} value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          </div>
          <div>
            <label className={labelClass}>Temp password</label>
            <input required className={inputClass} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
          </div>
          <div>
            <label className={labelClass}>Role</label>
            <select className={inputClass} value={form.accountType} onChange={(e) => setForm({ ...form, accountType: e.target.value as "ADMIN" | "STAFF" })}>
              <option value="STAFF">Staff</option>
              <option value="ADMIN">Admin</option>
            </select>
          </div>
          <button type="submit" className="rounded-lg bg-teal-700 text-white text-sm font-medium py-2 hover:bg-teal-800 transition-colors">
            Create
          </button>
        </form>
      )}

      <div className="bg-surface border border-line rounded-2xl overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-xs text-ink-muted border-b border-line">
              <th className="px-5 py-3 font-medium">Name</th>
              <th className="px-5 py-3 font-medium">Email</th>
              <th className="px-5 py-3 font-medium">Role</th>
              <th className="px-5 py-3 font-medium">Status</th>
              <th className="px-5 py-3 font-medium"></th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className="border-b border-line last:border-0">
                <td className="px-5 py-3 text-ink font-medium">{u.name}</td>
                <td className="px-5 py-3 text-ink-muted">{u.email}</td>
                <td className="px-5 py-3 text-ink-muted">{u.accountType}</td>
                <td className="px-5 py-3">
                  <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${u.isActive ? "bg-green-100 text-green-700" : "bg-line text-ink-muted"}`}>
                    {u.isActive ? "Active" : "Inactive"}
                  </span>
                </td>
                <td className="px-5 py-3 text-right">
                  <button onClick={() => toggleActive(u)} className="text-xs font-medium text-teal-700 hover:text-teal-800">
                    {u.isActive ? "Deactivate" : "Activate"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
