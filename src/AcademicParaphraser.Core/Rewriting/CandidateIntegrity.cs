using System;
using System.Linq;

namespace AcademicParaphraser.Core.Rewriting
{
    public static class CandidateIntegrity
    {
        // Discard an entire proposal when any part cannot be written. Removing individual edits
        // can destroy a sentence transformation and leave a misleading one-word proposal.
        public static bool IsApplicable(string source, Candidate candidate, Func<TextEdit, bool> canEdit)
        {
            if (candidate == null || candidate.Edits == null || candidate.Edits.Count == 0)
                return false;
            try
            {
                return candidate.Edits.All(e => e != null && e.Start >= 0 && e.Length > 0 && e.Start <= source.Length - e.Length
                    && e.Original != null && e.Replacement != null && !e.Replacement.Any(char.IsControl) && canEdit(e))
                    && string.Equals(EditApplication.Apply(source, candidate.Edits), candidate.Text, StringComparison.Ordinal);
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }
    }
}
