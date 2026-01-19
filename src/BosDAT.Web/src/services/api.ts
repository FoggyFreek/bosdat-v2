import axios, { AxiosError } from 'axios'
import type { AuthResponse, LoginDto, User } from '@/types'

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

    return Promise.reject(error)
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
}

// Teachers API
export const teachersApi = {
  getAll: async (params?: { activeOnly?: boolean; instrumentId?: number }) => {
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
}

// Courses API
export const coursesApi = {
  getAll: async (params?: { status?: string; teacherId?: string; dayOfWeek?: number }) => {
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

// Lesson Types API
export const lessonTypesApi = {
  getAll: async (params?: { activeOnly?: boolean; instrumentId?: number }) => {
    const response = await api.get('/lesson-types', { params })
    return response.data
  },

  getById: async (id: number) => {
    const response = await api.get(`/lesson-types/${id}`)
    return response.data
  },

  create: async (data: unknown) => {
    const response = await api.post('/lesson-types', data)
    return response.data
  },

  update: async (id: number, data: unknown) => {
    const response = await api.put(`/lesson-types/${id}`, data)
    return response.data
  },
}
