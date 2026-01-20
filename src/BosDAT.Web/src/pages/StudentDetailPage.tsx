import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Mail, Phone, MapPin, Calendar, Plus, Music, Trash2, User, CreditCard } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { studentsApi, enrollmentsApi, lessonsApi, coursesApi } from '@/services/api'
import type { Student, StudentEnrollment, Lesson, CourseList } from '@/types'
import { formatDate, formatTime, getDayName, cn } from '@/lib/utils'

export function StudentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const [showEnrollForm, setShowEnrollForm] = useState(false)
  const [selectedCourse, setSelectedCourse] = useState('')

  const { data: student, isLoading } = useQuery<Student>({
    queryKey: ['student', id],
    queryFn: () => studentsApi.getById(id!),
    enabled: !!id,
  })

  const { data: enrollments = [] } = useQuery<StudentEnrollment[]>({
    queryKey: ['enrollments', 'student', id],
    queryFn: () => enrollmentsApi.getByStudent(id!),
    enabled: !!id,
  })

  const { data: lessons = [] } = useQuery<Lesson[]>({
    queryKey: ['lessons', 'student', id],
    queryFn: () => lessonsApi.getByStudent(id!),
    enabled: !!id,
  })

  const { data: courses = [] } = useQuery<CourseList[]>({
    queryKey: ['courses', 'active'],
    queryFn: () => coursesApi.getAll({ status: 'Active' }),
    enabled: showEnrollForm,
  })

  const enrollMutation = useMutation({
    mutationFn: (courseId: string) =>
      enrollmentsApi.create({ studentId: id!, courseId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments', 'student', id] })
      setShowEnrollForm(false)
      setSelectedCourse('')
    },
  })

  const withdrawMutation = useMutation({
    mutationFn: (enrollmentId: string) => enrollmentsApi.delete(enrollmentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrollments', 'student', id] })
    },
  })

  const handleEnroll = () => {
    if (selectedCourse) {
      enrollMutation.mutate(selectedCourse)
    }
  }

  // Filter out courses the student is already enrolled in
  const availableCourses = courses.filter(
    (course) => !enrollments.some((e) => e.courseId === course.id)
  )

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!student) {
    return (
      <div className="text-center py-8">
        <p className="text-muted-foreground">Student not found</p>
        <Button asChild className="mt-4">
          <Link to="/students">Back to Students</Link>
        </Button>
      </div>
    )
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Active':
        return 'bg-green-100 text-green-800'
      case 'Trial':
        return 'bg-yellow-100 text-yellow-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to="/students">
            <ArrowLeft className="h-5 w-5" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">{student.fullName}</h1>
          <span
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${getStatusColor(
              student.status
            )}`}
          >
            {student.status}
          </span>
        </div>
        <Button asChild>
          <Link to={`/students/${id}/edit`}>Edit Student</Link>
        </Button>
      </div>

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
            {student.notes && (
              <div>
                <p className="text-sm text-muted-foreground">Notes</p>
                <p className="whitespace-pre-wrap">{student.notes}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Enrollments</CardTitle>
          <Button size="sm" onClick={() => setShowEnrollForm(!showEnrollForm)}>
            <Plus className="h-4 w-4 mr-2" />
            Enroll in Course
          </Button>
        </CardHeader>
        <CardContent>
          {showEnrollForm && (
            <div className="flex gap-2 mb-4 p-4 bg-muted/50 rounded-lg">
              <Select value={selectedCourse} onValueChange={setSelectedCourse}>
                <SelectTrigger className="flex-1">
                  <SelectValue placeholder="Select a course" />
                </SelectTrigger>
                <SelectContent>
                  {availableCourses.length === 0 ? (
                    <SelectItem value="" disabled>
                      No available courses
                    </SelectItem>
                  ) : (
                    availableCourses.map((course) => (
                      <SelectItem key={course.id} value={course.id}>
                        {course.instrumentName} - {course.teacherName} ({getDayName(course.dayOfWeek).substring(0, 3)} {formatTime(course.startTime)})
                      </SelectItem>
                    ))
                  )}
                </SelectContent>
              </Select>
              <Button onClick={handleEnroll} disabled={!selectedCourse || enrollMutation.isPending}>
                {enrollMutation.isPending ? 'Enrolling...' : 'Enroll'}
              </Button>
              <Button variant="outline" onClick={() => setShowEnrollForm(false)}>
                Cancel
              </Button>
            </div>
          )}

          {enrollments.length === 0 ? (
            <p className="text-muted-foreground">No enrollments yet</p>
          ) : (
            <div className="divide-y">
              {enrollments.map((enrollment) => (
                <div key={enrollment.id} className="flex items-center justify-between py-3">
                  <div className="flex items-center gap-3">
                    <Music className="h-5 w-5 text-muted-foreground" />
                    <div>
                      <p className="font-medium">{enrollment.instrumentName}</p>
                      <p className="text-sm text-muted-foreground">
                        {enrollment.teacherName} - {getDayName(enrollment.dayOfWeek)} {formatTime(enrollment.startTime)}
                      </p>
                      {enrollment.roomName && (
                        <p className="text-xs text-muted-foreground">{enrollment.roomName}</p>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span
                      className={cn(
                        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                        enrollment.status === 'Active'
                          ? 'bg-green-100 text-green-800'
                          : 'bg-gray-100 text-gray-800'
                      )}
                    >
                      {enrollment.status}
                    </span>
                    {enrollment.status === 'Active' && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-red-600 hover:text-red-700 hover:bg-red-50"
                        onClick={() => withdrawMutation.mutate(enrollment.id)}
                        disabled={withdrawMutation.isPending}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Recent Lessons</CardTitle>
        </CardHeader>
        <CardContent>
          {lessons.length === 0 ? (
            <p className="text-muted-foreground">No lessons yet</p>
          ) : (
            <div className="divide-y">
              {lessons.slice(0, 10).map((lesson) => (
                <div key={lesson.id} className="flex items-center justify-between py-3">
                  <div>
                    <p className="font-medium">{lesson.instrumentName}</p>
                    <p className="text-sm text-muted-foreground">
                      {formatDate(lesson.scheduledDate)} - {formatTime(lesson.startTime)} to {formatTime(lesson.endTime)}
                    </p>
                    <p className="text-xs text-muted-foreground">{lesson.teacherName}</p>
                  </div>
                  <span
                    className={cn(
                      'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                      lesson.status === 'Scheduled' && 'bg-blue-100 text-blue-800',
                      lesson.status === 'Completed' && 'bg-green-100 text-green-800',
                      lesson.status === 'Cancelled' && 'bg-red-100 text-red-800',
                      lesson.status === 'NoShow' && 'bg-orange-100 text-orange-800'
                    )}
                  >
                    {lesson.status}
                  </span>
                </div>
              ))}
              {lessons.length > 10 && (
                <p className="text-sm text-muted-foreground pt-3">
                  Showing 10 of {lessons.length} lessons
                </p>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Invoices</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground">No invoices yet</p>
        </CardContent>
      </Card>
    </div>
  )
}
