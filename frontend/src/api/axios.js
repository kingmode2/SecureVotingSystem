import axios from 'axios'

const resolveApiBase = () => {
  // 1. Use environment variable if set (Render)
  const envBase = import.meta?.env?.VITE_API_BASE
  if (envBase) {
    console.log('🔍 Using VITE_API_BASE:', envBase)
    if (envBase.endsWith('/api')) {
      return envBase
    }
    return `${envBase}/api`
  }

  // 2. Check if we're running on Render (production)
  //    Render sets this environment variable automatically
  if (import.meta?.env?.PROD) {
    console.log('🔍 Running in production (Render) – using backend URL')
    return 'https://securevotingsystem.onrender.com/api'
  }

  // 3. Local development
  if (typeof window !== 'undefined') {
    const { hostname, port } = window.location
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      if (['5173', '5172', '5174'].includes(port)) {
        return 'http://localhost:5000/api'
      }
    }

    // Docker local
    if (hostname === 'host.docker.internal') {
      return 'http://host.docker.internal:5000/api'
    }
  }

  // 4. Fallback – relative path (local development)
  console.warn('⚠️ No API base set – using relative /api')
  return '/api'
}

// Get the base URL
const base = resolveApiBase()
console.log('✅ Axios base URL:', base)

const instance = axios.create({
  baseURL: base,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Track pending requests to prevent duplicates
const pendingRequests = new Map()

const getRequestKey = (config) => {
  return `${config.method}:${config.url}:${JSON.stringify(config.data || {})}`
}

// Request interceptor: add token and deduplicate auth requests
instance.interceptors.request.use(cfg => {
  const token = localStorage.getItem('token')
  if (token) {
    cfg.headers['Authorization'] = `Bearer ${token}`
  }
  
  // For auth endpoints, prevent duplicate requests
  if (cfg.url?.includes('/auth/')) {
    const requestKey = getRequestKey(cfg)
    
    if (pendingRequests.has(requestKey)) {
      // Return the pending promise instead of making a duplicate request
      return pendingRequests.get(requestKey).promise.then(response => cfg)
    }
    
    // Store this request promise
    let resolveFn
    const promise = new Promise(resolve => {
      resolveFn = resolve
    })
    
    pendingRequests.set(requestKey, { promise, resolve: resolveFn })
  }
  
  return cfg
}, (error) => {
  return Promise.reject(error)
})

// Response interceptor: clean up pending requests and handle 401
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
    
    // Handle 401 Unauthorized
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