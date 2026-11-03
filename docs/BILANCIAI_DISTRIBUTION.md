# Distribuzione desktop Bilanciai 12.1.5.1

La distribuzione prevista è **12.1.5.1**, dal ramo `bilanciai/12.1.5` del
[fork aziendale](https://github.com/Soc-Coop-Bilanciai/Avalonia). Questa guida
descrive preparazione, packaging e consumo; non attesta che la versione sia già
pubblicata. Verificare la disponibilità sul feed prima del restore dei consumer.

Il sorgente qualificato è `8f3564635b025908710dbe963e9f2e3ac7abf9b9`, basato sul
tag ufficiale **12.1.5**, commit `5825bf8c66fba0c11e67c980d8ece6444576bffa`.
Il runtime misurato resta `6d20d22cad6dc3e5fc80be6ab26d1e265806fb76`; i commit
successivi di test, documentazione e packaging non sono una nuova qualifica
prestazionale. Per ambito e limiti delle prove vedere [README](../readme.md) e
[registro patch](BILANCIAI_PATCHES.md).

## Versioni e perimetro

Il [manifest di distribuzione](../build/Bilanciai/distribution.json) definisce
base, commit qualificato, revisione, repository e feed esistente:

`https://artifacts.coopbilanciai.it/nuget/v3/index.json`

Il driver imposta solo **PackageVersion = 12.1.5.1**. `Version` e identità
assembly restano quelle ufficiali: normalmente **12.1.5.0**, con l'eccezione
intenzionale **DesignerSupport 0.7.0.0**. Non passare override globali di
`Version`, `AssemblyVersion` o `FileVersion`. Repository e commit nei pacchetti
devono identificare il commit finale realmente impacchettato, registrato nel
manifest generato, senza inventare qui il suo SHA futuro.

Restano **SkiaSharp e asset nativi 3.119.4**, **HarfBuzzSharp e asset nativi
8.3.1.3**, i framework target, la firma assembly e le policy AOT/trimming
ufficiali. Non vengono ripristinati SHM, tile, profili ARM, profiler o cap FPS
del vecchio fork. Browser, Android, iOS, WinUI e SingleProject sono esclusi dalla
traversal desktop; questo non costituisce una qualifica runtime di ogni OS
desktop o di ogni framework target incluso nei pacchetti.

## Creare i pacchetti

Prerequisiti: PowerShell 7, SDK conforme a `global.json`, submodule fissati e
popolati, sorgenti di rilascio committati e puliti, restore dalle sorgenti
approvate esistenti, cache NuGet preparata e Bun **1.3.4** già disponibile.
Il driver verifica il binario Bun tramite SHA-256; se manca o non corrisponde
si ferma, senza installare strumenti.

Dalla radice del repository, impostare esplicitamente la cache esistente e
avviare il driver in un processo senza la chiave di pubblicazione:

```powershell
$env:NUGET_PACKAGES = 'C:/Users/f.ruscelli/.nuget/packages' # cache esistente approvata
$child = [Diagnostics.ProcessStartInfo]::new('pwsh')
$child.UseShellExecute = $false
$child.WorkingDirectory = (Get-Location).Path
[void]$child.Environment.Remove('NUGET_API_KEY')
foreach ($argument in @('-NoProfile', '-File', 'build/Bilanciai/Pack.ps1')) {
    $child.ArgumentList.Add($argument)
}
$process = [Diagnostics.Process]::Start($child)
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Packaging fallito: $($process.ExitCode)" }
```

Il percorso Bun predefinito è
`$env:NUGET_PACKAGES/bun.unofficial.tool.win-x64/1.3.4/tools/any/win-x64/bun.exe`.
Per indicare esplicitamente lo stesso binario verificato, aggiungere agli
argomenti del processo `-BunExecutablePath` e il suo percorso assoluto.
`NUGET_PACKAGES` resta richiesto anche con l'override. La build degli asset
Designer rimane attiva; fuori dalla modalità desktop Bilanciai il comando Bun
upstream conserva il proprio default `dnx`.

[Pack.ps1](../build/Bilanciai/Pack.ps1) richiede la base corretta, storia lineare,
trailer `Bilanciai-Patch` validi e sorgenti puliti; controlla inoltre che runtime
e dipendenze qualificati non cambino. Invoca il target ufficiale
`CreateNugetPackages` in Release con `--bilanciai-desktop` e
`--force-nuget-version 12.1.5.1`, attraverso
[Desktop.proj](../build/Bilanciai/Desktop.proj). Restano patch dei build task,
merge Numerge e generazione delle reference assembly ufficiali: non basta
eseguire `dotnet pack` su un solo progetto.

Per macOS si riusa esclusivamente il binario della **Avalonia.Native ufficiale
12.1.5**. Il driver legge
`build-intermediate/bilanciai-native/avalonia.native.12.1.5.nupkg`, scaricando
l'archivio ufficiale solo se assente; verifica ID/versione del nuspec ed estrae
`runtimes/osx/native/libAvaloniaNative.dylib`. Registra gli hash di archivio e
dylib e confronta il binario nel pacchetto finale. Un archivio 12.1.4 non è un
sostituto valido. Il driver rifiuta differenze nei sorgenti nativi rispetto alla
base ufficiale; il riuso non è una nuova build o qualifica macOS.

L'output da distribuire è **`artifacts/nuget`**, dopo il merge ufficiale, con
`bilanciai-release.json`: versione, commit upstream/qualificato/finale, patch ID,
provenienza nativa, Bun e inventario ID/versioni/SHA-256 dei pacchetti. Non
distribuire `build-intermediate/nuget`. Il successo del comando indica creazione
e controlli del driver, non upload o compatibilità applicativa già dimostrata.
Non viene aggiunto un publisher né modificato il feed o le sue credenziali.

## Coorte desktop e dipendenze consumer

Il driver richiede **22 pacchetti desktop finali**, tutti a **12.1.5.1**. I due
grafi NextFramework e ScreenKeyboard richiedono questi **17** ID:

| Gruppo | Pacchetti |
| --- | --- |
| Runtime, 12 ID | `Avalonia`, `Avalonia.Controls.ColorPicker`, `Avalonia.Desktop`, `Avalonia.FreeDesktop`, `Avalonia.FreeDesktop.AtSpi`, `Avalonia.HarfBuzz`, `Avalonia.LinuxFramebuffer`, `Avalonia.Native`, `Avalonia.Remote.Protocol`, `Avalonia.Skia`, `Avalonia.Win32`, `Avalonia.X11` |
| Preview, benchmark e test, altri 5 ID | `Avalonia.Headless`, `Avalonia.Markup.Xaml.Loader`, `Avalonia.Fonts.Inter`, `Avalonia.Wayland`, `Avalonia.Themes.Fluent` |
| Altri 5 della coorte desktop completa | `Avalonia.Win32.Interoperability`, `Avalonia.Headless.XUnit`, `Avalonia.Headless.NUnit`, `Avalonia.Headless.Vnc`, `Avalonia.Themes.Simple` |

I pacchetti intermedi core, build task, analyzer e generatori confluiscono nei
pacchetti finali secondo le regole upstream. Non pubblicarli separatamente per
completare artificialmente l'inventario. I riferimenti interni alla coorte
devono restare coerenti alla stessa versione; le dipendenze indipendenti
mantengono le proprie versioni, fra cui `Avalonia.BuildServices` **11.3.2** e
`Avalonia.Angle.Windows.Natives` **2.1.27548.20260419**. DataGrid e librerie SVG
consumer non vengono ricostruiti da questo rilascio.

## Usare 12.1.5.1 in NextFramework e ScreenKeyboard

Dopo la conferma di pubblicazione, usare il feed aziendale già configurato e i
suoi mapping esistenti. Non aggiungere credenziali nei progetti o nei comandi.
I pattern `Avalonia.*` possono instradare anche dipendenze indipendenti: un
restore riuscito dalla vecchia cache non dimostra la disponibilità sul feed.
Verificare un restore con cache isolata e controllare gli asset effettivi.

Nei consumer correnti, passare queste proprietà **sia al restore sia a build,
test o publish** del relativo progetto:

```text
NextFramework:  -p:AvaloniaUpstreamVersion=12.1.5 -p:AvaloniaForkRevision=1
ScreenKeyboard: -p:AvaloniaPackageVersion=12.1.5.1
```

Esempio dalla radice del consumer, con progetto e configurazione approvata:

```powershell
dotnet restore $consumerProject --configfile $approvedConfig @versionProperties
dotnet build $consumerProject -c Release --no-restore @versionProperties
```

`$versionProperties` è l'array delle proprietà del consumer indicato sopra;
usare output e cache separati per confronti diversi. Dopo restore verificare
`project.assets.json`, versioni di tutti i pacchetti fork e asset nativi per il
RID scelto. Per `linux-arm64` eseguire un restore specifico per il RID prima del
publish; una build generica non ne dimostra la closure.

**NextFramework ha un vincolo SVG indipendente:**
`Svg.Controls.Skia.Avalonia` **12.0.0.17** → `Svg.Skia` **5.2.3** richiede Skia
managed/native almeno **4.148.0** e HarfBuzz **14.2.0**. Il solo aggiornamento
Avalonia a 12.1.5.1 può quindi selezionare Skia 4. Un suo build riuscito non
dimostra consumo del renderer qualificato con Skia 3. Non sopprimere `NU1605`
e non forzare versioni sotto i requisiti delle librerie SVG.

La coppia `Svg.Controls.Skia.Avalonia` **12.0.0.13** / `Svg.Skia` **5.1.1**
permette Skia **3.119.4** e HarfBuzz **8.3.1.3** come vincoli di dipendenza.
È una candidata per validazione separata di API, build, render e lifetime
dell'applicazione; non una compatibilità già certificata o una modifica
automatica di NextFramework. La variante sperimentale Skia 4 non fa parte di
questa distribuzione. ScreenKeyboard non presenta lo stesso requisito SVG
nel grafo ispezionato, ma anche il suo restore e i suoi test devono provare
le versioni realmente selezionate.

I risultati del renderer nel README rimangono riferiti ai byte e all'harness
qualificati. Packaging, restore e compilazione dei consumer non attestano
nuove misure hardware, assenza di SHM nell'intera applicazione o parità pixel
di tutte le sue schermate.
