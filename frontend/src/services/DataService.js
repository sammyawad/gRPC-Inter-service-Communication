import * as signalR from "@microsoft/signalr"

// Robust, centralized parsing helpers
const parseNumber = (val) => {
  if (typeof val === 'number') return Number.isFinite(val) ? val : NaN
  if (val == null) return NaN
  // Strip thousands separators and non-numeric (allow ., sign, exponent)
  const s = String(val).trim().replace(/,/g, '').replace(/[^0-9.\-+eE]/g, '')
  if (s === '' || s === '.' || s === '+' || s === '-' || s === '+.' || s === '-.') return NaN
  const n = Number(s)
  return Number.isFinite(n) ? n : NaN
}

const parseTimestampMs = (ts) => {
  if (typeof ts === 'number') return Number.isFinite(ts) ? ts : Date.now()
  if (!ts) return Date.now()
  const t = new Date(ts).getTime()
  return Number.isFinite(t) ? t : Date.now()
}

const normalizeKeys = (obj) => ({
  userId: obj?.UserId ?? obj?.userId ?? obj?.clientId ?? obj?.id,
  value: obj?.Value ?? obj?.value ?? obj?.val ?? obj?.data,
  timestamp: obj?.Timestamp ?? obj?.timestamp ?? obj?.ts,
  points: obj?.Points ?? obj?.points
})

export class DataService {
  constructor(dataState) {
    this.dataState = dataState || {}
    this.connection = null

    // Ensure dataState shape exists
    if (!this.dataState.series) this.dataState.series = {}
    if (!Number.isFinite(this.dataState.maxPoints)) this.dataState.maxPoints = 1000
    if (!Number.isFinite(this.dataState.totalPoints)) this.dataState.totalPoints = 0
    if (!Number.isFinite(this.dataState.windowMs)) this.dataState.windowMs = 60000
    if (typeof this.dataState.connected !== 'boolean') this.dataState.connected = false
  }

  async connect() {
    const backendUrl = this.getBackendUrl()
    console.log('[DataService] connecting to', `${backendUrl}/chathub`)

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${backendUrl}/chathub`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build()

    this.setupEventHandlers()

    await this.connection.start()
    console.log('[DataService] connection started')
    this.dataState.connected = true

    // Optional: register identity on hub if supported
    try {
      await this.connection.invoke("JoinChat", "viewer", "📊")
      console.log('[DataService] JoinChat invoked as viewer')
    } catch (e) {
      // Some hubs won’t have this method; not fatal
      console.warn('JoinChat skipped/failed:', e?.message || e)
    }

  }

  setupEventHandlers() {
    // Data updates from server (decimal values per client)
    this.connection.on("DataUpdated", (m) => {
      try {
        const n = normalizeKeys(m)
        if (n.userId === undefined || n.value === undefined) {
          console.warn('[DataService] DataUpdated missing required fields', m)
          return
        }
        const ts = parseTimestampMs(n.timestamp)
        this.appendDataPoint(n.userId, n.value, ts)
        console.debug('[DataService] DataUpdated', { user: n.userId, value: n.value, ts })
      } catch (err) {
        console.error('[DataService] failed handling DataUpdated', m, err)
      }
    })

    // Connection lifecycle logs
    this.connection.onreconnecting(() => {
      console.log("Reconnecting to data stream...")
      this.dataState.connected = false
    })

    this.connection.onreconnected(() => {
      console.log("Reconnected to data stream")
      this.dataState.connected = true
    })

    this.connection.onclose(() => {
      console.log("Data stream disconnected")
      this.dataState.connected = false
    })
  }

  // Append a data point to dataState with trimming and color assignment
  appendDataPoint(seriesId, value, timestampMs) {
    if (seriesId == null) {
      console.warn('[DataService] appendDataPoint missing seriesId')
      return
    }

    // Ensure series exists
    if (!this.dataState.series[seriesId]) {
      this.dataState.series[seriesId] = {
        color: this.pickColor(Object.keys(this.dataState.series).length),
        points: []
      }
    }

    // Parse and validate inputs
    const y = parseNumber(value)
    if (!Number.isFinite(y)) {
      console.warn('[DataService] Dropping non-finite value', { seriesId, value })
      return
    }
    const ts = parseTimestampMs(timestampMs)

    const series = this.dataState.series[seriesId]
    series.points.push({ x: ts, y })

    // Trim to maxPoints
    const maxPoints = Number.isFinite(this.dataState.maxPoints) ? this.dataState.maxPoints : 1000
    const extra = series.points.length - maxPoints
    if (extra > 0) series.points.splice(0, extra)

    // Prune out-of-range points by time window to cap memory
    this.pruneOldPoints()

    // Update UI status
    this.dataState.totalPoints = (this.dataState.totalPoints || 0) + 1
    this.dataState.lastEventTs = Date.now()
  }

  // Remove points older than current window across all series
  pruneOldPoints(nowTs) {
    const now = Number.isFinite(nowTs) ? nowTs : Date.now()
    const windowMs = Number.isFinite(this.dataState.windowMs) ? this.dataState.windowMs : 60000
    const minT = now - windowMs
    for (const s of Object.values(this.dataState.series)) {
      if (!Array.isArray(s.points) || s.points.length === 0) continue
      // Find first index >= minT to avoid allocating a new array when possible
      let idx = 0
      while (idx < s.points.length && (!Number.isFinite(s.points[idx]?.x) || s.points[idx].x < minT)) idx++
      if (idx > 0) s.points.splice(0, idx)
      // Also enforce maxPoints per series after time prune
      const maxPoints = Number.isFinite(this.dataState.maxPoints) ? this.dataState.maxPoints : 1000
      const extra = s.points.length - maxPoints
      if (extra > 0) s.points.splice(0, extra)
    }
  }

  pickColor(idx) {
    const palette = [
      '#1f77b4', '#ff7f0e', '#2ca02c', '#d62728', '#9467bd',
      '#8c564b', '#e377c2', '#7f7f7f', '#bcbd22', '#17becf'
    ]
    return palette[idx % palette.length]
  }

  async disconnect() {
    if (this.connection) {
      try {
        await this.connection.stop()
      } catch (_) {}
    }
  }

  getBackendUrl() {
    // Default to same host, backend 5000; adjust as needed for your setup
    try {
      const { protocol, hostname } = window.location
      return `${protocol}//${hostname}:5000`
    } catch {
      return 'http://localhost:5000'
    }
  }
}
