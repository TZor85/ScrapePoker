## ADDED Requirements

### Requirement: HandScore struct sin heap allocations
SHALL existir un struct `HandScore` con `CompositeScore` (long) y `HandRank` que permite comparar manos sin crear objetos en heap.

#### Scenario: Comparación por CompositeScore
- **GIVEN** dos HandScore con diferentes ranks
- **WHEN** se comparan con CompareTo()
- **THEN** el de mayor rank gana (codificado en los bits altos del CompositeScore)

#### Scenario: Comparación de kickers
- **GIVEN** dos HandScore con mismo rank pero diferentes kickers
- **WHEN** se comparan con CompareTo()
- **THEN** el de mejor kicker gana (codificado en los bits siguientes del CompositeScore)

#### Scenario: Zero allocations en evaluación
- **GIVEN** una lista de 7 cartas
- **WHEN** se llama a EvaluateHandScore()
- **THEN** retorna un HandScore struct sin crear objetos en heap (no List, no HandEvaluation class)

### Requirement: EvaluateHandScore en IHandEvaluator
La interfaz IHandEvaluator SHALL incluir el método `EvaluateHandScore(List<CardDataOuts>)` que retorna `HandScore`.

#### Scenario: Equivalencia con EvaluateBestHand
- **GIVEN** la misma lista de 7 cartas
- **WHEN** se evalúa con EvaluateBestHand y EvaluateHandScore
- **THEN** ambos detectan el mismo HandRank
