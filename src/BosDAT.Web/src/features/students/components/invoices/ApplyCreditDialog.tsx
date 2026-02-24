import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { invoicesApi } from '@/features/students/api'
import { formatCurrency } from '@/lib/utils'

interface ApplyCreditDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  invoiceId: string
  studentId: string
  availableCredit: number
  remainingBalance: number
}

export function ApplyCreditDialog({
  open,
  onOpenChange,
  invoiceId,
  studentId,
  availableCredit,
  remainingBalance,
}: Readonly<ApplyCreditDialogProps>) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const defaultAmount = Math.min(availableCredit, remainingBalance)
  const [amount, setAmount] = useState(defaultAmount.toFixed(2))
  const [notes, setNotes] = useState('')

  const resetForm = () => {
    setAmount(Math.min(availableCredit, remainingBalance).toFixed(2))
    setNotes('')
  }

  const applyCreditMutation = useMutation({
    mutationFn: () =>
      invoicesApi.applyCreditBalance(invoiceId, {
        amount: Number.parseFloat(amount),
        notes: notes || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', 'student', studentId] })
      queryClient.invalidateQueries({ queryKey: ['invoice', invoiceId] })
      queryClient.invalidateQueries({ queryKey: ['student-transactions', studentId] })
      queryClient.invalidateQueries({ queryKey: ['student-balance', studentId] })
      resetForm()
      onOpenChange(false)
    },
  })

  const parsedAmount = Number.parseFloat(amount)
  const isValidAmount =
    !Number.isNaN(parsedAmount) &&
    parsedAmount > 0 &&
    parsedAmount <= availableCredit &&
    parsedAmount <= remainingBalance

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('students.creditBalance.title')}</DialogTitle>
          <DialogDescription>
            {t('students.creditBalance.description', {
              credit: formatCurrency(availableCredit),
              balance: formatCurrency(remainingBalance),
            })}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4 rounded-md bg-muted p-3 text-sm">
            <div>
              <div className="text-muted-foreground">{t('students.creditBalance.availableCredit')}</div>
              <div className="font-medium text-green-600">{formatCurrency(availableCredit)}</div>
            </div>
            <div>
              <div className="text-muted-foreground">{t('students.creditBalance.invoiceBalance')}</div>
              <div className="font-medium">{formatCurrency(remainingBalance)}</div>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="credit-amount">{t('students.creditBalance.amount')}</Label>
            <div className="flex gap-2">
              <Input
                id="credit-amount"
                type="number"
                lang="nl-NL"
                step="0.01"
                min="0.01"
                max={defaultAmount}
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder={`Max ${formatCurrency(defaultAmount)}`}
              />
              <Button variant="outline" onClick={() => setAmount(defaultAmount.toFixed(2))}>
                {t('students.creditBalance.fullAmount')}
              </Button>
            </div>
            {amount && !isValidAmount && (
              <p className="text-sm text-destructive">
                {parsedAmount > availableCredit
                  ? t('students.creditBalance.exceedsCredit')
                  : parsedAmount > remainingBalance
                    ? t('students.creditBalance.exceedsBalance')
                    : t('students.payments.invalidAmount')}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="credit-notes">{t('students.creditBalance.notes')}</Label>
            <Textarea
              id="credit-notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={t('students.creditBalance.notesPlaceholder')}
              rows={2}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.actions.cancel')}
          </Button>
          <Button
            onClick={() => applyCreditMutation.mutate()}
            disabled={!isValidAmount || applyCreditMutation.isPending}
          >
            {applyCreditMutation.isPending
              ? t('students.actions.saving')
              : t('students.creditBalance.apply')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
