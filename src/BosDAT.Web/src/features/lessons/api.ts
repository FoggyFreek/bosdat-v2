import { api } from '@/services/api'

export const lessonsApi = {
  getAll: async (params?: {
    startDate?: string
    endDate?: string
    teacherId?: string
    studentId?: string
    courseId?: string
    roomId?: number
    status?: string
    top?: number
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
