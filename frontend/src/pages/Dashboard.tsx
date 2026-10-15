import { useEffect, useState, useCallback } from "react";
import { Link } from "react-router-dom";
import {
  Activity,
  HeartPulse,
  Stethoscope,
  RefreshCw,
  Plus,
  Search,
  BedDouble,
  AlertTriangle,
  CheckCircle2,
  Clock,
  ArrowUpRight,
  ShieldCheck,
  Eye,
} from "lucide-react";
import { dashboardApi } from "../services/dashboard";
import type { DashboardStats } from "../types/api";
import { ClinicalStatusBadge, AdmissionStatusBadge } from "../components/StatusBadge";

export default function Dashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const fetchStats = useCallback(async (isManualRefresh = false) => {
    if (isManualRefresh) {
      setRefreshing(true);
    }
    setError(null);
    try {
      const data = await dashboardApi.stats();
      setStats(data);
      setLastUpdated(new Date());
    } catch {
      setError("Couldn't load dashboard stats. Please check backend connection.");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  // Optional periodic background refresh (every 30 seconds if enabled)
  useEffect(() => {
    if (!autoRefresh) return;
    const interval = setInterval(() => {
      fetchStats();
    }, 30000);
    return () => clearInterval(interval);
  }, [autoRefresh, fetchStats]);

  const cards = [
    {
      id: "stat-total-patients",
      key: "totalPatients" as const,
      label: "Total Patients",
      subtitle: "Lifetime hospital registrations",
      icon: Stethoscope,
      iconBg: "bg-teal-50 text-teal-700 border-teal-200",
      accentBorder: "hover:border-teal-400",
      pill: "Registry",
      pillClass: "bg-teal-50 text-teal-800 border-teal-200",
    },
    {
      id: "stat-under-treatment",
      key: "underTreatment" as const,
      label: "Under Treatment",
      subtitle: "Active veterinary care",
      icon: Activity,
      iconBg: "bg-amber-50 text-amber-600 border-amber-200",
      accentBorder: "hover:border-amber-400",
      pill: "Active Care",
      pillClass: "bg-amber-50 text-amber-800 border-amber-200",
    },
    {
      id: "stat-treated-today",
      key: "treatedToday" as const,
      label: "Treated Today",
      subtitle: "Discharged recovered today",
      icon: CheckCircle2,
      iconBg: "bg-emerald-50 text-emerald-600 border-emerald-200",
      accentBorder: "hover:border-emerald-400",
      pill: "Discharged",
      pillClass: "bg-emerald-50 text-emerald-800 border-emerald-200",
    },
    {
      id: "stat-deceased-mtd",
      key: "deceasedMtd" as const,
      label: "Deceased (MTD)",
      subtitle: "Month-to-date audit count",
      icon: HeartPulse,
      iconBg: "bg-rose-50 text-rose-600 border-rose-200",
      accentBorder: "hover:border-rose-400",
      pill: "Audited",
      pillClass: "bg-rose-50 text-rose-800 border-rose-200",
    },
  ];

  return (
    <div className="space-y-8 animate-fadeIn">
      {/* Header bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-2 border-b border-line">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="font-display text-2xl md:text-3xl font-bold text-ink tracking-tight">
              Hospital Dashboard
            </h1>
            <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
              <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
              Live Feed
            </span>
          </div>
          <p className="text-sm text-ink-muted mt-1">
            Real-time telemetry, clinical census, and ward occupancy overview.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          {/* Last updated indicator */}
          {lastUpdated && (
            <span className="text-xs text-ink-muted flex items-center gap-1 mr-1">
              <Clock className="w-3.5 h-3.5 text-ink-muted/70" />
              {lastUpdated.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" })}
            </span>
          )}

          {/* Auto refresh toggle */}
          <button
            type="button"
            onClick={() => setAutoRefresh((prev) => !prev)}
            className={`text-xs px-2.5 py-1.5 rounded-lg border font-medium transition-colors ${
              autoRefresh
                ? "bg-teal-50 text-teal-700 border-teal-200 hover:bg-teal-100"
                : "bg-surface text-ink-muted border-line hover:bg-bg"
            }`}
            title="Toggle 30-second live auto-refresh"
          >
            {autoRefresh ? "Auto (30s)" : "Manual"}
          </button>

          {/* Refresh button */}
          <button
            id="refresh-dashboard-btn"
            type="button"
            onClick={() => fetchStats(true)}
            disabled={refreshing}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg border border-line bg-surface hover:bg-bg text-ink transition-colors disabled:opacity-50"
            title="Refresh statistics now"
          >
            <RefreshCw className={`w-3.5 h-3.5 text-teal-700 ${refreshing ? "animate-spin" : ""}`} />
            Refresh
          </button>

          {/* Quick Admit */}
          <Link
            to="/patients/new"
            className="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold rounded-lg bg-teal-700 hover:bg-teal-800 text-white shadow-sm transition-colors"
          >
            <Plus className="w-3.5 h-3.5" />
            Admit Patient
          </Link>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-xl p-4 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-red-500 shrink-0" />
            <span>{error}</span>
          </div>
          <button
            onClick={() => fetchStats(true)}
            className="text-xs font-semibold underline hover:text-red-900"
          >
            Try Again
          </button>
        </div>
      )}

      {/* Primary KPI Grid (Core Day 13 requirements) */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        {cards.map(({ id, key, label, subtitle, icon: Icon, iconBg, accentBorder, pill, pillClass }) => (
          <div
            id={id}
            key={key}
            className={`bg-surface rounded-2xl border border-line p-5 shadow-sm transition-all duration-200 hover:shadow-md ${accentBorder} relative overflow-hidden`}
          >
            <div className="flex items-start justify-between">
              <div className={`w-10 h-10 rounded-xl border flex items-center justify-center ${iconBg}`}>
                <Icon className="w-5 h-5" />
              </div>
              <span className={`text-[11px] font-semibold px-2 py-0.5 rounded-full border ${pillClass}`}>
                {pill}
              </span>
            </div>

            <div className="mt-4">
              <div className="font-display text-3xl font-bold text-ink tracking-tight">
                {loading && !stats ? (
                  <span className="inline-block w-12 h-8 bg-line/60 rounded animate-pulse" />
                ) : stats ? (
                  stats[key]
                ) : (
                  "—"
                )}
              </div>
              <div className="text-sm font-semibold text-ink mt-0.5">{label}</div>
              <p className="text-xs text-ink-muted mt-1 line-clamp-1">{subtitle}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Clinical Census Breakdown */}
      {stats && (
        <div className="bg-surface rounded-2xl border border-line p-6 shadow-sm">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-4">
            <div>
              <h2 className="font-display text-lg font-semibold text-ink">Clinical Census Distribution</h2>
              <p className="text-xs text-ink-muted">
                Active in-patient clinical status breakdown ({stats.admittedPatients ?? 0} currently admitted).
              </p>
            </div>
            <Link
              to="/patients"
              className="inline-flex items-center gap-1 text-xs font-medium text-teal-700 hover:text-teal-800"
            >
              <Search className="w-3.5 h-3.5" />
              Filter All Patients
            </Link>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
            <Link
              to="/patients?clinicalStatus=CRITICAL"
              className="p-3 rounded-xl border border-rose-200 bg-rose-50/60 hover:bg-rose-50 transition-colors group"
            >
              <div className="flex items-center justify-between text-xs font-semibold text-rose-800 mb-1">
                <span>Critical</span>
                <AlertTriangle className="w-3.5 h-3.5 text-rose-600 group-hover:scale-110 transition-transform" />
              </div>
              <div className="font-display text-2xl font-bold text-rose-900">
                {stats.criticalPatients ?? 0}
              </div>
              <div className="text-[11px] text-rose-700/80">Requires immediate care</div>
            </Link>

            <Link
              to="/patients?clinicalStatus=UNDER_TREATMENT"
              className="p-3 rounded-xl border border-amber-200 bg-amber-50/60 hover:bg-amber-50 transition-colors group"
            >
              <div className="flex items-center justify-between text-xs font-semibold text-amber-800 mb-1">
                <span>Under Treatment</span>
                <Activity className="w-3.5 h-3.5 text-amber-600 group-hover:scale-110 transition-transform" />
              </div>
              <div className="font-display text-2xl font-bold text-amber-900">
                {stats.underTreatment}
              </div>
              <div className="text-[11px] text-amber-700/80">Scheduled therapy & meds</div>
            </Link>

            <Link
              to="/patients?clinicalStatus=UNDER_OBSERVATION"
              className="p-3 rounded-xl border border-blue-200 bg-blue-50/60 hover:bg-blue-50 transition-colors group"
            >
              <div className="flex items-center justify-between text-xs font-semibold text-blue-800 mb-1">
                <span>Under Observation</span>
                <Eye className="w-3.5 h-3.5 text-blue-600 group-hover:scale-110 transition-transform" />
              </div>
              <div className="font-display text-2xl font-bold text-blue-900">
                {stats.underObservationPatients ?? 0}
              </div>
              <div className="text-[11px] text-blue-700/80">Monitoring vitals</div>
            </Link>

            <Link
              to="/patients?clinicalStatus=RECOVERING"
              className="p-3 rounded-xl border border-teal-200 bg-teal-50/60 hover:bg-teal-50 transition-colors group"
            >
              <div className="flex items-center justify-between text-xs font-semibold text-teal-800 mb-1">
                <span>Recovering</span>
                <ShieldCheck className="w-3.5 h-3.5 text-teal-600 group-hover:scale-110 transition-transform" />
              </div>
              <div className="font-display text-2xl font-bold text-teal-900">
                {stats.recoveringPatients ?? 0}
              </div>
              <div className="text-[11px] text-teal-700/80">Post-critical healing</div>
            </Link>

            <Link
              to="/patients?clinicalStatus=STABLE"
              className="p-3 rounded-xl border border-emerald-200 bg-emerald-50/60 hover:bg-emerald-50 transition-colors group"
            >
              <div className="flex items-center justify-between text-xs font-semibold text-emerald-800 mb-1">
                <span>Stable</span>
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 group-hover:scale-110 transition-transform" />
              </div>
              <div className="font-display text-2xl font-bold text-emerald-900">
                {stats.stablePatients ?? 0}
              </div>
              <div className="text-[11px] text-emerald-700/80">Pre-discharge evaluation</div>
            </Link>
          </div>
        </div>
      )}

      {/* Ward Occupancy Overview & Recent Admissions */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Ward Occupancy */}
        <div className="bg-surface rounded-2xl border border-line p-6 shadow-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-lg bg-teal-50 text-teal-700 flex items-center justify-center">
                  <BedDouble className="w-4 h-4" />
                </div>
                <div>
                  <h2 className="font-display text-base font-semibold text-ink">Ward Occupancy</h2>
                  <p className="text-xs text-ink-muted">
                    {stats?.occupiedBeds ?? 0} of {stats?.totalCapacity ?? 0} beds occupied ({stats?.occupancyRate ?? 0}%)
                  </p>
                </div>
              </div>
              <Link
                to="/wards"
                className="text-xs font-medium text-teal-700 hover:text-teal-800 inline-flex items-center gap-0.5"
              >
                Manage
                <ArrowUpRight className="w-3 h-3" />
              </Link>
            </div>

            {/* Overall progress bar */}
            <div className="w-full bg-line rounded-full h-2.5 overflow-hidden mb-5">
              <div
                className="bg-teal-600 h-2.5 rounded-full transition-all duration-500"
                style={{ width: `${Math.min(100, stats?.occupancyRate ?? 0)}%` }}
              />
            </div>

            {/* Ward list */}
            <div className="space-y-3">
              {stats?.wardSummaries && stats.wardSummaries.length > 0 ? (
                stats.wardSummaries.map((w) => (
                  <div key={w.wardId} className="p-3 rounded-xl border border-line bg-bg/50">
                    <div className="flex items-center justify-between text-xs mb-1.5">
                      <span className="font-semibold text-ink">{w.name}</span>
                      <span className="text-ink-muted font-medium">
                        {w.currentOccupancy} / {w.capacity} beds ({w.occupancyPercent}%)
                      </span>
                    </div>
                    <div className="w-full bg-line rounded-full h-1.5 overflow-hidden">
                      <div
                        className={`h-1.5 rounded-full ${
                          w.occupancyPercent >= 90
                            ? "bg-rose-500"
                            : w.occupancyPercent >= 70
                            ? "bg-amber-500"
                            : "bg-teal-600"
                        }`}
                        style={{ width: `${Math.min(100, w.occupancyPercent)}%` }}
                      />
                    </div>
                    <div className="text-[10px] text-ink-muted mt-1 flex justify-between">
                      <span>Available: {w.availableBeds}</span>
                      <span>{w.availableBeds === 0 ? "Full" : "Space open"}</span>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-6 text-xs text-ink-muted">
                  No wards registered yet.
                </div>
              )}
            </div>
          </div>

          <Link
            to="/wards"
            className="mt-4 text-center py-2 px-3 rounded-lg border border-line text-xs font-medium text-ink hover:bg-bg transition-colors block"
          >
            View All Ward Capacities
          </Link>
        </div>

        {/* Recent Admissions */}
        <div className="lg:col-span-2 bg-surface rounded-2xl border border-line p-6 shadow-sm flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4">
              <div>
                <h2 className="font-display text-base font-semibold text-ink">Recent Admissions</h2>
                <p className="text-xs text-ink-muted">Latest patients admitted for care.</p>
              </div>
              <Link
                to="/patients"
                className="text-xs font-medium text-teal-700 hover:text-teal-800 inline-flex items-center gap-0.5"
              >
                View all patients
                <ArrowUpRight className="w-3 h-3" />
              </Link>
            </div>

            {stats?.recentAdmissions && stats.recentAdmissions.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead>
                    <tr className="border-b border-line text-ink-muted font-medium">
                      <th className="pb-2.5 font-medium">Patient</th>
                      <th className="pb-2.5 font-medium">Ward</th>
                      <th className="pb-2.5 font-medium">Clinical Status</th>
                      <th className="pb-2.5 font-medium">Owner</th>
                      <th className="pb-2.5 font-medium text-right">Action</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-line/60">
                    {stats.recentAdmissions.map((p) => (
                      <tr key={p.id} className="hover:bg-bg/60 transition-colors">
                        <td className="py-3">
                          <Link
                            to={`/patients/${p.id}`}
                            className="font-semibold text-ink hover:text-teal-700 block"
                          >
                            {p.name}
                          </Link>
                          <span className="text-[11px] text-ink-muted">
                            {p.species} • {p.breed}
                          </span>
                        </td>
                        <td className="py-3 text-ink-muted font-medium">
                          {p.wardName}
                        </td>
                        <td className="py-3">
                          <div className="flex flex-col gap-1 items-start">
                            <ClinicalStatusBadge status={p.clinicalStatus} />
                            <AdmissionStatusBadge status={p.admissionStatus} />
                          </div>
                        </td>
                        <td className="py-3 text-ink-muted">
                          {p.ownerName}
                        </td>
                        <td className="py-3 text-right">
                          <Link
                            to={`/patients/${p.id}`}
                            className="inline-flex items-center px-2 py-1 rounded-md text-[11px] font-medium text-teal-700 hover:bg-teal-50 border border-teal-200 transition-colors"
                          >
                            Details
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="text-center py-10">
                <p className="text-xs text-ink-muted">No admitted patients found.</p>
                <Link
                  to="/patients/new"
                  className="mt-3 inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-teal-700 text-white"
                >
                  <Plus className="w-3.5 h-3.5" />
                  Admit First Patient
                </Link>
              </div>
            )}
          </div>

          <div className="pt-4 border-t border-line mt-4 flex items-center justify-between text-xs text-ink-muted">
            <span>Hospital operations active</span>
            <Link to="/patients" className="text-teal-700 hover:underline">
              Search & Filter all records →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
