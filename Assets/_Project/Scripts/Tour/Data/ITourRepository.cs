using System;
using System.Collections;
using System.Collections.Generic;

namespace OdisseiaVR.Tour.Data
{
    /// <summary>
    /// Fonte de dados do tour. Hoje é um JSON + Resources; amanhã pode ser Addressables
    /// ou um servidor, sem tocar em Lobby nem Tour (DIP).
    /// </summary>
    public interface ITourRepository
    {
        int LocationCount { get; }
        string GetLocationName(int index);

        /// <summary>Carrega só nome + miniatura do mapa de todos os locais (uso do Lobby).</summary>
        IEnumerator LoadSummariesAsync(Action<IReadOnlyList<LocationSummary>> onLoaded);

        /// <summary>Carrega os assets pesados de UM local. Entrega null em caso de erro.</summary>
        IEnumerator LoadLocationAsync(int index, Action<TourLocation> onLoaded);

        /// <summary>Libera da memória assets que não estão mais referenciados.</summary>
        IEnumerator ReleaseAssetsAsync();
    }
}
