/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'

const raiz = fileURLToPath(new URL('..', import.meta.url))

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: { '/api': 'http://localhost:5220' },
    // La marca (Marca/) y los vectores de cálculo (tests/) viven fuera de frontend/.
    fs: { allow: [raiz] },
  },
  build: {
    // En producción la API sirve el frontend compilado (research R4).
    outDir: '../backend/Optica.Api/wwwroot',
    emptyOutDir: true,
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
