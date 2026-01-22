// Global Type Utilities
// Add shared type utilities here (e.g., Prettify<T>)

export type Prettify<T> = {
  [K in keyof T]: T[K]
} & {}