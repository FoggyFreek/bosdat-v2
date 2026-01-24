import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, Check } from 'lucide-react'
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
import { instrumentsApi, courseTypesApi, settingsApi } from '@/services/api'
import { useSettingsDirty } from '@/context/SettingsDirtyContext'
import { cn, formatCurrency } from '@/lib/utils'
import type { Instrument } from '@/features/instruments/types'
import type { CourseType, CourseTypeCategory } from '@/features/course-types/types'

const durationOptions = ['20', '30', '40', '45', '50', '60', '90', '120']

interface FormData {
  name: string
  instrumentId: string
  durationMinutes: string
  customDuration: string
  type: CourseTypeCategory
  priceAdult: string
  priceChild: string
  maxStudents: string
  isActive: boolean
}

const defaultFormData: FormData = {
  name: '',
  instrumentId: '',
  durationMinutes: '30',
  customDuration: '',
  type: 'Individual',
  priceAdult: '',
  priceChild: '',
  maxStudents: '1',
  isActive: true,
}

export function CourseTypesSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [teacherWarning, setTeacherWarning] = useState<string | null>(null)
  const [useCustomDuration, setUseCustomDuration] = useState(false)
  const { setIsDirty } = useSettingsDirty()
  const [formData, setFormData] = useState<FormData>(defaultFormData)

  const { data: courseTypes = [], isLoading } = useQuery<CourseType[]>({
    queryKey: ['courseTypes'],
    queryFn: () => courseTypesApi.getAll(),
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
      const result = await courseTypesApi.getTeacherCountForInstrument(parseInt(instrumentId))
      if (result === 0) {
        setTeacherWarning(`No active teachers teach this instrument`)
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
    setFormData({ ...formData, type: value as CourseTypeCategory, maxStudents })
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
    mutationFn: (data: FormData) =>
      courseTypesApi.create({
        name: data.name,
        instrumentId: parseInt(data.instrumentId),
        durationMinutes: parseInt(getDuration()),
        type: data.type,
        priceAdult: parseFloat(data.priceAdult),
        priceChild: parseFloat(data.priceChild),
        maxStudents: parseInt(data.maxStudents),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
      resetForm()
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to create course type')
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: FormData }) =>
      courseTypesApi.update(id, {
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
      queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
      resetForm()
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to update course type')
    },
  })

  const archiveMutation = useMutation({
    mutationFn: (id: number) => courseTypesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to archive course type')
    },
  })

  const reactivateMutation = useMutation({
    mutationFn: (id: number) => courseTypesApi.reactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
    },
  })

  const startEdit = (lt: CourseType) => {
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
          <CardTitle>Course Types</CardTitle>
          <CardDescription>Configure types of courses and pricing</CardDescription>
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
            <h4 className="font-medium">{editId ? 'Edit Lesson Type' : 'New Course Type'}</h4>
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
        ) : courseTypes.length === 0 ? (
          <p className="text-muted-foreground">No course types configured</p>
        ) : (
          <div className="divide-y">
            {courseTypes.map((lt) => (
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
                    {!lt.hasTeachersForCourseType && (
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
