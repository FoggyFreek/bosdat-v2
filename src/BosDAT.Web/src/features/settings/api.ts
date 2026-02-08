import { api } from '@/services/api'
import type {
  SystemSetting,
  SeederStatusResponse,
  SeederActionResponse,
  SchedulingStatus,
  ScheduleRunsResponse,
  ManualRunResult,
} from '@/features/settings/types'

export const settingsApi = {
  getAll: async (): Promise<SystemSetting[]> => {
    const response = await api.get<SystemSetting[]>('/settings')
    return response.data
  },

  getByKey: async (key: string): Promise<SystemSetting> => {
    const response = await api.get<SystemSetting>(`/settings/${key}`)
    return response.data
  },

  update: async (key: string, value: string): Promise<SystemSetting> => {
    const response = await api.put<SystemSetting>(`/settings/${key}`, { value })
    return response.data
  },
}

export const holidaysApi = {
  getAll: async () => {
    const response = await api.get('/holidays')
    return response.data
  },

  create: async (data: { name: string; startDate: string; endDate: string }) => {
    const response = await api.post('/holidays', data)
    return response.data
  },

  update: async (id: number, data: { name: string; startDate: string; endDate: string }) => {
    const response = await api.put(`/holidays/${id}`, data)
    return response.data
  },

  delete: async (id: number) => {
    await api.delete(`/holidays/${id}`)
  },
}

export const seederApi = {
  getStatus: async (): Promise<SeederStatusResponse> => {
    const response = await api.get<SeederStatusResponse>('/admin/seeder/status')
    return response.data
  },

  seed: async (): Promise<SeederActionResponse> => {
    const response = await api.post<SeederActionResponse>('/admin/seeder/seed')
    return response.data
  },

  reset: async (): Promise<SeederActionResponse> => {
    const response = await api.post<SeederActionResponse>('/admin/seeder/reset')
    return response.data
  },

  reseed: async (): Promise<SeederActionResponse> => {
    const response = await api.post<SeederActionResponse>('/admin/seeder/reseed')
    return response.data
  },
}

export const schedulingApi = {
  getStatus: async (): Promise<SchedulingStatus> => {
    const response = await api.get<SchedulingStatus>('/admin/scheduling/status')
    return response.data
  },

  getRuns: async (page = 1, pageSize = 5): Promise<ScheduleRunsResponse> => {
    const response = await api.get<ScheduleRunsResponse>('/admin/scheduling/runs', {
      params: { page, pageSize },
    })
    return response.data
  },

  runManual: async (): Promise<ManualRunResult> => {
    const response = await api.post<ManualRunResult>('/admin/scheduling/run')
    return response.data
  },

  runSingle: async (id: string): Promise<ManualRunResult> => {
    const response = await api.post<ManualRunResult>(`/admin/scheduling/run/${id}`)
    return response.data
  },
}
