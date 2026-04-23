using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JiebaNet.Segmenter;

namespace JiebaNet.Analyser
{
    public class IdfLoader
    {
        internal string IdfFilePath { get; set; }
        internal IDictionary<string, double> IdfFreq { get; set; }
        internal double MedianIdf { get; set; }

        public IdfLoader(string? idfPath = null)
        {
            IdfFilePath = string.Empty;
            IdfFreq = new Dictionary<string, double>();
            MedianIdf = 0.0;
            if (!string.IsNullOrWhiteSpace(idfPath))
            {
                SetNewPath(idfPath!);
            }
        }

        public void SetNewPath(string newIdfPath)
        {
            var idfPath = Path.GetFullPath(newIdfPath);
            if (IdfFilePath != idfPath)
            {
                IdfFilePath = idfPath;
                IdfFreq = new Dictionary<string, double>();
                
                // 加载简体中文IDF
                LoadIdfFile(idfPath);
                
                // 加载繁体中文IDF
                var idfHantPath = ConfigManager.IdfHantFile;
                LoadIdfFile(idfHantPath);

                if (IdfFreq.Count > 0)
                {
                    MedianIdf = IdfFreq.Values.OrderBy(v => v).ToList()[IdfFreq.Count / 2];
                }
            }
        }

        /// <summary>
        /// 加载IDF文件
        /// </summary>
        /// <param name="idfPath">IDF文件路径</param>
        private void LoadIdfFile(string idfPath)
        {
            if (!File.Exists(idfPath))
            {
                return;
            }

            var lines = File.ReadAllLines(idfPath, Encoding.UTF8);
            foreach (var line in lines)
            {
                var parts = line.Trim().Split(' ');
                if (parts.Length < 2)
                {
                    continue;
                }
                var word = parts[0];
                if (double.TryParse(parts[1], out var freq))
                {
                    IdfFreq[word] = freq;
                }
            }
        }
    }
}