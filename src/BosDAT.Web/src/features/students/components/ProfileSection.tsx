import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Mail, Phone, MapPin, Calendar, User, CreditCard, CheckCircle, XCircle } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { studentsApi } from '@/features/students/api'
import type { Student, RegistrationFeeStatus } from '@/features/students/types'
import { genderTranslations } from '@/features/students/types'
import { formatDate } from '@/lib/datetime-helpers'

interface ProfileSectionProps {
  readonly studentId: string
}

export function ProfileSection({ studentId }: ProfileSectionProps) {
  const { t } = useTranslation()
  const { data: student } = useQuery<Student>({
    queryKey: ['student', studentId],
    queryFn: () => studentsApi.getById(studentId),
    enabled: !!studentId,
  })

  const { data: feeStatus } = useQuery<RegistrationFeeStatus>({
    queryKey: ['student', studentId, 'registration-fee'],
    queryFn: () => studentsApi.getRegistrationFeeStatus(studentId),
    enabled: !!studentId,
  })

  if (!student) {
    return null
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">{t('students.sections.profile')}</h2>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('students.profile.contactInfo')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3">
              <Mail className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="text-sm text-muted-foreground">{t('students.form.email')}</p>
                <p>{student.email}</p>
              </div>
            </div>
            {student.phone && (
              <div className="flex items-center gap-3">
                <Phone className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('students.form.phone')}</p>
                  <p>{student.phone}</p>
                </div>
              </div>
            )}
            {student.phoneAlt && (
              <div className="flex items-center gap-3">
                <Phone className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('students.form.phoneAlt')}</p>
                  <p>{student.phoneAlt}</p>
                </div>
              </div>
            )}
            {student.address && (
              <div className="flex items-center gap-3">
                <MapPin className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('students.form.address')}</p>
                  <p>
                    {student.address}
                    <br />
                    {student.postalCode} {student.city}
                  </p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('students.profile.personalInfo')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {student.prefix && (
              <div>
                <p className="text-sm text-muted-foreground">{t('students.form.prefix')}</p>
                <p>{student.prefix}</p>
              </div>
            )}
            {student.dateOfBirth && (
              <div className="flex items-center gap-3">
                <Calendar className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">{t('students.form.dateOfBirth')}</p>
                  <p>{formatDate(student.dateOfBirth)}</p>
                </div>
              </div>
            )}
            {student.gender && (
              <div>
                <p className="text-sm text-muted-foreground">{t('students.form.gender')}</p>
                <p>{t(genderTranslations[student.gender])}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <CreditCard className="h-5 w-5" />
              {t('students.sections.billing')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {!student.billingContactName && !student.billingAddress ? (
              <p className="text-muted-foreground">{t('students.profile.sameAsStudent')}</p>
            ) : (
              <>
                {student.billingContactName && (
                  <div className="flex items-center gap-3">
                    <User className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">{t('students.form.billingContactName')}</p>
                      <p>{student.billingContactName}</p>
                    </div>
                  </div>
                )}
                {student.billingContactEmail && (
                  <div className="flex items-center gap-3">
                    <Mail className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">{t('students.form.billingContactEmail')}</p>
                      <p>{student.billingContactEmail}</p>
                    </div>
                  </div>
                )}
                {student.billingContactPhone && (
                  <div className="flex items-center gap-3">
                    <Phone className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">{t('students.form.billingContactPhone')}</p>
                      <p>{student.billingContactPhone}</p>
                    </div>
                  </div>
                )}
                {student.billingAddress && (
                  <div className="flex items-center gap-3">
                    <MapPin className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">{t('students.form.billingAddress')}</p>
                      <p>
                        {student.billingAddress}
                        <br />
                        {student.billingPostalCode} {student.billingCity}
                      </p>
                    </div>
                  </div>
                )}
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('students.profile.accountDetails')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm text-muted-foreground">{t('students.profile.enrolled')}</p>
              <p>{student.enrolledAt ? formatDate(student.enrolledAt) : t('students.profile.notSet')}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">{t('students.form.autoDebit')}</p>
              <p>{student.autoDebit ? t('common.form.yes') : t('common.form.no')}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">{t('students.profile.registrationFee')}</p>
              {feeStatus ? (
                <div className="flex items-center gap-2">
                  {feeStatus.hasPaid ? (
                    <>
                      <CheckCircle className="h-4 w-4 text-green-600" />
                      <span className="text-green-700">
                        {t('students.profile.paid')} {feeStatus.paidAt && formatDate(feeStatus.paidAt)}
                        {feeStatus.amount && ` - ${feeStatus.amount.toFixed(2)}`}
                      </span>
                    </>
                  ) : (
                    <>
                      <XCircle className="h-4 w-4 text-orange-500" />
                      <span className="text-orange-600">
                        {t('students.profile.notPaidYet')} {feeStatus.amount && `(${feeStatus.amount.toFixed(2)})`}
                      </span>
                    </>
                  )}
                </div>
              ) : (
                <span className="text-muted-foreground">{t('common.states.loading')}</span>
              )}
            </div>
            {student.notes && (
              <div>
                <p className="text-sm text-muted-foreground">{t('students.form.notes')}</p>
                <p className="whitespace-pre-wrap">{student.notes}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
