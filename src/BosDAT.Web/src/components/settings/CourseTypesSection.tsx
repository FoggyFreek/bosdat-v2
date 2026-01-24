import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, Check, AlertCircle } from 'lucide-react'
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
import { Alert, AlertDescription } from '@/components/ui/alert'
import { instrumentsApi, courseTypesApi, settingsApi } from '@/services/api'
import { useSettingsDirty } from '@/context/SettingsDirtyContext'
import { cn, formatCurrency } from '@/lib/utils'
import { PricingHistoryCollapsible } from './PricingHistoryCollapsible'
import { NewPricingVersionDialog } from './NewPricingVersionDialog'
import type { Instrument } from '@/features/instruments/types'
import type { CourseType, CourseTypeCategory, CreateCourseTypePricingVersion } from '@/features/course-types/types'

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
  const [editId, setEditId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [teacherWarning, setTeacherWarning] = useState<string | null>(null)
  const [useCustomDuration, setUseCustomDuration] = useState(false)
  const { setIsDirty } = useSettingsDirty()
  const [formData, setFormData] = useState<FormData>(defaultFormData)

  // State for pricing version dialog
  const [pricingDialogOpen, setPricingDialogOpen] = useState(false)
  const [pricingDialogCourseType, setPricingDialogCourseType] = useState<CourseType | null>(null)
  const [pricingDialogError, setPricingDialogError] = useState<string | null>(null)
  // Track if we're editing an existing course type that needs pricing via versioning
  const [editingCourseType, setEditingCourseType] = useState<CourseType | null>(null)

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
    setEditingCourseType(null)
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
    mutationFn: ({ id, data }: { id: string; data: FormData }) =>
      courseTypesApi.update(id, {
        name: data.name,
        instrumentId: parseInt(data.instrumentId),
        durationMinutes: parseInt(getDuration()),
        type: data.type,
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

  const updatePricingMutation = useMutation({
    mutationFn: ({ id, priceAdult, priceChild }: { id: string; priceAdult: number; priceChild: number }) =>
      courseTypesApi.updatePricing(id, { priceAdult, priceChild }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to update pricing')
    },
  })

  const createPricingVersionMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateCourseTypePricingVersion }) =>
      courseTypesApi.createPricingVersion(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
      setPricingDialogOpen(false)
      setPricingDialogCourseType(null)
      setPricingDialogError(null)
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setPricingDialogError(err.response?.data?.message || 'Failed to create pricing version')
    },
  })

  const archiveMutation = useMutation({
    mutationFn: (id: string) => courseTypesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
    },
    onError: (err: { response?: { data?: { message?: string } } }) => {
      setError(err.response?.data?.message || 'Failed to archive course type')
    },
  })

  const reactivateMutation = useMutation({
    mutationFn: (id: string) => courseTypesApi.reactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['courseTypes'] })
    },
  })

  const startEdit = (lt: CourseType) => {
    const isCustom = !durationOptions.includes(lt.durationMinutes.toString())
    const currentPricing = lt.currentPricing
    setFormData({
      name: lt.name,
      instrumentId: lt.instrumentId.toString(),
      durationMinutes: isCustom ? '30' : lt.durationMinutes.toString(),
      customDuration: isCustom ? lt.durationMinutes.toString() : '',
      type: lt.type,
      priceAdult: currentPricing?.priceAdult.toFixed(2) ?? '0.00',
      priceChild: currentPricing?.priceChild.toFixed(2) ?? '0.00',
      maxStudents: lt.maxStudents.toString(),
      isActive: lt.isActive,
    })
    setUseCustomDuration(isCustom)
    setEditId(lt.id)
    setEditingCourseType(lt)
    setShowAdd(false)
    setIsDirty(true)
    checkTeachersForInstrument(lt.instrumentId.toString())
  }

  const handleShowAdd = () => {
    resetForm()
    setShowAdd(true)
    setIsDirty(true)
  }

  const handleSubmit = async () => {
    if (editId && editingCourseType) {
      // Update the course type (non-pricing fields)
      await updateMutation.mutateAsync({ id: editId, data: formData })

      // Check if pricing changed and handle accordingly
      const currentPricing = editingCourseType.currentPricing
      const newPriceAdult = parseFloat(formData.priceAdult)
      const newPriceChild = parseFloat(formData.priceChild)

      const pricingChanged =
        currentPricing &&
        (currentPricing.priceAdult !== newPriceAdult || currentPricing.priceChild !== newPriceChild)

      if (pricingChanged) {
        if (editingCourseType.canEditPricingDirectly) {
          // Direct update allowed
          await updatePricingMutation.mutateAsync({
            id: editId,
            priceAdult: newPriceAdult,
            priceChild: newPriceChild,
          })
        } else {
          // Need to create new version - open dialog
          setPricingDialogCourseType(editingCourseType)
          setPricingDialogOpen(true)
          return // Don't reset form until pricing dialog is handled
        }
      }
    } else {
      createMutation.mutate(formData)
    }
  }

  const handlePricingVersionSubmit = async (data: CreateCourseTypePricingVersion) => {
    if (pricingDialogCourseType) {
      await createPricingVersionMutation.mutateAsync({
        id: pricingDialogCourseType.id,
        data,
      })
      resetForm()
    }
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
          <Alert variant="destructive" className="mb-4">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              {error}
              <button className="ml-2 font-bold" onClick={() => setError(null)}>×</button>
            </AlertDescription>
          </Alert>
        )}

        {(showAdd || editId !== null) && (
          <div className="mb-4 p-4 bg-muted/50 rounded-lg space-y-3">
            <h4 className="font-medium">{editId ? 'Edit Course Type' : 'New Course Type'}</h4>
            {teacherWarning && (
              <Alert className="border-yellow-200 bg-yellow-50 text-yellow-800">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{teacherWarning}</AlertDescription>
              </Alert>
            )}
            {editingCourseType && !editingCourseType.canEditPricingDirectly && (
              <Alert className="border-blue-200 bg-blue-50 text-blue-800">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>
                  Pricing has been used in invoices. Changing the price will create a new pricing version.
                </AlertDescription>
              </Alert>
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
                <Label>Price Adult *</Label>
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
                <Label>Price Child</Label>
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
                onClick={handleSubmit}
                disabled={!isFormValid || createMutation.isPending || updateMutation.isPending || updatePricingMutation.isPending}
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
              <div key={lt.id} className="py-3">
                <div className="flex items-center justify-between">
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
                    {lt.currentPricing ? (
                      <>
                        <p className="text-sm">Adult: {formatCurrency(lt.currentPricing.priceAdult)}</p>
                        <p className="text-sm text-muted-foreground">Child: {formatCurrency(lt.currentPricing.priceChild)}</p>
                      </>
                    ) : (
                      <p className="text-sm text-muted-foreground">No pricing</p>
                    )}
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
                {lt.pricingHistory && lt.pricingHistory.length > 1 && (
                  <PricingHistoryCollapsible pricingHistory={lt.pricingHistory} />
                )}
              </div>
            ))}
          </div>
        )}
      </CardContent>

      {/* New Pricing Version Dialog */}
      <NewPricingVersionDialog
        open={pricingDialogOpen}
        onOpenChange={(open) => {
          setPricingDialogOpen(open)
          if (!open) {
            setPricingDialogCourseType(null)
            setPricingDialogError(null)
          }
        }}
        courseTypeName={pricingDialogCourseType?.name ?? ''}
        currentPricing={pricingDialogCourseType?.currentPricing ?? null}
        childDiscountPercent={childDiscountPercent}
        onSubmit={handlePricingVersionSubmit}
        isLoading={createPricingVersionMutation.isPending}
        error={pricingDialogError}
      />
    </Card>
  )
}
