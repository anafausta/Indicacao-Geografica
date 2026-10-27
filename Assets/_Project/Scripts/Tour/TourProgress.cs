using OdisseiaVR.Tour.Data;

namespace OdisseiaVR.Tour
{
    public enum AdvanceResult
    {
        NextChallenge,
        NextLocation,
        TourFinished
    }

    /// <summary>
    /// Regras de "o que vem depois" isoladas de Unity: sem MonoBehaviour, sem corrotina.
    /// Pode ser coberta por testes unitários (EditMode) sem abrir cena.
    /// </summary>
    public sealed class TourProgress
    {
        public int LocationCount { get; }
        public int LocationIndex { get; private set; }
        public int ChallengeIndex { get; private set; }
        public TourLocation CurrentLocation { get; private set; }

        public Challenge CurrentChallenge => CurrentLocation.Challenges[ChallengeIndex];

        public TourProgress(int locationCount)
        {
            LocationCount = locationCount;
        }

        public void EnterLocation(int index, TourLocation location)
        {
            LocationIndex = index;
            CurrentLocation = location;
            ChallengeIndex = 0;
        }

        /// <summary>Solta a referência do local atual para que seus assets possam ser descarregados.</summary>
        public void LeaveLocation()
        {
            CurrentLocation = null;
        }

        public AdvanceResult Advance()
        {
            if (ChallengeIndex + 1 < CurrentLocation.Challenges.Count)
            {
                ChallengeIndex++;
                return AdvanceResult.NextChallenge;
            }

            return LocationIndex + 1 < LocationCount
                ? AdvanceResult.NextLocation
                : AdvanceResult.TourFinished;
        }
    }
}
