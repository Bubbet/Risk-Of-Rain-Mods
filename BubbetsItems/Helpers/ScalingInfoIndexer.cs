using System.Collections.Generic;
using System.Linq;

namespace BubbetsItems.Helpers;

public class ScalingInfoIndexer : List<ItemBase.ScalingInfo>
{
    public ItemBase.ScalingInfo this[string which] => this.First(x => x._name == which);
}