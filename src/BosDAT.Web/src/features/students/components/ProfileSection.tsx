import { useQuery } from '@tanstack/react-query'
import { Mail, Phone, MapPin, Calendar, User, CreditCard, CheckCircle, XCircle } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { studentsApi } from '@/services/api'
import type { Student, RegistrationFeeStatus } from '@/features/students/types'
import { formatDate } from '@/lib/utils'

interface ProfileSectionProps {
  studentId: string
}

export function ProfileSection({ studentId }: ProfileSectionProps) {
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
      <h2 className="text-2xl font-bold">Profile</h2>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Contact Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3">
              <Mail className="h-5 w-5 text-muted-foreground" />
              <div>
                <p className="text-sm text-muted-foreground">Email</p>
                <p>{student.email}</p>
              </div>
            </div>
            {student.phone && (
              <div className="flex items-center gap-3">
                <Phone className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">Phone</p>
                  <p>{student.phone}</p>
                </div>
              </div>
            )}
            {student.phoneAlt && (
              <div className="flex items-center gap-3">
                <Phone className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">Alternative Phone</p>
                  <p>{student.phoneAlt}</p>
                </div>
              </div>
            )}
            {student.address && (
              <div className="flex items-center gap-3">
                <MapPin className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">Address</p>
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
            <CardTitle>Personal Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {student.prefix && (
              <div>
                <p className="text-sm text-muted-foreground">Prefix</p>
                <p>{student.prefix}</p>
              </div>
            )}
            {student.dateOfBirth && (
              <div className="flex items-center gap-3">
                <Calendar className="h-5 w-5 text-muted-foreground" />
                <div>
                  <p className="text-sm text-muted-foreground">Date of Birth</p>
                  <p>{formatDate(student.dateOfBirth)}</p>
                </div>
              </div>
            )}
            {student.gender && (
              <div>
                <p className="text-sm text-muted-foreground">Gender</p>
                <p>{student.gender === 'PreferNotToSay' ? 'Prefer not to say' : student.gender}</p>
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
              Billing / Payer
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {!student.billingContactName && !student.billingAddress ? (
              <p className="text-muted-foreground">Same as student</p>
            ) : (
              <>
                {student.billingContactName && (
                  <div className="flex items-center gap-3">
                    <User className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">Contact Name</p>
                      <p>{student.billingContactName}</p>
                    </div>
                  </div>
                )}
                {student.billingContactEmail && (
                  <div className="flex items-center gap-3">
                    <Mail className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">Contact Email</p>
                      <p>{student.billingContactEmail}</p>
                    </div>
                  </div>
                )}
                {student.billingContactPhone && (
                  <div className="flex items-center gap-3">
                    <Phone className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">Contact Phone</p>
                      <p>{student.billingContactPhone}</p>
                    </div>
                  </div>
                )}
                {student.billingAddress && (
                  <div className="flex items-center gap-3">
                    <MapPin className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="text-sm text-muted-foreground">Billing Address</p>
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
            <CardTitle>Account Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm text-muted-foreground">Enrolled</p>
              <p>{student.enrolledAt ? formatDate(student.enrolledAt) : 'Not set'}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Auto Debit</p>
              <p>{student.autoDebit ? 'Yes' : 'No'}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Registration Fee</p>
              {feeStatus ? (
                <div className="flex items-center gap-2">
                  {feeStatus.hasPaid ? (
                    <>
                      <CheckCircle className="h-4 w-4 text-green-600" />
                      <span className="text-green-700">
                        Paid {feeStatus.paidAt && formatDate(feeStatus.paidAt)}
                        {feeStatus.amount && ` - ${feeStatus.amount.toFixed(2)}`}
                      </span>
                    </>
                  ) : (
                    <>
                      <XCircle className="h-4 w-4 text-orange-500" />
                      <span className="text-orange-600">
                        Not paid yet {feeStatus.amount && `(${feeStatus.amount.toFixed(2)})`}
                      </span>
                    </>
                  )}
                </div>
              ) : (
                <span className="text-muted-foreground">Loading...</span>
              )}
            </div>
            {student.notes && (
              <div>
                <p className="text-sm text-muted-foreground">Notes</p>
                <p className="whitespace-pre-wrap">{student.notes}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
