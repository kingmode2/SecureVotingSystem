import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
    // This ensures _redirects is copied
    rollupOptions: {
      output: {
        assetFileNames: (assetInfo) => {
          // Keep _redirects as-is, not hashed
          if (assetInfo.name === '_redirects') {
            return '_redirects'
          }
          return 'assets/[name]-[hash][extname]'
        }
      }
    }
  },
  // Explicitly set public directory
  publicDir: 'public'
})