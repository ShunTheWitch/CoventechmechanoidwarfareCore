using System.Collections.Generic;
using Verse;

namespace taranchuk_mechsuits
{
    public class MechPartSlot
    {
        public string label;
    }

    public class MechPart : BodyPartRecord
    {
        private List<MechPartSlot> customSlots;
        private bool isSlot;
        private MechPartSlot cachedSlot;
        public IEnumerable<MechPartSlot> AllSlots
        {
            get
            {
                if (customSlots != null)
                {
                    foreach (var slot in customSlots)
                    {
                        yield return slot;
                    }
                }

                if (isSlot)
                {
                    if (cachedSlot is null)
                    {
                        cachedSlot = new MechPartSlot
                        {
                            label = customLabel,
                        };
                    }
                    yield return cachedSlot;
                }
            }
        }
    }
}
