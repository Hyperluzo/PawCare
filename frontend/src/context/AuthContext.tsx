import { createContext, useContext, useState, type ReactNode } from "react";
import { getCurrentUser, login as loginApi, logout as logoutApi, type DecodedToken } from "../services/auth";

interface AuthContextValue {
  user: DecodedToken | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<DecodedToken | null>(() => getCurrentUser());

  async function login(email: string, password: string) {
    const decoded = await loginApi(email, password);
    setUser(decoded);
  }

  function logout() {
    logoutApi();
    setUser(null);
  }

  return <AuthContext.Provider value={{ user, login, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
