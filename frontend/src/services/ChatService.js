import * as signalR from "@microsoft/signalr"

export class ChatService {
  constructor(chatState) {
    this.chatState = chatState
    this.connection = null
  }

  async connect(username, avatar, room = 'general') {
    try {
      this.chatState.connecting = true

      // Use environment-aware backend URL
      const backendUrl = this.getBackendUrl()
      
      // Create SignalR connection to your C# backend
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${backendUrl}/chathub`)
        .withAutomaticReconnect()
        .build()

      // Set up event handlers
      this.setupEventHandlers()

      // Start connection
      await this.connection.start()
      
      // Join the chat
      await this.connection.invoke("JoinChat", username, avatar)
      
      // Update global state
      this.chatState.connected = true
      this.chatState.connecting = false
      this.chatState.stats.connectionTime = Date.now()
      this.chatState.currentUser.username = username
      this.chatState.currentUser.avatar = avatar

      console.log("Connected to chat!")

    } catch (error) {
      this.chatState.connecting = false
      this.chatState.connected = false
      console.error("Connection failed:", error)
      throw error
    }
  }

  setupEventHandlers() {
    // Receive messages
    this.connection.on("ReceiveMessage", (message) => {
      this.chatState.messages.push(message)
      this.chatState.stats.messagesReceived++
      console.log("Received message:", message)
    })

    // User joined
    this.connection.on("UserJoined", (notification) => {
      this.chatState.messages.push(notification)
      console.log("User joined:", notification)
    })

    // User left
    this.connection.on("UserLeft", (notification) => {
      this.chatState.messages.push(notification)
      console.log("User left:", notification)
    })

    // Online users update
    this.connection.on("OnlineUsersUpdate", (users) => {
      this.chatState.onlineUsers = users
      console.log("Online users updated:", users)
    })

    // Typing indicators
    this.connection.on("UserTyping", (data) => {
      const user = this.chatState.onlineUsers.find(u => u.id === data.userId)
      if (user) {
        user.typing = data.isTyping
      }
    })

    // Connection events
    this.connection.onreconnecting(() => {
      this.chatState.connected = false
      console.log("Reconnecting...")
    })

    this.connection.onreconnected(() => {
      this.chatState.connected = true
      console.log("Reconnected!")
    })

    this.connection.onclose(() => {
      this.chatState.connected = false
      console.log("Disconnected")
    })
  }

  async sendMessage(content) {
    if (this.connection && content.trim()) {
      try {
        await this.connection.invoke("SendMessage", content)
        this.chatState.stats.messagesSent++
        console.log("Sent message:", content)
      } catch (error) {
        console.error("Failed to send message:", error)
      }
    }
  }

  async sendTypingIndicator(isTyping) {
    if (this.connection) {
      try {
        await this.connection.invoke("SendTypingIndicator", isTyping)
      } catch (error) {
        console.error("Failed to send typing indicator:", error)
      }
    }
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      this.chatState.connected = false
      this.chatState.messages = []
      this.chatState.onlineUsers = []
      this.chatState.currentUser.username = ''
      console.log("Disconnected from chat")
    }
  }

  getBackendUrl() {
    // Check if we're running in Docker vs development
    const isDocker = window.location.port === '3000'
    const isDev = window.location.port === '5173'
    
    if (isDocker) {
      // Browser can reach backend via host machine port mapping
      return 'http://localhost:5000'
    } else if (isDev) {
      // Development mode (Vite dev server)
      return 'http://localhost:5000'
    } else {
      // Fallback
      return 'http://localhost:5000'
    }
  }
}