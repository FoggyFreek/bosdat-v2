// Global Type Utilities

/**
 * Flattens complex intersections into a single object type.
 * The '& {}' intersection forces TypeScript to resolve the type 
 * immediately, providing cleaner hover tooltips in your IDE.
 */
export type Prettify<T> = {
  [K in keyof T]: T[K]
} & {}