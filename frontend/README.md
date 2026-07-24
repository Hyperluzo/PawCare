# PawCare — Frontend

React + Vite + TypeScript + Tailwind v4 frontend for the PawCare veterinary
management portal. Talks to the ASP.NET Core backend via `VITE_API_BASE_URL`.

## Run locally

1. `npm install`
2. Copy `.env.example` to `.env.local` and set `VITE_API_BASE_URL` to your
   backend's URL (defaults to `http://localhost:5000/api`).
3. `npm run dev`

## Structure

- `src/services/` — one file per backend resource (auth, wards, patients,
  admin, dashboard). All requests go through `services/api.ts`, which attaches
  the JWT and handles 401s.
- `src/context/AuthContext.tsx` — holds the logged-in user + role, backed by
  the token in `localStorage`.
- `src/pages/` — one screen per route.
- `src/components/` — shared layout, route guard, small UI pieces.

## Auth

On login, the JWT returned by `POST /api/auth/login` is stored in
`localStorage` and attached to every request as `Authorization: Bearer <token>`.
Role-based UI (Admin-only screens) reads the role out of the token; the
backend is the real enforcement point via `[Authorize(Roles="ADMIN")]`.
