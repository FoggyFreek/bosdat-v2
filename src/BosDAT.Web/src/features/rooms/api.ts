import { api } from '@/services/api'

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
