import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { holidaysApi } from '@/services/api'
import { useFormDirty } from '@/context/FormDirtyContext'
import { formatDate } from '@/lib/utils'
import type { Holiday } from '@/features/schedule/types'

interface FormData {
  name: string
  startDate: string
  endDate: string
}

export function HolidaysSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [formData, setFormData] = useState<FormData>({ name: '', startDate: '', endDate: '' })
  const { setIsDirty } = useFormDirty()

  const { data: holidays = [], isLoading } = useQuery<Holiday[]>({
    queryKey: ['holidays'],
    queryFn: () => holidaysApi.getAll(),
  })

  const createMutation = useMutation({
    mutationFn: (data: FormData) => holidaysApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['holidays'] })
      setShowAdd(false)
      setFormData({ name: '', startDate: '', endDate: '' })
      setIsDirty(false)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => holidaysApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['holidays'] })
    },
  })

  const handleShowAdd = () => {
    setShowAdd(true)
    setIsDirty(true)
  }

  const handleCancelAdd = () => {
    setShowAdd(false)
    setFormData({ name: '', startDate: '', endDate: '' })
    setIsDirty(false)
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Holidays</CardTitle>
          <CardDescription>Set school holidays and closures</CardDescription>
        </div>
        <Button size="sm" onClick={handleShowAdd}>
          <Plus className="h-4 w-4 mr-2" />
          Add
        </Button>
      </CardHeader>
      <CardContent>
        {showAdd && (
          <div className="mb-4 p-4 bg-muted/50 rounded-lg space-y-3">
            <div className="grid grid-cols-3 gap-3">
              <div>
                <Label>Name</Label>
                <Input
                  placeholder="e.g., Summer Break"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                />
              </div>
              <div>
                <Label>Start Date</Label>
                <Input
                  type="date"
                  value={formData.startDate}
                  onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                />
              </div>
              <div>
                <Label>End Date</Label>
                <Input
                  type="date"
                  value={formData.endDate}
                  onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={handleCancelAdd}>Cancel</Button>
              <Button
                onClick={() => createMutation.mutate(formData)}
                disabled={!formData.name || !formData.startDate || !formData.endDate || createMutation.isPending}
              >
                Create
              </Button>
            </div>
          </div>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        ) : holidays.length === 0 ? (
          <p className="text-muted-foreground">No holidays configured</p>
        ) : (
          <div className="divide-y">
            {holidays.map((holiday) => (
              <div key={holiday.id} className="flex items-center justify-between py-3">
                <div>
                  <p className="font-medium">{holiday.name}</p>
                  <p className="text-sm text-muted-foreground">
                    {formatDate(holiday.startDate)} - {formatDate(holiday.endDate)}
                  </p>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="text-red-600 hover:text-red-700 hover:bg-red-50"
                  onClick={() => deleteMutation.mutate(holiday.id)}
                  disabled={deleteMutation.isPending}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
