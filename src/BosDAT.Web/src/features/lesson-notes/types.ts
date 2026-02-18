export interface NoteAttachment {
  id: string
  fileName: string
  contentType: string
  fileSize: number
  url: string
}

export interface LessonNote {
  id: string
  lessonId: string
  content: string // Lexical JSON
  lessonDate: string // DateOnly from API: "YYYY-MM-DD"
  attachments: NoteAttachment[]
  createdAt: string
  updatedAt: string
}

export interface CreateLessonNote {
  content: string
}

export interface UpdateLessonNote {
  content: string
}
