import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, X, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { roomsApi } from '@/services/api'
import { useFormDirty } from '@/context/FormDirtyContext'
import { cn } from '@/lib/utils'
import type { Room } from '@/features/rooms/types'

interface FormData {
  name: string
  floorLevel: string
  capacity: string
  hasPiano: boolean
  hasDrums: boolean
  hasAmplifier: boolean
  hasMicrophone: boolean
  hasWhiteboard: boolean
  hasStereo: boolean
  hasGuitar: boolean
  notes: string
}

const defaultFormData: FormData = {
  name: '',
  floorLevel: '',
  capacity: '2',
  hasPiano: false,
  hasDrums: false,
  hasAmplifier: false,
  hasMicrophone: false,
  hasWhiteboard: false,
  hasStereo: false,
  hasGuitar: false,
  notes: '',
}

const equipmentKeys = ['hasPiano', 'hasDrums', 'hasAmplifier', 'hasMicrophone', 'hasWhiteboard', 'hasStereo', 'hasGuitar'] as const

export function RoomsSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { setIsDirty } = useFormDirty()
  const [formData, setFormData] = useState<FormData>(defaultFormData)

  const { data: rooms = [], isLoading } = useQuery<Room[]>({
    queryKey: ['rooms'],
    queryFn: () => roomsApi.getAll(),
  })

  const resetForm = () => {
    setFormData(defaultFormData)
    setShowAdd(false)
    setEditId(null)
    setError(null)
    setIsDirty(false)
  }

  const createMutation = useMutation({
    mutationFn: (data: FormData) =>
      roomsApi.create({
        name: data.name,
        floorLevel: data.floorLevel ? parseInt(data.floorLevel) : undefined,
        capacity: parseInt(data.capacity),
        hasPiano: data.hasPiano,
        hasDrums: data.hasDrums,
        hasAmplifier: data.hasAmplifier,
        hasMicrophone: data.hasMicrophone,
        hasWhiteboard: data.hasWhiteboard,
        hasStereo: data.hasStereo,
        hasGuitar: data.hasGuitar,
        notes: data.notes || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
      resetForm()
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to create room')
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: FormData }) =>
      roomsApi.update(id, {
        name: data.name,
        floorLevel: data.floorLevel ? parseInt(data.floorLevel) : undefined,
        capacity: parseInt(data.capacity),
        hasPiano: data.hasPiano,
        hasDrums: data.hasDrums,
        hasAmplifier: data.hasAmplifier,
        hasMicrophone: data.hasMicrophone,
        hasWhiteboard: data.hasWhiteboard,
        hasStereo: data.hasStereo,
        hasGuitar: data.hasGuitar,
        notes: data.notes || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
      resetForm()
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to update room')
    },
  })

  const archiveMutation = useMutation({
    mutationFn: (id: number) => roomsApi.archive(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to archive room')
    },
  })

  const reactivateMutation = useMutation({
    mutationFn: (id: number) => roomsApi.reactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to reactivate room')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => roomsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to delete room')
    },
  })

  const handleShowAdd = () => {
    resetForm()
    setShowAdd(true)
    setIsDirty(true)
  }

  const startEdit = (room: Room) => {
    setFormData({
      name: room.name,
      floorLevel: room.floorLevel?.toString() || '',
      capacity: room.capacity.toString(),
      hasPiano: room.hasPiano,
      hasDrums: room.hasDrums,
      hasAmplifier: room.hasAmplifier,
      hasMicrophone: room.hasMicrophone,
      hasWhiteboard: room.hasWhiteboard,
      hasStereo: room.hasStereo,
      hasGuitar: room.hasGuitar,
      notes: room.notes || '',
    })
    setEditId(room.id)
    setShowAdd(false)
    setIsDirty(true)
  }

  const hasLinkedData = (room: Room) => room.activeCourseCount > 0 || room.scheduledLessonCount > 0
  const getLinkedDataWarning = (room: Room) => {
    const parts = []
    if (room.activeCourseCount > 0) parts.push(`${room.activeCourseCount} active course${room.activeCourseCount > 1 ? 's' : ''}`)
    if (room.scheduledLessonCount > 0) parts.push(`${room.scheduledLessonCount} scheduled lesson${room.scheduledLessonCount > 1 ? 's' : ''}`)
    return parts.join(' and ')
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Rooms</CardTitle>
          <CardDescription>Manage lesson rooms and equipment</CardDescription>
        </div>
        <Button size="sm" onClick={handleShowAdd}>
          <Plus className="h-4 w-4 mr-2" />
          Add
        </Button>
      </CardHeader>
      <CardContent>
        {error && (
          <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm">
            {error}
            <button className="ml-2 text-red-900" onClick={() => setError(null)}>×</button>
          </div>
        )}

        {(showAdd || editId !== null) && (
          <div className="mb-4 p-4 bg-muted/50 rounded-lg space-y-3">
            <h4 className="font-medium">{editId ? 'Edit Room' : 'New Room'}</h4>
            <div className="grid grid-cols-3 gap-3">
              <div>
                <Label>Room Name *</Label>
                <Input
                  placeholder="e.g., Room A"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                />
              </div>
              <div>
                <Label>Floor Level</Label>
                <Input
                  type="number"
                  placeholder="e.g., 1"
                  value={formData.floorLevel}
                  onChange={(e) => setFormData({ ...formData, floorLevel: e.target.value })}
                />
              </div>
              <div>
                <Label>Capacity *</Label>
                <Input
                  type="number"
                  min="1"
                  value={formData.capacity}
                  onChange={(e) => setFormData({ ...formData, capacity: e.target.value })}
                />
              </div>
            </div>
            <div>
              <Label className="mb-2 block">Equipment</Label>
              <div className="flex flex-wrap gap-4">
                {equipmentKeys.map((key) => (
                  <label key={key} className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={formData[key]}
                      onChange={(e) => setFormData({ ...formData, [key]: e.target.checked })}
                      className="rounded border-gray-300"
                    />
                    {key.replace('has', '')}
                  </label>
                ))}
              </div>
            </div>
            <div>
              <Label>Notes</Label>
              <Input
                placeholder="Optional notes about the room"
                value={formData.notes}
                onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
              />
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={resetForm}>Cancel</Button>
              <Button
                onClick={() => {
                  if (editId) {
                    updateMutation.mutate({ id: editId, data: formData })
                  } else {
                    createMutation.mutate(formData)
                  }
                }}
                disabled={!formData.name || createMutation.isPending || updateMutation.isPending}
              >
                {editId ? 'Save' : 'Create'}
              </Button>
            </div>
          </div>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        ) : rooms.length === 0 ? (
          <p className="text-muted-foreground">No rooms configured</p>
        ) : (
          <div className="divide-y">
            {rooms.map((room) => (
              <div key={room.id} className="flex items-center justify-between py-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <p className="font-medium">{room.name}</p>
                    <span className={cn(
                      'inline-flex items-center rounded-full px-2 py-0.5 text-xs',
                      room.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                    )}>
                      {room.isActive ? 'Active' : 'Archived'}
                    </span>
                    {hasLinkedData(room) && (
                      <span className="inline-flex items-center rounded-full px-2 py-0.5 text-xs bg-blue-100 text-blue-800">
                        {getLinkedDataWarning(room)}
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-muted-foreground">
                    {room.floorLevel !== undefined && room.floorLevel !== null && `Floor ${room.floorLevel} - `}
                    Capacity: {room.capacity}
                    {' - '}
                    {[
                      room.hasPiano && 'Piano',
                      room.hasDrums && 'Drums',
                      room.hasAmplifier && 'Amp',
                      room.hasMicrophone && 'Mic',
                      room.hasWhiteboard && 'Whiteboard',
                      room.hasStereo && 'Stereo',
                      room.hasGuitar && 'Guitar',
                    ]
                      .filter(Boolean)
                      .join(', ') || 'No equipment'}
                  </p>
                </div>
                <div className="flex items-center gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => startEdit(room)}
                    title="Edit"
                  >
                    <Pencil className="h-4 w-4" />
                  </Button>
                  {room.isActive ? (
                    <>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="text-orange-600 hover:text-orange-700 hover:bg-orange-50"
                        onClick={() => archiveMutation.mutate(room.id)}
                        disabled={hasLinkedData(room) || archiveMutation.isPending}
                        title={hasLinkedData(room) ? `Cannot archive: ${getLinkedDataWarning(room)}` : 'Archive'}
                      >
                        <X className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                        onClick={() => deleteMutation.mutate(room.id)}
                        disabled={room.activeCourseCount > 0 || room.scheduledLessonCount > 0 || (rooms.find(r => r.id === room.id)?.activeCourseCount ?? 0) > 0 || deleteMutation.isPending}
                        title={hasLinkedData(room) ? `Cannot delete: has linked data` : 'Delete permanently'}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </>
                  ) : (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-green-600 hover:text-green-700 hover:bg-green-50"
                      onClick={() => reactivateMutation.mutate(room.id)}
                      disabled={reactivateMutation.isPending}
                      title="Reactivate"
                    >
                      <Check className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
