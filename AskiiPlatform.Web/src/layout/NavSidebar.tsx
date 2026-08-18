import { NavLink, useLocation } from 'react-router-dom'
import { Badge, Icon, LinkList, LinkListItem, Sidebar } from 'design-react-kit'
import type { IconName } from 'design-react-kit'
import { useAuth } from '../auth/AuthContext'

interface Voce {
  to: string
  label: string
  icon: IconName
  soloAdmin?: boolean
}

const SEZIONI: { titolo: string; voci: Voce[] }[] = [
  {
    titolo: 'Generale',
    voci: [{ to: '/', label: 'Riepilogo', icon: 'it-chart-line' }],
  },
  {
    titolo: 'Utenti',
    voci: [
      { to: '/utenti', label: 'Elenco utenti', icon: 'it-list', soloAdmin: true },
      { to: '/utenti/nuovo', label: 'Nuovo utente', icon: 'it-plus-circle', soloAdmin: true },
      { to: '/utenti/modifica', label: 'Modifica utente', icon: 'it-pencil', soloAdmin: true },
      { to: '/utenti/attiva', label: 'Attiva utente', icon: 'it-check-circle' },
      { to: '/utenti/elimina', label: 'Elimina utente', icon: 'it-delete', soloAdmin: true },
    ],
  },
  {
    titolo: 'Il mio account',
    voci: [
      { to: '/profilo', label: 'Profilo e 2FA', icon: 'it-user' },
      { to: '/password', label: 'Cambia password', icon: 'it-key' },
    ],
  },
]

export function NavSidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { isAdmin } = useAuth()
  const { pathname } = useLocation()

  return (
    <Sidebar left className="rounded shadow-sm">
      {SEZIONI.map((sezione) => {
        const voci = sezione.voci.filter((v) => !v.soloAdmin || isAdmin)
        if (voci.length === 0) return null

        return (
          <LinkList key={sezione.titolo} className="mb-2">
            <LinkListItem header>{sezione.titolo}</LinkListItem>
            {voci.map((voce) => (
              <LinkListItem
                key={voce.to}
                tag={NavLink}
                to={voce.to}
                active={voce.to === '/' ? pathname === '/' : pathname.startsWith(voce.to)}
                onClick={onNavigate}
                // Il kit imposta role="button" sull'ancora: qui è una
                // navigazione, e va annunciata come link.
                role="link"
              >
                <Icon icon={voce.icon} size="sm" aria-hidden className="me-2" />
                <span>{voce.label}</span>
                {voce.soloAdmin && (
                  <Badge color="primary" className="ms-2" pill>
                    Admin
                  </Badge>
                )}
              </LinkListItem>
            ))}
          </LinkList>
        )
      })}
    </Sidebar>
  )
}
