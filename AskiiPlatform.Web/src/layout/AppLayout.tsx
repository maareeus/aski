import { Outlet } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar'
import { TooltipProvider } from '@/components/ui/tooltip'
import { useAuth } from '@/auth/AuthContext'
import { AppSidebar } from './AppSidebar'

export function AppLayout() {
  const { session, logout } = useAuth()

  const iniziali =
    session?.fullName
      ?.trim()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase())
      .join('') || session?.email?.[0]?.toUpperCase()

  return (
    <TooltipProvider delayDuration={300}>
      <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        <header className="bg-background sticky top-0 z-10 flex h-16 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger className="-ml-1" />
          <Separator orientation="vertical" className="mr-2 !h-5" />

          <div className="ml-auto flex items-center gap-3">
            <div className="flex items-center gap-2.5">
              <div className="bg-muted text-muted-foreground flex size-8 items-center justify-center rounded-full text-xs font-medium">
                {iniziali}
              </div>
              <div className="hidden text-sm leading-tight sm:grid">
                <span className="font-medium">{session?.fullName?.trim() || session?.email}</span>
                <span className="text-muted-foreground text-xs">{session?.role}</span>
              </div>
            </div>
            <Button variant="outline" size="sm" onClick={() => logout('utente')}>
              <LogOut />
              Esci
            </Button>
          </div>
        </header>

        <main className="flex-1 p-4 md:p-6">
          <div className="mx-auto w-full max-w-5xl">
            <Outlet />
          </div>
        </main>
      </SidebarInset>
      </SidebarProvider>
    </TooltipProvider>
  )
}
