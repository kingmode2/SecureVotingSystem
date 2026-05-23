import React, { useState, useEffect } from 'react'
import axios from '../api/axios'
import { useNavigate, useLocation, Link } from 'react-router-dom'

export default function Login() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const nav = useNavigate()
  const location = useLocation()

  useEffect(() => {
    const existingToken = localStorage.getItem('token')
    const existingRole = localStorage.getItem('role')
    if (existingToken) {
      nav(existingRole === 'Admin' ? '/admin' : '/voter', { replace: true })
    }
  }, [nav])

  const submit = async (e) => {
    e.preventDefault()
    if (loading) return

    setError('')
    localStorage.removeItem('token')
    localStorage.removeItem('role')
    localStorage.removeItem('userId')
    setLoading(true)

    try {
      const res = await axios.post('/auth/login', { email, password })
      nav('/verify-otp', { state: { userId: res.data.userId } })
    } catch (err) {
      const errorMsg = err?.response?.data?.error || 'Login failed'
      setError(errorMsg)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (location.state?.message) {
      setMessage(location.state.message)
      window.history.replaceState({}, document.title)
    }
  }, [location.state])

  return (
    <div className="row justify-content-center mt-5">
      <div className="col-md-6">
        <div className="card shadow-sm">
          <div className="card-body">
            <h3 className="card-title mb-3">Login</h3>
            <p className="card-text text-muted mb-4">Enter your credentials to sign in.</p>
            {message && <div className="alert alert-success">{message}</div>}
            {error && <div className="alert alert-danger">{error}</div>}
            <form onSubmit={submit}>
              <div className="mb-3">
                <label className="form-label">Email</label>
                <input type="email" className="form-control" value={email} onChange={e => setEmail(e.target.value)} disabled={loading} />
              </div>
              <div className="mb-3">
                <label className="form-label">Password</label>
                <input type="password" className="form-control" value={password} onChange={e => setPassword(e.target.value)} disabled={loading} />
              </div>
              <button type="submit" className="btn btn-primary w-100" disabled={loading}>
                {loading ? 'Logging in...' : 'Login'}
              </button>
            </form>
            <p className="mt-3 text-center">
              Don&apos;t have an account? <Link to="/register">Register</Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
