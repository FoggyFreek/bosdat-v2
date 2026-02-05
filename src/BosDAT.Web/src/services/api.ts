/// <reference types="vite/client" />

import axios, { AxiosError } from 'axios'
import type { AuthResponse, LoginDto, User } from '@/features/auth/types'
import type {
  CheckDuplicatesDto,
  DuplicateCheckResult,
  RegistrationFeeStatus,
  StudentLedgerEntry,
  StudentLedgerSummary,
  CreateStudentLedgerEntry,
  DecoupleApplicationResult,
  EnrollmentPricing,
} from '@/features/students/types'

const API_URL = import.meta.env.VITE_API_URL || ''

export const api = axios.create({
  baseURL: `${API_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor to handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && originalRequest && !originalRequest.headers['X-Retry']) {
      const refreshToken = localStorage.getItem('refreshToken')

      if (refreshToken) {
        try {
          const response = await axios.post<AuthResponse>(`${API_URL}/api/auth/refresh`, {
            refreshToken,
          })

          const { token, refreshToken: newRefreshToken } = response.data

          localStorage.setItem('token', token)
          localStorage.setItem('refreshToken', newRefreshToken)

          originalRequest.headers.Authorization = `Bearer ${token}`
          originalRequest.headers['X-Retry'] = 'true'

          return api(originalRequest)
        } catch {
          localStorage.removeItem('token')
          localStorage.removeItem('refreshToken')
          window.location.href = '/login'
        }
      }
    }

     throw error;
  }
)

// Auth API
export const authApi = {
  login: async (data: LoginDto): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/login', data)
    return response.data
  },

  logout: async (): Promise<void> => {
    const refreshToken = localStorage.getItem('refreshToken')
    if (refreshToken) {
      try {
        await api.post('/auth/logout', { refreshToken })
      } catch {
        // Ignore errors on logout
      }
    }
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
  },

  getCurrentUser: async (): Promise<User> => {
    const response = await api.get<User>('/auth/me')
    return response.data
  },
}

// Students API
export const studentsApi = {
  getAll: async (params?: { search?: string; status?: string }) => {
    const response = await api.get('/students', { params })
    return response.data
  },

  getById: async (id: string) => {
    const response = await api.get(`/students/${id}`)
    return response.data
  },

  getWithEnrollments: async (id: string) => {
    const response = await api.get(`/students/${id}/enrollments`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/students', data)
    return response.data
  },

  update: async (id: string, data: unknown) => {
    const response = await api.put(`/students/${id}`, data)
    return response.data
  },

  delete: async (id: string) => {
    await api.delete(`/students/${id}`)
  },

  checkDuplicates: async (data: CheckDuplicatesDto): Promise<DuplicateCheckResult> => {
    const response = await api.post<DuplicateCheckResult>('/students/check-duplicates', data)
    return response.data
  },

  getRegistrationFeeStatus: async (id: string): Promise<RegistrationFeeStatus> => {
    const response = await api.get<RegistrationFeeStatus>(`/students/${id}/registration-fee`)
    return response.data
  },

  hasActiveEnrollments: async (id: string): Promise<boolean> => {
    const response = await api.get<boolean>(`/students/${id}/has-active-enrollments`)
    return response.data
  },
}

// Teachers API
export const teachersApi = {
  getAll: async (params?: { activeOnly?: boolean; instrumentId?: number; courseTypeId?: string }) => {
    const response = await api.get('/teachers', { params })
    return response.data
  },

  getById: async (id: string) => {
    const response = await api.get(`/teachers/${id}`)
    return response.data
  },

  getWithCourses: async (id: string) => {
    const response = await api.get(`/teachers/${id}/courses`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/teachers', data)
    return response.data
  },

  update: async (id: string, data: unknown) => {
    const response = await api.put(`/teachers/${id}`, data)
    return response.data
  },

  delete: async (id: string) => {
    await api.delete(`/teachers/${id}`)
  },

  getAvailableCourseTypes: async (id: string, instrumentIds: number[]) => {
    const params = instrumentIds.length > 0 ? { instrumentIds: instrumentIds.join(',') } : {}
    const response = await api.get(`/teachers/${id}/available-course-types`, { params })
    return response.data
  },

  getAvailability: async (id: string) => {
    const response = await api.get(`/teachers/${id}/availability`)
    return response.data
  },

  updateAvailability: async (id: string, data: { dayOfWeek: number; fromTime: string; untilTime: string }[]) => {
    const response = await api.put(`/teachers/${id}/availability`, data)
    return response.data
  },
}

// Instruments API
export const instrumentsApi = {
  getAll: async (params?: { activeOnly?: boolean }) => {
    const response = await api.get('/instruments', { params })
    return response.data
  },

  getById: async (id: number) => {
    const response = await api.get(`/instruments/${id}`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/instruments', data)
    return response.data
  },

  update: async (id: number, data: unknown) => {
    const response = await api.put(`/instruments/${id}`, data)
    return response.data
  },
}

// Rooms API
export const roomsApi = {
  getAll: async (params?: { activeOnly?: boolean }) => {
    const response = await api.get('/rooms', { params })
    return response.data
  },

  getById: async (id: number) => {
    const response = await api.get(`/rooms/${id}`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/rooms', data)
    return response.data
  },

  update: async (id: number, data: unknown) => {
    const response = await api.put(`/rooms/${id}`, data)
    return response.data
  },

  delete: async (id: number) => {
    await api.delete(`/rooms/${id}`)
  },

  archive: async (id: number) => {
    const response = await api.put(`/rooms/${id}/archive`)
    return response.data
  },

  reactivate: async (id: number) => {
    const response = await api.put(`/rooms/${id}/reactivate`)
    return response.data
  },
}

// Courses API
export const coursesApi = {
  getCount: async (params?: { status?: string; teacherId?: string; dayOfWeek?: number; roomId?: number }) => {
    const response = await api.get('/courses/count', { params })
    return response.data
  },

  getSummary: async (params?: { status?: string; teacherId?: string; dayOfWeek?: number; roomId?: number }) => {
    const response = await api.get('/courses/summary', { params })
    return response.data
  },

  getAll: async (params?: { status?: string; teacherId?: string; dayOfWeek?: number; roomId?: number }) => {
    const response = await api.get('/courses', { params })
    return response.data
  },

  getById: async (id: string) => {
    const response = await api.get(`/courses/${id}`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/courses', data)
    return response.data
  },

  update: async (id: string, data: unknown) => {
    const response = await api.put(`/courses/${id}`, data)
    return response.data
  },

  enroll: async (courseId: string, data: { studentId: string; discountPercent?: number; notes?: string }) => {
    const response = await api.post(`/courses/${courseId}/enroll`, data)
    return response.data
  },
}

// Course Types API
export const courseTypesApi = {
  getAll: async (params?: { activeOnly?: boolean; instrumentId?: number }) => {
    const response = await api.get('/course-types', { params })
    return response.data
  },

  getById: async (id: string) => {
    const response = await api.get(`/course-types/${id}`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/course-types', data)
    return response.data
  },

  update: async (id: string, data: unknown) => {
    const response = await api.put(`/course-types/${id}`, data)
    return response.data
  },

  delete: async (id: string) => {
    await api.delete(`/course-types/${id}`)
  },

  reactivate: async (id: string) => {
    const response = await api.put(`/course-types/${id}/reactivate`)
    return response.data
  },

  getTeacherCountForInstrument: async (instrumentId: number) => {
    const response = await api.get(`/course-types/teachers-for-instrument/${instrumentId}`)
    return response.data
  },

  // Pricing endpoints
  getPricingHistory: async (id: string) => {
    const response = await api.get(`/course-types/${id}/pricing/history`)
    return response.data
  },

  checkPricingEditability: async (id: string) => {
    const response = await api.get(`/course-types/${id}/pricing/can-edit`)
    return response.data
  },

  updatePricing: async (id: string, data: { priceAdult: number; priceChild: number }) => {
    const response = await api.put(`/course-types/${id}/pricing`, data)
    return response.data
  },

  createPricingVersion: async (id: string, data: { priceAdult: number; priceChild: number; validFrom: string }) => {
    const response = await api.post(`/course-types/${id}/pricing/versions`, data)
    return response.data
  },
}

// Enrollments API
export const enrollmentsApi = {
  getAll: async (params?: { studentId?: string; courseId?: string; status?: string }) => {
    const response = await api.get('/enrollments', { params })
    return response.data
  },

  getById: async (id: string) => {
    const response = await api.get(`/enrollments/${id}`)
    return response.data
  },

  getByStudent: async (studentId: string) => {
    const response = await api.get(`/enrollments/student/${studentId}`)
    return response.data
  },

  create: async (data: { studentId: string; courseId: string; discountPercent?: number; notes?: string }) => {
    const response = await api.post('/enrollments', data)
    return response.data
  },

  update: async (id: string, data: { discountPercent: number; status: string; notes?: string }) => {
    const response = await api.put(`/enrollments/${id}`, data)
    return response.data
  },

  delete: async (id: string) => {
    await api.delete(`/enrollments/${id}`)
  },

  promoteFromTrail: async (id: string) => {
    const response = await api.put(`/enrollments/${id}/promote`)
    return response.data
  },

  getEnrollmentPricing: async (studentId: string, courseId: string): Promise<EnrollmentPricing> => {
    const response = await api.get<EnrollmentPricing>(
      `/enrollments/student/${studentId}/course/${courseId}/pricing`
    )
    return response.data
  }
}

// Lessons API
export const lessonsApi = {
  getAll: async (params?: {
    startDate?: string
    endDate?: string
    teacherId?: string
    studentId?: string
    roomId?: number
    status?: string
  }) => {
    const response = await api.get('/lessons', { params })
    return response.data
  },

  getById: async (id: string) => {
    const response = await api.get(`/lessons/${id}`)
    return response.data
  },

  getByStudent: async (studentId: string) => {
    const response = await api.get(`/lessons/student/${studentId}`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/lessons', data)
    return response.data
  },

  update: async (id: string, data: unknown) => {
    const response = await api.put(`/lessons/${id}`, data)
    return response.data
  },

  updateStatus: async (id: string, data: { status: string; cancellationReason?: string }) => {
    const response = await api.put(`/lessons/${id}/status`, data)
    return response.data
  },

  delete: async (id: string) => {
    await api.delete(`/lessons/${id}`)
  },

  generate: async (data: { courseId: string; startDate: string; endDate: string; skipHolidays?: boolean }) => {
    const response = await api.post('/lessons/generate', data)
    return response.data
  },

  generateBulk: async (data: { startDate: string; endDate: string; skipHolidays?: boolean }) => {
    const response = await api.post('/lessons/generate-bulk', data)
    return response.data
  },
}

// Calendar API
export const calendarApi = {
  getWeek: async (params?: { date?: string; teacherId?: string; roomId?: number }) => {
    const response = await api.get('/calendar/week', { params })
    return response.data
  },

  getDay: async (params?: { date?: string; teacherId?: string; roomId?: number }) => {
    const response = await api.get('/calendar/day', { params })
    return response.data
  },

  getMonth: async (params?: { year?: number; month?: number; teacherId?: string; roomId?: number }) => {
    const response = await api.get('/calendar/month', { params })
    return response.data
  },

  getTeacherSchedule: async (teacherId: string, date?: string) => {
    const response = await api.get(`/calendar/teacher/${teacherId}`, { params: { date } })
    return response.data
  },

  getRoomSchedule: async (roomId: number, date?: string) => {
    const response = await api.get(`/calendar/room/${roomId}`, { params: { date } })
    return response.data
  },

  checkAvailability: async (params: {
    date: string
    startTime: string
    endTime: string
    teacherId?: string
    roomId?: number
  }) => {
    const response = await api.get('/calendar/availability', { params })
    return response.data
  },
}

// Holidays API
export const holidaysApi = {
  getAll: async () => {
    const response = await api.get('/holidays')
    return response.data
  },

  create: async (data: { name: string; startDate: string; endDate: string }) => {
    const response = await api.post('/holidays', data)
    return response.data
  },

  update: async (id: number, data: { name: string; startDate: string; endDate: string }) => {
    const response = await api.put(`/holidays/${id}`, data)
    return response.data
  },

  delete: async (id: number) => {
    await api.delete(`/holidays/${id}`)
  },
}

// Settings API
export interface Setting {
  key: string
  value: string
  type?: string
  description?: string
}

export const settingsApi = {
  getAll: async (): Promise<Setting[]> => {
    const response = await api.get<Setting[]>('/settings')
    return response.data
  },

  getByKey: async (key: string): Promise<Setting> => {
    const response = await api.get<Setting>(`/settings/${key}`)
    return response.data
  },

  update: async (key: string, value: string): Promise<Setting> => {
    const response = await api.put<Setting>(`/settings/${key}`, { value })
    return response.data
  },
}

// Student Ledger API
export const studentLedgerApi = {
  getByStudent: async (studentId: string): Promise<StudentLedgerEntry[]> => {
    const response = await api.get<StudentLedgerEntry[]>(`/studentledger/student/${studentId}`)
    return response.data
  },

  getSummary: async (studentId: string): Promise<StudentLedgerSummary> => {
    const response = await api.get<StudentLedgerSummary>(`/studentledger/student/${studentId}/summary`)
    return response.data
  },

  getById: async (id: string): Promise<StudentLedgerEntry> => {
    const response = await api.get<StudentLedgerEntry>(`/studentledger/${id}`)
    return response.data
  },

  create: async (data: CreateStudentLedgerEntry): Promise<StudentLedgerEntry> => {
    const response = await api.post<StudentLedgerEntry>('/studentledger', data)
    return response.data
  },

  reverse: async (id: string, reason: string): Promise<StudentLedgerEntry> => {
    const response = await api.post<StudentLedgerEntry>(`/studentledger/${id}/reverse`, { reason })
    return response.data
  },

  getAvailableCredit: async (studentId: string): Promise<{ availableCredit: number }> => {
    const response = await api.get<{ availableCredit: number }>(`/studentledger/student/${studentId}/available-credit`)
    return response.data
  },

  decouple: async (applicationId: string, reason: string): Promise<DecoupleApplicationResult> => {
    const response = await api.post<DecoupleApplicationResult>(
      `/studentledger/applications/${applicationId}/decouple`,
      { reason },
    )
    return response.data
  },
}

// Seeder API Types
export interface SeederStatusResponse {
  isSeeded: boolean
  environment: string
  canSeed: boolean
  canReset: boolean
}

export interface SeederActionResponse {
  success: boolean
  message: string
  action: string
}

// Seeder API (Admin only, Development environment)
export const seederApi = {
  getStatus: async (): Promise<SeederStatusResponse> => {
    const response = await api.get<SeederStatusResponse>('/admin/seeder/status')
    return response.data
  },

  seed: async (): Promise<SeederActionResponse> => {
    const response = await api.post<SeederActionResponse>('/admin/seeder/seed')
    return response.data
  },

  reset: async (): Promise<SeederActionResponse> => {
    const response = await api.post<SeederActionResponse>('/admin/seeder/reset')
    return response.data
  },

  reseed: async (): Promise<SeederActionResponse> => {
    const response = await api.post<SeederActionResponse>('/admin/seeder/reseed')
    return response.data
  },
}
