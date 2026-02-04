import { useQuery } from '@tanstack/react-query'
import { Wallet, TrendingUp, TrendingDown, CreditCard, FileText } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { studentLedgerApi } from '@/services/api'
import type { StudentLedgerSummary, StudentLedgerEntry } from '@/features/students/types'
import { formatDate } from '@/lib/datetime-helpers'
import { cn } from '@/lib/utils'

interface BalanceSectionProps {
  studentId: string
}

export function BalanceSection({ studentId }: BalanceSectionProps) {
  const { data: summary, isLoading: summaryLoading } = useQuery<StudentLedgerSummary>({
    queryKey: ['student-ledger-summary', studentId],
    queryFn: () => studentLedgerApi.getSummary(studentId),
    enabled: !!studentId,
  })

  const { data: entries = [], isLoading: entriesLoading } = useQuery<StudentLedgerEntry[]>({
    queryKey: ['student-ledger', studentId],
    queryFn: () => studentLedgerApi.getByStudent(studentId),
    enabled: !!studentId,
  })

  const isLoading = summaryLoading || entriesLoading

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  // Get only open entries
  const openEntries = entries.filter((e) => e.status === 'Open' || e.status === 'PartiallyApplied')

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Balance</h2>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Available Credit</CardTitle>
            <CreditCard className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className={cn(
              'text-2xl font-bold',
              (summary?.availableCredit ?? 0) > 0 && 'text-green-600',
              (summary?.availableCredit ?? 0) < 0 && 'text-red-600'
            )}>
              {(summary?.availableCredit ?? 0).toFixed(2)}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Credits</CardTitle>
            <TrendingUp className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              +{(summary?.totalCredits ?? 0).toFixed(2)}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Debits</CardTitle>
            <TrendingDown className="h-4 w-4 text-red-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">
              -{(summary?.totalDebits ?? 0).toFixed(2)}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Open Entries</CardTitle>
            <FileText className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {summary?.openEntryCount ?? 0}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Open Entries */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Wallet className="h-5 w-5" />
            Open Balance Entries
          </CardTitle>
        </CardHeader>
        <CardContent>
          {openEntries.length === 0 ? (
            <p className="text-muted-foreground">No open balance entries</p>
          ) : (
            <div className="divide-y">
              {openEntries.map((entry) => (
                <div key={entry.id} className="flex items-center justify-between py-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-sm text-muted-foreground">
                        {entry.correctionRefName}
                      </span>
                      <span
                        className={cn(
                          'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                          entry.entryType === 'Credit' && 'bg-green-100 text-green-800',
                          entry.entryType === 'Debit' && 'bg-red-100 text-red-800'
                        )}
                      >
                        {entry.entryType}
                      </span>
                    </div>
                    <p className="font-medium">{entry.description}</p>
                    <p className="text-sm text-muted-foreground">
                      {formatDate(entry.createdAt)}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className={cn(
                      'font-medium',
                      entry.entryType === 'Credit' && 'text-green-600',
                      entry.entryType === 'Debit' && 'text-red-600'
                    )}>
                      {entry.entryType === 'Credit' ? '+' : '-'}{entry.remainingAmount.toFixed(2)}
                    </p>
                    {entry.appliedAmount > 0 && (
                      <p className="text-xs text-muted-foreground">
                        Applied: {entry.appliedAmount.toFixed(2)}
                      </p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* All Entries History */}
      <Card>
        <CardHeader>
          <CardTitle>Transaction History</CardTitle>
        </CardHeader>
        <CardContent>
          {entries.length === 0 ? (
            <p className="text-muted-foreground">No transactions yet</p>
          ) : (
            <div className="divide-y">
              {entries.map((entry) => (
                <div key={entry.id} className="flex items-center justify-between py-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-sm text-muted-foreground">
                        {entry.correctionRefName}
                      </span>
                      <span
                        className={cn(
                          'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                          entry.status === 'Open' && 'bg-blue-100 text-blue-800',
                          entry.status === 'Applied' && 'bg-gray-100 text-gray-800',
                          entry.status === 'PartiallyApplied' && 'bg-yellow-100 text-yellow-800',
                          entry.status === 'Reversed' && 'bg-red-100 text-red-800'
                        )}
                      >
                        {entry.status}
                      </span>
                    </div>
                    <p className="text-sm">{entry.description}</p>
                    <p className="text-xs text-muted-foreground">
                      {formatDate(entry.createdAt)} by {entry.createdByName}
                    </p>
                  </div>
                  <div className={cn(
                    'font-medium',
                    entry.entryType === 'Credit' && 'text-green-600',
                    entry.entryType === 'Debit' && 'text-red-600'
                  )}>
                    {entry.entryType === 'Credit' ? '+' : '-'}{entry.amount.toFixed(2)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
