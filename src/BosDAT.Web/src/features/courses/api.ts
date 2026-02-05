import { api } from '@/services/api'

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
