<!-- filepath: c:\Users\sammy\Documents\gRPC-Inter-service-Communication\frontend\src\App.vue -->
<template>
  <div id="app">
    <div class="chat-demo">
      <h1>gRPC Chat Demo</h1>
      
      <!-- Connection Setup -->
      <div v-if="!chatState.connected" class="connection-setup">
        <h2>Join Chat</h2>
        
        <div class="user-setup">
          <div class="avatar-picker">
            <span v-for="emoji in avatars" 
                  :key="emoji"
                  @click="chatState.currentUser.avatar = emoji"
                  :class="{ active: chatState.currentUser.avatar === emoji }"
                  class="avatar-option">
              {{ emoji }}
            </span>
          </div>
          
          <input v-model="chatState.currentUser.username" 
                 placeholder="Enter your username..."
                 @keyup.enter="joinChat" />
          
          <button @click="joinChat" 
                  :disabled="!chatState.currentUser.username || chatState.connecting">
            {{ chatState.connecting ? 'Connecting...' : 'Join Chat' }}
          </button>
        </div>
      </div>
      
      <!-- Active Chat -->
      <div v-else class="active-chat">
        <div class="chat-header">
          <h2>Connected as {{ chatState.currentUser.avatar }} {{ chatState.currentUser.username }}</h2>
          <button @click="disconnect">Disconnect</button>
        </div>
        
        <!-- Online Users -->
        <div class="online-users">
          <h3>Online ({{ chatState.onlineUsers.length }})</h3>
          <div v-for="user in chatState.onlineUsers" :key="user.id" class="user">
            {{ user.avatar }} {{ user.username }}
          </div>
        </div>
        
        <!-- Messages -->
        <div class="messages" ref="messagesContainer">
          <div v-for="message in chatState.messages" :key="message.id || message.timestamp" 
               :class="['message', message.type]">
            <span v-if="message.type === 'message'" class="message-content">
              <strong>{{ message.avatar }} {{ message.username }}:</strong> {{ message.content }}
            </span>
            <span v-else class="system-message">
              {{ message.message }}
            </span>
          </div>
        </div>
        
        <!-- Input -->
        <div class="input-area">
          <input v-model="newMessage" 
                 @keyup.enter="sendMessage"
                 placeholder="Type a message..." />
          <button @click="sendMessage">Send</button>
        </div>
        
        <!-- Stats -->
        <div class="stats">
          <span>Sent: {{ chatState.stats.messagesSent }}</span>
          <span>Received: {{ chatState.stats.messagesReceived }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { inject, ref, nextTick } from 'vue'
import { ChatService } from './services/ChatService.js'

export default {
  name: 'App',
  setup() {
    const chatState = inject('chatState')
    const chatService = new ChatService(chatState)
    const newMessage = ref('')
    const messagesContainer = ref(null)
    
    const avatars = ['🦊', '🐱', '🐻', '🐸', '🦁', '🐯', '🐨', '🐼']
    
    const joinChat = async () => {
      try {
        await chatService.connect(
          chatState.currentUser.username,
          chatState.currentUser.avatar
        )
      } catch (error) {
        alert('Failed to connect: ' + error.message)
      }
    }
    
    const sendMessage = async () => {
      if (newMessage.value.trim()) {
        await chatService.sendMessage(newMessage.value)
        newMessage.value = ''
        scrollToBottom()
      }
    }
    
    const disconnect = async () => {
      await chatService.disconnect()
    }
    
    const scrollToBottom = async () => {
      await nextTick()
      if (messagesContainer.value) {
        messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
      }
    }
    
    return {
      chatState,
      newMessage,
      messagesContainer,
      avatars,
      joinChat,
      sendMessage,
      disconnect
    }
  }
}
</script>

<style>
.chat-demo {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
}

.avatar-picker {
  display: flex;
  gap: 10px;
  margin-bottom: 10px;
}

.avatar-option {
  font-size: 2em;
  cursor: pointer;
  padding: 5px;
  border-radius: 5px;
}

.avatar-option.active {
  background-color: #007bff;
}

.messages {
  height: 400px;
  overflow-y: auto;
  border: 1px solid #ccc;
  padding: 10px;
  margin: 10px 0;
}

.message {
  margin-bottom: 10px;
}

.system-message {
  font-style: italic;
  color: #666;
}

.input-area {
  display: flex;
  gap: 10px;
}

.input-area input {
  flex: 1;
  padding: 8px;
}

.stats {
  margin-top: 10px;
  display: flex;
  gap: 20px;
  font-size: 0.9em;
  color: #666;
}
</style>