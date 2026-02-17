import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { ArrowDownLeft, ArrowUpRight, RefreshCw, Receipt } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { studentTransactionsApi } from '@/features/students/api'
import { transactionTypeTranslations } from '@/features/students/types'
import type { StudentTransaction, TransactionType } from '@/features/students/types'
import { formatCurrency } from '@/lib/utils'
import { formatDate } from '@/lib/datetime-helpers'

interface TransactionsSectionProps {
  studentId: string
}

const typeVariant: Record<TransactionType, string> = {
  InvoiceCharge: 'bg-blue-100 text-blue-800',
  Payment: 'bg-green-100 text-green-800',
  InvoiceCancellation: 'bg-gray-100 text-gray-600',
  InvoiceAdjustment: 'bg-yellow-100 text-yellow-800',
}

function TransactionRow({ transaction }: { transaction: StudentTransaction }) {
  const { t } = useTranslation()

  return (
    <tr className="border-t text-sm">
      <td className="p-3 text-muted-foreground whitespace-nowrap">
        {formatDate(transaction.transactionDate)}
      </td>
      <td className="p-3">{transaction.description}</td>
      <td className="p-3 text-muted-foreground font-mono text-xs">
        {transaction.referenceNumber}
      </td>
      <td className="p-3">
        <Badge className={typeVariant[transaction.type] ?? 'bg-gray-100 text-gray-600'}>
          {t(transactionTypeTranslations[transaction.type] ?? transaction.type)}
        </Badge>
      </td>
      <td className="p-3 text-right">
        {transaction.debit > 0 && (
          <span className="flex items-center justify-end gap-1 text-red-600">
            <ArrowUpRight className="h-3.5 w-3.5" />
            {formatCurrency(transaction.debit)}
          </span>
        )}
      </td>
      <td className="p-3 text-right">
        {transaction.credit > 0 && (
          <span className="flex items-center justify-end gap-1 text-green-600">
            <ArrowDownLeft className="h-3.5 w-3.5" />
            {formatCurrency(transaction.credit)}
          </span>
        )}
      </td>
      <td className={`p-3 text-right font-medium ${transaction.runningBalance > 0 ? 'text-red-600' : 'text-green-600'}`}>
        {formatCurrency(Math.abs(transaction.runningBalance))}
        {transaction.runningBalance > 0 && (
          <span className="text-xs text-muted-foreground ml-1">{t('students.transactions.owed')}</span>
        )}
      </td>
    </tr>
  )
}

export function TransactionsSection({ studentId }: TransactionsSectionProps) {
  const { t } = useTranslation()

  const { data: transactions = [], isLoading } = useQuery<StudentTransaction[]>({
    queryKey: ['student', studentId, 'transactions'],
    queryFn: () => studentTransactionsApi.getAll(studentId),
    enabled: !!studentId,
  })

  const balance = transactions.length > 0 ? transactions[transactions.length - 1].runningBalance : 0

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h2 className="text-2xl font-bold">{t('students.sections.transactions')}</h2>
        <Card>
          <CardContent className="py-12">
            <div className="flex items-center justify-center">
              <div className="animate-spin h-8 w-8 border-4 border-primary border-t-transparent rounded-full" />
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">{t('students.sections.transactions')}</h2>

      {transactions.length > 0 && (
        <div className="grid grid-cols-2 gap-4">
          <Card>
            <CardContent className="pt-4">
              <div className="text-sm text-muted-foreground">{t('students.transactions.currentBalance')}</div>
              <div className={`text-2xl font-bold ${balance > 0 ? 'text-red-600' : 'text-green-600'}`}>
                {formatCurrency(Math.abs(balance))}
              </div>
              <div className="text-xs text-muted-foreground mt-1">
                {balance > 0 ? t('students.transactions.outstanding') : t('students.transactions.noBalance')}
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4">
              <div className="text-sm text-muted-foreground">{t('students.transactions.totalTransactions')}</div>
              <div className="text-2xl font-bold">{transactions.length}</div>
            </CardContent>
          </Card>
        </div>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <RefreshCw className="h-5 w-5" />
            {t('students.transactions.history')}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {transactions.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center px-6">
              <Receipt className="h-12 w-12 text-muted-foreground/50 mb-4" />
              <p className="text-lg font-medium text-muted-foreground">{t('students.transactions.noTransactions')}</p>
              <p className="text-sm text-muted-foreground mt-1">{t('students.transactions.noTransactionsDesc')}</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-muted text-sm">
                  <tr>
                    <th className="text-left p-3 font-medium">{t('students.transactions.date')}</th>
                    <th className="text-left p-3 font-medium">{t('students.transactions.description')}</th>
                    <th className="text-left p-3 font-medium">{t('students.transactions.reference')}</th>
                    <th className="text-left p-3 font-medium">{t('students.transactions.typeLabel')}</th>
                    <th className="text-right p-3 font-medium">{t('students.transactions.debit')}</th>
                    <th className="text-right p-3 font-medium">{t('students.transactions.credit')}</th>
                    <th className="text-right p-3 font-medium">{t('students.transactions.balance')}</th>
                  </tr>
                </thead>
                <tbody>
                  {transactions.map((transaction) => (
                    <TransactionRow key={transaction.id} transaction={transaction} />
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
