import { Navigate, Outlet } from 'react-router-dom'
import { getRoleHomePath } from '../../app/roleRoutes'
import { useAuthStore } from '../../stores/auth.store'

export function GuestRoute() {
  const role = useAuthStore((state) => state.role)

  if (role) {
    return <Navigate to={getRoleHomePath(role)} replace />
  }

  return <Outlet />
}
