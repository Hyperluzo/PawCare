import { useEffect, useState } from "react";
import { Activity, HeartPulse, Stethoscope, Users } from "lucide-react";
import { dashboardApi } from "../services/dashboard";
import type { DashboardStats } from "../types/api";

const cards = [
  { key: "totalPatients", label: "Total Patients", icon: Stethoscope },
  { key: "underTreatment", label: "Under Treatment", icon: Activity },
  { key: "treatedToday", label: "Treated Today", icon: Users },
  { key: "deceasedMtd", label: "Deceased (MTD)", icon: HeartPulse },
] as const;

export default function Dashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    dashboardApi
      .stats()
      .then(setStats)
      .catch(() => setError("Couldn't load dashboard stats."));
  }, []);

  return (
    <div>
      <h1 className="font-display text-2xl font-semibold text-ink mb-1">Dashboard</h1>
      <p className="text-sm text-ink-muted mb-8">Live overview of hospital activity.</p>

      {error && <p className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2 mb-6">{error}</p>}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {cards.map(({ key, label, icon: Icon }) => (
          <div key={key} className="bg-surface rounded-2xl border border-line p-5">
            <Icon className="w-5 h-5 text-teal-700 mb-3" />
            <div className="font-display text-2xl font-semibold text-ink">
              {stats ? stats[key] : "—"}
            </div>
            <div className="text-sm text-ink-muted mt-0.5">{label}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
