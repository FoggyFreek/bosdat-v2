import { AlertCircle } from 'lucide-react'
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
import { Alert, AlertDescription } from '@/components/ui/alert'
import { DURATION_OPTIONS, type CourseTypeFormData } from '../hooks/useCourseTypeForm'
import type { Instrument } from '@/features/instruments/types'
import type { CourseType } from '@/features/course-types/types'

export interface CourseTypeFormProps {
  formData: CourseTypeFormData
  instruments: Instrument[]
  editId: string | null
  editingCourseType: CourseType | null
  useCustomDuration: boolean
  teacherWarning: string | null
  childDiscountPercent: number
  isFormValid: boolean
  isSubmitting: boolean
  onFormDataChange: (updates: Partial<CourseTypeFormData>) => void
  onInstrumentChange: (value: string) => void
  onTypeChange: (value: string) => void
  onAdultPriceChange: (value: string) => void
  onChildPriceChange: (value: string) => void
  onCustomDurationToggle: () => void
  onCancel: () => void
  onSubmit: () => void
}

export const CourseTypeForm = ({
  formData,
  instruments,
  editId,
  editingCourseType,
  useCustomDuration,
  teacherWarning,
  childDiscountPercent,
  isFormValid,
  isSubmitting,
  onFormDataChange,
  onInstrumentChange,
  onTypeChange,
  onAdultPriceChange,
  onChildPriceChange,
  onCustomDurationToggle,
  onCancel,
  onSubmit,
}: CourseTypeFormProps) => {
  const hasChildPriceError = Number.parseFloat(formData.priceChild) > Number.parseFloat(formData.priceAdult)

  return (
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
            onChange={(e) => onFormDataChange({ name: e.target.value })}
          />
        </div>

        <div>
          <Label>Instrument *</Label>
          <Select value={formData.instrumentId} onValueChange={onInstrumentChange}>
            <SelectTrigger>
              <SelectValue placeholder="Select instrument" />
            </SelectTrigger>
            <SelectContent>
              {instruments.map((inst) => (
                <SelectItem key={inst.id} value={inst.id.toString()}>
                  {inst.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div>
          <Label>Duration (min)</Label>
          <div className="flex gap-2">
            {useCustomDuration ? (
              <Input
                type="number"
                min="1"
                placeholder="Custom"
                value={formData.customDuration}
                onChange={(e) => onFormDataChange({ customDuration: e.target.value })}
                className="flex-1"
              />
            ) : (
              <Select
                value={formData.durationMinutes}
                onValueChange={(v) => onFormDataChange({ durationMinutes: v })}
              >
                <SelectTrigger className="flex-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {DURATION_OPTIONS.map((d) => (
                    <SelectItem key={d} value={d}>
                      {d} min
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onCustomDurationToggle}
            >
              {useCustomDuration ? 'Preset' : 'Custom'}
            </Button>
          </div>
        </div>

        <div>
          <Label>Type</Label>
          <Select value={formData.type} onValueChange={onTypeChange}>
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
            onChange={(e) => onAdultPriceChange(e.target.value)}
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
            onChange={(e) => onChildPriceChange(e.target.value)}
            className={hasChildPriceError ? 'border-red-500' : ''}
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
              onChange={(e) => onFormDataChange({ maxStudents: e.target.value })}
            />
          </div>
        )}
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button onClick={onSubmit} disabled={!isFormValid || isSubmitting}>
          {editId ? 'Save' : 'Create'}
        </Button>
      </div>
    </div>
  )
}
