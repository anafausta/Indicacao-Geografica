# ☕ OdisseiaVR — Indicação Geográfica (IP Vale da Grama)

> **Uma experiência imersiva em Realidade Virtual (RV) para apresentação, valorização e disseminação de territórios associados a Indicações Geográficas.**

---

## 📌 Sobre o Projeto

As **Indicações Geográficas (IG)** reconhecem produtos ou serviços cujas qualidades, reputação e características estão essencialmente ligadas ao seu território de origem, refletindo o saber-fazer, o patrimônio cultural e a identidade das comunidades produtoras. Contudo, a distância geográfica e a visibilidade limitada frequentemente dificultam a difusão do valor histórico e produtivo dessas regiões.

O **OdisseiaVR (Vertente IG)** utiliza a tecnologia de Realidade Virtual no **Meta Quest 2** para aproximar o público de territórios detentores de IG[cite: 30]. Como estudo de caso, a aplicação apresenta a **Indicação de Procedência (IP) Vale da Grama**, localizada no município de **São Sebastião da Grama - SP**.

A experiência combina **panoramas 360°**, **depoimentos audiovisuais de produtores locais** e **quizzes interativos** embasados no *Caderno de Especificações Técnicas da IG*.

---

## 🚀 Funcionalidades Principais

* 🔄 **Navegação Imersiva 360°:** Exploração de pontos turísticos, históricos e de cultivo agrícola em projeções esféricas (equiretangulares) registradas em campo.
* 🎥 **Contextualização Audiovisual:** Depoimentos e entrevistas com cafeicultores locais e autoridades municipais, com suporte a legendas adaptadas para RV.
* 🎯 **Gamificação Interativa (Quizzes):** Desafios de múltipla escolha distribuídos ao longo dos cenários[cite: 30]. As perguntas foram elaboradas por meio da triangulação de três fontes:
  1. Caderno de Especificações Técnicas da IP Vale da Grama.
  2. Historiografia e documentos oficiais do município.
  3. Transcrições de depoimentos coletados na expedição de campo.
* 🧩 **Arquitetura Modular:** Separação estrita entre lógica e dados via `JSON`. A estrutura permite adaptar o sistema para qualquer outra região detentora de IG apenas trocando os ativos de mídia e o arquivo de configuração.
* 🎵 **Trilha Sonora Cultural:** Áudios de fundo temáticos inspirados na cultura local e gerados com auxílio de Inteligência Artificial.

---

## 📍 Pontos de Interesse (Estudo de Caso: Vale da Grama)

O tour imersivo abrange os seguintes locais representativos do município e da cadeia produtiva do café:

1. **Associação dos Cafeicultores do Vale da Grama:** Sede da organização que impulsiona a IG na região.
2. **Secretaria de Turismo e Cultura:** Ponto de referência e gestão cultural.
3. **Museu Municipal Prefeito Arakén Cruz:** Acervo da memória histórica e cultural da cidade.
4. **Cristo Redentor:** Mirante e marco turístico do município.
5. **Paróquia São Sebastião:** Patrimônio histórico e religioso[cite: 30].
6. **Área de Plantação de Café:** Imersão direta nas lavouras do café especial do Vale da Grama.

---

## 🛠️ Tecnologias e Ferramentas

* **Game Engine:** Unity (Universal Render Pipeline - URP)
* **SDK VR:** XR Interaction Toolkit
* **Hardware Alvo:** Meta Quest 2
* **Linguagem:** C# (.NET Framework / Unity API)
* **Captura de Mídia:** Câmera Samsung Gear 360° (fotografias e vídeos em formato fisheye/equiretangular)[cite: 30]
* **Modelagem de Dados:** JSON (`tour_data.json`)

---

## 📐 Arquitetura de Software

A aplicação foi projetada seguindo princípios de **Clean Code** e **SOLID**, com padrões de projeto bem definidos para facilitar testes e manutenção:

* **Strategy Pattern (`IChallengeHandler`):** Cada tipo de etapa (Quiz ou Vídeo) possui seu próprio manipulador isolado (`QuizChallengeHandler`, `VideoChallengeHandler`).
* **Observer Pattern (`ITourView`):** A interface de usuário avisa eventos para o gerenciador sem que o backend dependa diretamente dos elementos do Canvas.
* **Repository Pattern (`ITourRepository` / `JsonTourRepository`):** O carregamento de cenários, áudios e vídeos é feito sob demanda de forma assíncrona (`Resources.LoadAsync`), garantindo baixo consumo de RAM no Meta Quest 2.

---

## 📂 Estrutura de Diretórios (Scripts)

```text
Assets/_Project/Scripts/
├── 📁 Core
│   ├── AppContext.cs
│   ├── IScreenOverlay.cs
│   └── SceneLoader.cs
├── 📁 Tour
│   ├── 📁 Challenges
│   │   ├── IChallengeHandler.cs
│   │   ├── QuizChallengeHandler.cs
│   │   └── VideoChallengeHandler.cs
│   ├── 📁 Data
│   │   ├── ITourRepository.cs
│   │   ├── JsonTourRepository.cs
│   │   ├── TourDtos.cs
│   │   └── TourModels.cs
│   ├── ITourView.cs
│   ├── PanoramaPresenter.cs
│   ├── TourAudio.cs
│   ├── TourProgress.cs
│   ├── TourSettings.cs
│   ├── VideoPanoramaPlayer.cs
│   └── XrTourInput.cs
├── LobbyManager.cs
├── PersistentFade.cs
├── TourManager.cs
├── TourUIManager.cs
└── tour_data.json
```

---

## ⚙️ Como Executar o Projeto

### Pré-requisitos
* **Unity Editor** (Versão recomendada compatível com URP e XR Interaction Toolkit).
* Módulo de suporte **Android Build Support** instalado na Unity (para Meta Quest).
* Dispositivo **Meta Quest 2** em Modo Desenvolvedor ou aplicativo Meta Quest Link/SideQuest.

### Passos
1. Clone este repositório:
   ```bash
   git clone [https://github.com/AlmirCesar8/OdisseiaVR.git](https://github.com/AlmirCesar8/OdisseiaVR.git)
   ```
2. Abra o projeto na **Unity Editor**.
3. Certifique-se de que a cena `LobbyScene` é a primeira na lista de *Build Settings*.
4. Para testar no headset:
   * Conecte o Meta Quest 2 via cabo USB.
   * Vá em **File > Build and Run** e selecione a plataforma Android.

---

## 🔄 Adaptando para Outras Indicações Geográficas

Para utilizar este projeto com outra região detentora de IG:

1. Adicione as fotografias 360° e vídeos na pasta `Resources/`.
2. Edite o arquivo `tour_data.json` na raiz da pasta de Scripts:
   * Defina os nomes dos locais (`locationName`).
   * Indique os caminhos das mídias (`panoramaMaterialPath`, `videoPath`, `backgroundMusicPath`).
   * Cadastre as perguntas, alternativas e o índice da resposta correta no array `desafios`.
3. O sistema carregará a nova IG automaticamente sem necessidade de recalcular ou reescrever scripts em C#!

---

## 👥 Equipe e Autores

* **Ana Maria Fausta Bonfim Santos** — *Graduanda em Bacharelado em Sistemas de Informação (Bolsista PIBIC/CNPq - IFSP)*
* **Almir César da Silva Filho** — *Estudante do Técnico em Desenvolvimento de Sistemas Integrado ao Ensino Médio (IFSP)*
* **Profª. Dra. Daniela dos Santos Santana** — *Docente e Orientadora (IFSP)*

### Agradecimentos
* **Instituto Federal de São Paulo (IFSP - Campus São Paulo)**
* **Pró-Reitoria de Pesquisa e Pós-Graduação (PRP-IFSP) e PIBIC/CNPq**
* **Associação dos Cafeicultores do Vale da Grama** e Prefeitura Municipal de São Sebastião da Grama

---
