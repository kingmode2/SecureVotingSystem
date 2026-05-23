import axios from 'axios'

const base = (import.meta?.env?.VITE_API_BASE) || '/api'
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
