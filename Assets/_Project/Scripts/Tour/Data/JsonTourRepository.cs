using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using OdisseiaVR.Core;

namespace OdisseiaVR.Tour.Data
{
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

        public string GetLocationName(int index) => ResolveText(locais[index].locationName);

        public IEnumerator LoadSummariesAsync(Action<IReadOnlyList<LocationSummary>> onLoaded)
        {
            var result = new List<LocationSummary>(locais.Count);

            foreach (DadosLocalJson local in locais)
            {
                Texture map = null;
                yield return LoadAsset<Texture>(local.mapImagePath, asset => map = asset);
                result.Add(new LocationSummary(ResolveText(local.locationName), map));
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

                    var hotspotsDomainList = ParseHotspots(desafio.hotspots);
                    var translatedAnswers = ParseAnswers(desafio.answers);

                    challenges.Add(new QuizChallenge(
                        desafio.initialYRotation,
                        panorama,
                        ResolveText(desafio.questionText),
                        translatedAnswers,
                        desafio.correctAnswerIndex,
                        hotspotsDomainList));
                }
            }

            onLoaded(new TourLocation(ResolveText(dto.locationName), music, challenges));
        }

        public IEnumerator ReleaseAssetsAsync()
        {
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        // Extrai o texto no idioma atual com fallback para PT
        private static string ResolveText(LocalizedTextJson textObj)
        {
            if (textObj == null) return string.Empty;

            return LocalizationManager.CurrentLanguage switch
            {
                AppLanguage.EN => !string.IsNullOrWhiteSpace(textObj.en) ? textObj.en : textObj.pt,
                AppLanguage.ES => !string.IsNullOrWhiteSpace(textObj.es) ? textObj.es : textObj.pt,
                _ => textObj.pt ?? string.Empty
            };
        }

        private static List<string> ParseAnswers(List<LocalizedTextJson> dtos)
        {
            var result = new List<string>();
            if (dtos == null) return result;

            foreach (var ans in dtos)
            {
                result.Add(ResolveText(ans));
            }
            return result;
        }

        private static List<HotspotData> ParseHotspots(List<HotspotJson> dtos)
        {
            var result = new List<HotspotData>();
            if (dtos == null || dtos.Count == 0) return result;

            foreach (var h in dtos)
            {
                Vector3 pos = h.position != null
                    ? new Vector3(h.position.x, h.position.y, h.position.z)
                    : Vector3.zero;

                result.Add(new HotspotData(
                    h.id,
                    ResolveText(h.title),
                    ResolveText(h.description),
                    pos,
                    h.imagePath));
            }

            return result;
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

        private static string SanitizePath(string original)
        {
            if (string.IsNullOrWhiteSpace(original)) return string.Empty;

            string path = original.Trim();
            int dotIndex = path.LastIndexOf('.');
            return dotIndex > 0 ? path.Substring(0, dotIndex) : path;
        }
    }
}