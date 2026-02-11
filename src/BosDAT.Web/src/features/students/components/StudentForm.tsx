import { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
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
import { useDuplicateCheck } from '@/hooks/useDuplicateCheck'
import { validateEmail } from '@/lib/utils'
import type { Student, StudentStatus, Gender, CreateStudent } from '@/features/students/types'
import { studentStatusTranslations } from '@/features/students/types'

const getStatusBadgeClass = (status: StudentStatus) => {
  if (status === 'Active') return 'bg-green-100 text-green-700'
  if (status === 'Trial') return 'bg-yellow-100 text-yellow-700'
  return 'bg-gray-100 text-gray-700'
}

interface StudentFormProps {
  readonly student?: Student
  readonly onSubmit: (data: CreateStudent) => Promise<{ id: string }>
  readonly isSubmitting: boolean
  readonly error?: string
  readonly onSuccess?: (student: Student) => void
}

interface FormData {
  firstName: string
  lastName: string
  prefix: string
  email: string
  phone: string
  phoneAlt: string
  address: string
  postalCode: string
  city: string
  dateOfBirth: string
  gender: Gender | ''
  status: StudentStatus
  billingContactName: string
  billingContactEmail: string
  billingContactPhone: string
  billingAddress: string
  billingPostalCode: string
  billingCity: string
  autoDebit: boolean
  notes: string
}

interface FormErrors {
  firstName?: string
  lastName?: string
  email?: string
  billingContactEmail?: string
}

export function StudentForm({ student, onSubmit, isSubmitting, error, onSuccess }: StudentFormProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const isEditMode = !!student

  const [formData, setFormData] = useState<FormData>(() => ({
    firstName: student?.firstName ?? '',
    lastName: student?.lastName ?? '',
    prefix: student?.prefix ?? '',
    email: student?.email ?? '',
    phone: student?.phone ?? '',
    phoneAlt: student?.phoneAlt ?? '',
    address: student?.address ?? '',
    postalCode: student?.postalCode ?? '',
    city: student?.city ?? '',
    dateOfBirth: student?.dateOfBirth ?? '',
    gender: student?.gender ?? '',
    status: student?.status ?? 'Active',
    billingContactName: student?.billingContactName ?? '',
    billingContactEmail: student?.billingContactEmail ?? '',
    billingContactPhone: student?.billingContactPhone ?? '',
    billingAddress: student?.billingAddress ?? '',
    billingPostalCode: student?.billingPostalCode ?? '',
    billingCity: student?.billingCity ?? '',
    autoDebit: student?.autoDebit ?? false,
    notes: student?.notes ?? '',
  }))

  const [errors, setErrors] = useState<FormErrors>({})

  const {
    duplicates,
    hasDuplicates,
    isChecking,
    checkDuplicates,
    acknowledgedDuplicates,
    acknowledgeDuplicates,
    resetAcknowledgement,
  } = useDuplicateCheck({ excludeId: student?.id })

  const getSubmitButtonText = () => {
    if (isSubmitting) {
      return isEditMode ? t('students.actions.saving') : t('students.actions.creating')
    }
    return isEditMode ? t('students.actions.saveChanges') : t('students.actions.createStudent')
  }

  // Trigger duplicate check when key fields change
  useEffect(() => {
    if (formData.firstName && formData.lastName && formData.email) {
      checkDuplicates({
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        phone: formData.phone || undefined,
        dateOfBirth: formData.dateOfBirth || undefined,
      })
    }
  }, [formData.firstName, formData.lastName, formData.email, formData.phone, formData.dateOfBirth, checkDuplicates])


  const validate = (): boolean => {
    const newErrors: FormErrors = {}

    if (!formData.firstName.trim()) {
      newErrors.firstName = t('students.validation.firstNameRequired')
    }

    if (!formData.lastName.trim()) {
      newErrors.lastName = t('students.validation.lastNameRequired')
    }

    if (!formData.email.trim()) {
      newErrors.email = t('students.validation.emailRequired')
    } else if (!validateEmail(formData.email)) {
      newErrors.email = t('students.validation.emailInvalid')
    }

    if (formData.billingContactEmail.trim() && !validateEmail(formData.billingContactEmail)) {
      newErrors.billingContactEmail = t('students.validation.billingEmailInvalid')
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) {
      return
    }

    // Block submission if duplicates exist and not acknowledged
    if (hasDuplicates && !acknowledgedDuplicates) {
      return
    }

    const submitData: CreateStudent = {
      firstName: formData.firstName.trim(),
      lastName: formData.lastName.trim(),
      prefix: formData.prefix.trim() || undefined,
      email: formData.email.trim(),
      phone: formData.phone.trim() || undefined,
      phoneAlt: formData.phoneAlt.trim() || undefined,
      address: formData.address.trim() || undefined,
      postalCode: formData.postalCode.trim() || undefined,
      city: formData.city.trim() || undefined,
      dateOfBirth: formData.dateOfBirth || undefined,
      gender: formData.gender || undefined,
      status: formData.status,
      billingContactName: formData.billingContactName.trim() || undefined,
      billingContactEmail: formData.billingContactEmail.trim() || undefined,
      billingContactPhone: formData.billingContactPhone.trim() || undefined,
      billingAddress: formData.billingAddress.trim() || undefined,
      billingPostalCode: formData.billingPostalCode.trim() || undefined,
      billingCity: formData.billingCity.trim() || undefined,
      autoDebit: formData.autoDebit,
      notes: formData.notes.trim() || undefined,
    }

    const result = await onSubmit(submitData)
    if (result?.id) {
      // If onSuccess callback is provided, use it instead of navigating
      if (onSuccess) {
        onSuccess(result as unknown as Student)
      } else {
        navigate(`/students/${result.id}`)
      }
    }
  }

  const handleChange = (field: keyof FormData, value: string | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }))
    }
    // Reset acknowledgement when key fields change
    if (['firstName', 'lastName', 'email', 'phone', 'dateOfBirth'].includes(field)) {
      resetAcknowledgement()
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-8" noValidate>
      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <p className="text-sm text-red-800">{error}</p>
        </div>
      )}

      {/* Duplicate Warning */}
      {hasDuplicates && (
        <div className="rounded-md border border-amber-200 bg-amber-50 p-4">
          <div className="flex items-start gap-3">
            <svg
              className="h-5 w-5 text-amber-600 mt-0.5 shrink-0"
              fill="none"
              viewBox="0 0 24 24"
              strokeWidth={1.5}
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z"
              />
            </svg>
            <div className="flex-1">
              <h4 className="text-sm font-medium text-amber-800">
                {t('students.duplicate.title')}
              </h4>
              <p className="mt-1 text-sm text-amber-700">
                {t('students.duplicate.description')}
              </p>
              <ul className="mt-3 space-y-2">
                {duplicates.map((duplicate) => (
                  <li
                    key={duplicate.id}
                    className="flex items-center justify-between rounded-md bg-white p-3 shadow-xs"
                  >
                    <div>
                      <Link
                        to={`/students/${duplicate.id}`}
                        className="font-medium text-amber-900 hover:underline"
                      >
                        {duplicate.fullName}
                      </Link>
                      <p className="text-sm text-amber-700">{duplicate.email}</p>
                      <p className="text-xs text-amber-600 mt-1">
                        {t('students.duplicate.match', {
                          reason: duplicate.matchReason,
                          score: duplicate.confidenceScore
                        })}
                      </p>
                    </div>
                    <span
                      className={`inline-flex items-center rounded-full px-2 py-1 text-xs font-medium ${getStatusBadgeClass(duplicate.status)}`}
                    >
                      {t(studentStatusTranslations[duplicate.status])}
                    </span>
                  </li>
                ))}
              </ul>
              {!acknowledgedDuplicates && (
                <div className="mt-4 flex items-center gap-3">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={acknowledgeDuplicates}
                    className="border-amber-300 text-amber-800 hover:bg-amber-100"
                  >
                    {t('students.duplicate.acknowledge')}
                  </Button>
                  <span className="text-xs text-amber-600">
                    {t('students.duplicate.acknowledgementRequired')}
                  </span>
                </div>
              )}
              {acknowledgedDuplicates && (
                <p className="mt-3 text-sm text-green-700 flex items-center gap-1">
                  <svg
                    className="h-4 w-4"
                    fill="none"
                    viewBox="0 0 24 24"
                    strokeWidth={2}
                    stroke="currentColor"
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                  </svg>
                  {t('students.duplicate.acknowledged')}
                </p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Duplicate check loading indicator */}
      {isChecking && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <svg
            className="h-4 w-4 animate-spin"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
            />
          </svg>
          {t('students.duplicate.checking')}
        </div>
      )}

      {/* Basic Information Section */}
      <div className="space-y-4">
        <h3 className="text-lg font-medium">{t('students.sections.basicInfo')}</h3>
        <div className="grid gap-6 md:grid-cols-3">
          {isEditMode && (
            <div className="space-y-2">
              <Label htmlFor="prefix">{t('students.form.prefix')}</Label>
              <Input
                id="prefix"
                value={formData.prefix}
                onChange={(e) => handleChange('prefix', e.target.value)}
                placeholder={t('students.form.placeholders.prefix')}
              />
            </div>
          )}
          <div className="space-y-2">
            <Label htmlFor="firstName">
              {t('students.form.firstName')} <span className="text-red-500">*</span>
            </Label>
            <Input
              id="firstName"
              value={formData.firstName}
              onChange={(e) => handleChange('firstName', e.target.value)}
              placeholder={t('students.form.placeholders.firstName')}
              className={errors.firstName ? 'border-red-500' : ''}
            />
            {errors.firstName && (
              <p className="text-sm text-red-500">{errors.firstName}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="lastName">
              {t('students.form.lastName')} <span className="text-red-500">*</span>
            </Label>
            <Input
              id="lastName"
              value={formData.lastName}
              onChange={(e) => handleChange('lastName', e.target.value)}
              placeholder={t('students.form.placeholders.lastName')}
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
              {t('students.form.email')} <span className="text-red-500">*</span>
            </Label>
            <Input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              placeholder={t('students.form.placeholders.email')}
              className={errors.email ? 'border-red-500' : ''}
            />
            {errors.email && (
              <p className="text-sm text-red-500">{errors.email}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="phone">{t('students.form.phone')}</Label>
            <Input
              id="phone"
              type="tel"
              value={formData.phone}
              onChange={(e) => handleChange('phone', e.target.value)}
              placeholder={t('students.form.placeholders.phone')}
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="status">{t('students.form.status')}</Label>
          <Select
            value={formData.status}
            onValueChange={(value) => handleChange('status', value)}
          >
            <SelectTrigger className="w-full md:w-[200px]">
              <SelectValue placeholder={t('students.form.placeholders.status')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Active">{t('common.status.active')}</SelectItem>
              <SelectItem value="Inactive">{t('common.status.inactive')}</SelectItem>
              <SelectItem value="Trial">{t('common.status.trial')}</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Edit Mode: Additional Fields */}
      {isEditMode && (
        <>
          {/* Contact Section */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">{t('students.sections.contact')}</h3>
            <div className="grid gap-6 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="phoneAlt">{t('students.form.phoneAlt')}</Label>
                <Input
                  id="phoneAlt"
                  type="tel"
                  value={formData.phoneAlt}
                  onChange={(e) => handleChange('phoneAlt', e.target.value)}
                  placeholder={t('students.form.placeholders.phoneAlt')}
                />
              </div>
            </div>
          </div>

          {/* Address Section */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">{t('students.sections.address')}</h3>
            <div className="space-y-2">
              <Label htmlFor="address">{t('students.form.address')}</Label>
              <Input
                id="address"
                value={formData.address}
                onChange={(e) => handleChange('address', e.target.value)}
                placeholder={t('students.form.placeholders.address')}
              />
            </div>
            <div className="grid gap-6 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="postalCode">{t('students.form.postalCode')}</Label>
                <Input
                  id="postalCode"
                  value={formData.postalCode}
                  onChange={(e) => handleChange('postalCode', e.target.value)}
                  placeholder={t('students.form.placeholders.postalCode')}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="city">{t('students.form.city')}</Label>
                <Input
                  id="city"
                  value={formData.city}
                  onChange={(e) => handleChange('city', e.target.value)}
                  placeholder={t('students.form.placeholders.city')}
                />
              </div>
            </div>
          </div>

          {/* Personal Section */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">{t('students.sections.personal')}</h3>
            <div className="grid gap-6 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="dateOfBirth">{t('students.form.dateOfBirth')}</Label>
                <Input
                  id="dateOfBirth"
                  type="date"
                  value={formData.dateOfBirth}
                  onChange={(e) => handleChange('dateOfBirth', e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="gender">{t('students.form.gender')}</Label>
                <Select
                  value={formData.gender}
                  onValueChange={(value) => handleChange('gender', value)}
                >
                  <SelectTrigger>
                    <SelectValue placeholder={t('students.form.placeholders.gender')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Male">{t('students.gender.male')}</SelectItem>
                    <SelectItem value="Female">{t('students.gender.female')}</SelectItem>
                    <SelectItem value="Other">{t('students.gender.other')}</SelectItem>
                    <SelectItem value="PreferNotToSay">{t('students.gender.preferNotToSay')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>

          {/* Billing / Payer Section */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">{t('students.sections.billing')}</h3>
            <p className="text-sm text-muted-foreground">
              {t('students.billingHelp')}
            </p>
            <div className="space-y-2">
              <Label htmlFor="billingContactName">{t('students.form.billingContactName')}</Label>
              <Input
                id="billingContactName"
                value={formData.billingContactName}
                onChange={(e) => handleChange('billingContactName', e.target.value)}
                placeholder={t('students.form.placeholders.billingContactName')}
              />
            </div>
            <div className="grid gap-6 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="billingContactEmail">{t('students.form.billingContactEmail')}</Label>
                <Input
                  id="billingContactEmail"
                  type="email"
                  value={formData.billingContactEmail}
                  onChange={(e) => handleChange('billingContactEmail', e.target.value)}
                  placeholder={t('students.form.placeholders.billingContactEmail')}
                  className={errors.billingContactEmail ? 'border-red-500' : ''}
                />
                {errors.billingContactEmail && (
                  <p className="text-sm text-red-500">{errors.billingContactEmail}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="billingContactPhone">{t('students.form.billingContactPhone')}</Label>
                <Input
                  id="billingContactPhone"
                  type="tel"
                  value={formData.billingContactPhone}
                  onChange={(e) => handleChange('billingContactPhone', e.target.value)}
                  placeholder={t('students.form.placeholders.billingContactPhone')}
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="billingAddress">{t('students.form.billingAddress')}</Label>
              <Input
                id="billingAddress"
                value={formData.billingAddress}
                onChange={(e) => handleChange('billingAddress', e.target.value)}
                placeholder={t('students.form.placeholders.address')}
              />
            </div>
            <div className="grid gap-6 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="billingPostalCode">{t('students.form.billingPostalCode')}</Label>
                <Input
                  id="billingPostalCode"
                  value={formData.billingPostalCode}
                  onChange={(e) => handleChange('billingPostalCode', e.target.value)}
                  placeholder={t('students.form.placeholders.postalCode')}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="billingCity">{t('students.form.billingCity')}</Label>
                <Input
                  id="billingCity"
                  value={formData.billingCity}
                  onChange={(e) => handleChange('billingCity', e.target.value)}
                  placeholder={t('students.form.placeholders.city')}
                />
              </div>
            </div>
          </div>

          {/* Other Section */}
          <div className="space-y-4">
            <h3 className="text-lg font-medium">{t('students.sections.other')}</h3>
            <div className="flex items-center space-x-2">
              <Checkbox
                id="autoDebit"
                checked={formData.autoDebit}
                onCheckedChange={(checked) => handleChange('autoDebit', !!checked)}
              />
              <Label htmlFor="autoDebit" className="font-normal">
                {t('students.form.autoDebit')}
              </Label>
            </div>
            <div className="space-y-2">
              <Label htmlFor="notes">{t('students.form.notes')}</Label>
              <Textarea
                id="notes"
                value={formData.notes}
                onChange={(e) => handleChange('notes', e.target.value)}
                placeholder={t('students.form.placeholders.notes')}
                rows={4}
              />
            </div>
          </div>
        </>
      )}

      <div className="flex gap-4 pt-4">
        <Button
          type="submit"
          disabled={isSubmitting || (hasDuplicates && !acknowledgedDuplicates)}
        >
          {getSubmitButtonText()}
        </Button>
        {!onSuccess && (
          <Button type="button" variant="outline" onClick={() => navigate(-1)}>
            {t('common.actions.cancel')}
          </Button>
        )}
        {hasDuplicates && !acknowledgedDuplicates && (
          <span className="flex items-center text-sm text-amber-600">
            {t('students.duplicate.reviewWarning')}
          </span>
        )}
      </div>
    </form>
  )
}
