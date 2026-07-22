import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { GuestRoute } from './components/routing/GuestRoute'
import { RequireAuth } from './components/routing/RequireAuth'
import { RequireRole } from './components/routing/RequireRole'
import { RoleRedirect } from './components/routing/RoleRedirect'
import { AuditLogsPage } from './pages/admin/AuditLogsPage'
import { StaffPage } from './pages/admin/StaffPage'
import { LoginPage } from './pages/auth/LoginPage'
import { CaseDetailPage } from './pages/expert/CaseDetailPage'
import { CreateCampaignPage } from './pages/expert/CreateCampaignPage'
import { GameProfilePage } from './pages/expert/GameProfilePage'
import { MyCasesPage } from './pages/expert/MyCasesPage'
import { MyCampaignsPage } from './pages/subscriber/MyCampaignsPage'
import { OfferDetailPage } from './pages/subscriber/OfferDetailPage'
import { OffersPage } from './pages/subscriber/OffersPage'
import { CasesPage } from './pages/supervisor/CasesPage'
import { LazyDashboardPage } from './pages/supervisor/LazyDashboardPage'
import { QueuePage } from './pages/supervisor/QueuePage'

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
            {
            element: <RequireRole allowedRoles={['MUSTERI']} />,
            children: [
              { path: 'offers', element: <OffersPage /> },
              { path: 'offers/:offerId', element: <OfferDetailPage /> },
              { path: 'my-campaigns', element: <MyCampaignsPage /> },
            ],
          },
          {
            // Kampanya olusturma: MUSTERI YAPAMAZ, digerleri (PERSONEL/SUPERVIZOR/ADMIN)
            // yapabilir (case §3.3 netlestirme sonrasi) - cases/game-profile'dan ayri blok,
            // cunku onlar hala PERSONEL-only.
            element: <RequireRole allowedRoles={['PERSONEL', 'SUPERVIZOR', 'ADMIN']} />,
            children: [{ path: 'campaigns/new', element: <CreateCampaignPage /> }],
          },
          {
            element: <RequireRole allowedRoles={['PERSONEL']} />,
            children: [
              { path: 'cases', element: <MyCasesPage /> },
              { path: 'cases/:caseId', element: <CaseDetailPage /> },
              { path: 'game-profile', element: <GameProfilePage /> },
            ],
          },
          {
            element: <RequireRole allowedRoles={['SUPERVIZOR']} />,
            children: [
              {
                path: 'dashboard',
                element: <LazyDashboardPage />,
              },
              { path: 'queue', element: <QueuePage /> },
              { path: 'supervisor/cases', element: <CasesPage /> },
            ],
          },
          {
            element: <RequireRole allowedRoles={['ADMIN']} />,
            children: [
              { path: 'admin/staff', element: <StaffPage /> },
              { path: 'admin/audit-logs', element: <AuditLogsPage /> },
            ],
          },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])
