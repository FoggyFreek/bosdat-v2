import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
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
import { instrumentsApi } from '@/features/instruments/api'
import { courseTypesApi } from '@/features/course-types/api'
import { useAuth } from '@/context/AuthContext'
import { validateEmail } from '@/lib/utils'
import type { Teacher, TeacherRole, CreateTeacher } from '@/features/teachers/types'
import type { Instrument } from '@/features/instruments/types'
import type { CourseTypeSimple } from '@/features/course-types/types'

interface TeacherFormProps {
  readonly teacher?: Teacher
  readonly onSubmit: (data: CreateTeacher) => Promise<{ id: string }>
  readonly isSubmitting: boolean
  readonly error?: string
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
  courseTypeIds: string[]
}

interface FormErrors {
  firstName?: string
  lastName?: string
  email?: string
  hourlyRate?: string
}

const FINANCIAL_ADMIN_ROLE = 'FinancialAdmin'

const VALID_TEACHER_ROLES = ['Teacher', 'Admin', 'Staff'] as const
function isTeacherRole(value: string): value is TeacherRole {
  return VALID_TEACHER_ROLES.includes(value as TeacherRole)
}

export function TeacherForm({ teacher, onSubmit, isSubmitting, error }: TeacherFormProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { user } = useAuth()
  const isEditMode = !!teacher

  const canViewHourlyRate = user?.roles.includes(FINANCIAL_ADMIN_ROLE) || user?.roles.includes('Admin')

  const getSubmitButtonText = () => {
    if (isSubmitting) return t('teachers.actions.saving')
    return isEditMode ? t('teachers.actions.updateTeacher') : t('teachers.actions.createTeacher')
  }

  const { data: instruments = [] } = useQuery<Instrument[]>({
    queryKey: ['instruments'],
    queryFn: () => instrumentsApi.getAll({ activeOnly: true }),
  })

  const [formData, setFormData] = useState<FormData>(() => ({
    firstName: teacher?.firstName ?? '',
    lastName: teacher?.lastName ?? '',
    prefix: teacher?.prefix ?? '',
    email: teacher?.email ?? '',
    phone: teacher?.phone ?? '',
    address: teacher?.address ?? '',
    postalCode: teacher?.postalCode ?? '',
    city: teacher?.city ?? '',
    hourlyRate: teacher?.hourlyRate?.toString() ?? '',
    role: teacher?.role ?? 'Teacher',
    notes: teacher?.notes ?? '',
    isActive: teacher?.isActive ?? true,
    instrumentIds: teacher?.instruments?.map((i) => i.id) ?? [],
    courseTypeIds: teacher?.courseTypes?.map((lt) => lt.id) ?? [],
  }))

  // Query all active lesson types, filter by selected instruments client-side
  const { data: allCourseTypes = [] } = useQuery<CourseTypeSimple[]>({
    queryKey: ['courseTypes', 'active'],
    queryFn: () => courseTypesApi.getAll({ activeOnly: true }),
  })

  // Filter available lesson types by selected instruments
  const availableCourseTypes = allCourseTypes.filter(
    (lt) => formData.instrumentIds.includes(lt.instrumentId)
  )

  const [errors, setErrors] = useState<FormErrors>({})



  const validate = (): boolean => {
    const newErrors: FormErrors = {}

    if (!formData.firstName.trim()) {
      newErrors.firstName = t('teachers.validation.firstNameRequired')
    }

    if (!formData.lastName.trim()) {
      newErrors.lastName = t('teachers.validation.lastNameRequired')
    }

    if (!formData.email.trim()) {
      newErrors.email = t('teachers.validation.emailRequired')
    } else if (!validateEmail(formData.email)) {
      newErrors.email = t('teachers.validation.emailInvalid')
    }

    if (canViewHourlyRate && formData.hourlyRate) {
      const rate = Number.parseFloat(formData.hourlyRate)
      if (Number.isNaN(rate) || rate < 0) {
        newErrors.hourlyRate = t('teachers.validation.hourlyRateInvalid')
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
        ? Number.parseFloat(formData.hourlyRate)
        : teacher?.hourlyRate || 0,
      isActive: formData.isActive,  
      role: formData.role,
      notes: formData.notes.trim() || undefined,
      instrumentIds: formData.instrumentIds,
      courseTypeIds: formData.courseTypeIds,
    }

    const result = await onSubmit(submitData)
    if (result?.id) {
      navigate(`/teachers/${result.id}`)
    }
  }

  const handleChange = (field: keyof FormData, value: string | boolean | number[] | string[]) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }))
    }
  }

  const handleInstrumentToggle = (instrumentId: number, checked: boolean) => {
    const newInstrumentIds = checked
      ? [...formData.instrumentIds, instrumentId]
      : formData.instrumentIds.filter((id) => id !== instrumentId)

    // When removing an instrument, also remove its associated course types
    const validCourseTypeIds = formData.courseTypeIds.filter((ltId) => {
      const courseType = allCourseTypes.find((lt) => lt.id === ltId)
      return courseType && newInstrumentIds.includes(courseType.instrumentId)
    })

    setFormData((prev) => ({
      ...prev,
      instrumentIds: newInstrumentIds,
      courseTypeIds: validCourseTypeIds,
    }))
  }

  const handleCourseTypeToggle = (courseTypeId: string, checked: boolean) => {
    const newIds = checked
      ? [...formData.courseTypeIds, courseTypeId]
      : formData.courseTypeIds.filter((id) => id !== courseTypeId)
    handleChange('courseTypeIds', newIds)
  }

  // Group available lesson types by instrument
  const courseTypesByInstrument = availableCourseTypes.reduce(
    (acc, lt) => {
      if (!acc[lt.instrumentName]) {
        acc[lt.instrumentName] = []
      }
      acc[lt.instrumentName].push(lt)
      return acc
    },
    {} as Record<string, CourseTypeSimple[]>
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
        <h3 className="text-lg font-medium">{t('teachers.sections.basicInfo')}</h3>
        <div className="grid gap-6 md:grid-cols-3">
          <div className="space-y-2">
            <Label htmlFor="firstName">
              {t('teachers.form.firstName')} <span className="text-red-500">*</span>
            </Label>
            <Input
              id="firstName"
              value={formData.firstName}
              onChange={(e) => handleChange('firstName', e.target.value)}
              placeholder={t('teachers.form.placeholders.firstName')}
              className={errors.firstName ? 'border-red-500' : ''}
            />
            {errors.firstName && (
              <p className="text-sm text-red-500">{errors.firstName}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="prefix">{t('teachers.form.prefix')}</Label>
            <Input
              id="prefix"
              value={formData.prefix}
              onChange={(e) => handleChange('prefix', e.target.value)}
              placeholder={t('teachers.form.placeholders.prefix')}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="lastName">
              {t('teachers.form.lastName')} <span className="text-red-500">*</span>
            </Label>
            <Input
              id="lastName"
              value={formData.lastName}
              onChange={(e) => handleChange('lastName', e.target.value)}
              placeholder={t('teachers.form.placeholders.lastName')}
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
              {t('teachers.form.email')} <span className="text-red-500">*</span>
            </Label>
            <Input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              placeholder={t('teachers.form.placeholders.email')}
              className={errors.email ? 'border-red-500' : ''}
            />
            {errors.email && (
              <p className="text-sm text-red-500">{errors.email}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="phone">{t('teachers.form.phone')}</Label>
            <Input
              id="phone"
              type="tel"
              value={formData.phone}
              onChange={(e) => handleChange('phone', e.target.value)}
              placeholder={t('teachers.form.placeholders.phone')}
            />
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="role">{t('teachers.form.role')}</Label>
            <Select
              value={formData.role}
              onValueChange={(value) => {
                if (isTeacherRole(value)) {
                  handleChange('role', value)
                }
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder={t('teachers.form.placeholders.role')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Teacher">{t('teachers.roles.teacher')}</SelectItem>
                <SelectItem value="Admin">{t('teachers.roles.admin')}</SelectItem>
                <SelectItem value="Staff">{t('teachers.roles.staff')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {isEditMode && (
            <div className="space-y-2">
              <Label>{t('students.form.status')}</Label>
              <div className="flex items-center space-x-2 pt-2">
                <Checkbox
                  id="isActive"
                  checked={formData.isActive}
                  onCheckedChange={(checked) => handleChange('isActive', !!checked)}
                />
                <Label htmlFor="isActive" className="font-normal">
                  {t('teachers.form.isActive')}
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
        {formData.instrumentIds.length === 0 && (
          <p className="text-sm text-muted-foreground">
            Please select at least one instrument to see available lesson types
          </p>
        )}

        {formData.instrumentIds.length > 0 && availableCourseTypes.length === 0 && (
          <p className="text-sm text-muted-foreground">
            No lesson types available for the selected instruments
          </p>
        )}

        {formData.instrumentIds.length > 0 && availableCourseTypes.length > 0 && (
          <div className="space-y-4">
            {Object.entries(courseTypesByInstrument).map(([instrumentName, courseTypes]) => (
              <div key={instrumentName}>
                <h4 className="text-sm font-medium mb-2">{instrumentName}</h4>
                <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
                  {courseTypes.map((courseType) => (
                    <div key={courseType.id} className="flex items-center space-x-2">
                      <Checkbox
                        id={`courseType-${courseType.id}`}
                        checked={formData.courseTypeIds.includes(courseType.id)}
                        onCheckedChange={(checked) =>
                          handleCourseTypeToggle(courseType.id, !!checked)
                        }
                      />
                      <Label htmlFor={`courseType-${courseType.id}`} className="font-normal">
                        {courseType.name} ({courseType.durationMinutes} min)
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
          {getSubmitButtonText()}
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
