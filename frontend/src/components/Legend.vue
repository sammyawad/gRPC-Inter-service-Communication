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

    // Show only connected data-sending clients by listing active series keys
    const series = computed(() => dataState.series || {})

    const ids = computed(() => Object.keys(series.value || {}))

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

    // Name format: last 5 of id + mode (graph type) + range, e.g., "566b2 sine [-1, 5]"
    const displayName = (id) => {
      const s = series.value?.[id] || {}
      const shortId = typeof id === 'string' && id.length > 0 ? id.slice(-5) : String(id)
      const graphType = (s.graphType || s.mode || 'client').toString().toLowerCase()
      
      // Add Y range if available
      let rangeStr = ''
      if (Number.isFinite(s.yMin) && Number.isFinite(s.yMax)) {
        rangeStr = ` [${s.yMin.toFixed(1)}, ${s.yMax.toFixed(1)}]`
      }
      
      return `${shortId} ${graphType}${rangeStr}`
    }

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

