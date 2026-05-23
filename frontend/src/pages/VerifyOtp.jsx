import React, { useState } from 'react'
import axios from '../api/axios'
import { useLocation, useNavigate } from 'react-router-dom'

export default function VerifyOtp() {
  const loc = useLocation()
  const nav = useNavigate()
  const userId = loc.state?.userId
  const [code, setCode] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(false)

  const submit = async (e) => {
    e.preventDefault()
    if (loading) return

    setStatus('')
    setLoading(true)

    try {
      const res = await axios.post('/auth/verify-otp', { userId, code })
      localStorage.setItem('token', res.data.token)
      if (res.data.role) localStorage.setItem('role', res.data.role)
      if (res.data.userId) localStorage.setItem('userId', res.data.userId)
      window.dispatchEvent(new Event('authChange'))
      setStatus('OTP verified successfully. Logging you in...')
      setTimeout(() => {
        if (res.data.role === 'Admin') nav('/admin')
        else nav('/voter')
      }, 1200)
    } catch (err) {
      setStatus(err?.response?.data?.error || 'OTP verify failed')
    } finally {
      setLoading(false)
    }
  }

  const resend = async () => {
    if (loading) return

    setLoading(true)
    setStatus('')

    try {
      if (!userId) throw new Error('Unable to identify user for OTP resend.')
      await axios.post('/auth/resend-otp', { userId })
      setStatus('A new OTP has been sent to your email. Check MailHog at http://localhost:8025 for delivery.')
    } catch (err) {
      setStatus(err?.response?.data?.error || 'Resend OTP failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="row justify-content-center mt-5">
      <div className="col-md-6">
        <div className="card shadow-sm">
          <div className="card-body">
            <h3 className="card-title mb-3">Verify OTP</h3>
            <p className="text-muted mb-4">Enter the code sent to your registered email to complete sign in.</p>
            {status && (
              <div className={`alert ${status.includes('success') ? 'alert-success' : 'alert-secondary'}`}>
                {status}
              </div>
            )}
            <form onSubmit={submit}>
              <div className="mb-3">
                <label className="form-label">OTP code</label>
                <input type="text" className="form-control" value={code} onChange={e => setCode(e.target.value)} disabled={loading} />
              </div>
              <div className="d-flex gap-2 flex-column flex-sm-row">
                <button type="button" className="btn btn-outline-secondary w-100" onClick={resend} disabled={!userId || loading}>
                  {loading ? 'Sending...' : 'Resend OTP'}
                </button>
                <button type="submit" className="btn btn-primary w-100" disabled={loading}>
                  {loading ? 'Verifying...' : 'Verify'}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  )
}
