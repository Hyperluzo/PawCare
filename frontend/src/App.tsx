import { Navigate, Route, Routes } from "react-router-dom";
import Layout from "./components/Layout";
import ProtectedRoute from "./components/ProtectedRoute";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import Wards from "./pages/Wards";
import Patients from "./pages/Patients";
import PatientDetail from "./pages/PatientDetail";
import AdmitPatient from "./pages/AdmitPatient";
import AdminUsers from "./pages/AdminUsers";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/wards" element={<Wards />} />
          <Route path="/patients" element={<Patients />} />
          <Route path="/patients/new" element={<AdmitPatient />} />
          <Route path="/patients/:id" element={<PatientDetail />} />
          <Route
            path="/admin/users"
            element={
              <ProtectedRoute requireRole="ADMIN">
                <AdminUsers />
              </ProtectedRoute>
            }
          />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
