import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { GuestRoute } from './components/routing/GuestRoute'
import { RequireAuth } from './components/routing/RequireAuth'
import { RequireRole } from './components/routing/RequireRole'
import { RoleRedirect } from './components/routing/RoleRedirect'
import { PhaseOnePage } from './pages/PhaseOnePage'
import { StaffPage } from './pages/admin/StaffPage'
import { LoginPage } from './pages/auth/LoginPage'
import { CaseDetailPage } from './pages/expert/CaseDetailPage'
import { CreateCampaignPage } from './pages/expert/CreateCampaignPage'
import { GameProfilePage } from './pages/expert/GameProfilePage'
import { MyCasesPage } from './pages/expert/MyCasesPage'
import { OffersPage } from './pages/subscriber/OffersPage'
import { DashboardPage } from './pages/supervisor/DashboardPage'

export const router = createBrowserRouter([
  {
    element: <GuestRoute />,
    children: [{ path: '/login', element: <LoginPage /> }],
  },
  {
    element: <RequireAuth />,
    children: [
      {
        path: '/',
        element: <AppShell />,
        children: [
          { index: true, element: <RoleRedirect /> },
          { path: 'phase-one', element: <PhaseOnePage /> },
          {
            element: <RequireRole allowedRoles={['MUSTERI']} />,
            children: [{ path: 'offers', element: <OffersPage /> }],
          },
          {
            element: <RequireRole allowedRoles={['PERSONEL']} />,
            children: [
              { path: 'campaigns/new', element: <CreateCampaignPage /> },
              { path: 'cases', element: <MyCasesPage /> },
              { path: 'cases/:caseId', element: <CaseDetailPage /> },
              { path: 'game-profile', element: <GameProfilePage /> },
            ],
          },
          {
            element: <RequireRole allowedRoles={['SUPERVIZOR']} />,
            children: [{ path: 'dashboard', element: <DashboardPage /> }],
          },
          {
            element: <RequireRole allowedRoles={['ADMIN']} />,
            children: [{ path: 'admin/staff', element: <StaffPage /> }],
          },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
