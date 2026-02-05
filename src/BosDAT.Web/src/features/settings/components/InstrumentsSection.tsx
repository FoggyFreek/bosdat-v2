import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { instrumentsApi } from '@/features/instruments/api'
import { useFormDirty } from '@/context/FormDirtyContext'
import type { Instrument, InstrumentCategory } from '@/features/instruments/types'
import { InstrumentForm } from './InstrumentForm'
import { InstrumentListItem } from './InstrumentListItem'

interface InstrumentFormData {
  name: string
  category: InstrumentCategory
  isActive: boolean
}

const initialFormData: InstrumentFormData = {
  name: '',
  category: 'Other',
  isActive: true,
}

export function InstrumentsSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [formData, setFormData] = useState<InstrumentFormData>(initialFormData)
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
      setFormData(initialFormData)
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
    setFormData(initialFormData)
    setIsDirty(false)
  }

  const handleStartEdit = (instrument: Instrument) => {
    setEditId(instrument.id)
    setFormData({
      name: instrument.name,
      category: instrument.category,
      isActive: instrument.isActive,
    })
    setIsDirty(true)
  }

  const handleCancelEdit = () => {
    setEditId(null)
    setFormData(initialFormData)
    setIsDirty(false)
  }

  const handleCreateSubmit = () => {
    createMutation.mutate(formData)
  }

  const handleUpdateSubmit = () => {
    if (editId !== null) {
      updateMutation.mutate({ id: editId, data: formData })
    }
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
          <div className="mb-4 p-4 bg-muted/50 rounded-lg">
            <InstrumentForm
              formData={formData}
              isPending={createMutation.isPending}
              onCancel={handleCancelAdd}
              onFormDataChange={setFormData}
              onSubmit={handleCreateSubmit}
            />
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
              <InstrumentListItem
                key={instrument.id}
                formData={formData}
                instrument={instrument}
                isEditing={editId === instrument.id}
                isPending={updateMutation.isPending}
                onCancelEdit={handleCancelEdit}
                onEdit={handleStartEdit}
                onFormDataChange={setFormData}
                onUpdate={handleUpdateSubmit}
              />
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
