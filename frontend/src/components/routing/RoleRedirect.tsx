import { Navigate } from 'react-router-dom'
import { getRoleHomePath } from '../../app/roleRoutes'
import { useAuthStore } from '../../stores/auth.store'

export function RoleRedirect() {
  const role = useAuthStore((state) => state.role)

  if (!role) {
    return <Navigate to="/login" replace />
  }

  return <Navigate to={getRoleHomePath(role)} replace />
}
