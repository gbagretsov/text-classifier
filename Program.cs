using System;
using System.Collections.Generic;

using Classifier.DataCollecting;
using Classifier.TextPreprocessing;
using Classifier.ClassifierModel;
using Accord.MachineLearning.VectorMachines.Learning;
using System.IO;
using KBCsv;
using System.Text;

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

            // Параметры экспериментов
            int[] featuresCount = new int[] { 1500, 3000, 4500, 6000, 7500, 9000 };
            float[] complexity = new float[] { 0.1f, 0.5f, 1, 2, 5 };
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

            foreach (var item in traits)
            {
                Console.WriteLine(item.Key);

                using (var sw = new StreamWriter("results_" + item.Key + ".csv", false, Encoding.UTF8))
                using (var writer = new CsvWriter(sw))
                {
                    writer.ForceDelimit = false;
                    writer.ValueSeparator = ';';
                    writer.ValueDelimiter = '\'';

                    string[] columns = {
                        "trait", "N", "features", "C", "loss",

                        "ovrAcc",      "avgAcc",
                        "microAvgPre", "macroAvgPre",
                        "microAvgRec", "macroAvgRec",
                        "microAvgF1",  "macroAvgF1"
                    };
                    writer.WriteRecord(columns);

                    for (bool unigrams = true; ; unigrams = false)
                    {
                        foreach (int features in featuresCount)
                        {
                            Console.WriteLine("Calculating TF-IDF...");

                            double[][] inputs = TFIDF.Transform(documents, extractUnigrams: unigrams, featuresAmount: features);
                            inputs = TFIDF.Normalize(inputs);

                            Console.WriteLine("Training...");

                            foreach (float c in complexity)
                                foreach (var l in loss)
                                {
                                    Console.WriteLine(string.Format("N = {0}, features = {1}, C = {2}, loss = {3} ",
                                        unigrams ? 1 : 2, features, c, l == Loss.L1 ? "L1" : "L2"));

                                    double overallAccuracy = 0, averageAccuracy = 0;
                                    double microAveragedPrecision = 0, macroAveragedPrecision = 0;
                                    double microAveragedRecall = 0, macroAveragedRecall = 0;
                                    double microAveragedF1Score = 0, macroAveragedF1Score = 0;
                                    double time = 0;

                                    for (int iter = 0; iter < 10; iter++)
                                    {
                                        double overallAccuracyCur, averageAccuracyCur;
                                        double microAveragedPrecisionCur, macroAveragedPrecisionCur;
                                        double microAveragedRecallCur, macroAveragedRecallCur;
                                        double microAveragedF1ScoreCur, macroAveragedF1ScoreCur;

                                        DateTime start = DateTime.Now;

                                        SVM svm = new SVM(inputs, item.Value, c, l);
                                        svm.Train();

                                        DateTime finish = DateTime.Now;
                                        time += (finish - start).TotalSeconds;

                                        svm.GetPerformance(out overallAccuracyCur,
                                                           out averageAccuracyCur,
                                                           out microAveragedPrecisionCur,
                                                           out macroAveragedPrecisionCur,
                                                           out microAveragedRecallCur,
                                                           out macroAveragedRecallCur,
                                                           out microAveragedF1ScoreCur,
                                                           out macroAveragedF1ScoreCur);

                                        overallAccuracy += overallAccuracyCur;
                                        averageAccuracy += averageAccuracyCur;
                                        microAveragedPrecision += microAveragedPrecisionCur;
                                        macroAveragedPrecision += macroAveragedPrecisionCur;
                                        microAveragedRecall += microAveragedRecallCur;
                                        macroAveragedRecall += macroAveragedRecallCur;
                                        microAveragedF1Score += microAveragedF1ScoreCur;
                                        macroAveragedF1Score += macroAveragedF1ScoreCur;
                                    }

                                    overallAccuracy /= 10;
                                    averageAccuracy /= 10;
                                    microAveragedPrecision /= 10;
                                    macroAveragedPrecision /= 10;
                                    microAveragedRecall /= 10;
                                    macroAveragedRecall /= 10;
                                    microAveragedF1Score /= 10;
                                    macroAveragedF1Score /= 10;
                                    time /= 10;

                                    var outputRecord = new string[columns.Length];
                                    // "trait", "N", "features", "C", "loss",
                                    // "overallAccuracy", "averageAccuracy",
                                    // "microAveragedPrecision", "macroAveragedPrecision",
                                    // "microAveragedRecall", "macroAveragedRecall",
                                    // "microAveragedF1Score", "macroAveragedF1Score"
                                    outputRecord[0]  = item.Key;
                                    outputRecord[1]  = unigrams ? "1" : "2";
                                    outputRecord[2]  = Convert.ToString(features);
                                    outputRecord[3]  = Convert.ToString(c);
                                    outputRecord[4]  = l == Loss.L1 ? "L1" : "L2";

                                    outputRecord[5]  = Convert.ToString(overallAccuracy);
                                    outputRecord[6]  = Convert.ToString(averageAccuracy);
                                    outputRecord[7]  = Convert.ToString(microAveragedPrecision);
                                    outputRecord[8]  = Convert.ToString(macroAveragedPrecision);
                                    outputRecord[9]  = Convert.ToString(microAveragedRecall);
                                    outputRecord[10] = Convert.ToString(macroAveragedRecall);
                                    outputRecord[11] = Convert.ToString(microAveragedF1Score);
                                    outputRecord[12] = Convert.ToString(macroAveragedF1Score);

                                    writer.WriteRecord(outputRecord);
                                }
                        }

                        if (unigrams == true) break;
                    }

                }
            }


            Console.ReadKey();
        }

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
