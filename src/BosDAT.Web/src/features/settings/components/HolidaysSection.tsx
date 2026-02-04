import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Trash2, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { holidaysApi } from '@/services/api'
import { useFormDirty } from '@/context/FormDirtyContext'
import { formatDate } from '@/lib/datetime-helpers'
import type { Holiday } from '@/features/schedule/types'

const DISPLAY_NAME = 'HolidaysSection'

const INITIAL_FORM_DATA: FormData = {
  name: '',
  startDate: '',
  endDate: '',
}

interface FormData {
  name: string
  startDate: string
  endDate: string
}

export function HolidaysSection() {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [formData, setFormData] = useState<FormData>(INITIAL_FORM_DATA)
  const { setIsDirty } = useFormDirty()

  const { data: holidays = [], isLoading } = useQuery<Holiday[]>({
    queryKey: ['holidays'],
    queryFn: () => holidaysApi.getAll(),
  })

  const createMutation = useMutation({
    mutationFn: (data: FormData) => holidaysApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['holidays'] })
      setShowAdd(false)
      setFormData(INITIAL_FORM_DATA)
      setIsDirty(false)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => holidaysApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['holidays'] })
    },
  })

  const isFormValid = formData.name && formData.startDate && formData.endDate
  const isCreateDisabled = !isFormValid || createMutation.isPending

  const handleShowAdd = () => {
    setShowAdd(true)
    setIsDirty(true)
  }

  const handleCancelAdd = () => {
    setShowAdd(false)
    setFormData(INITIAL_FORM_DATA)
    setIsDirty(false)
  }

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, name: e.target.value })
  }

  const handleStartDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, startDate: e.target.value })
  }

  const handleEndDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, endDate: e.target.value })
  }

  const handleCreate = () => {
    createMutation.mutate(formData)
  }

  const handleDelete = (id: number) => {
    deleteMutation.mutate(id)
  }

  const renderHeaderTitle = () => (
    <div>
      <CardTitle>Holidays</CardTitle>
      <CardDescription>Set school holidays and closures</CardDescription>
    </div>
  )

  const renderAddButton = () => (
    <Button onClick={handleShowAdd} size="sm">
      <Plus className="h-4 w-4 mr-2" />
      Add
    </Button>
  )

  const renderHeader = () => (
    <CardHeader className="flex flex-row items-center justify-between">
      {renderHeaderTitle()}
      {renderAddButton()}
    </CardHeader>
  )

  const renderNameField = () => (
    <div>
      <Label>Name</Label>
      <Input
        onChange={handleNameChange}
        placeholder="e.g., Summer Break"
        value={formData.name}
      />
    </div>
  )

  const renderStartDateField = () => (
    <div>
      <Label>Start Date</Label>
      <Input
        onChange={handleStartDateChange}
        type="date"
        value={formData.startDate}
      />
    </div>
  )

  const renderEndDateField = () => (
    <div>
      <Label>End Date</Label>
      <Input
        onChange={handleEndDateChange}
        type="date"
        value={formData.endDate}
      />
    </div>
  )

  const renderFormFields = () => (
    <div className="grid grid-cols-3 gap-3">
      {renderNameField()}
      {renderStartDateField()}
      {renderEndDateField()}
    </div>
  )

  const renderCancelButton = () => (
    <Button onClick={handleCancelAdd} variant="outline">
      Cancel
    </Button>
  )

  const renderCreateButton = () => (
    <Button disabled={isCreateDisabled} onClick={handleCreate}>
      Create
    </Button>
  )

  const renderFormActions = () => (
    <div className="flex justify-end gap-2">
      {renderCancelButton()}
      {renderCreateButton()}
    </div>
  )

  const renderAddForm = () => (
    <div className="mb-4 p-4 bg-muted/50 rounded-lg space-y-3">
      {renderFormFields()}
      {renderFormActions()}
    </div>
  )

  const renderLoadingState = () => (
    <div className="flex items-center justify-center py-8">
      <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
    </div>
  )

  const renderEmptyState = () => (
    <p className="text-muted-foreground">No holidays configured</p>
  )

  const renderHolidayInfo = (holiday: Holiday) => (
    <div>
      <p className="font-medium">{holiday.name}</p>
      <p className="text-sm text-muted-foreground">
        {formatDate(holiday.startDate)} - {formatDate(holiday.endDate)}
      </p>
    </div>
  )

  const renderDeleteButton = (holiday: Holiday) => (
    <Button
      className="text-red-600 hover:text-red-700 hover:bg-red-50"
      disabled={deleteMutation.isPending}
      onClick={() => handleDelete(holiday.id)}
      size="icon"
      variant="ghost"
    >
      <Trash2 className="h-4 w-4" />
    </Button>
  )

  const renderHolidayItem = (holiday: Holiday) => (
    <div key={holiday.id} className="flex items-center justify-between py-3">
      {renderHolidayInfo(holiday)}
      {renderDeleteButton(holiday)}
    </div>
  )

  const renderHolidaysList = () => (
    <div className="divide-y">
      {holidays.map((holiday) => renderHolidayItem(holiday))}
    </div>
  )

  const renderContent = () => (
    <>
      {showAdd && renderAddForm()}
      {isLoading && renderLoadingState()}
      {!isLoading && holidays.length === 0 && renderEmptyState()}
      {!isLoading && holidays.length > 0 && renderHolidaysList()}
    </>
  )

  return (
    <Card>
      {renderHeader()}
      <CardContent>{renderContent()}</CardContent>
    </Card>
  )
}

HolidaysSection.displayName = DISPLAY_NAME
