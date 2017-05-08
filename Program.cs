using System;
using System.Collections.Generic;

using Classifier.DataCollecting;
using Classifier.TextPreprocessing;
using Classifier.ClassifierModel;

namespace Classifier
{
    class Program
    {
        static void Main(string[] args)
        {
            // VkParser.ParseInformation(args[0], args[1]);

            string[] documents = FileParser.GetDocuments();
            
            Console.WriteLine("Calculating TF-IDF...");

            TFIDF.TryLoadVocabulary();
            double[][] inputs = TFIDF.Transform(documents);
            inputs = TFIDF.Normalize(inputs);
            TFIDF.SaveVocabulary();

            Dictionary<string, int[]> traits = new Dictionary<string, int[]>(9);

            traits.Add("Denial",             FileParser.GetDenial());
            traits.Add("Repression",         FileParser.GetRepression());
            traits.Add("Regression",         FileParser.GetRegression());
            traits.Add("Compensation",       FileParser.GetCompensation());
            traits.Add("Projection",         FileParser.GetProjection());
            traits.Add("Displacement",       FileParser.GetDisplacement());
            traits.Add("Rationalization",    FileParser.GetRationalization());
            traits.Add("Reaction Formation", FileParser.GetReactionFormation());
            traits.Add("Overall Level",      FileParser.GetOverallLevel());

            Console.WriteLine("Training...");

            foreach (var item in traits)
            {            
                // TODO: собирать статистику: лучший/худший/средний проход
                for (int iter = 0; iter < 1; iter++)
                {
                    DateTime start = DateTime.Now;

                    SVM svm = new SVM(inputs, item.Value);
                    svm.Train();

                    DateTime finish = DateTime.Now;
                    double time = (finish - start).TotalSeconds;

                    OutputLog(item.Key, iter, time, svm.GetLoss(), svm.GetConfusionMatrix());            
                }
            }

            Console.ReadKey();

        }

        // TODO: запись в файл
        private static void OutputLog(string name, int iteration, double time, double loss, double[,] matrix)
        {
            Console.WriteLine(string.Format("\n{0}, iteration {1} - elapsed time: {2:0.00}s, loss: {3:0.000}", 
                name, iteration, time, loss));
            OutputConfusionMatrix(matrix);
        }

        private static void OutputConfusionMatrix(double[,] matrix)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Console.Write(string.Format("{0:0.00}\t", matrix[i, j]));
                }
                Console.WriteLine();
            }
        }

    }
}
