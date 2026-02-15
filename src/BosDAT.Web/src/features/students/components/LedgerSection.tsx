import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { BookOpen, TrendingUp, TrendingDown, Wallet } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { studentTransactionsApi } from '@/features/students/api'
import type { StudentLedgerView, TransactionType } from '@/features/students/types'
import { transactionTypeTranslations } from '@/features/students/types'
import { formatDate } from '@/lib/datetime-helpers'
import { formatCurrency } from '@/lib/utils'
import { cn } from '@/lib/utils'

interface LedgerSectionProps {
  studentId: string
}

const TRANSACTION_TYPE_STYLES: Record<TransactionType, string> = {
  InvoiceCharge: 'bg-orange-100 text-orange-800',
  Payment: 'bg-green-100 text-green-800',
  CreditCorrection: 'bg-blue-100 text-blue-800',
  DebitCorrection: 'bg-red-100 text-red-800',
  Reversal: 'bg-purple-100 text-purple-800',
  InvoiceCancellation: 'bg-gray-100 text-gray-800',
  InvoiceAdjustment: 'bg-amber-100 text-amber-800',
  CorrectionApplied: 'bg-teal-100 text-teal-800',
}

export function LedgerSection({ studentId }: LedgerSectionProps) {
  const { t } = useTranslation()
  const [typeFilter, setTypeFilter] = useState<string>('all')

  const { data: ledger, isLoading } = useQuery<StudentLedgerView>({
    queryKey: ['student-transactions', studentId],
    queryFn: () => studentTransactionsApi.getLedger(studentId),
    enabled: !!studentId,
  })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  const transactions = ledger?.transactions ?? []
  const filteredTransactions =
    typeFilter === 'all'
      ? transactions
      : transactions.filter((tx) => tx.type === typeFilter)

  const currentBalance = ledger?.currentBalance ?? 0
  const totalDebited = ledger?.totalDebited ?? 0
  const totalCredited = ledger?.totalCredited ?? 0

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">{t('students.ledger.title')}</h2>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('students.ledger.currentBalance')}
            </CardTitle>
            <Wallet className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div
              className={cn(
                'text-2xl font-bold',
                currentBalance > 0 && 'text-red-600',
                currentBalance < 0 && 'text-green-600',
                currentBalance === 0 && 'text-muted-foreground',
              )}
            >
              {formatCurrency(currentBalance)}
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              {currentBalance > 0
                ? t('students.ledger.owes')
                : currentBalance < 0
                  ? t('students.ledger.credit')
                  : t('students.ledger.settled')}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('students.ledger.totalDebited')}
            </CardTitle>
            <TrendingUp className="h-4 w-4 text-red-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">
              {formatCurrency(totalDebited)}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('students.ledger.totalCredited')}
            </CardTitle>
            <TrendingDown className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {formatCurrency(totalCredited)}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filter + Transaction Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              <BookOpen className="h-5 w-5" />
              {t('students.ledger.transactions')}
            </CardTitle>
            <Select value={typeFilter} onValueChange={setTypeFilter}>
              <SelectTrigger className="w-[200px]">
                <SelectValue placeholder={t('students.ledger.filterByType')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">{t('students.ledger.allTypes')}</SelectItem>
                <SelectItem value="InvoiceCharge">
                  {t('students.ledger.transactionType.invoiceCharge')}
                </SelectItem>
                <SelectItem value="Payment">
                  {t('students.ledger.transactionType.payment')}
                </SelectItem>
                <SelectItem value="CreditCorrection">
                  {t('students.ledger.transactionType.creditCorrection')}
                </SelectItem>
                <SelectItem value="DebitCorrection">
                  {t('students.ledger.transactionType.debitCorrection')}
                </SelectItem>
                <SelectItem value="Reversal">
                  {t('students.ledger.transactionType.reversal')}
                </SelectItem>
                <SelectItem value="InvoiceCancellation">
                  {t('students.ledger.transactionType.invoiceCancellation')}
                </SelectItem>
                <SelectItem value="InvoiceAdjustment">
                  {t('students.ledger.transactionType.invoiceAdjustment')}
                </SelectItem>
                <SelectItem value="CorrectionApplied">
                  {t('students.ledger.transactionType.correctionApplied')}
                </SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardHeader>
        <CardContent>
          {filteredTransactions.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <BookOpen className="h-12 w-12 text-muted-foreground/50 mb-4" />
              <p className="text-lg font-medium text-muted-foreground">
                {t('students.ledger.noTransactions')}
              </p>
            </div>
          ) : (
            <div className="border rounded-md overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-muted">
                  <tr>
                    <th className="text-left p-2">{t('students.ledger.table.date')}</th>
                    <th className="text-left p-2">
                      {t('students.ledger.table.description')}
                    </th>
                    <th className="text-left p-2">
                      {t('students.ledger.table.reference')}
                    </th>
                    <th className="text-left p-2">{t('students.ledger.table.type')}</th>
                    <th className="text-right p-2">{t('students.ledger.table.debit')}</th>
                    <th className="text-right p-2">
                      {t('students.ledger.table.credit')}
                    </th>
                    <th className="text-right p-2">
                      {t('students.ledger.table.balance')}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {filteredTransactions.map((tx) => (
                    <tr key={tx.id} className="border-t hover:bg-muted/50">
                      <td className="p-2 whitespace-nowrap">
                        {formatDate(tx.transactionDate)}
                      </td>
                      <td className="p-2">{tx.description}</td>
                      <td className="p-2 font-mono text-xs">{tx.referenceNumber}</td>
                      <td className="p-2">
                        <span
                          className={cn(
                            'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                            TRANSACTION_TYPE_STYLES[tx.type],
                          )}
                        >
                          {t(transactionTypeTranslations[tx.type])}
                        </span>
                      </td>
                      <td className="p-2 text-right">
                        {tx.debit > 0 && (
                          <span className="text-red-600">
                            {formatCurrency(tx.debit)}
                          </span>
                        )}
                      </td>
                      <td className="p-2 text-right">
                        {tx.credit > 0 && (
                          <span className="text-green-600">
                            {formatCurrency(tx.credit)}
                          </span>
                        )}
                      </td>
                      <td
                        className={cn(
                          'p-2 text-right font-medium',
                          tx.runningBalance > 0 && 'text-red-600',
                          tx.runningBalance < 0 && 'text-green-600',
                        )}
                      >
                        {formatCurrency(tx.runningBalance)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
