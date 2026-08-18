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

export default defineConfig({
  plugins: [react()],
  server: { port: 5173, proxy },
  // `vite preview` non eredita la configurazione di `server`: va ripetuta,
  // altrimenti la build servita localmente non raggiunge l'API.
  preview: { port: 5173, proxy },
})
