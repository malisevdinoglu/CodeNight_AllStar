import { useState } from 'react'
import type { AuditLogDto } from '../../api/types'
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
import { useAuditLogs } from '../../hooks/useAdmin'

const actionTypes = ['', 'CASE_ASSIGN', 'FORBIDDEN_DASHBOARD_ACCESS', 'USER_CREATE']

const columns: DataTableColumn<AuditLogDto>[] = [
  {
    key: 'occurredAt',
    header: 'Zaman',
    render: (item) => new Date(item.occurredAt).toLocaleString('tr-TR'),
  },
  {
    key: 'userName',
    header: 'Kullanici',
    render: (item) => <span className="font-semibold text-slate-800">{item.userName}</span>,
  },
  {
    key: 'actionType',
    header: 'Aksiyon',
    render: (item) => <Badge tone="neutral">{item.actionType}</Badge>,
  },
  {
    key: 'resourceId',
    header: 'Kaynak',
    render: (item) => item.resourceId,
  },
  {
    key: 'ipAddress',
    header: 'IP',
    render: (item) => item.ipAddress,
  },
  {
    key: 'success',
    header: 'Sonuc',
    align: 'right',
    render: (item) => (
      <Badge tone={item.success ? 'success' : 'danger'}>{item.success ? 'Basarili' : 'Red'}</Badge>
    ),
  },
]

export function AuditLogsPage() {
  const [actionType, setActionType] = useState('')
  const auditLogsQuery = useAuditLogs({
    page: 1,
    pageSize: 20,
    actionType: actionType || undefined,
  })
  const logs = auditLogsQuery.data?.items ?? []

  return (
    <section className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Audit log</CardTitle>
          <CardDescription>
            403 yetki ihlalleri, personel olusturma ve operasyon aksiyonlari burada izlenir.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-5 grid gap-3 md:grid-cols-[16rem_1fr]">
            <label className="block">
              <span className="text-sm font-semibold text-slate-700">Aksiyon tipi</span>
              <select
                className="mt-2 h-11 w-full rounded-md border border-slate-300 px-3 text-sm outline-none transition focus:border-brand-navy focus:ring-2 focus:ring-brand-navy/15"
                onChange={(event) => setActionType(event.target.value)}
                value={actionType}
              >
                {actionTypes.map((item) => (
                  <option key={item || 'ALL'} value={item}>
                    {item || 'Tum aksiyonlar'}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {auditLogsQuery.isLoading ? <Spinner className="min-h-60" label="Loglar yukleniyor" /> : null}
          {auditLogsQuery.isError ? (
            <ErrorState onRetry={() => auditLogsQuery.refetch()} title="Audit log alinamadi" />
          ) : null}
          {!auditLogsQuery.isLoading && !auditLogsQuery.isError && logs.length === 0 ? (
            <EmptyState description="Filtreye uyan audit kaydi bulunmuyor." title="Log yok" />
          ) : null}
          {logs.length > 0 ? (
            <div className="space-y-3">
              <Table columns={columns} getRowKey={(item) => `${item.occurredAt}-${item.resourceId}`} items={logs} />
              <p className="text-xs font-semibold text-slate-500">
                Toplam {auditLogsQuery.data?.totalCount ?? logs.length} kayit
              </p>
            </div>
          ) : null}
        </CardContent>
      </Card>
    </section>
  )
}
