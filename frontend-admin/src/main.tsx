import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import './styles/globals.css'
import { App } from './App'
import { queryClient } from './lib/queryClient'
import { FeedbackProvider } from '@/components/ui/feedback'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <FeedbackProvider>
        <App />
      </FeedbackProvider>
    </QueryClientProvider>
  </StrictMode>,
)
