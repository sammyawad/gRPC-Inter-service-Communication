<template>
  <div class="chart-container">
    <canvas ref="canvasEl" class="chart-canvas"></canvas>
  </div>
</template>

<script>
import { inject, onMounted, onBeforeUnmount, ref, computed, watchEffect } from 'vue'
import {
  Chart,
  LineController,
  LineElement,
  PointElement,
  TimeScale,
  LinearScale,
  Title,
  Tooltip,
  Legend
} from 'chart.js'
import 'chartjs-adapter-date-fns'

Chart.register(LineController, LineElement, PointElement, TimeScale, LinearScale, Title, Tooltip, Legend)

export default {
  name: 'LiveChart',
  setup() {
    const dataState = inject('dataState')

    const canvasEl = ref(null)
    let chart = null

    const windowMs = computed(() => dataState.windowMs || 60000)

    const buildDatasets = () => {
      const now = Date.now()
      const minT = now - windowMs.value
      return Object.entries(dataState.series).map(([id, s]) => ({
        label: String(id),
        borderColor: s.color,
        backgroundColor: s.color,
        borderWidth: 2,
        pointRadius: 0,
        tension: 0,
        fill: false,
        data: (s.points || [])
          .filter(p => Number.isFinite(p?.x) && Number.isFinite(p?.y) && p.x >= minT)
          .map(p => ({ x: p.x, y: p.y }))
      }))
    }

    const ensureChart = () => {
      if (chart || !canvasEl.value) return
      chart = new Chart(canvasEl.value.getContext('2d'), {
        type: 'line',
        data: { datasets: [] },
        options: {
          animation: false,
          responsive: true,
          maintainAspectRatio: false,
          parsing: false,
            plugins: {
            legend: { display: false },
            title: { display: false }
          },
          scales: {
            x: {
              type: 'time',
              adapters: {},
              time: { unit: 'second' },
              ticks: { display: false }
            },
            y: {
              type: 'linear',
              // Initialize with defaults; will be overridden dynamically
              min: 0,
              max: 1
            }
          }
        }
      })
    }

    // Compute y-axis bounds from current datasets and window
    // This accounts for both actual data values and configured client ranges
    const computeYBounds = () => {
      const now = Date.now()
      const minT = now - windowMs.value
      let ymin = Number.POSITIVE_INFINITY
      let ymax = Number.NEGATIVE_INFINITY
      
      // First, consider all configured client ranges
      for (const s of Object.values(dataState.series || {})) {
        // Include client's configured range if available
        if (Number.isFinite(s.yMin) && s.yMin < ymin) ymin = s.yMin
        if (Number.isFinite(s.yMax) && s.yMax > ymax) ymax = s.yMax
        
        // Also consider actual data points within the time window
        const pts = Array.isArray(s.points) ? s.points : []
        for (let i = pts.length - 1; i >= 0; i--) {
          const p = pts[i]
          if (!p || !Number.isFinite(p.x) || !Number.isFinite(p.y)) continue
          if (p.x < minT) break // older points will be even earlier
          if (p.y < ymin) ymin = p.y
          if (p.y > ymax) ymax = p.y
        }
      }
      
      // Fallback to defaults if no valid bounds found
      if (!Number.isFinite(ymin) || !Number.isFinite(ymax)) {
        return { min: 0, max: 1 }
      }
      
      // Return exact bounds without padding
      return { min: ymin, max: ymax }
    }

    let rafId = null
    const loop = () => {
      // Updating datasets every frame keeps the chart in sync with live data
      if (!chart) ensureChart()
      if (chart) {
        const now = Date.now()
        // Clamp x-axis to a sliding window [now - windowMs, now]
        const minT = now - windowMs.value
        chart.options.scales.x.min = minT
        chart.options.scales.x.max = now

        // Rebuild datasets filtered to current window
        chart.data.datasets = buildDatasets()

        // Dynamically adjust y-axis to include all current values
        const yb = computeYBounds()
        chart.options.scales.y.min = yb.min
        chart.options.scales.y.max = yb.max

        chart.update('none')
      }
      rafId = requestAnimationFrame(loop)
    }

    onMounted(() => {
      ensureChart()
      rafId = requestAnimationFrame(loop)
    })

    onBeforeUnmount(() => {
      if (rafId) cancelAnimationFrame(rafId)
      if (chart) {
        chart.destroy()
        chart = null
      }
    })

    return { canvasEl }
  }
}
</script>

<style>
.chart-container {
  width: 100%;
  height: 420px;
}
.chart-canvas { width: 100%; height: 100%; display: block; }
</style>
