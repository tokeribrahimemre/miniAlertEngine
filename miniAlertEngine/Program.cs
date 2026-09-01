using System.Text.Json;
using MiniAlertEngine;
using MiniAlertEngine.Cli;
using MiniAlertEngine.Rules;

if (args.Length != 2)
{
    Console.Error.WriteLine("Kullanım: miniAlertEngine <prices.json> <rules.json>");
    return 1;
}

var (pricesPath, rulesPath) = (args[0], args[1]);

foreach (var path in new[] { pricesPath, rulesPath })
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"Dosya bulunamadı: {path}");
        return 1;
    }
}

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

List<PricePointDto>? priceDtos;
List<RuleDefinition>? ruleDefinitions;

try
{
    priceDtos = JsonSerializer.Deserialize<List<PricePointDto>>(await File.ReadAllTextAsync(pricesPath), jsonOptions);
    ruleDefinitions = JsonSerializer.Deserialize<List<RuleDefinition>>(await File.ReadAllTextAsync(rulesPath), jsonOptions);
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"JSON ayrıştırma hatası: {ex.Message}");
    return 1;
}

if (priceDtos is null || priceDtos.Count == 0)
{
    Console.Error.WriteLine("Fiyat dosyası boş veya geçersiz.");
    return 1;
}

if (ruleDefinitions is null || ruleDefinitions.Count == 0)
{
    Console.Error.WriteLine("Kural dosyası boş veya geçersiz.");
    return 1;
}

var rules = ruleDefinitions.Select(RuleFactory.Create).ToList();
var prices = priceDtos.Select(dto => dto.ToModel()).OrderBy(p => p.Time).ToList();

var engine = new AlertEngine(rules);

var alertCount = 0;
foreach (var alert in engine.Run(prices))
{
    Console.WriteLine(alert);
    alertCount++;
}

if (alertCount == 0)
    Console.WriteLine("Hiçbir kural eşleşmedi.");

return 0;
