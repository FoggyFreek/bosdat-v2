import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, X, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { instrumentsApi, roomsApi, lessonTypesApi, holidaysApi } from '@/services/api'
import type { Instrument, Room, LessonType, Holiday } from '@/types'
import { cn, formatDate, formatCurrency } from '@/lib/utils'

export function SettingsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Settings</h1>
        <p className="text-muted-foreground">Manage your music school settings</p>
      </div>

      <div className="grid gap-6">
        <InstrumentsSection />
        <LessonTypesSection />
        <RoomsSection />
        <HolidaysSection />
      </div>
    </div>
  )
}

function InstrumentsSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [formData, setFormData] = useState({ name: '', category: 'Other' as const })

  const { data: instruments = [], isLoading } = useQuery<Instrument[]>({
    queryKey: ['instruments'],
    queryFn: () => instrumentsApi.getAll(),
  })

  const createMutation = useMutation({
    mutationFn: (data: { name: string; category: string }) => instrumentsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['instruments'] })
      setShowAdd(false)
      setFormData({ name: '', category: 'Other' })
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: { name: string; category: string } }) =>
      instrumentsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['instruments'] })
      setEditId(null)
    },
  })

  const categories = ['String', 'Percussion', 'Vocal', 'Keyboard', 'Wind', 'Brass', 'Electronic', 'Other']

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Instruments</CardTitle>
          <CardDescription>Manage available instruments for lessons</CardDescription>
        </div>
        <Button size="sm" onClick={() => setShowAdd(true)}>
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
              onValueChange={(value) => setFormData({ ...formData, category: value as typeof formData.category })}
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
            <Button variant="ghost" onClick={() => { setShowAdd(false); setFormData({ name: '', category: 'Other' }) }}>
              <X className="h-4 w-4" />
            </Button>
          </div>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        ) : instruments.length === 0 ? (
          <p className="text-muted-foreground">No instruments configured</p>
        ) : (
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
                    <Select
                      value={formData.category}
                      onValueChange={(value) => setFormData({ ...formData, category: value as typeof formData.category })}
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
                    <Button size="icon" variant="ghost" onClick={() => setEditId(null)}>
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
                        onClick={() => {
                          setEditId(instrument.id)
                          setFormData({ name: instrument.name, category: instrument.category as typeof formData.category })
                        }}
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

function LessonTypesSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [formData, setFormData] = useState({
    name: '',
    instrumentId: '',
    durationMinutes: '30',
    type: 'Individual' as const,
    priceAdult: '',
    priceChild: '',
    maxStudents: '1',
  })

  const { data: lessonTypes = [], isLoading } = useQuery<LessonType[]>({
    queryKey: ['lessonTypes'],
    queryFn: () => lessonTypesApi.getAll(),
  })

  const { data: instruments = [] } = useQuery<Instrument[]>({
    queryKey: ['instruments'],
    queryFn: () => instrumentsApi.getAll({ activeOnly: true }),
  })

  const createMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      lessonTypesApi.create({
        name: data.name,
        instrumentId: parseInt(data.instrumentId),
        durationMinutes: parseInt(data.durationMinutes),
        type: data.type,
        priceAdult: parseFloat(data.priceAdult),
        priceChild: parseFloat(data.priceChild),
        maxStudents: parseInt(data.maxStudents),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lessonTypes'] })
      setShowAdd(false)
      setFormData({
        name: '',
        instrumentId: '',
        durationMinutes: '30',
        type: 'Individual',
        priceAdult: '',
        priceChild: '',
        maxStudents: '1',
      })
    },
  })

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Lesson Types</CardTitle>
          <CardDescription>Configure types of lessons and pricing</CardDescription>
        </div>
        <Button size="sm" onClick={() => setShowAdd(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Add
        </Button>
      </CardHeader>
      <CardContent>
        {showAdd && (
          <div className="mb-4 p-4 bg-muted/50 rounded-lg space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Name</Label>
                <Input
                  placeholder="e.g., Piano 30 min"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                />
              </div>
              <div>
                <Label>Instrument</Label>
                <Select value={formData.instrumentId} onValueChange={(v) => setFormData({ ...formData, instrumentId: v })}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select instrument" />
                  </SelectTrigger>
                  <SelectContent>
                    {instruments.map((inst) => (
                      <SelectItem key={inst.id} value={inst.id.toString()}>{inst.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Duration (min)</Label>
                <Select value={formData.durationMinutes} onValueChange={(v) => setFormData({ ...formData, durationMinutes: v })}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {['15', '30', '40', '45', '60', '90'].map((d) => (
                      <SelectItem key={d} value={d}>{d} minutes</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Type</Label>
                <Select value={formData.type} onValueChange={(v) => setFormData({ ...formData, type: v as typeof formData.type })}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Individual">Individual</SelectItem>
                    <SelectItem value="Group">Group</SelectItem>
                    <SelectItem value="Workshop">Workshop</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Price Adult</Label>
                <Input
                  type="number"
                  step="0.01"
                  placeholder="0.00"
                  value={formData.priceAdult}
                  onChange={(e) => setFormData({ ...formData, priceAdult: e.target.value })}
                />
              </div>
              <div>
                <Label>Price Child</Label>
                <Input
                  type="number"
                  step="0.01"
                  placeholder="0.00"
                  value={formData.priceChild}
                  onChange={(e) => setFormData({ ...formData, priceChild: e.target.value })}
                />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setShowAdd(false)}>Cancel</Button>
              <Button
                onClick={() => createMutation.mutate(formData)}
                disabled={!formData.name || !formData.instrumentId || createMutation.isPending}
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
        ) : lessonTypes.length === 0 ? (
          <p className="text-muted-foreground">No lesson types configured</p>
        ) : (
          <div className="divide-y">
            {lessonTypes.map((lt) => (
              <div key={lt.id} className="flex items-center justify-between py-3">
                <div>
                  <p className="font-medium">{lt.name}</p>
                  <p className="text-sm text-muted-foreground">
                    {lt.instrumentName} - {lt.durationMinutes} min - {lt.type}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-sm">Adult: {formatCurrency(lt.priceAdult)}</p>
                  <p className="text-sm text-muted-foreground">Child: {formatCurrency(lt.priceChild)}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function RoomsSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [formData, setFormData] = useState({
    name: '',
    capacity: '1',
    hasPiano: false,
    hasDrums: false,
    hasAmplifier: false,
    hasMicrophone: false,
    hasWhiteboard: false,
  })

  const { data: rooms = [], isLoading } = useQuery<Room[]>({
    queryKey: ['rooms'],
    queryFn: () => roomsApi.getAll(),
  })

  const createMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      roomsApi.create({
        ...data,
        capacity: parseInt(data.capacity),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] })
      setShowAdd(false)
      setFormData({
        name: '',
        capacity: '1',
        hasPiano: false,
        hasDrums: false,
        hasAmplifier: false,
        hasMicrophone: false,
        hasWhiteboard: false,
      })
    },
  })

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Rooms</CardTitle>
          <CardDescription>Manage lesson rooms and equipment</CardDescription>
        </div>
        <Button size="sm" onClick={() => setShowAdd(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Add
        </Button>
      </CardHeader>
      <CardContent>
        {showAdd && (
          <div className="mb-4 p-4 bg-muted/50 rounded-lg space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Room Name</Label>
                <Input
                  placeholder="e.g., Room A"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                />
              </div>
              <div>
                <Label>Capacity</Label>
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
                {(['hasPiano', 'hasDrums', 'hasAmplifier', 'hasMicrophone', 'hasWhiteboard'] as const).map((key) => (
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
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setShowAdd(false)}>Cancel</Button>
              <Button onClick={() => createMutation.mutate(formData)} disabled={!formData.name || createMutation.isPending}>
                Create
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
                <div>
                  <p className="font-medium">{room.name}</p>
                  <p className="text-sm text-muted-foreground">
                    Capacity: {room.capacity} -
                    {[
                      room.hasPiano && 'Piano',
                      room.hasDrums && 'Drums',
                      room.hasAmplifier && 'Amp',
                      room.hasMicrophone && 'Mic',
                      room.hasWhiteboard && 'Whiteboard',
                    ]
                      .filter(Boolean)
                      .join(', ') || ' No equipment'}
                  </p>
                </div>
                <span className={cn(
                  'inline-flex items-center rounded-full px-2 py-0.5 text-xs',
                  room.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                )}>
                  {room.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function HolidaysSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [formData, setFormData] = useState({ name: '', startDate: '', endDate: '' })

  const { data: holidays = [], isLoading } = useQuery<Holiday[]>({
    queryKey: ['holidays'],
    queryFn: () => holidaysApi.getAll(),
  })

  const createMutation = useMutation({
    mutationFn: (data: typeof formData) => holidaysApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['holidays'] })
      setShowAdd(false)
      setFormData({ name: '', startDate: '', endDate: '' })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => holidaysApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['holidays'] })
    },
  })

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Holidays</CardTitle>
          <CardDescription>Set school holidays and closures</CardDescription>
        </div>
        <Button size="sm" onClick={() => setShowAdd(true)}>
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
              <Button variant="outline" onClick={() => setShowAdd(false)}>Cancel</Button>
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
