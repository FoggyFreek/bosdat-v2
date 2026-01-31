// Utility functions for date and time calculations

/**
 * Checks if two dates represent the same calendar day
 * @param date1 - First date to compare
 * @param date2 - Second date to compare
 * @returns True if both dates are the same calendar day
 */
export const isSameDay = (date1: Date, date2: Date): boolean => {
  return (
    date1.getFullYear() === date2.getFullYear() &&
    date1.getMonth() === date2.getMonth() &&
    date1.getDate() === date2.getDate()
  );
};

/**
 * Extracts the date portion from an ISO datetime string as a Date object
 * @param dateTimeString - ISO 8601 datetime string
 * @returns Date object representing the date
 */
export const getDateFromDateTime = (dateTimeString: string): Date => {
  return new Date(dateTimeString);
};

/**
 * Calculates the start time as decimal hours from an ISO datetime string
 * @param dateTimeString - ISO 8601 datetime string
 * @returns Hours as decimal (e.g., 9.5 for 9:30 AM)
 */
export const getDecimalHours = (dateTimeString: string): number => {
  const date = new Date(dateTimeString);
  const hours = date.getHours();
  const minutes = date.getMinutes();
  return hours + minutes / 60;
};

/**
 * Calculates duration in decimal hours between two datetime strings
 * @param startDateTime - ISO 8601 datetime string
 * @param endDateTime - ISO 8601 datetime string
 * @returns Duration in hours as decimal (e.g., 1.5 for 90 minutes)
 */
export const getDurationInHours = (startDateTime: string, endDateTime: string): number => {
  const start = new Date(startDateTime);
  const end = new Date(endDateTime);
  const durationMs = end.getTime() - start.getTime();
  return durationMs / (1000 * 60 * 60); // Convert milliseconds to hours
};

/**
 * Formats datetime to display time string
 * @param startDateTime - ISO 8601 datetime string
 * @param endDateTime - ISO 8601 datetime string
 * @returns Formatted time string (e.g., "09:30 – 10:00")
 */
export const formatTimeRange = (startDateTime: string, endDateTime: string): string => {
  const start = new Date(startDateTime);
  const end = new Date(endDateTime);

  const formatTime = (date: Date) => {
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${hours}:${minutes}`;
  };

  return `${formatTime(start)} – ${formatTime(end)}`;
};

/**
 * Validates that an event has valid time bounds
 * @param startDateTime - ISO 8601 datetime string
 * @param endDateTime - ISO 8601 datetime string
 * @returns True if the event times are valid (end is after start)
 */
export const isValidEventTime = (startDateTime: string, endDateTime: string): boolean => {
  const start = new Date(startDateTime);
  const end = new Date(endDateTime);
  return end.getTime() > start.getTime();
};
