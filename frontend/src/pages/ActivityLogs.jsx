import React, { useEffect, useState } from 'react'
import api from '../api/axios'

export default function ActivityLogs() {
  const [logs, setLogs] = useState([])

  useEffect(() => {
    api.get('/logs').then(r => setLogs(r.data)).catch(() => {})
  }, [])

  return (
    <div className="row justify-content-center mt-5">
      <div className="col-lg-8">
        <div className="card shadow-sm">
          <div className="card-body">
            <h4 className="card-title mb-3">Activity Logs</h4>
            <p className="text-muted mb-4">Review system events, voter actions, and admin operations.</p>
            {logs.length === 0 ? (
              <div className="alert alert-info">No logs available yet.</div>
            ) : (
              logs.map(l => (
                <div key={l.id} className="border-bottom py-3">
                  <p className="mb-1"><strong>{l.action}</strong></p>
                  <small className="text-muted">{new Date(l.createdAt).toLocaleString()} • User #{l.userId}</small>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
