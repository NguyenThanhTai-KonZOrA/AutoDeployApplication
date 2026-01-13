import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import ProtectedRoute from './components/ProtectedRoute'
import RoleBasedRedirect from './components/RoleBasedRedirect'
import { SessionManager } from './components/SessionManager'
import { PageTitleProvider } from './contexts/PageTitleContext'
import NetworkAlert from './components/NetworkAlert'
import { useNetworkStatus } from './hooks/useNetworkStatus'
import Login from './components/Login'
import './App.css'
import AdminDashboard from './pages/AdminDashboard'
import EmployeeReportPage from './pages/EmployeeReportPage'
import AdminSettingsPage from './pages/AdminSettingsPage'
import { AppLoadingProvider } from './contexts/AppLoadingContext'
import RoleBasedRoute from './components/RoleBasedRoute'
import { Permission } from './constants/roles'
import { useTokenValidator } from './hooks/useTokenValidator'
import AdminAuditLogsPage from './pages/AdminAuditLogsPage'
import AdminRolePage from './pages/AdminRolePage'
import AdminPermissionPage from './pages/AdminPermissionPage'
import AdminEmployeePage from './pages/AdminEmployeePage'
import AdminApplicationPage from './pages/AdminApplicationPage'
import AdminCategoryPage from './pages/AdminCategoryPage'
import AdminInstallationLogPage from './pages/AdminInstallationLogPage'
import AdminPackagesPage from './pages/AdminPackagesPage'
import AdminReportByApplicationPage from './pages/AdminReportByApplicationPage'
import AdminIconsPage from './pages/AdminIconsPage'

function AppContent() {
  const networkStatus = useNetworkStatus();

  // Validate token periodically
  useTokenValidator();

  return (
    <>
      {/* Network Alert */}
      <NetworkAlert {...networkStatus} />

      <Routes>
        {/* Public route */}
        <Route path="/login" element={<Login />} />

        {/* Root route - redirect based on user role */}
        <Route path="/" element={
          <ProtectedRoute>
            <RoleBasedRedirect />
          </ProtectedRoute>
        } />

        <Route path="/admin-dashboard" element={
          <ProtectedRoute>
            <AdminDashboard />
          </ProtectedRoute>
        } />

        <Route path="/admin-application" element={
          <ProtectedRoute>
            <AdminApplicationPage />
          </ProtectedRoute>
        } />

        <Route path="/admin-category" element={
          <ProtectedRoute>
            <AdminCategoryPage />
          </ProtectedRoute>
        } />

        <Route path="/admin-installation" element={
          <ProtectedRoute>
            <AdminInstallationLogPage />
          </ProtectedRoute>
        } />

        <Route path="/admin-package" element={
          <ProtectedRoute>
            <AdminPackagesPage />
          </ProtectedRoute>
        } />

        <Route path="/admin-report-by-application" element={
          <ProtectedRoute>
            <AdminReportByApplicationPage />
          </ProtectedRoute>
        } />

        <Route path="/admin-icons" element={
          <ProtectedRoute>
            <AdminIconsPage />
          </ProtectedRoute>
        } />

        <Route path="/admin-audit-logs" element={
          <ProtectedRoute>
            <AdminAuditLogsPage />
          </ProtectedRoute>
        } />

        <Route path="/admin-roles" element={
          <ProtectedRoute>
            <RoleBasedRoute
              requiredPermission={Permission.VIEW_ROLE_MANAGEMENT}
              fallbackPath="/admin-roles"
              showAccessDenied={true}
            >
              <AdminRolePage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/admin-permissions" element={
          <ProtectedRoute>
            <RoleBasedRoute
              requiredPermission={Permission.VIEW_ROLE_MANAGEMENT}
              fallbackPath="/admin-permissions"
              showAccessDenied={true}
            >
              <AdminPermissionPage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/admin-employees" element={
          <ProtectedRoute>
            <RoleBasedRoute
              requiredPermission={Permission.VIEW_EMPLOYEE_MANAGEMENT}
              fallbackPath="/admin-employees"
              showAccessDenied={true}
            >
              <AdminEmployeePage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

      </Routes>
    </>
  );
}

function App() {
  return (
    <Router>
      <PageTitleProvider>
        <AppLoadingProvider>
          <SessionManager>
            <AppContent />
          </SessionManager>
        </AppLoadingProvider>
      </PageTitleProvider>
    </Router>
  )
}

export default App