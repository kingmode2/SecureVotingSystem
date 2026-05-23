import React, {useEffect, useState} from 'react'
import axios from 'axios'

export default function VoterDashboard(){
  const [elections,setElections]=useState([])
  useEffect(()=>{
    axios.get('/api/elections').then(r=>setElections(r.data)).catch(()=>{})
  },[])

  return (
    <div>
      <h3>Voter Dashboard</h3>
      <ul>
        {elections.map(e=> (
          <li key={e.id}>{e.title} - <a href={`/elections/${e.id}`}>Details</a></li>
        ))}
      </ul>
    </div>
  )
}
