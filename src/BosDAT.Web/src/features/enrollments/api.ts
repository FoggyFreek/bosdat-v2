import { api } from '@/services/api'
import type { InvoicingPreference } from '@/features/students/types'

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

  create: async (data: { 
    studentId: string; 
    courseId: string; 
    discountPercent?: number; 
    discountType?: string; 
    invoicingPreference: InvoicingPreference;
    notes?: string }) => {
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
}
