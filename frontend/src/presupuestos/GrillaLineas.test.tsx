import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { GrillaLineas, type LineaEditable } from './GrillaLineas'

const inicial: LineaEditable[] = [
  { clave: 'a', id: null, articuloCodigo: 7, descripcion: 'Armazón metal', precioUnitario: 181500, cantidad: '1', descuento: '' },
]

function ConEstado({ editable = true }: { editable?: boolean }) {
  const [lineas, setLineas] = useState(inicial)
  return (
    <GrillaLineas
      lineas={lineas}
      editable={editable}
      errores={{}}
      alCambiar={(i, campo, valor) => setLineas((ls) => ls.map((l, j) => (j === i ? { ...l, [campo]: valor } : l)))}
      alQuitar={(i) => setLineas((ls) => ls.filter((_, j) => j !== i))}
    />
  )
}

describe('GrillaLineas', () => {
  it('muestra las siete columnas de la línea (AC-25) con importes en pesos', () => {
    render(<ConEstado />)
    for (const columna of ['Código de artículo', 'Descripción', 'Precio unitario', 'Cantidad', '% Descuento', 'Precio con descuento', 'Precio final'])
      expect(screen.getByRole('columnheader', { name: columna })).toBeInTheDocument()
    const fila = screen.getByText('Armazón metal').closest('tr')!
    expect(within(fila).getAllByText('$ 1.815,00')).toHaveLength(3)
  })

  it('recalcula al cambiar cantidad o descuento', async () => {
    render(<ConEstado />)
    const cantidad = screen.getByLabelText('Cantidad de la línea 1')
    await userEvent.clear(cantidad)
    await userEvent.type(cantidad, '3')
    await userEvent.type(screen.getByLabelText('Descuento (%) de la línea 1'), '10')
    const fila = screen.getByText('Armazón metal').closest('tr')!
    expect(within(fila).getByText('$ 1.633,50')).toBeInTheDocument()
    expect(within(fila).getByText('$ 4.900,50')).toBeInTheDocument()
  })

  it('sin edición no muestra campos ni botón para quitar', () => {
    render(<ConEstado editable={false} />)
    expect(screen.queryAllByRole('textbox')).toHaveLength(0)
    expect(screen.queryByRole('button', { name: /Quitar/ })).not.toBeInTheDocument()
  })

  it('quita una línea', async () => {
    render(<ConEstado />)
    await userEvent.click(screen.getByRole('button', { name: 'Quitar Armazón metal' }))
    expect(screen.queryByText('Armazón metal')).not.toBeInTheDocument()
  })
})
