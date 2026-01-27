import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import type { EnrollmentPricing } from '@/features/students/types'

interface PricingBreakdownProps {
  pricing: EnrollmentPricing
  numberOfOccurrences: string
  calculatedAmount: number
}

export function PricingBreakdown({
  pricing,
  numberOfOccurrences,
  calculatedAmount,
}: PricingBreakdownProps) {
  const occurrences = parseInt(numberOfOccurrences, 10)
  const hasValidOccurrences = numberOfOccurrences && occurrences > 0

  return (
    <Card className="bg-background">
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">Pricing Breakdown</CardTitle>
      </CardHeader>
      <CardContent className="space-y-2 text-sm">
        <PricingRow label="Course:" value={pricing.courseName} />
        <PricingRow
          label={`Base Price (${pricing.isChildPricing ? 'Child' : 'Adult'}):`}
          value={`€${pricing.applicableBasePrice.toFixed(2)}`}
        />
        {pricing.discountPercent > 0 && (
          <PricingRow
            label="Discount:"
            value={`-${pricing.discountPercent}% (€${pricing.discountAmount.toFixed(2)})`}
            valueClassName="text-green-600"
          />
        )}
        <PricingRow
          label="Price per Lesson:"
          value={`€${pricing.pricePerLesson.toFixed(2)}`}
          className="font-medium border-t pt-2"
        />
        {hasValidOccurrences && (
          <PricingRow
            label={`Total (${numberOfOccurrences} ${occurrences === 1 ? 'lesson' : 'lessons'}):`}
            value={`€${calculatedAmount.toFixed(2)}`}
            className="font-bold text-base border-t pt-2"
            valueClassName="text-primary"
          />
        )}
      </CardContent>
    </Card>
  )
}

interface PricingRowProps {
  label: string
  value: string
  className?: string
  valueClassName?: string
}

function PricingRow({ label, value, className = '', valueClassName = '' }: PricingRowProps) {
  return (
    <div className={`flex justify-between ${className}`}>
      <span className="text-muted-foreground">{label}</span>
      <span className={valueClassName}>{value}</span>
    </div>
  )
}
