using KBCsv;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
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

                    long id;
                    string domain = long.TryParse(dataRecord["id"], out id) ? "id" + id.ToString() : dataRecord["id"];
                    List<Post> posts = new List<Post>();

                    try
                    {
                        uint count = 0;

                        try
                        {
                            posts = api.Wall.Get(new WallGetParams()
                            {
                                Domain = domain,
                                Filter = WallFilter.Owner,
                                Count = 100
                            }).WallPosts.ToList();
                            count = (uint)posts.Count;
                        }
                        catch (InvalidParameterException)
                        {
                            // Пытаемся пропустить неопознаваемые записи                        
                            for (uint i = 0; i < 100; i++)
                            {
                                Thread.Sleep(500);
                                try
                                {
                                    var curPost = api.Wall.Get(new WallGetParams()
                                    {
                                        Domain = domain,
                                        Filter = WallFilter.Owner,
                                        Count = 1,
                                        Offset = i
                                    }).WallPosts.ToList();
                                    if (curPost.Count == 1)
                                    {
                                        posts.Add(curPost[0]);
                                        count++;
                                    }
                                }
                                catch (InvalidParameterException)
                                {
                                    Console.WriteLine(string.Format("Skipped {0} post from {1}", i, domain));
                                }
                            }
                        }                            

                        Console.WriteLine(string.Format("Got {0} posts from {1}", count, domain));

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
                            for (int i = 1; i < columns.Length; i++)
                            {
                                outputRecord[i] = dataRecord[columns[i]];
                            }

                            writer.WriteRecord(outputRecord);
                        }

                        Thread.Sleep(500);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(domain + ": " + e.Message);
                    }
                }
            }
        }
    }
}
