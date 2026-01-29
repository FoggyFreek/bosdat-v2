import { memo } from 'react'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import type { EnrollmentGroupMember, DiscountType } from '../types'

interface EnrollmentGroupMemberCardProps {
  readonly member: Readonly<EnrollmentGroupMember>
  readonly courseStartDate: string
  readonly familyDiscountPercent: number
  readonly courseDiscountPercent: number
  readonly onUpdate: (updates: Partial<EnrollmentGroupMember>) => void
  readonly onRemove: () => void
}

export const EnrollmentGroupMemberCard = memo(function EnrollmentGroupMemberCard({
  member,
  courseStartDate,
  familyDiscountPercent,
  courseDiscountPercent,
  onUpdate,
  onRemove,
}: EnrollmentGroupMemberCardProps) {
  const handleDiscountTypeChange = (value: DiscountType) => {
    let discountPercentage = 0
    if (value === 'Family') {
      discountPercentage = familyDiscountPercent
    } else if (value === 'Course') {
      discountPercentage = courseDiscountPercent
    }

    // Only allow Course discount if eligible
    if (value === 'Course' && !member.isEligibleForCourseDiscount) {
      return
    }

    onUpdate({ discountType: value, discountPercentage })
  }

  const handleEnrolledAtChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onUpdate({ enrolledAt: e.target.value })
  }

  const handleNoteChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onUpdate({ note: e.target.value })
  }

  const isStartingOnCourseStart = member.enrolledAt === courseStartDate

  return (
    <div className="rounded-lg border p-4 space-y-4">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h4 className="font-medium truncate">{member.studentName}</h4>
            {member.isEligibleForCourseDiscount && (
              <Badge variant="secondary" className="text-xs">
                Has active enrollments
              </Badge>
            )}
            {isStartingOnCourseStart && (
              <Badge variant="outline" className="text-xs text-green-600">
                Starts on course date
              </Badge>
            )}
          </div>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={onRemove}
          className="text-destructive hover:text-destructive"
          title="Remove student"
        >
          <svg
            className="h-4 w-4"
            fill="none"
            viewBox="0 0 24 24"
            strokeWidth={1.5}
            stroke="currentColor"
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </Button>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor={`enrolledAt-${member.studentId}`}>Enrollment Date</Label>
          <Input
            id={`enrolledAt-${member.studentId}`}
            type="date"
            value={member.enrolledAt}
            onChange={handleEnrolledAtChange}
            min={courseStartDate}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor={`discount-${member.studentId}`}>Discount Type</Label>
          <Select
            value={member.discountType}
            onValueChange={handleDiscountTypeChange}
          >
            <SelectTrigger id={`discount-${member.studentId}`}>
              <SelectValue placeholder="Select discount" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="None">No Discount</SelectItem>
              <SelectItem value="Family">
                Family Discount ({familyDiscountPercent}%)
              </SelectItem>
              <SelectItem
                value="Course"
                disabled={!member.isEligibleForCourseDiscount}
              >
                Course Discount ({courseDiscountPercent}%)
                {!member.isEligibleForCourseDiscount && ' - Not eligible'}
              </SelectItem>
            </SelectContent>
          </Select>
          {member.discountType !== 'None' && (
            <p className="text-xs text-muted-foreground">
              {member.discountPercentage}% discount will be applied
            </p>
          )}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor={`note-${member.studentId}`}>Note (optional)</Label>
        <Textarea
          id={`note-${member.studentId}`}
          value={member.note}
          onChange={handleNoteChange}
          placeholder="Add a note for this enrollment..."
          rows={2}
        />
      </div>
    </div>
  )
})
