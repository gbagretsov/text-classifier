using KBCsv;

using System.Collections.Generic;
using System.IO;

namespace Classifier.DataCollecting
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

        /// <summary>
        /// Отрицание
        /// </summary>
        public static int[] GetDenial()
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
                    result.Add(GetClass(byte.Parse(dataRecord["Отриц."]), 25, 46));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Вытеснение
        /// </summary>
        public static int[] GetRepression()
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
                    result.Add(GetClass(byte.Parse(dataRecord["Вытесн."]), 20, 41));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Регрессия
        /// </summary>
        public static int[] GetRegression()
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
                    result.Add(GetClass(byte.Parse(dataRecord["Регрессия"]), 25, 46));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Компенсация
        /// </summary>
        public static int[] GetCompensation()
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
                    result.Add(GetClass(byte.Parse(dataRecord["Компенсац."]), 20, 41));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Проекция
        /// </summary>
        public static int[] GetProjection()
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
                    result.Add(GetClass(byte.Parse(dataRecord["Проекц."]), 50, 71));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Замещение
        /// </summary>
        public static int[] GetDisplacement()
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
                    result.Add(GetClass(byte.Parse(dataRecord["Замещ."]), 20, 41));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Рационализация
        /// </summary>
        public static int[] GetRationalization()
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
                    result.Add(GetClass(byte.Parse(dataRecord["Рационализац."]), 40, 61));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Реактивное образование (гиперкомпенсация)
        /// </summary>
        public static int[] GetReactionFormation()
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
                    byte gender = byte.Parse(dataRecord["gender"]);
                    byte div1, div2;
                    if (gender == 0)
                    {
                        div1 = 30;
                        div2 = 51;
                    }
                    else
                    {
                        div1 = 11;
                        div2 = 31;
                    }
                    result.Add(GetClass(byte.Parse(dataRecord["Гиперкомпенсац."]), div1, div2));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Общий уровень
        /// </summary>
        public static int[] GetOverallLevel()
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
                    result.Add(GetClass(byte.Parse(dataRecord["Общ. Ур."]), 30, 51));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        /// Определяет класс указанного значения
        /// </summary>
        /// <param name="value">Значение характеристики</param>
        /// <param name="divideMedium">Нижняя граница среднего класса</param>
        /// <param name="divideHigh">Нижняя граница высокого класса</param>
        /// <returns>Класс значения</returns>
        private static int GetClass(byte value, byte divideMedium, byte divideHigh)
        {
            return value < divideMedium ? 0 : value < divideHigh ? 1 : 2;
        }

    }
}
