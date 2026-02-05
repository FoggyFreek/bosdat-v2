import { api } from '@/services/api'

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
