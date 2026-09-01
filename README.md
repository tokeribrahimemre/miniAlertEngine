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

## Çalıştırma

```powershell
dotnet run --project miniAlertEngine -- samples/prices.json samples/rules.json
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
  { "id": "safe-band", "type": "range",     "min": 95, "max": 105 }
]
```

## Kural Tipleri

| Tip | Alanlar | Davranış |
|---|---|---|
| `threshold` | `operator` (`gt`/`lt`), `value` | Fiyat eşiğin üstüne çıkınca (`gt`) veya altına inince (`lt`) eşleşir. Eşitlik eşleşmez. |
| `change` | `percent` | Fiyat bir önceki saate göre mutlak olarak `%percent` veya daha fazla değişince eşleşir (yön bağımsız). |
| `range` | `min`, `max` | Fiyat `[min, max]` bandının dışına çıkınca eşleşir; sınır değerler bandın içinde sayılır. |

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
dotnet test
```
