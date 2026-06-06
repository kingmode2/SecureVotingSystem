import React, {useEffect, useState} from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axios'

const getNameFromToken = (token) => {
  if (!token) return ''
  try {
    const payload = token.split('.')[1]
    const decoded = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')))
    return decoded?.name || decoded?.email || ''
  } catch {
    return ''
  }
}

export default function VoterDashboard(){
  const [elections,setElections]=useState([])
  const [name, setName] = useState('')
  const nav = useNavigate()

  useEffect(()=>{
    api.get('/elections').then(r=>setElections(r.data)).catch(()=>{})
    const token = localStorage.getItem('token')
    setName(getNameFromToken(token))
  },[])

  const logout = async () => {
    try { await api.post('/auth/logout') } catch {}
    localStorage.removeItem('token')
    localStorage.removeItem('role')
    localStorage.removeItem('userId')
    window.dispatchEvent(new Event('authChange'))
    nav('/login', { state: { message: 'Logout successful.' } })
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h3>Voter Dashboard</h3>
        <div>
          {name && <span className="me-3">Welcome, {name}</span>}
          <button className="btn btn-outline-secondary" onClick={logout}>Logout</button>
        </div>
      </div>
      <ul>
        {elections.map(e=> (
          <li key={e.id}>{e.title} - <a href={`/elections/${e.id}`}>Details</a></li>
        ))}
      </ul>
    </div>
  )
}
