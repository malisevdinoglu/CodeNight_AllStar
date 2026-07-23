import type { ApiResponse } from './types'

export function unwrapApiResponse<TData>(response: ApiResponse<TData>): TData {
  // Onceden data === null durumunu da hata sayiyordu; ancak backend'de bazi endpoint'ler
  // (ApiResponseFactory.SuccessEmpty() - orn. logout, otp/request) BILEREK success:true +
  // data:null doner. Tek gecerli hata sinyali "success" alanidir.
  if (!response.success) {
    throw new Error(response.error?.message ?? 'API isteği başarısız oldu.')
  }

  return response.data as TData
}
