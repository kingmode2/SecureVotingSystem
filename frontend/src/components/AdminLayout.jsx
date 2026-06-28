import React from 'react'
import { useLocation } from 'react-router-dom'
import Sidebar from './Sidebar'

export default function AdminLayout({ children }){
  const location = useLocation()
  return (
    <div className="space-y-6 lg:space-y-0">
      <div className="rounded-3xl bg-slate-50 p-6 shadow-glow ring-1 ring-slate-200 dark:bg-slate-900 dark:ring-slate-800">
        <p className="text-sm uppercase tracking-[0.24em] text-sky-500">Admin</p>
        <h1 className="mt-2 text-3xl font-semibold text-slate-950 dark:text-white">{location.pathname === '/admin' ? 'Admin Dashboard' : location.pathname.startsWith('/logs') ? 'Activity Logs' : 'Voting Results'}</h1>
        <p className="mt-3 text-slate-600 dark:text-slate-400">Manage elections, voters, and analytics from one secure admin console.</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
        <Sidebar />
        <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-glow dark:border-slate-800 dark:bg-slate-950">
          {children}
        </div>
      </div>
    </div>
  )
}
