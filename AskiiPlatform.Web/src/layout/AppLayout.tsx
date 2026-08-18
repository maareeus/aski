import { useState } from 'react'
import type { FC } from 'react'
import { Link, Outlet } from 'react-router-dom'
import type { LinkProps } from 'react-router-dom'
import {
  Button,
  Col,
  Container,
  Header,
  HeaderBrand,
  HeaderContent,
  HeaderRightZone,
  Headers,
  Icon,
  Row,
} from 'design-react-kit'
import type { HeaderBrandProps } from 'design-react-kit'
import { useAuth } from '../auth/AuthContext'
import { NavSidebar } from './NavSidebar'

/**
 * HeaderBrand accetta `tag` per rendersi con un componente diverso, ma i suoi
 * prop non includono quelli del componente passato: senza questa asserzione
 * `to` di react-router non è accettato e resterebbe solo `href`, che ricarica
 * l'intera pagina invece di navigare lato client.
 */
const BrandLink = HeaderBrand as unknown as FC<HeaderBrandProps & Pick<LinkProps, 'to'>>

export function AppLayout() {
  const { session, logout } = useAuth()
  const [menuAperto, setMenuAperto] = useState(false)

  return (
    <>
      <Headers>
        <Header theme="dark" type="center" small>
          <HeaderContent>
            <BrandLink tag={Link} to="/">
              <h2>Askii Platform</h2>
              <h3>Pannello di amministrazione</h3>
            </BrandLink>
            <HeaderRightZone>
              <div className="d-flex align-items-center gap-3">
                <Link to="/profilo" className="text-white text-decoration-none d-none d-md-flex align-items-center gap-2">
                  <Icon icon="it-user" color="white" size="sm" aria-hidden />
                  <span>
                    {session?.fullName?.trim() || session?.email}
                    <span className="visually-hidden"> — vai al profilo</span>
                  </span>
                </Link>
                <Button
                  color="primary"
                  size="xs"
                  onClick={() => logout('utente')}
                  aria-label="Esci dalla sessione"
                >
                  <Icon icon="it-logout" color="white" size="xs" aria-hidden className="me-1" />
                  Esci
                </Button>
              </div>
            </HeaderRightZone>
          </HeaderContent>
        </Header>
      </Headers>

      <Container fluid className="my-4">
        <Row>
          <Col xs="12" lg="3" xl="2">
            {/* Su schermi piccoli la navigazione è collassata dietro un toggle */}
            <Button
              color="primary"
              outline
              className="d-lg-none mb-3 w-100"
              onClick={() => setMenuAperto((v) => !v)}
              aria-expanded={menuAperto}
              aria-controls="nav-principale"
            >
              <Icon icon={menuAperto ? 'it-close' : 'it-burger'} size="sm" aria-hidden className="me-1" />
              {menuAperto ? 'Chiudi menu' : 'Menu'}
            </Button>
            <div id="nav-principale" className={menuAperto ? '' : 'd-none d-lg-block'}>
              <NavSidebar onNavigate={() => setMenuAperto(false)} />
            </div>
          </Col>

          <Col xs="12" lg="9" xl="10">
            <main>
              <Outlet />
            </main>
          </Col>
        </Row>
      </Container>
    </>
  )
}
