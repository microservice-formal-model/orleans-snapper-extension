﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace Marketplace.Grains.Common
{
    public interface IMetadataGrain : IGrainWithIntegerKey
    {
        public Task Init(ActorSettings settings);

        public Task<IDictionary<string, int>> GetActorSettings(IList<string> actors);
    }
}

