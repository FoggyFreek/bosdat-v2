import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, X, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import { instrumentsApi } from '@/services/api'
import { useFormDirty } from '@/context/FormDirtyContext'
import { cn } from '@/lib/utils'
import type { Instrument, InstrumentCategory } from '@/features/instruments/types'

const categories: InstrumentCategory[] = ['String', 'Percussion', 'Vocal', 'Keyboard', 'Wind', 'Brass', 'Electronic', 'Other']

export function InstrumentsSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [formData, setFormData] = useState<{ name: string; category: InstrumentCategory; isActive: boolean }>({ name: '', category: 'Other', isActive: true })
  const { setIsDirty } = useFormDirty()

  const { data: instruments = [], isLoading } = useQuery<Instrument[]>({
    queryKey: ['instruments'],
    queryFn: () => instrumentsApi.getAll(),
  })

  const createMutation = useMutation({
    mutationFn: (data: { name: string; category: string }) => instrumentsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['instruments'] })
      setShowAdd(false)
      setFormData({ name: '', category: 'Other', isActive: true })
      setIsDirty(false)
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: { name: string; category: string; isActive: boolean } }) =>
      instrumentsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['instruments'] })
      setEditId(null)
      setIsDirty(false)
    },
  })

  const handleShowAdd = () => {
    setShowAdd(true)
    setIsDirty(true)
  }

  const handleCancelAdd = () => {
    setShowAdd(false)
    setFormData({ name: '', category: 'Other', isActive: true })
    setIsDirty(false)
  }

  const handleStartEdit = (instrument: Instrument) => {
    setEditId(instrument.id)
    setFormData({ name: instrument.name, category: instrument.category, isActive: instrument.isActive })
    setIsDirty(true)
  }

  const handleCancelEdit = () => {
    setEditId(null)
    setIsDirty(false)
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Instruments</CardTitle>
          <CardDescription>Manage available instruments for lessons</CardDescription>
        </div>
        <Button size="sm" onClick={handleShowAdd}>
          <Plus className="h-4 w-4 mr-2" />
          Add
        </Button>
      </CardHeader>
      <CardContent>
        {showAdd && (
          <div className="flex gap-2 mb-4 p-4 bg-muted/50 rounded-lg">
            <Input
              placeholder="Instrument name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              className="flex-1"
            />
            <Select
              value={formData.category}
              onValueChange={(value) => setFormData({ ...formData, category: value as InstrumentCategory })}
            >
              <SelectTrigger className="w-[150px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {categories.map((cat) => (
                  <SelectItem key={cat} value={cat}>{cat}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button onClick={() => createMutation.mutate(formData)} disabled={!formData.name || createMutation.isPending}>
              <Check className="h-4 w-4" />
            </Button>
            <Button variant="ghost" onClick={handleCancelAdd}>
              <X className="h-4 w-4" />
            </Button>
          </div>
        )}

        {isLoading && (
          <div className="flex items-center justify-center py-8">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        )}

        {!isLoading && instruments.length === 0 && (
          <p className="text-muted-foreground">No instruments configured</p>
        )}

        {!isLoading && instruments.length > 0 && (
          <div className="divide-y">
            {instruments.map((instrument) => (
              <div key={instrument.id} className="flex items-center justify-between py-2">
                {editId === instrument.id ? (
                  <div className="flex gap-2 flex-1">
                    <Input
                      value={formData.name}
                      onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                      className="flex-1"
                    />
                    <Checkbox
                      checked={formData.isActive}
                      onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked as boolean })}
                      className="h-4 w-4 shrink-0"
                    />
                    <Select
                      value={formData.category}
                      onValueChange={(value) => setFormData({ ...formData, category: value as InstrumentCategory })}
                    >
                      <SelectTrigger className="w-[150px]">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {categories.map((cat) => (
                          <SelectItem key={cat} value={cat}>{cat}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Button size="icon" onClick={() => updateMutation.mutate({ id: instrument.id, data: formData })}>
                      <Check className="h-4 w-4" />
                    </Button>
                    <Button size="icon" variant="ghost" onClick={handleCancelEdit}>
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ) : (
                  <>
                    <div>
                      <p className="font-medium">{instrument.name}</p>
                      <p className="text-sm text-muted-foreground">{instrument.category}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className={cn(
                        'inline-flex items-center rounded-full px-2 py-0.5 text-xs',
                        instrument.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                      )}>
                        {instrument.isActive ? 'Active' : 'Inactive'}
                      </span>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleStartEdit(instrument)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </div>
                  </>
                )}
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
