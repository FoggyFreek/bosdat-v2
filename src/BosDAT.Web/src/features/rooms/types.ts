// Room Domain Types

export interface Room {
  id: number
  name: string
  floorLevel?: number
  capacity: number
  hasPiano: boolean
  hasDrums: boolean
  hasAmplifier: boolean
  hasMicrophone: boolean
  hasWhiteboard: boolean
  hasStereo: boolean
  hasGuitar: boolean
  isActive: boolean
  notes?: string
  activeCourseCount: number
  scheduledLessonCount: number
}

export interface CreateRoom {
  name: string
  floorLevel?: number
  capacity: number
  hasPiano: boolean
  hasDrums: boolean
  hasAmplifier: boolean
  hasMicrophone: boolean
  hasWhiteboard: boolean
  hasStereo: boolean
  hasGuitar: boolean
  notes?: string
}

export interface UpdateRoom {
  name: string
  floorLevel?: number
  capacity: number
  hasPiano: boolean
  hasDrums: boolean
  hasAmplifier: boolean
  hasMicrophone: boolean
  hasWhiteboard: boolean
  hasStereo: boolean
  hasGuitar: boolean
  notes?: string
}
