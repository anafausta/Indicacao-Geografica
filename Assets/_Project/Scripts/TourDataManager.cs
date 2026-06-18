using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class Desafio
{
    public Material panoramaMaterial;
    public VideoClip videoClip;
    public float initialYRotation;
    public string questionText;
    public List<string> answers;
    public int correctAnswerIndex;

    public bool IsVideo => videoClip != null;
}

public class DadosLocal
{
    public string locationName;
    public AudioClip backgroundMusic;
    public List<Desafio> desafios = new List<Desafio>();
}

[System.Serializable]
public class DesafioJson
{
    public string panoramaMaterialPath;
    public string videoPath; // Nova propriedade mapeada do JSON
    public float initialYRotation;
    public string questionText;
    public List<string> answers;
    public int correctAnswerIndex;
}

[System.Serializable]
public class DadosLocalJson
{
    public string locationName;
    public string backgroundMusicPath;
    public string mapImagePath;
    public List<DesafioJson> desafios;
}

[System.Serializable]
public class TourDataJson
{
    public List<DadosLocalJson> locais;
}

public class TourDataManager : MonoBehaviour
{
    [Header("Arquivo de Conteúdo")]
    public TextAsset tourDataJson;

    public List<DadosLocal> Locais { get; private set; } = new List<DadosLocal>();
    public bool IsDataLoaded { get; private set; } = false;

    private void Start()
    {
        if (tourDataJson != null)
        {
            StartCoroutine(CarregarDadosAsync(tourDataJson.text));
        }
        else
        {
            Debug.LogError("[TourDataManager] Arquivo JSON não foi atribuído no Inspector!");
        }
    }

    private IEnumerator CarregarDadosAsync(string jsonTexto)
    {
        TourDataJson dadosJson = JsonUtility.FromJson<TourDataJson>(jsonTexto);
        Locais.Clear();

        foreach (var localJson in dadosJson.locais)
        {
            DadosLocal novoLocal = new DadosLocal { locationName = localJson.locationName };

            // Carrega a música de forma assíncrona
            string pathMusica = SanitizarCaminhoAsset(localJson.backgroundMusicPath);
            if (!string.IsNullOrEmpty(pathMusica))
            {
                ResourceRequest reqMusica = Resources.LoadAsync<AudioClip>(pathMusica);
                yield return reqMusica;
                novoLocal.backgroundMusic = reqMusica.asset as AudioClip;
            }

            foreach (var desJson in localJson.desafios)
            {
                Desafio novoDesafio = new Desafio
                {
                    initialYRotation = desJson.initialYRotation,
                    questionText = desJson.questionText,
                    answers = new List<string>(desJson.answers),
                    correctAnswerIndex = desJson.correctAnswerIndex
                };

                // Carrega Material 360 de forma assíncrona
                string pathMat = SanitizarCaminhoAsset(desJson.panoramaMaterialPath);
                if (!string.IsNullOrEmpty(pathMat))
                {
                    ResourceRequest reqMat = Resources.LoadAsync<Material>(pathMat);
                    yield return reqMat;
                    novoDesafio.panoramaMaterial = reqMat.asset as Material;
                }

                // Carrega Vídeo de forma assíncrona
                string pathVid = SanitizarCaminhoAsset(desJson.videoPath);
                if (!string.IsNullOrEmpty(pathVid))
                {
                    ResourceRequest reqVid = Resources.LoadAsync<VideoClip>(pathVid);
                    yield return reqVid;
                    novoDesafio.videoClip = reqVid.asset as VideoClip;
                }

                novoLocal.desafios.Add(novoDesafio);
            }
            Locais.Add(novoLocal);
        }

        IsDataLoaded = true;
        Debug.Log("[TourDataManager] Carga Assíncrona de Assets concluída com sucesso.");
    }

    private string SanitizarCaminhoAsset(string caminhoOriginal)
    {
        if (string.IsNullOrEmpty(caminhoOriginal)) return string.Empty;
        string caminhoSanitizado = caminhoOriginal.Trim();
        int pontoIndex = caminhoSanitizado.LastIndexOf('.');
        if (pontoIndex > 0)
        {
            caminhoSanitizado = caminhoSanitizado.Substring(0, pontoIndex);
        }
        return caminhoSanitizado;
    }

    public void LimparAssetsCarregados()
    {
        Locais.Clear();
        IsDataLoaded = false;
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    private void OnDestroy() => LimparAssetsCarregados();
}