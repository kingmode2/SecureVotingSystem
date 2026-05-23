import React from 'react'
import Navbar from './Navbar'

export default function Layout({ children }) {
  return (
    <>
      <Navbar />
      <main className="container py-4">
        {children}
      </main>
      <footer className="text-center text-muted py-3 border-top">
        Secure Voting System
      </footer>
    </>
  )
}
