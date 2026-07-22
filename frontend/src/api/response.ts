import type { ApiResponse } from './types'

export function unwrapApiResponse<TData>(response: ApiResponse<TData>): TData {
  if (!response.success || response.data === null) {
    throw new Error(response.error?.message ?? 'API isteği başarısız oldu.')
  }

  return response.data
}
