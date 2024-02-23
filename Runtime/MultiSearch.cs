using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Unity.Sentis;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;
using Cloud.Unum.USearch;

namespace RAGSearchUnity
{
    [DataContract]
    public class MultiSearchEngine
    {
        Dictionary<string, SearchEngine> engines;
        [DataMember]
        string delimiters;
        [DataMember]
        EmbeddingModel embedder;
        [DataMember]
        ScalarKind quantization;

        public MultiSearchEngine(
            EmbeddingModel embedder,
            string delimiters = SentenceSplitter.DefaultDelimiters,
            ScalarKind quantization = ScalarKind.Float16
        )
        {
            engines = new Dictionary<string, SearchEngine>();
            this.embedder = embedder;
            this.delimiters = delimiters;
            this.quantization = quantization;
        }

        public EmbeddingModel GetEmbedder()
        {
            return embedder;
        }

        public void SetEmbedder(EmbeddingModel model)
        {
            embedder = model;
        }

        public void Add(string text, string engineID="")
        {
            if (!engines.ContainsKey(engineID))
            {
                engines[engineID] = new SearchEngine(embedder, delimiters, quantization);
            }
            engines[engineID].Add(text);
        }

        public SearchEngine[] Filter(string engineID = null)
        {
            if (engineID == null) return engines.Values.ToArray();
            return new SearchEngine[]{engines[engineID]};
        }

        public int Remove(string text, string engineID=null)
        {
            SearchEngine[] engines = Filter(engineID);
            int removed = 0;
            foreach (SearchEngine engine in engines)
            {
                removed += engine.Remove(text);
            }
            return removed;
        }

        public string[] Get(string engineID = null, bool returnSentences = false)
        {
            SearchEngine[] engines = Filter(engineID);
            List<string> result = new List<string>();
            foreach (SearchEngine engine in engines)
            {
                result.AddRange(returnSentences ? engine.GetSentences() : engine.GetPhrases());
            }
            return result.ToArray();
        }

        public string[] GetPhrases(string engineID = null)
        {
            return Get(engineID, false);
        }

        public string[] GetSentences(string engineID = null)
        {
            return Get(engineID, true);
        }

        public string[] Search(string queryString, int k, out float[] distances, string engineID = null, bool returnSentences = false)
        {
            SearchEngine[] engines = Filter(engineID);
            if (engines.Length == 0)
            {
                distances = null;
                return null;
            }
            if (engines.Length == 1)
            {
                return engines[0].Search(queryString, k, out distances, returnSentences);
            }

            TensorFloat encodingTensor = embedder.Encode(queryString);
            encodingTensor.MakeReadable();
            float[] encoding = encodingTensor.ToReadOnlyArray();

            ConcurrentBag<(string, float)> resultPairs = new ConcurrentBag<(string, float)>();
            Task.Run(() =>
            {
                Parallel.ForEach(engines, engine =>
                {
                    string[] searchResults = engine.Search(encoding, k, out float[] searchDistances, returnSentences);
                    for (int i = 0; i < searchResults.Length; i++)
                    {
                        resultPairs.Add((searchResults[i], searchDistances[i]));
                    }
                });
            }).Wait();

            var sortedLists = resultPairs.OrderBy(item => item.Item2).ToList();
            int kmax = k == -1 ? sortedLists.Count : Math.Min(k, sortedLists.Count);
            string[] results = new string[kmax];
            distances = new float[kmax];
            for (int i = 0; i < kmax; i++)
            {
                results[i] = sortedLists[i].Item1;
                distances[i] = sortedLists[i].Item2;
            }
            return results;
        }

        public string[] Search(string queryString, int k=1, string engineID = null, bool returnSentences = false)
        {
            return Search(queryString, k, out float[] distances, engineID, returnSentences);
        }

        public string[] SearchPhrases(string queryString, int k, out float[] distances, string engineID = null)
        {
            return Search(queryString, k, out distances, engineID, false);
        }

        public string[] SearchPhrases(string queryString, int k=1, string engineID = null)
        {
            return SearchPhrases(queryString, k, out float[] distances, engineID);
        }

        public string[] SearchSentences(string queryString, int k, out float[] distances, string engineID = null)
        {
            return Search(queryString, k, out distances, engineID, true);
        }

        public string[] SearchSentences(string queryString, int k=1, string engineID = null)
        {
            return SearchSentences(queryString, k, out float[] distances, engineID);
        }

        public int NumPhrases(string engineID = null)
        {
            int num = 0;
            SearchEngine[] engines = Filter(engineID);
            foreach (SearchEngine engine in engines)
            {
                num += engine.NumPhrases();
            }
            return num;
        }

        public int NumSentences(string engineID = null)
        {
            int num = 0;
            SearchEngine[] engines = Filter(engineID);
            foreach (SearchEngine engine in engines)
            {
                num += engine.NumSentences();
            }
            return num;
        }

        public static string GetModelHashPath(string dirname)
        {
            return Path.Combine(dirname, "EmbedderHash.txt");
        }

        public static string GetOutputPath(string dirname)
        {
            return Path.Combine(dirname, "MultiSearchEngine.json");
        }

        public static string GetEngineEntriesPath(string dirname)
        {
            return Path.Combine(dirname, "EngineEntries.csv");
        }

        public void Save(string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Save(archive, dirname);
            }
        }

        public void Save(ZipArchive archive, string dirname = "")
        {
            Saver.Save(this, archive, GetOutputPath(dirname));
            
            embedder.SaveHashCode(archive, dirname);

            List<string> dialoguePartLines = new List<string>();
            foreach ((string engineID, SearchEngine engine) in engines)
            {
                string basedir = $"{dirname}/Dialogues/{Saver.EscapeFileName(engineID)}";
                engine.Save(archive, basedir);

                dialoguePartLines.Add(engineID);
                dialoguePartLines.Add(basedir);
            }
            

            ZipArchiveEntry dialoguesEntry = archive.CreateEntry(GetEngineEntriesPath(dirname));
            using (StreamWriter writer = new StreamWriter(dialoguesEntry.Open()))
            {
                foreach (string line in dialoguePartLines)
                {
                    writer.WriteLine(line);
                }
            }
        }

        public static MultiSearchEngine Load(EmbeddingModel embedder, string filePath, string dirname = "")
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                MultiSearchEngine engine = Saver.Load<MultiSearchEngine>(archive, GetOutputPath(dirname));

                int embedderHash = EmbeddingModel.LoadHashCode(archive, dirname);
                if (embedder.GetHashCode() != embedderHash)
                    throw new Exception($"The MultiSearchEngine object uses different embedding model than the MultiSearchEngine object stored in {filePath}");
                engine.SetEmbedder(embedder);

                ZipArchiveEntry dialoguesEntry = archive.GetEntry(GetEngineEntriesPath(dirname));
                List<string> dialogueDirs = new List<string>();
                engine.engines = new Dictionary<string, SearchEngine>();
                using (StreamReader reader = new StreamReader(dialoguesEntry.Open()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string engineID = line;
                        string basedir = reader.ReadLine();
                        engine.engines[engineID] = SearchEngine.Load(embedder, archive, basedir);
                    }
                }
                return engine;
            }
        }
    }
}
