import React from 'react'
import { Routes, Route, Link, Navigate } from 'react-router-dom'
import Login from './pages/Login'
import Register from './pages/Register'
import VerifyOtp from './pages/VerifyOtp'
import VoterDashboard from './pages/VoterDashboard'
import AdminDashboard from './pages/AdminDashboard'
import ElectionDetails from './pages/ElectionDetails'
import Results from './pages/Results'
import ActivityLogs from './pages/ActivityLogs'
import Layout from './components/Layout'
import ProtectedRoute from './components/ProtectedRoute'

const getDashboardPath = () => {
  const token = localStorage.getItem('token')
  if (!token) return null

  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
    return payload?.role === 'Admin' ? '/admin' : '/voter'
  } catch {
    return null
  }
}

function App() {
  const dashboardPath = getDashboardPath()

  return (
    <Layout>
      <Routes>
        <Route
          path="/"
          element={
            dashboardPath ? (
              <Navigate to={dashboardPath} replace />
            ) : (
              <div className="text-center py-5">
                <h1 className="mb-4">Secure Voting System</h1>
                <p className="lead mb-4">Register, login, and participate in secure online elections.</p>
                <Link className="btn btn-primary me-2" to="/login">Login</Link>
                <Link className="btn btn-outline-secondary" to="/register">Register</Link>
              </div>
            )
          }
        />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/verify-otp" element={<VerifyOtp />} />
        <Route path="/voter" element={<ProtectedRoute><VoterDashboard /></ProtectedRoute>} />
        <Route path="/elections/:id" element={<ProtectedRoute><ElectionDetails /></ProtectedRoute>} />
        <Route path="/results/:id" element={<ProtectedRoute role="Admin"><Results /></ProtectedRoute>} />
        <Route path="/admin" element={<ProtectedRoute role="Admin"><AdminDashboard /></ProtectedRoute>} />
        <Route path="/logs" element={<ProtectedRoute role="Admin"><ActivityLogs /></ProtectedRoute>} />
      </Routes>
    </Layout>
  )
}

export default App
