import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { AlertCircle, Loader2 } from 'lucide-react'
import { CreateCourseTypePricingVersion, CourseTypePricingVersion } from '@/features/course-types/types'
import { getTodayForApi } from '@/lib/datetime-helpers'

const pricingVersionSchema = z.object({
  priceAdult: z.coerce.number().min(0, 'Price must be 0 or greater'),
  priceChild: z.coerce.number().min(0, 'Price must be 0 or greater'),
  validFrom: z.string().min(1, 'Activation date is required'),
}).refine(data => data.priceChild <= data.priceAdult, {
  message: 'Child price cannot be higher than adult price',
  path: ['priceChild'],
})

type PricingVersionFormData = z.infer<typeof pricingVersionSchema>

interface NewPricingVersionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  courseTypeName: string
  currentPricing: CourseTypePricingVersion | null
  onSubmit: (data: CreateCourseTypePricingVersion) => Promise<void>
  isLoading?: boolean
  error?: string | null
  childDiscountPercent?: number
}

export const NewPricingVersionDialog = ({
  open,
  onOpenChange,
  courseTypeName,
  currentPricing,
  onSubmit,
  isLoading = false,
  error = null,
  childDiscountPercent,
}: NewPricingVersionDialogProps) => {
  const today = getTodayForApi()

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<PricingVersionFormData>({
    resolver: zodResolver(pricingVersionSchema),
    defaultValues: {
      priceAdult: currentPricing?.priceAdult ?? 0,
      priceChild: currentPricing?.priceChild ?? 0,
      validFrom: today,
    },
  })

  const handlePriceAdultChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const adultPrice = Number.parseFloat(e.target.value)
    if (!Number.isNaN(adultPrice) && adultPrice >= 0) {
      const discountRate = 1 - (childDiscountPercent ?? 10) / 100
      const calculatedChildPrice = Number.parseFloat((adultPrice * discountRate).toFixed(2))
      setValue('priceChild', calculatedChildPrice)
    }
  }

  const handleFormSubmit = async (data: PricingVersionFormData) => {
    await onSubmit({
      priceAdult: data.priceAdult,
      priceChild: data.priceChild,
      validFrom: data.validFrom,
    })
  }

  const handleClose = () => {
    reset()
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Create New Pricing Version</DialogTitle>
          <DialogDescription>
            Create a new pricing version for &quot;{courseTypeName}&quot;. The current pricing has been
            used in invoices and cannot be edited directly.
          </DialogDescription>
        </DialogHeader>

        {error && (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="priceAdult">Adult Price</Label>
              <Input
                id="priceAdult"
                type="number"
                step="0.01"
                min="0"
                {...register('priceAdult', {
                  onChange: handlePriceAdultChange,
                })}
                disabled={isLoading}
              />
              {errors.priceAdult && (
                <p className="text-sm text-destructive">{errors.priceAdult.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="priceChild">Child Price</Label>
              <Input
                id="priceChild"
                type="number"
                step="0.01"
                min="0"
                {...register('priceChild')}
                disabled={isLoading}
              />
              {errors.priceChild && (
                <p className="text-sm text-destructive">{errors.priceChild.message}</p>
              )}
              <p className="text-xs text-muted-foreground">
                Default: {childDiscountPercent ?? 10}% discount from adult price
              </p>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="validFrom">Activation Date</Label>
            <Input
              id="validFrom"
              type="date"
              min={today}
              {...register('validFrom')}
              disabled={isLoading}
            />
            {errors.validFrom && (
              <p className="text-sm text-destructive">{errors.validFrom.message}</p>
            )}
            <p className="text-xs text-muted-foreground">
              The new pricing will become active on this date. Current pricing will remain in effect
              until the day before.
            </p>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose} disabled={isLoading}>
              Cancel
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Create New Version
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
