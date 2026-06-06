import React, {useEffect, useState} from 'react'
import { useNavigate } from 'react-router-dom'
import api from '../api/axios'

const getRoleFromToken = (token) => {
  if (!token) return ''
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
    return payload?.role || ''
  } catch {
    return ''
  }
}

export default function AdminDashboard(){
  const nav = useNavigate()
  const [elections, setElections] = useState([])
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const role = getRoleFromToken(localStorage.getItem('token'))

  const fetch = () => api.get('/elections').then(r=>setElections(r.data)).catch(()=>{})
  useEffect(()=>{ fetch() },[])

  const create = async (e) =>{
    e.preventDefault()
    if (!title.trim()) return alert('Title is required.')
    if (!description.trim()) return alert('Description is required.')

    try{
      await api.post('/admin/elections', {
        title: title.trim(),
        description: description.trim(),
        startDate: new Date(),
        endDate: new Date(Date.now()+7*24*3600*1000)
      })
      setTitle('')
      setDescription('')
      fetch()
    }catch(err){
      const response = err?.response?.data
      const message = response?.error || (Array.isArray(response?.errors) ? response.errors.join(', ') : null) || err.message || 'Failed to create election.'
      alert(message)
    }
  }

  const addCandidate = async (electionId) =>{
    const name = prompt('Candidate name')
    if (!name) return
    await api.post('/admin/candidates', { electionId, name, party: '' })
    fetch()
  }

  const openClose = async (id, open) =>{
    await api.post(`/admin/elections/${id}/${open? 'open':'close'}`)
    fetch()
  }

  const logout = async () => {
    try {
      await api.post('/auth/logout')
    } catch {
      // ignore failures; clear locally anyway
    }
    localStorage.removeItem('token')
    localStorage.removeItem('role')
    localStorage.removeItem('userId')
    nav('/login', { state: { message: 'Logout successful.' } })
  }

  return (
    <div>
      <div className="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h3>Admin Dashboard</h3>
          <p className="mb-0">Welcome, {role === 'Admin' ? 'Admin' : 'User'}</p>
        </div>
        <button type="button" className="btn btn-outline-secondary" onClick={logout}>
          Logout
        </button>
      </div>
      <div className="mb-4">
        <h5>Create Election</h5>
        <form onSubmit={create} className="row g-2">
          <div className="col-md-4"><input className="form-control" value={title} onChange={e=>setTitle(e.target.value)} placeholder="Title"/></div>
          <div className="col-md-6"><input className="form-control" value={description} onChange={e=>setDescription(e.target.value)} placeholder="Description"/></div>
          <div className="col-md-2"><button className="btn btn-success">Create</button></div>
        </form>
      </div>

      <h5>Manage Elections</h5>
      <div className="row">
        {elections.map(e => (
          <div className="col-md-6" key={e.id}>
            <div className="card p-3 mb-3">
              <h6>{e.title} <small className="text-muted">{e.isActive? 'Active':'Closed'}</small></h6>
              <p className="text-muted">{e.description}</p>
              <button className="btn btn-primary me-2" onClick={()=>addCandidate(e.id)}>Add Candidate</button>
              <button className="btn btn-secondary me-2" onClick={()=>openClose(e.id, !e.isActive)}>{e.isActive? 'Close':'Open'}</button>
              <a className="btn btn-outline-info" href={`/results/${e.id}`}>View Results</a>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
