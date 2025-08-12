import { createApp, reactive } from 'vue'
import App from './App.vue'

// Data state for live decimal series per client
const dataState = reactive({
  // Map of seriesId -> { color: string, points: Array<{ x: number, y: number }> }
  series: {},
  // Keep roughly up to 60s at 10Hz (or faster if desired)
  maxPoints: 6000,
  // Time window to render in ms
  windowMs: 15000,
  // Connection/data status for small UI indicator
  connected: false,
  totalPoints: 0,
  lastEventTs: null
})

// Make it available to all components
const app = createApp(App)
app.provide('dataState', dataState)
app.mount('#app')
