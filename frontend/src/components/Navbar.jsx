import React, { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import axios from '../api/axios'

const getEmailFromToken = (token) => {
  if (!token) return ''
  try {
    const payload = token.split('.')[1]
    const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')))
    return decoded?.email || ''
  } catch {
    return ''
  }
}

export default function Navbar() {
  const nav = useNavigate()
  const [token, setToken] = useState(null)
  const [role, setRole] = useState('Voter')
  const [email, setEmail] = useState('')

  useEffect(() => {
    const refreshAuthState = () => {
      const updatedToken = localStorage.getItem('token')
      setToken(updatedToken)
      setRole(localStorage.getItem('role') || 'Voter')
      setEmail(getEmailFromToken(updatedToken))
    }

    refreshAuthState()

    window.addEventListener('storage', refreshAuthState)
    window.addEventListener('authChange', refreshAuthState)
    return () => {
      window.removeEventListener('storage', refreshAuthState)
      window.removeEventListener('authChange', refreshAuthState)
    }
  }, [])

  const logout = async () => {
    try {
      if (token) await axios.post('/auth/logout')
    } catch {
      // ignore failures; token will still be removed locally
    }

    localStorage.removeItem('token')
    localStorage.removeItem('role')
    localStorage.removeItem('userId')
    setToken(null)
    setRole('Voter')
    setEmail('')
    nav('/login', { state: { message: 'Logout successful.' } })
  }

  const homePath = token ? (role === 'Admin' ? '/admin' : '/voter') : '/'

  return (
    <nav className="navbar navbar-expand-lg navbar-light bg-light shadow-sm">
      <div className="container-fluid">
        <Link className="navbar-brand" to={homePath}>Secure Voting</Link>
        <div className="collapse navbar-collapse show">
          <ul className="navbar-nav me-auto mb-2 mb-lg-0">
            <li className="nav-item">
              <Link className="nav-link" to={homePath}>Home</Link>
            </li>
            {token && role === 'Admin' && (
              <>
                <li className="nav-item">
                  <Link className="nav-link" to="/admin">Admin</Link>
                </li>
                <li className="nav-item">
                  <Link className="nav-link" to="/logs">Activity Logs</Link>
                </li>
              </>
            )}
          </ul>
          <div className="d-flex align-items-center gap-2">
            {token ? (
              <>
                {email && <span className="navbar-text me-2">Welcome, {email}</span>}
                <span className="badge bg-secondary text-white">{role}</span>
                <Link className="btn btn-outline-primary" to={role === 'Admin' ? '/admin' : '/voter'}>
                  Dashboard
                </Link>
                <button type="button" className="btn btn-outline-secondary" onClick={logout}>
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link className="btn btn-outline-primary" to="/login">Login</Link>
                <Link className="btn btn-primary" to="/register">Register</Link>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  )
}
