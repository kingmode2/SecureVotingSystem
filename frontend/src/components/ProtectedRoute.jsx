import React from 'react'
import { Navigate } from 'react-router-dom'

const parseJwt = (token) => {
  try {
    const base64Url = token.split('.')[1]
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(atob(base64).split('').map(c => '%'+('00'+c.charCodeAt(0).toString(16)).slice(-2)).join(''))
    return JSON.parse(jsonPayload)
  } catch {
    return null
  }
}

export default function ProtectedRoute({ children, role }){
  const token = localStorage.getItem('token')
  const decodedToken = token ? parseJwt(token) : null
  const exp = decodedToken?.exp
  const now = Math.floor(Date.now() / 1000)
  if (!token || !decodedToken || !exp || now >= exp) {
    localStorage.removeItem('token')
    localStorage.removeItem('role')
    localStorage.removeItem('userId')
    return <Navigate to="/login" replace />
  }

  const userRole = decodedToken?.role || localStorage.getItem('role') || 'Voter'
  if (role && userRole !== role) return <Navigate to="/" replace />
  return children
}
