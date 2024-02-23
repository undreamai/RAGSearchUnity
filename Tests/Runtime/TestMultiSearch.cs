using NUnit.Framework;
using RAGSearchUnity;
using System.IO;
using System.Collections.Generic;

namespace RAGSearchUnityTests
{
    public class TestDialogueManager: TestWithEmbeddings
    {
        List<(string, string)> phrases = new List<(string, string)>(){
            ("To be, or not to be, that is the question. Whether tis nobler in the mind to suffer.", "Hamlet"),
            ("Or to take arms against a sea of troubles, and by opposing end them? To die—to sleep.", "Hamlet"),
            ("I humbly thank you; well, well, well.", "Hamlet"),
            ("Good my lord.", "Ophelia"),
            ("How does your honour for this many a day?", "Ophelia"),
            ("I humbly thank you; well, well, well.", "King"),
        };

        [Test]
        public void TestAdd()
        {
            MultiSearchEngine manager = new MultiSearchEngine(model);
            foreach (var phrase in phrases)
                manager.Add(phrase.Item1, phrase.Item2);
            Assert.AreEqual(manager.NumPhrases(), 6);
            Assert.AreEqual(manager.NumSentences(), 10);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 3);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 6);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 2);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 2);
            Assert.AreEqual(manager.NumPhrases("King"), 1);
            Assert.AreEqual(manager.NumSentences("King"), 2);

            Assert.AreEqual(manager.GetPhrases("Hamlet"), new string[] { phrases[0].Item1 , phrases[1].Item1 , phrases[2].Item1});
            string[] sentencesGT = phrases[5].Item1.Split(";");
            sentencesGT[0] += ";";
            sentencesGT[1] = sentencesGT[1].Trim();
            Assert.AreEqual(manager.GetSentences("King"), sentencesGT);

            manager.Add(phrases[3].Item1, phrases[3].Item2);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 3);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 3);
            manager.Remove(phrases[2].Item1);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 2);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 4);
            manager.Remove(phrases[3].Item1);
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 1);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 1);
            manager.Add(phrases[0].Item1, "Ophelia");
            manager.Remove(phrases[0].Item1, "Hamlet");
            Assert.AreEqual(manager.NumPhrases("Ophelia"), 2);
            Assert.AreEqual(manager.NumSentences("Ophelia"), 3);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 1);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 2);

            sentencesGT = phrases[1].Item1.Split("?");
            sentencesGT[0] += "?";
            sentencesGT[1] = sentencesGT[1].Trim();
            Assert.AreEqual(manager.GetSentences("Hamlet"), sentencesGT);
            Assert.AreEqual(manager.GetPhrases("Ophelia"), new string[] { phrases[4].Item1, phrases[0].Item1 });
        }

        [Test]
        public void TestSaveLoad()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            MultiSearchEngine manager = new MultiSearchEngine(model);
            manager.Save(path);
            MultiSearchEngine loadedManager = MultiSearchEngine.Load(model, path);
            File.Delete(path);

            Assert.AreEqual(manager.NumSentences(), loadedManager.NumSentences());
            Assert.AreEqual(manager.NumPhrases(), loadedManager.NumPhrases());

            foreach (var phrase in phrases)
                manager.Add(phrase.Item1, phrase.Item2);
            manager.Save(path);
            
            loadedManager = MultiSearchEngine.Load(model, path);
            File.Delete(path);

            Assert.AreEqual(manager.NumSentences(), loadedManager.NumSentences());
            Assert.AreEqual(manager.NumPhrases(), loadedManager.NumPhrases());

            manager.Remove(phrases[2].Item1);
            Assert.AreEqual(manager.NumPhrases("Hamlet"), 2);
            Assert.AreEqual(manager.NumSentences("Hamlet"), 4);
        }

        [Test]
        public void TestSearch()
        {
            MultiSearchEngine manager = new MultiSearchEngine(model);
            foreach (var phrase in phrases)
                manager.Add(phrase.Item1, phrase.Item2);
            manager.Add(phrases[0].Item1, "Ophelia");

            string[] results = manager.SearchPhrases(phrases[0].Item1, 2);
            Assert.AreEqual(results, new string[] { phrases[0].Item1, phrases[0].Item1 });

            results = manager.SearchPhrases(phrases[0].Item1, 2, "Hamlet");
            Assert.AreEqual(results[0], phrases[0].Item1);
            Assert.AreNotEqual(results[1], phrases[0].Item1);
            results = manager.SearchPhrases(phrases[1].Item1, 1, "Ophelia");
            Assert.AreNotEqual(results[0], phrases[1].Item1);
        }
    }
}
