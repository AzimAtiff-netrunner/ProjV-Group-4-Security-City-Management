import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import './VehicleDashboard.css'

interface Address {
  streetNumber?: string | null
  streetName?: string | null
  city?: string | null
  postalCode?: string | null
}

interface VehicleLocation {
  address?: Address | null
  latitude?: number | null
  longitude?: number | null
  recordedAt?: string | null
}

interface Vehicle {
  id: string
  callsign: string
  licensePlate: string
  status: string
  currentLocation: VehicleLocation | null
}

interface LatestTelemetry {
  vehicleId: string
  address?: Address | null
  latitude?: number | null
  longitude?: number | null
  speedKmh?: number | null
  headingDegrees?: number | null
  recordedAt: string
  receivedAt?: string
}

interface VehicleDashboardProps {
  token: string
}

function formatAddress(location: VehicleLocation | LatestTelemetry | null | undefined) {
  if (!location) return 'No location reported'

  const address = location.address
  if (address) {
    const street = [address.streetNumber, address.streetName]
      .filter(Boolean)
      .join(' ')
    return [street, address.city, address.postalCode].filter(Boolean).join(', ') || 'Address unavailable'
  }

  if (location.latitude != null && location.longitude != null) {
    return `${location.latitude}, ${location.longitude}`
  }

  return 'No location reported'
}

function formatTime(value?: string | null) {
  if (!value) return 'Time unavailable'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}

function statusClass(status: string) {
  return status.toLowerCase().replace(/[^a-z]/g, '')
}

export default function VehicleDashboard({ token }: VehicleDashboardProps) {
  const [vehicles, setVehicles] = useState<Vehicle[]>([])
  const [selectedVehicle, setSelectedVehicle] = useState<Vehicle | null>(null)
  const [latestTelemetry, setLatestTelemetry] = useState<LatestTelemetry | null>(null)
  const [loading, setLoading] = useState(false)
  const [detailLoading, setDetailLoading] = useState(false)
  const [error, setError] = useState('')
  const [totalCount, setTotalCount] = useState(0)
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [creating, setCreating] = useState(false)
  const [createSuccess, setCreateSuccess] = useState('')
  const [createError, setCreateError] = useState('')
  const [newCallsign, setNewCallsign] = useState('')
  const [newLicensePlate, setNewLicensePlate] = useState('')
  const [newStatus, setNewStatus] = useState('Available')
  const [editing, setEditing] = useState(false)
  const [savingEdit, setSavingEdit] = useState(false)
  const [editError, setEditError] = useState('')
  const [editCallsign, setEditCallsign] = useState('')
  const [editPlate, setEditPlate] = useState('')
  const [editStatus, setEditStatus] = useState('Available')
  const [locationForm, setLocationForm] = useState({ streetNumber: '', streetName: '', postalCode: '', speedKmh: '', headingDegrees: '' })
  const [savingLocation, setSavingLocation] = useState(false)
  const [deleting, setDeleting] = useState(false)

  const fetchVehicles = useCallback(async () => {
    if (!token.trim()) {
      setVehicles([])
      setError('Enter an officer JWT above to load the protected vehicle dashboard.')
      return
    }

    setLoading(true)
    setError('')

    try {
      const response = await fetch('/api/security/v1/vehicles?page=1&pageSize=100', {
        headers: { Authorization: `Bearer ${token.trim()}` },
      })

      if (!response.ok) {
        const message = await response.text()
        throw new Error(message || `Could not load vehicles (${response.status}).`)
      }

      const result = await response.json()
      // The API returns a paged result with its vehicles inside the items array.
      const items: Vehicle[] = Array.isArray(result) ? result : result.items ?? []
      setVehicles(items)
      setTotalCount(result.totalCount ?? items.length)
      setSelectedVehicle((current) => current ? items.find((vehicle) => vehicle.id === current.id) ?? null : null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unexpected error occurred.')
    } finally {
      setLoading(false)
    }
  }, [token])

  useEffect(() => {
    void Promise.resolve().then(fetchVehicles)
  }, [fetchVehicles])

  const fetchLatestTelemetry = async (vehicle: Vehicle) => {
    setSelectedVehicle(vehicle)
    setLatestTelemetry(null)
    setDetailLoading(true)
    setError('')

    try {
      const response = await fetch(
        `/api/security/v1/vehicles/${vehicle.id}/telemetry/latest`,
        { headers: { Authorization: `Bearer ${token.trim()}` } },
      )

      if (response.status === 404) {
        setLatestTelemetry(null)
        return
      }
      if (!response.ok) {
        const message = await response.text()
        throw new Error(message || `Could not load latest telemetry (${response.status}).`)
      }

      setLatestTelemetry(await response.json())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load vehicle details.')
    } finally {
      setDetailLoading(false)
    }
  }

  const handleCreateVehicle = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setCreateError('')
    setCreateSuccess('')

    if (!token.trim()) {
      setCreateError('Enter a Security Administrator JWT before adding a vehicle.')
      return
    }

    setCreating(true)
    try {
      const response = await fetch('/api/security/v1/vehicles', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token.trim()}`,
        },
        body: JSON.stringify({
          callsign: newCallsign.trim(),
          licensePlate: newLicensePlate.trim(),
          status: newStatus,
        }),
      })

      if (!response.ok) {
        const responseText = await response.text()
        let message = responseText || `Could not create vehicle (${response.status}).`
        try {
          const apiError = JSON.parse(responseText) as { detail?: string; message?: string; title?: string }
          message = apiError.detail || apiError.message || apiError.title || message
        } catch {
          // Keep the original response text when the API response is not JSON.
        }
        if (response.status === 403) {
          message = 'This action requires a Security Administrator token.'
        } else if (response.status === 409) {
          message = 'That callsign or license plate is already in use. Please choose unique values.'
        }
        throw new Error(message)
      }

      const createdVehicle = await response.json() as Vehicle
      setNewCallsign('')
      setNewLicensePlate('')
      setNewStatus('Available')
      setShowCreateForm(false)
      setCreateSuccess(`${createdVehicle.callsign || 'Vehicle'} was added successfully.`)
      await fetchVehicles()
      setSelectedVehicle(createdVehicle)
      setLatestTelemetry(null)
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'An unexpected error occurred while creating the vehicle.')
    } finally {
      setCreating(false)
    }
  }

  const startEditing = (vehicle: Vehicle) => {
    setEditCallsign(vehicle.callsign)
    setEditPlate(vehicle.licensePlate)
    setEditStatus(vehicle.status)
    setEditError('')
    setEditing(true)
  }

  const handleUpdateVehicle = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selectedVehicle) return
    setSavingEdit(true); setEditError('')
    try {
      const response = await fetch(`/api/security/v1/vehicles/${selectedVehicle.id}`, {
        method: 'PUT', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token.trim()}` },
        body: JSON.stringify({ callsign: editCallsign.trim(), licensePlate: editPlate.trim(), status: editStatus }),
      })
      if (!response.ok) throw new Error(response.status === 403 ? 'Updating vehicles requires a Security Administrator token.' : await response.text() || `Could not update vehicle (${response.status}).`)
      const updated = await response.json() as Vehicle
      setEditing(false); setCreateSuccess(`${updated.callsign} was updated successfully.`)
      await fetchVehicles(); setSelectedVehicle(updated)
    } catch (err) { setEditError(err instanceof Error ? err.message : 'Could not update vehicle.') }
    finally { setSavingEdit(false) }
  }

  const handleSaveLocation = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!selectedVehicle) return
    setSavingLocation(true); setEditError('')
    try {
      const response = await fetch(`/api/security/v1/vehicles/${selectedVehicle.id}/telemetry`, {
        method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token.trim()}` },
        body: JSON.stringify({
          address: { streetNumber: locationForm.streetNumber.trim(), streetName: locationForm.streetName.trim(), city: 'Waterloo', postalCode: locationForm.postalCode.trim().toUpperCase() },
          speedKmh: locationForm.speedKmh === '' ? null : Number(locationForm.speedKmh),
          headingDegrees: locationForm.headingDegrees === '' ? null : Number(locationForm.headingDegrees),
          recordedAt: new Date().toISOString(),
        }),
      })
      if (!response.ok) throw new Error(response.status === 403 ? 'Recording telemetry requires a token with the telemetry-writer permission.' : await response.text() || `Could not save telemetry (${response.status}).` )
      const telemetry = await response.json() as LatestTelemetry
      setLatestTelemetry(telemetry); setLocationForm({ streetNumber: '', streetName: '', postalCode: '', speedKmh: '', headingDegrees: '' })
      await fetchVehicles()
    } catch (err) { setEditError(err instanceof Error ? err.message : 'Could not save location.') }
    finally { setSavingLocation(false) }
  }

  const handleDeleteVehicle = async () => {
    if (!selectedVehicle || !window.confirm(`Remove ${selectedVehicle.callsign} from the active fleet?`)) return
    setDeleting(true); setEditError('')
    try {
      const response = await fetch(`/api/security/v1/vehicles/${selectedVehicle.id}`, { method: 'DELETE', headers: { Authorization: `Bearer ${token.trim()}` } })
      if (!response.ok) throw new Error(response.status === 403 ? 'Deleting vehicles requires a Security Administrator token.' : await response.text() || `Could not delete vehicle (${response.status}).`)
      setSelectedVehicle(null); setLatestTelemetry(null); setEditing(false)
      setCreateSuccess('Vehicle was removed from the active fleet.')
      await fetchVehicles()
    } catch (err) { setEditError(err instanceof Error ? err.message : 'Could not delete vehicle.') }
    finally { setDeleting(false) }
  }

  const counts = useMemo(() => ({
    onPatrol: vehicles.filter((vehicle) => vehicle.status.toLowerCase() === 'onpatrol').length,
    dispatched: vehicles.filter((vehicle) => vehicle.status.toLowerCase() === 'dispatched').length,
    unavailable: vehicles.filter((vehicle) => vehicle.status.toLowerCase() === 'outofservice').length,
  }), [vehicles])

  return (
    <section className="vehicle-dashboard">
      <div className="vehicle-heading-row">
        <div>
          <p className="eyebrow">FLEET OPERATIONS</p>
          <h2>Police Vehicle Dashboard</h2>
          <p className="vehicle-subtitle">View police vehicles and their latest reported locations.</p>
        </div>
        <div className="vehicle-heading-actions">
          <button className="add-vehicle-button" type="button" onClick={() => { setShowCreateForm((open) => !open); setCreateError(''); setCreateSuccess('') }}>
            {showCreateForm ? 'Cancel' : '+ Add vehicle'}
          </button>
          <button className="refresh-button" onClick={() => void fetchVehicles()} disabled={loading}>
            {loading ? 'Refreshing...' : 'Refresh vehicles'}
          </button>
        </div>
      </div>

      {showCreateForm && (
        <section className="vehicle-panel create-vehicle-panel">
          <div className="vehicle-panel-heading">
            <div>
              <h3>Add a police vehicle</h3>
              <p>Register a vehicle in the fleet. A location will appear after it reports telemetry.</p>
            </div>
          </div>
          <form className="create-vehicle-form" onSubmit={handleCreateVehicle}>
            <label>
              Callsign *
              <input value={newCallsign} onChange={(event) => setNewCallsign(event.target.value)} maxLength={30} required placeholder="e.g. UNIT-104" />
            </label>
            <label>
              License plate *
              <input value={newLicensePlate} onChange={(event) => setNewLicensePlate(event.target.value)} maxLength={20} required placeholder="e.g. CPL 104" />
            </label>
            <label>
              Initial status
              <select value={newStatus} onChange={(event) => setNewStatus(event.target.value)}>
                <option value="Available">Available</option>
                <option value="OnPatrol">On patrol</option>
                <option value="Dispatched">Dispatched</option>
                <option value="OutOfService">Out of service</option>
              </select>
            </label>
            <div className="create-vehicle-submit">
              <button className="add-vehicle-button" type="submit" disabled={creating}>
                {creating ? 'Adding vehicle...' : 'Create vehicle'}
              </button>
            </div>
          </form>
          <p className="create-vehicle-note">Requires a Security Administrator JWT. Callsign and license plate must both be unique.</p>
          {createError && <div className="error-message" role="alert">{createError}</div>}
        </section>
      )}

      {createSuccess && <div className="success-message" role="status">{createSuccess}</div>}
      {error && <div className="error-message" role="alert">{error}</div>}

      <div className="vehicle-summary">
        <article><span>Total vehicles</span><strong>{totalCount}</strong></article>
        <article><span>On patrol</span><strong>{counts.onPatrol}</strong></article>
        <article><span>Dispatched</span><strong>{counts.dispatched}</strong></article>
        <article><span>Out of service</span><strong>{counts.unavailable}</strong></article>
      </div>

      <div className="vehicle-content-grid">
        <section className="vehicle-panel">
          <div className="vehicle-panel-heading">
            <div>
              <h3>Fleet locations</h3>
              <p>Select a vehicle to see its latest telemetry.</p>
            </div>
            <span className="location-legend"><i /> Reported location</span>
          </div>

          {loading ? <p className="vehicle-empty">Loading vehicles...</p> : vehicles.length === 0 ? (
            <p className="vehicle-empty">No vehicles were returned by the API.</p>
          ) : (
            <div className="vehicle-location-list">
              {vehicles.map((vehicle) => (
                <button
                  type="button"
                  className={`vehicle-location-row ${selectedVehicle?.id === vehicle.id ? 'selected' : ''}`}
                  key={vehicle.id}
                  onClick={() => void fetchLatestTelemetry(vehicle)}
                >
                  <span className={`vehicle-dot ${statusClass(vehicle.status)}`} aria-label={`${vehicle.status} location`} />
                  <span className="vehicle-location-copy">
                    <strong>{vehicle.callsign}</strong>
                    <small>{formatAddress(vehicle.currentLocation)}</small>
                  </span>
                  <span className={`vehicle-status ${statusClass(vehicle.status)}`}>{vehicle.status}</span>
                </button>
              ))}
            </div>
          )}
          <p className="map-note">Dots indicate vehicles with a reported location. Exact locations are shown as addresses; this is a fleet overview, not a street-level map.</p>
        </section>

        <section className="vehicle-panel vehicle-details-panel">
          <h3>Vehicle details</h3>
          {!selectedVehicle ? (
            <p className="vehicle-empty">Choose a vehicle from the fleet list.</p>
          ) : (
            <>
              <div className="vehicle-detail-title">
                <div className="vehicle-detail-icon">●</div>
                <div>
                  <strong>{selectedVehicle.callsign}</strong>
                  <span>{selectedVehicle.licensePlate}</span>
                </div>
              </div>
              <dl className="vehicle-detail-list">
                <div><dt>Status</dt><dd>{selectedVehicle.status}</dd></div>
                <div><dt>Vehicle ID</dt><dd className="vehicle-id">{selectedVehicle.id}</dd></div>
                <div><dt>Current address</dt><dd>{formatAddress(latestTelemetry ?? selectedVehicle.currentLocation)}</dd></div>
                {latestTelemetry?.speedKmh != null && <div><dt>Speed</dt><dd>{latestTelemetry.speedKmh} km/h</dd></div>}
                {latestTelemetry?.headingDegrees != null && <div><dt>Heading</dt><dd>{latestTelemetry.headingDegrees}°</dd></div>}
                <div><dt>Last reported</dt><dd>{formatTime(latestTelemetry?.recordedAt ?? selectedVehicle.currentLocation?.recordedAt)}</dd></div>
              </dl>
              <div className="vehicle-management-actions">
                <button type="button" className="refresh-button" onClick={() => startEditing(selectedVehicle)}>Edit vehicle</button>
                <button type="button" className="danger-button" onClick={() => void handleDeleteVehicle()} disabled={deleting}>{deleting ? 'Removing...' : 'Delete vehicle'}</button>
              </div>
              {editing && (
                <form className="vehicle-edit-form" onSubmit={handleUpdateVehicle}>
                  <h4>Update vehicle details</h4>
                  <label>Callsign<input value={editCallsign} onChange={e => setEditCallsign(e.target.value)} maxLength={30} required /></label>
                  <label>License plate<input value={editPlate} onChange={e => setEditPlate(e.target.value)} maxLength={20} required /></label>
                  <label>Status<select value={editStatus} onChange={e => setEditStatus(e.target.value)}><option value="Available">Available</option><option value="OnPatrol">On patrol</option><option value="Dispatched">Dispatched</option><option value="OutOfService">Out of service</option></select></label>
                  <div className="vehicle-management-actions"><button className="add-vehicle-button" disabled={savingEdit}>{savingEdit ? 'Saving...' : 'Save changes'}</button><button type="button" className="refresh-button" onClick={() => setEditing(false)}>Cancel</button></div>
                </form>
              )}
              <form className="vehicle-edit-form" onSubmit={handleSaveLocation}>
                <h4>Update location and telemetry</h4>
                <p>Record a new Waterloo address, speed, and direction (heading in degrees).</p>
                <label>Street number<input required maxLength={10} value={locationForm.streetNumber} onChange={e => setLocationForm(v => ({ ...v, streetNumber: e.target.value }))} /></label>
                <label>Street name<input required maxLength={100} value={locationForm.streetName} onChange={e => setLocationForm(v => ({ ...v, streetName: e.target.value }))} /></label>
                <label>Postal code<input required placeholder="N2L 3G1" value={locationForm.postalCode} onChange={e => setLocationForm(v => ({ ...v, postalCode: e.target.value }))} /></label>
                <label>Speed (km/h)<input type="number" min="0" max="500" step="0.1" value={locationForm.speedKmh} onChange={e => setLocationForm(v => ({ ...v, speedKmh: e.target.value }))} /></label>
                <label>Direction / heading (0–359.99°)<input type="number" min="0" max="359.99" step="0.01" value={locationForm.headingDegrees} onChange={e => setLocationForm(v => ({ ...v, headingDegrees: e.target.value }))} /></label>
                <button className="add-vehicle-button" disabled={savingLocation}>{savingLocation ? 'Saving telemetry...' : 'Save location update'}</button>
              </form>
              {editError && <div className="error-message" role="alert">{editError}</div>}
              {detailLoading && <p className="vehicle-empty">Loading latest telemetry...</p>}
              {!detailLoading && !latestTelemetry && !selectedVehicle.currentLocation && (
                <p className="vehicle-empty">This vehicle has not reported a location yet.</p>
              )}
            </>
          )}
        </section>
      </div>
    </section>
  )
}
