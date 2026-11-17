using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace OdisseiaVR.Tour.Data
{
    public enum ChallengeKind
    {
        Quiz,
        Video
    }

    /// <summary>
    /// Modelo de domínio que representa um Hotspot Interativo no cenário 360°.
    /// </summary>
    public sealed class HotspotData
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public Vector3 Position { get; }
        public string ImagePath { get; }

        public HotspotData(string id, string title, string description, Vector3 position, string imagePath = null)
        {
            Id = id;
            Title = title;
            Description = description;
            Position = position;
            ImagePath = imagePath;
        }
    }

    /// <summary>
    /// Um passo do tour. Substitui a classe Desafio que misturava quiz e vídeo
    /// (campos de quiz vazios em vídeos, flag IsVideo). Cada tipo carrega só o que usa.
    /// </summary>
    public abstract class Challenge
    {
        public float InitialYRotation { get; }
        public abstract ChallengeKind Kind { get; }

        protected Challenge(float initialYRotation)
        {
            InitialYRotation = initialYRotation;
        }
    }

    public sealed class QuizChallenge : Challenge
    {
        public Material Panorama { get; }
        public string Question { get; }
        public IReadOnlyList<string> Answers { get; }
        public int CorrectAnswerIndex { get; }
        public IReadOnlyList<HotspotData> Hotspots { get; } // Campo adicionado

        public override ChallengeKind Kind => ChallengeKind.Quiz;

        public QuizChallenge(float yRotation, Material panorama, string question,
                             IEnumerable<string> answers, int correctAnswerIndex,
                             IEnumerable<HotspotData> hotspots = null) : base(yRotation)
        {
            Panorama = panorama;
            Question = question;
            Hotspots = new List<HotspotData>(hotspots ?? new List<HotspotData>());

            var answerList = new List<string>(answers ?? new List<string>());

            // Valida se existem respostas e se o índice recebido do JSON é válido
            if (answerList.Count > 0 && correctAnswerIndex >= 0 && correctAnswerIndex < answerList.Count)
            {
                // 1. Guarda o texto da resposta correta antes de embaralhar
                string correctAnswerText = answerList[correctAnswerIndex];

                // 2. Embaralha a lista localmente (Algoritmo de Fisher-Yates)
                int n = answerList.Count;
                while (n > 1)
                {
                    n--;
                    int k = UnityEngine.Random.Range(0, n + 1);
                    string temp = answerList[k];
                    answerList[k] = answerList[n];
                    answerList[n] = temp;
                }

                Answers = answerList;
                // 3. Busca a nova posição da resposta correta e salva como o novo ID válido
                CorrectAnswerIndex = answerList.IndexOf(correctAnswerText);
            }
            else
            {
                // Fallback de segurança
                Answers = answerList;
                CorrectAnswerIndex = correctAnswerIndex;
            }
        }

        public bool IsCorrect(int answerIndex) => answerIndex == CorrectAnswerIndex;
    }

    public sealed class VideoChallenge : Challenge
    {
        public VideoClip Clip { get; }

        public override ChallengeKind Kind => ChallengeKind.Video;

        public VideoChallenge(float yRotation, VideoClip clip) : base(yRotation)
        {
            Clip = clip;
        }
    }

    /// <summary>Local com todos os assets carregados. Antes: DadosLocal.</summary>
    public sealed class TourLocation
    {
        public string Name { get; }
        public AudioClip BackgroundMusic { get; }
        public IReadOnlyList<Challenge> Challenges { get; }

        public TourLocation(string name, AudioClip backgroundMusic, IReadOnlyList<Challenge> challenges)
        {
            Name = name;
            BackgroundMusic = backgroundMusic;
            Challenges = challenges;
        }
    }

    /// <summary>Dados leves para o menu. Antes: LobbyDadosLocal.</summary>
    public sealed class LocationSummary
    {
        public string Name { get; }
        public Texture MapTexture { get; }

        public LocationSummary(string name, Texture mapTexture)
        {
            Name = name;
            MapTexture = mapTexture;
        }
    }
}