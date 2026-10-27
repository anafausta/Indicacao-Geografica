using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace OdisseiaVR.Tour.Data
{
    /// <summary>
    /// Lê o tour_data.json (parse único) e carrega assets de Resources sob demanda.
    /// Diferente do TourDataManager antigo, NÃO carrega todos os locais de uma vez:
    /// no Quest 2 isso mantinha todos os vídeos e panoramas na RAM.
    /// </summary>
    public sealed class JsonTourRepository : ITourRepository
    {
        private readonly List<DadosLocalJson> locais;

        public JsonTourRepository(TextAsset json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            TourDataJson data = JsonUtility.FromJson<TourDataJson>(json.text);
            locais = data?.locais ?? new List<DadosLocalJson>();
        }

        public int LocationCount => locais.Count;

        public string GetLocationName(int index) => locais[index].locationName;

        public IEnumerator LoadSummariesAsync(Action<IReadOnlyList<LocationSummary>> onLoaded)
        {
            var result = new List<LocationSummary>(locais.Count);

            foreach (DadosLocalJson local in locais)
            {
                Texture map = null;
                yield return LoadAsset<Texture>(local.mapImagePath, asset => map = asset);
                result.Add(new LocationSummary(local.locationName, map));
            }

            onLoaded(result);
        }

        public IEnumerator LoadLocationAsync(int index, Action<TourLocation> onLoaded)
        {
            if (index < 0 || index >= locais.Count)
            {
                Debug.LogError($"[JsonTourRepository] Índice de local inválido: {index}.");
                onLoaded(null);
                yield break;
            }

            DadosLocalJson dto = locais[index];

            AudioClip music = null;
            yield return LoadAsset<AudioClip>(dto.backgroundMusicPath, asset => music = asset);

            var challenges = new List<Challenge>();
            List<DesafioJson> desafios = dto.desafios ?? new List<DesafioJson>();

            foreach (DesafioJson desafio in desafios)
            {
                // Regra de negócio centralizada aqui: videoPath preenchido => vídeo; senão => quiz.
                if (!string.IsNullOrWhiteSpace(desafio.videoPath))
                {
                    VideoClip clip = null;
                    yield return LoadAsset<VideoClip>(desafio.videoPath, asset => clip = asset);
                    challenges.Add(new VideoChallenge(desafio.initialYRotation, clip));
                }
                else
                {
                    Material panorama = null;
                    yield return LoadAsset<Material>(desafio.panoramaMaterialPath, asset => panorama = asset);
                    challenges.Add(new QuizChallenge(
                        desafio.initialYRotation, panorama, desafio.questionText,
                        desafio.answers, desafio.correctAnswerIndex));
                }
            }

            onLoaded(new TourLocation(dto.locationName, music, challenges));
        }

        public IEnumerator ReleaseAssetsAsync()
        {
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private static IEnumerator LoadAsset<T>(string path, Action<T> onLoaded) where T : UnityEngine.Object
        {
            string sanitized = SanitizePath(path);
            if (string.IsNullOrEmpty(sanitized))
            {
                onLoaded(null);
                yield break;
            }

            ResourceRequest request = Resources.LoadAsync<T>(sanitized);
            yield return request;

            T asset = request.asset as T;
            if (asset == null)
            {
                Debug.LogWarning($"[JsonTourRepository] Asset não encontrado em Resources: '{sanitized}'.");
            }
            onLoaded(asset);
        }

        // Resources.Load não aceita extensão no caminho.
        private static string SanitizePath(string original)
        {
            if (string.IsNullOrWhiteSpace(original)) return string.Empty;

            string path = original.Trim();
            int dotIndex = path.LastIndexOf('.');
            return dotIndex > 0 ? path.Substring(0, dotIndex) : path;
        }
    }
}
