using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AcademicParaphraser.Core.Rewriting
{
    public sealed class EnrichmentSummary
    {
        public int RootsTotal { get; set; }
        public int RootsChecked { get; set; }
        public int RootsFound { get; set; }
        public int OnlineRootsQueried { get; set; }
        public int OnlineRootsFound { get; set; }
        public int RootsUnresolved { get; set; }
        public int ProviderErrors { get; set; }
        public int LexicalEntries { get; set; }
        public int StructureInventory { get; set; }
        public int ApplicableStructures { get; set; }
        public string StructureStatus { get; set; } = "kapalı";
    }
    public sealed class EnrichmentContext
    {
        public EnrichmentSummary Summary { get; } = new EnrichmentSummary();
        public IProgress<string>? Progress { get; set; }
        public void Report(string message) => Progress?.Report(message);
    }
    public interface IStructureSource
    {
        Task<IReadOnlyList<RuleDefinition>> FindAsync(string text, UserSettings settings, CancellationToken cancellation, EnrichmentContext context);
    }
}
