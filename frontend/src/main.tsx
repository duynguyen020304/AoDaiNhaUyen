import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { PersistQueryClientProvider } from '@tanstack/react-query-persist-client';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import App from './App';
import { AuthProvider } from './auth/AuthContext';
import { ToastProvider } from './components/Toast/ToastContext';
import { queryClient } from './lib/queryClient';
import { queryPersister, shouldDehydrateQuery } from './lib/queryPersist';
import { registerServiceWorker } from './utils/serviceWorkerCache';
import './styles/global.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <PersistQueryClientProvider
      client={queryClient}
      persistOptions={{
        persister: queryPersister,
        dehydrateOptions: {
          shouldDehydrateQuery: (query) => shouldDehydrateQuery(query.queryKey),
        },
      }}
    >
      <BrowserRouter>
        <AuthProvider>
          <ToastProvider>
            <App />
            {import.meta.env.DEV ? <ReactQueryDevtools initialIsOpen={false} /> : null}
          </ToastProvider>
        </AuthProvider>
      </BrowserRouter>
    </PersistQueryClientProvider>
  </StrictMode>,
);

registerServiceWorker();
