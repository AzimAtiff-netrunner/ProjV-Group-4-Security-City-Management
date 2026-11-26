import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import VehicleDashboard from '../VehicleDashboard'

const vehicle = {
  id: 'vehicle-101',
  callsign: 'UNIT-101',
  licensePlate: 'TEST 101',
  status: 'Available',
  currentLocation: null,
}

const telemetry = {
  vehicleId: vehicle.id,
  address: { streetNumber: '100', streetName: 'King Street', city: 'Waterloo', postalCode: 'N2J 2W9' },
  speedKmh: 62.4,
  headingDegrees: 180.25,
  recordedAt: '2026-11-25T18:00:00Z',
}

function response(body: unknown, status = 200): Response {
  return { ok: status >= 200 && status < 300, status, json: async () => body, text: async () => '' } as Response
}

function setupFetch() {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'
    if (url.includes('/telemetry/latest')) return response(telemetry)
    if (url.endsWith('/telemetry') && method === 'POST') return response(telemetry, 201)
    if (url.split('?')[0].endsWith('/vehicles') && method === 'GET') return response({ items: [vehicle], totalCount: 1 })
    if (url.endsWith('/vehicles') && method === 'POST') return response({ ...vehicle, callsign: 'UNIT-102' }, 201)
    if (url.endsWith(vehicle.id) && method === 'PUT') return response({ ...vehicle, callsign: 'UNIT-UPDATED', status: 'OnPatrol' })
    if (url.endsWith(vehicle.id) && method === 'DELETE') return response(null, 204)
    return response({}, 200)
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('VehicleDashboard telemetry and fleet management', () => {
  beforeEach(() => setupFetch())
  afterEach(() => vi.unstubAllGlobals())

  async function selectVehicle() {
    await screen.findByText('UNIT-101')
    fireEvent.click(screen.getByRole('button', { name: /UNIT-101/ }))
    await screen.findByText('Update location and telemetry')
  }

  it('loads vehicles and retrieves the selected vehicle telemetry', async () => {
    const fetchMock = vi.mocked(fetch)
    render(<VehicleDashboard token="test-jwt" />)
    await selectVehicle()

    expect(fetchMock).toHaveBeenCalledWith('/api/security/v1/vehicles?page=1&pageSize=100', {
      headers: { Authorization: 'Bearer test-jwt' },
    })
    expect(await screen.findByText('62.4 km/h')).toBeInTheDocument()
    expect(screen.getByText('180.25°')).toBeInTheDocument()
  })

  it('submits location, speed and direction telemetry', async () => {
    const fetchMock = vi.mocked(fetch)
    render(<VehicleDashboard token="writer-jwt" />)
    await selectVehicle()

    fireEvent.change(screen.getByLabelText('Street number'), { target: { value: '100' } })
    fireEvent.change(screen.getByLabelText('Street name'), { target: { value: 'King Street' } })
    fireEvent.change(screen.getByLabelText('Postal code'), { target: { value: 'n2j 2w9' } })
    fireEvent.change(screen.getByLabelText('Speed (km/h)'), { target: { value: '62.4' } })
    fireEvent.change(screen.getByLabelText(/Direction \/ heading/), { target: { value: '180.25' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save location update' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/api/security/v1/vehicles/${vehicle.id}/telemetry`,
      expect.objectContaining({ method: 'POST', headers: expect.objectContaining({ Authorization: 'Bearer writer-jwt' }) }),
    ))
    const request = fetchMock.mock.calls.find(([url, init]) => String(url).endsWith('/telemetry') && init?.method === 'POST')
    const body = JSON.parse(String(request?.[1]?.body))
    expect(body.address).toMatchObject({ streetNumber: '100', streetName: 'King Street', postalCode: 'N2J 2W9' })
    expect(body.speedKmh).toBe(62.4)
    expect(body.headingDegrees).toBe(180.25)
    expect(body.recordedAt).toBeTruthy()
  })

  it('updates vehicle callsign, plate and status', async () => {
    const fetchMock = vi.mocked(fetch)
    render(<VehicleDashboard token="admin-jwt" />)
    await selectVehicle()
    fireEvent.click(screen.getByRole('button', { name: 'Edit vehicle' }))
    fireEvent.change(screen.getByLabelText('Callsign'), { target: { value: 'UNIT-UPDATED' } })
    fireEvent.change(screen.getByLabelText('License plate'), { target: { value: 'NEW 101' } })
    fireEvent.change(screen.getByLabelText('Status'), { target: { value: 'OnPatrol' } })
    fireEvent.click(screen.getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/api/security/v1/vehicles/${vehicle.id}`,
      expect.objectContaining({ method: 'PUT' }),
    ))
    const request = fetchMock.mock.calls.find(([url, init]) => String(url).endsWith(vehicle.id) && init?.method === 'PUT')
    expect(JSON.parse(String(request?.[1]?.body))).toEqual({ callsign: 'UNIT-UPDATED', licensePlate: 'NEW 101', status: 'OnPatrol' })
  })

  it('deletes a vehicle after confirmation', async () => {
    const fetchMock = vi.mocked(fetch)
    vi.stubGlobal('confirm', vi.fn(() => true))
    render(<VehicleDashboard token="admin-jwt" />)
    await selectVehicle()
    fireEvent.click(screen.getByRole('button', { name: 'Delete vehicle' }))

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(
      `/api/security/v1/vehicles/${vehicle.id}`,
      expect.objectContaining({ method: 'DELETE', headers: { Authorization: 'Bearer admin-jwt' } }),
    ))
    expect(await screen.findByText('Vehicle was removed from the active fleet.')).toBeInTheDocument()
  })

  it('does not delete when confirmation is cancelled', async () => {
    const fetchMock = vi.mocked(fetch)
    vi.stubGlobal('confirm', vi.fn(() => false))
    render(<VehicleDashboard token="admin-jwt" />)
    await selectVehicle()
    fireEvent.click(screen.getByRole('button', { name: 'Delete vehicle' }))

    expect(fetchMock).not.toHaveBeenCalledWith(expect.stringContaining(vehicle.id), expect.objectContaining({ method: 'DELETE' }))
  })
})
