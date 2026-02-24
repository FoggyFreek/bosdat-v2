import { useTranslation } from 'react-i18next'
import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Receipt,
  FileText,
  Printer,
  RefreshCw,
  ChevronDown,
  ChevronUp,
  ExternalLink,
  Download,
  CreditCard,
  MinusCircle,
  Check,
  Wallet,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { invoicesApi, studentTransactionsApi } from '@/features/students/api'
import { RecordPaymentDialog } from '@/features/students/components/invoices/RecordPaymentDialog'
import { CreditInvoiceDialog } from '@/features/students/components/invoices/CreditInvoiceDialog'
import { ApplyCreditDialog } from '@/features/students/components/invoices/ApplyCreditDialog'
import type { Invoice, InvoiceListItem, InvoiceStatus } from '@/features/students/types'
import { invoiceStatusTranslations } from '@/features/students/types'
import { formatCurrency } from '@/lib/utils'
import { formatDate } from '@/lib/datetime-helpers'

interface InvoicesSectionProps {
  readonly studentId: string
}

const statusColors: Record<InvoiceStatus, string> = {
  Draft: 'bg-gray-100 text-gray-800',
  Sent: 'bg-blue-100 text-blue-800',
  Paid: 'bg-green-100 text-green-800',
  Overdue: 'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-500',
}

export function InvoicesSection({ studentId }: InvoicesSectionProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null)
  const [expandedInvoiceId, setExpandedInvoiceId] = useState<string | null>(null)
  const [showPrintDialog, setShowPrintDialog] = useState(false)
  const [showPaymentDialog, setShowPaymentDialog] = useState(false)
  const [showCreditDialog, setShowCreditDialog] = useState(false)
  const [showApplyCreditDialog, setShowApplyCreditDialog] = useState(false)

  const { data: invoices = [], isLoading } = useQuery<InvoiceListItem[]>({
    queryKey: ['invoices', 'student', studentId],
    queryFn: () => invoicesApi.getByStudent(studentId),
    enabled: !!studentId,
  })

  const { data: studentBalance = 0 } = useQuery<number>({
    queryKey: ['student-balance', studentId],
    queryFn: () => studentTransactionsApi.getBalance(studentId),
    enabled: !!studentId,
  })

  const { data: selectedInvoice, isLoading: isLoadingDetail } = useQuery<Invoice>({
    queryKey: ['invoice', selectedInvoiceId],
    queryFn: () => invoicesApi.getById(selectedInvoiceId!),
    enabled: !!selectedInvoiceId,
  })

  const recalculateMutation = useMutation({
    mutationFn: (invoiceId: string) => invoicesApi.recalculate(invoiceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', 'student', studentId] })
      queryClient.invalidateQueries({ queryKey: ['invoice', selectedInvoiceId] })
    },
  })

  const confirmCreditMutation = useMutation({
    mutationFn: (creditInvoiceId: string) => invoicesApi.confirmCreditInvoice(creditInvoiceId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices', 'student', studentId] })
      queryClient.invalidateQueries({ queryKey: ['invoice', selectedInvoiceId] })
      queryClient.invalidateQueries({ queryKey: ['student-transactions', studentId] })
    },
  })

  const canCreateCreditInvoice = (invoice: InvoiceListItem) => {
    return invoice.status !== 'Draft' && invoice.status !== 'Cancelled' && !invoice.isCreditInvoice
  }

  const canApplyCredit = (invoice: Invoice) => {
    return (
      invoice.status !== 'Cancelled' &&
      invoice.status !== 'Paid' &&
      !invoice.isCreditInvoice &&
      invoice.balance > 0 &&
      studentBalance < 0
    )
  }

  const handleViewInvoice = (invoiceId: string) => {
    setSelectedInvoiceId(invoiceId)
    setShowPrintDialog(true)
  }

  const handleToggleExpand = (invoiceId: string) => {
    setExpandedInvoiceId(expandedInvoiceId === invoiceId ? null : invoiceId)
    if (expandedInvoiceId !== invoiceId) {
      setSelectedInvoiceId(invoiceId)
    }
  }

  const handlePrint = () => {
    window.print()
  }

  const canRecalculate = (status: InvoiceStatus) => {
    return status === 'Draft' || status === 'Sent' || status === 'Overdue'
  }

  const canRecordPayment = (invoice: Invoice) => {
    return invoice.status !== 'Cancelled' && invoice.status !== 'Paid' && invoice.balance > 0
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h2 className="text-2xl font-bold">{t('students.sections.invoices')}</h2>
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
      <h2 className="text-2xl font-bold">{t('students.sections.invoices')}</h2>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Receipt className="h-5 w-5" />
            {t('students.invoices.invoiceHistory')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {invoices.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <Receipt className="h-12 w-12 text-muted-foreground/50 mb-4" />
              <p className="text-lg font-medium text-muted-foreground">{t('students.invoices.noInvoices')}</p>
              <p className="text-sm text-muted-foreground mt-1">
                {t('students.invoices.noInvoicesDesc')}
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              {invoices.map((invoice) => (
                <div
                  key={invoice.id}
                  className="border rounded-lg overflow-hidden"
                >
                  <button
                    type="button"
                    className="flex items-center justify-between p-4 w-full text-left cursor-pointer hover:bg-muted/50"
                    onClick={() => handleToggleExpand(invoice.id)}
                  >
                    <div className="flex items-center gap-4">
                      <FileText className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{invoice.invoiceNumber}</span>
                          {invoice.isCreditInvoice && (
                            <Badge className="bg-orange-100 text-orange-800 text-xs">
                              {t('students.creditInvoice.badge')}
                            </Badge>
                          )}
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {invoice.isCreditInvoice && invoice.originalInvoiceNumber
                            ? t('students.creditInvoice.forInvoice', { invoiceNumber: invoice.originalInvoiceNumber })
                            : (invoice.description || formatDate(invoice.issueDate))}
                        </div>
                      </div>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="text-right">
                        <div className="font-medium">{formatCurrency(invoice.total)}</div>
                        {invoice.balance > 0 && (
                          <div className="text-sm text-muted-foreground">
                            {t('students.invoices.balance')}: {formatCurrency(invoice.balance)}
                          </div>
                        )}
                      </div>
                      <Badge className={statusColors[invoice.status]}>{t(invoiceStatusTranslations[invoice.status])}</Badge>
                      {expandedInvoiceId === invoice.id ? (
                        <ChevronUp className="h-5 w-5" />
                      ) : (
                        <ChevronDown className="h-5 w-5" />
                      )}
                    </div>
                  </button>

                  {expandedInvoiceId === invoice.id && selectedInvoice && (
                    <div className="border-t p-4 bg-muted/30">
                      {isLoadingDetail ? (
                        <div className="flex items-center justify-center py-4">
                          <div className="animate-spin h-6 w-6 border-4 border-primary border-t-transparent rounded-full" />
                        </div>
                      ) : (
                        <div className="space-y-4">
                          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                            <div>
                              <div className="text-muted-foreground">{t('students.invoices.issueDate')}</div>
                              <div>{formatDate(selectedInvoice.issueDate)}</div>
                            </div>
                            <div>
                              <div className="text-muted-foreground">{t('students.invoices.dueDate')}</div>
                              <div>{formatDate(selectedInvoice.dueDate)}</div>
                            </div>
                            {selectedInvoice.periodStart && selectedInvoice.periodEnd && (
                              <>
                                <div>
                                  <div className="text-muted-foreground">{t('students.invoices.period')}</div>
                                  <div>
                                    {formatDate(selectedInvoice.periodStart)} -{' '}
                                    {formatDate(selectedInvoice.periodEnd)}
                                  </div>
                                </div>
                                <div>
                                  <div className="text-muted-foreground">{t('students.invoices.billingType')}</div>
                                  <div>{selectedInvoice.periodType}</div>
                                </div>
                              </>
                            )}
                          </div>

                          <div className="border rounded-md overflow-hidden">
                            <table className="w-full text-sm">
                              <thead className="bg-muted">
                                <tr>
                                  <th className="text-left p-2">{t('students.invoices.description')}</th>
                                  <th className="text-right p-2">{t('students.invoices.qty')}</th>
                                  <th className="text-right p-2">{t('students.invoices.unitPrice')}</th>
                                  <th className="text-right p-2"></th>
                                  <th className="text-right p-2">{t('common.entities.invoice')}</th>
                                </tr>
                              </thead>
                              <tbody>
                                {selectedInvoice.lines.map((line) => (
                                  <tr key={line.id} className="border-t">
                                    <td className="p-2">{line.description}</td>
                                    <td className="text-right p-2">{line.quantity}</td>
                                    <td className="text-right p-2">{formatCurrency(line.unitPrice)}</td>
                                    <td className="text-right p-2"></td>
                                    <td className="text-right p-2">{formatCurrency(line.lineTotal)}</td>
                                  </tr>
                                ))}
                              </tbody>
                              <tfoot className="border-t bg-muted/50">
                                <tr>
                                  <td colSpan={4} className="text-right p-2 font-medium">
                                    {t('students.invoices.subtotal')}
                                  </td>
                                  <td className="text-right p-2">
                                    {formatCurrency(selectedInvoice.subtotal)}
                                  </td>
                                </tr>
                                <tr>
                                  <td colSpan={4} className="text-right p-2 font-medium">
                                    {t('students.invoices.vat')}
                                  </td>
                                  <td className="text-right p-2">
                                    {formatCurrency(selectedInvoice.vatAmount)}
                                  </td>
                                </tr>
                                <tr className="font-bold">
                                  <td colSpan={4} className="text-right p-2">
                                    {t('students.invoices.total')}
                                  </td>
                                  <td className="text-right p-2">
                                    {formatCurrency(
                                      selectedInvoice.total)}
                                  </td>
                                </tr>
                              </tfoot>
                            </table>
                          </div>

                          {selectedInvoice.isCreditInvoice && selectedInvoice.originalInvoiceNumber && (
                            <div className="rounded-md bg-orange-50 border border-orange-200 p-3 text-sm">
                              <span className="font-medium">{t('students.creditInvoice.referencesInvoice')}: </span>
                              <span>{selectedInvoice.originalInvoiceNumber}</span>
                            </div>
                          )}

                          <div className="flex justify-end gap-2">
                            {selectedInvoice.isCreditInvoice && selectedInvoice.status === 'Draft' && (
                              <Button
                                size="sm"
                                onClick={() => confirmCreditMutation.mutate(selectedInvoice.id)}
                                disabled={confirmCreditMutation.isPending}
                              >
                                <Check className="h-4 w-4 mr-2" />
                                {confirmCreditMutation.isPending
                                  ? t('students.actions.saving')
                                  : t('students.creditInvoice.confirm')}
                              </Button>
                            )}
                            {canApplyCredit(selectedInvoice) && (
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => setShowApplyCreditDialog(true)}
                              >
                                <Wallet className="h-4 w-4 mr-2" />
                                {t('students.creditBalance.applyCredit')}
                              </Button>
                            )}
                            {canRecordPayment(selectedInvoice) && (
                              <Button
                                size="sm"
                                onClick={() => setShowPaymentDialog(true)}
                              >
                                <CreditCard className="h-4 w-4 mr-2" />
                                {t('students.payments.recordPayment')}
                              </Button>
                            )}
                            {canCreateCreditInvoice(invoice) && (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setShowCreditDialog(true)}
                              >
                                <MinusCircle className="h-4 w-4 mr-2" />
                                {t('students.creditInvoice.create')}
                              </Button>
                            )}
                            {canRecalculate(selectedInvoice.status) && (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => recalculateMutation.mutate(selectedInvoice.id)}
                                disabled={recalculateMutation.isPending}
                              >
                                <RefreshCw
                                  className={`h-4 w-4 mr-2 ${recalculateMutation.isPending ? 'animate-spin' : ''}`}
                                />
                                {t('students.invoices.recalculate')}
                              </Button>
                            )}
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleViewInvoice(selectedInvoice.id)}
                            >
                              <ExternalLink className="h-4 w-4 mr-2" />
                              {t('students.invoices.viewFullInvoice')}
                            </Button>
                          </div>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={showPrintDialog} onOpenChange={setShowPrintDialog}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{t('students.invoices.invoice')} {selectedInvoice?.invoiceNumber}</DialogTitle>
            <DialogDescription>
              {t('students.invoices.printOrDownload')}
            </DialogDescription>
          </DialogHeader>
          {selectedInvoice && (
            <InvoicePrintView invoice={selectedInvoice} onPrint={handlePrint} />
          )}
        </DialogContent>
      </Dialog>

      {selectedInvoice && (
        <RecordPaymentDialog
          open={showPaymentDialog}
          onOpenChange={setShowPaymentDialog}
          invoiceId={selectedInvoice.id}
          invoiceNumber={selectedInvoice.invoiceNumber}
          remainingBalance={selectedInvoice.balance}
          studentId={studentId}
        />
      )}

      {selectedInvoice && !selectedInvoice.isCreditInvoice && (
        <CreditInvoiceDialog
          open={showCreditDialog}
          onOpenChange={setShowCreditDialog}
          invoice={selectedInvoice}
          studentId={studentId}
        />
      )}

      {selectedInvoice && canApplyCredit(selectedInvoice) && (
        <ApplyCreditDialog
          open={showApplyCreditDialog}
          onOpenChange={setShowApplyCreditDialog}
          invoiceId={selectedInvoice.id}
          studentId={studentId}
          availableCredit={Math.abs(studentBalance)}
          remainingBalance={selectedInvoice.balance}
        />
      )}
    </div>
  )
}

interface InvoicePrintViewProps {
  readonly invoice: Invoice
  readonly onPrint: () => void
}

function InvoicePrintView({ invoice, onPrint }: InvoicePrintViewProps) {
  const { t } = useTranslation()
  const { data: schoolInfo } = useQuery({
    queryKey: ['school-billing-info'],
    queryFn: () => invoicesApi.getSchoolBillingInfo(),
  })

  const totalOwed =
    invoice.total

  return (
    <div className="space-y-6 print:p-8">
      <div className="flex justify-between items-start print:hidden">
        <div />
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={onPrint}>
            <Printer className="h-4 w-4 mr-2" />
            {t('common.actions.download')}
          </Button>
          <Button variant="outline" size="sm">
            <Download className="h-4 w-4 mr-2" />
            {t('students.invoices.downloadPdf')}
          </Button>
        </div>
      </div>

      <div className="border rounded-lg p-6 space-y-6 print:border-0">
        {/* Header */}
        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-2xl font-bold">{schoolInfo?.name || t('students.invoices.musicSchool')}</h1>
            {schoolInfo && (
              <div className="text-sm text-muted-foreground mt-1">
                {schoolInfo.address && <div>{schoolInfo.address}</div>}
                {schoolInfo.postalCode && schoolInfo.city && (
                  <div>
                    {schoolInfo.postalCode} {schoolInfo.city}
                  </div>
                )}
                {schoolInfo.phone && <div>{t('students.invoices.tel')}: {schoolInfo.phone}</div>}
                {schoolInfo.email && <div>{t('students.invoices.email')}: {schoolInfo.email}</div>}
                {schoolInfo.kvkNumber && <div>{t('students.invoices.kvk')}: {schoolInfo.kvkNumber}</div>}
                {schoolInfo.btwNumber && <div>{t('students.invoices.btw')}: {schoolInfo.btwNumber}</div>}
              </div>
            )}
          </div>
          <div className="text-right">
            <h2 className="text-xl font-bold">
              {invoice.isCreditInvoice
                ? t('students.creditInvoice.creditInvoice')
                : t('students.invoices.invoice')}
            </h2>
            <div className="text-sm mt-1">
              <div className="font-medium">#{invoice.invoiceNumber}</div>
              {invoice.description && (
                <div className="text-muted-foreground">{invoice.description}</div>
              )}
              {invoice.isCreditInvoice && invoice.originalInvoiceNumber && (
                <div className="text-muted-foreground mt-1">
                  {t('students.creditInvoice.referencesInvoice')}: {invoice.originalInvoiceNumber}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Billing Info */}
        <div className="grid grid-cols-2 gap-6">
          <div>
            <h3 className="font-medium mb-2">{t('students.invoices.billTo')}:</h3>
            {invoice.billingContact && (
              <div className="text-sm">
                <div>{invoice.billingContact.name}</div>
                {invoice.billingContact.address && <div>{invoice.billingContact.address}</div>}
                {invoice.billingContact.postalCode && invoice.billingContact.city && (
                  <div>
                    {invoice.billingContact.postalCode} {invoice.billingContact.city}
                  </div>
                )}
                {invoice.billingContact.email && <div>{invoice.billingContact.email}</div>}
              </div>
            )}
          </div>
          <div className="text-right">
            <div className="text-sm space-y-1">
              <div>
                <span className="text-muted-foreground">{t('students.invoices.issueDate')}:</span>{' '}
                {formatDate(invoice.issueDate)}
              </div>
              <div>
                <span className="text-muted-foreground">{t('students.invoices.dueDate')}:</span>{' '}
                {formatDate(invoice.dueDate)}
              </div>
              {invoice.periodStart && invoice.periodEnd && (
                <div>
                  <span className="text-muted-foreground">{t('students.invoices.period')}:</span>{' '}
                  {formatDate(invoice.periodStart)} - {formatDate(invoice.periodEnd)}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Line Items */}
        <table className="w-full text-sm">
          <thead className="border-b">
            <tr>
              <th className="text-left py-2">{t('students.invoices.description')}</th>
              <th className="text-right py-2">{t('students.invoices.qty')}</th>
              <th className="text-right py-2">{t('students.invoices.unitPrice')}</th>
              <th className="text-right py-2"></th>
              <th className="text-right py-2">{t('students.invoices.total')}</th>
            </tr>
          </thead>
          <tbody>
            {invoice.lines.map((line) => (
              <tr key={line.id} className="border-b">
                <td className="py-2">{line.description}</td>
                <td className="text-right py-2">{line.quantity}</td>
                <td className="text-right py-2">{formatCurrency(line.unitPrice)}</td>
                <td className="text-right py-2"></td>
                <td className="text-right py-2">{formatCurrency(line.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>



        {/* Totals */}
        <div className="flex justify-end">
          <div className="w-64 space-y-2 text-sm">
            <div className="flex justify-between">
              <span>{t('students.invoices.subtotal')}</span>
              <span>{formatCurrency(invoice.subtotal)}</span>
            </div>
          </div>
        </div>
        
        <div className="flex justify-end">
          <div className="w-64 space-y-2 text-sm">
            <div className="flex justify-between">
              <span>{t('students.invoices.total')}</span>
              <span>{formatCurrency(invoice.subtotal)}</span>
            </div>
            <div className="flex justify-between">
              <span>{t('students.invoices.vat')} ({schoolInfo?.vatRate || 21}%)</span>
              <span>{formatCurrency(invoice.vatAmount)}</span>
            </div>
            <div className="flex justify-between font-bold text-lg border-t pt-2">
              <span>
                {invoice.isCreditInvoice
                  ? t('students.invoices.totalCredit')
                  : t('students.invoices.totalDue')}
              </span>
              <span>{formatCurrency(totalOwed)}</span>
            </div>
            {!invoice.isCreditInvoice && invoice.amountPaid > 0 && (
              <>
                <div className="flex justify-between text-green-600">
                  <span>{t('students.invoices.amountPaid')}</span>
                  <span>-{formatCurrency(invoice.amountPaid)}</span>
                </div>
                <div className="flex justify-between font-bold">
                  <span>{t('students.invoices.balance')}</span>
                  <span>{formatCurrency(invoice.balance)}</span>
                </div>
              </>
            )}
          </div>
        </div>

        {/* Payment Instructions / Credit Note */}
        <div className="border-t pt-4 text-sm">
          {invoice.isCreditInvoice ? (
            <>
              <h4 className="font-medium mb-2">{t('students.invoices.creditNote')}</h4>
              <p className="text-muted-foreground">
                {t('students.invoices.creditNoteText')}
              </p>
            </>
          ) : (
            <>
              <h4 className="font-medium mb-2">{t('students.invoices.paymentInstructions')}</h4>
              <p className="text-muted-foreground">
                {t('students.invoices.paymentInstructionsText', { invoiceNumber: invoice.invoiceNumber })}
              </p>
              {schoolInfo?.iban && (
                <div className="mt-2">
                  <span className="text-muted-foreground">{t('students.invoices.iban')}:</span>{' '}
                  <span className="font-mono">{schoolInfo.iban}</span>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}
