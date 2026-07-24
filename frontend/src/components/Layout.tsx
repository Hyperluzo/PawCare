import { NavLink, Outlet } from "react-router-dom";
import { PawPrint, LayoutDashboard, BedDouble, PlusCircle, Stethoscope, Users, LogOut } from "lucide-react";
import { useAuth } from "../context/AuthContext";

const navItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/wards", label: "Wards", icon: BedDouble },
  { to: "/patients", label: "Patients", icon: Stethoscope },
  { to: "/patients/new", label: "Admit Patient", icon: PlusCircle },
];

export default function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-screen flex bg-bg">
      <aside className="w-64 shrink-0 bg-teal-800 text-white flex flex-col">
        <div className="flex items-center gap-2 px-6 py-6">
          <PawPrint className="w-6 h-6 text-amber-500" strokeWidth={2.5} />
          <span className="font-display text-lg font-semibold tracking-tight">PawCare</span>
        </div>

        <nav className="flex-1 px-3 space-y-1">
          {navItems.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
                  isActive ? "bg-white/10 text-white" : "text-white/70 hover:bg-white/5 hover:text-white"
                }`
              }
            >
              <Icon className="w-4 h-4" />
              {label}
            </NavLink>
          ))}

          {user?.role === "ADMIN" && (
            <NavLink
              to="/admin/users"
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
                  isActive ? "bg-white/10 text-white" : "text-white/70 hover:bg-white/5 hover:text-white"
                }`
              }
            >
              <Users className="w-4 h-4" />
              Staff Management
            </NavLink>
          )}
        </nav>

        <div className="px-3 pb-5 pt-3 border-t border-white/10">
          <div className="px-3 pb-2 text-xs text-white/50 truncate">{user?.email}</div>
          <button
            onClick={logout}
            className="w-full flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-white/70 hover:bg-white/5 hover:text-white transition-colors"
          >
            <LogOut className="w-4 h-4" />
            Log out
          </button>
        </div>
      </aside>

      <main className="flex-1 min-w-0">
        <div className="max-w-6xl mx-auto px-8 py-8">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
