using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Interfaces
{
    public interface ILoggerService
    {
        public Logger _logger { get; set; }
        void GetInstance();
    }
}
