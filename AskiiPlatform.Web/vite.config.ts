import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// L'API non ha CORS configurato: il proxy fa sì che il browser parli sempre
// con l'origin di Vite, quindi in sviluppo non serve toccare il backend.
// In produzione serve o l'hosting same-origin o AddCors/UseCors lato API.
const API_TARGET = process.env.API_URL ?? 'http://localhost:5244'

const proxy = {
  '/api': {
    target: API_TARGET,
    changeOrigin: true,
  },
}

/**
 * Il watch di default usa inotify, che su Linux ha un tetto di istanze per
 * utente (`fs.inotify.max_user_instances`, tipicamente 128) facilmente saturato
 * dai language server degli editor: in quel caso Vite muore all'avvio con
 * ENOSPC. Il polling non usa inotify e parte sempre.
 *
 * Alzato il limite di sistema, si torna al watch nativo (più leggero) con:
 *   VITE_POLLING=0 npm run dev
 */
const usePolling = process.env.VITE_POLLING !== '0'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy,
    watch: usePolling
      ? {
          usePolling: true,
          interval: 300,
          // Senza queste esclusioni il polling andrebbe a fare stat su
          // decine di migliaia di file, con un costo di CPU inutile.
          ignored: ['**/node_modules/**', '**/dist/**', '**/.git/**'],
        }
      : undefined,
  },
  // `vite preview` non eredita la configurazione di `server`: va ripetuta,
  // altrimenti la build servita localmente non raggiunge l'API.
  preview: { port: 5173, proxy },
})
