import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import { Check, X } from 'lucide-react'
import type { InstrumentCategory } from '@/features/instruments/types'

const categories: readonly InstrumentCategory[] = [
  'String',
  'Percussion',
  'Vocal',
  'Keyboard',
  'Wind',
  'Brass',
  'Electronic',
  'Other',
] as const

function isInstrumentCategory(value: string): value is InstrumentCategory {
  return categories.includes(value as InstrumentCategory)
}

interface InstrumentFormData {
  name: string
  category: InstrumentCategory
  isActive: boolean
}

interface InstrumentFormProps {
  readonly formData: InstrumentFormData
  readonly isEdit?: boolean
  readonly isPending?: boolean
  readonly onFormDataChange: (data: InstrumentFormData) => void
  readonly onSubmit: () => void
  readonly onCancel: () => void
}

export function InstrumentForm({
  formData,
  isEdit = false,
  isPending = false,
  onFormDataChange,
  onSubmit,
  onCancel,
}: InstrumentFormProps) {
  const handleNameChange = (name: string) => {
    onFormDataChange({ ...formData, name })
  }

  const handleCategoryChange = (value: string) => {
    if (isInstrumentCategory(value)) {
      onFormDataChange({ ...formData, category: value })
    }
  }

  const handleActiveChange = (isActive: boolean) => {
    onFormDataChange({ ...formData, isActive })
  }

  const isSubmitDisabled = !formData.name || isPending

  return (
    <div className="flex gap-2 items-center">
      <Input
        className="flex-1"
        placeholder="Instrument name"
        value={formData.name}
        onChange={(e) => handleNameChange(e.target.value)}
      />
      {isEdit && (
        <Checkbox
          checked={formData.isActive}
          className="h-4 w-4 shrink-0"
          onCheckedChange={(checked) => handleActiveChange(checked === true)}
        />
      )}
      <Select value={formData.category} onValueChange={handleCategoryChange}>
        <SelectTrigger className="w-[150px]">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {categories.map((cat) => (
            <SelectItem key={cat} value={cat}>
              {cat}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Button disabled={isSubmitDisabled} size="icon" onClick={onSubmit}>
        <Check className="h-4 w-4" />
      </Button>
      <Button size="icon" variant="ghost" onClick={onCancel}>
        <X className="h-4 w-4" />
      </Button>
    </div>
  )
}
