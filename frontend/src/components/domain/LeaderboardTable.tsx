import type { LeaderboardEntryDto } from '../../api/types'
import { Badge } from '../ui/Badge'
import { type DataTableColumn, Table } from '../ui/Table'

const columns: DataTableColumn<LeaderboardEntryDto>[] = [
  {
    key: 'rank',
    header: 'Sira',
    render: (item) => <span className="font-bold text-slate-950">#{item.rank}</span>,
  },
  {
    key: 'displayName',
    header: 'Uzman',
    render: (item) => <span className="font-semibold text-slate-800">{item.displayName}</span>,
  },
  {
    key: 'level',
    header: 'Seviye',
    render: (item) => <Badge tone="brand">{item.level}</Badge>,
  },
  {
    key: 'points',
    header: 'Puan',
    align: 'right',
    render: (item) => <span className="font-bold text-brand-navy">{item.points}</span>,
  },
]

export function LeaderboardTable({ entries }: { entries: LeaderboardEntryDto[] }) {
  return <Table columns={columns} getRowKey={(item) => item.expertId} items={entries} />
}
