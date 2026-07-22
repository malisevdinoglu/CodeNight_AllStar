import { HttpResponse } from 'msw'
import type { ApiError, ApiResponse } from '../api/types'

export function ok<TData>(data: TData, init?: ResponseInit) {
  return HttpResponse.json<ApiResponse<TData>>(
    {
      success: true,
      data,
      error: null,
    },
    init,
  )
}

export function fail(code: string, message: string, status: number, details: string[] = []) {
  return HttpResponse.json<ApiResponse<null>>(
    {
      success: false,
      data: null,
      error: {
        code,
        message,
        details,
      } satisfies ApiError,
    },
    { status },
  )
}
