interface StepPlaceholderProps {
  title: string
}

export const StepPlaceholder = ({ title }: StepPlaceholderProps) => {
  return (
    <div className="flex flex-col items-center justify-center py-12 text-center">
      <h3 className="text-lg font-medium text-muted-foreground">{title}</h3>
      <p className="mt-2 text-sm text-muted-foreground">Coming soon...</p>
    </div>
  )
}
