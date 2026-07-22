export const appConfig = {
  apiUrl:
    import.meta.env.VITE_API_URL ??
    import.meta.env.VITE_API_BASE_URL ??
    'http://localhost:8080/api/v1',
  useMocks: import.meta.env.VITE_USE_MOCKS !== 'false',
} as const
