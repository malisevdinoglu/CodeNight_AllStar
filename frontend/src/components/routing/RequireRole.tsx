import { Navigate, Outlet } from 'react-router-dom'
import { getRoleHomePath } from '../../app/roleRoutes'
import type { UserRole } from '../../api/types'
import { useAuthStore } from '../../stores/auth.store'

type RequireRoleProps = {
  allowedRoles: UserRole[]
}

export function RequireRole({ allowedRoles }: RequireRoleProps) {
  const role = useAuthStore((state) => state.role)

  if (!role) {
    return <Navigate to="/login" replace />
  }

  if (!allowedRoles.includes(role)) {
    return <Navigate to={getRoleHomePath(role)} replace />
  }

  return <Outlet />
}
