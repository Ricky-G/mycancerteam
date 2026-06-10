using MyCancerTeam.Core.Models;
using MyCancerTeam.Core.Research;

namespace MyCancerTeam.Infrastructure.Research;

public sealed class ResearchOncologyService : IResearchOncologyService
{
    public Task<ResearchUpdate> GetLatestEvidenceAsync(string patientContext, CancellationToken cancellationToken = default)
    {
        // TODO: Integrate PubMed, guideline APIs, ClinicalTrials.gov, and curated oncology journal feeds.
        // TODO: Add patient-specific filtering by subtype, biomarkers, line of treatment, and geography.
        // TODO: Add evidence grading and conflict-resolution logic across sources.

        var update = new ResearchUpdate
        {
            PatientFriendlySummary = "Early research scan prepared. No treatment change should be made without clinician confirmation.",
            TechnicalSummary = "Mock summary only: structure supports guideline updates, trial deltas, toxicity management updates, and evidence conflicts.",
            SuggestedQuestions =
            [
                "Has any new evidence changed the risk-benefit balance for the current plan?",
                "Are there suitable clinical trials or second-opinion centres to discuss now?"
            ],
            EvidenceGapNotes = ["Patient-specific eligibility and local availability remain unverified."],
            Citations =
            [
                new CitationMetadata
                {
                    SourceName = "ClinicalTrials.gov",
                    Title = "Clinical trial registry reference placeholder",
                    Url = "https://clinicaltrials.gov",
                    EvidenceLevel = "registry"
                },
                new CitationMetadata
                {
                    SourceName = "PubMed",
                    Title = "Literature index reference placeholder",
                    Url = "https://pubmed.ncbi.nlm.nih.gov",
                    EvidenceLevel = "literature-index"
                }
            ]
        };

        return Task.FromResult(update);
    }
}
