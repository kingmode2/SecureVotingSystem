import React, { useState, useEffect } from 'react'
import axios from '../api/axios'
import { useNavigate } from 'react-router-dom'

export default function Register() {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [errors, setErrors] = useState([])
  const [loading, setLoading] = useState(false)
  const nav = useNavigate()

  useEffect(() => {
    const existingToken = localStorage.getItem('token')
    const existingRole = localStorage.getItem('role')
    if (existingToken) {
      nav(existingRole === 'Admin' ? '/admin' : '/voter', { replace: true })
    }
  }, [nav])

  const isStrongPassword = (pwd) => {
    if (!pwd || pwd.length < 8) return false
    const hasUpper = /[A-Z]/.test(pwd)
    const hasLower = /[a-z]/.test(pwd)
    const hasDigit = /[0-9]/.test(pwd)
    const hasSpecial = /[^A-Za-z0-9]/.test(pwd)
    return hasUpper && hasLower && hasDigit && hasSpecial
  }

  const submit = async (e) => {
    e.preventDefault()
    if (loading) return

    setErrors([])
    localStorage.removeItem('token')
    localStorage.removeItem('role')
    localStorage.removeItem('userId')

    if (!fullName || !email || !password) {
      setErrors(['All fields are required'])
      return
    }
    if (!isStrongPassword(password)) {
      setErrors(['Password must be at least 8 chars and include uppercase, lowercase, digit and special char'])
      return
    }

    setLoading(true)

    try {
      await axios.post('/auth/register', { fullName, email, password })
      nav('/login', { state: { message: 'Registration successful. Please log in to continue.' } })
    } catch (err) {
      const data = err?.response?.data
      if (data?.errors) setErrors(data.errors)
      else if (data?.error) setErrors([data.error])
      else setErrors(['Register failed'])
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="row justify-content-center mt-5">
      <div className="col-md-6">
        <div className="card shadow-sm">
          <div className="card-body">
            <h3 className="card-title mb-3">Register</h3>
            <p className="text-muted mb-4">Create your account to participate in secure elections.</p>
            {errors.length > 0 && (
              <div className="alert alert-danger">
                <ul className="mb-0">
                  {errors.map((err, i) => (
                    <li key={i}>{err}</li>
                  ))}
                </ul>
              </div>
            )}
            <form onSubmit={submit}>
              <div className="mb-3">
                <label className="form-label">Full name</label>
                <input type="text" className="form-control" value={fullName} onChange={e => setFullName(e.target.value)} disabled={loading} />
              </div>
              <div className="mb-3">
                <label className="form-label">Email</label>
                <input type="email" className="form-control" value={email} onChange={e => setEmail(e.target.value)} disabled={loading} />
              </div>
              <div className="mb-3">
                <label className="form-label">Password</label>
                <input type={showPassword ? 'text' : 'password'} className="form-control" value={password} onChange={e => setPassword(e.target.value)} disabled={loading} />
              </div>
              <div className="form-check mb-3">
                <input className="form-check-input" type="checkbox" id="showPassword" checked={showPassword} onChange={e => setShowPassword(e.target.checked)} disabled={loading} />
                <label className="form-check-label" htmlFor="showPassword">Show password</label>
              </div>
              <button type="submit" className="btn btn-primary w-100" disabled={loading}>
                {loading ? 'Registering...' : 'Register'}
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  )
}
