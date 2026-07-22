import { useMemo, useState } from 'react'
import type { CaseDto, CaseStatus, Priority } from '../../api/types'
import { PriorityBadge, SegmentBadge, SlaCountdown } from '../../components/domain'
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  ErrorState,
  Spinner,
  Table,
  type DataTableColumn,
} from '../../components/ui'
import { useCases } from '../../hooks/useCases'

const statuses: Array<CaseStatus | ''> = [
  '',
  'YENI',
  'ATANDI',
  'OPTIMIZE_EDILIYOR',
  'TEST_EDILIYOR',
  'TAMAMLANDI',
  'YAYINDA',
  'ARSIVLENDI',
]

const priorities: Array<Priority | ''> = ['', 'KRITIK', 'YUKSEK', 'ORTA', 'DUSUK']

const columns: DataTableColumn<CaseDto>[] = [
  {
    key: 'caseNumber',
    header: 'Vaka',
    render: (item) => <span className="font-bold text-slate-950">{item.caseNumber}</span>,
  },
  {
    key: 'campaignTitle',
    header: 'Kampanya',
    render: (item) => <span className="font-semibold text-slate-800">{item.campaignTitle}</span>,
  },
  {
    key: 'segment',
    header: 'Segment',
    render: (item) => <SegmentBadge segment={item.segment} />,
  },
  {
    key: 'priority',
    header: 'Oncelik',
    render: (item) => <PriorityBadge priority={item.priority} />,
  },
  {
    key: 'status',
    header: 'Durum',
    render: (item) => <Badge tone="neutral">{item.status}</Badge>,
  },
  {
    key: 'expert',
    header: 'Uzman',
    render: (item) => item.assignedExpertName ?? 'Atama bekliyor',
  },
  {
    key: 'ai',
    header: 'AI',
    align: 'right',
    render: (item) =>
      item.conversionProbability ? `%${Math.round(item.conversionProbability * 100)}` : 'Bekliyor',
  },
  {
    key: 'sla',
    header: 'SLA',
    align: 'right',
    render: (item) => (
      <SlaCountdown breached={item.slaBreached} remainingSeconds={item.remainingSlaSeconds} />
    ),
  },
]

export function CasesPage() {
  const [status, setStatus] = useState<CaseStatus | ''>('')
  const [priority, setPriority] = useState<Priority | ''>('')
  const casesQuery = useCases({
    status: status || undefined,
    priority: priority || undefined,
    page: 1,
    pageSize: 50,
  })
  const cases = useMemo(() => casesQuery.data?.items ?? [], [casesQuery.data?.items])
  const stats = useMemo(
    () => ({
      total: cases.length,
      unassigned: cases.filter((item) => !item.assignedExpertId).length,
      breached: cases.filter((item) => item.slaBreached).length,
    }),
    [cases],
  )

  return (
    <section className="space-y-6">
      <div className="grid gap-4 md:grid-cols-3">
        <Stat label="Toplam vaka" value={stats.total.toString()} />
        <Stat label="Atama bekleyen" value={stats.unassigned.toString()} />
        <Stat label="SLA asan" value={stats.breached.toString()} tone="danger" />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Tum vakalar</CardTitle>
          <CardDescription>Supervisor operasyon gorunumu; backend pagination gelince ayni tablo korunur.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-5 grid gap-3 md:grid-cols-[12rem_12rem]">
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

          {casesQuery.isLoading ? <Spinner className="min-h-60" label="Vakalar yukleniyor" /> : null}
          {casesQuery.isError ? (
            <ErrorState onRetry={() => casesQuery.refetch()} title="Vakalar alinamadi" />
          ) : null}
          {!casesQuery.isLoading && !casesQuery.isError && cases.length === 0 ? (
            <EmptyState
              description="Filtrelere uyan operasyon vakasi bulunmuyor."
              title="Vaka yok"
            />
          ) : null}
          {cases.length > 0 ? <Table columns={columns} getRowKey={(item) => item.id} items={cases} /> : null}
        </CardContent>
      </Card>
    </section>
  )
}

function Stat({ label, tone = 'neutral', value }: { label: string; value: string; tone?: 'neutral' | 'danger' }) {
  return (
    <Card>
      <CardContent>
        <p className="text-xs font-bold uppercase text-slate-500">{label}</p>
        <p className={tone === 'danger' ? 'mt-2 text-3xl font-bold text-red-700' : 'mt-2 text-3xl font-bold text-brand-navy'}>
          {value}
        </p>
      </CardContent>
    </Card>
  )
}
