import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
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
  remainingBalance: number
}

export function ApplyCreditDialog({
  open,
  onOpenChange,
  invoiceId,
  studentId,
  remainingBalance,
}: Readonly<ApplyCreditDialogProps>) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const applyCreditMutation = useMutation({
    mutationFn: () => invoicesApi.applyCreditInvoices(invoiceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', 'student', studentId] })
      queryClient.invalidateQueries({ queryKey: ['invoice', invoiceId] })
      queryClient.invalidateQueries({ queryKey: ['student-transactions', studentId] })
      queryClient.invalidateQueries({ queryKey: ['student-balance', studentId] })
      onOpenChange(false)
    },
  })

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('students.creditBalance.title')}</DialogTitle>
          <DialogDescription>
            {t('students.creditBalance.description', {
              balance: formatCurrency(remainingBalance),
            })}
          </DialogDescription>
        </DialogHeader>

        <div className="rounded-md bg-muted p-3 text-sm">
          <div className="text-muted-foreground">{t('students.creditBalance.invoiceBalance')}</div>
          <div className="font-medium">{formatCurrency(remainingBalance)}</div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.actions.cancel')}
          </Button>
          <Button
            onClick={() => applyCreditMutation.mutate()}
            disabled={applyCreditMutation.isPending}
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
