import { Plus, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import type { CaseDto, CaseStatus, Priority } from '../../api/types'
import { PriorityBadge, SegmentBadge, SlaCountdown } from '../../components/domain'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  ErrorState,
  Spinner,
} from '../../components/ui'
import { useCases } from '../../hooks/useCases'

const priorityOrder: Record<Priority, number> = {
  KRITIK: 0,
  YUKSEK: 1,
  ORTA: 2,
  DUSUK: 3,
}

const statuses: Array<CaseStatus | ''> = [
  '',
  'YENI',
  'ATANDI',
  'OPTIMIZE_EDILIYOR',
  'TEST_EDILIYOR',
  'TAMAMLANDI',
]

const priorities: Array<Priority | ''> = ['', 'KRITIK', 'YUKSEK', 'ORTA', 'DUSUK']

function sortCases(items: CaseDto[]) {
  return [...items].sort((first, second) => {
    const priorityDiff = priorityOrder[first.priority] - priorityOrder[second.priority]

    if (priorityDiff !== 0) {
      return priorityDiff
    }

    return first.remainingSlaSeconds - second.remainingSlaSeconds
  })
}

export function MyCasesPage() {
  const [status, setStatus] = useState<CaseStatus | ''>('')
  const [priority, setPriority] = useState<Priority | ''>('')
  const casesQuery = useCases({
    assignedToMe: true,
    status: status || undefined,
    priority: priority || undefined,
    page: 1,
    pageSize: 20,
  })

  const sortedCases = useMemo(
    () => sortCases(casesQuery.data?.items ?? []),
    [casesQuery.data?.items],
  )

  return (
    <section className="space-y-6">
      <Card>
        <CardHeader
          action={
            <Link to="/campaigns/new">
              <Button leftIcon={<Plus size={18} aria-hidden="true" />}>Yeni kampanya</Button>
            </Link>
          }
        >
          <CardTitle>Atanan vakalar</CardTitle>
          <CardDescription>
            Kritik ve yuksek oncelikli vakalar SLA suresine gore en ustte listelenir.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 md:grid-cols-[1fr_12rem_12rem]">
            <div className="flex h-11 items-center gap-2 rounded-md border border-slate-300 bg-white px-3 text-sm text-slate-500">
              <Search size={18} aria-hidden="true" />
              Backend geldikten sonra arama baglanacak
            </div>
            <select
              className="h-11 rounded-md border border-slate-300 px-3 text-sm outline-none focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
              onChange={(event) => setStatus(event.target.value as CaseStatus | '')}
              value={status}
            >
              {statuses.map((item) => (
                <option key={item || 'ALL'} value={item}>
                  {item || 'Tum durumlar'}
                </option>
              ))}
            </select>
            <select
              className="h-11 rounded-md border border-slate-300 px-3 text-sm outline-none focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
              onChange={(event) => setPriority(event.target.value as Priority | '')}
              value={priority}
            >
              {priorities.map((item) => (
                <option key={item || 'ALL'} value={item}>
                  {item || 'Tum oncelikler'}
                </option>
              ))}
            </select>
          </div>
        </CardContent>
      </Card>

      {casesQuery.isLoading ? <Spinner className="min-h-60" label="Vakalar yukleniyor" /> : null}

      {casesQuery.isError ? (
        <ErrorState onRetry={() => casesQuery.refetch()} title="Vakalar alinamadi" />
      ) : null}

      {!casesQuery.isLoading && !casesQuery.isError && sortedCases.length === 0 ? (
        <EmptyState
          action={
            <Link to="/campaigns/new">
              <Button>Ilk kampanyayi olustur</Button>
            </Link>
          }
          description="Filtrelere uyan atanmis vaka bulunmuyor."
          title="Vaka yok"
        />
      ) : null}

      <div className="grid gap-4">
        {sortedCases.map((item) => (
          <article
            className="rounded-md border border-slate-200 bg-white p-5 shadow-sm transition hover:border-brand-navy/40"
            key={item.id}
          >
            <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge tone="neutral">{item.caseNumber}</Badge>
                  <SegmentBadge segment={item.segment} />
                  <PriorityBadge priority={item.priority} />
                </div>
                <h2 className="mt-3 text-lg font-bold text-slate-950">{item.campaignTitle}</h2>
                <p className="mt-2 text-sm text-slate-600">
                  Durum: <span className="font-bold text-slate-800">{item.status}</span>
                  {item.conversionProbability
                    ? ` - AI donusum: %${Math.round(item.conversionProbability * 100)}`
                    : ' - AI degerlendirmesi bekleniyor'}
                </p>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <SlaCountdown
                  breached={item.slaBreached}
                  remainingSeconds={item.remainingSlaSeconds}
                />
                <Link
                  className="inline-flex h-10 items-center justify-center rounded-md border border-slate-200 bg-white px-4 text-sm font-bold text-slate-800 transition hover:border-brand-navy/40 hover:bg-slate-50"
                  to={`/cases/${item.id}`}
                >
                  Detay
                </Link>
              </div>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}
