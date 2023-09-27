using Godot;
using System;

namespace ThousandYearsHome.Extensions
{
    public class Numerology
    {
        // TODO: Thread-safety?
        private static Random _random = new Random();

        /// <summary>
        /// Returns a random float between <paramref name="min"/> and <paramref name="max"/>, inclusive.
        /// </summary>
        public static float RandRange(float min, float max) => (float)_random.NextDouble() * (max - min) + min;        
    }
}
