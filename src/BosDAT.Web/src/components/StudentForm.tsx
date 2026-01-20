import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import type { Student, StudentStatus, CreateStudent } from '@/types'

interface StudentFormProps {
  student?: Student
  onSubmit: (data: CreateStudent) => Promise<{ id: string }>
  isSubmitting: boolean
  error?: string
}

interface FormData {
  firstName: string
  lastName: string
  email: string
  phone: string
  status: StudentStatus
}

interface FormErrors {
  firstName?: string
  lastName?: string
  email?: string
}

export function StudentForm({ student, onSubmit, isSubmitting, error }: StudentFormProps) {
  const navigate = useNavigate()
  const isEditMode = !!student

  const [formData, setFormData] = useState<FormData>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    status: 'Active',
  })

  const [errors, setErrors] = useState<FormErrors>({})

  useEffect(() => {
    if (student) {
      setFormData({
        firstName: student.firstName,
        lastName: student.lastName,
        email: student.email,
        phone: student.phone || '',
        status: student.status,
      })
    }
  }, [student])

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

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validate()) {
      return
    }

    const submitData: CreateStudent = {
      firstName: formData.firstName.trim(),
      lastName: formData.lastName.trim(),
      email: formData.email.trim(),
      phone: formData.phone.trim() || undefined,
      status: formData.status,
    }

    const result = await onSubmit(submitData)
    if (result?.id) {
      navigate(`/students/${result.id}`)
    }
  }

  const handleChange = (field: keyof FormData, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6" noValidate>
      {error && (
        <div className="rounded-md bg-red-50 p-4">
          <p className="text-sm text-red-800">{error}</p>
        </div>
      )}

      <div className="grid gap-6 md:grid-cols-2">
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

      <div className="space-y-2">
        <Label htmlFor="status">Status</Label>
        <Select
          value={formData.status}
          onValueChange={(value) => handleChange('status', value)}
        >
          <SelectTrigger className="w-full md:w-[200px]">
            <SelectValue placeholder="Select status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Active">Active</SelectItem>
            <SelectItem value="Inactive">Inactive</SelectItem>
            <SelectItem value="Trial">Trial</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="flex gap-4 pt-4">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting
            ? isEditMode
              ? 'Saving...'
              : 'Creating...'
            : isEditMode
              ? 'Save Changes'
              : 'Create Student'}
        </Button>
        <Button type="button" variant="outline" onClick={() => navigate(-1)}>
          Cancel
        </Button>
      </div>
    </form>
  )
}
