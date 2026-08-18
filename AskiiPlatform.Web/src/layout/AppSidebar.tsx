import { NavLink, useLocation } from 'react-router-dom'
import { KeyRound, LayoutDashboard, ShieldCheck, Users } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/components/ui/sidebar'
import { useAuth } from '@/auth/AuthContext'

interface Voce {
  to: string
  label: string
  icon: LucideIcon
  soloAdmin?: boolean
}

/**
 * La navigazione elenca solo i punti d'ingresso. Le operazioni su un singolo
 * utente — creazione, modifica, attivazione, eliminazione, reset password — si
 * raggiungono dall'elenco e dal dettaglio, dove l'utente su cui agiscono è già
 * identificato.
 */
const SEZIONI: { titolo: string; voci: Voce[] }[] = [
  {
    titolo: 'Generale',
    voci: [{ to: '/', label: 'Riepilogo', icon: LayoutDashboard }],
  },
  {
    titolo: 'Gestione',
    voci: [{ to: '/users', label: 'Utenti', icon: Users, soloAdmin: true }],
  },
  {
    titolo: 'Il mio account',
    voci: [
      { to: '/profile', label: 'Profilo e 2FA', icon: ShieldCheck },
      { to: '/password', label: 'Cambia password', icon: KeyRound },
    ],
  },
]

export function AppSidebar() {
  const { isAdmin } = useAuth()
  const { pathname } = useLocation()

  const attiva = (to: string) => (to === '/' ? pathname === '/' : pathname.startsWith(to))

  return (
    <Sidebar>
      <SidebarHeader className="border-b p-4">
        <NavLink to="/" className="flex items-center gap-2.5">
          <div className="bg-primary text-primary-foreground flex size-9 shrink-0 items-center justify-center rounded-md font-semibold">
            A
          </div>
          <div className="grid text-sm leading-tight">
            <span className="truncate font-semibold">Askii Platform</span>
            <span className="text-muted-foreground truncate text-xs">Amministrazione</span>
          </div>
        </NavLink>
      </SidebarHeader>

      <SidebarContent>
        {SEZIONI.map((sezione) => {
          const voci = sezione.voci.filter((v) => !v.soloAdmin || isAdmin)
          if (voci.length === 0) return null

          return (
            <SidebarGroup key={sezione.titolo}>
              <SidebarGroupLabel>{sezione.titolo}</SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {voci.map((voce) => (
                    <SidebarMenuItem key={voce.to}>
                      <SidebarMenuButton asChild isActive={attiva(voce.to)} tooltip={voce.label}>
                        <NavLink to={voce.to}>
                          <voce.icon />
                          <span>{voce.label}</span>
                        </NavLink>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          )
        })}
      </SidebarContent>
    </Sidebar>
  )
}
