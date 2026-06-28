import React, { useEffect, useState } from 'react'
import api from '../api/axios'
import { useParams } from 'react-router-dom'

export default function ElectionDetails() {
  const { id } = useParams()
  const [election, setElection] = useState(null)
  const [candidates, setCandidates] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.get(`/elections/${id}`)
      .then(r => {
        setElection(r.data.election)
        setCandidates(r.data.candidates)
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [id])

  const getStatus = (election) => {
    if (!election?.isActive) return 'Inactive'
    const now = new Date()
    const start = new Date(election.startDate)
    const end = new Date(election.endDate)
    if (now < start) return 'Upcoming'
    if (now > end) return 'Closed'
    return 'Active'
  }

  const cast = async (candidateId) => {
    try {
      await api.post('/votes', { electionId: parseInt(id), candidateId: parseInt(candidateId) })
      alert('Vote cast successfully')
    } catch (err) {
      alert(err?.response?.data?.error || err?.message || 'Failed to cast vote')
    }
  }

  if (loading) {
    return (
      <div className="text-center mt-5">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    )
  }

  if (!election) {
    return <div className="alert alert-danger mt-5">Election not found</div>
  }

  return (
    <div className="row justify-content-center mt-5">
      <div className="col-lg-10">
        <div className="card shadow-sm mb-4">
          <div className="card-body">
            <h4 className="card-title">{election.title}</h4>
            <p className="text-muted">{election.description}</p>
            <p>
              <strong>Status:</strong> {getStatus(election)}
            </p>
            <p className="mb-1"><strong>Starts:</strong> {new Date(election.startDate).toLocaleString()}</p>
            <p><strong>Ends:</strong> {new Date(election.endDate).toLocaleString()}</p>
          </div>
        </div>
        <div className="row g-3">
          {candidates.map(c => (
            <div className="col-md-6" key={c.id}>
              <div className="card shadow-sm h-100">
                <div className="card-body d-flex flex-column">
                  <h5 className="card-title">{c.name}</h5>
                  <p className="card-text text-muted mb-4">Party: {c.party || 'Independent'}</p>
                  <button
                    className="btn btn-primary mt-auto"
                    onClick={() => cast(c.id)}
                    disabled={getStatus(election) !== 'Active'}
                  >
                    Vote for {c.name}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
