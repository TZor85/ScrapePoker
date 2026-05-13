# Flowchart — `FrmMain` (4502 LOC)

> Form WinForms gigante: composition consumer de DI, captura, pipeline de juego, 5 tabs UI, 95+ métodos.
> Generado por el Arqueólogo del Reversa.

## Inventario de tabs y sus eventos

```mermaid
flowchart LR
    FM[FrmMain<br/>5 tabs en tbControl]

    FM --> Juego[Juego<br/>btnCapture / btnWindow<br/>cbMark, cbTest, cbSpeed<br/>rb Flop/Turn/River]
    FM --> Config[Config<br/>twRegionsConfig TreeView<br/>tb X,Y,W,H + Color/Umbrales<br/>btn ↑↓←→ +/− W/H<br/>btnSaveMap, btnTest Color/Texto/Carta]
    FM --> Tablas[Tablas<br/>twTables TreeView jerárquico<br/>HeroPosition → posiciones]
    FM --> Logs[Logs<br/>tbResume TextBox<br/>format ═══ FLOP/TURN/RIVER ═══]
    FM --> Historial[Historial<br/>dgvSessions + dgvSessionHands<br/>btnBacktest A/B<br/>FrmHandDetail popup]
    FM --> Bankroll[Bankroll<br/>dgvBankrollHistory<br/>UpdateBankrollDashboard]
    FM --> Metrics[Métricas<br/>dgvMetrics 9 cols<br/>Timer 1s refresh<br/>btnResetMetrics]

    Juego --> BtnCapture[btnCapture_Click<br/>~270 LOC pipeline principal]
    Juego --> BtnWindow[btnWindow_Click<br/>FindWindow + start BackgroundWorker]
    Juego --> BgWorker[BackgroundWorker1_DoWork<br/>~165 LOC loop polling]
```

## Inventario de estado mutable

> Documentado dentro del propio FrmMain.cs:4448-4500 con plan de migración a Fase 7.3.

```mermaid
flowchart TB
    subgraph LoopState["Control del loop → GameLoopCoordinator"]
        executeCapture[volatile _executeCapture]
        backgroundExecute[volatile _backgroundExecute]
        speed[_speed]
    end

    subgraph CrossStreet["Cross-street → PostflopGameContext"]
        heroStack[_heroStackPreRebuy<br/>auto-rebuy detection]
        newHand[_newHand bool]
        tableHand[_tableHand string]
        previousSB[_previousSBPlayerName]
        previousBB[_previousBBPlayerName]
        lastActive[_lastActivePlayerCount]
        flopResult[_flopResult]
        turnResult[_turnResult]
        riverResult[_riverResult]
        responseAction[_responseAction]
    end

    subgraph TableRead["Lectura de mesa → ITableLayoutService"]
        playerGameState[_playerGameState]
        handle[_handle IntPtr]
        tableName[_tableName]
        session[_session]
    end

    subgraph PureUI["UI Pura → mantiene en FrmMain"]
        lastChecked[_lastChecked RadioButton]
        img[_img Image]
        isClosing[_isClosing]
        historialLoaded[_historialLoaded]
        bankrollLoaded[_bankrollLoaded]
    end
```

## Constructor — Inyección de dependencias (44 parámetros)

```mermaid
flowchart TD
    Ctor([FrmMain ctor 44 servicios]) --> Validate[Null-check todos con throw ArgumentNullException<br/>via ?? throw new ArgumentNullException nameof X]
    Validate --> SetTextBox[textBoxLoggerProvider.SetTextBoxTarget tbResume<br/>logger empieza a flush a UI]
    SetTextBox --> Init[InitializeComponent WinForms designer]
    Init --> Random[GenerateRandomNumbers _session]
    Random --> Closing[FormClosing += FrmMain_FormClosing]
    Closing --> InitTabs[InitializeHistorialTab<br/>InitializeBankrollTab<br/>InitializeMetricsTab]
```

## FrmMain_Load (cuando se muestra)

```mermaid
flowchart TD
    Load([FrmMain_Load]) --> CreateForms[_formImage = new FormImage<br/>_frmOverlay = new FrmOverlay overlayConfig<br/>cbSpeed.SelectedIndex = 0]
    CreateForms --> Session[await using sessionDB = LightweightSession]
    Session --> LoadRegions[LoadRegionTableMapAsync sessionDB<br/>Query RegionTableMap<br/>regionLookupCache.Initialize maps<br/>LoadTreeViewRegions]
    LoadRegions --> LoadTables[LoadTablesAsync sessionDB<br/>Query Table<br/>LoadTreeViewTables tablas → posiciones]
    LoadTables --> LoadCards[Query Card all<br/>_cards.AddRange]
    LoadCards --> ShowImage[_formImage.Location = right of frmMain<br/>_formImage.Show]
    ShowImage --> UpdateBank[UpdateBankrollDashboard]
```

## FrmMain_FormClosing (persistencia al cerrar)

```mermaid
flowchart TD
    Closing([FrmMain_FormClosing]) --> ClosingFlag{_isClosing already?}
    ClosingFlag -->|Yes| Return[return]
    ClosingFlag -->|No| LoopRunning{gameLoopCoordinator.IsRunning?}

    LoopRunning -->|Yes| StopLoop[uiSyncService.Detach<br/>_gameLoopCts.Cancel<br/>await coordinator.StopAsync]
    LoopRunning -->|No| ActiveCheck

    StopLoop --> ActiveCheck{HasActiveHand OR HasActiveSession?}
    ActiveCheck -->|No| Allow[allow close natural]
    ActiveCheck -->|Yes| Cancel[e.Cancel = true<br/>_isClosing = true]

    Cancel --> EndHand[Si HasActiveHand:<br/>closingStack = _heroStackPreRebuy or HeroStack<br/>gameLogger.EndHand closingStack]
    EndHand --> SaveSession[Si HasActiveSession:<br/>await SaveSessionAsync]
    SaveSession --> ReClose[Close ahora _isClosing=true → return inmediato]
```

## btnCapture_Click — Pipeline principal (canónico)

> Ya documentado en `OpenScrape.App.md`. Resumen:
>
> 1. Métricas: `cycleTimer = metrics.Measure CycleTotal`
> 2. Limpiar overlay y `_executeCapture = true`
> 3. **No-test:** `GetImageWhilePlaying` → captura, save PNG, init scaler en primera vez
> 4. **Test postflop:** `ForceState` Flop/Turn/River
> 5. `SetTableHand` (OCR pot, holes, table name, hand number)
> 6. `DetectNewHand` con 7 indicadores (handChange + 3-of-6 secundarios)
> 7. Si nueva mano: snapshot postflop si misma mano, reset PlayerGameState, restaurar estado
> 8. `HandleNewHandAsync` (EndHand previa + SaveSession + Tracker.RecordHand/VPIP/PFR + StartNewHandAsync)
> 9. Inicializar/refrescar jugadores via `tableLayout`
> 10. `SetBetPlayer` + `SetHeroStack` (con auto-rebuy detect) + `RetryEmptyAliases`
> 11. `ProcessTableInfoAsync`: rama preflop o postflop con retries para hole cards
> 12. **Postflop:** `ProcessFlopAsync`/`ProcessTurnAsync`/`ProcessRiverAsync` (cada uno OCR cards 3 retries → `pokerCalculator.Calculate` → `coordinator.DetermineXxxAction`)
> 13. Overlay update + log final

## ProcessFlopAsync (canónico)

```mermaid
flowchart TD
    Start([ProcessFlopAsync potOddsResult]) --> Loop{attempt ≤ MaxOcrRetries 2?}
    Loop -->|Yes| OCR[await getCardsFlopUseCase.ExecuteAsync<br/>regions Board.Card1/2/3<br/>dHash compare vs CardCache 52]
    OCR --> CheckCount{flopCards >= 3?}
    CheckCount -->|Yes| Break[break]
    CheckCount -->|No & retry| Wait[Delay 200ms; attempt++]
    Wait --> Loop
    CheckCount -->|No & last| Continue

    Break --> AssignBoard[playerGameState.BoardCards = dataBoard<br/>Add hole cards Position=Hand]
    Continue --> AssignBoard

    AssignBoard --> SetForceBoard[setFlopForceBoardUseCase.Execute<br/>BoardTexture/HeroStrength/Draws]
    SetForceBoard --> CheckCount2{flopCards >= 3?}
    CheckCount2 -->|No| Err[ResponseAction = Error<br/>UpdateOverlayWithPotOdds empty<br/>return]
    CheckCount2 -->|Yes| Calc

    Calc[pokerCalculator.Calculate<br/>myCards 2, communityCards 3<br/>potSize, maxBet, isInPosition, heroStack, villainStack<br/>handSituation, opponentProfile=GetActiveVillainProfile]
    Calc --> Save[_flopResult = result<br/>UpdateOverlayWithPotOdds]
    Save --> SetIP[tableLayout.SetIsInPosition state]
    SetIP --> Determine[DetermineFlopActionUnified<br/>coordinator.SetFlopResult<br/>coordinator.DetermineFlopAction state<br/>responseAction.Action = result.Action<br/>logger.LogError result.LogText]

    Determine --> LogPersist[GameLogger.UpdateBoard flopCardNames<br/>LogStreetDecision con eq/odds/EV/maxBet/SPR<br/>UpdateSituation]
```

## SetTableHand — Detección de nueva mano

```mermaid
flowchart TD
    Start([SetTableHand]) --> NullState{_playerGameState null?}
    NullState -->|Yes| New[_playerGameState = new]
    NullState -->|No| Pot

    New --> Pot[SetPotValue]
    Pot --> Holes[await ObtainCardsPlayerAsync<br/>dHash compare cartas hero]
    Holes --> RegHand[regionTableHand = lookupCache 'Table' 'tablehand']
    RegHand --> RegCheck{region null?}
    RegCheck -->|Yes| TableName[skip a tableName]
    RegCheck -->|No| FirstRead

    FirstRead{_tableHand vacío?}
    FirstRead -->|Yes| Initial[_tableHand = ReadHandNumber<br/>_newHand = true<br/>metrics.StartHand<br/>_cycleCounter++]
    FirstRead -->|No| ReadCurrent[currentHand = ReadHandNumber]

    ReadCurrent --> Parse[long.TryParse old/new]
    Parse --> ParseOK{both parse?}
    ParseOK -->|Yes| ChangeChk{old != new OR new=0?}
    ChangeChk -->|Yes| Suspicious[handNumberChanged=true]
    ChangeChk -->|No| Skip
    ParseOK -->|No| TextCmp{currentHand != _tableHand?}
    TextCmp -->|Yes| Suspicious
    TextCmp -->|No| Skip

    Suspicious --> Postflop{IsFlop OR IsTurn OR IsRiver?}
    Postflop -->|Yes| Heuristic[ratio = new/old<br/>lengthDiffers OR ratio > 100 OR < 0.01<br/>= suspicious]
    Postflop -->|No| Trust

    Heuristic --> Sus{suspiciousOcrChange?}
    Sus -->|Yes| DetectNew[_newHand = DetectNewHand false, currentHand<br/>requiere 3+ secundarios]
    Sus -->|No| Trust

    Trust[_newHand = DetectNewHand true, currentHand<br/>1+ secundario]
    Trust --> Update[Si _newHand:<br/>_tableHand = newOrCurrentOrCounter++]
    DetectNew --> Update

    Update --> TableName[regionTableName = 'Table' 'tablename'<br/>si _tableName vacío:<br/>_tableName = ReadText sin números]
    Initial --> TableName
    Skip --> TableName
```

## Backtest A/B (Historial tab)

```mermaid
flowchart TD
    Start([BtnBacktest_Click]) --> Disable[btnBacktest.Enabled = false<br/>btnBacktest.Text = 'Analizando']
    Disable --> Hands[await GetRecentHandsAsync 500]
    Hands --> Empty{hands vacío?}
    Empty -->|Yes| Msg1[MessageBox 'No hay manos'<br/>finally re-enable]
    Empty -->|No| BB[bigBlind = loadedSessions first BigBlind ?? 0.50m]
    BB --> Backtester[new StrategyBacktester postflopDecisionService<br/>backtester.RunBacktest hands, bigBlind]
    Backtester --> Show[MessageBox result.ToString<br/>finally re-enable]
```

## Métricas tab — Refresh loop

```mermaid
flowchart TD
    Init([InitializeMetricsTab]) --> Cols[dgvMetrics.Columns.Add Fase + 8x cols<br/>Last/Session × P50/P95/Max/Count]
    Cols --> Rows[For cat in TelemetryCategories.DisplayOrder:<br/>Rows.Add cat, '—'×4, '0', '—'×3, '0'<br/>row.Tag = cat]
    Rows --> Timer[_metricsRefreshTimer = Timer interval=1000<br/>Tick → RefreshMetricsGrid]
    Timer --> Hook[tbControl.Selected += TabControl_Selected]

    Selected([TabControl_Selected]) --> CheckTab{e.TabPage == tabMetrics?}
    CheckTab -->|Yes| Refresh[RefreshMetricsGrid; Timer.Start]
    CheckTab -->|No| Stop[Timer.Stop]

    Refresh([RefreshMetricsGrid]) --> Snap[_metrics.SnapshotSession]
    Snap --> Header[lblCurrentHand = HandId<br/>lblCycleCount<br/>lblLastUpdate]
    Header --> Loop{For row in Rows}
    Loop --> Cat[category = row.Tag]
    Cat --> Last[FillCells row, 1, snap.LastHand cat]
    Last --> Sess[FillCells row, 5, snap.Session cat]
    Sess --> Loop
```

## Anomalías y deuda técnica críticas

🔴 **Críticas:**
- **God Class** `FrmMain` 4502 LOC con 95+ métodos. El propio archivo documenta inventario de migración pendiente al `GameLoopCoordinator` (refactor `refactor-frmmain-coordinators` Fases 6-7).
- **`SaveReferenceDimensionsToConfig`** reescribe `appsettings.json` mediante string-building manual (no roundtrip JSON real). Si una clave contiene `,` o se reordena, puede corromper el archivo.
- **`BackgroundWorker1_DoWork`** loop infinito `while(true)` sin `cancellationToken`; sólo sale por `e.Cancel = true` desde subthread o si `_frmOverlay` no es visible. Pendiente de migración a `IGameLoopCoordinator` (skeleton presente, feature flag OFF).
- **`btnCapture_Click`** método de ~270 LOC con responsabilidades mixtas: captura, OCR, lifecycle de manos, decisiones, persistencia, overlay update. Es el "main" del game loop.

🟡 **Medias:**
- **Hardcoded path** en `GetImageWhilePlaying`: `"C:", "Code", "Poker", "ScrapePoker", "resources", "Games"`. Debería derivar de `AppDomain.CurrentDomain.BaseDirectory` o config.
- `ProcessRiverAsync` cuenta cartas con `!string.IsNullOrEmpty(d.Name)` mientras `ProcessFlopAsync`/`ProcessTurnAsync` cuentan por `Position == BoardPosition.Flop` o `count >= 4`. Inconsistencia post-fix OCR-3.
- `_handle == IntPtr.Zero` se chequea en algunos métodos pero no en otros (posible NRE en P-Invoke si la ventana del poker se cerró).
- `tbResume.Text` se acumula sin truncar dentro de `HandleNewHandAsync` (`File.AppendAllTextAsync(path, tbResume.Text)`). Si crece mucho, escribe todo el log cada mano (write amplification).

🟢 **Bajas:**
- Comentarios `//portatil` legacy y `//folderPath = @"C:\Code\ScrapePoker\..."` en `FormImage.cs`.
- `_papel = _formImage.pbImage.CreateGraphics()` (`UpdateRegionDisplay`) crea Graphics sin disposar fuera del using.
- `MaximumSize = new Size(500, 400)` hardcoded en `FrmOverlay`.
- Mezcla `_logger.LogError` / `_logger.LogInformation` / `_logger.LogDebug` / `Console.WriteLine` indistintamente (existe `TextBoxLogger` correlated, pero conviven con `Debug.WriteLine`).
