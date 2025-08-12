<template>
  <div class="chart-container">
    <svg :viewBox="`0 0 ${width} ${height}`" preserveAspectRatio="none" class="chart-svg">
      <!-- Axes -->
      <line :x1="paddingLeft" :y1="height - paddingBottom" :x2="width - paddingRight" :y2="height - paddingBottom" stroke="#999" stroke-width="1" />
      <line :x1="paddingLeft" :y1="paddingTop" :x2="paddingLeft" :y2="height - paddingBottom" stroke="#999" stroke-width="1" />

      <!-- Y ticks (0..1) -->
      <g>
        <template v-for="yv in yTicks" :key="yv">
          <line :x1="paddingLeft" :x2="width - paddingRight" :y1="yScale(yv)" :y2="yScale(yv)" stroke="#eee" />
          <text :x="paddingLeft - 8" :y="yScale(yv) + 4" text-anchor="end" class="tick">{{ yv.toFixed(2) }}</text>
        </template>
      </g>

      <!-- X ticks (nicely spaced) -->
      <g>
        <template v-for="t in xTicks" :key="t">
          <line :x1="xScale(t)" :x2="xScale(t)" :y1="paddingTop" :y2="height - paddingBottom" stroke="#f3f3f3" />
          <g :transform="`translate(${xScale(t)}, ${height - paddingBottom + 2}) rotate(315)`">
            <text text-anchor="end" class="tick">{{ formatTime(t) }}</text>
          </g>
        </template>
      </g>

      <!-- Series polylines -->
      <g v-for="s in seriesList" :key="s.id">
        <polyline :points="polylinePoints(s.points)" :stroke="s.color" fill="none" stroke-width="2" />
      </g>
    </svg>
  </div>
</template>

<script>
import { inject, onMounted, onBeforeUnmount, ref, computed } from 'vue'

export default {
  name: 'LiveChart',
  setup() {
    const dataState = inject('dataState')

    const width = 900
    const height = 420
    const paddingLeft = 56
    const paddingRight = 10
    const paddingTop = 10
    const paddingBottom = 38

    const nowRef = ref(Date.now())
    let rafId = null

    const windowMs = computed(() => dataState.windowMs || 60000)

    const xMin = computed(() => nowRef.value - windowMs.value)
    const xMax = computed(() => nowRef.value)

    const xScale = (t) => {
      const x0 = paddingLeft
      const x1 = width - paddingRight
      if (!Number.isFinite(t)) return x0
      const denom = (xMax.value - xMin.value) || 1
      const frac = (t - xMin.value) / denom
      return x0 + Math.max(0, Math.min(1, frac)) * (x1 - x0)
    }
    const yScale = (v) => {
      const y0 = height - paddingBottom
      const y1 = paddingTop
      if (!Number.isFinite(v)) return y0
      const frac = (v - 0) / 1
      return y0 - Math.max(0, Math.min(1, frac)) * (y0 - y1)
    }

    // Nice Y ticks: 0, 0.25, 0.5, 0.75, 1
    const yTicks = [0, 0.25, 0.5, 0.75, 1]

    // Compute ~8-10 nicely spaced X ticks using 1/2/5 multiples of seconds
    const xTicks = computed(() => {
      const desired = 8
      const ms = windowMs.value
      const candidatesSec = [1, 2, 5, 10, 15, 20, 30, 60]
      let stepSec = candidatesSec[0]
      for (const s of candidatesSec) {
        if (ms / (s * 1000) <= desired) { stepSec = s; break }
        stepSec = s
      }
      const stepMs = stepSec * 1000
      const ticks = []
      const start = Math.ceil(xMin.value / stepMs) * stepMs
      for (let t = start; t <= xMax.value; t += stepMs) ticks.push(t)
      return ticks
    })

    const formatTime = (t) => new Date(t).toLocaleTimeString([], { hour12: false, minute: '2-digit', second: '2-digit' })

    const polylinePoints = (pts) => {
      const minT = xMin.value
      const coords = pts
        .filter(p => Number.isFinite(p?.x) && Number.isFinite(p?.y) && p.x >= minT)
        .map(p => ({ x: xScale(p.x), y: yScale(p.y) }))
        .filter(p => Number.isFinite(p.x) && Number.isFinite(p.y))
      if (coords.length < 2) return ''
      return coords.map(p => `${p.x},${p.y}`).join(' ')
    }

    // Flatten reactive map into an array for safe template iteration
    const seriesList = computed(() =>
      Object.entries(dataState.series).map(([id, s]) => ({ id, color: s.color, points: s.points }))
    )

    const animate = () => {
      nowRef.value = Date.now()
      rafId = requestAnimationFrame(animate)
    }

    onMounted(() => {
      rafId = requestAnimationFrame(animate)
    })

    onBeforeUnmount(() => {
      if (rafId) cancelAnimationFrame(rafId)
    })

    return { dataState, width, height, paddingLeft, paddingRight, paddingTop, paddingBottom, xScale, yScale, yTicks, xTicks, formatTime, polylinePoints, seriesList }
  }
}
</script>

<style>
.chart-container {
  width: 100%;
  height: 420px;
}
.chart-svg { width: 100%; height: 100%; }
.tick { font-size: 10px; fill: #444; }
</style>

