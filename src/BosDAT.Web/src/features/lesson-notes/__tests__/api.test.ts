import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import { api } from '@/services/api'
import { lessonNotesApi } from '../api'

describe('lessonNotesApi', () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('getByCourse fetches all notes for a lesson', async () => {
    const notes = [{ id: 'note-1', lessonId: 'lesson-1', content: '{}', attachments: [] }]
    mock.onGet('/lessons/lesson-1/notes').reply(200, notes)

    const result = await lessonNotesApi.getByCourse('lesson-1')

    expect(result).toEqual(notes)
  })

  it('create posts a new note for a lesson', async () => {
    const content = '{"root":{"children":[]}}'
    const created = { id: 'note-2', lessonId: 'lesson-1', content, attachments: [] }
    mock.onPost('/lessons/lesson-1/notes').reply(201, created)

    const result = await lessonNotesApi.create('lesson-1', content)

    expect(result).toEqual(created)
    expect(JSON.parse(mock.history.post[0].data)).toEqual({ content })
  })

  it('update puts updated note content', async () => {
    const content = '{"root":{"children":[{"text":"Updated"}]}}'
    const updated = { id: 'note-1', lessonId: 'lesson-1', content, attachments: [] }
    mock.onPut('/lessons/lesson-1/notes/note-1').reply(200, updated)

    const result = await lessonNotesApi.update('lesson-1', 'note-1', content)

    expect(result).toEqual(updated)
    expect(JSON.parse(mock.history.put[0].data)).toEqual({ content })
  })

  it('delete removes a note', async () => {
    mock.onDelete('/lessons/lesson-1/notes/note-1').reply(204)

    await lessonNotesApi.delete('lesson-1', 'note-1')

    expect(mock.history.delete).toHaveLength(1)
    expect(mock.history.delete[0].url).toBe('/lessons/lesson-1/notes/note-1')
  })

  it('addAttachment uploads a file', async () => {
    const attachment = { id: 'att-1', fileName: 'file.pdf', contentType: 'application/pdf', fileSize: 1024, url: '/api/files/file.pdf' }
    mock.onPost('/lessons/lesson-1/notes/note-1/attachments').reply(201, attachment)

    const file = new File(['content'], 'file.pdf', { type: 'application/pdf' })
    const result = await lessonNotesApi.addAttachment('lesson-1', 'note-1', file)

    expect(result).toEqual(attachment)
    expect(mock.history.post[0].url).toBe('/lessons/lesson-1/notes/note-1/attachments')
  })

  it('deleteAttachment removes an attachment', async () => {
    mock.onDelete('/lessons/lesson-1/notes/note-1/attachments/att-1').reply(204)

    await lessonNotesApi.deleteAttachment('lesson-1', 'note-1', 'att-1')

    expect(mock.history.delete).toHaveLength(1)
    expect(mock.history.delete[0].url).toBe('/lessons/lesson-1/notes/note-1/attachments/att-1')
  })
})
