import { RouterProvider } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { AppProviders } from './app/providers'
import { router } from './router'

export default function App() {
  return (
    <AppProviders>
      <RouterProvider router={router} />
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 3500,
          className: 'toast-surface',
        }}
      />
    </AppProviders>
  )
}
