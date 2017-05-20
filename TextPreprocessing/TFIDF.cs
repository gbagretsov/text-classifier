﻿using Iveonik.Stemmers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Classifier.TextPreprocessing
{
    /// <summary>
    /// Copyright (c) 2013 Kory Becker http://www.primaryobjects.com/kory-becker.aspx
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining
    /// a copy of this software and associated documentation files (the
    /// "Software"), to deal in the Software without restriction, including
    /// without limitation the rights to use, copy, modify, merge, publish,
    /// distribute, sublicense, and/or sell copies of the Software, and to
    /// permit persons to whom the Software is furnished to do so, subject to
    /// the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be
    /// included in all copies or substantial portions of the Software.
    /// 
    /// Description:
    /// Performs a TF*IDF (Term Frequency * Inverse Document Frequency) transformation on an array of documents.
    /// Each document string is transformed into an array of doubles, cooresponding to their associated TF*IDF values.
    /// 
    /// Usage:
    /// string[] documents = LoadYourDocuments();
    ///
    /// double[][] inputs = TFIDF.Transform(documents);
    /// inputs = TFIDF.Normalize(inputs);
    /// 
    /// </summary>
    public static class TFIDF
    {
        /// <summary>
        /// Document vocabulary, containing each word's IDF value.
        /// </summary>
        private static Dictionary<string, double> _vocabularyIDF = new Dictionary<string, double>();

        private static Dictionary<string, double> _filteredVocabularyIDF = new Dictionary<string, double>();

        /// <summary>
        /// Transforms a list of documents into their associated TF*IDF values.
        /// If a vocabulary does not yet exist, one will be created, based upon the documents' words.
        /// </summary>
        /// <param name="documents">string[]</param>
        /// <param name="vocabularyThreshold">Minimum number of occurences of the term within all documents</param>
        /// <param name="extractUnigrams">Whether unigrams should be extracted</param>
        /// <param name="extractBigrams">Whether bigrams should be extracted</param>
        /// <param name="featuresAmount">Maximum amount of features included to the vocabulary (top N features with highest IDF value are selected)</param>
        /// <returns>double[][]</returns>
        public static double[][] Transform(string[] documents,
                                           bool extractUnigrams = true,
                                           int featuresAmount = int.MaxValue)
        {
            List<List<string>> stemmedDocs;
            List<string> vocabulary;

            int vocabularyThreshold = 2;

            // Get the vocabulary and stem the documents at the same time.
            vocabulary = GetVocabulary(documents, out stemmedDocs, vocabularyThreshold, extractUnigrams);

            Console.WriteLine();

            TryLoadVocabulary(extractUnigrams);

            if (_vocabularyIDF.Count == 0)
            {

                int i = 0;

                // Calculate the IDF for each vocabulary term.
                foreach (var term in vocabulary)
                { 

                    i++;
                    ClearCurrentConsoleLine();
                    Console.Write("Creating IDF dictionary: term " + i + " of " + vocabulary.Count);

                    double numberOfDocsContainingTerm;
                    if (term.Contains(" "))
                    { // биграмма?
                        // да, биграмма
                        numberOfDocsContainingTerm = stemmedDocs
                            .Select(doc => doc.Aggregate((prev, cur) => prev + " " + cur)) // преобразуем документы в строки
                            .Where(d => d.Contains(term))
                            .Count();
                    }
                    else
                    {
                        // нет, униграмма
                        numberOfDocsContainingTerm = stemmedDocs
                            .Where(d => d.Contains(term))
                            .Count();
                    }
                    _vocabularyIDF[term] = Math.Log((double)stemmedDocs.Count / ((double)1 + numberOfDocsContainingTerm));
                }

                SaveVocabulary(extractUnigrams);

            }

            _filteredVocabularyIDF = _vocabularyIDF.OrderByDescending(t => t.Value).Take(featuresAmount).ToDictionary(x => x.Key, x => x.Value);

            // Transform each document into a vector of tfidf values.
            return TransformToTFIDFVectors(stemmedDocs, _filteredVocabularyIDF);
        }

        /// <summary>
        /// Converts a list of stemmed documents (lists of stemmed words) and their associated vocabulary + idf values, into an array of TF*IDF values.
        /// </summary>
        /// <param name="stemmedDocs">List of List of string</param>
        /// <param name="vocabularyIDF">Dictionary of string, double (term, IDF)</param>
        /// <returns>double[][]</returns>
        private static double[][] TransformToTFIDFVectors(List<List<string>> stemmedDocs, Dictionary<string, double> vocabularyIDF)
        {
            Console.WriteLine("Counting TF-IDF...");
            int i = 0;

            // Transform each document into a vector of tfidf values.
            Dictionary<int, List<double>> vectors = new Dictionary<int, List<double>>(stemmedDocs.Count);

            //foreach (var doc in stemmedDocs)
            Parallel.For(0, stemmedDocs.Count, (index) =>
            {

                //i++;
                //ClearCurrentConsoleLine();
                //Console.Write("Counting TF-IDF: document " + i + " of " + stemmedDocs.Count);

                List<double> vector = new List<double>();

                foreach (var vocab in vocabularyIDF)
                {
                    // Term frequency = count how many times the term appears in this document.
                    double tf = 0;

                    if (vocab.Key.Contains(" "))
                    {
                        // биграмма
                        string s = stemmedDocs[index].Aggregate((prev, cur) => prev + " " + cur);
                        tf =
                            s.Split(' ').Length - 1 - // Длина документа, выраженная в биграммах
                            s.Replace(vocab.Key, "").Replace("  ", " ").Split(' ').Length + 1;
                        if (tf < 0)
                        {
                            throw new ArgumentException("tf < 0", "tf");
                        }
                    }
                    else
                    {
                        // униграмма
                        tf = stemmedDocs[index].Where(d => d == vocab.Key).Count();
                    }

                    double tfidf = tf * vocab.Value;

                    vector.Add(tfidf);
                }

                vectors.Add(index, vector);
            });

            Console.WriteLine();
            var result = vectors.OrderBy(v => v.Key).Select(v => v.Value.ToArray()).ToArray();
            return result;
        }

        /// <summary>
        /// Normalizes a TF*IDF array of vectors using L2-Norm.
        /// Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
        /// </summary>
        /// <param name="vectors">double[][]</param>
        /// <returns>double[][]</returns>
        public static double[][] Normalize(double[][] vectors)
        {
            // Normalize the vectors using L2-Norm.
            List<double[]> normalizedVectors = new List<double[]>();
            foreach (var vector in vectors)
            {
                var normalized = Normalize(vector);
                normalizedVectors.Add(normalized);
            }

            return normalizedVectors.ToArray();
        }

        /// <summary>
        /// Normalizes a TF*IDF vector using L2-Norm.
        /// Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
        /// </summary>
        /// <param name="vectors">double[][]</param>
        /// <returns>double[][]</returns>
        public static double[] Normalize(double[] vector)
        {
            List<double> result = new List<double>();

            double sumSquared = 0;
            foreach (var value in vector)
            {
                sumSquared += value * value;
            }

            double SqrtSumSquared = Math.Sqrt(sumSquared);

            foreach (var value in vector)
            {
                if (sumSquared == 0)
                {
                    result.Add(0);
                }
                else
                {
                    // L2-norm: Xi = Xi / Sqrt(X0^2 + X1^2 + .. + Xn^2)
                    result.Add(value / SqrtSumSquared);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Saves the TFIDF vocabulary to disk.
        /// </summary>
        /// <param name="filePath">File path</param>
        public static void SaveVocabulary(bool uni)
        {
            var filePath = uni ? "voc1.dat" : "voc2.dat";
            // Save result to disk.
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, _vocabularyIDF);
            }
        }
        
        /// <summary>
        /// Loads the TFIDF vocabulary from disk.
        /// </summary>
        /// <param name="filePath">File path</param>
        public static void TryLoadVocabulary(bool uni)
        {
            var filePath = uni ? "voc1.dat" : "voc2.dat";
            // Load from disk.
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    _vocabularyIDF = (Dictionary<string, double>)formatter.Deserialize(fs);
                }
            }
            catch (FileNotFoundException) { }
        }

        #region Private Helpers

        /// <summary>
        /// Parses and tokenizes a list of documents, returning a vocabulary of words.
        /// </summary>
        /// <param name="docs">string[]</param>
        /// <param name="stemmedDocs">List of List of string</param>
        /// <returns>Vocabulary (list of strings)</returns>
        private static List<string> GetVocabulary(string[] docs, out List<List<string>> stemmedDocs, int vocabularyThreshold, bool extractUnigrams)
        {
            List<string> vocabulary = new List<string>();
            Dictionary<string, int> wordCountList = new Dictionary<string, int>();
            stemmedDocs = new List<List<string>>();
            var stemmer = new RussianStemmer();

            int count = 0;

            foreach (var doc in docs)
            {

                count++;
                ClearCurrentConsoleLine();
                Console.Write("Extracting terms: document " + count + " of " + docs.Length);

                List<string> stemmedDoc = new List<string>();
                
                string[] parts2 = Tokenize(doc)
                    .Select(s => s.ToLower()) // Приводим слова в нижний регистр
                    .Select(s => Regex.Replace(s, "[^a-zA-Zа-яА-Я0-9]", "")) // Удаляем небуквенные символы
                    .Where(s => !StopWords.stopWordsList.Contains(s)) // Исключаем стоп-слова
                    .Where(s => s.Length > 0) // Исключаем пустые слова
                    .Select(s => stemmer.Stem(s))
                    .Distinct()
                    .ToArray();

                List<string> words = new List<string>();
                foreach (string part in parts2)
                {
                    // unigrams
                    if (extractUnigrams)
                    {
                        words.Add(part);
                        // Build the word count list.
                        if (wordCountList.ContainsKey(part))
                        {
                            wordCountList[part]++;
                        }
                        else
                        {
                            wordCountList.Add(part, 1);
                        }
                    }
                    
                    stemmedDoc.Add(part);
                }

                // bigrams
                if (!extractUnigrams)
                {
                    for (int i = 1; i < parts2.Length; i++)
                    {
                        string bigram = parts2[i - 1] + " " + parts2[i];
                        words.Add(bigram);
                        // Build the word count list.
                        if (wordCountList.ContainsKey(bigram))
                        {
                            wordCountList[bigram]++;
                        }
                        else
                        {
                            wordCountList.Add(bigram, 1);
                        }
                    }
                }

                if (stemmedDoc.Count > 0)
                {
                    stemmedDocs.Add(stemmedDoc);
                }
            }

            // Get the top words.
            var vocabList = wordCountList.Where(w => w.Value >= vocabularyThreshold);
            foreach (var item in vocabList)
            {
                vocabulary.Add(item.Key);
            }

            return vocabulary;
        }

        /// <summary>
        /// Tokenizes a string, returning its list of words.
        /// </summary>
        /// <param name="text">string</param>
        /// <returns>string[]</returns>
        private static string[] Tokenize(string text)
        {
            // Strip all HTML.
            text = Regex.Replace(text, "<[^<>]+>", "");

            // Strip numbers.
            text = Regex.Replace(text, "[0-9]+", "");

            // Strip urls.
            text = Regex.Replace(text, @"(http|https)://[^\s]*", "");

            // Strip email addresses.
            text = Regex.Replace(text, @"[^\s]+@[^\s]+", "");

            // Strip dollar sign.
            text = Regex.Replace(text, "[$]+", "");

            // Strip usernames.
            text = Regex.Replace(text, @"@[^\s]+", "usename");

            // Tokenize and also get rid of any punctuation
            return text.Split(" @$/#.-:&*+=[]?!(){},''\">_<;%\\".ToCharArray());
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        #endregion
    }
}
