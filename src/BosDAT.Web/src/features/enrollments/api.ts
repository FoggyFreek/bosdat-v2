import { api } from '@/services/api'
import type { EnrollmentPricing } from '@/features/students/types'

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
