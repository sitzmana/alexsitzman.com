using System.Runtime.CompilerServices;

// The generator's build pipeline is internal by design; the end-to-end tests drive
// it directly rather than shelling out to the CLI.
[assembly: InternalsVisibleTo("Portfolio.Tests")]
