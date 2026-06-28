/** @type {import('tailwindcss').Config} */
export default {
  content: [
    './index.html',
    './src/**/*.{js,jsx,ts,tsx}'
  ],
  theme: {
    extend: {
      boxShadow: {
        glow: '0 20px 45px rgba(15,23,42,0.08)'
      },
      backgroundImage: {
        'hero-gradient': 'radial-gradient(circle at top, rgba(96,165,250,0.16), transparent 45%), radial-gradient(circle at bottom right, rgba(59,130,246,0.12), transparent 35%)'
      }
    }
  },
  plugins: []
}
