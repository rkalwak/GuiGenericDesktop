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
