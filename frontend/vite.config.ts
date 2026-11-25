import { defineConfig } from 'vite'
import path from 'path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [
    // The React and Tailwind plugins are both required for Make, even if
    // Tailwind is not being actively used – do not remove them
    react(),
    tailwindcss(),
  ],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    env: { VITE_API_BASE_URL: 'http://localhost/api' },
  },
  resolve: {
    alias: {
      '@/styles': path.resolve(__dirname, './src/styles'),
      '@': path.resolve(__dirname, './src/app'),
    },
  },
})
