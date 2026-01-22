import { useState, createContext, useContext, ReactNode } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Plus, Pencil, Trash2, X, Check,
  User, SlidersHorizontal, Music, BookOpen,
  DoorOpen, CalendarDays, Settings2
} from 'lucide-react'
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
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { instrumentsApi, roomsApi, lessonTypesApi, holidaysApi, settingsApi } from '@/services/api'
import type { Holiday} from '@/features/schedule/types'
import type { Instrument, InstrumentCategory } from '@/features/instruments/types'
import type { Room } from '@/features/rooms/types'
import type { LessonType, LessonTypeCategory} from '@/features/lesson-types/types'

import { cn, formatDate, formatCurrency } from '@/lib/utils'

// Settings dirty state context
interface SettingsDirtyContextType {
  isDirty: boolean
  setIsDirty: (dirty: boolean) => void
}

const SettingsDirtyContext = createContext<SettingsDirtyContextType>({
  isDirty: false,
  setIsDirty: () => {},
})

export const useSettingsDirty = () => useContext(SettingsDirtyContext)

// Navigation items configuration
type SettingKey = 'profile' | 'preferences' | 'instruments' | 'lesson-types' | 'rooms' | 'holidays' | 'system'

interface NavItem {
  key: SettingKey
  label: string
  icon: ReactNode
}

interface NavGroup {
  label: string
  items: NavItem[]
}

const navigationGroups: NavGroup[] = [
  {
    label: 'ACCOUNT',
    items: [
      { key: 'profile', label: 'Profile', icon: <User className="h-4 w-4" /> },
      { key: 'preferences', label: 'Preferences', icon: <SlidersHorizontal className="h-4 w-4" /> },
    ],
  },
  {
    label: 'LESSONS',
    items: [
      { key: 'instruments', label: 'Instruments', icon: <Music className="h-4 w-4" /> },
      { key: 'lesson-types', label: 'Lesson types', icon: <BookOpen className="h-4 w-4" /> },
    ],
  },
  {
    label: 'SCHEDULING',
    items: [
      { key: 'rooms', label: 'Rooms', icon: <DoorOpen className="h-4 w-4" /> },
      { key: 'holidays', label: 'Holidays', icon: <CalendarDays className="h-4 w-4" /> },
    ],
  },
  {
    label: 'GENERAL',
    items: [
      { key: 'system', label: 'System settings', icon: <Settings2 className="h-4 w-4" /> },
    ],
  },
]

export function SettingsPage() {
  const [selectedSetting, setSelectedSetting] = useState<SettingKey>('instruments')
  const [isDirty, setIsDirty] = useState(false)
  const [pendingNavigation, setPendingNavigation] = useState<SettingKey | null>(null)
  const [showUnsavedDialog, setShowUnsavedDialog] = useState(false)

  const handleNavigation = (key: SettingKey) => {
    if (isDirty && key !== selectedSetting) {
      setPendingNavigation(key)
      setShowUnsavedDialog(true)
    } else {
      setSelectedSetting(key)
    }
  }

  const handleDiscardChanges = () => {
    setIsDirty(false)
    if (pendingNavigation) {
      setSelectedSetting(pendingNavigation)
      setPendingNavigation(null)
    }
    setShowUnsavedDialog(false)
  }

  const handleCancelNavigation = () => {
    setPendingNavigation(null)
    setShowUnsavedDialog(false)
  }

  const renderContent = () => {
    switch (selectedSetting) {
      case 'profile':
        return <ProfileSection />
      case 'preferences':
        return <PreferencesSection />
      case 'instruments':
        return <InstrumentsSection />
      case 'lesson-types':
        return <LessonTypesSection />
      case 'rooms':
        return <RoomsSection />
      case 'holidays':
        return <HolidaysSection />
      case 'system':
        return <SystemSettingsSection />
      default:
        return null
    }
  }

  return (
    <SettingsDirtyContext.Provider value={{ isDirty, setIsDirty }}>
      <div className="flex h-[calc(100vh-8rem)]">
        {/* Navigation Sidebar */}
        <nav className="w-auto min-w-[200px] border-r bg-muted/30 p-4 overflow-y-auto">
          <h1 className="text-xl font-bold mb-6">Settings</h1>
          <div className="space-y-6">
            {navigationGroups.map((group) => (
              <div key={group.label}>
                <h2 className="text-xs font-medium text-muted-foreground tracking-wider mb-2">
                  {group.label}
                </h2>
                <div className="space-y-1">
                  {group.items.map((item) => (
                    <button
                      key={item.key}
                      onClick={() => handleNavigation(item.key)}
                      className={cn(
                        'w-full flex items-center gap-3 px-3 py-2 text-sm rounded-md transition-colors text-left',
                        selectedSetting === item.key
                          ? 'bg-primary text-primary-foreground'
                          : 'hover:bg-muted'
                      )}
                    >
                      {item.icon}
                      {item.label}
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </nav>

        {/* Content Area */}
        <main className="flex-1 p-6 overflow-y-auto">
          {renderContent()}
        </main>

        {/* Unsaved Changes Dialog */}
        <AlertDialog open={showUnsavedDialog} onOpenChange={setShowUnsavedDialog}>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Unsaved changes</AlertDialogTitle>
              <AlertDialogDescription>
                You have unsaved changes. Do you want to discard them and navigate away?
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={handleCancelNavigation}>Cancel</AlertDialogCancel>
              <AlertDialogAction onClick={handleDiscardChanges}>Discard changes</AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </SettingsDirtyContext.Provider>
  )
}

// Placeholder sections
function ProfileSection() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Profile</CardTitle>
        <CardDescription>Manage your account profile</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-center py-12 text-muted-foreground">
          <div className="text-center">
            <User className="h-12 w-12 mx-auto mb-4 opacity-50" />
            <p>Profile settings coming soon</p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function PreferencesSection() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Preferences</CardTitle>
        <CardDescription>Customize your experience</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-center py-12 text-muted-foreground">
          <div className="text-center">
            <SlidersHorizontal className="h-12 w-12 mx-auto mb-4 opacity-50" />
            <p>Preference settings coming soon</p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function SystemSettingsSection() {
  const queryClient = useQueryClient()
  const [editKey, setEditKey] = useState<string | null>(null)
  const [editValue, setEditValue] = useState('')
  const { setIsDirty } = useSettingsDirty()

  const { data: settings = [], isLoading } = useQuery<{ key: string; value: string; type?: string; description?: string }[]>({
    queryKey: ['settings'],
    queryFn: () => settingsApi.getAll(),
  })

  const updateMutation = useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) => settingsApi.update(key, value),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] })
      setEditKey(null)
      setEditValue('')
      setIsDirty(false)
    },
  })

  const startEdit = (key: string, value: string) => {
    setEditKey(key)
    setEditValue(value)
    setIsDirty(true)
  }

  const cancelEdit = () => {
    setEditKey(null)
    setEditValue('')
    setIsDirty(false)
  }

  const formatSettingName = (key: string) => {
    return key.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase())
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>System Settings</CardTitle>
        <CardDescription>Configure application-wide settings</CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        ) : settings.length === 0 ? (
          <p className="text-muted-foreground">No settings configured</p>
        ) : (
          <div className="divide-y">
            {settings.map((setting) => (
              <div key={setting.key} className="flex items-center justify-between py-3">
                {editKey === setting.key ? (
                  <div className="flex gap-2 flex-1 items-center">
                    <div className="flex-1">
                      <p className="font-medium text-sm">{formatSettingName(setting.key)}</p>
                      <Input
                        value={editValue}
                        onChange={(e) => setEditValue(e.target.value)}
                        className="mt-1"
                      />
                    </div>
                    <Button
                      size="icon"
                      onClick={() => updateMutation.mutate({ key: setting.key, value: editValue })}
                      disabled={updateMutation.isPending}
                    >
                      <Check className="h-4 w-4" />
                    </Button>
                    <Button size="icon" variant="ghost" onClick={cancelEdit}>
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ) : (
                  <>
                    <div>
                      <p className="font-medium">{formatSettingName(setting.key)}</p>
                      <p className="text-sm text-muted-foreground">{setting.description}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-mono bg-muted px-2 py-1 rounded">
                        {setting.value}
                      </span>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => startEdit(setting.key, setting.value)}
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

function InstrumentsSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [formData, setFormData] = useState<{ name: string; category: InstrumentCategory }>({ name: '', category: 'Other' })
  const { setIsDirty } = useSettingsDirty()

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
      setIsDirty(false)
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: { name: string; category: string } }) =>
      instrumentsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['instruments'] })
      setEditId(null)
      setIsDirty(false)
    },
  })

  const categories = ['String', 'Percussion', 'Vocal', 'Keyboard', 'Wind', 'Brass', 'Electronic', 'Other']

  const handleShowAdd = () => {
    setShowAdd(true)
    setIsDirty(true)
  }

  const handleCancelAdd = () => {
    setShowAdd(false)
    setFormData({ name: '', category: 'Other' })
    setIsDirty(false)
  }

  const handleStartEdit = (instrument: Instrument) => {
    setEditId(instrument.id)
    setFormData({ name: instrument.name, category: instrument.category })
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

function LessonTypesSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [teacherWarning, setTeacherWarning] = useState<string | null>(null)
  const [useCustomDuration, setUseCustomDuration] = useState(false)
  const { setIsDirty } = useSettingsDirty()

  const defaultFormData = {
    name: '',
    instrumentId: '',
    durationMinutes: '30',
    customDuration: '',
    type: 'Individual' as LessonTypeCategory,
    priceAdult: '',
    priceChild: '',
    maxStudents: '1',
    isActive: true,
  }
  const [formData, setFormData] = useState(defaultFormData)

  const durationOptions = ['20', '30', '40', '45', '50', '60', '90', '120']

  const { data: lessonTypes = [], isLoading } = useQuery<LessonType[]>({
    queryKey: ['lessonTypes'],
    queryFn: () => lessonTypesApi.getAll(),
  })

  const { data: instruments = [] } = useQuery<Instrument[]>({
    queryKey: ['instruments'],
    queryFn: () => instrumentsApi.getAll({ activeOnly: true }),
  })

  const { data: settings = [] } = useQuery<{ key: string; value: string }[]>({
    queryKey: ['settings'],
    queryFn: () => settingsApi.getAll(),
  })

  const childDiscountPercent = parseFloat(settings.find(s => s.key === 'child_discount_percent')?.value || '10')
  const groupMaxStudents = parseInt(settings.find(s => s.key === 'group_max_students')?.value || '6')
  const workshopMaxStudents = parseInt(settings.find(s => s.key === 'workshop_max_students')?.value || '12')

  const checkTeachersForInstrument = async (instrumentId: string) => {
    if (!instrumentId) {
      setTeacherWarning(null)
      return
    }
    try {
      const result = await lessonTypesApi.getTeachersForInstrument(parseInt(instrumentId))
      if (!result.hasTeachers) {
        setTeacherWarning(`No active teachers teach ${result.instrumentName}`)
      } else {
        setTeacherWarning(null)
      }
    } catch {
      setTeacherWarning(null)
    }
  }

  const handleInstrumentChange = (value: string) => {
    setFormData({ ...formData, instrumentId: value })
    checkTeachersForInstrument(value)
  }

  const handleTypeChange = (value: string) => {
    let maxStudents = '1'
    if (value === 'Group') maxStudents = groupMaxStudents.toString()
    if (value === 'Workshop') maxStudents = workshopMaxStudents.toString()
    setFormData({ ...formData, type: value as typeof formData.type, maxStudents })
  }

  const handleAdultPriceChange = (value: string) => {
    const adultPrice = parseFloat(value) || 0
    const childPrice = (adultPrice * (1 - childDiscountPercent / 100)).toFixed(2)
    setFormData({ ...formData, priceAdult: value, priceChild: childPrice })
  }

  const handleChildPriceChange = (value: string) => {
    setFormData({ ...formData, priceChild: value })
    if (parseFloat(value) > parseFloat(formData.priceAdult)) {
      setError('Child price cannot be higher than adult price')
    } else {
      setError(null)
    }
  }

  const getDuration = () => {
    return useCustomDuration ? formData.customDuration : formData.durationMinutes
  }

  const resetForm = () => {
    setFormData(defaultFormData)
    setShowAdd(false)
    setEditId(null)
    setError(null)
    setTeacherWarning(null)
    setUseCustomDuration(false)
    setIsDirty(false)
  }

  const createMutation = useMutation({
    mutationFn: (data: typeof formData) =>
      lessonTypesApi.create({
        name: data.name,
        instrumentId: parseInt(data.instrumentId),
        durationMinutes: parseInt(getDuration()),
        type: data.type,
        priceAdult: parseFloat(data.priceAdult),
        priceChild: parseFloat(data.priceChild),
        maxStudents: parseInt(data.maxStudents),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lessonTypes'] })
      resetForm()
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to create lesson type')
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: typeof formData }) =>
      lessonTypesApi.update(id, {
        name: data.name,
        instrumentId: parseInt(data.instrumentId),
        durationMinutes: parseInt(getDuration()),
        type: data.type,
        priceAdult: parseFloat(data.priceAdult),
        priceChild: parseFloat(data.priceChild),
        maxStudents: parseInt(data.maxStudents),
        isActive: data.isActive,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lessonTypes'] })
      resetForm()
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to update lesson type')
    },
  })

  const archiveMutation = useMutation({
    mutationFn: (id: number) => lessonTypesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lessonTypes'] })
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to archive lesson type')
    },
  })

  const reactivateMutation = useMutation({
    mutationFn: (id: number) => lessonTypesApi.reactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lessonTypes'] })
    },
  })

  const startEdit = (lt: LessonType) => {
    const isCustom = !durationOptions.includes(lt.durationMinutes.toString())
    setFormData({
      name: lt.name,
      instrumentId: lt.instrumentId.toString(),
      durationMinutes: isCustom ? '30' : lt.durationMinutes.toString(),
      customDuration: isCustom ? lt.durationMinutes.toString() : '',
      type: lt.type,
      priceAdult: lt.priceAdult.toFixed(2),
      priceChild: lt.priceChild.toFixed(2),
      maxStudents: lt.maxStudents.toString(),
      isActive: lt.isActive,
    })
    setUseCustomDuration(isCustom)
    setEditId(lt.id)
    setShowAdd(false)
    setIsDirty(true)
    checkTeachersForInstrument(lt.instrumentId.toString())
  }

  const handleShowAdd = () => {
    resetForm()
    setShowAdd(true)
    setIsDirty(true)
  }

  const isFormValid = formData.name && formData.instrumentId && formData.priceAdult && !error

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Lesson Types</CardTitle>
          <CardDescription>Configure types of lessons and pricing</CardDescription>
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
            <h4 className="font-medium">{editId ? 'Edit Lesson Type' : 'New Lesson Type'}</h4>
            {teacherWarning && (
              <div className="p-2 bg-yellow-50 border border-yellow-200 text-yellow-800 rounded text-sm">
                {teacherWarning}
              </div>
            )}
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Name *</Label>
                <Input
                  placeholder="e.g., Piano 30 min"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                />
              </div>
              <div>
                <Label>Instrument *</Label>
                <Select value={formData.instrumentId} onValueChange={handleInstrumentChange}>
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
                <div className="flex gap-2">
                  {!useCustomDuration ? (
                    <Select value={formData.durationMinutes} onValueChange={(v) => setFormData({ ...formData, durationMinutes: v })}>
                      <SelectTrigger className="flex-1">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {durationOptions.map((d) => (
                          <SelectItem key={d} value={d}>{d} min</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    <Input
                      type="number"
                      min="1"
                      placeholder="Custom"
                      value={formData.customDuration}
                      onChange={(e) => setFormData({ ...formData, customDuration: e.target.value })}
                      className="flex-1"
                    />
                  )}
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setUseCustomDuration(!useCustomDuration)}
                  >
                    {useCustomDuration ? 'Preset' : 'Custom'}
                  </Button>
                </div>
              </div>
              <div>
                <Label>Type</Label>
                <Select value={formData.type} onValueChange={handleTypeChange}>
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
                <Label>Price Adult (€) *</Label>
                <Input
                  type="number"
                  step="0.01"
                  min="0"
                  placeholder="0.00"
                  value={formData.priceAdult}
                  onChange={(e) => handleAdultPriceChange(e.target.value)}
                />
              </div>
              <div>
                <Label>Price Child (€)</Label>
                <Input
                  type="number"
                  step="0.01"
                  min="0"
                  placeholder="0.00"
                  value={formData.priceChild}
                  onChange={(e) => handleChildPriceChange(e.target.value)}
                  className={parseFloat(formData.priceChild) > parseFloat(formData.priceAdult) ? 'border-red-500' : ''}
                />
                <p className="text-xs text-muted-foreground mt-1">
                  Default: {childDiscountPercent}% discount from adult price
                </p>
              </div>
              {formData.type !== 'Individual' && (
                <div>
                  <Label>Max Students</Label>
                  <Input
                    type="number"
                    min="1"
                    value={formData.maxStudents}
                    onChange={(e) => setFormData({ ...formData, maxStudents: e.target.value })}
                  />
                </div>
              )}
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
                disabled={!isFormValid || createMutation.isPending || updateMutation.isPending}
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
        ) : lessonTypes.length === 0 ? (
          <p className="text-muted-foreground">No lesson types configured</p>
        ) : (
          <div className="divide-y">
            {lessonTypes.map((lt) => (
              <div key={lt.id} className="flex items-center justify-between py-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <p className="font-medium">{lt.name}</p>
                    <span className={cn(
                      'inline-flex items-center rounded-full px-2 py-0.5 text-xs',
                      lt.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                    )}>
                      {lt.isActive ? 'Active' : 'Archived'}
                    </span>
                    {!lt.hasTeachersForInstrument && (
                      <span className="inline-flex items-center rounded-full px-2 py-0.5 text-xs bg-yellow-100 text-yellow-800">
                        No teachers
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-muted-foreground">
                    {lt.instrumentName} - {lt.durationMinutes} min - {lt.type}
                    {lt.type !== 'Individual' && ` (max ${lt.maxStudents})`}
                  </p>
                </div>
                <div className="text-right mr-4">
                  <p className="text-sm">Adult: {formatCurrency(lt.priceAdult)}</p>
                  <p className="text-sm text-muted-foreground">Child: {formatCurrency(lt.priceChild)}</p>
                </div>
                <div className="flex items-center gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => startEdit(lt)}
                    title="Edit"
                  >
                    <Pencil className="h-4 w-4" />
                  </Button>
                  {lt.isActive ? (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-red-600 hover:text-red-700 hover:bg-red-50"
                      onClick={() => archiveMutation.mutate(lt.id)}
                      disabled={archiveMutation.isPending}
                      title={lt.activeCourseCount > 0 ? `Cannot archive: ${lt.activeCourseCount} active courses` : 'Archive'}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  ) : (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-green-600 hover:text-green-700 hover:bg-green-50"
                      onClick={() => reactivateMutation.mutate(lt.id)}
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

function RoomsSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { setIsDirty } = useSettingsDirty()

  const defaultFormData = {
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
  const [formData, setFormData] = useState(defaultFormData)

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
    mutationFn: (data: typeof formData) =>
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
    mutationFn: ({ id, data }: { id: number; data: typeof formData }) =>
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

  const equipmentKeys = ['hasPiano', 'hasDrums', 'hasAmplifier', 'hasMicrophone', 'hasWhiteboard', 'hasStereo', 'hasGuitar'] as const

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

function HolidaysSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [formData, setFormData] = useState({ name: '', startDate: '', endDate: '' })
  const { setIsDirty } = useSettingsDirty()

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
