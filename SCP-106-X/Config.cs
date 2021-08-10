using Exiled.API.Interfaces;

namespace Scp106X
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public float StalkCooldown { get; set; } = 60f;
    }
}