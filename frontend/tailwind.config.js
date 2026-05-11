/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{vue,js,ts}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#f4f7fb',
          100: '#e5ecf6',
          200: '#cfdcef',
          300: '#aec4e3',
          400: '#87a5d3',
          500: '#6889c5',
          600: '#526fb5',
          700: '#465c9c',
          800: '#3e4f81',
          900: '#364468',
        },
      },
    },
  },
  plugins: [],
};
