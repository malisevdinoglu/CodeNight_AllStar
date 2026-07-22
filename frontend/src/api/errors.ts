import axios from 'axios'
import type { ApiError, ApiResponse } from './types'

export function getApiError(error: unknown): ApiError {
  if (axios.isAxiosError<ApiResponse<unknown>>(error)) {
    const apiError = error.response?.data?.error

    if (apiError) {
      return apiError
    }
  }

  if (error instanceof Error) {
    return {
      code: 'FE_UNKNOWN_ERROR',
      message: error.message,
      details: [],
    }
  }

  return {
    code: 'FE_UNKNOWN_ERROR',
    message: 'Beklenmeyen bir hata oluştu.',
    details: [],
  }
}
