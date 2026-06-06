import axios from 'axios'

const resolveApiBase = () => {
  const envBase = import.meta?.env?.VITE_API_BASE
  if (envBase) return envBase

  if (typeof window !== 'undefined') {
    const { hostname, port, protocol } = window.location
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      if (port === '5173' || port === '5172' || port === '5174') {
        return 'http://localhost:5000/api'
      }
    }

    // In a static build served from Docker on localhost, use the host backend
    if (hostname === 'host.docker.internal') {
      return 'http://host.docker.internal:5000/api'
    }
  }

  return '/api'
}

const base = resolveApiBase()
const instance = axios.create({ baseURL: base })

// Track pending requests to prevent duplicates
const pendingRequests = new Map()

const getRequestKey = (config) => {
  return `${config.method}:${config.url}:${JSON.stringify(config.data || {})}`
}

// Request interceptor: deduplicate authentication requests
instance.interceptors.request.use(cfg => {
  const token = localStorage.getItem('token')
  if (token) cfg.headers['Authorization'] = `Bearer ${token}`
  
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
})

// Response interceptor: clean up pending requests
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
