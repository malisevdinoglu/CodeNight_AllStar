export type UserRole = 'MUSTERI' | 'PERSONEL' | 'SUPERVIZOR' | 'ADMIN'

export type ApiError = {
  code: string
  message: string
  details?: string[]
}

export type ApiResponse<TData> = {
  success: boolean
  data: TData | null
  error: ApiError | null
}

export type AuthUser = {
  id: string
  firstName: string
  lastName: string
  role: UserRole
  expertise?: string[]
}

export type AuthSession = {
  accessToken: string
  refreshToken: string
  user: AuthUser
}
