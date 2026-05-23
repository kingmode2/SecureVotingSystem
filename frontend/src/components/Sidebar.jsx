import React from 'react'
import { NavLink } from 'react-router-dom'

const navItems = [
  { name: 'Dashboard', to: '/admin' },
  { name: 'Activity Logs', to: '/logs' },
  { name: 'Voting Results', to: '/results/1' }
]

export default function Sidebar(){
  return (
    <aside className="hidden h-full min-h-[calc(100vh-5rem)] w-full max-w-[280px] flex-none rounded-3xl border border-slate-200 bg-white p-6 shadow-glow dark:border-slate-800 dark:bg-slate-950 lg:block">
      <div className="mb-8">
        <div className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-2 text-xs font-semibold uppercase tracking-[0.24em] text-slate-600 dark:bg-slate-900 dark:text-slate-300">
          Admin
        </div>
        <h2 className="mt-4 text-2xl font-semibold text-slate-950 dark:text-white">Navigation</h2>
        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Quick access to election controls, voter verification, and analytics.</p>
      </div>

      <nav className="space-y-3">
        {navItems.map(item => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              `block rounded-3xl px-4 py-3 text-sm font-medium transition ${isActive ? 'bg-sky-500 text-white shadow-sm shadow-sky-500/10' : 'text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-900'}`
            }
          >
            {item.name}
          </NavLink>
        ))}
      </nav>

      <div className="mt-10 rounded-3xl bg-slate-50 p-4 text-sm text-slate-600 dark:bg-slate-900 dark:text-slate-400">
        <p className="font-semibold text-slate-900 dark:text-white">Pro tip</p>
        <p className="mt-2">Use the results page to monitor vote shares and the logs page to audit user actions.</p>
      </div>
    </aside>
  )
}
