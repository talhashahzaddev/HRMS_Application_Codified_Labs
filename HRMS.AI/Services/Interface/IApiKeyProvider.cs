using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.AI.Services.Interface
{
    public interface IApiKeyProvider
    {
        string GetCurrentKey();
        void RotateKey();

    }
}
