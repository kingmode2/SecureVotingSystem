import React, { useEffect, useRef, useState } from 'react'
import { Chart } from 'chart.js/auto'
import api from '../api/axios'
import { useParams } from 'react-router-dom'

export default function Results() {
  const { id } = useParams()
  const canvasRef = useRef(null)
  const chartRef = useRef(null)
  const [results, setResults] = useState([])

  useEffect(() => {
    api.get(`/admin/results/${id}`).then(r => {
      setResults(r.data)
      const labels = r.data.map(x => x.candidate.name)
      const data = r.data.map(x => x.votes)
      if (chartRef.current) chartRef.current.destroy()
      const ctx = canvasRef.current
      if (!ctx) return
      chartRef.current = new Chart(ctx, {
        type: 'bar',
        data: {
          labels,
          datasets: [{
            label: 'Votes',
            data,
            backgroundColor: ['#0d6efd', '#198754', '#fd7e14', '#dc3545'],
            borderRadius: 6,
            maxBarThickness: 40
          }]
        },
        options: {
          responsive: true,
          plugins: { legend: { display: false } },
          scales: {
            x: { grid: { display: false } },
            y: { beginAtZero: true }
          }
        }
      })
    }).catch(() => {})
  }, [id])

  return (
    <div className="row justify-content-center mt-5">
      <div className="col-lg-10">
        <div className="card shadow-sm mb-4">
          <div className="card-body">
            <h4 className="card-title">Election Results</h4>
            <p className="text-muted mb-3">Review voting totals for the selected election.</p>
            <p className="mb-0"><strong>Election ID:</strong> {id}</p>
          </div>
        </div>

        <div className="card shadow-sm mb-4">
          <div className="card-body">
            <h5 className="card-title">Vote distribution</h5>
            <canvas ref={canvasRef} />
          </div>
        </div>

        <div className="row g-3">
          {results.map(r => (
            <div className="col-md-6" key={r.candidate.id}>
              <div className="card">
                <div className="card-body">
                  <h5 className="card-title">{r.candidate.name}</h5>
                  <p className="card-text text-muted mb-2">Party: {r.candidate.party || 'N/A'}</p>
                  <p className="mb-0"><strong>Votes:</strong> {r.votes}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
