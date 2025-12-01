using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilationLib
{
    public class DeployHandler
    {
        private readonly EsptoolWrapper _esptoolWrapper;
        public DeployHandler(EsptoolWrapper esptoolWrapper)
        {
            _esptoolWrapper = esptoolWrapper;
        }
        public async Task Deploy(string comPort, string chip, string pathToFile, CancellationToken cancellationToken)
        {
           await _esptoolWrapper.WriteFlush(comPort, chip, pathToFile, cancellationToken);
        }
    }
}
