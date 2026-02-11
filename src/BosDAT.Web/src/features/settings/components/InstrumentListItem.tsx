import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Pencil } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { Instrument, InstrumentCategory } from '@/features/instruments/types'
import { instrumentCategoryTranslations } from '@/features/instruments/types'
import { InstrumentForm } from './InstrumentForm'

interface InstrumentFormData {
  name: string
  category: InstrumentCategory
  isActive: boolean
}

interface InstrumentListItemProps {
  readonly instrument: Instrument
  readonly isEditing: boolean
  readonly formData: InstrumentFormData
  readonly isPending?: boolean
  readonly onEdit: (instrument: Instrument) => void
  readonly onFormDataChange: (data: InstrumentFormData) => void
  readonly onUpdate: () => void
  readonly onCancelEdit: () => void
}

const getStatusBadgeClassName = (isActive: boolean): string => {
  return cn(
    'inline-flex items-center rounded-full px-2 py-0.5 text-xs',
    isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
  )
}

export function InstrumentListItem({
  instrument,
  isEditing,
  formData,
  isPending = false,
  onEdit,
  onFormDataChange,
  onUpdate,
  onCancelEdit,
}: InstrumentListItemProps) {
  const { t } = useTranslation()

  const getStatusLabel = (isActive: boolean): string => {
    return isActive ? t('common.status.active') : t('common.status.inactive')
  }
  if (isEditing) {
    return (
      <div className="flex items-center py-2">
        <InstrumentForm
          formData={formData}
          isEdit
          isPending={isPending}
          onCancel={onCancelEdit}
          onFormDataChange={onFormDataChange}
          onSubmit={onUpdate}
        />
      </div>
    )
  }

  return (
    <div className="flex items-center justify-between py-2">
      <div>
        <p className="font-medium">{instrument.name}</p>
        <p className="text-sm text-muted-foreground">{t(instrumentCategoryTranslations[instrument.category])}</p>
      </div>
      <div className="flex items-center gap-2">
        <span className={getStatusBadgeClassName(instrument.isActive)}>
          {getStatusLabel(instrument.isActive)}
        </span>
        <Button
          size="icon"
          variant="ghost"
          onClick={() => onEdit(instrument)}
        >
          <Pencil className="h-4 w-4" />
        </Button>
      </div>
    </div>
  )
}
