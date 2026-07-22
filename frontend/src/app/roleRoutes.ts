import type { UserRole } from '../api/types'

export const roleHomePath: Record<UserRole, string> = {
  MUSTERI: '/offers',
  PERSONEL: '/cases',
  SUPERVIZOR: '/dashboard',
  ADMIN: '/admin/staff',
}

export function getRoleHomePath(role: UserRole) {
  return roleHomePath[role]
}
