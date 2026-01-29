import { Pencil, Trash2, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn, formatCurrency } from '@/lib/utils'
import { PricingHistoryCollapsible } from './PricingHistoryCollapsible'
import type { CourseType } from '@/features/course-types/types'

export interface CourseTypeListItemProps {
  courseType: CourseType
  onEdit: (courseType: CourseType) => void
  onArchive: (id: string) => void
  onReactivate: (id: string) => void
  isArchiving: boolean
  isReactivating: boolean
}

const getStatusBadgeClasses = (isActive: boolean): string => {
  return cn(
    'inline-flex items-center rounded-full px-2 py-0.5 text-xs',
    isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
  )
}

export const CourseTypeListItem = ({
  courseType,
  onEdit,
  onArchive,
  onReactivate,
  isArchiving,
  isReactivating,
}: CourseTypeListItemProps) => {
  const { currentPricing, pricingHistory } = courseType

  return (
    <div className="py-3">
      <div className="flex items-center justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <p className="font-medium">{courseType.name}</p>
            <span className={getStatusBadgeClasses(courseType.isActive)}>
              {courseType.isActive ? 'Active' : 'Archived'}
            </span>
            {!courseType.hasTeachersForCourseType && (
              <span className="inline-flex items-center rounded-full px-2 py-0.5 text-xs bg-yellow-100 text-yellow-800">
                No teachers
              </span>
            )}
          </div>
          <p className="text-sm text-muted-foreground">
            {courseType.instrumentName} - {courseType.durationMinutes} min - {courseType.type}
            {courseType.type !== 'Individual' && ` (max ${courseType.maxStudents})`}
          </p>
        </div>

        <div className="text-right mr-4">
          {currentPricing ? (
            <>
              <p className="text-sm">Adult: {formatCurrency(currentPricing.priceAdult)}</p>
              <p className="text-sm text-muted-foreground">
                Child: {formatCurrency(currentPricing.priceChild)}
              </p>
            </>
          ) : (
            <p className="text-sm text-muted-foreground">No pricing</p>
          )}
        </div>

        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => onEdit(courseType)}
            title="Edit"
          >
            <Pencil className="h-4 w-4" />
          </Button>
          {courseType.isActive ? (
            <Button
              variant="ghost"
              size="icon"
              className="text-red-600 hover:text-red-700 hover:bg-red-50"
              onClick={() => onArchive(courseType.id)}
              disabled={isArchiving}
              title={
                courseType.activeCourseCount > 0
                  ? `Cannot archive: ${courseType.activeCourseCount} active courses`
                  : 'Archive'
              }
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          ) : (
            <Button
              variant="ghost"
              size="icon"
              className="text-green-600 hover:text-green-700 hover:bg-green-50"
              onClick={() => onReactivate(courseType.id)}
              disabled={isReactivating}
              title="Reactivate"
            >
              <Check className="h-4 w-4" />
            </Button>
          )}
        </div>
      </div>

      {pricingHistory && pricingHistory.length > 1 && (
        <PricingHistoryCollapsible pricingHistory={pricingHistory} />
      )}
    </div>
  )
}
