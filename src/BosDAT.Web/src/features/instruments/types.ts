// Instrument Domain Types

export type InstrumentCategory = 'String' | 'Percussion' | 'Vocal' | 'Keyboard' | 'Wind' | 'Brass' | 'Electronic' | 'Other'

export interface Instrument {
  id: number
  name: string
  category: InstrumentCategory
  isActive: boolean
}

// Translation Mappings
export const instrumentCategoryTranslations = {
  String: 'settings.instruments.categories.String',
  Percussion: 'settings.instruments.categories.Percussion',
  Vocal: 'settings.instruments.categories.Vocal',
  Keyboard: 'settings.instruments.categories.Keyboard',
  Wind: 'settings.instruments.categories.Wind',
  Brass: 'settings.instruments.categories.Brass',
  Electronic: 'settings.instruments.categories.Electronic',
  Other: 'settings.instruments.categories.Other',
} as const satisfies Record<InstrumentCategory, string>
