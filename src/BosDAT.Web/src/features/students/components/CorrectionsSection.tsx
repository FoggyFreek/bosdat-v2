import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Undo2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { studentLedgerApi } from '@/services/api'
import type { StudentLedgerEntry, LedgerEntryType } from '@/features/students/types'
import { formatDate, cn } from '@/lib/utils'

interface CorrectionsSectionProps {
  studentId: string
}

export function CorrectionsSection({ studentId }: CorrectionsSectionProps) {
  const queryClient = useQueryClient()
  const [showAddForm, setShowAddForm] = useState(false)
  const [reverseDialog, setReverseDialog] = useState<{ open: boolean; entryId?: string }>({ open: false })
  const [reverseReason, setReverseReason] = useState('')

  // Form state
  const [description, setDescription] = useState('')
  const [amount, setAmount] = useState('')
  const [entryType, setEntryType] = useState<LedgerEntryType>('Credit')

  const { data: entries = [], isLoading } = useQuery<StudentLedgerEntry[]>({
    queryKey: ['student-ledger', studentId],
    queryFn: () => studentLedgerApi.getByStudent(studentId),
    enabled: !!studentId,
  })

  const createMutation = useMutation({
    mutationFn: () =>
      studentLedgerApi.create({
        studentId,
        description,
        amount: parseFloat(amount),
        entryType,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['student-ledger', studentId] })
      queryClient.invalidateQueries({ queryKey: ['student-ledger-summary', studentId] })
      resetForm()
    },
  })

  const reverseMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      studentLedgerApi.reverse(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['student-ledger', studentId] })
      queryClient.invalidateQueries({ queryKey: ['student-ledger-summary', studentId] })
      setReverseDialog({ open: false })
      setReverseReason('')
    },
  })

  const resetForm = () => {
    setShowAddForm(false)
    setDescription('')
    setAmount('')
    setEntryType('Credit')
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (description && amount && parseFloat(amount) > 0) {
      createMutation.mutate()
    }
  }

  const handleReverse = () => {
    if (reverseDialog.entryId && reverseReason.trim()) {
      reverseMutation.mutate({ id: reverseDialog.entryId, reason: reverseReason })
    }
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Corrections</h2>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Ledger Entries</CardTitle>
          <Button size="sm" onClick={() => setShowAddForm(!showAddForm)}>
            <Plus className="h-4 w-4 mr-2" />
            Add Correction
          </Button>
        </CardHeader>
        <CardContent>
          {showAddForm && (
            <form onSubmit={handleSubmit} className="mb-6 p-4 bg-muted/50 rounded-lg space-y-4">
              <div className="grid gap-4 md:grid-cols-3">
                <div className="space-y-2">
                  <Label htmlFor="description">Description</Label>
                  <Input
                    id="description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder="e.g., Lesson credit"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="amount">Amount</Label>
                  <Input
                    id="amount"
                    type="number"
                    step="0.01"
                    min="0.01"
                    value={amount}
                    onChange={(e) => setAmount(e.target.value)}
                    placeholder="0.00"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="entryType">Type</Label>
                  <Select value={entryType} onValueChange={(v) => setEntryType(v as LedgerEntryType)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Credit">Credit</SelectItem>
                      <SelectItem value="Debit">Debit</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="flex gap-2">
                <Button type="submit" disabled={createMutation.isPending}>
                  {createMutation.isPending ? 'Adding...' : 'Add Entry'}
                </Button>
                <Button type="button" variant="outline" onClick={resetForm}>
                  Cancel
                </Button>
              </div>
            </form>
          )}

          {entries.length === 0 ? (
            <p className="text-muted-foreground">No corrections yet</p>
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
                          entry.entryType === 'Credit' && 'bg-green-100 text-green-800',
                          entry.entryType === 'Debit' && 'bg-red-100 text-red-800'
                        )}
                      >
                        {entry.entryType}
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
                    <p className="font-medium">{entry.description}</p>
                    <p className="text-sm text-muted-foreground">
                      {formatDate(entry.createdAt)} by {entry.createdByName}
                    </p>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="text-right">
                      <p className={cn(
                        'font-medium',
                        entry.entryType === 'Credit' && 'text-green-600',
                        entry.entryType === 'Debit' && 'text-red-600'
                      )}>
                        {entry.entryType === 'Credit' ? '+' : '-'}{entry.amount.toFixed(2)}
                      </p>
                      {entry.remainingAmount !== entry.amount && (
                        <p className="text-xs text-muted-foreground">
                          Remaining: {entry.remainingAmount.toFixed(2)}
                        </p>
                      )}
                    </div>
                    {entry.status === 'Open' && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 text-orange-600 hover:text-orange-700 hover:bg-orange-50"
                        onClick={() => setReverseDialog({ open: true, entryId: entry.id })}
                      >
                        <Undo2 className="h-4 w-4 mr-1" />
                        Reverse
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={reverseDialog.open} onOpenChange={(open) => setReverseDialog({ open })}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reverse Entry</DialogTitle>
            <DialogDescription>
              This will create an offsetting entry. Please provide a reason for the reversal.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="reverseReason">Reason</Label>
              <Input
                id="reverseReason"
                value={reverseReason}
                onChange={(e) => setReverseReason(e.target.value)}
                placeholder="e.g., Entered in error"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setReverseDialog({ open: false })}>
              Cancel
            </Button>
            <Button
              onClick={handleReverse}
              disabled={!reverseReason.trim() || reverseMutation.isPending}
            >
              {reverseMutation.isPending ? 'Reversing...' : 'Reverse Entry'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
