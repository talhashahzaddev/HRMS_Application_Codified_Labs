using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.AI.Services.Interface
{
    public interface IIntentRecognitionService
    {
        Task<IntentDetectionResult> DetectIntentAsync(string message);
    }
}
