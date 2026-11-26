import {
  describe,
  it,
  expect,
  vi,
  beforeEach,
  afterEach,
} from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import App from '../App'

const personnel = [
  {
    officerId: 'officer-1',
    userId: null,
    badgeNumber: 'B1001',
    firstName: 'John',
    lastName: 'Stamos',
    rankTitle: 'Constable',
    phone: null,
    email: 'johnStamos@gmail.com',
    hireDate: null,
    isActive: true,
    createdAt: '2026-01-01',
    currentRole: 'Police Officer',
  },
]

const roadClosures = [
  {
    closureId: 'closure-1',
    roadName: 'King Street',
    description: 'Road closed due to construction.',
    location: 'Between Queen Street and Ontario Street',
    startDate: '2026-01-01T10:00:00Z',
    endDate: '2026-01-01T16:00:00Z',
    status: 'Active',
    lastUpdated: '2026-01-01T10:00:00Z',
  },
  {
    closureId: 'closure-2',
    roadName: 'Weber Street',
    description: 'Lane closure due to emergency repairs.',
    location: 'Near Frederick Street',
    startDate: '2026-01-01T11:00:00Z',
    endDate: null,
    status: 'Active',
    lastUpdated: '2026-01-01T11:00:00Z',
  },
]

const successfulResponse = (data: unknown): Response => ({
  ok: true,
  status: 200,
  json: async () => data,
}) as Response

describe('Security Admin Dashboard', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockImplementation((url: string) => {
        if (url.includes('/road-closures')) {
          return Promise.resolve(successfulResponse(roadClosures))
        }

        return Promise.resolve(successfulResponse(personnel))
      })
    )
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders the dashboard', async () => {
    render(<App />)

    expect(
      screen.getByRole('heading', {
        name: 'Security Administration',
      })
    ).toBeInTheDocument()

    expect(await screen.findByText('B1001')).toBeInTheDocument()
  })

  it('displays personnel returned by the API', async () => {
    render(<App />)

    expect(await screen.findByText('John Stamos')).toBeInTheDocument()
    expect(
      screen.getByRole('cell', { name: 'Police Officer' })
    ).toBeInTheDocument()
  })

  it('displays an error when personnel cannot be loaded', async () => {
    vi.mocked(fetch).mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/road-closures')) {
        return Promise.resolve(successfulResponse(roadClosures))
      }

      return Promise.resolve({
        ok: false,
        status: 500,
        json: async () => ({})
      } as Response)
    })

    render(<App />)

    expect(
      await screen.findAllByText(/Failed to load personnel/)
    ).not.toHaveLength(0)
  })

  it('requires a JWT before creating personnel', async () => {
    render(<App />)

    await screen.findByText('B1001')

    fireEvent.change(screen.getByLabelText('Badge Number *'), {
      target: { value: 'B1002' },
    })

    fireEvent.change(screen.getByLabelText('First Name *'), {
      target: { value: 'Jane' },
    })

    fireEvent.change(screen.getByLabelText('Last Name *'), {
      target: { value: 'Doe' },
    })

    fireEvent.click(
      screen.getByRole('button', { name: 'Create Personnel' })
    )

    expect(
      await screen.findAllByText(/Enter a Security Administrator JWT/)
    ).not.toHaveLength(0)
  })

  it('requires a JWT before updating a role', async () => {
    render(<App />)

    const row = await screen.findByText('B1001')

    const tableRow = row.closest('tr')
    expect(tableRow).not.toBeNull()

    const roleSelect = tableRow!.querySelector('select')
    expect(roleSelect).not.toBeNull()

    fireEvent.change(roleSelect!, {
      target: { value: 'Sergeant' },
    })

    fireEvent.click(
      screen.getByRole('button', { name: 'Update Role' })
    )

    expect(
      await screen.findAllByText(/Enter a Security Administrator JWT/)
    ).not.toHaveLength(0)
  })

  it('displays road closures returned by the API', async () => {
    render(<App />)

    expect(await screen.findByText('King Street')).toBeInTheDocument()
    expect(await screen.findByText('Weber Street')).toBeInTheDocument()
  })

  it('displays an error when road closures cannot be loaded', async () => {
    vi.mocked(fetch).mockImplementation((input: RequestInfo | URL) => {
      const url = String(input)
      if (url.includes('/road-closures')) {
        return Promise.resolve({
          ok: false,
          status: 500,
          json: async () => ({}),
        } as Response)
      }

      return Promise.resolve(successfulResponse(personnel))
    })

    render(<App />)

    expect(
      await screen.findByText(/Failed to load road closures/i)
    ).toBeInTheDocument()
  })

  it('requests road closures again when refresh is clicked', async () => {
    render(<App />)

    await screen.findByText('King Street')

    const roadClosureRequests = vi.mocked(fetch).mock.calls.filter(
      ([url]) => String(url).includes('/road-closures')
    ).length

    fireEvent.click(
      screen.getByRole('button', { name: /Refresh Closures/i })
    )

    await waitFor(() => {
      const updatedRequests = vi.mocked(fetch).mock.calls.filter(
        ([url]) => String(url).includes('/road-closures')
      ).length

      expect(updatedRequests).toBeGreaterThan(roadClosureRequests)
    })
  })
})