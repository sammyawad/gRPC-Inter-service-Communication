<template>
  <div class="legend" v-if="hasAny">
    <div class="legend-item" v-for="id in ids" :key="id">
      <span class="color-swatch" :style="{ backgroundColor: swatchColor(id) }"></span>
      <span class="legend-avatar" v-if="users[id] && users[id].avatar">{{ users[id].avatar }}</span>
      <span class="legend-name">{{ displayName(id) }}</span>
    </div>
  </div>
</template>

<script>
import { inject, computed } from 'vue'

export default {
  name: 'Legend',
  setup() {
    const dataState = inject('dataState')
    const users = computed(() => dataState.users || {})
    const series = computed(() => dataState.series || {})

    // Legend should list clients: union of (non-viewer users) and active data series keys.
    const ids = computed(() => {
      const u = users.value || {}
      const nonViewerUsers = Object.entries(u)
        .filter(([_, info]) => (info?.username || '').toLowerCase() !== 'viewer')
        .map(([id]) => id)
      const seriesIds = Object.keys(series.value || {})
      return Array.from(new Set([...nonViewerUsers, ...seriesIds]))
    })

    const hasAny = computed(() => ids.value.length > 0)

    const palette = [
      '#1f77b4', '#ff7f0e', '#2ca02c', '#d62728', '#9467bd',
      '#8c564b', '#e377c2', '#7f7f7f', '#bcbd22', '#17becf'
    ]
    const hashToIdx = (s) => {
      if (!s) return 0
      let h = 0
      for (let i = 0; i < s.length; i++) {
        h = (h * 31 + s.charCodeAt(i)) >>> 0
      }
      return h % palette.length
    }
    const swatchColor = (id) => {
      const col = series.value?.[id]?.color
      return col || palette[hashToIdx(String(id))]
    }

    const displayName = (id) => {
      // Prefer per-series mode (wave) if available; fall back to user.username, else 'client'
      const s = series.value?.[id]
      const u = users.value?.[id]
      const wave = (s?.mode || '').toString().trim()
      const userMode = (u?.username || '').toString().trim()
      const mode = (wave || (userMode.toLowerCase() === 'viewer' ? '' : userMode) || 'client').toLowerCase()
      const shortId = typeof id === 'string' && id.length > 0 ? id.slice(-5) : ''
      return shortId ? `${shortId} ${mode}` : mode
    }

    // Return refs so the template stays reactive
    return { users, series, ids, hasAny, displayName, swatchColor }
  }
}
</script>

