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

        public SVM(double[][] inputs, int[] outputs, double ratio = 0.8, int? randomSeed = null)
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
                    // TODO: изменяемый гиперпараметр
                    Loss = Loss.L1,
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

        // TODO: считать accuracy, precision, recall

        public double[,] GetConfusionMatrix()
        {
            int[,] confusionMatrix = new int[3, 3];
            double[,] confusionPercentageMatrix = new double[3, 3];

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
