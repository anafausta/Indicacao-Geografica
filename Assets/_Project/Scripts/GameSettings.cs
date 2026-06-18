using UnityEngine;

/// <summary>
/// Este é um script Singleton (Instância Única) que persiste entre as cenas.
/// Sua única função é carregar dados de uma cena (Lobby) para outra (Tour).
/// </summary>
public class GameSettings : MonoBehaviour
{
    // A instância estática permite que outros scripts acessem este componente
    // de qualquer lugar usando "GameSettings.Instance".
    public static GameSettings Instance { get; private set; }

    // A informação que queremos passar para a cena do Tour.
    public int selectedLocationIndex = 0;

    /// <summary>
    /// Awake é chamado antes de Start.
    /// Usamos para garantir que apenas uma instância deste objeto exista.
    /// </summary>
    void Awake()
    {
        // Padrão Singleton:
        if (Instance == null)
        {
            // Se eu sou o primeiro, eu me torno a Instância.
            Instance = this;
            // E peço ao Unity para não me destruir ao carregar uma nova cena.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Se uma Instância já existe, eu sou um duplicado.
            // Então, eu me destruo.
            Destroy(gameObject);
        }
    }
}