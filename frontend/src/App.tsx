import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './contexts/AuthContext';
import { MainLayout } from './layouts/MainLayout';
import { AuthLayout } from './layouts/AuthLayout';
import { AdminLayout } from './layouts/AdminLayout';

// Pages
import { LoginPage } from './pages/auth/LoginPage';
import { DashboardPage } from './pages/user/DashboardPage';
import { AvailabilityPage } from './pages/user/AvailabilityPage';
import { MyReservationsPage } from './pages/user/MyReservationsPage';
import { ProfilePage } from './pages/user/ProfilePage';
import { CheckInPage } from './pages/public/CheckInPage';
import { AdminDashboardPage } from './pages/admin/AdminDashboardPage';
import { AdminResourcesPage } from './pages/admin/AdminResourcesPage';
import { AdminUsersPage } from './pages/admin/AdminUsersPage';
import { AdminAuditPage } from './pages/admin/AdminAuditPage';
import { NotFoundPage } from './pages/public/NotFoundPage';

function ProtectedRoute({ children, allowedRoles }: { children: React.ReactNode; allowedRoles?: string[] }) {
  const { user, isAuthenticated, hasRole } = useAuth();
  
  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: window.location.pathname }} />;
  }
  
  if (allowedRoles && !allowedRoles.some(role => hasRole(role))) {
    return <Navigate to="/dashboard" replace />;
  }
  
  return <>{children}</>;
}

function App() {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/login" element={<AuthLayout><LoginPage /></AuthLayout>} />
      <Route path="/check-in/:publicQrId" element={<CheckInPage />} />
      
      {/* Protected User Routes */}
      <Route element={<MainLayout><ProtectedRoute><DashboardPage /></ProtectedRoute></MainLayout>} path="/dashboard" />
      <Route element={<MainLayout><ProtectedRoute><AvailabilityPage /></ProtectedRoute></MainLayout>} path="/availability" />
      <Route element={<MainLayout><ProtectedRoute><MyReservationsPage /></ProtectedRoute></MainLayout>} path="/my-reservations" />
      <Route element={<MainLayout><ProtectedRoute><ProfilePage /></ProtectedRoute></MainLayout>} path="/profile" />
      
      {/* Admin Routes */}
      <Route 
        element={
          <AdminLayout>
            <ProtectedRoute allowedRoles={['GLOBAL_ADMIN']}>
              <AdminDashboardPage />
            </ProtectedRoute>
          </AdminLayout>
        } 
        path="/admin" 
      />
      <Route 
        element={
          <AdminLayout>
            <ProtectedRoute allowedRoles={['GLOBAL_ADMIN']}>
              <AdminResourcesPage />
            </ProtectedRoute>
          </AdminLayout>
        } 
        path="/admin/resources" 
      />
      <Route 
        element={
          <AdminLayout>
            <ProtectedRoute allowedRoles={['GLOBAL_ADMIN']}>
              <AdminUsersPage />
            </ProtectedRoute>
          </AdminLayout>
        } 
        path="/admin/users" 
      />
      <Route 
        element={
          <AdminLayout>
            <ProtectedRoute allowedRoles={['GLOBAL_ADMIN', 'SUPPORT']}>
              <AdminAuditPage />
            </ProtectedRoute>
          </AdminLayout>
        } 
        path="/admin/audit" 
      />
      
      {/* Redirects */}
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default App;