## 1. Street Validation en Game Loop

- [x] 1.1 FlopDetected transition pasa boardCards=3.
- [x] 1.2 TurnDetected transition pasa boardCards=4.
- [x] 1.3 RiverDetected transition pasa boardCards=5.

## 2. Villain Type Posicional

- [x] 2.1 GetVillainType() acepta heroIsInPosition opcional.
- [x] 2.2 Usa GetTypeForPosition(!heroIP) cuando posición conocida.
- [x] 2.3 Usa HasReliablePreflopData en vez de IsReliable para threshold.
- [x] 2.4 Las 3 llamadas (flop/turn/river) pasan inPosition.

## 3. OpponentTracker IP/OOP

- [x] 3.1 RecordPostflopAction acepta isVillainInPosition opcional.
- [x] 3.2 Trackea contadores TimesAggressiveIP/OOP y TimesPassiveIP/OOP.
- [x] 3.3 TrackVillainPostflopAction pasa heroIsInPosition (invertido para villain).
- [x] 3.4 Llamadas de flop y turn pasan inPosition.

## 4. All-In Detection

- [x] 4.1 Detectar maxBet > 0 && villainStack <= 0 en DetermineFlopActionUnified.
- [x] 4.2 Detectar en DetermineTurnAction.
- [x] 4.3 Detectar en DetermineRiverAction.
- [x] 4.4 Pasar isAnyoneAllIn a DetermineAction en las 3 calles.

## 5. Verificación

- [x] 5.1 Build sin errores.
- [x] 5.2 514 tests pasan.
