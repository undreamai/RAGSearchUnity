
#RAGSearchUnity

<h3 align="center">Semantic search in Unity!</h3>

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
<a href="https://discord.gg/RwXKQb6zdv"><img src="https://discordapp.com/api/guilds/1194779009284841552/widget.png?style=shield"/></a>
[![Reddit](https://img.shields.io/badge/Reddit-%23FF4500.svg?style=flat&logo=Reddit&logoColor=white)](https://www.reddit.com/user/UndreamAI)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-blue?style=flat&logo=linkedin&labelColor=blue)](https://www.linkedin.com/company/undreamai)

RAGSearchUnity allow to implement semantic search within the Unity engine.<br>
It is a Retrieval Augmented Generation (RAG) system empowered by the best open-source retrieval models available.<br>
RAGSearchUnity is built on top of the awesome [sharp-transformers](https://github.com/huggingface/sharp-transformers) and [usearch](https://github.com/unum-cloud/usearch) libraries.

<sub>
<a href="#at-a-glance" style="color: black">At a glance</a>&nbsp;&nbsp;•&nbsp;
<a href="#how-to-help" style=color: black>How to help</a>&nbsp;&nbsp;•&nbsp;
<a href="#setup" style=color: black>Setup</a>&nbsp;&nbsp;•&nbsp;
<a href="#how-to-use" style=color: black>How to use</a>&nbsp;&nbsp;•&nbsp;
<a href="#examples" style=color: black>Examples</a>&nbsp;&nbsp;•&nbsp;
<a href="#license" style=color: black>License</a>
</sub>

## At a glance
- :computer: Cross-platform! Windows, Linux and macOS ([versions](https://github.com/Mozilla-Ocho/llamafile?tab=readme-ov-file#supported-oses))
- :house: Runs locally without internet access. No data ever leave the game!
- :zap: Blazing fast search with Approximate Nearest Neighbors (ANN)
- :hugs: Support of the best retrieval models
- :wrench: Easy to setup and use
- :moneybag: Free to use for both personal and commercial purposes

Tested on Unity: 2021 LTS, 2022 LTS, 2023<br>

## How to help
- Join us at [Discord](https://discord.gg/RwXKQb6zdv) and say hi!
- ⭐ Star the repo and spread the word about the project!
- Submit feature requests or bugs as [issues](https://github.com/undreamai/RAGSearchUnity/issues) or even submit a PR and become a collaborator!

## Setup
- Open the Package Manager in Unity: `Window > Package Manager`
- Click the `+` button and select `Add package from git URL`
- Use the repository URL `https://github.com/undreamai/RAGSearchUnity.git` and click `Add`

## How to use
RAGSearchUnity implements a super-fast similarity search functionality with a Retrieval-Augmented Generation (RAG) system.<br>
This works as follows.

**Building the data** You provide text inputs (a phrase, paragraph, document) to add in the data<br>
Each input is split into sentences (optional) and encoded into embeddings with a deep learning model.

**Searching** You can then search for an query text input. <br>
The input is again encoded and the most similar text inputs or sentences in the data are retrieved.

To use search:
- create an empty GameObject for the embedding model 🔍.<br>In the GameObject Inspector click `Add Component` and select the `Embedding` script).
- select the model you prefer from the drop-down list to download it (bge small, bge base or MiniLM v6).

In your script you can then use it as follows :unicorn::
``` c#
using RAGSearchUnity;

public class MyScript {
  public Embedding embedding;
  SearchEngine search;

  void Game(){
    ...
    string[] inputs = new string[]{
      "Hi! I'm a search system.", "the weather is nice. I like it.", "I'm a RAG system"
    };
    // build the embedding
    EmbeddingModel model = embedding.GetModel();
    search = new SearchEngine(model);
    foreach (string input in inputs) search.Add(input);
    // get the 2 most similar phrases
    string[] similar = search.Search("hello!", 2);
    // or get the 2 most similar sentences
    string[] similarSentences = search.SearchSentences("hello!", 2);
    ...
  }
}
```
- Finally, in the Inspector of the GameObject of your script, select the Embedding GameObject created above as the embedding property.

You can save the data along with the embeddings:

``` c#
search.Save("Embeddings.zip");
```
and load them from disk:
``` c#
SearchEngine search = SearchEngine.Load(model, "Embeddings.zip");
```


If you want to manage multiple independent searches, RAGSearchUnity provides the `MultiSearchEngine` class for ease of use:
``` c#
MultiSearchEngine multisearch = new MultiSearchEngine(model);

// add a text for a specific search
multisearch.Add("hi I'm Luke", "search1");
multisearch.Add("Searching, searching, searching...", "search1");
multisearch.Add("hi I'm Jane", "search2");

// search for similar text in all searches
string[] similar = multisearch.Search("hello!", 2);
// search for similar texts within a specific search
string[] similar = multisearch.Search("hi there!", 1, "search1");
```

That's all :sparkles:!

## Examples
The [HamletSearch](Samples~/HamletSearch) sample contains an example search system for the Hamlet play 🎭.
To install the sample:
- Open the Package Manager: `Window > Package Manager`
- Select the `RAGSearchUnity` Package. From the `Samples` Tab, click `Import` next to the sample.

The sample can be run with the `Scene.unity` scene it contains inside their folder.<br>
In the scene, select the `Embedding` GameObject and download one of the models (`Download model`).<br>
Save the scene, run and enjoy!

## License
The license of RAGSearchUnity is MIT ([LICENSE.md](LICENSE.md)) and uses third-party software and models with MIT and Apache licenses ([Third Party Notices.md](<Third Party Notices.md>)).
