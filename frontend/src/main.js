import { createApp, reactive } from 'vue'
import App from './App.vue'

// Create our global chat state
const chatState = reactive({
  // Connection
  connected: false,
  connecting: false,
  
  // Current user
  currentUser: {
    username: '',
    avatar: '🦊'
  },
  
  // Chat data
  messages: [],
  onlineUsers: [],
  currentRoom: 'general',
  
  // Demo stats
  stats: {
    messagesSent: 0,
    messagesReceived: 0,
    connectionTime: null
  }
})

// Make it available to all components
const app = createApp(App)
app.provide('chatState', chatState)
app.mount('#app')