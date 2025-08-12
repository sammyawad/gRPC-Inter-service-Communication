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
            legend: { display: true },
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
              suggestedMin: 0,
              suggestedMax: 1
            }
          }
        }
      })
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
