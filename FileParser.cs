using KBCsv;
using System;
using System.Collections.Generic;
using System.IO;

namespace Classifier
{
    class FileParser
    {
        private static string inputFileName = @"C:\vk-psych.csv";

        public static string[] GetDocuments()
        {
            List<string> result = new List<string>();
            
            using (var sr = new StreamReader(inputFileName))
            using (var reader = new CsvReader(sr))
            {
                reader.ValueSeparator = ',';
                reader.ReadHeaderRecord();

                while (reader.HasMoreRecords)
                {
                    var dataRecord = reader.ReadDataRecord();
                    result.Add(dataRecord["text"]);
                }

                return result.ToArray();
            }
        }

        public static int[] GetВытеснение()
        {
            List<int> result = new List<int>();

            using (var sr = new StreamReader(inputFileName))
            using (var reader = new CsvReader(sr))
            {
                reader.ValueSeparator = ',';
                reader.ReadHeaderRecord();

                while (reader.HasMoreRecords)
                {
                    var dataRecord = reader.ReadDataRecord();
                    result.Add(GetClass(int.Parse(dataRecord["Вытесн."]), 20, 41));
                }

                return result.ToArray();
            }
        }

        // TODO: добавить все характеристики

        /// <summary>
        /// Определяет класс указанного значения
        /// </summary>
        /// <param name="value">Значение характеристики</param>
        /// <param name="divideMedium">Нижняя граница среднего класса</param>
        /// <param name="divideHigh">Нижняя граница высокого класса</param>
        /// <returns>Класс значения</returns>
        private static int GetClass(int value, byte divideMedium, byte divideHigh)
        {
            return value < divideMedium ? 0 : value < divideHigh ? 1 : 2;
        }

    }
}
