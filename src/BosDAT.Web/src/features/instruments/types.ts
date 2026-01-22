// Instrument Domain Types

export type InstrumentCategory = 'String' | 'Percussion' | 'Vocal' | 'Keyboard' | 'Wind' | 'Brass' | 'Electronic' | 'Other'

export interface Instrument {
  id: number
  name: string
  category: InstrumentCategory
  isActive: boolean
}
