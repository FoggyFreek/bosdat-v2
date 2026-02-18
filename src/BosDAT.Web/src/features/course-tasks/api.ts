import { api } from '@/services/api'
import type { CourseTask, CreateCourseTask } from './types'

export const courseTasksApi = {
  getByCourse: (courseId: string) =>
    api.get<CourseTask[]>(`/courses/${courseId}/tasks`).then(r => r.data),

  create: (courseId: string, data: CreateCourseTask) =>
    api.post<CourseTask>(`/courses/${courseId}/tasks`, data).then(r => r.data),

  delete: (courseId: string, taskId: string) =>
    api.delete(`/courses/${courseId}/tasks/${taskId}`),
}
