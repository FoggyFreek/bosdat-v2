import { api } from '@/services/api'

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
