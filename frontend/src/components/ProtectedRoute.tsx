import type { ReactNode } from "react";
import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import type { Role } from "../types/api";

interface Props {
  requireRole?: Role;
  children?: ReactNode;
}

export default function ProtectedRoute({ requireRole, children }: Props) {
  const { user } = useAuth();

  if (!user) return <Navigate to="/login" replace />;
  if (requireRole && user.role !== requireRole) return <Navigate to="/" replace />;

  return children ? <>{children}</> : <Outlet />;
}
