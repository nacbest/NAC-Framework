using System.Threading.Channels;

namespace Nac.EventBus.InMemory;

public sealed class InMemoryEventBusOptions
{
    public int ChannelCapacity { get; set; } = 1000;
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;
}
