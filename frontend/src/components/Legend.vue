<template>
  <div class="legend" v-if="hasAny">
    <div class="legend-item" v-for="id in ids" :key="id">
      <span class="color-swatch" :style="{ backgroundColor: swatchColor(id) }"></span>
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

    // Presence-driven legend: reflect currently connected clients
    const clients = computed(() => dataState.clients || {})
    const series = computed(() => dataState.series || {})

    // Use stable order from presence updates; if none, fallback to presence keys; if no presence, fallback to series keys
    const ids = computed(() => {
      const order = Array.isArray(dataState.clientOrder) ? dataState.clientOrder : []
      if (order.length > 0) return order
      const presenceKeys = Object.keys(clients.value || {})
      if (presenceKeys.length > 0) return presenceKeys
      return Object.keys(series.value || {})
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

    // Prefer series color if available; otherwise stable hash-based palette per id
    const swatchColor = (id) => {
      const col = series.value?.[id]?.color
      return col || palette[hashToIdx(String(id))]
    }

    // Prefer username from presence; fallback to short id and optional graph type
    const displayName = (id) => {
      const presence = clients.value?.[id]
      if (presence?.username) return presence.username
      const s = series.value?.[id] || {}
      const shortId = typeof id === 'string' && id.length > 0 ? id.slice(-5) : String(id)
      const graphType = (s.graphType || s.mode || 'client').toString()
      return `${shortId} ${graphType}`
    }

    // Return refs so the template stays reactive
    return { series, ids, hasAny, displayName, swatchColor }
  }
}
</script>

<style>
.legend {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  align-items: center;
}
.legend-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.9rem;
}
.color-swatch {
  width: 12px;
  height: 12px;
  border-radius: 2px;
  display: inline-block;
}
</style>

