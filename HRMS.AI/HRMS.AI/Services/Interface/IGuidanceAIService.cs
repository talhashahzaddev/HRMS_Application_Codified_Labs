using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.AI.Services.Interface
{
    public interface IGuidanceAIService
    {
        Task<string> GenerateGuidanceAsync(string message, string module);
    }
}
