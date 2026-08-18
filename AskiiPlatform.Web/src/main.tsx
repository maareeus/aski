import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

// Design system Designers Italia: il CSS di Bootstrap Italia va importato
// prima degli stili applicativi, così le nostre regole possono sovrascriverlo.
import 'bootstrap-italia/dist/css/bootstrap-italia.min.css'
import './index.css'

import App from './App'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
