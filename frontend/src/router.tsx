import { createBrowserRouter, Navigate } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { PhaseOnePage } from './pages/PhaseOnePage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    children: [
      { index: true, element: <Navigate to="/phase-one" replace /> },
      { path: 'phase-one', element: <PhaseOnePage /> },
    ],
  },
])
