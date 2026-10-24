using System;

namespace SteelRide.Core
{
    public interface IDefeatPresentation
    {
        bool IsComplete { get; }

        event Action Completed;
    }
}