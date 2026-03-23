## ADDED Requirements

### Requirement: Mostrar detalle de mano en formato Hand History
El sistema SHALL mostrar el detalle de la mano seleccionada en un `RichTextBox` (`rtbHandDetail`) con formato Hand History clásico, incluyendo: header con número de mano, fecha, mesa y situación; cartas del héroe con posición y stack; decisiones por cada street jugado; y resultado final.

#### Scenario: Seleccionar mano muestra su detalle
- **WHEN** el usuario selecciona una fila en `dgvSessionHands`
- **THEN** `rtbHandDetail` se limpia y se llena con el Hand History formateado de esa mano

#### Scenario: Sin mano seleccionada
- **WHEN** se cambia de sesión y no hay mano seleccionada
- **THEN** `rtbHandDetail` queda vacío

---

### Requirement: Formato del header de Hand History
El sistema SHALL mostrar un bloque header con: línea de separación (`═══`), número de mano y fecha UTC, nombre de mesa con blinds, situación preflop y número de oponentes, seguido de otra línea de separación.

#### Scenario: Header completo
- **WHEN** se formatea una mano con HandNumber=12345, Timestamp=2026-03-22 14:30:05, BigBlind=0.50, Situation=OpenRaise, NumOpponents=3
- **THEN** el header muestra:
  ```
  ═══════════════════════════════════════════
  Hand #12345 — 2026-03-22 14:30:05 UTC
  Mesa: NL Holdem ($0.25/$0.50)
  Situación: OpenRaise | Oponentes: 3
  ═══════════════════════════════════════════
  ```

---

### Requirement: Formato de cartas del héroe
El sistema SHALL mostrar las cartas del héroe, su posición abreviada y su stack inicial en una línea separada después del header.

#### Scenario: Línea de héroe
- **WHEN** la mano tiene HeroCard1="Ah", HeroCard2="Kd", HeroPosition=Button, HeroStackStart=52.30
- **THEN** se muestra: `Hero [Ah Kd] — Button — Stack: $52.30`

---

### Requirement: Formato de decisiones por street
El sistema SHALL mostrar por cada `StreetDecision` en `hand.Decisions`: un header de street con las cartas del board progresivo, seguido de equity%, pot odds%, EV, acción recomendada, acción tomada y tamaño del pot.

#### Scenario: Decisión de flop
- **WHEN** la mano tiene una StreetDecision con Street=Flop, EquityPercent=45.2, PotOddsPercent=33.0, ExpectedValue=1.25, RecommendedAction="Bet 1/2", ActionTaken="Bet", BetSize=2.50, PotSizeAtDecision=6.75
- **AND** FlopCards=["Qs", "Jc", "3h"]
- **THEN** se muestra:
  ```
  *** FLOP *** [Qs Jc 3h]
    Equity: 45.2% | Pot Odds: 33.0% | EV: +1.25
    Recomendado: Bet 1/2
    Acción: Bet $2.50 | Pot: $6.75
  ```

#### Scenario: Decisión de turn con board progresivo
- **WHEN** la mano tiene una StreetDecision con Street=Turn y TurnCard="Td"
- **THEN** el header muestra: `*** TURN *** [Qs Jc 3h] [Td]` (flop entre corchetes + turn separado)

#### Scenario: Decisión de river con board completo
- **WHEN** la mano tiene una StreetDecision con Street=River y RiverCard="2s"
- **THEN** el header muestra: `*** RIVER *** [Qs Jc 3h Td] [2s]` (flop+turn entre corchetes + river separado)

#### Scenario: Mano sin decisiones postflop
- **WHEN** la mano no tiene StreetDecisions (fold preflop)
- **THEN** no se muestra ninguna sección de street, solo header, cartas y resultado

---

### Requirement: Formato del resultado final
El sistema SHALL mostrar al final del Hand History: una línea de separación (`───`), el resultado (Won/Lost/Push) con el P/L entre paréntesis, el stack final, y una línea de cierre (`═══`).

#### Scenario: Mano ganada
- **WHEN** la mano tiene Result=Won, HeroStackEnd=60.50, HeroStackStart=52.30
- **THEN** se muestra:
  ```
  ───────────────────────────────────────────
  RESULTADO: Won (+$8.20)
  Stack final: $60.50
  ═══════════════════════════════════════════
  ```

#### Scenario: Mano perdida
- **WHEN** la mano tiene Result=Lost, HeroStackEnd=42.00, HeroStackStart=52.30
- **THEN** se muestra: `RESULTADO: Lost (-$10.30)` en rojo

#### Scenario: Push
- **WHEN** la mano tiene Result=Push
- **THEN** se muestra: `RESULTADO: Push ($0.00)` en gris

---

### Requirement: Colores diferenciados por sección
El sistema SHALL aplicar colores específicos a cada sección del Hand History según la tabla de colores definida en el diseño.

#### Scenario: Header en azul acento
- **WHEN** se renderiza el header (líneas `═══`, `Hand #`, `Mesa:`, `Situación:`)
- **THEN** el texto se muestra en `AppThemeHelper.Accent` (0,123,191)

#### Scenario: Street headers en azul claro
- **WHEN** se renderiza un header de street (`*** FLOP ***`, `*** TURN ***`, `*** RIVER ***`)
- **THEN** el texto se muestra en `Color.DodgerBlue`

#### Scenario: Stats en gris
- **WHEN** se renderiza la línea de equity/pot odds/EV
- **THEN** el texto se muestra en `AppThemeHelper.PrimaryLight`

#### Scenario: Acción tomada en amarillo
- **WHEN** se renderiza la línea de acción tomada ("Acción: Bet $2.50")
- **THEN** el texto se muestra en `Color.DarkGoldenrod`

#### Scenario: Resultado con color según outcome
- **WHEN** el resultado es Won → verde (`AppThemeHelper.Success`)
- **WHEN** el resultado es Lost → rojo (`AppThemeHelper.Danger`)
- **WHEN** el resultado es Push → gris (`AppThemeHelper.PrimaryLight`)

---

### Requirement: Configuración visual del RichTextBox
El `rtbHandDetail` SHALL tener fondo oscuro `Color.FromArgb(30, 33, 45)`, fuente "Consolas" 10pt, ForeColor blanco, ReadOnly=true, y Dock=Fill.

#### Scenario: Aspecto inicial
- **WHEN** se inicializa la pestaña Historial
- **THEN** `rtbHandDetail` tiene fondo oscuro, fuente monoespaciada, y está vacío en modo solo lectura

---

### Requirement: Helper AppendColoredText
El sistema SHALL implementar un método helper `AppendColoredText(RichTextBox rtb, string text, Color color, bool bold = false)` que permita construir el contenido del RichTextBox progresivamente con colores y estilos diferenciados.

#### Scenario: Texto con color aplicado
- **WHEN** se llama `AppendColoredText(rtb, "*** FLOP ***", Color.DodgerBlue)`
- **THEN** el texto se añade al final del RichTextBox con el color especificado sin afectar el texto previo

#### Scenario: Texto con negrita
- **WHEN** se llama `AppendColoredText(rtb, "[Ah Kd]", Color.White, bold: true)`
- **THEN** el texto se añade en negrita con el color especificado
