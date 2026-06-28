import axios from 'axios'

// ============================================================
// ✅ RELIABLE API BASE DETECTION – based on hostname
// ============================================================
const resolveApiBase = () => {
  if (typeof window !== 'undefined') {
    const { hostname, port } = window.location

    // Local development
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      if (['5173', '5172', '5174'].includes(port)) {
        return 'http://localhost:5000/api'
      }
      return 'http://localhost:5000/api'
    }

    // Docker local
    if (hostname === 'host.docker.internal') {
      return 'http://host.docker.internal:5000/api'
    }

    // Any other host (Render) – use the deployed backend
    console.log('🔍 Detected production host:', hostname)
    return 'https://securevotingsystem.onrender.com/api'
  }

  // Fallback (should never happen)
  return 'https://securevotingsystem.onrender.com/api'
}

const base = resolveApiBase()
console.log('✅ Axios base URL:', base)

const instance = axios.create({
  baseURL: base,
  headers: {
    'Content-Type': 'application/json'
  }
})

// ============================================================
// Your existing interceptors (unchanged)
// ============================================================
const pendingRequests = new Map()

const getRequestKey = (config) => {
  return `${config.method}:${config.url}:${JSON.stringify(config.data || {})}`
}

instance.interceptors.request.use(cfg => {
  const token = localStorage.getItem('token')
  if (token) {
    cfg.headers['Authorization'] = `Bearer ${token}`
  }
  
  if (cfg.url?.includes('/auth/')) {
    const requestKey = getRequestKey(cfg)
    if (pendingRequests.has(requestKey)) {
      return pendingRequests.get(requestKey).promise.then(response => cfg)
    }
    let resolveFn
    const promise = new Promise(resolve => { resolveFn = resolve })
    pendingRequests.set(requestKey, { promise, resolve: resolveFn })
  }
  return cfg
}, (error) => Promise.reject(error))

instance.interceptors.response.use(
  (response) => {
    const requestKey = getRequestKey(response.config)
    if (pendingRequests.has(requestKey)) {
      const { resolve } = pendingRequests.get(requestKey)
      resolve(response)
      pendingRequests.delete(requestKey)
    }
    return response
  },
  (error) => {
    const requestKey = getRequestKey(error.config)
    if (pendingRequests.has(requestKey)) {
      pendingRequests.delete(requestKey)
    }
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('role')
      localStorage.removeItem('userId')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default instance