using System;

namespace SteelRide.Core
{
    public interface IPressInput
    {
        event Action Pressed;
    }
}