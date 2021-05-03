﻿using System;

namespace Knapcode.ExplorePackages
{
    public interface IIncrementalHash : IDisposable
    {
        HashOutput Output { get; }
        void TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount);
        void TransformFinalBlock();
    }
}