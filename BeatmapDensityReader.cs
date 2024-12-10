// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable All

namespace osu.Game.Screens.Select.Carousel
{
    public class BeatmapDensityReader
    {
        public static Dictionary<string, double> cachedMaps = new Dictionary<string, double>();
        private static string path = GetOsuPath() + @"/files";
        private static bool Preloaded_Dict = false;
        private static int processed = 0;
        private const double SLIDER_IMPORTANCE = 0.25;
        private const double BREAK_IMPORTANCE = 0.25;
        private const double STREAMOBJ_PERCENT = 0.20;
        private const double GOOD_DELTATIME = 89;
        private static readonly double GOOD_DELTATIMELOG10 = Math.Log10(GOOD_DELTATIME);
        public static double GetDensity(string beatmapInfoHash, int totalObjectCount)
        {
            string hash = beatmapInfoHash;
            if(Preloaded_Dict == false)
            {
            if (File.Exists(path + "/moorf_caches.txt")) cachedMaps = ImportKeyValuePairs(path + "/moorf_caches.txt");
            else File.Create(path + "/moorf_caches.txt");
            }
            Preloaded_Dict = true;
            cachedMaps.TryGetValue(hash, out var z);
            if (z != 0) return z;
            if (totalObjectCount < 2) return -1;

            int sliderCount = 0;
            double totalTimeToSubtract = 0;
            List<double> times = new List<double>(1000);
            List<double> futuretimes = new List<double>(1000);
            double totalBreakTimeToSubtract = 0;
            string hitObjectsStr = GetStringHitObjects(hash); if (hitObjectsStr is null or "err") return -1;
            List<string> lines = new List<string>(hitObjectsStr.Split('\n'));

            for (int i = 1; i < lines.Count - 1; i++)
            {
                List<string> hitObj_1 = new List<string>(lines[i].Split(','));
                List<string> hitObj_2 = new List<string>(lines[i + 1].Split(','));

                double deltaTime = (Double.Parse(hitObj_2[2]) - Double.Parse(hitObj_1[2]));

                if (!lines[i].Contains('|'))
                {
                    times.Add(deltaTime);
                }
                else
                {
                    sliderCount++;
                    totalTimeToSubtract += deltaTime * SLIDER_IMPORTANCE;
                }
                if (deltaTime > 1000)
                {
                    totalBreakTimeToSubtract += deltaTime * BREAK_IMPORTANCE;
                    futuretimes.Add(deltaTime);
                }
            }

            if (times.Count <= 0) return -1;
            times.Sort();
            //double avg = times[(int)(times.Count * 0.5)];
            int index = (int)(times.Count * STREAMOBJ_PERCENT);
            double avg2 = times[index];

            int countHigher = 0;

            foreach (double i in times)
            {
                countHigher++;

                if ((avg2 > GOOD_DELTATIME && i > avg2) || i > GOOD_DELTATIME) //more streams captures then
                {
                    avg2 = times.Take(countHigher).Average();
                    break;
                }
            }

            int futuresCount = avg2 < GOOD_DELTATIME ? futuretimes.Count : 1;
            futuresCount = Math.Max(1, futuresCount);
            double result = DoMath(countHigher, times.Count - futuresCount, avg2);
            result = Math.Max(result, 0.01);
            cachedMaps.Add(hash, result);

            if (processed++ % 10 == 0 && File.Exists(path + "/moorf_caches.txt"))
            {
                foreach (var i in cachedMaps.Skip(processed-10))
                {
                    File.AppendAllText(path + "/moorf_caches.txt", i.Key + '|' + Math.Round(i.Value, 2) + '\n');
                }
            }
            return result;
        }
#if DEBUG
        private const string base_game_name = @"osu-development";
#else
        private const string base_game_name = @"osu";
#endif
        public static string GetOsuPath()
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), base_game_name);
            if (Directory.Exists(path))
                return path;
            return "/home/moorf/.local/share/osu";
        }
        private static string GetStringHitObjects(string hash)
        {
            string fullpath = path + '/' + hash.Substring(0, 1) + '/' + hash.Substring(0, 2) + '/' + hash;
            if (!System.IO.File.Exists(fullpath)) return "err";
            string map = System.IO.File.ReadAllText(fullpath);
            int gd = map.IndexOf("[HitObjects]");
            string result = map.Substring(gd);
            if (result == null || result.Length < 3)
            {
                return "err";
            }
            return result.Trim();
        }

        public static Dictionary<string, double> ImportKeyValuePairs(string filePath)
        {
            var dictionary = new Dictionary<string, double>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split('|');
                if (parts.Length == 2 && double.TryParse(parts[1], out var value))
                {
                    dictionary[parts[0]] = value;
                }
            }

            return dictionary;
        }

        private static double DoMath(double countHigher, double timesCount, double avgDeltaTime)
        {
            int isMore = (Convert.ToInt32(avgDeltaTime > (GOOD_DELTATIME)));
            int isLess = isMore == 1 ? 0 : 1;
            double r1 = avgDeltaTime;
            double ret1 = Math.Pow((GOOD_DELTATIME-r1),1.5);
            if (Double.IsNaN(ret1) == true) ret1 = 1;
            double mul = (90 + ret1) / (r1 + (isMore * Math.Pow((r1 - 90), 2)));
            double calc = (countHigher / (double)(timesCount)) * mul;
            double val = Math.Pow((calc * 100), 1.5); //Appreciates count of fastest objects in the beatmap;
            return calc * 100;
        }
    }
}
