import { useState } from 'react'
import { Plus, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useFormDirty } from '@/context/FormDirtyContext'
import { CourseTypeForm } from './CourseTypeForm'
import { CourseTypesList } from './CourseTypesList'
import { NewPricingVersionDialog } from './NewPricingVersionDialog'
import { useCourseTypesData, useCourseTypeForm, useCourseTypeMutations } from '../hooks'
import type { CourseType, CreateCourseTypePricingVersion } from '@/features/course-types/types'

export function CourseTypesSection() {
  const { setIsDirty } = useFormDirty()

  // State for pricing version dialog
  const [pricingDialogOpen, setPricingDialogOpen] = useState(false)
  const [pricingDialogCourseType, setPricingDialogCourseType] = useState<CourseType | null>(null)
  const [pricingDialogError, setPricingDialogError] = useState<string | null>(null)

  // Data fetching
  const {
    courseTypes,
    instruments,
    isLoading,
    childDiscountPercent,
    groupMaxStudents,
    workshopMaxStudents,
  } = useCourseTypesData()

  // Form state management
  const form = useCourseTypeForm({
    childDiscountPercent,
    groupMaxStudents,
    workshopMaxStudents,
    onDirtyChange: setIsDirty,
  })

  // Mutations
  const mutations = useCourseTypeMutations({
    getDuration: form.getDuration,
    onCreateSuccess: form.resetForm,
    onUpdateSuccess: () => {
      // Don't reset form yet if pricing changed and needs dialog
    },
    onPricingVersionSuccess: () => {
      setPricingDialogOpen(false)
      setPricingDialogCourseType(null)
      setPricingDialogError(null)
      form.resetForm()
    },
    onError: form.setError,
    onPricingVersionError: setPricingDialogError,
  })

  const handleSubmit = async () => {
    if (form.editId && form.editingCourseType) {
      // Update the course type (non-pricing fields)
      await mutations.updateMutation.mutateAsync({ id: form.editId, data: form.formData })

      // Check if pricing changed and handle accordingly
      const currentPricing = form.editingCourseType.currentPricing
      const newPriceAdult = Number.parseFloat(form.formData.priceAdult)
      const newPriceChild = Number.parseFloat(form.formData.priceChild)

      const pricingChanged =
        currentPricing &&
        (currentPricing.priceAdult !== newPriceAdult || currentPricing.priceChild !== newPriceChild)

      if (pricingChanged) {
        if (form.editingCourseType.canEditPricingDirectly) {
          // Direct update allowed
          await mutations.updatePricingMutation.mutateAsync({
            id: form.editId,
            priceAdult: newPriceAdult,
            priceChild: newPriceChild,
          })
          form.resetForm()
        } else {
          // Need to create new version - open dialog
          setPricingDialogCourseType(form.editingCourseType)
          setPricingDialogOpen(true)
          return // Don't reset form until pricing dialog is handled
        }
      } else {
        form.resetForm()
      }
    } else {
      mutations.createMutation.mutate(form.formData)
    }
  }

  const handlePricingVersionSubmit = async (data: CreateCourseTypePricingVersion) => {
    if (pricingDialogCourseType) {
      await mutations.createPricingVersionMutation.mutateAsync({
        id: pricingDialogCourseType.id,
        data,
      })
    }
  }

  const handlePricingDialogClose = (open: boolean) => {
    setPricingDialogOpen(open)
    if (!open) {
      setPricingDialogCourseType(null)
      setPricingDialogError(null)
    }
  }

  const isFormSubmitting =
    mutations.createMutation.isPending ||
    mutations.updateMutation.isPending ||
    mutations.updatePricingMutation.isPending

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <div>
          <CardTitle>Course Types</CardTitle>
          <CardDescription>Configure types of courses and pricing</CardDescription>
        </div>
        <Button size="sm" onClick={form.handleShowAdd}>
          <Plus className="h-4 w-4 mr-2" />
          Add
        </Button>
      </CardHeader>

      <CardContent>
        {form.error && (
          <Alert variant="destructive" className="mb-4">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              {form.error}
              <button className="ml-2 font-bold" onClick={() => form.setError(null)}>
                ×
              </button>
            </AlertDescription>
          </Alert>
        )}

        {(form.showAdd || form.editId !== null) && (
          <CourseTypeForm
            formData={form.formData}
            instruments={instruments}
            editId={form.editId}
            editingCourseType={form.editingCourseType}
            useCustomDuration={form.useCustomDuration}
            teacherWarning={form.teacherWarning}
            childDiscountPercent={childDiscountPercent}
            isFormValid={form.isFormValid}
            isSubmitting={isFormSubmitting}
            onFormDataChange={(updates) => form.setFormData((prev) => ({ ...prev, ...updates }))}
            onInstrumentChange={form.handleInstrumentChange}
            onTypeChange={form.handleTypeChange}
            onAdultPriceChange={form.handleAdultPriceChange}
            onChildPriceChange={form.handleChildPriceChange}
            onCustomDurationToggle={() => form.setUseCustomDuration(!form.useCustomDuration)}
            onCancel={form.resetForm}
            onSubmit={handleSubmit}
          />
        )}

        <CourseTypesList
          courseTypes={courseTypes}
          isLoading={isLoading}
          onEdit={form.startEdit}
          onArchive={(id) => mutations.archiveMutation.mutate(id)}
          onReactivate={(id) => mutations.reactivateMutation.mutate(id)}
          isArchiving={mutations.archiveMutation.isPending}
          isReactivating={mutations.reactivateMutation.isPending}
        />
      </CardContent>

      <NewPricingVersionDialog
        open={pricingDialogOpen}
        onOpenChange={handlePricingDialogClose}
        courseTypeName={pricingDialogCourseType?.name ?? ''}
        currentPricing={pricingDialogCourseType?.currentPricing ?? null}
        childDiscountPercent={childDiscountPercent}
        onSubmit={handlePricingVersionSubmit}
        isLoading={mutations.createPricingVersionMutation.isPending}
        error={pricingDialogError}
      />
    </Card>
  )
}
