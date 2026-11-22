import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
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

describe('Security Admin Dashboard', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => personnel,
      })
    )
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders the dashboard', async () => {
    render(<App />)

    expect(
      screen.getByRole('heading', { name: 'Security Administration' })).toBeInTheDocument()

    expect(await screen.findByText('B1001')).toBeInTheDocument()
  })

  it('displays personnel returned by the API', async () => {
    render(<App />)

    expect(await screen.findByText('John Stamos')).toBeInTheDocument()
    expect(screen.getByRole('cell' , {name: 'Police Officer'})).toBeInTheDocument()
  })

  it('displays an error when personnel cannot be loaded', async () => {
    vi.mocked(fetch).mockResolvedValueOnce({
      ok: false,
      status: 500,
    } as Response)

    render(<App />)

    expect(await screen.findAllByText(/Failed to load personnel/)).not.toHaveLength(0)
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
})