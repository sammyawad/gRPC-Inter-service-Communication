<!-- filepath: c:\Users\sammy\Documents\gRPC-Inter-service-Communication\frontend\src\App.vue -->
<template>
  <div id="app">
    <div class="chart-page">
      <h1>Data</h1>
      <div class="status">
        <span :class="{ online: dataState.connected }">
          {{ dataState.connected ? 'Connected' : 'Disconnected' }}
        </span>
        <span class="sep">•</span>
        <span>
          {{ dataState.totalPoints > 0 ? 'Receiving data' : 'No data yet' }}
        </span>
      </div>
      <!-- Single live chart, starts blank until data arrives -->
      <LiveChart />
    </div>
  </div>
</template>
<script>
import { inject, onMounted, onBeforeUnmount } from 'vue'
import { DataService } from './services/DataService.js'
import LiveChart from './components/LiveChart.vue'

export default {
  name: 'App',
  components: { LiveChart },
  setup() {
    const dataState = inject('dataState')
    const dataService = new DataService(dataState)

    onMounted(async () => {
      try {
        await dataService.connect()
      } catch (err) {
        console.error('Failed to connect to data stream:', err)
      }
    })

    onBeforeUnmount(async () => {
      await dataService.disconnect()
    })

    return { dataState }
  }
}
</script>

<style>
.chart-page {
  max-width: 900px;
  margin: 0 auto;
  padding: 20px;
}
.status {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.95rem;
  color: #555;
  margin: 6px 0 12px;
}
.status .online {
  color: #0a7d27;
  font-weight: 600;
}
.status .sep {
  opacity: 0.6;
}
</style>
