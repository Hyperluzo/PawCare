import { FormEvent, useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { PawPrint } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { ApiError } from "../services/api";

export default function Login() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (user) return <Navigate to="/" replace />;

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(email, password);
      navigate("/", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Something went wrong. Try again.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-bg px-4">
      <div className="w-full max-w-sm">
        <div className="flex items-center gap-2 justify-center mb-8">
          <PawPrint className="w-7 h-7 text-teal-700" strokeWidth={2.5} />
          <span className="font-display text-2xl font-semibold tracking-tight text-ink">PawCare</span>
        </div>

        <div className="bg-surface rounded-2xl border border-line shadow-sm p-8">
          <h1 className="font-display text-lg font-semibold text-ink mb-1">Staff sign in</h1>
          <p className="text-sm text-ink-muted mb-6">Use the credentials issued by your administrator.</p>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-ink mb-1.5">
                Email
              </label>
              <input
                id="email"
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="w-full rounded-lg border border-line px-3 py-2 text-sm text-ink focus:border-teal-600 outline-none"
                placeholder="you@pawcare.local"
              />
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-ink mb-1.5">
                Password
              </label>
              <input
                id="password"
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full rounded-lg border border-line px-3 py-2 text-sm text-ink focus:border-teal-600 outline-none"
                placeholder="••••••••"
              />
            </div>

            {error && (
              <p role="alert" className="text-sm text-red-500 bg-red-100 rounded-lg px-3 py-2">
                {error}
              </p>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full rounded-lg bg-teal-700 text-white text-sm font-medium py-2.5 hover:bg-teal-800 transition-colors disabled:opacity-60"
            >
              {loading ? "Signing in…" : "Sign in"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
