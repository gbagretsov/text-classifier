using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.MachineLearning.VectorMachines;
using Accord.Math.Optimization.Losses;
using Accord.IO;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Classifier.ClassifierModel
{
    class SVM
    {
        private double[][] inputs;
        private int[] outputs;

        double[][] trainSet;
        int[] trainAnswers;

        double[][] testSet;
        int[] testAnswers;

        private MulticlassSupportVectorLearning<Linear> teacher;
        private MulticlassSupportVectorMachine<Linear> model;

        public SVM(double[][] inputs, int[] outputs, 
                   double complexity, Loss loss, 
                   double ratio = 0.8, int? randomSeed = null)
        {
            this.inputs = inputs;
            this.outputs = outputs;

            ShuffleData(randomSeed);
            SplitData(ratio);

            // Create a one-vs-one multi-class SVM learning algorithm 
            teacher = new MulticlassSupportVectorLearning<Linear>()
            {
                // using LIBLINEAR's L2-loss SVC dual for each SVM
                Learner = (p) => new LinearDualCoordinateDescent()
                {
                    Loss = loss,
                    Complexity = complexity,
                    Tolerance = 1e-6
                }
            };
            
            teacher.ParallelOptions.MaxDegreeOfParallelism = 4;
        }

        public void Train()
        {
            model = teacher.Learn(trainSet, trainAnswers);

            // Create the multi-class learning algorithm for the machine
            var calibration = new MulticlassSupportVectorLearning<Linear>()
            {
                Model = model, // We will start with an existing machine

                // Configure the learning algorithm to use SMO to train the
                //  underlying SVMs in each of the binary class subproblems.
                Learner = (param) => new ProbabilisticOutputCalibration<Linear>()
                {
                    Model = param.Model // Start with an existing machine
                }
            };
            
            calibration.ParallelOptions.MaxDegreeOfParallelism = 4;
            calibration.Learn(trainSet, trainAnswers);

        }

        public void GetLoss(out double crossEntropyLoss, out double zeroOneLoss)
        {
            int[] predicted = model.Decide(testSet);
            // double[] scores = model.Score(testSet);
            // double[][] logl = model.LogLikelihoods(testSet);
            double[][] prob = model.Probabilities(testSet);

            crossEntropyLoss = new CategoryCrossEntropyLoss(testAnswers).Loss(prob);
            zeroOneLoss = new ZeroOneLoss(testAnswers).Loss(predicted);
        }
        
        public void GetPerformance(out double overallAccuracy,
                                   out double averageAccuracy,
                                   out double microAveragedPrecision,
                                   out double macroAveragedPrecision,
                                   out double microAveragedRecall,
                                   out double macroAveragedRecall,
                                   out double microAveragedF1Score,
                                   out double macroAveragedF1Score)
        {
            int[,] confusionMatrix = GetConfusionMatrix();
                        
            int TP0 = confusionMatrix[0, 0];
            int TP1 = confusionMatrix[1, 1];
            int TP2 = confusionMatrix[2, 2];
            
            int FP0 = confusionMatrix[1, 0] + confusionMatrix[2, 0];
            int FP1 = confusionMatrix[0, 1] + confusionMatrix[2, 1];
            int FP2 = confusionMatrix[0, 2] + confusionMatrix[1, 2];
            
            int TN0 = confusionMatrix[1, 1] + confusionMatrix[1, 2] + confusionMatrix[2, 1] + confusionMatrix[2, 2];
            int TN1 = confusionMatrix[0, 0] + confusionMatrix[0, 2] + confusionMatrix[2, 0] + confusionMatrix[2, 2];
            int TN2 = confusionMatrix[0, 0] + confusionMatrix[0, 1] + confusionMatrix[1, 0] + confusionMatrix[1, 1];

            int FN0 = confusionMatrix[0, 1] + confusionMatrix[0, 2];
            int FN1 = confusionMatrix[1, 0] + confusionMatrix[1, 2];
            int FN2 = confusionMatrix[2, 0] + confusionMatrix[2, 1];

            double PRE0 = (double)TP0 / (TP0 + FP0);
            double PRE1 = (double)TP1 / (TP1 + FP1);
            double PRE2 = (double)TP2 / (TP2 + FP2);

            double REC0 = (double)TP0 / (TP0 + FN0);
            double REC1 = (double)TP1 / (TP1 + FN1);
            double REC2 = (double)TP2 / (TP2 + FN2);

            // Overall accuracy: number of correctly predicted items / total of items to predict
            overallAccuracy = (double)(TP0 + TP1 + TP2) / testAnswers.Length;

            // Average accuracy is the average of each accuracy per class 
            // (sum of accuracy for each class predicted/number of class)
            averageAccuracy  = (TP0 + TN0) / (double)(TP0 + TN0 + FP0 + FN0);
            averageAccuracy += (TP1 + TN1) / (double)(TP1 + TN1 + FP1 + FN1);
            averageAccuracy += (TP2 + TN2) / (double)(TP2 + TN2 + FP2 + FN2);
            averageAccuracy /= 3;

            // Micro-avg Precision = sum(TP) / sum(TP+FP)
            microAveragedPrecision = (double) (TP0 + TP1 + TP2) / (TP0 + TP1 + TP2 + FP0 + FP1 + FP2);

            // Macro-avg Precision = sum(PRE) / classes
            macroAveragedPrecision = (PRE0 + PRE1 + PRE2) / 3;

            // Micro-avg Recall = sum(TP) / sum(TP+FN)
            microAveragedRecall = (double)(TP0 + TP1 + TP2) / (TP0 + TP1 + TP2 + FN0 + FN1 + FN2);

            // Macro-avg Recall = sum(REC) / classes
            macroAveragedRecall = (REC0 + REC1 + REC2) / 3;

            microAveragedF1Score = (double) (2 * (TP0 + TP1 + TP2)) / (2 * (TP0 + TP1 + TP2) + (FP0 + FP1 + FP2) + (FN0 + FN1 + FN2));
            macroAveragedF1Score =  2 * (
                (PRE0 * REC0) / (PRE0 + REC0) + 
                (PRE1 * REC1) / (PRE1 + REC1) + 
                (PRE2 * REC2) / (PRE2 + REC2)
            ) / 3;
        }

        public int[,] GetConfusionMatrix()
        {
            int[,] confusionMatrix = new int[3, 3];

            int[] predicted = model.Decide(testSet);

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

            return confusionMatrix;      
        }

        public double[,] GetConfusionPercentageMatrix()
        {
            int[,] confusionMatrix = GetConfusionMatrix();
            double[,] confusionPercentageMatrix = new double[3, 3];

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    confusionPercentageMatrix[i, j] =
                        (double)confusionMatrix[i, j] * 100 /
                        (confusionMatrix[i, 0] + confusionMatrix[i, 1] + confusionMatrix[i, 2]);

            return confusionPercentageMatrix;
        }

        public void SaveToFile(string path)
        {
            Serializer.Save(model, path);
        }

        private void ShuffleData(int? randomSeed)
        {
            Dictionary<double[], int> d = new Dictionary<double[], int>();
            for (int i = 0; i < inputs.Length; i++)
            {
                d.Add(inputs[i], outputs[i]);
            }

            Random rnd = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
            d = d.OrderBy(x => rnd.Next()).ToDictionary(x => x.Key, x => x.Value);

            inputs = d.Keys.ToArray();
            outputs = d.Values.ToArray();
        }

        private void SplitData(double ratio)
        {
            int train = Convert.ToInt32(Math.Floor(inputs.Length * ratio));
            int test = inputs.Length - train;

            trainSet = inputs.Take(train).ToArray();
            trainAnswers = outputs.Take(train).ToArray();

            testSet = inputs.Skip(train).Take(test).ToArray();
            testAnswers = outputs.Skip(train).Take(test).ToArray();
        }

    }
}
