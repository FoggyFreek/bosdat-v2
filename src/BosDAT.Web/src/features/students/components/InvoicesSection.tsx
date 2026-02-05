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
import { invoicesApi } from '@/features/students/api'
import type { Invoice, InvoiceListItem, InvoiceStatus } from '@/features/students/types'
import { formatCurrency } from '@/lib/utils'
import { formatDate } from '@/lib/datetime-helpers'

interface InvoicesSectionProps {
  studentId: string
}

const statusColors: Record<InvoiceStatus, string> = {
  Draft: 'bg-gray-100 text-gray-800',
  Sent: 'bg-blue-100 text-blue-800',
  Paid: 'bg-green-100 text-green-800',
  Overdue: 'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-500',
}

export function InvoicesSection({ studentId }: InvoicesSectionProps) {
  const queryClient = useQueryClient()
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null)
  const [expandedInvoiceId, setExpandedInvoiceId] = useState<string | null>(null)
  const [showPrintDialog, setShowPrintDialog] = useState(false)

  const { data: invoices = [], isLoading } = useQuery<InvoiceListItem[]>({
    queryKey: ['invoices', 'student', studentId],
    queryFn: () => invoicesApi.getByStudent(studentId),
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

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h2 className="text-2xl font-bold">Invoices</h2>
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
      <h2 className="text-2xl font-bold">Invoices</h2>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Receipt className="h-5 w-5" />
            Invoice History
          </CardTitle>
        </CardHeader>
        <CardContent>
          {invoices.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <Receipt className="h-12 w-12 text-muted-foreground/50 mb-4" />
              <p className="text-lg font-medium text-muted-foreground">No invoices yet</p>
              <p className="text-sm text-muted-foreground mt-1">
                Invoices will appear here once they are generated for this student.
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              {invoices.map((invoice) => (
                <div
                  key={invoice.id}
                  className="border rounded-lg overflow-hidden"
                >
                  <div
                    className="flex items-center justify-between p-4 cursor-pointer hover:bg-muted/50"
                    onClick={() => handleToggleExpand(invoice.id)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' || e.key === ' ') {
                        handleToggleExpand(invoice.id)
                      }
                    }}
                    role="button"
                    tabIndex={0}
                  >
                    <div className="flex items-center gap-4">
                      <FileText className="h-5 w-5 text-muted-foreground" />
                      <div>
                        <div className="font-medium">{invoice.invoiceNumber}</div>
                        <div className="text-sm text-muted-foreground">
                          {invoice.description || formatDate(invoice.issueDate)}
                        </div>
                      </div>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="text-right">
                        <div className="font-medium">{formatCurrency(invoice.total)}</div>
                        {invoice.balance > 0 && (
                          <div className="text-sm text-muted-foreground">
                            Balance: {formatCurrency(invoice.balance)}
                          </div>
                        )}
                      </div>
                      <Badge className={statusColors[invoice.status]}>{invoice.status}</Badge>
                      {expandedInvoiceId === invoice.id ? (
                        <ChevronUp className="h-5 w-5" />
                      ) : (
                        <ChevronDown className="h-5 w-5" />
                      )}
                    </div>
                  </div>

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
                              <div className="text-muted-foreground">Issue Date</div>
                              <div>{formatDate(selectedInvoice.issueDate)}</div>
                            </div>
                            <div>
                              <div className="text-muted-foreground">Due Date</div>
                              <div>{formatDate(selectedInvoice.dueDate)}</div>
                            </div>
                            {selectedInvoice.periodStart && selectedInvoice.periodEnd && (
                              <>
                                <div>
                                  <div className="text-muted-foreground">Period</div>
                                  <div>
                                    {formatDate(selectedInvoice.periodStart)} -{' '}
                                    {formatDate(selectedInvoice.periodEnd)}
                                  </div>
                                </div>
                                <div>
                                  <div className="text-muted-foreground">Billing Type</div>
                                  <div>{selectedInvoice.periodType}</div>
                                </div>
                              </>
                            )}
                          </div>

                          <div className="border rounded-md overflow-hidden">
                            <table className="w-full text-sm">
                              <thead className="bg-muted">
                                <tr>
                                  <th className="text-left p-2">Description</th>
                                  <th className="text-right p-2">Qty</th>
                                  <th className="text-right p-2">Unit Price</th>
                                  <th className="text-right p-2">VAT</th>
                                  <th className="text-right p-2">Total</th>
                                </tr>
                              </thead>
                              <tbody>
                                {selectedInvoice.lines.map((line) => (
                                  <tr key={line.id} className="border-t">
                                    <td className="p-2">{line.description}</td>
                                    <td className="text-right p-2">{line.quantity}</td>
                                    <td className="text-right p-2">{formatCurrency(line.unitPrice)}</td>
                                    <td className="text-right p-2">{line.vatRate}%</td>
                                    <td className="text-right p-2">{formatCurrency(line.lineTotal)}</td>
                                  </tr>
                                ))}
                              </tbody>
                              <tfoot className="border-t bg-muted/50">
                                <tr>
                                  <td colSpan={4} className="text-right p-2 font-medium">
                                    Subtotal
                                  </td>
                                  <td className="text-right p-2">
                                    {formatCurrency(selectedInvoice.subtotal)}
                                  </td>
                                </tr>
                                <tr>
                                  <td colSpan={4} className="text-right p-2 font-medium">
                                    VAT
                                  </td>
                                  <td className="text-right p-2">
                                    {formatCurrency(selectedInvoice.vatAmount)}
                                  </td>
                                </tr>
                                {selectedInvoice.ledgerCreditsApplied > 0 && (
                                  <tr>
                                    <td colSpan={4} className="text-right p-2 font-medium text-green-600">
                                      Credits Applied
                                    </td>
                                    <td className="text-right p-2 text-green-600">
                                      -{formatCurrency(selectedInvoice.ledgerCreditsApplied)}
                                    </td>
                                  </tr>
                                )}
                                {selectedInvoice.ledgerDebitsApplied > 0 && (
                                  <tr>
                                    <td colSpan={4} className="text-right p-2 font-medium text-red-600">
                                      Debits Applied
                                    </td>
                                    <td className="text-right p-2 text-red-600">
                                      +{formatCurrency(selectedInvoice.ledgerDebitsApplied)}
                                    </td>
                                  </tr>
                                )}
                                <tr className="font-bold">
                                  <td colSpan={4} className="text-right p-2">
                                    Total
                                  </td>
                                  <td className="text-right p-2">
                                    {formatCurrency(
                                      selectedInvoice.total +
                                        selectedInvoice.ledgerDebitsApplied -
                                        selectedInvoice.ledgerCreditsApplied
                                    )}
                                  </td>
                                </tr>
                              </tfoot>
                            </table>
                          </div>

                          {selectedInvoice.ledgerApplications.length > 0 && (
                            <div>
                              <h4 className="font-medium mb-2">Applied Corrections</h4>
                              <div className="space-y-1 text-sm">
                                {selectedInvoice.ledgerApplications.map((app) => (
                                  <div
                                    key={app.id}
                                    className="flex justify-between items-center p-2 bg-muted/30 rounded"
                                  >
                                    <span>
                                      {app.correctionRefName}: {app.description}
                                    </span>
                                    <span
                                      className={
                                        app.entryType === 'Credit' ? 'text-green-600' : 'text-red-600'
                                      }
                                    >
                                      {app.entryType === 'Credit' ? '-' : '+'}
                                      {formatCurrency(app.appliedAmount)}
                                    </span>
                                  </div>
                                ))}
                              </div>
                            </div>
                          )}

                          <div className="flex justify-end gap-2">
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
                                Recalculate
                              </Button>
                            )}
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleViewInvoice(selectedInvoice.id)}
                            >
                              <ExternalLink className="h-4 w-4 mr-2" />
                              View Full Invoice
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
            <DialogTitle>Invoice {selectedInvoice?.invoiceNumber}</DialogTitle>
            <DialogDescription>
              Print or download this invoice for your records.
            </DialogDescription>
          </DialogHeader>
          {selectedInvoice && (
            <InvoicePrintView invoice={selectedInvoice} onPrint={handlePrint} />
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}

interface InvoicePrintViewProps {
  invoice: Invoice
  onPrint: () => void
}

function InvoicePrintView({ invoice, onPrint }: InvoicePrintViewProps) {
  const { data: schoolInfo } = useQuery({
    queryKey: ['school-billing-info'],
    queryFn: () => invoicesApi.getSchoolBillingInfo(),
  })

  const totalOwed =
    invoice.total + invoice.ledgerDebitsApplied - invoice.ledgerCreditsApplied

  return (
    <div className="space-y-6 print:p-8">
      <div className="flex justify-between items-start print:hidden">
        <div />
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={onPrint}>
            <Printer className="h-4 w-4 mr-2" />
            Print
          </Button>
          <Button variant="outline" size="sm">
            <Download className="h-4 w-4 mr-2" />
            Download PDF
          </Button>
        </div>
      </div>

      <div className="border rounded-lg p-6 space-y-6 print:border-0">
        {/* Header */}
        <div className="flex justify-between items-start">
          <div>
            <h1 className="text-2xl font-bold">{schoolInfo?.name || 'Music School'}</h1>
            {schoolInfo && (
              <div className="text-sm text-muted-foreground mt-1">
                {schoolInfo.address && <div>{schoolInfo.address}</div>}
                {schoolInfo.postalCode && schoolInfo.city && (
                  <div>
                    {schoolInfo.postalCode} {schoolInfo.city}
                  </div>
                )}
                {schoolInfo.phone && <div>Tel: {schoolInfo.phone}</div>}
                {schoolInfo.email && <div>Email: {schoolInfo.email}</div>}
                {schoolInfo.kvkNumber && <div>KvK: {schoolInfo.kvkNumber}</div>}
              </div>
            )}
          </div>
          <div className="text-right">
            <h2 className="text-xl font-bold">INVOICE</h2>
            <div className="text-sm mt-1">
              <div className="font-medium">#{invoice.invoiceNumber}</div>
              {invoice.description && (
                <div className="text-muted-foreground">{invoice.description}</div>
              )}
            </div>
          </div>
        </div>

        {/* Billing Info */}
        <div className="grid grid-cols-2 gap-6">
          <div>
            <h3 className="font-medium mb-2">Bill To:</h3>
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
                <span className="text-muted-foreground">Issue Date:</span>{' '}
                {formatDate(invoice.issueDate)}
              </div>
              <div>
                <span className="text-muted-foreground">Due Date:</span>{' '}
                {formatDate(invoice.dueDate)}
              </div>
              {invoice.periodStart && invoice.periodEnd && (
                <div>
                  <span className="text-muted-foreground">Period:</span>{' '}
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
              <th className="text-left py-2">Description</th>
              <th className="text-right py-2">Qty</th>
              <th className="text-right py-2">Unit Price</th>
              <th className="text-right py-2">VAT</th>
              <th className="text-right py-2">Total</th>
            </tr>
          </thead>
          <tbody>
            {invoice.lines.map((line) => (
              <tr key={line.id} className="border-b">
                <td className="py-2">{line.description}</td>
                <td className="text-right py-2">{line.quantity}</td>
                <td className="text-right py-2">{formatCurrency(line.unitPrice)}</td>
                <td className="text-right py-2">{line.vatRate}%</td>
                <td className="text-right py-2">{formatCurrency(line.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {/* Totals */}
        <div className="flex justify-end">
          <div className="w-64 space-y-2 text-sm">
            <div className="flex justify-between">
              <span>Subtotal</span>
              <span>{formatCurrency(invoice.subtotal)}</span>
            </div>
            <div className="flex justify-between">
              <span>VAT ({schoolInfo?.vatRate || 21}%)</span>
              <span>{formatCurrency(invoice.vatAmount)}</span>
            </div>
            {invoice.ledgerCreditsApplied > 0 && (
              <div className="flex justify-between text-green-600">
                <span>Credits Applied</span>
                <span>-{formatCurrency(invoice.ledgerCreditsApplied)}</span>
              </div>
            )}
            {invoice.ledgerDebitsApplied > 0 && (
              <div className="flex justify-between text-red-600">
                <span>Outstanding Charges</span>
                <span>+{formatCurrency(invoice.ledgerDebitsApplied)}</span>
              </div>
            )}
            <div className="flex justify-between font-bold text-lg border-t pt-2">
              <span>Total Due</span>
              <span>{formatCurrency(totalOwed)}</span>
            </div>
            {invoice.amountPaid > 0 && (
              <>
                <div className="flex justify-between text-green-600">
                  <span>Amount Paid</span>
                  <span>-{formatCurrency(invoice.amountPaid)}</span>
                </div>
                <div className="flex justify-between font-bold">
                  <span>Balance</span>
                  <span>{formatCurrency(invoice.balance)}</span>
                </div>
              </>
            )}
          </div>
        </div>

        {/* Payment Instructions */}
        <div className="border-t pt-4 text-sm">
          <h4 className="font-medium mb-2">Payment Instructions</h4>
          <p className="text-muted-foreground">
            Please transfer the amount to the following bank account, using invoice number{' '}
            <strong>{invoice.invoiceNumber}</strong> as the payment reference.
          </p>
          {schoolInfo?.iban && (
            <div className="mt-2">
              <span className="text-muted-foreground">IBAN:</span>{' '}
              <span className="font-mono">{schoolInfo.iban}</span>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
