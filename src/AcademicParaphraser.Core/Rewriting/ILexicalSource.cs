using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AcademicParaphraser.Core.Rewriting
{
    public interface ILexicalSource
    {
        Task<IReadOnlyList<LexiconEntry>> FindAsync(string text, IReadOnlyList<MorphToken> words, UserSettings settings, CancellationToken cancellation, EnrichmentContext? context = null);
    }
}
