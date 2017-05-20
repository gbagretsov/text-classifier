using System;
using System.Collections.Generic;

using Classifier.DataCollecting;
using Classifier.TextPreprocessing;
using Classifier.ClassifierModel;
using Accord.MachineLearning.VectorMachines.Learning;

namespace Classifier
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                VkParser.ParseInformation(args[0], args[1]);
                Console.ReadKey();
            }

            string[] documents = FileParser.GetDocuments();
            
            Console.WriteLine("Calculating TF-IDF...");

            double[][] inputs = TFIDF.Transform(documents, extractUnigrams: true);
            inputs = TFIDF.Normalize(inputs);

            // Параметры экспериментов
            int[] featuresUni = new int[] { 7500, 10000, 12500, 15000, 17500, 20000 };
            int[] featuresBi  = new int[] { 1000, 2000, 3500, 5000, 6500, 8000 };
            double[] complexity = new double[] { 0.1, 0.5, 1, 2, 5 };
            Loss[] loss = new Loss[] { Loss.L1, Loss.L2 };

            Dictionary<string, int[]> traits = new Dictionary<string, int[]>(9);

            //traits.Add("Denial",             FileParser.GetDenial());
            traits.Add("Repression",         FileParser.GetRepression());
            //traits.Add("Regression",         FileParser.GetRegression());
            //traits.Add("Compensation",       FileParser.GetCompensation());
            //traits.Add("Projection",         FileParser.GetProjection());
            //traits.Add("Displacement",       FileParser.GetDisplacement());
            //traits.Add("Rationalization",    FileParser.GetRationalization());
            //traits.Add("Reaction Formation", FileParser.GetReactionFormation());
            //traits.Add("Overall Level",      FileParser.GetOverallLevel());

            Console.WriteLine("Training...");

            foreach (var item in traits)
            {            
                // TODO: собирать статистику: лучший/худший/средний проход
                for (int iter = 0; iter < 10; iter++)
                {
                    DateTime start = DateTime.Now;

                    SVM svm = new SVM(inputs, item.Value, 0.1, Loss.L1);
                    svm.Train();

                    DateTime finish = DateTime.Now;
                    double time = (finish - start).TotalSeconds;

                    double crossEntropyLoss, zeroOneLoss;
                    svm.GetLoss(out crossEntropyLoss, out zeroOneLoss);
                    OutputLog(item.Key, iter, time, crossEntropyLoss, zeroOneLoss, svm.GetConfusionMatrix());

                    svm.SaveToFile(@"Models\" + item.Key + ".dat");
                }
            }

            Console.ReadKey();

        }

        // TODO: запись в файл
        private static void OutputLog
            (string name, int iteration, double time, double crossEntropyLoss, double zeroOneLoss, double[,] matrix)
        {
            Console.WriteLine(string.Format
                ("\n{0}, iteration {1} - elapsed time: {2:0.00}s, cross entropy loss: {3:0.000}, zero-one loss: {4:0.000}", 
                name, iteration, time, crossEntropyLoss, zeroOneLoss));
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
