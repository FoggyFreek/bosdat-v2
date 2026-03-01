import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { invoicesApi } from '@/features/students/api'
import type { Invoice } from '@/features/students/types'
import { formatCurrency } from '@/lib/utils'

interface CreditInvoiceDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  invoice: Invoice
  studentId: string
}

export function CreditInvoiceDialog({
  open,
  onOpenChange,
  invoice,
  studentId,
}: Readonly<CreditInvoiceDialogProps>) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [selectedLineIds, setSelectedLineIds] = useState<number[]>([])
  const [notes, setNotes] = useState('')

  const resetForm = () => {
    setSelectedLineIds([])
    setNotes('')
  }

  const toggleLine = (lineId: number) => {
    setSelectedLineIds((prev) =>
      prev.includes(lineId)
        ? prev.filter((id) => id !== lineId)
        : [...prev, lineId],
    )
  }

  const toggleAll = () => {
    if (selectedLineIds.length === invoice.lines.length) {
      setSelectedLineIds([])
    } else {
      setSelectedLineIds(invoice.lines.map((l) => l.id))
    }
  }

  const creditTotal = useMemo(() => {
    const lines = invoice.lines ?? []
    return lines
      .filter((l) => selectedLineIds.includes(l.id))
      .reduce((sum, l) => sum + l.lineTotal, 0)
  }, [selectedLineIds, invoice.lines])

  const creditVat = useMemo(() => {
    const lines = invoice.lines ?? []
    return lines
      .filter((l) => selectedLineIds.includes(l.id))
      .reduce((sum, l) => sum + l.unitPrice * l.quantity * (l.vatRate / 100), 0)
  }, [selectedLineIds, invoice.lines])

  const createCreditMutation = useMutation({
    mutationFn: () =>
      invoicesApi.createCreditInvoice(invoice.id, {
        selectedLineIds,
        notes: notes || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', 'student', studentId] })
      queryClient.invalidateQueries({ queryKey: ['invoice', invoice.id] })
      resetForm()
      onOpenChange(false)
    },
  })

  const canSubmit = selectedLineIds.length > 0

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{t('students.creditInvoice.title')}</DialogTitle>
          <DialogDescription>
            {t('students.creditInvoice.description', {
              invoiceNumber: invoice.invoiceNumber,
            })}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="rounded-md border">
            <div className="flex items-center border-b px-4 py-2 bg-muted/50">
              <Checkbox
                id="select-all"
                checked={selectedLineIds.length === invoice.lines.length && invoice.lines.length > 0}
                onCheckedChange={toggleAll}
              />
              <label htmlFor="select-all" className="ml-3 text-sm font-medium flex-1 cursor-pointer">
                {t('students.creditInvoice.selectAll')}
              </label>
              <span className="text-sm font-medium w-20 text-right">
                {t('students.invoices.qty')}
              </span>
              <span className="text-sm font-medium w-28 text-right">
                {t('students.invoices.unitPrice')}
              </span>
              <span className="text-sm font-medium w-28 text-right">
                {t('students.invoices.total')}
              </span>
            </div>

            <div className="divide-y max-h-64 overflow-y-auto">
              {(invoice.lines ?? []).map((line) => (
                <div
                  key={line.id}
                  className="flex items-center px-4 py-2 hover:bg-muted/30 cursor-pointer"
                  onClick={() => toggleLine(line.id)}
                >
                  <Checkbox
                    checked={selectedLineIds.includes(line.id)}
                    onCheckedChange={() => toggleLine(line.id)}
                    onClick={(e) => e.stopPropagation()}
                  />
                  <span className="ml-3 text-sm flex-1">{line.description}</span>
                  <span className="text-sm w-20 text-right">{line.quantity}</span>
                  <span className="text-sm w-28 text-right">
                    {formatCurrency(line.unitPrice)}
                  </span>
                  <span className="text-sm w-28 text-right font-medium">
                    {formatCurrency(line.lineTotal)}
                  </span>
                </div>
              ))}
            </div>
          </div>

          {selectedLineIds.length > 0 && (
            <div className="rounded-md border bg-muted/30 p-4 space-y-1">
              <div className="flex justify-between text-sm">
                <span>{t('students.invoices.subtotal')}</span>
                <span className="font-medium">{formatCurrency(creditTotal)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span>{t('students.invoices.vat')}</span>
                <span className="font-medium">{formatCurrency(creditVat)}</span>
              </div>
              <div className="flex justify-between text-sm font-semibold border-t pt-1">
                <span>{t('students.creditInvoice.creditTotal')}</span>
                <span>{formatCurrency(creditTotal + creditVat)}</span>
              </div>
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="credit-notes">{t('students.creditInvoice.notes')}</Label>
            <Textarea
              id="credit-notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={t('students.creditInvoice.notesPlaceholder')}
              rows={2}
            />
          </div>

          <p className="text-sm text-muted-foreground">
            {t('students.creditInvoice.draftNotice')}
          </p>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.actions.cancel')}
          </Button>
          <Button
            onClick={() => createCreditMutation.mutate()}
            disabled={!canSubmit || createCreditMutation.isPending}
          >
            {createCreditMutation.isPending
              ? t('students.actions.saving')
              : t('students.creditInvoice.create')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
