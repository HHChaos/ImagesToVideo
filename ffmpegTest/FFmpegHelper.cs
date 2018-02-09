using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ffmpegTest
{
    public static class FFmpegHelper
    {
        /// <summary>
        /// 构造指定长度音频（输出长度无限制）
        /// </summary>
        /// <param name="ffmpegPath"></param>
        /// <param name="audioFilePath"></param>
        /// <param name="outputLength"></param>
        /// <param name="outputFilePath"></param>
        /// <returns></returns>
        public static bool ConstructAudio(string ffmpegPath, string audioFilePath, TimeSpan outputLength,
            string outputFilePath)
        {
            var file = new FileInfo(outputFilePath);
            var folder = file.Directory;
            if (!folder.Exists)
            {
                folder.Create();
            }
            var audioTmpFolder = folder.CreateSubdirectory($"audioTmp_{DateTime.Now.Ticks}");
            if (!audioTmpFolder.Exists)
            {
                return false;
            }
            try
            {
                var files = new List<string>();
                var audiaDuration = GetMediaDuration(ffmpegPath, audioFilePath);
                var count = (int)(outputLength.TotalSeconds / audiaDuration.TotalSeconds);
                if (count > 0)
                {
                    //todo convertAudio
                    var filePath = $"{audioTmpFolder.FullName}\\audioTmpPart_0.mp3";
                    if (ClipAudio(ffmpegPath, audioFilePath, audiaDuration, filePath))
                    {
                        for (var i = 0; i < count; i++)
                        {
                            files.Add("file 'audioTmpPart_0.mp3'");
                        }
                    }
                    else
                    {
                        return false;
                    }

                }
                var endPartLength = outputLength.TotalSeconds % audiaDuration.TotalSeconds;
                if (endPartLength > 0)
                {
                    //todo clipAudio
                    var filePath = $"{audioTmpFolder.FullName}\\audioTmpPart_1.mp3";
                    if (ClipAudio(ffmpegPath, audioFilePath, TimeSpan.FromSeconds(endPartLength), filePath))
                    {
                        files.Add("file 'audioTmpPart_1.mp3'");
                    }
                    else
                    {
                        return false;
                    }
                }
                if (files.Count > 0)
                {
                    using (var fs = new FileStream($"{audioTmpFolder.FullName}\\tmpList.txt", FileMode.Create,
                        FileAccess.Write))
                    using (var sw = new StreamWriter(fs))
                    {
                        foreach (var str in files)
                        {
                            sw.WriteLine(str);
                        }
                    }
                    using (var pro = new System.Diagnostics.Process())
                    {
                        pro.StartInfo.UseShellExecute = false;
                        pro.StartInfo.ErrorDialog = false;
                        pro.StartInfo.RedirectStandardError = true;
                        pro.StartInfo.CreateNoWindow = true;
                        pro.StartInfo.FileName = ffmpegPath;
                        pro.StartInfo.Arguments =
                            $"-f concat -i {audioTmpFolder.FullName.Replace('\\', '/')}/tmpList.txt -c copy -y {outputFilePath}";
                        pro.Start();
                        pro.WaitForExit();
                        var result = pro.StandardError.ReadToEnd();
                        if (string.IsNullOrEmpty(result)) return false;
                        result = result.Replace(" ", string.Empty);
                        if (result.LastIndexOf("time=", StringComparison.Ordinal) != -1)
                        {
                            result = result.Substring(result.LastIndexOf("time=", StringComparison.Ordinal) +
                                                      "time=".Length, "00:00:00".Length);
                            if (TimeSpan.TryParse(result, out var span))
                            {
                                if (Math.Abs(span.TotalSeconds - outputLength.TotalSeconds) < 1)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            finally
            {
                audioTmpFolder.Delete(true);
            }
        }
        /// <summary>
        /// 裁剪转码（转为192k无附加封面信息纯音频）音频（输出长度必须小于等于输入音频的长度）
        /// </summary>
        /// <param name="ffmpegPath"></param>
        /// <param name="audioFilePath"></param>
        /// <param name="outputLength"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        public static bool ClipAudio(string ffmpegPath, string audioFilePath, TimeSpan outputLength,
            string outputFile)
        {
            using (var pro = new System.Diagnostics.Process())
            {
                pro.StartInfo.UseShellExecute = false;
                pro.StartInfo.ErrorDialog = false;
                pro.StartInfo.RedirectStandardError = true;
                pro.StartInfo.CreateNoWindow = true;
                pro.StartInfo.FileName = ffmpegPath; ;
                pro.StartInfo.Arguments = $"-i {audioFilePath} -ss 00:00:00 -t {outputLength:hh\\:mm\\:ss} -y -vn -ar 44100 -ac 2 -ab 192k -f mp3 {outputFile}";

                pro.Start();
                pro.WaitForExit();
                var result = pro.StandardError.ReadToEnd();
                if (string.IsNullOrEmpty(result)) return false;
                result = result.Replace(" ", string.Empty);
                if (result.LastIndexOf("time=", StringComparison.Ordinal) != -1)
                {
                    result = result.Substring(result.LastIndexOf("time=", StringComparison.Ordinal) +
                                              "time=".Length, "00:00:00".Length);
                    if (TimeSpan.TryParse(result, out var span))
                    {
                        if (Math.Abs(span.TotalSeconds - outputLength.TotalSeconds) < 1)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 获取媒体长度信息
        /// </summary>
        /// <param name="ffmpegPath"></param>
        /// <param name="mediaFilePath"></param>
        /// <returns></returns>
        public static TimeSpan GetMediaDuration(string ffmpegPath, string mediaFilePath)
        {
            using (var pro = new System.Diagnostics.Process())
            {
                pro.StartInfo.UseShellExecute = false;
                pro.StartInfo.ErrorDialog = false;
                pro.StartInfo.RedirectStandardError = true;
                pro.StartInfo.CreateNoWindow = true;
                pro.StartInfo.FileName = ffmpegPath;
                pro.StartInfo.Arguments = $"-i {mediaFilePath}";

                pro.Start();
                pro.WaitForExit();

                var result = pro.StandardError.ReadToEnd();
                if (string.IsNullOrEmpty(result)) return TimeSpan.Zero;
                result = result.Replace(" ", string.Empty);
                if (result.IndexOf("Duration:", StringComparison.Ordinal) != -1)
                {
                    result = result.Substring(result.IndexOf("Duration:", StringComparison.Ordinal) +
                                              "Duration:".Length, "00:00:00".Length);
                    if (TimeSpan.TryParse(result, out var span))
                    {
                        return span;
                    }
                }
            }
            return TimeSpan.Zero;
        }
        /// <summary>
        /// 合成视频
        /// </summary>
        /// <param name="ffmpegPath"></param>
        /// <param name="picsPath">形如C:\export_%d.png，必须顺序命名</param>
        /// <param name="audioFilePath"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        public static bool ConvertVideo(string ffmpegPath, string picsPath, string audioFilePath,
            string outputFile)
        {
            using (var pro = new System.Diagnostics.Process())
            {
                pro.StartInfo.UseShellExecute = false;
                pro.StartInfo.ErrorDialog = false;
                pro.StartInfo.RedirectStandardError = true;
                pro.StartInfo.CreateNoWindow = true;
                pro.StartInfo.FileName = ffmpegPath; ;
                pro.StartInfo.Arguments = $"-threads 4 -y -r 25 -i {picsPath} -i {audioFilePath} -vcodec mpeg4 -b:v 2000k {outputFile}";

                pro.Start();
                pro.WaitForExit();
                var result = pro.StandardError.ReadToEnd();
                if (string.IsNullOrEmpty(result)) return false;
                result = result.Replace(" ", string.Empty);
                if (result.LastIndexOf("time=", StringComparison.Ordinal) != -1)
                {
                    result = result.Substring(result.LastIndexOf("time=", StringComparison.Ordinal) +
                                              "time=".Length, "00:00:00".Length);
                    if (TimeSpan.TryParse(result, out var span))
                    {
                        if (span.TotalSeconds > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
