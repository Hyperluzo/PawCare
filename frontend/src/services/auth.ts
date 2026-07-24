import { api } from "./api";
import type { Role } from "../types/api";

interface LoginResponse {
  token: string;
}

export interface DecodedToken {
  sub: string;
  email: string;
  role: Role;
  exp: number;
}

export async function login(email: string, password: string) {
  const { token } = await api.post<LoginResponse>("/auth/login", { email, password });
  localStorage.setItem("pawcare_token", token);
  return decodeToken(token);
}

export function logout() {
  localStorage.removeItem("pawcare_token");
  window.location.href = "/login";
}

// Display-only decode — never trusted for authorization, only for showing/
// hiding UI. The backend enforces access on every request regardless.
export function decodeToken(token: string): DecodedToken | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return {
      sub: payload.sub ?? payload.nameid,
      email: payload.email,
      role: payload.role,
      exp: payload.exp,
    };
  } catch {
    return null;
  }
}

export function getStoredToken(): string | null {
  return localStorage.getItem("pawcare_token");
}

export function getCurrentUser(): DecodedToken | null {
  const token = getStoredToken();
  if (!token) return null;
  const decoded = decodeToken(token);
  if (!decoded || decoded.exp * 1000 < Date.now()) {
    localStorage.removeItem("pawcare_token");
    return null;
  }
  return decoded;
}
