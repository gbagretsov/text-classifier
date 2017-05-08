using KBCsv;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace Classifier.DataCollecting
{
    class VkParser
    {
        public static void ParseInformation(string email, string password)
        {
            string[] columns = {
                "text", "gender",
                "Отриц.", "Вытесн.", "Регрессия", "Компенсац.", "Проекц.", "Замещ.", "Рационализац.", "Гиперкомпенсац.", "Общ. Ур."
            };

            string inputFileName = @"C:\psych.csv";
            string outFileName = @"C:\vk-psych.csv";

            VkApi api = new VkApi();
            api.Authorize(5677623, email, password, Settings.Wall);

            using (var sr = new StreamReader(inputFileName))
            using (var reader = new CsvReader(sr))
            using (var sw = new StreamWriter(outFileName, false, Encoding.UTF8))
            using (var writer = new CsvWriter(sw))
            {
                reader.ValueSeparator = ',';

                writer.ForceDelimit = false;
                writer.ValueSeparator = ',';
                writer.ValueDelimiter = '\'';

                reader.ReadHeaderRecord();
                writer.WriteRecord(columns);

                while (reader.HasMoreRecords)
                {
                    var dataRecord = reader.ReadDataRecord();
                    
                    try
                    {
                        Sex userSex = api.Users.Get(long.Parse(dataRecord["id"]), ProfileFields.Sex).Sex;

                        List<Post> posts = api.Wall.Get(new WallGetParams()
                        {
                            OwnerId = long.Parse(dataRecord["id"]),
                            Filter = WallFilter.Owner,
                            Count = 100
                        }).WallPosts.ToList();

                        foreach (Post post in posts)
                        {
                            string s = post.Text + " ";
                            if (post.CopyHistory.Count != 0)
                            {
                                s += post.CopyHistory.First().Text;
                            }
                            s = s.Replace(Environment.NewLine, " ")
                                .Replace("\n", " ")
                                .Replace("\r", " ")
                                .Replace("\"", "")
                                .Replace(";", " ")
                                .Replace(",", " ")
                                .Replace("'", "")
                                .Trim();
                            if (s == string.Empty) continue;
                            
                            var outputRecord = new string[columns.Length];
                            outputRecord[0] = s;
                            outputRecord[1] = ((int)userSex - 1).ToString();
                            for (int i = 2; i < columns.Length; i++)
                            {
                                outputRecord[i] = dataRecord[columns[i]];
                            }

                            writer.WriteRecord(outputRecord);
                        }

                        System.Threading.Thread.Sleep(50);
                    }
                    catch (VkNet.Exception.InvalidParameterException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}
