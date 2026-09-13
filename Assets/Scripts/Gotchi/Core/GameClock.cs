using System;

namespace Gotchi.Core
{
    // Thin wrapper so systems can be tested with a fake clock.
    public class GameClock
    {
        public virtual DateTime UtcNow => DateTime.UtcNow;
        public virtual DateTime LocalNow => DateTime.Now;
    }
}
