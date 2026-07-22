import { lazy, Suspense } from 'react'
import { Spinner } from '../../components/ui'

const DashboardPage = lazy(() =>
  import('./DashboardPage').then((module) => ({
    default: module.DashboardPage,
  })),
)

export function LazyDashboardPage() {
  return (
    <Suspense fallback={<Spinner className="min-h-80" />}>
      <DashboardPage />
    </Suspense>
  )
}
