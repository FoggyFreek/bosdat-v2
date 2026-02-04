import { useState, memo } from 'react'
import { ChevronDown, ChevronRight } from 'lucide-react'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { CourseTypePricingVersion } from '@/features/course-types/types'
import { formatCurrency } from '@/lib/utils'
import { formatDate } from '@/lib/datetime-helpers'

interface PricingHistoryCollapsibleProps {
  readonly pricingHistory: readonly CourseTypePricingVersion[]
}

export const PricingHistoryCollapsible = memo(function PricingHistoryCollapsible({
  pricingHistory,
}: PricingHistoryCollapsibleProps) {
  const [isOpen, setIsOpen] = useState(false)

  if (pricingHistory.length <= 1) {
    return null
  }

  const historicVersions = pricingHistory.filter(pv => !pv.isCurrent)

  if (historicVersions.length === 0) {
    return null
  }

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="mt-2">
      <CollapsibleTrigger asChild>
        <Button variant="ghost" size="sm" className="h-6 px-2 text-xs text-muted-foreground">
          {isOpen ? <ChevronDown className="h-3 w-3 mr-1" /> : <ChevronRight className="h-3 w-3 mr-1" />}
          {historicVersions.length} historic pricing version{historicVersions.length > 1 ? 's' : ''}
        </Button>
      </CollapsibleTrigger>
      <CollapsibleContent className="mt-2">
        <div className="rounded-md border bg-muted/50 p-2 space-y-2">
          {historicVersions.map((version) => (
            <div
              key={version.id}
              className="flex items-center justify-between text-xs text-muted-foreground"
            >
              <div className="flex items-center gap-2">
                <span className="font-medium">
                  {formatCurrency(version.priceAdult)} / {formatCurrency(version.priceChild)}
                </span>
                {version.validUntil && (
                  <Badge variant="secondary" className="text-xs">
                    until {formatDate(version.validUntil)}
                  </Badge>
                )}
              </div>
              <span className="text-muted-foreground">
                from {formatDate(version.validFrom)}
              </span>
            </div>
          ))}
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
})
