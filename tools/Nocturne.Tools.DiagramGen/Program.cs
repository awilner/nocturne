using DbToMermaid;
using Microsoft.EntityFrameworkCore;
using Nocturne.Infrastructure.Data;

// Build a DbContext using Npgsql — we only need the model metadata,
// not a real database connection.
var optionsBuilder = new DbContextOptionsBuilder<NocturneDbContext>();
optionsBuilder.UseNpgsql("Host=localhost"); // connection never opened

await using var context = new NocturneDbContext(optionsBuilder.Options);

var outputPath = args.Length > 0
    ? args[0]
    : "docs/diagrams/er-diagram.mmd";

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await EfToMermaid.RenderToFile(context.Model, outputPath);

Console.WriteLine($"ER diagram written to {outputPath}");
