#pragma warning disable CS0162

using System;
using System.IO;

using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.IO.Serialization;
using osu.Game.Beatmaps;
using osu.Game;
using osu.Game.Screens.Play;

namespace BellaFioraUtils
{
    public static class Utils
    {
        public static OsuGame Osu = new OsuGame();
        public static void PrintError(string message, params object[] args)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message, args);
            Console.ForegroundColor = old;
        }

        public static int JsonifyOsuFile(string osu_path, string json_path)
        {
            try
            {
                using (var stream = File.OpenRead(osu_path))
                using (var reader = new LineBufferedReader(stream))
                {
                    var decoder = new LegacyBeatmapDecoder();
                    IBeatmap beatmap;
                    try
                    {
                        beatmap = decoder.Decode(reader);
                    }
                    catch (Exception e)
                    {
                        PrintError("Failed to parse beatmap: " + e);
                        return 1;
                    }
                    var a = new BellaFioraPlayer();
                    //WorkingBeatmap wbm = new WorkingBeatmap(beatmap.BeatmapInfo.Ruleset);
                    //WorkingBeatmap wbm = 
                    //GetPlayableBeatmap(beatmap.BeatmapInfo.Ruleset);
                    var processor = beatmap.BeatmapInfo.Ruleset.CreateInstance().CreateBeatmapProcessor(beatmap);
                    // there is no Taiko or Mania beatmap processor
                    if (processor != null)
                    {
                        processor.PreProcess();
                        processor.PostProcess();
                    }
                    string json;
                    try
                    {
                        json = beatmap.Serialize();
                    }
                    catch (Exception e)
                    {
                        PrintError("Beatmap.Serialize failed: " + e);
                        return 1;
                    }
                    try
                    {
                        File.WriteAllText(json_path, json);
                    }
                    catch (Exception e)
                    {
                        PrintError("File.WriteAllText failed: " + e);
                        return 1;
                    }
                    return 0;
                }
            }
            catch (FileNotFoundException)
            {
                PrintError("JsonifyOsuFile: {0} not found", osu_path);
                return 1;
            }
        }
    }
}
