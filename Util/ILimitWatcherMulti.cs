using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MultiFloorStorage.Util
{
    public interface ILimitWatcherMulti
    {
        public bool ItemIsLimit(ThingDef thing, bool CntStacks, int limit);

    }
}