# Mini Alert Engine

Saatlik fiyat verilerini gezip JSON ile tanımlanmış kuralları değerlendiren,
eşleşmelerde konsola uyarı basan minimal bir alarm motoru. DB, ağ çağrısı veya UI
içermez; saf C# mantığı ve birim testlerinden oluşur.

> **Not:** Projeler `net10.0` hedefler (geliştirme ortamında yalnızca .NET 10 SDK
> kurulu olduğundan). Kod, .NET 8 ile tamamen uyumlu API'ler kullanır; .NET 8 SDK
> olan bir ortamda tüm `.csproj` dosyalarındaki `TargetFramework` değeri
> `net8.0` yapılarak derlenebilir.

## Projeler

| Proje | Tür | Amaç |
|---|---|---|
| `miniAlertEngine.Engine` | Class Library | Kural motoru: `IRule`, kural implementasyonları, `AlertEngine` |
| `miniAlertEngine` | Console App | CLI giriş noktası: JSON dosyalarını okur, motoru çalıştırır |
| `miniAlertEngine.Tests` | xUnit | Kural mantığının birim testleri |

## Derleme

Çözümün kök dizininde:

```powershell
dotnet build miniAlertEngine.slnx
```

Release derlemesi için:

```powershell
dotnet build miniAlertEngine.slnx -c Release
```

## Çalıştırma

```powershell
dotnet run --project miniAlertEngine -- samples/prices.json samples/rules.json
```

Derlenmiş çıktıyı doğrudan çalıştırmak için:

```powershell
.\miniAlertEngine\bin\Debug\net10.0\miniAlertEngine.exe samples\prices.json samples\rules.json
```

Program iki argüman alır: **fiyatlar** JSON dosyası ve **kurallar** JSON dosyası.
Fiyatlar zamana göre sıralanır ve saat saat gezilir; her eşleşmede şu formatta
çıktı basılır:

```
[2024-01-01 11:00] too-high: Fiyat eşik değerin üstünde (>110) (price: 115.0)
```

## JSON Formatları

**prices.json**

```json
[
  { "time": "2024-01-01T09:00:00+03:00", "price": 100.0 }
]
```

**rules.json**

```json
[
  { "id": "too-high",  "type": "threshold", "operator": "gt", "value": 110 },
  { "id": "too-low",   "type": "threshold", "operator": "lt", "value": 90 },
  { "id": "fast-move", "type": "change",    "percent": 5 },
  { "id": "safe-band", "type": "range",     "min": 95, "max": 105 },
  {
    "id": "spike-outside-band",
    "type": "and",
    "rules": [
      { "type": "change", "percent": 10 },
      { "type": "range", "min": 95, "max": 105 }
    ]
  },
  {
    "id": "calm-market",
    "type": "not",
    "rule": { "type": "change", "percent": 5 }
  }
]
```

## Kural Tipleri

| Tip | Alanlar | Davranış |
|---|---|---|
| `threshold` | `operator` (`gt`/`lt`), `value` | Fiyat eşiğin üstüne çıkınca (`gt`) veya altına inince (`lt`) eşleşir. Eşitlik eşleşmez. |
| `change` | `percent` | Fiyat bir önceki saate göre mutlak olarak `%percent` veya daha fazla değişince eşleşir (yön bağımsız). |
| `range` | `min`, `max` | Fiyat `[min, max]` bandının dışına çıkınca eşleşir; sınır değerler bandın içinde sayılır. |
| `and` | `rules` (dizi) | İçindeki **tüm** kurallar eşleşince eşleşir. |
| `or` | `rules` (dizi) | İçindeki kurallardan **en az biri** eşleşince eşleşir. |
| `not` | `rule` (tekil) | İçindeki kural **eşleşmediğinde** eşleşir. |
| `streak` | `hours`, `direction` (`up`/`down`) | Fiyat üst üste `hours` kadar **geçiş** boyunca aynı yönde hareket edince eşleşir. |
| `cooldown` | `hours`, `rule` (tekil) | İç kuralın bildirim sıklığını sınırlar: en fazla `hours` saatte bir basılır. |

## Birleşik Kurallar (and / or / not)

Birleşim kuralları **sınırsız derinlikte** iç içe geçebilir (`and` içinde `or`,
onun içinde `not` vb.):

```json
{
  "id": "extreme-zone",
  "type": "or",
  "rules": [
    { "type": "threshold", "operator": "gt", "value": 120 },
    {
      "type": "and",
      "rules": [
        { "type": "threshold", "operator": "lt", "value": 95 },
        { "type": "not", "rule": { "type": "range", "min": 90, "max": 110 } }
      ]
    }
  ]
}
```

Kurallar:

- **`id` yalnızca kök kurallarda zorunludur.** İç kurallar `id` ve mesaj taşımaz;
  fabrika onlara anonim kimlik atar.
- **İç kurallar asla kendi başına bildirim basmaz.** Değerlendirme sonuçları
  yalnızca birleşimin kararı için sinyal olarak kullanılır; eşleşme durumunda tek
  bildirim, kök kuralın `id`'siyle üretilir. Bu davranış
  `CompositeRuleTests.Motor_IcKurallarAslaKendiBasinaBildirimBasmaz` testiyle
  sabitlenmiştir.
- `change` gibi önceki fiyata bağımlı kurallar birleşim içinde de aynı "bir
  önceki saat" değerini alır. Dolayısıyla ilk saatte `not(change)` eşleşir,
  çünkü `change` ilk saatte hiçbir zaman eşleşmez (aşağıdaki tasarım kararına
  bakınız).

## Durum Bilen Kurallar (streak / cooldown)

Bu kurallar saatler arası **state** gerektirir. Motor her çalıştırmada bir
`EvaluationContext` oluşturur; durum, kural `id`'si başına izole tutulur ve
çalıştırmalar arasında sıfırlanır. Durumsuz kurallar bağlamı yok sayar
(`IRule`'daki varsayılan metot sayesinde geriye dönük uyumluluk korunur).

**streak** — örnek:

```json
{ "id": "down-trend", "type": "streak", "hours": 2, "direction": "down" }
```

- `hours` **geçiş sayısıdır**: `hours=5` için 6 gözlem (5 ardışık hareket)
  gerekir; 5. saatte eşleşir. Bu, `change` kuralının "ilk saat yok" semantiğiyle
  tutarlıdır.
- **Sabit kalan saat seriyi kırar** (sıfır hareket ne `up` ne `down` sayılır).
- Eşik aşıldıktan sonra seri sürdükçe her saat eşleşmeye devam eder.

**cooldown** — örnek:

```json
{
  "id": "band-alert-limited",
  "type": "cooldown",
  "hours": 3,
  "rule": { "type": "range", "min": 95, "max": 105 }
}
```

- **İlk eşleşme her zaman basılır.**
- Sonrasında, son **basılan** bildirimden itibaren `hours` saat dolmadan tekrar
  basılmaz; yutulan eşleşmeler sayacı ilerletmez.
- Bildirim kök kuralın `id`'siyle basılır; iç kural kendi başına bildirim
  üretmez (Bölüm 2'deki birleşim kuralıyla aynı prensip).

## Tasarım Kararı: `change` Kuralı İlk Saatte Ne Yapar?

**Karar:** İlk fiyat noktasında `change` kuralı **asla eşleşmez** (sessiz başlangıç).

**Gerekçe:** Yüzde değişim iki gözlem gerektirir: `(şimdiki - önceki) / önceki`.
İlk saatte "önceki" yoktur. Üç alternatif değerlendirildi:

1. **İlk saati atlamak (seçilen):** Deterministik, yanlış pozitif üretmez ve
   "veri yoksa karar da yok" ilkesine uyar. Testle sabitlenmiştir
   (`ChangeRuleTests.IlkSaat_OncekiFiyatYok_Eslesmez`).
2. İlk saati %0 değişim saymak: Her zaman eşleşmemekle aynı sonucu verir ama
   gerçek dışı bir "değişim yok" iddiası taşır.
3. İlk saati her zaman eşleşme saymak: Gerçek bir değişim olmadan alarm üretir;
   yanlış pozitif.

Aynı nedenle önceki fiyat `0` ise (sıfıra bölme) kural yine eşleşmez.

## Testler

```powershell
dotnet test miniAlertEngine.slnx
```

Testler xUnit ile yazılmıştır; her kural tipi için ayrı test sınıfı vardır
(`ThresholdRuleTests`, `ChangeRuleTests`, `RangeRuleTests`, `StreakRuleTests`,
`CooldownRuleTests`, `CompositeRuleTests`, `AlertEngineTests`). Yalnızca belirli
bir sınıfı çalıştırmak için:

```powershell
dotnet test --filter "FullyQualifiedName~CooldownRuleTests"
```

## Bölüm 4 — Teknik Analiz Soruları

### S1: Motor saniyede 10.000 fiyat güncellemesi alırsa sistem tasarımında neler, neden değişirdi?

Mevcut tasarım **tek seferlik toplu işlemdir**: dosyadan oku → sırala → tek iş
parçacığında tüm kuralları sırayla çalıştır → konsola yaz. 10.000 mesaj/sn'lik
sürekli bir akışta bu varsayımların tamamı bozulur:

1. **Batch → streaming.** Dosya okuma yerine kalıcı bir giriş akışı gerekir
   (Kafka, Azure Event Hubs, Redis Stream, gRPC stream). `IEnumerable<PricePoint>`
   yerine `IAsyncEnumerable<PricePoint>` veya `System.Threading.Channels` tabanlı
   bir pipeline kurulur; `AlertEngine.Run` push-model bir tüketiciye dönüşür.
   Böylece **backpressure** (kanal dolunca üreticiyi yavaşlatma) doğal olarak
   kazanılır.

2. **Durumun yaşam süresi ve kalıcılığı.** Şu an state her `Run` çağrısında
   sıfırlanıyor; akış sonsuz olduğundan state **kalıcı** olmalıdır.
   `EvaluationContext`'in in-memory `Dictionary`'si yerine kural kimliği + sembol
   bazlı anahtarlanan, TTL'li bir store (Redis veya in-memory + periyodik
   snapshot) gerekir. Aksi hâlde sınırsız bellek büyümesi ve restart'ta state
   kaybı kaçınılmazdır.

3. **Zaman ve sıralama semantiği.** `previous` artık "bir önceki kayıt" değil,
   event-time'a göre önceki pencere kapanışıdır. `OrderBy(p => p.Time)` gibi
   global sıralama sonsuz akışta imkânsızdır; bunun yerine gecikmiş/sırasız
   mesajlar için **watermark** (ör. 5 sn tolerans) ve pencereleme (windowing)
   kullanılır. Duplike mesajlar için idempotency anahtarı (mesaj ID'si) tutulur.

4. **Çoklu sembol ve paralellik.** 10.000/sn genelde tek enstrüman değil, N
   sembolün toplamıdır. Kural değerlendirmesi saf CPU işi olduğundan tek
   çekirdek çoğu zaman yeter; ancak state'in kural+sembol bazında izole olması
   sayesinde sembol bazlı bölümleme (partitioning) ile yatay ölçekleme kolaydır.
   Bu durumda **kuralların thread-safe olması** zorunluluğu doğar:
   `EvaluationContext` `ConcurrentDictionary`'ye çevrilir ya da her sembolün
   state'i tek tüketiciye sabitlenir (actor modeli, ör. Orleans grain'i).

5. **Kural değerlendirme maliyeti.** Her mesajda tüm kuralları gezmek
   O(mesaj × kural) maliyettir. Kural sayısı büyürse sembol/tip bazlı indeksleme
   ("bu sembolü ilgilendiren kurallar") ve derlenmiş ifade ağaçları
   (`Expression.Compile`) gerekir. `decimal` → `double` geçişi ancak profilleme
   sonrası düşünülmelidir (finansta hassasiyet genelde `decimal`'de kalmayı
   gerektirir).

6. **Çıktı tarafı.** Konsol, saniyede binlerce alert'i kaldıramaz; alert'ler de
   bir kuyruğa gönderilir (outbox → Kafka/webhook). **Dedup ve rate limiting**
   zorunlu hâle gelir — `cooldown` kuralı zaten bunun ilkel hâlidir.

7. **Gözlemlenebilirlik ve dayanıklılık.** Metrikler (mesaj/sn, kural başına
   değerlendirme süresi, alert/sn), health check, crash sonrası kuyruktan devam
   (at-least-once teslimat + idempotent tüketici) ve dağıtık izleme (trace)
   olmazsa olmaz olur.

Özetle değişimin ekseni: **pull → push, ephemeral state → kalıcı bölümlenmiş
state, global sıralama → pencereli event-time akışı, senkron döngü →
backpressure'lı async pipeline.**

### S2: Koda hiç dokunmadan, yalnızca konfigürasyonla (JSON, script) yepyeni bir kural tipi eklemek nasıl çalışırdı ve dezavantajları ne olurdu?

**Nasıl çalışırdı:** Bugün kural tipleri `RuleFactory`'deki `switch` içinde sabit
kodludur; yeni tip eklemek kod değişikliği gerektirir. Bunu konfigürasyona
açmanın iki tipik yolu vardır:

1. **Birleştirilebilir ilkel kural dili (composite DSL).** JSON şemasına genel
   amaçlı ilkel bloklar eklenir: `expr` (aritmetik/karşılaştırma ifadesi, ör.
   `abs((price - prev) / prev * 100) >= 5`), `window` (son N saat üzerinde
   `min/max/avg/slope` gibi agregasyonlar), `count` gibi. "Yeni kural tipi"
   artık bu ilkellerin konfigürasyonla yazılmış bir birleşimidir: örneğin
   "son 3 saatin ortalaması eşiği aştı" kuralı `window(avg, 3h)` + `expr`
   bileşimiyle, kod yazılmadan tanımlanabilir. `RuleFactory` yalnızca bu
   ilkelleri bilir; türetilmiş tipler tamamen veri olur.

2. **Betik (script) tabanlı kurallar.** Kural tanımına bir ifade alanı eklenir
   (`{ "type": "script", "expr": "..." }`); çalışma zamanında Roslyn
   (`CSharpScript`), Lua (MoonSharp) veya bir ifade motoru (DynamicExpresso,
   NCalc) ile derlenip çalıştırılır. Betik ortamına `price`, `prev`, `history`
   gibi değişkenler enjekte edilir. İlk derlemede üretilen delegate kural
   kimliğiyle önbelleğe alınır.

**Dezavantajları:**

- **Tip güvenliği kaybı:** `percent < 0` gibi derleme/ctor zamanı doğrulamaları
  yerine çalışma zamanı hataları alınır; hatalı konfigürasyon ancak veri
  geldiğinde patlar. JSON şema doğrulaması ile kısmen telafi edilir.
- **Güvenlik:** Keyfi betik çalıştırmak kod enjeksiyonu kapısıdır; sandbox
  (whitelist API, süre/bellek limiti) şarttır. Salt ifade dili (güvenli alt
  küme) riski azaltır ama ifade gücünü kısar.
- **Performans:** Her mesajda betik yorumlamak/derlemek pahalıdır; derlenmiş
  delegelerin önbelleğe alınması zorunludur. 10.000/sn senaryosunda bu kritikleşir.
- **State'li kurallar zorlaşır:** `streak`/`cooldown` gibi saatler arası durum
  taşıyan kuralları deklaratif ifadeyle tanımlamak karmaşıktır; DSL'e açık
  `state` primitifleri eklemek gerekir, bu da dilin sadeliğini bozar.
- **Hata ayıklama ve test:** Konfigürasyondaki mantık IDE desteği, birim testi
  ve refactor güvencesinden yoksundur; doğrulama ayrı araçlara (schema + lint)
  kayar.
- **Şema evrimi:** DSL/betik şeması evrildikçe eski konfigürasyonlarla geriye
  dönük uyumluluk yükü doğar; şema versiyonlama zorunlu olur.

Pratik denge: öncelikle **composite DSL** (güvenli, doğrulanabilir, vakaların
büyük çoğunluğunu kapsar); gerçekten özel mantık gereken az sayıda kural için
sandbox'lı betik kaçış kapısı bırakılır.

## Daha Fazla Vaktim Olsa Sırada Ne Yapardım

1. **JSON şema doğrulaması:** `rules.json` için şema + satır bilgili anlamlı hata
   mesajları (hangi kural, hangi alan). Şu an eksik alanlar `RuleFactory`'de
   fırlatılıyor ama konum bilgisi yok.
2. **Kural lint aracı:** Veri olmadan kuralları analiz edip "bu kural hiç
   eşleşemez" (ör. `and` içinde `gt:100` ve `lt:50`) gibi çelişkileri raporlama.
3. **`--explain` modu:** Her alert için kural ağacındaki hangi dalın, hangi ara
   değerlerle eşleştiğini gösteren iz (trace) çıktısı — operasyonel hata
   ayıklamayı ciddi kolaylaştırır.
4. **Benchmark:** BenchmarkDotNet ile büyük serilerde (ör. 1 milyon nokta) kural
   başına maliyet ölçümü.
5. **Streaming modu:** Stdin'den satır satır (NDJSON) fiyat okuyup gerçek
   zamanlı çalışan `--stream` bayrağı; Bölüm 4/S1'deki tasarımın ilk adımı.
6. **Makine okunabilir çıktı:** `--format json` bayrağı ile alert'lerin JSON
   olarak basılması (downstream sistemlerle entegrasyon için).
7. **Çoklu sembol desteği:** `PricePoint`'e `symbol` alanı ekleyip state'i
   kural+sembol başına izole etmek.
