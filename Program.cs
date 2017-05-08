using System;

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

            double[][] inputs = TFIDF.Transform(documents, 5);
            inputs = TFIDF.Normalize(inputs);

            // TODO: добавить все характеристики
            int[] outputsВытеснение = FileParser.GetВытеснение();

            // TODO: собирать статистику: лучший/худший/средний проход
            for (int iter = 0; iter < 5; iter++)
            {
                Console.WriteLine("Training...");
                DateTime start = DateTime.Now;

                SVM svmВытеснение = new SVM(inputs, outputsВытеснение);
                svmВытеснение.Train();

                DateTime finish = DateTime.Now;
                double time = (finish - start).TotalSeconds;
                Console.WriteLine("Elapsed time: " + time + "s, loss: " + svmВытеснение.GetLoss());

                Console.WriteLine("Confusion matrix - Вытеснение:");
                var confusionMatrixВытеснение = svmВытеснение.GetConfusionMatrix();
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Console.Write(string.Format("{0:0.00}\t", confusionMatrixВытеснение[i, j]));
                    }
                    Console.WriteLine();
                }
            }

            Console.ReadKey();

        }
    }
}
