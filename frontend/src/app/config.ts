const apiUrl =
  import.meta.env.VITE_API_URL ??
  import.meta.env.VITE_API_BASE_URL ??
  'http://localhost:8080/api/v1'

const gatewayUrl = apiUrl.replace(/\/api\/v1\/?$/, '')

export const appConfig = {
  apiUrl,
  gameHubUrl: import.meta.env.VITE_GAME_HUB_URL ?? `${gatewayUrl}/hubs/game`,
  useMocks: import.meta.env.VITE_USE_MOCKS !== 'false',
} as const
