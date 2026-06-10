import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Проксі /api → бекенд: фронт ходить на відносні шляхи, CORS не потрібен.
// Прод-збірка лягає прямо у wwwroot API — програма працює одним процесом
// (dotnet run → localhost:5196 віддає і SPA, і API; same-origin).
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5196',
    },
  },
  build: {
    outDir: '../src/PayrollCalc.API/wwwroot',
    emptyOutDir: true,
  },
})
