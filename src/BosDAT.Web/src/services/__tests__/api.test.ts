import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import axios from 'axios'
import MockAdapter from 'axios-mock-adapter'
import {
  api,
  authApi,
  studentsApi,
  teachersApi,
  instrumentsApi,
  roomsApi,
  coursesApi,
  courseTypesApi,
  enrollmentsApi,
  lessonsApi,
  calendarApi,
  holidaysApi,
  settingsApi,
  studentLedgerApi,
} from '../api'

describe('API Configuration', () => {
  it('creates axios instance with correct baseURL', () => {
    // The baseURL is set from import.meta.env.VITE_API_URL at module load time
    // In tests, this defaults to empty string, so baseURL is '/api'
    expect(api.defaults.baseURL).toMatch(/\/api$/)
  })

  it('sets Content-Type header to application/json', () => {
    expect(api.defaults.headers['Content-Type']).toBe('application/json')
  })
})

describe('Request Interceptor', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
    localStorage.clear()
  })

  afterEach(() => {
    mock.restore()
  })

  it('adds Authorization header when token exists', async () => {
    localStorage.setItem('token', 'test-token')

    mock.onGet('/test').reply((config) => {
      expect(config.headers?.Authorization).toBe('Bearer test-token')
      return [200, {}]
    })

    await api.get('/test')
  })

  it('does not add Authorization header when token is missing', async () => {
    mock.onGet('/test').reply((config) => {
      expect(config.headers?.Authorization).toBeUndefined()
      return [200, {}]
    })

    await api.get('/test')
  })

  it('handles request interceptor error', async () => {
    // This tests the error path in the request interceptor
    mock.onGet('/test').networkError()

    await expect(api.get('/test')).rejects.toThrow()
  })
})

describe('Response Interceptor - Token Refresh', () => {
  let mock: MockAdapter
  let axiosMock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
    axiosMock = new MockAdapter(axios)
    localStorage.clear()
  })

  afterEach(() => {
    mock.restore()
    axiosMock.restore()
  })

  it('refreshes token on 401 error and retries request', async () => {
    localStorage.setItem('token', 'old-token')
    localStorage.setItem('refreshToken', 'refresh-token')

    let callCount = 0
    mock.onGet('/protected').reply(() => {
      callCount++
      if (callCount === 1) {
        // First request fails with 401
        return [401]
      }
      // After refresh, retry succeeds
      return [200, { data: 'success' }]
    })

    // Refresh endpoint returns new tokens (using base axios instance)
    axiosMock.onPost('/api/auth/refresh').reply(200, {
      token: 'new-token',
      refreshToken: 'new-refresh-token',
    })

    const response = await api.get('/protected')

    expect(response.data).toEqual({ data: 'success' })
    expect(localStorage.getItem('token')).toBe('new-token')
    expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token')
    expect(callCount).toBe(2) // Verify it was called twice (original + retry)
  })

  it('does not refresh when no refresh token exists', async () => {
    localStorage.setItem('token', 'old-token')
    // No refresh token

    mock.onGet('/protected').reply(401)

    await expect(api.get('/protected')).rejects.toThrow()

    // Should not call refresh endpoint
    expect(axiosMock.history.post.length).toBe(0)
  })

  it('redirects to login when token refresh fails', async () => {
    // Mock window.location.href setter
    const mockHref = vi.fn()
    Object.defineProperty(window, 'location', {
      value: { href: '' },
      writable: true,
    })
    Object.defineProperty(window.location, 'href', {
      set: mockHref,
      get: () => '/login',
    })

    localStorage.setItem('token', 'old-token')
    localStorage.setItem('refreshToken', 'refresh-token')

    mock.onGet('/protected').reply(401)
    axiosMock.onPost('/api/auth/refresh').reply(401)

    await expect(api.get('/protected')).rejects.toThrow()

    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
    expect(mockHref).toHaveBeenCalledWith('/login')
  })

  it('does not retry request with X-Retry header already set', async () => {
    localStorage.setItem('token', 'old-token')
    localStorage.setItem('refreshToken', 'refresh-token')

    let requestCount = 0
    mock.onGet('/protected').reply((config) => {
      requestCount++
      return [401]
    })

    await expect(
      api.get('/protected', {
        headers: { 'X-Retry': 'true' },
      })
    ).rejects.toThrow()

    // Should only make one request, not retry
    expect(requestCount).toBe(1)
  })

  it('does not refresh on non-401 errors', async () => {
    localStorage.setItem('token', 'old-token')
    localStorage.setItem('refreshToken', 'refresh-token')

    mock.onGet('/error').reply(500)

    await expect(api.get('/error')).rejects.toThrow()

    // Should not call refresh endpoint
    expect(axiosMock.history.post.length).toBe(0)
  })

  it('passes through successful responses', async () => {
    mock.onGet('/success').reply(200, { message: 'success' })

    const response = await api.get('/success')

    expect(response.data).toEqual({ message: 'success' })
  })
})

describe('authApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
    localStorage.clear()
  })

  afterEach(() => {
    mock.restore()
  })

  it('login returns auth response', async () => {
    const authResponse = {
      token: 'jwt-token',
      refreshToken: 'refresh-token',
      user: { id: '1', email: 'test@example.com' },
    }

    mock.onPost('/auth/login').reply(200, authResponse)

    const result = await authApi.login({
      email: 'test@example.com',
      password: 'password',
    })

    expect(result).toEqual(authResponse)
    expect(mock.history.post[0].data).toBe(JSON.stringify({
      email: 'test@example.com',
      password: 'password',
    }))
  })

  it('logout removes tokens and calls API with refresh token', async () => {
    localStorage.setItem('token', 'token')
    localStorage.setItem('refreshToken', 'refresh-token')

    mock.onPost('/auth/logout').reply(200)

    await authApi.logout()

    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
    expect(mock.history.post.length).toBe(1)
    expect(mock.history.post[0].data).toBe(JSON.stringify({ refreshToken: 'refresh-token' }))
  })

  it('logout removes tokens even if API call fails', async () => {
    localStorage.setItem('token', 'token')
    localStorage.setItem('refreshToken', 'refresh-token')

    mock.onPost('/auth/logout').reply(500)

    await authApi.logout()

    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
  })

  it('logout clears tokens when no refresh token exists', async () => {
    localStorage.setItem('token', 'token')

    await authApi.logout()

    expect(localStorage.getItem('token')).toBeNull()
    expect(mock.history.post.length).toBe(0)
  })

  it('getCurrentUser returns user data', async () => {
    const user = { id: '1', email: 'test@example.com', name: 'Test User' }
    mock.onGet('/auth/me').reply(200, user)

    const result = await authApi.getCurrentUser()

    expect(result).toEqual(user)
  })
})

describe('studentsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches students without params', async () => {
    const students = [{ id: '1', name: 'John' }]
    mock.onGet('/students').reply(200, students)

    const result = await studentsApi.getAll()

    expect(result).toEqual(students)
  })

  it('getAll fetches students with params', async () => {
    const students = [{ id: '1', name: 'John' }]
    mock.onGet('/students').reply((config) => {
      expect(config.params).toEqual({ search: 'John', status: 'Active' })
      return [200, students]
    })

    const result = await studentsApi.getAll({ search: 'John', status: 'Active' })

    expect(result).toEqual(students)
  })

  it('getById fetches single student', async () => {
    const student = { id: '1', name: 'John' }
    mock.onGet('/students/1').reply(200, student)

    const result = await studentsApi.getById('1')

    expect(result).toEqual(student)
  })

  it('getWithEnrollments fetches student with enrollments', async () => {
    const data = { id: '1', name: 'John', enrollments: [] }
    mock.onGet('/students/1/enrollments').reply(200, data)

    const result = await studentsApi.getWithEnrollments('1')

    expect(result).toEqual(data)
  })

  it('create posts student data', async () => {
    const newStudent = { firstName: 'John', lastName: 'Doe' }
    const created = { id: '1', ...newStudent }
    mock.onPost('/students').reply(200, created)

    const result = await studentsApi.create(newStudent)

    expect(result).toEqual(created)
  })

  it('update puts student data', async () => {
    const updates = { firstName: 'Jane' }
    const updated = { id: '1', ...updates }
    mock.onPut('/students/1').reply(200, updated)

    const result = await studentsApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('delete removes student', async () => {
    mock.onDelete('/students/1').reply(204)

    await studentsApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
    expect(mock.history.delete[0].url).toBe('/students/1')
  })

  it('checkDuplicates returns duplicate check result', async () => {
    const checkData = {
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
    }
    const checkResult = { hasDuplicates: true, duplicates: [] }
    mock.onPost('/students/check-duplicates').reply(200, checkResult)

    const result = await studentsApi.checkDuplicates(checkData)

    expect(result).toEqual(checkResult)
  })

  it('getRegistrationFeeStatus returns registration fee status', async () => {
    const status = { hasPaid: true, amount: 50 }
    mock.onGet('/students/1/registration-fee').reply(200, status)

    const result = await studentsApi.getRegistrationFeeStatus('1')

    expect(result).toEqual(status)
  })

  it('hasActiveEnrollments returns boolean', async () => {
    mock.onGet('/students/1/has-active-enrollments').reply(200, true)

    const result = await studentsApi.hasActiveEnrollments('1')

    expect(result).toBe(true)
  })
})

describe('teachersApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches teachers without params', async () => {
    const teachers = [{ id: '1', name: 'Teacher' }]
    mock.onGet('/teachers').reply(200, teachers)

    const result = await teachersApi.getAll()

    expect(result).toEqual(teachers)
  })

  it('getAll fetches teachers with params', async () => {
    const teachers = [{ id: '1', name: 'Teacher' }]
    mock.onGet('/teachers').reply((config) => {
      expect(config.params).toEqual({ activeOnly: true, instrumentId: 5, courseTypeId: 'ct-1' })
      return [200, teachers]
    })

    const result = await teachersApi.getAll({ activeOnly: true, instrumentId: 5, courseTypeId: 'ct-1' })

    expect(result).toEqual(teachers)
  })

  it('getById fetches single teacher', async () => {
    const teacher = { id: '1', name: 'Teacher' }
    mock.onGet('/teachers/1').reply(200, teacher)

    const result = await teachersApi.getById('1')

    expect(result).toEqual(teacher)
  })

  it('getWithCourses fetches teacher with courses', async () => {
    const data = { id: '1', name: 'Teacher', courses: [] }
    mock.onGet('/teachers/1/courses').reply(200, data)

    const result = await teachersApi.getWithCourses('1')

    expect(result).toEqual(data)
  })

  it('create posts teacher data', async () => {
    const newTeacher = { name: 'New Teacher' }
    const created = { id: '1', ...newTeacher }
    mock.onPost('/teachers').reply(200, created)

    const result = await teachersApi.create(newTeacher)

    expect(result).toEqual(created)
  })

  it('update puts teacher data', async () => {
    const updates = { name: 'Updated' }
    const updated = { id: '1', ...updates }
    mock.onPut('/teachers/1').reply(200, updated)

    const result = await teachersApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('delete removes teacher', async () => {
    mock.onDelete('/teachers/1').reply(204)

    await teachersApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
  })

  it('getAvailableCourseTypes with instrument IDs', async () => {
    const courseTypes = [{ id: 'ct-1', name: 'Piano' }]
    mock.onGet('/teachers/1/available-course-types').reply((config) => {
      expect(config.params).toEqual({ instrumentIds: '1,2,3' })
      return [200, courseTypes]
    })

    const result = await teachersApi.getAvailableCourseTypes('1', [1, 2, 3])

    expect(result).toEqual(courseTypes)
  })

  it('getAvailableCourseTypes without instrument IDs', async () => {
    const courseTypes = [{ id: 'ct-1', name: 'Piano' }]
    mock.onGet('/teachers/1/available-course-types').reply((config) => {
      expect(config.params).toEqual({})
      return [200, courseTypes]
    })

    const result = await teachersApi.getAvailableCourseTypes('1', [])

    expect(result).toEqual(courseTypes)
  })
})

describe('instrumentsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches instruments', async () => {
    const instruments = [{ id: 1, name: 'Piano' }]
    mock.onGet('/instruments').reply(200, instruments)

    const result = await instrumentsApi.getAll()

    expect(result).toEqual(instruments)
  })

  it('getAll with activeOnly param', async () => {
    const instruments = [{ id: 1, name: 'Piano' }]
    mock.onGet('/instruments').reply((config) => {
      expect(config.params).toEqual({ activeOnly: true })
      return [200, instruments]
    })

    const result = await instrumentsApi.getAll({ activeOnly: true })

    expect(result).toEqual(instruments)
  })

  it('getById fetches single instrument', async () => {
    const instrument = { id: 1, name: 'Piano' }
    mock.onGet('/instruments/1').reply(200, instrument)

    const result = await instrumentsApi.getById(1)

    expect(result).toEqual(instrument)
  })

  it('create posts instrument data', async () => {
    const newInstrument = { name: 'Guitar' }
    const created = { id: 2, ...newInstrument }
    mock.onPost('/instruments').reply(200, created)

    const result = await instrumentsApi.create(newInstrument)

    expect(result).toEqual(created)
  })

  it('update puts instrument data', async () => {
    const updates = { name: 'Updated Piano' }
    const updated = { id: 1, ...updates }
    mock.onPut('/instruments/1').reply(200, updated)

    const result = await instrumentsApi.update(1, updates)

    expect(result).toEqual(updated)
  })
})

describe('roomsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches rooms', async () => {
    const rooms = [{ id: 1, name: 'Room A' }]
    mock.onGet('/rooms').reply(200, rooms)

    const result = await roomsApi.getAll()

    expect(result).toEqual(rooms)
  })

  it('getById fetches single room', async () => {
    const room = { id: 1, name: 'Room A' }
    mock.onGet('/rooms/1').reply(200, room)

    const result = await roomsApi.getById(1)

    expect(result).toEqual(room)
  })

  it('create posts room data', async () => {
    const newRoom = { name: 'Room B' }
    const created = { id: 2, ...newRoom }
    mock.onPost('/rooms').reply(200, created)

    const result = await roomsApi.create(newRoom)

    expect(result).toEqual(created)
  })

  it('update puts room data', async () => {
    const updates = { name: 'Updated Room' }
    const updated = { id: 1, ...updates }
    mock.onPut('/rooms/1').reply(200, updated)

    const result = await roomsApi.update(1, updates)

    expect(result).toEqual(updated)
  })

  it('delete removes room', async () => {
    mock.onDelete('/rooms/1').reply(204)

    await roomsApi.delete(1)

    expect(mock.history.delete.length).toBe(1)
  })

  it('archive archives room', async () => {
    const archived = { id: 1, name: 'Room A', isArchived: true }
    mock.onPut('/rooms/1/archive').reply(200, archived)

    const result = await roomsApi.archive(1)

    expect(result).toEqual(archived)
  })

  it('reactivate reactivates room', async () => {
    const reactivated = { id: 1, name: 'Room A', isArchived: false }
    mock.onPut('/rooms/1/reactivate').reply(200, reactivated)

    const result = await roomsApi.reactivate(1)

    expect(result).toEqual(reactivated)
  })
})

describe('coursesApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches courses with params', async () => {
    const courses = [{ id: '1', name: 'Piano 101' }]
    mock.onGet('/courses').reply((config) => {
      expect(config.params).toEqual({ status: 'Active', teacherId: 't-1', dayOfWeek: 1 })
      return [200, courses]
    })

    const result = await coursesApi.getAll({ status: 'Active', teacherId: 't-1', dayOfWeek: 1 })

    expect(result).toEqual(courses)
  })

  it('getById fetches single course', async () => {
    const course = { id: '1', name: 'Piano 101' }
    mock.onGet('/courses/1').reply(200, course)

    const result = await coursesApi.getById('1')

    expect(result).toEqual(course)
  })

  it('create posts course data', async () => {
    const newCourse = { name: 'Guitar 101' }
    const created = { id: '2', ...newCourse }
    mock.onPost('/courses').reply(200, created)

    const result = await coursesApi.create(newCourse)

    expect(result).toEqual(created)
  })

  it('update puts course data', async () => {
    const updates = { name: 'Updated Course' }
    const updated = { id: '1', ...updates }
    mock.onPut('/courses/1').reply(200, updated)

    const result = await coursesApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('enroll enrolls student in course', async () => {
    const enrollmentData = { studentId: 's-1', discountPercent: 10, notes: 'Test' }
    const enrollment = { id: 'e-1', ...enrollmentData }
    mock.onPost('/courses/c-1/enroll').reply(200, enrollment)

    const result = await coursesApi.enroll('c-1', enrollmentData)

    expect(result).toEqual(enrollment)
  })
})

describe('courseTypesApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches course types', async () => {
    const types = [{ id: '1', name: 'Individual' }]
    mock.onGet('/course-types').reply(200, types)

    const result = await courseTypesApi.getAll()

    expect(result).toEqual(types)
  })

  it('getAll with params', async () => {
    const types = [{ id: '1', name: 'Individual' }]
    mock.onGet('/course-types').reply((config) => {
      expect(config.params).toEqual({ activeOnly: true, instrumentId: 5 })
      return [200, types]
    })

    const result = await courseTypesApi.getAll({ activeOnly: true, instrumentId: 5 })

    expect(result).toEqual(types)
  })

  it('getById fetches single course type', async () => {
    const type = { id: '1', name: 'Individual' }
    mock.onGet('/course-types/1').reply(200, type)

    const result = await courseTypesApi.getById('1')

    expect(result).toEqual(type)
  })

  it('create posts course type data', async () => {
    const newType = { name: 'Group' }
    const created = { id: '2', ...newType }
    mock.onPost('/course-types').reply(200, created)

    const result = await courseTypesApi.create(newType)

    expect(result).toEqual(created)
  })

  it('update puts course type data', async () => {
    const updates = { name: 'Updated' }
    const updated = { id: '1', ...updates }
    mock.onPut('/course-types/1').reply(200, updated)

    const result = await courseTypesApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('delete removes course type', async () => {
    mock.onDelete('/course-types/1').reply(204)

    await courseTypesApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
  })

  it('reactivate reactivates course type', async () => {
    const reactivated = { id: '1', name: 'Individual', isActive: true }
    mock.onPut('/course-types/1/reactivate').reply(200, reactivated)

    const result = await courseTypesApi.reactivate('1')

    expect(result).toEqual(reactivated)
  })

  it('getTeacherCountForInstrument returns count', async () => {
    const count = { count: 5 }
    mock.onGet('/course-types/teachers-for-instrument/1').reply(200, count)

    const result = await courseTypesApi.getTeacherCountForInstrument(1)

    expect(result).toEqual(count)
  })

  it('getPricingHistory returns pricing history', async () => {
    const history = [{ validFrom: '2024-01-01', priceAdult: 50 }]
    mock.onGet('/course-types/1/pricing/history').reply(200, history)

    const result = await courseTypesApi.getPricingHistory('1')

    expect(result).toEqual(history)
  })

  it('checkPricingEditability returns editability status', async () => {
    const status = { canEdit: true, reason: '' }
    mock.onGet('/course-types/1/pricing/can-edit').reply(200, status)

    const result = await courseTypesApi.checkPricingEditability('1')

    expect(result).toEqual(status)
  })

  it('updatePricing updates pricing', async () => {
    const pricing = { priceAdult: 60, priceChild: 40 }
    const updated = { id: '1', ...pricing }
    mock.onPut('/course-types/1/pricing').reply(200, updated)

    const result = await courseTypesApi.updatePricing('1', pricing)

    expect(result).toEqual(updated)
  })

  it('createPricingVersion creates new pricing version', async () => {
    const versionData = { priceAdult: 60, priceChild: 40, validFrom: '2024-06-01' }
    const created = { id: 'v-1', ...versionData }
    mock.onPost('/course-types/1/pricing/versions').reply(200, created)

    const result = await courseTypesApi.createPricingVersion('1', versionData)

    expect(result).toEqual(created)
  })
})

describe('enrollmentsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches enrollments with params', async () => {
    const enrollments = [{ id: '1' }]
    mock.onGet('/enrollments').reply((config) => {
      expect(config.params).toEqual({ studentId: 's-1', courseId: 'c-1', status: 'Active' })
      return [200, enrollments]
    })

    const result = await enrollmentsApi.getAll({ studentId: 's-1', courseId: 'c-1', status: 'Active' })

    expect(result).toEqual(enrollments)
  })

  it('getById fetches single enrollment', async () => {
    const enrollment = { id: '1' }
    mock.onGet('/enrollments/1').reply(200, enrollment)

    const result = await enrollmentsApi.getById('1')

    expect(result).toEqual(enrollment)
  })

  it('getByStudent fetches student enrollments', async () => {
    const enrollments = [{ id: '1' }]
    mock.onGet('/enrollments/student/s-1').reply(200, enrollments)

    const result = await enrollmentsApi.getByStudent('s-1')

    expect(result).toEqual(enrollments)
  })

  it('create posts enrollment data', async () => {
    const newEnrollment = { studentId: 's-1', courseId: 'c-1', discountPercent: 10, notes: 'Test' }
    const created = { id: '1', ...newEnrollment }
    mock.onPost('/enrollments').reply(200, created)

    const result = await enrollmentsApi.create(newEnrollment)

    expect(result).toEqual(created)
  })

  it('update puts enrollment data', async () => {
    const updates = { discountPercent: 15, status: 'Active', notes: 'Updated' }
    const updated = { id: '1', ...updates }
    mock.onPut('/enrollments/1').reply(200, updated)

    const result = await enrollmentsApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('delete removes enrollment', async () => {
    mock.onDelete('/enrollments/1').reply(204)

    await enrollmentsApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
  })

  it('promoteFromTrail promotes enrollment', async () => {
    const promoted = { id: '1', status: 'Active' }
    mock.onPut('/enrollments/1/promote').reply(200, promoted)

    const result = await enrollmentsApi.promoteFromTrail('1')

    expect(result).toEqual(promoted)
  })

  it('getEnrollmentPricing returns pricing', async () => {
    const pricing = { price: 50, discount: 5 }
    mock.onGet('/enrollments/student/s-1/course/c-1/pricing').reply(200, pricing)

    const result = await enrollmentsApi.getEnrollmentPricing('s-1', 'c-1')

    expect(result).toEqual(pricing)
  })
})

describe('lessonsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches lessons with params', async () => {
    const lessons = [{ id: '1' }]
    const params = {
      startDate: '2024-01-01',
      endDate: '2024-01-31',
      teacherId: 't-1',
      studentId: 's-1',
      roomId: 1,
      status: 'Scheduled',
    }
    mock.onGet('/lessons').reply((config) => {
      expect(config.params).toEqual(params)
      return [200, lessons]
    })

    const result = await lessonsApi.getAll(params)

    expect(result).toEqual(lessons)
  })

  it('getById fetches single lesson', async () => {
    const lesson = { id: '1' }
    mock.onGet('/lessons/1').reply(200, lesson)

    const result = await lessonsApi.getById('1')

    expect(result).toEqual(lesson)
  })

  it('getByStudent fetches student lessons', async () => {
    const lessons = [{ id: '1' }]
    mock.onGet('/lessons/student/s-1').reply(200, lessons)

    const result = await lessonsApi.getByStudent('s-1')

    expect(result).toEqual(lessons)
  })

  it('create posts lesson data', async () => {
    const newLesson = { date: '2024-01-01', time: '10:00' }
    const created = { id: '1', ...newLesson }
    mock.onPost('/lessons').reply(200, created)

    const result = await lessonsApi.create(newLesson)

    expect(result).toEqual(created)
  })

  it('update puts lesson data', async () => {
    const updates = { time: '11:00' }
    const updated = { id: '1', ...updates }
    mock.onPut('/lessons/1').reply(200, updated)

    const result = await lessonsApi.update('1', updates)

    expect(result).toEqual(updated)
  })

  it('updateStatus updates lesson status', async () => {
    const statusUpdate = { status: 'Cancelled', cancellationReason: 'Sick' }
    const updated = { id: '1', ...statusUpdate }
    mock.onPut('/lessons/1/status').reply(200, updated)

    const result = await lessonsApi.updateStatus('1', statusUpdate)

    expect(result).toEqual(updated)
  })

  it('delete removes lesson', async () => {
    mock.onDelete('/lessons/1').reply(204)

    await lessonsApi.delete('1')

    expect(mock.history.delete.length).toBe(1)
  })

  it('generate generates lessons', async () => {
    const generateData = { courseId: 'c-1', startDate: '2024-01-01', endDate: '2024-01-31', skipHolidays: true }
    const generated = { count: 4 }
    mock.onPost('/lessons/generate').reply(200, generated)

    const result = await lessonsApi.generate(generateData)

    expect(result).toEqual(generated)
  })

  it('generateBulk generates bulk lessons', async () => {
    const bulkData = { startDate: '2024-01-01', endDate: '2024-01-31', skipHolidays: true }
    const generated = { count: 20 }
    mock.onPost('/lessons/generate-bulk').reply(200, generated)

    const result = await lessonsApi.generateBulk(bulkData)

    expect(result).toEqual(generated)
  })
})

describe('calendarApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getWeek fetches week calendar', async () => {
    const data = [{ date: '2024-01-01', lessons: [] }]
    mock.onGet('/calendar/week').reply((config) => {
      expect(config.params).toEqual({ date: '2024-01-01', teacherId: 't-1', roomId: 1 })
      return [200, data]
    })

    const result = await calendarApi.getWeek({ date: '2024-01-01', teacherId: 't-1', roomId: 1 })

    expect(result).toEqual(data)
  })

  it('getDay fetches day calendar', async () => {
    const data = { date: '2024-01-01', lessons: [] }
    mock.onGet('/calendar/day').reply(200, data)

    const result = await calendarApi.getDay()

    expect(result).toEqual(data)
  })

  it('getMonth fetches month calendar', async () => {
    const data = [{ date: '2024-01-01', lessons: [] }]
    mock.onGet('/calendar/month').reply((config) => {
      expect(config.params).toEqual({ year: 2024, month: 1, teacherId: 't-1', roomId: 1 })
      return [200, data]
    })

    const result = await calendarApi.getMonth({ year: 2024, month: 1, teacherId: 't-1', roomId: 1 })

    expect(result).toEqual(data)
  })

  it('getTeacherSchedule fetches teacher schedule', async () => {
    const schedule = { lessons: [] }
    mock.onGet('/calendar/teacher/t-1').reply((config) => {
      expect(config.params).toEqual({ date: '2024-01-01' })
      return [200, schedule]
    })

    const result = await calendarApi.getTeacherSchedule('t-1', '2024-01-01')

    expect(result).toEqual(schedule)
  })

  it('getRoomSchedule fetches room schedule', async () => {
    const schedule = { lessons: [] }
    mock.onGet('/calendar/room/1').reply(200, schedule)

    const result = await calendarApi.getRoomSchedule(1)

    expect(result).toEqual(schedule)
  })

  it('checkAvailability checks availability', async () => {
    const params = {
      date: '2024-01-01',
      startTime: '10:00',
      endTime: '11:00',
      teacherId: 't-1',
      roomId: 1,
    }
    const availability = { isAvailable: true }
    mock.onGet('/calendar/availability').reply((config) => {
      expect(config.params).toEqual(params)
      return [200, availability]
    })

    const result = await calendarApi.checkAvailability(params)

    expect(result).toEqual(availability)
  })
})

describe('holidaysApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches holidays', async () => {
    const holidays = [{ id: 1, name: 'Christmas' }]
    mock.onGet('/holidays').reply(200, holidays)

    const result = await holidaysApi.getAll()

    expect(result).toEqual(holidays)
  })

  it('create posts holiday data', async () => {
    const newHoliday = { name: 'New Year', startDate: '2024-01-01', endDate: '2024-01-01' }
    const created = { id: 1, ...newHoliday }
    mock.onPost('/holidays').reply(200, created)

    const result = await holidaysApi.create(newHoliday)

    expect(result).toEqual(created)
  })

  it('update puts holiday data', async () => {
    const updates = { name: 'Updated', startDate: '2024-01-01', endDate: '2024-01-02' }
    const updated = { id: 1, ...updates }
    mock.onPut('/holidays/1').reply(200, updated)

    const result = await holidaysApi.update(1, updates)

    expect(result).toEqual(updated)
  })

  it('delete removes holiday', async () => {
    mock.onDelete('/holidays/1').reply(204)

    await holidaysApi.delete(1)

    expect(mock.history.delete.length).toBe(1)
  })
})

describe('settingsApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getAll fetches settings', async () => {
    const settings = [{ key: 'theme', value: 'dark' }]
    mock.onGet('/settings').reply(200, settings)

    const result = await settingsApi.getAll()

    expect(result).toEqual(settings)
  })

  it('getByKey fetches single setting', async () => {
    const setting = { key: 'theme', value: 'dark' }
    mock.onGet('/settings/theme').reply(200, setting)

    const result = await settingsApi.getByKey('theme')

    expect(result).toEqual(setting)
  })

  it('update updates setting value', async () => {
    const updated = { key: 'theme', value: 'light' }
    mock.onPut('/settings/theme').reply(200, updated)

    const result = await settingsApi.update('theme', 'light')

    expect(result).toEqual(updated)
    expect(mock.history.put[0].data).toBe(JSON.stringify({ value: 'light' }))
  })
})

describe('studentLedgerApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getByStudent fetches student ledger entries', async () => {
    const entries = [{ id: '1', amount: 50 }]
    mock.onGet('/studentledger/student/s-1').reply(200, entries)

    const result = await studentLedgerApi.getByStudent('s-1')

    expect(result).toEqual(entries)
  })

  it('getSummary fetches ledger summary', async () => {
    const summary = { totalDebit: 100, totalCredit: 50, balance: 50 }
    mock.onGet('/studentledger/student/s-1/summary').reply(200, summary)

    const result = await studentLedgerApi.getSummary('s-1')

    expect(result).toEqual(summary)
  })

  it('getById fetches single ledger entry', async () => {
    const entry = { id: '1', amount: 50 }
    mock.onGet('/studentledger/1').reply(200, entry)

    const result = await studentLedgerApi.getById('1')

    expect(result).toEqual(entry)
  })

  it('create posts ledger entry', async () => {
    const newEntry = {
      description: 'Test ledger entry',
      studentId: 's-1',
      amount: 50,
      entryType: 'Debit' as const,
    }
    const created = { id: '1', ...newEntry }
    mock.onPost('/studentledger').reply(200, created)

    const result = await studentLedgerApi.create(newEntry)

    expect(result).toEqual(created)
  })

  it('reverse reverses ledger entry', async () => {
    const reversed = { id: '2', amount: -50, originalEntryId: '1' }
    mock.onPost('/studentledger/1/reverse').reply(200, reversed)

    const result = await studentLedgerApi.reverse('1', 'Correction needed')

    expect(result).toEqual(reversed)
    expect(mock.history.post[0].data).toBe(JSON.stringify({ reason: 'Correction needed' }))
  })

  it('getAvailableCredit fetches available credit', async () => {
    const credit = { availableCredit: 100 }
    mock.onGet('/studentledger/student/s-1/available-credit').reply(200, credit)

    const result = await studentLedgerApi.getAvailableCredit('s-1')

    expect(result).toEqual(credit)
  })
})
