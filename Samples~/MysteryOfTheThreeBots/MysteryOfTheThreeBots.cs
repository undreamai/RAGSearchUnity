using System.Collections.Generic;
using UnityEngine;
using RAGSearchUnity;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;

public class MysteryOfTheThreeBots : MonoBehaviour
{
    public Dropdown CharacterSelect;
    public InputField PlayerText;
    public Text AIText;

    public TextAsset ButlerText;
    public TextAsset MaidText;
    public TextAsset ChefText;
    public RawImage ButlerImage;
    public RawImage MaidImage;
    public RawImage ChefImage;
    public Embedding embedding;

    Dictionary<string, Bot> bots;
    Dictionary<string, RawImage> botImages;
    string currentBotName;
    Bot currentBot;
    RawImage currentImage;

    void Start()
    {
        StartCoroutine(InitDialogue());
        CharacterSelect.onValueChanged.AddListener(DropdownChange);
    }

    IEnumerator<string> InitDialogue()
    {
        PlayerText.interactable = false;
        EmbeddingModel model = embedding.GetModel();
        if (model == null)
        {
            throw new System.Exception("Please select an Embedding model in the HamletSearch GameObject!");
        }

        bots = new Dictionary<string, Bot>();
        botImages = new Dictionary<string, RawImage>();
        string outputDir = Path.Combine(Application.streamingAssetsPath, "MysteryOfTheThreeBots");
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        foreach ((string botName, TextAsset asset, RawImage image) in new (string, TextAsset, RawImage)[]{
            ("Butler", ButlerText, ButlerImage), ("Maid", MaidText, MaidImage), ("Chef", ChefText, ChefImage)}
        )
        {
            string embeddingsPath = Path.Combine(outputDir, botName + ".zip");
            PlayerText.text += File.Exists(embeddingsPath)? $"Loading {botName} dialogues...\n": $"Creating Embeddings for {botName} (only once)...\n";
            yield return null;
            bots[botName] = new Bot(asset.text, model, embeddingsPath);
            botImages[botName] = image;
        }
        PlayerText.interactable = true;
        InitPlayerText();
    }

    void InitPlayerText()
    {
        DropdownChange(CharacterSelect.value);
        PlayerText.onSubmit.AddListener(OnInputFieldSubmit);
        PlayerText.onValueChanged.AddListener(OnValueChanged);
        PlayerText.Select();
        PlayerText.text = "";
    }

    void OnValueChanged(string newText)
    {
        // Get rid of newline character added when we press enter
        if (Input.GetKey(KeyCode.Return))
        {
            if (PlayerText.text.Trim() == "")
                PlayerText.text = "";
        }
    }

    void OnInputFieldSubmit(string message)
    {
        PlayerText.interactable = false;

        // search for the most similar text and reply
        AIText.text = currentBot.Ask(message);

        PlayerText.interactable = true;
        PlayerText.Select();
        PlayerText.text = "";
    }

    void DropdownChange(int selection)
    {
        // select another character
        currentBotName = CharacterSelect.options[selection].text;
        currentBot = bots[currentBotName];
        if (currentImage != null) currentImage.gameObject.SetActive(false);
        currentImage = botImages[currentBotName];
        currentImage.gameObject.SetActive(true);
        Debug.Log($"{currentBotName}: {currentBot.NumPhrases()} phrases available");
    }

}


public class Bot
{
    Dictionary<string, string> dialogues;
    SearchEngine search;

    public Bot(string dialogueText, EmbeddingModel model, string embeddingsPath)
    {
        LoadDialogues(dialogueText);
        CreateEmbeddings(model, embeddingsPath);
    }

    void LoadDialogues(string dialogueText)
    {
        dialogues = new Dictionary<string, string>();
        foreach (string line in dialogueText.Split("\n"))
        {
            if (line == "" ) continue;
            string[] lineParts = line.Split("|");
            dialogues[lineParts[0]] = lineParts[1];
        }
    }

    void CreateEmbeddings(EmbeddingModel model, string embeddingsPath)
    {
        if (File.Exists(embeddingsPath))
        {
            // load the embeddings
            search = SearchEngine.Load(model, embeddingsPath);
        }
        else
        {
#if UNITY_EDITOR
            search = new SearchEngine(model);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            // build the embeddings
            foreach (string question in dialogues.Keys)
            {
                search.Add(question);
            }
            Debug.Log($"embedded {search.NumPhrases()} phrases, {search.NumSentences()} sentences in {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");
            // store the embeddings
            search.Save(embeddingsPath);
#else
            // if in play mode throw an error
            throw new System.Exception("The embeddings could not be found!");
#endif
        }
    }

    public string Ask(string question)
    {
        string similarQuestion = search.Search(question, 1)[0];
        return dialogues[similarQuestion];
    }

    public int NumPhrases()
    {
        return search.NumPhrases();
    }
}