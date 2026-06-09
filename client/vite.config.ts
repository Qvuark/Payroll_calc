import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Проксі /api → бекенд: фронт ходить на відносні шляхи,
// CORS не потрібен ні в dev, ні пізніше в Electron (SPA віддаватиметься з wwwroot API).
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5196',
    },
  },
})
