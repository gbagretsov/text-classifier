using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Kernels;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Classifier
{
    class Program
    {
        static void Main(string[] args)
        {
            // VkParser.ParseInformation(args[0], args[1]);

            string[] documents = FileParser.GetDocuments();

            DateTime start = DateTime.Now;

            Console.WriteLine("Calculating TF-IDF...");

            double[][] inputs = TFIDF.Transform(documents, 5);
            inputs = TFIDF.Normalize(inputs);

            // TODO: добавить все характеристики
            int[] outputs = FileParser.GetВытеснение();

            Dictionary<double[], int> d = new Dictionary<double[], int>();
            for (int i = 0; i < inputs.Length; i++)
            {
                d.Add(inputs[i], outputs[i]);
            }

            Random rnd = new Random(123);
            d = d.OrderBy(x => rnd.Next()).ToDictionary(x => x.Key, x => x.Value);

            inputs  = d.Keys.ToArray();
            outputs = d.Values.ToArray();

            double ratio = 0.8;

            int train = Convert.ToInt32(Math.Floor(inputs.Length * ratio));
            int test = inputs.Length - train;

            double[][] trainSet = inputs.Take(train).ToArray();
            int[] trainAnswers = outputs.Take(train).ToArray();

            double[][] testSet = inputs.Skip(train).Take(test).ToArray();
            int[] testAnswers = outputs.Skip(train).Take(test).ToArray();

            Console.WriteLine("Training...");

            // TODO: сделать обёртку
            // Create a one-vs-one multi-class SVM learning algorithm 
            var teacher = new MulticlassSupportVectorLearning<Linear>()
            {
                // using LIBLINEAR's L2-loss SVC dual for each SVM
                Learner = (p) => new LinearDualCoordinateDescent()
                {
                    Loss = Loss.L2,
                    Tolerance = 1e-6
                }
            };

            // Configure parallel execution options
            teacher.ParallelOptions.MaxDegreeOfParallelism = 4;

            // Learn a machine
            var Вытеснение = teacher.Learn(trainSet, trainAnswers);

            // Obtain class predictions for each sample
            int[] predicted = Вытеснение.Decide(testSet);

            // Compute classification error
            double error = new ZeroOneLoss(testAnswers).Loss(predicted);

            DateTime finish = DateTime.Now;
            double time = (finish - start).TotalSeconds;

            Console.WriteLine("Time elapsed: " + time + "s, zero-one loss " + error);

            int[,] confusionMatrix = new int[3, 3];

            for (int i = 0; i < testAnswers.Length; i++)
            {
                if (testAnswers[i] == 0 && predicted[i] == 0) confusionMatrix[0, 0]++;
                if (testAnswers[i] == 0 && predicted[i] == 1) confusionMatrix[0, 1]++;
                if (testAnswers[i] == 0 && predicted[i] == 2) confusionMatrix[0, 2]++;

                if (testAnswers[i] == 1 && predicted[i] == 0) confusionMatrix[1, 0]++;
                if (testAnswers[i] == 1 && predicted[i] == 1) confusionMatrix[1, 1]++;
                if (testAnswers[i] == 1 && predicted[i] == 2) confusionMatrix[1, 2]++;

                if (testAnswers[i] == 2 && predicted[i] == 0) confusionMatrix[2, 0]++;
                if (testAnswers[i] == 2 && predicted[i] == 1) confusionMatrix[2, 1]++;
                if (testAnswers[i] == 2 && predicted[i] == 2) confusionMatrix[2, 2]++;
            }

            Console.WriteLine("Confusion matrix:");
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Console.Write(confusionMatrix[i, j] + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine("Confusion matrix - percentage:");
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Console.Write(string.Format("{0:0.00}\t", (double)confusionMatrix[i, j] * 100 / (confusionMatrix[i, 0] + confusionMatrix[i, 1] + confusionMatrix[i, 2])));
                }
                Console.WriteLine();
            }
            Console.ReadKey();

        }
        

    }
}
