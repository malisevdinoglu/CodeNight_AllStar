import { apiClient } from './client'
import { unwrapApiResponse } from './response'
import type {
  ApiResponse,
  AuthSession,
  LoginRequest,
  OtpRequest,
  OtpVerifyRequest,
  RefreshTokenRequest,
} from './types'

export const authApi = {
  async login(payload: LoginRequest) {
    const response = await apiClient.post<ApiResponse<AuthSession>>('/auth/login', payload)
    return unwrapApiResponse(response.data)
  },

  async requestOtp(payload: OtpRequest) {
    // Backend OTP'yi simule ediyor (sabit "1234", gercek SMS/expiry takibi yok) - yanit
    // ApiResponseFactory.SuccessEmpty() ile data:null doner.
    const response = await apiClient.post<ApiResponse<null>>('/auth/otp/request', payload)
    return unwrapApiResponse(response.data)
  },

  async verifyOtp(payload: OtpVerifyRequest) {
    const response = await apiClient.post<ApiResponse<AuthSession>>('/auth/otp/verify', payload)
    return unwrapApiResponse(response.data)
  },

  async refresh(payload: RefreshTokenRequest) {
    const response = await apiClient.post<ApiResponse<AuthSession>>('/auth/refresh', payload)
    return unwrapApiResponse(response.data)
  },
}
