# Avalonia Bilanciai — renderer software minimale

Fork locale basato su **Avalonia 12.1.5**, commit ufficiale
`5825bf8c66fba0c11e67c980d8ece6444576bffa`, con **SkiaSharp 3.119.4** e
**HarfBuzzSharp 8.3.1.3** ufficiali. Ramo di rilascio: `bilanciai/12.1.5`;
sorgente qualificato `8f3564635b025908710dbe963e9f2e3ac7abf9b9`, dal ramo
`bilanciai/software-minimal-12.1.5`. La distribuzione NuGet **12.1.5.1** è in
preparazione; questo documento non ne attesta ancora la pubblicazione.

La serie qualificata contiene sei commit funzionali, un commit aggiuntivo solo
test e un commit documentale. Il packaging aggiunto per 12.1.5.1 conserva quel
runtime e non estende la qualifica. I binari effettivamente misurati provengono dal sorgente
`6d20d22cad6dc3e5fc80be6ab26d1e265806fb76`; il commit test
`a3ecab8dfb3bebd8814f95d4b11250bddbff25c2` e la documentazione non ne cambiano
il runtime. SHA finale e patch esportate sono nel
manifest di consegna (percorso companion locale: `../deliverables/software-minimal-12.1.5/series.json`).

## Usare i pacchetti 12.1.5.1

La coorte desktop prevista contiene **22 pacchetti** a **12.1.5.1** sul feed
aziendale esistente `https://artifacts.coopbilanciai.it/nuget/v3/index.json`;
**17 ID** coprono i grafi NextFramework e ScreenKeyboard ispezionati.
Verificare la pubblicazione sul feed prima del restore. La
[guida di distribuzione](docs/BILANCIAI_DISTRIBUTION.md) contiene inventario,
comando reale `build/Bilanciai/Pack.ps1`, cache/Bun, provenienza nativa e controlli
sui pacchetti finali. Il driver crea i pacchetti con merge/reference assembly
upstream e non esegue upload; non viene introdotto un publisher.

Nei consumer correnti, mantenere le stesse proprietà in restore e build:

```text
NextFramework:  -p:AvaloniaUpstreamVersion=12.1.5 -p:AvaloniaForkRevision=1
ScreenKeyboard: -p:AvaloniaPackageVersion=12.1.5.1
```

**Il grafo SVG attuale di NextFramework richiede Skia >=4.148.0 e HarfBuzz
14.2.0.** Aggiornare solo Avalonia può quindi selezionare Skia 4. La coppia SVG
12.0.0.13 / Svg.Skia 5.1.1 permette la baseline Skia 3, ma richiede una qualifica
applicativa separata; non sopprimere downgrade NuGet per simulare compatibilità.
La guida distingue disponibilità dei pacchetti, restore/build e qualifica runtime.
Il rilascio imposta solo `PackageVersion`; preserva le identità assembly ufficiali,
inclusa DesignerSupport 0.7.0, e non cambia policy AOT/trimming.

Le evidenze di fase 1 indicate sotto sono percorsi locali companion non
normativi esterni al repository (`audit`, `software-minimal-audit`,
`deliverables`), non dipendenze del prodotto NuGet. Il README e il
[registro patch](docs/BILANCIAI_PATCHES.md) ne conservano il riepilogo nel
repository; la guida di distribuzione descrive il packaging corrente.

## Modifiche mantenute

| Patch | Scelta nel candidato |
| --- | --- |
| BIL-AVA-001 | Cache pigra degli outline stroke per superfici CPU note, con lifetime protetto e massimo quattro voci per geometria. Solo parte lineare unitaria, prospettiva identità, larghezza float nativa >1 pixel e niente dash; traslazioni ammesse. Hairline e casi non supportati seguono il path nativo. Bounds e hit test restano upstream. |
| BIL-AVA-007 | Lifetime della cache insieme a 001; rilascio del framebuffer quando la creazione del drawing context fallisce, in commit indipendente. |
| BIL-AVA-008 | Solo invalidazione della misura geometrica dopo modifica del path. Dipendenze e costruzione delle geometrie restano quelle ufficiali Skia 3. |
| BIL-AVA-009 | Riuso conservativo di TextLayout fra Measure e Arrange con vincoli uguali e allineamento supportato. È un miglioramento di layout/allocazioni, non una promessa di FPS. |
| BIL-AVA-010 | Ownership, reinserimento e cancellazione delle transizioni Carousel: correttezza verificata, senza claim prestazionale. |
| BIL-AVA-002 / 006 | Test di fedeltà delle ombre; README, registro, manifest e guida di manutenzione. |

Sono ritirati dal codice attivo: **SHM sperimentale** e relative opzioni/dispatcher/test
(011), sostituzione **mask blur** (002), **tile globale** con full repaint/halo
fisso (003), profiler in produzione (004), auto-default ARM, self-test runtime e
policy/cap FPS del fork (005 e parti di 007), upgrade automatico Skia 4 e migrazione
PathBuilder condizionata a quell'upgrade (008). Storia e misure restano conservate.
Le funzionalità e API pubbliche ufficiali non vengono rimosse per eliminare gli esperimenti.

L'indice completo [BILANCIAI_PATCHES.md](docs/BILANCIAI_PATCHES.md) contiene
keep/adapt/retire, commit, dipendenze e prove per tutti gli ID. Il
registro dei 26 commit originali e sei follow-up (percorso companion locale: `../audit/commit-inventory.md`)
comprende anche packaging, wrapper e documentazione storica.

## Ombra del navigator

Il gradiente apparente sotto la barra è un `BoxShadow`: barra alta 60 DIP,
`0 0 12 0 #AA000000`, clipping disabilitato. BIL-AVA-002 sostituiva l'image blur
con una maschera, alterando la caduta dell'ombra; non cambiava radius→sigma.
La sostituzione è ritirata e si usa il filtro immagine upstream. La prova locale
a Skia invariato rilevava 12.280 pixel diversi, errore massimo 6.

Ripristinare l'immagine corretta ha un costo: nel vecchio fork seriale il
navigator mostra −6,07% throughput e +15,16% CPU/frame rispetto alla maschera,
mediane di tre rapporti appaiati. È una coorte storica distinta dal nuovo
candidato. Nessuna modifica NextFramework nasconde il difetto del renderer.
Diagnosi, prima/dopo e raw (percorso companion locale: `../software-minimal-audit/REPORT.md`).

## Risultati su cb-next

Harness indipendente da NextFramework, eseguito via RemoteAutoPilot su ARM64
i.MX8M Mini, .NET 10.0.14, framebuffer seriale retained BGRA8888/Premul,
1024×600 e scala 1. Timer X11 upstream osservato **61 Hz**, richieste UI **24 Hz**;
gli FPS sotto sono frame completati fino alla submission client, non refresh del
pannello. `target_ms` include update/render/readback, non il solo raster.

**18/18 run frame**, tre repliche alternate per confronto:

| Confronto geometria | Throughput | CPU core-ms/frame | Latenza p95 fino alla submission client |
| --- | ---: | ---: | ---: |
| Cache Skia3 on/off, stessi byte | +20,70% [17,48;21,09] | −13,35% [−14,35;−9,71] | −11,71% [−14,01;−8,54] |
| Candidato Skia3 rispetto a Skia4 | +44,16% [41,26;44,74] | −25,01% [−26,25;−20,84] | −19,34% [−25,94;−16,41] |

Percentuali = mediana di tre rapporti appaiati, intervalli = minimo/massimo
delle tre coppie; non rapporto fra mediane né prova di significatività statistica.
CPU 100% equivale a un core. Cache3 completa più frame: CPU app/secondo +4,60%,
pur riducendo il costo per frame. Nel testo la cache osservata è zero; throughput
on/off +0,60% con intervallo −5,89..+9,70%: nessun beneficio o regressione generale
attribuito alla cache. Il confronto3/4 include l'intero livello managed/API/native.

**TextBlock: 12 processi, tre blocchi ABBA**, 1.000 controlli per batch:

| Caso riutilizzabile | Tempo A/B ms | Delta wall [min;max] | Delta allocazioni thread |
| --- | ---: | ---: | ---: |
| Left/LTR | 851,58 / 788,48 | −6,99% [−9,99;−6,82] | −11,82% |
| Left/RTL | 611,76 / 546,53 | −10,66% [−17,29;−10,05] | −8,81% |
| Start/LTR | 522,61 / 478,48 | −8,34% [−10,76;−6,12] | −11,82% |

I quattro fallback conservano le allocazioni e hanno tempi sostanzialmente
invariati. Sette geometrie/PNG coincidono nei dodici processi e sono ispezionati,
inclusi glifi ebraici. Questi snapshot CPU non misurano FPS o scanout.

**8/8 catture desktop reali**: confronto esatto ufficiale3/candidato3 su navigator,
geometrie e gradienti: **zero pixel differenti**. Tutti i PNG sono legati a raw,
hash caricati e identità RAP, e sono stati ispezionati. La parità riguarda il
viewport catturato: contenuto a (32,32), ultimi 32 pixel client destra/basso
fuori schermo. ROI ombra corretta y92..112, zero differenze. La variante4 non è
equivalente: 8 pixel/max1 sul pulsante navigator, 91.903/max192 nelle geometrie.

Report conclusivo (percorso companion locale: `../software-minimal-audit/REPORT.md`) ·
Frame/raw/ranges (percorso companion locale: `../software-minimal-audit/results-frame/N3-METRICS.md`) ·
ABBA (percorso companion locale: `../software-minimal-audit/results-layout/LAYOUT_REPORT.md`) ·
QA e immagini (percorso companion locale: `../software-minimal-audit/visual-diffs-v2/VISUAL.md`) ·
ispezione effettiva registrata (percorso companion locale: `../software-minimal-audit/visual-diffs-v2/INSPECTION-software-analysis.md`).
Le temperature registrate sono 58–63°C nelle nuove misure frame; frequenza,
governor, throttling e inventario globale dei processi non sono raccolti.
Non si escludono carichi esterni o throttling e non si generalizza a ogni scena.

## Build, test e configurazione

`global.json` richiede SDK **10.0.201**, `rollForward=latestFeature`, con
Microsoft.Testing.Platform. La qualifica locale usa net10.0 Windows x64; il
target usa .NET 10.0.14 ARM64. Versioni pacchetti in
[Directory.Packages.props](Directory.Packages.props); istruzioni upstream in
[docs/build.md](docs/build.md), procedura del fork in
[BILANCIAI_SOFTWARE_PORT.md](docs/BILANCIAI_SOFTWARE_PORT.md).

Per un controllo locale con cache e sorgenti NuGet già disponibili, impostare
`$taskNuGetConfig` e `$taskPackages` ai percorsi approvati esistenti, poi:

```powershell
dotnet restore tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj --configfile $taskNuGetConfig -p:RestorePackagesPath=$taskPackages -p:AvsSkipBuildingLegacyTargetFrameworks=True -p:NuGetAudit=false
dotnet build tests/Avalonia.Skia.UnitTests/Avalonia.Skia.UnitTests.csproj -c Release --no-restore -p:RestorePackagesPath=$taskPackages -p:AvsSkipBuildingLegacyTargetFrameworks=True -p:DisableSourceLink=true
dotnet tests/Avalonia.Skia.UnitTests/bin/Release/net10.0/Avalonia.Skia.UnitTests.dll --report-xunit --report-xunit-filename all-skia.xml --results-directory ./artifacts/local-tests
```

Ripetere restore/build per Controls UnitTests e
`src/Avalonia.Desktop/Avalonia.Desktop.csproj`; i filtri mirati sono
`TextBlockTests*`, `CarouselTests*`, `VirtualizingCarouselPanelTests*`.
La suite completa congelata a6d20 è **622 pass + uno skip preesistente**;
Controls **141/141**, API strict **38/38** (17 contro official3 e21 contro baseline
Desktop pulita Skia4), assenza/policy **14/14**, 21 asset Skia identici all'ufficiale3.
Tre nuovi test passano separatamente sugli stessi binari; non si rinomina la
suite precedente. Build zero warning/errori. Log, XML e hash (percorso companion locale: `../audit/build-minimal3/VALIDATION.md`).

La cache è attiva per default nei casi qualificati. Per A/B, in un **nuovo processo**:

```powershell
$env:AVALONIA_STROKE_CACHE = '0' # disabilita; assente o '1' abilita
```

La suite funzionale è eseguita con il default attivo; ripristinare la variabile
prima di lanciarla. Le prove X11 usano questa configurazione upstream nel
bootstrap dell'applicazione:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new X11PlatformOptions
    {
        RenderingMode = [X11RenderingMode.Software],
        UseRetainedFramebuffer = true
    });
```

Il frame rate resta policy upstream/applicazione. Non esistono opzioni attive del
fork per SHM, mask blur, raster workers/tiles, profiler o cap FPS. Il benchmark
e le sue variabili di controllo non fanno parte del prodotto distribuito.

Non qualificati: suite Controls completa, TFM legacy/net8, altre piattaforme,
NativeAOT/trimming e intero grafo dell'app. Nessun nuovo divieto AOT è introdotto.
I pacchetti pubblici upstream non contengono queste patch. La distribuzione
12.1.5.1 usa una closure desktop coerente e il feed aziendale esistente, come
descritto nella [guida di distribuzione](docs/BILANCIAI_DISTRIBUTION.md).
Il driver di packaging non aggiunge feed, credenziali o un publisher.

## Porting e integrazione

Applicare i commit selezionati con `git cherry-pick -x` nell'ordine dell'indice;
verificare prima equivalenze upstream e dipendenze. 001 è autonomo; 008 richiede
testualmente001. Il commit test richiede001 e il fix unlock007. Il replay di ogni
albero intermedio e della serie completa è verificato. I trailer
`Bilanciai-Patch: BIL-AVA-NNN` restano stabili nei port; non riprodurre le patch
ritirate. Aggiornare manifest e qualifica a ogni nuova versione Avalonia.

Skia3 è il default preferito. Il livello opzionale Skia4 è un solo commit
`a026731c9007207c47f15e7db9f5799894b294e3`, nel ramo separato
`bilanciai/software-minimal-12.1.5-skia4`; riproduce il runtime di riferimento62493.
Se selezionato, aggiornare i metadati della propria distribuzione e riqualificare
l'integrazione. Lo stack SVG attuale NextFramework (`Svg.Controls.Skia.Avalonia`
12.0.0.17 / Svg.Skia5.2.3) richiede Skia >=4.148.0. La cache contiene il candidato
control12.0.0.13 / Svg.Skia5.1.1 con minimo3.119.2, ma la compatibilità dell'app
non è certificata da questi test del renderer. Non forzare la risoluzione NuGet
come sostituto dei test API/asset/pixel/lifetime. Evidenze dipendenze (percorso companion locale: `../audit/skia4-dependency-necessity.md`).

Originale12.1.4 `acf6d55b`, port storico `b666e9b`, fix ombre `d8431f1`, vecchi
117 benchmark e coorti successive rimangono preservati e distinti. La fase 1
si è conclusa senza push, release o upload. La sessione test cb-next è stata
rilasciata alle22:34UTC. La preparazione della distribuzione 12.1.5.1 è una fase
successiva: non rinomina le misure storiche né implica un'integrazione
NextFramework già qualificata. L'ulteriore audit Avalonia/SkiaSharp è coordinato
separatamente.

## Progetto upstream, attribuzioni e risorse

[Avalonia](https://avaloniaui.net/) è il framework .NET/XAML multipiattaforma
da cui deriva questo fork. Codice, autori e contributori upstream restano
riconosciuti; licenza **[MIT](licence.md)** e [NOTICE](NOTICE.md) restano invariati.
Il README originale è conservato nella storia del tag12.1.5.

- [Documentazione](https://docs.avaloniaui.net/), [Getting Started](https://docs.avaloniaui.net/docs/get-started), [tutorial](https://docs.avaloniaui.net/docs/get-started/starter-tutorial), [samples](https://github.com/AvaloniaUI/Avalonia.Samples), [breaking changes12](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes) e [confronto WPF](https://docs.avaloniaui.net/docs/next/get-started/wpf/).
- Strumenti upstream: [VS Code](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.vscode-avalonia), [Visual Studio](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS), [Rider](https://www.jetbrains.com/rider/) e [AvaloniaRider](https://github.com/ForNeVeR/AvaloniaRider).
- [Blog Avalonia](https://www.avaloniaui.net/blog), [NuGet upstream](https://www.nuget.org/packages/Avalonia), [nightly upstream](https://github.com/AvaloniaUI/Avalonia/wiki/Using-nightly-build-feed): riferimenti al progetto originale, non versioni o canali di questo candidato.
- [Contributi](CONTRIBUTING.md), [Code of Conduct](CODE_OF_CONDUCT.md), [sponsorship](https://github.com/sponsors/AvaloniaUI) e sponsor upstream [Devolutions](https://devolutions.net/).
- Segnalazioni di sicurezza upstream: [security@avaloniaui.net](mailto:security@avaloniaui.net), con disclosure responsabile come descritto dal progetto.
- [Avalonia XPF](https://avaloniaui.net/xpf) è il prodotto commerciale upstream per WPF, distinto da questo fork.
