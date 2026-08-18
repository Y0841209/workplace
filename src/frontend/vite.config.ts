import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig(({ mode }) => {
  const isDev = mode === 'development';
  
  return {
    plugins: [
      react({
        // Enable React Fast Refresh
        fastRefresh: true,
      }),
      VitePWA({
        registerType: 'autoUpdate',
        includeAssets: ['favicon.ico', 'robots.txt'],
        manifest: {
          name: 'Workplace Booking Platform',
          short_name: 'Booking',
          description: 'Reserva de espacios de trabajo',
          theme_color: '#FFD800',
          background_color: '#0E0E0E',
          display: 'standalone',
          orientation: 'portrait-primary',
          scope: '/',
          start_url: '/',
          icons: [
            {
              src: '/icons/icon-72x72.png',
              sizes: '72x72',
              type: 'image/png',
            },
            {
              src: '/icons/icon-96x96.png',
              sizes: '96x96',
              type: 'image/png',
            },
            {
              src: '/icons/icon-128x128.png',
              sizes: '128x128',
              type: 'image/png',
            },
            {
              src: '/icons/icon-144x144.png',
              sizes: '144x144',
              type: 'image/png',
            },
            {
              src: '/icons/icon-152x152.png',
              sizes: '152x152',
              type: 'image/png',
            },
            {
              src: '/icons/icon-192x192.png',
              sizes: '192x192',
              type: 'image/png',
              purpose: 'any maskable',
            },
            {
              src: '/icons/icon-384x384.png',
              sizes: '384x384',
              type: 'image/png',
            },
            {
              src: '/icons/icon-512x512.png',
              sizes: '512x512',
              type: 'image/png',
            },
          ],
        },
        workbox: {
          globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
          cleanupOutdatedCaches: true,
          clientsClaim: true,
          skipWaiting: true,
        },
      }),
    ],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
        '@components': path.resolve(__dirname, './src/components'),
        '@pages': path.resolve(__dirname, './src/pages'),
        '@hooks': path.resolve(__dirname, './src/hooks'),
        '@services': path.resolve(__dirname, './src/services'),
        '@contexts': path.resolve(__dirname, './src/contexts'),
        '@types': path.resolve(__dirname, './src/types'),
        '@utils': path.resolve(__dirname, './src/utils'),
        '@theme': path.resolve(__dirname, './src/theme'),
        '@layouts': path.resolve(__dirname, './src/layouts'),
        '@assets': path.resolve(__dirname, './src/assets'),
      },
    },
    server: {
      port: 3000,
      host: true,
      strictPort: true,
      hmr: {
        port: 24678,
      },
      proxy: {
        '/api': {
          target: 'http://localhost:8080',
          changeOrigin: true,
          secure: false,
        },
        '/health': {
          target: 'http://localhost:8080',
          changeOrigin: true,
        },
        '/hangfire': {
          target: 'http://localhost:8080',
          changeOrigin: true,
        },
      },
    },
    build: {
      outDir: 'dist',
      sourcemap: !isDev,
      minify: 'esbuild',
      target: 'es2022',
      cssCodeSplit: true,
      rollupOptions: {
        output: {
          manualChunks: {
            vendor: ['react', 'react-dom', 'react-router-dom'],
            mui: ['@mui/material', '@mui/icons-material', '@mui/x-date-pickers', '@mui/x-data-grid'],
            query: ['@tanstack/react-query'],
            forms: ['react-hook-form', '@hookform/resolvers', 'zod'],
            utils: ['date-fns', 'date-fns-tz', 'clsx', 'tailwind-merge'],
            charts: ['html2canvas', 'file-saver'],
          },
          chunkFileNames: 'assets/js/[name]-[hash].js',
          entryFileNames: 'assets/js/[name]-[hash].js',
          assetFileNames: (assetInfo) => {
            const info = assetInfo.name.split('.');
            const ext = info[info.length - 1];
            if (/\.(png|jpe?g|gif|svg|ico|webp)$/.test(assetInfo.name)) {
              return `assets/images/[name]-[hash].${ext}`;
            }
            if (/\.(woff2?|ttf|eot)$/.test(assetInfo.name)) {
              return `assets/fonts/[name]-[hash].${ext}`;
            }
            if (/\.css$/.test(assetInfo.name)) {
              return `assets/css/[name]-[hash].${ext}`;
            }
            return `assets/[name]-[hash].${ext}`;
          },
        },
      },
    },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      include: ['src/**/*.{test,spec}.{ts,tsx}'],
      coverage: {
        provider: 'v8',
        reporter: ['text', 'json', 'html', 'lcov'],
        exclude: [
          'node_modules/',
          'src/test/',
          'src/main.tsx',
          'src/vite-env.d.ts',
          '**/*.d.ts',
          '**/*.config.*',
        ],
      },
    },
    define: {
      'import.meta.env.VITE_API_BASE_URL': JSON.stringify(process.env.VITE_API_BASE_URL || '/api/v1'),
      'import.meta.env.VITE_APP_TITLE': JSON.stringify(process.env.VITE_APP_TITLE || 'Workplace Booking'),
    },
  };
});