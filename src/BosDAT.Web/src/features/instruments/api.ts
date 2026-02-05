import { api } from '@/services/api'

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
