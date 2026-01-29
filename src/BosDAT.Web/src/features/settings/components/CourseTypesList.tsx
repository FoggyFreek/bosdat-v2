import { CourseTypeListItem } from './CourseTypeListItem'
import type { CourseType } from '@/features/course-types/types'

export interface CourseTypesListProps {
  courseTypes: CourseType[]
  isLoading: boolean
  onEdit: (courseType: CourseType) => void
  onArchive: (id: string) => void
  onReactivate: (id: string) => void
  isArchiving: boolean
  isReactivating: boolean
}

export const CourseTypesList = ({
  courseTypes,
  isLoading,
  onEdit,
  onArchive,
  onReactivate,
  isArchiving,
  isReactivating,
}: CourseTypesListProps) => {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
      </div>
    )
  }

  if (courseTypes.length === 0) {
    return <p className="text-muted-foreground">No course types configured</p>
  }

  return (
    <div className="divide-y">
      {courseTypes.map((courseType) => (
        <CourseTypeListItem
          key={courseType.id}
          courseType={courseType}
          onEdit={onEdit}
          onArchive={onArchive}
          onReactivate={onReactivate}
          isArchiving={isArchiving}
          isReactivating={isReactivating}
        />
      ))}
    </div>
  )
}
