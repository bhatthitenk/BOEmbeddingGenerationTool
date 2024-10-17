using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Models
{
    public class WriteToFileModel
    {
        public string ModelName { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
        public string FilePath {  get; set; }
        public Prompts Prompts { get; set; }
        public string Response {  get; set; }
    }

    public class Prompts
    {
        public string SystemPrompt { get; set; }
        public string UserPrompt { get; set; }
    }
}
