import { api } from '@/services/api'
import type { Absence, CreateAbsence, UpdateAbsence } from './types'

export const absencesApi = {
  getAll: async (): Promise<Absence[]> => {
    const response = await api.get<Absence[]>('/absences')
    return response.data
  },

  getById: async (id: string): Promise<Absence> => {
    const response = await api.get<Absence>(`/absences/${id}`)
    return response.data
  },

  getByStudent: async (studentId: string): Promise<Absence[]> => {
    const response = await api.get<Absence[]>(`/absences/student/${studentId}`)
    return response.data
  },

  getByTeacher: async (teacherId: string): Promise<Absence[]> => {
    const response = await api.get<Absence[]>(`/absences/teacher/${teacherId}`)
    return response.data
  },

  getTeacherAbsencesForPeriod: async (startDate: string, endDate: string): Promise<Absence[]> => {
    const response = await api.get<Absence[]>('/absences/teacher-absences', {
      params: { startDate, endDate },
    })
    return response.data
  },

  create: async (data: CreateAbsence): Promise<Absence> => {
    const response = await api.post<Absence>('/absences', data)
    return response.data
  },

  update: async (id: string, data: UpdateAbsence): Promise<Absence> => {
    const response = await api.put<Absence>(`/absences/${id}`, data)
    return response.data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/absences/${id}`)
  },
}
