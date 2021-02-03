using System.Threading;
using System.Threading.Tasks;

namespace CanisLupus
{
    public interface IMarketMakerHandler
    {
         Task ExecuteAsync(CancellationToken stoppingToken);
    }
}