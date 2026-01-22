import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { instrumentsApi, lessonTypesApi } from '@/services/api'
import { useAuth } from '@/context/AuthContext'
import type { Teacher, TeacherRole, CreateTeacher } from '@/features/teachers/types'
import type { Instrument } from '@/features/instruments/types'
import type { LessonTypeSimple } from '@/features/lesson-types/types'

interface TeacherFormProps {
  teacher?: Teacher
  onSubmit: (data: CreateTeacher) => Promise<{ id: string }>
  isSubmitting: boolean
  error?: string
}

interface FormData {
  firstName: string
  lastName: string
  prefix: string
  email: string
  phone: string
  address: string
  postalCode: string
  city: string
  hourlyRate: string
  role: TeacherRole
  notes: string
  isActive: boolean
  instrumentIds: number[]
  lessonTypeIds: number[]
}

interface FormErrors {
  firstName?: string
  lastName?: string
  email?: string
  hourlyRate?: string
}

const FINANCIAL_ADMIN_ROLE = 'FinancialAdmin'

export function TeacherForm({ teacher, onSubmit, isSubmitting, error }: TeacherFormProps) {
  const navigate = useNavigate()
  const { user } = useAuth()
  const isEditMode = !!teacher

  const canViewHourlyRate = user?.roles.includes(FINANCIAL_ADMIN_ROLE) || user?.roles.includes('Admin')

  const { data: instruments = [] } = useQuery<Instrument[]>({
    queryKey: ['instruments'],
    queryFn: () => instrumentsApi.getAll({ activeOnly: true }),
  })

  const [formData, setFormData] = useState<FormData>({
    firstName: '',
    lastName: '',
    prefix: '',
    email: '',
    phone: '',
    address: '',
    postalCode: '',
    city: '',
    hourlyRate: '',
    role: 'Teacher',
    notes: '',
    isActive: true,
    instrumentIds: [],
    lessonTypeIds: [],
  })

  // Query all active lesson types, filter by selected instruments client-side
  const { data: allLessonTypes = [] } = useQuery<LessonTypeSimple[]>({
    queryKey: ['lessonTypes', 'active'],
    queryFn: () => lessonTypesApi.getAll({ activeOnly: true }),
  })

  // Filter available lesson types by selected instruments
  const availableLessonTypes = allLessonTypes.filter(
    (lt) => formData.instrumentIds.includes(lt.instrumentId)
  )

  const [errors, setErrors] = useState<FormErrors>({})

  useEffect(() => {
    if (teacher) {
      setFormData({
        firstName: teacher.firstName,
        lastName: teacher.lastName,
        prefix: teacher.prefix || '',
        email: teacher.email,
        phone: teacher.phone || '',
        address: teacher.address || '',
        postalCode: teacher.postalCode || '',
        city: teacher.city || '',
        hourlyRate: teacher.hourlyRate?.toString() || '',
        role: teacher.role,
        notes: teacher.notes || '',
        isActive: teacher.isActive,
        instrumentIds: teacher.instruments?.map((i) => i.id) || [],
        lessonTypeIds: teacher.lessonTypes?.map((lt) => lt.id) || [],
      })
    }
  }, [teacher])

  // Remove lesson types when their instrument is removed
  useEffect(() => {
    if (allLessonTypes.length > 0 && formData.lessonTypeIds.length > 0) {
      const validLessonTypeIds = formData.lessonTypeIds.filter((ltId) => {
        const lessonType = allLessonTypes.find((lt) => lt.id === ltId)
        return lessonType && formData.instrumentIds.includes(lessonType.instrumentId)
      })
      if (validLessonTypeIds.length !== formData.lessonTypeIds.length) {
        setFormData((prev) => ({ ...prev, lessonTypeIds: validLessonTypeIds }))
      }
    }
  }, [formData.instrumentIds, formData.lessonTypeIds, allLessonTypes])

  const validateEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    return emailRegex.test(email)
  }

  const validate = (): boolean => {
    const newErrors: FormErrors = {}

    if (!formData.firstName.trim()) {
      newErrors.firstName = 'First name is required'
    }

    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Last name is required'
    }

    if (!formData.email.trim()) {
      newErrors.email = 'Email is required'
    } else if (!validateEmail(formData.email)) {
      newErrors.email = 'Please enter a valid email address'
    }

    if (canViewHourlyRate && formData.hourlyRate) {
      const rate = parseFloat(formData.hourlyRate)
      if (isNaN(rate) || rate < 0) {
        newErrors.hourlyRate = 'Please enter a valid hourly rate'
      }
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) {
      return
    }

    const submitData: CreateTeacher = {
      firstName: formData.firstName.trim(),
      lastName: formData.lastName.trim(),
      prefix: formData.prefix.trim() || undefined,
      email: formData.email.trim(),
      phone: formData.phone.trim() || undefined,
      address: formData.address.trim() || undefined,
      postalCode: formData.postalCode.trim() || undefined,
      city: formData.city.trim() || undefined,
      hourlyRate: canViewHourlyRate && formData.hourlyRate
        ? parseFloat(formData.hourlyRate)
        : teacher?.hourlyRate || 0,
      isActive: formData.isActive,  
      role: formData.role,
      notes: formData.notes.trim() || undefined,
      instrumentIds: formData.instrumentIds,
      lessonTypeIds: formData.lessonTypeIds,
    }

    const result = await onSubmit(submitData)
    if (result?.id) {
      navigate(`/teachers/${result.id}`)
    }
  }

  const handleChange = (field: keyof FormData, value: string | boolean | number[]) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }))
    }
  }

  const handleInstrumentToggle = (instrumentId: number, checked: boolean) => {
    const newIds = checked
      ? [...formData.instrumentIds, instrumentId]
      : formData.instrumentIds.filter((id) => id !== instrumentId)
    handleChange('instrumentIds', newIds)
  }

  const handleLessonTypeToggle = (lessonTypeId: number, checked: boolean) => {
    const newIds = checked
      ? [...formData.lessonTypeIds, lessonTypeId]
      : formData.lessonTypeIds.filter((id) => id !== lessonTypeId)
    handleChange('lessonTypeIds', newIds)
  }

  // Group available lesson types by instrument
  const lessonTypesByInstrument = availableLessonTypes.reduce(
    (acc, lt) => {
      if (!acc[lt.instrumentName]) {
        acc[lt.instrumentName] = []
      }
      acc[lt.instrumentName].push(lt)
      return acc
    },
    {} as Record<string, LessonTypeSimple[]>
  )

  return (
    <form onSubmit={handleSubmit} className="space-y-8" noValidate>
      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <p className="text-sm text-red-800">{error}</p>
        </div>
      )}

      {/* Basic Information Section */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium">Basic Information</h3>
        <div className="grid gap-6 md:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="firstName">
              First Name <span className="text-red-500">*</span>
            </Label>
            <Input
              id="firstName"
              value={formData.firstName}
              onChange={(e) => handleChange('firstName', e.target.value)}
              placeholder="Enter first name"
              className={errors.firstName ? 'border-red-500' : ''}
            />
            {errors.firstName && (
              <p className="text-sm text-red-500">{errors.firstName}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="prefix">Prefix</Label>
            <Input
              id="prefix"
              value={formData.prefix}
              onChange={(e) => handleChange('prefix', e.target.value)}
              placeholder="e.g., van, de"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="lastName">
              Last Name <span className="text-red-500">*</span>
            </Label>
            <Input
              id="lastName"
              value={formData.lastName}
              onChange={(e) => handleChange('lastName', e.target.value)}
              placeholder="Enter last name"
              className={errors.lastName ? 'border-red-500' : ''}
            />
            {errors.lastName && (
              <p className="text-sm text-red-500">{errors.lastName}</p>
            )}
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="email">
              Email <span className="text-red-500">*</span>
            </Label>
            <Input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              placeholder="Enter email address"
              className={errors.email ? 'border-red-500' : ''}
            />
            {errors.email && (
              <p className="text-sm text-red-500">{errors.email}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="phone">Phone</Label>
            <Input
              id="phone"
              type="tel"
              value={formData.phone}
              onChange={(e) => handleChange('phone', e.target.value)}
              placeholder="Enter phone number"
            />
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="role">Role</Label>
            <Select
              value={formData.role}
              onValueChange={(value) => handleChange('role', value as TeacherRole)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select role" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Teacher">Teacher</SelectItem>
                <SelectItem value="Admin">Admin</SelectItem>
                <SelectItem value="Staff">Staff</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isEditMode && (
            <div className="space-y-2">
              <Label>Status</Label>
              <div className="flex items-center space-x-2 pt-2">
                <Checkbox
                  id="isActive"
                  checked={formData.isActive}
                  onCheckedChange={(checked) => handleChange('isActive', !!checked)}
                />
                <Label htmlFor="isActive" className="font-normal">
                  Active
                </Label>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Address Section */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium">Address</h3>
        <div className="space-y-2">
          <Label htmlFor="address">Address</Label>
          <Input
            id="address"
            value={formData.address}
            onChange={(e) => handleChange('address', e.target.value)}
            placeholder="Street and number"
          />
        </div>
        <div className="grid gap-6 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="postalCode">Postal Code</Label>
            <Input
              id="postalCode"
              value={formData.postalCode}
              onChange={(e) => handleChange('postalCode', e.target.value)}
              placeholder="e.g., 1234 AB"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="city">City</Label>
            <Input
              id="city"
              value={formData.city}
              onChange={(e) => handleChange('city', e.target.value)}
              placeholder="Enter city"
            />
          </div>
        </div>
      </div>

      {/* Instruments Section */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium">Instruments</h3>
        <p className="text-sm text-muted-foreground">
          Select the instruments this teacher can teach
        </p>
        {instruments.length === 0 ? (
          <p className="text-sm text-muted-foreground">No instruments available</p>
        ) : (
          <div className="grid gap-3 md:grid-cols-3 lg:grid-cols-4">
            {instruments.map((instrument) => (
              <div key={instrument.id} className="flex items-center space-x-2">
                <Checkbox
                  id={`instrument-${instrument.id}`}
                  checked={formData.instrumentIds.includes(instrument.id)}
                  onCheckedChange={(checked) =>
                    handleInstrumentToggle(instrument.id, !!checked)
                  }
                />
                <Label htmlFor={`instrument-${instrument.id}`} className="font-normal">
                  {instrument.name}
                </Label>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Lesson Types Section */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium">Lesson Types</h3>
        <p className="text-sm text-muted-foreground">
          Select the lesson types this teacher can teach
        </p>
        {formData.instrumentIds.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Please select at least one instrument to see available lesson types
          </p>
        ) : availableLessonTypes.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No lesson types available for the selected instruments
          </p>
        ) : (
          <div className="space-y-4">
            {Object.entries(lessonTypesByInstrument).map(([instrumentName, lessonTypes]) => (
              <div key={instrumentName}>
                <h4 className="text-sm font-medium mb-2">{instrumentName}</h4>
                <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
                  {lessonTypes.map((lessonType) => (
                    <div key={lessonType.id} className="flex items-center space-x-2">
                      <Checkbox
                        id={`lessonType-${lessonType.id}`}
                        checked={formData.lessonTypeIds.includes(lessonType.id)}
                        onCheckedChange={(checked) =>
                          handleLessonTypeToggle(lessonType.id, !!checked)
                        }
                      />
                      <Label htmlFor={`lessonType-${lessonType.id}`} className="font-normal">
                        {lessonType.name} ({lessonType.durationMinutes} min)
                      </Label>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Financial Information - Only visible to FinancialAdmin */}
      {canViewHourlyRate && (
        <div className="space-y-4">
          <h3 className="text-lg font-medium">Financial Information</h3>
          <div className="grid gap-6 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="hourlyRate">Hourly Rate</Label>
              <Input
                id="hourlyRate"
                type="number"
                step="0.01"
                min="0"
                value={formData.hourlyRate}
                onChange={(e) => handleChange('hourlyRate', e.target.value)}
                placeholder="Enter hourly rate"
                className={errors.hourlyRate ? 'border-red-500' : ''}
              />
              {errors.hourlyRate && (
                <p className="text-sm text-red-500">{errors.hourlyRate}</p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Notes Section */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium">Additional Information</h3>
        <div className="space-y-2">
          <Label htmlFor="notes">Notes</Label>
          <Textarea
            id="notes"
            value={formData.notes}
            onChange={(e) => handleChange('notes', e.target.value)}
            placeholder="Any additional notes about this teacher"
            rows={4}
          />
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex items-center gap-4">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : isEditMode ? 'Update Teacher' : 'Create Teacher'}
        </Button>
        <Button
          type="button"
          variant="outline"
          onClick={() => navigate('/teachers')}
        >
          Cancel
        </Button>
      </div>
    </form>
  )
}
