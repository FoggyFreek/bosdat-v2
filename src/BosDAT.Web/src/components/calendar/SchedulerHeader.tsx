import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

type SchedulerHeaderProps = {
  title: string;
  onNavigatePrevious?: () => void;
  onNavigateNext?: () => void;
};

const SchedulerHeaderComponent: React.FC<SchedulerHeaderProps> = ({
  title,
  onNavigatePrevious,
  onNavigateNext,
}) => {
  return (
    <header className="flex items-center p-6 space-x-8" role="toolbar" aria-label="Calendar navigation">
      <div className="flex space-x-4">
        <button
          type="button"
          className="w-8 h-8 flex items-center justify-center cursor-pointer border-0 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
          onClick={onNavigatePrevious}
          aria-label="Previous week"
        >
          <ChevronLeft className="w-6 h-6 text-slate-600 hover:text-slate-900 transition-colors" />
        </button>
        <button
          type="button"
          className="w-8 h-8 flex items-center justify-center cursor-pointer border-0 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
          onClick={onNavigateNext}
          aria-label="Next week"
        >
          <ChevronRight className="w-6 h-6 text-slate-600 hover:text-slate-900 transition-colors" />
        </button>
      </div>
      <h1 className="text-2xl font-normal text-slate-800">{title}</h1>
    </header>
  );
};

export const SchedulerHeader = React.memo(SchedulerHeaderComponent);
SchedulerHeader.displayName = 'SchedulerHeader';
