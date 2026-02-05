import { api } from '@/services/api'

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
