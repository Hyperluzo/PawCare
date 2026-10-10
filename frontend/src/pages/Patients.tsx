import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  Search,
  SlidersHorizontal,
  RotateCcw,
  Plus,
  ChevronLeft,
  ChevronRight,
  X,
  PawPrint,
  User,
  BedDouble,
  Phone,
} from "lucide-react";
import { patientsApi } from "../services/patients";
import { wardsApi } from "../services/wards";
import type { Patient, Ward } from "../types/api";
import { ClinicalStatusBadge, AdmissionStatusBadge } from "../components/StatusBadge";

const CLINICAL_STATUS_OPTIONS = [
  { value: "", label: "All Clinical Statuses" },
  { value: "UNDER_TREATMENT", label: "Under Treatment" },
  { value: "STABLE", label: "Stable" },
  { value: "CRITICAL", label: "Critical" },
  { value: "RECOVERING", label: "Recovering" },
  { value: "UNDER_OBSERVATION", label: "Under Observation" },
];

const ADMISSION_STATUS_OPTIONS = [
  { value: "", label: "All Admission Statuses" },
  { value: "ADMITTED", label: "Admitted" },
  { value: "DISCHARGED", label: "Discharged" },
  { value: "DECEASED", label: "Deceased" },
];

const SPECIES_OPTIONS = [
  { value: "", label: "All Species" },
  { value: "Canine", label: "Canine / Dog" },
  { value: "Feline", label: "Feline / Cat" },
  { value: "Avian", label: "Avian / Bird" },
  { value: "Equine", label: "Equine / Horse" },
  { value: "Bovine", label: "Bovine / Cattle" },
  { value: "Other", label: "Other" },
];

const SORT_OPTIONS = [
  { value: "createdAt:desc", label: "Latest Registered" },
  { value: "admittedAt:desc", label: "Latest Admitted" },
  { value: "admittedAt:asc", label: "Oldest Admitted" },
  { value: "name:asc", label: "Patient Name (A–Z)" },
  { value: "name:desc", label: "Patient Name (Z–A)" },
  { value: "species:asc", label: "Species (A–Z)" },
];

export default function Patients() {
  const [results, setResults] = useState<Patient[]>([]);
  const [total, setTotal] = useState(0);
  const [wards, setWards] = useState<Ward[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [search, setSearch] = useState("");
  const [species, setSpecies] = useState("");
  const [breed, setBreed] = useState("");
  const [owner, setOwner] = useState("");
  const [clinicalStatus, setClinicalStatus] = useState("");
  const [admissionStatus, setAdmissionStatus] = useState("");
  const [wardId, setWardId] = useState("");
  const [sortOption, setSortOption] = useState("createdAt:desc");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);

  // Fetch wards once for filter dropdown
  useEffect(() => {
    wardsApi.list().then(setWards).catch(() => {});
  }, []);

  // Fetch patients when filter parameters change
  useEffect(() => {
    const handle = setTimeout(() => {
      setLoading(true);
      setError(null);

      const [sortBy, sortOrder] = sortOption.split(":");

      patientsApi
        .search({
          search: search.trim() || undefined,
          species: species || undefined,
          breed: breed.trim() || undefined,
          owner: owner.trim() || undefined,
          clinicalStatus: clinicalStatus || undefined,
          admissionStatus: admissionStatus || undefined,
          wardId: wardId ? Number(wardId) : undefined,
          sortBy,
          sortOrder,
          page,
          pageSize,
        })
        .then((r) => {
          setResults(r.results);
          setTotal(r.total);
        })
        .catch(() => setError("Couldn't load patients. Please try again."))
        .finally(() => setLoading(false));
    }, 300);

    return () => clearTimeout(handle);
  }, [
    search,
    species,
    breed,
    owner,
    clinicalStatus,
    admissionStatus,
    wardId,
    sortOption,
    page,
    pageSize,
  ]);

  const activeFiltersCount = [
    search,
    species,
    breed,
    owner,
    clinicalStatus,
    admissionStatus,
    wardId,
  ].filter(Boolean).length;

  function handleResetFilters() {
    setSearch("");
    setSpecies("");
    setBreed("");
    setOwner("");
    setClinicalStatus("");
    setAdmissionStatus("");
    setWardId("");
    setSortOption("createdAt:desc");
    setPage(1);
  }

  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const startIndex = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const endIndex = Math.min(page * pageSize, total);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="font-display text-2xl font-semibold text-ink">Patients</h1>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-teal-50 text-teal-700 border border-teal-100">
              {total} total
            </span>
          </div>
          <p className="text-sm text-ink-muted mt-1">
            Search, filter, and track animal patients across hospital wards.
          </p>
        </div>

        <Link
          to="/patients/new"
          className="inline-flex items-center justify-center gap-2 rounded-lg bg-teal-700 text-white text-sm font-medium px-4 py-2.5 hover:bg-teal-800 transition-colors shadow-sm"
        >
          <Plus className="w-4 h-4" />
          Admit Patient
        </Link>
      </div>

      {/* Filter Card */}
      <div className="bg-surface border border-line rounded-2xl p-5 shadow-xs space-y-4">
        {/* Primary Filter Row */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-12 gap-3 items-center">
          {/* Search Input */}
          <div className="relative sm:col-span-2 lg:col-span-4">
            <Search className="w-4 h-4 text-ink-muted absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              placeholder="Search by name, owner, or breed…"
              className="w-full rounded-lg border border-line pl-9 pr-8 py-2 text-sm text-ink outline-none focus:border-teal-600 transition-colors"
            />
            {search && (
              <button
                type="button"
                onClick={() => {
                  setSearch("");
                  setPage(1);
                }}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-ink-muted hover:text-ink"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>

          {/* Species */}
          <div className="lg:col-span-3">
            <select
              value={species}
              onChange={(e) => {
                setSpecies(e.target.value);
                setPage(1);
              }}
              className="w-full rounded-lg border border-line px-3 py-2 text-sm text-ink outline-none focus:border-teal-600 bg-surface transition-colors"
            >
              {SPECIES_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          {/* Clinical Status */}
          <div className="lg:col-span-3">
            <select
              value={clinicalStatus}
              onChange={(e) => {
                setClinicalStatus(e.target.value);
                setPage(1);
              }}
              className="w-full rounded-lg border border-line px-3 py-2 text-sm text-ink outline-none focus:border-teal-600 bg-surface transition-colors"
            >
              {CLINICAL_STATUS_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          {/* Advanced toggle & Reset */}
          <div className="flex items-center gap-2 lg:col-span-2 justify-end">
            <button
              type="button"
              onClick={() => setShowAdvancedFilters((prev) => !prev)}
              className={`inline-flex items-center gap-1.5 px-3 py-2 text-sm font-medium rounded-lg border transition-colors ${
                showAdvancedFilters || activeFiltersCount > 0
                  ? "border-teal-600 text-teal-700 bg-teal-50"
                  : "border-line text-ink-muted hover:text-ink hover:border-ink/20"
              }`}
            >
              <SlidersHorizontal className="w-4 h-4" />
              Filters
              {activeFiltersCount > 0 && (
                <span className="w-5 h-5 rounded-full bg-teal-700 text-white text-xs flex items-center justify-center font-bold">
                  {activeFiltersCount}
                </span>
              )}
            </button>

            {activeFiltersCount > 0 && (
              <button
                type="button"
                onClick={handleResetFilters}
                title="Reset all filters"
                className="p-2 rounded-lg border border-line text-ink-muted hover:text-ink hover:bg-bg transition-colors"
              >
                <RotateCcw className="w-4 h-4" />
              </button>
            )}
          </div>
        </div>

        {/* Secondary / Advanced Filters Drawer */}
        {showAdvancedFilters && (
          <div className="pt-3 border-t border-line grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            {/* Admission Status */}
            <div>
              <label className="block text-xs font-medium text-ink-muted mb-1">
                Admission Status
              </label>
              <select
                value={admissionStatus}
                onChange={(e) => {
                  setAdmissionStatus(e.target.value);
                  setPage(1);
                }}
                className="w-full rounded-lg border border-line px-3 py-2 text-sm text-ink outline-none focus:border-teal-600 bg-surface transition-colors"
              >
                {ADMISSION_STATUS_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>

            {/* Ward */}
            <div>
              <label className="block text-xs font-medium text-ink-muted mb-1">Ward Location</label>
              <select
                value={wardId}
                onChange={(e) => {
                  setWardId(e.target.value);
                  setPage(1);
                }}
                className="w-full rounded-lg border border-line px-3 py-2 text-sm text-ink outline-none focus:border-teal-600 bg-surface transition-colors"
              >
                <option value="">All Wards</option>
                {wards.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.patientCount}/{w.capacity})
                  </option>
                ))}
              </select>
            </div>

            {/* Breed Filter */}
            <div>
              <label className="block text-xs font-medium text-ink-muted mb-1">Specific Breed</label>
              <input
                type="text"
                value={breed}
                onChange={(e) => {
                  setBreed(e.target.value);
                  setPage(1);
                }}
                placeholder="e.g. Golden Retriever…"
                className="w-full rounded-lg border border-line px-3 py-2 text-sm text-ink outline-none focus:border-teal-600 transition-colors"
              />
            </div>

            {/* Owner Filter */}
            <div>
              <label className="block text-xs font-medium text-ink-muted mb-1">Owner Name or Phone</label>
              <input
                type="text"
                value={owner}
                onChange={(e) => {
                  setOwner(e.target.value);
                  setPage(1);
                }}
                placeholder="e.g. John Doe / 555-0199…"
                className="w-full rounded-lg border border-line px-3 py-2 text-sm text-ink outline-none focus:border-teal-600 transition-colors"
              />
            </div>
          </div>
        )}

        {/* Active Filter Chips */}
        {activeFiltersCount > 0 && (
          <div className="flex flex-wrap items-center gap-2 pt-2">
            <span className="text-xs text-ink-muted">Active filters:</span>
            {search && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs bg-bg border border-line text-ink">
                Search: "{search}"
                <button
                  onClick={() => {
                    setSearch("");
                    setPage(1);
                  }}
                  className="hover:text-red-500"
                >
                  <X className="w-3 h-3" />
                </button>
              </span>
            )}
            {species && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs bg-bg border border-line text-ink">
                Species: {species}
                <button
                  onClick={() => {
                    setSpecies("");
                    setPage(1);
                  }}
                  className="hover:text-red-500"
                >
                  <X className="w-3 h-3" />
                </button>
              </span>
            )}
            {clinicalStatus && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs bg-bg border border-line text-ink">
                Status: {clinicalStatus.replaceAll("_", " ")}
                <button
                  onClick={() => {
                    setClinicalStatus("");
                    setPage(1);
                  }}
                  className="hover:text-red-500"
                >
                  <X className="w-3 h-3" />
                </button>
              </span>
            )}
            {admissionStatus && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs bg-bg border border-line text-ink">
                Admission: {admissionStatus}
                <button
                  onClick={() => {
                    setAdmissionStatus("");
                    setPage(1);
                  }}
                  className="hover:text-red-500"
                >
                  <X className="w-3 h-3" />
                </button>
              </span>
            )}
            {wardId && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs bg-bg border border-line text-ink">
                Ward: {wards.find((w) => w.id === Number(wardId))?.name || `#${wardId}`}
                <button
                  onClick={() => {
                    setWardId("");
                    setPage(1);
                  }}
                  className="hover:text-red-500"
                >
                  <X className="w-3 h-3" />
                </button>
              </span>
            )}
            {breed && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs bg-bg border border-line text-ink">
                Breed: "{breed}"
                <button
                  onClick={() => {
                    setBreed("");
                    setPage(1);
                  }}
                  className="hover:text-red-500"
                >
                  <X className="w-3 h-3" />
                </button>
              </span>
            )}
            {owner && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs bg-bg border border-line text-ink">
                Owner: "{owner}"
                <button
                  onClick={() => {
                    setOwner("");
                    setPage(1);
                  }}
                  className="hover:text-red-500"
                >
                  <X className="w-3 h-3" />
                </button>
              </span>
            )}
            <button
              onClick={handleResetFilters}
              className="text-xs text-teal-700 hover:underline font-medium ml-1"
            >
              Clear all
            </button>
          </div>
        )}
      </div>

      {/* Error alert */}
      {error && (
        <div className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-xl px-4 py-3 flex items-center justify-between">
          <span>{error}</span>
          <button
            onClick={() => setError(null)}
            className="text-xs font-semibold text-red-700 underline"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Results Controls Bar (Sorting & Count) */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-sm">
        <div className="text-ink-muted">
          {total > 0 ? (
            <span>
              Showing <strong className="text-ink">{startIndex}</strong>–
              <strong className="text-ink">{endIndex}</strong> of{" "}
              <strong className="text-ink">{total}</strong> patient{total === 1 ? "" : "s"}
            </span>
          ) : (
            <span>No patients found</span>
          )}
        </div>

        <div className="flex items-center gap-3">
          <label className="text-xs text-ink-muted">Sort by:</label>
          <select
            value={sortOption}
            onChange={(e) => {
              setSortOption(e.target.value);
              setPage(1);
            }}
            className="rounded-lg border border-line px-2.5 py-1.5 text-xs text-ink outline-none focus:border-teal-600 bg-surface"
          >
            {SORT_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>

          <label className="text-xs text-ink-muted ml-2">Per page:</label>
          <select
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value));
              setPage(1);
            }}
            className="rounded-lg border border-line px-2.5 py-1.5 text-xs text-ink outline-none focus:border-teal-600 bg-surface"
          >
            <option value={10}>10</option>
            <option value={20}>20</option>
            <option value={50}>50</option>
          </select>
        </div>
      </div>

      {/* Results Table */}
      <div className="bg-surface border border-line rounded-2xl overflow-hidden shadow-xs">
        {loading ? (
          <div className="p-12 text-center">
            <div className="inline-block w-8 h-8 border-3 border-teal-700 border-t-transparent rounded-full animate-spin mb-3"></div>
            <p className="text-sm text-ink-muted">Loading matching patient records…</p>
          </div>
        ) : results.length === 0 ? (
          <div className="p-12 text-center max-w-md mx-auto">
            <div className="w-12 h-12 bg-teal-50 text-teal-700 rounded-full flex items-center justify-center mx-auto mb-3">
              <PawPrint className="w-6 h-6" />
            </div>
            <h3 className="font-display text-base font-semibold text-ink mb-1">
              No patients match your criteria
            </h3>
            <p className="text-xs text-ink-muted mb-4">
              Try adjusting your search terms or clearing some of your filters to see more results.
            </p>
            {activeFiltersCount > 0 ? (
              <button
                onClick={handleResetFilters}
                className="inline-flex items-center gap-1.5 rounded-lg border border-line px-3 py-1.5 text-xs font-medium text-ink hover:bg-bg transition-colors"
              >
                <RotateCcw className="w-3.5 h-3.5" />
                Clear all filters
              </button>
            ) : (
              <Link
                to="/patients/new"
                className="inline-flex items-center gap-1.5 rounded-lg bg-teal-700 text-white px-3 py-1.5 text-xs font-medium hover:bg-teal-800 transition-colors"
              >
                <Plus className="w-3.5 h-3.5" />
                Admit your first patient
              </Link>
            )}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs font-medium text-ink-muted border-b border-line bg-bg/50">
                  <th className="px-5 py-3.5">Patient</th>
                  <th className="px-5 py-3.5">Species & Breed</th>
                  <th className="px-5 py-3.5">Owner</th>
                  <th className="px-5 py-3.5">Ward</th>
                  <th className="px-5 py-3.5">Clinical Status</th>
                  <th className="px-5 py-3.5">Admission</th>
                  <th className="px-5 py-3.5 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {results.map((p) => (
                  <tr
                    key={p.id}
                    className="hover:bg-bg/60 transition-colors group"
                  >
                    {/* Patient */}
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-teal-50 text-teal-700 flex items-center justify-center font-semibold text-xs shrink-0">
                          {p.name.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <Link
                            to={`/patients/${p.id}`}
                            className="font-medium text-ink hover:text-teal-700 transition-colors group-hover:underline"
                          >
                            {p.name}
                          </Link>
                          <div className="text-xs text-ink-muted">
                            {p.gender} · {p.age} {p.age === 1 ? "yr" : "yrs"} old
                          </div>
                        </div>
                      </div>
                    </td>

                    {/* Species / Breed */}
                    <td className="px-5 py-3.5 text-ink-muted">
                      <div className="font-medium text-ink text-xs">{p.species}</div>
                      <div className="text-xs text-ink-muted">{p.breed}</div>
                    </td>

                    {/* Owner */}
                    <td className="px-5 py-3.5 text-ink-muted">
                      <div className="flex items-center gap-1.5 text-ink font-medium text-xs">
                        <User className="w-3 h-3 text-ink-muted shrink-0" />
                        <span>{p.owner?.name ?? "—"}</span>
                      </div>
                      {p.owner?.phone && (
                        <div className="flex items-center gap-1 text-xs text-ink-muted mt-0.5">
                          <Phone className="w-2.5 h-2.5 shrink-0" />
                          <span>{p.owner.phone}</span>
                        </div>
                      )}
                    </td>

                    {/* Ward */}
                    <td className="px-5 py-3.5 text-ink-muted">
                      <div className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs bg-bg border border-line text-ink">
                        <BedDouble className="w-3 h-3 text-teal-700" />
                        {p.ward?.name ?? "Unassigned"}
                      </div>
                    </td>

                    {/* Clinical Status */}
                    <td className="px-5 py-3.5">
                      <ClinicalStatusBadge status={p.clinicalStatus} />
                    </td>

                    {/* Admission Status */}
                    <td className="px-5 py-3.5">
                      <AdmissionStatusBadge status={p.admissionStatus} />
                    </td>

                    {/* Action */}
                    <td className="px-5 py-3.5 text-right">
                      <Link
                        to={`/patients/${p.id}`}
                        className="inline-flex items-center text-xs font-medium text-teal-700 hover:text-teal-800 hover:underline"
                      >
                        View &rarr;
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination Bar */}
        {total > 0 && totalPages > 1 && (
          <div className="border-t border-line px-5 py-3.5 flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-surface">
            <span className="text-xs text-ink-muted">
              Page <strong className="text-ink">{page}</strong> of{" "}
              <strong className="text-ink">{totalPages}</strong>
            </span>

            <div className="flex items-center gap-1">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium rounded-lg border border-line text-ink disabled:opacity-40 disabled:cursor-not-allowed hover:bg-bg transition-colors"
              >
                <ChevronLeft className="w-3.5 h-3.5" />
                Previous
              </button>

              {/* Page numbers */}
              <div className="flex items-center gap-1 mx-1">
                {Array.from({ length: totalPages }, (_, i) => i + 1)
                  .filter((pNum) => {
                    // Show first, last, and window around current
                    return (
                      pNum === 1 ||
                      pNum === totalPages ||
                      Math.abs(pNum - page) <= 1
                    );
                  })
                  .map((pNum, idx, arr) => {
                    const prev = arr[idx - 1];
                    const showEllipsis = prev && pNum - prev > 1;
                    return (
                      <span key={pNum} className="flex items-center">
                        {showEllipsis && (
                          <span className="px-1 text-xs text-ink-muted">…</span>
                        )}
                        <button
                          type="button"
                          onClick={() => setPage(pNum)}
                          className={`w-7 h-7 text-xs font-medium rounded-md transition-colors ${
                            page === pNum
                              ? "bg-teal-700 text-white font-semibold"
                              : "text-ink hover:bg-bg"
                          }`}
                        >
                          {pNum}
                        </button>
                      </span>
                    );
                  })}
              </div>

              <button
                type="button"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium rounded-lg border border-line text-ink disabled:opacity-40 disabled:cursor-not-allowed hover:bg-bg transition-colors"
              >
                Next
                <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
