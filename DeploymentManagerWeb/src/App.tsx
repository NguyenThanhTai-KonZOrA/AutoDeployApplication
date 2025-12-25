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
import AdminCallPage from './pages/AdminCallPage'
import AdminCounterPage from './pages/AdminCounterPage'
import AdminTicketArchivedPage from './pages/AdminTicketArchived'
import AdminIssuedProcessedByHourPage from './pages/AdminIssuedProcessedByHourPage'
import AdminServiceReport from './pages/AdminServiceReport'
import EmployeeReportPage from './pages/EmployeeReportPage'
import AdminSettingsPage from './pages/AdminSettingsPage'
import { AppLoadingProvider } from './contexts/AppLoadingContext'
import RoleBasedRoute from './components/RoleBasedRoute'
import { Permission } from './constants/roles'
import AdminServiceTypePage from './pages/AdminServiceTypePage'
import { useTokenValidator } from './hooks/useTokenValidator'
import AdminAuditLogsPage from './pages/AdminAuditLogsPage'
import AdminRolePage from './pages/AdminRolePage'
import AdminPermissionPage from './pages/AdminPermissionPage'
import AdminEmployeePage from './pages/AdminEmployeePage'

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

        <Route path="/admin-call" element={
          <ProtectedRoute>
            <AdminCallPage />
          </ProtectedRoute>
        } />

        <Route path="/admin-dashboard" element={
          <ProtectedRoute>
            <RoleBasedRoute requiredPermission={Permission.VIEW_ADMIN_DASHBOARD}>
              <AdminDashboard />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/admin-counter" element={
          <ProtectedRoute>
            <RoleBasedRoute requiredPermission={Permission.VIEW_SYSTEM_SETTINGS}>
              <AdminCounterPage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/admin-ticket-archived" element={
          <ProtectedRoute>
            <RoleBasedRoute requiredPermission={Permission.VIEW_SYSTEM_SETTINGS}>
              <AdminTicketArchivedPage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/admin-issued-processed-by-hour" element={
          <ProtectedRoute>
            <RoleBasedRoute requiredPermission={Permission.VIEW_REPORTS}>
              <AdminIssuedProcessedByHourPage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/admin-service-report" element={
          <ProtectedRoute>
            <RoleBasedRoute requiredPermission={Permission.VIEW_REPORTS}>
              <AdminServiceReport />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/employee-report" element={
          <ProtectedRoute>
            <RoleBasedRoute requiredPermission={Permission.VIEW_REPORTS}>
              <EmployeeReportPage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/service-type-report" element={
          <ProtectedRoute>
            <RoleBasedRoute requiredPermission={Permission.VIEW_SYSTEM_SETTINGS}>
              <AdminServiceTypePage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/admin-settings" element={
          <ProtectedRoute>
            <RoleBasedRoute requiredPermission={Permission.VIEW_SYSTEM_SETTINGS}>
              <AdminSettingsPage />
            </RoleBasedRoute>
          </ProtectedRoute>
        } />

        <Route path="/admin-audit-logs" element={
          <ProtectedRoute>
            <RoleBasedRoute
              requiredPermission={Permission.VIEW_AUDIT_LOGS}
              fallbackPath="/admin-audit-logs"
              showAccessDenied={true}
            >
              <AdminAuditLogsPage />
            </RoleBasedRoute>
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