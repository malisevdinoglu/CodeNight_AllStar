import type { ReactNode } from 'react'

export type DataTableColumn<TItem> = {
  key: string
  header: string
  render: (item: TItem) => ReactNode
  align?: 'left' | 'right'
}

type TableProps<TItem> = {
  columns: DataTableColumn<TItem>[]
  items: TItem[]
  getRowKey: (item: TItem) => string
}

export function Table<TItem>({ columns, getRowKey, items }: TableProps<TItem>) {
  return (
    <div className="overflow-hidden rounded-md border border-slate-200 bg-white">
      <div className="overflow-x-auto">
        <table className="min-w-full border-collapse text-left text-sm">
          <thead className="bg-slate-50 text-xs font-bold uppercase text-slate-500">
            <tr>
              {columns.map((column) => (
                <th
                  className={`whitespace-nowrap px-4 py-3 ${
                    column.align === 'right' ? 'text-right' : 'text-left'
                  }`}
                  key={column.key}
                  scope="col"
                >
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200">
            {items.map((item) => (
              <tr className="transition hover:bg-slate-50" key={getRowKey(item)}>
                {columns.map((column) => (
                  <td
                    className={`whitespace-nowrap px-4 py-3 text-slate-700 ${
                      column.align === 'right' ? 'text-right' : 'text-left'
                    }`}
                    key={column.key}
                  >
                    {column.render(item)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
