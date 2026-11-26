import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react'
import './App.css'

interface Personnel {
  officerId: string
  userId: string | null
  badgeNumber: string
  firstName: string
  lastName: string
  rankTitle: string | null
  phone: string | null
  email: string | null
  hireDate: string | null
  isActive: boolean
  createdAt: string
  currentRole: string | null
}

interface RoadClosure {
  closureId: string
  roadName: string
  description: string
  location: string
  startDate: string
  endDate: string | null
  status: string
  lastUpdated: string
}

interface CreatePersonnelForm {
  userId: string
  badgeNumber: string
  firstName: string
  lastName: string
  rankTitle: string
  phone: string
  email: string
  hireDate: string
  roleName: string
}

const emptyForm: CreatePersonnelForm = {
  userId: '',
  badgeNumber: '',
  firstName: '',
  lastName: '',
  rankTitle: '',
  phone: '',
  email: '',
  hireDate: '',
  roleName: 'Police Officer',
}

const roles = [
  'Police Officer',
  'Sergeant',
  'Police Chief',
  'Security Administrator',
]

function App() {
  const [personnel, setPersonnel] = useState<Personnel[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [form, setForm] = useState<CreatePersonnelForm>(emptyForm)
  const [token, setToken] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [roleChanges, setRoleChanges] = useState<Record<string, string>> ({})
  const [updatingOfficerId, setUpdatingOfficerId] = useState<string | null>(null)
  const [roadClosures, setRoadClosures] = useState<RoadClosure[]>([])
  const [closuresLoading, setClosuresLoading] = useState(true)
  const [closuresError, setClosuresError] = useState('')

  const fetchPersonnel = async () => {
    setLoading(true)
    setError('')

    try {
      const response = await fetch('/api/security/v1/personnel')

      if (!response.ok) {
        throw new Error(`Failed to load personnel. Status: ${response.status}`)
      }

      const data: Personnel[] = await response.json()
      setPersonnel(data)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'An unexpected error occurred.'
      )
    } finally {
      setLoading(false)
    }
  }

  const fetchRoadClosures = async () => {
  setClosuresLoading(true)
  setClosuresError('')

  try {
    const response = await fetch('/api/security/v1/road-closures')

    if (!response.ok) {
      throw new Error(
        `Failed to load road closures. Status: ${response.status}`
      )
    }

    const data: RoadClosure[] = await response.json()
    setRoadClosures(data)
  } catch (err) {
    setClosuresError(
      err instanceof Error
        ? err.message
        : 'An unexpected error occurred.'
    )
  } finally {
    setClosuresLoading(false)
  }
}

  useEffect(() => {
    void fetchPersonnel()
    void fetchRoadClosures()
  }, [])

  const handleFormChange = (
    event: ChangeEvent<HTMLInputElement | HTMLSelectElement>
  ) => {
    const { name, value } = event.target

    setForm((previous) => ({
      ...previous,
      [name]: value,
    }))
  }

  const handleCreatePersonnel = async (event: FormEvent<HTMLFormElement>
  ) => {
    event.preventDefault()
    setError('')
    setSuccess('')

    if (!token.trim()) {
      setError('Enter a Security Administrator JWT before creating personnel.')
      return
    }

    setSubmitting(true)

    try {
      const requestBody = {
        ...form,
        userId: form.userId.trim() || null,
        hireDate: form.hireDate || null,
      }

      const response = await fetch('/api/security/v1/personnel', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token.trim()}`,
        },
        body: JSON.stringify(requestBody),
      })

      if (!response.ok) {
        const message = await response.text()
        throw new Error(
          message || `Failed to create personnel. Status: ${response.status}`
        )
      }

      setSuccess('Personnel record created successfully.')
      setForm(emptyForm)
      await fetchPersonnel()
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'An unexpected error occurred.'
      )
    } finally {
      setSubmitting(false)
    }
  }

  const handleAssignRole = async (officerId: string) => {
    const roleName = roleChanges[officerId]

    if (!token.trim()) {
      setError('Enter a Security Administrator JWT before updating roles.')
      return
    }

    if (!roleName) {
      setError('Please select a role.')
      return
    }

    setError('')
    setSuccess('')
    setUpdatingOfficerId(officerId)

    try {
      const response = await fetch(`/api/security/v1/personnel/${officerId}/role`,
        {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token.trim()}`,
          },
          body: JSON.stringify({ roleName }),
        }
      )

      if (!response.ok) {
        const message = await response.text()
        throw new Error(message || `Failed to update role. Status: ${response.status}`)
      }

      setSuccess('Personnel role updated successfully.')
      await fetchPersonnel()
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'An unexpected error occurred.'
      )
    } finally {
      setUpdatingOfficerId(null)
    }
  }

  return (
    <main className="dashboard">
      {error && (
        <div className="error-message" role="alert">
          {error}
        </div>
      )}
      {success && (
        <div className="success-message" role="status">
          {success}
        </div>
      )}
      <header className="dashboard-header">
        <div>
          <p className="eyebrow">CITY MANAGEMENT SYSTEM</p>
          <h1>Security Administration</h1>
          <p className="subtitle">
            Manage police personnel and their assigned roles.
          </p>
        </div>

        <button
          className="refresh-button"
          onClick={() => void fetchPersonnel()}
          disabled={loading}
        >
          {loading ? 'Loading...' : 'Refresh'}
        </button>
      </header>

      <section className="summary">
        <div className="summary-card">
          <p>Total Personnel</p>
          <h2>{personnel.length}</h2>
        </div>

        <div className="summary-card">
          <p>Active Personnel</p>
          <h2>{personnel.filter((person) => person.isActive).length}</h2>
        </div>

        <div className="summary-card">
          <p>Inactive Personnel</p>
          <h2>{personnel.filter((person) => !person.isActive).length}</h2>
        </div>
      </section>

      <section className="personnel-section create-section">
        <div className="section-header">
          <div>
            <h2>Add Police Personnel</h2>
            <p>Create a personnel record and assign an initial role.</p>
          </div>
        </div>

        <form onSubmit={handleCreatePersonnel}>
          <div className="form-grid">
            <label>
              Badge Number *
              <input
                name="badgeNumber"
                value={form.badgeNumber}
                onChange={handleFormChange}
                maxLength={30}
                required
              />
            </label>

            <label>
              First Name *
              <input
                name="firstName"
                value={form.firstName}
                onChange={handleFormChange}
                maxLength={100}
                required
              />
            </label>

            <label>
              Last Name *
              <input
                name="lastName"
                value={form.lastName}
                onChange={handleFormChange}
                maxLength={100}
                required
              />
            </label>

            <label>
              Rank
              <input
                name="rankTitle"
                value={form.rankTitle}
                onChange={handleFormChange}
                maxLength={100}
                placeholder="e.g. Constable"
              />
            </label>

            <label>
              Phone
              <input
                name="phone"
                value={form.phone}
                onChange={handleFormChange}
                maxLength={30}
                type="tel"
              />
            </label>

            <label>
              Email
              <input
                name="email"
                value={form.email}
                onChange={handleFormChange}
                maxLength={255}
                type="email"
              />
            </label>

            <label>
              Hire Date
              <input
                name="hireDate"
                value={form.hireDate}
                onChange={handleFormChange}
                type="date"
              />
            </label>

            <label>
              Initial Role *
              <select
                name="roleName"
                value={form.roleName}
                onChange={handleFormChange}
                required
              >
                {roles.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
            </label>

            <label className="full-width">
              Registration User ID (optional)
              <input
                name="userId"
                value={form.userId}
                onChange={handleFormChange}
                maxLength={36}
                placeholder="Enter the user's ID when available"
              />
            </label>
          </div>

          <div className="token-field">
            <label>
              Security Administrator JWT *
              <input
                type="password"
                value={token}
                onChange={(event) => setToken(event.target.value)}
                placeholder="Paste your JWT access token"
                autoComplete="off"
              />
            </label>
            <p>
              Temporary testing field. Your token is kept only in this page's
              memory.
            </p>
          </div>

          {error && (
            <div className="error-message" role="alert">
              {error}
            </div>
          )}

          {success && (
            <div className="success-message" role="status">
              {success}
            </div>
          )}

          <button
            className="submit-button"
            type="submit"
            disabled={submitting}
          >
            {submitting ? 'Creating...' : 'Create Personnel'}
          </button>
        </form>
      </section>

      <section className="personnel-section">
        <div className="section-header">
          <div>
            <h2>Police Personnel</h2>
            <p>View currently registered security personnel.</p>
          </div>
        </div>

        {error && !success && (
          <div className="error-message" role="alert">
            {error}
          </div>
        )}

        {loading ? (
          <p className="status-message">Loading personnel...</p>
        ) : personnel.length === 0 ? (
          <p className="status-message">
            No personnel records were found.
          </p>
        ) : (
          <div className="table-container">
            <table>
              <thead>
                <tr>
                  <th>Badge Number</th>
                  <th>Name</th>
                  <th>Rank</th>
                  <th>Email</th>
                  <th>Role</th>
                  <th>Status</th>
                  <th>Manange Role</th>
                </tr>
              </thead>

              <tbody>
                {personnel.map((person) => (
                  <tr key={person.officerId}>
                    <td>{person.badgeNumber}</td>
                    <td>
                      {person.firstName} {person.lastName}
                    </td>
                    <td>{person.rankTitle || '—'}</td>
                    <td>{person.email || '—'}</td>
                    <td>{person.currentRole || 'Unassigned'}</td>
                    <td>
                      <span
                        className={`status-badge ${
                          person.isActive ? 'active' : 'inactive'
                        }`}
                      >
                        {person.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td>
                      <div className="role-actions">
                        <select
                          value={
                            roleChanges[person.officerId] ??
                            person.currentRole ??
                            'Police Officer'
                          }
                          onChange={(event) =>
                            setRoleChanges((previous) => ({
                              ...previous,
                              [person.officerId]: event.target.value,
                            }))
                          }
                        >
                          {roles.map((role) => (
                            <option key={role} value={role}>
                              {role}
                            </option>
                          ))}
                        </select>

                        <button
                          className="update-role-button"
                          onClick={() => void handleAssignRole(person.officerId)}
                          disabled={
                            updatingOfficerId === person.officerId ||
                            (roleChanges[person.officerId] ?? person.currentRole ?? 'Police Officer') ===
                              (person.currentRole ?? 'Police Officer')
                          }
                        >
                          {updatingOfficerId === person.officerId
                            ? 'Updating...'
                            : 'Update Role'}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      <section className="personnel-section road-closures-section">
        <div className="section-header">
          <div>
            <h2>Road Closure Notifications</h2>
            <p>View currently active road closures and their locations.</p>
          </div>

          <button
            className="refresh-button"
            onClick={() => void fetchRoadClosures()}
            disabled={closuresLoading}
          >
            {closuresLoading ? 'Loading...' : 'Refresh Closures'}
          </button>
        </div>

        {closuresError && (
          <div className="error-message" role="alert">
            {closuresError}
          </div>
        )}

        {closuresLoading ? (
          <p className="status-message">Loading road closures...</p>
        ) : roadClosures.length === 0 ? (
          <p className="status-message">
            There are currently no active road closures.
          </p>
        ) : (
          <div className="road-closure-list">
            {roadClosures.map((closure) => (
              <article className="road-closure-card" key={closure.closureId}>
                <div className="road-closure-header">
                  <h3>{closure.roadName}</h3>
                  <span className="status-badge active">
                    {closure.status}
                  </span>
                </div>

                <p>{closure.description}</p>

                <p>
                  <strong>Location:</strong> {closure.location}
                </p>

                <p>
                  <strong>Started:</strong>{' '}
                  {new Date(closure.startDate).toLocaleString()}
                </p>

                <p>
                  <strong>Expected End:</strong>{' '}
                  {closure.endDate
                    ? new Date(closure.endDate).toLocaleString()
                    : 'Unknown'}
                </p>

                <p className="closure-updated">
                  Last updated: {new Date(closure.lastUpdated).toLocaleString()}
                </p>
              </article>
            ))}
          </div>
        )}
      </section>
    </main>
  )
}

export default App