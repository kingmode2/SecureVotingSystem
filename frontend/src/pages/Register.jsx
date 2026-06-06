import React, {useState} from 'react'
import axios from '../api/axios'
import { useNavigate } from 'react-router-dom'

export default function Register(){
  const [fullName,setFullName]=useState('')
  const [email,setEmail]=useState('')
  const [password,setPassword]=useState('')
  const [showPassword,setShowPassword]=useState(false)
  const [errors,setErrors]=useState([])
  const [message,setMessage]=useState('')
  const nav = useNavigate()

  const isStrongPassword = (pwd) => {
    if (!pwd || pwd.length < 8) return false
    const hasUpper = /[A-Z]/.test(pwd)
    const hasLower = /[a-z]/.test(pwd)
    const hasDigit = /[0-9]/.test(pwd)
    const hasSpecial = /[^A-Za-z0-9]/.test(pwd)
    return hasUpper && hasLower && hasDigit && hasSpecial
  }

  const submit = async (e) =>{
    e.preventDefault()
    setErrors([])

    if (!fullName || !email || !password) {
      setErrors(['All fields are required'])
      return
    }
    if (!isStrongPassword(password)){
      setErrors(['Password must be at least 8 chars and include uppercase, lowercase, digit and special char'])
      return
    }

    try{
      const res = await axios.post('/auth/register',{ fullName, email, password })
      const state = { userId: res.data.userId }
      if (res.data.otp) state['otp'] = res.data.otp
      if (res.data.userId) nav('/verify-otp', { state })
      else nav('/verify-otp', { state })
    }catch(err){
      const data = err?.response?.data
      if (data?.errors) setErrors(data.errors)
      else if (data?.error) setErrors([data.error])
      else setErrors(['Register failed'])
    }
  }

  return (
    <div className="col-md-6">
      <h3>Register</h3>
      {errors.length > 0 && (
        <div className="alert alert-danger">
          <ul className="mb-0">
            {errors.map((err,i)=>(<li key={i}>{err}</li>))}
          </ul>
        </div>
      )}
      <form onSubmit={submit}>
        <div className="mb-3">
          <label>Full name</label>
          <input className="form-control" value={fullName} onChange={e=>setFullName(e.target.value)} />
        </div>
        <div className="mb-3">
          <label>Email</label>
          <input className="form-control" value={email} onChange={e=>setEmail(e.target.value)} />
        </div>
        <div className="mb-3">
          <label>Password</label>
          <div className="input-group">
            <input
              type={showPassword ? 'text' : 'password'}
              className="form-control"
              value={password}
              onChange={e=>setPassword(e.target.value)}
            />
            <button
              type="button"
              className="btn btn-outline-secondary"
              onClick={() => setShowPassword(prev => !prev)}
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? 'Hide' : 'Show'}
            </button>
          </div>
          <small className="form-text text-muted">At least 8 characters, include uppercase, lowercase, digit and special char.</small>
        </div>
        <button className="btn btn-primary">Register</button>
      </form>
    </div>
  )
}
