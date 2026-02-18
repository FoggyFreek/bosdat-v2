import { api } from '@/services/api'
import type { LessonNote, NoteAttachment } from './types'

export const lessonNotesApi = {
  getByCourse: (lessonId: string) =>
    api.get<LessonNote[]>(`/lessons/${lessonId}/notes`).then(r => r.data),

  create: (lessonId: string, content: string) =>
    api
      .post<LessonNote>(`/lessons/${lessonId}/notes`, { content })
      .then(r => r.data),

  update: (lessonId: string, noteId: string, content: string) =>
    api
      .put<LessonNote>(`/lessons/${lessonId}/notes/${noteId}`, { content })
      .then(r => r.data),

  delete: (lessonId: string, noteId: string) =>
    api.delete(`/lessons/${lessonId}/notes/${noteId}`),

  addAttachment: (lessonId: string, noteId: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return api
      .post<NoteAttachment>(`/lessons/${lessonId}/notes/${noteId}/attachments`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then(r => r.data)
  },

  deleteAttachment: (lessonId: string, noteId: string, attachmentId: string) =>
    api.delete(`/lessons/${lessonId}/notes/${noteId}/attachments/${attachmentId}`),
}
