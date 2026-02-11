import { memo } from 'react'
import { useTranslation } from 'react-i18next'
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
import type { EnrollmentGroupMember, DiscountType, InvoicingPreference } from '../types'

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
  const { t } = useTranslation()
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

  const handleInvoicingPreferenceChange = (value: InvoicingPreference) => {
    onUpdate({ invoicingPreference: value })
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
                {t('enrollments.step2.hasActiveEnrollments')}
              </Badge>
            )}
            {isStartingOnCourseStart && (
              <Badge variant="outline" className="text-xs text-green-600">
                {t('enrollments.step2.startsOnCourseDate')}
              </Badge>
            )}
          </div>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={onRemove}
          className="text-destructive hover:text-destructive"
          title={t('enrollments.step2.removeStudent')}
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

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div className="space-y-2">
          <Label htmlFor={`enrolledAt-${member.studentId}`}>{t('enrollments.step2.enrollmentDate')}</Label>
          <Input
            id={`enrolledAt-${member.studentId}`}
            type="date"
            value={member.enrolledAt}
            onChange={handleEnrolledAtChange}
            min={courseStartDate}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor={`discount-${member.studentId}`}>{t('enrollments.step2.discountType')}</Label>
          <Select
            value={member.discountType}
            onValueChange={handleDiscountTypeChange}
          >
            <SelectTrigger id={`discount-${member.studentId}`}>
              <SelectValue placeholder={t('enrollments.step2.selectDiscount')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="None">{t('enrollments.step2.noDiscount')}</SelectItem>
              <SelectItem value="Family">
                {t('enrollments.step2.familyDiscount', { percent: familyDiscountPercent })}
              </SelectItem>
              <SelectItem
                value="Course"
                disabled={!member.isEligibleForCourseDiscount}
              >
                {t('enrollments.step2.courseDiscount', { percent: courseDiscountPercent })}
                {!member.isEligibleForCourseDiscount && ` - ${t('enrollments.step2.courseDiscountNotEligible', { percent: courseDiscountPercent })}`}
              </SelectItem>
            </SelectContent>
          </Select>
          {member.discountType !== 'None' && (
            <p className="text-xs text-muted-foreground">
              {t('enrollments.step2.discountApplied', { percent: member.discountPercentage })}
            </p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor={`invoicing-${member.studentId}`}>{t('enrollments.step2.invoiceFrequency')}</Label>
          <Select
            value={member.invoicingPreference}
            onValueChange={handleInvoicingPreferenceChange}
          >
            <SelectTrigger id={`invoicing-${member.studentId}`}>
              <SelectValue placeholder={t('enrollments.step2.selectFrequency')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Monthly">{t('enrollments.step2.monthly')}</SelectItem>
              <SelectItem value="Quarterly">{t('enrollments.step2.quarterly')}</SelectItem>
            </SelectContent>
          </Select>
          <p className="text-xs text-muted-foreground">
            {member.invoicingPreference === 'Monthly'
              ? t('enrollments.step2.monthlyHint')
              : t('enrollments.step2.quarterlyHint')}
          </p>
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor={`note-${member.studentId}`}>{t('enrollments.step2.noteLabel')}</Label>
        <Textarea
          id={`note-${member.studentId}`}
          value={member.note}
          onChange={handleNoteChange}
          placeholder={t('enrollments.step2.notePlaceholder')}
          rows={2}
        />
      </div>
    </div>
  )
})
