using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Sora.Adapter.Milky;

// ---- Read args ----
// Usage: TestReporter <resultsDir> <groupId> [unitDuration] [funcDuration]
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: TestReporter <resultsDir> <groupId> [unitDur] [funcDur]");
    return 1;
}

string resultsDir  = args[0];
long   groupId     = long.Parse(args[1]);
double unitSeconds = args.Length > 2 ? double.Parse(args[2]) : 0;
double funcSeconds = args.Length > 3 ? double.Parse(args[3]) : 0;

TimeSpan unitDuration = TimeSpan.FromSeconds(unitSeconds);
TimeSpan funcDuration = TimeSpan.FromSeconds(funcSeconds);
GroupId  gid          = groupId;

// ---- Parse TRX files for test counts per category and collection ----
List<string> trxFiles = [];
int unitTotal = 0, unitPassed = 0, unitFailed = 0, unitSkipped = 0;
int funcTotal = 0, funcPassed = 0, funcFailed = 0, funcSkipped = 0;
List<(string Name, string Category, int Total, int Passed, int Failed, int Skipped)> collectionStats = [];

if (Directory.Exists(resultsDir))
{
    DirectoryInfo dir = new(resultsDir);
    trxFiles = dir.GetFiles("*.trx", SearchOption.AllDirectories)
                  .OrderBy(f => f.LastWriteTimeUtc)
                  .Select(f => f.FullName)
                  .ToList();

    foreach (string trxPath in trxFiles)
    {
        string fileName = Path.GetFileName(trxPath);

        // Extract category from TRX filename: [Category][...][...].trx → "Unit" or "Func" etc.
        string category = "Unknown";
        if (fileName.StartsWith("[") && fileName.IndexOf(']') > 1) category = fileName[1..fileName.IndexOf(']')];
        bool isUnit = string.Equals(category, "Unit", StringComparison.OrdinalIgnoreCase);

        try
        {
            XDocument  doc = XDocument.Load(trxPath);
            XNamespace ns  = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

            // Aggregate totals from ResultSummary
            XElement? counters = doc.Descendants(ns + "Counters").FirstOrDefault();
            if (counters is not null)
            {
                int total   = int.Parse(counters.Attribute("total")?.Value ?? "0");
                int passed  = int.Parse(counters.Attribute("passed")?.Value ?? "0");
                int failed  = int.Parse(counters.Attribute("failed")?.Value ?? "0");
                int skipped = total - passed - failed;

                if (isUnit)
                {
                    unitTotal   += total;
                    unitPassed  += passed;
                    unitFailed  += failed;
                    unitSkipped += skipped;
                }
                else
                {
                    funcTotal   += total;
                    funcPassed  += passed;
                    funcFailed  += failed;
                    funcSkipped += skipped;
                }
            }

            // Per-class (collection) breakdown from UnitTestResult elements
            // Extract protocol from TRX filename: [Category][Protocol][Collection]_date.trx
            string protocol = "";
            if (fileName.StartsWith("["))
            {
                string[] parts                  = fileName.Split(']');
                if (parts.Length >= 3) protocol = parts[1].TrimStart('[');
            }

            IEnumerable<XElement> results = doc.Descendants(ns + "UnitTestResult");
            foreach (IGrouping<string, XElement> classGroup in results.GroupBy(r =>
                     {
                         // testName format: "Sora.Tests.Unit.Core.IdTypeTests.MethodName"
                         string testName = r.Attribute("testName")?.Value ?? "";
                         int    lastDot  = testName.LastIndexOf('.');
                         return lastDot > 0 ? testName[..lastDot] : testName;
                     }))
            {
                string   className = classGroup.Key;
                string[] nameParts = className.Split('.');
                string   folder    = nameParts.Length >= 2 ? nameParts[^2] : "";
                string   shortName = nameParts[^1];
                string label = isUnit
                    ? string.IsNullOrEmpty(folder) ? shortName : $"{folder}/{shortName}"
                    : string.IsNullOrEmpty(protocol)
                        ? shortName
                        : $"{protocol}/{shortName}";

                int clsTotal   = classGroup.Count();
                int clsPassed  = classGroup.Count(r => r.Attribute("outcome")?.Value == "Passed");
                int clsFailed  = classGroup.Count(r => r.Attribute("outcome")?.Value == "Failed");
                int clsSkipped = clsTotal - clsPassed - clsFailed;

                collectionStats.Add((label, category, clsTotal, clsPassed, clsFailed, clsSkipped));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[TestReporter] Warning: Failed to parse {fileName}: {ex.Message}");
        }
    }
}

int allTotal   = unitTotal + funcTotal;
int allPassed  = unitPassed + funcPassed;
int allFailed  = unitFailed + funcFailed;
int allSkipped = unitSkipped + funcSkipped;

// ---- Build report message ----
StringBuilder sb          = new();
string        statusEmoji = allFailed > 0 ? "❌" : "✅";
sb.AppendLine($"{statusEmoji} Sora Test Report");
sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━");

// Overall results
sb.AppendLine($"📊 Total: {allPassed}/{allTotal} passed, {allFailed} failed, {allSkipped} skipped");

// Per-category results with collection breakdown (auto-discovered from TRX filenames)
if (unitTotal > 0)
{
    string unitStatus = unitFailed > 0 ? "❌" : "✅";
    sb.AppendLine($"  {unitStatus} Unit: {unitPassed}/{unitTotal} passed, {unitFailed} failed ({unitDuration.TotalSeconds:F3} s)");
    IEnumerable<IGrouping<string, (string Name, string Category, int Total, int Passed, int Failed, int Skipped)>> unitByFolder =
        collectionStats.Where(c => c.Category == "Unit")
                       .GroupBy(c =>
                       {
                           int slash = c.Name.IndexOf('/');
                           return slash > 0 ? c.Name[..slash] : c.Name;
                       });

    foreach (IGrouping<string, (string Name, string Category, int Total, int Passed, int Failed, int Skipped)> folder in unitByFolder)
    {
        int    fTotal  = folder.Sum(c => c.Total);
        int    fPassed = folder.Sum(c => c.Passed);
        int    fFailed = folder.Sum(c => c.Failed);
        string fIcon   = fFailed > 0 ? "❌" : "✅";
        sb.AppendLine($"     └ {fIcon} {folder.Key}: {fPassed}/{fTotal} passed{(fFailed > 0 ? $", {fFailed} failed" : "")}");
    }
}

if (funcTotal > 0)
{
    string funcStatus = funcFailed > 0 ? "❌" : "✅";
    sb.AppendLine($"  {funcStatus} Func: {funcPassed}/{funcTotal} passed, {funcFailed} failed ({funcDuration.TotalSeconds:F3} s)");
    foreach ((string name, _, int t, int p, int f, _) in collectionStats.Where(c => c.Category == "Func"))
    {
        string icon = f > 0 ? "❌" : "✅";
        sb.AppendLine($"     └ {icon} {name}: {p}/{t} passed{(f > 0 ? $", {f} failed" : "")}");
    }
}

// Any other categories (future-proof)
IEnumerable<string> otherCategories = collectionStats
                                      .Select(c => c.Category)
                                      .Distinct()
                                      .Where(c => c != "Unit" && c != "Func");
foreach (string cat in otherCategories)
{
    List<(string Name, string Category, int Total, int Passed, int Failed, int Skipped)> catStats =
        collectionStats.Where(c => c.Category == cat).ToList();
    int    catTotal  = catStats.Sum(c => c.Total);
    int    catPassed = catStats.Sum(c => c.Passed);
    int    catFailed = catStats.Sum(c => c.Failed);
    string catStatus = catFailed > 0 ? "❌" : "✅";
    sb.AppendLine($"  {catStatus} {cat}: {catPassed}/{catTotal} passed, {catFailed} failed");
    foreach ((string name, _, int t, int p, int f, _) in catStats)
    {
        string icon = f > 0 ? "❌" : "✅";
        sb.AppendLine($"     └ {icon} {name}: {p}/{t} passed{(f > 0 ? $", {f} failed" : "")}");
    }
}

// Duration and timing
TimeSpan totalDuration = unitDuration + funcDuration;
DateTime endTime       = DateTime.Now;
DateTime startTime     = endTime - totalDuration;
sb.AppendLine($"⏱ Total: {totalDuration.TotalSeconds:F3} s");
sb.AppendLine($"📅 {startTime:yyyy-MM-dd HH:mm:ss} → {endTime:HH:mm:ss} ({totalDuration.TotalSeconds:F3} s)");

// Platform
sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━");
sb.AppendLine($"💻 OS: {RuntimeInformation.OSDescription}");
sb.AppendLine($"🔧 SDK: {RuntimeInformation.FrameworkDescription}");

// ENV config
sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━");
sb.AppendLine("🔑 ENV:");
string ob11PrimaryHost    = Environment.GetEnvironmentVariable("SORA_TEST_OB11_PRIMARY_HOST") ?? "";
string ob11SecondaryHost  = Environment.GetEnvironmentVariable("SORA_TEST_OB11_SECONDARY_HOST") ?? "";
string milkyPrimaryHost   = Environment.GetEnvironmentVariable("SORA_TEST_MILKY_PRIMARY_HOST") ?? "";
string milkySecondaryHost = Environment.GetEnvironmentVariable("SORA_TEST_MILKY_SECONDARY_HOST") ?? "";
string milkyPrefix        = Environment.GetEnvironmentVariable("SORA_TEST_MILKY_PREFIX") ?? "";
if (!string.IsNullOrEmpty(ob11PrimaryHost))
    sb.AppendLine($"  OB11 Primary: {ob11PrimaryHost}:{Environment.GetEnvironmentVariable("SORA_TEST_OB11_PORT") ?? "3001"}");
if (!string.IsNullOrEmpty(ob11SecondaryHost))
    sb.AppendLine($"  OB11 Secondary: {ob11SecondaryHost}:{Environment.GetEnvironmentVariable("SORA_TEST_OB11_PORT") ?? "3001"}");
if (!string.IsNullOrEmpty(milkyPrimaryHost))
    sb.AppendLine(
        $"  Milky Primary: {milkyPrimaryHost}:{Environment.GetEnvironmentVariable("SORA_TEST_MILKY_PORT") ?? "3010"}/{milkyPrefix}");
if (!string.IsNullOrEmpty(milkySecondaryHost))
    sb.AppendLine(
        $"  Milky Secondary: {milkySecondaryHost}:{Environment.GetEnvironmentVariable("SORA_TEST_MILKY_PORT") ?? "3010"}/{milkyPrefix}");
sb.AppendLine($"  Group: {groupId}");

// TRX files
sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━");
if (trxFiles.Count > 0)
{
    sb.AppendLine($"📁 TRX ({trxFiles.Count}):");
    foreach (string f in trxFiles)
        sb.AppendLine($"  • {Path.GetFileName(f)}");
}
else
{
    sb.AppendLine($"⚠ No TRX files in {resultsDir}");
}

string finalReport = sb.ToString();
Console.WriteLine(finalReport);

// ---- Save report to resultsDir
await File.WriteAllTextAsync(Path.Combine(resultsDir, $"report-{DateTime.Now:MMddyyyy_HHmmss}.txt"), finalReport);

// ---- Connect and send ----
IBotApi?          api        = null;
IAsyncDisposable? disposable = null;

try
{
    if (!string.IsNullOrEmpty(milkyPrimaryHost))
    {
        MilkyConfig config = new()
            {
                Host = milkyPrimaryHost,
                Port = int.TryParse(Environment.GetEnvironmentVariable("SORA_TEST_MILKY_PORT"), out int mp)
                    ? mp
                    : 3010,
                Prefix         = milkyPrefix,
                AccessToken    = Environment.GetEnvironmentVariable("SORA_TEST_MILKY_TOKEN") ?? "",
                EventTransport = EventTransport.WebSocket,
                ApiTimeout     = TimeSpan.FromSeconds(30)
            };

        MilkyAdapter                  adapter = new(config);
        TaskCompletionSource<IBotApi> ready   = new();
        ((IAdapterEventSource)adapter).OnEvent += async e =>
        {
            if (e is ConnectedEvent && e.Api is not null) ready.TrySetResult(e.Api);
            await ValueTask.CompletedTask;
        };
        await adapter.StartAsync();
        await Task.WhenAny(ready.Task, Task.Delay(10000));
        if (ready.Task.IsCompletedSuccessfully) api = ready.Task.Result;
        disposable = adapter;
    }

    if (api is null)
    {
        Console.Error.WriteLine("[TestReporter] Failed to connect — skipping group report");
        return 0;
    }

    // Send text report
    MessageBody textMsg = [new TextSegment { Text = finalReport }];
    await api.SendGroupMessageAsync(gid, textMsg);
    Console.WriteLine("[TestReporter] Report sent to group");

    // Upload TRX files — create a time-stamped folder, read files as base64
    if (trxFiles.Count > 0)
    {
        string folderName = $"TestResults_{DateTime.Now:yyyyMMdd_HHmmss}";

        // Create folder in group
        ApiResult<string> folderResult = await api.CreateGroupFolderAsync(gid, folderName);
        string            folderId     = folderResult.IsSuccess && folderResult.Data is not null ? folderResult.Data : "/";
        Console.WriteLine(
            $"[TestReporter] Folder '{folderName}': {(folderResult.IsSuccess ? $"created (id={folderId})" : $"fallback to root ({folderResult.Message})")}");

        foreach (string trxPath in trxFiles)
        {
            string uploadName = Path.GetFileName(trxPath);

            // Read local file and encode as base64:// URI
            byte[] fileBytes = File.ReadAllBytes(trxPath);
            string base64Uri = $"base64://{Convert.ToBase64String(fileBytes)}";

            ApiResult<string> uploadResult = await api.UploadGroupFileAsync(gid, base64Uri, uploadName, folderId);
            Console.WriteLine($"[TestReporter] Upload {uploadName}: {(uploadResult.IsSuccess ? "OK" : uploadResult.Message)}");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[TestReporter] Error: {ex.Message}");
}
finally
{
    if (disposable is not null) await disposable.DisposeAsync();
}

return 0;