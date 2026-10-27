using UnityEngine;

namespace OdisseiaVR.Core
{
    /// <summary>Estado que sobrevive à troca Lobby -> Tour. Substitui o MonoBehaviour GameSettings.</summary>
    public sealed class TourSession
    {
        public int SelectedLocationIndex { get; set; }
    }

    /// <summary>
    /// Composition root. Cria os serviços globais uma única vez, antes de qualquer cena.
    /// Elimina EnsureExists() espalhado e o "GameSettings de emergência" do Lobby.
    /// Se o projeto crescer, troque por um container de DI (VContainer/Zenject).
    /// </summary>
    public static class AppContext
    {
        private static PersistentFade fade;

        public static TourSession Session { get; private set; }
        public static SceneLoader SceneLoader { get; private set; }
        public static IScreenOverlay Overlay => fade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Session = new TourSession();

            // "== null" cobre o caso de Enter Play Mode sem Domain Reload (objeto Unity já destruído).
            if (fade == null)
            {
                var go = new GameObject("PersistentFade");
                fade = go.AddComponent<PersistentFade>();
            }

            SceneLoader = new SceneLoader(fade);
        }
    }
}
