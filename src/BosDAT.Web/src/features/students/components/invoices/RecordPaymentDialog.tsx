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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { invoicesApi } from '@/features/students/api'
import type { PaymentMethod } from '@/features/students/types'
import { paymentMethodTranslations } from '@/features/students/types'
import { formatCurrency } from '@/lib/utils'

interface RecordPaymentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  invoiceId: string
  invoiceNumber: string
  remainingBalance: number
  studentId: string
}

const PAYMENT_METHODS: PaymentMethod[] = ['Cash', 'Bank', 'Card', 'DirectDebit', 'Other']

export function RecordPaymentDialog({
  open,
  onOpenChange,
  invoiceId,
  invoiceNumber,
  remainingBalance,
  studentId,
}: RecordPaymentDialogProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [amount, setAmount] = useState('')
  const [paymentDate, setPaymentDate] = useState(
    new Date().toISOString().split('T')[0],
  )
  const [method, setMethod] = useState<PaymentMethod>('Bank')
  const [reference, setReference] = useState('')
  const [notes, setNotes] = useState('')

  const resetForm = () => {
    setAmount('')
    setPaymentDate(new Date().toISOString().split('T')[0])
    setMethod('Bank')
    setReference('')
    setNotes('')
  }

  const recordPaymentMutation = useMutation({
    mutationFn: () =>
      invoicesApi.recordPayment(invoiceId, {
        amount: parseFloat(amount),
        paymentDate,
        method,
        reference: reference || undefined,
        notes: notes || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', 'student', studentId] })
      queryClient.invalidateQueries({ queryKey: ['invoice', invoiceId] })
      queryClient.invalidateQueries({
        queryKey: ['student-transactions', studentId],
      })
      resetForm()
      onOpenChange(false)
    },
  })

  const parsedAmount = parseFloat(amount)
  const isValidAmount =
    !isNaN(parsedAmount) && parsedAmount > 0 && parsedAmount <= remainingBalance
  const canSubmit = isValidAmount && paymentDate && method

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('students.payments.recordPayment')}</DialogTitle>
          <DialogDescription>
            {t('students.payments.recordPaymentDesc', {
              invoiceNumber,
              balance: formatCurrency(remainingBalance),
            })}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="payment-amount">{t('students.payments.amount')}</Label>
            <div className="flex space-y-2 space-x-2">
            <Input
              id="payment-amount"
              type="number"
              lang='nl-NL'
              step="0.01"
              min="0.01"
              max={remainingBalance}
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder={`Max ${formatCurrency(remainingBalance)}`}
            />
            {amount && !isValidAmount && (
              <p className="text-sm text-destructive">
                {parsedAmount > remainingBalance
                  ? t('students.payments.exceedsBalance')
                  : t('students.payments.invalidAmount')}
              </p>
            )}
            <Button variant={'outline'} onClick={() =>  setAmount(remainingBalance.toString())}>
              Volledige bedrag
            </Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="payment-date">{t('students.payments.paymentDate')}</Label>
            <Input
              id="payment-date"
              type="date"
              value={paymentDate}
              onChange={(e) => setPaymentDate(e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label>{t('students.payments.method')}</Label>
            <Select value={method} onValueChange={(val) => setMethod(val as PaymentMethod)}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {PAYMENT_METHODS.map((m) => (
                  <SelectItem key={m} value={m}>
                    {t(paymentMethodTranslations[m])}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="payment-reference">
              {t('students.payments.reference')}
            </Label>
            <Input
              id="payment-reference"
              value={reference}
              onChange={(e) => setReference(e.target.value)}
              placeholder={t('students.payments.referencePlaceholder')}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="payment-notes">{t('students.payments.notes')}</Label>
            <Textarea
              id="payment-notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={t('students.payments.notesPlaceholder')}
              rows={2}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.actions.cancel')}
          </Button>
          <Button
            onClick={() => recordPaymentMutation.mutate()}
            disabled={!canSubmit || recordPaymentMutation.isPending}
          >
            {recordPaymentMutation.isPending
              ? t('students.actions.saving')
              : t('students.payments.recordPayment')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
